using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Cutting;
using TidyUpFramework.Objects;
using TidyUpFramework.Player;
using TidyUpFramework.Interaction;
using TidyUpFramework.Save;

public static class RuntimeSliceSafetyChecks
{
 [Serializable] sealed class LegacyState { public bool rolled,processed; public string oreName; public Vector3 leftPosition,rightPosition,leftScale,rightScale; public Quaternion leftRotation,rightRotation; }
 static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static T Get<T>(object o,string field)=>(T)o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
 public static async Task<object> Main()
 {
  if(!Application.isPlaying) throw new Exception("Run in Play mode");
  var passed=new List<string>(); void Check(bool ok,string text) {if(!ok)throw new Exception(text);passed.Add(text);}
  var saw=UnityEngine.Object.FindAnyObjectByType<StoneSawMachine>();var slot=UnityEngine.Object.FindAnyObjectByType<StoneSawSlot>();
  var interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();var controller=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();
  var stone=UnityEngine.Object.FindObjectsByType<RawStone>().OrderBy(s=>s.name).First();var tidy=stone.GetComponent<TidyObject>();
  var before=new TidyLevelSnapshot();TidyLevelSnapshotter.CaptureWorld(before,interactor,controller);
  var plane=Get<Transform>(saw,"cuttingPlane"); var planePos=plane.position;var planeRot=plane.rotation;
  var right=Get<Transform>(saw,"rightOutputPoint"); float duration=Get<float>(saw,"cutDuration");
  var owned=new List<UnityEngine.Object>();
  async Task Finish()
  {var end=DateTime.UtcNow.AddSeconds(4);while(saw.IsCutting){if(DateTime.UtcNow>end)throw new Exception("Cut stalled");await Task.Delay(25);}await Task.Delay(60);}
  void Aim()
  {
   var approach=slot.transform.position+new Vector3(0,-slot.transform.position.y+.03f,-1.45f);controller.Teleport(approach,0,25);
   var dir=(slot.transform.position+Vector3.up*.08f-interactor.AimCamera.transform.position).normalized;
   controller.Teleport(approach,Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg,-Mathf.Asin(dir.y)*Mathf.Rad2Deg);
  }
  try
  {
   Set(saw,"cutDuration",.15f);Aim();await Task.Delay(100);Check(slot.TryPlaceDirect(tidy,false),"Input placed for safety checks");
   Set(saw,"cuttingPlane",null);Check(!saw.TryStartCut() && tidy.State==TidyObjectState.Placed && !stone.IsProcessed,"Missing plane rejects start and preserves source");Set(saw,"cuttingPlane",plane);
   plane.position=planePos+Vector3.right*20;
   Check(saw.TryStartCut(),"Outside-plane cut starts timed attempt");await Finish();
   Check(!stone.IsProcessed && stone.gameObject.activeSelf && slot.Occupants.Contains(tidy) && tidy.IsAvailableForPickup && saw.LastCutError!=null,"Nonintersecting cut retains and unlocks original");
   plane.position=planePos;Check(saw.TryStartCut(),"Valid cut starts after failure");Set(saw,"rightOutputPoint",null);await Finish();Set(saw,"rightOutputPoint",right);
   Check(!stone.IsProcessed && stone.gameObject.activeSelf && !UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Any(h=>h.SourceStoneId==tidy.InstanceId),"Failure after first output rolls back partial piece and preserves input");
   TidyLevelSnapshotter.RestoreWorld(before,interactor,controller);await Task.Delay(80);
   var legacy=new LegacyState {rolled=true,processed=true,oreName=(string)null,leftPosition=new Vector3(25,2,0),rightPosition=new Vector3(26,2,0),leftScale=new Vector3(.14f,.07f,.14f),rightScale=new Vector3(.14f,.07f,.14f),leftRotation=Quaternion.identity,rightRotation=Quaternion.identity};
   stone.RestoreObjectState(JsonUtility.ToJson(legacy));
   Check(stone.IsProcessed && UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Count(h=>h.SourceStoneId==tidy.InstanceId && h.GeneratedMesh==null)==2,"Legacy pre-mesh snapshot loads via retained compatibility assets");
   TidyLevelSnapshotter.RestoreWorld(before,interactor,controller);await Task.Delay(60);
   var corrupt=JsonUtility.FromJson<TidyLevelSnapshot>(JsonUtility.ToJson(before));
   var corruptEntry=corrupt.Objects.First(e=>e.Id==tidy.InstanceId);corruptEntry.State=(int)TidyObjectState.Consumed;corruptEntry.TargetId=null;
   corruptEntry.CustomState="{\"rolled\":true,\"processed\":true,\"oreName\":\"Komur\",\"meshVersion\":1,\"leftMesh\":{},\"rightMesh\":{}}";
   TidyLevelSnapshotter.RestoreWorld(corrupt,interactor,controller);
   Check(stone.RestoreFailed && stone.gameObject.activeSelf && tidy.State==TidyObjectState.Loose && stone.TryGetOre(out var recoveredOre) && recoveredOre!=null && recoveredOre.name=="Komur","Incomplete saved geometry restores raw source and preserves cached ore");
   corruptEntry.CustomState="{not valid json}";
   TidyLevelSnapshotter.RestoreWorld(corrupt,interactor,controller);
   Check(stone.RestoreFailed && stone.gameObject.activeSelf && tidy.State==TidyObjectState.Loose,"Malformed custom snapshot retains raw source instead of consuming it");
   TidyLevelSnapshotter.RestoreWorld(before,interactor,controller);await Task.Delay(80);

   var source=GameObject.CreatePrimitive(PrimitiveType.Cube);owned.Add(source);source.name="Runtime physics test source";source.transform.position=new Vector3(20,8,0);source.AddComponent<Rigidbody>().isKinematic=true;
   var sourceTidy=source.AddComponent<TidyObject>();sourceTidy.AssignInstanceId("runtime_physics_test");
   var physics=ScriptableObject.CreateInstance<TidyPhysicsProfile>();owned.Add(physics);physics.Configure(TidyInteractionMode.Physics,3);sourceTidy.AssignPhysicsProfileOverride(physics);
   var settings=new StoneMeshCutSettings();
   Check(StoneRuntimeMeshSlicer.TrySlice(sourceTidy,source.transform.position,Vector3.right,saw.CutSurfaceMaterial,settings,out var result,out var error),"Physics cube sliced: "+error);
   var pieces=new List<CutStoneHalf>();
   try
   {
    var a=StoneCutPieceFactory.Create(result.Negative,result.Materials,result.NegativeSurfaceCenter,result.Normal,result.NegativeRadius,result.NegativeVolume,new Vector3(19.5f,1.3f,0),Quaternion.identity,sourceTidy,stone.CutProfile,"left",null,settings,.5f);result.Negative=null;pieces.Add(a);owned.Add(a.gameObject);
    var b=StoneCutPieceFactory.Create(result.Positive,result.Materials,result.PositiveSurfaceCenter,-result.Normal,result.PositiveRadius,result.PositiveVolume,new Vector3(20.5f,1.3f,0),Quaternion.identity,sourceTidy,stone.CutProfile,"right",null,settings,.5f);result.Positive=null;pieces.Add(b);owned.Add(b.gameObject);
    Check(pieces.All(h=>h.GetComponent<MeshCollider>()!=null && h.GetComponent<MeshCollider>().convex),"Simple pieces use legal convex MeshColliders");
    Check(pieces.All(h=>!h.GetComponent<Rigidbody>().isKinematic) && Mathf.Abs(pieces.Sum(h=>h.GetComponent<Rigidbody>().mass)-3)<.001f,"Dynamic Rigidbody mode and total mass preserved");
    var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);owned.Add(floor);floor.transform.position=new Vector3(20,0,0);floor.transform.localScale=new Vector3(5,.1f,5);
    float startY=a.transform.position.y;await Task.Delay(800);
    Check(a.transform.position.y<startY && a.GetComponent<Collider>().bounds.min.y>.025f,"Dynamic cut piece falls and collides with floor");
    interactor.ForcePickUp(a.GetComponent<TidyObject>());Check(interactor.HeldObject==a.GetComponent<TidyObject>(),"Dynamic generated piece supports framework pickup");interactor.ForceDrop();await Task.Delay(100);
    Check(Mathf.Abs(a.GetComponent<Rigidbody>().mass-1.5f)<.001f,"Mass fraction survives framework pickup/drop transitions");
   }
   finally {result.Dispose();}
   var model=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/STONE/Art/Models/Stein.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
   source.GetComponent<MeshFilter>().sharedMesh=model;source.GetComponent<MeshRenderer>().sharedMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/STONE/Art/Materials/Rocks/New Material.mat");sourceTidy.RefreshCachedComponents();
   Check(StoneRuntimeMeshSlicer.TrySlice(sourceTidy,source.transform.position,Vector3.right,saw.CutSurfaceMaterial,settings,out result,out error),"Complex physics source sliced: "+error);
   try
   {
    var complex=StoneCutPieceFactory.Create(result.Negative,result.Materials,result.NegativeSurfaceCenter,result.Normal,result.NegativeRadius,result.NegativeVolume,new Vector3(23,5,0),Quaternion.identity,sourceTidy,stone.CutProfile,"complex",null,settings,.5f);result.Negative=null;owned.Add(complex.gameObject);
    Check(complex.GetComponents<BoxCollider>().Length>1 && complex.GetComponent<MeshCollider>()==null && !complex.GetComponent<Rigidbody>().isKinematic,"Complex dynamic geometry uses bounded compound boxes, no invalid concave MeshCollider");
   }
   finally {result.Dispose();}
   return new {passed,count=passed.Count};
  }
  finally
  {
   interactor.ForceDrop();saw.CancelCut();Set(saw,"cuttingPlane",plane);Set(saw,"rightOutputPoint",right);Set(saw,"cutDuration",duration);plane.SetPositionAndRotation(planePos,planeRot);
   TidyLevelSnapshotter.RestoreWorld(before,interactor,controller);
   foreach(var obj in owned) if(obj!=null){if(obj is GameObject go){var t=go.GetComponent<TidyObject>();if(t!=null)TidyObjectRegistry.Unregister(t);go.SetActive(false);}UnityEngine.Object.Destroy(obj);}
  }
 }
}
