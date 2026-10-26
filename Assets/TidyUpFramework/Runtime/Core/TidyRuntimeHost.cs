using UnityEngine;

namespace TidyUpFramework.Core
{
    [AddComponentMenu("")]
    public sealed class TidyRuntimeHost : MonoBehaviour
    {
        const string HostName = "[Tidy Up Runtime]";

        static TidyRuntimeHost host;

        public static TidyRuntimeHost Instance
        {
            get
            {
                if (host == null)
                {
                    var hostObject = new GameObject(HostName);
                    DontDestroyOnLoad(hostObject);
                    host = hostObject.AddComponent<TidyRuntimeHost>();
                }

                return host;
            }
        }

        public static T GetOrAddComponent<T>() where T : Component
        {
            var hostObject = Instance.gameObject;
            var existing = hostObject.GetComponent<T>();
            return existing != null ? existing : hostObject.AddComponent<T>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnDomainReload()
        {
            host = null;
        }
    }
}
