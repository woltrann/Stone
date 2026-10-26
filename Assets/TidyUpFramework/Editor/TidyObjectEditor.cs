using System.Linq;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Editor
{
    [CustomEditor(typeof(TidyObject))]
    [CanEditMultipleObjects]
    public sealed class TidyObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var tidyObject = (TidyObject)target;
            DrawSummary(tidyObject);
            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
            EditorGUILayout.Space(4f);
            DrawTools(tidyObject);
            if (Application.isPlaying)
            {
                DrawRuntimeState(tidyObject);
            }
        }

        static void DrawSummary(TidyObject tidyObject)
        {
            var definition = tidyObject.Definition;
            if (definition == null)
            {
                EditorGUILayout.HelpBox("Assign an Object Definition so sorting rules can recognise this object.", MessageType.Warning);
                return;
            }

            var tags = string.Join(", ", definition.Tags.Where(tag => tag != null).Select(tag => tag.DisplayName));
            var category = definition.Category != null ? definition.Category.DisplayName : "No category";
            var matchingTargets = Object.FindObjectsByType<TidyPlacementTarget>(FindObjectsSortMode.None).Count(placementTarget => placementTarget.Belongs(tidyObject));
            var summary = definition.DisplayName + "\nCategory: " + category + "\nTags: " + (tags.Length > 0 ? tags : "none") + "\nBelongs to " + matchingTargets + " target(s) in this scene.";
            EditorGUILayout.HelpBox(summary, matchingTargets > 0 || tidyObject.CountsAsClutter ? MessageType.Info : MessageType.Warning);
        }

        void DrawTools(TidyObject tidyObject)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fit Box Collider"))
                {
                    foreach (var selected in targets)
                    {
                        TidyObjectSetupUtility.FitBoxCollider(((TidyObject)selected).gameObject);
                    }
                }

                if (GUILayout.Button("Complete Setup"))
                {
                    foreach (var selected in targets)
                    {
                        TidyObjectSetupUtility.Configure(((TidyObject)selected).gameObject, null);
                    }
                }

                if (tidyObject.Definition != null && GUILayout.Button("Select Definition"))
                {
                    Selection.activeObject = tidyObject.Definition;
                }
            }
        }

        static void DrawRuntimeState(TidyObject tidyObject)
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("State", tidyObject.State);
                EditorGUILayout.ObjectField("Current Target", tidyObject.CurrentTarget, typeof(TidyPlacementTarget), true);
                EditorGUILayout.Toggle("Correctly Placed", tidyObject.IsCorrectlyPlaced);
                EditorGUILayout.IntField("Mistakes", tidyObject.MistakeCount);
            }
        }
    }
}
