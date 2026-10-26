using System;
using UnityEngine;

namespace TidyUpFramework.Core
{
    public enum TidyTweenKind
    {
        Pose,
        Scale,
        Punch,
        Value
    }

    public sealed class TidyTween
    {
        internal int Id;
        internal TidyTweenKind Kind;
        internal Transform Target;
        internal float Duration;
        internal float Delay;
        internal float Elapsed;
        internal bool UseUnscaledTime;
        internal TidyEaseType Ease;
        internal Vector3 FromPosition;
        internal Vector3 ToPosition;
        internal Quaternion FromRotation;
        internal Quaternion ToRotation;
        internal Vector3 FromScale;
        internal Vector3 ToScale;
        internal float ArcHeight;
        internal float PunchStrength;
        internal int PunchHalfWaves;
        internal Action<float> ValueSetter;
        internal Action Completed;
        internal bool IsAlive;

        internal void Reset()
        {
            Id = 0;
            Target = null;
            ValueSetter = null;
            Completed = null;
            IsAlive = false;
            Elapsed = 0f;
            Delay = 0f;
            ArcHeight = 0f;
            UseUnscaledTime = false;
        }

        internal bool Step(float deltaTime)
        {
            if (Kind != TidyTweenKind.Value && Target == null)
            {
                return true;
            }

            if (Delay > 0f)
            {
                Delay -= deltaTime;
                return false;
            }

            Elapsed += deltaTime;
            var progress = Duration <= 0f ? 1f : Mathf.Clamp01(Elapsed / Duration);
            Apply(progress);
            return progress >= 1f;
        }

        void Apply(float progress)
        {
            var eased = TidyEase.Evaluate(Ease, progress);
            switch (Kind)
            {
                case TidyTweenKind.Pose:
                    ApplyPose(progress, eased);
                    break;
                case TidyTweenKind.Scale:
                    Target.localScale = Vector3.LerpUnclamped(FromScale, ToScale, eased);
                    break;
                case TidyTweenKind.Punch:
                    ApplyPunch(progress);
                    break;
                case TidyTweenKind.Value:
                    ValueSetter?.Invoke(eased);
                    break;
            }
        }

        void ApplyPose(float progress, float eased)
        {
            var position = Vector3.LerpUnclamped(FromPosition, ToPosition, eased);
            if (ArcHeight > 0f)
            {
                position.y += Mathf.Sin(progress * Mathf.PI) * ArcHeight;
            }

            Target.SetPositionAndRotation(position, Quaternion.SlerpUnclamped(FromRotation, ToRotation, eased));
        }

        void ApplyPunch(float progress)
        {
            var decay = 1f - progress;
            var wave = Mathf.Sin(progress * Mathf.PI * PunchHalfWaves);
            Target.localScale = FromScale * (1f + wave * decay * PunchStrength);
        }
    }
}
