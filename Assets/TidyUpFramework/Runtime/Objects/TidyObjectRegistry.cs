using System;
using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Objects
{
    public static class TidyObjectRegistry
    {
        static readonly List<TidyObject> RegisteredObjects = new List<TidyObject>(512);
        static readonly Dictionary<Collider, TidyObject> ObjectsByCollider = new Dictionary<Collider, TidyObject>(1024);

        public static event Action<TidyObject> Registered;
        public static event Action<TidyObject> Unregistered;
        public static event Action<TidyObject> AnyStateChanged;

        public static IReadOnlyList<TidyObject> All => RegisteredObjects;

        public static void Register(TidyObject tidyObject)
        {
            if (tidyObject == null || RegisteredObjects.Contains(tidyObject))
            {
                return;
            }

            RegisteredObjects.Add(tidyObject);
            foreach (var collider in tidyObject.Colliders)
            {
                if (collider != null)
                {
                    ObjectsByCollider[collider] = tidyObject;
                }
            }

            Registered?.Invoke(tidyObject);
        }

        public static void Unregister(TidyObject tidyObject)
        {
            if (tidyObject == null || !RegisteredObjects.Remove(tidyObject))
            {
                return;
            }

            foreach (var collider in tidyObject.Colliders)
            {
                if (collider != null)
                {
                    ObjectsByCollider.Remove(collider);
                }
            }

            Unregistered?.Invoke(tidyObject);
        }

        public static bool TryGetFromCollider(Collider collider, out TidyObject tidyObject)
        {
            if (collider == null)
            {
                tidyObject = null;
                return false;
            }

            if (ObjectsByCollider.TryGetValue(collider, out tidyObject) && tidyObject != null)
            {
                return true;
            }

            tidyObject = collider.GetComponentInParent<TidyObject>();
            if (tidyObject != null)
            {
                ObjectsByCollider[collider] = tidyObject;
                return true;
            }

            return false;
        }

        public static TidyObject FindByInstanceId(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            foreach (var tidyObject in RegisteredObjects)
            {
                if (tidyObject != null && tidyObject.InstanceId == instanceId)
                {
                    return tidyObject;
                }
            }

            return null;
        }

        internal static void NotifyStateChanged(TidyObject tidyObject)
        {
            AnyStateChanged?.Invoke(tidyObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnDomainReload()
        {
            RegisteredObjects.Clear();
            ObjectsByCollider.Clear();
            Registered = null;
            Unregistered = null;
            AnyStateChanged = null;
        }
    }
}
