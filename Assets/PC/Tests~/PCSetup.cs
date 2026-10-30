using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using StoneGame.Computers;
using TidyUpFramework.Player;
using TidyUpFramework.Interaction;
using TidyUpFramework.Feedback;
using TidyUpFramework.UI;
using TidyUpFramework.Levels;
using TidyUpFramework.Settings;
using TidyUpFramework.Save;

public static class PCSetup
{
 static void Ref(UnityEngine.Object o,string name,UnityEngine.Object value) {var s=new SerializedObject(o);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void Number(UnityEngine.Object o,string name,float value) {var s=new SerializedObject(o);s.FindProperty(name).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void Refs(UnityEngine.Object o,string name,UnityEngine.Object[] values) {var s=new SerializedObject(o);var p=s.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];s.ApplyModifiedPropertiesWithoutUndo();}
 static T Ensure<T>(GameObject go) where T:Component {var existing=go.GetComponent<T>();return existing!=null?existing:go.AddComponent<T>();}
 static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
 {var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;return r;}
 static Color C(float r,float g,float b)=>new Color(r,g,b,1);
 static void Label(string name,Transform parent,string text,TMP_FontAsset font,float size,Color color,Vector2 min,Vector2 max,Vector2 a,Vector2 b)
 {var r=Rect(name,parent,min,max,a,b);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.NoWrap;}
 static UnityEngine.UI.Button Button(string name,Transform parent,string text,TMP_FontAsset font,bool accent=false)
 {
  var r=Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
  var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=accent?C(.08f,.34f,.31f):C(.09f,.15f,.19f);
  var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.transition=UnityEngine.UI.Selectable.Transition.ColorTint;
  var c=b.colors;c.normalColor=Color.white;c.highlightedColor=C(1.35f,1.65f,1.55f);c.pressedColor=C(.65f,.85f,.78f);c.selectedColor=c.highlightedColor;c.fadeDuration=.1f;b.colors=c;
  var nav=b.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.None;b.navigation=nav;
  var layout=r.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();layout.preferredHeight=92;layout.flexibleWidth=1;
  var feedback=r.gameObject.AddComponent<TidyButtonFeedback>();Number(feedback,"hoverScale",1.015f);Number(feedback,"pressedScale",.985f);
  Label("Label",r,text,font,36,accent?C(.72f,1,.91f):C(.85f,.91f,.93f),Vector2.zero,Vector2.one,new Vector2(30,0),new Vector2(-30,0));
  Label("Arrow",r,">",font,34,C(.4f,.8f,.72f),new Vector2(1,0),Vector2.one,new Vector2(-65,0),new Vector2(-25,0));
  return b;
 }
 public static object Main()
 {
  if(EditorApplication.isPlaying)throw new Exception("Only Edit mode.");
  foreach(var folder in new[]{"Scripts","Materials","Prefabs","UI","Settings","Tests~","Documentation~"})Directory.CreateDirectory("Assets/PC/"+folder);
  AssetDatabase.Refresh();
  var pc=GameObject.Find("PC");if(pc==null)throw new Exception("PC missing");
  var screen=pc.transform.Find("Screen");var renderer=screen.GetComponent<MeshRenderer>();
  GameObjectUtility.SetStaticEditorFlags(screen.gameObject,GameObjectUtility.GetStaticEditorFlags(screen.gameObject)&~StaticEditorFlags.BatchingStatic);
  var manager=UnityEngine.Object.FindAnyObjectByType<TidyLevelManager>();
  const string baseline="Assets/PC/Tests~/SaveBaseline.json";
  if(!File.Exists(baseline))
  {
   var fs=AssetDatabase.FindAssets("t:TidyFrameworkSettings").Select(g=>AssetDatabase.LoadAssetAtPath<TidyFrameworkSettings>(AssetDatabase.GUIDToAssetPath(g))).First();
   ITidySaveStorage storage=fs.SaveStorage==TidySaveStorageKind.JsonFile?(ITidySaveStorage)new TidyJsonFileStorage(fs.SaveFolderName):new TidyPlayerPrefsStorage(fs.SaveFolderName+".");
   storage.TryRead("level_"+manager.LevelId,out var level);storage.TryRead("progress",out var progress);
   File.WriteAllText(baseline,JsonUtility.ToJson(new Baseline{saveProgress=manager.SavesProgress,level=level,progress=progress,levelId=manager.LevelId,runInBackground=PlayerSettings.runInBackground},true));
  }
  var ms=new SerializedObject(manager);ms.FindProperty("saveProgress").boolValue=false;ms.ApplyModifiedPropertiesWithoutUndo();
  var settings=AssetDatabase.LoadAssetAtPath<InteractiveComputerSettings>("Assets/PC/Settings/PCSettings.asset");
  if(settings==null){settings=ScriptableObject.CreateInstance<InteractiveComputerSettings>();AssetDatabase.CreateAsset(settings,"Assets/PC/Settings/PCSettings.asset");}
  settings.outlineShader=Shader.Find("TidyUpFramework/Outline");EditorUtility.SetDirty(settings);
  var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/PC/Materials/PCScreen.mat");
  if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,"Assets/PC/Materials/PCScreen.mat");}
  mat.SetColor("_BaseColor",C(.015f,.045f,.055f));mat.SetFloat("_Smoothness",.3f);mat.SetFloat("_Metallic",0);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",settings.emissionColor*settings.emissionIntensity);EditorUtility.SetDirty(mat);
  renderer.sharedMaterial=mat;Ref(Ensure<ComputerScreenEmission>(screen.gameObject),"settings",settings);
  var box=Ensure<BoxCollider>(screen.gameObject);var bounds=renderer.localBounds;box.center=bounds.center;box.size=new Vector3(bounds.size.x,bounds.size.y,.008f);
  var desk=pc.transform.Find("WarehouseDesk");var old=desk.GetComponent<BoxCollider>();old.enabled=false;
  var mesh=Ensure<MeshCollider>(desk.gameObject);mesh.sharedMesh=desk.GetComponent<MeshFilter>().sharedMesh;mesh.convex=false;mesh.sharedMaterial=old.sharedMaterial;
  var point=pc.transform.Find("PCViewPoint");if(point==null){point=new GameObject("PCViewPoint").transform;point.SetParent(pc.transform,false);}
  var normal=screen.localToWorldMatrix.inverse.transpose.MultiplyVector(Vector3.back).normalized;
  var center=screen.TransformPoint(new Vector3(bounds.center.x,bounds.center.y,bounds.min.z));point.SetPositionAndRotation(center+normal*.4f,Quaternion.LookRotation(-normal,screen.up));
  var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/PC/UI/PCInterfaceFont.asset");
  if(font==null)
  {
   font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf"),90,9,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic);
   font.name="PC Interface Turkish SDF";AssetDatabase.CreateAsset(font,"Assets/PC/UI/PCInterfaceFont.asset");AssetDatabase.AddObjectToAsset(font.material,font);AssetDatabase.AddObjectToAsset(font.atlasTextures[0],font);
   string chars=new string(Enumerable.Range(32,95).Select(i=>(char)i).ToArray())+"ÇçĞğİıÖöŞşÜü";
   if(!font.TryAddCharacters(chars,out string missing))throw new Exception("Font missing: "+missing);
   font.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(font);EditorUtility.SetDirty(font.atlasTextures[0]);
  }
  if(screen.Find("ComputerDesktop")!=null)throw new Exception("Desktop already exists; do not duplicate.");
  var root=new GameObject("ComputerDesktop",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));root.layer=5;
  var rt=(RectTransform)root.transform;float width=bounds.size.x*Mathf.Abs(screen.lossyScale.x),height=bounds.size.y*Mathf.Abs(screen.lossyScale.y);rt.sizeDelta=new Vector2(1000,1000*height/width);
  var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=Camera.main;
  var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;scaler.dynamicPixelsPerUnit=2;
  root.GetComponent<UnityEngine.UI.GraphicRaycaster>().blockingObjects=UnityEngine.UI.GraphicRaycaster.BlockingObjects.None;
  var group=root.GetComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
  var background=Rect("DesktopBackground",rt,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var bg=background.gameObject.AddComponent<UnityEngine.UI.Image>();bg.color=C(.022f,.045f,.059f);bg.raycastTarget=false;
  var top=Rect("Accent",rt,new Vector2(0,1),Vector2.one,new Vector2(0,-6),Vector2.zero);top.gameObject.AddComponent<UnityEngine.UI.Image>().color=C(.18f,.78f,.63f);
  Label("Kicker",rt,"ATÖLYE TERMİNALİ  /  01",font,21,C(.34f,.71f,.64f),new Vector2(0,1),Vector2.one,new Vector2(65,-74),new Vector2(-65,-30));
  Label("Title",rt,"STONE MARKET",font,61,C(.88f,.97f,.94f),new Vector2(0,1),Vector2.one,new Vector2(60,-155),new Vector2(-60,-80));
  Label("Subtitle",rt,"Taş atölyenize hoş geldiniz.",font,25,C(.48f,.61f,.64f),new Vector2(0,1),Vector2.one,new Vector2(65,-202),new Vector2(-65,-157));
  var list=Rect("Actions",rt,Vector2.zero,Vector2.one,new Vector2(65,90),new Vector2(-65,-230));
  var vertical=list.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();vertical.spacing=14;vertical.childControlHeight=true;vertical.childControlWidth=true;vertical.childForceExpandHeight=false;vertical.childForceExpandWidth=true;
  var buy=Button("BuyStone",list,"Taş Satın Al",font,true);var orders=Button("MyOrders",list,"Siparişlerim",font);var exit=Button("Exit",list,"Çıkış",font);
  Label("Footer",rt,"ESC  /  BİLGİSAYARDAN AYRIL",font,19,C(.38f,.5f,.53f),Vector2.zero,new Vector2(1,0),new Vector2(65,23),new Vector2(-65,58));
  var view=root.AddComponent<ComputerDesktopView>();Ref(view,"desktopCanvas",canvas);Ref(view,"interactionGroup",group);Ref(view,"buyButton",buy);Ref(view,"ordersButton",orders);Ref(view,"exitButton",exit);Ref(view,"interfaceFont",font);
  var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/PC/Prefabs/ComputerDesktop.prefab");UnityEngine.Object.DestroyImmediate(root);
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,screen);rt=(RectTransform)instance.transform;rt.localPosition=new Vector3(bounds.center.x,bounds.center.y,bounds.min.z-.004f);rt.localRotation=Quaternion.identity;
  float units=width/1000*.97f;rt.localScale=new Vector3(units/Mathf.Abs(screen.lossyScale.x),units/Mathf.Abs(screen.lossyScale.y),units/Mathf.Abs(screen.lossyScale.z));
  view=instance.GetComponent<ComputerDesktopView>();view.Canvas.worldCamera=Camera.main;
  var controller=Ensure<InteractiveComputer>(pc);Ref(controller,"settings",settings);Ref(controller,"screenRenderer",renderer);Ref(controller,"pcViewPoint",point);Ref(controller,"desktop",view);
  Ref(controller,"playerCamera",Camera.main);Ref(controller,"playerController",UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>());Ref(controller,"interactor",UnityEngine.Object.FindAnyObjectByType<TidyInteractor>());Ref(controller,"promptView",UnityEngine.Object.FindAnyObjectByType<TidyInteractionPromptView>());Ref(controller,"highlighter",UnityEngine.Object.FindAnyObjectByType<TidyOutlineHighlighter>());
  Refs(controller,"interactionColliders",new UnityEngine.Object[]{box,pc.transform.Find("Monitor 2").GetComponent<MeshCollider>()});
  Refs(controller,"pauseMenus",UnityEngine.Object.FindObjectsByType<TidyPauseMenu>(FindObjectsInactive.Include));Refs(controller,"screenStacks",UnityEngine.Object.FindObjectsByType<TidyScreenStack>(FindObjectsInactive.Include));Refs(controller,"cameraDrivers",new UnityEngine.Object[]{Camera.main.GetComponent<TidyCameraPunch>()});
  PrefabUtility.RecordPrefabInstancePropertyModifications(rt);PrefabUtility.RecordPrefabInstancePropertyModifications(view.Canvas);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(pc.scene);Selection.activeGameObject=pc;Physics.SyncTransforms();
  return new {configured=true,screenSize=new[]{width,height},font=AssetDatabase.GetAssetPath(font),canvasRect=rt.sizeDelta.ToString(),saveDisabled=!manager.SavesProgress,eventSystems=UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include).Length};
 }
 [Serializable] public class Baseline {public bool saveProgress;public string levelId;public string level;public string progress;public bool runInBackground;}
}
