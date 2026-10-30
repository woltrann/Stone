using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using StoneGame.Economy;
using StoneGame.Delivery;
using StoneGame.Shop;
using StoneGame.Orders;
using TidyUpFramework.Save;
public static class DeliveryAsyncChecks
{
 sealed class Store:IEconomyStore
 {
  public string data;public bool fail;public int lastThread,writes;
  public bool TryRead(out string json,out string error){json=data;error=null;return true;}
  public bool TryWrite(string json,out string error){lastThread=Thread.CurrentThread.ManagedThreadId;Thread.Sleep(15);writes++;error=fail?"Injected write failure":null;if(fail)return false;data=json;return true;}
 }
 public static async Task<object> Main()
 {
  var passed=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);passed.Add(name);}
  int main=Thread.CurrentThread.ManagedThreadId,callback=0;var store=new Store();var service=new EconomyService(store,100000);
  var catalog=AssetDatabase.LoadAssetAtPath<StoneMarketCatalog>("Assets/PC/ScriptableObjects/StoneMarketCatalog.asset");var product=catalog.Products[0];
  Check(service.TryPlaceOrder(catalog,product.id,1,product.unitPrice,Guid.NewGuid().ToString("N"),out var order,out _),"Paid test order accepted");long paid=service.Balance;
  service.OrdersChanged+=()=>callback=Thread.CurrentThread.ManagedThreadId;
  var begin=service.BeginDeliveryAsync(order.OrderId,"test");
  Check(!begin.IsCompleted && service.FindOrder(order.OrderId).Status==StoneOrderStatus.Pending,"Async state stays unpublished until durable acknowledgement");
  while(!begin.IsCompleted){service.PumpWrites();await Task.Delay(5);}
  Check(begin.Success && store.lastThread!=main && callback==main,"Encoding/write run on worker; notifications run on Unity thread");
  var item=new StoneDeliveryItem{orderId=order.OrderId,index=0,sceneName="test",falling=true,localScale=Vector3.one,source=new TidyObjectSnapshot{Id=order.ItemInstanceId(0),Position=Vector3.up*4}};
  var prepare=service.PrepareDeliveryItemAsync(item);item.source.Position=Vector3.one*99;service.FlushPendingWrites();
  Check(prepare.Success && service.GetDeliveryItem(order.OrderId,0).source.Position==Vector3.up*4,"Caller mutations cannot change in-flight detached save");
  var detached=service.GetDeliveryItems("test");detached[0].source.Position=Vector3.zero;
  Check(service.GetDeliveryItem(order.OrderId,0).source.Position==Vector3.up*4,"Read snapshots do not alias durable mutable fields");
  store.fail=true;var confirm=service.ConfirmDeliveryItemAsync(order.OrderId,0);service.FlushPendingWrites();
  Check(!confirm.Success && !service.GetDeliveryItem(order.OrderId,0).confirmed && !service.HasPendingWrite,"Failed worker write preserves reservation and releases writer");
  store.fail=false;confirm=service.ConfirmDeliveryItemAsync(order.OrderId,0);
  Check(service.TryAddMoney(7,out _) && confirm.Success && service.GetDeliveryItem(order.OrderId,0).confirmed && service.Balance==paid+7,"Wallet transaction serializes behind outstanding delivery write without lost update");
  var delivered=service.SetOrderStatusAsync(order.OrderId,StoneOrderStatus.Delivered);service.FlushPendingWrites();
  var reload=new EconomyService(store,0);
  Check(delivered.Success && reload.IsReady && reload.FindOrder(order.OrderId).Status==StoneOrderStatus.Delivered && reload.Balance==paid+7,"Background save remains compatible with existing envelope and loader");
  int count=store.writes;var unchanged=service.CheckpointDeliveryItemsAsync(service.GetDeliveryItems("test"));
  Check(unchanged.IsCompleted && unchanged.Success && store.writes==count,"Unchanged snapshot does not schedule file writes");
  var updates=service.GetDeliveryItems("test");updates[0].source.Position=Vector3.one;
  var checkpoint=service.CheckpointDeliveryItemsAsync(updates);service.StartNewGame(123,out _);
  Check(checkpoint.Success && service.Balance==123 && service.GetDeliveryItems("test").Count==0 && new EconomyService(store,0).Balance==123,"Reset flushes old writer so it cannot overwrite new game");
  var hash=typeof(EconomyService).Assembly.GetType("StoneGame.Economy.EconomySaveHash").GetMethod("Compute",BindingFlags.Public|BindingFlags.Static);
  foreach(var value in new[]{"","abc","Türkçe taş cevher 🎮",new string('x',100000)})using(var sha=SHA256.Create())Check((string)hash.Invoke(null,new object[]{value})==Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value))),"Standard SHA-256 compatibility: "+value.Length+" chars");
  var result=new Results{passed=passed.ToArray()};File.WriteAllText("Assets/PC/Documentation~/Delivery-async-results.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable]public class Results{public string[] passed;}
}
