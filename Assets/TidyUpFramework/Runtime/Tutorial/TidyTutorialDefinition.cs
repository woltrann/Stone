using System;
using System.Collections.Generic;
using TidyUpFramework.Input;
using TidyUpFramework.Rules;
using UnityEngine;

namespace TidyUpFramework.Tutorial
{
    public enum TidyTutorialCompletion
    {
        PressContinue,
        Timer,
        MovePlayer,
        LookAround,
        PickUpAny,
        PickUpMatching,
        PlaceAny,
        PlaceCorrectly,
        PlaceInTarget,
        RotateHeldObject,
        Inspect,
        Drop,
        UseHint,
        CleanAny,
        CompleteObjective
    }

    [Serializable]
    public sealed class TidyTutorialStep
    {
        [TextArea(2, 4)]
        public string Message = "Tutorial step";

        [Tooltip("Input action whose prompt is shown next to the message.")]
        public TidyInputAction PromptAction = TidyInputAction.Interact;

        public bool ShowPrompt = true;

        public TidyTutorialCompletion Completion = TidyTutorialCompletion.PressContinue;

        [Tooltip("Objects that satisfy Pick Up Matching and Place Correctly steps, and receive the highlight.")]
        public TidyMatchFilter ObjectFilter = new TidyMatchFilter();

        [Tooltip("Target id used by Place In Target steps and for the destination highlight.")]
        public string TargetId;

        [Tooltip("Objective id used by Complete Objective steps.")]
        public string ObjectiveId;

        [Min(0f)]
        public float DurationSeconds = 3f;

        public bool HighlightObjects = true;
        public bool HighlightTarget = true;
        public bool ShowArrow = true;
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Tutorial Definition", fileName = "Tutorial_New", order = 43)]
    public sealed class TidyTutorialDefinition : ScriptableObject
    {
        [SerializeField]
        string tutorialId;

        [SerializeField, Tooltip("Skip the tutorial once the player has completed it.")]
        bool playOnlyOnce = true;

        [SerializeField]
        List<TidyTutorialStep> steps = new List<TidyTutorialStep>();

        public string TutorialId => string.IsNullOrEmpty(tutorialId) ? name : tutorialId;
        public bool PlayOnlyOnce => playOnlyOnce;
        public IReadOnlyList<TidyTutorialStep> Steps => steps;

        public void Configure(string id, bool once, IEnumerable<TidyTutorialStep> newSteps)
        {
            tutorialId = id;
            playOnlyOnce = once;
            steps = new List<TidyTutorialStep>(newSteps);
        }
    }
}
