using System;
using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Rules
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Rules/Alphabetical Range Rule", fileName = "Rule_Alphabetical", order = 104)]
    public sealed class TidySortKeyRangeRule : TidyRuleDefinition
    {
        [SerializeField, Tooltip("First accepted initial letter, inclusive.")]
        char firstLetter = 'A';

        [SerializeField, Tooltip("Last accepted initial letter, inclusive.")]
        char lastLetter = 'M';

        public override bool Matches(TidyObject tidyObject)
        {
            if (tidyObject == null || string.IsNullOrEmpty(tidyObject.SortKey))
            {
                return false;
            }

            var initial = char.ToUpperInvariant(tidyObject.SortKey[0]);
            return initial >= char.ToUpperInvariant(firstLetter) && initial <= char.ToUpperInvariant(lastLetter);
        }

        public override string Describe()
        {
            return string.Format("{0}-{1}", char.ToUpperInvariant(firstLetter), char.ToUpperInvariant(lastLetter));
        }

        public void Configure(char first, char last)
        {
            firstLetter = first;
            lastLetter = last;
        }
    }
}
