using System.Collections.Generic;
using System.Linq;
using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Rules
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Rules/Category Rule", fileName = "Rule_Category", order = 100)]
    public sealed class TidyCategoryRule : TidyRuleDefinition
    {
        [SerializeField, Tooltip("The object matches when it belongs to any of these categories or their subcategories.")]
        List<TidyCategoryDefinition> categories = new List<TidyCategoryDefinition>();

        public IReadOnlyList<TidyCategoryDefinition> Categories => categories;

        public override bool Matches(TidyObject tidyObject)
        {
            if (tidyObject == null)
            {
                return false;
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

        public override string Describe()
        {
            return string.Join(" or ", categories.Where(category => category != null).Select(category => category.DisplayName));
        }

        public void Configure(IEnumerable<TidyCategoryDefinition> newCategories)
        {
            categories = new List<TidyCategoryDefinition>(newCategories);
        }
    }
}
