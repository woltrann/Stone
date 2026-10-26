using System.Collections.Generic;
using TidyUpFramework.Placement;
using TidyUpFramework.Rules;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidySceneDressing
    {
        public static void Settle(GameObject instance, float surfaceY)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            instance.transform.position += Vector3.up * (surfaceY - bounds.min.y + 0.0015f);
        }

        public static float TopOf(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var top = instance.transform.position.y;
            foreach (var renderer in renderers)
            {
                top = Mathf.Max(top, renderer.bounds.max.y);
            }

            return top;
        }

        public static T Target<T>(string targetName, Transform parent, Vector3 localPosition, Quaternion localRotation, string targetId, string displayName, TidyMatchFilter acceptance, Color gizmo, Vector3 triggerCenter, Vector3 triggerSize, TidyCompletionMode completion = TidyCompletionMode.WhenAllMatchingObjectsPlaced, int requiredCount = 1) where T : TidyPlacementTarget
        {
            var targetObject = new GameObject(targetName);
            targetObject.transform.SetParent(parent, false);
            targetObject.transform.localPosition = localPosition;
            targetObject.transform.localRotation = localRotation;
            targetObject.layer = LayerMask.NameToLayer("Default");
            var target = targetObject.AddComponent<T>();
            target.ConfigureIdentity(targetId, displayName);
            target.ConfigureRules(acceptance, TidyPlacementPolicy.RejectIncorrect, completion, requiredCount);
            target.SetGizmoColor(gizmo);
            TidyPropFactory.AimTrigger(targetObject, triggerCenter, triggerSize);
            return target;
        }

        public static TextMeshPro Label(string text, Transform parent, Vector3 localPosition, Quaternion localRotation, float fontSize, Color color, Vector2 size)
        {
            var labelObject = new GameObject("Label " + text, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            var label = labelObject.AddComponent<TextMeshPro>();
            label.font = TidyUIKit.Font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = size;
            label.rectTransform.localPosition = localPosition;
            label.rectTransform.localRotation = localRotation;
            label.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return label;
        }

        public static List<Vector3> ScatterPoints(System.Random random, Rect area, int count, float minimumDistance, IReadOnlyList<Rect> exclusions, float y)
        {
            var points = new List<Vector3>();
            var attempts = count * 40;
            while (points.Count < count && attempts-- > 0)
            {
                var candidate = new Vector2(Mathf.Lerp(area.xMin, area.xMax, (float)random.NextDouble()), Mathf.Lerp(area.yMin, area.yMax, (float)random.NextDouble()));
                if (exclusions != null && Contains(exclusions, candidate))
                {
                    continue;
                }

                var valid = true;
                foreach (var point in points)
                {
                    if ((new Vector2(point.x, point.z) - candidate).sqrMagnitude < minimumDistance * minimumDistance)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    points.Add(new Vector3(candidate.x, y, candidate.y));
                }
            }

            return points;
        }

        static bool Contains(IReadOnlyList<Rect> rects, Vector2 point)
        {
            foreach (var rect in rects)
            {
                if (rect.Contains(point))
                {
                    return true;
                }
            }

            return false;
        }

        public static GameObject PlaceObject(GameObject prefab, Transform parent, Vector3 position, Vector3 euler, float surfaceY)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            Settle(instance, surfaceY);
            return instance;
        }

        public static GameObject Decor(string objectName, Transform parent, Mesh mesh, Material[] materials, Vector3 localPosition, Quaternion localRotation)
        {
            var decor = TidyPropFactory.Visual(objectName, mesh, materials);
            decor.transform.SetParent(parent, false);
            decor.transform.localPosition = localPosition;
            decor.transform.localRotation = localRotation;
            return decor;
        }
    }
}
