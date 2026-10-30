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
using StoneGame.Cutting;
using StoneGame.Chipping;
using StoneGame.Economy;
using StoneGame.Delivery;
using StoneGame.Computers;
using TidyUpFramework.Core;
using TidyUpFramework.Input;
using TidyUpFramework.Interaction;
using TidyUpFramework.Objects;
using TidyUpFramework.Player;
using TidyUpFramework.Save;
using TidyUpFramework.Settings;
using TidyUpFramework.UI;
public static class ChippingPlayChecks
{
 static bool keepSave;
 public static async Task<object> PrepareRestart(){keepSave=true;try{return await Main();}finally{keepSave=false;}}
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static async Task Until(Func<bool> predicate,string name){var end=DateTime.UtcNow.AddSeconds(12);while(!predicate()){if(DateTime.UtcNow>end)throw new Exception(name);await Task.Delay(15);}}
 sealed class MemorySave:ITidySaveStorage{readonly Dictionary<string,string> data=new Dictionary<string,string>();public bool TryRead(string k,out string v)=>data.TryGetValue(k,out v);public void Write(string k,string v)=>data[k]=v;public void Delete(string k)=>data.Remove(k);}
 static TidyFirstPersonController player;static TidyInteractor interactor;
 static async Task Aim(Vector3 position,Vector3 target){var d=(target-(position+Vector3.up*1.6f)).normalized;player.Teleport(position,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,-Mathf.Asin(d.y)*Mathf.Rad2Deg);await Task.Delay(120);d=(target-interactor.AimCamera.transform.position).normalized;player.Teleport(position,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,-Mathf.Asin(d.y)*Mathf.Rad2Deg);await Task.Delay(100);Physics.SyncTransforms();}
 static Ray StoneRay(CutStoneHalf stone){var bounds=stone.GetComponent<Renderer>().bounds;var camera=Camera.main.transform.position;return new Ray(camera,(bounds.center-camera).normalized);}
 public static async Task<object> Main()
 {
  var passed=new List<string>();var failed=new List<string>();void Check(bool ok,string label){if(!ok)throw new Exception(label);passed.Add(label);}
  var boot=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(!Application.isPlaying || !boot.StoragePath.Contains("Tests~"))throw new Exception("Sandbox required");Set(TidyFramework.Save,"storage",new MemorySave());
  var service=boot.Service;var delivery=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var table=UnityEngine.Object.FindAnyObjectByType<StoneChippingTable>();var slot=UnityEngine.Object.FindAnyObjectByType<StoneWorkSlot>();var saw=UnityEngine.Object.FindAnyObjectByType<StoneSawMachine>();var sawSlot=UnityEngine.Object.FindAnyObjectByType<StoneSawSlot>();var input=UnityEngine.Object.FindAnyObjectByType<TidyInputReader>();
  player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();var start=player.transform.position;float yaw=player.Yaw,pitch=player.Pitch;
  var product=delivery.Catalog.Products[0];var profile=UnityEngine.Object.Instantiate(product.prefab.GetComponent<RawStone>().CutProfile);var ruby=AssetDatabase.LoadAssetAtPath<OreDefinition>("Assets/STONE/Data/Ores/Yakut.asset");Set(profile,"containsOrePercent",100f);Set(profile,"oreChances",new List<OreChanceEntry>{new OreChanceEntry{ore=ruby,chancePercent=100}});
  var originalCut=Get<float>(saw,"cutDuration");Set(delivery,"travelDuration",.1f);Set(delivery,"waitDuration",.05f);Set(delivery,"returnDuration",.1f);Set(table,"transitionDuration",.12f);Set(table,"hitDuration",.1f);
  var keyboard=InputSystem.AddDevice<Keyboard>("ChippingKeyboard");var mouse=InputSystem.AddDevice<Mouse>("ChippingMouse");
  var oldBackground=InputSystem.settings.backgroundBehavior;var oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(mouse);
  var keys=new KeyboardState();var mouseState=new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.5f)};
  int calls=0; Action before=()=>{calls++;InputSystem.QueueStateEvent(keyboard,keys);InputSystem.QueueStateEvent(mouse,mouseState);};InputSystem.onBeforeUpdate+=before;
  GameObject blocker=null;
  int contactHits=0;float maxContactError=0;
  Action<CutStoneHalf,Vector3> impact=(stone,point)=>{contactHits++;maxContactError=Mathf.Max(maxContactError,Vector3.Distance(table.HammerImpactPoint.position,point));};table.HammerHit+=impact;
  try
  {
   Check(service.TryPlaceOrder(delivery.Catalog,product.id,1,product.unitPrice,Guid.NewGuid().ToString("N"),out var order,out _),"Existing shop/order API accepts test stone");
   await Until(()=>service.FindOrder(order.OrderId).Status==StoneGame.Orders.StoneOrderStatus.Delivered && delivery.Phase==StoneDeliveryPhase.Idle,"Delivery completed");
   var raw=UnityEngine.Object.FindObjectsByType<DeliveredStoneInstance>().Single(s=>s.OrderId==order.OrderId).GetComponent<RawStone>();Set(raw,"cutProfile",profile);var tidy=raw.GetComponent<TidyObject>();
   Check(!slot.IsCompatible(tidy),"Table rejects uncut RawStone");
   await Aim(sawSlot.transform.position+new Vector3(0,-sawSlot.transform.position.y+.03f,-1.45f),sawSlot.transform.position+Vector3.up*.08f);Set(saw,"cutDuration",.15f);
   Check(sawSlot.TryPlaceDirect(tidy,false) && saw.TryStartCut(),"Existing timed runtime mesh saw cuts ore-bearing stone");await Until(()=>!saw.IsCutting,"Cut complete");
   var halves=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Where(h=>h.SourceStoneId==tidy.InstanceId).ToArray();Check(halves.Length==2 && halves.All(h=>h.ContainsOre && h.CurrentHits==0 && h.RequiredHits==6),"Two independent ore halves start at zero of six");var a=halves[0];var b=halves[1];string aId=a.GetComponent<TidyObject>().InstanceId;
   interactor.ForcePickUp(a.GetComponent<TidyObject>());await Aim(new Vector3(-1.13f,.03f,9.8f),slot.transform.position+Vector3.up*.1f);await Task.Delay(200);
   Check(interactor.CurrentCandidate.Target==slot && interactor.CurrentCandidate.IsValid,"Existing placement preview finds table slot: target="+(interactor.CurrentCandidate.Target!=null?interactor.CurrentCandidate.Target.name:"none")+" reason="+interactor.CurrentCandidate.Rejection+" compatible="+slot.IsCompatible(a.GetComponent<TidyObject>())+" held="+(interactor.HeldObject!=null));
   input.TriggerVirtual(TidyInputAction.Interact);await Until(()=>a.GetComponent<TidyObject>().State==TidyObjectState.Placed,"Input places A");
   Check(interactor.HeldObject==null && slot.Stone==a && a.GetComponent<Rigidbody>().isKinematic,"Existing placement input seats stone without dynamic jitter");
   await Task.Delay(350);
   Check(Vector3.Dot(a.transform.worldToLocalMatrix.transpose.MultiplyVector(a.CutSurfaceNormal).normalized,slot.transform.up)<-.999f,"Cut face points down toward tabletop");
   Check(Vector3.Distance(a.transform.TransformPoint(a.CutSurfaceCenter),slot.transform.position)<.001f,"Actual cut-face center seats directly on table work point");
   Check(a.GeneratedMesh.vertices.Min(v=>Vector3.Dot(a.transform.TransformPoint(v)-slot.transform.position,slot.transform.up))>-.001f,"Stone mesh stays above tabletop instead of sinking into it");
   Check(!slot.TryPlaceDirect(b.GetComponent<TidyObject>(),false),"Occupied table rejects a second stone");
   Check((bool)table.GetType().GetMethod("CanEnter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(table,null),"Work entry aimed before E");keys=new KeyboardState(Key.E);await Task.Delay(150);keys=new KeyboardState();Check(table.IsInUse,"E detected: calls="+calls+" enabled="+keyboard.enabled+" E="+keyboard.eKey.isPressed+" current="+Keyboard.current.name+" held="+interactor.CarriedCount+" stone="+(slot.Stone!=null));await Until(()=>table.Mode==ChippingMode.Working,"Camera enters");Check(table.Mode==ChippingMode.Working,"Physical E input enters aimed table work mode");
   Check(!player.enabled && !interactor.enabled && TidyControlLock.IsLocked(TidyControlChannel.Movement|TidyControlChannel.Look|TidyControlChannel.Interaction),"Movement, look and world interaction locked");
   Check(Cursor.lockState==CursorLockMode.None && Cursor.visible,"Cursor free and visible");
   var point=Get<Transform>(table,"cameraPoint");Check(Vector3.Distance(Camera.main.transform.position,point.position)<.001f,"Camera reaches configured table/panel viewpoint");
   var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();Check(!pc.TryEnter(),"Computer cannot enter during table work mode");
   var holder=Get<Transform>(table,"hammerHolder");var holderRay=new Ray(Camera.main.transform.position,(holder.position-Camera.main.transform.position).normalized);
   Check(table.TryPointerAction(holderRay) && table.HammerHeld,"Ray clicking existing wall hammer takes it: first="+(Physics.Raycast(holderRay,out var hh,5,~0,QueryTriggerInteraction.Collide)?hh.collider.name:"none")+" UI="+(UnityEngine.EventSystems.EventSystem.current!=null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()));
   Check(!Cursor.visible && Cursor.lockState==CursorLockMode.None,"Selected hammer hides cursor without locking mouse motion");
   var capNormal=a.transform.worldToLocalMatrix.transpose.MultiplyVector(a.CutSurfaceNormal).normalized;var capRay=new Ray(a.transform.TransformPoint(a.CutSurfaceCenter)+capNormal*.02f,-capNormal);var capArgs=new object[]{capRay,null};
   Check((bool)table.GetType().GetMethod("TryStoneMeshHit",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(table,capArgs) && (bool)capArgs[1].GetType().GetField("CutFace").GetValue(capArgs[1]),"Exact picking recognizes the underside as cut-face geometry");
   Check(a.CurrentHits==0,"Coverage probe does not change stone progress");
   var probeObject=new GameObject("Temporary exact-mesh test collider");probeObject.transform.SetParent(a.transform,false);var probe=probeObject.AddComponent<MeshCollider>();probe.sharedMesh=a.GeneratedMesh;
   var bounds=a.GetComponent<Renderer>().bounds;var low=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var high=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
   for(int k=0;k<8;k++){var w=bounds.center+Vector3.Scale(bounds.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1));var pixel=Camera.main.WorldToScreenPoint(w);low=Vector2.Min(low,new Vector2(pixel.x,pixel.y));high=Vector2.Max(high,new Vector2(pixel.x,pixel.y));}
   var picking=table.GetType().GetMethod("TryPointerHit",BindingFlags.Instance|BindingFlags.NonPublic);int sampled=0;float maxPickError=0;
   for(int y=1;y<8;y++)for(int x=1;x<8;x++)
   {
    var ray=Camera.main.ScreenPointToRay(new Vector2(Mathf.Lerp(low.x,high.x,x/8f),Mathf.Lerp(low.y,high.y,y/8f)));
    if(!probe.Raycast(ray,out var truth,5))continue;
    var args=new object[]{ray,null};Check((bool)picking.Invoke(table,args),"Visible mesh pixel has work hit "+sampled);var hit=args[1];var hitType=hit.GetType();
    Check((CutStoneHalf)hitType.GetField("Stone").GetValue(hit)==a,"Visible pixel selects any visible submesh of active stone "+sampled);
    maxPickError=Mathf.Max(maxPickError,Vector3.Distance((Vector3)hitType.GetField("Point").GetValue(hit),truth.point));sampled++;
   }
   probeObject.SetActive(false);UnityEngine.Object.Destroy(probeObject);Check(sampled>=5 && maxPickError<.001f,"Rendered stone silhouette agrees with independent exact MeshCollider raycasts");
   Check(!table.TryPointerAction(new Ray(Camera.main.transform.position,Vector3.up)) && a.CurrentHits==0,"Empty-space click produces no hit");
   Check(!table.TryPointerAction(new Ray(Camera.main.transform.position,(new Vector3(-.1f,.88f,11.1f)-Camera.main.transform.position).normalized)),"Table surface click produces no hit");
   Check(!table.TryPointerAction(StoneRay(b)) && b.CurrentHits==0,"Other stone cannot receive work-table hit");
   mouseState.position=Camera.main.WorldToScreenPoint(a.GetComponent<Renderer>().bounds.center);await Task.Delay(120);
   var previousHammer=Get<Transform>(table,"hammerVisual").position;mouseState.position+=new Vector2(40,20);await Task.Delay(100);Check(Vector3.Distance(previousHammer,Get<Transform>(table,"hammerVisual").position)>.01f,"Hammer visual follows actual mouse motion");
   mouseState.position=Camera.main.WorldToScreenPoint(a.GetComponent<Renderer>().bounds.center);await Task.Delay(100);ScreenCapture.CaptureScreenshot(Path.GetFullPath("Assets/TABLE/Chipping/Documentation~/Exterior-click-guide.png"),3);await Task.Delay(200);mouseState.buttons=1;await Task.Delay(350);Check(a.CurrentHits==1 && !table.IsStriking,"Holding actual left mouse produces exactly one strike");mouseState.buttons=0;await Task.Delay(70);
   var screenContact=Camera.main.WorldToScreenPoint(table.HammerImpactPoint.position);
   Check(Vector2.Distance(new Vector2(screenContact.x,screenContact.y),mouseState.position)<.5f,"Metal head stays on the real mouse position over the stone");
   Check(contactHits==1 && maxContactError<.0001f,"Actual mouse click lands the metal contact point on the stone surface");
   for(int i=1;i<3;i++){Check(table.TryPointerAction(StoneRay(a)),"Active stone accepts strike "+(i+1));Check(!table.TryPointerAction(StoneRay(a)),"No second strike during animation "+(i+1));await Until(()=>!table.IsStriking,"Strike ends");}
   blocker=new GameObject("Test UI blocker",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.GraphicRaycaster));blocker.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;blocker.GetComponent<Canvas>().sortingOrder=999;
   var button=new GameObject("Button",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));button.transform.SetParent(blocker.transform,false);var rr=button.GetComponent<RectTransform>();rr.anchorMin=Vector2.zero;rr.anchorMax=Vector2.one;rr.offsetMin=rr.offsetMax=Vector2.zero;await Task.Delay(120);
   Check(UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() && !table.TryPointerAction(StoneRay(a)) && a.CurrentHits==3,"UI button blocks stone hits");UnityEngine.Object.Destroy(blocker);blocker=null;await Task.Delay(100);
   Check(a.CurrentHits==3 && b.CurrentHits==0,"Three successful clicks update only A to 3/6");
   keys=new KeyboardState(Key.Escape);await Task.Delay(80);keys=new KeyboardState();await Task.Delay(80);Check(!table.HammerHeld && table.Mode==ChippingMode.Working,"Actual first ESC returns hammer and keeps table mode");
   Check(Cursor.visible,"Returning hammer restores visible cursor");
   keys=new KeyboardState(Key.Escape);await Task.Delay(80);keys=new KeyboardState();await Until(()=>table.Mode==ChippingMode.Idle,"Second ESC exits");Check(player.enabled && interactor.enabled && !TidyControlLock.IsLocked(TidyControlChannel.Interaction),"Actual second ESC restores gameplay controls");
   Check(!UnityEngine.Object.FindAnyObjectByType<TidyPauseMenu>().IsPaused,"Table ESC does not leak to pause menu");
   interactor.ForcePickUp(a.GetComponent<TidyObject>());await Task.Delay(100);interactor.ForceDrop();await Task.Delay(400);Check(a.CurrentHits==3,"Pickup and drop preserve stone progress");
   Check(slot.TryPlaceDirect(b.GetComponent<TidyObject>(),false) && b.CurrentHits==0,"B seats with its own zero progress");
   var level=UnityEngine.Object.FindAnyObjectByType<TidyUpFramework.Levels.TidyLevelManager>();Set(level,"hasUnsavedChanges",false);
   Check(b.TryChip() && b.CurrentHits==1 && a.CurrentHits==3,"Stone progress remains independent");Check(Get<bool>(level,"hasUnsavedChanges"),"Chipping marks framework autosave dirty even without placement change");interactor.ForcePickUp(b.GetComponent<TidyObject>());await Task.Delay(50);interactor.ForceDrop();await Task.Delay(400);
   Check(slot.TryPlaceDirect(a.GetComponent<TidyObject>(),false) && a.CurrentHits==3,"Returning A resumes 3/6");
   Check(delivery.CheckpointNow(),"Partial chipping saves to existing delivery ledger");
   var legacyRecords=service.GetDeliveryItems(delivery.gameObject.scene.name);var legacyPiece=legacyRecords.SelectMany(r=>r.pieces).Single(p=>p.Id==aId);legacyPiece.Rotation=Quaternion.FromToRotation(a.CutSurfaceNormal,Vector3.up);legacyPiece.Position=slot.transform.position;
   Check(service.TryCheckpointDeliveryItems(legacyRecords,out _),"Simulated older face-up table pose saved in sandbox");
   var snap=new TidyLevelSnapshot();TidyLevelSnapshotter.RestoreWorld(snap,null,null);await Task.Delay(150);
   a=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Single(h=>h.GetComponent<TidyObject>().InstanceId==aId);
   Check(a.CurrentHits==3 && a.RequiredHits==6 && slot.Stone==a,"Runtime source reconstruction restores new half state and table placement");
   Check(Vector3.Dot(a.transform.worldToLocalMatrix.transpose.MultiplyVector(a.CutSurfaceNormal).normalized,slot.transform.up)<-.999f && Vector3.Distance(a.transform.TransformPoint(a.CutSurfaceCenter),slot.transform.position)<.001f,"Older saved face-up pose is corrected to cut-face-down on restore");
   await Aim(new Vector3(-1.13f,.03f,9.8f),slot.transform.position+Vector3.up*.12f);Check(table.TryEnter(),"Restored stone enters work mode");await Until(()=>table.Mode==ChippingMode.Working,"Reenter");Check(table.TakeHammer(),"Hammer can be retaken");
   for(int i=0;i<3;i++){Check(table.TryPointerAction(StoneRay(a)),"Remaining strike "+(i+4));await Until(()=>!table.IsStriking,"Strike ends");}
   Check(a.IsChippingCompleted && a.CurrentHits==6 && a.GetComponent<Renderer>().sharedMaterials.All(m=>m==a.CutSurfaceMaterial),"Sixth hit changes every stone submesh to its captured cut-surface material");
   Check(contactHits==6 && maxContactError<.0001f,"All six strokes make exact metal-head contact, once per stroke");
   ScreenCapture.CaptureScreenshot(Path.GetFullPath("Assets/TABLE/Chipping/Documentation~/Chipping-completed.png"));await Task.Delay(200);
   Check(!table.TryPointerAction(StoneRay(a)) && !a.TryChip() && a.CurrentHits==6,"Completed stone rejects further hits");
   Check(table.TryPointerAction(holderRay) && !table.HammerHeld,"Clicking holder returns hammer");table.HandleEscape();await Until(()=>table.Mode==ChippingMode.Idle,"Exit completed");
   Check(delivery.CheckpointNow(),"Completed ore checkpoint succeeds");TidyLevelSnapshotter.RestoreWorld(new TidyLevelSnapshot(),null,null);await Task.Delay(150);
   a=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Single(h=>h.GetComponent<TidyObject>().InstanceId==aId);
   Check(a.IsChippingCompleted && a.CurrentHits==6 && a.GetComponent<Renderer>().sharedMaterials.All(m=>m==a.CutSurfaceMaterial),"Save/restore preserves completion and ore appearance");
   interactor.ForcePickUp(a.GetComponent<TidyObject>());Check(interactor.HeldObject==a.GetComponent<TidyObject>(),"Completed ore still uses original pickup");interactor.ForceDrop();
   if(!keepSave){await Task.Delay(300);b=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Single(h=>h.SourceStoneId==tidy.InstanceId && h.GetComponent<TidyObject>().InstanceId!=aId);b.Initialize(null,tidy.InstanceId,profile);
   Check(slot.TryPlaceDirect(b.GetComponent<TidyObject>(),false) && b.TryChip(),"Empty cut half is placed and can be chipped");await Aim(new Vector3(-1.13f,.03f,9.8f),slot.transform.position+Vector3.up*.12f);Check(table.TryEnter(),"Empty stone can display work information");await Until(()=>table.Mode==ChippingMode.Working,"Empty work mode");await Task.Delay(30);Check(table.LastMessage.Contains("Yontma İşlemi") && !table.LastMessage.Contains("cevher bulunmuyor"),"Empty stone shows normal chipping progress");table.HandleEscape();await Until(()=>table.Mode==ChippingMode.Idle,"Exit empty mode");}
  }
  catch(Exception ex){failed.Add(ex.ToString());}
  finally
  {
   table.HammerHit-=impact;
   InputSystem.onBeforeUpdate-=before;InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;if(blocker!=null)UnityEngine.Object.Destroy(blocker);
   if(table.IsInUse){table.TryExit();await Task.Delay(300);}saw.CancelCut();Set(saw,"cutDuration",originalCut);if(keepSave){delivery.CheckpointNow();File.WriteAllText("Assets/TABLE/Chipping/Documentation~/Chipping-restart-path.txt",boot.StoragePath);}else{service.StartNewGame(100000,out _);UnityEngine.Object.Destroy(profile);}player.Teleport(start,yaw,pitch);
  }
  var result=new Results{passed=passed.ToArray(),failed=failed.ToArray()};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/"+(keepSave?"Chipping-restart-prepare.json":"Chipping-play-results.json"),JsonUtility.ToJson(result,true));return result;
 }
 [Serializable]public class Results{public string[] passed,failed;}
}









