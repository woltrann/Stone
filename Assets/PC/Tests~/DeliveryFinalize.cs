using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using StoneGame.Delivery;
using StoneGame.Economy;
using TidyUpFramework.Levels;
using TidyUpFramework.Save;
using TidyUpFramework.Settings;
public static class DeliveryFinalize
{
 [Serializable]public class Baseline{public bool saveProgress;public string levelId,levelJson,progressJson,economyPath,economyJson,bootstrapPath,testPath;public Vector3 pointPosition;public Quaternion pointRotation,carRotation;}
 static string Hash(string value){if(value==null)return "absent";using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value)));}
 public static object Main()
 {
  if(Application.isPlaying)throw new Exception("Stop test Play Mode before finalizing");
  var before=JsonUtility.FromJson<Baseline>(File.ReadAllText("Assets/PC/Tests~/DeliveryBaseline.json"));var settings=Resources.Load<TidyFrameworkSettings>(TidyFrameworkSettings.ResourcesPath);
  ITidySaveStorage storage=settings.SaveStorage==TidySaveStorageKind.JsonFile?(ITidySaveStorage)new TidyJsonFileStorage(settings.SaveFolderName):new TidyPlayerPrefsStorage(settings.SaveFolderName+".");storage.TryRead("level_"+before.levelId,out var levelJson);storage.TryRead("progress",out var progress);
  string economy=File.Exists(before.economyPath)?File.ReadAllText(before.economyPath):null;
  var level=UnityEngine.Object.FindAnyObjectByType<TidyLevelManager>();var ls=new SerializedObject(level);ls.FindProperty("saveProgress").boolValue=before.saveProgress;ls.ApplyModifiedPropertiesWithoutUndo();
  var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();var bs=new SerializedObject(bootstrap);bs.FindProperty("storagePathOverride").stringValue=before.bootstrapPath;bs.ApplyModifiedPropertiesWithoutUndo();
  var manager=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var ms=new SerializedObject(manager);
  int missing=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(manager.gameObject);
  bool config=ms.FindProperty("travelDuration").floatValue==3 && ms.FindProperty("waitDuration").floatValue==2 && ms.FindProperty("spawnInterval").floatValue==.15f && ms.FindProperty("startPosition").vector3Value==new Vector3(50,1,15) && ms.FindProperty("destination").vector3Value==new Vector3(-7,1,15);
  var result=new Audit{economyUnchanged=economy==before.economyJson,levelUnchanged=levelJson==before.levelJson,progressUnchanged=progress==before.progressJson,economyBefore=Hash(before.economyJson),economyAfter=Hash(economy),levelBefore=Hash(before.levelJson),levelAfter=Hash(levelJson),progressBefore=Hash(before.progressJson),progressAfter=Hash(progress),pointUnchanged=manager.SpawnPoint.position==before.pointPosition && Quaternion.Angle(manager.SpawnPoint.rotation,before.pointRotation)<.0001f,carRotationUnchanged=Quaternion.Angle(manager.Car.rotation,before.carRotation)<.0001f,defaultSettingsCorrect=config,saveFlagRestored=level.SavesProgress==before.saveProgress,overrideRestored=bs.FindProperty("storagePathOverride").stringValue==before.bootstrapPath,missingScripts=missing,temporaryEditorStones=UnityEngine.Object.FindObjectsByType<DeliveredStoneInstance>(FindObjectsInactive.Include).Length,temporaryTruckRigidbody=manager.Car.GetComponent<Rigidbody>()!=null};
  if(!result.economyUnchanged || !result.levelUnchanged || !result.progressUnchanged || !result.pointUnchanged || !config)throw new Exception("Final safety audit failed; inspect baseline before further writes");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);result.sceneSaved=EditorSceneManager.SaveScene(manager.gameObject.scene);Selection.activeGameObject=manager.gameObject;
  File.WriteAllText("Assets/PC/Documentation~/Delivery-final-audit.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable]public class Audit{public bool economyUnchanged,levelUnchanged,progressUnchanged,pointUnchanged,carRotationUnchanged,defaultSettingsCorrect,saveFlagRestored,overrideRestored,sceneSaved,temporaryTruckRigidbody;public int missingScripts,temporaryEditorStones;public string economyBefore,economyAfter,levelBefore,levelAfter,progressBefore,progressAfter;}
}
