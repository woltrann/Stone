using System.IO;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Editor
{
    public static class TidyEditorUtility
    {
        public const string RootFolder = "Assets/TidyUpFramework";
        public const string PrefabFolder = RootFolder + "/Prefabs";
        public const string MenuRoot = "Tools/Tidy Up Framework/";
        public const string GameObjectMenuRoot = "GameObject/Tidy Up Framework/";
        public const int GameObjectMenuPriority = 10;

        public static GameObject LoadPrefab(string prefabName)
        {
            var direct = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + prefabName + ".prefab");
            if (direct != null)
            {
                return direct;
            }

            foreach (var guid in AssetDatabase.FindAssets(prefabName + " t:Prefab", new[] { RootFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == prefabName)
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            return null;
        }

        public static GameObject InstantiatePrefab(string prefabName, MenuCommand command)
        {
            var prefab = LoadPrefab(prefabName);
            if (prefab == null)
            {
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Register(instance, command, false);
            return instance;
        }

        public static GameObject CreateObject(string objectName, MenuCommand command, bool placeAtSceneView = true)
        {
            var created = new GameObject(objectName);
            Register(created, command, placeAtSceneView);
            return created;
        }

        public static void Register(GameObject created, MenuCommand command, bool placeAtSceneView)
        {
            var parent = command != null ? command.context as GameObject : null;
            if (parent != null)
            {
                GameObjectUtility.SetParentAndAlign(created, parent);
            }
            else if (placeAtSceneView && SceneView.lastActiveSceneView != null)
            {
                created.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            Undo.RegisterCreatedObjectUndo(created, "Create " + created.name);
            Selection.activeGameObject = created;
        }

        public static BoxCollider EnsureAimTrigger(GameObject target, Vector3 center, Vector3 size)
        {
            var trigger = target.GetComponent<BoxCollider>();
            if (trigger == null)
            {
                trigger = Undo.AddComponent<BoxCollider>(target);
            }

            Undo.RecordObject(trigger, "Configure Aim Trigger");
            trigger.isTrigger = true;
            trigger.center = center;
            trigger.size = size;
            return trigger;
        }

        public static string Slug(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "item";
            }

            var builder = new System.Text.StringBuilder(value.Length);
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
                else if (builder.Length > 0 && builder[builder.Length - 1] != '_')
                {
                    builder.Append('_');
                }
            }

            return builder.ToString().Trim('_');
        }
    }
}
