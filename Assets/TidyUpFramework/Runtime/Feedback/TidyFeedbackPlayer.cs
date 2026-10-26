using System.Collections.Generic;
using TidyUpFramework.Audio;
using TidyUpFramework.Cleaning;
using TidyUpFramework.Core;
using TidyUpFramework.Hints;
using TidyUpFramework.Interaction;
using TidyUpFramework.Levels;
using TidyUpFramework.Objectives;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Scoring;
using UnityEngine;

namespace TidyUpFramework.Feedback
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Tidy Up Framework/Feedback/Feedback Player")]
    public sealed class TidyFeedbackPlayer : MonoBehaviour
    {
        const float ParticleLifetimeFallback = 2f;

        [SerializeField]
        TidyFeedbackDefinition definition;

        readonly List<(GameObject instance, float despawnTime)> activeParticles = new List<(GameObject, float)>();
        TidyPrefabPool particlePool;
        TidyInteractor interactor;
        TidyLevelManager levelManager;
        TidyCameraPunch cameraPunch;
        TidyHintSystem hintSystem;
        float lastComboIntensity;

        public TidyFeedbackDefinition Definition => definition;

        public void AssignDefinition(TidyFeedbackDefinition newDefinition)
        {
            definition = newDefinition;
        }

        public void Play(TidyFeedbackEffect effect, Vector3 position, float intensity = 0f)
        {
            if (effect == null)
            {
                return;
            }

            var pitchOffset = effect.PitchFollowsCombo ? intensity * effect.ComboPitchRange : 0f;
            TidyAudio.PlayAt(effect.Sound, position, 1f, pitchOffset);
            if (effect.Particles != null)
            {
                SpawnParticles(effect.Particles, position);
            }

            if (effect.CameraPunch > 0f && cameraPunch != null)
            {
                cameraPunch.Punch(effect.CameraPunch * (0.6f + intensity * 0.4f));
            }
        }

        bool IsRestoring => levelManager != null && levelManager.IsRestoring;

        void Awake()
        {
            var poolRoot = new GameObject("Feedback Pool").transform;
            poolRoot.SetParent(transform, false);
            particlePool = new TidyPrefabPool(poolRoot);
            TidyServices.Register(this);
        }

        void Start()
        {
            TidyServices.TryGet(out interactor);
            TidyServices.TryGet(out levelManager);
            TidyServices.TryGet(out cameraPunch);
            TidyServices.TryGet(out hintSystem);
            if (interactor != null)
            {
                interactor.ObjectPickedUp += HandlePickedUp;
                interactor.ObjectDropped += HandleDropped;
                interactor.PlacementRejected += HandleRejected;
            }

            if (levelManager != null)
            {
                levelManager.PlacementEvaluated += HandlePlacementEvaluated;
                levelManager.ObjectiveCompleted += HandleObjectiveCompleted;
                levelManager.LevelCompleted += HandleLevelCompleted;
                levelManager.LevelStarted += HandleLevelStarted;
                if (levelManager.Combo != null)
                {
                    SubscribeCombo(levelManager.Combo);
                }
            }

            if (hintSystem != null)
            {
                hintSystem.HintShown += HandleHintShown;
            }

            TidyTargetRegistry.AnyCompletionChanged += HandleTargetCompletion;
            TidyCleanable.AnyCleaned += HandleCleaned;
        }

        void OnDestroy()
        {
            TidyServices.Unregister(this);
            if (interactor != null)
            {
                interactor.ObjectPickedUp -= HandlePickedUp;
                interactor.ObjectDropped -= HandleDropped;
                interactor.PlacementRejected -= HandleRejected;
            }

            if (levelManager != null)
            {
                levelManager.PlacementEvaluated -= HandlePlacementEvaluated;
                levelManager.ObjectiveCompleted -= HandleObjectiveCompleted;
                levelManager.LevelCompleted -= HandleLevelCompleted;
                levelManager.LevelStarted -= HandleLevelStarted;
                if (levelManager.Combo != null)
                {
                    levelManager.Combo.TierReached -= HandleComboTier;
                    levelManager.Combo.ComboChanged -= HandleComboChanged;
                }
            }

            if (hintSystem != null)
            {
                hintSystem.HintShown -= HandleHintShown;
            }

            TidyTargetRegistry.AnyCompletionChanged -= HandleTargetCompletion;
            TidyCleanable.AnyCleaned -= HandleCleaned;
        }

        void HandleLevelStarted(TidyLevelManager manager)
        {
            SubscribeCombo(manager.Combo);
        }

        void SubscribeCombo(TidyComboSystem combo)
        {
            combo.TierReached -= HandleComboTier;
            combo.ComboChanged -= HandleComboChanged;
            combo.TierReached += HandleComboTier;
            combo.ComboChanged += HandleComboChanged;
        }

        void Update()
        {
            var now = Time.time;
            for (var index = activeParticles.Count - 1; index >= 0; index--)
            {
                if (activeParticles[index].despawnTime > now)
                {
                    continue;
                }

                particlePool.Despawn(activeParticles[index].instance);
                activeParticles.RemoveAt(index);
            }
        }

        void SpawnParticles(ParticleSystem prefab, Vector3 position)
        {
            var instance = particlePool.Spawn(prefab.gameObject, position, Quaternion.identity);
            if (instance == null)
            {
                return;
            }

            var system = instance.GetComponent<ParticleSystem>();
            var lifetime = ParticleLifetimeFallback;
            if (system != null)
            {
                system.Clear(true);
                system.Play(true);
                lifetime = system.main.duration + system.main.startLifetime.constantMax;
            }

            activeParticles.Add((instance, Time.time + lifetime));
        }

        void HandlePickedUp(TidyObject tidyObject)
        {
            if (IsRestoring)
            {
                return;
            }

            if (definition != null)
            {
                Play(definition.PickUp, tidyObject.transform.position);
            }
        }

        void HandleDropped(TidyObject tidyObject)
        {
            if (IsRestoring)
            {
                return;
            }

            if (definition != null)
            {
                Play(definition.Drop, tidyObject.transform.position);
            }
        }

        void HandleRejected(TidyPlacementCandidate candidate)
        {
            if (definition != null && candidate.Object != null)
            {
                Play(definition.Rejected, candidate.Object.transform.position);
            }
        }

        void HandlePlacementEvaluated(TidyPlacementEvent placement)
        {
            if (definition == null || placement.Object == null)
            {
                return;
            }

            var position = placement.Object.transform.position + Vector3.up * placement.Object.ScaledSize.y * 0.5f;
            Play(placement.IsCorrect ? definition.PlaceCorrect : definition.PlaceIncorrect, position, lastComboIntensity);
        }

        void HandleTargetCompletion(TidyPlacementTarget target, bool complete)
        {
            if (IsRestoring)
            {
                return;
            }

            if (definition != null && complete && levelManager != null && levelManager.IsPlaying)
            {
                Play(definition.TargetComplete, target.transform.position + Vector3.up * 0.2f, 1f);
            }
        }

        void HandleObjectiveCompleted(TidyObjectiveTracker tracker)
        {
            if (definition != null)
            {
                Play(definition.ObjectiveComplete, ListenerPosition());
            }
        }

        void HandleLevelCompleted(TidyLevelResult result)
        {
            if (definition != null)
            {
                Play(definition.LevelComplete, ListenerPosition(), 1f);
            }
        }

        void HandleComboTier(TidyComboTier tier)
        {
            if (definition != null)
            {
                Play(definition.ComboTier, ListenerPosition(), tier.FeedbackIntensity);
            }
        }

        void HandleComboChanged(TidyComboSystem combo)
        {
            lastComboIntensity = combo.CurrentTier != null ? combo.CurrentTier.FeedbackIntensity : Mathf.Clamp01(combo.Count * 0.05f);
        }

        void HandleCleaned(TidyCleanable cleanable)
        {
            if (IsRestoring)
            {
                return;
            }

            if (definition != null)
            {
                Play(definition.Cleaned, cleanable.transform.position, lastComboIntensity);
            }
        }

        void HandleHintShown(TidyHint hint)
        {
            if (definition != null)
            {
                Play(definition.Hint, ListenerPosition());
            }
        }

        Vector3 ListenerPosition()
        {
            return interactor != null && interactor.AimCamera != null ? interactor.AimCamera.transform.position : transform.position;
        }
    }
}
