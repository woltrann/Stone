using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using StoneGame.Cutting;
using StoneGame.Economy;
public static class DentConvexChecks
{
 public static object Main()
 {
  if(!Application.isPlaying || !UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>().StoragePath.Contains("Tests~"))throw new Exception("Sandbox required");
  foreach(var leftover in UnityEngine.Object.FindObjectsByType<CutStoneHalf>(FindObjectsInactive.Include).Where(h=>h.name=="Temporary dent physics test").ToArray())UnityEngine.Object.DestroyImmediate(leftover.gameObject); var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText("Assets/TABLE/Chipping/Tests~/UserStoneFixture.json"));var profile=AssetDatabase.LoadAssetAtPath<StoneCutProfile>(fixture.profilePath);
  var root=GameObject.CreatePrimitive(PrimitiveType.Cube);root.name="Temporary dent physics test";root.transform.position=Vector3.one*10000;root.SetActive(false);var mesh=UnityEngine.Object.Instantiate(root.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(root.GetComponent<BoxCollider>());root.GetComponent<MeshFilter>().sharedMesh=mesh;
  var collider=root.AddComponent<MeshCollider>();collider.convex=true;collider.sharedMesh=mesh;var half=root.AddComponent<CutStoneHalf>();var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("89f6c7b2ad8e65d45909aba8dd18c228"));
  if(material==null)throw new Exception("A real material-bearing cut fixture must exist");root.GetComponent<Renderer>().sharedMaterial=material;half.InitializeGenerated(mesh,null,new Vector3(0,-.5f,0),Vector3.down,.5f,1,null,"test-convex",profile,new StoneMeshCutSettings(),0);
  var passed=new System.Collections.Generic.List<string>();void Check(bool b,string s){if(!b)throw new Exception(s);passed.Add(s);}
  try
  {
   half.TryChip(root.transform.TransformPoint(new Vector3(0,.5f,0)),Vector3.up);Check(StoneMeshTopology.ValidateClosed(half.ChippingSurfaceMesh,1e-5f,out _,out var error),"Dented coarse mesh remains closed: "+error);
   for(int i=1;i<6;i++)half.TryChip(root.transform.TransformPoint(new Vector3(0,.5f,0)),Vector3.up);
   Check(collider.sharedMesh!=mesh && Vector3.Distance(collider.sharedMesh.bounds.size,Vector3.one*.92f)<1e-5f,"Convex physics clone shrinks exactly to final size");Check(StoneMeshTopology.ValidateClosed(half.ChippingSurfaceMesh,1e-5f,out _,out error),"Completed coarse mesh remains closed: "+error);var completed=half.CaptureObjectState();half.RestoreObjectState(completed);Check(collider.sharedMesh!=mesh && Vector3.Distance(collider.sharedMesh.bounds.size,Vector3.one*.92f)<1e-5f,"Convex reload does not double shrink");half.RestoreObjectState(null);Check(collider.sharedMesh==mesh,"Convex reset restores original physics mesh");
  }
  finally{UnityEngine.Object.Destroy(root);}
  var result=new Result{passed=passed.ToArray()};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/Dent-convex-tests.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable]public class Fixture{public string profilePath;}
 [Serializable]public class Result{public string[] passed;}
}

