using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using StoneGame.Computers;
using TidyUpFramework.Core;
using TidyUpFramework.Player;
using TidyUpFramework.Interaction;
using TidyUpFramework.Objects;
using TidyUpFramework.UI;

public static class PCIntegrationChecks
{
 static readonly List<string> passed=new List<string>(),failed=new List<string>();
 static void Check(bool ok,string label) {if(ok)passed.Add(label);else failed.Add(label);}
 static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
 static async Task Wait(int ms=150) {await Task.Delay(ms);}
 static InteractiveComputer pc;static TidyFirstPersonController player;static Camera camera;static Keyboard keyboard;static Mouse mouse;
 static KeyboardState keyState;static MouseState mouseState;
 static void InputTick() {if(InputState.currentUpdateType!=InputUpdateType.Dynamic)return;InputSystem.QueueStateEvent(keyboard,keyState);InputSystem.QueueStateEvent(mouse,mouseState);}
 static async Task KeyTap(Key key,int ms=100) {keyState=new KeyboardState(key);await Wait(ms);keyState=new KeyboardState();await Wait(100);}
 static async Task Approach(float distance=1.5f,float side=0)
 {
  var position=pc.ScreenCenter+pc.ScreenNormal*distance+pc.ScreenRenderer.transform.right*side;position.y=.04f;
  var direction=(pc.ScreenCenter-(position+Vector3.up*1.63f)).normalized;
  player.Teleport(position,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,-Mathf.Asin(direction.y)*Mathf.Rad2Deg);
  await Wait(250);Physics.SyncTransforms();
 }
 static async Task Click(UnityEngine.UI.Button button)
 {
  var p=camera.WorldToScreenPoint(((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
  mouseState=new MouseState{position=new Vector2(p.x,p.y)};await Wait(220);
  var pointer=new PointerEventData(EventSystem.current){position=mouseState.position};var results=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,results);
  Check(results.Any(h=>h.gameObject==button.gameObject || h.gameObject.transform.IsChildOf(button.transform)),button.name+" world canvas raycast");
  Check(button.transform.localScale.x>1,button.name+" pointer hover feedback");
  mouseState=mouseState.WithButton(MouseButton.Left,true);await Wait(100);Check(button.transform.localScale.x<1,button.name+" pointer pressed feedback");mouseState=mouseState.WithButton(MouseButton.Left,false);await Wait(220);
 }
 static async Task Capture(string path)
 {
  var old=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;var rt=new RenderTexture(1440,900,24);var image=new Texture2D(1440,900,TextureFormat.RGB24,false);
  try {camera.targetTexture=rt;camera.aspect=1.6f;await Wait(200);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
  finally{camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);}
 }
 public static async Task<object> Main()
 {
  if(!Application.isPlaying)throw new Exception("Play mode required.");
  pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();camera=pc.PlayerCamera;var interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();
  var oldPlayer=player.transform.position;float oldYaw=player.Yaw,oldPitch=player.Pitch,oldAspect=camera.aspect;var oldBackground=InputSystem.settings.backgroundBehavior;var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
  GameObject blocker=null;int startFrame=Time.frameCount;int buys=0,orders=0;Action buy=()=>buys++,order=()=>orders++;pc.BuyRequested+=buy;pc.OrdersRequested+=order;
  keyboard=InputSystem.AddDevice<Keyboard>("PCIntegrationKeyboard");mouse=InputSystem.AddDevice<Mouse>("PCIntegrationMouse");keyState=new KeyboardState();mouseState=new MouseState{position=new Vector2(15,15)};
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(mouse);InputSystem.onBeforeUpdate+=InputTick;
  try
  {
   await Wait(200);Check(Time.frameCount>startFrame+2,"Play loop advances");Check(!UnityEngine.Object.FindAnyObjectByType<TidyUpFramework.Levels.TidyLevelManager>().SavesProgress,"Real saves disabled before Play");
   Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length==1,"Existing single EventSystem retained");
   Check(pc.Desktop.Canvas.renderMode==RenderMode.WorldSpace && pc.Desktop.transform.IsChildOf(pc.ScreenRenderer.transform),"UI is world space under Screen");
   await Approach(3.5f);Check(!pc.IsHovered,"Out of range hides prompt/outline");await Approach();
   Check(pc.IsHovered,"Front center ray detects PC");Check(pc.CanTarget(out var hit),"Existing Tidy raycast accepts Screen/monitor collider");
   var prompt=UnityEngine.Object.FindAnyObjectByType<TidyInteractionPromptView>();Check(Field<TMP_Text>(prompt,"promptLabel").text=="İnternete erişmek için [E] tuşuna bas","Exact Turkish interaction prompt");
   var highlighter=UnityEngine.Object.FindAnyObjectByType<TidyOutlineHighlighter>();var entries=Field<IDictionary>(highlighter,"entries");
   bool screenOnly=false;foreach(DictionaryEntry entry in entries)if(ReferenceEquals(entry.Key,pc)){var renderers=(IReadOnlyList<Renderer>)entry.Value.GetType().GetField("Renderers").GetValue(entry.Value);screenOnly=renderers.Count==1 && renderers[0].transform.IsChildOf(pc.transform.Find("Monitor 2"));}
   Check(screenOnly,"Existing outline targets Monitor 2 only");
   var emission=new MaterialPropertyBlock();pc.ScreenRenderer.GetPropertyBlock(emission);Check(emission.GetColor("_EmissionColor").maxColorComponent>0,"Screen emission property block set");
   blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="PC test occluder";blocker.transform.position=Vector3.Lerp(camera.transform.position,pc.ScreenCenter,.55f);blocker.transform.localScale=Vector3.one*.35f;Physics.SyncTransforms();await Wait();Check(!pc.IsHovered,"Solid object occludes PC targeting");UnityEngine.Object.Destroy(blocker);await Wait();
   player.Teleport(player.transform.position,player.Yaw+60,player.Pitch);await Wait();Check(!pc.IsHovered,"Looking away clears PC hover");await Approach();
   var local=camera.transform.localPosition;var rotation=camera.transform.localRotation;float fov=camera.fieldOfView,yaw=player.Yaw,pitch=player.Pitch;var world=camera.transform.position;
   keyState=new KeyboardState(Key.E);await Wait(100);Check(pc.State==ComputerUseState.Entering,"E begins smooth transition");Check(Vector3.Distance(camera.transform.position,world)>.001f,"Camera advances during entry");keyState=new KeyboardState();await Wait(80);await KeyTap(Key.E);Check(pc.IsInUse,"Repeated E keeps one session");await Wait(900);
   Check(pc.State==ComputerUseState.Using,"Entry completes");if(pc.State!=ComputerUseState.Using)throw new Exception("PC did not enter; dependent UI tests skipped.");Check(!player.enabled && !interactor.enabled && TidyControlLock.IsLocked(TidyControlChannel.All),"Movement/look/interaction locked during PC use");Check(Cursor.visible && Cursor.lockState==CursorLockMode.None,"PC cursor visible and unlocked");Check(pc.Desktop.IsInteractive,"Desktop interactive after entry");Check(interactor.CarriedCount==0,"Entry E does not pick up world object");
   var stay=player.transform.position;var camStay=camera.transform.rotation;keyState=new KeyboardState(Key.W,Key.Space,Key.G);mouseState.delta=new Vector2(90,50);await Wait(200);keyState=new KeyboardState();mouseState.delta=Vector2.zero;await Wait(100);Check(Vector3.Distance(stay,player.transform.position)<.0001f && Quaternion.Angle(camStay,camera.transform.rotation)<.01f,"Walking/jump/drop/mouse look blocked in PC");
   foreach(float aspect in new[]{16f/9,4f/3,9f/16})
   {camera.aspect=aspect;await Wait(600);var corners=new Vector3[4];pc.GetScreenCorners(corners);Check(corners.All(c=>{var v=camera.WorldToViewportPoint(c);return v.z>camera.nearClipPlane && v.x>.005f && v.x<.995f && v.y>.005f && v.y<.995f;}),"Full monitor safely framed at aspect "+aspect);}
   camera.aspect=oldAspect;await Wait(600);await Capture("Assets/PC/Documentation~/PC-in-use.png");await Wait(300);
   await Click(pc.Desktop.BuyButton);Check(buys==1,"Real mouse click invokes Buy event once");await Click(pc.Desktop.OrdersButton);Check(orders==1,"Real mouse click invokes Orders event once");
   await Click(pc.Desktop.ExitButton);Check(pc.State==ComputerUseState.Exiting,"UI Exit starts return");Check(!player.enabled,"Movement stays disabled during return");await Wait(1000);
   Check(pc.State==ComputerUseState.Idle && player.enabled && interactor.enabled,"Return restores normal controllers");Check(Vector3.Distance(local,camera.transform.localPosition)<.0001f && Quaternion.Angle(rotation,camera.transform.localRotation)<.01f && Mathf.Abs(fov-camera.fieldOfView)<.001f && Mathf.Abs(yaw-player.Yaw)<.001f && Mathf.Abs(pitch-player.Pitch)<.001f,"Camera local pose/FOV/yaw/pitch restored");
   Check(!UnityEngine.Object.FindAnyObjectByType<TidyPauseMenu>().IsPaused,"UI exit does not pause game");
   var resumedPosition=player.transform.position;float resumedYaw=player.Yaw;keyState=new KeyboardState(Key.W);mouseState.delta=new Vector2(2,0);await Wait(120);keyState=new KeyboardState();mouseState.delta=Vector2.zero;await Wait(100);Check(Vector3.Distance(resumedPosition,player.transform.position)>.01f,"Actual WASD movement resumes after return");Check(Mathf.Abs(resumedYaw-player.Yaw)>.1f,"Actual mouse look resumes after return");
   await Approach();await KeyTap(Key.E);await Wait(900);keyState=new KeyboardState(Key.Escape);await Wait(1000);Check(pc.State==ComputerUseState.Exiting && !player.enabled,"Held ESC cannot fall through to Pause");keyState=new KeyboardState();await Wait(250);Check(pc.State==ComputerUseState.Idle && !UnityEngine.Object.FindAnyObjectByType<TidyPauseMenu>().IsPaused,"ESC release restores game without Pause");
   await Approach(1.6f,.15f);await KeyTap(Key.E);await KeyTap(Key.Escape);await Wait(900);Check(pc.State==ComputerUseState.Idle && player.enabled,"ESC cancels entry safely");
   await Approach();await KeyTap(Key.E);await Wait(900);object otherOwner=new object();TidyControlLock.Acquire(otherOwner,TidyControlChannel.Movement);pc.enabled=false;await Wait();Check(!pc.IsInUse && player.enabled && interactor.enabled && TidyControlLock.IsLocked(TidyControlChannel.Movement),"Disable PC restores controls and retains another owner lock");TidyControlLock.Release(otherOwner);pc.enabled=true;await Wait();Check(!TidyControlLock.IsLocked(TidyControlChannel.All),"PC abort releases its locks");
   var stone=UnityEngine.Object.FindObjectsByType<TidyObject>(FindObjectsInactive.Exclude).FirstOrDefault(s=>s.IsAvailableForPickup && s.GetComponent("RawStone")!=null);
   if(stone==null)failed.Add("No real raw stone found for pickup regression");else
   {
    interactor.ForcePickUp(stone);await Wait(180);Check(interactor.HeldObject==stone && stone.State==TidyObjectState.Held,"Existing raw stone pickup/hold still works");
    Check(!pc.TryEnter(),"PC rejects entry while carrying stone");Check(interactor.HeldObject==stone,"PC rejection preserves held stone");interactor.ForceDrop();await Wait(450);Check(interactor.CarriedCount==0 && stone.State!=TidyObjectState.Held,"Existing stone drop still works");
   }
  }
  catch(Exception ex) {failed.Add(ex.ToString());}
  finally
  {
   keyState=new KeyboardState();mouseState=new MouseState();pc.AbortImmediately();pc.enabled=true;player.Teleport(oldPlayer,oldYaw,oldPitch);camera.aspect=oldAspect;pc.BuyRequested-=buy;pc.OrdersRequested-=order;
   if(blocker!=null)UnityEngine.Object.Destroy(blocker);InputSystem.onBeforeUpdate-=InputTick;InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
  }
  var result=new Results{passed=passed.ToArray(),failed=failed.ToArray(),frames=Time.frameCount-startFrame};File.WriteAllText("Assets/PC/Documentation~/PC-test-results.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable] public class Results {public string[] passed;public string[] failed;public int frames;}
}



