using System;
using System.Diagnostics;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyDemoPipeline
    {
        const string Menu = TidyUpFramework.Editor.TidyEditorUtility.MenuRoot + "Demo Content/";

        [MenuItem(Menu + "Build Everything", false, 100)]
        public static void BuildEverything()
        {
            Run("Foundation", BuildFoundation);
            Run("Art", BuildArt);
            Run("Core Prefabs", BuildCorePrefabs);
            Run("Library Content", BuildLibraryContent);
            Run("Store Content", BuildStoreContent);
            Run("Warehouse Content", BuildWarehouseContent);
            Run("Cozy Room Content", BuildRoomContent);
            Run("Levels", BuildLevels);
            Run("Scenes", BuildScenes);
        }

        [MenuItem(Menu + "1. Foundation (Data, Sprites, Audio, VFX)", false, 120)]
        public static void BuildFoundation()
        {
            var context = TidyDemoContext.LoadFromProject();
            TidyDemoDataBuilder.BuildShared(context);
            TidySpriteGeneration.Build();
            TidyVfxGeneration.Build();
            TidyAudioGeneration.Build(context);
            TidySystemsSetup.ConfigureFeedback(context);
            AssetDatabase.SaveAssets();
        }

        [MenuItem(Menu + "2. Art Library (Textures, Materials)", false, 121)]
        public static void BuildArt()
        {
            TidyArtLibrary.Build(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "3. Core Prefabs (Player, Systems, HUD, Menu)", false, 122)]
        public static void BuildCorePrefabs()
        {
            TidyCorePrefabBuilder.BuildAll(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "4. Library Content", false, 123)]
        public static void BuildLibraryContent()
        {
            TidyLibraryContent.Build(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "4b. Store Content", false, 124)]
        public static void BuildStoreContent()
        {
            TidyStoreContent.Build(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "4c. Warehouse Content", false, 125)]
        public static void BuildWarehouseContent()
        {
            TidyWarehouseContent.Build(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "4d. Cozy Room Content", false, 126)]
        public static void BuildRoomContent()
        {
            TidyRoomContent.Build(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "6a. Library Scene", false, 140)]
        public static void BuildLibraryScene()
        {
            TidyLibraryScene.Build(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "6b. Store Scene", false, 141)]
        public static void BuildStoreScene()
        {
            TidyStoreScene.Build(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "6c. Warehouse Scene", false, 142)]
        public static void BuildWarehouseScene()
        {
            TidyWarehouseScene.Build(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "6d. Cozy Room Scene", false, 143)]
        public static void BuildRoomScene()
        {
            TidyRoomScene.Build(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "5. Levels and Framework Settings", false, 130)]
        public static void BuildLevels()
        {
            var context = TidyDemoContext.LoadFromProject();
            TidyDemoLevels.Build(context);
            TidySystemsSetup.ConfigureFrameworkSettings(context);
        }

        [MenuItem(Menu + "6. Scenes", false, 131)]
        public static void BuildScenes()
        {
            BuildLibraryScene();
            BuildStoreScene();
            BuildWarehouseScene();
            BuildRoomScene();
            BuildSandboxScene();
            BuildMenuScene();
        }

        [MenuItem(Menu + "6e. Sandbox Scene", false, 144)]
        public static void BuildSandboxScene()
        {
            TidySandboxScene.Build(TidyDemoContext.LoadFromProject());
        }

        [MenuItem(Menu + "6f. Main Menu Scene", false, 145)]
        public static void BuildMenuScene()
        {
            TidyMenuScene.Build(TidyDemoContext.LoadFromProject());
        }

        static void Run(string stepName, Action step)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                step();
                Debug.Log("Tidy Up pipeline: " + stepName + " finished in " + timer.Elapsed.TotalSeconds.ToString("0.0") + " s.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
            finally
            {
                AssetDatabase.SaveAssets();
            }
        }
    }
}
