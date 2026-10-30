using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using StoneGame.Computers;
using StoneGame.Computers.UI;
using StoneGame.Economy;
using TidyUpFramework.Levels;
using TidyUpFramework.Save;
using TidyUpFramework.Settings;
public static class MarketFinalize
{
 [Serializable] public class Baseline{public bool saveProgress;public string levelId,level,progress,testPath;}
 static string Hash(string text){if(text==null)return "absent";using(var h=SHA256.Create())return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes(text)));}
 public static object Main()
 {
  if(Application.isPlaying)throw new Exception("Stop test Play Mode first.");
  var baseline=JsonUtility.FromJson<Baseline>(File.ReadAllText("Assets/PC/Tests~/MarketTestBaseline.json"));var fs=Resources.Load<TidyFrameworkSettings>(TidyFrameworkSettings.ResourcesPath);
  ITidySaveStorage storage=fs.SaveStorage==TidySaveStorageKind.JsonFile?(ITidySaveStorage)new TidyJsonFileStorage(fs.SaveFolderName):new TidyPlayerPrefsStorage(fs.SaveFolderName+".");storage.TryRead("level_"+baseline.levelId,out var level);storage.TryRead("progress",out var progress);
  var m=UnityEngine.Object.FindAnyObjectByType<TidyLevelManager>();var ms=new SerializedObject(m);ms.FindProperty("saveProgress").boolValue=baseline.saveProgress;ms.ApplyModifiedPropertiesWithoutUndo();
  var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();var bs=new SerializedObject(bootstrap);bs.FindProperty("storagePathOverride").stringValue=string.Empty;bs.ApplyModifiedPropertiesWithoutUndo();
  var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();var view=pc.Desktop.GetComponent<StoneMarketView>();
  int missing=0;foreach(var t in pc.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
  EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);AssetDatabase.SaveAssets();bool saved=EditorSceneManager.SaveScene(pc.gameObject.scene);Selection.activeGameObject=pc.gameObject;
  var audit=new Audit{levelSaveUnchanged=level==baseline.level,progressSaveUnchanged=progress==baseline.progress,levelBefore=Hash(baseline.level),levelAfter=Hash(level),progressBefore=Hash(baseline.progress),progressAfter=Hash(progress),sceneSaved=saved,realSaveSettingRestored=m.SavesProgress==baseline.saveProgress,testPathOverrideRemoved=string.IsNullOrEmpty(bs.FindProperty("storagePathOverride").stringValue),missingScripts=missing,catalogErrors=view.Catalog.ValidateCatalog().ToArray(),productionEconomyPath=Path.Combine(Application.persistentDataPath,fs.SaveFolderName,"stone-market.json")};
  File.WriteAllText("Assets/PC/Documentation~/Market-final-audit.json",JsonUtility.ToJson(audit,true));return audit;
 }
 [Serializable] public class Audit{public bool levelSaveUnchanged,progressSaveUnchanged,sceneSaved,realSaveSettingRestored,testPathOverrideRemoved;public string levelBefore,levelAfter,progressBefore,progressAfter,productionEconomyPath;public int missingScripts;public string[] catalogErrors;}
}
