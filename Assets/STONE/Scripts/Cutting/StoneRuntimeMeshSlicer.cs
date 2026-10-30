using System;
using System.Collections.Generic;
using ProceduralRocks;
using TidyUpFramework.Objects;
using UnityEngine;
using UnityEngine.Rendering;

namespace StoneGame.Cutting
{
    [Serializable]
    public sealed class StoneMeshCutSettings
    {
        [Range(0.0000001f,0.001f),Tooltip("Relative to the baked mesh bounds diagonal.")] public float relativeTolerance=0.00001f;
        [Range(0.0001f,0.1f)] public float minimumVolumeFraction=0.001f;
        [Min(100)] public int maximumSourceTriangles=50000;
        [Range(16,4096)] public int maximumContourVertices=2048;
        [Min(0)] public float pieceSeparation=.04f;
        [Range(2,6),Tooltip("Complex dynamic pieces use a conservative compound voxel-box collider.")] public int compoundColliderResolution=4;
        [Range(20,200)] public int convexTriangleLimit=180;
        [Range(.05f,.95f)] public float oreRadiusFraction=.65f;
        public Vector2 oreSurfaceOffset;
        public Vector3 oreEulerOffset;
        [Tooltip("Normal offset in metres; zero embeds the visual halfway into the cut face.")] public float oreNormalOffset;
    }

