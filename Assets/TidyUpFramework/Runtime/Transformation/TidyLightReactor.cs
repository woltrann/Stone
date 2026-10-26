using UnityEngine;

namespace TidyUpFramework.Transformation
{
    [AddComponentMenu("Tidy Up Framework/Before After/Light Reactor")]
    [RequireComponent(typeof(Light))]
    public sealed class TidyLightReactor : TidyProgressReactor
    {
        [SerializeField] float messyIntensity = 0.4f;
        [SerializeField] float organisedIntensity = 1.6f;
        [SerializeField] Color messyColor = new Color(0.7f, 0.65f, 0.6f);
        [SerializeField] Color organisedColor = new Color(1f, 0.85f, 0.6f);

        Light targetLight;

        public void Configure(float messy, float organised, Color messyTint, Color organisedTint)
        {
            messyIntensity = messy;
            organisedIntensity = organised;
            messyColor = messyTint;
            organisedColor = organisedTint;
        }

        protected override void Apply(float organisedAmount)
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }

            targetLight.intensity = Mathf.Lerp(messyIntensity, organisedIntensity, organisedAmount);
            targetLight.color = Color.Lerp(messyColor, organisedColor, organisedAmount);
        }
    }
}
