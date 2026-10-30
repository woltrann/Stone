using System;
using System.Collections;
using System.Collections.Generic;
using StoneGame.Cutting;
using StoneGame.Computers;
using TidyUpFramework.Core;
using TidyUpFramework.Interaction;
using TidyUpFramework.Player;
using TidyUpFramework.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace StoneGame.Chipping
{
    public enum ChippingMode{Idle,Entering,Working,Exiting}
    [DefaultExecutionOrder(-65),DisallowMultipleComponent]
    public sealed class StoneChippingTable:MonoBehaviour
    {
        [SerializeField] StoneWorkSlot slot;
        [SerializeField] Transform cameraPoint,hammerHolder,hammerVisual;
        [SerializeField,Tooltip("Contact point on the metal head. Its forward axis points out of the striking face.")]
        Transform hammerImpactPoint;
        [SerializeField,Tooltip("Grip pivot in the rubber handle; the swing rotates around this point.")] Transform hammerGripPoint;
        [SerializeField,Range(5,100)] float swingDegrees=55f;
        [SerializeField] Collider hammerReturnCollider;
        [SerializeField] Camera playerCamera;
        [SerializeField] TidyFirstPersonController player;
        [SerializeField] TidyInteractor interactor;
        [SerializeField] MonoBehaviour[] cameraDrivers=Array.Empty<MonoBehaviour>();
        [SerializeField] GameObject progressPanel;
        [SerializeField] TMPro.TMP_Text progressLabel;
        [SerializeField] UnityEngine.UI.Image progressFill;
        [SerializeField] Canvas gameplayHud;
        [SerializeField,Min(.01f)] float transitionDuration=.55f,hitDuration=.45f;
        [SerializeField,Min(.1f)] float interactionDistance=2.5f,rayDistance=5f,hammerDepth=1.1f;
        [SerializeField] Vector3 hammerOffset=new Vector3(.12f,-.06f,0),heldHammerEuler=new Vector3(0,0,-30);
        [SerializeField] Vector3 strikeEuler=new Vector3(65,0,0);
        [SerializeField,Min(0)] float strikeLift=.25f;
        [SerializeField,Min(.01f)] float heldHammerScale=.65f;
        struct SavedBehaviour{public Behaviour target;public bool enabled;}
        struct SavedStack{public TidyScreenStack target;public bool back;}
        readonly List<SavedBehaviour> suspended=new List<SavedBehaviour>();
        readonly List<SavedStack> stacks=new List<SavedStack>();
        Transform cameraParent,hammerParent;
        Vector3 cameraLocalPosition,cameraWorldPosition,hammerLocalPosition,hammerScale;
        Quaternion cameraLocalRotation,cameraWorldRotation,hammerLocalRotation;
        bool cursorVisible,session;
        CursorLockMode cursorLock;
        Collider[] hammerColliders;
        bool[] colliderStates;
        TidyInteractionPromptView prompt;
        TidyOutlineHighlighter outline;
        Renderer[] tableRenderers;
        public ChippingMode Mode{get;private set;}
        public bool IsInUse=>Mode!=ChippingMode.Idle;
        public bool HammerHeld{get;private set;}
        public bool IsStriking{get;private set;}
        public Transform HammerImpactPoint=>hammerImpactPoint;
        public event Action<CutStoneHalf,Vector3> HammerHit;
        public CutStoneHalf ActiveStone=>slot!=null?slot.Stone:null;
        public string LastMessage{get;private set;}
        void Awake()
        {
            if(playerCamera==null)playerCamera=Camera.main;
            if(player==null)player=FindAnyObjectByType<TidyFirstPersonController>();
            if(interactor==null)interactor=FindAnyObjectByType<TidyInteractor>();
            prompt=FindAnyObjectByType<TidyInteractionPromptView>();outline=FindAnyObjectByType<TidyOutlineHighlighter>();
            tableRenderers=GetComponentsInChildren<Renderer>();
            if(hammerVisual!=null){hammerParent=hammerVisual.parent;hammerLocalPosition=hammerVisual.localPosition;hammerLocalRotation=hammerVisual.localRotation;hammerScale=hammerVisual.localScale;hammerColliders=hammerVisual.GetComponentsInChildren<Collider>(true);colliderStates=new bool[hammerColliders.Length];}
            if(progressPanel!=null)progressPanel.SetActive(false);
        }
        void Start()
        {
            // The existing hammer is now a dedicated wall tool, not a carried world object.
            // TidyObject registers in Awake even when disabled, so detach it after all Awakes.
            if(hammerVisual==null)return;
            var tidy=hammerVisual.GetComponent<TidyUpFramework.Objects.TidyObject>();
            if(tidy!=null){if(tidy.CurrentTarget!=null)tidy.CurrentTarget.Release(tidy);TidyUpFramework.Objects.TidyObjectRegistry.Unregister(tidy);}
            foreach(var collider in hammerColliders)collider.enabled=false;
            var body=hammerVisual.GetComponent<Rigidbody>();if(body!=null){body.isKinematic=true;body.useGravity=false;}
        }
        bool CanEnter()
        {
            if(!isActiveAndEnabled || IsInUse || ActiveStone==null || playerCamera==null || cameraPoint==null || interactor==null || player==null || !interactor.enabled || interactor.CarriedCount!=0 || interactor.IsInspecting || Time.timeScale<=0 || InteractiveComputer.ActiveComputer!=null || TidyControlLock.IsLocked(TidyControlChannel.Interaction))return false;
            return interactor.TryFindAimed<StoneChippingTable>(out var aimed,out _,interactionDistance) && aimed==this;
        }
        void Update()
        {
            if(!IsInUse)
            {
                bool hovered=CanEnter();if(hovered){prompt?.SetExternalPrompt(this,"E — Yontma masasında çalış",new Color(.5f,1,.7f),true);outline?.SetHighlight(this,tableRenderers,TidyHighlightStyle.Hover,this);}
                else{prompt?.ClearExternalPrompt(this);outline?.ClearOwner(this);}
                if(hovered && Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame)TryEnter();return;
            }
            if(Mode==ChippingMode.Exiting)return;
            if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame){HandleEscape();return;}
            if(Mode!=ChippingMode.Working)return;
            if(ActiveStone==null){TryExit();return;}
            if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame && !(EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject()))TryPointerAction(playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue()));
            RefreshProgress();
        }
        public bool TryEnter()
        {
            if(!CanEnter())return false;
            cameraParent=playerCamera.transform.parent;cameraLocalPosition=playerCamera.transform.localPosition;cameraLocalRotation=playerCamera.transform.localRotation;cameraWorldPosition=playerCamera.transform.position;cameraWorldRotation=playerCamera.transform.rotation;
            cursorLock=Cursor.lockState;cursorVisible=Cursor.visible;session=true;
            Suspend(player);Suspend(interactor);Suspend(gameplayHud);foreach(var driver in cameraDrivers)Suspend(driver);
            foreach(var menu in FindObjectsByType<TidyPauseMenu>(FindObjectsInactive.Include))Suspend(menu);
            foreach(var stack in FindObjectsByType<TidyScreenStack>(FindObjectsInactive.Include)){stacks.Add(new SavedStack{target=stack,back=stack.HandleBackInput});stack.HandleBackInput=false;}
            TidyControlLock.Acquire(this,TidyControlChannel.All);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            prompt?.ClearExternalPrompt(this);outline?.ClearOwner(this);Mode=ChippingMode.Entering;
            if(progressPanel!=null)progressPanel.SetActive(true);RefreshProgress();StartCoroutine(MoveCamera(true));return true;
        }
        void Suspend(Behaviour target){if(target==null)return;suspended.Add(new SavedBehaviour{target=target,enabled=target.enabled});target.enabled=false;}
        IEnumerator MoveCamera(bool entering)
        {
            var p=playerCamera.transform.position;var q=playerCamera.transform.rotation;float elapsed=0;
            while(elapsed<transitionDuration)
            {
                elapsed+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,transitionDuration));
                Vector3 target=entering?cameraPoint.position:(cameraParent!=null?cameraParent.TransformPoint(cameraLocalPosition):cameraWorldPosition);
                Quaternion rotation=entering?cameraPoint.rotation:(cameraParent!=null?cameraParent.rotation*cameraLocalRotation:cameraWorldRotation);
                playerCamera.transform.SetPositionAndRotation(Vector3.Lerp(p,target,t),Quaternion.Slerp(q,rotation,t));yield return null;
            }
            if(entering)Mode=ChippingMode.Working;
            else{while(Keyboard.current!=null && Keyboard.current.escapeKey.isPressed)yield return null;yield return new WaitForEndOfFrame();FinishSession();}
        }
        public void HandleEscape(){if(HammerHeld)ReturnHammer();else TryExit();}
        public bool TryExit(){if(!IsInUse || Mode==ChippingMode.Exiting)return false;StopAllCoroutines();ReturnHammer();Mode=ChippingMode.Exiting;StartCoroutine(MoveCamera(false));return true;}
        public bool TakeHammer()
        {
            if(Mode!=ChippingMode.Working || HammerHeld || hammerVisual==null || hammerImpactPoint==null || !hammerImpactPoint.IsChildOf(hammerVisual) || IsStriking)return false;
            for(int i=0;i<hammerColliders.Length;i++){colliderStates[i]=hammerColliders[i].enabled;hammerColliders[i].enabled=false;}
            hammerVisual.SetParent(null,true);hammerVisual.localScale*=heldHammerScale;HammerHeld=true;Cursor.visible=false;return true;
        }
        public void ReturnHammer()
        {
            if(!HammerHeld)return;
            StopCoroutineIfStriking();HammerHeld=false;
            if(session)Cursor.visible=true;
            if(hammerVisual!=null){hammerVisual.SetParent(hammerParent,false);hammerVisual.localPosition=hammerLocalPosition;hammerVisual.localRotation=hammerLocalRotation;hammerVisual.localScale=hammerScale;}
            for(int i=0;i<hammerColliders.Length;i++)if(hammerColliders[i]!=null)hammerColliders[i].enabled=colliderStates[i];
        }
        Coroutine strike;
        struct PointerHit{public Collider Collider;public CutStoneHalf Stone;public Vector3 Point,Normal;public float Distance;public bool CutFace;}
        CutStoneHalf pickingStone;
        Mesh pickingMesh;
        Vector3[] pickingVertices;
        int[] pickingIndices;
        bool[] pickingCutFaces;
        bool pointerOnStone;
        readonly RaycastHit[] pointerHits=new RaycastHit[64];
        void StopCoroutineIfStriking(){if(strike!=null)StopCoroutine(strike);strike=null;IsStriking=false;}
        void PreparePickingMesh(CutStoneHalf stone)
        {
            var mesh=stone!=null?stone.GeneratedMesh:null;
            if(stone==pickingStone && mesh==pickingMesh)return;
            pickingStone=stone;pickingMesh=mesh;pickingVertices=null;pickingIndices=null;pickingCutFaces=null;
            if(mesh==null || !mesh.isReadable)return;
            pickingVertices=mesh.vertices;var indices=new List<int>();var cutFaces=new List<bool>();
            for(int s=0;s<mesh.subMeshCount;s++){var sub=mesh.GetTriangles(s);indices.AddRange(sub);for(int i=0;i<sub.Length/3;i++)cutFaces.Add(s==stone.CutSurfaceSubmesh);}
            pickingIndices=indices.ToArray();pickingCutFaces=cutFaces.ToArray();
        }
        bool TryStoneMeshHit(Ray ray,out PointerHit hit)
        {
            hit=default;var stone=ActiveStone;PreparePickingMesh(stone);
            if(pickingVertices==null || stone==null)return false;
            var inverse=stone.transform.worldToLocalMatrix;var origin=inverse.MultiplyPoint3x4(ray.origin);var direction=inverse.MultiplyVector(ray.direction.normalized);
            var localRay=new Ray(origin,direction);if(!pickingMesh.bounds.IntersectRay(localRay))return false;
            float closest=rayDistance;int best=-1;Vector3 normal=default;
            for(int i=0;i+2<pickingIndices.Length;i+=3)
            {
                var a=pickingVertices[pickingIndices[i]];var edge1=pickingVertices[pickingIndices[i+1]]-a;var edge2=pickingVertices[pickingIndices[i+2]]-a;
                var p=Vector3.Cross(direction,edge2);float det=Vector3.Dot(edge1,p);if(Mathf.Abs(det)<1e-9f)continue;
                float inv=1/det;var relative=origin-a;float u=Vector3.Dot(relative,p)*inv;if(u<0 || u>1)continue;
                var q=Vector3.Cross(relative,edge1);float v=Vector3.Dot(direction,q)*inv;if(v<0 || u+v>1)continue;
                float distance=Vector3.Dot(edge2,q)*inv;if(distance<.0001f || distance>=closest)continue;
                closest=distance;best=i/3;normal=Vector3.Cross(edge1,edge2);
            }
            if(best<0)return false;
            normal=inverse.transpose.MultiplyVector(normal).normalized;if(Vector3.Dot(normal,ray.direction)>0)normal=-normal;
            hit=new PointerHit{Stone=stone,Point=ray.origin+ray.direction.normalized*closest,Normal=normal,Distance=closest,CutFace=pickingCutFaces[best]};return true;
        }
        bool TryPointerHit(Ray ray,out PointerHit hit)
        {
            hit=default;
            bool found=TryStoneMeshHit(ray,out hit);float nearest=found?hit.Distance:float.PositiveInfinity;
            int count=Physics.RaycastNonAlloc(ray,pointerHits,rayDistance,~0,QueryTriggerInteraction.Collide);
            if(count==pointerHits.Length)return false;
            for(int i=0;i<count;i++)
            {
                var candidate=pointerHits[i];if(candidate.collider.isTrigger && candidate.collider!=hammerReturnCollider)continue;
                if(player!=null && candidate.collider.transform.IsChildOf(player.transform))continue;
                if(HammerHeld && hammerVisual!=null && candidate.collider.transform.IsChildOf(hammerVisual))continue;
                // Convex/voxel physics proxies need not match the rendered cut silhouette.
                if(pickingVertices!=null && candidate.collider.GetComponentInParent<CutStoneHalf>()==ActiveStone)continue;
                if(candidate.distance<nearest-.001f){nearest=candidate.distance;hit=new PointerHit{Collider=candidate.collider,Point=candidate.point,Normal=candidate.normal,Distance=candidate.distance};found=true;}
            }
            return found;
        }
        public bool TryPointerAction(Ray ray)
        {
            if(Mode!=ChippingMode.Working || IsStriking || (EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject()))return false;
            if(!TryPointerHit(ray,out var hit))return false;
            if(hit.Collider!=null && (hit.Collider==hammerReturnCollider || (hammerVisual!=null && hit.Collider.transform.IsChildOf(hammerVisual))))
            {if(HammerHeld)ReturnHammer();else return TakeHammer();return true;}
            if(!HammerHeld || ActiveStone==null || hit.Stone!=ActiveStone || !ActiveStone.CanChip)return false;
            IsStriking=true;strike=StartCoroutine(Strike(ActiveStone,hit,ray.direction));return true;
        }
        Quaternion ImpactRotation(Vector3 normal)
        {
            var faceAxis=hammerVisual.InverseTransformDirection(hammerImpactPoint.forward).normalized;
            var rest=playerCamera.transform.rotation*Quaternion.Euler(heldHammerEuler);
            return Quaternion.FromToRotation(rest*faceAxis,-normal)*rest;
        }
        void SetHammerContactPose(Vector3 contact,Quaternion rotation)
        {
            hammerVisual.rotation=rotation;
            hammerVisual.position+=contact-hammerImpactPoint.position;
        }
        void SetHammerGripPose(Vector3 grip,Quaternion rotation)
        {
            hammerVisual.rotation=rotation;
            var pivot=hammerGripPoint!=null?hammerGripPoint:hammerVisual;
            hammerVisual.position+=grip-pivot.position;
        }
        IEnumerator Strike(CutStoneHalf stone,PointerHit hit,Vector3 direction)
        {
            float elapsed=0;bool applied=false;
            var grip=hammerGripPoint!=null?hammerGripPoint:hammerVisual;
            var startGrip=grip.position;var startRotation=hammerVisual.rotation;
            var impactRotation=ImpactRotation(hit.Normal);
            var localArm=hammerVisual.InverseTransformPoint(hammerImpactPoint.position)-hammerVisual.InverseTransformPoint(grip.position);
            var arm=Vector3.Scale(localArm,hammerVisual.localScale);
            var faceAxis=hammerVisual.InverseTransformDirection(hammerImpactPoint.forward).normalized;
            var axis=Vector3.Cross(faceAxis,arm).normalized;if(axis.sqrMagnitude<.001f)axis=Vector3.up;
            var targetGrip=hit.Point-impactRotation*arm;
            var windupRotation=impactRotation*Quaternion.AngleAxis(swingDegrees,axis);
            while(elapsed<hitDuration)
            {
                elapsed+=Time.unscaledDeltaTime;float phase=Mathf.Clamp01(elapsed/Mathf.Max(.01f,hitDuration));
                Vector3 gripPosition;Quaternion rotation;
                if(phase<.28f){float t=Mathf.SmoothStep(0,1,phase/.28f);gripPosition=Vector3.Lerp(startGrip,targetGrip,t);rotation=Quaternion.Slerp(startRotation,windupRotation,t);}
                else if(phase<.58f){float t=(phase-.28f)/.3f;t*=t;gripPosition=targetGrip;rotation=Quaternion.Slerp(windupRotation,impactRotation,t);}
                else{float t=Mathf.SmoothStep(0,1,(phase-.58f)/.42f);gripPosition=Vector3.Lerp(targetGrip,startGrip,t);rotation=Quaternion.Slerp(impactRotation,startRotation,t);}
                bool impactFrame=!applied && phase>=.58f;
                if(impactFrame){gripPosition=targetGrip;rotation=impactRotation;}
                SetHammerGripPose(gripPosition,rotation);
                if(impactFrame){applied=true;if(stone!=null && ActiveStone==stone && stone.TryChip()){NotifyProgress(stone);RefreshProgress();HammerHit?.Invoke(stone,hit.Point);}}
                yield return null;
            }
            IsStriking=false;strike=null;
        }
        void NotifyProgress(CutStoneHalf stone)
        {
            var obj=stone.GetComponent<TidyUpFramework.Objects.TidyObject>();
            foreach(var manager in FindObjectsByType<StoneGame.Delivery.StoneDeliveryManager>())manager.MarkWorldDirty(obj);
        }
        void LateUpdate()
        {
            if(Mode==ChippingMode.Working){playerCamera.transform.SetPositionAndRotation(cameraPoint.position,cameraPoint.rotation);Cursor.lockState=CursorLockMode.None;Cursor.visible=!HammerHeld;}
            if(!HammerHeld || IsStriking || Mode!=ChippingMode.Working || Mouse.current==null)return;
            var ray=playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());var point=ray.GetPoint(Mathf.Max(.1f,hammerDepth+hammerOffset.z));
            var rotation=playerCamera.transform.rotation*Quaternion.Euler(heldHammerEuler);
            pointerOnStone=TryPointerHit(ray,out var hit) && hit.Stone==ActiveStone;
            if(pointerOnStone){point=hit.Point-ray.direction*strikeLift;rotation=ImpactRotation(hit.Normal)*Quaternion.Euler(-strikeEuler);}
            // Keep the metal head under the actual pointer; smoothing its screen position made clicks miss.
            SetHammerContactPose(point,Quaternion.Slerp(hammerVisual.rotation,rotation,1-Mathf.Exp(-20*Time.unscaledDeltaTime)));
        }
        void RefreshProgress()
        {
            var stone=ActiveStone;string text;
            if(stone==null)text="Masaya kesilmiş bir taş yerleştirin.";
            else if(!stone.HasChippingSurface)text=stone.ChippingConfigurationError;
            else text="Taş — Yontma İşlemi\nVuruş: "+stone.CurrentHits+" / "+stone.RequiredHits+"    İlerleme: %"+Mathf.RoundToInt(stone.ChippingProgress*100)+"\n"+(stone.IsChippingCompleted?"Yontma işlemi tamamlandı.":HammerHeld?(pointerOnStone?"Taş hedefte — sol tıkla vur • ESC: çekici bırak":"Metal ucu taşın görünen herhangi bir yüzeyine getir"):"Panodaki çekice tıkla • ESC: çık");
            if(progressLabel!=null && progressLabel.text!=text)progressLabel.text=text;
            if(progressFill!=null)progressFill.fillAmount=stone!=null?stone.ChippingProgress:0;LastMessage=text;
        }
        void FinishSession()
        {
            ReturnHammer();if(!session)return;session=false;
            if(playerCamera!=null){playerCamera.transform.localPosition=cameraLocalPosition;playerCamera.transform.localRotation=cameraLocalRotation;}
            foreach(var state in suspended)if(state.target!=null)state.target.enabled=state.enabled;suspended.Clear();
            foreach(var state in stacks)if(state.target!=null)state.target.HandleBackInput=state.back;stacks.Clear();
            TidyControlLock.Release(this);Cursor.lockState=cursorLock;Cursor.visible=cursorVisible;Mode=ChippingMode.Idle;
            if(progressPanel!=null)progressPanel.SetActive(false);
        }
        void OnDisable(){StopAllCoroutines();FinishSession();prompt?.ClearExternalPrompt(this);outline?.ClearOwner(this);}
    }
}