    public sealed class StoneMeshCutResult : IDisposable
    {
        public Mesh Negative, Positive;
        public Material[] Materials;
        public int CutSurfaceSubmesh;
        public Vector3 NegativeSurfaceCenter, PositiveSurfaceCenter, Normal;
        public float NegativeRadius, PositiveRadius, NegativeVolume, PositiveVolume;
        public Quaternion SourceRotation;
        public void Dispose() { DestroyMesh(Negative); DestroyMesh(Positive); Negative=Positive=null; }
        internal static void DestroyMesh(UnityEngine.Object obj)
        {
            if(obj==null) return;
            if(Application.isPlaying) UnityEngine.Object.Destroy(obj); else UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    public static class StoneRuntimeMeshSlicer
    {
        public static Vector3 PlaneNormal(Transform plane)
        {
            return plane != null ? plane.localToWorldMatrix.inverse.transpose.MultiplyVector(Vector3.right).normalized : Vector3.zero;
        }

        public static bool TrySlice(TidyObject source,Vector3 worldPoint,Vector3 worldNormal,Material cutMaterial,
            StoneMeshCutSettings settings,out StoneMeshCutResult result,out string error)
        {
            result=null; error=null; Mesh combined=null,negative=null,positive=null;
            if(source==null || settings==null || worldNormal.sqrMagnitude<1e-10f) { error="Source, settings and a valid CuttingPlane normal are required."; return false; }
            try
            {
                combined=BakeSource(source,settings.maximumSourceTriangles,out var materials);
                float epsilon=Mathf.Max(combined.bounds.size.magnitude*settings.relativeTolerance,1e-7f);
                if(!StoneMeshTopology.ValidateClosed(combined,epsilon*.5f,out var originalVolume,out error)) { error="Source: "+error; return false; }
                Quaternion rotation=source.transform.rotation;
                var frame=Matrix4x4.TRS(source.transform.position,rotation,Vector3.one);
                Vector3 point=frame.inverse.MultiplyPoint3x4(worldPoint);
                Vector3 normal=Quaternion.Inverse(rotation)*worldNormal.normalized;
                if(!RockMeshSlicer.TryKeepNegative(combined,point,normal,out negative,out error,settings.relativeTolerance,true,settings.maximumContourVertices)) return false;
                if(!RockMeshSlicer.TryKeepNegative(combined,point,-normal,out positive,out error,settings.relativeTolerance,true,settings.maximumContourVertices)) return false;
                if(!StoneMeshTopology.ValidateClosed(negative,epsilon*2f,out var leftVolume,out error)) { error="Negative cut result: "+error; return false; }
                if(!StoneMeshTopology.ValidateClosed(positive,epsilon*2f,out var rightVolume,out error)) { error="Positive cut result: "+error; return false; }
                if(leftVolume<originalVolume*settings.minimumVolumeFraction || rightVolume<originalVolume*settings.minimumVolumeFraction)
                { error="CuttingPlane produces an insignificant sliver. Move the plane further inside the stone."; return false; }
                if(Mathf.Abs(leftVolume+rightVolume-originalVolume)>originalVolume*.005f)
                { error="Cut volumes do not reconstruct the source. Check mesh winding or tolerance."; return false; }
                if(!StoneMeshTopology.FindCutSurface(negative,normal,epsilon*2f,out var leftCenter,out var leftRadius) ||
                    !StoneMeshTopology.FindCutSurface(positive,-normal,epsilon*2f,out var rightCenter,out var rightRadius))
                { error="Could not find a safe point inside the generated cut surface."; return false; }
                materials.Add(cutMaterial!=null?cutMaterial:materials[0]);
                // RockMeshSlicer writes caps into input.subMeshCount (its new dedicated submesh).
                result=new StoneMeshCutResult { Negative=negative,Positive=positive,Materials=materials.ToArray(),Normal=normal,CutSurfaceSubmesh=combined.subMeshCount,
                    NegativeSurfaceCenter=leftCenter,PositiveSurfaceCenter=rightCenter,NegativeRadius=leftRadius,PositiveRadius=rightRadius,
                    NegativeVolume=leftVolume,PositiveVolume=rightVolume,SourceRotation=rotation };
                negative=positive=null;
                return true;
            }
            catch(Exception exception) { error=exception.Message; return false; }
            finally { StoneMeshCutResult.DestroyMesh(combined); StoneMeshCutResult.DestroyMesh(negative); StoneMeshCutResult.DestroyMesh(positive); }
        }

        public static Material[] SourceMaterials(TidyObject source)
        {
            var materials=new List<Material>();
            foreach(var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                var slots=renderer.sharedMaterials;
                materials.AddRange(slots);
            }
            return materials.ToArray();
        }

        static Mesh BakeSource(TidyObject source,int maximumTriangles,out List<Material> materials)
        {
            if(source.GetComponentInChildren<SkinnedMeshRenderer>()!=null) throw new ArgumentException("Skinned meshes are not supported. Use static MeshFilter rocks.");
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var uv=new List<Vector2>(); var colors=new List<Color>();
            var submeshes=new List<int[]>(); materials=new List<Material>();
            var frame=Matrix4x4.TRS(source.transform.position,source.transform.rotation,Vector3.one).inverse;
            int triangleCount=0;
            foreach(var filter in source.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null || !renderer.enabled) continue;
                var mesh=filter.sharedMesh;
                if(mesh==null || !mesh.isReadable) throw new ArgumentException($"'{filter.name}' has no readable mesh. Enable Read/Write in Model Import Settings.");
                var matrix=frame*filter.transform.localToWorldMatrix;
                if(Mathf.Abs(matrix.determinant)<1e-12f) throw new ArgumentException("Zero or near-zero transform scale is not supported.");
                var normalMatrix=matrix.inverse.transpose; bool mirrored=matrix.determinant<0;
                var points=mesh.vertices; var ns=mesh.normals; var uvs=mesh.uv; var cs=mesh.colors;
                int offset=vertices.Count;
                for(int i=0;i<points.Length;i++)
                {
                    vertices.Add(matrix.MultiplyPoint3x4(points[i]));
                    normals.Add(ns.Length==points.Length?normalMatrix.MultiplyVector(ns[i]).normalized:Vector3.zero);
                    uv.Add(uvs.Length==points.Length?uvs[i]:Vector2.zero);
                    colors.Add(cs.Length==points.Length?cs[i]:Color.white);
                }
                var slots=renderer.sharedMaterials;
                for(int s=0;s<mesh.subMeshCount;s++)
                {
                    if(mesh.GetTopology(s)!=MeshTopology.Triangles) throw new ArgumentException("Only triangle topology is supported.");
                    var indices=mesh.GetTriangles(s); triangleCount+=indices.Length/3;
                    if(triangleCount>maximumTriangles) throw new ArgumentException($"Source exceeds the configured {maximumTriangles} triangle safety limit.");
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        int a=indices[i]+offset,b=indices[i+1]+offset,c=indices[i+2]+offset;
                        indices[i]=a; indices[i+1]=mirrored?c:b; indices[i+2]=mirrored?b:c;
                        if(ns.Length!=points.Length)
                        {
                            var n=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).normalized*(mirrored?-1:1);
                            normals[a]+=n; normals[b]+=n; normals[c]+=n;
                        }
                    }
                    submeshes.Add(indices);
                    var material=slots.Length>0?slots[Mathf.Min(s,slots.Length-1)]:null;
                    if(material==null) throw new ArgumentException($"Mesh '{filter.name}' submesh {s} needs a material.");
                    materials.Add(material);
                }
                if(ns.Length!=points.Length)
                    for(int i=offset;i<vertices.Count;i++) normals[i]=normals[i].normalized;
            }
            if(vertices.Count==0) throw new ArgumentException("No enabled static MeshFilter/MeshRenderer pair was found.");
            var combined=new Mesh {name=source.name+"_BakedForCut",indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            combined.SetVertices(vertices); combined.SetNormals(normals); combined.SetUVs(0,uv); combined.SetColors(colors);
            combined.subMeshCount=submeshes.Count;
            for(int i=0;i<submeshes.Count;i++) combined.SetTriangles(submeshes[i],i,false);
            combined.RecalculateBounds(); return combined;
        }
    }
}
