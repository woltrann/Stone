using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using StoneGame.Chipping;
using StoneGame.Cutting;
public static class UserStoneCoverageProbe
{
 public static object Proposed()=>Run(true);
 [Serializable]public class Report{public string stone;public int meshPixels,acceptedPixels,capPixels;public string[] blockers;public Vector2 screenSize;}
 public static object Main()=>Run(false);
 static object Run(bool proposed)
 {
  var table=UnityEngine.Object.FindAnyObjectByType<StoneChippingTable>();var stone=table.ActiveStone;if(stone==null)throw new Exception("Place user's stone on table first");var camera=Camera.main;var bounds=stone.GetComponent<Renderer>().bounds;
  var low=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var high=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
  for(int i=0;i<8;i++){var p=camera.WorldToScreenPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));low=Vector2.Min(low,new Vector2(p.x,p.y));high=Vector2.Max(high,new Vector2(p.x,p.y));}
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;var meshMethod=table.GetType().GetMethod("TryStoneMeshHit",flags);var worldMethod=table.GetType().GetMethod("TryPointerHit",flags);int mesh=0,accepted=0,cap=0;var blockers=new Dictionary<string,int>();
  for(int y=1;y<25;y++)for(int x=1;x<25;x++)
  {
   var ray=camera.ScreenPointToRay(new Vector2(Mathf.Lerp(low.x,high.x,x/25f),Mathf.Lerp(low.y,high.y,y/25f)));var args=new object[]{ray,null};
   if(!(bool)meshMethod.Invoke(table,args))continue;mesh++;var type=args[1].GetType();if((bool)type.GetField("CutFace").GetValue(args[1]))cap++;
   if(proposed)
   {
    var player=UnityEngine.Object.FindAnyObjectByType<TidyUpFramework.Player.TidyFirstPersonController>();float nearest=(float)type.GetField("Distance").GetValue(args[1]);Collider blocker=null;
    foreach(var h in Physics.RaycastAll(ray,5,~0,QueryTriggerInteraction.Collide))
    {if(h.collider.isTrigger || h.collider.transform.IsChildOf(player.transform) || h.collider.GetComponentInParent<CutStoneHalf>()==stone)continue;if(h.distance<nearest-.001f){nearest=h.distance;blocker=h.collider;}}
    if(blocker==null){accepted++;continue;}blockers.TryGetValue(blocker.name,out int previous);blockers[blocker.name]=previous+1;continue;
   }
   args=new object[]{ray,null};if((bool)worldMethod.Invoke(table,args) && (CutStoneHalf)type.GetField("Stone").GetValue(args[1])==stone){accepted++;continue;}
   var collider=(Collider)type.GetField("Collider").GetValue(args[1]);string name=collider!=null?collider.name:"none";blockers.TryGetValue(name,out int n);blockers[name]=n+1;
  }
  var rows=new List<string>();foreach(var pair in blockers)rows.Add(pair.Key+": "+pair.Value);
  var result=new Report{stone=stone.name,meshPixels=mesh,acceptedPixels=accepted,capPixels=cap,blockers=rows.ToArray(),screenSize=high-low};File.WriteAllText("Assets/TABLE/Chipping/Documentation~/"+(proposed?"User-stone-proposed-filter.json":"User-stone-coverage-before.json"),JsonUtility.ToJson(result,true));return result;
 }
}
