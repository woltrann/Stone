using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using StoneGame.Economy;
using StoneGame.Orders;
using StoneGame.Delivery;
using StoneGame.Shop;
using TidyUpFramework.Save;
public static class DeliveryCoreChecks
{
 public sealed class Store:IEconomyStore
 {public string data;public bool fail;public bool TryRead(out string json,out string error){json=data;error=null;return true;}public bool TryWrite(string json,out string error){error=fail?"Simulated disk failure":null;if(fail)return false;data=json;return true;}}
 static StoneDeliveryItem Item(StoneOrder o,int index)=>new StoneDeliveryItem{orderId=o.OrderId,index=index,sceneName="test-scene",source=new TidyObjectSnapshot{Id=o.ItemInstanceId(index),Position=new Vector3(0,4,0)},localScale=Vector3.one,falling=true};
 public static object Main()
 {
  var passed=new List<string>();void Check(bool ok,string text){if(!ok)throw new Exception(text);passed.Add(text);}
  var catalog=AssetDatabase.LoadAssetAtPath<StoneMarketCatalog>("Assets/PC/ScriptableObjects/StoneMarketCatalog.asset");var product=catalog.Products[0];var store=new Store();var service=new EconomyService(store,100000);
  int placed=0;service.OrderPlaced+=_=>placed++;
  Check(service.TryPlaceOrder(catalog,product.id,2,product.unitPrice,Guid.NewGuid().ToString("N"),out var order,out _),"Existing order API accepts paid order");long paidBalance=service.Balance;
  Check(!service.TrySetOrderStatus(order.OrderId,StoneOrderStatus.Delivered,out _),"Pending cannot skip delivery");
  Check(!service.TryPrepareDeliveryItem(Item(order,0),out _),"Cannot reserve item before Processing");
  Check(service.TryBeginDelivery(order.OrderId,"test-scene",out _) && service.FindOrder(order.OrderId).Status==StoneOrderStatus.Processing,"Begin persists Processing and scene");
  Check(!service.TryBeginDelivery(order.OrderId,"other-scene",out _),"Processing order cannot be claimed by another scene");
  Check(!service.TrySetOrderStatus(order.OrderId,StoneOrderStatus.Delivered,out _),"No Delivered status before every item exists");
  Check(!service.TryPrepareDeliveryItem(Item(order,1),out _),"Ordinal one waits for confirmed ordinal zero");
  Check(service.TryPrepareDeliveryItem(Item(order,0),out _),"Durable item reservation saved before activation");
  var reload=new EconomyService(store,1);Check(reload.IsReady && !reload.GetDeliveryItem(order.OrderId,0).confirmed && reload.Balance==paidBalance,"Unconfirmed reservation survives reload without another charge");
  Check(reload.TryPrepareDeliveryItem(Item(order,0),out _) && reload.GetDeliveryItems("test-scene").Count==1,"Retry of same reservation is idempotent");
  store.fail=true;Check(!reload.TryConfirmDeliveryItem(order.OrderId,0,out _) && !reload.GetDeliveryItem(order.OrderId,0).confirmed,"Failed confirmation retains retryable reservation");store.fail=false;
  Check(reload.TryConfirmDeliveryItem(order.OrderId,0,out _) && reload.TryConfirmDeliveryItem(order.OrderId,0,out _),"Confirm is durable and idempotent");
  Check(!reload.TrySetOrderStatus(order.OrderId,StoneOrderStatus.Delivered,out _),"Partial order is never Delivered");
  Check(reload.TryPrepareDeliveryItem(Item(order,1),out _) && reload.TryConfirmDeliveryItem(order.OrderId,1,out _),"Second ordinal accepted exactly once");
  var world=reload.GetDeliveryItems("test-scene");world[0].source.Position=new Vector3(8,1,2);Check(reload.TryCheckpointDeliveryItems(world,out _),"Physical world snapshot checkpoint saved");
  Check(reload.TrySetOrderStatus(order.OrderId,StoneOrderStatus.Delivered,out _) && reload.Balance==paidBalance,"Complete delivery updates existing status without payment");
  var loaded=new EconomyService(store,1);Check(loaded.FindOrder(order.OrderId).Status==StoneOrderStatus.Delivered && loaded.GetDeliveryItems("test-scene").Count==2 && loaded.GetDeliveryItem(order.OrderId,0).source.Position==new Vector3(8,1,2),"Delivered status and exact item identities/poses survive load");
  Check(!loaded.TryBeginDelivery(order.OrderId,"test-scene",out _),"Delivered order cannot start another trip");Check(placed==1,"Delivery operations never reemit purchase event");
  Check(!loaded.TryPlaceOrder(catalog,product.id,999,Math.Max(product.unitPrice,1000000),Guid.NewGuid().ToString("N"),out _,out _),"Rejected payment creates no delivery event");
  Check(loaded.StartNewGame(12345,out _) && loaded.GetDeliveryItems("test-scene").Count==0,"New game clears delivery ledger with orders");
  var result=new Results{passed=passed.ToArray()};File.WriteAllText("Assets/PC/Documentation~/Delivery-core-results.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable] public class Results{public string[] passed;}
}
