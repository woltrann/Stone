using TidyUpFramework.Core;
using TidyUpFramework.Input;
using TidyUpFramework.UI;
using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyHudBuilder
    {
        public static GameObject Build()
        {
            var canvas = TidyUIKit.Canvas("TidyHUD", 10);
            var safe = TidyUIKit.Stretch(TidyUIKit.Rect("Safe Area", canvas.transform));
            safe.gameObject.AddComponent<TidySafeArea>();
            var stack = canvas.AddComponent<TidyScreenStack>();

            BuildFloatingScores(safe);
            BuildCrosshairAndPrompts(safe);
            BuildObjectives(safe);
            BuildScoreCorner(safe);
            BuildProgress(safe);
            BuildBanner(safe);
            BuildHintButton(safe);
            BuildInspectPanel(safe);
            BuildTutorialPanel(safe);
            BuildMobileControls(safe);

            var pause = BuildPause(canvas.transform, stack, out var pauseMenu);
            var settings = TidyScreenBuilders.Settings(canvas.transform, stack);
            var controls = TidyScreenBuilders.Controls(canvas.transform, stack);
            TidySerialization.Set(pauseMenu, "settingsScreen", settings);
            TidySerialization.Set(pauseMenu, "controlsScreen", controls);
            BuildResults(canvas.transform);
            TidyUIKit.EventSystem(canvas.transform);
            return canvas;
        }

        static void BuildFloatingScores(RectTransform parent)
        {
            var layer = TidyUIKit.Stretch(TidyUIKit.Rect("Floating Scores", parent));
            var prefab = TidyUIKit.Text("Floating Score", layer, "+10", 40f, TidyUIPalette.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
            prefab.rectTransform.sizeDelta = new Vector2(200f, 60f);
            prefab.outlineWidth = 0.18f;
            prefab.outlineColor = new Color32(40, 25, 20, 255);
            var view = layer.gameObject.AddComponent<TidyFloatingScoreView>();
            TidySerialization.Set(view, "labelPrefab", prefab);
            TidySerialization.Set(view, "container", layer);
        }

        static void BuildCrosshairAndPrompts(RectTransform parent)
        {
            var crosshair = TidyUIKit.Image("Crosshair", parent, TidyUISprites.Crosshair, Color.white, false);
            TidyUIKit.Anchor(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
            var nameLabel = TidyUIKit.Text("Object Name", parent, string.Empty, 30f, TidyUIPalette.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            TidyUIKit.Anchor(nameLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(900f, 44f));
            nameLabel.outlineWidth = 0.15f;
            nameLabel.outlineColor = new Color32(20, 14, 16, 220);

            var promptBar = TidyUIKit.Image("Prompt Bar", parent, TidyUISprites.RoundedPanel, TidyUIPalette.PanelBackgroundSoft);
            TidyUIKit.Anchor(promptBar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(860f, 64f));
            var group = promptBar.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            var prompt = TidyUIKit.Text("Prompt", promptBar.transform, string.Empty, 28f, TidyUIPalette.TextPrimary, TextAlignmentOptions.Center);
            TidyUIKit.Stretch(prompt.rectTransform, 8f);
            var view = parent.gameObject.AddComponent<TidyInteractionPromptView>();
            TidySerialization.Set(view, "promptLabel", prompt);
            TidySerialization.Set(view, "objectNameLabel", nameLabel);
            TidySerialization.Set(view, "crosshair", crosshair);
            TidySerialization.Set(view, "promptGroup", group);
        }

        static void BuildObjectives(RectTransform parent)
        {
            var panel = TidyUIKit.Panel("Objectives", parent, TidyUIPalette.PanelBackgroundSoft, false);
            TidyUIKit.Anchor(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(520f, 0f));
            TidyUIKit.Vertical(panel.gameObject, 8f, new RectOffset(26, 26, 20, 22));
            TidyUIKit.FitVertically(panel.gameObject);
            var header = TidyUIKit.Text("Header", panel.transform, "OBJECTIVES", 22f, TidyUIPalette.Accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            header.characterSpacing = 8f;
            TidyUIKit.Layout(header, 30f);
            var list = TidyUIKit.Rect("List", panel.transform);
            TidyUIKit.Vertical(list.gameObject, 8f, new RectOffset(0, 0, 0, 0));
            TidyUIKit.FitVertically(list.gameObject);
            var item = BuildObjectiveItem(list);
            item.gameObject.SetActive(false);
            var view = panel.gameObject.AddComponent<TidyObjectiveListView>();
            TidySerialization.Set(view, "itemPrefab", item);
            TidySerialization.Set(view, "container", list);
        }

        static TidyObjectiveItemView BuildObjectiveItem(Transform parent)
        {
            var row = TidyUIKit.Rect("Objective Item", parent);
            TidyUIKit.Layout(row, 56f);
            var check = TidyUIKit.Image("Check Box", row, TidyUISprites.RoundedSmall, new Color(1f, 1f, 1f, 0.1f));
            TidyUIKit.Anchor(check.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -4f), new Vector2(28f, 28f));
            var checkmark = TidyUIKit.Image("Checkmark", check.transform, TidyUISprites.Check, TidyUIPalette.Success, false);
            TidyUIKit.Stretch(checkmark.rectTransform, 3f);
            var title = TidyUIKit.Text("Title", row, "Objective", 26f, TidyUIPalette.TextPrimary, TextAlignmentOptions.TopLeft);
            TidyUIKit.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(42f, 12f);
            title.rectTransform.offsetMax = new Vector2(-80f, 0f);
            var counter = TidyUIKit.Text("Counter", row, "0/0", 24f, TidyUIPalette.TextSecondary, TextAlignmentOptions.TopRight, FontStyles.Bold);
            TidyUIKit.Anchor(counter.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(90f, 32f));
            var track = TidyUIKit.Image("Progress Track", row, TidyUISprites.RoundedSmall, new Color(1f, 1f, 1f, 0.08f));
            TidyUIKit.Anchor(track.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(42f, 2f), new Vector2(426f, 6f));
            var fill = TidyUIKit.Image("Progress Fill", track.transform, null, TidyUIPalette.Accent, false);
            TidyUIKit.Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.preserveAspect = false;
            fill.sprite = TidyUISprites.RoundedSmall;
            title.enableAutoSizing = true;
            title.fontSizeMin = 18f;
            title.fontSizeMax = 25f;
            var view = row.gameObject.AddComponent<TidyObjectiveItemView>();
            TidySerialization.Set(view, "titleLabel", title);
            TidySerialization.Set(view, "counterLabel", counter);
            TidySerialization.Set(view, "progressFill", fill);
            TidySerialization.Set(view, "checkmark", checkmark.gameObject);
            return view;
        }

        static void BuildScoreCorner(RectTransform parent)
        {
            var panel = TidyUIKit.Panel("Score Panel", parent, TidyUIPalette.PanelBackgroundSoft, false);
            TidyUIKit.Anchor(panel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -32f), new Vector2(300f, 150f));
            var caption = TidyUIKit.Text("Caption", panel.transform, "SCORE", 20f, TidyUIPalette.Accent, TextAlignmentOptions.TopRight, FontStyles.Bold);
            caption.characterSpacing = 8f;
            TidyUIKit.Anchor(caption.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -18f), new Vector2(240f, 28f));
            var score = TidyUIKit.Text("Score", panel.transform, "0", 56f, TidyUIPalette.TextPrimary, TextAlignmentOptions.TopRight, FontStyles.Bold);
            TidyUIKit.Anchor(score.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -42f), new Vector2(260f, 64f));
            var scoreView = panel.gameObject.AddComponent<TidyScoreView>();
            TidySerialization.Set(scoreView, "scoreLabel", score);
            var timerIcon = TidyUIKit.Image("Timer Icon", panel.transform, TidyUISprites.Hourglass, TidyUIPalette.TextSecondary, false);
            TidyUIKit.Anchor(timerIcon.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-130f, 18f), new Vector2(24f, 24f));
            var timer = TidyUIKit.Text("Timer", panel.transform, "0:00", 28f, TidyUIPalette.TextSecondary, TextAlignmentOptions.BottomRight);
            TidyUIKit.Anchor(timer.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-26f, 14f), new Vector2(100f, 36f));
            var timerView = panel.gameObject.AddComponent<TidyTimerView>();
            TidySerialization.Set(timerView, "timeLabel", timer);
            TidySerialization.SetColor(timerView, "normalColor", TidyUIPalette.TextSecondary);

            var combo = TidyUIKit.Rect("Combo", parent);
            TidyUIKit.Anchor(combo, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -200f), new Vector2(300f, 140f));
            var comboGroup = combo.gameObject.AddComponent<CanvasGroup>();
            var count = TidyUIKit.Text("Count", combo, "x2", 64f, TidyUIPalette.Accent, TextAlignmentOptions.TopRight, FontStyles.Bold);
            TidyUIKit.Anchor(count.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(200f, 72f));
            count.outlineWidth = 0.15f;
            count.outlineColor = new Color32(40, 25, 20, 255);
            var multiplier = TidyUIKit.Text("Multiplier", combo, "1.0x score", 22f, TidyUIPalette.TextSecondary, TextAlignmentOptions.TopRight);
            TidyUIKit.Anchor(multiplier.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -74f), new Vector2(240f, 28f));
            var windowTrack = TidyUIKit.Image("Window Track", combo, TidyUISprites.RoundedSmall, new Color(1f, 1f, 1f, 0.1f));
            TidyUIKit.Anchor(windowTrack.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -108f), new Vector2(180f, 6f));
            var windowFill = TidyUIKit.Image("Window Fill", windowTrack.transform, TidyUISprites.RoundedSmall, TidyUIPalette.Accent, false);
            TidyUIKit.Stretch(windowFill.rectTransform);
            windowFill.type = Image.Type.Filled;
            windowFill.fillMethod = Image.FillMethod.Horizontal;
            windowFill.preserveAspect = false;
            windowFill.fillOrigin = (int)Image.OriginHorizontal.Right;
            var tier = TidyUIKit.Text("Tier", parent, "Tidy!", 72f, TidyUIPalette.Accent, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            TidyUIKit.Anchor(tier.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(700f, 100f));
            tier.outlineWidth = 0.2f;
            tier.outlineColor = new Color32(40, 25, 20, 255);
            var comboView = combo.gameObject.AddComponent<TidyComboView>();
            TidySerialization.Set(comboView, "group", comboGroup);
            TidySerialization.Set(comboView, "countLabel", count);
            TidySerialization.Set(comboView, "multiplierLabel", multiplier);
            TidySerialization.Set(comboView, "tierLabel", tier);
            TidySerialization.Set(comboView, "windowFill", windowFill);
        }

        static void BuildProgress(RectTransform parent)
        {
            var root = TidyUIKit.Rect("Progress", parent);
            TidyUIKit.Anchor(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(560f, 64f));
            var label = TidyUIKit.Text("Percent", root, "0% tidy", 24f, TidyUIPalette.TextPrimary, TextAlignmentOptions.Top, FontStyles.Bold);
            TidyUIKit.Anchor(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(560f, 32f));
            label.outlineWidth = 0.12f;
            label.outlineColor = new Color32(20, 14, 16, 200);
            var track = TidyUIKit.Image("Track", root, TidyUISprites.RoundedSmall, new Color32(20, 16, 20, 170));
            TidyUIKit.Anchor(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(560f, 16f));
            var fill = TidyUIKit.Image("Fill", track.transform, TidyUISprites.RoundedSmall, TidyUIPalette.Accent, false);
            TidyUIKit.Stretch(fill.rectTransform, 2f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.preserveAspect = false;
            var view = root.gameObject.AddComponent<TidyProgressView>();
            TidySerialization.Set(view, "fillImage", fill);
            TidySerialization.Set(view, "percentLabel", label);
        }

        static void BuildBanner(RectTransform parent)
        {
            var banner = TidyUIKit.Rect("Banner", parent);
            TidyUIKit.Anchor(banner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(900f, 130f));
            var group = banner.gameObject.AddComponent<CanvasGroup>();
            var background = TidyUIKit.Image("Background", banner, TidyUISprites.Glow, new Color32(20, 14, 18, 200), false);
            TidyUIKit.Stretch(background.rectTransform, -60f);
            var title = TidyUIKit.Text("Title", banner, "Level", 52f, TidyUIPalette.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
            TidyUIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 66f));
            var subtitle = TidyUIKit.Text("Subtitle", banner, string.Empty, 28f, TidyUIPalette.TextPrimary, TextAlignmentOptions.Top);
            TidyUIKit.Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(860f, 60f));
            var view = banner.gameObject.AddComponent<TidyBannerView>();
            TidySerialization.Set(view, "group", group);
            TidySerialization.Set(view, "titleLabel", title);
            TidySerialization.Set(view, "subtitleLabel", subtitle);
        }

        static void BuildHintButton(RectTransform parent)
        {
            var root = TidyUIKit.Rect("Hint", parent);
            TidyUIKit.Anchor(root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(104f, 104f));
            var button = TidyUIKit.IconButton("Hint Button", root, TidyUISprites.Lightbulb, new Vector2(104f, 104f), TidyUIPalette.Accent);
            ((Image)button.targetGraphic).sprite = TidyUISprites.Circle;
            var cooldown = TidyUIKit.Image("Cooldown", button.transform, TidyUISprites.Circle, new Color32(10, 8, 10, 170), false);
            TidyUIKit.Stretch(cooldown.rectTransform);
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillAmount = 0f;
            var count = TidyUIKit.Text("Count", root, string.Empty, 22f, TidyUIPalette.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            TidyUIKit.Anchor(count.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), new Vector2(40f, 40f));
            var key = TidyUIKit.Text("Key", root, "{Hint}", 20f, TidyUIPalette.TextSecondary, TextAlignmentOptions.Center);
            TidyUIKit.Anchor(key.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(160f, 28f));
            key.gameObject.AddComponent<TidyPromptLabel>().Template = "{Hint}";
            var view = root.gameObject.AddComponent<TidyHintButtonView>();
            TidySerialization.Set(view, "button", button);
            TidySerialization.Set(view, "cooldownFill", cooldown);
            TidySerialization.Set(view, "countLabel", count);
            TidySerialization.Set(view, "root", button.gameObject);
        }

        static void BuildInspectPanel(RectTransform parent)
        {
            var screen = TidyUIKit.Screen("Inspect Panel", parent, TidyScreenTransition.SlideRight, TidyControlChannel.None, false);
            screen.GetComponent<CanvasGroup>().blocksRaycasts = false;
            var panel = TidyUIKit.Panel("Card", screen.transform, TidyUIPalette.PanelBackground);
            TidyUIKit.Anchor(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, 0f), new Vector2(460f, 420f));
            panel.raycastTarget = false;
            TidyUIKit.Vertical(panel.gameObject, 10f, new RectOffset(32, 32, 28, 28));
            var title = TidyUIKit.Text("Name", panel.transform, "Name", 38f, TidyUIPalette.Accent, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            TidyUIKit.Layout(title, 52f);
            var category = TidyUIKit.Text("Category", panel.transform, "Category", 24f, TidyUIPalette.TextSecondary, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            TidyUIKit.Layout(category, 32f);
            var tags = TidyUIKit.Text("Tags", panel.transform, string.Empty, 22f, TidyUIPalette.TextPrimary, TextAlignmentOptions.TopLeft);
            TidyUIKit.Layout(tags, 64f);
            var description = TidyUIKit.Text("Description", panel.transform, string.Empty, 24f, TidyUIPalette.TextSecondary, TextAlignmentOptions.TopLeft);
            TidyUIKit.Layout(description, 180f);
            var view = parent.gameObject.AddComponent<TidyInspectPanelView>();
            TidySerialization.Set(view, "screen", screen);
            TidySerialization.Set(view, "nameLabel", title);
            TidySerialization.Set(view, "categoryLabel", category);
            TidySerialization.Set(view, "tagsLabel", tags);
            TidySerialization.Set(view, "descriptionLabel", description);
        }

        static void BuildTutorialPanel(RectTransform parent)
        {
            var screen = TidyUIKit.Screen("Tutorial Panel", parent, TidyScreenTransition.SlideUp, TidyControlChannel.None, false);
            var panel = TidyUIKit.Panel("Card", screen.transform, TidyUIPalette.PanelBackground);
            TidyUIKit.Anchor(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(880f, 170f));
            var icon = TidyUIKit.Image("Icon", panel.transform, TidyUISprites.Lightbulb, TidyUIPalette.Accent, false);
            TidyUIKit.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 10f), new Vector2(64f, 64f));
            var message = TidyUIKit.Text("Message", panel.transform, "Message", 30f, TidyUIPalette.TextPrimary, TextAlignmentOptions.MidlineLeft);
            TidyUIKit.Stretch(message.rectTransform);
            message.rectTransform.offsetMin = new Vector2(118f, 40f);
            message.rectTransform.offsetMax = new Vector2(-30f, -20f);
            var prompt = TidyUIKit.Text("Prompt", panel.transform, "[E]", 26f, TidyUIPalette.Accent, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            TidyUIKit.Anchor(prompt.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(118f, 14f), new Vector2(300f, 32f));
            var step = TidyUIKit.Text("Step", panel.transform, "1 / 5", 20f, TidyUIPalette.TextMuted, TextAlignmentOptions.TopRight);
            TidyUIKit.Anchor(step.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -14f), new Vector2(120f, 26f));
            var continueButton = TidyUIKit.Button("Continue", panel.transform, "Got it", TidyButtonStyle.Primary, new Vector2(150f, 46f), 22f);
            TidyUIKit.Anchor((RectTransform)continueButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 14f), new Vector2(150f, 46f));
            var skip = TidyUIKit.Button("Skip", panel.transform, "Skip tutorial", TidyButtonStyle.Ghost, new Vector2(170f, 40f), 18f);
            TidyUIKit.Anchor((RectTransform)skip.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 17f), new Vector2(170f, 40f));
            var view = parent.gameObject.AddComponent<TidyTutorialView>();
            TidySerialization.Set(view, "screen", screen);
            TidySerialization.Set(view, "messageLabel", message);
            TidySerialization.Set(view, "promptLabel", prompt);
            TidySerialization.Set(view, "stepLabel", step);
            TidySerialization.Set(view, "continueButton", continueButton);
            TidySerialization.Set(view, "skipButton", skip);
        }

        static void BuildMobileControls(RectTransform parent)
        {
            var root = TidyUIKit.Stretch(TidyUIKit.Rect("Mobile Controls", parent));
            var controls = TidyUIKit.Stretch(TidyUIKit.Rect("Touch Root", root));
            var lookArea = TidyUIKit.Image("Look Area", controls, null, new Color(0f, 0f, 0f, 0f), false, true);
            TidyUIKit.Stretch(lookArea.rectTransform);
            lookArea.rectTransform.anchorMin = new Vector2(0.35f, 0f);
            lookArea.gameObject.AddComponent<TidyTouchLookArea>();
            var joystickArea = TidyUIKit.Image("Joystick Area", controls, null, new Color(0f, 0f, 0f, 0f), false, true);
            joystickArea.rectTransform.anchorMin = Vector2.zero;
            joystickArea.rectTransform.anchorMax = new Vector2(0.35f, 0.6f);
            joystickArea.rectTransform.offsetMin = Vector2.zero;
            joystickArea.rectTransform.offsetMax = Vector2.zero;
            var background = TidyUIKit.Image("Joystick", joystickArea.transform, TidyUISprites.Ring, new Color(1f, 1f, 1f, 0.35f), false);
            TidyUIKit.Anchor(background.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(220f, 220f), new Vector2(220f, 220f));
            var handle = TidyUIKit.Image("Handle", background.transform, TidyUISprites.Circle, new Color(1f, 0.95f, 0.85f, 0.7f), false);
            TidyUIKit.Anchor(handle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
            var joystick = joystickArea.gameObject.AddComponent<TidyVirtualJoystick>();
            TidySerialization.Set(joystick, "background", background.rectTransform);
            TidySerialization.Set(joystick, "handle", handle.rectTransform);

            var interact = TouchButton(controls, "Interact", TidyInputAction.Interact, TidyUISprites.Circle, new Vector2(-150f, 190f), 150f, "Use");
            var pause = TouchButton(controls, "Pause", TidyInputAction.Pause, TidyUISprites.RoundedSmall, new Vector2(340f, -62f), 72f, "II");
            ((RectTransform)pause.transform).anchorMin = new Vector2(0.5f, 1f);
            ((RectTransform)pause.transform).anchorMax = new Vector2(0.5f, 1f);
            var held = TidyUIKit.Stretch(TidyUIKit.Rect("Held Buttons", controls));
            TouchButton(held, "Drop", TidyInputAction.Drop, TidyUISprites.Circle, new Vector2(-330f, 120f), 100f, "Drop");
            TouchButton(held, "Inspect", TidyInputAction.Inspect, TidyUISprites.Circle, new Vector2(-150f, 380f), 100f, "Look");
            var rotateLeft = TouchButton(held, "Rotate Left", TidyInputAction.Rotate, TidyUISprites.Circle, new Vector2(-330f, 260f), 90f, "<");
            rotateLeft.GetComponent<TidyTouchActionButton>().Configure(TidyInputAction.Rotate, -1f);
            var rotateRight = TouchButton(held, "Rotate Right", TidyInputAction.Rotate, TidyUISprites.Circle, new Vector2(-330f, 380f), 90f, ">");
            rotateRight.GetComponent<TidyTouchActionButton>().Configure(TidyInputAction.Rotate, 1f);
            var mobile = root.gameObject.AddComponent<TidyMobileControls>();
            TidySerialization.Set(mobile, "controlsRoot", controls.gameObject);
            TidySerialization.Set(mobile, "heldObjectButtons", held.gameObject);
            interact.transform.SetAsLastSibling();
        }

        static GameObject TouchButton(Transform parent, string buttonName, TidyInputAction action, Sprite sprite, Vector2 position, float size, string label)
        {
            var image = TidyUIKit.Image(buttonName, parent, sprite, new Color32(34, 27, 33, 190), true, true);
            TidyUIKit.Anchor(image.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
            var ring = TidyUIKit.Image("Ring", image.transform, TidyUISprites.Ring, new Color32(242, 196, 109, 160), false);
            TidyUIKit.Stretch(ring.rectTransform);
            var text = TidyUIKit.Text("Label", image.transform, label, size * 0.24f, TidyUIPalette.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            TidyUIKit.Stretch(text.rectTransform);
            var button = image.gameObject.AddComponent<TidyTouchActionButton>();
            button.Configure(action, 1f);
            return image.gameObject;
        }

        static TidyUIScreen BuildPause(Transform parent, TidyScreenStack stack, out TidyPauseMenu pauseMenu)
        {
            var screen = TidyUIKit.Screen("Pause Screen", parent, TidyScreenTransition.FadeScale, TidyControlChannel.All, true);
            var card = TidyScreenBuilders.Card("Pause Card", screen.transform, new Vector2(560f, 700f), "Paused", out var body);
            TidyUIKit.Vertical(body.gameObject, 16f, new RectOffset(40, 40, 10, 10), TextAnchor.UpperCenter);
            var resume = TidyUIKit.Button("Resume", body, "Resume", TidyButtonStyle.Primary, new Vector2(400f, 72f));
            var save = TidyUIKit.Button("Save", body, "Save Progress", TidyButtonStyle.Secondary, new Vector2(400f, 68f));
            var restart = TidyUIKit.Button("Restart", body, "Restart", TidyButtonStyle.Secondary, new Vector2(400f, 68f));
            var settings = TidyUIKit.Button("Settings", body, "Settings", TidyButtonStyle.Secondary, new Vector2(400f, 68f));
            var controls = TidyUIKit.Button("Controls", body, "Controls", TidyButtonStyle.Secondary, new Vector2(400f, 68f));
            var menu = TidyUIKit.Button("Main Menu", body, "Main Menu", TidyButtonStyle.Secondary, new Vector2(400f, 68f));
            pauseMenu = parent.gameObject.AddComponent<TidyPauseMenu>();
            TidySerialization.Set(pauseMenu, "pauseScreen", screen);
            TidySerialization.Set(pauseMenu, "screenStack", stack);
            TidySerialization.Set(pauseMenu, "resumeButton", resume);
            TidySerialization.Set(pauseMenu, "restartButton", restart);
            TidySerialization.Set(pauseMenu, "settingsButton", settings);
            TidySerialization.Set(pauseMenu, "controlsButton", controls);
            TidySerialization.Set(pauseMenu, "menuButton", menu);
            TidySerialization.Set(pauseMenu, "saveButton", save);
            screen.FirstSelected = resume.gameObject;
            return screen;
        }

        static void BuildResults(Transform parent)
        {
            var screen = TidyUIKit.Screen("Results Screen", parent, TidyScreenTransition.FadeScale, TidyControlChannel.All, true);
            var card = TidyScreenBuilders.Card("Results Card", screen.transform, new Vector2(900f, 900f), "Beautifully organised!", out var body);
            var title = card.Find("Title").GetComponent<TextMeshProUGUI>();
            var starsRow = TidyUIKit.Rect("Stars", body);
            TidyUIKit.Anchor(starsRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(420f, 130f));
            var stars = new Image[3];
            for (var index = 0; index < 3; index++)
            {
                var size = index == 1 ? 130f : 104f;
                stars[index] = TidyUIKit.Image("Star " + (index + 1), starsRow, TidyUISprites.Star, new Color(1f, 1f, 1f, 0.15f), false);
                TidyUIKit.Anchor(stars[index].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((index - 1) * 140f, index == 1 ? 6f : -8f), new Vector2(size, size));
            }

            var score = TidyUIKit.Text("Score", body, "0", 84f, TidyUIPalette.TextPrimary, TextAlignmentOptions.Top, FontStyles.Bold);
            TidyUIKit.Anchor(score.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(600f, 100f));
            var best = TidyUIKit.Text("New Best", body, "NEW BEST!", 24f, TidyUIPalette.Success, TextAlignmentOptions.Top, FontStyles.Bold);
            TidyUIKit.Anchor(best.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(300f, 32f));
            var stats = TidyUIKit.Rect("Stats", body);
            TidyUIKit.Anchor(stats, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(720f, 90f));
            TidyUIKit.Horizontal(stats.gameObject, 20f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
            var time = Stat(stats, "Time");
            var accuracy = Stat(stats, "Accuracy");
            var combo = Stat(stats, "Best Combo");
            var breakdown = TidyUIKit.Rect("Breakdown", body);
            TidyUIKit.Anchor(breakdown, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -390f), new Vector2(620f, 250f));
            TidyUIKit.Vertical(breakdown.gameObject, 2f, new RectOffset(0, 0, 0, 0));
            var line = TidyUIKit.Text("Line", breakdown, "Label<pos=75%>+0", 24f, TidyUIPalette.TextSecondary, TextAlignmentOptions.MidlineLeft);
            TidyUIKit.Layout(line, 34f);
            var buttons = TidyUIKit.Rect("Buttons", body);
            TidyUIKit.Anchor(buttons, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(780f, 80f));
            TidyUIKit.Horizontal(buttons.gameObject, 20f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
            var menu = TidyUIKit.Button("Menu", buttons, "Menu", TidyButtonStyle.Secondary, new Vector2(200f, 72f));
            var retry = TidyUIKit.Button("Retry", buttons, "Retry", TidyButtonStyle.Secondary, new Vector2(220f, 72f));
            var next = TidyUIKit.Button("Next", buttons, "Next level", TidyButtonStyle.Primary, new Vector2(280f, 72f));
            var view = parent.gameObject.AddComponent<TidyResultsScreen>();
            TidySerialization.Set(view, "screen", screen);
            TidySerialization.Set(view, "titleLabel", title);
            TidySerialization.Set(view, "scoreLabel", score);
            TidySerialization.Set(view, "timeLabel", time);
            TidySerialization.Set(view, "accuracyLabel", accuracy);
            TidySerialization.Set(view, "comboLabel", combo);
            TidySerialization.Set(view, "newBestBadge", best.gameObject);
            TidySerialization.SetArray(view, "stars", stars);
            TidySerialization.Set(view, "breakdownContainer", breakdown);
            TidySerialization.Set(view, "breakdownLinePrefab", line);
            TidySerialization.Set(view, "nextButton", next);
            TidySerialization.Set(view, "retryButton", retry);
            TidySerialization.Set(view, "menuButton", menu);
        }

        static TextMeshProUGUI Stat(Transform parent, string caption)
        {
            var box = TidyUIKit.Image(caption, parent, TidyUISprites.RoundedSmall, new Color(1f, 1f, 1f, 0.05f));
            TidyUIKit.Layout(box, 86f, 220f);
            var captionLabel = TidyUIKit.Text("Caption", box.transform, caption.ToUpperInvariant(), 18f, TidyUIPalette.TextMuted, TextAlignmentOptions.Top, FontStyles.Bold);
            TidyUIKit.Anchor(captionLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(200f, 24f));
            var value = TidyUIKit.Text("Value", box.transform, "-", 34f, TidyUIPalette.TextPrimary, TextAlignmentOptions.Bottom, FontStyles.Bold);
            TidyUIKit.Anchor(value.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(200f, 44f));
            return value;
        }
    }
}
