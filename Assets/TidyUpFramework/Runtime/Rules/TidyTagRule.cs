using System.Collections.Generic;
using System.Linq;
using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Rules
{
    public enum TidyTagMatchMode
    {
        All,
        Any,
        None
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Rules/Tag Rule", fileName = "Rule_Tag", order = 101)]
    public sealed class TidyTagRule : TidyRuleDefinition
    {
        [SerializeField]
        List<TidyTagDefinition> tags = new List<TidyTagDefinition>();

        [SerializeField, Tooltip("All: every tag is required. Any: one tag is enough. None: the object must carry none of the tags.")]
        TidyTagMatchMode matchMode = TidyTagMatchMode.All;

        public override bool Matches(TidyObject tidyObject)
        {
            if (tidyObject == null)
            {
                return false;
            }

            switch (matchMode)
            {
                case TidyTagMatchMode.Any:
                    return tags.Any(tidyObject.HasTag);
                case TidyTagMatchMode.None:
                    return !tags.Any(tidyObject.HasTag);
                default:
                    return tags.Where(tag => tag != null).All(tidyObject.HasTag);
            }
        }

        public override string Describe()
        {
            var joiner = matchMode == TidyTagMatchMode.Any ? " or " : " and ";
            var names = string.Join(joiner, tags.Where(tag => tag != null).Select(tag => tag.DisplayName));
            return matchMode == TidyTagMatchMode.None ? "Not " + names : names;
        }

        public void Configure(IEnumerable<TidyTagDefinition> newTags, TidyTagMatchMode newMode)
        {
            tags = new List<TidyTagDefinition>(newTags);
            matchMode = newMode;
        }
    }
}
