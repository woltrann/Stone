using System;
using System.Collections.Generic;
using System.Linq;
using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Rules
{
    [Serializable]
    public sealed class TidyMatchFilter
    {
        [SerializeField, Tooltip("Accepted categories. Empty accepts every category.")]
        List<TidyCategoryDefinition> categories = new List<TidyCategoryDefinition>();

        [SerializeField, Tooltip("Tags the object must carry, all of them.")]
        List<TidyTagDefinition> requiredTags = new List<TidyTagDefinition>();

        [SerializeField, Tooltip("Tags that disqualify the object.")]
        List<TidyTagDefinition> excludedTags = new List<TidyTagDefinition>();

        [SerializeField, Tooltip("Exact object definitions. Empty accepts every definition.")]
        List<TidyObjectDefinition> definitions = new List<TidyObjectDefinition>();

        [SerializeField, Tooltip("Optional reusable rule asset evaluated in addition to the fields above.")]
        TidyRuleDefinition rule;

        public bool IsEmpty => categories.Count == 0 && requiredTags.Count == 0 && excludedTags.Count == 0 && definitions.Count == 0 && rule == null;
        public TidyRuleDefinition Rule => rule;
        public IReadOnlyList<TidyCategoryDefinition> Categories => categories;
        public IReadOnlyList<TidyTagDefinition> RequiredTags => requiredTags;
        public IReadOnlyList<TidyObjectDefinition> Definitions => definitions;

        public static TidyMatchFilter Create(IEnumerable<TidyCategoryDefinition> acceptedCategories, IEnumerable<TidyTagDefinition> tagsRequired, TidyRuleDefinition ruleAsset = null)
        {
            var filter = new TidyMatchFilter();
            filter.categories.AddRange(acceptedCategories ?? Enumerable.Empty<TidyCategoryDefinition>());
            filter.requiredTags.AddRange(tagsRequired ?? Enumerable.Empty<TidyTagDefinition>());
            filter.rule = ruleAsset;
            return filter;
        }

        public static TidyMatchFilter ForDefinitions(IEnumerable<TidyObjectDefinition> acceptedDefinitions)
        {
            var filter = new TidyMatchFilter();
            filter.definitions.AddRange(acceptedDefinitions);
            return filter;
        }

        public bool Matches(TidyObject tidyObject)
        {
            if (tidyObject == null)
            {
                return false;
            }

            return MatchesCategories(tidyObject)
                && MatchesDefinitions(tidyObject)
                && MatchesRequiredTags(tidyObject)
                && !HasExcludedTag(tidyObject)
                && (rule == null || rule.Matches(tidyObject));
        }

        public string Describe()
        {
            if (IsEmpty)
            {
                return "Anything";
            }

            var parts = new List<string>();
            parts.AddRange(requiredTags.Where(tag => tag != null).Select(tag => tag.DisplayName));
            if (categories.Count > 0)
            {
                parts.Add(string.Join("/", categories.Where(category => category != null).Select(category => category.DisplayName)));
            }

            if (definitions.Count > 0)
            {
                parts.Add(string.Join("/", definitions.Where(definition => definition != null).Select(definition => definition.DisplayName)));
            }

            if (rule != null)
            {
                parts.Add(rule.PlayerFacingDescription);
            }

            return string.Join(" ", parts);
        }

        bool MatchesCategories(TidyObject tidyObject)
        {
            if (categories.Count == 0)
            {
                return true;
            }

            foreach (var category in categories)
            {
                if (tidyObject.IsInCategory(category))
                {
                    return true;
                }
            }

            return false;
        }

        bool MatchesDefinitions(TidyObject tidyObject)
        {
            return definitions.Count == 0 || definitions.Contains(tidyObject.Definition);
        }

        bool MatchesRequiredTags(TidyObject tidyObject)
        {
            foreach (var tag in requiredTags)
            {
                if (tag != null && !tidyObject.HasTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        bool HasExcludedTag(TidyObject tidyObject)
        {
            foreach (var tag in excludedTags)
            {
                if (tag != null && tidyObject.HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
