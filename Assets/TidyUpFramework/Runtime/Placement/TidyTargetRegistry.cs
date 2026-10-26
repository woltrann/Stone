using System;
using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Placement
{
    public static class TidyTargetRegistry
    {
        static readonly List<TidyPlacementTarget> RegisteredTargets = new List<TidyPlacementTarget>(128);
        static readonly Dictionary<Collider, TidyPlacementTarget> TargetsByCollider = new Dictionary<Collider, TidyPlacementTarget>(256);

        public static event Action<TidyPlacementEvent> AnyObjectPlaced;
        public static event Action<TidyPlacementEvent> AnyObjectRemoved;
        public static event Action<TidyPlacementTarget, bool> AnyCompletionChanged;

        public static IReadOnlyList<TidyPlacementTarget> All => RegisteredTargets;

        public static void Register(TidyPlacementTarget target)
        {
            if (target == null || RegisteredTargets.Contains(target))
            {
                return;
            }

            RegisteredTargets.Add(target);
            foreach (var collider in target.GetComponentsInChildren<Collider>(true))
            {
                if (collider.GetComponentInParent<Objects.TidyObject>() == null)
                {
                    TargetsByCollider[collider] = target;
                }
            }
        }

        public static void Unregister(TidyPlacementTarget target)
        {
            if (target == null || !RegisteredTargets.Remove(target))
            {
                return;
            }

            var staleColliders = new List<Collider>();
            foreach (var pair in TargetsByCollider)
            {
                if (pair.Value == target)
                {
                    staleColliders.Add(pair.Key);
                }
            }

            foreach (var collider in staleColliders)
            {
                TargetsByCollider.Remove(collider);
            }
        }

        public static bool TryGetFromCollider(Collider collider, out TidyPlacementTarget target)
        {
            if (collider == null)
            {
                target = null;
                return false;
            }

            if (TargetsByCollider.TryGetValue(collider, out target) && target != null)
            {
                return true;
            }

            target = collider.GetComponentInParent<TidyPlacementTarget>();
            return target != null;
        }

        public static TidyPlacementTarget FindById(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return null;
            }

            foreach (var target in RegisteredTargets)
            {
                if (target != null && target.TargetId == targetId)
                {
                    return target;
                }
            }

            return null;
        }

        internal static void RaisePlaced(TidyPlacementEvent placementEvent)
        {
            AnyObjectPlaced?.Invoke(placementEvent);
        }

        internal static void RaiseRemoved(TidyPlacementEvent placementEvent)
        {
            AnyObjectRemoved?.Invoke(placementEvent);
        }

        internal static void RaiseCompletionChanged(TidyPlacementTarget target, bool isComplete)
        {
            AnyCompletionChanged?.Invoke(target, isComplete);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnDomainReload()
        {
            RegisteredTargets.Clear();
            TargetsByCollider.Clear();
            AnyObjectPlaced = null;
            AnyObjectRemoved = null;
            AnyCompletionChanged = null;
        }
    }
}
