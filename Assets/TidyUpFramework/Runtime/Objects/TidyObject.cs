using System;
using System.Collections.Generic;
using TidyUpFramework.Core;
using TidyUpFramework.Placement;
using UnityEngine;

namespace TidyUpFramework.Objects
{
    public enum TidyObjectState
    {
        Loose,
        Held,
        Moving,
        Placed,
        Inspecting,
        Consumed
    }

    [DisallowMultipleComponent]
    [SelectionBase]
    [AddComponentMenu("Tidy Up Framework/Tidy Object")]
    public sealed class TidyObject : MonoBehaviour
    {
        static TidyPhysicsProfile fallbackPhysicsProfile;

        [SerializeField, Tooltip("Data asset describing what this object is.")]
        TidyObjectDefinition definition;

        [SerializeField, Tooltip("Unique id inside the scene. Use the editor 'Generate IDs' tool to fill it.")]
        string instanceId;

        [Header("Per Instance Overrides")]
        [SerializeField, Tooltip("Overrides the alphabetical sort key of the definition.")]
        string sortKeyOverride;

        [SerializeField, Tooltip("Overrides the sequence index of the definition. Negative values keep the definition value.")]
        int sequenceOverride = -1;

        [SerializeField, Tooltip("Overrides the physics profile of the definition.")]
        TidyPhysicsProfile physicsProfileOverride;

        [Header("Gameplay Flags")]
        [SerializeField, Tooltip("Counts towards 'Find Objects' objectives until the player picks it up.")]
        bool requiresDiscovery;

        [SerializeField, Tooltip("Counts towards 'Clear Clutter' objectives until it is placed or disposed.")]
        bool countsAsClutter;

        [Header("Presentation")]
        [SerializeField, Tooltip("Transform animated by pickup and placement feedback. Defaults to this transform.")]
        Transform visualRoot;

        [SerializeField, Tooltip("Rotation applied on top of a placement pose, useful for meshes authored in a different orientation.")]
        Vector3 placementEulerOffset;

        Rigidbody body;
        Collider[] colliders = Array.Empty<Collider>();
        Renderer[] renderers = Array.Empty<Renderer>();
        Bounds localBounds;
        Transform originalParent;
        Vector3 restVisualScale = Vector3.one;
        bool isCached;

        public event Action<TidyObject> StateChanged;

        public TidyObjectDefinition Definition => definition;
        public string InstanceId => string.IsNullOrEmpty(instanceId) ? name : instanceId;
        public TidyObjectState State { get; private set; } = TidyObjectState.Loose;
        public TidyPlacementTarget CurrentTarget { get; private set; }
        public bool IsCorrectlyPlaced { get; private set; }
        public bool HasBeenPickedUp { get; private set; }
        public int MistakeCount { get; private set; }
        public bool RequiresDiscovery => requiresDiscovery;
        public bool CountsAsClutter => countsAsClutter;
        public bool IsResolvedClutter => countsAsClutter && (State == TidyObjectState.Placed || State == TidyObjectState.Consumed);

        public string DisplayName => definition != null ? definition.DisplayName : name;
        public TidyCategoryDefinition Category => definition != null ? definition.Category : null;
        public IReadOnlyList<TidyTagDefinition> Tags => definition != null ? definition.Tags : Array.Empty<TidyTagDefinition>();
        public float Size => definition != null ? definition.Size : 1f;
        public float Weight => definition != null ? definition.Weight : 1f;
        public int Priority => definition != null ? definition.Priority : 0;
        public string SortKey => !string.IsNullOrEmpty(sortKeyOverride) ? sortKeyOverride : definition != null ? definition.SortKey : name;
        public int SequenceIndex => sequenceOverride >= 0 ? sequenceOverride : definition != null ? definition.SequenceIndex : 0;
        public int BaseScore => definition != null ? definition.BaseScore : 10;
        public bool IsMovable => definition == null || definition.Movable;
        public bool IsRotatable => definition == null || definition.Rotatable;
        public bool IsPlaceable => definition == null || definition.Placeable;
        public bool IsSortable => definition == null || definition.Sortable;
        public bool IsStackable => definition != null && definition.Stackable;
        public bool IsInspectable => definition == null || definition.Inspectable;
        public bool IsContainerCompatible => definition == null || definition.ContainerCompatible;
        public bool IsSnapCompatible => definition == null || definition.SnapCompatible;
        public bool CanCarryInStack => definition == null || definition.CarryInStack;
        public bool IsCarried { get; private set; }
        public Quaternion PlacementRotationOffset => Quaternion.Euler(placementEulerOffset);

