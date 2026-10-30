using System;
using System.Collections.Generic;
using TidyUpFramework.Core;
using TidyUpFramework.Input;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using UnityEngine;

namespace TidyUpFramework.Interaction
{
    [DefaultExecutionOrder(10)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Tidy Up Framework/Interaction/Interactor")]
    public sealed class TidyInteractor : MonoBehaviour
    {
        const int MaximumHits = 24;
        const float GroundProbeHeight = 0.6f;
        const float GroundProbeDistance = 20f;
        const float RestingTolerance = 0.06f;
        const float EmbeddedPickDepth = 0.3f;

        [Header("References")]
        [SerializeField, Tooltip("Camera used for aiming. Defaults to the camera on this GameObject or the main camera.")]
        Camera aimCamera;

        [Header("Reach")]
        [SerializeField, Min(0.1f)]
        float reachDistance = 3f;

        [SerializeField, Min(0f), Tooltip("Sphere radius used for aiming. A small radius makes tiny objects easier to target.")]
        float aimRadius = 0.025f;

        [SerializeField]
        LayerMask interactionMask = ~0;

        [Header("Holding")]
        [SerializeField, Tooltip("Hold position relative to the camera.")]
        Vector3 holdOffset = new Vector3(0f, -0.12f, 0.75f);

        [SerializeField, Min(0f), Tooltip("Degrees per second for continuous rotate input.")]
        float rotateSpeed = 180f;

        [SerializeField, Min(1f), Tooltip("Degrees per scroll wheel notch.")]
        float rotateStepDegrees = 15f;

        [SerializeField, Tooltip("Pressing interact while not aiming at a target drops the held object.")]
        bool interactDropsWhenNoTarget;

        [Header("Carrying")]
        [SerializeField, Min(1), Tooltip("How many objects the player can carry at once. 1 keeps classic single-object holding.")]
        int carryCapacity = 1;

        [SerializeField, Min(0.01f), Tooltip("Largest dimension in metres an object may have to join a carried stack. Bigger objects are always carried alone.")]
        float maximumStackedSize = 0.6f;

        [SerializeField, Tooltip("Position of the carried stack relative to the camera.")]
        Vector3 stackOffset = new Vector3(0.36f, -0.44f, 0.9f);

        [SerializeField, Range(0.2f, 1f), Tooltip("Visual scale of stacked objects so a full stack never blocks the view.")]
        float stackVisualScale = 0.6f;

        [SerializeField, Min(0f), Tooltip("Gap between objects in the carried stack.")]
        float stackSpacing = 0.012f;

        [SerializeField, Tooltip("While carrying several objects, aiming at a target brings a matching carried object to the hand.")]
        bool autoSelectMatchingObject = true;

        [Header("Dropping")]
        [SerializeField, Min(0f)]
        float controlledThrowDistance = 4f;

        [SerializeField, Min(0f)]
        float controlledDropArc = 0.15f;

        [Header("Inspecting")]
        [SerializeField, Tooltip("Inspect position relative to the camera.")]
        Vector3 inspectOffset = new Vector3(0f, 0f, 0.5f);

        [SerializeField, Min(0f)]
        float inspectRotateMultiplier = 2.5f;

        readonly RaycastHit[] hits = new RaycastHit[MaximumHits];
        readonly TidyHoldMotion holdMotion = new TidyHoldMotion();
        readonly List<TidyObject> carried = new List<TidyObject>();
        readonly Dictionary<TidyObject, float> carriedYaw = new Dictionary<TidyObject, float>();
        ITidyPlayerInput input;
        ITidyHighlighter highlighter;
        TidyPlacementPreview preview;
        Transform ignoredRoot;
        float heldYaw;
        Quaternion inspectRotation = Quaternion.identity;
        TidyPlacementCandidate currentCandidate;

        public event Action<TidyObject> HoverChanged;
        public event Action<TidyObject> HeldChanged;
        public event Action<TidyObject> ObjectPickedUp;
        public event Action<TidyPlacementCandidate> ObjectPlaced;
        public event Action<TidyPlacementCandidate> PlacementRejected;
        public event Action<TidyObject> ObjectDropped;
        public event Action<TidyObject> InspectChanged;
        public event Action<TidyInteractionContext> ContextChanged;
        public event Action CarriedChanged;

        public float AimAssistMultiplier { get; set; } = 1f;
        public Func<TidyObject, bool> StackFilter { get; set; }
        public TidyObject HoveredObject { get; private set; }
        public TidyObject HeldObject { get; private set; }
        public IReadOnlyList<TidyObject> StackedObjects => carried;
        public int CarriedCount => (HeldObject != null ? 1 : 0) + carried.Count;
        public bool IsCarryingStack => carried.Count > 0;
        public bool IsInspecting { get; private set; }

        public int CarryCapacity
        {
            get => carryCapacity;
            set
            {
                carryCapacity = Mathf.Max(1, value);
                CarriedChanged?.Invoke();
            }
        }
        public TidyPlacementCandidate CurrentCandidate => currentCandidate;
        public TidyInteractionContext Context { get; private set; }
        public Camera AimCamera => aimCamera;
        public float ReachDistance => reachDistance;
        public LayerMask InteractionMask => interactionMask;

        public bool TryAim(out RaycastHit hit)
        {
            return TryRaycastFirstSolid(CenterRay(), out hit);
        }

        public bool TryFindAimed<T>(out T component, out RaycastHit hit, float distance = -1f) where T : Component
        {
            var count = CastSorted(CenterRay(), distance < 0f ? reachDistance : distance);
            for (var index = 0; index < count; index++)
            {
                var candidate = hits[index];
                if (IsPartOfHeldObject(candidate.collider))
                {
                    continue;
                }

                component = candidate.collider.GetComponentInParent<T>();
                if (component != null)
                {
                    hit = candidate;
                    return true;
                }

                if (!candidate.collider.isTrigger)
                {
                    break;
                }
            }

            component = null;
            hit = default;
            return false;
        }

        public Ray CenterRay()
        {
            var cameraTransform = aimCamera.transform;
            return new Ray(cameraTransform.position, cameraTransform.forward);
        }

        public void ForcePickUp(TidyObject tidyObject)
        {
            if (tidyObject != null && HeldObject == null && tidyObject.IsAvailableForPickup)
            {
                PickUp(tidyObject);
            }
        }

        public void ForceDrop()
        {
            if (HeldObject != null)
            {
                Drop(false);
            }
        }

        public void DropAll()
        {
            if (IsInspecting)
            {
                EndInspect();
            }

            while (HeldObject != null)
            {
                Drop(false);
            }
        }

        public bool CanStack(TidyObject tidyObject)
        {
            if (tidyObject == null || HeldObject == null || tidyObject == HeldObject || carried.Contains(tidyObject))
            {
                return false;
            }

            return CarriedCount < carryCapacity
                && tidyObject.State == TidyObjectState.Loose
                && tidyObject.IsAvailableForPickup
                && FitsInStack(tidyObject)
                && FitsInStack(HeldObject);
        }

        public bool TryAddToStack(TidyObject tidyObject)
        {
            if (!CanStack(tidyObject))
            {
                return false;
            }

            AddToStack(tidyObject);
            return true;
        }

        public bool SelectCarried(TidyObject tidyObject)
        {
            if (IsInspecting || tidyObject == null || !carried.Contains(tidyObject))
            {
                return false;
            }

            SwapActive(tidyObject);
            return true;
        }

        void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = GetComponent<Camera>();
            }

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            preview = GetComponent<TidyPlacementPreview>();
            var owner = GetComponentInParent<CharacterController>();
            ignoredRoot = owner != null ? owner.transform : transform.root;
        }

