using TidyUpFramework.Audio;
using TidyUpFramework.Levels;
using TidyUpFramework.Save;
using UnityEngine;
using UnityEngine.Audio;

namespace TidyUpFramework.Settings
{
    public enum TidySaveStorageKind
    {
        JsonFile,
        PlayerPrefs
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Framework Settings", fileName = "TidyUpFrameworkSettings", order = 60)]
    public sealed class TidyFrameworkSettings : ScriptableObject
    {
        public const string ResourcesPath = "TidyUpFrameworkSettings";

        [Header("Levels")]
        [SerializeField] TidyLevelCatalog levelCatalog;

        [Header("Save")]
        [SerializeField] TidySaveStorageKind saveStorage = TidySaveStorageKind.JsonFile;
        [SerializeField] string saveFolderName = "TidyUpSaves";
        [SerializeField] TidySettingsData defaultSettings = new TidySettingsData();

        [Header("Audio Mixer (optional)")]
        [SerializeField] AudioMixer audioMixer;
        [SerializeField] AudioMixerGroup sfxGroup;
        [SerializeField] AudioMixerGroup musicGroup;
        [SerializeField] AudioMixerGroup uiGroup;
        [SerializeField] AudioMixerGroup ambienceGroup;
        [SerializeField] string masterVolumeParameter = "MasterVolume";
        [SerializeField] string sfxVolumeParameter = "SfxVolume";
        [SerializeField] string musicVolumeParameter = "MusicVolume";
        [SerializeField] string uiVolumeParameter = "UiVolume";
        [SerializeField] string ambienceVolumeParameter = "AmbienceVolume";

        [Header("Interface Sounds")]
        [SerializeField] TidyAudioCue uiClickCue;
        [SerializeField] TidyAudioCue uiHoverCue;
        [SerializeField] TidyAudioCue uiBackCue;
        [SerializeField] TidyAudioCue menuMusic;

        [Header("Input")]
        [SerializeField] Input.TidyInputPromptDefinition inputPrompts;

        [Header("Scene Transitions")]
        [SerializeField, Min(0f)] float fadeSeconds = 0.45f;
        [SerializeField] Color fadeColor = new Color(0.07f, 0.06f, 0.08f, 1f);

        public TidyLevelCatalog LevelCatalog => levelCatalog;
        public TidySaveStorageKind SaveStorage => saveStorage;
        public string SaveFolderName => saveFolderName;
        public TidySettingsData DefaultSettings => defaultSettings;
        public AudioMixer AudioMixer => audioMixer;
        public AudioMixerGroup SfxGroup => sfxGroup;
        public AudioMixerGroup MusicGroup => musicGroup;
        public AudioMixerGroup UiGroup => uiGroup;
        public AudioMixerGroup AmbienceGroup => ambienceGroup;
        public string MasterVolumeParameter => masterVolumeParameter;
        public string SfxVolumeParameter => sfxVolumeParameter;
        public string MusicVolumeParameter => musicVolumeParameter;
        public string UiVolumeParameter => uiVolumeParameter;
        public string AmbienceVolumeParameter => ambienceVolumeParameter;
        public TidyAudioCue UiClickCue => uiClickCue;
        public TidyAudioCue UiHoverCue => uiHoverCue;
        public TidyAudioCue UiBackCue => uiBackCue;
        public TidyAudioCue MenuMusic => menuMusic;
        public Input.TidyInputPromptDefinition InputPrompts => inputPrompts;
        public float FadeSeconds => fadeSeconds;
        public Color FadeColor => fadeColor;

        public void AssignCatalog(TidyLevelCatalog catalog)
        {
            levelCatalog = catalog;
        }

        public void AssignInterfaceSounds(TidyAudioCue click, TidyAudioCue hover, TidyAudioCue back, TidyAudioCue music)
        {
            uiClickCue = click;
            uiHoverCue = hover;
            uiBackCue = back;
            menuMusic = music;
        }

        public void AssignInputPrompts(Input.TidyInputPromptDefinition prompts)
        {
            inputPrompts = prompts;
        }

        public void AssignMixer(AudioMixer mixer, AudioMixerGroup sfx, AudioMixerGroup music, AudioMixerGroup ui, AudioMixerGroup ambience)
        {
            audioMixer = mixer;
            sfxGroup = sfx;
            musicGroup = music;
            uiGroup = ui;
            ambienceGroup = ambience;
        }
    }
}
