using System;
using System.Collections.Generic;
using TidyUpFramework.Objects;
using UnityEngine;

namespace StoneGame.Cutting
{
    [Serializable]
    public sealed class OreChanceEntry
    {
        public OreDefinition ore;
        [Range(0f, 100f)] public float chancePercent;
    }

    [CreateAssetMenu(fileName = "StoneProfile_New", menuName = "Stone Game/Stone Cut Profile")]
    public sealed class StoneCutProfile : ScriptableObject
    {
        [Header("Ore probabilities")]
        [SerializeField, Range(0f, 100f)] float containsOrePercent = 5f;
        [SerializeField, Tooltip("Conditional percentages; all entries must total exactly 100 when the ore chance is > 0.")]
        List<OreChanceEntry> oreChances = new List<OreChanceEntry>();

        [Header("Cut result")]
        [SerializeField] GameObject leftHalfPrefab;
        [SerializeField] GameObject rightHalfPrefab;
        [SerializeField, Tooltip("Optional: definition assigned to both cut pieces. Use a different definition from raw stones.")]
        TidyObjectDefinition cutPieceTidyDefinition;

        public GameObject LeftHalfPrefab => leftHalfPrefab;
        public GameObject RightHalfPrefab => rightHalfPrefab;
        public TidyObjectDefinition CutPieceTidyDefinition => cutPieceTidyDefinition;

        public bool TryRollOre(out OreDefinition selectedOre)
        {
            selectedOre = null;
            if (!ValidateDistribution(out var error))
            {
                Debug.LogError($"Invalid ore probabilities in '{name}': {error}", this);
                return false;
            }

            // First roll: does this particular rock contain any ore?
            if (UnityEngine.Random.value * 100f >= containsOrePercent)
            {
                return true; // Valid result: empty rock.
            }

            // Second roll: if ore exists, which kind?
            float roll = UnityEngine.Random.value * 100f;
            OreDefinition lastOre = null;
            foreach (var entry in oreChances)
            {
                if (entry == null || entry.ore == null || entry.chancePercent <= 0f)
                    continue;

                lastOre = entry.ore;
                roll -= entry.chancePercent;
                if (roll < 0f)
                {
                    selectedOre = entry.ore;
                    return true;
                }
            }

            // Handles rare floating-point boundary cases.
            selectedOre = lastOre;
            return true;
        }

        public bool ValidateDistribution(out string error)
        {
            error = null;
            if (containsOrePercent <= 0f)
                return true;

            float total = 0f;
            foreach (var entry in oreChances)
            {
                if (entry == null || entry.ore == null)
                {
                    if (entry != null && entry.chancePercent > 0f)
                    {
                        error = "A positive percentage is assigned without an Ore Definition.";
                        return false;
                    }
                    continue;
                }
                total += entry.chancePercent;
            }

            if (Mathf.Abs(total - 100f) > 0.01f)
            {
                error = $"Ore chances total {total:0.##}%, but must total 100%.";
                return false;
            }
            return true;
        }
    }
}
