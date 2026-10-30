using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using StoneGame.Economy;
using StoneGame.Shop;
using StoneGame.Orders;
public static class MarketCoreChecks
{
 public sealed class MemoryStore:IEconomyStore
 {
  public string json;public bool failWrite;public int writes;
  public bool TryRead(out string value,out string error){value=json;error=null;return true;}
  public bool TryWrite(string value,out string error){error=failWrite?"Simulated disk failure":null;if(failWrite)return false;json=value;writes++;return true;}
 }
 public static object Main()
 {
  var passed=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);passed.Add(name);}
  var source=AssetDatabase.LoadAssetAtPath<StoneMarketCatalog>("Assets/PC/ScriptableObjects/StoneMarketCatalog.asset");var catalog=UnityEngine.Object.Instantiate(source);var product=catalog.Products[0];var mem=new MemoryStore();
  try
  {
   Check(source.ValidateCatalog().Count==0,"Real prefab/Definition/RawStone/profile/collider references valid");
   var economy=new EconomyService(mem,10000);Check(economy.IsReady && economy.Balance==10000,"New game starts with configured 10000");
   foreach(var input in new[]{"","0","-1","1.5","1,5","abc","2147483648","999999999999999"," 1","1 ","+1","1e2"})Check(!MarketAmounts.TryQuantity(input,999,out _),"Invalid quantity rejected: '"+input+"'");
   Check(MarketAmounts.TryQuantity("15",999,out var q) && q==15,"Integer quantity parsed");Check(!MarketAmounts.TryQuantity("1000",999,out _),"Inspector quantity cap enforced");
   Check(!MarketAmounts.TryTotal(long.MaxValue,2,out _),"Total overflow blocked");Check(!MarketAmounts.TryTotal(-1,2,out _),"Negative unit price blocked");
   int placedEvents=0,balanceEvents=0;economy.OrderPlaced+=_=>placedEvents++;economy.BalanceChanged+=_=>balanceEvents++;
   string request=Guid.NewGuid().ToString("N");Check(economy.TryPlaceOrder(catalog,product.id,15,100,request,out var order,out var error),"15 x 100 order commits");
   Check(economy.Balance==8500 && economy.Orders.Count==1 && order.TotalPaid==1500,"Balance 8500 and one order after 1500 payment");Check(order.Status==StoneOrderStatus.Pending,"New order Pending");
   Check(economy.TryPlaceOrder(catalog,product.id,15,100,request,out var duplicate,out _) && duplicate.OrderId==order.OrderId && economy.Balance==8500 && placedEvents==1 && balanceEvents==1,"Repeated request charges once and emits once");
   Check(!economy.TryPlaceOrder(catalog,product.id,16,100,request,out _,out _),"Same request cannot change payload");
   var reload=new EconomyService(mem,999999);Check(reload.Balance==8500 && reload.Orders.Count==1,"Load preserves wallet/history and ignores new starting balance");
   Check(reload.TryPlaceOrder(catalog,product.id,15,100,request,out _,out _) && reload.Balance==8500 && reload.Orders.Count==1,"Idempotency survives save/load");
   Check(!reload.TryPlaceOrder(catalog,product.id,999,100,Guid.NewGuid().ToString("N"),out _,out var insufficient) && insufficient==EconomyService.InsufficientFunds && reload.Balance==8500 && reload.Orders.Count==1,"Insufficient funds leaves wallet/history unchanged");
   mem.failWrite=true;Check(!reload.TryPlaceOrder(catalog,product.id,1,100,Guid.NewGuid().ToString("N"),out _,out _) && reload.Balance==8500 && reload.Orders.Count==1,"Save failure rolls back both debit and order");Check(!reload.TryAddMoney(100,out _) && reload.Balance==8500,"Income save failure leaves balance unchanged");mem.failWrite=false;
   product.unitPrice=250;Check(order.UnitPrice==100 && order.TotalPaid==1500,"Historical price remains immutable after catalog edit");Check(!reload.TryPlaceOrder(catalog,product.id,1,100,Guid.NewGuid().ToString("N"),out _,out _),"Stale quote rejected");
   product.active=false;Check(!reload.TryPlaceOrder(catalog,product.id,1,250,Guid.NewGuid().ToString("N"),out _,out _),"Inactive product rejected");product.active=true;
   var definition=product.definition;product.definition=null;Check(!catalog.TryFind(product.id,out _,out _),"Missing Definition rejected");product.definition=AssetDatabase.LoadAssetAtPath<TidyUpFramework.Objects.TidyObjectDefinition>("Assets/STONE/Data/TidyDefinitions/Stone_CutPiece.asset");Check(!catalog.TryFind(product.id,out _,out _),"Mismatched Definition rejected");product.definition=definition;
   product.unitPrice=-1;Check(!catalog.TryFind(product.id,out _,out _),"Negative product price rejected at transaction boundary");product.unitPrice=100;
   Check(!reload.TrySpend(-1,out _) && !reload.TryAddMoney(-1,out _),"Negative money API calls rejected");Check(reload.TryAddMoney(500,out _) && reload.Balance==9000,"Shared income API persists addition");Check(reload.TrySpend(200,out _) && reload.Balance==8800,"Shared spend API persists subtraction");
   Check(!reload.TryAddMoney(long.MaxValue,out _) && reload.Balance==8800,"Balance addition overflow blocked");
   Check(reload.TryBeginDelivery(order.OrderId,"core-test",out _) && !reload.TrySetOrderStatus(order.OrderId,StoneOrderStatus.Delivered,out _),"Delivery waits for all confirmed physical item records");
   for(int i=0;i<order.Quantity;i++){var item=new StoneGame.Delivery.StoneDeliveryItem{orderId=order.OrderId,index=i,sceneName="core-test",source=new TidyUpFramework.Save.TidyObjectSnapshot{Id=order.ItemInstanceId(i)},localScale=Vector3.one};if(!reload.TryPrepareDeliveryItem(item,out _) || !reload.TryConfirmDeliveryItem(order.OrderId,i,out _))throw new Exception("Core-test delivery setup");}
   Check(reload.TrySetOrderStatus(order.OrderId,StoneOrderStatus.Delivered,out _),"Complete delivery status API transitions");Check(!reload.TrySetOrderStatus(order.OrderId,StoneOrderStatus.Pending,out _),"Completed order cannot return to Pending");
   Check(new EconomyService(mem,10000).Orders[0].Status==StoneOrderStatus.Delivered,"Order status persists");Check(order.ItemInstanceId(0)!=order.ItemInstanceId(1),"Future per-stone instance IDs distinct and deterministic");
   Check(new EconomyService(new MemoryStore{json="{}"},10000).IsReady==false,"Malformed existing save does not reset to starting money");
   var failedInitial=new MemoryStore{failWrite=true};Check(!new EconomyService(failedInitial,10000).IsReady,"Initial save failure does not enable spending");
   string path=Path.GetFullPath("Assets/PC/Tests~/MarketFileCheck_"+Guid.NewGuid().ToString("N")+".json");var disk=new EconomyFileStore(path);var persisted=new EconomyService(disk,10000);
   Check(persisted.TryPlaceOrder(catalog,product.id,15,100,Guid.NewGuid().ToString("N"),out _,out _) && new EconomyService(disk,1).Balance==8500,"Real atomic file replacement/load preserves payment and order");
   Check(File.Exists(path+".bak"),"Previous complete document retained as backup");
   Check(reload.StartNewGame(12000,out _) && reload.Balance==12000 && reload.Orders.Count==0,"Explicit new game resets wallet/history together");
   var result=new Results{passed=passed.ToArray(),failed=Array.Empty<string>()};File.WriteAllText("Assets/PC/Documentation~/Market-core-results.json",JsonUtility.ToJson(result,true));return result;
  }
  finally{UnityEngine.Object.DestroyImmediate(catalog);}
 }
 [Serializable] public class Results{public string[] passed,failed;}
}
