using UnityEngine;

namespace TidyUpFramework.Objects
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Category", fileName = "Category_New", order = 1)]
    public sealed class TidyCategoryDefinition : TidyDefinition
    {
        const int MaximumHierarchyDepth = 16;

        [SerializeField, Tooltip("Optional parent category. A 'Spell Book' category can live inside a 'Book' category.")]
        TidyCategoryDefinition parent;

        public TidyCategoryDefinition Parent => parent;

        public bool IsOrDescendsFrom(TidyCategoryDefinition other)
        {
            if (other == null)
            {
                return false;
            }

            var current = this;
            for (var depth = 0; current != null && depth < MaximumHierarchyDepth; depth++)
            {
                if (current == other)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        public void AssignParent(TidyCategoryDefinition newParent)
        {
            parent = newParent == this ? null : newParent;
        }
    }
}
