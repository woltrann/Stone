using System.Collections.Generic;
using TidyUpFramework.Core;
using TidyUpFramework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyScreenBuilders
    {
        const float RowHeight = 64f;

        static readonly (TidySettingKey key, string title)[] GraphicsRows =
        {
            (TidySettingKey.Quality, "Quality preset"),
            (TidySettingKey.Resolution, "Resolution"),
            (TidySettingKey.DisplayMode, "Display mode"),
            (TidySettingKey.VSync, "V-Sync"),
            (TidySettingKey.ShadowQuality, "Shadows"),
            (TidySettingKey.TextureQuality, "Texture quality"),
            (TidySettingKey.RenderScale, "Render scale")
        };

        static readonly (TidySettingKey key, string title)[] AudioRows =
        {
            (TidySettingKey.MasterVolume, "Master volume"),
            (TidySettingKey.MusicVolume, "Music volume"),
            (TidySettingKey.SfxVolume, "Effects volume"),
            (TidySettingKey.AmbienceVolume, "Ambience volume")
        };

        static readonly (TidySettingKey key, string title)[] GameplayRows =
        {
            (TidySettingKey.HintsEnabled, "Hints"),
            (TidySettingKey.TutorialEnabled, "Tutorials"),
            (TidySettingKey.PlacementPreview, "Placement preview"),
            (TidySettingKey.HeadBob, "Head bob"),
            (TidySettingKey.InteractionSensitivity, "Interaction assist")
        };

        static readonly (TidySettingKey key, string title)[] InputRows =
        {
            (TidySettingKey.InputScheme, "Input method"),
            (TidySettingKey.LookSensitivity, "Camera sensitivity"),
            (TidySettingKey.InvertY, "Invert look")
        };

        public static RectTransform Card(string cardName, Transform parent, Vector2 size, string title, out RectTransform body)
        {
            var panel = TidyUIKit.Panel(cardName, parent, TidyUIPalette.PanelBackground);
            TidyUIKit.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var header = TidyUIKit.Text("Title", panel.transform, title, 46f, TidyUIPalette.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
            TidyUIKit.Anchor(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(size.x - 80f, 64f));
            body = TidyUIKit.Rect("Body", panel.transform);
            TidyUIKit.Stretch(body);
            body.offsetMin = new Vector2(48f, 40f);
            body.offsetMax = new Vector2(-48f, -110f);
            return panel.rectTransform;
        }

        public static Button BackButton(Transform parent, TidyScreenStack stack)
        {
            var back = TidyUIKit.Button("Back", parent, "Back", TidyButtonStyle.Secondary, new Vector2(220f, 64f));
            TidyUIKit.Anchor((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(220f, 64f));
            TidySerialization.SetBool(back.GetComponent<TidyButtonFeedback>(), "isBackButton", true);
            if (stack != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(back.onClick, stack.Pop);
            }

            return back;
        }

        public static TidyUIScreen Settings(Transform parent, TidyScreenStack stack)
        {
            var screen = TidyUIKit.Screen("Settings Screen", parent, TidyScreenTransition.FadeScale, TidyControlChannel.All, true);
            var card = Card("Settings Card", screen.transform, new Vector2(1180f, 860f), "Settings", out var body);
            body.offsetMin = new Vector2(48f, 120f);
            var tabsBar = TidyUIKit.Rect("Tabs", body);
            TidyUIKit.Anchor(tabsBar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1000f, 64f));
            TidyUIKit.Horizontal(tabsBar.gameObject, 12f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
            var pages = TidyUIKit.Rect("Pages", body);
            TidyUIKit.Stretch(pages);
            pages.offsetMax = new Vector2(0f, -92f);
            var tabs = new List<TidyTab>
            {
                BuildSettingsTab("Graphics", tabsBar, pages, GraphicsRows),
                BuildSettingsTab("Audio", tabsBar, pages, AudioRows),
                BuildSettingsTab("Gameplay", tabsBar, pages, GameplayRows),
                BuildSettingsTab("Input", tabsBar, pages, InputRows)
            };
            var tabGroup = card.gameObject.AddComponent<TidyTabGroup>();
            tabGroup.Configure(tabs);
            var back = BackButton(card, stack);
            screen.FirstSelected = tabs[0].Button.gameObject;
            return screen;
        }

        static TidyTab BuildSettingsTab(string tabName, Transform tabsBar, Transform pages, (TidySettingKey key, string title)[] rows)
        {
            var button = TidyUIKit.Button(tabName + " Tab", tabsBar, tabName, TidyButtonStyle.Ghost, new Vector2(220f, 56f), 26f);
            var indicator = TidyUIKit.Image("Indicator", button.transform, TidyUISprites.RoundedSmall, new Color(1f, 1f, 1f, 0.25f));
            TidyUIKit.Anchor(indicator.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -6f), new Vector2(120f, 5f));
            var page = TidyUIKit.Rect(tabName + " Page", pages);
            TidyUIKit.Stretch(page);
            TidyUIKit.Vertical(page.gameObject, 10f, new RectOffset(20, 20, 10, 10));
            foreach (var row in rows)
            {
                SettingRow(page, row.key, row.title);
            }

            return new TidyTab { Button = button, Page = page.gameObject, Indicator = indicator };
        }

        public static TidySettingControl SettingRow(Transform parent, TidySettingKey key, string title)
        {
            var row = TidyUIKit.Image(title + " Row", parent, TidyUISprites.RoundedSmall, new Color(1f, 1f, 1f, 0.04f));
            TidyUIKit.Layout(row, RowHeight);
            var label = TidyUIKit.Text("Title", row.transform, title, 28f, TidyUIPalette.TextPrimary, TextAlignmentOptions.MidlineLeft);
            TidyUIKit.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(24f, 0f);
            label.rectTransform.offsetMax = new Vector2(-460f, 0f);
            var control = row.gameObject.AddComponent<TidySettingControl>();
            control.Configure(key, title);
            TidySerialization.Set(control, "titleLabel", label);
            var valueArea = TidyUIKit.Rect("Value", row.transform);
            TidyUIKit.Anchor(valueArea, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(440f, 52f));
            if (TidySettingsBinding.IsSlider(key))
            {
                var slider = TidyUIKit.Slider("Slider", valueArea, new Vector2(320f, 28f));
                TidyUIKit.Anchor((RectTransform)slider.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(320f, 28f));
                var value = TidyUIKit.Text("Value Label", valueArea, "100%", 24f, TidyUIPalette.TextSecondary, TextAlignmentOptions.MidlineRight);
                TidyUIKit.Anchor(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(100f, 40f));
                TidySerialization.Set(control, "slider", slider);
                TidySerialization.Set(control, "valueLabel", value);
            }
            else
            {
                var previous = TidyUIKit.IconButton("Previous", valueArea, TidyUISprites.Chevron, new Vector2(52f, 52f), TidyUIPalette.Accent);
                TidyUIKit.Anchor((RectTransform)previous.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
                previous.transform.Find("Icon").localScale = new Vector3(-1f, 1f, 1f);
                var next = TidyUIKit.IconButton("Next", valueArea, TidyUISprites.Chevron, new Vector2(52f, 52f), TidyUIPalette.Accent);
                TidyUIKit.Anchor((RectTransform)next.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
                var value = TidyUIKit.Text("Value Label", valueArea, "Value", 26f, TidyUIPalette.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
                TidyUIKit.Stretch(value.rectTransform);
                value.rectTransform.offsetMin = new Vector2(64f, 0f);
                value.rectTransform.offsetMax = new Vector2(-64f, 0f);
                TidySerialization.Set(control, "previousButton", previous);
                TidySerialization.Set(control, "nextButton", next);
                TidySerialization.Set(control, "valueLabel", value);
            }

            return control;
        }

        public static TidyUIScreen Controls(Transform parent, TidyScreenStack stack)
        {
            var screen = TidyUIKit.Screen("Controls Screen", parent, TidyScreenTransition.FadeScale, TidyControlChannel.All, true);
            var card = Card("Controls Card", screen.transform, new Vector2(1060f, 860f), "Controls", out var body);
            body.offsetMin = new Vector2(48f, 120f);
            var tabsBar = TidyUIKit.Rect("Schemes", body);
            TidyUIKit.Anchor(tabsBar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(860f, 60f));
            TidyUIKit.Horizontal(tabsBar.gameObject, 12f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
            var keyboard = TidyUIKit.Button("Keyboard", tabsBar, "Keyboard & Mouse", TidyButtonStyle.Ghost, new Vector2(280f, 54f), 24f);
            var gamepad = TidyUIKit.Button("Gamepad", tabsBar, "Gamepad", TidyButtonStyle.Ghost, new Vector2(220f, 54f), 24f);
            var touch = TidyUIKit.Button("Touch", tabsBar, "Touch", TidyButtonStyle.Ghost, new Vector2(200f, 54f), 24f);
            var list = TidyUIKit.Rect("Rows", body);
            TidyUIKit.Stretch(list);
            list.offsetMax = new Vector2(0f, -84f);
            TidyUIKit.Vertical(list.gameObject, 4f, new RectOffset(40, 40, 6, 6));
            var rowPrefab = TidyUIKit.Text("Row", list, "Action<pos=60%>Key", 28f, TidyUIPalette.TextPrimary, TextAlignmentOptions.Left);
            TidyUIKit.Layout(rowPrefab, 46f);
            var view = card.gameObject.AddComponent<TidyControlsView>();
            TidySerialization.Set(view, "rowPrefab", rowPrefab);
            TidySerialization.Set(view, "container", list);
            TidySerialization.Set(view, "keyboardButton", keyboard);
            TidySerialization.Set(view, "gamepadButton", gamepad);
            TidySerialization.Set(view, "touchButton", touch);
            BackButton(card, stack);
            screen.FirstSelected = keyboard.gameObject;
            return screen;
        }

        public static TidyUIScreen Credits(Transform parent, TidyScreenStack stack)
        {
            var screen = TidyUIKit.Screen("Credits Screen", parent, TidyScreenTransition.FadeScale, TidyControlChannel.All, true);
            var card = Card("Credits Card", screen.transform, new Vector2(960f, 720f), "Credits", out var body);
            var text = TidyUIKit.Text("Credits Text", body, CreditsText, 28f, TidyUIPalette.TextSecondary, TextAlignmentOptions.Top);
            TidyUIKit.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(0f, 80f);
            var back = BackButton(card, stack);
            screen.FirstSelected = back.gameObject;
            return screen;
        }

        const string CreditsText =
            "<b><color=#F2C46D>Tidy Up Framework</color></b>\n" +
            "Sorting · Organizing · Placement · Satisfying Interaction\n\n" +
            "<b>Design, code, models, textures and audio</b>\nCreated procedurally for this package\n\n" +
            "<b>Built with</b>\nUnity 6 · Universal Render Pipeline · Input System · TextMeshPro\n\n" +
            "<size=80%>Replace this text with your own team and licences.</size>";
    }
}
