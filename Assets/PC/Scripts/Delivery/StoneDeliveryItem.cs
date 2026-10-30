using System;
using System.Collections.Generic;
using StoneGame.Orders;
using TidyUpFramework.Objects;
using TidyUpFramework.Save;
using UnityEngine;
namespace StoneGame.Delivery
{
    // Stored in the existing atomic economy document, never in source ScriptableObjects.
    [Serializable]
    public sealed class StoneDeliveryItem
    {
        public string orderId,sceneName;
        public int index;
        public bool confirmed,falling;
        public Vector3 localScale=Vector3.one;
        public TidyObjectSnapshot source;
        public List<TidyObjectSnapshot> pieces=new List<TidyObjectSnapshot>();
        public StoneDeliveryItem Clone()
        {
            var copy=(StoneDeliveryItem)MemberwiseClone();copy.source=CopyPose(source);
            copy.pieces=new List<TidyObjectSnapshot>(pieces?.Count??0);
            if(pieces!=null)foreach(var piece in pieces)copy.pieces.Add(CopyPose(piece));return copy;
        }
        static TidyObjectSnapshot CopyPose(TidyObjectSnapshot p)=>p==null?null:new TidyObjectSnapshot
        {Id=p.Id,State=p.State,TargetId=p.TargetId,TargetOrder=p.TargetOrder,HasPose=p.HasPose,Position=p.Position,Rotation=p.Rotation,PickedUp=p.PickedUp,Mistakes=p.Mistakes,CarryOrder=p.CarryOrder,CustomState=p.CustomState};
        internal bool SameWorldState(StoneDeliveryItem other)
        {
            if(other==null || orderId!=other.orderId || index!=other.index || sceneName!=other.sceneName || falling!=other.falling || !localScale.Equals(other.localScale) || !SamePose(source,other.source) || pieces.Count!=other.pieces.Count)return false;
            for(int i=0;i<pieces.Count;i++)if(!SamePose(pieces[i],other.pieces[i]))return false;return true;
        }
        static bool SamePose(TidyObjectSnapshot a,TidyObjectSnapshot b)=>ReferenceEquals(a,b) || (a!=null && b!=null &&
            a.Id==b.Id && a.State==b.State && a.TargetId==b.TargetId && a.TargetOrder==b.TargetOrder && a.HasPose==b.HasPose &&
            a.Position.Equals(b.Position) && a.Rotation.Equals(b.Rotation) && a.PickedUp==b.PickedUp && a.Mistakes==b.Mistakes && a.CarryOrder==b.CarryOrder && a.CustomState==b.CustomState);
        internal bool Valid(StoneOrder order)
        {
            if(order==null || index<0 || index>=order.Quantity || source==null || source.Id!=order.ItemInstanceId(index) ||
                string.IsNullOrEmpty(sceneName) || (!string.IsNullOrEmpty(order.DeliveryScene) && order.DeliveryScene!=sceneName) ||
                !Finite(localScale) || localScale.x<=0 || localScale.y<=0 || localScale.z<=0 || !ValidPose(source))return false;
            if(pieces==null)pieces=new List<TidyObjectSnapshot>();
            var ids=new HashSet<string>();foreach(var piece in pieces)
                if(piece==null || (piece.Id!=source.Id+"_left" && piece.Id!=source.Id+"_right") || !ids.Add(piece.Id) || !ValidPose(piece))return false;
            return true;
        }
        static bool ValidPose(TidyObjectSnapshot pose)=>pose!=null && Enum.IsDefined(typeof(TidyObjectState),pose.State) &&
            Finite(pose.Position) && Finite(new Vector3(pose.Rotation.x,pose.Rotation.y,pose.Rotation.z)) && !float.IsNaN(pose.Rotation.w) && !float.IsInfinity(pose.Rotation.w);
        static bool Finite(Vector3 v)=>!float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }
}
