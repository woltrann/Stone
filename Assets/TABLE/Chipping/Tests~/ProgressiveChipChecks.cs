using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using StoneGame.Cutting;
using StoneGame.Economy;
using TidyUpFramework.Objects;
using TidyUpFramework.Settings;
using TidyUpFramework.Save;
public static class ProgressiveChipChecks
{
 [Serializable]public class Fixture{public string sourceId,activeId,sourceState,leftId,leftState,rightId,rightState,profilePath;}
 [Serializable]public class Result{public string[] passed;public int[] revealedTriangles;public float firstHitMilliseconds,laterHitMilliseconds;}
 sealed class MemorySave:ITidySaveStorage{readonly Dictionary<string,string> v=new Dictionary<string,string>();public bool TryRead(string k,out string s)=>v.TryGetValue(k,out s);public void Write(string k,string s)=>v[k]=s;public void Delete(string k)=>v.Remove(k);}
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 public static object Main()
 {
  if(!Application.isPlaying || !UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>().StoragePath.Contains("Tests~"))throw new Exception("Isolated play required");Set(TidyFramework.Save,"storage",new MemorySave());
  var checks=new List<string>();void Check(bool b,string s){if(!b)throw new Exception(s);checks.Add(s);}
  var f=JsonUtility.FromJson<Fixture>(File.ReadAllText("Assets/TABLE/Chipping/Tests~/UserStoneFixture.json"));var raw=UnityEngine.Object.FindObjectsByType<RawStone>(FindObjectsInactive.Include).Single(s=>s.GetComponent<TidyObject>().InstanceId==f.sourceId);Set(raw,"cutProfile",AssetDatabase.LoadAssetAtPath<StoneCutProfile>(f.profilePath));raw.RestoreObjectState(f.sourceState);raw.GetComponent<TidyObject>().MarkConsumed();raw.gameObject.SetActive(false);
  var counts=new List<int>();float first=0,later=0;
  try
  {
   var stone=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Single(s=>s.GetComponent<TidyObject>().InstanceId==f.activeId);stone.RestoreObjectState(null);var mesh=stone.GeneratedMesh;var verts=mesh.vertices;var triangles=mesh.GetTriangles(0);var filter=stone.GetComponent<MeshFilter>();var renderer=stone.GetComponent<MeshRenderer>();var sourceMaterials=renderer.sharedMaterials;string geometry=JsonUtility.ToJson(StoneMeshSnapshot.Capture(stone));var physics=stone.GetComponentsInChildren<MeshCollider>().Select(c=>c.sharedMesh).ToArray();
   Check(stone.RequiredHits==6 && stone.CurrentHits==0,"Existing six-hit requirement unchanged");var chosen=new List<Vector3>();var normals=new List<Vector3>();
   for(int h=0;h<5;h++)
   {
    int best=-1;float farthest=-1;for(int t=0;t<triangles.Length;t+=3){var p=(verts[triangles[t]]+verts[triangles[t+1]]+verts[triangles[t+2]])/3;float d=chosen.Count==0?(p-mesh.bounds.center).sqrMagnitude:chosen.Min(q=>(p-q).sqrMagnitude);if(d>farthest){farthest=d;best=t;}}
    var point=(verts[triangles[best]]+verts[triangles[best+1]]+verts[triangles[best+2]])/3;var normal=Vector3.Cross(verts[triangles[best+1]]-verts[triangles[best]],verts[triangles[best+2]]-verts[triangles[best]]).normalized;chosen.Add(point);normals.Add(normal);
    var watch=System.Diagnostics.Stopwatch.StartNew();Check(stone.TryChip(stone.transform.TransformPoint(point),stone.transform.worldToLocalMatrix.transpose.MultiplyVector(normal).normalized),"Positioned hit "+(h+1));watch.Stop();if(h==0)first=(float)watch.Elapsed.TotalMilliseconds;else later=Mathf.Max(later,(float)watch.Elapsed.TotalMilliseconds);
    var visual=filter.sharedMesh;int exposed=visual.GetTriangles(visual.subMeshCount-1).Length/3;counts.Add(exposed);Check(visual!=mesh && visual.subMeshCount==mesh.subMeshCount+1 && exposed>0,"Hit produces local surface patches "+(h+1));if(h>0)Check(counts[h]>counts[h-1],"Distinct hit adds exposed area "+(h+1));Check(visual.GetTriangles(0).Length>0 && !stone.IsChippingCompleted,"Untouched outer material remains "+(h+1));Check(renderer.sharedMaterials.Last()==stone.CutSurfaceMaterial,"Patch uses exact cut material "+(h+1));
    var displayVerts=visual.vertices;var indices=visual.GetTriangles(visual.subMeshCount-1);float limit=mesh.bounds.size.magnitude*.25f;bool local=true;for(int i=0;i<indices.Length;i+=3){var p=(displayVerts[indices[i]]+displayVerts[indices[i+1]]+displayVerts[indices[i+2]])/3;if(chosen.All(q=>Vector3.Distance(p,q)>limit))local=false;}Check(local,"Exposed triangles stay near actual impact positions "+(h+1));
    Check(JsonUtility.ToJson(StoneMeshSnapshot.Capture(stone))==geometry,"Source geometry and material snapshot unchanged "+(h+1));Check(stone.GetComponentsInChildren<MeshCollider>().Select(c=>c.sharedMesh).SequenceEqual(physics),"Physics proxies unchanged "+(h+1));
   }
   string state=stone.CaptureObjectState();var exposedBefore=filter.sharedMesh.GetTriangles(mesh.subMeshCount);stone.RestoreObjectState(null);Check(filter.sharedMesh==mesh && renderer.sharedMaterials.SequenceEqual(sourceMaterials),"Reset restores original mesh and materials");stone.RestoreObjectState(state);Check(stone.CurrentHits==5 && filter.sharedMesh.GetTriangles(mesh.subMeshCount).SequenceEqual(exposedBefore),"Five patches restore exactly from saved hit coordinates");
   stone.transform.SetPositionAndRotation(stone.transform.position+Vector3.one,Quaternion.Euler(31,76,18));stone.transform.localScale*=1.2f;stone.RestoreObjectState(state);Check(filter.sharedMesh.GetTriangles(mesh.subMeshCount).SequenceEqual(exposedBefore),"Local patches follow moved rotated scaled stone");
   string fullSource=raw.CaptureObjectState();var capMaterial=stone.CutSurfaceMaterial;Check(stone.TryChip(stone.transform.TransformPoint(chosen[0]),stone.transform.up) && stone.CurrentHits==6 && stone.IsChippingCompleted,"Final hit completes exactly at six");Check(filter.sharedMesh!=mesh && renderer.sharedMaterials.Length==filter.sharedMesh.subMeshCount && renderer.sharedMaterials.All(m=>m==capMaterial),"Last hit converts entire stone and retains dented surface");Check(!stone.TryChip(stone.transform.position,Vector3.up),"Completed stone rejects extra hit");string completed=stone.CaptureObjectState();stone.RestoreObjectState(null);stone.RestoreObjectState(completed);Check(stone.IsChippingCompleted && renderer.sharedMaterials.All(m=>m==capMaterial),"Completed appearance survives save restore");
   stone.RestoreObjectState(state.Replace("\"version\":4","\"version\":2"));Check(stone.CurrentHits==5 && filter.sharedMesh==mesh,"Legacy v2 progress loads without invented impact positions");Check(stone.TryChip(stone.transform.position,Vector3.up) && stone.IsChippingCompleted,"Legacy progress finishes normally"); raw.RestoreObjectState(fullSource);stone=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Single(h=>h.GetComponent<TidyObject>().InstanceId==f.activeId);stone.RestoreObjectState(state);Check(stone.CurrentHits==5 && stone.GetComponent<MeshFilter>().sharedMesh.GetTriangles(stone.GeneratedMesh.subMeshCount).SequenceEqual(exposedBefore),"Full raw source and half state reload reconstructs exact partial appearance on a new instance");Check(!stone.TryChip(new Vector3(float.NaN,0,0),Vector3.up) && stone.CurrentHits==5,"Invalid hit coordinates rejected without changing progress");
  }
  finally{raw.RestoreObjectState(null);raw.gameObject.SetActive(true);raw.GetComponent<TidyObject>().MarkLoose();}
  var result=new Result{passed=checks.ToArray(),revealedTriangles=counts.ToArray(),firstHitMilliseconds=first,laterHitMilliseconds=later};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/Progressive-chip-tests.json",JsonUtility.ToJson(result,true));return result;
 }
}


