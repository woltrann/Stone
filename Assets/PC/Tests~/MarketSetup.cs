using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using StoneGame.Computers;
using StoneGame.Computers.UI;
using StoneGame.Cutting;
using StoneGame.Shop;
using StoneGame.Economy;
using TidyUpFramework.Objects;
using TidyUpFramework.Levels;
using TidyUpFramework.Settings;
using TidyUpFramework.Save;
using TidyUpFramework.UI;

public static class MarketSetup
{
 static TMP_FontAsset font;
 static Color Ink=new Color(.87f,.94f,.95f),Muted=new Color(.57f,.72f,.75f),Accent=new Color(.4f,.92f,.76f);
 static void Ref(UnityEngine.Object o,string key,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(key).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static RectTransform R(string name,Transform parent,Vector2 min,Vector2 max,Vector2 a,Vector2 b)
 {var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);Place(r,min,max,a,b);return r;}
 static void Place(RectTransform r,Vector2 min,Vector2 max,Vector2 a,Vector2 b){r.anchorMin=min;r.anchorMax=max;r.offsetMin=a;r.offsetMax=b;}
 static UnityEngine.UI.Image Image(RectTransform r,Color color){var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.color=color;return i;}
 static TMP_Text Text(string name,Transform parent,string value,float size,Vector2 min,Vector2 max,Vector2 a,Vector2 b,Color? color=null)
 {var r=R(name,parent,min,max,a,b);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=color??Ink;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;t.textWrappingMode=TextWrappingModes.Normal;return t;}
 static UnityEngine.UI.Button Button(string name,Transform parent,string text,Vector2 min,Vector2 max,Vector2 a,Vector2 b)
 {var r=R(name,parent,min,max,a,b);var image=Image(r,new Color(.08f,.3f,.26f));var button=r.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=new Color(1.3f,1.65f,1.5f);colors.pressedColor=new Color(.6f,.8f,.7f);colors.fadeDuration=.1f;button.colors=colors;var nav=button.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=nav;r.gameObject.AddComponent<TidyButtonFeedback>();var t=Text("Label",r,text,23,Vector2.zero,Vector2.one,new Vector2(10,0),new Vector2(-10,0),Accent);t.alignment=TextAlignmentOptions.Center;return button;}
 static UnityEngine.UI.ScrollRect Scroll(string name,Transform parent,Vector2 min,Vector2 max,Vector2 a,Vector2 b,bool grid,out RectTransform content)
 {
  var root=R(name,parent,min,max,a,b);var scroll=root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();scroll.horizontal=false;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45;scroll.inertia=true;
  var vp=R("Viewport",root,Vector2.zero,Vector2.one,Vector2.zero,new Vector2(-15,0));Image(vp,new Color(.035f,.075f,.09f));vp.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=true;
  content=R("Content",vp,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);content.pivot=new Vector2(.5f,1);
  var fitter=content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();fitter.horizontalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
  if(grid){var g=content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();g.cellSize=new Vector2(286,184);g.spacing=new Vector2(12,12);g.padding=new RectOffset(8,8,8,8);g.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;g.constraintCount=2;}
  else{var v=content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();v.spacing=7;v.padding=new RectOffset(14,14,8,8);v.childControlWidth=true;v.childControlHeight=true;v.childForceExpandHeight=false;}
  var barRect=R("Scrollbar",root,new Vector2(1,0),Vector2.one,new Vector2(-9,0),Vector2.zero);Image(barRect,new Color(.07f,.13f,.16f));var bar=barRect.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
  var sliding=R("SlidingArea",barRect,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var handle=R("Handle",sliding,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var hi=Image(handle,new Color(.25f,.55f,.49f));bar.handleRect=handle;bar.targetGraphic=hi;
  scroll.content=content;scroll.viewport=vp;scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;
  return scroll;
 }
 static StoneProductCard Card()
 {
  var root=R("StoneProductCard",null,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(286,184));Image(root,new Color(.065f,.13f,.155f));
  var art=R("Art",root,new Vector2(0,1),new Vector2(0,1),new Vector2(12,-112),new Vector2(106,-14));Image(art,new Color(.08f,.2f,.22f));
  var placeholder=Text("Placeholder",art,"TAŞ",25,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Accent);placeholder.alignment=TextAlignmentOptions.Center;
  var picture=R("ProductImage",art,Vector2.zero,Vector2.one,new Vector2(4,4),new Vector2(-4,-4));var image=Image(picture,Color.white);image.raycastTarget=false;image.preserveAspect=true;
  var name=Text("ProductName",root,"Ham Taş",25,new Vector2(0,1),Vector2.one,new Vector2(120,-80),new Vector2(-12,-14));name.enableAutoSizing=true;name.fontSizeMin=19;name.fontSizeMax=25;
  var price=Text("Price",root,"100 TL",23,new Vector2(0,1),Vector2.one,new Vector2(120,-116),new Vector2(-12,-80),Accent);
  var select=Button("Select",root,"Ürünü Seç",Vector2.zero,new Vector2(1,0),new Vector2(12,12),new Vector2(-12,56));
  var card=root.gameObject.AddComponent<StoneProductCard>();Ref(card,"nameLabel",name);Ref(card,"priceLabel",price);Ref(card,"placeholderLabel",placeholder);Ref(card,"productImage",image);Ref(card,"selectButton",select);
  var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/PC/Prefabs/StoneProductCard.prefab");UnityEngine.Object.DestroyImmediate(root.gameObject);return prefab.GetComponent<StoneProductCard>();
 }
 public static object Main()
 {
  if(Application.isPlaying)throw new Exception("Edit mode required.");
  foreach(var dir in new[]{"ScriptableObjects","Sprites","Prefabs/Products","Scripts/Shop","Scripts/Orders","Scripts/Economy","Scripts/UI"})Directory.CreateDirectory("Assets/PC/"+dir);
  AssetDatabase.Refresh();
  var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();var manager=UnityEngine.Object.FindAnyObjectByType<TidyLevelManager>();
  const string baselinePath="Assets/PC/Tests~/MarketTestBaseline.json";
  if(!File.Exists(baselinePath))
  {
   var fs=Resources.Load<TidyFrameworkSettings>(TidyFrameworkSettings.ResourcesPath);ITidySaveStorage storage=fs.SaveStorage==TidySaveStorageKind.JsonFile?(ITidySaveStorage)new TidyJsonFileStorage(fs.SaveFolderName):new TidyPlayerPrefsStorage(fs.SaveFolderName+".");
   storage.TryRead("level_"+manager.LevelId,out var level);storage.TryRead("progress",out var progress);
   File.WriteAllText(baselinePath,JsonUtility.ToJson(new Baseline{saveProgress=manager.SavesProgress,levelId=manager.LevelId,level=level,progress=progress,testPath=Path.GetFullPath("Assets/PC/Tests~/MarketSandbox_"+Guid.NewGuid().ToString("N")+".json")},true));
  }
  var baseline=JsonUtility.FromJson<Baseline>(File.ReadAllText(baselinePath));var ms=new SerializedObject(manager);ms.FindProperty("saveProgress").boolValue=false;ms.ApplyModifiedPropertiesWithoutUndo();
  var catalog=AssetDatabase.LoadAssetAtPath<StoneMarketCatalog>("Assets/PC/ScriptableObjects/StoneMarketCatalog.asset");
  if(catalog==null)
  {
   var raw=UnityEngine.Object.FindObjectsByType<RawStone>().First(r=>r.gameObject.activeSelf && Mathf.Abs(r.transform.localScale.x-.14f)<.01f);var clone=UnityEngine.Object.Instantiate(raw.gameObject);clone.name="Market Raw Stone";
   if(PrefabUtility.IsPartOfPrefabInstance(clone))PrefabUtility.UnpackPrefabInstance(clone,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   clone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);clone.GetComponent<TidyObject>().AssignInstanceId(string.Empty);
   var rs=new SerializedObject(clone.GetComponent<RawStone>());rs.FindProperty("hasRolled").boolValue=false;rs.FindProperty("rolledOre").objectReferenceValue=null;rs.ApplyModifiedPropertiesWithoutUndo();
   var stonePrefab=PrefabUtility.SaveAsPrefabAsset(clone,"Assets/PC/Prefabs/Products/RawStone.prefab");UnityEngine.Object.DestroyImmediate(clone);
   catalog=ScriptableObject.CreateInstance<StoneMarketCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/PC/ScriptableObjects/StoneMarketCatalog.asset");
   var cs=new SerializedObject(catalog);var list=cs.FindProperty("products");list.arraySize=1;var p=list.GetArrayElementAtIndex(0);
   p.FindPropertyRelative("id").stringValue="raw-stone-default";p.FindPropertyRelative("displayName").stringValue=raw.GetComponent<TidyObject>().DisplayName;p.FindPropertyRelative("description").stringValue="Atölyenizde işleyebileceğiniz ham taş. İçeriği kesim sırasında ortaya çıkar.";
   p.FindPropertyRelative("unitPrice").longValue=100;p.FindPropertyRelative("prefab").objectReferenceValue=stonePrefab;p.FindPropertyRelative("definition").objectReferenceValue=raw.GetComponent<TidyObject>().Definition;p.FindPropertyRelative("active").boolValue=true;cs.ApplyModifiedPropertiesWithoutUndo();
  }
  var settings=AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/PC/ScriptableObjects/EconomySettings.asset");if(settings==null){settings=ScriptableObject.CreateInstance<EconomySettings>();AssetDatabase.CreateAsset(settings,"Assets/PC/ScriptableObjects/EconomySettings.asset");}
  var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();if(bootstrap==null)bootstrap=new GameObject("Stone Economy").AddComponent<StoneEconomyBootstrap>();Ref(bootstrap,"settings",settings);var bs=new SerializedObject(bootstrap);bs.FindProperty("storagePathOverride").stringValue=baseline.testPath;bs.ApplyModifiedPropertiesWithoutUndo();
  font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/PC/UI/PCInterfaceFont.asset");font.atlasPopulationMode=AtlasPopulationMode.Dynamic;font.TryAddCharacters("—–×•‹›",out string missing);font.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(font);foreach(var texture in font.atlasTextures)EditorUtility.SetDirty(texture);
  var card=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PC/Prefabs/StoneProductCard.prefab");var cardView=card!=null?card.GetComponent<StoneProductCard>():Card();
  const string path="Assets/PC/Prefabs/ComputerDesktop.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   if(root.GetComponent<StoneMarketView>()!=null)throw new Exception("Market UI already configured; do not duplicate setup.");
   var rt=(RectTransform)root.transform;var desktop=root.GetComponent<ComputerDesktopView>();
   root.transform.Find("DesktopBackground").GetComponent<UnityEngine.UI.Image>().color=new Color(.025f,.06f,.075f);
   var title=root.transform.Find("Title").GetComponent<TMP_Text>();Place((RectTransform)title.transform,new Vector2(0,1),Vector2.one,new Vector2(35,-75),new Vector2(-360,-15));title.fontSize=46;
   var kicker=root.transform.Find("Kicker").GetComponent<TMP_Text>();kicker.gameObject.SetActive(false);
   var subtitle=root.transform.Find("Subtitle").GetComponent<TMP_Text>();Place((RectTransform)subtitle.transform,Vector2.zero,new Vector2(1,0),new Vector2(35,8),new Vector2(-270,43));subtitle.text="Taş seçin, adedinizi belirleyin ve sipariş oluşturun.";subtitle.fontSize=17;
   var footer=root.transform.Find("Footer").GetComponent<TMP_Text>();Place((RectTransform)footer.transform,Vector2.zero,new Vector2(1,0),new Vector2(735,8),new Vector2(-35,43));footer.text="ESC  /  ÇIKIŞ";footer.fontSize=18;footer.alignment=TextAlignmentOptions.MidlineRight;
   var balance=Text("Balance",rt,"10.000 TL",30,new Vector2(0,1),Vector2.one,new Vector2(640,-75),new Vector2(-35,-15),Accent);balance.alignment=TextAlignmentOptions.MidlineRight;
   var actions=(RectTransform)root.transform.Find("Actions");actions.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().enabled=false;Place(actions,new Vector2(0,1),Vector2.one,new Vector2(35,-144),new Vector2(-35,-88));
   Place((RectTransform)desktop.BuyButton.transform,new Vector2(0,0),new Vector2(0,1),Vector2.zero,new Vector2(275,0));Place((RectTransform)desktop.OrdersButton.transform,new Vector2(0,0),new Vector2(0,1),new Vector2(287,0),new Vector2(562,0));Place((RectTransform)desktop.ExitButton.transform,new Vector2(1,0),Vector2.one,new Vector2(-122,0),Vector2.zero);
   foreach(var button in new[]{desktop.BuyButton,desktop.OrdersButton,desktop.ExitButton}){button.transform.Find("Arrow").gameObject.SetActive(false);var label=button.transform.Find("Label").GetComponent<TMP_Text>();label.fontSize=25;label.alignment=TextAlignmentOptions.Center;}
   desktop.BuyButton.transform.Find("Label").GetComponent<TMP_Text>().text="Taş Mağazası";
   var shop=R("ShopPage",rt,Vector2.zero,Vector2.one,new Vector2(35,63),new Vector2(-35,-164));var orders=R("OrdersPage",rt,Vector2.zero,Vector2.one,new Vector2(35,63),new Vector2(-35,-164));orders.gameObject.SetActive(false);
   var productsScroll=Scroll("Products",shop,Vector2.zero,Vector2.one,Vector2.zero,new Vector2(-315,0),true,out var productsContent);
   var panel=R("OrderPanel",shop,new Vector2(1,0),Vector2.one,new Vector2(-300,0),Vector2.zero);Image(panel,new Color(.055f,.11f,.135f));
   var selectedName=Text("SelectedName",panel,"Taş seçin",30,new Vector2(0,1),Vector2.one,new Vector2(20,-70),new Vector2(-20,-12));
   var description=Text("Description",panel,"Sipariş oluşturmak için bir ürün seçin.",20,new Vector2(0,1),Vector2.one,new Vector2(20,-146),new Vector2(-20,-72),Muted);description.alignment=TextAlignmentOptions.TopLeft;
   var unit=Text("UnitPrice",panel,"Birim fiyat  —",22,new Vector2(0,1),Vector2.one,new Vector2(20,-185),new Vector2(-20,-147));
   Text("QuantityCaption",panel,"Sipariş adedi",21,new Vector2(0,1),Vector2.one,new Vector2(20,-218),new Vector2(-20,-185),Muted);
   var inputRect=R("Quantity",panel,new Vector2(0,1),Vector2.one,new Vector2(20,-272),new Vector2(-20,-220));var inputImage=Image(inputRect,new Color(.08f,.18f,.21f));var input=inputRect.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=inputImage;
   var textArea=R("TextArea",inputRect,Vector2.zero,Vector2.one,new Vector2(12,5),new Vector2(-12,-5));textArea.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();var inputText=(TextMeshProUGUI)Text("Text",textArea,"1",27,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);inputText.richText=false;
   input.textViewport=textArea;input.textComponent=inputText;input.fontAsset=font;input.contentType=TMP_InputField.ContentType.Standard;input.characterLimit=12;input.lineType=TMP_InputField.LineType.SingleLine;input.keyboardType=TouchScreenKeyboardType.NumberPad;input.text="1";input.caretColor=Accent;input.customCaretColor=true;
   var hint=Text("QuantityHint",panel,"",17,new Vector2(0,1),Vector2.one,new Vector2(20,-318),new Vector2(-20,-274),Muted);hint.enableAutoSizing=true;hint.fontSizeMin=13;hint.fontSizeMax=17;
   var total=Text("Total",panel,"—",31,Vector2.zero,new Vector2(1,0),new Vector2(20,75),new Vector2(-20,125),Accent);
   var create=Button("CreateOrder",panel,"Siparişi Oluştur",Vector2.zero,new Vector2(1,0),new Vector2(20,16),new Vector2(-20,66));
   var ordersScroll=Scroll("Orders",orders,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,false,out var ordersContent);
   var empty=Text("EmptyOrders",orders,"Henüz siparişiniz bulunmuyor.",28,Vector2.zero,Vector2.one,new Vector2(30,20),new Vector2(-30,-20),Muted);empty.alignment=TextAlignmentOptions.Center;
   var rowRoot=R("StoneOrderRow",null,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(900,90));var rowText=rowRoot.gameObject.AddComponent<TextMeshProUGUI>();rowText.font=font;rowText.fontSize=24;rowText.color=Ink;rowText.raycastTarget=false;rowText.textWrappingMode=TextWrappingModes.Normal;rowText.alignment=TextAlignmentOptions.MidlineLeft;rowRoot.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=90;
   var rowPrefab=PrefabUtility.SaveAsPrefabAsset(rowRoot.gameObject,"Assets/PC/Prefabs/StoneOrderRow.prefab");UnityEngine.Object.DestroyImmediate(rowRoot.gameObject);
   var notification=R("Notification",rt,Vector2.zero,new Vector2(1,0),new Vector2(35,62),new Vector2(-35,123));var notificationImage=Image(notification,new Color(.07f,.29f,.22f));notificationImage.raycastTarget=false;var notificationGroup=notification.gameObject.AddComponent<CanvasGroup>();notificationGroup.blocksRaycasts=false;
   var notificationText=Text("Message",notification,"",23,Vector2.zero,Vector2.one,new Vector2(20,5),new Vector2(-20,-5));notificationText.enableAutoSizing=true;notificationText.fontSizeMin=17;notificationText.fontSizeMax=23;notification.gameObject.SetActive(false);
   var view=root.AddComponent<StoneMarketView>();
   Ref(view,"catalog",catalog);Ref(view,"cardPrefab",cardView);Ref(view,"shopPage",shop.gameObject);Ref(view,"ordersPage",orders.gameObject);Ref(view,"productsContent",productsContent);Ref(view,"ordersContent",ordersContent);Ref(view,"productsScroll",productsScroll);Ref(view,"ordersScroll",ordersScroll);
   Ref(view,"balanceLabel",balance);Ref(view,"selectedName",selectedName);Ref(view,"descriptionLabel",description);Ref(view,"unitPriceLabel",unit);Ref(view,"totalLabel",total);Ref(view,"quantityHint",hint);Ref(view,"emptyOrdersLabel",empty);Ref(view,"quantityInput",input);Ref(view,"createOrderButton",create);Ref(view,"notificationPanel",notification.gameObject);Ref(view,"notificationGroup",notificationGroup);Ref(view,"notificationBackground",notificationImage);Ref(view,"notificationText",notificationText);Ref(view,"orderRowPrefab",rowPrefab.GetComponent<TMP_Text>());Ref(view,"shopTab",desktop.BuyButton);Ref(view,"ordersTab",desktop.OrdersButton);
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
  return new {products=catalog.Products.Count,errors=catalog.ValidateCatalog(),testSave=baseline.testPath,realSavesDisabled=!manager.SavesProgress};
 }
 [Serializable] public class Baseline{public bool saveProgress;public string levelId,level,progress,testPath;}
}