        void OnEnable()
        {
            TidyServices.Register(this);
        }

        void OnDisable()
        {
            TidyServices.Unregister(this);
            highlighter?.ClearOwner(this);
            preview?.Hide();
        }

        void Update()
        {
            if (aimCamera == null)
            {
                return;
            }

            ResolveServices();
            var inputAllowed = input != null && !TidyControlLock.IsLocked(TidyControlChannel.Interaction);
            if (IsInspecting)
            {
                UpdateInspecting(inputAllowed);
                return;
            }

            if (HeldObject == null)
            {
                UpdateIdle(inputAllowed);
            }
            else
            {
                UpdateHolding(inputAllowed);
            }
        }

        void FixedUpdate()
        {
            if (HeldObject != null && !IsInspecting && HeldObject.UsesPhysics)
            {
                holdMotion.MovePhysics(HeldObject, HoldCenter(), HoldRotation(), Time.fixedDeltaTime);
            }
        }

        void ResolveServices()
        {
            if (input == null)
            {
                TidyServices.TryGet(out input);
            }

            if (highlighter == null)
            {
                TidyServices.TryGet(out highlighter);
            }
        }

        void UpdateIdle(bool inputAllowed)
        {
            var ray = inputAllowed && input.HasPointerAim ? aimCamera.ScreenPointToRay(input.PointerAimPosition) : CenterRay();
            var candidate = FindPickable(ray);
            SetHover(candidate);
            SetContext(candidate != null ? TidyInteractionContext.CanPickUp : TidyInteractionContext.Idle);
            if (inputAllowed && input.InteractPressed && candidate != null)
            {
                PickUp(candidate);
            }
        }

