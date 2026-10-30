using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using StoneGame.Chipping;
using StoneGame.Cutting;
using StoneGame.Computers;
using TidyUpFramework.Placement;
public static class ChippingSetup
{
 static void Ref(UnityEngine.Object o,string f,UnityEngine.Object v){var s=new SerializedObject(o);s.FindProperty(f).objectReferenceValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
 static Transform Child(Transform parent,string name,Vector3 world,Quaternion rotation){var t=parent.Find(name);if(t==null){t=new GameObject(name).transform;t.SetParent(parent,false);}t.SetPositionAndRotation(world,rotation);var scale=parent.lossyScale;t.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);return t;}
 public static object Main()
 {
  if(Application.isPlaying)throw new Exception("Edit mode required");
  var table=GameObject.Find("Table");var hammer=GameObject.Find("WarehouseHammer");if(table==null || hammer==null)throw new Exception("Existing models missing");
  var t=table.GetComponent<StoneChippingTable>();if(t==null)t=table.AddComponent<StoneChippingTable>();
  var work=Child(table.transform,"StoneWorkPoint",new Vector3(-1.13f,.9f,11.12f),Quaternion.identity);
  var slotT=Child(table.transform,"StonePlacementSlot",work.position,work.rotation);var slot=slotT.GetComponent<StoneWorkSlot>();if(slot==null)slot=slotT.gameObject.AddComponent<StoneWorkSlot>();
  slot.ConfigureIdentity("stone-chipping-table-slot","Cevher yontma masası");slot.ConfigureBehaviour(true,false,false);slot.ConfigureRules(new TidyUpFramework.Rules.TidyMatchFilter(),TidyPlacementPolicy.RejectIncorrect,TidyCompletionMode.WhenFull,1);
  var slotSO=new SerializedObject(slot);slotSO.FindProperty("countsTowardsProgress").boolValue=false;slotSO.FindProperty("contentRoot").objectReferenceValue=work;slotSO.ApplyModifiedPropertiesWithoutUndo();Ref(slot,"table",t);
  var trigger=slotT.GetComponent<BoxCollider>();if(trigger==null)trigger=slotT.gameObject.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=new Vector3(0,.15f,0);trigger.size=new Vector3(.65f,.35f,.6f);
  var camera=Child(table.transform,"TableCameraPoint",new Vector3(-1.13f,2.1f,9.55f),Quaternion.LookRotation(new Vector3(0,-.75f,1.8f)));
  var holder=Child(table.transform,"HammerHolder",new Vector3(-2.1f,1.85f,11.78f),Quaternion.identity);
  var returnCollider=holder.GetComponent<BoxCollider>();if(returnCollider==null)returnCollider=holder.gameObject.AddComponent<BoxCollider>();returnCollider.isTrigger=true;returnCollider.size=new Vector3(.4f,.45f,.16f);
  Undo.RecordObject(hammer.transform,"Move existing hammer to pegboard");hammer.transform.SetParent(holder,true);hammer.transform.SetPositionAndRotation(holder.position,Quaternion.Euler(90,0,0));hammer.transform.localScale=Vector3.one;
  // This existing prop becomes the dedicated wall tool; the standard stone pickup system stays unchanged.
  var hammerTidy=hammer.GetComponent<TidyUpFramework.Objects.TidyObject>();if(hammerTidy!=null){Undo.RecordObject(hammerTidy,"Reserve hammer for work mode");hammerTidy.enabled=false;}
  foreach(var c in hammer.GetComponentsInChildren<Collider>()){Undo.RecordObject(c,"Dedicated hammer pointer target");c.enabled=false;}
  var body=hammer.GetComponent<Rigidbody>();if(body!=null){Undo.RecordObject(body,"Park wall hammer");body.isKinematic=true;body.useGravity=false;}
  Ref(t,"slot",slot);Ref(t,"cameraPoint",camera);Ref(t,"hammerHolder",holder);Ref(t,"hammerVisual",hammer.transform);Ref(t,"hammerReturnCollider",returnCollider);
  var contact=hammer.transform.Find("HammerImpactPoint");if(contact==null){contact=new GameObject("HammerImpactPoint").transform;contact.SetParent(hammer.transform,false);var mesh=hammer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var indices=mesh.GetTriangles(0);var bounds=new Bounds(vertices[indices[0]],Vector3.zero);foreach(var i in indices)bounds.Encapsulate(vertices[i]);contact.localPosition=new Vector3(bounds.max.x,bounds.center.y,bounds.center.z);contact.localRotation=Quaternion.LookRotation(Vector3.right,Vector3.up);}Ref(t,"hammerImpactPoint",contact);
  var grip=hammer.transform.Find("HammerGripPoint");if(grip==null){grip=new GameObject("HammerGripPoint").transform;grip.SetParent(hammer.transform,false);var mesh=hammer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var indices=mesh.GetTriangles(2);var bounds=new Bounds(vertices[indices[0]],Vector3.zero);foreach(var i in indices)bounds.Encapsulate(vertices[i]);grip.localPosition=bounds.center;}Ref(t,"hammerGripPoint",grip);
  camera.SetPositionAndRotation(new Vector3(-1.13f,1.85f,10.05f),Quaternion.LookRotation(new Vector3(0,-.57f,1.15f)));
  Ref(t,"playerCamera",Camera.main);Ref(t,"player",UnityEngine.Object.FindAnyObjectByType<TidyUpFramework.Player.TidyFirstPersonController>());Ref(t,"interactor",UnityEngine.Object.FindAnyObjectByType<TidyUpFramework.Interaction.TidyInteractor>());
  var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();if(pc!=null){var source=new SerializedObject(pc).FindProperty("cameraDrivers");var so=new SerializedObject(t);var dest=so.FindProperty("cameraDrivers");dest.arraySize=source.arraySize;for(int i=0;i<source.arraySize;i++)dest.GetArrayElementAtIndex(i).objectReferenceValue=source.GetArrayElementAtIndex(i).objectReferenceValue;so.ApplyModifiedPropertiesWithoutUndo();}
  if(table.transform.Find("ChippingHUD")==null)
  {
   var hud=new GameObject("ChippingHUD",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));hud.transform.SetParent(table.transform,false);hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;hud.GetComponent<Canvas>().sortingOrder=150;
   var scaler=hud.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
   var panel=new GameObject("ProgressPanel",typeof(RectTransform),typeof(UnityEngine.UI.Image));panel.transform.SetParent(hud.transform,false);var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-24);rect.sizeDelta=new Vector2(650,145);panel.GetComponent<UnityEngine.UI.Image>().color=new Color(.035f,.065f,.09f,.94f);panel.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
   var label=new GameObject("ProgressText",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(panel.transform,false);var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(22,27);lr.offsetMax=new Vector2(-22,-12);var text=label.GetComponent<TextMeshProUGUI>();text.font=UnityEngine.Object.FindAnyObjectByType<TMP_Text>().font;text.fontSize=25;text.color=new Color(.84f,.95f,.92f);text.raycastTarget=false;
   var bar=new GameObject("ProgressFill",typeof(RectTransform),typeof(UnityEngine.UI.Image));bar.transform.SetParent(panel.transform,false);var br=bar.GetComponent<RectTransform>();br.anchorMin=new Vector2(0,0);br.anchorMax=new Vector2(1,0);br.offsetMin=new Vector2(22,12);br.offsetMax=new Vector2(-22,20);var image=bar.GetComponent<UnityEngine.UI.Image>();image.sprite=UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;image.fillAmount=0;image.color=new Color(.2f,.85f,.65f);image.raycastTarget=false;
   Ref(t,"progressPanel",panel);Ref(t,"progressLabel",text);Ref(t,"progressFill",image);panel.SetActive(false);
  }
  foreach(var hudCanvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))if(hudCanvas.name=="TidyHUD")Ref(t,"gameplayHud",hudCanvas);
  var panelRect=table.transform.Find("ChippingHUD/ProgressPanel").GetComponent<RectTransform>();panelRect.anchorMin=panelRect.anchorMax=new Vector2(.5f,0);panelRect.pivot=new Vector2(.5f,0);panelRect.anchoredPosition=new Vector2(0,24);
  StoneGame.Chipping.Editor.StoneCutMaterialReferencesEditor.SyncLoadedSaws();
  EditorSceneManager.MarkSceneDirty(table.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(table.scene);return new{table=table.name,hammer=hammer.name,slot=slot.TargetId};
 }
}





