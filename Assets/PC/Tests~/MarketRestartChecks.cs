using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StoneGame.Economy;
using StoneGame.Delivery;
using StoneGame.Computers.UI;
using StoneGame.Orders;
using TidyUpFramework.Core;
using TidyUpFramework.Settings;
using TidyUpFramework.Save;
using TidyUpFramework.UI;
public static class MarketRestartChecks
{
 static FieldInfo Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
 static T Get<T>(object o,string name)=>(T)Field(o,name).GetValue(o);
 static void Set(object o,string name,object value)=>Field(o,name).SetValue(o,value);
 static async Task Until(Func<bool> condition,string label){var end=DateTime.UtcNow.AddSeconds(18);while(!condition()){if(DateTime.UtcNow>end)throw new Exception(label);await Task.Delay(15);}}
 sealed class MemorySave:ITidySaveStorage
 {readonly Dictionary<string,string> values=new Dictionary<string,string>();public bool TryRead(string k,out string v)=>values.TryGetValue(k,out v);public void Write(string k,string v)=>values[k]=v;public void Delete(string k)=>values.Remove(k);}
 sealed class GateStore:IEconomyStore
 {public IEconomyStore inner;public bool fail;public bool TryRead(out string data,out string error)=>inner.TryRead(out data,out error);public bool TryWrite(string data,out string error){if(fail){error="Test: restart write failed";return false;}return inner.TryWrite(data,out error);}}
 static void ClickRestart()
 {var menu=UnityEngine.Object.FindAnyObjectByType<TidyPauseMenu>();menu.Pause();if(!menu.IsPaused)throw new Exception("Pause failed");Get<Button>(menu,"restartButton").onClick.Invoke();}
 public static async Task<object> Main()
 {
  var checks=new List<string>();void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add(text);}
  var boot=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!Application.isPlaying || !boot.StoragePath.Contains("Tests~"))throw new Exception("Isolated Play Mode required");
  // All level/settings saves in this test stay in memory, including the actual scene reload.
  Set(TidyFramework.Save,"storage",new MemorySave());
  var economy=boot.Service;var delivery=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var originalStore=Get<IEconomyStore>(economy,"store");var gate=new GateStore{inner=originalStore};Set(economy,"store",gate);
  Set(delivery,"travelDuration",.12f);Set(delivery,"waitDuration",.06f);Set(delivery,"returnDuration",.12f);
  long initial=boot.Settings.startingBalance;var p=delivery.Catalog.Products[0];
  economy.StartNewGame(Math.Max(initial,100000),out _);
  Check(economy.TryPlaceOrder(delivery.Catalog,p.id,1,p.unitPrice,Guid.NewGuid().ToString("N"),out var first,out _),"Paid order created");
  await Until(()=>economy.FindOrder(first.OrderId).Status==StoneOrderStatus.Delivered && delivery.Phase==StoneDeliveryPhase.Idle,"First delivery");
  Check(UnityEngine.Object.FindObjectsByType<DeliveredStoneInstance>(FindObjectsInactive.Include).Length==1,"Purchased stone exists before restart");
  long previous=economy.Balance;gate.fail=true;ClickRestart();await Task.Delay(150);
  Check(delivery!=null && !TidyFramework.SceneLoader.IsLoading && economy.Balance==previous && economy.Orders.Count==1,"Failed reset write cancels reload and preserves paid state");gate.fail=false;
  Check(economy.TryPlaceOrder(delivery.Catalog,p.id,10,p.unitPrice,Guid.NewGuid().ToString("N"),out var partial,out _),"Second order starts active delivery");
  await Until(()=>economy.GetDeliveryItem(partial.OrderId,0)!=null && delivery.Phase==StoneDeliveryPhase.Spawning,"Active spill");
  Check(economy.HasPendingWrite || economy.FindOrder(partial.OrderId).Status==StoneOrderStatus.Processing,"Restart exercised during pending write or unfinished delivery");
  ClickRestart();
  await Until(()=>delivery==null && !TidyFramework.SceneLoader.IsLoading,"Actual pause-menu scene reload");await Task.Delay(350);
  Check(economy.Balance==initial,"Money returns to EconomySettings starting balance");
  Check(economy.Orders.Count==0,"All orders removed");
  var manager=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();
  Check(economy.GetDeliveryItems(manager.gameObject.scene.name).Count==0,"Delivery ledger cleared");
  Check(UnityEngine.Object.FindObjectsByType<DeliveredStoneInstance>(FindObjectsInactive.Include).Length==0,"Purchased and partially delivered stones removed");
  Check(manager.QueueCount==0 && manager.CurrentOrderId==null && manager.Phase==StoneDeliveryPhase.Idle,"Truck and delivery queue return to idle");
  Check(Vector3.Distance(manager.Car.position,new Vector3(50,1,15))<.001f,"Truck returns to scene starting position");
  var reloaded=new EconomyService(new EconomyFileStore(boot.StoragePath),0);
  Check(reloaded.IsReady && reloaded.Balance==initial && reloaded.Orders.Count==0 && reloaded.GetDeliveryItems(manager.gameObject.scene.name).Count==0,"Reset is durable in existing save file");
  var view=UnityEngine.Object.FindAnyObjectByType<StoneMarketView>();view.OpenOrders();await Task.Delay(50);
  Check(Get<TMP_Text>(view,"balanceLabel").text==MarketAmounts.Money(initial),"Computer balance label refreshes after restart");
  Check(Get<RectTransform>(view,"ordersContent").GetComponentsInChildren<TMP_Text>().All(t=>!t.text.Contains(first.OrderId.Substring(0,8).ToUpperInvariant()) && !t.text.Contains(partial.OrderId.Substring(0,8).ToUpperInvariant())),"Orders UI contains no previous orders");
  await Task.Delay(1500);Check(economy.Orders.Count==0 && UnityEngine.Object.FindObjectsByType<DeliveredStoneInstance>(FindObjectsInactive.Include).Length==0,"Old coroutine/background write cannot resurrect orders or stones");
  var result=new Result{passed=checks.ToArray(),initialBalance=initial};File.WriteAllText("Assets/PC/Documentation~/Market-restart-results.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable]public class Result{public string[] passed;public long initialBalance;}
}
