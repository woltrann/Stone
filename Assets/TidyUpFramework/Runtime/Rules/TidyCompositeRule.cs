using System.Collections.Generic;
using System.Linq;
using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Rules
{
    public enum TidyCompositeMode
    {
        All,
        Any
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Rules/Composite Rule", fileName = "Rule_Composite", order = 110)]
    public sealed class TidyCompositeRule : TidyRuleDefinition
    {
        const int MaximumNestingDepth = 8;

        [SerializeField, Tooltip("All: every child rule must match. Any: one child rule is enough.")]
        TidyCompositeMode mode = TidyCompositeMode.All;

        [SerializeField]
        List<TidyRuleDefinition> rules = new List<TidyRuleDefinition>();

        [SerializeField, Tooltip("Inverts the final result, turning the composite into a NOT rule.")]
        bool invert;

        int evaluationDepth;

        public override bool Matches(TidyObject tidyObject)
        {
            if (evaluationDepth > MaximumNestingDepth)
            {
                return false;
            }

            evaluationDepth++;
            var result = mode == TidyCompositeMode.All ? MatchesAll(tidyObject) : MatchesAny(tidyObject);
            evaluationDepth--;
            return invert ? !result : result;
        }

        public override string Describe()
        {
            var joiner = mode == TidyCompositeMode.All ? " and " : " or ";
            var text = string.Join(joiner, rules.Where(rule => rule != null && rule != this).Select(rule => rule.PlayerFacingDescription));
            return invert ? "Not (" + text + ")" : text;
        }

        public void Configure(TidyCompositeMode newMode, IEnumerable<TidyRuleDefinition> newRules, bool shouldInvert)
        {
            mode = newMode;
            rules = new List<TidyRuleDefinition>(newRules);
            invert = shouldInvert;
        }

        bool MatchesAll(TidyObject tidyObject)
        {
            foreach (var rule in rules)
            {
                if (rule != null && rule != this && !rule.Matches(tidyObject))
                {
                    return false;
                }
            }

            return true;
        }

        bool MatchesAny(TidyObject tidyObject)
        {
            foreach (var rule in rules)
            {
                if (rule != null && rule != this && rule.Matches(tidyObject))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