        public TidyPhysicsProfile PhysicsProfile
        {
            get
            {
                if (physicsProfileOverride != null)
                {
                    return physicsProfileOverride;
                }

                if (definition != null && definition.PhysicsProfile != null)
                {
                    return definition.PhysicsProfile;
                }

                if (fallbackPhysicsProfile == null)
                {
                    fallbackPhysicsProfile = ScriptableObject.CreateInstance<TidyPhysicsProfile>();
                    fallbackPhysicsProfile.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallbackPhysicsProfile;
            }
        }

        public bool UsesPhysics => PhysicsProfile.InteractionMode == TidyInteractionMode.Physics;

        public Rigidbody Body
        {
            get
            {
                EnsureCached();
                return body;
            }
        }

        public IReadOnlyList<Collider> Colliders
        {
            get
            {
                EnsureCached();
                return colliders;
            }
        }

        public IReadOnlyList<Renderer> Renderers
        {
            get
            {
                EnsureCached();
                return renderers;
            }
        }

        public Bounds LocalBounds
        {
            get
            {
                EnsureCached();
                return localBounds;
            }
        }

        public Vector3 ScaledSize => Vector3.Scale(LocalBounds.size, transform.localScale);
        public Transform VisualRoot => visualRoot != null ? visualRoot : transform;
        public Vector3 RestVisualScale => restVisualScale;
        public bool IsAvailableForPickup => IsMovable && (State == TidyObjectState.Loose || State == TidyObjectState.Placed) && (CurrentTarget == null || CurrentTarget.AllowsRemoval(this));

        public Bounds CalculateWorldBounds()
        {
            var local = LocalBounds;
            var matrix = transform.localToWorldMatrix;
            var world = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
            var extents = local.extents;
            for (var corner = 0; corner < 8; corner++)
            {
                var offset = new Vector3((corner & 1) == 0 ? -extents.x : extents.x, (corner & 2) == 0 ? -extents.y : extents.y, (corner & 4) == 0 ? -extents.z : extents.z);
                world.Encapsulate(matrix.MultiplyPoint3x4(local.center + offset));
            }

            return world;
        }

        public void RestoreProgressFlags(bool pickedUp, int mistakes)
        {
            HasBeenPickedUp = pickedUp;
            MistakeCount = Mathf.Max(0, mistakes);
        }

        public Vector3 PivotPositionForSurface(Vector3 surfacePoint, Quaternion rotation)
        {
            var bounds = LocalBounds;
            var bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return surfacePoint - rotation * Vector3.Scale(bottomCenter, transform.localScale);
        }

        public Vector3 BoundsCenterOffset(Quaternion rotation)
        {
            return rotation * Vector3.Scale(LocalBounds.center, transform.localScale);
        }

        public bool HasTag(TidyTagDefinition tag)
        {
            return definition != null && definition.HasTag(tag);
        }

        public bool IsInCategory(TidyCategoryDefinition category)
        {
            return definition != null && definition.IsInCategory(category);
        }

        public TidyTagDefinition FindTag(TidyTagGroup group)
        {
            return definition != null ? definition.FindTag(group) : null;
        }

        public void AssignDefinition(TidyObjectDefinition newDefinition)
        {
            definition = newDefinition;
        }

        public void AssignPhysicsProfileOverride(TidyPhysicsProfile profile)
        {
            physicsProfileOverride = profile;
        }

        public void AssignInstanceId(string newInstanceId)
        {
            instanceId = newInstanceId;
        }

        public void ConfigureOverrides(string newSortKey, int newSequence)
        {
            sortKeyOverride = newSortKey;
            sequenceOverride = newSequence;
        }

        public void ConfigureFlags(bool discovery, bool clutter)
        {
            requiresDiscovery = discovery;
            countsAsClutter = clutter;
        }

        public void RefreshCachedComponents()
        {
            isCached = false;
            EnsureCached();
        }

        public void MarkHeld()
        {
            HasBeenPickedUp = true;
            IsCarried = false;
            ClearPlacement();
            ApplyPhysicsForHeld();
            SetState(TidyObjectState.Held);
        }

        public void MarkInspecting()
        {
            SetKinematic(true);
            SetCollidersEnabled(false);
            SetState(TidyObjectState.Inspecting);
        }

        public void MarkCarried()
        {
            HasBeenPickedUp = true;
            IsCarried = true;
            ClearPlacement();
            SetKinematic(true);
            SetCollidersEnabled(false);
            SetState(TidyObjectState.Held);
        }

        public void MarkMoving()
        {
            IsCarried = false;
            SetKinematic(true);
            SetCollidersEnabled(false);
            SetState(TidyObjectState.Moving);
        }

        public void MarkPlaced(TidyPlacementTarget target, bool isCorrect, Transform contentParent)
        {
            CurrentTarget = target;
            IsCorrectlyPlaced = isCorrect;
            if (!isCorrect)
            {
                MistakeCount++;
            }

            if (contentParent != null)
            {
                transform.SetParent(contentParent, true);
            }

            SetKinematic(true);
            SetCollidersEnabled(true);
            SetState(TidyObjectState.Placed);
        }

        public void UpdateCorrectness(bool isCorrect)
        {
            if (IsCorrectlyPlaced == isCorrect)
            {
                return;
            }

            IsCorrectlyPlaced = isCorrect;
            SetState(State);
        }

        public void MarkLoose()
        {
            IsCarried = false;
            ClearPlacement();
            SetCollidersEnabled(true);
            if (UsesPhysics)
            {
                ApplyDynamicBody(true);
            }
            else
            {
                SetKinematic(true);
            }

            SetState(TidyObjectState.Loose);
        }

        public void MarkConsumed()
        {
            IsCarried = false;
            SetCollidersEnabled(false);
            SetKinematic(true);
            SetState(TidyObjectState.Consumed);
        }

        void ClearPlacement()
        {
            CurrentTarget = null;
            IsCorrectlyPlaced = false;
            if (transform.parent != originalParent)
            {
                transform.SetParent(originalParent, true);
            }
        }

        void ApplyPhysicsForHeld()
        {
            if (UsesPhysics)
            {
                ApplyDynamicBody(false);
                SetCollidersEnabled(true);
            }
            else
            {
                SetKinematic(true);
                SetCollidersEnabled(false);
            }
        }

        void ApplyDynamicBody(bool useGravity)
        {
            if (body == null)
            {
                return;
            }

            var profile = PhysicsProfile;
            body.isKinematic = false;
            body.useGravity = useGravity;
            body.mass = profile.Mass;
            body.linearDamping = profile.LinearDamping;
            body.angularDamping = profile.AngularDamping;
            body.collisionDetectionMode = profile.CollisionDetection;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.WakeUp();
        }

        void SetKinematic(bool kinematic)
        {
            if (body == null)
            {
                return;
            }

            if (kinematic && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = kinematic;
            body.interpolation = kinematic ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
            if (kinematic)
            {
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
        }

        void SetCollidersEnabled(bool enabledState)
        {
            foreach (var collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = enabledState;
                }
            }
        }

        void SetState(TidyObjectState newState)
        {
            State = newState;
            StateChanged?.Invoke(this);
            TidyObjectRegistry.NotifyStateChanged(this);
        }

        void EnsureCached()
        {
            if (isCached)
            {
                return;
            }

            isCached = true;
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            localBounds = TidyBoundsUtility.CalculateLocalBounds(transform);
            restVisualScale = VisualRoot.localScale;
        }

        void Awake()
        {
            EnsureCached();
            originalParent = transform.parent;
            if (body != null && !UsesPhysics)
            {
                body.isKinematic = true;
            }

            TidyObjectRegistry.Register(this);
        }

        void OnDestroy()
        {
            TidyObjectRegistry.Unregister(this);
        }

        void OnValidate()
        {
            isCached = false;
        }
    }
}
