using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class PCSceneProbe
{
 public static async Task<object> Main()
 {
  if(EditorApplication.isPlaying) throw new Exception("Probe must run in Edit mode.");
  var pc=GameObject.Find("PC");var screen=pc.transform.Find("Screen");var camera=Camera.main;
  Vector3 center=screen.GetComponent<Renderer>().bounds.center;
  Vector3 normal=screen.localToWorldMatrix.inverse.transpose.MultiplyVector(Vector3.back).normalized;
  var ray=new Ray(center+normal*1.5f,-normal);
  var colliders=pc.GetComponentsInChildren<Collider>();
  var hits=colliders.Select(c=>{bool hit=c.Raycast(ray,out var h,3);return new {c.name,kind=c.GetType().Name,hit,point=hit?h.point.ToString():null,distance=hit?h.distance:0};}).ToArray();
  var desk=pc.transform.Find("WarehouseDesk");var temp=new GameObject("PC desk mesh probe");temp.hideFlags=HideFlags.HideAndDontSave;
  temp.transform.SetPositionAndRotation(desk.position,desk.rotation);temp.transform.localScale=desk.lossyScale;
  var meshCollider=temp.AddComponent<MeshCollider>();meshCollider.sharedMesh=desk.GetComponent<MeshFilter>().sharedMesh;
  Physics.SyncTransforms();bool realHit=meshCollider.Raycast(ray,out var real,3);
  var oldPosition=camera.transform.localPosition;var oldRotation=camera.transform.localRotation;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
  var texture=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
  try
  {
   camera.transform.position=center+normal*.9f+Vector3.up*.08f;camera.transform.LookAt(center);
   camera.targetTexture=texture;await Task.Delay(100);camera.Render();RenderTexture.active=texture;
   image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();System.IO.File.WriteAllBytes("Assets/PC/Documentation~/PC-before.png",image.EncodeToPNG());
   return new {center=center.ToString(),normal=normal.ToString(),hits,deskMeshHit=realHit,deskMeshPoint=realHit?real.point.ToString():null,deskMeshDistance=realHit?real.distance:0};
  }
  finally
  {
   camera.transform.localPosition=oldPosition;camera.transform.localRotation=oldRotation;camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
   texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(temp);
  }
 }
}
