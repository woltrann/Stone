using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using StoneGame.Chipping;
using StoneGame.Cutting;
public static class TABLEFolderAudit
{
 [Serializable]public class Result{public bool tableFolderExists,oldFolderGone,tableGuidPreserved,slotGuidPreserved,tableSettingsPreserved,slotSettingsPreserved,fallbackMaterialsRegistered;public int missingReferences,missingScripts;public string[] references;}
 public static object Main()
 {
  if(Application.isPlaying)throw new Exception("Edit mode required");
  var table=UnityEngine.Object.FindAnyObjectByType<StoneChippingTable>();var slot=UnityEngine.Object.FindAnyObjectByType<StoneWorkSlot>();var so=new SerializedObject(table);var refs=new List<string>();int missing=0;
  foreach(var field in new[]{"slot","cameraPoint","hammerHolder","hammerVisual","hammerReturnCollider","playerCamera","player","interactor","progressPanel","progressLabel","progressFill","gameplayHud"})
  {var value=so.FindProperty(field).objectReferenceValue;if(value==null)missing++;refs.Add(field+"="+(value!=null?GlobalObjectId.GetGlobalObjectIdSlow(value).ToString():"NULL"));}
  string root="Assets/TABLE/Chipping/Documentation~/";
  var saw=UnityEngine.Object.FindAnyObjectByType<StoneSawMachine>();var sawSO=new SerializedObject(saw);var previous=sawSO.FindProperty("cutSurfaceMaterial").objectReferenceValue;bool fallback=true;
  try
  {
   sawSO.FindProperty("cutSurfaceMaterial").objectReferenceValue=null;sawSO.ApplyModifiedPropertiesWithoutUndo();StoneGame.Chipping.Editor.StoneCutMaterialReferencesEditor.SyncLoadedSaws();
   foreach(var raw in Resources.FindObjectsOfTypeAll<RawStone>())foreach(var renderer in raw.GetComponentsInChildren<MeshRenderer>(true))foreach(var m in renderer.sharedMaterials)if(m!=null && string.IsNullOrEmpty(StoneCutMaterialReferences.IdFor(m)))fallback=false;
  }
  finally{sawSO.Update();sawSO.FindProperty("cutSurfaceMaterial").objectReferenceValue=previous;sawSO.ApplyModifiedPropertiesWithoutUndo();}
  int missingScripts=0;foreach(var tr in table.GetComponentsInChildren<Transform>(true))missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);
  var result=new Result{tableFolderExists=AssetDatabase.IsValidFolder("Assets/TABLE"),oldFolderGone=!AssetDatabase.IsValidFolder("Assets/STONE/Chipping"),tableGuidPreserved=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(table)))=="99a162027e764c340976a185217f44f2",slotGuidPreserved=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(slot)))=="04ad821a0bdc90642ad8caa53f634014",tableSettingsPreserved=EditorJsonUtility.ToJson(table,true)==File.ReadAllText(root+"TABLE-settings-before.json"),slotSettingsPreserved=EditorJsonUtility.ToJson(slot,true)==File.ReadAllText(root+"TABLE-slot-before.json"),fallbackMaterialsRegistered=fallback,missingReferences=missing,missingScripts=missingScripts,references=refs.ToArray()};
  if(!result.tableFolderExists || !result.oldFolderGone || !result.tableGuidPreserved || !result.slotGuidPreserved || !result.tableSettingsPreserved || !result.slotSettingsPreserved || !fallback || missing>0 || missingScripts>0)throw new Exception("TABLE audit failed: "+JsonUtility.ToJson(result));
  AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(table.gameObject.scene);File.WriteAllText(root+"TABLE-folder-audit.json",JsonUtility.ToJson(result,true));return result;
 }
}
