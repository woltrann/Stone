using System.IO;
using System.Linq;
using TidyUpFramework.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TidyUpFramework.Editor
{
    [CustomEditor(typeof(TidyLevelDefinition))]
    public sealed class TidyLevelDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var level = (TidyLevelDefinition)target;
            DrawPreview(level);
            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
            EditorGUILayout.Space(6f);
            DrawActions(level);
        }

        static void DrawPreview(TidyLevelDefinition level)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(level.DisplayName, EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(level.Description))
                {
                    EditorGUILayout.LabelField(level.Description, EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUILayout.Space(2f);
                if (level.Objectives.Count == 0)
                {
                    EditorGUILayout.LabelField("No objectives yet.", EditorStyles.miniLabel);
                }

                foreach (var entry in level.Objectives)
                {
                    var title = entry?.Objective != null ? entry.Objective.FormatTitle(default) : "<missing objective>";
                    var prefix = entry != null && entry.Optional ? "○ Optional  " : "● ";
                    EditorGUILayout.LabelField(prefix + title, EditorStyles.miniLabel);
                }

                var timeText = level.HasTimeLimit ? Mathf.FloorToInt(level.TimeLimitSeconds / 60f) + ":" + Mathf.FloorToInt(level.TimeLimitSeconds % 60f).ToString("00") : "none";
                EditorGUILayout.LabelField("Difficulty " + level.Difficulty + "   •   Time limit " + timeText + "   •   Stars by " + level.StarMetric, EditorStyles.miniLabel);
            }
        }

        static void DrawActions(TidyLevelDefinition level)
        {
            var scenePath = FindScenePath(level.SceneName);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(scenePath)))
                {
                    if (GUILayout.Button("Open Scene") && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scenePath);
                    }
                }

                if (GUILayout.Button("Validate Open Scene"))
                {
                    TidyLevelValidatorWindow.Open();
                }
            }

            if (string.IsNullOrEmpty(scenePath) && !string.IsNullOrEmpty(level.SceneName))
            {
                EditorGUILayout.HelpBox("Scene '" + level.SceneName + "' was not found in the project.", MessageType.Warning);
            }
        }

        static string FindScenePath(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return null;
            }

            return AssetDatabase.FindAssets(sceneName + " t:Scene").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == sceneName);
        }
    }
}
