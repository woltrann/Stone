using System.Collections.Generic;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TidyUpFramework.Editor
{
    public static class TidyIdGenerator
    {
        [MenuItem(TidyEditorUtility.MenuRoot + "Generate Missing Scene IDs", false, 21)]
        public static void GenerateSceneIds()
        {
            var objectCount = AssignObjectIds();
            var targetCount = AssignTargetIds();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("Tidy Up Framework: assigned " + objectCount + " object id(s) and " + targetCount + " target id(s).");
        }

        [MenuItem(TidyEditorUtility.MenuRoot + "Generate Missing Definition IDs", false, 22)]
        public static void GenerateDefinitionIds()
        {
            var assigned = 0;
            var used = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:TidyDefinition"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<TidyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition == null)
                {
                    continue;
                }

                var serialized = new SerializedObject(definition);
                var idProperty = serialized.FindProperty("id");
                var id = idProperty.stringValue;
                if (string.IsNullOrEmpty(id) || used.Contains(id))
                {
                    id = Unique(TidyEditorUtility.Slug(definition.name), used);
                    idProperty.stringValue = id;
                    serialized.ApplyModifiedProperties();
                    assigned++;
                }

                used.Add(id);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Tidy Up Framework: assigned " + assigned + " definition id(s).");
        }

        public static int AssignObjectIds()
        {
            var assigned = 0;
            var used = new HashSet<string>();
            var objects = Object.FindObjectsByType<TidyObject>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            foreach (var tidyObject in objects)
            {
                var serialized = new SerializedObject(tidyObject);
                var idProperty = serialized.FindProperty("instanceId");
                var id = idProperty.stringValue;
                if (!string.IsNullOrEmpty(id) && used.Add(id))
                {
                    continue;
                }

                var baseName = tidyObject.Definition != null ? tidyObject.Definition.Id : TidyEditorUtility.Slug(tidyObject.name);
                id = Unique(baseName, used);
                idProperty.stringValue = id;
                serialized.ApplyModifiedProperties();
                used.Add(id);
                assigned++;
            }

            return assigned;
        }

        public static int AssignTargetIds()
        {
            var assigned = 0;
            var used = new HashSet<string>();
            var targets = Object.FindObjectsByType<TidyPlacementTarget>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            foreach (var target in targets)
            {
                var serialized = new SerializedObject(target);
                var idProperty = serialized.FindProperty("targetId");
                var id = idProperty.stringValue;
                if (!string.IsNullOrEmpty(id) && used.Add(id))
                {
                    continue;
                }

                id = Unique(TidyEditorUtility.Slug(target.name), used);
                idProperty.stringValue = id;
                serialized.ApplyModifiedProperties();
                used.Add(id);
                assigned++;
            }

            return assigned;
        }

        static string Unique(string baseName, HashSet<string> used)
        {
            var candidate = baseName;
            var suffix = 1;
            while (used.Contains(candidate))
            {
                candidate = baseName + "_" + suffix.ToString("00");
                suffix++;
            }

            return candidate;
        }
    }
}
