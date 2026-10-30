using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using StoneGame.Cutting;
using TidyUpFramework.Objects;
using ProceduralRocks;

public static class RuntimeSliceGeometryChecks
{
    static readonly List<string> passed=new List<string>();
    static void Check(bool ok,string label) { if(!ok) throw new Exception(label); passed.Add(label); }
    public static Mesh LPrism()
    {
        var outline=new [] {new Vector2(0,0),new Vector2(2,0),new Vector2(2,1),new Vector2(1,1),new Vector2(1,2),new Vector2(0,2)};
        var vertices=new Vector3[12];
        for(int i=0;i<6;i++) {vertices[i]=new Vector3(outline[i].x-1,-.5f,outline[i].y-1);vertices[i+6]=vertices[i]+Vector3.up;}
        var triangles=new List<int>(); var face=new [] {0,1,3,1,2,3,0,3,5,3,4,5};
        for(int i=0;i<face.Length;i+=3) {triangles.AddRange(new[]{face[i],face[i+1],face[i+2],face[i]+6,face[i+2]+6,face[i+1]+6});}
        for(int a=0;a<6;a++) {int b=(a+1)%6;triangles.AddRange(new[]{a,a+6,b+6,a,b+6,b});}
        var mesh=new Mesh {name="Concave L prism"}; mesh.vertices=vertices;mesh.triangles=triangles.ToArray();mesh.uv=vertices.Select(v=>new Vector2(v.x,v.z)).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static Mesh Torus()
    {
        int nu=24,nv=12;var vertices=new Vector3[nu*nv];var triangles=new List<int>();
        for(int u=0;u<nu;u++) for(int v=0;v<nv;v++)
        {float a=u*2*Mathf.PI/nu,b=v*2*Mathf.PI/nv;vertices[u*nv+v]=new Vector3((1+.3f*Mathf.Cos(b))*Mathf.Cos(a),.3f*Mathf.Sin(b),(1+.3f*Mathf.Cos(b))*Mathf.Sin(a));}
        for(int u=0;u<nu;u++) for(int v=0;v<nv;v++)
        {int a=u*nv+v,b=((u+1)%nu)*nv+v,c=((u+1)%nu)*nv+(v+1)%nv,d=u*nv+(v+1)%nv;triangles.AddRange(new[]{a,c,b,a,d,c});}
        var mesh=new Mesh {name="Nested contour torus"};mesh.vertices=vertices;mesh.triangles=triangles.ToArray();mesh.RecalculateBounds();mesh.RecalculateNormals();return mesh;
    }
    public static object Main()
    {
        var owned=new List<UnityEngine.Object>(); var timings=new List<double>();
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/STONE/Art/Materials/Rocks/New Material.mat");
        var cap=AssetDatabase.LoadAssetAtPath<Material>("Assets/STONE/Art/Materials/Rocks/Stone_CutSurface.mat");
        var settings=new StoneMeshCutSettings();
        GameObject Source(Mesh mesh,string name)
        {
            var go=new GameObject(name);owned.Add(go);go.hideFlags=HideFlags.HideAndDontSave;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=Enumerable.Repeat(material,mesh.subMeshCount).ToArray();go.AddComponent<Rigidbody>().isKinematic=true;go.AddComponent<BoxCollider>();go.AddComponent<TidyObject>();return go;
        }
        StoneMeshCutResult Slice(GameObject go,Vector3 point,Vector3 normal,string label)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            bool ok=StoneRuntimeMeshSlicer.TrySlice(go.GetComponent<TidyObject>(),point,normal,cap,settings,out var pair,out var error);
            timings.Add(watch.Elapsed.TotalMilliseconds);
            Check(ok,label+": "+error);
            var combinedBounds=pair.Negative.bounds; combinedBounds.Encapsulate(pair.Positive.bounds);
            float epsilon=combinedBounds.size.magnitude*settings.relativeTolerance*2;
            Check(StoneMeshTopology.ValidateClosed(pair.Negative,epsilon,out _,out _) && StoneMeshTopology.ValidateClosed(pair.Positive,epsilon,out _,out _),label+" two closed, consistently wound meshes");
            Check(pair.Negative.normals.Length==pair.Negative.vertexCount && pair.Positive.uv.Length==pair.Positive.vertexCount,label+" normals/UV exist");
            Check(pair.Materials.Length==pair.Negative.subMeshCount && pair.Materials.Last()==cap,label+" submesh/material mapping");
            return pair;
        }
        try
        {
            var baseModel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/STONE/Art/Models/Stein.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
            var source=Source(baseModel,"Original rock");
            using(var pair=Slice(source,Vector3.zero,Vector3.right,"Source FBX"))
            {
                Check(pair.Negative.vertexCount!=baseModel.vertexCount && pair.Positive.vertexCount!=baseModel.vertexCount,"Results use actual clipped topology");
                var vertices=baseModel.vertices;var normals=baseModel.normals;var outVertices=pair.Negative.vertices;var outNormals=pair.Negative.normals;
                bool normalsMatch=true;
                for(int i=0,checkedCount=0;i<vertices.Length && checkedCount<20;i++)
                {
                    if(vertices[i].x>=-.01f) continue; checkedCount++;
                    bool found=false;for(int j=0;j<outVertices.Length;j++) if((vertices[i]-outVertices[j]).sqrMagnitude<1e-10f && Vector3.Dot(normals[i],outNormals[j])>.9999f) {found=true;break;}
                    if(!found) normalsMatch=false;
                }
                Check(normalsMatch,"Original exterior normals are preserved");
                var capIndices=pair.Negative.GetTriangles(pair.Negative.subMeshCount-1);var ns=pair.Negative.normals;
                Check(capIndices.All(i=>Vector3.Dot(ns[i],Vector3.right)>.999f),"Negative cap normals point outwards");
            }
            var withoutNormals=UnityEngine.Object.Instantiate(baseModel); owned.Add(withoutNormals); withoutNormals.normals=Array.Empty<Vector3>();
            var noNormalsSource=Source(withoutNormals,"Normals missing source");
            using(var pair=Slice(noNormalsSource,Vector3.zero,Vector3.right,"Missing source normals reconstructed"))
                Check(pair.Negative.normals.All(n=>n.sqrMagnitude>.99f),"Fallback normals are unit length");
            var concave=LPrism();owned.Add(concave);var l=Source(concave,"L rock");
            using(var pair=Slice(l,Vector3.zero,Vector3.up,"Concave cut with ear clipping"))
                Check(Mathf.Abs(pair.NegativeVolume+pair.PositiveVolume-3)<.001f,"Concave volume reconstructed");
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);owned.Add(cube);cube.hideFlags=HideFlags.HideAndDontSave;cube.GetComponent<MeshRenderer>().sharedMaterial=material;cube.AddComponent<TidyObject>();
            using(var pair=Slice(cube,Vector3.zero,new Vector3(1,1,0).normalized,"Plane through existing vertices and edges")) {}
            var parent=new GameObject("Nonuniform parent");owned.Add(parent);parent.hideFlags=HideFlags.HideAndDontSave;
            parent.transform.SetPositionAndRotation(new Vector3(8,-3,4),Quaternion.Euler(20,32,11));parent.transform.localScale=new Vector3(2,.5f,1.3f);
            source.transform.SetParent(parent.transform,false);source.transform.localRotation=Quaternion.Euler(10,37,15);source.transform.localScale=new Vector3(-.7f,1.1f,.9f);
            var bladeNormal=StoneRuntimeMeshSlicer.PlaneNormal(source.transform);
            Check(Mathf.Abs(Vector3.Dot(bladeNormal,source.transform.localToWorldMatrix.MultiplyVector(Vector3.up).normalized))<.0001f && Mathf.Abs(Vector3.Dot(bladeNormal,source.transform.localToWorldMatrix.MultiplyVector(Vector3.forward).normalized))<.0001f,"CuttingPlane inverse-transpose normal remains perpendicular under sheared parent");
            using(var pair=Slice(source,source.transform.TransformPoint(baseModel.bounds.center),new Vector3(.7f,.2f,.4f).normalized,"Mirrored scale, shear and transformed pivot"))
            {
                StoneMeshTopology.ValidateClosed(baseModel,.00001f,out var volume,out _);
                Check(Mathf.Abs(pair.NegativeVolume+pair.PositiveVolume-volume*Mathf.Abs(source.transform.localToWorldMatrix.determinant))<volume*.005f,"World volume preserved under transformed parent");
            }
            source.transform.SetParent(null,false);source.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);source.transform.localScale=Vector3.one;
            var multiple=new GameObject("Multiple filters");owned.Add(multiple);multiple.hideFlags=HideFlags.HideAndDontSave;
            var t=multiple.AddComponent<TidyObject>();
            for(int i=0;i<2;i++) {var child=Source(cube.GetComponent<MeshFilter>().sharedMesh,"Child "+i);UnityEngine.Object.DestroyImmediate(child.GetComponent<TidyObject>());UnityEngine.Object.DestroyImmediate(child.GetComponent<Rigidbody>());child.transform.SetParent(multiple.transform,false);child.transform.localPosition=new Vector3(0,0,i==0?-1.2f:1.2f);}
            using(var pair=Slice(multiple,Vector3.zero,Vector3.right,"Multiple child filters and separate contours"))
                Check(pair.Materials.Length==3,"Both original child submeshes retained plus cap");
            var torus=Torus();owned.Add(torus);var torusObject=Source(torus,"Hole contour");
            Check(!StoneRuntimeMeshSlicer.TrySlice(torusObject.GetComponent<TidyObject>(),Vector3.up*.01f,Vector3.up,cap,settings,out var rejected,out var holeError) && holeError.Contains("Nested"),"Nested hole contours rejected safely: "+holeError); rejected?.Dispose();
            var open=UnityEngine.Object.Instantiate(baseModel);owned.Add(open);var tris=open.triangles;open.triangles=tris.Skip(3).ToArray();var openSource=Source(open,"Open rock");
            Check(!StoneRuntimeMeshSlicer.TrySlice(openSource.GetComponent<TidyObject>(),Vector3.zero,Vector3.right,cap,settings,out rejected,out var openError),"Open mesh rejected safely");rejected?.Dispose();
            Check(!StoneRuntimeMeshSlicer.TrySlice(source.GetComponent<TidyObject>(),Vector3.right*20,Vector3.right,cap,settings,out rejected,out _),"Outside plane rejected");rejected?.Dispose();
            var notReadable=UnityEngine.Object.Instantiate(baseModel);owned.Add(notReadable);notReadable.UploadMeshData(true);var unreadable=Source(notReadable,"Unreadable rock");
            Check(!StoneRuntimeMeshSlicer.TrySlice(unreadable.GetComponent<TidyObject>(),Vector3.zero,Vector3.right,cap,settings,out rejected,out _),"Unreadable mesh rejected");rejected?.Dispose();
            for(int i=0;i<12;i++)
            {
                var options=new RockMeshBuilder.Options {Seed=6300+i,Shape=i%2==0?RockShape.Natural:RockShape.Jagged,Roughness=.2f+i*.04f,Dents=.6f,Spikes=.7f,Cuts=0};
                var mesh=RockMeshBuilder.Build(baseModel,options,out _,out _);owned.Add(mesh);source.GetComponent<MeshFilter>().sharedMesh=mesh;source.GetComponent<TidyObject>().RefreshCachedComponents();
                var variantNormal=new Vector3(.7f,.1f+i*.03f,.2f).normalized;
                if(!StoneRuntimeMeshSlicer.TrySlice(source.GetComponent<TidyObject>(),mesh.bounds.center,variantNormal,cap,settings,out var probe,out var variantError))
                {
                    Check(variantError.Contains("Nested"),"Strongly deformed variant "+i+" rejects unsupported nested contours: "+variantError);
                    continue;
                }
                probe.Dispose();
                using(var pair=Slice(source,mesh.bounds.center,variantNormal,"Deformed/spiky variant "+i)) {}
            }
            return new {passed,count=passed.Count,cuts=timings.Count,averageMilliseconds=timings.Average(),maximumMilliseconds=timings.Max()};
        }
        finally { for(int i=owned.Count-1;i>=0;i--) if(owned[i]!=null) UnityEngine.Object.DestroyImmediate(owned[i]); }
    }
}
