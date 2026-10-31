using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StoneGame.Cutting
{
    public sealed partial class CutStoneHalf
    {
        // Immutable source geometry plus a deterministic per-stone deformed surface.
        // Source snapshots never serialize already-deformed vertices a second time.
        [Serializable] sealed class ChipMark { public Vector3 point,normal; }
        readonly List<ChipMark> chipMarks=new List<ChipMark>();
        Mesh chipDisplayMesh;
        List<int>[] chipSurfaceTriangles;
        Vector3[] chipVertices;
        MeshFilter chipFilter;
        public Mesh ChippingSurfaceMesh=>chipDisplayMesh!=null?chipDisplayMesh:ownedMesh;
        public int ChippingGeometryRevision{get;private set;}
        sealed class ColliderSizeState
        {
            public BoxCollider box;public Vector3 center,size;
            public MeshCollider collider;public Mesh source,scaled;
        }
        readonly List<ColliderSizeState> sizedColliders=new List<ColliderSizeState>();

        static bool ValidChipMark(ChipMark mark)=>mark!=null && Finite(mark.point) && Finite(mark.normal) && mark.normal.sqrMagnitude>.01f;
        static bool Finite(Vector3 v)=>float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);

        void ClearChipPatches()
        {
            RestoreChippingColliders();
            if(chipFilter!=null && chipFilter.sharedMesh==chipDisplayMesh)chipFilter.sharedMesh=ownedMesh;
            if(chipDisplayMesh!=null)StoneMeshCutResult.DestroyMesh(chipDisplayMesh);
            chipDisplayMesh=null;chipSurfaceTriangles=null;chipVertices=null;chipFilter=null;chipMarks.Clear();ChippingGeometryRevision++;
        }

        void ApplyChipPatches()
        {
            if(chipMarks.Count==0 && !IsChippingCompleted)return;
            if(chipDisplayMesh==null && !BuildChipDisplayMesh())return;
            var exposed=new List<int>();
            float radius=Mathf.Max(ownedMesh.bounds.size.magnitude*.16f,.0001f);
            for(int sub=0;sub<chipSurfaceTriangles.Length;sub++)
            {
                var source=chipSurfaceTriangles[sub];var remaining=new List<int>(source.Count);
                // Include the nearest eligible triangle even on very coarse meshes.
                var nearest=new int[chipMarks.Count];var distances=new float[chipMarks.Count];
                for(int m=0;m<nearest.Length;m++){nearest[m]=-1;distances[m]=float.PositiveInfinity;}
                var reveal=new bool[source.Count/3];
                for(int i=0;i<source.Count;i+=3)
                {
                    var a=chipVertices[source[i]];var b=chipVertices[source[i+1]];var c=chipVertices[source[i+2]];
                    var center=(a+b+c)/3;var normal=Vector3.Cross(b-a,c-a).normalized;
                    for(int m=0;m<chipMarks.Count;m++)
                    {
                        var mark=chipMarks[m];if(Vector3.Dot(normal,mark.normal)<.1f)continue;
                        float d=(center-mark.point).sqrMagnitude;
                        if(d<distances[m]){distances[m]=d;nearest[m]=i/3;}
                        // Uneven edges keep individual chips from looking like perfect circles.
                        float jagged=.85f+.3f*((uint)(i*1664525+sub*1013904223)&1023)/1023f;
                        if(d<radius*radius*jagged)reveal[i/3]=true;
                    }
                }
                for(int m=0;m<nearest.Length;m++)if(nearest[m]>=0 && distances[m]<radius*radius*2.25f)reveal[nearest[m]]=true;
                for(int i=0;i<source.Count;i+=3)
                {
                    var target=reveal[i/3]?exposed:remaining;target.Add(source[i]);target.Add(source[i+1]);target.Add(source[i+2]);
                }
                chipDisplayMesh.SetTriangles(remaining,sub,false);
            }
            chipDisplayMesh.SetTriangles(exposed,chipSurfaceTriangles.Length,false);
            var renderer=GetComponent<MeshRenderer>();var materials=new Material[chipSurfaceTriangles.Length+1];
            var originals=originalMaterials!=null && originalMaterials.Length>0?originalMaterials[0]:renderer.sharedMaterials;
            for(int i=0;i<materials.Length-1;i++)materials[i]=originals!=null && i<originals.Length?originals[i]:cutSurfaceMaterial;
            materials[materials.Length-1]=cutSurfaceMaterial;renderer.sharedMaterials=materials;
            ApplyChipDents(radius);
            chipFilter.sharedMesh=chipDisplayMesh;
            if(IsChippingCompleted)ResizeChippingColliders();
        }

        void ApplyChipDents(float radius)
        {
            var vertices=(Vector3[])chipVertices.Clone();float depth=ownedMesh.bounds.size.magnitude*dentDepth;
            float radiusSquared=radius*radius;var planeNormal=CutSurfaceNormal.normalized;
            for(int i=0;i<vertices.Length;i++)
            {
                var p=vertices[i];var source=p;
                foreach(var mark in chipMarks)
                {
                    float distance=(p-mark.point).sqrMagnitude;if(distance>=radiusSquared)continue;
                    float falloff=1-distance/radiusSquared;falloff*=falloff;
                    // Identical positions receive identical offsets even across UV/material
                    // seams, so the dent cannot open cracks between duplicated vertices.
                    p-=mark.normal.normalized*(depth*falloff);
                }
                var displacement=p-source;float maxDepth=radius*.45f;
                if(displacement.sqrMagnitude>maxDepth*maxDepth)p=source+displacement.normalized*maxDepth;
                // Do not push any vertex below the original tabletop seating plane.
                float planeLimit=Mathf.Min(0,Vector3.Dot(source-CutSurfaceCenter,planeNormal)*.5f);
                float below=Vector3.Dot(p-CutSurfaceCenter,planeNormal)-planeLimit;
                if(below>0)p-=planeNormal*below;
                if(IsChippingCompleted)p=CutSurfaceCenter+(p-CutSurfaceCenter)*finalSize;
                vertices[i]=p;
            }
            chipDisplayMesh.vertices=vertices;chipDisplayMesh.RecalculateBounds();chipDisplayMesh.RecalculateNormals();chipDisplayMesh.RecalculateTangents();
            ChippingGeometryRevision++;
        }

        void ResizeChippingColliders()
        {
            if(sizedColliders.Count>0 || Mathf.Approximately(finalSize,1))return;
            foreach(var box in GetComponents<BoxCollider>())
            {
                if(box.isTrigger)continue;
                sizedColliders.Add(new ColliderSizeState{box=box,center=box.center,size=box.size});
                box.center=CutSurfaceCenter+(box.center-CutSurfaceCenter)*finalSize;box.size*=finalSize;
            }
            foreach(var collider in GetComponents<MeshCollider>())
            {
                if(collider.isTrigger || collider.sharedMesh==null)continue;
                var source=collider.sharedMesh;var scaled=Instantiate(source);scaled.name=source.name+"_ChippedPhysics";
                var vertices=scaled.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=CutSurfaceCenter+(vertices[i]-CutSurfaceCenter)*finalSize;
                scaled.vertices=vertices;scaled.RecalculateBounds();
                sizedColliders.Add(new ColliderSizeState{collider=collider,source=source,scaled=scaled});collider.sharedMesh=scaled;
            }
        }
        void RestoreChippingColliders()
        {
            foreach(var state in sizedColliders)
            {
                if(state.box!=null){state.box.center=state.center;state.box.size=state.size;}
                if(state.collider!=null)state.collider.sharedMesh=state.source;
                if(state.scaled!=null)StoneMeshCutResult.DestroyMesh(state.scaled);
            }
            sizedColliders.Clear();
        }

        bool BuildChipDisplayMesh()
        {
            chipFilter=GetComponent<MeshFilter>();var renderer=GetComponent<MeshRenderer>();
            if(chipFilter==null || renderer==null || ownedMesh==null || !ownedMesh.isReadable)return false;
            var vertices=new List<Vector3>(ownedMesh.vertices);var normals=new List<Vector3>(ownedMesh.normals);
            var tangents=new List<Vector4>(ownedMesh.tangents);var colors=new List<Color>(ownedMesh.colors);
            var uv=new List<Vector4>[8];for(int i=0;i<uv.Length;i++){uv[i]=new List<Vector4>();ownedMesh.GetUVs(i,uv[i]);}
            bool hasNormals=normals.Count==vertices.Count,hasTangents=tangents.Count==vertices.Count,hasColors=colors.Count==vertices.Count;
            var hasUV=new bool[8];for(int i=0;i<uv.Length;i++)hasUV[i]=uv[i].Count==vertices.Count;
            chipSurfaceTriangles=new List<int>[ownedMesh.subMeshCount];int triangleCount=0;
            for(int s=0;s<chipSurfaceTriangles.Length;s++){chipSurfaceTriangles[s]=new List<int>(ownedMesh.GetTriangles(s));triangleCount+=chipSurfaceTriangles[s].Count/3;}
            // Bounded, one-time subdivision gives small localized patches on coarse rocks.
            // No mesh rebuilding or allocation is performed per frame.
            for(int level=0;level<2 && triangleCount*4<=12000;level++)
            {
                var midpoints=new Dictionary<ulong,int>();
                int Mid(int a,int b)
                {
                    ulong key=((ulong)(uint)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);
                    if(midpoints.TryGetValue(key,out int index))return index;
                    index=vertices.Count;midpoints.Add(key,index);vertices.Add((vertices[a]+vertices[b])*.5f);
                    if(hasNormals)normals.Add((normals[a]+normals[b]).normalized);
                    if(hasTangents){var t=(tangents[a]+tangents[b])*.5f;var direction=new Vector3(t.x,t.y,t.z).normalized;tangents.Add(new Vector4(direction.x,direction.y,direction.z,tangents[a].w));}
                    if(hasColors)colors.Add((colors[a]+colors[b])*.5f);
                    for(int u=0;u<uv.Length;u++)if(hasUV[u])uv[u].Add((uv[u][a]+uv[u][b])*.5f);
                    return index;
                }
                for(int s=0;s<chipSurfaceTriangles.Length;s++)
                {
                    var source=chipSurfaceTriangles[s];var split=new List<int>(source.Count*4);
                    for(int i=0;i<source.Count;i+=3)
                    {
                        int a=source[i],b=source[i+1],c=source[i+2],ab=Mid(a,b),bc=Mid(b,c),ca=Mid(c,a);
                        split.Add(a);split.Add(ab);split.Add(ca);split.Add(ab);split.Add(b);split.Add(bc);
                        split.Add(ca);split.Add(bc);split.Add(c);split.Add(ab);split.Add(bc);split.Add(ca);
                    }
                    chipSurfaceTriangles[s]=split;
                }
                triangleCount*=4;
            }
            chipDisplayMesh=new Mesh{name=ownedMesh.name+"_ChippingSurface",indexFormat=IndexFormat.UInt32};
            chipDisplayMesh.SetVertices(vertices);if(hasNormals)chipDisplayMesh.SetNormals(normals);if(hasTangents)chipDisplayMesh.SetTangents(tangents);if(hasColors)chipDisplayMesh.SetColors(colors);
            for(int u=0;u<uv.Length;u++)if(hasUV[u])chipDisplayMesh.SetUVs(u,uv[u]);
            chipDisplayMesh.subMeshCount=chipSurfaceTriangles.Length+1;
            if(!hasNormals){for(int s=0;s<chipSurfaceTriangles.Length;s++)chipDisplayMesh.SetTriangles(chipSurfaceTriangles[s],s,false);chipDisplayMesh.RecalculateNormals();}
            chipDisplayMesh.bounds=ownedMesh.bounds;chipVertices=vertices.ToArray();return true;
        }
    }
}
