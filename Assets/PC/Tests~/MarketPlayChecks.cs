using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using StoneGame.Computers;
using StoneGame.Computers.UI;
using StoneGame.Economy;
using StoneGame.Shop;
using TidyUpFramework.Player;
using TidyUpFramework.Interaction;
using TidyUpFramework.Objects;
public static class MarketPlayChecks
{
 static Keyboard keyboard;static Mouse mouse;static KeyboardState keyState;static MouseState mouseState;static string pendingText;
 static void Tick(){if(InputState.currentUpdateType!=InputUpdateType.Dynamic)return;InputSystem.QueueStateEvent(keyboard,keyState);InputSystem.QueueStateEvent(mouse,mouseState);if(pendingText!=null){foreach(char c in pendingText)InputSystem.QueueTextEvent(keyboard,c);pendingText=null;}}
 static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
 static async Task Wait(int ms=150){await Task.Delay(ms);}
 static async Task Tap(Key key){keyState=new KeyboardState(key);await Wait(70);keyState=new KeyboardState();await Wait(80);}
 static Camera camera;static List<string> passed;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);passed.Add(label);}
 static async Task Click(Transform target)
 {
  var rect=(RectTransform)target;var point=camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));mouseState=new MouseState{position=new Vector2(point.x,point.y)};await Wait(100);
  var data=new PointerEventData(EventSystem.current){position=mouseState.position};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
  Check(hits.Any(h=>h.gameObject.transform==target || h.gameObject.transform.IsChildOf(target)),target.name+" real pointer raycast");
  mouseState=mouseState.WithButton(MouseButton.Left,true);await Wait(70);mouseState=mouseState.WithButton(MouseButton.Left,false);await Wait(140);
 }
 public static async Task<object> Main()
 {
  if(!Application.isPlaying)throw new Exception("Play mode required");passed=new List<string>();var failed=new List<string>();
  var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();var view=UnityEngine.Object.FindAnyObjectByType<StoneMarketView>();var player=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();var interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();camera=pc.PlayerCamera;var economy=view.Economy;
  var originalPosition=player.transform.position;float originalYaw=player.Yaw,originalPitch=player.Pitch;var originalCatalog=view.Catalog;var testCatalog=UnityEngine.Object.Instantiate(originalCatalog);Texture2D texture=null;Sprite sprite=null;
  var oldBackground=InputSystem.settings.backgroundBehavior;var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  keyboard=InputSystem.AddDevice<Keyboard>("StoneMarketChecks");mouse=InputSystem.AddDevice<Mouse>("StoneMarketMouse");InputSystem.EnableDevice(keyboard);InputSystem.EnableDevice(mouse);InputSystem.onBeforeUpdate+=Tick;
  keyState=new KeyboardState();mouseState=new MouseState{position=new Vector2(5,5)};int frame=Time.frameCount;
  try
  {
   await Wait();Check(Time.frameCount>frame,"Player loop advances");Check(!UnityEngine.Object.FindAnyObjectByType<TidyUpFramework.Levels.TidyLevelManager>().SavesProgress,"Real level saves isolated");Check(economy.IsReady && economy.Balance==10000,"Sandbox economy starts ready with 10000");
   Check(pc.Desktop.Canvas.renderMode==RenderMode.WorldSpace && view.transform.IsChildOf(pc.ScreenRenderer.transform),"Existing physical monitor Canvas retained");Check(view.Cards.Count==1,"Catalog produces initial product card");
   Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"Existing single EventSystem retained");Check(economy.Orders.Count==0,"No delivery or order spawned on startup");
   var approach=pc.ScreenCenter+pc.ScreenNormal*1.5f;approach.y=.04f;player.Teleport(approach,90,20);await Wait();var dir=(pc.ScreenCenter-camera.transform.position).normalized;player.Teleport(player.transform.position,Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg,-Mathf.Asin(dir.y)*Mathf.Rad2Deg);await Wait();Check(pc.IsHovered,"Monitor target still detected");
   await Tap(Key.E);await Wait(900);Check(pc.State==ComputerUseState.Using && Cursor.visible && !player.enabled,"E opens market with existing camera/cursor locks");
   await Click(pc.Desktop.OrdersButton.transform);Check(view.OrdersVisible && view.VisibleOrderCount==0,"Orders tab empty state");await Click(pc.Desktop.BuyButton.transform);
   await Click(view.Cards[0].SelectButton.transform);Check(view.QuantityInput.text=="1" && view.TotalText=="100 TL","Selecting product starts at one and quotes unit price");
   await Click(view.QuantityInput.transform);Check(view.QuantityInput.isFocused,"TMP quantity input receives real pointer focus");
   view.QuantityInput.ProcessEvent(Event.KeyboardEvent("^a"));foreach(char c in "15")view.QuantityInput.ProcessEvent(new Event{type=EventType.KeyDown,character=c});await Wait(150);
   Check(view.QuantityInput.text=="15" && view.TotalText=="1.500 TL","TMP keyboard event processing updates quantity and total immediately");
   foreach(var invalid in new[]{"0","-1","1.5","","2147483648","1000"})
   {view.QuantityInput.text=invalid;await Wait(30);await Click(view.CreateOrderButton.transform);Check(economy.Balance==10000 && economy.Orders.Count==0 && view.NotificationVisible && view.NotificationText.Contains("Geçersiz adet"),"Invalid UI order rejected: '"+invalid+"'");}
   view.QuantityInput.text="15";await Wait();await Click(view.CreateOrderButton.transform);Check(economy.Balance==8500 && economy.Orders.Count==1,"Real create click charges 1500 and creates one order");Check(view.NotificationVisible && view.NotificationText=="Siparişiniz başarıyla oluşturuldu!","Success notification visible");
   await Click(view.CreateOrderButton.transform);view.Submit();Check(economy.Balance==8500 && economy.Orders.Count==1,"Repeated click/event does not double charge");
   Check(economy.Orders[0].UnitPrice==100 && economy.Orders[0].TotalPaid==1500 && economy.Orders[0].Status!=StoneGame.Orders.StoneOrderStatus.Cancelled,"Historical price preserved while delivery can advance existing order status");
   await Click(pc.Desktop.OrdersButton.transform);await Wait();Check(view.OrdersVisible && view.VisibleOrderCount==1,"Order appears in Orders tab");
   var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();var reloaded=new EconomyService(new EconomyFileStore(bootstrap.StoragePath),1);Check(reloaded.Balance==8500 && reloaded.Orders.Count==1,"Live file reload preserves wallet/history without replay");
   await Click(pc.Desktop.BuyButton.transform);await Click(view.Cards[0].SelectButton.transform);view.QuantityInput.text="100";await Click(view.CreateOrderButton.transform);Check(view.NotificationText==EconomyService.InsufficientFunds && economy.Balance==8500 && economy.Orders.Count==1,"Insufficient UI payment shows exact error and no mutation");
   Set(view,"catalog",testCatalog);var source=testCatalog.Products[0];var products=new List<StoneProduct>();
   for(int i=0;i<100;i++)products.Add(new StoneProduct{id="test-product-"+i,displayName="Taş "+(i+1),unitPrice=100,prefab=source.prefab,definition=source.definition,description="Test ürünü",active=true});Set(testCatalog,"products",products);view.OpenShop();await Wait(300);
   Check(view.Cards.Count==100,"100 catalog products dynamically instantiated");var scroll=view.GetComponentInChildren<UnityEngine.UI.ScrollRect>();var content=(RectTransform)view.Cards[0].transform.parent;Check(content.rect.height>5000,"Product content grows for scrolling");
   var productScroll=content.parent.parent.GetComponent<UnityEngine.UI.ScrollRect>();productScroll.verticalNormalizedPosition=0;await Wait();Check(content.anchoredPosition.y>1000,"ScrollRect reaches products beyond viewport");
   texture=new Texture2D(32,16);sprite=Sprite.Create(texture,new Rect(0,0,32,16),new Vector2(.5f,.5f));products[0].image=sprite;Set(testCatalog,"products",products.Take(2).ToList());view.OpenShop();await Wait();Check(view.Cards.Count==2 && view.Cards[0].ProductImage.sprite==sprite && view.Cards[0].ProductImage.preserveAspect,"New product count and changed Sprite reflected with preserved aspect");
   Set(testCatalog,"products",products.Take(1).ToList());view.OpenShop();await Wait();Check(view.Cards.Count==1,"Removing product updates list without duplicates");
   products[0].id=source.id;products[0].unitPrice=200;view.OpenShop();await Wait();Check(economy.Orders[0].UnitPrice==100 && economy.Orders[0].TotalPaid==1500,"Catalog price changes do not rewrite past orders");
   Set(view,"catalog",originalCatalog);view.OpenShop();await Wait();await Tap(Key.Escape);await Wait(950);Check(pc.State==ComputerUseState.Idle && player.enabled && interactor.enabled,"ESC exit restores existing controllers");Check(!view.NotificationVisible && !view.QuantityInput.isFocused,"Exit cancels notification and text focus");
   player.Teleport(approach,Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg,-Mathf.Asin(dir.y)*Mathf.Rad2Deg);await Wait();await Tap(Key.E);await Wait(950);Check(pc.State==ComputerUseState.Using && economy.Balance==8500 && economy.Orders.Count==1,"Reentering PC retains balance/history");
   await Click(pc.Desktop.ExitButton.transform);await Wait(950);Check(pc.State==ComputerUseState.Idle,"Existing UI Exit button still works");
   var raw=UnityEngine.Object.FindObjectsByType<StoneGame.Cutting.RawStone>().First();var tidy=raw.GetComponent<TidyObject>();interactor.ForcePickUp(tidy);await Wait();Check(interactor.HeldObject==tidy,"Existing stone pickup remains functional");interactor.ForceDrop();await Wait(400);Check(interactor.CarriedCount==0,"Existing stone drop remains functional");
  }
  catch(Exception ex){failed.Add(ex.ToString());}
  finally
  {
   keyState=new KeyboardState();pc.AbortImmediately();Set(view,"catalog",originalCatalog);view.OpenShop();player.Teleport(originalPosition,originalYaw,originalPitch);InputSystem.onBeforeUpdate-=Tick;InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;UnityEngine.Object.Destroy(testCatalog);if(sprite!=null)UnityEngine.Object.Destroy(sprite);if(texture!=null)UnityEngine.Object.Destroy(texture);
  }
  var result=new Results{passed=passed.ToArray(),failed=failed.ToArray(),frames=Time.frameCount-frame};File.WriteAllText("Assets/PC/Documentation~/Market-play-results.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable] public class Results{public string[] passed,failed;public int frames;}
}

