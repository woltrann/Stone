using System;
using System.Collections;
using System.Collections.Generic;
using StoneGame.Cutting;
using StoneGame.Economy;
using StoneGame.Orders;
using StoneGame.Shop;
using TidyUpFramework.Core;
using TidyUpFramework.Interaction;
using TidyUpFramework.Levels;
using TidyUpFramework.Objects;
using TidyUpFramework.Player;
using TidyUpFramework.Save;
using UnityEngine;
using UnityEngine.Serialization;
namespace StoneGame.Delivery
{
    public enum StoneDeliveryPhase{Idle,Returning,Arriving,Waiting,Spawning,Faulted}
    [DefaultExecutionOrder(-15),DisallowMultipleComponent]
    public sealed class StoneDeliveryManager : MonoBehaviour
    {
        [Header("Existing scene and services")]
        [SerializeField] Transform car,spawnPoint;
        [SerializeField] StoneEconomyBootstrap economySource;
        [SerializeField] StoneMarketCatalog catalog;
        [SerializeField] TidyLevelManager levelManager;
        [Header("Truck movement (world space)")]
        [SerializeField] Vector3 startPosition=new Vector3(50,1,15),destination=new Vector3(-7,1,15);
        [SerializeField,Min(.01f)] float travelDuration=3,waitDuration=2,returnDuration=3;
        [SerializeField] bool returnAfterDelivery=true;
        [Header("Stone spill")]
        [SerializeField,Min(.01f)] float spawnInterval=.15f;
        [FormerlySerializedAs("spawnJitter")]
        [SerializeField,Min(0),Tooltip("World-space radius of the horizontal spawn disk centered on Point.")]
        float spawnRadius=.75f;
        [SerializeField,Min(.1f)] float blockedOutletTimeout=5,retryDelay=5,checkpointInterval=1;
        EconomyService economy;
        readonly List<string> queue=new List<string>();
        readonly HashSet<string> queued=new HashSet<string>();
        readonly Dictionary<string,DeliveredStoneInstance> instances=new Dictionary<string,DeliveredStoneInstance>();
        readonly HashSet<string> dirtySources=new HashSet<string>();
        readonly HashSet<string> previouslyMoving=new HashSet<string>();
        EconomySaveOperation checkpointWrite;
        readonly Collider[] overlaps=new Collider[128];
        Coroutine routine;
        GameObject staging;
        Rigidbody carBody;
        bool carOwned,bodyKinematic,bodyGravity,restoring,initialized,quitting,worldDirty;
        RigidbodyInterpolation bodyInterpolation;
        CollisionDetectionMode bodyCollision;
        bool moving;
        Vector3 motionStart,motionEnd;
        double motionTime;
        float motionDuration,nextRetry,nextCheckpoint;
        public StoneDeliveryPhase Phase{get;private set;}
        public string CurrentOrderId{get;private set;}
        public string LastError{get;private set;}
        public int QueueCount=>queue.Count;
        public Transform Car=>car;
        public Transform SpawnPoint=>spawnPoint;
        public float SpawnRadius=>Mathf.Max(0,spawnRadius);
        public Vector3 SampleSpawnPosition()
        {
            if(spawnPoint==null)throw new InvalidOperationException("Spawn Point reference is missing.");
            var offset=UnityEngine.Random.insideUnitCircle*SpawnRadius;
            return spawnPoint.position+new Vector3(offset.x,0,offset.y);
        }
        public StoneMarketCatalog Catalog=>catalog;
        public double TripStartedAt{get;private set;}
        public double ArrivedAt{get;private set;}
        public double FirstSpawnAt{get;private set;}
        public event Action<string,int,GameObject> StoneSpawned;
        string SceneKey=>gameObject.scene.name;
        void OnEnable(){if(!Application.isPlaying)return;TidyLevelSnapshotter.Restoring+=BeforeRestore;TidyLevelSnapshotter.Restored+=AfterRestore;TidyObjectRegistry.AnyStateChanged+=MarkWorldDirty;StartCoroutine(Initialize());}
        IEnumerator Initialize()
        {
            while(!Connect())yield return null;
            while(levelManager!=null && (!levelManager.IsPlaying || levelManager.IsRestoring || levelManager.HasPendingRestore))yield return null;
            if(!initialized)
            {
                var snapshot=new TidyLevelSnapshot();
                TidyLevelSnapshotter.RestoreWorld(snapshot,null,null);
            }
            initialized=true;EnqueueOutstanding();nextCheckpoint=Time.time+checkpointInterval;
        }
        bool Connect()
        {
            if(economy!=null)return economy.IsReady;
            if(economySource!=null)economy=economySource.Service;
            if(economy==null)TidyServices.TryGet(out economy);
            if(economy==null)return false;
            economy.OrderPlaced+=OrderPlaced;economy.NewGameStarted+=NewGame;
            return economy.IsReady;
        }
        void OrderPlaced(StoneOrder order){TryQueueOrder(order.OrderId);}
        void EnqueueOutstanding(){if(economy==null)return;foreach(var order in economy.Orders)TryQueueOrder(order.OrderId);}
        public bool TryQueueOrder(string id)
        {
            var order=economy?.FindOrder(id);
            if(order==null || (order.Status!=StoneOrderStatus.Pending && order.Status!=StoneOrderStatus.Processing) ||
                (!string.IsNullOrEmpty(order.DeliveryScene) && order.DeliveryScene!=SceneKey))return false;
            if(queued.Add(id))queue.Add(id);return true;
        }
        void Update()
        {
            economy?.PumpWrites();
            if(!initialized || restoring || economy==null || !economy.IsReady || Time.timeScale<=0)return;
            if(routine==null && queue.Count>0 && Time.time>=nextRetry)
            {
                var order=economy.FindOrder(queue[0]);
                if(order==null || order.Status==StoneOrderStatus.Delivered || order.Status==StoneOrderStatus.Cancelled){RemoveHead();return;}
                routine=StartCoroutine(Deliver(order.OrderId));
            }
        }
        void LateUpdate()
        {
            if(checkpointWrite!=null && checkpointWrite.IsCompleted)
            {if(!checkpointWrite.Success){LastError=checkpointWrite.Error;worldDirty=true;}checkpointWrite=null;}
            if(!initialized || restoring || quitting || Time.time<nextCheckpoint)return;
            if(economy.HasPendingWrite || checkpointWrite!=null)return;
            var records=CaptureCheckpoint(false);nextCheckpoint=Time.time+checkpointInterval;
            if(records.Count>0)checkpointWrite=economy.CheckpointDeliveryItemsAsync(records);
        }
        void FixedUpdate(){if(moving && carBody!=null)carBody.MovePosition(MotionPosition());}
        Vector3 MotionPosition()
        {float t=Mathf.Clamp01((float)((Time.timeAsDouble-motionTime)/motionDuration));t=t*t*(3-2*t);return Vector3.LerpUnclamped(motionStart,motionEnd,t);}
        IEnumerator MoveTo(Vector3 target,float duration,StoneDeliveryPhase phase)
        {
            if(car==null){Fail("Car referansı eksik.");yield break;}
            OwnCar();Phase=phase;motionStart=car.position;motionEnd=target;motionTime=Time.timeAsDouble;motionDuration=Mathf.Max(.01f,duration);moving=true;
            if(carBody!=null)carBody.interpolation=RigidbodyInterpolation.Interpolate;
            while(Time.timeAsDouble-motionTime<motionDuration)
            {if(car==null){Fail("Car hareket sırasında kaldırıldı.");yield break;}if(carBody==null)car.position=MotionPosition();yield return null;}
            moving=false;
            // Flush the interpolation history at the exact parked pose before returning body ownership.
            if(carBody!=null){carBody.interpolation=RigidbodyInterpolation.None;carBody.position=target;}
            car.position=target;
        }
        IEnumerator Deliver(string id)
        {
            // Defer work until the Coroutine handle is assigned, including immediate validation failures.
            yield return null;
            CurrentOrderId=id;LastError=null;FirstSpawnAt=-1;Phase=StoneDeliveryPhase.Idle;
            if(!ValidateScene(out var error) || !catalog.TryFind(economy.FindOrder(id).ProductId,out var product,out error,false))
            {Fail(error);yield break;}
            if(!ValidateBody(product,out error)){Fail(error);yield break;}
            yield return WaitForStore();yield return WaitForWrite(economy.BeginDeliveryAsync(id,SceneKey));if(Phase==StoneDeliveryPhase.Faulted)yield break;
            if(Vector3.Distance(car.position,startPosition)>.001f)
            {yield return MoveTo(startPosition,returnDuration,StoneDeliveryPhase.Returning);if(Phase==StoneDeliveryPhase.Faulted)yield break;}
            TripStartedAt=Time.timeAsDouble;
            yield return MoveTo(destination,travelDuration,StoneDeliveryPhase.Arriving);if(Phase==StoneDeliveryPhase.Faulted)yield break;
            ArrivedAt=Time.timeAsDouble;Phase=StoneDeliveryPhase.Waiting;
            double waitEnd=ArrivedAt+Mathf.Max(0,waitDuration);
            while(Time.timeAsDouble<waitEnd)yield return null;
            Phase=StoneDeliveryPhase.Spawning;
            int quantity=economy.FindOrder(id).Quantity;
            double nextSpawn=0;
            for(int index=0;index<quantity;index++)
            {
                var order=economy.FindOrder(id);
                if(order==null || order.Status!=StoneOrderStatus.Processing){RemoveHead();routine=null;CurrentOrderId=null;Phase=StoneDeliveryPhase.Idle;yield break;}
                var record=FindRecord(id,index);var existing=FindInstance(order.ItemInstanceId(index));
                if(record!=null && existing!=null)
                {if(!record.confirmed){yield return WaitForStore();yield return WaitForWrite(economy.ConfirmDeliveryItemAsync(id,index));if(Phase==StoneDeliveryPhase.Faulted)yield break;}continue;}
                if(record!=null){Fail("Kayıtlı teslimat taşı yeniden kurulamadı: "+order.ItemInstanceId(index));yield break;}
                if(spawnPoint==null || !catalog.TryFind(order.ProductId,out product,out error,false) || !ValidateBody(product,out error))
                {Fail(error??"Boru çıkışı eksik.");yield break;}
                var pose=new TidyObjectSnapshot{Id=order.ItemInstanceId(index),State=(int)TidyObjectState.Loose,Position=SampleSpawnPosition(),Rotation=product.prefab.transform.rotation};
                var stone=Stage(product,pose,product.prefab.transform.localScale,out error);
                if(stone==null){Fail(error);yield break;}
                double deadline=Time.timeAsDouble+blockedOutletTimeout;
                while(!OutletClear(stone))
                {if(Time.timeAsDouble>=deadline){Destroy(stone);Fail("Boru çıkışı kapalı; taşlar üst üste zorlanmadı. Çıkışı boşaltın.");yield break;}yield return null;}
                pose.CustomState=stone.GetComponent<RawStone>().CaptureObjectState();
                record=new StoneDeliveryItem{orderId=id,index=index,sceneName=SceneKey,source=pose,localScale=stone.transform.localScale,falling=true};
                yield return WaitForStore();yield return WaitForWrite(economy.PrepareDeliveryItemAsync(record));
                if(Phase==StoneDeliveryPhase.Faulted){Destroy(stone);yield break;}
                while(Time.timeAsDouble<nextSpawn)yield return null;
                if(!Activate(stone,record,out error)){Fail(error);yield break;}
                nextSpawn=Time.timeAsDouble+Mathf.Max(.01f,spawnInterval);
                if(FirstSpawnAt<0)FirstSpawnAt=Time.timeAsDouble;
                if(StoneSpawned!=null)foreach(Action<string,int,GameObject> listener in StoneSpawned.GetInvocationList())
                    try{listener(id,index,stone);}catch(Exception ex){Debug.LogException(ex);}
                yield return WaitForStore();yield return WaitForWrite(economy.ConfirmDeliveryItemAsync(id,index));if(Phase==StoneDeliveryPhase.Faulted)yield break;
            }
            yield return WaitForStore();yield return WaitForWrite(economy.CheckpointDeliveryItemsAsync(CaptureCheckpoint(true)));if(Phase==StoneDeliveryPhase.Faulted)yield break;
            yield return WaitForStore();yield return WaitForWrite(economy.SetOrderStatusAsync(id,StoneOrderStatus.Delivered));if(Phase==StoneDeliveryPhase.Faulted)yield break;
            RemoveHead();
            if(returnAfterDelivery){yield return MoveTo(startPosition,returnDuration,StoneDeliveryPhase.Returning);if(Phase==StoneDeliveryPhase.Faulted)yield break;}
            ReleaseCar();CurrentOrderId=null;Phase=StoneDeliveryPhase.Idle;routine=null;
        }
        IEnumerator WaitForWrite(EconomySaveOperation operation)
        {while(!operation.IsCompleted){economy.PumpWrites();yield return null;}if(!operation.Success)Fail(operation.Error);}
        IEnumerator WaitForStore(){while(economy.HasPendingWrite){economy.PumpWrites();yield return null;}}
        bool ValidateScene(out string error)
        {error=null;if(car==null || spawnPoint==null || catalog==null)error="Teslimat için Car, Boru/Point ve katalog referanslarını atayın.";return error==null;}
        static bool ValidateBody(StoneProduct product,out string error)
        {
            error=null;bool solid=false;
            foreach(var collider in product.prefab.GetComponentsInChildren<Collider>(true))
            {if(collider.isTrigger)continue;solid=true;if(collider is MeshCollider mesh && !mesh.convex){error="Teslimat taşının dinamik MeshCollider'ı convex olmalı: "+product.id;return false;}}
            if(!solid)error="Teslimat taşında katı collider bulunamadı: "+product.id;
            else if((product.prefab.GetComponent<Rigidbody>().constraints&RigidbodyConstraints.FreezePositionY)!=0)error="Taşın Rigidbody Y hareketi kilitli: "+product.id;
            return error==null;
        }
        GameObject Stage(StoneProduct product,TidyObjectSnapshot pose,Vector3 scale,out string error)
        {
            error=null;GameObject stone=null;
            try
            {
                if(staging==null){staging=new GameObject("Delivery staging (inactive)");staging.SetActive(false);}
                stone=Instantiate(product.prefab,staging.transform);stone.SetActive(false);stone.transform.SetPositionAndRotation(pose.Position,pose.Rotation);stone.transform.localScale=scale;
                stone.name=product.displayName+" ["+pose.Id+"]";stone.GetComponent<TidyObject>().AssignInstanceId(pose.Id);return stone;
            }
            catch(Exception ex){if(stone!=null)Destroy(stone);error="Taş prefabı oluşturulamadı: "+ex.Message;return null;}
        }
        bool Activate(GameObject stone,StoneDeliveryItem record,out string error)
        {
            error=null;
            try
            {
                var marker=stone.GetComponent<DeliveredStoneInstance>();if(marker==null)marker=stone.AddComponent<DeliveredStoneInstance>();
                marker.Bind(this,record.orderId,record.index,record.falling);stone.transform.SetParent(null,true);stone.SetActive(true);
                instances[record.source.Id]=marker;stone.GetComponent<TidyObject>().MarkLoose();marker.ResumeFall(record.falling);
                var bounds=stone.GetComponent<TidyObject>().CalculateWorldBounds();int count=Physics.OverlapBoxNonAlloc(bounds.center,bounds.extents*.98f,overlaps,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++)if(overlaps[i]!=null && overlaps[i].GetComponentInParent<DeliveredStoneInstance>() is DeliveredStoneInstance other && other!=marker && other.Falling)
                    marker.IgnoreInitialOverlap(overlaps[i]);
                return true;
            }
            catch(Exception ex){error="Taş etkinleştirilemedi: "+ex.Message;if(stone!=null)Destroy(stone);return false;}
        }
        bool OutletClear(GameObject stone)
        {
            var bounds=stone.GetComponent<TidyObject>().LocalBounds;var t=stone.transform;var scale=t.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            int count=Physics.OverlapBoxNonAlloc(t.TransformPoint(bounds.center),Vector3.Scale(bounds.extents,scale)*.95f,overlaps,t.rotation,~0,QueryTriggerInteraction.Ignore);
            if(count==overlaps.Length)return false;
            for(int i=0;i<count;i++)
            {var c=overlaps[i];if(c==null || c.transform.IsChildOf(t))continue;var other=c.GetComponentInParent<DeliveredStoneInstance>();var body=other!=null?other.GetComponent<Rigidbody>():null;if(other!=null && other.Falling && body!=null && !body.isKinematic && !body.IsSleeping() && body.linearVelocity.y<-.01f)continue;return false;}
            return true;
        }
        StoneDeliveryItem FindRecord(string id,int index)=>economy.GetDeliveryItem(id,index);
        DeliveredStoneInstance FindInstance(string id)
        {
            if(instances.TryGetValue(id,out var found) && found!=null)return found;
            foreach(var tidy in TidyObjectRegistry.All)if(tidy!=null && tidy.gameObject.scene==gameObject.scene && tidy.InstanceId==id)
            {var marker=tidy.GetComponent<DeliveredStoneInstance>();if(marker!=null){instances[id]=marker;return marker;}}
            return null;
        }
        public void MarkWorldDirty(){worldDirty=true;}
        public void MarkWorldDirty(TidyObject obj)
        {if(obj!=null && TrySourceId(obj.InstanceId,out var id)){worldDirty=true;dirtySources.Add(id);}}
        bool TrySourceId(string id,out string source)
        {
            source=id;if(instances.ContainsKey(id))return true;
            if(id.EndsWith("_left",StringComparison.Ordinal))source=id.Substring(0,id.Length-5);
            else if(id.EndsWith("_right",StringComparison.Ordinal))source=id.Substring(0,id.Length-6);
            return instances.ContainsKey(source);
        }
        static bool Moving(TidyObject obj)
        {
            if(obj==null || !obj.gameObject.activeInHierarchy)return false;
            if(obj.State==TidyObjectState.Held || obj.State==TidyObjectState.Moving || obj.State==TidyObjectState.Inspecting)return true;
            var body=obj.Body;return body!=null && !body.isKinematic && !body.IsSleeping();
        }
        public bool CheckpointNow()
        {
            if(economy==null || !economy.IsReady || restoring)return false;
            economy.FlushPendingWrites();
            if(!economy.TryCheckpointDeliveryItems(CaptureCheckpoint(true),out var error)){LastError=error;return false;}return true;
        }
        List<StoneDeliveryItem> CaptureCheckpoint(bool all)
        {
            var records=economy.GetDeliveryItems(SceneKey);var changed=new HashSet<string>(dirtySources);
            // Capture once more after a body sleeps so the durable pose includes its final resting position.
            changed.UnionWith(previouslyMoving);previouslyMoving.Clear();
            foreach(var obj in TidyObjectRegistry.All)if(obj!=null && TrySourceId(obj.InstanceId,out var source) && Moving(obj)){changed.Add(source);previouslyMoving.Add(source);}
            if(all || (worldDirty && changed.Count==0))foreach(var record in records)changed.Add(record.source.Id);
            var updates=new List<StoneDeliveryItem>();if(changed.Count==0)return updates;
            var ids=new HashSet<string>();var cachedCustom=new Dictionary<string,string>();
            foreach(var record in records)
            {
                if(!changed.Contains(record.source.Id))continue;
                ids.Add(record.source.Id);ids.Add(record.source.Id+"_left");ids.Add(record.source.Id+"_right");
                if(record.source.State==(int)TidyObjectState.Consumed && record.pieces.Count==2 && FindInstance(record.source.Id)?.GetComponent<RawStone>().IsProcessed==true)
                    cachedCustom[record.source.Id]=record.source.CustomState;
            }
            var snapshot=new TidyLevelSnapshot();TidyServices.TryGet<TidyInteractor>(out var interactor);TidyLevelSnapshotter.CaptureWorld(snapshot,interactor,null,ids,cachedCustom);
            var poses=new Dictionary<string,TidyObjectSnapshot>();foreach(var entry in snapshot.Objects)poses[entry.Id]=entry;
            foreach(var record in records)
            {
                if(!changed.Contains(record.source.Id))continue;
                var marker=FindInstance(record.source.Id);if(marker==null)continue;
                if(!poses.TryGetValue(record.source.Id,out var source))continue;
                record.source=source;record.localScale=marker.transform.lossyScale;record.falling=marker.Falling;record.pieces.Clear();
                if(poses.TryGetValue(source.Id+"_left",out var left))record.pieces.Add(left);if(poses.TryGetValue(source.Id+"_right",out var right))record.pieces.Add(right);
                updates.Add(record);
            }
            dirtySources.Clear();worldDirty=false;return updates;
        }
        void BeforeRestore(TidyLevelSnapshot snapshot)
        {
            if(!Connect() || (snapshot.SceneName!=null && snapshot.SceneName!=SceneKey))return;
            economy.FlushPendingWrites();
            restoring=true;if(routine!=null)StopCoroutine(routine);routine=null;moving=false;queue.Clear();queued.Clear();
            var records=economy.GetDeliveryItems(SceneKey);var ids=new HashSet<string>();
            foreach(var record in records)
            {
                ids.Add(record.source.Id);ids.Add(record.source.Id+"_left");ids.Add(record.source.Id+"_right");
                var marker=FindInstance(record.source.Id);
                if(marker!=null)marker.Bind(this,record.orderId,record.index,record.falling);
                if(marker==null)
                {
                    var order=economy.FindOrder(record.orderId);
                    if(catalog==null || order==null){Report("Kayıtlı taşı geri yüklemek için katalog ve sipariş verisi gerekli.");continue;}
                    if(!catalog.TryFind(order.ProductId,out var product,out var error,false)){Report(error);continue;}
                    if(!ValidateBody(product,out error)){Report(error);continue;}
                    var go=Stage(product,record.source,record.localScale,out error);
                    if(go==null || !Activate(go,record,out error)){Report(error);continue;}
                }
            }
            // Delivery records are authoritative for runtime objects; an older level snapshot must not revive a cut source or re-deliver it.
            snapshot.Objects.RemoveAll(p=>p!=null && ids.Contains(p.Id));
            foreach(var record in records){snapshot.Objects.Add(record.source);snapshot.Objects.AddRange(record.pieces);}
        }
        void AfterRestore(TidyLevelSnapshot snapshot)
        {
            if(!restoring)return;
            foreach(var record in economy.GetDeliveryItems(SceneKey))
            {var marker=FindInstance(record.source.Id);if(marker!=null)marker.ResumeFall(record.falling);}
            restoring=false;initialized=true;Phase=StoneDeliveryPhase.Idle;CurrentOrderId=null;EnqueueOutstanding();
        }
        void OwnCar()
        {
            if(carOwned)return;carBody=car.GetComponent<Rigidbody>();carOwned=true;
            if(carBody==null)return;bodyKinematic=carBody.isKinematic;bodyGravity=carBody.useGravity;bodyInterpolation=carBody.interpolation;bodyCollision=carBody.collisionDetectionMode;
            if(!carBody.isKinematic){carBody.linearVelocity=Vector3.zero;carBody.angularVelocity=Vector3.zero;}
            carBody.isKinematic=true;carBody.useGravity=false;carBody.interpolation=RigidbodyInterpolation.Interpolate;
        }
        void ReleaseCar()
        {
            moving=false;
            if(carOwned && carBody!=null)
            {
                var parked=carBody.position;carBody.isKinematic=bodyKinematic;
                if(!bodyKinematic){carBody.linearVelocity=Vector3.zero;carBody.angularVelocity=Vector3.zero;}
                carBody.useGravity=bodyGravity;carBody.interpolation=bodyInterpolation;carBody.collisionDetectionMode=bodyCollision;
                carBody.position=parked;car.position=parked;
            }
            carOwned=false;
        }
        void RemoveHead(){if(queue.Count==0)return;queued.Remove(queue[0]);queue.RemoveAt(0);}
        void Report(string error){if(LastError!=error)Debug.LogWarning("[Stone Delivery] "+error,this);LastError=error;}
        void Fail(string error){Report(error??"Teslimat tamamlanamadı.");Phase=StoneDeliveryPhase.Faulted;moving=false;routine=null;nextRetry=Time.time+retryDelay;}
        public void RetryFailedDelivery(){nextRetry=0;}
        void NewGame()
        {
            if(routine!=null)StopCoroutine(routine);routine=null;moving=false;queue.Clear();queued.Clear();
            foreach(var marker in instances.Values)if(marker!=null)
            {var raw=marker.GetComponent<RawStone>();raw.RestoreObjectState(null);marker.gameObject.SetActive(false);Destroy(marker.gameObject);}
            instances.Clear();dirtySources.Clear();previouslyMoving.Clear();checkpointWrite=null;CurrentOrderId=null;Phase=StoneDeliveryPhase.Idle;worldDirty=false;
        }
        void OnApplicationPause(bool paused){if(paused)CheckpointNow();}
        void OnDrawGizmosSelected()
        {
            if(spawnPoint==null)return;
            Gizmos.color=new Color(.2f,1f,.65f,.8f);
            var center=spawnPoint.position;const int segments=64;
            var previous=center+Vector3.right*SpawnRadius;
            for(int i=1;i<=segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;
                var next=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*SpawnRadius;
                Gizmos.DrawLine(previous,next);previous=next;
            }
            Gizmos.DrawWireSphere(center,.035f);
        }
        void OnApplicationQuit(){CheckpointNow();quitting=true;}
        void OnDisable()
        {
            if(!quitting && initialized)CheckpointNow();StopAllCoroutines();routine=null;ReleaseCar();
            TidyLevelSnapshotter.Restoring-=BeforeRestore;TidyLevelSnapshotter.Restored-=AfterRestore;
            TidyObjectRegistry.AnyStateChanged-=MarkWorldDirty;
            if(economy!=null){economy.OrderPlaced-=OrderPlaced;economy.NewGameStarted-=NewGame;}economy=null;
            initialized=false;if(staging!=null)Destroy(staging);
        }
    }
}
