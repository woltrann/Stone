using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StoneGame.Cutting
{
    [Serializable]
    public sealed class StoneMeshSnapshot
    {
        [Serializable] public sealed class Submesh { public int[] indices; }
        public Vector3[] vertices, normals;
        public Vector2[] uv;
        public Color[] colors;
        public Submesh[] submeshes;
        public Vector3 surfaceCenter, surfaceNormal;
        public float surfaceRadius, volume;
        public string[] materialNames;
        public int surfaceMetadataVersion,cutSurfaceSubmesh;
        public string cutSurfaceMaterialId;
        public bool HasGeometry => vertices != null && vertices.Length >= 4 && submeshes != null && submeshes.Length > 0;

        public static StoneMeshSnapshot Capture(CutStoneHalf half)
        {
            var mesh = half.GeneratedMesh;
            if (mesh == null) return null;
            var data = new StoneMeshSnapshot { vertices=mesh.vertices, normals=mesh.normals, uv=mesh.uv,
                colors=mesh.colors, submeshes=new Submesh[mesh.subMeshCount], surfaceCenter=half.CutSurfaceCenter,
                surfaceNormal=half.CutSurfaceNormal, surfaceRadius=half.CutSurfaceRadius, volume=half.Volume,
                surfaceMetadataVersion=1,cutSurfaceSubmesh=half.CutSurfaceSubmesh,cutSurfaceMaterialId=half.CutSurfaceMaterialId };
            for(int i=0;i<mesh.subMeshCount;i++) data.submeshes[i]=new Submesh {indices=mesh.GetTriangles(i)};
            var materials=half.GetComponent<MeshRenderer>().sharedMaterials;
            data.materialNames=new string[materials.Length];
            for(int i=0;i<materials.Length;i++) data.materialNames[i]=materials[i]!=null?materials[i].name:null;
            return data;
        }

        public Mesh ToMesh()
        {
            if(vertices==null || vertices.Length<4 || submeshes==null) throw new ArgumentException("Saved cut mesh is incomplete.");
            var mesh=new Mesh {name="Restored stone cut",indexFormat=vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.vertices=vertices; mesh.normals=normals; mesh.uv=uv;
            if(colors!=null && colors.Length==vertices.Length) mesh.colors=colors;
            mesh.subMeshCount=submeshes.Length;
            for(int i=0;i<submeshes.Length;i++) mesh.SetTriangles(submeshes[i].indices,i,false);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            return mesh;
        }
    }

    public static class StoneMeshTopology
    {
        internal sealed class Welder
        {
            readonly float tolerance;
            readonly Dictionary<Vector3Int,List<int>> buckets=new Dictionary<Vector3Int,List<int>>();
            readonly Dictionary<Vector3,int> exactPoints=new Dictionary<Vector3,int>();
            internal readonly List<Vector3> Points=new List<Vector3>();
            internal Welder(float tolerance) { this.tolerance=Mathf.Max(tolerance,1e-8f); }
            internal int Add(Vector3 point)
            {
                // Triangle-soup outputs repeat most positions verbatim; avoid 27 spatial probes for each copy.
                if(exactPoints.TryGetValue(point,out int exact)) return exact;
                var key=new Vector3Int(Mathf.RoundToInt(point.x/tolerance),Mathf.RoundToInt(point.y/tolerance),Mathf.RoundToInt(point.z/tolerance));
                for(int x=-1;x<=1;x++) for(int y=-1;y<=1;y++) for(int z=-1;z<=1;z++)
                    if(buckets.TryGetValue(key+new Vector3Int(x,y,z),out var ids))
                        foreach(int id in ids) if((Points[id]-point).sqrMagnitude<=tolerance*tolerance) { exactPoints[point]=id; return id; }
                int index=Points.Count; Points.Add(point);
                if(!buckets.TryGetValue(key,out var bucket)) buckets[key]=bucket=new List<int>(1);
                bucket.Add(index); exactPoints[point]=index; return index;
            }
        }
        struct Edge { internal int Count, Direction, A, B; }
        static ulong Key(int a,int b) => ((ulong)(uint)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);
        static void AddEdge(Dictionary<ulong,Edge> edges,int a,int b)
        {
            var key=Key(a,b); edges.TryGetValue(key,out var edge);
            edge.Count++; edge.Direction+=a<b?1:-1; edge.A=a; edge.B=b; edges[key]=edge;
        }

        public static bool ValidateClosed(Mesh mesh,float tolerance,out float volume,out string error)
        {
            volume=0; error=null;
            if(mesh==null || !mesh.isReadable) { error="Mesh is missing or not readable. Enable Read/Write on the source model."; return false; }
            var vertices=mesh.vertices; var weld=new Welder(tolerance); var ids=new int[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var p=vertices[i];
                if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)) { error="Mesh contains NaN/Infinity vertices."; return false; }
                ids[i]=weld.Add(p);
            }
            var edges=new Dictionary<ulong,Edge>(); double signedVolume=0;
            var origin=mesh.bounds.center; int triangles=0;
            for(int s=0;s<mesh.subMeshCount;s++)
            {
                if(mesh.GetTopology(s)!=MeshTopology.Triangles) { error="Only triangle submeshes are supported."; return false; }
                var indices=mesh.GetTriangles(s);
                for(int t=0;t<indices.Length;t+=3)
                {
                    int a=indices[t],b=indices[t+1],c=indices[t+2];
                    if(ids[a]==ids[b] || ids[b]==ids[c] || ids[a]==ids[c])
                    { error="Degenerate triangles at the configured welding tolerance. Reduce tolerance or repair the source."; return false; }
                    AddEdge(edges,ids[a],ids[b]); AddEdge(edges,ids[b],ids[c]); AddEdge(edges,ids[c],ids[a]);
                    Vector3 pa=vertices[a]-origin,pb=vertices[b]-origin,pc=vertices[c]-origin;
                    signedVolume+=((double)pa.x*(pb.y*pc.z-pb.z*pc.y)+(double)pa.y*(pb.z*pc.x-pb.x*pc.z)+(double)pa.z*(pb.x*pc.y-pb.y*pc.x))/6.0;
                    triangles++;
                }
            }
            foreach(var edge in edges.Values)
                if(edge.Count!=2 || edge.Direction!=0) { error=$"Mesh is open, non-manifold, or has inconsistent winding (edge uses {edge.Count}, orientation sum {edge.Direction})."; return false; }
            volume=(float)Math.Abs(signedVolume);
            if(triangles==0 || volume<=tolerance*tolerance*tolerance*10f) { error="Mesh has no meaningful closed volume."; return false; }
            return true;
        }

        public static bool FindCutSurface(Mesh mesh,Vector3 normal,float tolerance,out Vector3 center,out float radius)
        {
            var vertices=mesh.vertices; var indices=mesh.GetTriangles(mesh.subMeshCount-1);
            center=Vector3.zero; radius=0;
            var weld=new Welder(tolerance); var edges=new Dictionary<ulong,Edge>();
            var candidates=new List<Vector3>(); Vector3 average=Vector3.zero; float totalArea=0,largestArea=0;
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=vertices[indices[i]]; var b=vertices[indices[i+1]]; var c=vertices[indices[i+2]];
                float area=Vector3.Cross(b-a,c-a).magnitude;
                var centroid=(a+b+c)/3f;
                average+=centroid*area; totalArea+=area;
                if(area>largestArea) { largestArea=area; center=centroid; }
                int ia=weld.Add(a),ib=weld.Add(b),ic=weld.Add(c);
                AddEdge(edges,ia,ib); AddEdge(edges,ib,ic); AddEdge(edges,ic,ia);
            }
            if(totalArea<=0) return false;
            candidates.Add(center); candidates.Add(average/totalArea);
            Vector3 axisU=Vector3.Cross(Mathf.Abs(normal.y)<.9f?Vector3.up:Vector3.right,normal).normalized;
            Vector3 axisV=Vector3.Cross(normal,axisU);
            float minU=float.PositiveInfinity,maxU=float.NegativeInfinity,minV=minU,maxV=maxU;
            foreach(var p in weld.Points)
            {
                float u=Vector3.Dot(p,axisU),v=Vector3.Dot(p,axisV);
                minU=Mathf.Min(minU,u); maxU=Mathf.Max(maxU,u); minV=Mathf.Min(minV,v); maxV=Mathf.Max(maxV,v);
            }
            float planeD=Vector3.Dot(center,normal);
            for(int u=0;u<7;u++) for(int v=0;v<7;v++)
                candidates.Add(axisU*Mathf.Lerp(minU,maxU,(u+.5f)/7)+axisV*Mathf.Lerp(minV,maxV,(v+.5f)/7)+normal*planeD);
            foreach(var candidate in candidates)
            {
                bool inside=false;
                for(int i=0;i<indices.Length;i+=3)
                {
                    var a=vertices[indices[i]]; var b=vertices[indices[i+1]]; var c=vertices[indices[i+2]];
                    if(Vector3.Dot(Vector3.Cross(b-a,candidate-a),normal)>=-tolerance*tolerance &&
                        Vector3.Dot(Vector3.Cross(c-b,candidate-b),normal)>=-tolerance*tolerance &&
                        Vector3.Dot(Vector3.Cross(a-c,candidate-c),normal)>=-tolerance*tolerance) { inside=true; break; }
                }
                if(!inside) continue;
                float clearance=float.PositiveInfinity;
                foreach(var edge in edges.Values)
                {
                    if(edge.Count!=1) continue;
                    var a=weld.Points[edge.A]; var delta=weld.Points[edge.B]-a;
                    float t=Mathf.Clamp01(Vector3.Dot(candidate-a,delta)/delta.sqrMagnitude);
                    clearance=Mathf.Min(clearance,Vector3.Distance(candidate,a+delta*t));
                }
                if(clearance>radius && float.IsFinite(clearance)) { center=candidate; radius=clearance; }
            }
            return radius>tolerance;
        }
    }
}