        void UpdateHolding(bool inputAllowed)
        {
            PruneCarried();
            if (HeldObject.State != TidyObjectState.Held || HeldObject.IsCarried)
            {
                ReleaseHeldReference();
                return;
            }

            if (inputAllowed)
            {
                heldYaw += input.RotateAxis * rotateSpeed * Time.deltaTime + input.RotateSteps * rotateStepDegrees;
            }

            if (!HeldObject.UsesPhysics)
            {
                holdMotion.MoveControlled(HeldObject, HoldCenter(), HoldRotation(), Time.deltaTime);
            }

            MoveStack(Time.deltaTime);
            var ray = inputAllowed && input.HasPointerAim ? aimCamera.ScreenPointToRay(input.PointerAimPosition) : CenterRay();
            currentCandidate = FindPlacement(ray);
            if (autoSelectMatchingObject && carried.Count > 0 && currentCandidate.Target != null && !(currentCandidate.IsValid && currentCandidate.IsCorrect) && TrySelectMatching(currentCandidate.Target))
            {
                currentCandidate = FindPlacement(ray);
            }

            var stackCandidate = currentCandidate.Target == null && carryCapacity > 1 ? FindPickable(ray) : null;
            if (stackCandidate != null && !CanStack(stackCandidate))
            {
                stackCandidate = null;
            }

            SetHover(stackCandidate);
            UpdatePreviewAndContext(stackCandidate != null);

            if (!inputAllowed)
            {
                return;
            }

            if (input.InspectPressed && HeldObject.IsInspectable)
            {
                BeginInspect();
            }
            else if (input.InteractPressed && stackCandidate != null)
            {
                AddToStack(stackCandidate);
            }
            else if (input.InteractPressed)
            {
                TryCommitPlacement();
            }
            else if (input.ThrowPressed)
            {
                Drop(true);
            }
            else if (input.DropPressed)
            {
                Drop(false);
            }
        }

        void UpdatePreviewAndContext(bool canStack)
        {
            if (currentCandidate.Target == null)
            {
                preview?.Hide();
                SetContext(canStack ? TidyInteractionContext.CanStack : TidyInteractionContext.Holding);
                return;
            }

            preview?.Show(currentCandidate);
            if (currentCandidate.IsValid)
            {
                SetContext(currentCandidate.IsCorrect ? TidyInteractionContext.CanPlace : TidyInteractionContext.CanPlaceIncorrect);
            }
            else
            {
                var full = currentCandidate.Rejection == TidyRejectionReason.Full || currentCandidate.Rejection == TidyRejectionReason.NoSpace;
                SetContext(full ? TidyInteractionContext.TargetFull : TidyInteractionContext.WrongPlace);
            }
        }

        void TryCommitPlacement()
        {
            if (currentCandidate.Target == null)
            {
                if (interactDropsWhenNoTarget)
                {
                    Drop(false);
                }

                return;
            }

            if (!currentCandidate.IsValid)
            {
                PlacementRejected?.Invoke(currentCandidate);
                TidyTweenRunner.Punch(HeldObject.VisualRoot, HeldObject.RestVisualScale, 0.08f, 0.3f, 3);
                return;
            }

            var placed = currentCandidate;
            ReleaseHeldReference();
            if (placed.Target.TryPlace(placed))
            {
                ObjectPlaced?.Invoke(placed);
            }
        }

