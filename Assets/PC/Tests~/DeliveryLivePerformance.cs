using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Delivery;
using StoneGame.Economy;
using StoneGame.Orders;
using TidyUpFramework.Core;
using TidyUpFramework.Objects;
public static class DeliveryLivePerformance
{
 static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static async Task Until(Func<bool> condition,string text,int seconds=40){var end=DateTime.UtcNow.AddSeconds(seconds);while(!condition()){if(DateTime.UtcNow>end)throw new Exception(text);await Task.Delay(10);}}
 sealed class TraceStore:IEconomyStore
 {
  readonly EconomyFileStore inner;readonly int main;public int mainWrites,workerWrites;public TraceStore(string path){inner=new EconomyFileStore(path);main=Thread.CurrentThread.ManagedThreadId;}
  public bool TryRead(out string value,out string error)=>inner.TryRead(out value,out error);
  public bool TryWrite(string value,out string error){if(Thread.CurrentThread.ManagedThreadId==main)Interlocked.Increment(ref mainWrites);else Interlocked.Increment(ref workerWrites);return inner.TryWrite(value,out error);}
 }
 static double Percentile(List<double> numbers,double percentile){numbers.Sort();return numbers[Math.Min(numbers.Count-1,(int)(numbers.Count*percentile))];}
 public static async Task<object> Main()
 {
  if(!Application.isPlaying)throw new Exception("Play required");var manager=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!bootstrap.StoragePath.Contains("Tests~"))throw new Exception("Refuse production writes");
  var original=bootstrap.Service;manager.enabled=false;
  string production=Path.Combine(Application.persistentDataPath,"TidyUpSaves","stone-market.json");string scratch=Path.GetFullPath("Assets/PC/Tests~/PerfLarge_"+Guid.NewGuid().ToString("N")+".json");File.WriteAllText(scratch,File.ReadAllText(production));
  var trace=new TraceStore(scratch);var service=new EconomyService(trace,0);if(!service.IsReady)throw new Exception(service.LoadError);
  var frames=new List<double>();var gaps=new List<double>();int spawned=0;Action<string,int,GameObject> handler=(id,index,go)=>{spawned++;gaps.Add(Time.unscaledTimeAsDouble);};
  try
  {
   foreach(var old in service.Orders.ToArray())if(old.Status==StoneOrderStatus.Pending || old.Status==StoneOrderStatus.Processing)service.TrySetOrderStatus(old.OrderId,StoneOrderStatus.Cancelled,out _);
   service.TryAddMoney(1000000,out _);int history=service.GetDeliveryItems(manager.gameObject.scene.name).Count;
   Set(bootstrap,"service",service);TidyServices.Register(service);manager.enabled=true;await Task.Delay(5000);service.FlushPendingWrites();
   var p=manager.Catalog.Products[0];manager.StoneSpawned+=handler;
   if(!service.TryPlaceOrder(manager.Catalog,p.id,30,p.unitPrice,Guid.NewGuid().ToString("N"),out var order,out var error))throw new Exception(error);
   long balance=service.Balance;
   await Until(()=>manager.Phase==StoneDeliveryPhase.Spawning,"Start stream");int baselineMain=trace.mainWrites,baselineWorker=trace.workerWrites,lastFrame=-1;
   var end=DateTime.UtcNow.AddSeconds(40);
   while(service.FindOrder(order.OrderId).Status!=StoneOrderStatus.Delivered)
   {
    if(DateTime.UtcNow>end)throw new Exception(manager.LastError??"Stream timeout");
    if(lastFrame!=Time.frameCount){lastFrame=Time.frameCount;frames.Add(Time.unscaledDeltaTime*1000d);}await Task.Delay(1);
   }
   if(spawned!=30 || service.Balance!=balance)throw new Exception("Count or payment regression");
   var result=new Result{existingItems=history,ordered=30,spawned=spawned,frameCount=frames.Count,medianFrameMs=Percentile(frames,.5),p95FrameMs=Percentile(frames,.95),maxFrameMs=frames.Max(),framesOver50ms=frames.Count(x=>x>50),mainThreadFileWrites=trace.mainWrites-baselineMain,workerFileWrites=trace.workerWrites-baselineWorker,lastWorkerMs=service.LastBackgroundWriteMilliseconds};
   File.WriteAllText("Assets/PC/Documentation~/Delivery-performance-live.json",JsonUtility.ToJson(result,true));return result;
  }
  finally
  {
   manager.StoneSpawned-=handler;service.StartNewGame(100000,out _);await Task.Delay(150);manager.enabled=false;Set(bootstrap,"service",original);TidyServices.Register(original);manager.enabled=true;
   foreach(var path in new[]{scratch,scratch+".bak",scratch+".tmp"})if(File.Exists(path))File.Delete(path);
  }
 }
 [Serializable]public class Result{public int existingItems,ordered,spawned,frameCount,framesOver50ms,mainThreadFileWrites,workerFileWrites;public double medianFrameMs,p95FrameMs,maxFrameMs,lastWorkerMs;}
}
