using System.Collections.Generic;
using TidyUpFramework.Objects;
using UnityEngine;
namespace StoneGame.Delivery
{
    [DisallowMultipleComponent]
    public sealed class DeliveredStoneInstance : MonoBehaviour
    {
        public string OrderId{get;private set;}
        public int Index{get;private set;}
        public bool Falling{get;private set;}
        public StoneDeliveryManager Owner{get;private set;}
        TidyObject tidy;
        Rigidbody body;
        readonly List<KeyValuePair<Collider,Collider>> ignored=new List<KeyValuePair<Collider,Collider>>();
        public void Bind(StoneDeliveryManager owner,string orderId,int index,bool falling)
        {Owner=owner;OrderId=orderId;Index=index;Falling=falling;tidy=GetComponent<TidyObject>();body=GetComponent<Rigidbody>();}
        void OnEnable(){tidy=GetComponent<TidyObject>();body=GetComponent<Rigidbody>();tidy.StateChanged+=StateChanged;}
        void StateChanged(TidyObject obj)
        {
            if(obj.State!=TidyObjectState.Loose){Falling=false;RestoreCollisions();}
            else if(Falling)ApplyGravity();
            Owner?.MarkWorldDirty(obj);
        }
        public void ResumeFall(bool falling)
        {Falling=falling && tidy.State==TidyObjectState.Loose;if(Falling)ApplyGravity();}
        void ApplyGravity()
        {
            // Only the initial spill overrides Controlled mode. Held/drop/placement remain framework-owned.
            body.isKinematic=false;body.useGravity=true;body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.WakeUp();
        }
        public void IgnoreInitialOverlap(Collider other)
        {
            foreach(var own in tidy.Colliders)
            {
                if(own==null || other==null || own==other || Physics.GetIgnoreCollision(own,other))continue;
                Physics.IgnoreCollision(own,other,true);ignored.Add(new KeyValuePair<Collider,Collider>(own,other));
            }
        }
        void FixedUpdate()
        {
            for(int i=ignored.Count-1;i>=0;i--)
            {
                var a=ignored[i].Key;var b=ignored[i].Value;
                if(a==null || b==null || !Falling || !Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out _))
                {if(a!=null && b!=null)Physics.IgnoreCollision(a,b,false);ignored.RemoveAt(i);}
            }
        }
        void RestoreCollisions(){foreach(var pair in ignored)if(pair.Key!=null && pair.Value!=null)Physics.IgnoreCollision(pair.Key,pair.Value,false);ignored.Clear();}
        void OnDisable(){if(tidy!=null)tidy.StateChanged-=StateChanged;RestoreCollisions();}
    }
}
