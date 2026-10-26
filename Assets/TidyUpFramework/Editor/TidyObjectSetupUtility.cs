using TidyUpFramework.Core;
using TidyUpFramework.Objects;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Editor
{
    public static class TidyObjectSetupUtility
    {
        const float MinimumColliderSize = 0.01f;

        [MenuItem(TidyEditorUtility.MenuRoot + "Make Selected Objects Interactive", false, 20)]
        static void MakeSelectedInteractive()
        {
            var count = 0;
            foreach (var selected in Selection.gameObjects)
            {
                if (selected != null && Configure(selected, null))
                {
                    count++;
                }
            }

            Debug.Log("Tidy Up Framework: configured " + count + " interactive object(s).");
        }

        [MenuItem(TidyEditorUtility.MenuRoot + "Make Selected Objects Interactive", true)]
        static bool CanMakeSelectedInteractive()
        {
            return Selection.gameObjects.Length > 0;
        }

        public static bool Configure(GameObject target, TidyObjectDefinition definition)
        {
            Undo.RegisterFullObjectHierarchyUndo(target, "Make Interactive");
            var tidyObject = target.GetComponent<TidyObject>();
            if (tidyObject == null)
            {
                tidyObject = Undo.AddComponent<TidyObject>(target);
            }

            if (definition != null)
            {
                tidyObject.AssignDefinition(definition);
            }

            if (target.GetComponent<Rigidbody>() == null)
            {
                var body = Undo.AddComponent<Rigidbody>(target);
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.None;
            }

            if (target.GetComponentInChildren<Collider>() == null)
            {
                FitBoxCollider(target);
            }

            if (string.IsNullOrEmpty(tidyObject.InstanceId) || tidyObject.InstanceId == target.name)
            {
                tidyObject.AssignInstanceId(TidyEditorUtility.Slug(target.name) + "_" + GlobalObjectId.GetGlobalObjectIdSlow(target).targetObjectId);
            }

            tidyObject.RefreshCachedComponents();
            EditorUtility.SetDirty(tidyObject);
            return true;
        }

        public static BoxCollider FitBoxCollider(GameObject target)
        {
            var bounds = TidyBoundsUtility.CalculateLocalBounds(target.transform);
            var collider = target.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = Undo.AddComponent<BoxCollider>(target);
            }

            Undo.RecordObject(collider, "Fit Collider");
            collider.isTrigger = false;
            collider.center = bounds.center;
            collider.size = Vector3.Max(bounds.size, Vector3.one * MinimumColliderSize);
            return collider;
        }
    }
}
