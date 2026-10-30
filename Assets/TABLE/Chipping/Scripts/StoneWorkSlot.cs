using StoneGame.Cutting;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Save;
using UnityEngine;
namespace StoneGame.Chipping
{
    public sealed class StoneWorkSlot:TidySnapSlot
    {
        [SerializeField] StoneChippingTable table;
        public CutStoneHalf Stone=>Occupants.Count==1 && Occupants[0]!=null && Occupants[0].State==TidyObjectState.Placed?Occupants[0].GetComponent<CutStoneHalf>():null;
        public override bool IsCompatible(TidyObject obj)=>base.IsCompatible(obj) && obj.GetComponent<CutStoneHalf>()!=null && (table==null || !table.IsInUse);
        public override bool AllowsRemoval(TidyObject obj)=>(table==null || !table.IsInUse) && base.AllowsRemoval(obj);
        protected override void Awake(){base.Awake();TidyLevelSnapshotter.Restored+=AfterRestore;}
        protected override void OnDestroy(){TidyLevelSnapshotter.Restored-=AfterRestore;base.OnDestroy();}
        void AfterRestore(TidyLevelSnapshot snapshot)
        {
            var stone=Stone;if(stone==null || stone.CutSurfaceNormal.sqrMagnitude<.01f)return;
            var obj=stone.GetComponent<TidyObject>();
            if(TryFindPose(TidyPlacementQuery.WithoutAim(obj),out var position,out var rotation,out _))
            {obj.transform.SetPositionAndRotation(position,rotation);if(obj.Body!=null){obj.Body.position=position;obj.Body.rotation=rotation;}}
        }
        protected override bool TryFindPose(TidyPlacementQuery query,out Vector3 position,out Quaternion rotation,out int slotIndex)
        {
            var half=query.Object.GetComponent<CutStoneHalf>();
            if(half==null || half.CutSurfaceNormal.sqrMagnitude<.01f)return base.TryFindPose(query,out position,out rotation,out slotIndex);
            var scale=query.Object.transform.lossyScale;
            var normal=new Vector3(half.CutSurfaceNormal.x/scale.x,half.CutSurfaceNormal.y/scale.y,half.CutSurfaceNormal.z/scale.z).normalized;
            rotation=transform.rotation*Quaternion.FromToRotation(normal,Vector3.down);
            // Seat the actual cut plane, rather than an unrelated local-Y bounds corner.
            position=transform.position-rotation*Vector3.Scale(half.CutSurfaceCenter,scale);slotIndex=0;return true;
        }
    }
}
