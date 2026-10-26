using System.Collections.Generic;
using TidyUpFramework.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Controls View")]
    public sealed class TidyControlsView : MonoBehaviour
    {
        static readonly (TidyInputAction action, string description)[] Rows =
        {
            (TidyInputAction.Move, "Move"),
            (TidyInputAction.Look, "Look around"),
            (TidyInputAction.Interact, "Pick up / Stack / Place / Clean"),
            (TidyInputAction.Drop, "Drop"),
            (TidyInputAction.Throw, "Toss"),
            (TidyInputAction.Rotate, "Rotate held object"),
            (TidyInputAction.Inspect, "Inspect"),
            (TidyInputAction.Sprint, "Hurry"),
            (TidyInputAction.Crouch, "Crouch"),
            (TidyInputAction.Hint, "Hint"),
            (TidyInputAction.Pause, "Pause")
        };

        [SerializeField] TMP_Text rowPrefab;
        [SerializeField] RectTransform container;
        [SerializeField] Button keyboardButton;
        [SerializeField] Button gamepadButton;
        [SerializeField] Button touchButton;

        [Header("Layout")]
        [SerializeField, Min(10f), Tooltip("Fixed height of every row so the list never shifts between schemes.")] float rowHeight = 42f;
        [SerializeField, Range(0.3f, 0.8f), Tooltip("Horizontal position of the key column.")] float keyColumn = 0.6f;
        [SerializeField, Tooltip("Gives the scheme tabs the same width so they line up.")] bool equalTabWidths = true;
        [SerializeField] Color activeTabColor = new Color(1f, 0.86f, 0.55f, 1f);
        [SerializeField] Color inactiveTabColor = new Color(1f, 1f, 1f, 0.55f);

        readonly List<TMP_Text> rows = new List<TMP_Text>();

        void Start()
        {
            if (keyboardButton != null)
            {
                keyboardButton.onClick.AddListener(() => Show(TidyInputScheme.KeyboardMouse));
            }

            if (gamepadButton != null)
            {
                gamepadButton.onClick.AddListener(() => Show(TidyInputScheme.Gamepad));
            }

            if (touchButton != null)
            {
                touchButton.onClick.AddListener(() => Show(TidyInputScheme.Touch));
            }

            if (equalTabWidths)
            {
                AlignTabs();
            }

            Show(TidyInputPrompts.CurrentScheme);
        }

        public void Show(TidyInputScheme scheme)
        {
            if (rowPrefab == null)
            {
                return;
            }

            rowPrefab.gameObject.SetActive(false);
            var parent = container != null ? container : (RectTransform)transform;
            while (rows.Count < Rows.Length)
            {
                var row = Instantiate(rowPrefab, parent);
                row.gameObject.SetActive(true);
                ConfigureRow(row);
                rows.Add(row);
            }

            var keyPosition = Mathf.RoundToInt(keyColumn * 100f);
            for (var index = 0; index < Rows.Length; index++)
            {
                var label = TidyInputPrompts.Label(Rows[index].action, scheme);
                rows[index].text = Rows[index].description + "<pos=" + keyPosition + "%><b><color=#FFE3A3>" + label + "</color></b>";
            }

            HighlightTab(keyboardButton, scheme == TidyInputScheme.KeyboardMouse);
            HighlightTab(gamepadButton, scheme == TidyInputScheme.Gamepad);
            HighlightTab(touchButton, scheme == TidyInputScheme.Touch);
        }

        void ConfigureRow(TMP_Text row)
        {
            row.textWrappingMode = TextWrappingModes.NoWrap;
            row.overflowMode = TextOverflowModes.Ellipsis;
            row.alignment = TextAlignmentOptions.Left;
            var layout = row.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = row.gameObject.AddComponent<LayoutElement>();
            }

            layout.minHeight = rowHeight;
            layout.preferredHeight = rowHeight;
            layout.flexibleHeight = 0f;
        }

        void AlignTabs()
        {
            var width = 0f;
            foreach (var button in new[] { keyboardButton, gamepadButton, touchButton })
            {
                if (button == null)
                {
                    continue;
                }

                var existing = button.GetComponent<LayoutElement>();
                var preferred = existing != null && existing.preferredWidth > 0f ? existing.preferredWidth : ((RectTransform)button.transform).sizeDelta.x;
                width = Mathf.Max(width, preferred);
            }

            if (width <= 0f)
            {
                return;
            }

            var bar = keyboardButton != null ? keyboardButton.transform.parent as RectTransform : null;
            var group = bar != null ? bar.GetComponent<HorizontalLayoutGroup>() : null;
            if (bar != null && bar.rect.width > 0f)
            {
                var spacing = group != null ? group.spacing : 0f;
                width = Mathf.Min(width, (bar.rect.width - spacing * 2f) / 3f);
            }

            foreach (var button in new[] { keyboardButton, gamepadButton, touchButton })
            {
                if (button == null)
                {
                    continue;
                }

                var layout = button.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = button.gameObject.AddComponent<LayoutElement>();
                }

                layout.minWidth = width;
                layout.preferredWidth = width;
                ((RectTransform)button.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }
        }

        void HighlightTab(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = active ? activeTabColor : inactiveTabColor;
            }

            if (button.targetGraphic != null)
            {
                var color = button.targetGraphic.color;
                color.a = active ? 1f : 0.6f;
                button.targetGraphic.color = color;
            }
        }
    }
}
