using System.Text;
using TidyUpFramework.Core;
using TidyUpFramework.Interaction;
using TidyUpFramework.Objects;
using TMPro;
using UnityEngine;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Inspect Panel View")]
    public sealed class TidyInspectPanelView : MonoBehaviour
    {
        [SerializeField] TidyUIScreen screen;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text categoryLabel;
        [SerializeField] TMP_Text tagsLabel;
        [SerializeField] TMP_Text descriptionLabel;

        readonly StringBuilder builder = new StringBuilder(64);
        TidyInteractor interactor;

        void Start()
        {
            if (TidyServices.TryGet(out interactor))
            {
                interactor.InspectChanged += HandleInspectChanged;
            }
        }

        void OnDestroy()
        {
            if (interactor != null)
            {
                interactor.InspectChanged -= HandleInspectChanged;
            }
        }

        void HandleInspectChanged(TidyObject tidyObject)
        {
            if (tidyObject == null)
            {
                screen?.Hide();
                return;
            }

            if (nameLabel != null)
            {
                nameLabel.text = tidyObject.DisplayName;
            }

            if (categoryLabel != null)
            {
                categoryLabel.text = tidyObject.Category != null ? tidyObject.Category.DisplayName : string.Empty;
            }

            if (tagsLabel != null)
            {
                builder.Clear();
                foreach (var tag in tidyObject.Tags)
                {
                    if (tag == null)
                    {
                        continue;
                    }

                    builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(tag.DisplayColor)).Append("><size=160%>·</size></color> ").Append(tag.DisplayName).Append("   ");
                }

                tagsLabel.text = builder.ToString();
            }

            if (descriptionLabel != null)
            {
                var definition = tidyObject.Definition;
                descriptionLabel.text = definition != null ? (string.IsNullOrEmpty(definition.InspectText) ? definition.Description : definition.InspectText) : string.Empty;
            }

            screen?.Show();
        }
    }
}
