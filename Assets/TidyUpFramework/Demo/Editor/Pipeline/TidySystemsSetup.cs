using TidyUpFramework.Input;
using TidyUpFramework.Settings;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidySystemsSetup
    {
        public static void ConfigureFeedback(TidyDemoContext context)
        {
            var feedback = context.Feedback;
            Effect(feedback, "pickUp", context.Cue("PickUp"), null, 0f, false);
            Effect(feedback, "drop", context.Cue("PlaceSoft"), TidyVfxGeneration.Dust, 0f, false);
            Effect(feedback, "placeCorrect", context.Cue("Correct"), TidyVfxGeneration.Sparkle, 0f, true);
            Effect(feedback, "placeIncorrect", context.Cue("PlaceWood"), TidyVfxGeneration.Dust, 0f, false);
            Effect(feedback, "rejected", context.Cue("Error"), null, 0f, false);
            Effect(feedback, "targetComplete", context.Cue("Combo"), TidyVfxGeneration.BigSparkle, 0.25f, false);
            Effect(feedback, "comboTier", context.Cue("Combo"), null, 0.12f, true);
            Effect(feedback, "objectiveComplete", context.Cue("Objective"), null, 0f, false);
            Effect(feedback, "levelComplete", context.Cue("LevelComplete"), TidyVfxGeneration.Confetti, 0.4f, false);
            Effect(feedback, "cleaned", context.Cue("Cleaned"), TidyVfxGeneration.Sparkle, 0f, true);
            Effect(feedback, "hint", context.Cue("Hint"), null, 0f, false);
            Effect(feedback, "rotate", context.Cue("Snap"), null, 0f, false);
            EditorUtility.SetDirty(feedback);
        }

        static void Effect(Object feedback, string field, Object sound, ParticleSystem particles, float punch, bool pitchFollowsCombo)
        {
            TidySerialization.Set(feedback, field + ".Sound", sound);
            TidySerialization.Set(feedback, field + ".Particles", particles);
            TidySerialization.SetFloat(feedback, field + ".CameraPunch", punch);
            TidySerialization.SetBool(feedback, field + ".PitchFollowsCombo", pitchFollowsCombo);
        }

        public static TidyFrameworkSettings ConfigureFrameworkSettings(TidyDemoContext context)
        {
            var settings = TidyAssetFactory.CreateOrLoad<TidyFrameworkSettings>(TidyDemoPaths.Resources + "/" + TidyFrameworkSettings.ResourcesPath + ".asset");
            settings.AssignCatalog(context.Catalog);
            settings.AssignInterfaceSounds(context.Cue("UiClick"), context.Cue("UiHover"), context.Cue("UiBack"), context.Cue("Music_Menu"));
            settings.AssignInputPrompts(AssetDatabase.LoadAssetAtPath<TidyInputPromptDefinition>(TidyDemoPaths.Settings + "/TidyUpInputPrompts.asset"));
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