        void PickUp(TidyObject tidyObject)
        {
            if (tidyObject.CurrentTarget != null)
            {
                tidyObject.CurrentTarget.Release(tidyObject);
            }

            TidyTweenRunner.Kill(tidyObject.transform);
            var support = tidyObject.State == TidyObjectState.Loose ? tidyObject.CalculateWorldBounds() : default;
            tidyObject.MarkHeld();
            if (support.size != Vector3.zero)
            {
                SettleObjectsRestingOn(tidyObject, support);
            }

            heldYaw = Mathf.DeltaAngle(aimCamera.transform.eulerAngles.y, tidyObject.transform.eulerAngles.y);
            heldYaw = Mathf.Round(heldYaw / rotateStepDegrees) * rotateStepDegrees;
            SetHover(null);
            HeldObject = tidyObject;
            TidyTweenRunner.Punch(tidyObject.VisualRoot, tidyObject.RestVisualScale, 0.1f, 0.25f);
            HeldChanged?.Invoke(tidyObject);
            ObjectPickedUp?.Invoke(tidyObject);
            SetContext(TidyInteractionContext.Holding);
        }

        void Drop(bool throwObject)
        {
            var dropped = HeldObject;
            ReleaseHeldReference();
            if (dropped.UsesPhysics)
            {
                dropped.MarkLoose();
                if (dropped.Body != null)
                {
                    dropped.Body.linearVelocity = throwObject ? aimCamera.transform.forward * dropped.PhysicsProfile.ThrowSpeed : Vector3.zero;
                }
            }
            else
            {
                DropControlled(dropped, throwObject);
            }

            ObjectDropped?.Invoke(dropped);
        }

        void AddToStack(TidyObject tidyObject)
        {
            var previous = HeldObject;
            TidyTweenRunner.Kill(tidyObject.transform);
            var support = tidyObject.CalculateWorldBounds();
            previous.MarkCarried();
            carried.Add(previous);
            carriedYaw[previous] = UnityEngine.Random.Range(-12f, 12f);
            tidyObject.MarkHeld();
            SettleObjectsRestingOn(tidyObject, support);
            heldYaw = Mathf.Round(Mathf.DeltaAngle(aimCamera.transform.eulerAngles.y, tidyObject.transform.eulerAngles.y) / rotateStepDegrees) * rotateStepDegrees;
            SetHover(null);
            HeldObject = tidyObject;
            TidyTweenRunner.Punch(tidyObject.VisualRoot, tidyObject.RestVisualScale, 0.12f, 0.25f);
            HeldChanged?.Invoke(tidyObject);
            ObjectPickedUp?.Invoke(tidyObject);
            CarriedChanged?.Invoke();
            SetContext(TidyInteractionContext.Holding);
        }

        bool TrySelectMatching(TidyPlacementTarget target)
        {
            for (var index = carried.Count - 1; index >= 0; index--)
            {
                var candidate = carried[index];
                if (target.Belongs(candidate) && target.FitsGroup(candidate) && target.IsCompatible(candidate) && !target.IsFull)
                {
                    SwapActive(candidate);
                    return true;
                }
            }

            return false;
        }

        void SwapActive(TidyObject next)
        {
            var index = carried.IndexOf(next);
            var previous = HeldObject;
            previous.MarkCarried();
            carried[index] = previous;
            carriedYaw[previous] = carriedYaw.TryGetValue(next, out var yaw) ? yaw : 0f;
            carriedYaw.Remove(next);
            RestoreVisualScale(next);
            next.MarkHeld();
            HeldObject = next;
            TidyTweenRunner.Punch(next.VisualRoot, next.RestVisualScale, 0.1f, 0.2f);
            HeldChanged?.Invoke(next);
            CarriedChanged?.Invoke();
        }

