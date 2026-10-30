using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using StoneGame.Computers;
using TidyUpFramework.Player;
using TidyUpFramework.Feedback;
public static class PCCameraEffectChecks
{
 public static async Task<object> Main()
 {
  var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();var player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();var camera=pc.PlayerCamera;var punch=camera.GetComponent<TidyCameraPunch>();
  var oldPlayer=player.transform.position;float oldYaw=player.Yaw,oldPitch=player.Pitch;var samples=new List<string>();
  try
  {
   var pos=pc.ScreenCenter+pc.ScreenNormal*1.5f;pos.y=.04f;player.Teleport(pos,90,22);await Task.Delay(200);
   var dir=(pc.ScreenCenter-camera.transform.position).normalized;player.Teleport(player.transform.position,Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg,-Mathf.Asin(dir.y)*Mathf.Rad2Deg);await Task.Delay(100);
   var local=camera.transform.localPosition;float fov=camera.fieldOfView;punch.Punch(.8f);await Task.Delay(25);
   if(!pc.TryEnter())throw new Exception("Entry failed");await Task.Delay(900);pc.TryExit();await Task.Delay(800);
   float maxPositionError=0,maxFovError=0;for(int i=0;i<12;i++){maxPositionError=Mathf.Max(maxPositionError,Vector3.Distance(local,camera.transform.localPosition));maxFovError=Mathf.Max(maxFovError,Mathf.Abs(fov-camera.fieldOfView));samples.Add(camera.transform.localPosition+" fov="+camera.fieldOfView);await Task.Delay(20);}
   var result=new Result{pass=maxPositionError<.0001f && maxFovError<.001f,maxPositionError=maxPositionError,maxFovError=maxFovError,samples=samples.ToArray()};File.WriteAllText("Assets/PC/Documentation~/PC-camera-effect-results.json",JsonUtility.ToJson(result,true));return result;
  }
  finally{pc.AbortImmediately();player.Teleport(oldPlayer,oldYaw,oldPitch);}
 }
 [Serializable] public class Result{public bool pass;public float maxPositionError,maxFovError;public string[] samples;}
}
