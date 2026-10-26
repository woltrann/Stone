using System.Collections.Generic;
using TidyUpFramework.Save;
using TidyUpFramework.Settings;
using UnityEngine;

namespace TidyUpFramework.UI
{
    public static class TidySettingsBinding
    {
        static readonly string[] DisplayModes = { "Exclusive Fullscreen", "Borderless", "Maximized Window", "Windowed" };
        static readonly string[] ShadowLevels = { "Off", "Low", "Medium", "High" };
        static readonly string[] TextureLevels = { "Full", "Half", "Quarter", "Eighth" };
        static readonly string[] OnOff = { "Off", "On" };
        static readonly string[] Schemes = { "Automatic", "Keyboard & Mouse", "Gamepad", "Touch" };

        public static bool IsSlider(TidySettingKey key)
        {
            switch (key)
            {
                case TidySettingKey.MasterVolume:
                case TidySettingKey.MusicVolume:
                case TidySettingKey.SfxVolume:
                case TidySettingKey.AmbienceVolume:
                case TidySettingKey.LookSensitivity:
                case TidySettingKey.InteractionSensitivity:
                case TidySettingKey.RenderScale:
                    return true;
                default:
                    return false;
            }
        }

        public static Vector2 SliderRange(TidySettingKey key)
        {
            switch (key)
            {
                case TidySettingKey.LookSensitivity:
                case TidySettingKey.InteractionSensitivity:
                    return new Vector2(0.2f, 3f);
                case TidySettingKey.RenderScale:
                    return new Vector2(0.5f, 1f);
                default:
                    return new Vector2(0f, 1f);
            }
        }

        public static string FormatSlider(TidySettingKey key, float value)
        {
            switch (key)
            {
                case TidySettingKey.LookSensitivity:
                case TidySettingKey.InteractionSensitivity:
                    return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "x";
                default:
                    return Mathf.RoundToInt(value * 100f) + "%";
            }
        }

        public static float GetFloat(TidySettingKey key, TidySettingsData data)
        {
            switch (key)
            {
                case TidySettingKey.MasterVolume:
                    return data.MasterVolume;
                case TidySettingKey.MusicVolume:
                    return data.MusicVolume;
                case TidySettingKey.SfxVolume:
                    return data.SfxVolume;
                case TidySettingKey.AmbienceVolume:
                    return data.AmbienceVolume;
                case TidySettingKey.LookSensitivity:
                    return data.LookSensitivity;
                case TidySettingKey.InteractionSensitivity:
                    return data.InteractionSensitivity;
                case TidySettingKey.RenderScale:
                    return data.RenderScale;
                default:
                    return 0f;
            }
        }

        public static void SetFloat(TidySettingKey key, TidySettingsData data, float value)
        {
            switch (key)
            {
                case TidySettingKey.MasterVolume:
                    data.MasterVolume = value;
                    break;
                case TidySettingKey.MusicVolume:
                    data.MusicVolume = value;
                    break;
                case TidySettingKey.SfxVolume:
                    data.SfxVolume = value;
                    data.UiVolume = value;
                    break;
                case TidySettingKey.AmbienceVolume:
                    data.AmbienceVolume = value;
                    break;
                case TidySettingKey.LookSensitivity:
                    data.LookSensitivity = value;
                    break;
                case TidySettingKey.InteractionSensitivity:
                    data.InteractionSensitivity = value;
                    break;
                case TidySettingKey.RenderScale:
                    data.RenderScale = value;
                    break;
            }
        }

        public static IReadOnlyList<string> GetOptions(TidySettingKey key)
        {
            switch (key)
            {
                case TidySettingKey.Quality:
                    return QualitySettings.names;
                case TidySettingKey.Resolution:
                    return ResolutionNames();
                case TidySettingKey.DisplayMode:
                    return DisplayModes;
                case TidySettingKey.ShadowQuality:
                    return ShadowLevels;
                case TidySettingKey.TextureQuality:
                    return TextureLevels;
                case TidySettingKey.InputScheme:
                    return Schemes;
                default:
                    return OnOff;
            }
        }