        bool PromoteNextCarried()
        {
            PruneCarried();
            if (carried.Count == 0)
            {
                return false;
            }

            var next = carried[carried.Count - 1];
            carried.RemoveAt(carried.Count - 1);
            carriedYaw.Remove(next);
            RestoreVisualScale(next);
            next.MarkHeld();
            HeldObject = next;
            heldYaw = 0f;
            TidyTweenRunner.Punch(next.VisualRoot, next.RestVisualScale, 0.08f, 0.2f);
            HeldChanged?.Invoke(next);
            CarriedChanged?.Invoke();
            SetContext(TidyInteractionContext.Holding);
            return true;
        }

        void PruneCarried()
        {
            var removed = false;
            for (var index = carried.Count - 1; index >= 0; index--)
            {
                var item = carried[index];
                if (item == null || !item.gameObject.activeInHierarchy || item.State != TidyObjectState.Held || !item.IsCarried)
                {
                    carried.RemoveAt(index);
                    if (item != null)
                    {
                        carriedYaw.Remove(item);
                        RestoreVisualScale(item);
                    }

                    removed = true;
                }
            }

            if (removed)
            {
                CarriedChanged?.Invoke();
            }
        }

        void MoveStack(float deltaTime)
        {
            var cameraTransform = aimCamera.transform;
            var height = 0f;
            for (var index = 0; index < carried.Count; index++)
            {
                var item = carried[index];
                var visual = item.VisualRoot;
                var ratio = visual == item.transform || item.RestVisualScale.y <= 0f ? 1f : visual.localScale.y / item.RestVisualScale.y;
                var itemHeight = item.ScaledSize.y * ratio;
                var center = cameraTransform.TransformPoint(stackOffset + Vector3.up * (height + itemHeight * 0.5f));
                carriedYaw.TryGetValue(item, out var yaw);
                var rotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y + yaw, 0f);
                holdMotion.MoveControlled(item, center, rotation, deltaTime);
                visual.localScale = Vector3.Lerp(visual.localScale, item.RestVisualScale * stackVisualScale, 1f - Mathf.Exp(-12f * deltaTime));
                height += itemHeight + stackSpacing;
            }
        }

        static void RestoreVisualScale(TidyObject tidyObject)
        {
            TidyTweenRunner.Kill(tidyObject.VisualRoot);
            tidyObject.VisualRoot.localScale = tidyObject.RestVisualScale;
        }

        bool FitsInStack(TidyObject tidyObject)
        {
            if (!tidyObject.CanCarryInStack || (StackFilter != null && !StackFilter(tidyObject)))
            {
                return false;
            }

            var size = tidyObject.ScaledSize;
            return Mathf.Max(size.x, Mathf.Max(size.y, size.z)) <= maximumStackedSize;
        }

        void SettleObjectsRestingOn(TidyObject support, Bounds supportBounds)
        {
            var objects = TidyObjectRegistry.All;
            for (var index = 0; index < objects.Count; index++)
            {
                var candidate = objects[index];
                if (candidate == null || candidate == support || candidate.State != TidyObjectState.Loose || candidate.UsesPhysics || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var bounds = candidate.CalculateWorldBounds();
                var restsOnTop = Mathf.Abs(bounds.min.y - supportBounds.max.y) <= RestingTolerance;
                var overlaps = bounds.min.x < supportBounds.max.x && bounds.max.x > supportBounds.min.x && bounds.min.z < supportBounds.max.z && bounds.max.z > supportBounds.min.z;
                if (restsOnTop && overlaps)
                {
                    DropControlled(candidate, false);
                }
            }
        }

        void DropControlled(TidyObject tidyObject, bool throwObject)
        {
            var origin = throwObject ? ThrowLandingPoint() : tidyObject.transform.position + tidyObject.BoundsCenterOffset(tidyObject.transform.rotation);
            var rotation = Quaternion.Euler(0f, tidyObject.transform.eulerAngles.y, 0f);
            tidyObject.MarkMoving();
            if (!TryFindGround(origin, out var groundPoint))
            {
                tidyObject.MarkLoose();
                return;
            }

            var target = tidyObject.PivotPositionForSurface(groundPoint, rotation);
            var duration = tidyObject.PhysicsProfile.ControlledDropDuration * (throwObject ? 1.6f : 1f);
            TidyTweenRunner.MovePose(tidyObject.transform, target, rotation, duration, TidyEaseType.OutBounce, throwObject ? controlledDropArc * 3f : controlledDropArc * 0.2f, tidyObject.MarkLoose);
        }

        Vector3 ThrowLandingPoint()
        {
            var ray = CenterRay();
            if (TryRaycastFirstSolid(ray, out var hit, controlledThrowDistance))
            {
                return hit.point + hit.normal * 0.05f;
            }

            return ray.origin + ray.direction * controlledThrowDistance;
        }

        bool TryFindGround(Vector3 point, out Vector3 groundPoint)
        {
            var probe = new Ray(point + Vector3.up * GroundProbeHeight, Vector3.down);
            var count = Physics.RaycastNonAlloc(probe, hits, GroundProbeDistance, interactionMask, QueryTriggerInteraction.Ignore);
            var bestDistance = float.MaxValue;
            groundPoint = point;
            var found = false;
            for (var index = 0; index < count; index++)
            {
                var hit = hits[index];
                if (IsIgnoredCollider(hit.collider))
                {
                    continue;
                }

                if (TidyObjectRegistry.TryGetFromCollider(hit.collider, out var hitObject) && hitObject.State != TidyObjectState.Loose && hitObject.State != TidyObjectState.Placed)
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    groundPoint = hit.point;
                    found = true;
                }
            }

            return found;
        }

        void BeginInspect()
        {
            IsInspecting = true;
            inspectRotation = Quaternion.Inverse(aimCamera.transform.rotation) * HeldObject.transform.rotation;
            HeldObject.MarkInspecting();
            preview?.Hide();
            TidyControlLock.Acquire(this, TidyControlChannel.Movement | TidyControlChannel.Look);
            SetContext(TidyInteractionContext.Inspecting);
            InspectChanged?.Invoke(HeldObject);
        }

        void EndInspect()
        {
            IsInspecting = false;
            TidyControlLock.Release(this);
            if (HeldObject != null)
            {
                HeldObject.MarkHeld();
            }

            InspectChanged?.Invoke(null);
            SetContext(TidyInteractionContext.Holding);
        }

        void UpdateInspecting(bool inputAllowed)
        {
            if (HeldObject == null)
            {
                EndInspect();
                return;
            }

            if (input != null)
            {
                var look = input.LookDelta * inspectRotateMultiplier;
                inspectRotation = Quaternion.AngleAxis(-look.x, Vector3.up) * Quaternion.AngleAxis(look.y, Vector3.right) * inspectRotation;
                if (input.InspectPressed || input.DropPressed || input.PausePressed || (inputAllowed && input.InteractPressed))
                {
                    EndInspect();
                    return;
                }
            }

            var cameraTransform = aimCamera.transform;
            var offset = inspectOffset;
            offset.z = Mathf.Max(offset.z, HeldObject.ScaledSize.magnitude * 0.8f + aimCamera.nearClipPlane);
            var targetCenter = cameraTransform.TransformPoint(offset);
            holdMotion.MoveControlled(HeldObject, targetCenter, cameraTransform.rotation * inspectRotation, Time.deltaTime);
        }

        Vector3 HoldCenter()
        {
            return aimCamera.transform.TransformPoint(holdOffset);
        }

        Quaternion HoldRotation()
        {
            return Quaternion.Euler(0f, aimCamera.transform.eulerAngles.y + heldYaw, 0f);
        }

        TidyObject FindPickable(Ray ray)
        {
            var count = CastSorted(ray, reachDistance);
            Collider blocker = null;
            var blockerDistance = 0f;
            for (var index = 0; index < count; index++)
            {
                var hit = hits[index];
                if (IsPartOfHeldObject(hit.collider))
                {
                    continue;
                }

                if (blocker != null && hit.distance > blockerDistance + EmbeddedPickDepth)
                {
                    return null;
                }

                if (TidyObjectRegistry.TryGetFromCollider(hit.collider, out var tidyObject))
                {
                    if (blocker != null && !IsEmbeddedIn(tidyObject, blocker))
                    {
                        return null;
                    }

                    return tidyObject.IsAvailableForPickup ? tidyObject : null;
                }

                if (!hit.collider.isTrigger)
                {
                    if (blocker != null)
                    {
                        return null;
                    }

                    blocker = hit.collider;
                    blockerDistance = hit.distance;
                }
            }

            return null;
        }

        static bool IsEmbeddedIn(TidyObject tidyObject, Collider blocker)
        {
            return blocker.bounds.Contains(tidyObject.CalculateWorldBounds().center);
        }

        TidyPlacementCandidate FindPlacement(Ray ray)
        {
            var count = CastSorted(ray, reachDistance);
            for (var index = 0; index < count; index++)
            {
                var hit = hits[index];
                if (IsPartOfHeldObject(hit.collider))
                {
                    continue;
                }

                TidyPlacementTarget target = null;
                if (TidyObjectRegistry.TryGetFromCollider(hit.collider, out var hitObject))
                {
                    target = hitObject.CurrentTarget;
                }
                else
                {
                    TidyTargetRegistry.TryGetFromCollider(hit.collider, out target);
                }

                if (target != null)
                {
                    var query = new TidyPlacementQuery(HeldObject, hit.point, hit.normal, aimCamera.transform.eulerAngles.y + heldYaw);
                    return target.Evaluate(query);
                }

                if (!hit.collider.isTrigger)
                {
                    break;
                }
            }

            return default;
        }

        bool IsPartOfHeldObject(Collider collider)
        {
            return HeldObject != null && TidyObjectRegistry.TryGetFromCollider(collider, out var owner) && (owner == HeldObject || carried.Contains(owner));
        }

        bool TryRaycastFirstSolid(Ray ray, out RaycastHit result, float distance = -1f)
        {
            var count = CastSorted(ray, distance < 0f ? reachDistance : distance);
            for (var index = 0; index < count; index++)
            {
                var hit = hits[index];
                if (hit.collider.isTrigger || IsPartOfHeldObject(hit.collider))
                {
                    continue;
                }

                result = hit;
                return true;
            }

            result = default;
            return false;
        }

        int CastSorted(Ray ray, float distance)
        {
            var radius = aimRadius * Mathf.Max(0.1f, AimAssistMultiplier);
            var count = radius > 0f
                ? Physics.SphereCastNonAlloc(ray, radius, hits, distance, interactionMask, QueryTriggerInteraction.Collide)
                : Physics.RaycastNonAlloc(ray, hits, distance, interactionMask, QueryTriggerInteraction.Collide);
            var kept = 0;
            for (var index = 0; index < count; index++)
            {
                var hit = hits[index];
                if (IsIgnoredCollider(hit.collider) || (hit.distance <= 0f && hit.point == Vector3.zero))
                {
                    continue;
                }

                hits[kept++] = hit;
            }

            SortByDistance(kept);
            return kept;
        }

        bool IsIgnoredCollider(Collider collider)
        {
            return collider == null || (ignoredRoot != null && collider.transform.IsChildOf(ignoredRoot));
        }

        void SortByDistance(int count)
        {
            for (var outer = 1; outer < count; outer++)
            {
                var current = hits[outer];
                var inner = outer - 1;
                while (inner >= 0 && hits[inner].distance > current.distance)
                {
                    hits[inner + 1] = hits[inner];
                    inner--;
                }

                hits[inner + 1] = current;
            }
        }

        void SetHover(TidyObject tidyObject)
        {
            if (HoveredObject == tidyObject)
            {
                return;
            }

            if (HoveredObject != null)
            {
                highlighter?.ClearHighlight(HoveredObject, this);
            }

            HoveredObject = tidyObject;
            if (HoveredObject != null)
            {
                highlighter?.SetHighlight(HoveredObject, HoveredObject.Renderers, TidyHighlightStyle.Hover, this);
            }

            HoverChanged?.Invoke(HoveredObject);
        }

        void ReleaseHeldReference()
        {
            HeldObject = null;
            currentCandidate = default;
            preview?.Hide();
            if (PromoteNextCarried())
            {
                return;
            }

            HeldChanged?.Invoke(null);
            SetContext(TidyInteractionContext.Idle);
        }

        void SetContext(TidyInteractionContext context)
        {
            if (Context == context)
            {
                return;
            }

            Context = context;
            ContextChanged?.Invoke(context);
        }
    }
}
