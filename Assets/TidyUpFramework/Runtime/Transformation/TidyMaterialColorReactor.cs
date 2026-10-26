using UnityEngine;

namespace TidyUpFramework.Transformation
{
    [AddComponentMenu("Tidy Up Framework/Before After/Material Color Reactor")]
    public sealed class TidyMaterialColorReactor : TidyProgressReactor
    {
        [SerializeField]
        Renderer[] renderers = new Renderer[0];

        [SerializeField]
        string colorProperty = "_BaseColor";

        [SerializeField]
        Color messyColor = new Color(0.55f, 0.52f, 0.5f);

        [SerializeField]
        Color organisedColor = Color.white;

        [SerializeField, Tooltip("Optional emission colour reached when organised. Requires an emissive material.")]
        bool driveEmission;

        [SerializeField, ColorUsage(false, true)]
        Color organisedEmission = Color.black;

        MaterialPropertyBlock propertyBlock;
        int colorId;
        int emissionId;

        public void Configure(Renderer[] targets, Color messy, Color organised)
        {
            renderers = targets ?? new Renderer[0];
            messyColor = messy;
            organisedColor = organised;
        }

        protected override void Apply(float organisedAmount)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
                colorId = Shader.PropertyToID(colorProperty);
                emissionId = Shader.PropertyToID("_EmissionColor");
            }

            var color = Color.Lerp(messyColor, organisedColor, organisedAmount);
            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(colorId, color);
                if (driveEmission)
                {
                    propertyBlock.SetColor(emissionId, Color.Lerp(Color.black, organisedEmission, organisedAmount));
                }

                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
