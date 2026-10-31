using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using StoneGame.Economy;
using TidyUpFramework.Levels;
using TidyUpFramework.Settings;
using TidyUpFramework.Save;
public static class ChipSandbox
{
 [Serializable]public class State{public bool saves,dirty;public string path,test,economy,economyData,levelId,levelData,progress;}
 const string FilePath="Assets/TABLE/Chipping/Tests~/ChipSandbox.json";
 static ITidySaveStorage Storage(TidyFrameworkSettings s)=>s.SaveStorage==TidySaveStorageKind.JsonFile?(ITidySaveStorage)new TidyJsonFileStorage(s.SaveFolderName):new TidyPlayerPrefsStorage(s.SaveFolderName+".");
 public static object Main()
 {
  if(Application.isPlaying || File.Exists(FilePath))throw new Exception("Clean edit mode required");
  var b=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();var l=UnityEngine.Object.FindAnyObjectByType<TidyLevelManager>();var settings=Resources.Load<TidyFrameworkSettings>(TidyFrameworkSettings.ResourcesPath);var bs=new SerializedObject(b);
  var s=new State{saves=l.SavesProgress,path=bs.FindProperty("storagePathOverride").stringValue,test=Path.GetFullPath("Assets/TABLE/Chipping/Tests~/ChipEconomy.json"),dirty=l.gameObject.scene.isDirty,levelId=l.LevelId,economy=Path.Combine(Application.persistentDataPath,settings.SaveFolderName,"stone-market.json")};
  s.economyData=File.Exists(s.economy)?File.ReadAllText(s.economy):null;var storage=Storage(settings);storage.TryRead("level_"+s.levelId,out s.levelData);storage.TryRead("progress",out s.progress);File.WriteAllText(FilePath,JsonUtility.ToJson(s));
  bs.FindProperty("storagePathOverride").stringValue=s.test;bs.ApplyModifiedPropertiesWithoutUndo();var ls=new SerializedObject(l);ls.FindProperty("saveProgress").boolValue=false;ls.ApplyModifiedPropertiesWithoutUndo();return new{isolated=true,scenePreviouslyDirty=s.dirty};
 }
 public static object Finish()
 {
  if(Application.isPlaying)throw new Exception("Stop own test session first");var s=JsonUtility.FromJson<State>(File.ReadAllText(FilePath));var b=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();var l=UnityEngine.Object.FindAnyObjectByType<TidyLevelManager>();var settings=Resources.Load<TidyFrameworkSettings>(TidyFrameworkSettings.ResourcesPath);var storage=Storage(settings);storage.TryRead("level_"+s.levelId,out var level);storage.TryRead("progress",out var progress);
  var bs=new SerializedObject(b);bs.FindProperty("storagePathOverride").stringValue=s.path;bs.ApplyModifiedPropertiesWithoutUndo();var ls=new SerializedObject(l);ls.FindProperty("saveProgress").boolValue=s.saves;ls.ApplyModifiedPropertiesWithoutUndo();
  bool unchanged=level==s.levelData && progress==s.progress && (File.Exists(s.economy)?File.ReadAllText(s.economy):null)==s.economyData;
  if(!unchanged)throw new Exception("Real save audit failed");AssetDatabase.SaveAssets();if(!s.dirty)EditorSceneManager.SaveScene(l.gameObject.scene);
  File.Delete(FilePath);foreach(var p in new[]{s.test,s.test+".bak",s.test+".tmp"})if(File.Exists(p))File.Delete(p);
  return new{realSavesUnchanged=unchanged,originalSettingsRestored=true};
 }
}