        public static int GetIndex(TidySettingKey key, TidySettingsData data)
        {
            switch (key)
            {
                case TidySettingKey.Quality:
                    return data.QualityLevel >= 0 ? data.QualityLevel : QualitySettings.GetQualityLevel();
                case TidySettingKey.Resolution:
                    return ResolutionIndex(data);
                case TidySettingKey.DisplayMode:
                    return Mathf.Clamp(data.FullScreenMode, 0, DisplayModes.Length - 1);
                case TidySettingKey.ShadowQuality:
                    return Mathf.Clamp(data.ShadowQuality, 0, ShadowLevels.Length - 1);
                case TidySettingKey.TextureQuality:
                    return Mathf.Clamp(data.TextureQuality, 0, TextureLevels.Length - 1);
                case TidySettingKey.VSync:
                    return data.VSync ? 1 : 0;
                case TidySettingKey.InvertY:
                    return data.InvertY ? 1 : 0;
                case TidySettingKey.HintsEnabled:
                    return data.HintsEnabled ? 1 : 0;
                case TidySettingKey.TutorialEnabled:
                    return data.TutorialEnabled ? 1 : 0;
                case TidySettingKey.HeadBob:
                    return data.HeadBob ? 1 : 0;
                case TidySettingKey.PlacementPreview:
                    return data.ShowPlacementPreview ? 1 : 0;
                case TidySettingKey.InputScheme:
                    return data.PreferredScheme + 1;
                default:
                    return 0;
            }
        }

        public static void SetIndex(TidySettingKey key, TidySettingsData data, int index)
        {
            var on = index > 0;
            switch (key)
            {
                case TidySettingKey.Quality:
                    data.QualityLevel = index;
                    break;
                case TidySettingKey.Resolution:
                    ApplyResolution(data, index);
                    break;
                case TidySettingKey.DisplayMode:
                    data.FullScreenMode = index;
                    break;
                case TidySettingKey.ShadowQuality:
                    data.ShadowQuality = index;
                    break;
                case TidySettingKey.TextureQuality:
                    data.TextureQuality = index;
                    break;
                case TidySettingKey.VSync:
                    data.VSync = on;
                    break;
                case TidySettingKey.InvertY:
                    data.InvertY = on;
                    break;
                case TidySettingKey.HintsEnabled:
                    data.HintsEnabled = on;
                    break;
                case TidySettingKey.TutorialEnabled:
                    data.TutorialEnabled = on;
                    break;
                case TidySettingKey.HeadBob:
                    data.HeadBob = on;
                    break;
                case TidySettingKey.PlacementPreview:
                    data.ShowPlacementPreview = on;
                    break;
                case TidySettingKey.InputScheme:
                    data.PreferredScheme = index - 1;
                    break;
            }
        }

        static string[] ResolutionNames()
        {
            var service = TidyFramework.SettingsService;
            if (service == null || service.Resolutions.Count == 0)
            {
                return new[] { Screen.width + " x " + Screen.height };
            }

            var names = new string[service.Resolutions.Count];
            for (var index = 0; index < names.Length; index++)
            {
                names[index] = service.Resolutions[index].width + " x " + service.Resolutions[index].height;
            }

            return names;
        }

        static int ResolutionIndex(TidySettingsData data)
        {
            var service = TidyFramework.SettingsService;
            if (service == null || service.Resolutions.Count == 0)
            {
                return 0;
            }

            var width = data.ResolutionWidth > 0 ? data.ResolutionWidth : Screen.width;
            var height = data.ResolutionHeight > 0 ? data.ResolutionHeight : Screen.height;
            return Mathf.Max(0, service.FindResolutionIndex(width, height));
        }

        static void ApplyResolution(TidySettingsData data, int index)
        {
            var service = TidyFramework.SettingsService;
            if (service == null || index < 0 || index >= service.Resolutions.Count)
            {
                return;
            }

            data.ResolutionWidth = service.Resolutions[index].width;
            data.ResolutionHeight = service.Resolutions[index].height;
        }
    }
}
