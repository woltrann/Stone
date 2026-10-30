using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using StoneGame.Delivery;
using StoneGame.Economy;
using StoneGame.Orders;
using TidyUpFramework.Core;
using TidyUpFramework.Objects;
public static class DeliveryFailureChecks
{
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
 static async Task Until(Func<bool> test,string name){var end=DateTime.UtcNow.AddSeconds(8);while(!test()){if(DateTime.UtcNow>end)throw new Exception("Timeout: "+name);await Task.Delay(15);}}
 static int Count(string id)=>TidyObjectRegistry.All.Count(t=>t!=null && t.GetComponent<DeliveredStoneInstance>()!=null && t.GetComponent<DeliveredStoneInstance>().OrderId==id);
 sealed class ToggleStore:IEconomyStore
 {
  readonly IEconomyStore inner;public string failOrder;public bool armed;public bool triggered;
  public ToggleStore(IEconomyStore store){inner=store;}
  public bool TryRead(out string data,out string error)=>inner.TryRead(out data,out error);
  public bool TryWrite(string data,out string error)
  {
   if(armed)
   {var payload=JObject.Parse((string)JObject.Parse(data)["payload"]);if(payload["deliveryItems"] is JArray items && items.Any(i=>(string)i["orderId"]==failOrder && (int)i["index"]==0 && (bool)i["confirmed"]))
    {armed=false;triggered=true;error="Test: confirmation write failed";return false;}}
   return inner.TryWrite(data,out error);
  }
 }
 public static async Task<object> Main()
 {
  var passed=new List<string>();var failed=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);passed.Add(name);}
  var manager=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!bootstrap.StoragePath.Contains("Tests~"))throw new Exception("Production save protected");
  var original=bootstrap.Service;var originalCatalog=manager.Catalog;var point=manager.SpawnPoint;var car=manager.Car;var oldRotation=car.rotation;
  var durations=new[]{"travelDuration","waitDuration","returnDuration","spawnInterval","retryDelay"};var values=durations.Select(n=>Get<float>(manager,n)).ToArray();
  var toggle=new ToggleStore(Get<IEconomyStore>(original,"store"));var economy=new EconomyService(toggle,100000);GameObject blocker=null;Rigidbody temporaryBody=null;Action<string,int,GameObject> partial=null;
  try
  {
   manager.enabled=false;Set(bootstrap,"service",economy);TidyServices.Register(economy);
   Set(manager,"travelDuration",.12f);Set(manager,"waitDuration",.08f);Set(manager,"returnDuration",.12f);Set(manager,"spawnInterval",.12f);Set(manager,"retryDelay",60f);manager.enabled=true;await Task.Delay(100);
   var product=originalCatalog.Products[0];
   Check(economy.TryPlaceOrder(originalCatalog,product.id,2,product.unitPrice,Guid.NewGuid().ToString("N"),out var ack,out _),"Fault test order paid once");toggle.failOrder=ack.OrderId;toggle.armed=true;long balance=economy.Balance;
   await Until(()=>manager.Phase==StoneDeliveryPhase.Faulted,"confirmation failure");
   Check(toggle.triggered && Count(ack.OrderId)==1 && !economy.GetDeliveryItem(ack.OrderId,0).confirmed,"Write failure after activation retains one physical stone and unconfirmed record");
   Check(economy.FindOrder(ack.OrderId).Status==StoneOrderStatus.Processing && economy.Balance==balance,"Failed confirmation never marks Delivered or charges again");
   manager.RetryFailedDelivery();await Until(()=>economy.FindOrder(ack.OrderId).Status==StoneOrderStatus.Delivered && manager.Phase==StoneDeliveryPhase.Idle,"confirm retry");
   Check(Count(ack.OrderId)==2 && TidyObjectRegistry.All.Count(t=>t!=null && t.InstanceId==ack.ItemInstanceId(0))==1,"Retry confirms existing first stone and creates only missing ordinal");
   economy.StartNewGame(100000,out _);await Task.Delay(150);
   Check(economy.TryPlaceOrder(originalCatalog,product.id,3,product.unitPrice,Guid.NewGuid().ToString("N"),out var partialOrder,out _),"Partial-spawn fault order accepted");balance=economy.Balance;
   partial=(id,index,go)=>{if(id==partialOrder.OrderId && index==0)Set(manager,"spawnPoint",null);};manager.StoneSpawned+=partial;
   await Until(()=>manager.Phase==StoneDeliveryPhase.Faulted,"missing outlet after first item");manager.StoneSpawned-=partial;partial=null;
   Check(Count(partialOrder.OrderId)==1 && economy.FindOrder(partialOrder.OrderId).Status==StoneOrderStatus.Processing,"Missing outlet after one item preserves partial shipment");
   Set(manager,"spawnPoint",point);manager.RetryFailedDelivery();await Until(()=>economy.FindOrder(partialOrder.OrderId).Status==StoneOrderStatus.Delivered && manager.Phase==StoneDeliveryPhase.Idle,"partial resume");Check(Count(partialOrder.OrderId)==3 && economy.Balance==balance,"Partial retry produces exactly total quantity without second payment");
   economy.StartNewGame(100000,out _);await Task.Delay(150);
   Check(economy.TryPlaceOrder(originalCatalog,product.id,1,product.unitPrice,Guid.NewGuid().ToString("N"),out var missing,out _),"Validation fault test paid order exists");Set(manager,"catalog",null);
   await Until(()=>manager.Phase==StoneDeliveryPhase.Faulted,"missing catalog validation");Check(Count(missing.OrderId)==0 && economy.FindOrder(missing.OrderId).Status==StoneOrderStatus.Pending,"Invalid references fail before any spawn or Processing claim");
   Set(manager,"catalog",originalCatalog);manager.RetryFailedDelivery();await Until(()=>economy.FindOrder(missing.OrderId).Status==StoneOrderStatus.Delivered && manager.Phase==StoneDeliveryPhase.Idle,"validation retry");Check(Count(missing.OrderId)==1,"Immediate validation failure remains retryable instead of stuck coroutine");
   economy.StartNewGame(100000,out _);await Task.Delay(150);
   temporaryBody=car.gameObject.AddComponent<Rigidbody>();temporaryBody.useGravity=false;temporaryBody.isKinematic=false;
   Check(economy.TryPlaceOrder(originalCatalog,product.id,1,product.unitPrice,Guid.NewGuid().ToString("N"),out var rb,out _),"Truck Rigidbody branch order accepted");await Until(()=>manager.Phase==StoneDeliveryPhase.Arriving,"kinematic truck movement");Check(temporaryBody.isKinematic,"Existing truck Rigidbody made temporarily kinematic");
   await Until(()=>economy.FindOrder(rb.OrderId).Status==StoneOrderStatus.Delivered && manager.Phase==StoneDeliveryPhase.Idle,"Rigidbody truck returns");Check(!temporaryBody.isKinematic && !temporaryBody.useGravity && Vector3.Distance(car.position,new Vector3(50,1,15))<.001f && Quaternion.Angle(car.rotation,oldRotation)<.001f,"MovePosition branch restores truck body policy and rotation: pos="+car.position+" bodyPos="+temporaryBody.position+" kin="+temporaryBody.isKinematic+" gravity="+temporaryBody.useGravity+" angle="+Quaternion.Angle(car.rotation,oldRotation));
  }
  catch(Exception ex){failed.Add(ex.ToString());}
  finally
  {
   if(partial!=null)manager.StoneSpawned-=partial;toggle.armed=false;economy.StartNewGame(100000,out _);manager.enabled=false;
   for(int i=0;i<durations.Length;i++)Set(manager,durations[i],values[i]);Set(manager,"catalog",originalCatalog);Set(manager,"spawnPoint",point);if(temporaryBody!=null)UnityEngine.Object.Destroy(temporaryBody);if(blocker!=null)UnityEngine.Object.Destroy(blocker);
   Set(bootstrap,"service",original);TidyServices.Register(original);manager.enabled=true;await Task.Delay(120);
  }
  var result=new Results{passed=passed.ToArray(),failed=failed.ToArray()};File.WriteAllText("Assets/PC/Documentation~/Delivery-failure-results.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable]public class Results{public string[] passed,failed;}
}

