using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using StoneGame.Cutting;
using StoneGame.Chipping;
using StoneGame.Delivery;
using StoneGame.Economy;
using TidyUpFramework.Objects;
using TidyUpFramework.Interaction;
using TidyUpFramework.Player;
using TidyUpFramework.Save;
using TidyUpFramework.Settings;
public static class SurfaceMaterialPlayChecks
{
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static async Task Until(Func<bool> test,string name){var end=DateTime.UtcNow.AddSeconds(12);while(!test()){if(DateTime.UtcNow>end)throw new Exception(name);await Task.Delay(15);}}
 sealed class MemorySave:ITidySaveStorage{readonly Dictionary<string,string> values=new Dictionary<string,string>();public bool TryRead(string k,out string v)=>values.TryGetValue(k,out v);public void Write(string k,string v)=>values[k]=v;public void Delete(string k)=>values.Remove(k);}
 static TidyFirstPersonController player;static TidyInteractor interactor;
 static async Task Aim(Vector3 p,Vector3 target){player.Teleport(p,0,0);await Task.Delay(70);var d=(target-interactor.AimCamera.transform.position).normalized;player.Teleport(p,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,-Mathf.Asin(d.y)*Mathf.Rad2Deg);await Task.Delay(130);Physics.SyncTransforms();}
 public static async Task<object> Main()
 {
  var checks=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
  var boot=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!boot.StoragePath.Contains("Tests~"))throw new Exception("Sandbox required");Set(TidyFramework.Save,"storage",new MemorySave());var service=boot.Service;service.StartNewGame(100000,out _);
  var delivery=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var saw=UnityEngine.Object.FindAnyObjectByType<StoneSawMachine>();var sawSlot=UnityEngine.Object.FindAnyObjectByType<StoneSawSlot>();var slot=UnityEngine.Object.FindAnyObjectByType<StoneWorkSlot>();var table=UnityEngine.Object.FindAnyObjectByType<StoneChippingTable>();player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();
  var position=player.transform.position;float yaw=player.Yaw,pitch=player.Pitch;var product=delivery.Catalog.Products[0];var profile=UnityEngine.Object.Instantiate(product.prefab.GetComponent<RawStone>().CutProfile);Set(profile,"containsOrePercent",0f);
  var aMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/STONE/Art/Materials/Rocks/Stone_CutSurface.mat");var bMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/STONE/Art/Materials/Rocks/New Material.mat");var originalMat=saw.CutSurfaceMaterial;var duration=Get<float>(saw,"cutDuration");CutStoneHalf a=null,b=null;
  try
  {
   Set(delivery,"travelDuration",.1f);Set(delivery,"waitDuration",.05f);Set(delivery,"returnDuration",.1f);Set(saw,"cutDuration",.12f);Set(table,"transitionDuration",.12f);Set(table,"hitDuration",.1f);
   Check(service.TryPlaceOrder(delivery.Catalog,product.id,2,product.unitPrice,Guid.NewGuid().ToString("N"),out var order,out _),"Paid order delivers two empty-cut test sources");await Until(()=>service.FindOrder(order.OrderId).Status==StoneGame.Orders.StoneOrderStatus.Delivered && delivery.Phase==StoneDeliveryPhase.Idle,"Delivery");
   var raws=UnityEngine.Object.FindObjectsByType<DeliveredStoneInstance>().Where(s=>s.OrderId==order.OrderId).OrderBy(s=>s.Index).ToArray();
   for(int i=0;i<2;i++)
   {
    var raw=raws[i].GetComponent<RawStone>();Set(raw,"cutProfile",profile);Set(saw,"cutSurfaceMaterial",i==0?aMat:bMat);
    await Aim(sawSlot.transform.position+new Vector3(0,-sawSlot.transform.position.y+.03f,-1.45f),sawSlot.transform.position+Vector3.up*.08f);
    Check(sawSlot.TryPlaceDirect(raw.GetComponent<TidyObject>(),false) && saw.TryStartCut(),"Actual empty-stone saw cut "+i);await Until(()=>!saw.IsCutting,"Cut");
    var halves=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Where(h=>h.SourceStoneId==raw.GetComponent<TidyObject>().InstanceId).ToArray();Check(halves.Length==2 && halves.All(h=>!h.ContainsOre && h.CanChip && h.CutSurfaceMaterial==(i==0?aMat:bMat)),"Both real empty halves retain explicit material and permit chipping "+i);
    if(i==0)a=halves[0];else b=halves[0];
   }
   var mesh=a.GeneratedMesh;var vertices=mesh.vertices;var bOuter=b.GetComponent<Renderer>().sharedMaterials.ToArray();
   async Task Seat(CutStoneHalf stone)
   {await Aim(new Vector3(-1.13f,.03f,9.8f),slot.transform.position+Vector3.up*.1f);if(!slot.TryPlaceDirect(stone.GetComponent<TidyObject>(),false) || !table.TryEnter())throw new Exception("Seat/enter failed");await Until(()=>table.Mode==ChippingMode.Working,"Enter work");if(!table.TakeHammer())throw new Exception("Hammer");}
   async Task Hit(CutStoneHalf stone,int count)
   {for(int i=0;i<count;i++){var center=stone.GetComponent<Renderer>().bounds.center;if(!table.TryPointerAction(new Ray(Camera.main.transform.position,(center-Camera.main.transform.position).normalized)))throw new Exception("Real hit rejected");await Until(()=>!table.IsStriking,"Strike");}}
   async Task Remove(CutStoneHalf stone){table.HandleEscape();table.HandleEscape();await Until(()=>!table.IsInUse,"Exit");interactor.ForcePickUp(stone.GetComponent<TidyObject>());await Task.Delay(60);interactor.ForceDrop();await Task.Delay(380);}
   await Seat(a);await Hit(a,3);Check(a.CurrentHits==3 && !a.IsChippingCompleted,"Empty A reaches partial 3/6 through table raycasts");await Remove(a);
   await Seat(b);Check(b.CurrentHits==0,"Different material B starts at own zero progress");await Hit(b,3);await Remove(b);Check(a.CurrentHits==3 && b.CurrentHits==3,"Switching stones preserves independent partial progress");
   await Seat(a);await Hit(a,3);Check(a.IsChippingCompleted && a.GetComponent<Renderer>().sharedMaterials.All(m=>m==aMat),"Empty A converts all submeshes to A's actual cut material");
   Check(b.GetComponent<Renderer>().sharedMaterials.SequenceEqual(bOuter) && !b.IsChippingCompleted,"Completing A does not recolor or complete B");
   Check(a.GeneratedMesh==mesh && mesh.vertices.SequenceEqual(vertices),"Real sliced mesh is unchanged by chipping");Check(!a.TryChip(),"Completed empty stone rejects further hit");await Remove(a);
   Check(delivery.CheckpointNow(),"Two different cut materials persist through existing checkpoint");
   var plan=new Plan{a=a.GetComponent<TidyObject>().InstanceId,b=b.GetComponent<TidyObject>().InstanceId,aMaterial=StoneCutMaterialReferences.IdFor(aMat),bMaterial=StoneCutMaterialReferences.IdFor(bMat),balance=service.Balance};File.WriteAllText("Assets/TABLE/Chipping/Tests~/SurfaceRestartPlan.json",JsonUtility.ToJson(plan,true));
  }
  finally{if(table.IsInUse){table.TryExit();await Task.Delay(300);}Set(saw,"cutSurfaceMaterial",originalMat);Set(saw,"cutDuration",duration);player.Teleport(position,yaw,pitch);}
  var result=new Result{passed=checks.ToArray()};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/Surface-play-results.json",JsonUtility.ToJson(result,true));return result;
 }
 public static async Task<object> VerifyRestart()
 {
  var checks=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
  var boot=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!boot.StoragePath.Contains("Tests~"))throw new Exception("Sandbox required");var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("Assets/TABLE/Chipping/Tests~/SurfaceRestartPlan.json"));
  await Until(()=>UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Length==4,"Restored four pieces");var pieces=UnityEngine.Object.FindObjectsByType<CutStoneHalf>();var a=pieces.Single(h=>h.GetComponent<TidyObject>().InstanceId==plan.a);var b=pieces.Single(h=>h.GetComponent<TidyObject>().InstanceId==plan.b);
  Check(a.CurrentHits==6 && a.IsChippingCompleted && !a.ContainsOre,"Completed empty A restores 6/6");Check(b.CurrentHits==3 && !b.IsChippingCompleted && !b.ContainsOre,"Independent empty B restores 3/6");
  Check(a.CutSurfaceMaterial==StoneCutMaterialReferences.Resolve(plan.aMaterial) && b.CutSurfaceMaterial==StoneCutMaterialReferences.Resolve(plan.bMaterial) && a.CutSurfaceMaterial!=b.CutSurfaceMaterial,"Saved cut materials retain distinct asset identities after full restart");
  Check(a.GetComponent<Renderer>().sharedMaterials.All(m=>m==a.CutSurfaceMaterial),"Completed cut appearance restored");
  Check(b.CutSurfaceMaterial!=UnityEngine.Object.FindAnyObjectByType<StoneSawMachine>().CutSurfaceMaterial,"B does not borrow the saw's currently configured material");
  for(int i=0;i<3;i++)Check(b.TryChip(),"Resumed B hit "+(i+4));Check(b.IsChippingCompleted && b.GetComponent<Renderer>().sharedMaterials.All(m=>m==b.CutSurfaceMaterial),"B completes with its own saved cut material");
  Check(boot.Service.Balance==plan.balance && boot.Service.Orders.Count==1 && UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>().QueueCount==0,"No extra payment or redelivery after material restore");
  var result=new Result{passed=checks.ToArray()};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/Surface-restart-results.json",JsonUtility.ToJson(result,true));boot.Service.StartNewGame(100000,out _);return result;
 }
 [Serializable]public class Plan{public string a,b,aMaterial,bMaterial;public long balance;}
 [Serializable]public class Result{public string[] passed;}
}
