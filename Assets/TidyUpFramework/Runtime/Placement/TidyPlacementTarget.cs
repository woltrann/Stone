using System;
using System.Collections.Generic;
using TidyUpFramework.Core;
using TidyUpFramework.Objects;
using TidyUpFramework.Rules;
using UnityEngine;

namespace TidyUpFramework.Placement
{
    [Serializable]
    public sealed class TidyPlacementMotion
    {
        [Min(0f)] public float Duration = 0.32f;
        public TidyEaseType Ease = TidyEaseType.OutCubic;
        [Min(0f)] public float ArcHeight = 0.12f;
        [Min(0f)] public float LandingPunch = 0.12f;
        [Min(0f)] public float LandingPunchDuration = 0.25f;
    }

    public abstract class TidyPlacementTarget : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Unique id inside the scene. Objectives, hints and tutorials reference targets by this id.")]
        string targetId;

        [SerializeField]
        string displayName;

        [SerializeField]
        Color gizmoColor = new Color(0.35f, 0.85f, 0.55f, 1f);

        [Header("Rules")]
        [SerializeField, Tooltip("Objects matching this filter belong here and count as correctly placed.")]
        TidyMatchFilter acceptance = new TidyMatchFilter();

        [SerializeField, Tooltip("RejectIncorrect: wrong objects bounce back. AcceptIncorrect: wrong objects stay but count as mistakes.")]
        TidyPlacementPolicy policy = TidyPlacementPolicy.RejectIncorrect;

        [SerializeField, Tooltip("Required order of the occupants from first slot to last slot.")]
        TidyOrderingMode ordering = TidyOrderingMode.None;

        [SerializeField]
        TidyOrderingDirection orderingDirection = TidyOrderingDirection.Ascending;

        [SerializeField, Tooltip("Requires every occupant to share the same definition, category or tag.")]
        TidyGroupingMode grouping = TidyGroupingMode.None;

        [SerializeField, Tooltip("Tag group compared when grouping is set to Same Tag In Group.")]
        TidyTagGroup groupingTagGroup = TidyTagGroup.Color;

        [Header("Completion")]
        [SerializeField]
        TidyCompletionMode completionMode = TidyCompletionMode.WhenFull;

        [SerializeField, Min(1), Tooltip("Used when completion mode is 'When Required Count Reached'.")]
        int requiredCount = 1;

        [SerializeField, Tooltip("Whether this target contributes to the level organisation progress.")]
        bool countsTowardsProgress = true;

        [Header("Behaviour")]
        [SerializeField]
        bool allowRemoval = true;

        [SerializeField, Tooltip("Correctly placed objects cannot be taken out again.")]
        bool lockCorrectObjects;

        [SerializeField, Tooltip("Once complete, no object can be taken out.")]
        bool lockWhenComplete;

        [SerializeField, Tooltip("Parent for placed objects. Defaults to this transform.")]
        Transform contentRoot;

        [SerializeField]
        TidyPlacementMotion motion = new TidyPlacementMotion();

        [SerializeField, Tooltip("Objects that start inside this target. They are snapped silently when the scene starts.")]
        List<TidyObject> initialOccupants = new List<TidyObject>();

        protected readonly List<TidyObject> occupants = new List<TidyObject>();
        readonly Dictionary<TidyObject, bool> correctnessByOccupant = new Dictionary<TidyObject, bool>();
        bool isComplete;

        public event Action<TidyPlacementEvent> ObjectPlaced;
        public event Action<TidyPlacementEvent> ObjectRemoved;
        public event Action<TidyPlacementTarget, bool> CompletionChanged;

        public string TargetId => string.IsNullOrEmpty(targetId) ? name : targetId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Color GizmoColor => gizmoColor;
        public TidyMatchFilter Acceptance => acceptance;
        public TidyPlacementPolicy Policy => policy;
        public TidyOrderingMode Ordering => ordering;
        public TidyGroupingMode Grouping => grouping;
        public TidyCompletionMode CompletionMode => completionMode;
        public bool CountsTowardsProgress => countsTowardsProgress;
        public Transform ContentRoot => contentRoot != null ? contentRoot : transform;
        public TidyPlacementMotion Motion => motion;
        public IReadOnlyList<TidyObject> Occupants => occupants;
        public IReadOnlyList<TidyObject> InitialOccupants => initialOccupants;
        public bool IsComplete => isComplete;
        public bool IsFull => occupants.Count >= Capacity;
        public abstract int Capacity { get; }

