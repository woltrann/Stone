using UnityEngine;

namespace StoneGame.Cutting
{
    [CreateAssetMenu(fileName = "Ore_New", menuName = "Stone Game/Ore Definition")]
    public sealed class OreDefinition : ScriptableObject
    {
        [SerializeField] string displayName = "New Ore";
        [SerializeField, Min(0)] int baseSellValue = 10;
        [SerializeField, Tooltip("Visual-only prefab shown on the cut face. Give it no TidyObject for now.")]
        GameObject rawOreVisualPrefab;
        [SerializeField, HideInInspector, Tooltip("Legacy value retained for compatibility; Table chipping uses its actual cut-face material.")]
        Material processedOreMaterial;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public int BaseSellValue => baseSellValue;
        public GameObject RawOreVisualPrefab => rawOreVisualPrefab;
        public Material ProcessedOreMaterial => processedOreMaterial;

        public bool ValidateVisual(out string error)
        {
            error = null;
            if (rawOreVisualPrefab == null) error = $"Ore '{DisplayName}' needs a visual prefab.";
            else if (rawOreVisualPrefab.GetComponentInChildren<TidyUpFramework.Objects.TidyObject>(true) != null)
                error = $"Ore '{DisplayName}' must be a visual prefab without TidyObject.";
            else if (rawOreVisualPrefab.GetComponentInChildren<Collider>(true) != null ||
                rawOreVisualPrefab.GetComponentInChildren<Rigidbody>(true) != null)
                error = $"Ore '{DisplayName}' is visual-only: remove Collider and Rigidbody from its visual prefab.";
            return error == null;
        }
    }
}
