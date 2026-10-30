using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using StoneGame.Delivery;
using StoneGame.Economy;
using StoneGame.Computers.UI;
public static class DeliveryCleanup
{
 public static async Task<object> Main()
 {
  var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!bootstrap.StoragePath.Contains("Tests~"))throw new Exception("Cannot clear production orders");
  var view=UnityEngine.Object.FindAnyObjectByType<StoneMarketView>();view.OpenOrders();await Task.Delay(50);bool ui=view.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Teslim edildi"));
  if(!ui)throw new Exception("Orders UI did not show Delivered");
  if(!bootstrap.Service.StartNewGame(100000,out var error))throw new Exception(error);await Task.Delay(200);
  int count=UnityEngine.Object.FindObjectsByType<DeliveredStoneInstance>(FindObjectsInactive.Include).Length;
  if(count!=0)throw new Exception("Temporary delivery stones remain: "+count);
  var result=new Result{ordersUiShowsDelivered=ui,remainingTestStones=count,remainingTestOrders=bootstrap.Service.Orders.Count};File.WriteAllText("Assets/PC/Documentation~/Delivery-cleanup-results.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable]public class Result{public bool ordersUiShowsDelivered;public int remainingTestStones,remainingTestOrders;}
}
