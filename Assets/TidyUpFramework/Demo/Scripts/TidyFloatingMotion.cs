using UnityEngine;

namespace TidyUpFramework.Demo
{
    [AddComponentMenu("Tidy Up Framework/Demo/Floating Motion")]
    public sealed class TidyFloatingMotion : MonoBehaviour
    {
        [SerializeField, Min(0f)] float amplitude = 0.05f;
        [SerializeField, Min(0f)] float frequency = 0.6f;
        [SerializeField] float spinDegreesPerSecond = 12f;

        Vector3 origin;
        float phase;

        void Awake()
        {
            origin = transform.localPosition;
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            transform.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time * frequency * Mathf.PI * 2f + phase) * amplitude);
            transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }
    }
}
