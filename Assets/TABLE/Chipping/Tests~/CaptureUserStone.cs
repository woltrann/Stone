using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using StoneGame.Chipping;
using StoneGame.Cutting;
using TidyUpFramework.Objects;
public static class CaptureUserStone
{
 [Serializable]public class Fixture{public string sourceId,activeId,sourceState,leftId,leftState,rightId,rightState,profilePath;public Vector3 playerPosition,sourcePosition,sourceScale;public Quaternion sourceRotation;public float yaw,pitch;}
 public static object Main()
 {
  var table=UnityEngine.Object.FindAnyObjectByType<StoneChippingTable>();var active=table.ActiveStone;if(active==null)throw new Exception("User stone missing");var source=UnityEngine.Object.FindObjectsByType<RawStone>(FindObjectsInactive.Include).Single(s=>s.GetComponent<TidyObject>().InstanceId==active.SourceStoneId);var halves=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Where(h=>h.SourceStoneId==active.SourceStoneId).ToArray();var player=UnityEngine.Object.FindAnyObjectByType<TidyUpFramework.Player.TidyFirstPersonController>();
  var f=new Fixture{sourceId=active.SourceStoneId,activeId=active.GetComponent<TidyObject>().InstanceId,sourceState=source.CaptureObjectState(),profilePath=AssetDatabase.GetAssetPath(source.CutProfile),sourcePosition=source.transform.position,sourceRotation=source.transform.rotation,sourceScale=source.transform.lossyScale,playerPosition=player.transform.position,yaw=player.Yaw,pitch=player.Pitch,leftId=halves[0].GetComponent<TidyObject>().InstanceId,leftState=halves[0].CaptureObjectState(),rightId=halves[1].GetComponent<TidyObject>().InstanceId,rightState=halves[1].CaptureObjectState()};
  File.WriteAllText("Assets/TABLE/Chipping/Tests~/UserStoneFixture.json",JsonUtility.ToJson(f));return new{source=source.name,active=active.name,profile=f.profilePath,player=f.playerPosition.ToString()};
 }
}
