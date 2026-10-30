using System;
using System.Collections;
using System.Collections.Generic;
using TidyUpFramework.Core;
using TidyUpFramework.Feedback;
using TidyUpFramework.Interaction;
using TidyUpFramework.Player;
using TidyUpFramework.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace StoneGame.Computers
{
    public enum ComputerUseState { Idle, Entering, Using, Exiting }

    [DefaultExecutionOrder(-60),DisallowMultipleComponent]
    public sealed class InteractiveComputer : MonoBehaviour
    {
        [Header("PC references")]
        [SerializeField] InteractiveComputerSettings settings;
        [SerializeField] MeshRenderer screenRenderer;
        [SerializeField,Tooltip("Renderers highlighted when the computer is targeted. Assign one LOD only for LOD models.")]
        Renderer[] outlineRenderers=Array.Empty<Renderer>();
        [SerializeField] Transform pcViewPoint;
        [SerializeField] ComputerDesktopView desktop;
        [SerializeField] Collider[] interactionColliders=Array.Empty<Collider>();
        [Header("Existing player systems")]
        [SerializeField] Camera playerCamera;
        [SerializeField] TidyFirstPersonController playerController;
        [SerializeField] TidyInteractor interactor;
        [SerializeField] TidyInteractionPromptView promptView;
        [SerializeField] TidyOutlineHighlighter highlighter;
        [SerializeField] TidyPauseMenu[] pauseMenus=Array.Empty<TidyPauseMenu>();
        [SerializeField] TidyScreenStack[] screenStacks=Array.Empty<TidyScreenStack>();
        [SerializeField,Tooltip("Only camera effects that write to the gameplay camera; their enabled states are restored.")]
        MonoBehaviour[] cameraDrivers=Array.Empty<MonoBehaviour>();

        static InteractiveComputer activeComputer;
        readonly List<BehaviourState> suspended=new List<BehaviourState>();
        readonly List<StackState> stacks=new List<StackState>();
        readonly Vector3[] corners=new Vector3[4];
        Material outlineMaterial;
        Coroutine transition;
        Transform savedCameraParent;
        Vector3 savedCameraLocalPosition,savedCameraWorldPosition;
        Quaternion savedCameraLocalRotation,savedCameraWorldRotation;
        float savedFieldOfView,savedYaw,savedPitch;
        CursorLockMode savedCursorLock;
        bool savedCursorVisible,hasSessionSnapshot;
        GameObject previousSelection;
        Color lastOutlineColor;
        float lastOutlineWidth=-1;
        string shownPrompt;
        bool applicationQuitting;
        public ComputerUseState State { get; private set; }
        public bool IsHovered { get; private set; }
        public bool IsInUse=>State!=ComputerUseState.Idle;
        public static InteractiveComputer ActiveComputer=>activeComputer;
        public InteractiveComputerSettings Settings=>settings;
        public ComputerDesktopView Desktop=>desktop;
        public Camera PlayerCamera=>playerCamera;
        public Transform ViewPoint=>pcViewPoint;
        public MeshRenderer ScreenRenderer=>screenRenderer;
        public event Action BuyRequested;
        public event Action OrdersRequested;
        public event Action<ComputerUseState> StateChanged;
        struct BehaviourState { public Behaviour Target; public bool Enabled; }
        struct StackState { public TidyScreenStack Target; public bool HandleBack; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { activeComputer=null; }

        void Awake()
        {
            ResolveReferences();
            if((outlineRenderers==null || outlineRenderers.Length==0) && screenRenderer!=null) outlineRenderers=new Renderer[]{screenRenderer};
            if(desktop!=null && playerCamera!=null) desktop.Bind(this,playerCamera);
        }
        void ResolveReferences()
        {
            if(playerCamera==null) playerCamera=Camera.main;
            if(interactor==null) TidyServices.TryGet(out interactor);
            if(interactor==null) interactor=FindAnyObjectByType<TidyInteractor>();
            if(playerController==null) TidyServices.TryGet(out playerController);
            if(playerController==null) playerController=FindAnyObjectByType<TidyFirstPersonController>();
            if(promptView==null) promptView=FindAnyObjectByType<TidyInteractionPromptView>();
            if(highlighter==null) highlighter=FindAnyObjectByType<TidyOutlineHighlighter>();
            if(pauseMenus==null || pauseMenus.Length==0) pauseMenus=FindObjectsByType<TidyPauseMenu>(FindObjectsInactive.Include);
            if(screenStacks==null || screenStacks.Length==0) screenStacks=FindObjectsByType<TidyScreenStack>(FindObjectsInactive.Include);
        }

        void Update()
        {
            if(settings==null) { SetHover(false);return; }
            UpdateOutlineMaterial();
            if(State==ComputerUseState.Idle)
            {
                SetHover(CanTarget(out _));
                if(IsHovered && Keyboard.current!=null && settings.interactionKey!=Key.None && Keyboard.current[settings.interactionKey].wasPressedThisFrame)
                    TryEnter();
            }
            else if(State==ComputerUseState.Entering || State==ComputerUseState.Using)
            {
                if(ExitPressed()) TryExit();
            }
        }
        void LateUpdate()
        {
            if(State!=ComputerUseState.Using || playerCamera==null) return;
            ComputeUsePose(out var position,out var rotation);
            // Keep the complete monitor in view when aspect ratio or view point changes in use.
            float blend=1f-Mathf.Exp(-14f*Time.unscaledDeltaTime);
            playerCamera.transform.SetPositionAndRotation(Vector3.Lerp(playerCamera.transform.position,position,blend),
                Quaternion.Slerp(playerCamera.transform.rotation,rotation,blend));
        }

        public bool CanTarget(out RaycastHit hit)
        {
            hit=default;
            if(!isActiveAndEnabled || State!=ComputerUseState.Idle || settings==null || playerCamera==null || screenRenderer==null ||
                interactor==null || !interactor.isActiveAndEnabled || interactor.AimCamera==null ||
                (activeComputer!=null && activeComputer!=this) || TidyControlLock.IsLocked(TidyControlChannel.Interaction) ||
                interactor.IsInspecting || interactor.CarriedCount>0) return false;
            foreach(var menu in pauseMenus) if(menu!=null && menu.IsPaused) return false;
            foreach(var stack in screenStacks) if(stack!=null && stack.Count>0) return false;
            if(settings.requireFrontSide && Vector3.Dot(playerCamera.transform.position-ScreenCenter,ScreenNormal)<=.01f) return false;
            if(!interactor.TryFindAimed<InteractiveComputer>(out var aimed,out hit,settings.interactionDistance) || aimed!=this) return false;
            if(interactionColliders.Length==0) return true;
            foreach(var collider in interactionColliders) if(collider!=null && hit.collider==collider) return true;
            return false;
        }

        public bool TryEnter()
        {
            if(!Application.isPlaying || !CanTarget(out _) || pcViewPoint==null || desktop==null || playerController==null || EventSystem.current==null) return false;
            // Carrying is never transferred to a new system or silently dropped.
            if(interactor.CarriedCount!=0) return false;
            activeComputer=this;
            var cameraTransform=playerCamera.transform;
            var entryPosition=cameraTransform.position;var entryRotation=cameraTransform.rotation;float entryFov=playerCamera.fieldOfView;
            foreach(var driver in cameraDrivers)
                if(driver is TidyCameraPunch punch && punch.isActiveAndEnabled) punch.Cancel();
            savedCameraParent=cameraTransform.parent;
            savedCameraLocalPosition=cameraTransform.localPosition;savedCameraLocalRotation=cameraTransform.localRotation;
            savedCameraWorldPosition=cameraTransform.position;savedCameraWorldRotation=cameraTransform.rotation;
            savedFieldOfView=playerCamera.fieldOfView;savedYaw=playerController.Yaw;savedPitch=playerController.Pitch;
            savedCursorLock=Cursor.lockState;savedCursorVisible=Cursor.visible;
            previousSelection=EventSystem.current.currentSelectedGameObject;hasSessionSnapshot=true;
            // Transition from the visible pose, but return to the stable gameplay pose after transient effects.
            cameraTransform.SetPositionAndRotation(entryPosition,entryRotation);playerCamera.fieldOfView=entryFov;
            SetHover(false);
            TidyControlLock.Acquire(this,TidyControlChannel.All);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            Suspend(playerController);Suspend(interactor);
            foreach(var driver in cameraDrivers) Suspend(driver);
            foreach(var pause in pauseMenus) Suspend(pause);
            stacks.Clear();
            foreach(var stack in screenStacks) if(stack!=null) {stacks.Add(new StackState{Target=stack,HandleBack=stack.HandleBackInput});stack.HandleBackInput=false;}
            desktop.SetInteractive(false);
            promptView?.SetExternalPrompt(this,string.Empty,settings.outlineColor,false,desktop.InterfaceFont);
            EventSystem.current.SetSelectedGameObject(null);
            SetState(ComputerUseState.Entering);
            transition=StartCoroutine(Transition(true));
            return true;
        }

        void Suspend(Behaviour behaviour)
        {
            if(behaviour==null || behaviour==this) return;
            foreach(var item in suspended) if(item.Target==behaviour) return;
            suspended.Add(new BehaviourState{Target=behaviour,Enabled=behaviour.enabled});behaviour.enabled=false;
        }
        public bool TryExit()
        {
            if(State!=ComputerUseState.Entering && State!=ComputerUseState.Using) return false;
            desktop.SetInteractive(false);
            if(EventSystem.current!=null && EventSystem.current.currentSelectedGameObject!=null &&
                EventSystem.current.currentSelectedGameObject.transform.IsChildOf(desktop.transform)) EventSystem.current.SetSelectedGameObject(null);
            if(transition!=null) StopCoroutine(transition);
            SetState(ComputerUseState.Exiting);
            // The normal cursor policy resumes now; movement/look/interaction stay locked until return ends.
            TidyControlLock.Acquire(this,TidyControlChannel.Movement|TidyControlChannel.Look|TidyControlChannel.Interaction);
            RestoreCursorFallback();
            transition=StartCoroutine(Transition(false));
            return true;
        }
        IEnumerator Transition(bool entering)
        {
            var sourcePosition=playerCamera.transform.position;var sourceRotation=playerCamera.transform.rotation;
            float sourceFov=playerCamera.fieldOfView;
            float duration=entering?settings.enterDuration:settings.exitDuration;
            float elapsed=0;
            do
            {
                elapsed+=Time.unscaledDeltaTime;
                float t=Mathf.Clamp01(elapsed/Mathf.Max(.01f,duration));t=t*t*(3f-2f*t);
                Vector3 target;Quaternion rotation;
                if(entering) ComputeUsePose(out target,out rotation);else ReturnPose(out target,out rotation);
                playerCamera.transform.SetPositionAndRotation(Vector3.Lerp(sourcePosition,target,t),Quaternion.Slerp(sourceRotation,rotation,t));
                playerCamera.fieldOfView=Mathf.Lerp(sourceFov,savedFieldOfView,t);
                yield return null;
            } while(elapsed<duration);
            if(entering)
            {
                ComputeUsePose(out var target,out var rotation);playerCamera.transform.SetPositionAndRotation(target,rotation);
                SetState(ComputerUseState.Using);desktop.SetInteractive(true);transition=null;
            }
            else
            {
                while(ExitInputHeld()) yield return null;
                yield return new WaitForEndOfFrame();
                FinishSession();transition=null;
            }
        }

        public Vector3 ScreenCenter
        {
            get {var b=screenRenderer.localBounds;return screenRenderer.transform.TransformPoint(new Vector3(b.center.x,b.center.y,b.min.z));}
        }
        public Vector3 ScreenNormal=>screenRenderer.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(Vector3.back).normalized;
        public void GetScreenCorners(Vector3[] output)
        {
            var b=screenRenderer.localBounds;
            for(int i=0;i<4;i++) output[i]=screenRenderer.transform.TransformPoint(new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,b.min.z));
        }
        public void ComputeUsePose(out Vector3 position,out Quaternion rotation)
        {
            position=pcViewPoint.position;rotation=pcViewPoint.rotation;
            if(!settings.automaticallyFrameScreen) return;
            var delta=ScreenCenter-position;
            if(delta.sqrMagnitude>.000001f) rotation=Quaternion.LookRotation(delta,screenRenderer.transform.up);
            GetScreenCorners(corners);
            var inverse=Quaternion.Inverse(rotation);
            float tanV=Mathf.Tan(playerCamera.fieldOfView*Mathf.Deg2Rad*.5f),tanH=tanV*Mathf.Max(.1f,playerCamera.aspect),back=0;
            foreach(var corner in corners)
            {
                var p=inverse*(corner-position);
                back=Mathf.Max(back,Mathf.Max(Mathf.Abs(p.x)*settings.screenPadding/tanH-p.z,
                    Mathf.Max(Mathf.Abs(p.y)*settings.screenPadding/tanV-p.z,playerCamera.nearClipPlane+settings.nearClipClearance-p.z)));
            }
            position-=rotation*Vector3.forward*back;
        }
        void ReturnPose(out Vector3 position,out Quaternion rotation)
        {
            position=savedCameraParent!=null?savedCameraParent.TransformPoint(savedCameraLocalPosition):savedCameraWorldPosition;
            rotation=savedCameraParent!=null?savedCameraParent.rotation*savedCameraLocalRotation:savedCameraWorldRotation;
        }
        bool ExitPressed()=>Keyboard.current!=null && (Keyboard.current.escapeKey.wasPressedThisFrame ||
            (settings.exitKey!=Key.None && Keyboard.current[settings.exitKey].wasPressedThisFrame));
        bool ExitInputHeld()
        {
            var keyboard=Keyboard.current;
            return (keyboard!=null && (keyboard.escapeKey.isPressed || (settings.exitKey!=Key.None && keyboard[settings.exitKey].isPressed) ||
                (settings.interactionKey!=Key.None && keyboard[settings.interactionKey].isPressed))) ||
                (Mouse.current!=null && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed));
        }
        void FinishSession()
        {
            if(hasSessionSnapshot)
            {
                if(playerController!=null) playerController.Teleport(playerController.transform.position,savedYaw,savedPitch);
                if(playerCamera!=null)
                {
                    if(savedCameraParent!=null && playerCamera.transform.parent!=savedCameraParent) playerCamera.transform.SetParent(savedCameraParent,false);
                    playerCamera.transform.localPosition=savedCameraLocalPosition;playerCamera.transform.localRotation=savedCameraLocalRotation;
                    playerCamera.fieldOfView=savedFieldOfView;
                }
                foreach(var item in suspended) if(item.Target!=null) item.Target.enabled=item.Enabled;
                foreach(var stack in stacks) if(stack.Target!=null) stack.Target.HandleBackInput=stack.HandleBack;
                suspended.Clear();stacks.Clear();hasSessionSnapshot=false;
            }
            if(desktop!=null) desktop.SetInteractive(false);
            promptView?.ClearExternalPrompt(this);
            TidyControlLock.Release(this);RestoreCursorFallback();
            if(EventSystem.current!=null)
                EventSystem.current.SetSelectedGameObject(previousSelection!=null && previousSelection.activeInHierarchy?previousSelection:null);
            previousSelection=null;
            if(activeComputer==this) activeComputer=null;
            SetState(ComputerUseState.Idle);
        }
        void RestoreCursorFallback()
        {
            if(FindAnyObjectByType<TidyCursorController>()!=null) return;
            bool cursorOwned=TidyControlLock.IsLocked(TidyControlChannel.Cursor);
            Cursor.lockState=cursorOwned?CursorLockMode.None:savedCursorLock;Cursor.visible=cursorOwned || savedCursorVisible;
        }
        void SetState(ComputerUseState value) { State=value;StateChanged?.Invoke(value); }
        void SetHover(bool value)
        {
            if(IsHovered==value)
            {
                if(value && shownPrompt!=settings.PromptText) {shownPrompt=settings.PromptText;promptView?.SetExternalPrompt(this,shownPrompt,settings.outlineColor,true,desktop?.InterfaceFont);}
                return;
            }
            IsHovered=value;
            if(value)
            {
                if(outlineRenderers==null && screenRenderer!=null) outlineRenderers=new Renderer[]{screenRenderer};
                highlighter?.SetHighlight(this,outlineRenderers,TidyHighlightStyle.Hover,this,outlineMaterial);
                shownPrompt=settings.PromptText;promptView?.SetExternalPrompt(this,shownPrompt,settings.outlineColor,true,desktop?.InterfaceFont);
            }
            else
            {
                highlighter?.ClearOwner(this);shownPrompt=null;
                if(State==ComputerUseState.Idle) promptView?.ClearExternalPrompt(this);
            }
        }
        void UpdateOutlineMaterial()
        {
            if(outlineMaterial==null)
            {
                var shader=settings.outlineShader!=null?settings.outlineShader:Shader.Find("TidyUpFramework/Outline");
                if(shader==null) return;
                outlineMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave,enableInstancing=true};
                // Draw the outline before the monitor's transparent world UI.
                outlineMaterial.renderQueue=(int)RenderQueue.Transparent-10;
                outlineMaterial.SetFloat("_ZTest",(float)CompareFunction.LessEqual);
            }
            if(lastOutlineColor!=settings.outlineColor || lastOutlineWidth!=settings.outlineWidth)
            {
                outlineMaterial.SetColor("_OutlineColor",settings.outlineColor);outlineMaterial.SetFloat("_OutlineWidth",settings.outlineWidth);
                lastOutlineColor=settings.outlineColor;lastOutlineWidth=settings.outlineWidth;
            }
        }
        public void RequestBuy() {if(State!=ComputerUseState.Using)return;Debug.Log("[Interactive PC] Taş Satın Al",this);BuyRequested?.Invoke();}
        public void RequestOrders() {if(State!=ComputerUseState.Using)return;Debug.Log("[Interactive PC] Siparişlerim",this);OrdersRequested?.Invoke();}
        public void AbortImmediately()
        {
            if(transition!=null) StopCoroutine(transition);transition=null;
            SetHover(false);if(IsInUse || hasSessionSnapshot) FinishSession();else {promptView?.ClearExternalPrompt(this);highlighter?.ClearOwner(this);}
        }
        void OnApplicationQuit() { applicationQuitting=true; }
        void OnDisable()
        {
            if(applicationQuitting)
            {
                StopAllCoroutines();
                // UI tween hosts may already be destroyed during shutdown. Do not recreate them.
                highlighter?.ClearOwner(this);TidyControlLock.Release(this);
                if(activeComputer==this) activeComputer=null;
                return;
            }
            AbortImmediately();
        }
        void OnDestroy() {if(outlineMaterial!=null) Destroy(outlineMaterial);}
    }
}
