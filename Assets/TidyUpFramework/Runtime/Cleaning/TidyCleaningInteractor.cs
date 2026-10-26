using System;
using TidyUpFramework.Core;
using TidyUpFramework.Input;
using TidyUpFramework.Interaction;
using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Cleaning
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(TidyInteractor))]
    [AddComponentMenu("Tidy Up Framework/Cleaning/Cleaning Interactor")]
    public sealed class TidyCleaningInteractor : MonoBehaviour
    {
        const int MaximumOverlaps = 32;

        [SerializeField, Min(0f), Tooltip("Scrubbing speed with bare hands, in cleaning seconds per real second.")]
        float handScrubSpeed = 1f;

        [SerializeField, Min(0.01f)]
        float vacuumTravelDuration = 0.25f;

        readonly Collider[] overlaps = new Collider[MaximumOverlaps];
        TidyInteractor interactor;
        ITidyPlayerInput input;
        TidyCleaningTool activeTool;

        public event Action<TidyCleanable> HoveredCleanableChanged;

        public TidyCleanable HoveredCleanable { get; private set; }
        public TidyCleanable BlockedCleanable { get; private set; }
        public bool IsCleaning { get; private set; }

        void Awake()
        {
            interactor = GetComponent<TidyInteractor>();
            interactor.StackFilter = candidate => candidate.GetComponent<TidyCleaningTool>() == null;
        }

        void Update()
        {
            if (input == null)
            {
                TidyServices.TryGet(out input);
            }

            var held = interactor.HeldObject;
            var tool = held != null ? held.GetComponent<TidyCleaningTool>() : null;
            UpdateHover(held, tool);
            var wantsToClean = input != null && input.InteractHeld && !TidyControlLock.IsLocked(TidyControlChannel.Interaction) && !interactor.IsInspecting;
            var canClean = held == null || tool != null;
            IsCleaning = wantsToClean && canClean && (HoveredCleanable != null || (tool != null && tool.CollectsClutter));
            SetToolVisual(tool, IsCleaning);
            if (!IsCleaning)
            {
                return;
            }

            if (tool != null)
            {
                CleanWithTool(held, tool);
            }
            else if (HoveredCleanable != null && HoveredCleanable.AcceptsTool(null))
            {
                HoveredCleanable.Scrub(handScrubSpeed * Time.deltaTime);
            }
        }

        void UpdateHover(TidyObject held, TidyCleaningTool tool)
        {
            TidyCleanable hovered = null;
            TidyCleanable blocked = null;
            if ((held == null || tool != null) && interactor.TryFindAimed(out hovered, out _))
            {
                if (hovered.IsClean)
                {
                    hovered = null;
                }
                else if (!hovered.AcceptsTool(held))
                {
                    blocked = hovered;
                    hovered = null;
                }
            }

            if (hovered == HoveredCleanable && blocked == BlockedCleanable)
            {
                return;
            }

            HoveredCleanable = hovered;
            BlockedCleanable = blocked;
            HoveredCleanableChanged?.Invoke(hovered);
        }

        void CleanWithTool(TidyObject held, TidyCleaningTool tool)
        {
            var hasAim = interactor.TryAim(out var hit);
            var center = hasAim ? hit.point : interactor.CenterRay().GetPoint(interactor.ReachDistance * 0.5f);
            var amount = handScrubSpeed * tool.CleaningSpeedMultiplier * Time.deltaTime;
            if (tool.CleaningRadius > 0f)
            {
                var count = Physics.OverlapSphereNonAlloc(center, tool.CleaningRadius, overlaps, interactor.InteractionMask, QueryTriggerInteraction.Collide);
                for (var index = 0; index < count; index++)
                {
                    var cleanable = overlaps[index].GetComponentInParent<TidyCleanable>();
                    if (cleanable != null && cleanable.AcceptsTool(held))
                    {
                        cleanable.Scrub(amount);
                    }
                }
            }
            else if (HoveredCleanable != null)
            {
                HoveredCleanable.Scrub(amount);
            }

            if (tool.CollectsClutter)
            {
                CollectClutter(held, center, tool.CollectionRadius);
            }
        }

        void CollectClutter(TidyObject held, Vector3 center, float radius)
        {
            var radiusSquared = radius * radius;
            var objects = TidyObjectRegistry.All;
            for (var index = objects.Count - 1; index >= 0; index--)
            {
                var candidate = objects[index];
                if (candidate == null || candidate == held || !candidate.CountsAsClutter || candidate.State != TidyObjectState.Loose)
                {
                    continue;
                }

                if ((candidate.transform.position - center).sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                candidate.MarkMoving();
                var destination = held.transform;
                TidyTweenRunner.ScaleTo(candidate.VisualRoot, Vector3.zero, vacuumTravelDuration, TidyEaseType.InQuad);
                TidyTweenRunner.MovePose(candidate.transform, destination.position, candidate.transform.rotation, vacuumTravelDuration, TidyEaseType.InQuad, 0f, () => Dispose(candidate));
            }
        }

        static void Dispose(TidyObject candidate)
        {
            if (candidate == null)
            {
                return;
            }

            candidate.MarkConsumed();
            candidate.gameObject.SetActive(false);
        }

        void SetToolVisual(TidyCleaningTool tool, bool active)
        {
            if (activeTool != null && activeTool != tool)
            {
                activeTool.SetActiveVisual(false);
            }

            activeTool = tool;
            if (activeTool != null)
            {
                activeTool.SetActiveVisual(active);
            }
        }
    }
}
