using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyMarketingShots
    {
        public const string Folder = "Marketing";
        const string OverlayName = "Marketing Overlay";
        const string BackgroundName = "Marketing Background";
        const int UILayer = 5;
        static readonly Color Accent = new Color32(242, 196, 109, 255);

        public static float UIScale { get; set; } = 1f;

        public static string Shot(string fileName, string caption, bool includeHud, int width = 1920, int height = 1080, Camera camera = null, Texture background = null)
        {
            var overlay = BuildOverlay();
            if (!string.IsNullOrEmpty(caption))
            {
                AddCaption(overlay.transform, caption, height);
            }

            return Finish(overlay, fileName, includeHud, width, height, camera, background);
        }

        public static string TitleCard(string fileName, string title, string subtitle, string tagline, int width, int height, Camera camera = null, Texture background = null, float scale = 1f)
        {
            var overlay = BuildOverlay();
            var root = overlay.transform;
            var fade = AddRaw(root, CenterFade());
            fade.rectTransform.anchorMin = new Vector2(0f, 0.18f);
            fade.rectTransform.anchorMax = new Vector2(1f, 0.82f);
            var reference = Mathf.Min(height, width * 0.62f);
            var titleText = AddText(root, title, 0.2f * reference * scale, Color.white, new Vector2(0.5f, 0.565f), FontStyles.Bold, 0.24f * reference * scale, 18f);
            titleText.outlineWidth = 0.06f;
            titleText.outlineColor = new Color32(20, 12, 14, 160);
            AddLine(root, new Vector2(0.5f, 0.445f), new Vector2(width * 0.36f * scale, Mathf.Max(2f, reference * 0.004f)), new Color(1f, 1f, 1f, 0.75f));
            AddText(root, subtitle, 0.055f * reference * scale, Accent, new Vector2(0.5f, 0.39f), FontStyles.Bold, 0.08f * reference * scale, 14f);
            if (!string.IsNullOrEmpty(tagline))
            {
                AddText(root, tagline, 0.034f * reference * scale, new Color(1f, 0.96f, 0.9f, 0.95f), new Vector2(0.5f, 0.32f), FontStyles.Normal, 0.05f * reference * scale, 4f);
            }

            return Finish(overlay, fileName, false, width, height, camera, background);
        }

        public static Texture2D Load(string path)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        public static Texture2D RenderScene(Camera camera, int width, int height)
        {
            camera = camera != null ? camera : Camera.main;
            return TidyThumbnailCapture.Render(camera, width, height);
        }

        public static Texture2D SideBySide(Texture2D left, Texture2D right, int width, int height, int divider = 6)
        {
            var result = new Texture2D(width, height, TextureFormat.RGB24, false);
            var half = width / 2;
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                var v = (y + 0.5f) / height;
                for (var x = 0; x < width; x++)
                {
                    Color color;
                    if (Mathf.Abs(x - half) < divider / 2)
                    {
                        color = Accent;
                    }
                    else if (x < half)
                    {
                        color = left.GetPixelBilinear(0.25f + (x + 0.5f) / width, v);
                    }
                    else
                    {
                        color = right.GetPixelBilinear(0.25f + (x - half + 0.5f) / width, v);
                    }

                    pixels[y * width + x] = color;
                }
            }

            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        public static GameObject BuildOverlay()
        {
            var canvasObject = new GameObject(OverlayName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            return canvasObject;
        }

        public static void AddLabel(GameObject overlay, string content, Vector2 anchor, float size)
        {
            var text = AddText(overlay.transform, content, size, Color.white, anchor, FontStyles.Bold, size * 1.5f, 8f);
            text.outlineWidth = 0.12f;
            text.outlineColor = new Color32(20, 12, 14, 220);
        }

        static string Finish(GameObject overlay, string fileName, bool includeHud, int width, int height, Camera camera, Texture background)
        {
            camera = camera != null ? camera : Camera.main;
            var hidden = includeHud ? new List<Canvas>() : HideCanvases(overlay);
            var scene = background;
            Texture2D rendered = null;
            if (scene == null)
            {
                rendered = TidyThumbnailCapture.Render(camera, width, height);
                scene = rendered;
            }

            var backgroundCanvas = new GameObject(BackgroundName, typeof(RectTransform), typeof(Canvas));
            backgroundCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            backgroundCanvas.GetComponent<Canvas>().sortingOrder = -30000;
            AddRaw(backgroundCanvas.transform, scene);
            var path = ComposeUI(fileName, width, height);
            Object.DestroyImmediate(backgroundCanvas);
            Object.DestroyImmediate(overlay);
            if (rendered != null)
            {
                Object.DestroyImmediate(rendered);
            }

            Restore(hidden);
            return path;
        }

        static string ComposeUI(string fileName, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            var cameraObject = new GameObject("Marketing UI Camera", typeof(Camera));
            cameraObject.transform.position = new Vector3(0f, -5000f, 0f);
            var uiCamera = cameraObject.GetComponent<Camera>();
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
            uiCamera.backgroundColor = Color.black;
            uiCamera.cullingMask = 1 << UILayer;
            uiCamera.nearClipPlane = 0.01f;
            uiCamera.farClipPlane = 10f;
            uiCamera.targetTexture = target;
            var restore = new List<(Canvas canvas, RenderMode mode, Camera camera, float distance)>();
            var scalers = new List<(CanvasScaler scaler, Vector2 resolution)>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || !canvas.enabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay || !canvas.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (canvas.name == OverlayName || canvas.name == BackgroundName)
                {
                    SetLayer(canvas.transform);
                }
                else if (canvas.TryGetComponent<CanvasScaler>(out var scaler) && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && !Mathf.Approximately(UIScale, 1f))
                {
                    scalers.Add((scaler, scaler.referenceResolution));
                    scaler.referenceResolution /= Mathf.Max(0.1f, UIScale);
                }

                restore.Add((canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 1f;
            }

            Canvas.ForceUpdateCanvases();
            uiCamera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previousActive;
            uiCamera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            foreach (var (canvas, mode, previousCamera, distance) in restore)
            {
                canvas.renderMode = mode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = distance;
            }

            foreach (var (scaler, resolution) in scalers)
            {
                scaler.referenceResolution = resolution;
            }

            Object.DestroyImmediate(cameraObject);
            Directory.CreateDirectory(Folder);
            var path = Folder + "/" + fileName + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return path;
        }

        static void SetLayer(Transform root)
        {
            root.gameObject.layer = UILayer;
            foreach (Transform child in root)
            {
                SetLayer(child);
            }
        }

        static void AddCaption(Transform root, string caption, int height)
        {
            var gradient = AddRaw(root, BottomFade());
            gradient.rectTransform.anchorMax = new Vector2(1f, 0.34f);
            var size = height * 0.058f;
            var text = AddText(root, caption.ToUpperInvariant(), size, Color.white, new Vector2(0.5f, 0.1f), FontStyles.Bold, size * 1.5f, 3f);
            text.outlineWidth = 0.1f;
            text.outlineColor = new Color32(20, 12, 14, 200);
            AddLine(root, new Vector2(0.5f, 0.058f), new Vector2(height * 0.075f, Mathf.Max(3f, height * 0.004f)), Accent);
        }

        static Texture2D BottomFade()
        {
            const int steps = 128;
            var texture = new Texture2D(1, steps, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (var index = 0; index < steps; index++)
            {
                var t = index / (steps - 1f);
                texture.SetPixel(0, index, new Color(0.02f, 0.015f, 0.015f, 0.85f * Mathf.Pow(1f - t, 1.4f)));
            }

            texture.Apply();
            return texture;
        }

        static Texture2D CenterFade()
        {
            const int steps = 128;
            var texture = new Texture2D(1, steps, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (var index = 0; index < steps; index++)
            {
                var t = Mathf.Abs(index / (steps - 1f) - 0.5f) * 2f;
                texture.SetPixel(0, index, new Color(0.03f, 0.02f, 0.02f, 0.78f * (1f - t * t * t)));
            }

            texture.Apply();
            return texture;
        }

        static RawImage AddRaw(Transform root, Texture texture)
        {
            var raw = new GameObject("Raw", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            raw.transform.SetParent(root, false);
            raw.texture = texture;
            raw.raycastTarget = false;
            raw.rectTransform.anchorMin = Vector2.zero;
            raw.rectTransform.anchorMax = Vector2.one;
            raw.rectTransform.offsetMin = Vector2.zero;
            raw.rectTransform.offsetMax = Vector2.zero;
            return raw;
        }

        static TextMeshProUGUI AddText(Transform root, string content, float size, Color color, Vector2 anchor, FontStyles style, float boxHeight, float spacing)
        {
            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(root, false);
            text.font = TidyUIKit.Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.characterSpacing = spacing;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = anchor;
            text.rectTransform.anchorMax = anchor;
            text.rectTransform.sizeDelta = new Vector2(6000f, boxHeight);
            text.rectTransform.anchoredPosition = Vector2.zero;
            return text;
        }

        static void AddLine(Transform root, Vector2 anchor, Vector2 size, Color color)
        {
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            line.transform.SetParent(root, false);
            line.color = color;
            line.raycastTarget = false;
            line.rectTransform.anchorMin = anchor;
            line.rectTransform.anchorMax = anchor;
            line.rectTransform.sizeDelta = size;
            line.rectTransform.anchoredPosition = Vector2.zero;
        }

        static List<Canvas> HideCanvases(GameObject overlay)
        {
            var hidden = new List<Canvas>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.isRootCanvas && canvas.enabled && canvas.gameObject != overlay)
                {
                    canvas.enabled = false;
                    hidden.Add(canvas);
                }
            }

            return hidden;
        }

        static void Restore(List<Canvas> hidden)
        {
            foreach (var canvas in hidden)
            {
                if (canvas != null)
                {
                    canvas.enabled = true;
                }
            }
        }
    }
}
