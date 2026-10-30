using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Computers;
using TidyUpFramework.Player;
public static class PCVisualChecks
{
 public static async Task<object> Main()
 {
  var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();var player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();var camera=pc.PlayerCamera;var position=player.transform.position;float yaw=player.Yaw,pitch=player.Pitch;
  var old=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;var rt=new RenderTexture(1440,900,24);var image=new Texture2D(1440,900,TextureFormat.RGB24,false);
  try
  {
   var pos=pc.ScreenCenter+pc.ScreenNormal*.95f;pos.y=.04f;player.Teleport(pos,90,30);await Task.Delay(150);var dir=(pc.ScreenCenter-camera.transform.position).normalized;player.Teleport(player.transform.position,Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg,-Mathf.Asin(dir.y)*Mathf.Rad2Deg);await Task.Delay(150);
   camera.targetTexture=rt;camera.aspect=1.6f;await Task.Delay(200);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes("Assets/PC/Documentation~/PC-hover.png",image.EncodeToPNG());return new {pc.IsHovered,camera=camera.transform.position.ToString()};
  }
  finally{camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;player.Teleport(position,yaw,pitch);rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);}
 }
}
