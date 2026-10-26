using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Transformation
{
    [AddComponentMenu("Tidy Up Framework/Before After/Post Processing Volume Reactor")]
    public sealed class TidyVolumeReactor : TidyProgressReactor
    {
        [SerializeField, Tooltip("Volume faded in while the area is messy, for example desaturated and dim.")]
        Volume messyVolume;

        [SerializeField, Tooltip("Volume faded in once organised, for example warm and bright.")]
        Volume organisedVolume;

        public void Configure(Volume messy, Volume organised)
        {
            messyVolume = messy;
            organisedVolume = organised;
        }

        protected override void Apply(float organisedAmount)
        {
            if (messyVolume != null)
            {
                messyVolume.weight = 1f - organisedAmount;
            }

            if (organisedVolume != null)
            {
                organisedVolume.weight = organisedAmount;
            }
        }
    }
}
