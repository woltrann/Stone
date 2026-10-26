using TidyUpFramework.Objects;
using UnityEngine;

namespace StoneGame.Cutting
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TidyObject))]
    public sealed class CutStoneHalf : MonoBehaviour
    {
        [SerializeField, Tooltip("Child transform located at the cut face, where ore becomes visible.")]
        Transform oreSocket;

        public OreDefinition ContainedOre { get; private set; }
        public bool ContainsOre => ContainedOre != null;

        public void Initialize(OreDefinition ore)
        {
            ContainedOre = ore;
            if (ore == null)
                return;

            if (oreSocket == null || ore.RawOreVisualPrefab == null)
            {
                Debug.LogWarning($"'{name}' contains {ore.DisplayName}, but the Ore Socket or ore visual prefab is missing.", this);
                return;
            }

            var visual = Instantiate(ore.RawOreVisualPrefab, oreSocket);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // The ore is currently only a visual part of this cut half.
            // A later polishing system will operate on this same object.
        }
    }
}
