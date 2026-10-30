using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using StoneGame.Computers.UI;
using StoneGame.Delivery;
using StoneGame.Economy;
using TidyUpFramework.Levels;
using TidyUpFramework.Settings;
using TidyUpFramework.Save;
public static class DeliverySetup
{
 static void Ref(UnityEngine.Object o,string name,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 public static object Main()
 {
  if(Application.isPlaying)throw new Exception("Edit mode required");
  Directory.CreateDirectory("Assets/PC/Scripts/Delivery");Directory.CreateDirectory("Assets/PC/Prefabs/Delivery");AssetDatabase.Refresh();
  var car=GameObject.Find("Car");var pipe=GameObject.Find("Boru");var point=pipe!=null?pipe.transform.Find("Point"):null;
  if(car==null || point==null)throw new Exception("Car or Boru/Point missing");
  var bootstrap=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>();var level=UnityEngine.Object.FindAnyObjectByType<TidyLevelManager>();var framework=Resources.Load<TidyFrameworkSettings>(TidyFrameworkSettings.ResourcesPath);
  string prod=Path.Combine(Application.persistentDataPath,framework.SaveFolderName,"stone-market.json");
  const string baselinePath="Assets/PC/Tests~/DeliveryBaseline.json";
  if(!File.Exists(baselinePath))
  {
   ITidySaveStorage storage=framework.SaveStorage==TidySaveStorageKind.JsonFile?(ITidySaveStorage)new TidyJsonFileStorage(framework.SaveFolderName):new TidyPlayerPrefsStorage(framework.SaveFolderName+".");storage.TryRead("level_"+level.LevelId,out var levelJson);storage.TryRead("progress",out var progress);
   var bs=new SerializedObject(bootstrap);
   var data=new Baseline{saveProgress=level.SavesProgress,levelId=level.LevelId,levelJson=levelJson,progressJson=progress,economyPath=prod,economyJson=File.Exists(prod)?File.ReadAllText(prod):null,pointPosition=point.position,pointRotation=point.rotation,carRotation=car.transform.rotation,bootstrapPath=bs.FindProperty("storagePathOverride").stringValue,testPath=Path.GetFullPath("Assets/PC/Tests~/DeliverySandbox_"+Guid.NewGuid().ToString("N")+".json")};
   File.WriteAllText(baselinePath,JsonUtility.ToJson(data,true));
   var service=new EconomyService(new EconomyFileStore(data.testPath),100000);if(!service.IsReady)throw new Exception(service.LoadError);
  }
  var before=JsonUtility.FromJson<Baseline>(File.ReadAllText(baselinePath));
  var levelSO=new SerializedObject(level);levelSO.FindProperty("saveProgress").boolValue=false;levelSO.ApplyModifiedPropertiesWithoutUndo();
  var bootSO=new SerializedObject(bootstrap);bootSO.FindProperty("storagePathOverride").stringValue=before.testPath;bootSO.ApplyModifiedPropertiesWithoutUndo();
  var delivery=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();if(delivery==null)delivery=new GameObject("Stone Delivery").AddComponent<StoneDeliveryManager>();
  Ref(delivery,"car",car.transform);Ref(delivery,"spawnPoint",point);Ref(delivery,"economySource",bootstrap);Ref(delivery,"catalog",UnityEngine.Object.FindAnyObjectByType<StoneMarketView>().Catalog);Ref(delivery,"levelManager",level);
  car.transform.position=new Vector3(50,1,15);EditorSceneManager.MarkSceneDirty(car.scene);
  return new {car=car.transform.position.ToString(),point=point.position.ToString(),pointUnchanged=point.position==before.pointPosition,testPath=before.testPath};
 }
 [Serializable]public class Baseline{public bool saveProgress;public string levelId,levelJson,progressJson,economyPath,economyJson,bootstrapPath,testPath;public Vector3 pointPosition;public Quaternion pointRotation,carRotation;}
}
