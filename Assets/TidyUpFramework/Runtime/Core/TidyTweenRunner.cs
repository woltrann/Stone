using System;
using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Core
{
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("")]
    public sealed class TidyTweenRunner : MonoBehaviour
    {
        const int DefaultPunchVibrato = 2;

        static TidyTweenRunner instance;

        readonly List<TidyTween> activeTweens = new List<TidyTween>(128);
        readonly Stack<TidyTween> pooledTweens = new Stack<TidyTween>(128);
        int nextId = 1;

        static TidyTweenRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = TidyRuntimeHost.GetOrAddComponent<TidyTweenRunner>();
                }

                return instance;
            }
        }

        public static int MovePose(Transform target, Vector3 position, Quaternion rotation, float duration, TidyEaseType ease, float arcHeight = 0f, Action completed = null)
        {
            if (target == null)
            {
                return 0;
            }

            Kill(target, TidyTweenKind.Pose);
            var tween = Instance.Rent(TidyTweenKind.Pose, target, duration, ease, completed);
            tween.FromPosition = target.position;
            tween.FromRotation = target.rotation;
            tween.ToPosition = position;
            tween.ToRotation = rotation;
            tween.ArcHeight = arcHeight;
            return tween.Id;
        }

        public static int Retarget(Transform target, Vector3 position, Quaternion rotation, float duration, TidyEaseType ease)
        {
            if (target == null)
            {
                return 0;
            }

            if (instance != null)
            {
                foreach (var tween in instance.activeTweens)
                {
                    if (tween.IsAlive && tween.Target == target && tween.Kind == TidyTweenKind.Pose)
                    {
                        tween.FromPosition = target.position;
                        tween.FromRotation = target.rotation;
                        tween.ToPosition = position;
                        tween.ToRotation = rotation;
                        tween.Elapsed = 0f;
                        tween.Duration = duration;
                        tween.Ease = ease;
                        tween.ArcHeight = 0f;
                        return tween.Id;
                    }
                }
            }

            return MovePose(target, position, rotation, duration, ease);
        }

        public static int ScaleTo(Transform target, Vector3 scale, float duration, TidyEaseType ease, Action completed = null, float delay = 0f)
        {
            if (target == null)
            {
                return 0;
            }

            Kill(target, TidyTweenKind.Scale);
            Kill(target, TidyTweenKind.Punch);
            var tween = Instance.Rent(TidyTweenKind.Scale, target, duration, ease, completed);
            tween.FromScale = target.localScale;
            tween.ToScale = scale;
            tween.Delay = delay;
            return tween.Id;
        }

        public static int Punch(Transform target, Vector3 restScale, float strength, float duration, int vibrato = DefaultPunchVibrato)
        {
            if (target == null)
            {
                return 0;
            }

            Kill(target, TidyTweenKind.Punch);
            Kill(target, TidyTweenKind.Scale);
            var tween = Instance.Rent(TidyTweenKind.Punch, target, duration, TidyEaseType.Linear, () => target.localScale = restScale);
            tween.FromScale = restScale;
            tween.PunchStrength = strength;
            tween.PunchHalfWaves = Mathf.Max(1, vibrato) * 2 - 1;
            return tween.Id;
        }

        public static int Value(Action<float> setter, float duration, TidyEaseType ease, Action completed = null, bool unscaledTime = false, float delay = 0f)
        {
            if (setter == null)
            {
                return 0;
            }

            var tween = Instance.Rent(TidyTweenKind.Value, null, duration, ease, completed);
            tween.ValueSetter = setter;
            tween.UseUnscaledTime = unscaledTime;
            tween.Delay = delay;
            return tween.Id;
        }

        public static void Kill(int handle)
        {
            if (instance == null || handle == 0)
            {
                return;
            }

            foreach (var tween in instance.activeTweens)
            {
                if (tween.Id == handle)
                {
                    tween.IsAlive = false;
                }
            }
        }

        public static void Kill(Transform target)
        {
            if (instance == null || target == null)
            {
                return;
            }

            foreach (var tween in instance.activeTweens)
            {
                if (tween.Target == target)
                {
                    tween.IsAlive = false;
                }
            }
        }

        public static void Kill(Transform target, TidyTweenKind kind)
        {
            if (instance == null || target == null)
            {
                return;
            }

            foreach (var tween in instance.activeTweens)
            {
                if (tween.Target == target && tween.Kind == kind)
                {
                    tween.IsAlive = false;
                }
            }
        }

        public static bool IsTweening(Transform target)
        {
            if (instance == null || target == null)
            {
                return false;
            }

            foreach (var tween in instance.activeTweens)
            {
                if (tween.IsAlive && tween.Target == target)
                {
                    return true;
                }
            }

            return false;
        }

        TidyTween Rent(TidyTweenKind kind, Transform target, float duration, TidyEaseType ease, Action completed)
        {
            var tween = pooledTweens.Count > 0 ? pooledTweens.Pop() : new TidyTween();
            tween.Reset();
            tween.Id = nextId++;
            tween.Kind = kind;
            tween.Target = target;
            tween.Duration = duration;
            tween.Ease = ease;
            tween.Completed = completed;
            tween.IsAlive = true;
            activeTweens.Add(tween);
            enabled = true;
            return tween;
        }

        void Update()
        {
            var scaledDelta = Time.deltaTime;
            var unscaledDelta = Time.unscaledDeltaTime;
            var index = 0;
            while (index < activeTweens.Count)
            {
                var tween = activeTweens[index];
                if (!tween.IsAlive)
                {
                    RecycleAt(index);
                    continue;
                }

                var finished = tween.Step(tween.UseUnscaledTime ? unscaledDelta : scaledDelta);
                if (!finished)
                {
                    index++;
                    continue;
                }

                var completed = tween.Completed;
                RecycleAt(index);
                completed?.Invoke();
            }

            if (activeTweens.Count == 0)
            {
                enabled = false;
            }
        }

        void RecycleAt(int index)
        {
            var tween = activeTweens[index];
            activeTweens.RemoveAt(index);
            tween.Reset();
            pooledTweens.Push(tween);
        }
    }
}
