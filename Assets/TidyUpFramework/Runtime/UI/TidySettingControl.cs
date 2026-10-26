using TidyUpFramework.Save;
using TidyUpFramework.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Setting Control")]
    public sealed class TidySettingControl : MonoBehaviour
    {
        [SerializeField] TidySettingKey key;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] string title;

        [Header("Selector")]
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [SerializeField] TMP_Text valueLabel;

        [Header("Slider")]
        [SerializeField] Slider slider;

        public TidySettingKey Key => key;

        public void Configure(TidySettingKey newKey, string newTitle)
        {
            key = newKey;
            title = newTitle;
        }

        void Start()
        {
            if (titleLabel != null && !string.IsNullOrEmpty(title))
            {
                titleLabel.text = title;
            }

            if (previousButton != null)
            {
                previousButton.onClick.AddListener(() => Step(-1));
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(() => Step(1));
            }

            if (slider != null)
            {
                var range = TidySettingsBinding.SliderRange(key);
                slider.minValue = range.x;
                slider.maxValue = range.y;
                slider.onValueChanged.AddListener(HandleSliderChanged);
            }

            if (TidyFramework.SettingsService != null)
            {
                TidyFramework.SettingsService.Changed += Refresh;
            }

            Refresh(Current);
        }

        void OnEnable()
        {
            if (TidyFramework.SettingsService != null)
            {
                Refresh(Current);
            }
        }

        void OnDestroy()
        {
            if (TidyFramework.SettingsService != null)
            {
                TidyFramework.SettingsService.Changed -= Refresh;
            }
        }

        static TidySettingsData Current => TidyFramework.SettingsService != null ? TidyFramework.SettingsService.Current : new TidySettingsData();

        void Step(int direction)
        {
            var options = TidySettingsBinding.GetOptions(key);
            if (options.Count == 0 || TidyFramework.SettingsService == null)
            {
                return;
            }

            var index = TidySettingsBinding.GetIndex(key, Current);
            var next = (index + direction + options.Count) % options.Count;
            TidyFramework.SettingsService.ApplyAndSave(data => TidySettingsBinding.SetIndex(key, data, next));
        }

        void HandleSliderChanged(float value)
        {
            if (TidyFramework.SettingsService == null || Mathf.Approximately(TidySettingsBinding.GetFloat(key, Current), value))
            {
                return;
            }

            TidyFramework.SettingsService.ApplyAndSave(data => TidySettingsBinding.SetFloat(key, data, value));
        }

        void Refresh(TidySettingsData data)
        {
            if (TidySettingsBinding.IsSlider(key))
            {
                var value = TidySettingsBinding.GetFloat(key, data);
                if (slider != null)
                {
                    slider.SetValueWithoutNotify(value);
                }

                if (valueLabel != null)
                {
                    valueLabel.text = TidySettingsBinding.FormatSlider(key, value);
                }

                return;
            }

            var options = TidySettingsBinding.GetOptions(key);
            var index = Mathf.Clamp(TidySettingsBinding.GetIndex(key, data), 0, Mathf.Max(0, options.Count - 1));
            if (valueLabel != null)
            {
                valueLabel.text = options.Count > 0 ? options[index] : string.Empty;
            }
        }
    }
}
