using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseSigns
    {
        static readonly Dictionary<string, Mesh> Boxes = new Dictionary<string, Mesh>();

        public static GameObject Sign(TidyWarehouseStage stage, string signName, Transform parent, Vector3 position, float yaw, Vector2 size, string faceMaterial, string text, Color textColor, float fontSize, string subText = null, Color? subColor = null)
        {
            var key = Mathf.RoundToInt(size.x * 100f) + "x" + Mathf.RoundToInt(size.y * 100f);
            if (!Boxes.TryGetValue(key, out var mesh) || mesh == null)
            {
                mesh = TidyWarehouseFixtures.SignBox(TidyWarehouseStage.MeshFolder, size);
                Boxes[key] = mesh;
            }

            var sign = TidySceneDressing.Decor(signName, parent, mesh, new[] { stage.Material("Warehouse_SignPanel"), stage.Material(faceMaterial) }, Vector3.zero, Quaternion.identity);
            sign.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var textY = string.IsNullOrEmpty(subText) ? 0f : size.y * 0.12f;
            Text(text, sign.transform, new Vector3(0f, textY, 0.046f), fontSize, textColor, new Vector2(size.x - 0.1f, size.y * 0.7f));
            if (!string.IsNullOrEmpty(subText))
            {
                Text(subText, sign.transform, new Vector3(0f, -size.y * 0.3f, 0.046f), fontSize * 0.36f, subColor ?? Color.white, new Vector2(size.x - 0.1f, size.y * 0.3f));
            }

            return sign;
        }

        public static TextMeshPro Text(string text, Transform parent, Vector3 localPosition, float fontSize, Color color, Vector2 size)
        {
            var label = TidySceneDressing.Label(text, parent, localPosition, Quaternion.Euler(0f, 180f, 0f), fontSize, color, size);
            label.characterSpacing = 4f;
            label.enableAutoSizing = true;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = fontSize * 0.25f;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            return label;
        }

        public static TextMeshPro FloorText(string text, Transform parent, Vector3 position, float yaw, float fontSize, Color color, Vector2 size)
        {
            var label = TidySceneDressing.Label(text, parent, Vector3.zero, Quaternion.identity, fontSize, color, size);
            label.rectTransform.SetPositionAndRotation(new Vector3(position.x, TidyWarehouseStage.DecalHeight + 0.003f, position.z), Quaternion.Euler(90f, yaw, 0f));
            label.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return label;
        }
    }
}
