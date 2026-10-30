using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Delivery;
using StoneGame.Economy;
using StoneGame.Orders;
using TidyUpFramework.Save;
public static class DeliveryRestartPrepare
{
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
 static async Task Until(Func<bool> test,string text){var end=DateTime.UtcNow.AddSeconds(15);while(!test()){if(DateTime.UtcNow>end)throw new Exception(text);await Task.Delay(20);}}
 public static async Task<object> Main()
 {
  var manager=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!bootstrap.StoragePath.Contains("Tests~"))throw new Exception("Sandbox required");var service=bootstrap.Service;var catalog=manager.Catalog;var product=catalog.Products[0];
  service.StartNewGame(0,out _);var position=manager.Car.position;
  bool rejected=!service.TryPlaceOrder(catalog,product.id,1,product.unitPrice,Guid.NewGuid().ToString("N"),out _,out var error) && error==EconomyService.InsufficientFunds;await Task.Delay(100);
  if(!rejected || manager.QueueCount!=0 || manager.Car.position!=position)throw new Exception("Insufficient-funds order moved truck");service.StartNewGame(100000,out _);
  if(!service.TryPlaceOrder(catalog,product.id,1,product.unitPrice,Guid.NewGuid().ToString("N"),out var delivered,out error))throw new Exception(error);
  await Until(()=>service.FindOrder(delivered.OrderId).Status==StoneOrderStatus.Delivered && manager.Phase==StoneDeliveryPhase.Idle,"Default full delivery");
  float travel=Get<float>(manager,"travelDuration"),wait=Get<float>(manager,"waitDuration"),ret=Get<float>(manager,"returnDuration"),retry=Get<float>(manager,"retryDelay");var point=manager.SpawnPoint;
  StoneOrder partial=null;Action<string,int,GameObject> observer=(id,index,go)=>{if(partial!=null && id==partial.OrderId && index==0)Set(manager,"spawnPoint",null);};
  try
  {
   Set(manager,"travelDuration",.12f);Set(manager,"waitDuration",.08f);Set(manager,"returnDuration",.12f);Set(manager,"retryDelay",60f);manager.StoneSpawned+=observer;
   if(!service.TryPlaceOrder(catalog,product.id,3,product.unitPrice,Guid.NewGuid().ToString("N"),out partial,out error))throw new Exception(error);
   await Until(()=>manager.Phase==StoneDeliveryPhase.Faulted,"Partial stream pause");
   var reservation=new StoneDeliveryItem{orderId=partial.OrderId,index=1,sceneName=manager.gameObject.scene.name,falling=true,localScale=product.prefab.transform.localScale,source=new TidyObjectSnapshot{Id=partial.ItemInstanceId(1),Position=point.position,Rotation=product.prefab.transform.rotation}};
   if(!service.TryPrepareDeliveryItem(reservation,out error) || !manager.CheckpointNow())throw new Exception(error??manager.LastError);
   var plan=new Plan{delivered=delivered.OrderId,partial=partial.OrderId,balance=service.Balance,productId=product.id,testPath=bootstrap.StoragePath,insufficientFundsDidNotMoveTruck=true};File.WriteAllText("Assets/PC/Tests~/DeliveryRestartPlan.json",JsonUtility.ToJson(plan,true));return plan;
  }
  finally{manager.StoneSpawned-=observer;Set(manager,"spawnPoint",point);Set(manager,"travelDuration",travel);Set(manager,"waitDuration",wait);Set(manager,"returnDuration",ret);Set(manager,"retryDelay",retry);}
 }
 [Serializable]public class Plan{public string delivered,partial,productId,testPath;public long balance;public bool insufficientFundsDidNotMoveTruck;}
}
