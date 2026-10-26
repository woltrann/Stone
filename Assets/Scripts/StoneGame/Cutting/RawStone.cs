using TidyUpFramework.Objects;
using UnityEngine;

namespace StoneGame.Cutting
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TidyObject))]
    public sealed class RawStone : MonoBehaviour
    {
        [SerializeField] StoneCutProfile cutProfile;

        bool hasRolled;
        OreDefinition rolledOre;

        public StoneCutProfile CutProfile => cutProfile;

        // Called once per stone. Repeated calls never redraw a different ore.
        public bool TryGetOre(out OreDefinition ore)
        {
            ore = null;
            if (cutProfile == null)
            {
                Debug.LogError($"RawStone on '{name}' needs a Stone Cut Profile.", this);
                return false;
            }

            if (!hasRolled)
            {
                if (!cutProfile.TryRollOre(out rolledOre))
                    return false;
                hasRolled = true;
            }

            ore = rolledOre; // Null means the rock contains no ore.
            return true;
        }
    }
}
