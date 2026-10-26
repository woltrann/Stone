using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Objects
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Object Definition", fileName = "Object_New", order = 0)]
    public sealed class TidyObjectDefinition : TidyDefinition
    {
        [Header("Classification")]
        [SerializeField, Tooltip("Most specific category. Parent categories are matched automatically.")]
        TidyCategoryDefinition category;

        [SerializeField, Tooltip("Colour, size, shape, theme or any custom tag.")]
        List<TidyTagDefinition> tags = new List<TidyTagDefinition>();

        [Header("Properties")]
        [SerializeField, Min(0f), Tooltip("Abstract size value used by size rules and ordering. Use any scale that suits your game.")]
        float size = 1f;

        [SerializeField, Min(0f)]
        float weight = 1f;

        [SerializeField, Tooltip("Used by priority ordering and the hint system. Higher is more important.")]
        int priority;

        [SerializeField, Tooltip("Text used by alphabetical ordering. Falls back to the display name.")]
        string sortKey;

        [SerializeField, Tooltip("Index used by sequential ordering, for example volume numbers.")]
        int sequenceIndex;

        [Header("Capabilities")]
        [SerializeField] bool movable = true;
        [SerializeField] bool rotatable = true;
        [SerializeField] bool placeable = true;
        [SerializeField] bool sortable = true;
        [SerializeField] bool stackable;
        [SerializeField] bool inspectable = true;
        [SerializeField] bool containerCompatible = true;
        [SerializeField] bool snapCompatible = true;
        [SerializeField, Tooltip("Can be gathered into a carried stack together with other objects. Turn off for tools and bulky items.")] bool carryInStack = true;

        [Header("Gameplay")]
        [SerializeField, Min(0), Tooltip("Score awarded for a correct placement before multipliers.")]
        int baseScore = 10;

        [SerializeField]
        TidyPhysicsProfile physicsProfile;

        [SerializeField, Tooltip("Optional prefab used by spawners and editor tools.")]
        GameObject prefab;

        [SerializeField, TextArea(2, 5), Tooltip("Flavour text shown while inspecting.")]
        string inspectText;

        public TidyCategoryDefinition Category => category;
        public IReadOnlyList<TidyTagDefinition> Tags => tags;
        public float Size => size;
        public float Weight => weight;
        public int Priority => priority;
        public string SortKey => string.IsNullOrEmpty(sortKey) ? DisplayName : sortKey;
        public int SequenceIndex => sequenceIndex;
        public bool Movable => movable;
        public bool Rotatable => rotatable;
        public bool Placeable => placeable;
        public bool Sortable => sortable;
        public bool Stackable => stackable;
        public bool Inspectable => inspectable;
        public bool ContainerCompatible => containerCompatible;
        public bool SnapCompatible => snapCompatible;
        public bool CarryInStack => carryInStack;
        public int BaseScore => baseScore;
        public TidyPhysicsProfile PhysicsProfile => physicsProfile;
        public GameObject Prefab => prefab;
        public string InspectText => inspectText;

        public bool HasTag(TidyTagDefinition tag)
        {
            return tag != null && tags.Contains(tag);
        }

        public TidyTagDefinition FindTag(TidyTagGroup group)
        {
            foreach (var tag in tags)
            {
                if (tag != null && tag.Group == group)
                {
                    return tag;
                }
            }

            return null;
        }

        public bool IsInCategory(TidyCategoryDefinition other)
        {
            return category != null && category.IsOrDescendsFrom(other);
        }

        public void Configure(TidyCategoryDefinition newCategory, IEnumerable<TidyTagDefinition> newTags, float newSize, float newWeight, int newPriority, string newSortKey, int newSequenceIndex)
        {
            category = newCategory;
            tags = new List<TidyTagDefinition>(newTags);
            size = newSize;
            weight = newWeight;
            priority = newPriority;
            sortKey = newSortKey;
            sequenceIndex = newSequenceIndex;
        }

        public void ConfigureCapabilities(bool isStackable, bool isRotatable, TidyPhysicsProfile profile, int score)
        {
            stackable = isStackable;
            rotatable = isRotatable;
            physicsProfile = profile;
            baseScore = score;
        }

        public void ConfigureCarrying(bool canCarryInStack)
        {
            carryInStack = canCarryInStack;
        }

        public void AssignPrefab(GameObject newPrefab)
        {
            prefab = newPrefab;
        }

        public void AssignInspectText(string text)
        {
            inspectText = text;
        }
    }
}
