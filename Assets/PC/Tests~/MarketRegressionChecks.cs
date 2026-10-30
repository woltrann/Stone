using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Cutting;
using TidyUpFramework.Interaction;
using TidyUpFramework.Player;
using TidyUpFramework.Save;
using TidyUpFramework.Objects;
public static class MarketRegressionChecks
{
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
 public static async Task<object> Main()
 {
  var passed=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);passed.Add(name);}
  Check(Application.isPlaying && !UnityEngine.Object.FindAnyObjectByType<TidyUpFramework.Levels.TidyLevelManager>().SavesProgress,"Regression uses isolated Play session");
  var interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();var player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();var saw=UnityEngine.Object.FindAnyObjectByType<StoneSawMachine>();var slot=UnityEngine.Object.FindAnyObjectByType<StoneSawSlot>();
  var stone=slot.Occupants.Count>0?slot.Occupants[0].GetComponent<RawStone>():UnityEngine.Object.FindObjectsByType<RawStone>().First(r=>r.GetComponent<TidyObject>().IsAvailableForPickup);
  var tidy=stone.GetComponent<TidyObject>();var original=stone.CutProfile;var profile=UnityEngine.Object.Instantiate(original);var snapshot=new TidyLevelSnapshot();TidyLevelSnapshotter.CaptureWorld(snapshot,interactor,player);float duration=Get<float>(saw,"cutDuration");
  try
  {
   Set(saw,"cutDuration",.2f);Set(stone,"cutProfile",profile);
   foreach(float chance in new[]{0f,100f})
   {
    Set(profile,"containsOrePercent",chance);
    var approach=slot.transform.position+new Vector3(0,-slot.transform.position.y+.03f,-1.45f);player.Teleport(approach,0,25);await Task.Delay(100);
    var direction=(slot.transform.position+Vector3.up*.08f-interactor.AimCamera.transform.position).normalized;player.Teleport(player.transform.position,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,-Mathf.Asin(direction.y)*Mathf.Rad2Deg);await Task.Delay(100);
    Check(slot.TryPlaceDirect(tidy,false),"Existing saw accepts raw stone at chance "+chance);Check(saw.TryStartCut(),"Existing timed cut starts at chance "+chance);
    var end=DateTime.UtcNow.AddSeconds(8);while(saw.IsCutting){if(DateTime.UtcNow>end)throw new Exception("Cut timeout");await Task.Delay(30);}await Task.Delay(100);
    var halves=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Where(h=>h.SourceStoneId==tidy.InstanceId).ToArray();Check(halves.Length==2 && halves.All(h=>h.GeneratedMesh!=null),"Runtime cut produces two real mesh halves at chance "+chance);
    Check(halves.All(h=>chance==0?h.ContainedOre==null:h.ContainedOre!=null),"Ore metadata correct at chance "+chance);
    TidyLevelSnapshotter.RestoreWorld(snapshot,interactor,player);await Task.Delay(100);
   }
   File.WriteAllText("Assets/PC/Documentation~/Market-cut-regression-results.json",JsonUtility.ToJson(new Results{passed=passed.ToArray()},true));return new {passed};
  }
  finally{saw.CancelCut();Set(saw,"cutDuration",duration);Set(stone,"cutProfile",original);TidyLevelSnapshotter.RestoreWorld(snapshot,interactor,player);UnityEngine.Object.Destroy(profile);}
 }
 [Serializable] public class Results{public string[] passed;}
}
