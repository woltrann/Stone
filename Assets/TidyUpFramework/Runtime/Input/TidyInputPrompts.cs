using System.Collections.Generic;
using System.Text;
using TidyUpFramework.Core;
using UnityEngine.InputSystem;

namespace TidyUpFramework.Input
{
    public static class TidyInputPrompts
    {
        const string DefaultKeyTemplate = "<b>[{0}]</b>";
        const string KeyboardMouseGroup = "KeyboardMouse";
        const string GamepadGroup = "Gamepad";

        static readonly Dictionary<TidyInputAction, string> DefaultTouchLabels = new Dictionary<TidyInputAction, string>
        {
            { TidyInputAction.Move, "Joystick" },
            { TidyInputAction.Look, "Drag" },
            { TidyInputAction.Interact, "Tap" },
            { TidyInputAction.Drop, "Drop" },
            { TidyInputAction.Throw, "Throw" },
            { TidyInputAction.Rotate, "Rotate" },
            { TidyInputAction.Inspect, "Inspect" },
            { TidyInputAction.Sprint, "Run" },
            { TidyInputAction.Crouch, "Crouch" },
            { TidyInputAction.Hint, "Hint" },
            { TidyInputAction.Pause, "Pause" }
        };

        static readonly StringBuilder Builder = new StringBuilder(128);

        public static TidyInputPromptDefinition Definition { get; set; }

        public static TidyInputScheme CurrentScheme => TidyServices.TryGet<ITidyPlayerInput>(out var input) ? input.ActiveScheme : TidyInputScheme.KeyboardMouse;

        public static string Label(TidyInputAction action)
        {
            return Label(action, CurrentScheme);
        }

        public static string Label(TidyInputAction action, TidyInputScheme scheme)
        {
            var entry = Definition != null ? Definition.Find(action) : null;
            switch (scheme)
            {
                case TidyInputScheme.Touch:
                    if (entry != null && !string.IsNullOrEmpty(entry.TouchLabel))
                    {
                        return entry.TouchLabel;
                    }

                    return DefaultTouchLabels.TryGetValue(action, out var touchLabel) ? touchLabel : action.ToString();
                case TidyInputScheme.Gamepad:
                    if (entry != null && !string.IsNullOrEmpty(entry.GamepadLabel))
                    {
                        return entry.GamepadLabel;
                    }

                    return BindingDisplay(action, GamepadGroup);
                default:
                    if (entry != null && !string.IsNullOrEmpty(entry.KeyboardMouseLabel))
                    {
                        return entry.KeyboardMouseLabel;
                    }

                    return BindingDisplay(action, KeyboardMouseGroup);
            }
        }

        public static string Key(TidyInputAction action)
        {
            var template = Definition != null && !string.IsNullOrEmpty(Definition.KeyTemplate) ? Definition.KeyTemplate : DefaultKeyTemplate;
            return string.Format(template, Label(action));
        }

        public static string Format(string template)
        {
            if (string.IsNullOrEmpty(template) || template.IndexOf('{') < 0)
            {
                return template;
            }

            Builder.Clear();
            Builder.Append(template);
            foreach (TidyInputAction action in System.Enum.GetValues(typeof(TidyInputAction)))
            {
                var token = "{" + action + "}";
                if (template.Contains(token))
                {
                    Builder.Replace(token, Key(action));
                }
            }

            return Builder.ToString();
        }

        static string BindingDisplay(TidyInputAction action, string group)
        {
            if (!TidyServices.TryGet<TidyInputReader>(out var reader))
            {
                return action.ToString();
            }

            var inputAction = reader.FindAction(action);
            if (inputAction == null)
            {
                return action.ToString();
            }

            var display = inputAction.GetBindingDisplayString(InputBinding.MaskByGroup(group), InputBinding.DisplayStringOptions.DontIncludeInteractions);
            if (string.IsNullOrEmpty(display))
            {
                return action.ToString();
            }

            var separator = display.IndexOf(" | ", System.StringComparison.Ordinal);
            return separator > 0 ? display.Substring(0, separator) : display;
        }
    }
}
