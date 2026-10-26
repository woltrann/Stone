using TidyUpFramework.Levels;
using UnityEngine;

namespace TidyUpFramework.Objectives
{
    public struct TidyObjectiveProgress
    {
        public int Current;
        public int Target;
        public bool IsFailed;

        public TidyObjectiveProgress(int current, int target, bool isFailed = false)
        {
            Current = current;
            Target = target;
            IsFailed = isFailed;
        }

        public bool IsComplete => !IsFailed && Target > 0 && Current >= Target;
        public float Ratio => Target <= 0 ? 0f : Mathf.Clamp01((float)Current / Target);
    }

    public abstract class TidyObjectiveDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable id for save data and tutorials.")]
        string objectiveId;

        [SerializeField, Tooltip("Shown in the objective list. Use {current} and {target} as placeholders.")]
        string title = "Objective {current}/{target}";

        [SerializeField, TextArea(2, 4)]
        string description;

        [SerializeField]
        Sprite icon;

        [SerializeField, Min(0), Tooltip("Bonus awarded when the objective is completed for the first time.")]
        int bonusScore = 100;

        [SerializeField, Tooltip("Hide the progress counter, for example for yes/no objectives.")]
        bool hideCounter;

        public string ObjectiveId => string.IsNullOrEmpty(objectiveId) ? name : objectiveId;
        public string Description => description;
        public Sprite Icon => icon;
        public int BonusScore => bonusScore;
        public bool HideCounter => hideCounter;
        public virtual bool IsResolvedAtLevelEnd => false;

        public abstract TidyObjectiveProgress Evaluate(TidyLevelContext context);

        public virtual string FormatTitle(TidyObjectiveProgress progress)
        {
            return Format(title, progress);
        }

        public virtual string FormatName(TidyObjectiveProgress progress)
        {
            return Format(title.Replace("{current}/{target}", string.Empty).Trim(), progress);
        }

        protected virtual string Format(string template, TidyObjectiveProgress progress)
        {
            return template.Replace("{current}", progress.Current.ToString()).Replace("{target}", progress.Target.ToString());
        }

        public void ConfigurePresentation(string newId, string newTitle, string newDescription, int newBonus)
        {
            objectiveId = newId;
            title = newTitle;
            description = newDescription;
            bonusScore = newBonus;
        }

        public void ConfigureCounterVisibility(bool hidden)
        {
            hideCounter = hidden;
        }
    }
}
