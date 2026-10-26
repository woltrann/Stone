using TidyUpFramework.Core;
using TidyUpFramework.UI;
using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyMenuBuilder
    {
        public static GameObject Build()
        {
            var canvas = TidyUIKit.Canvas("TidyMainMenu", 10);
            var stack = canvas.AddComponent<TidyScreenStack>();
            var vignette = TidyUIKit.Image("Vignette", canvas.transform, TidyUISprites.Glow, new Color32(12, 8, 12, 150), false);
            TidyUIKit.Stretch(vignette.rectTransform, -400f);
            var leftShade = TidyUIKit.Image("Left Shade", canvas.transform, TidyUISprites.Glow, new Color32(20, 14, 18, 210), false);
            TidyUIKit.Anchor(leftShade.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 0f), new Vector2(1500f, 1600f));

            var main = BuildMainScreen(canvas.transform, out var buttons);
            var levelSelect = BuildLevelSelect(canvas.transform, stack);
            var settings = TidyScreenBuilders.Settings(canvas.transform, stack);
            var controls = TidyScreenBuilders.Controls(canvas.transform, stack);
            var credits = TidyScreenBuilders.Credits(canvas.transform, stack);
            TidySerialization.Set(stack, "rootScreen", main);

            var menu = canvas.AddComponent<TidyMainMenu>();
            TidySerialization.Set(menu, "screenStack", stack);
            TidySerialization.Set(menu, "mainScreen", main);
            TidySerialization.Set(menu, "levelSelectScreen", levelSelect);
            TidySerialization.Set(menu, "settingsScreen", settings);
            TidySerialization.Set(menu, "controlsScreen", controls);
            TidySerialization.Set(menu, "creditsScreen", credits);
            TidySerialization.Set(menu, "playButton", buttons[0]);
            TidySerialization.Set(menu, "playButtonLabel", buttons[0].GetComponentInChildren<TextMeshProUGUI>());
            TidySerialization.Set(menu, "levelSelectButton", buttons[1]);
            TidySerialization.Set(menu, "settingsButton", buttons[2]);
            TidySerialization.Set(menu, "controlsButton", buttons[3]);
            TidySerialization.Set(menu, "creditsButton", buttons[4]);
            TidySerialization.Set(menu, "quitButton", buttons[5]);
            TidyUIKit.EventSystem(canvas.transform);
            return canvas;
        }

        static TidyUIScreen BuildMainScreen(Transform parent, out Button[] buttons)
        {
            var screen = TidyUIKit.Screen("Main Screen", parent, TidyScreenTransition.SlideRight, TidyControlChannel.None, false);
            screen.Configure(TidyScreenTransition.SlideRight, TidyControlChannel.None, true);
            var column = TidyUIKit.Rect("Column", screen.transform);
            TidyUIKit.Anchor(column, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, -10f), new Vector2(620f, 900f));
            var eyebrow = TidyUIKit.Text("Eyebrow", column, "SORT · ORGANISE · RESTORE", 24f, TidyUIPalette.Accent, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            eyebrow.characterSpacing = 10f;
            TidyUIKit.Anchor(eyebrow.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(620f, 36f));
            var title = TidyUIKit.Text("Title", column, "Tidy Up", 138f, TidyUIPalette.TextPrimary, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            TidyUIKit.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-6f, -30f), new Vector2(680f, 160f));
            title.outlineWidth = 0.08f;
            title.outlineColor = new Color32(30, 20, 18, 255);
            var subtitle = TidyUIKit.Text("Subtitle", column, "A satisfying organising framework", 30f, TidyUIPalette.TextSecondary, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            TidyUIKit.Anchor(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -190f), new Vector2(620f, 44f));
            var list = TidyUIKit.Rect("Buttons", column);
            TidyUIKit.Anchor(list, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -280f), new Vector2(420f, 560f));
            TidyUIKit.Vertical(list.gameObject, 14f, new RectOffset(0, 0, 0, 0));
            buttons = new[]
            {
                TidyUIKit.Button("Play", list, "Play", TidyButtonStyle.Primary, new Vector2(420f, 80f), 34f),
                TidyUIKit.Button("Level Select", list, "Level Select", TidyButtonStyle.Secondary, new Vector2(420f, 70f)),
                TidyUIKit.Button("Settings", list, "Settings", TidyButtonStyle.Secondary, new Vector2(420f, 70f)),
                TidyUIKit.Button("Controls", list, "Controls", TidyButtonStyle.Secondary, new Vector2(420f, 70f)),
                TidyUIKit.Button("Credits", list, "Credits", TidyButtonStyle.Secondary, new Vector2(420f, 70f)),
                TidyUIKit.Button("Quit", list, "Quit", TidyButtonStyle.Ghost, new Vector2(420f, 64f))
            };
            var version = TidyUIKit.Text("Version", screen.transform, "Tidy Up Framework · Demo", 20f, TidyUIPalette.TextMuted, TextAlignmentOptions.BottomRight);
            TidyUIKit.Anchor(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(600f, 30f));
            screen.FirstSelected = buttons[0].gameObject;
            return screen;
        }

        static TidyUIScreen BuildLevelSelect(Transform parent, TidyScreenStack stack)
        {
            var rect = TidyUIKit.Rect("Level Select Screen", parent);
            TidyUIKit.Stretch(rect);
            rect.gameObject.AddComponent<CanvasGroup>();
            var screen = rect.gameObject.AddComponent<TidyLevelSelectView>();
            screen.Configure(TidyScreenTransition.FadeScale, TidyControlChannel.All, false);
            var dim = TidyUIKit.Image("Dim", rect, null, TidyUIPalette.Overlay, false, true);
            TidyUIKit.Stretch(dim.rectTransform);
            var card = TidyScreenBuilders.Card("Level Select Card", rect, new Vector2(1640f, 900f), "Choose a Room", out var body);
            body.offsetMin = new Vector2(48f, 120f);
            var stars = TidyUIKit.Text("Total Stars", card, "0 / 0", 28f, TidyUIPalette.Accent, TextAlignmentOptions.TopRight, FontStyles.Bold);
            TidyUIKit.Anchor(stars.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56f, -40f), new Vector2(240f, 40f));
            var starIcon = TidyUIKit.Image("Star Icon", card, TidyUISprites.Star, TidyUIPalette.Accent, false);
            TidyUIKit.Anchor(starIcon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-300f, -36f), new Vector2(40f, 40f));
            var grid = TidyUIKit.Rect("Grid", body);
            TidyUIKit.Stretch(grid);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(360f, 560f);
            layout.spacing = new Vector2(28f, 28f);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            var cardPrefab = BuildLevelCard(grid);
            TidySerialization.Set(screen, "cardPrefab", cardPrefab);
            TidySerialization.Set(screen, "container", grid);
            TidySerialization.Set(screen, "totalStarsLabel", stars);
            TidyScreenBuilders.BackButton(card, stack);
            return screen;
        }

        static TidyLevelCardView BuildLevelCard(Transform parent)
        {
            var root = TidyUIKit.Rect("Level Card", parent);
            var background = root.gameObject.AddComponent<Image>();
            background.sprite = TidyUISprites.RoundedPanel;
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = TidyUIPalette.CardBackground;
            colors.highlightedColor = TidyUIPalette.CardHighlight;
            colors.selectedColor = TidyUIPalette.CardHighlight;
            colors.pressedColor = TidyUIPalette.ButtonPressed;
            colors.disabledColor = new Color32(40, 34, 38, 255);
            button.colors = colors;
            var glow = TidyUIKit.Image("Highlight", root, TidyUISprites.RoundedOutline, TidyUIPalette.Accent);
            TidyUIKit.Stretch(glow.rectTransform, -3f);
            var feedback = root.gameObject.AddComponent<TidyButtonFeedback>();
            TidySerialization.Set(feedback, "highlightVisual", glow.gameObject);
            TidySerialization.SetFloat(feedback, "hoverScale", 1.03f);

            var thumbnailFrame = TidyUIKit.Image("Thumbnail Frame", root, TidyUISprites.RoundedSmall, new Color32(20, 16, 20, 255));
            TidyUIKit.Anchor(thumbnailFrame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(328f, 220f));
            var mask = thumbnailFrame.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var thumbnail = TidyUIKit.Image("Thumbnail", thumbnailFrame.transform, null, Color.white, false);
            TidyUIKit.Stretch(thumbnail.rectTransform);
            thumbnail.preserveAspect = false;
            var accent = TidyUIKit.Image("Accent", root, TidyUISprites.RoundedSmall, TidyUIPalette.Accent);
            TidyUIKit.Anchor(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -244f), new Vector2(328f, 6f));
            var title = TidyUIKit.Text("Name", root, "1. Level", 30f, TidyUIPalette.TextPrimary, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            title.enableAutoSizing = true;
            title.fontSizeMin = 18f;
            title.fontSizeMax = 30f;
            TidyUIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -262f), new Vector2(320f, 40f));
            var difficulty = TidyUIKit.Text("Difficulty", root, "Normal", 20f, TidyUIPalette.Accent, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            TidyUIKit.Anchor(difficulty.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -304f), new Vector2(320f, 28f));
            var description = TidyUIKit.Text("Description", root, "Description", 21f, TidyUIPalette.TextSecondary, TextAlignmentOptions.TopLeft);
            description.enableAutoSizing = true;
            description.fontSizeMin = 15f;
            description.fontSizeMax = 21f;
            TidyUIKit.Anchor(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -338f), new Vector2(320f, 110f));
            var stars = new Image[3];
            for (var index = 0; index < 3; index++)
            {
                stars[index] = TidyUIKit.Image("Star " + (index + 1), root, TidyUISprites.Star, new Color(1f, 1f, 1f, 0.18f), false);
                TidyUIKit.Anchor(stars[index].rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f + index * 42f, 58f), new Vector2(38f, 38f));
            }

            var best = TidyUIKit.Text("Best", root, "Not completed", 19f, TidyUIPalette.TextMuted, TextAlignmentOptions.BottomLeft);
            TidyUIKit.Anchor(best.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(320f, 30f));
            var completed = TidyUIKit.Image("Completed", root, TidyUISprites.Check, TidyUIPalette.Success, false);
            TidyUIKit.Anchor(completed.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 56f), new Vector2(40f, 40f));
            var lockOverlay = TidyUIKit.Image("Lock Overlay", root, TidyUISprites.RoundedPanel, new Color32(16, 12, 16, 228));
            TidyUIKit.Stretch(lockOverlay.rectTransform);
            var lockIcon = TidyUIKit.Image("Lock", lockOverlay.transform, TidyUISprites.Lock, TidyUIPalette.TextSecondary, false);
            TidyUIKit.Anchor(lockIcon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -96f), new Vector2(72f, 72f));
            var lockLabel = TidyUIKit.Text("Lock Label", lockOverlay.transform, "Locked", 22f, TidyUIPalette.TextSecondary, TextAlignmentOptions.Top);
            lockLabel.enableAutoSizing = true;
            lockLabel.fontSizeMin = 16f;
            lockLabel.fontSizeMax = 22f;
            TidyUIKit.Anchor(lockLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -146f), new Vector2(300f, 64f));

            var view = root.gameObject.AddComponent<TidyLevelCardView>();
            TidySerialization.Set(view, "button", button);
            TidySerialization.Set(view, "nameLabel", title);
            TidySerialization.Set(view, "descriptionLabel", description);
            TidySerialization.Set(view, "difficultyLabel", difficulty);
            TidySerialization.Set(view, "bestLabel", best);
            TidySerialization.Set(view, "lockLabel", lockLabel);
            TidySerialization.Set(view, "thumbnail", thumbnail);
            TidySerialization.Set(view, "accent", accent);
            TidySerialization.SetArray(view, "stars", stars);
            TidySerialization.Set(view, "lockOverlay", lockOverlay.gameObject);
            TidySerialization.Set(view, "completedBadge", completed.gameObject);
            return view;
        }
    }
}
