using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using StoneGame.Economy;
using TidyUpFramework.Save;
public static class ThousandStoneCheckpointProbe
{
 sealed class MemoryStore:IEconomyStore{public bool TryRead(out string data,out string error){data=null;error=null;return true;}public bool TryWrite(string data,out string error){error=null;return true;}}
 public static object Main()
 {
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;var actual=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>().Service;
  var records=actual.GetDeliveryItems(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);var ids=new HashSet<string>();var cache=new Dictionary<string,string>();
  foreach(var record in records){ids.Add(record.source.Id);ids.Add(record.source.Id+"_left");ids.Add(record.source.Id+"_right");if(record.pieces.Count==2)cache[record.source.Id]=record.source.CustomState;}
  var snapshot=new TidyLevelSnapshot();var w=Stopwatch.StartNew();TidyLevelSnapshotter.CaptureWorld(snapshot,null,null,ids,cache);double capture=w.Elapsed.TotalMilliseconds;
  var byId=new Dictionary<string,TidyObjectSnapshot>();foreach(var entry in snapshot.Objects)byId[entry.Id]=entry;
  foreach(var record in records)if(byId.TryGetValue(record.source.Id,out var pose))record.source=pose;
  var copy=typeof(EconomyService).GetMethod("Copy",flags).Invoke(actual,null);
  var independent=new EconomyService(new MemoryStore(),0);typeof(EconomyService).GetField("state",flags).SetValue(independent,copy);
  var args=new object[]{records,null,null};w.Restart();typeof(EconomyService).GetMethod("BuildCheckpoint",flags).Invoke(independent,args);double build=w.Elapsed.TotalMilliseconds;
  var report=new Result{records=records.Count,captureMs=capture,buildCheckpointMs=build};File.WriteAllText("Assets/PC/Documentation~/Thousand-stone-checkpoint-probe.json",JsonUtility.ToJson(report,true));return report;
 }
 [Serializable]public class Result{public int records;public double captureMs,buildCheckpointMs;}
}
