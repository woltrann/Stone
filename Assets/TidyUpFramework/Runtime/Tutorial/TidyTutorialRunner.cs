using System;
using System.Collections.Generic;
using TidyUpFramework.Cleaning;
using TidyUpFramework.Core;
using TidyUpFramework.Hints;
using TidyUpFramework.Input;
using TidyUpFramework.Interaction;
using TidyUpFramework.Levels;
using TidyUpFramework.Objectives;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Save;
using TidyUpFramework.Settings;
using UnityEngine;

namespace TidyUpFramework.Tutorial
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Tidy Up Framework/Tutorial/Tutorial Runner")]
    public sealed class TidyTutorialRunner : MonoBehaviour
    {
        const int MaximumHighlightedObjects = 6;
        const float RequiredMoveDistance = 2f;
        const float RequiredLookDegrees = 90f;
        const float RequiredRotateDegrees = 30f;

        [SerializeField, Tooltip("Overrides the tutorial assigned to the level.")]
        TidyTutorialDefinition tutorialOverride;

        [SerializeField]
        TidyHintIndicator targetIndicator;

        [SerializeField, Min(0f), Tooltip("Delay before the first step appears.")]
        float startDelay = 1f;

        [SerializeField, Tooltip("Play even if the player completed it before. Useful while testing.")]
        bool ignorePlayOnce;

        readonly List<TidyObject> highlighted = new List<TidyObject>();
        TidyTutorialDefinition tutorial;
        TidyInteractor interactor;
        TidyLevelManager levelManager;
        TidyHintSystem hintSystem;
        ITidyPlayerInput input;
        ITidyHighlighter highlighter;
        Transform playerTransform;
        Vector3 lastPlayerPosition;
        float accumulated;
        float stepTimer;
        float startTimer;
        bool started;
        bool stepCompletedThisFrame;

        public event Action<TidyTutorialStep, int, int> StepStarted;
        public event Action<TidyTutorialStep> StepCompleted;
        public event Action TutorialFinished;

        public bool IsRunning { get; private set; }
        public int StepIndex { get; private set; } = -1;
        public TidyTutorialStep CurrentStep => tutorial != null && StepIndex >= 0 && StepIndex < tutorial.Steps.Count ? tutorial.Steps[StepIndex] : null;

        public void Continue()
        {
            var step = CurrentStep;
            if (step != null && (step.Completion == TidyTutorialCompletion.PressContinue || step.Completion == TidyTutorialCompletion.Timer))
            {
                CompleteStep();
            }
        }

        public void Skip()
        {
            if (IsRunning)
            {
                Finish();
                return;
            }

            if (!started && tutorial != null)
            {
                started = true;
                Finish();
            }
        }

        void Start()
        {
            TidyServices.TryGet(out interactor);
            TidyServices.TryGet(out levelManager);
            TidyServices.TryGet(out hintSystem);
            tutorial = tutorialOverride != null ? tutorialOverride : levelManager != null && levelManager.Level != null ? levelManager.Level.Tutorial : null;
            if (targetIndicator != null)
            {
                targetIndicator.gameObject.SetActive(false);
            }

            if (!ShouldRun())
            {
                enabled = false;
                return;
            }

            startTimer = startDelay;
            Subscribe();
        }

        bool ShouldRun()
        {
            if (tutorial == null || tutorial.Steps.Count == 0 || (levelManager != null && (levelManager.HasPendingRestore || levelManager.WasRestored)))
            {
                return false;
            }

            if (TidyServices.TryGet<TidySettingsService>(out var settings) && !settings.Current.TutorialEnabled)
            {
                return false;
            }

            if (ignorePlayOnce || !tutorial.PlayOnlyOnce)
            {
                return true;
            }

            return !TidyServices.TryGet<TidySaveService>(out var save) || !save.IsTutorialCompleted(tutorial.TutorialId);
        }

        void Subscribe()
        {
            if (interactor != null)
            {
                interactor.ObjectPickedUp += HandlePickedUp;
                interactor.ObjectPlaced += HandlePlaced;
                interactor.ObjectDropped += HandleDropped;
                interactor.InspectChanged += HandleInspect;
                var controller = interactor.GetComponentInParent<Player.TidyFirstPersonController>();
                playerTransform = controller != null ? controller.transform : interactor.transform;
            }

            if (levelManager != null)
            {
                levelManager.ObjectiveCompleted += HandleObjectiveCompleted;
            }

            if (hintSystem != null)
            {
                hintSystem.HintShown += HandleHint;
            }

            TidyCleanable.AnyCleaned += HandleCleaned;
        }

        void OnDestroy()
        {
            if (interactor != null)
            {
                interactor.ObjectPickedUp -= HandlePickedUp;
                interactor.ObjectPlaced -= HandlePlaced;
                interactor.ObjectDropped -= HandleDropped;
                interactor.InspectChanged -= HandleInspect;
            }

            if (levelManager != null)
            {
                levelManager.ObjectiveCompleted -= HandleObjectiveCompleted;
            }

            if (hintSystem != null)
            {
                hintSystem.HintShown -= HandleHint;
            }

            TidyCleanable.AnyCleaned -= HandleCleaned;
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

            if (!started)
            {
                startTimer -= Time.deltaTime;
                if (startTimer <= 0f)
                {
                    started = true;
                    IsRunning = true;
                    BeginStep(0);
                }

                return;
            }

            if (!IsRunning)
            {
                return;
            }

            stepCompletedThisFrame = false;
            UpdateStep(CurrentStep);
        }

        void UpdateStep(TidyTutorialStep step)
        {
            if (step == null)
            {
                return;
            }

            switch (step.Completion)
            {
                case TidyTutorialCompletion.PressContinue:
                    if (input != null && input.InteractPressed && interactor != null && interactor.HoveredObject == null && interactor.HeldObject == null)
                    {
                        CompleteStep();
                    }

                    break;
                case TidyTutorialCompletion.Timer:
                    stepTimer -= Time.deltaTime;
                    if (stepTimer <= 0f)
                    {
                        CompleteStep();
                    }

                    break;
                case TidyTutorialCompletion.MovePlayer:
                    if (playerTransform != null)
                    {
                        accumulated += Vector3.Distance(playerTransform.position, lastPlayerPosition);
                        lastPlayerPosition = playerTransform.position;
                        if (accumulated >= RequiredMoveDistance)
                        {
                            CompleteStep();
                        }
                    }

                    break;
                case TidyTutorialCompletion.LookAround:
                    if (input != null)
                    {
                        accumulated += input.LookDelta.magnitude;
                        if (accumulated >= RequiredLookDegrees)
                        {
                            CompleteStep();
                        }
                    }

                    break;
                case TidyTutorialCompletion.RotateHeldObject:
                    if (input != null && interactor != null && interactor.HeldObject != null)
                    {
                        accumulated += Mathf.Abs(input.RotateAxis) * 180f * Time.deltaTime + Mathf.Abs(input.RotateSteps) * 15f;
                        if (accumulated >= RequiredRotateDegrees)
                        {
                            CompleteStep();
                        }
                    }

                    break;
            }
        }

        void BeginStep(int index)
        {
            ClearHighlights();
            StepIndex = index;
            var step = CurrentStep;
            if (step == null)
            {
                Finish();
                return;
            }

            accumulated = 0f;
            stepTimer = step.DurationSeconds;
            lastPlayerPosition = playerTransform != null ? playerTransform.position : Vector3.zero;
            ApplyHighlights(step);
            StepStarted?.Invoke(step, index, tutorial.Steps.Count);
        }

        void CompleteStep()
        {
            if (stepCompletedThisFrame || !IsRunning)
            {
                return;
            }

            stepCompletedThisFrame = true;
            var step = CurrentStep;
            StepCompleted?.Invoke(step);
            BeginStep(StepIndex + 1);
        }

        void Finish()
        {
            ClearHighlights();
            IsRunning = false;
            StepIndex = -1;
            if (TidyServices.TryGet<TidySaveService>(out var save))
            {
                save.MarkTutorialCompleted(tutorial.TutorialId);
            }

            TutorialFinished?.Invoke();
        }

        void ApplyHighlights(TidyTutorialStep step)
        {
            if (step.HighlightObjects && !step.ObjectFilter.IsEmpty && highlighter != null)
            {
                foreach (var tidyObject in TidyObjectRegistry.All)
                {
                    if (highlighted.Count >= MaximumHighlightedObjects)
                    {
                        break;
                    }

                    if (tidyObject != null && tidyObject.State == TidyObjectState.Loose && step.ObjectFilter.Matches(tidyObject))
                    {
                        highlighter.SetHighlight(tidyObject, tidyObject.Renderers, TidyHighlightStyle.Hint, this);
                        highlighted.Add(tidyObject);
                    }
                }
            }

            if (targetIndicator == null || !step.ShowArrow)
            {
                return;
            }

            var target = TidyTargetRegistry.FindById(step.TargetId);
            if (target != null && step.HighlightTarget)
            {
                targetIndicator.ShowAt(target.transform.position);
            }
            else if (highlighted.Count > 0)
            {
                targetIndicator.ShowAt(highlighted[0].transform.position + Vector3.up * highlighted[0].ScaledSize.y);
            }
        }

        void ClearHighlights()
        {
            highlighter?.ClearOwner(this);
            highlighted.Clear();
            if (targetIndicator != null)
            {
                targetIndicator.Hide();
            }
        }

        void HandlePickedUp(TidyObject tidyObject)
        {
            var step = CurrentStep;
            if (step == null)
            {
                return;
            }

            if (step.Completion == TidyTutorialCompletion.PickUpAny || (step.Completion == TidyTutorialCompletion.PickUpMatching && step.ObjectFilter.Matches(tidyObject)))
            {
                CompleteStep();
            }
        }

        void HandlePlaced(TidyPlacementCandidate candidate)
        {
            var step = CurrentStep;
            if (step == null)
            {
                return;
            }

            switch (step.Completion)
            {
                case TidyTutorialCompletion.PlaceAny:
                    CompleteStep();
                    break;
                case TidyTutorialCompletion.PlaceCorrectly:
                    if (candidate.IsCorrect && (step.ObjectFilter.IsEmpty || step.ObjectFilter.Matches(candidate.Object)))
                    {
                        CompleteStep();
                    }

                    break;
                case TidyTutorialCompletion.PlaceInTarget:
                    if (candidate.Target != null && candidate.Target.TargetId == step.TargetId)
                    {
                        CompleteStep();
                    }

                    break;
            }
        }

        void HandleDropped(TidyObject tidyObject)
        {
            if (CurrentStep != null && CurrentStep.Completion == TidyTutorialCompletion.Drop)
            {
                CompleteStep();
            }
        }

        void HandleInspect(TidyObject tidyObject)
        {
            if (tidyObject != null && CurrentStep != null && CurrentStep.Completion == TidyTutorialCompletion.Inspect)
            {
                CompleteStep();
            }
        }

        void HandleHint(TidyHint hint)
        {
            if (CurrentStep != null && CurrentStep.Completion == TidyTutorialCompletion.UseHint)
            {
                CompleteStep();
            }
        }

        void HandleCleaned(TidyCleanable cleanable)
        {
            if (CurrentStep != null && CurrentStep.Completion == TidyTutorialCompletion.CleanAny)
            {
                CompleteStep();
            }
        }

        void HandleObjectiveCompleted(TidyObjectiveTracker tracker)
        {
            var step = CurrentStep;
            if (step != null && step.Completion == TidyTutorialCompletion.CompleteObjective && (string.IsNullOrEmpty(step.ObjectiveId) || tracker.Definition.ObjectiveId == step.ObjectiveId))
            {
                CompleteStep();
            }
        }
    }
}
