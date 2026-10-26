using TidyUpFramework.Core;
using TidyUpFramework.Input;
using TMPro;
using UnityEngine;

namespace TidyUpFramework.UI
{
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Tidy Up Framework/UI/Prompt Label")]
    public sealed class TidyPromptLabel : MonoBehaviour
    {
        [SerializeField, TextArea, Tooltip("Text with action tokens such as {Interact} or {Drop}. Tokens are replaced with the current device's key.")]
        string template = "{Interact} Interact";

        TMP_Text label;
        ITidyPlayerInput input;

        public string Template
        {
            get => template;
            set
            {
                template = value;
                Refresh();
            }
        }

        void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            TidyServices.ServiceChanged += HandleServiceChanged;
            Bind();
            Refresh();
        }

        void OnDisable()
        {
            TidyServices.ServiceChanged -= HandleServiceChanged;
            if (input != null)
            {
                input.SchemeChanged -= HandleSchemeChanged;
            }

            input = null;
        }

        void HandleServiceChanged(System.Type type)
        {
            if (type == typeof(ITidyPlayerInput))
            {
                Bind();
                Refresh();
            }
        }

        void Bind()
        {
            if (input != null)
            {
                input.SchemeChanged -= HandleSchemeChanged;
            }

            if (TidyServices.TryGet(out input))
            {
                input.SchemeChanged += HandleSchemeChanged;
            }
        }

        void HandleSchemeChanged(TidyInputScheme scheme)
        {
            Refresh();
        }

        void Refresh()
        {
            if (label != null)
            {
                label.text = TidyInputPrompts.Format(template);
            }
        }
    }
}
