using TidyUpFramework.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public enum TidyButtonStyle
    {
        Primary,
        Secondary,
        Ghost
    }

    public static class TidyUIKit
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        static TMP_FontAsset cachedFont;

        public static TMP_FontAsset Font
        {
            get
            {
                if (cachedFont == null)
                {
                    cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                }

                return cachedFont;
            }
        }

        public static GameObject Canvas(string canvasName, int sortingOrder)
        {
            var root = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.layer = LayerMask.NameToLayer("UI");
            return root;
        }

        public static GameObject EventSystem(Transform parent)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(parent, false);
            return eventSystem;
        }

        public static RectTransform Rect(string rectName, Transform parent)
        {
            var created = new GameObject(rectName, typeof(RectTransform));
            created.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)created.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        public static RectTransform Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Image(string imageName, Transform parent, Sprite sprite, Color color, bool sliced = true, bool raycast = false)
        {
            var rect = Rect(imageName, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sliced && sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget = raycast;
            image.preserveAspect = !sliced;
            return image;
        }

        public static Image Panel(string panelName, Transform parent, Color color, bool withShadow = true)
        {
            var container = Rect(panelName, parent);
            if (withShadow && TidyUISprites.Shadow != null)
            {
                var shadow = Image("Shadow", container, TidyUISprites.Shadow, TidyUIPalette.Shadow);
                shadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Stretch(shadow.rectTransform, -18f);
                shadow.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            }

            var background = container.gameObject.AddComponent<Image>();
            background.sprite = TidyUISprites.RoundedPanel;
            background.type = UnityEngine.UI.Image.Type.Sliced;
            background.color = color;
            background.raycastTarget = true;
            if (TidyUISprites.RoundedOutline != null)
            {
                var outline = Image("Outline", container, TidyUISprites.RoundedOutline, TidyUIPalette.PanelOutline);
                outline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Stretch(outline.rectTransform);
            }

            return background;
        }

        public static TextMeshProUGUI Text(string textName, Transform parent, string content, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left, FontStyles style = FontStyles.Normal)
        {
            var rect = Rect(textName, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.richText = true;
            return text;
        }

        public static Button Button(string buttonName, Transform parent, string label, TidyButtonStyle style, Vector2 size, float fontSize = 30f)
        {
            var rect = Rect(buttonName, parent);
            rect.sizeDelta = size;
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = size.y;
            layout.preferredWidth = size.x;
            var background = rect.gameObject.AddComponent<Image>();
            background.sprite = TidyUISprites.RoundedSmall != null ? TidyUISprites.RoundedSmall : TidyUISprites.RoundedPanel;
            background.type = UnityEngine.UI.Image.Type.Sliced;
            background.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ApplyStyle(button, background, style);
            var glow = Image("Highlight", rect, TidyUISprites.RoundedOutline, TidyUIPalette.Accent);
            Stretch(glow.rectTransform, -3f);
            var feedback = rect.gameObject.AddComponent<TidyButtonFeedback>();
            TidySerialization.Set(feedback, "highlightVisual", glow.gameObject);
            if (!string.IsNullOrEmpty(label))
            {
                var text = Text("Label", rect, label, fontSize, style == TidyButtonStyle.Primary ? new Color32(40, 28, 22, 255) : TidyUIPalette.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
                Stretch(text.rectTransform, 6f);
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }

            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;
            return button;
        }

        public static Button IconButton(string buttonName, Transform parent, Sprite icon, Vector2 size, Color iconColor)
        {
            var button = Button(buttonName, parent, null, TidyButtonStyle.Secondary, size);
            var image = Image("Icon", button.transform, icon, iconColor, false);
            Stretch(image.rectTransform, size.x * 0.24f);
            return button;
        }

        public static Slider Slider(string sliderName, Transform parent, Vector2 size)
        {
            var root = Rect(sliderName, parent);
            root.sizeDelta = size;
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            var track = Image("Track", root, TidyUISprites.RoundedSmall, TidyUIPalette.ButtonPressed);
            Stretch(track.rectTransform);
            track.rectTransform.offsetMin = new Vector2(0f, size.y * 0.35f);
            track.rectTransform.offsetMax = new Vector2(0f, -size.y * 0.35f);
            var fillArea = Rect("Fill Area", root);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(0f, size.y * 0.35f);
            fillArea.offsetMax = new Vector2(0f, -size.y * 0.35f);
            var fill = Image("Fill", fillArea, TidyUISprites.RoundedSmall, TidyUIPalette.Accent);
            Stretch(fill.rectTransform);
            var handleArea = Rect("Handle Area", root);
            Stretch(handleArea);
            var handle = Image("Handle", handleArea, TidyUISprites.Circle, TidyUIPalette.TextPrimary, false, true);
            handle.rectTransform.sizeDelta = new Vector2(size.y, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            var colors = slider.colors;
            colors.highlightedColor = TidyUIPalette.Accent;
            colors.selectedColor = TidyUIPalette.Accent;
            slider.colors = colors;
            return slider;
        }

        public static VerticalLayoutGroup Vertical(GameObject target, float spacing, RectOffset padding, TextAnchor alignment = TextAnchor.UpperLeft, bool controlHeight = true)
        {
            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup Horizontal(GameObject target, float spacing, RectOffset padding, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static void FitVertically(GameObject target)
        {
            var fitter = target.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        public static LayoutElement Layout(Component target, float preferredHeight, float preferredWidth = -1f, float flexibleWidth = -1f)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.gameObject.AddComponent<LayoutElement>();
            }

            element.preferredHeight = preferredHeight;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            return element;
        }

        public static TidyUIScreen Screen(string screenName, Transform parent, TidyScreenTransition transition, TidyUpFramework.Core.TidyControlChannel lockedChannels, bool overlay)
        {
            var rect = Rect(screenName, parent);
            Stretch(rect);
            rect.gameObject.AddComponent<CanvasGroup>();
            var screen = rect.gameObject.AddComponent<TidyUIScreen>();
            screen.Configure(transition, lockedChannels, false);
            if (overlay)
            {
                var dim = Image("Dim", rect, null, TidyUIPalette.Overlay, false, true);
                Stretch(dim.rectTransform);
            }

            return screen;
        }

        static void ApplyStyle(Button button, Image background, TidyButtonStyle style)
        {
            var colors = button.colors;
            colors.fadeDuration = 0.08f;
            colors.colorMultiplier = 1f;
            switch (style)
            {
                case TidyButtonStyle.Primary:
                    background.color = Color.white;
                    colors.normalColor = TidyUIPalette.Accent;
                    colors.highlightedColor = new Color32(255, 214, 140, 255);
                    colors.selectedColor = new Color32(255, 214, 140, 255);
                    colors.pressedColor = TidyUIPalette.AccentDark;
                    colors.disabledColor = TidyUIPalette.ButtonDisabled;
                    break;
                case TidyButtonStyle.Ghost:
                    background.color = Color.white;
                    colors.normalColor = new Color(1f, 1f, 1f, 0.04f);
                    colors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
                    colors.selectedColor = new Color(1f, 1f, 1f, 0.12f);
                    colors.pressedColor = new Color(1f, 1f, 1f, 0.2f);
                    colors.disabledColor = new Color(1f, 1f, 1f, 0.02f);
                    break;
                default:
                    background.color = Color.white;
                    colors.normalColor = TidyUIPalette.ButtonNormal;
                    colors.highlightedColor = TidyUIPalette.ButtonHighlighted;
                    colors.selectedColor = TidyUIPalette.ButtonHighlighted;
                    colors.pressedColor = TidyUIPalette.ButtonPressed;
                    colors.disabledColor = TidyUIPalette.ButtonDisabled;
                    break;
            }

            button.colors = colors;
        }
    }
}
