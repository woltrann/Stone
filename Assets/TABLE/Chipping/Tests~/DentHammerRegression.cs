using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using StoneGame.Chipping;
using StoneGame.Cutting;
using StoneGame.Economy;
using TidyUpFramework.Objects;
using TidyUpFramework.Player;
using TidyUpFramework.Interaction;
using TidyUpFramework.Save;
using TidyUpFramework.Settings;
public static class DentHammerRegression
{
 [Serializable]public class Fixture{public string sourceId,activeId,sourceState,leftId,leftState,rightId,rightState,profilePath;public Vector3 playerPosition;public float yaw,pitch;}
 [Serializable]public class Result{public string[] passed;public int pixels,accepted,playerBlockedPixels,physicalClicks;public float maxContactError,minHandleClearance;}
 sealed class MemorySave:ITidySaveStorage{readonly Dictionary<string,string> v=new Dictionary<string,string>();public bool TryRead(string k,out string s)=>v.TryGetValue(k,out s);public void Write(string k,string s)=>v[k]=s;public void Delete(string k)=>v.Remove(k);}
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static async Task Until(Func<bool> test,string label){var end=DateTime.UtcNow.AddSeconds(12);while(!test()){if(DateTime.UtcNow>end)throw new Exception(label);await Task.Delay(15);}}
 public static async Task<object> Main()
 {
  var checks=new List<string>();void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
  var boot=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!Application.isPlaying || !boot.StoragePath.Contains("Tests~"))throw new Exception("Isolated Play Mode required");Set(TidyFramework.Save,"storage",new MemorySave());
  var f=JsonUtility.FromJson<Fixture>(File.ReadAllText("Assets/TABLE/Chipping/Tests~/UserStoneFixture.json"));var table=UnityEngine.Object.FindAnyObjectByType<StoneChippingTable>();var slot=UnityEngine.Object.FindAnyObjectByType<StoneWorkSlot>();var player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();var interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();var raw=UnityEngine.Object.FindObjectsByType<RawStone>(FindObjectsInactive.Include).Single(s=>s.GetComponent<TidyObject>().InstanceId==f.sourceId);
  Set(raw,"cutProfile",AssetDatabase.LoadAssetAtPath<StoneCutProfile>(f.profilePath));raw.RestoreObjectState(f.sourceState);raw.GetComponent<TidyObject>().MarkConsumed();raw.gameObject.SetActive(false);
  var halves=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Where(h=>h.SourceStoneId==f.sourceId).ToArray();foreach(var h in halves)h.RestoreObjectState(h.GetComponent<TidyObject>().InstanceId==f.leftId?f.leftState:f.rightState);var stone=halves.Single(h=>h.GetComponent<TidyObject>().InstanceId==f.activeId);string progress=stone.CaptureObjectState();int initial=stone.CurrentHits;
  var oldPosition=player.transform.position;float oldYaw=player.Yaw,oldPitch=player.Pitch;float oldDuration=Get<float>(table,"hitDuration"),oldTransition=Get<float>(table,"transitionDuration");
  var keyboard=InputSystem.AddDevice<Keyboard>("UserStoneKeyboard");var mouse=InputSystem.AddDevice<Mouse>("UserStoneMouse");var bg=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(mouse);
  var mouseState=new MouseState();Action input=()=>{InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,mouseState);};InputSystem.onBeforeUpdate+=input;
  int pixels=0,accepted=0,blocked=0,clicks=0;float maxError=0,minClearance=float.PositiveInfinity;Vector3 hitNormal=Vector3.up;
  var hammer=Get<Transform>(table,"hammerVisual");var hammerMesh=hammer.GetComponent<MeshFilter>().sharedMesh;var hammerVertices=hammerMesh.vertices;var rubberIndices=hammerMesh.GetTriangles(2);
  Action<CutStoneHalf,Vector3> impacted=(h,p)=>{clicks++;maxError=Mathf.Max(maxError,Vector3.Distance(table.HammerImpactPoint.position,p));foreach(var i in rubberIndices)minClearance=Mathf.Min(minClearance,Vector3.Dot(hammer.TransformPoint(hammerVertices[i])-p,hitNormal));};table.HammerHit+=impacted;
  try
  {
   Check(stone.name=="Stein (2)_right" && stone.GeneratedMesh.vertexCount==4434 && initial==2,"Exact user stone geometry and 2/6 progress restored in test copy");Set(table,"hitDuration",.15f);Set(table,"transitionDuration",.12f);Check(slot.TryPlaceDirect(stone.GetComponent<TidyObject>(),false),"User stone seats on table");
   player.Teleport(f.playerPosition,0,0);await Task.Delay(80);var direction=(stone.GetComponent<Renderer>().bounds.center-Camera.main.transform.position).normalized;player.Teleport(f.playerPosition,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,-Mathf.Asin(direction.y)*Mathf.Rad2Deg);await Task.Delay(100);Check(table.TryEnter(),"Enter with user's original close player position");await Until(()=>table.Mode==ChippingMode.Working,"Work mode");
   var camera=Camera.main;var holder=Get<Transform>(table,"hammerHolder");mouseState.position=camera.WorldToScreenPoint(holder.position);await Task.Delay(90);mouseState.buttons=1;await Task.Delay(80);mouseState.buttons=0;await Task.Delay(80);Check(table.HammerHeld && !Cursor.visible,"Real mouse click selects hammer; cursor hidden");
   var bounds=stone.GetComponent<Renderer>().bounds;var low=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var high=new Vector2(float.NegativeInfinity,float.NegativeInfinity);for(int i=0;i<8;i++){var p=camera.WorldToScreenPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));low=Vector2.Min(low,new Vector2(p.x,p.y));high=Vector2.Max(high,new Vector2(p.x,p.y));}
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;var meshMethod=table.GetType().GetMethod("TryStoneMeshHit",flags);var worldMethod=table.GetType().GetMethod("TryPointerHit",flags);var samples=new List<Vector2>();
   for(int y=1;y<25;y++)for(int x=1;x<25;x++)
   {
    var p=new Vector2(Mathf.Lerp(low.x,high.x,x/25f),Mathf.Lerp(low.y,high.y,y/25f));var ray=camera.ScreenPointToRay(p);var args=new object[]{ray,null};if(!(bool)meshMethod.Invoke(table,args))continue;pixels++;var t=args[1].GetType();float distance=(float)t.GetField("Distance").GetValue(args[1]);if(Physics.RaycastAll(ray,5,~0,QueryTriggerInteraction.Collide).Any(h=>h.collider.transform.IsChildOf(player.transform) && h.distance<distance))blocked++;
    args=new object[]{ray,null};if((bool)worldMethod.Invoke(table,args) && (CutStoneHalf)t.GetField("Stone").GetValue(args[1])==stone){accepted++;samples.Add(p);}
   }
   Check(pixels>100 && accepted==pixels,"Every sampled visible pixel selects actual user stone");Check(blocked>0,"Original player-body occlusion reproduced and ignored without disabling collider");
   for(int i=0;i<10;i++)
   {
    stone.RestoreObjectState(progress);mouseState.position=samples[(int)((samples.Count-1)*i/9f)];await Task.Delay(70);var args=new object[]{camera.ScreenPointToRay(mouseState.position),null};worldMethod.Invoke(table,args);hitNormal=(Vector3)args[1].GetType().GetField("Normal").GetValue(args[1]);int before=clicks;
    mouseState.buttons=1;await Task.Delay(240);mouseState.buttons=0;await Task.Delay(60);Check(clicks==before+1 && stone.CurrentHits==initial+1,"Real click across user stone surface "+i);
   }
   Check(maxError<.0001f,"Metal contact point lands exactly on clicked mesh");Check(minClearance>.005f,"Rubber handle stays above contact plane; steel head makes the hit");
   stone.RestoreObjectState(progress);Set(table,"hitDuration",1f);mouseState.position=samples[samples.Count/2];await Task.Delay(100);
   var swingArgs=new object[]{camera.ScreenPointToRay(mouseState.position),null};worldMethod.Invoke(table,swingArgs);hitNormal=(Vector3)swingArgs[1].GetType().GetField("Normal").GetValue(swingArgs[1]);
   var grip=Get<Transform>(table,"hammerGripPoint");int lastFrame=-1,arcSamples=0;double started=-1;Vector3 firstGrip=default,firstHead=default,lastHead=default;float gripDrift=0;
   EditorApplication.CallbackFunction observe=()=>{if(lastFrame==Time.frameCount || !table.IsStriking)return;lastFrame=Time.frameCount;if(started<0)started=Time.unscaledTimeAsDouble-Time.unscaledDeltaTime;double phase=Time.unscaledTimeAsDouble-started;if(phase<.35 || phase>.5)return;if(arcSamples++==0){firstGrip=grip.position;firstHead=table.HammerImpactPoint.position;}gripDrift=Mathf.Max(gripDrift,Vector3.Distance(firstGrip,grip.position));lastHead=table.HammerImpactPoint.position;};
   EditorApplication.update+=observe;
   try{mouseState.buttons=1;await Task.Delay(1150);mouseState.buttons=0;await Task.Delay(80);}finally{EditorApplication.update-=observe;Set(table,"hitDuration",.15f);}
   Check(arcSamples>=3 && gripDrift<.0001f && Vector3.Distance(firstHead,lastHead)>.01f,"During downswing grip pivot stays fixed while metal head moves in an arc");
   var placedPosition=stone.transform.position;var placedRotation=stone.transform.rotation;stone.RestoreObjectState(progress);
   var capRotation=Quaternion.FromToRotation(stone.CutSurfaceNormal,Vector3.up);var capPoint=slot.transform.position+Vector3.up*.35f;stone.transform.SetPositionAndRotation(capPoint-capRotation*Vector3.Scale(stone.CutSurfaceCenter,stone.transform.lossyScale),capRotation);Physics.SyncTransforms();
   mouseState.position=camera.WorldToScreenPoint(capPoint);await Task.Delay(100);var capArgs=new object[]{camera.ScreenPointToRay(mouseState.position),null};Check((bool)worldMethod.Invoke(table,capArgs) && (bool)capArgs[1].GetType().GetField("CutFace").GetValue(capArgs[1]),"Temporarily exposed cut submesh is targetable too");hitNormal=(Vector3)capArgs[1].GetType().GetField("Normal").GetValue(capArgs[1]);int beforeCap=clicks;mouseState.buttons=1;await Task.Delay(240);mouseState.buttons=0;await Task.Delay(70);Check(clicks==beforeCap+1 && stone.CurrentHits==initial+1,"Actual mouse click can chip any visible submesh including cut surface");stone.transform.SetPositionAndRotation(placedPosition,placedRotation);stone.RestoreObjectState(progress);
   Check(Vector3.Distance(camera.transform.position,bounds.center)<1.6f,"Work camera moved closer to user stone");var viewport=camera.WorldToViewportPoint(holder.position);Check(viewport.x>0 && viewport.x<1 && viewport.y>0 && viewport.y<1,"Hammer holder remains accessible in closer camera frame");
   stone.RestoreObjectState(null);var originalMesh=stone.GeneratedMesh;var displayFilter=stone.GetComponent<MeshFilter>();int previousExposed=0;
   for(int n=0;n<6;n++)
   {
    mouseState.position=samples[(int)((samples.Count-1)*(new[]{.2f,.8f,.45f,.1f,.65f,.5f})[n])];await Task.Delay(90);var args=new object[]{camera.ScreenPointToRay(mouseState.position),null};Check((bool)worldMethod.Invoke(table,args),"Progressive demo target "+n);hitNormal=(Vector3)args[1].GetType().GetField("Normal").GetValue(args[1]);int before=clicks;
    mouseState.buttons=1;await Task.Delay(250);mouseState.buttons=0;await Task.Delay(120);Check(clicks==before+1 && stone.CurrentHits==n+1,"Actual hammer click advances localized reveal "+(n+1));
    if(n<5){var visual=displayFilter.sharedMesh;int exposed=visual.GetTriangles(originalMesh.subMeshCount).Length/3;Check(visual!=originalMesh && exposed>=previousExposed && exposed>0,"Actual hit preserves and adds visible patches "+(n+1));previousExposed=exposed;}
    else Check(displayFilter.sharedMesh!=originalMesh && stone.GetComponent<Renderer>().sharedMaterials.All(m=>m==stone.CutSurfaceMaterial),"Sixth real click exposes entire stone");
    if(n==0 || n==2 || n==5){ScreenCapture.CaptureScreenshot(Path.GetFullPath("Assets/TABLE/Chipping/Documentation~/Dent-hit-"+(n+1)+".png"),1);await Task.Delay(160);}
   }
  }
  finally
  {
   table.HammerHit-=impacted;InputSystem.onBeforeUpdate-=input;InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=bg;InputSystem.settings.editorInputBehaviorInPlayMode=editor;if(table.IsInUse){table.TryExit();await Task.Delay(400);}Set(table,"hitDuration",oldDuration);Set(table,"transitionDuration",oldTransition);raw.RestoreObjectState(null);raw.gameObject.SetActive(true);raw.GetComponent<TidyObject>().MarkLoose();player.Teleport(oldPosition,oldYaw,oldPitch);
  }
  var result=new Result{passed=checks.ToArray(),pixels=pixels,accepted=accepted,playerBlockedPixels=blocked,physicalClicks=clicks,maxContactError=maxError,minHandleClearance=minClearance};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/Dent-hammer-results.json",JsonUtility.ToJson(result,true));return result;
 }
}


