using UnityEngine;

namespace TidyUpFramework.Demo
{
    [AddComponentMenu("Tidy Up Framework/Demo/Menu Camera Drift")]
    public sealed class TidyMenuCameraDrift : MonoBehaviour
    {
        [SerializeField] Vector3 positionAmplitude = new Vector3(0.25f, 0.08f, 0.1f);
        [SerializeField] Vector3 rotationAmplitude = new Vector3(1.2f, 2.5f, 0f);
        [SerializeField, Min(0.01f)] float speed = 0.07f;

        Vector3 originPosition;
        Quaternion originRotation;

        void Awake()
        {
            originPosition = transform.localPosition;
            originRotation = transform.localRotation;
        }

        void Update()
        {
            var time = Time.unscaledTime * speed * Mathf.PI * 2f;
            var offset = new Vector3(Mathf.Sin(time) * positionAmplitude.x, Mathf.Sin(time * 1.3f) * positionAmplitude.y, Mathf.Cos(time * 0.7f) * positionAmplitude.z);
            var rotation = new Vector3(Mathf.Sin(time * 0.9f) * rotationAmplitude.x, Mathf.Sin(time * 0.6f) * rotationAmplitude.y, 0f);
            transform.localPosition = originPosition + offset;
            transform.localRotation = originRotation * Quaternion.Euler(rotation);
        }
    }
}
