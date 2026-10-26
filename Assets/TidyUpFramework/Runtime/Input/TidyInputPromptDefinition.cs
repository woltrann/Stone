using System;
using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Input
{
    [Serializable]
    public sealed class TidyInputPromptEntry
    {
        public TidyInputAction Action;
        [Tooltip("Leave empty to use the binding display name from the Input Action asset.")]
        public string KeyboardMouseLabel;
        [Tooltip("Leave empty to use the binding display name from the Input Action asset.")]
        public string GamepadLabel;
        public string TouchLabel;
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Input Prompt Definition", fileName = "InputPrompts", order = 61)]
    public sealed class TidyInputPromptDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Rich text template wrapped around every key label. {0} is the label.")]
        string keyTemplate = "<b><color=#FFE3A3>[{0}]</color></b>";

        [SerializeField]
        List<TidyInputPromptEntry> entries = new List<TidyInputPromptEntry>();

        public string KeyTemplate => keyTemplate;

        public TidyInputPromptEntry Find(TidyInputAction action)
        {
            foreach (var entry in entries)
            {
                if (entry != null && entry.Action == action)
                {
                    return entry;
                }
            }

            return null;
        }

        public void Configure(string template, IEnumerable<TidyInputPromptEntry> newEntries)
        {
            keyTemplate = template;
            entries = new List<TidyInputPromptEntry>(newEntries);
        }
    }
}
