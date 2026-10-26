using TidyUpFramework.Core;
using UnityEngine;

namespace TidyUpFramework.Feedback
{
    [AddComponentMenu("Tidy Up Framework/Feedback/Camera Punch")]
    public sealed class TidyCameraPunch : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Field of view change at full strength.")]
        float fieldOfViewKick = 2.5f;

        [SerializeField, Min(0f), Tooltip("Positional nudge at full strength.")]
        float positionKick = 0.02f;

        [SerializeField, Min(0.01f)]
        float recoverySpeed = 8f;

        [SerializeField, Tooltip("Global multiplier, set to zero for players sensitive to camera motion.")]
        float intensityMultiplier = 1f;

        Camera targetCamera;
        float baseFieldOfView;
        Vector3 basePosition;
        float strength;

        public float IntensityMultiplier
        {
            get => intensityMultiplier;
            set => intensityMultiplier = Mathf.Max(0f, value);
        }

        public void Punch(float amount)
        {
            strength = Mathf.Clamp01(Mathf.Max(strength, amount * intensityMultiplier));
        }

        void Awake()
        {
            targetCamera = GetComponent<Camera>();
            baseFieldOfView = targetCamera != null ? targetCamera.fieldOfView : 60f;
            basePosition = transform.localPosition;
            TidyServices.Register(this);
        }

        void OnDestroy()
        {
            TidyServices.Unregister(this);
        }

        void LateUpdate()
        {
            if (strength <= 0.0001f)
            {
                return;
            }

            strength = Mathf.MoveTowards(strength, 0f, recoverySpeed * Time.unscaledDeltaTime * Mathf.Max(0.1f, strength));
            var wave = Mathf.Sin(strength * Mathf.PI);
            if (targetCamera != null)
            {
                targetCamera.fieldOfView = baseFieldOfView - wave * fieldOfViewKick;
            }

            transform.localPosition = basePosition + new Vector3(0f, -wave * positionKick, 0f);
            if (strength <= 0.0001f)
            {
                if (targetCamera != null)
                {
                    targetCamera.fieldOfView = baseFieldOfView;
                }

                transform.localPosition = basePosition;
            }
        }
    }
}
