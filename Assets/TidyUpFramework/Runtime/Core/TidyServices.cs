using System;
using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Core
{
    public static class TidyServices
    {
        static readonly Dictionary<Type, object> RegisteredServices = new Dictionary<Type, object>();

        public static event Action<Type> ServiceChanged;

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                return;
            }

            RegisteredServices[typeof(T)] = service;
            ServiceChanged?.Invoke(typeof(T));
        }

        public static void Unregister<T>(T service) where T : class
        {
            if (RegisteredServices.TryGetValue(typeof(T), out var current) && ReferenceEquals(current, service))
            {
                RegisteredServices.Remove(typeof(T));
                ServiceChanged?.Invoke(typeof(T));
            }
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (RegisteredServices.TryGetValue(typeof(T), out var current) && current is T typed && !IsDestroyedUnityObject(typed))
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : class
        {
            return TryGet<T>(out var service) ? service : null;
        }

        static bool IsDestroyedUnityObject(object candidate)
        {
            return candidate is UnityEngine.Object unityObject && unityObject == null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnDomainReload()
        {
            RegisteredServices.Clear();
            ServiceChanged = null;
        }
    }
}
