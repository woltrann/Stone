using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using StoneGame.Cutting;
public static class ChippingDataChecks
{
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 public static object Main()
 {
  var checks=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
  var catalog=AssetDatabase.LoadAssetAtPath<StoneGame.Shop.StoneMarketCatalog>("Assets/PC/ScriptableObjects/StoneMarketCatalog.asset");var profile=UnityEngine.Object.Instantiate(catalog.Products[0].prefab.GetComponent<RawStone>().CutProfile);
  var cap=AssetDatabase.LoadAssetAtPath<Material>("Assets/STONE/Art/Materials/Rocks/Stone_CutSurface.mat");var exterior=catalog.Products[0].prefab.GetComponent<Renderer>().sharedMaterial;
  string materialPath=AssetDatabase.GetAssetPath(cap),originalAsset=File.ReadAllText(materialPath);
  var root=new GameObject("Chipping data test (inactive)");root.SetActive(false);var filter=root.AddComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(catalog.Products[0].prefab.GetComponent<MeshFilter>().sharedMesh);var triangles=mesh.triangles;mesh.subMeshCount=3;mesh.SetTriangles(triangles.Take(3).ToArray(),0);mesh.SetTriangles(triangles.Skip(3).ToArray(),1);mesh.SetTriangles(Array.Empty<int>(),2);filter.sharedMesh=mesh;
  var renderer=root.AddComponent<MeshRenderer>();root.AddComponent<Rigidbody>().isKinematic=true;root.AddComponent<TidyUpFramework.Objects.TidyObject>();var half=root.AddComponent<CutStoneHalf>();
  var child=new GameObject("Stone geometry",typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(root.transform,false);child.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
  var helper=new GameObject("Auxiliary mesh helper",typeof(MeshFilter),typeof(MeshRenderer));helper.transform.SetParent(root.transform,false);helper.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;helper.GetComponent<Renderer>().sharedMaterial=exterior;
  root.SetActive(true);OreDefinition legacy=null;
  try
  {
   foreach(var guid in AssetDatabase.FindAssets("t:OreDefinition"))
   {
    var ore=AssetDatabase.LoadAssetAtPath<OreDefinition>(AssetDatabase.GUIDToAssetPath(guid));renderer.sharedMaterials=new[]{cap,exterior,exterior};child.GetComponent<Renderer>().sharedMaterial=exterior;Set(half,"stoneGeometryRenderers",new Renderer[]{renderer,child.GetComponent<Renderer>()});
    half.InitializeGenerated(mesh,null,Vector3.zero,Vector3.up,1,1,ore,"test",profile,new StoneMeshCutSettings(),0);
    Check(half.CanChip && half.CurrentHits==0,"Configured ore ready: "+ore.DisplayName);
    Check(half.CutSurfaceSubmesh==0 && half.CutSurfaceMaterial==cap && renderer.sharedMaterials[2]!=cap,"Explicit cap slot wins over the last material: "+ore.DisplayName);
    var oreVisual=GetOreVisual(half);var oreMaterials=oreVisual.GetComponentsInChildren<Renderer>().Select(r=>r.sharedMaterial).ToArray();var originalMesh=filter.sharedMesh;var beforeVertices=mesh.vertices;
    for(int i=0;i<half.RequiredHits-1;i++)if(!half.TryChip())throw new Exception("Hit rejected");
    Check(renderer.sharedMaterials[1]==exterior,"Outer material unchanged before final hit: "+ore.DisplayName);
    half.TryChip();
    Check(half.IsChippingCompleted && renderer.sharedMaterials.All(m=>m==cap) && child.GetComponent<Renderer>().sharedMaterial==cap && !half.TryChip(),"Only stone renderers receive cut material and hits cap: "+ore.DisplayName);
    Check(oreVisual.activeSelf && oreVisual.GetComponentsInChildren<Renderer>().Select(r=>r.sharedMaterial).SequenceEqual(oreMaterials) && helper.GetComponent<Renderer>().sharedMaterial==exterior,"Ore visual and auxiliary mesh left untouched: "+ore.DisplayName);
    Check(filter.sharedMesh==originalMesh && mesh.vertices.SequenceEqual(beforeVertices),"No mesh replacement or geometry change: "+ore.DisplayName);
   }
   var ruby=AssetDatabase.LoadAssetAtPath<OreDefinition>("Assets/STONE/Data/Ores/Yakut.asset");Set(profile,"chippingRequiredHits",8);half.Initialize(ruby,"test",profile);half.TryChip();half.TryChip();half.TryChip();var json=half.CaptureObjectState();
   Set(profile,"chippingRequiredHits",2);half.Initialize(ruby,"test",profile);half.RestoreObjectState(json);
   Check(half.RequiredHits==8 && half.CurrentHits==3 && !half.IsChippingCompleted,"Saved per-instance required/current hits survive later profile changes");
   legacy=UnityEngine.Object.Instantiate(ruby);Set(legacy,"processedOreMaterial",null);half.Initialize(legacy,"test",profile);
   Check(half.CanChip && half.TryChip(),"Legacy processed ore material is irrelevant to chipping");
   half.Initialize(null,"test",profile);Check(half.CanChip && half.TryChip(),"No-ore half is chippable");
   half.RestoreObjectState("{\"version\":1,\"requiredHits\":6,\"currentHits\":3,\"completed\":false,\"oreName\":\"OldOreName\"}");Check(half.RequiredHits==6 && half.CurrentHits==3,"Legacy progress loads independently of ore identity");
   Set(half,"cutSurfaceMaterial",null);Check(!half.CanChip && !half.TryChip() && !string.IsNullOrEmpty(half.ChippingConfigurationError),"Missing real cut material safely blocks chipping with message");
   Check(File.ReadAllText(materialPath)==originalAsset,"Shared material asset bytes unchanged");
  }
  finally{root.SetActive(false);UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(profile);if(legacy!=null)UnityEngine.Object.Destroy(legacy);}
  var result=new Result{passed=checks.ToArray()};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/Chipping-data-results.json",JsonUtility.ToJson(result,true));return result;
 }
 static GameObject GetOreVisual(CutStoneHalf half)=>(GameObject)typeof(CutStoneHalf).GetField("oreVisual",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(half);
 [Serializable]public class Result{public string[] passed;}
}


