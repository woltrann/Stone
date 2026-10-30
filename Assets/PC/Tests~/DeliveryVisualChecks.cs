using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using StoneGame.Delivery;
using StoneGame.Economy;
public static class DeliveryVisualChecks
{
 public static async Task<object> Main()
 {
  var manager=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!bootstrap.StoragePath.Contains("Tests~"))throw new Exception("Sandbox required");var product=manager.Catalog.Products[0];
  var cameraObject=new GameObject("Delivery capture camera");var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;cameraObject.AddComponent<UniversalAdditionalCameraData>();camera.fieldOfView=65;
  var target=new RenderTexture(1600,1000,24);var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);var active=RenderTexture.active;
  GameObject spawned=null;Action<string,int,GameObject> handler=(id,index,go)=>spawned=go;manager.StoneSpawned+=handler;
  try
  {
   if(!bootstrap.Service.TryPlaceOrder(manager.Catalog,product.id,1,product.unitPrice,Guid.NewGuid().ToString("N"),out var order,out var error))throw new Exception(error);
   var end=DateTime.UtcNow.AddSeconds(16);while(spawned==null){if(DateTime.UtcNow>end)throw new Exception(manager.LastError??"Delivery timeout");await Task.Delay(10);}
   var focus=manager.SpawnPoint.position+new Vector3(0,-1,2);camera.transform.position=focus+new Vector3(8,6,-8);camera.transform.LookAt(focus);
   camera.targetTexture=target;camera.aspect=1.6f;await Task.Delay(80);camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes("Assets/PC/Documentation~/StoneDelivery-world.png",image.EncodeToPNG());return new {order=order.OrderId,position=spawned.transform.position.ToString(),car=manager.Car.position.ToString()};
  }
  finally{manager.StoneSpawned-=handler;RenderTexture.active=active;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(cameraObject);}
 }
}
