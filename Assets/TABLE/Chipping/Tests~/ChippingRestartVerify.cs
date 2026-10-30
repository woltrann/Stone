using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Cutting;
using StoneGame.Economy;
using StoneGame.Delivery;
public static class ChippingRestartVerify
{
 public static async Task<object> Main()
 {
  var boot=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!boot.StoragePath.Contains("Tests~"))throw new Exception("Sandbox required");var end=DateTime.UtcNow.AddSeconds(10);while(UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Length<2){if(DateTime.UtcNow>end)throw new Exception("Runtime halves restore timeout");await Task.Delay(30);}
  var checks=new List<string>();void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
  var pieces=UnityEngine.Object.FindObjectsByType<CutStoneHalf>();Check(pieces.Length==2,"Full Play Mode restart rebuilds exactly two runtime mesh halves");
  var completed=pieces.Single(h=>h.IsChippingCompleted);var partial=pieces.Single(h=>!h.IsChippingCompleted);
  Check(completed.CurrentHits==6 && completed.RequiredHits==6,"Completed stone persists six of six");
  Check(completed.GetComponent<Renderer>().sharedMaterials.All(m=>m==completed.CutSurfaceMaterial),"Captured cut-surface material reapplied after full restart");
  Check(partial.CurrentHits==1 && partial.RequiredHits==6,"Other stone independently resumes one of six");
  Check(completed.ContainedOre==partial.ContainedOre && !completed.TryChip(),"Ore identity preserved and completed stone remains locked");
  Check(pieces.Select(p=>p.GetComponent<TidyUpFramework.Objects.TidyObject>().InstanceId).Distinct().Count()==2,"Piece IDs remain unique");
  Check(boot.Service.Orders.Count==1 && UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>().QueueCount==0,"Completed order does not requeue");
  var result=new Result{passed=checks.ToArray()};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/Chipping-restart-results.json",JsonUtility.ToJson(result,true));boot.Service.StartNewGame(100000,out _);return result;
 }
 [Serializable]public class Result{public string[] passed;}
}


