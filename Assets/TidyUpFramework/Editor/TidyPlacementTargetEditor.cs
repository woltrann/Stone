using System.Linq;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Editor
{
    [CustomEditor(typeof(TidyPlacementTarget), true)]
    [CanEditMultipleObjects]
    public sealed class TidyPlacementTargetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var placementTarget = (TidyPlacementTarget)target;
            DrawSummary(placementTarget);
            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
            EditorGUILayout.Space(4f);
            DrawTools(placementTarget);
            if (Application.isPlaying)
            {
                DrawRuntime(placementTarget);
                Repaint();
            }
        }

        static void DrawSummary(TidyPlacementTarget placementTarget)
        {
            var objects = Object.FindObjectsByType<TidyObject>(FindObjectsSortMode.None);
            var belonging = objects.Count(placementTarget.Belongs);
            var compatible = objects.Count(tidyObject => placementTarget.Belongs(tidyObject) && placementTarget.IsCompatible(tidyObject));
            var capacity = placementTarget.Capacity == int.MaxValue ? "unlimited" : placementTarget.Capacity.ToString();
            var message = "Accepts: " + placementTarget.Acceptance.Describe() + "\nCapacity: " + capacity + "\nMatching objects in scene: " + belonging;
            if (compatible < belonging)
            {
                message += "\n" + (belonging - compatible) + " matching object(s) are not compatible with this target type (check Stackable / Container / Snap flags).";
            }

            var hasAimCollider = placementTarget.GetComponentsInChildren<Collider>(true).Any(collider => collider.GetComponentInParent<TidyObject>() == null);
            if (!hasAimCollider)
            {
                message += "\nNo aim collider: add a trigger so players can point at this target.";
            }

            EditorGUILayout.HelpBox(message, hasAimCollider && compatible == belonging ? MessageType.Info : MessageType.Warning);
        }

        void DrawTools(TidyPlacementTarget placementTarget)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add / Reset Aim Trigger"))
                {
                    foreach (var selected in targets)
                    {
                        var current = (TidyPlacementTarget)selected;
                        var existing = current.GetComponent<BoxCollider>();
                        var center = existing != null ? existing.center : new Vector3(0f, 0.15f, 0f);
                        var size = existing != null ? existing.size : new Vector3(0.5f, 0.3f, 0.4f);
                        TidyEditorUtility.EnsureAimTrigger(current.gameObject, center, size);
                    }
                }

                if (GUILayout.Button("Select Matching Objects"))
                {
                    Selection.objects = Object.FindObjectsByType<TidyObject>(FindObjectsSortMode.None).Where(placementTarget.Belongs).Select(tidyObject => (Object)tidyObject.gameObject).ToArray();
                }
            }
        }

        static void DrawRuntime(TidyPlacementTarget placementTarget)
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Occupants", placementTarget.Occupants.Count);
                EditorGUILayout.IntField("Correct", placementTarget.CorrectCount);
                EditorGUILayout.Slider("Completion", placementTarget.CompletionRatio, 0f, 1f);
                EditorGUILayout.Toggle("Order Satisfied", placementTarget.IsOrderSatisfied);
                EditorGUILayout.Toggle("Complete", placementTarget.IsComplete);
            }
        }
    }
}
