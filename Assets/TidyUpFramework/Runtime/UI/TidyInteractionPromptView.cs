using System;
using System.Collections.Generic;
using TidyUpFramework.Cleaning;
using TidyUpFramework.Core;
using TidyUpFramework.Input;
using TidyUpFramework.Interaction;
using TidyUpFramework.Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [Serializable]
    public sealed class TidyContextPrompt
    {
        public TidyInteractionContext Context;
        [TextArea] public string Template;
        public Color CrosshairColor = Color.white;
        [Min(0.1f)] public float CrosshairScale = 1f;
    }

    [AddComponentMenu("Tidy Up Framework/UI/HUD/Interaction Prompt View")]
    public sealed class TidyInteractionPromptView : MonoBehaviour
    {
        [SerializeField] TMP_Text promptLabel;
        [SerializeField] TMP_Text objectNameLabel;
        [SerializeField] Image crosshair;
        [SerializeField] CanvasGroup promptGroup;
        [SerializeField, TextArea] string cleaningTemplate = "Hold {Interact} Clean";
        [SerializeField] Color cleaningCrosshairColor = new Color(0.55f, 0.85f, 1f);
        [SerializeField, TextArea, Tooltip("Shown when the aimed surface needs a different cleaning tool. {method} is replaced.")] string cleaningBlockedTemplate = "Needs {method}";
        [SerializeField, TextArea] string stackTemplate = "{Interact} Add to stack";
        [SerializeField] Color stackCrosshairColor = new Color(1f, 0.8f, 0.45f);
        [SerializeField, Tooltip("Shown after the held object name while carrying a stack. {count} and {capacity} are replaced.")] string carryCountTemplate = "  <size=70%><color=#F2C46D>{count}/{capacity}</color></size>";

        [SerializeField]
        List<TidyContextPrompt> prompts = new List<TidyContextPrompt>
        {
            new TidyContextPrompt { Context = TidyInteractionContext.Idle, Template = "", CrosshairScale = 0.7f, CrosshairColor = new Color(1f, 1f, 1f, 0.6f) },
            new TidyContextPrompt { Context = TidyInteractionContext.CanPickUp, Template = "{Interact} Pick up", CrosshairScale = 1.2f, CrosshairColor = new Color(1f, 0.92f, 0.7f) },
            new TidyContextPrompt { Context = TidyInteractionContext.Holding, Template = "{Rotate} Rotate   {Inspect} Inspect   {Drop} Drop", CrosshairScale = 0.8f },
            new TidyContextPrompt { Context = TidyInteractionContext.CanPlace, Template = "{Interact} Place", CrosshairScale = 1.3f, CrosshairColor = new Color(0.5f, 1f, 0.6f) },
            new TidyContextPrompt { Context = TidyInteractionContext.CanPlaceIncorrect, Template = "{Interact} Place", CrosshairScale = 1.3f, CrosshairColor = new Color(1f, 0.85f, 0.4f) },
            new TidyContextPrompt { Context = TidyInteractionContext.WrongPlace, Template = "Doesn't belong here", CrosshairScale = 1f, CrosshairColor = new Color(1f, 0.45f, 0.4f) },
            new TidyContextPrompt { Context = TidyInteractionContext.TargetFull, Template = "No space left", CrosshairScale = 1f, CrosshairColor = new Color(1f, 0.6f, 0.4f) },
            new TidyContextPrompt { Context = TidyInteractionContext.Inspecting, Template = "{Look} Turn   {Inspect} Done", CrosshairScale = 0f }
        };

        TidyInteractor interactor;
        TidyCleaningInteractor cleaningInteractor;
        ITidyPlayerInput input;
        string lastText;
        int fadeHandle;
        Placement.TidyPlacementTarget lastCandidateTarget;
        TidyContextPrompt stackPrompt;

        void Start()
        {
            if (TidyServices.TryGet(out interactor))
            {
                interactor.ContextChanged += HandleContextChanged;
                interactor.HoverChanged += HandleObjectChanged;
                interactor.HeldChanged += HandleObjectChanged;
                interactor.CarriedChanged += HandleCarriedChanged;
                cleaningInteractor = interactor.GetComponent<TidyCleaningInteractor>();
                if (cleaningInteractor != null)
                {
                    cleaningInteractor.HoveredCleanableChanged += HandleCleanableChanged;
                }
            }

            if (TidyServices.TryGet(out input))
            {
                input.SchemeChanged += HandleSchemeChanged;
            }

            Refresh();
        }

        void OnDestroy()
        {
            if (interactor != null)
            {
                interactor.ContextChanged -= HandleContextChanged;
                interactor.HoverChanged -= HandleObjectChanged;
                interactor.HeldChanged -= HandleObjectChanged;
                interactor.CarriedChanged -= HandleCarriedChanged;
            }

            if (cleaningInteractor != null)
            {
                cleaningInteractor.HoveredCleanableChanged -= HandleCleanableChanged;
            }

            if (input != null)
            {
                input.SchemeChanged -= HandleSchemeChanged;
            }
        }

        void LateUpdate()
        {
            if (interactor == null || interactor.HeldObject == null)
            {
                return;
            }

            var target = interactor.CurrentCandidate.Target;
            if (target != lastCandidateTarget)
            {
                lastCandidateTarget = target;
                UpdateObjectName(interactor.Context);
            }
        }

        void HandleContextChanged(TidyInteractionContext context)
        {
            Refresh();
        }

        void HandleObjectChanged(TidyObject tidyObject)
        {
            Refresh();
        }

        void HandleCarriedChanged()
        {
            Refresh();
        }

        void HandleCleanableChanged(TidyCleanable cleanable)
        {
            Refresh();
        }

        void HandleSchemeChanged(TidyInputScheme scheme)
        {
            lastText = null;
            Refresh();
        }

        void Refresh()
        {
            if (interactor == null)
            {
                return;
            }

            var context = interactor.Context;
            var prompt = Find(context);
            var cleanable = cleaningInteractor != null ? cleaningInteractor.HoveredCleanable : null;
            var idleOrHolding = context == TidyInteractionContext.Idle || context == TidyInteractionContext.Holding;
            var showCleaning = cleanable != null && idleOrHolding;
            var blocked = cleaningInteractor != null && !showCleaning && idleOrHolding ? cleaningInteractor.BlockedCleanable : null;
            var template = showCleaning ? cleaningTemplate : blocked != null ? cleaningBlockedTemplate.Replace("{method}", DescribeMethod(blocked.AcceptedMethods)) : prompt != null ? prompt.Template : string.Empty;
            SetPrompt(TidyInputPrompts.Format(template));
            UpdateObjectName(context);
            UpdateCrosshair(prompt, showCleaning);
        }

        void SetPrompt(string text)
        {
            if (promptLabel == null || text == lastText)
            {
                return;
            }

            lastText = text;
            promptLabel.text = text;
            if (promptGroup == null)
            {
                return;
            }

            TidyTweenRunner.Kill(fadeHandle);
            var start = promptGroup.alpha;
            var target = string.IsNullOrEmpty(text) ? 0f : 1f;
            fadeHandle = TidyTweenRunner.Value(blend => promptGroup.alpha = Mathf.Lerp(start, target, blend), 0.15f, TidyEaseType.OutQuad, null, true);
        }

        void UpdateObjectName(TidyInteractionContext context)
        {
            if (objectNameLabel == null)
            {
                return;
            }

            var shown = context == TidyInteractionContext.CanStack && interactor.HoveredObject != null ? interactor.HoveredObject : interactor.HeldObject != null ? interactor.HeldObject : interactor.HoveredObject;
            var text = shown != null && context != TidyInteractionContext.Inspecting ? shown.DisplayName : string.Empty;
            if (shown != null && shown.CurrentTarget == null && interactor.CurrentCandidate.Target != null && context != TidyInteractionContext.CanPickUp)
            {
                text += "  <size=75%><color=#C9C2B8>» " + interactor.CurrentCandidate.Target.DisplayName + "</color></size>";
            }

            if (shown != null && interactor.HeldObject != null && interactor.CarryCapacity > 1 && (interactor.IsCarryingStack || context == TidyInteractionContext.CanStack))
            {
                text += carryCountTemplate.Replace("{count}", interactor.CarriedCount.ToString()).Replace("{capacity}", interactor.CarryCapacity.ToString());
            }

            objectNameLabel.text = text;
        }

        void UpdateCrosshair(TidyContextPrompt prompt, bool cleaning)
        {
            if (crosshair == null)
            {
                return;
            }

            var color = cleaning ? cleaningCrosshairColor : prompt != null ? prompt.CrosshairColor : Color.white;
            var scale = cleaning ? 1.2f : prompt != null ? prompt.CrosshairScale : 1f;
            crosshair.color = color;
            TidyTweenRunner.ScaleTo(crosshair.transform, Vector3.one * scale, 0.12f, TidyEaseType.OutBack);
        }

        static string DescribeMethod(TidyCleaningMethod methods)
        {
            if ((methods & TidyCleaningMethod.Scrubbing) != 0)
            {
                return "a scrubbing tool";
            }

            if ((methods & TidyCleaningMethod.Vacuum) != 0)
            {
                return "a vacuum";
            }

            return "bare hands";
        }

        TidyContextPrompt Find(TidyInteractionContext context)
        {
            foreach (var prompt in prompts)
            {
                if (prompt != null && prompt.Context == context)
                {
                    return prompt;
                }
            }

            if (context == TidyInteractionContext.CanStack)
            {
                stackPrompt ??= new TidyContextPrompt { Context = TidyInteractionContext.CanStack, CrosshairScale = 1.2f };
                stackPrompt.Template = stackTemplate;
                stackPrompt.CrosshairColor = stackCrosshairColor;
                return stackPrompt;
            }

            return null;
        }
    }
}
