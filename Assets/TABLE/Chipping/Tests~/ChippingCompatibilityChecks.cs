using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Computers;
using TidyUpFramework.Core;
using TidyUpFramework.Interaction;
using TidyUpFramework.Player;
using TidyUpFramework.UI;
public static class ChippingCompatibilityChecks
{
 public static async Task<object> Main()
 {
  var checks=new List<string>();void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
  var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();var player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();var interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();var pos=player.transform.position;float yaw=player.Yaw,pitch=player.Pitch;
  async Task Until(Func<bool> test){var end=DateTime.UtcNow.AddSeconds(5);while(!test()){if(DateTime.UtcNow>end)throw new Exception("PC transition timeout");await Task.Delay(20);}}
  try
  {
   var approach=pc.ScreenCenter+pc.ScreenNormal*1.5f;approach.y=.04f;player.Teleport(approach,0,0);await Task.Delay(100);var d=(pc.ScreenCenter-pc.PlayerCamera.transform.position).normalized;player.Teleport(approach,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,-Mathf.Asin(d.y)*Mathf.Rad2Deg);await Task.Delay(150);
   Check(pc.CanTarget(out _) && pc.TryEnter(),"Original computer interaction still enters");await Until(()=>pc.State==ComputerUseState.Using);
   Check(!player.enabled && !interactor.enabled && Cursor.visible,"Computer camera/input ownership still works");
   Check(pc.Desktop.IsInteractive,"Computer desktop remains interactive");
   Check(pc.TryExit(),"Computer exits normally");await Until(()=>pc.State==ComputerUseState.Idle);
   Check(player.enabled && interactor.enabled && !TidyControlLock.IsLocked(TidyControlChannel.Interaction),"Computer exit restores normal controls");
   var menu=UnityEngine.Object.FindAnyObjectByType<TidyPauseMenu>();menu.Pause();Check(menu.IsPaused && Time.timeScale==0,"Pause menu still opens normally outside work mode");menu.Resume();Check(!menu.IsPaused && Time.timeScale>0,"Resume restores gameplay");
  }
  finally{pc.AbortImmediately();player.Teleport(pos,yaw,pitch);}
  var result=new Result{passed=checks.ToArray()};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/Chipping-compatibility-results.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable]public class Result{public string[] passed;}
}