        public int CorrectCount
        {
            get
            {
                var count = 0;
                foreach (var occupant in occupants)
                {
                    if (IsOccupantCorrect(occupant))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int RequiredForCompletion
        {
            get
            {
                switch (completionMode)
                {
                    case TidyCompletionMode.WhenRequiredCountReached:
                        return requiredCount;
                    case TidyCompletionMode.WhenAllMatchingObjectsPlaced:
                        return Mathf.Max(1, CountMatchingObjectsInScene());
                    case TidyCompletionMode.Never:
                        return 0;
                    default:
                        return Capacity == int.MaxValue ? Mathf.Max(1, occupants.Count) : Capacity;
                }
            }
        }

        public float CompletionRatio
        {
            get
            {
                var required = RequiredForCompletion;
                return required <= 0 ? 0f : Mathf.Clamp01((float)CorrectCount / required);
            }
        }

        public bool IsOrderSatisfied => TidyOrdering.IsOrdered(GetOrderedOccupants(), ordering, orderingDirection);
        public bool IsGroupingSatisfied => TidyOrdering.IsGrouped(GetOrderedOccupants(), grouping, groupingTagGroup);

        public void ConfigureIdentity(string newTargetId, string newDisplayName)
        {
            targetId = newTargetId;
            displayName = newDisplayName;
        }

        public void ConfigureRules(TidyMatchFilter newAcceptance, TidyPlacementPolicy newPolicy, TidyCompletionMode newCompletionMode, int newRequiredCount)
        {
            acceptance = newAcceptance ?? new TidyMatchFilter();
            policy = newPolicy;
            completionMode = newCompletionMode;
            requiredCount = Mathf.Max(1, newRequiredCount);
        }

        public void ConfigureOrdering(TidyOrderingMode newOrdering, TidyOrderingDirection newDirection, TidyGroupingMode newGrouping, TidyTagGroup newTagGroup)
        {
            ordering = newOrdering;
            orderingDirection = newDirection;
            grouping = newGrouping;
            groupingTagGroup = newTagGroup;
        }

        public void ConfigureBehaviour(bool canRemove, bool lockCorrect, bool lockComplete)
        {
            allowRemoval = canRemove;
            lockCorrectObjects = lockCorrect;
            lockWhenComplete = lockComplete;
        }

        public void SetGizmoColor(Color color)
        {
            gizmoColor = color;
        }

        public void SetInitialOccupants(IEnumerable<TidyObject> objects)
        {
            initialOccupants = new List<TidyObject>(objects);
        }

        public bool Belongs(TidyObject tidyObject)
        {
            return tidyObject != null && acceptance.Matches(tidyObject);
        }

        public bool FitsGroup(TidyObject tidyObject)
        {
            if (grouping == TidyGroupingMode.None || tidyObject == null)
            {
                return true;
            }

            foreach (var occupant in occupants)
            {
                if (occupant != null && occupant != tidyObject && IsOccupantCorrect(occupant))
                {
                    return Equals(TidyOrdering.GroupKey(occupant, grouping, groupingTagGroup), TidyOrdering.GroupKey(tidyObject, grouping, groupingTagGroup));
                }
            }

            return true;
        }

        public virtual bool IsCompatible(TidyObject tidyObject)
        {
            return tidyObject != null && tidyObject.IsPlaceable;
        }

        public bool IsOccupantCorrect(TidyObject tidyObject)
        {
            return tidyObject != null && correctnessByOccupant.TryGetValue(tidyObject, out var correct) && correct;
        }

        public virtual bool AllowsRemoval(TidyObject tidyObject)
        {
            if (!allowRemoval)
            {
                return false;
            }

            if (lockWhenComplete && isComplete)
            {
                return false;
            }

            return !(lockCorrectObjects && IsOccupantCorrect(tidyObject));
        }

        public TidyPlacementCandidate Evaluate(TidyPlacementQuery query)
        {
            var candidate = new TidyPlacementCandidate { Target = this, Object = query.Object, SlotIndex = -1 };
            var tidyObject = query.Object;
            if (tidyObject == null || !tidyObject.IsPlaceable)
            {
                candidate.Rejection = TidyRejectionReason.NotPlaceable;
                return candidate;
            }

            if (!IsCompatible(tidyObject))
            {
                candidate.Rejection = TidyRejectionReason.Incompatible;
                return candidate;
            }

            if (occupants.Contains(tidyObject))
            {
                candidate.Rejection = TidyRejectionReason.Locked;
                return candidate;
            }

            if (IsFull)
            {
                candidate.Rejection = TidyRejectionReason.Full;
                return candidate;
            }

            if (!TryFindPose(query, out candidate.Position, out candidate.Rotation, out candidate.SlotIndex))
            {
                candidate.Rejection = TidyRejectionReason.NoSpace;
                return candidate;
            }

            candidate.IsCorrect = Belongs(tidyObject) && FitsGroup(tidyObject);
            if (!candidate.IsCorrect && policy == TidyPlacementPolicy.RejectIncorrect)
            {
                candidate.Rejection = TidyRejectionReason.WrongObject;
                return candidate;
            }

            candidate.IsValid = true;
            return candidate;
        }

        public bool TryPlace(TidyPlacementCandidate candidate, bool animate = true)
        {
            if (!candidate.IsValid || candidate.Target != this || candidate.Object == null)
            {
                return false;
            }

            var tidyObject = candidate.Object;
            InsertOccupant(tidyObject, candidate.SlotIndex);
            correctnessByOccupant[tidyObject] = candidate.IsCorrect;
            tidyObject.MarkMoving();
            if (animate)
            {
                TidyTweenRunner.MovePose(tidyObject.transform, candidate.Position, candidate.Rotation, motion.Duration, motion.Ease, motion.ArcHeight, () => CompletePlacement(tidyObject, true));
            }
            else
            {
                tidyObject.transform.SetPositionAndRotation(candidate.Position, candidate.Rotation);
                CompletePlacement(tidyObject, false);
            }

            OnOccupantsChanged(tidyObject, animate);
            return true;
        }

        public bool TryPlaceDirect(TidyObject tidyObject, bool animate)
        {
            var candidate = Evaluate(TidyPlacementQuery.WithoutAim(tidyObject));
            if (!candidate.IsValid && candidate.HasPose && candidate.Rejection == TidyRejectionReason.WrongObject)
            {
                candidate.IsValid = true;
            }

            return TryPlace(candidate, animate);
        }

        public bool Release(TidyObject tidyObject)
        {
            if (tidyObject == null || !occupants.Contains(tidyObject))
            {
                return false;
            }

            var wasCorrect = IsOccupantCorrect(tidyObject);
            RemoveOccupant(tidyObject);
            correctnessByOccupant.Remove(tidyObject);
            TidyTweenRunner.Kill(tidyObject.transform, TidyTweenKind.Pose);
            OnOccupantsChanged(null, true);
            var removal = new TidyPlacementEvent(tidyObject, this, wasCorrect, true);
            ObjectRemoved?.Invoke(removal);
            TidyTargetRegistry.RaiseRemoved(removal);
            RefreshCompletion();
            return true;
        }

        public virtual Vector3 GetHintPoint(TidyObject tidyObject)
        {
            if (tidyObject != null)
            {
                var candidate = Evaluate(TidyPlacementQuery.WithoutAim(tidyObject));
                if (candidate.HasPose)
                {
                    return candidate.Position + Vector3.up * tidyObject.ScaledSize.y * 0.5f;
                }
            }

            return transform.position;
        }

        public virtual IReadOnlyList<TidyObject> GetOrderedOccupants()
        {
            return occupants;
        }

        public void RefreshCompletion()
        {
            var complete = EvaluateCompletion();
            if (complete == isComplete)
            {
                return;
            }

            isComplete = complete;
            CompletionChanged?.Invoke(this, isComplete);
            TidyTargetRegistry.RaiseCompletionChanged(this, isComplete);
        }

        protected abstract bool TryFindPose(TidyPlacementQuery query, out Vector3 position, out Quaternion rotation, out int slotIndex);

        protected virtual void InsertOccupant(TidyObject tidyObject, int slotIndex)
        {
            occupants.Add(tidyObject);
        }

        protected virtual void RemoveOccupant(TidyObject tidyObject)
        {
            occupants.Remove(tidyObject);
        }

        protected virtual void OnOccupantsChanged(TidyObject insertedObject, bool animate)
        {
        }

        protected virtual void OnPlacementCompleted(TidyObject tidyObject)
        {
        }

        protected Vector3 PivotForBottomAt(TidyObject tidyObject, Vector3 surfacePoint, Quaternion rotation)
        {
            return tidyObject.PivotPositionForSurface(surfacePoint, rotation);
        }

        protected Vector3 RotatedSize(TidyObject tidyObject, Quaternion localRotation)
        {
            var size = tidyObject.ScaledSize;
            var right = localRotation * Vector3.right;
            var up = localRotation * Vector3.up;
            var forward = localRotation * Vector3.forward;
            return new Vector3(
                Mathf.Abs(right.x) * size.x + Mathf.Abs(up.x) * size.y + Mathf.Abs(forward.x) * size.z,
                Mathf.Abs(right.y) * size.x + Mathf.Abs(up.y) * size.y + Mathf.Abs(forward.y) * size.z,
                Mathf.Abs(right.z) * size.x + Mathf.Abs(up.z) * size.y + Mathf.Abs(forward.z) * size.z);
        }

        protected void MoveOccupant(TidyObject tidyObject, Vector3 position, Quaternion rotation, bool animate)
        {
            if (tidyObject == null)
            {
                return;
            }

            if (animate)
            {
                TidyTweenRunner.Retarget(tidyObject.transform, position, rotation, motion.Duration * 0.75f, TidyEaseType.OutCubic);
            }
            else
            {
                tidyObject.transform.SetPositionAndRotation(position, rotation);
            }
        }

        void CompletePlacement(TidyObject tidyObject, bool raiseEvents)
        {
            if (!occupants.Contains(tidyObject))
            {
                return;
            }

            var isCorrect = IsOccupantCorrect(tidyObject);
            tidyObject.MarkPlaced(this, isCorrect, ContentRoot);
            if (raiseEvents && motion.LandingPunch > 0f)
            {
                TidyTweenRunner.Punch(tidyObject.VisualRoot, tidyObject.RestVisualScale, motion.LandingPunch, motion.LandingPunchDuration);
            }

            OnPlacementCompleted(tidyObject);
            if (raiseEvents)
            {
                var placement = new TidyPlacementEvent(tidyObject, this, isCorrect, false);
                ObjectPlaced?.Invoke(placement);
                TidyTargetRegistry.RaisePlaced(placement);
            }

            RefreshCompletion();
        }

        bool EvaluateCompletion()
        {
            if (completionMode == TidyCompletionMode.Never || occupants.Count == 0)
            {
                return false;
            }

            if (!IsOrderSatisfied || !IsGroupingSatisfied)
            {
                return false;
            }

            var correctCount = CorrectCount;
            switch (completionMode)
            {
                case TidyCompletionMode.WhenRequiredCountReached:
                    return correctCount >= requiredCount;
                case TidyCompletionMode.WhenAllMatchingObjectsPlaced:
                    return correctCount == occupants.Count && correctCount >= CountMatchingObjectsInScene();
                default:
                    return IsFull && correctCount == occupants.Count;
            }
        }

        int CountMatchingObjectsInScene()
        {
            var count = 0;
            foreach (var tidyObject in TidyObjectRegistry.All)
            {
                if (tidyObject != null && tidyObject.State != TidyObjectState.Consumed && Belongs(tidyObject))
                {
                    count++;
                }
            }

            return count;
        }

        protected virtual void Awake()
        {
            TidyTargetRegistry.Register(this);
        }

        protected virtual void Start()
        {
            foreach (var tidyObject in initialOccupants)
            {
                if (tidyObject != null && tidyObject.State != TidyObjectState.Placed)
                {
                    TryPlaceDirect(tidyObject, false);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            TidyTargetRegistry.Unregister(this);
        }

        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.03f);
        }
    }
}
