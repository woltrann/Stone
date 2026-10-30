using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using StoneGame.Computers;
using TidyUpFramework.Levels;
using TidyUpFramework.Save;
using TidyUpFramework.Settings;
public static class PCFinalize
{
 [Serializable] public class Baseline{public bool saveProgress;public string levelId;public string level;public string progress;public bool runInBackground;}
 static string Hash(string s){if(s==null)return "absent";using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-","").ToLowerInvariant();}
 public static object Main()
 {
  if(Application.isPlaying)throw new Exception("Stop the owned test session first.");
  var baseline=JsonUtility.FromJson<Baseline>(File.ReadAllText("Assets/PC/Tests~/SaveBaseline.json"));
  var fs=AssetDatabase.FindAssets("t:TidyFrameworkSettings").Select(g=>AssetDatabase.LoadAssetAtPath<TidyFrameworkSettings>(AssetDatabase.GUIDToAssetPath(g))).First();
  ITidySaveStorage storage=fs.SaveStorage==TidySaveStorageKind.JsonFile?(ITidySaveStorage)new TidyJsonFileStorage(fs.SaveFolderName):new TidyPlayerPrefsStorage(fs.SaveFolderName+".");
  storage.TryRead("level_"+baseline.levelId,out var level);storage.TryRead("progress",out var progress);
  bool sameLevel=level==baseline.level,sameProgress=progress==baseline.progress;
  var manager=UnityEngine.Object.FindAnyObjectByType<TidyLevelManager>();var s=new SerializedObject(manager);s.FindProperty("saveProgress").boolValue=baseline.saveProgress;s.ApplyModifiedPropertiesWithoutUndo();
  var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();int missing=pc.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
  EditorUtility.SetDirty(pc.Settings);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
  bool saved=EditorSceneManager.SaveScene(pc.gameObject.scene);Selection.activeGameObject=pc.gameObject;
  var result=new Audit{levelSaveUnchanged=sameLevel,progressSaveUnchanged=sameProgress,levelBeforeSHA256=Hash(baseline.level),levelAfterSHA256=Hash(level),progressBeforeSHA256=Hash(baseline.progress),progressAfterSHA256=Hash(progress),saveProgressRestored=manager.SavesProgress==baseline.saveProgress,sceneSaved=saved,scene=pc.gameObject.scene.path,missingScripts=missing,screenExcludedFromStaticBatch=(GameObjectUtility.GetStaticEditorFlags(pc.ScreenRenderer.gameObject)&StaticEditorFlags.BatchingStatic)==0};
  File.WriteAllText("Assets/PC/Documentation~/PC-final-audit.json",JsonUtility.ToJson(result,true));return result;
 }
 [Serializable] public class Audit {public bool levelSaveUnchanged,progressSaveUnchanged,saveProgressRestored,sceneSaved,screenExcludedFromStaticBatch;public string levelBeforeSHA256,levelAfterSHA256,progressBeforeSHA256,progressAfterSHA256,scene;public int missingScripts;}
}
