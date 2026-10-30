using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using StoneGame.Economy;
public static class DeliveryPerformanceProbe
{
 sealed class MemoryStore:IEconomyStore
 {public string data;public bool TryRead(out string json,out string error){json=data;error=null;return true;}public bool TryWrite(string json,out string error){data=json;error=null;return true;}}
 static double Measure(Action run){var watch=Stopwatch.StartNew();run();return watch.Elapsed.TotalMilliseconds;}
 public static object Main()=>Run("before");
 public static object AfterCopy()=>Run("after-copy");
 static object Run(string label)
 {
  string source=Path.Combine(Application.persistentDataPath,"TidyUpSaves","stone-market.json");var json=File.ReadAllText(source);var store=new MemoryStore{data=json};var service=new EconomyService(store,0);if(!service.IsReady)throw new Exception(service.LoadError);
  var copy=typeof(EconomyService).GetMethod("Copy",BindingFlags.NonPublic|BindingFlags.Instance);
  var copies=new List<double>();var snapshots=new List<double>();var commits=new List<double>();var disk=new List<double>();
  for(int i=0;i<4;i++){copies.Add(Measure(()=>copy.Invoke(service,null)));snapshots.Add(Measure(()=>service.GetDeliveryItems("TidyUp_Warehouse")));commits.Add(Measure(()=>{if(!service.TryAddMoney(1,out var error))throw new Exception(error);}));}
  var path=Path.GetFullPath("Assets/PC/Tests~/PerfDisk_"+Guid.NewGuid().ToString("N")+".json");var file=new EconomyFileStore(path);
  try{for(int i=0;i<3;i++)disk.Add(Measure(()=>{if(!file.TryWrite(json,out var error))throw new Exception(error);}));}
  finally{foreach(var p in new[]{path,path+".bak",path+".tmp"})if(File.Exists(p))File.Delete(p);}
  var method=typeof(JsonUtility).GetMethod("ToJsonInternal",BindingFlags.Static|BindingFlags.NonPublic);var attrs=method!=null?method.GetCustomAttributesData():null;
  var result=new Result{fileBytes=new FileInfo(source).Length,copyMs=copies.ToArray(),deliveryCloneMs=snapshots.ToArray(),memoryCommitMs=commits.ToArray(),atomicDiskMs=disk.ToArray()};
  File.WriteAllText("Assets/PC/Documentation~/Delivery-performance-"+label+".json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable]public class Result{public long fileBytes;public double[] copyMs,deliveryCloneMs,memoryCommitMs,atomicDiskMs;}
}
