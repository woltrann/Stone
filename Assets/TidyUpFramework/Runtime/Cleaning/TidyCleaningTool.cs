using UnityEngine;

namespace TidyUpFramework.Cleaning
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Tidy Up Framework/Cleaning/Cleaning Tool")]
    public sealed class TidyCleaningTool : MonoBehaviour
    {
        [SerializeField, Min(0.1f), Tooltip("Multiplies scrubbing speed compared to bare hands.")]
        float cleaningSpeedMultiplier = 2f;

        [SerializeField, Min(0f), Tooltip("Radius around the aim point that is cleaned at once.")]
        float cleaningRadius = 0.25f;

        [SerializeField, Tooltip("Vacuum mode: loose clutter near the aim point is sucked in and disposed.")]
        bool collectsClutter;

        [SerializeField, Min(0f)]
        float collectionRadius = 0.6f;

        [SerializeField, Tooltip("How this tool cleans. Automatic treats vacuum tools as Vacuum and every other tool as Scrubbing.")]
        TidyCleaningToolMethod method = TidyCleaningToolMethod.Automatic;

        [SerializeField, Tooltip("Optional particle system played while the tool is active.")]
        ParticleSystem activeParticles;

        public float CleaningSpeedMultiplier => cleaningSpeedMultiplier;
        public float CleaningRadius => cleaningRadius;
        public bool CollectsClutter => collectsClutter;
        public float CollectionRadius => collectionRadius;

        public TidyCleaningMethod Method
        {
            get
            {
                switch (method)
                {
                    case TidyCleaningToolMethod.Scrubbing:
                        return TidyCleaningMethod.Scrubbing;
                    case TidyCleaningToolMethod.Vacuum:
                        return TidyCleaningMethod.Vacuum;
                    default:
                        return collectsClutter ? TidyCleaningMethod.Vacuum : TidyCleaningMethod.Scrubbing;
                }
            }
        }

        public void Configure(float speed, float radius, bool vacuum, float vacuumRadius)
        {
            cleaningSpeedMultiplier = speed;
            cleaningRadius = radius;
            collectsClutter = vacuum;
            collectionRadius = vacuumRadius;
        }

        public void ConfigureMethod(TidyCleaningToolMethod newMethod)
        {
            method = newMethod;
        }

        public void SetActiveVisual(bool active)
        {
            if (activeParticles == null)
            {
                return;
            }

            if (active && !activeParticles.isPlaying)
            {
                activeParticles.Play();
            }
            else if (!active && activeParticles.isPlaying)
            {
                activeParticles.Stop();
            }
        }
    }
}
