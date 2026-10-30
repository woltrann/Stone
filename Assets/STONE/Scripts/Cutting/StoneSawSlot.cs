using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using UnityEngine;

namespace StoneGame.Cutting
{
    // Keeps the framework's aiming, preview, placement tween and target registry.
    public sealed class StoneSawSlot : TidySnapSlot
    {
        [SerializeField] StoneSawMachine machine;

        public override bool IsCompatible(TidyObject tidyObject)
        {
            if (!base.IsCompatible(tidyObject) || (machine != null && machine.IsCutting)) return false;
            var stone = tidyObject.GetComponent<RawStone>();
            return stone != null && !stone.IsProcessed && stone.CutProfile != null;
        }

        public override bool AllowsRemoval(TidyObject tidyObject)
        {
            return (machine == null || !machine.IsCutting) && base.AllowsRemoval(tidyObject);
        }
    }
}
