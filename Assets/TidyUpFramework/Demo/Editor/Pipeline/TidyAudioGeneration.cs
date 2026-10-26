using System;
using TidyUpFramework.Audio;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyAudioGeneration
    {
        const int SampleRate = 44100;
        const float MusicSeconds = 48f;
        const float AmbienceSeconds = 30f;

        public static void Build(TidyDemoContext context)
        {
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.DefaultAudio);
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.AudioCues);
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.Audio);
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.DemoAudioCues);

            Cue(context, "PickUp", TidyAudioChannel.Sfx, 0.55f, new Vector2(0.95f, 1.08f), 0.7f, TidyAudioSynth.PickUpPop);
            Cue(context, "PlaceWood", TidyAudioChannel.Sfx, 0.7f, new Vector2(0.92f, 1.06f), 0.8f, TidyAudioSynth.PlaceWoodThunk);
            Cue(context, "PlaceSoft", TidyAudioChannel.Sfx, 0.7f, new Vector2(0.92f, 1.08f), 0.8f, TidyAudioSynth.PlaceSoftThud);
            Cue(context, "PlaceGlass", TidyAudioChannel.Sfx, 0.6f, new Vector2(0.95f, 1.1f), 0.8f, TidyAudioSynth.PlaceGlassClink);
            Cue(context, "Paper", TidyAudioChannel.Sfx, 0.5f, new Vector2(0.9f, 1.1f), 0.7f, TidyAudioSynth.PaperRustle);
            Cue(context, "Snap", TidyAudioChannel.Sfx, 0.55f, new Vector2(0.96f, 1.04f), 0.6f, TidyAudioSynth.SnapClick);
            Cue(context, "Correct", TidyAudioChannel.Sfx, 0.45f, new Vector2(1f, 1f), 0.2f, () => TidyAudioSynth.CorrectChime(0f));
            Cue(context, "Error", TidyAudioChannel.Sfx, 0.45f, new Vector2(0.98f, 1.02f), 0.2f, TidyAudioSynth.GentleError);
            Cue(context, "Combo", TidyAudioChannel.Sfx, 0.5f, new Vector2(1f, 1f), 0f, () => TidyAudioSynth.ComboSparkle(2));
            Cue(context, "Objective", TidyAudioChannel.Sfx, 0.55f, Vector2.one, 0f, TidyAudioSynth.ObjectiveFanfare);
            Cue(context, "LevelComplete", TidyAudioChannel.Sfx, 0.6f, Vector2.one, 0f, TidyAudioSynth.LevelCompleteFanfare);
            Cue(context, "Star", TidyAudioChannel.UI, 0.55f, Vector2.one, 0f, TidyAudioSynth.StarPing);
            Cue(context, "UiClick", TidyAudioChannel.UI, 0.45f, new Vector2(0.97f, 1.03f), 0f, TidyAudioSynth.UiClick);
            Cue(context, "UiHover", TidyAudioChannel.UI, 0.25f, new Vector2(0.97f, 1.05f), 0f, TidyAudioSynth.UiHover);
            Cue(context, "UiBack", TidyAudioChannel.UI, 0.4f, Vector2.one, 0f, TidyAudioSynth.UiBack);
            Cue(context, "Whoosh", TidyAudioChannel.Sfx, 0.4f, new Vector2(0.9f, 1.1f), 0.5f, TidyAudioSynth.Whoosh);
            Cue(context, "Hint", TidyAudioChannel.Sfx, 0.45f, Vector2.one, 0f, () => TidyAudioSynth.CorrectChime(7f));
            Cue(context, "Cleaned", TidyAudioChannel.Sfx, 0.5f, new Vector2(1f, 1.05f), 0.5f, () => TidyAudioSynth.ComboSparkle(1));
            Loop(context, "Scrub", TidyAudioChannel.Sfx, 0.4f, TidyDemoPaths.DefaultAudio, TidyDemoPaths.AudioCues, TidyAudioSynth.ScrubLoop);
            Loop(context, "Vacuum", TidyAudioChannel.Sfx, 0.35f, TidyDemoPaths.DefaultAudio, TidyDemoPaths.AudioCues, TidyAudioSynth.VacuumLoop);

            Loop(context, "Music_Menu", TidyAudioChannel.Music, 0.55f, TidyDemoPaths.Audio, TidyDemoPaths.DemoAudioCues, () => TidyAudioSynth.MenuMusicLoop(MusicSeconds));
            Loop(context, "Music_Library", TidyAudioChannel.Music, 0.5f, TidyDemoPaths.Audio, TidyDemoPaths.DemoAudioCues, () => TidyAudioSynth.CalmMusicLoop(MusicSeconds, 11, 0));
            Loop(context, "Music_Store", TidyAudioChannel.Music, 0.5f, TidyDemoPaths.Audio, TidyDemoPaths.DemoAudioCues, () => TidyAudioSynth.CalmMusicLoop(MusicSeconds, 23, 1));
            Loop(context, "Music_Warehouse", TidyAudioChannel.Music, 0.5f, TidyDemoPaths.Audio, TidyDemoPaths.DemoAudioCues, () => TidyAudioSynth.CalmMusicLoop(MusicSeconds, 37, 2));
            Loop(context, "Music_Room", TidyAudioChannel.Music, 0.5f, TidyDemoPaths.Audio, TidyDemoPaths.DemoAudioCues, () => TidyAudioSynth.CalmMusicLoop(MusicSeconds, 41, 3));
            Loop(context, "Ambience_Library", TidyAudioChannel.Ambience, 0.5f, TidyDemoPaths.Audio, TidyDemoPaths.DemoAudioCues, () => TidyAudioSynth.LibraryAmbienceLoop(AmbienceSeconds));
            Loop(context, "Ambience_Store", TidyAudioChannel.Ambience, 0.45f, TidyDemoPaths.Audio, TidyDemoPaths.DemoAudioCues, () => TidyAudioSynth.StoreAmbienceLoop(AmbienceSeconds));
            Loop(context, "Ambience_Room", TidyAudioChannel.Ambience, 0.45f, TidyDemoPaths.Audio, TidyDemoPaths.DemoAudioCues, () => TidyAudioSynth.RoomToneLoop(AmbienceSeconds));
            AssetDatabase.SaveAssets();
        }

        static void Cue(TidyDemoContext context, string key, TidyAudioChannel channel, float volume, Vector2 pitch, float spatial, Func<float[]> synthesize)
        {
            var clip = TidyWavWriter.SaveWav(synthesize(), SampleRate, TidyDemoPaths.DefaultAudio + "/SFX_" + key + ".wav");
            var cue = TidyAssetFactory.CreateOrLoad<TidyAudioCue>(TidyDemoPaths.AudioCues + "/Cue_" + key + ".asset");
            cue.Configure(new[] { clip }, channel, volume, pitch, spatial, false);
            EditorUtility.SetDirty(cue);
            context.AddCue(key, cue);
        }

        static void Loop(TidyDemoContext context, string key, TidyAudioChannel channel, float volume, string clipFolder, string cueFolder, Func<float[]> synthesize)
        {
            var clip = TidyWavWriter.SaveWav(synthesize(), SampleRate, clipFolder + "/" + key + ".wav", true, channel == TidyAudioChannel.Music || channel == TidyAudioChannel.Ambience);
            var cue = TidyAssetFactory.CreateOrLoad<TidyAudioCue>(cueFolder + "/Cue_" + key + ".asset");
            cue.Configure(new[] { clip }, channel, volume, Vector2.one, 0f, true);
            EditorUtility.SetDirty(cue);
            context.AddCue(key, cue);
        }
    }
}
