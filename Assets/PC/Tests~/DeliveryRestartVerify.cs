using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Delivery;
using StoneGame.Economy;
using StoneGame.Orders;
using TidyUpFramework.Objects;
public static class DeliveryRestartVerify
{
 [Serializable]public class Plan{public string delivered,partial,productId,testPath;public long balance;public bool insufficientFundsDidNotMoveTruck;}
 public static async Task<object> Main()
 {
  var passed=new List<string>();void Check(bool ok,string text){if(!ok)throw new Exception(text);passed.Add(text);}
  var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("Assets/PC/Tests~/DeliveryRestartPlan.json"));var manager=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var service=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>().Service;
  var end=DateTime.UtcNow.AddSeconds(18);while(service.FindOrder(plan.partial).Status!=StoneOrderStatus.Delivered || manager.Phase!=StoneDeliveryPhase.Idle){if(DateTime.UtcNow>end)throw new Exception("Resumed order timeout: "+manager.LastError);await Task.Delay(20);}
  var first=TidyObjectRegistry.All.Where(t=>t!=null && t.GetComponent<DeliveredStoneInstance>()?.OrderId==plan.delivered).ToArray();var second=TidyObjectRegistry.All.Where(t=>t!=null && t.GetComponent<DeliveredStoneInstance>()?.OrderId==plan.partial).ToArray();
  Check(plan.insufficientFundsDidNotMoveTruck,"Actual insufficient-funds order did not move truck");Check(service.Balance==plan.balance,"Full Play Mode restart preserves paid balance");Check(first.Length==1 && second.Length==3,"Restart restores delivered one and resumes partial three exactly");
  Check(first.Concat(second).Select(t=>t.InstanceId).Distinct().Count()==4,"All runtime source IDs unique after full restart");Check(service.GetDeliveryItems(manager.gameObject.scene.name).Count==4 && service.GetDeliveryItems(manager.gameObject.scene.name).TrueForAll(i=>i.confirmed),"Crash-window reservation confirmed without duplicate spawn");Check(service.FindOrder(plan.delivered).Status==StoneOrderStatus.Delivered && manager.QueueCount==0,"Previously Delivered order stays completed and unqueued");
  var result=new Results{passed=passed.ToArray(),balance=service.Balance};File.WriteAllText("Assets/PC/Documentation~/Delivery-restart-results.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable]public class Results{public string[] passed;public long balance;}
}
