using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Delivery;
using StoneGame.Economy;
using StoneGame.Orders;
using StoneGame.Shop;
using StoneGame.Cutting;
using TidyUpFramework.Core;
using TidyUpFramework.Objects;
using TidyUpFramework.Player;
using TidyUpFramework.Interaction;
using TidyUpFramework.Save;
public static class DeliveryPlayChecks
{
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
 static async Task Until(Func<bool> test,string name,int ms=18000){var end=DateTime.UtcNow.AddMilliseconds(ms);while(!test()){if(DateTime.UtcNow>end)throw new Exception("Timeout: "+name);await Task.Delay(20);}}
 static TidyObject[] Stones(string order)=>TidyObjectRegistry.All.Where(t=>t!=null && t.GetComponent<DeliveredStoneInstance>()!=null && t.GetComponent<DeliveredStoneInstance>().OrderId==order).ToArray();
 public static async Task<object> Main()
 {
  if(!Application.isPlaying)throw new Exception("Play mode required");var passed=new List<string>();var failed=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);passed.Add(name);}
  var manager=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();var economy=bootstrap.Service;var originalCatalog=manager.Catalog;
  if(!bootstrap.StoragePath.Contains("Tests~"))throw new Exception("Refusing to modify production economy");
  var catalog=ScriptableObject.CreateInstance<StoneMarketCatalog>();var p=originalCatalog.Products[0];
  var templateRoot=new GameObject("Delivery test template (inactive)");templateRoot.SetActive(false);var variant=UnityEngine.Object.Instantiate(p.prefab,templateRoot.transform);variant.SetActive(false);variant.transform.localScale=p.prefab.transform.localScale*1.3f;
  var product=new StoneProduct{id=p.id,displayName=p.displayName,description=p.description,unitPrice=p.unitPrice,prefab=p.prefab,definition=p.definition,active=true};
  var second=new StoneProduct{id="delivery-test-second",displayName="Test ikinci taş",unitPrice=p.unitPrice,prefab=variant,definition=p.definition,active=true};Set(catalog,"products",new List<StoneProduct>{product,second});Set(manager,"catalog",catalog);
  var times=new List<double>();var spawnOrders=new List<string>();var spawnPositions=new List<Vector3>();var point=manager.SpawnPoint.position;var rotation=manager.Car.rotation;int frame=Time.frameCount;
  Action<string,int,GameObject> observer=(id,index,go)=>{times.Add(Time.timeAsDouble);spawnOrders.Add(id);spawnPositions.Add(go.transform.position);};manager.StoneSpawned+=observer;
  var player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();var interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();var startPlayer=player.transform.position;float yaw=player.Yaw,pitch=player.Pitch;
  var saw=UnityEngine.Object.FindAnyObjectByType<StoneSawMachine>();float cutDuration=Get<float>(saw,"cutDuration");
  var orderIds=new List<string>();double travel=0,wait=0;
  try
  {
   await Task.Delay(120);Check(Time.frameCount>frame,"Live player loop advances");Check(!UnityEngine.Object.FindAnyObjectByType<TidyUpFramework.Levels.TidyLevelManager>().SavesProgress,"Real level saves disabled for tests");
   Check(manager.Car.position==new Vector3(50,1,15),"Car starts at exact requested world position");
   long balance=economy.Balance;
   Check(!economy.TryPlaceOrder(catalog,product.id,catalog.MaximumOrderQuantity,product.unitPrice+1,Guid.NewGuid().ToString("N"),out _,out _),"Rejected purchase emits no delivery");await Task.Delay(120);Check(manager.QueueCount==0 && manager.Car.position==new Vector3(50,1,15),"Rejected purchase leaves truck stationary");
   Check(economy.TryPlaceOrder(catalog,product.id,1,product.unitPrice,Guid.NewGuid().ToString("N"),out var one,out _),"Central successful order starts delivery");orderIds.Add(one.OrderId);long afterPayment=economy.Balance;
   await Until(()=>manager.Phase==StoneDeliveryPhase.Arriving,"trip begins");
   Check(manager.QueueCount==1 && manager.TryQueueOrder(one.OrderId) && manager.QueueCount==1,"Same order cannot be queued twice");
   await Task.Delay(1200);Check(manager.Car.position.x<50 && manager.Car.position.x>-7 && times.Count==0,"Truck moves smoothly while no stones spawn during travel");Check(Mathf.Abs(manager.Car.position.y-1)<.00001f && Quaternion.Angle(rotation,manager.Car.rotation)<.001f,"Truck keeps Y=1 and original rotation");
   await Until(()=>manager.Phase==StoneDeliveryPhase.Waiting,"arrival");travel=manager.ArrivedAt-manager.TripStartedAt;
   Check(travel>=3 && travel<3.15 && manager.Car.position==new Vector3(-7,1,15),"Requested 3-second trip reaches exact destination (one-frame tolerance)");
   await Task.Delay(1000);Check(times.Count==0,"No stones during first second of 2-second wait");await Until(()=>times.Count>0,"first stone");wait=times[0]-manager.ArrivedAt;
   Check(wait>=2 && wait<2.2,"First stone waits full 2 seconds after arrival");Check(Vector3.Distance(spawnPositions[0],point)<=manager.SpawnRadius+.001f && Mathf.Abs(spawnPositions[0].y-point.y)<.001f,"Stone originates inside horizontal disk centered on unchanged Boru/Point");
   var first=Stones(one.OrderId).Single();Check(!first.Body.isKinematic && first.Body.useGravity,"Controlled prefab receives temporary gravity during spill");
   Check(first.Definition==product.definition && first.GetComponent<RawStone>().CutProfile==product.prefab.GetComponent<RawStone>().CutProfile,"Original Definition and cut profile references preserved");
   var deliveredMesh=first.GetComponent<MeshFilter>().sharedMesh;var prefabMesh=product.prefab.GetComponent<MeshFilter>().sharedMesh;
   bool meshPreserved=product.prefab.GetComponent<RockMeshVariator>()!=null ? deliveredMesh!=prefabMesh && deliveredMesh.vertexCount==prefabMesh.vertexCount && deliveredMesh.name==prefabMesh.name+"_Variant" : deliveredMesh==prefabMesh;
   Check(meshPreserved && first.GetComponent<MeshRenderer>().sharedMaterial==product.prefab.GetComponent<MeshRenderer>().sharedMaterial,"Purchased mesh/material and configured RockMeshVariator behavior preserved");
   await Task.Delay(550);Check(first.transform.position.y<point.y-.4f,"RigidBody falls under real gravity");await Until(()=>manager.Phase==StoneDeliveryPhase.Idle && manager.QueueCount==0,"truck returns");
   Check(Stones(one.OrderId).Length==1 && economy.FindOrder(one.OrderId).Status==StoneOrderStatus.Delivered,"One purchased stone yields exactly one delivered object");Check(economy.Balance==afterPayment,"Delivery never charges a second payment");Check(first.transform.position.y>-.1f && first.transform.position.y<2,"Stone collides with existing ground/cage surface");
   Check(economy.TryPlaceOrder(catalog,product.id,10,product.unitPrice,Guid.NewGuid().ToString("N"),out var ten,out _),"Ten-stone order accepted");orderIds.Add(ten.OrderId);
   Check(economy.TryPlaceOrder(catalog,second.id,2,second.unitPrice,Guid.NewGuid().ToString("N"),out var different,out _),"Different product queues behind active order");orderIds.Add(different.OrderId);long queuedBalance=economy.Balance;
   await Until(()=>economy.FindOrder(different.OrderId).Status==StoneOrderStatus.Delivered && manager.Phase==StoneDeliveryPhase.Idle,"both queued deliveries",30000);
   Check(Stones(ten.OrderId).Length==10 && Stones(different.OrderId).Length==2,"Queue produces exactly ten plus two stones");
   Check(Stones(different.OrderId).All(t=>Vector3.Distance(t.transform.localScale,variant.transform.localScale)<.0001f),"Second product uses its own configured prefab scale");
   Check(Stones(ten.OrderId).All(t=>Vector3.Distance(t.transform.localScale,product.prefab.transform.localScale)<.0001f),"First product keeps its own prefab scale");
   var events=spawnOrders.Where(id=>id!=one.OrderId).ToArray();Check(events.Take(10).All(id=>id==ten.OrderId) && events.Skip(10).All(id=>id==different.OrderId),"Orders spill sequentially without interleaving");
   var tenTimes=times.Where((t,i)=>spawnOrders[i]==ten.OrderId).ToArray();Check(tenTimes.Zip(tenTimes.Skip(1),(a,b)=>b-a).All(d=>d>=.149),"Stones spaced by at least configured 0.15 seconds");
   Check(economy.Balance==queuedBalance,"Queued deliveries do not change paid balance");
   var delivered=Stones(one.OrderId).Single();interactor.ForcePickUp(delivered);await Task.Delay(120);Check(interactor.HeldObject==delivered && delivered.State==TidyObjectState.Held,"Delivered stone can be picked up by existing system");Check(!delivered.GetComponent<DeliveredStoneInstance>().Falling && delivered.Body.isKinematic,"Initial fall releases control to original pickup mode");
   interactor.ForceDrop();await Task.Delay(450);
   var slot=UnityEngine.Object.FindAnyObjectByType<StoneSawSlot>();var approach=slot.transform.position+new Vector3(0,-slot.transform.position.y+.03f,-1.45f);player.Teleport(approach,0,25);await Task.Delay(100);var direction=(slot.transform.position+Vector3.up*.08f-interactor.AimCamera.transform.position).normalized;player.Teleport(player.transform.position,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,-Mathf.Asin(direction.y)*Mathf.Rad2Deg);await Task.Delay(100);
   Set(saw,"cutDuration",.2f);Check(slot.TryPlaceDirect(delivered,false) && saw.TryStartCut(),"Delivered prefab enters existing timed saw");await Until(()=>!saw.IsCutting,"cut completes");
   Check(delivered.GetComponent<RawStone>().IsProcessed && UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Count(h=>h.SourceStoneId==delivered.InstanceId)==2,"Delivered stone produces two actual runtime mesh halves");
   Check(manager.CheckpointNow(),"Dynamic cut source and halves checkpoint successfully");string sourceId=delivered.InstanceId;int eventCount=times.Count;
   manager.enabled=false;delivered.GetComponent<RawStone>().RestoreObjectState(null);delivered.gameObject.SetActive(false);UnityEngine.Object.Destroy(delivered.gameObject);await Task.Delay(150);manager.enabled=true;
   await Until(()=>TidyObjectRegistry.All.Any(t=>t!=null && t.InstanceId==sourceId && t.State==TidyObjectState.Consumed),"runtime source reconstruction");await Task.Delay(250);
   Check(TidyObjectRegistry.All.Count(t=>t!=null && t.InstanceId==sourceId)==1,"Restore reconstructs exactly one durable source ID");
   Check(UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Count(h=>h.SourceStoneId==sourceId)==2,"Restore reconstructs cut meshes instead of redelivering raw stone");
   Check(times.Count==eventCount && manager.QueueCount==0 && economy.Balance==queuedBalance,"Delivered orders never requeue, respawn or repay after restore");
   Check(manager.SpawnPoint.position==point && Quaternion.Angle(rotation,manager.Car.rotation)<.001f,"Point and Car rotation remain unchanged after all deliveries");
  }
  catch(Exception ex){failed.Add(ex.ToString());}
  finally
  {
   manager.StoneSpawned-=observer;saw.CancelCut();Set(saw,"cutDuration",cutDuration);economy.StartNewGame(100000,out _);Set(manager,"catalog",originalCatalog);UnityEngine.Object.Destroy(templateRoot);UnityEngine.Object.Destroy(catalog);player.Teleport(startPlayer,yaw,pitch);
  }
  var result=new Results{passed=passed.ToArray(),failed=failed.ToArray(),travelSeconds=travel,waitSeconds=wait,spawnTimes=times.ToArray(),frames=Time.frameCount-frame};File.WriteAllText("Assets/PC/Documentation~/Delivery-play-results.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable] public class Results{public string[] passed,failed;public double travelSeconds,waitSeconds;public double[] spawnTimes;public int frames;}
}
