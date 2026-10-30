using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Computers;
using StoneGame.Computers.UI;
using TidyUpFramework.Player;
public static class MarketVisualChecks
{
 static async Task Capture(Camera camera,string file)
 {
  var target=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;var rt=new RenderTexture(1800,1100,24);var image=new Texture2D(1800,1100,TextureFormat.RGB24,false);
  try{camera.targetTexture=rt;camera.aspect=1800f/1100;await Task.Delay(300);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1800,1100),0,0);image.Apply();File.WriteAllBytes("Assets/PC/Documentation~/"+file,image.EncodeToPNG());}
  finally{camera.targetTexture=target;camera.aspect=aspect;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);}
 }
 public static async Task<object> Main()
 {
  var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();var view=UnityEngine.Object.FindAnyObjectByType<StoneMarketView>();var player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();var pos=player.transform.position;float yaw=player.Yaw,pitch=player.Pitch;
  try
  {
   var approach=pc.ScreenCenter+pc.ScreenNormal*1.5f;approach.y=.04f;player.Teleport(approach,90,20);await Task.Delay(100);var d=(pc.ScreenCenter-pc.PlayerCamera.transform.position).normalized;player.Teleport(player.transform.position,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,-Mathf.Asin(d.y)*Mathf.Rad2Deg);await Task.Delay(100);if(!pc.TryEnter())throw new Exception("PC entry failed");await Task.Delay(1000);
   view.OpenShop();view.SelectProduct(view.Catalog.Products[0]);view.QuantityInput.text="15";await Capture(pc.PlayerCamera,"StoneMarket-shop.png");view.OpenOrders();await Capture(pc.PlayerCamera,"StoneMarket-orders.png");return new {shop=true,orders=true};
  }
  finally{pc.AbortImmediately();player.Teleport(pos,yaw,pitch);}
 }
}
