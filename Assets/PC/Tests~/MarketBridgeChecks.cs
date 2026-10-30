using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using StoneGame.Economy;
using TidyUpFramework.Save;
public static class MarketBridgeChecks
{
 sealed class Store:IEconomyStore
 {public string data;public bool TryRead(out string json,out string error){json=data;error=null;return true;}public bool TryWrite(string json,out string error){data=json;error=null;return true;}}
 sealed class FrameworkStore:ITidySaveStorage
 {public bool TryRead(string key,out string json){json=null;return false;}public void Write(string key,string json){}public void Delete(string key){}}
 public static object Main()
 {
  var passed=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);passed.Add(name);}
  var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  var serviceField=typeof(StoneEconomyBootstrap).GetField("service",flags);var progressField=typeof(StoneEconomyBootstrap).GetField("observedProgress",flags);var settingsField=typeof(StoneEconomyBootstrap).GetField("settings",flags);
  var oldService=serviceField.GetValue(bootstrap);var oldProgress=progressField.GetValue(bootstrap);var oldSettings=settingsField.GetValue(bootstrap);var settings=UnityEngine.Object.Instantiate(bootstrap.Settings);settings.startingBalance=12345;
  var store=new Store();var economy=new EconomyService(store,100);var framework=new TidySaveService(new FrameworkStore(),null);
  try
  {
   serviceField.SetValue(bootstrap,economy);settingsField.SetValue(bootstrap,settings);progressField.SetValue(bootstrap,framework.Progress);
   var handler=(Action<TidyProgressData>)Delegate.CreateDelegate(typeof(Action<TidyProgressData>),bootstrap,typeof(StoneEconomyBootstrap).GetMethod("ProgressChanged",flags));framework.ProgressChanged+=handler;
   framework.SaveProgress();Check(economy.Balance==100,"Ordinary framework save does not reset wallet");
   framework.ResetProgress();Check(economy.Balance==12345 && economy.Orders.Count==0,"Actual framework ResetProgress event initializes economy once");economy.TrySpend(345,out _);framework.SaveProgress();Check(economy.Balance==12000,"Later ordinary saves retain post-reset balance");
   Check(new EconomyService(store,999).Balance==12000,"New-game bridge persists resulting balance");framework.ProgressChanged-=handler;
  }
  finally{serviceField.SetValue(bootstrap,oldService);progressField.SetValue(bootstrap,oldProgress);settingsField.SetValue(bootstrap,oldSettings);UnityEngine.Object.Destroy(settings);}
  File.WriteAllText("Assets/PC/Documentation~/Market-new-game-results.json",JsonUtility.ToJson(new Results{passed=passed.ToArray()},true));return new {passed};
 }
 [Serializable] public class Results{public string[] passed;}
}
