using UnityEngine;

namespace TidyUpFramework.Objects
{
    public abstract class TidyDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable unique identifier used by save data, rules and scene references.")]
        string id;

        [SerializeField, Tooltip("Player facing name.")]
        string displayName;

        [SerializeField, TextArea(2, 4)]
        string description;

        [SerializeField]
        Sprite icon;

        [SerializeField, Tooltip("Colour used by UI, gizmos and hint visuals.")]
        Color displayColor = Color.white;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Color DisplayColor => displayColor;

        public void AssignId(string newId)
        {
            id = newId;
        }

        public void AssignPresentation(string newDisplayName, Color newColor)
        {
            displayName = newDisplayName;
            displayColor = newColor;
        }

        public void AssignIcon(Sprite newIcon)
        {
            icon = newIcon;
        }

        public void AssignDescription(string newDescription)
        {
            description = newDescription;
        }
    }
}
