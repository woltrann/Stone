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
public static class DentChecks
{
 [Serializable]public class Fixture{public string sourceId,activeId,sourceState,profilePath;}
 [Serializable]public class Result{public string[] passed;public float maxDent,firstHitMilliseconds,laterHitMilliseconds;}
 sealed class MemorySave:ITidySaveStorage{readonly Dictionary<string,string> d=new Dictionary<string,string>();public bool TryRead(string k,out string s)=>d.TryGetValue(k,out s);public void Write(string k,string s)=>d[k]=s;public void Delete(string k)=>d.Remove(k);}
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 public static object Main()
 {
  if(!Application.isPlaying || !UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>().StoragePath.Contains("Tests~"))throw new Exception("Isolated play required");Set(TidyFramework.Save,"storage",new MemorySave());
  var checks=new List<string>();void Check(bool b,string s){if(!b)throw new Exception(s);checks.Add(s);}
  var f=JsonUtility.FromJson<Fixture>(File.ReadAllText("Assets/TABLE/Chipping/Tests~/UserStoneFixture.json"));var raw=UnityEngine.Object.FindObjectsByType<RawStone>(FindObjectsInactive.Include).Single(s=>s.GetComponent<TidyObject>().InstanceId==f.sourceId);Set(raw,"cutProfile",AssetDatabase.LoadAssetAtPath<StoneCutProfile>(f.profilePath));raw.RestoreObjectState(f.sourceState);raw.GetComponent<TidyObject>().MarkConsumed();raw.gameObject.SetActive(false);
  float maxDent=0,first=0,later=0;
  try
  {
   var stone=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Single(s=>s.GetComponent<TidyObject>().InstanceId==f.activeId);stone.RestoreObjectState(null);var source=stone.GeneratedMesh;string geometry=JsonUtility.ToJson(StoneMeshSnapshot.Capture(stone));var box=stone.GetComponents<BoxCollider>();var boxCenters=box.Select(c=>c.center).ToArray();var boxSizes=box.Select(c=>c.size).ToArray();var meshProxies=stone.GetComponents<MeshCollider>();var proxyMeshes=meshProxies.Select(c=>c.sharedMesh).ToArray();var body=stone.GetComponent<Rigidbody>();float mass=body.mass;bool kinematic=body.isKinematic;
   Check(stone.RequiredHits==6 && stone.SourceProfile.ChippingFinalSize==.92f && stone.SourceProfile.ChippingDentDepth>.01f,"Six hits and default depth/92 percent size configured");var indices=source.GetTriangles(0);var vertices=source.vertices;int best=0;float distance=-1;for(int i=0;i<indices.Length;i+=3){var p=(vertices[indices[i]]+vertices[indices[i+1]]+vertices[indices[i+2]])/3;float d=Mathf.Abs(Vector3.Dot(p-stone.CutSurfaceCenter,stone.CutSurfaceNormal));if(d>distance){distance=d;best=i;}}var point=(vertices[indices[best]]+vertices[indices[best+1]]+vertices[indices[best+2]])/3;var normal=Vector3.Cross(vertices[indices[best+1]]-vertices[indices[best]],vertices[indices[best+2]]-vertices[indices[best]]).normalized;
   int revision=stone.ChippingGeometryRevision;var timer=System.Diagnostics.Stopwatch.StartNew();Check(stone.TryChip(stone.transform.TransformPoint(point),stone.transform.worldToLocalMatrix.transpose.MultiplyVector(normal).normalized),"First located hammer hit");timer.Stop();first=(float)timer.Elapsed.TotalMilliseconds;Check(stone.ChippingSurfaceMesh!=source && stone.ChippingGeometryRevision>revision,"Surface and picking revision change after dent");
   var displaced=stone.ChippingSurfaceMesh.vertices;Set(stone,"dentDepth",0f);stone.RestoreObjectState(stone.CaptureObjectState());var baseline=stone.ChippingSurfaceMesh.vertices;Set(stone,"dentDepth",stone.SourceProfile.ChippingDentDepth);stone.RestoreObjectState(stone.CaptureObjectState());displaced=stone.ChippingSurfaceMesh.vertices;
   int moved=0,untouched=0;float radius=source.bounds.size.magnitude*.16f;var seam=new Dictionary<Vector3,Vector3>();bool seams=true,localized=true,finite=true;for(int i=0;i<baseline.Length;i++){float delta=Vector3.Distance(baseline[i],displaced[i]);if(delta>1e-6f){moved++;maxDent=Mathf.Max(maxDent,delta);if(Vector3.Distance(baseline[i],point)>radius*1.01f)localized=false;}else untouched++;if(seam.TryGetValue(baseline[i],out var prior)){if(Vector3.Distance(prior,displaced[i])>1e-6f)seams=false;}else seam.Add(baseline[i],displaced[i]);finite &= float.IsFinite(displaced[i].x)&&float.IsFinite(displaced[i].y)&&float.IsFinite(displaced[i].z);}
   Check(moved>10 && maxDent>source.bounds.size.magnitude*.003f,"Impact creates measurable local indentation");Check(untouched>moved && localized,"Vertices outside hit region remain unchanged");Check(seams && finite,"UV/material seam duplicates remain watertight and finite");Check(box.Select(c=>c.center).SequenceEqual(boxCenters) && box.Select(c=>c.size).SequenceEqual(boxSizes),"Partial dents preserve existing bounded physics proxies");
   string one=stone.CaptureObjectState();stone.RestoreObjectState(null);Check(stone.ChippingSurfaceMesh==source,"Reset restores undeformed source");stone.RestoreObjectState(one);Check(stone.ChippingSurfaceMesh.vertices.SequenceEqual(displaced),"Exact dent restores from saved marks");
   var current=stone.ChippingSurfaceMesh;var tri=current.GetTriangles(0);var cv=current.vertices;point=(cv[tri[0]]+cv[tri[1]]+cv[tri[2]])/3;normal=Vector3.Cross(cv[tri[1]]-cv[tri[0]],cv[tri[2]]-cv[tri[0]]).normalized;
   for(int h=1;h<5;h++){timer.Restart();Check(stone.TryChip(stone.transform.TransformPoint(point),stone.transform.worldToLocalMatrix.transpose.MultiplyVector(normal).normalized),"Additional dent "+(h+1));timer.Stop();later=Mathf.Max(later,(float)timer.Elapsed.TotalMilliseconds);}
   Check(JsonUtility.ToJson(StoneMeshSnapshot.Capture(stone))==geometry,"Immutable source snapshot unaffected by all dents");string partial=stone.CaptureObjectState();var partialVertices=stone.ChippingSurfaceMesh.vertices;string fullSource=raw.CaptureObjectState();raw.RestoreObjectState(fullSource);stone=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Single(h=>h.GetComponent<TidyObject>().InstanceId==f.activeId);stone.RestoreObjectState(partial);Check(stone.CurrentHits==5 && stone.ChippingSurfaceMesh.vertices.SequenceEqual(partialVertices),"Source plus half state reconstruct exact dents on new object");
   var priorScale=stone.transform.localScale;Check(stone.TryChip(stone.transform.TransformPoint(point),stone.transform.up) && stone.IsChippingCompleted,"Sixth hit completes");string completed=stone.CaptureObjectState();var small=stone.ChippingSurfaceMesh.vertices;var smallBounds=stone.ChippingSurfaceMesh.bounds;Check(stone.transform.localScale==priorScale,"Root scale unchanged to prevent snapshot double shrink");Check(stone.GetComponent<Renderer>().sharedMaterials.All(m=>m==stone.CutSurfaceMaterial),"Full final material retained");
   Set(stone,"finalSize",1f);stone.RestoreObjectState(stone.CaptureObjectState());var big=stone.ChippingSurfaceMesh.vertices;bool scaled=true;for(int i=0;i<big.Length;i++)if(Vector3.Distance(stone.CutSurfaceCenter+(big[i]-stone.CutSurfaceCenter)*.92f,small[i])>1e-6f)scaled=false;Check(scaled,"Every final vertex shrinks exactly 8 percent about seating center");Check(smallBounds.size.magnitude<stone.ChippingSurfaceMesh.bounds.size.magnitude,"Final stone is visibly smaller");
   stone.RestoreObjectState(completed);var boxes=stone.GetComponents<BoxCollider>();Check(boxes.Length==boxSizes.Length && boxes.Select((c,i)=>Vector3.Distance(c.size,boxSizes[i]*.92f)<1e-6f && Vector3.Distance(c.center,stone.CutSurfaceCenter+(boxCenters[i]-stone.CutSurfaceCenter)*.92f)<1e-6f).All(v=>v),"Final compound physics proxies shrink to 92 percent");Check(stone.GetComponent<Rigidbody>().mass==mass && stone.GetComponent<Rigidbody>().isKinematic==kinematic,"Rigidbody interaction state preserved");
   Check(stone.ChippingSurfaceMesh.vertices.All(v=>Vector3.Dot(v-stone.CutSurfaceCenter,stone.CutSurfaceNormal)<1e-5f),"No deformed vertex falls through tabletop cut plane");Check(!stone.TryChip(stone.transform.position,Vector3.up),"Completed stone rejects extra deformation");
   fullSource=raw.CaptureObjectState();raw.RestoreObjectState(fullSource);stone=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Single(h=>h.GetComponent<TidyObject>().InstanceId==f.activeId);for(int i=0;i<3;i++)stone.RestoreObjectState(completed);Check(stone.ChippingSurfaceMesh.vertices.SequenceEqual(small),"Repeated full reconstruction and loads do not compound shrink");
   stone.RestoreObjectState(one.Replace("\"version\":4","\"version\":3"));Check(stone.CurrentHits==1 && stone.ChippingSurfaceMesh!=stone.GeneratedMesh,"Previous v3 mark records acquire compatible dents");stone.RestoreObjectState(null);Check(stone.GetComponents<BoxCollider>().Select(c=>c.size).SequenceEqual(boxSizes),"Reset restores original collider dimensions");
  }
  finally{raw.RestoreObjectState(null);raw.gameObject.SetActive(true);raw.GetComponent<TidyObject>().MarkLoose();}
  var result=new Result{passed=checks.ToArray(),maxDent=maxDent,firstHitMilliseconds=first,laterHitMilliseconds=later};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/Dent-tests.json",JsonUtility.ToJson(result,true));return result;
 }
}
