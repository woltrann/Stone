using System.Collections.Generic;
using TidyUpFramework.Levels;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Rules;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidySandboxScene
    {
        const float InnerWidth = 1.48f;

        public static void Build(TidyDemoContext context)
        {
            var levelPath = AssetDatabase.GetAssetPath(BuildLevel(context));
            var scene = TidySceneKit.NewScene(TidyDemoPaths.SandboxScene);
            var level = AssetDatabase.LoadAssetAtPath<TidyLevelDefinition>(levelPath);
            var spec = new TidyLightingSpec
            {
                SkyColor = new Color(0.55f, 0.48f, 0.46f),
                EquatorColor = new Color(0.45f, 0.36f, 0.3f),
                GroundColor = new Color(0.2f, 0.15f, 0.12f),
                SunIntensity = 0.5f,
                PostExposure = 0.3f,
                FogDensity = 0.01f
            };
            TidySceneKit.Lighting(TidyDemoPaths.SandboxScene, spec, out _).shadows = LightShadows.None;
            var environment = TidySceneKit.Group("Environment");
            var targets = TidySceneKit.Group("Placement Targets");
            var clutter = TidySceneKit.Group("Clutter");
            TidyRoomShell.Build(new TidyRoomSpec
            {
                Name = "Study",
                Size = new Vector3(7f, 3.4f, 6f),
                Floor = context.Material("Floor_LightOak"),
                Wall = context.Material("Plaster_Cream"),
                Ceiling = context.Material("Wood_Painted"),
                Trim = context.Material("Wood_Oak"),
                CeilingBeams = false
            }, environment);

            var bookcase = TidySceneKit.Place(context.Prefab("Bookcase"), environment, new Vector3(0f, 0f, 2.75f), new Vector3(0f, 180f, 0f));
            var shelves = TidyLibraryModels.BookcaseShelfHeights(2.6f, 5);
            Row(context, bookcase.transform, targets, shelves[3], "shelf_red", "Red Books", TidyMatchFilter.Create(new[] { context.Category("Books") }, new[] { context.Tag("Red") }), TidyOrderingMode.None);
            Row(context, bookcase.transform, targets, shelves[2], "shelf_blue", "Blue Books", TidyMatchFilter.Create(new[] { context.Category("Books") }, new[] { context.Tag("Blue") }), TidyOrderingMode.None);
            Row(context, bookcase.transform, targets, shelves[1], "shelf_history", "Encyclopaedia (in order)", TidyMatchFilter.Create(new[] { context.Category("Encyclopaedia") }, null), TidyOrderingMode.Sequence);

            var desk = TidySceneKit.Place(context.Prefab("SideTable"), environment, new Vector3(2.2f, 0f, 1.6f), new Vector3(0f, -90f, 0f));
            var zone = TidySceneDressing.Target<TidyFreePlacementZone>("Desk", targets, desk.transform.position + new Vector3(0f, 0.762f, 0f), Quaternion.identity, "desk", "Desk", TidyMatchFilter.Create(new[] { context.Category("Curiosities") }, null), new Color(0.6f, 0.8f, 1f), new Vector3(0f, 0.05f, 0f), new Vector3(0.75f, 0.12f, 1.1f));
            zone.ConfigureArea(new Vector2(0.75f, 1.1f), 6, 0f, 15f, true);
            TidySceneKit.Place(context.Prefab("Chair"), environment, new Vector3(1.55f, 0f, 1.6f), new Vector3(0f, 90f, 0f));
            TidySceneKit.PointLight("Lamp", environment, new Vector3(0f, 3f, 0f), new Color(1f, 0.85f, 0.65f), 6f, 9f, true);

            var random = new System.Random(5);
            var ids = new List<string> { "book_kingdoms", "book_voyages", "book_castles", "book_constellations", "book_moon", "book_comets", "ink_bottle", "quill" };
            for (var volume = 1; volume <= 4; volume++)
            {
                ids.Add("book_encyclopaedia_" + volume);
            }

            var points = TidySceneDressing.ScatterPoints(random, new Rect(-2.6f, -2.4f, 5f, 3.8f), ids.Count, 0.5f, null, 0f);
            for (var index = 0; index < points.Count; index++)
            {
                var lying = ids[index].StartsWith("book");
                TidySceneDressing.PlaceObject(context.ObjectPrefab(ids[index]), clutter, points[index], new Vector3(0f, (float)random.NextDouble() * 360f, lying ? 90f : 0f), 0f);
            }

            TidySceneKit.MarkStatic(environment);
            TidySceneKit.Gameplay(level, new Vector3(0f, 0.02f, -2.4f), 0f);
            TidySceneKit.Save(scene, TidyDemoPaths.SandboxScene);
        }

        static void Row(TidyDemoContext context, Transform bookcase, Transform targets, float height, string id, string displayName, TidyMatchFilter filter, TidyOrderingMode ordering)
        {
            var row = TidySceneDressing.Target<TidyRowZone>(displayName, targets, Vector3.zero, Quaternion.identity, id, displayName, filter, new Color(0.9f, 0.7f, 0.4f), new Vector3(InnerWidth * 0.5f, 0.16f, 0f), new Vector3(InnerWidth, 0.34f, 0.38f));
            row.transform.SetParent(bookcase, false);
            row.transform.localPosition = new Vector3(InnerWidth * 0.5f, height, 0.06f);
            row.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            row.ConfigureRow(InnerWidth, 0.003f, 30, new Vector3(0f, 180f, 0f), TidyRowAlignment.Start);
            row.ConfigureOrdering(ordering, TidyOrderingDirection.Ascending, TidyGroupingMode.None, TidyTagGroup.Color);
        }

        static TidyLevelDefinition BuildLevel(TidyDemoContext context)
        {
            var red = TidyDemoLevels.Place(context, "Sandbox_Red", "Shelve the red books {current}/{target}", "Red books go on the top shelf.", 50, TidyMatchFilter.Create(new[] { context.Category("Books") }, new[] { context.Tag("Red") }), 0);
            var blue = TidyDemoLevels.Place(context, "Sandbox_Blue", "Shelve the blue books {current}/{target}", "Blue books go on the middle shelf.", 50, TidyMatchFilter.Create(new[] { context.Category("Books") }, new[] { context.Tag("Blue") }), 0);
            var ordered = TidyDemoLevels.Complete(context, "Sandbox_Encyclopaedia", "File the encyclopaedia in order", "Volumes I to IV, left to right.", 100, "shelf_history");
            var desk = TidyDemoLevels.Place(context, "Sandbox_Desk", "Clear the floor onto the desk {current}/{target}", "Ink and quill belong on the desk.", 30, TidyDemoLevels.Filter(context.Category("Curiosities")), 0);
            var level = TidyAssetFactory.CreateOrLoad<TidyLevelDefinition>(TidyDemoPaths.Levels + "/Level_Sandbox_MyStudy.asset");
            level.ConfigureIdentity("sandbox_my_study", "My Study", "The finished result of the 'Creating Your First Level' tutorial.", TidyDemoPaths.SandboxScene, TidyDifficulty.Relaxed, new Color(0.9f, 0.7f, 0.4f));
            level.ConfigureObjectives(new[] { TidyDemoLevels.Entry(red, false), TidyDemoLevels.Entry(blue, false), TidyDemoLevels.Entry(ordered, false), TidyDemoLevels.Entry(desk, true) });
            level.ConfigureTiming(0f, false, TidyStarMetric.Score, new Vector2(500f, 800f));
            level.ConfigureSystems(context.Score, context.Combo, context.Hints, null);
            level.ConfigureAudio(context.Cue("Music_Library"), context.Cue("Ambience_Room"));
            level.ConfigureProgression(new TidyUnlockRequirement { UnlockedByDefault = true }, 0, null);
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            return level;
        }
    }
}
