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

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public int BaseSellValue => baseSellValue;
        public GameObject RawOreVisualPrefab => rawOreVisualPrefab;
    }
}
