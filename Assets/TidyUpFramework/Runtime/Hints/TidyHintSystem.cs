using System;
using TidyUpFramework.Cleaning;
using TidyUpFramework.Core;
using TidyUpFramework.Input;
using TidyUpFramework.Interaction;
using TidyUpFramework.Levels;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using UnityEngine;

namespace TidyUpFramework.Hints
{
    public readonly struct TidyHint
    {
        public readonly TidyObject Object;
        public readonly TidyPlacementTarget Target;
        public readonly TidyCleanable Cleanable;
        public readonly Vector3 DestinationPoint;

        public TidyHint(TidyObject tidyObject, TidyPlacementTarget target, TidyCleanable cleanable, Vector3 destinationPoint)
        {
            Object = tidyObject;
            Target = target;
            Cleanable = cleanable;
            DestinationPoint = destinationPoint;
        }

        public bool IsValid => Object != null || Cleanable != null;
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Tidy Up Framework/Hints/Hint System")]
    public sealed class TidyHintSystem : MonoBehaviour
    {
        [SerializeField, Tooltip("Overrides the hint definition of the level.")]
        TidyHintDefinition definitionOverride;

        [SerializeField, Tooltip("Indicator placed above the suggested object.")]
        TidyHintIndicator objectIndicator;

        [SerializeField, Tooltip("Indicator placed above the suggested destination.")]
        TidyHintIndicator destinationIndicator;

        TidyHintDefinition definition;
        ITidyPlayerInput input;
        ITidyHighlighter highlighter;
        TidyLevelManager levelManager;
        TidyInteractor interactor;
        float cooldownRemaining;
        float visibleRemaining;
        float idleTimer;
        TidyHint activeHint;
        bool globallyEnabled = true;

        public event Action<TidyHint> HintShown;
        public event Action HintHidden;
        public event Action<TidyHintSystem> StateChanged;

        public int HintsUsed { get; private set; }
        public float CooldownRemaining => cooldownRemaining;
        public float Cooldown01 => definition == null || definition.CooldownSeconds <= 0f ? 0f : Mathf.Clamp01(cooldownRemaining / definition.CooldownSeconds);
        public bool IsAvailable => globallyEnabled && definition != null && definition.HintsEnabled && cooldownRemaining <= 0f && HasHintsLeft;
        public bool HasHintsLeft => definition == null || definition.MaximumHints < 0 || HintsUsed < definition.MaximumHints;
        public int FreeHintsLeft => definition == null ? 0 : Mathf.Max(0, definition.FreeHints - HintsUsed);
        public int HintsLeft => definition == null || definition.MaximumHints < 0 ? -1 : Mathf.Max(0, definition.MaximumHints - HintsUsed);
        public bool IsHintsFeatureEnabled => globallyEnabled && definition != null && definition.HintsEnabled;
        public TidyHint ActiveHint => activeHint;

        public void SetGloballyEnabled(bool enabledState)
        {
            globallyEnabled = enabledState;
            if (!enabledState)
            {
                HideHint();
            }

            StateChanged?.Invoke(this);
        }

        public bool RequestHint()
        {
            if (!IsAvailable)
            {
                return false;
            }

            var hint = FindHint();
            if (!hint.IsValid)
            {
                return false;
            }

            var cost = HintsUsed >= definition.FreeHints ? definition.ScoreCost : 0;
            HintsUsed++;
            levelManager?.RegisterHintUsed(cost);
            cooldownRemaining = definition.CooldownSeconds;
            Show(hint);
            StateChanged?.Invoke(this);
            return true;
        }

        public TidyHint FindHint()
        {
            var held = interactor != null ? interactor.HeldObject : null;
            if (held != null)
            {
                var target = FindCorrectTarget(held, out var point);
                return target != null ? new TidyHint(held, target, null, point) : default;
            }

            var origin = interactor != null && interactor.AimCamera != null ? interactor.AimCamera.transform.position : transform.position;
            TidyObject bestObject = null;
            TidyPlacementTarget bestTarget = null;
            var bestPoint = Vector3.zero;
            var bestScore = float.MaxValue;
            foreach (var candidate in TidyObjectRegistry.All)
            {
                if (!NeedsSorting(candidate))
                {
                    continue;
                }

                var distance = (candidate.transform.position - origin).sqrMagnitude - candidate.Priority * 4f;
                if (distance >= bestScore)
                {
                    continue;
                }

                var target = FindCorrectTarget(candidate, out var point);
                if (target == null)
                {
                    continue;
                }

                bestScore = distance;
                bestObject = candidate;
                bestTarget = target;
                bestPoint = point;
            }

            if (bestObject != null)
            {
                return new TidyHint(bestObject, bestTarget, null, bestPoint);
            }

            var cleanable = FindNearestDirty(origin);
            return cleanable != null ? new TidyHint(null, null, cleanable, cleanable.transform.position) : default;
        }

        public void HideHint()
        {
            if (!activeHint.IsValid)
            {
                return;
            }

            highlighter?.ClearOwner(this);
            if (objectIndicator != null)
            {
                objectIndicator.Hide();
            }

            if (destinationIndicator != null)
            {
                destinationIndicator.Hide();
            }

            activeHint = default;
            HintHidden?.Invoke();
        }

        static bool NeedsSorting(TidyObject candidate)
        {
            if (candidate == null || !candidate.IsSortable || !candidate.IsMovable || !candidate.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (candidate.State == TidyObjectState.Loose)
            {
                return true;
            }

            return candidate.State == TidyObjectState.Placed && !candidate.IsCorrectlyPlaced && candidate.IsAvailableForPickup;
        }

        static TidyPlacementTarget FindCorrectTarget(TidyObject tidyObject, out Vector3 point)
        {
            TidyPlacementTarget best = null;
            var bestDistance = float.MaxValue;
            point = Vector3.zero;
            foreach (var target in TidyTargetRegistry.All)
            {
                if (target == null || !target.Belongs(tidyObject) || !target.FitsGroup(tidyObject) || target.IsFull || !target.IsCompatible(tidyObject))
                {
                    continue;
                }

                var distance = (target.transform.position - tidyObject.transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = target;
                }
            }

            if (best != null)
            {
                point = best.GetHintPoint(tidyObject);
            }

            return best;
        }

        static TidyCleanable FindNearestDirty(Vector3 origin)
        {
            TidyCleanable best = null;
            var bestDistance = float.MaxValue;
            foreach (var cleanable in TidyCleanable.All)
            {
                if (cleanable == null || cleanable.IsClean || !cleanable.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var distance = (cleanable.transform.position - origin).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = cleanable;
                }
            }

            return best;
        }

        void Show(TidyHint hint)
        {
            HideHint();
            activeHint = hint;
            visibleRemaining = definition.DurationSeconds;
            if (hint.Object != null && definition.HighlightObject)
            {
                highlighter?.SetHighlight(hint.Object, hint.Object.Renderers, TidyHighlightStyle.Hint, this);
            }

            if (definition.ShowArrow && objectIndicator != null)
            {
                var anchor = hint.Object != null ? hint.Object.transform.position + Vector3.up * hint.Object.ScaledSize.y : hint.Cleanable.transform.position;
                objectIndicator.ShowAt(anchor);
            }

            if (hint.Target != null && definition.HighlightDestination && destinationIndicator != null)
            {
                destinationIndicator.ShowAt(hint.DestinationPoint);
            }

            HintShown?.Invoke(hint);
        }

        void Start()
        {
            TidyServices.TryGet(out levelManager);
            TidyServices.TryGet(out interactor);
            definition = definitionOverride != null ? definitionOverride : levelManager != null && levelManager.Level != null ? levelManager.Level.HintDefinition : null;
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<TidyHintDefinition>();
                definition.hideFlags = HideFlags.DontSave;
            }

            if (objectIndicator != null)
            {
                objectIndicator.gameObject.SetActive(false);
            }

            if (destinationIndicator != null)
            {
                destinationIndicator.gameObject.SetActive(false);
            }

            if (interactor != null)
            {
                interactor.ObjectPickedUp += HandleObjectPickedUp;
                interactor.ObjectPlaced += HandleObjectPlaced;
            }

            StateChanged?.Invoke(this);
        }

        void OnEnable()
        {
            TidyServices.Register(this);
        }

        void OnDisable()
        {
            TidyServices.Unregister(this);
        }

        void OnDestroy()
        {
            if (interactor != null)
            {
                interactor.ObjectPickedUp -= HandleObjectPickedUp;
                interactor.ObjectPlaced -= HandleObjectPlaced;
            }
        }

        void HandleObjectPickedUp(TidyObject tidyObject)
        {
            idleTimer = 0f;
            if (activeHint.Object == tidyObject && activeHint.Target != null)
            {
                highlighter?.ClearOwner(this);
                if (objectIndicator != null)
                {
                    objectIndicator.Hide();
                }
            }
            else if (activeHint.IsValid)
            {
                HideHint();
            }
        }

        void HandleObjectPlaced(TidyPlacementCandidate candidate)
        {
            idleTimer = 0f;
            if (activeHint.Object == candidate.Object)
            {
                HideHint();
            }
        }

        void Update()
        {
            if (input == null)
            {
                TidyServices.TryGet(out input);
            }

            if (highlighter == null)
            {
                TidyServices.TryGet(out highlighter);
            }

            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
                StateChanged?.Invoke(this);
            }

            if (activeHint.IsValid)
            {
                visibleRemaining -= Time.deltaTime;
                if (visibleRemaining <= 0f || (activeHint.Cleanable != null && activeHint.Cleanable.IsClean))
                {
                    HideHint();
                }
            }

            if (input != null && input.HintPressed && !TidyControlLock.IsLocked(TidyControlChannel.Interaction))
            {
                RequestHint();
            }

            UpdateAutoHint();
        }

        void UpdateAutoHint()
        {
            if (definition == null || definition.AutoHintAfterIdleSeconds <= 0f || !IsHintsFeatureEnabled || activeHint.IsValid)
            {
                return;
            }

            if (levelManager != null && !levelManager.IsPlaying)
            {
                return;
            }

            idleTimer += Time.deltaTime;
            if (idleTimer < definition.AutoHintAfterIdleSeconds)
            {
                return;
            }

            idleTimer = 0f;
            var hint = FindHint();
            if (hint.IsValid)
            {
                Show(hint);
            }
        }
    }
}
