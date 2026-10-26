using UnityEngine;

namespace TidyUpFramework.Demo
{
    [RequireComponent(typeof(Light))]
    [AddComponentMenu("Tidy Up Framework/Demo/Candle Flicker")]
    public sealed class TidyCandleFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float flickerAmount = 0.18f;
        [SerializeField, Min(0.1f)] float flickerSpeed = 7f;
        [SerializeField] Transform flame;

        Light candleLight;
        float baseIntensity;
        Vector3 flameScale;
        float seed;

        void Awake()
        {
            candleLight = GetComponent<Light>();
            baseIntensity = candleLight.intensity;
            seed = Random.value * 100f;
            if (flame != null)
            {
                flameScale = flame.localScale;
            }
        }

        void Update()
        {
            var noise = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
            var factor = 1f + (noise - 0.5f) * 2f * flickerAmount;
            candleLight.intensity = baseIntensity * factor;
            if (flame != null)
            {
                flame.localScale = new Vector3(flameScale.x, flameScale.y * factor, flameScale.z);
            }
        }
    }
}
