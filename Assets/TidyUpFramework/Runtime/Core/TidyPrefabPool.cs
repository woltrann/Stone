using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Core
{
    public sealed class TidyPrefabPool
    {
        readonly Dictionary<GameObject, Stack<GameObject>> availableByPrefab = new Dictionary<GameObject, Stack<GameObject>>();
        readonly Dictionary<GameObject, GameObject> prefabByInstance = new Dictionary<GameObject, GameObject>();
        readonly Transform poolRoot;

        public TidyPrefabPool(Transform poolRoot)
        {
            this.poolRoot = poolRoot;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = null;
            if (availableByPrefab.TryGetValue(prefab, out var available))
            {
                while (available.Count > 0 && instance == null)
                {
                    instance = available.Pop();
                }
            }

            if (instance == null)
            {
                instance = Object.Instantiate(prefab, poolRoot);
                prefabByInstance[instance] = prefab;
            }

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!prefabByInstance.TryGetValue(instance, out var prefab))
            {
                Object.Destroy(instance);
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(poolRoot, false);
            if (!availableByPrefab.TryGetValue(prefab, out var available))
            {
                available = new Stack<GameObject>();
                availableByPrefab[prefab] = available;
            }

            available.Push(instance);
        }
    }
}
