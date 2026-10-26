using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using TidyUpFramework.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static partial class TidyStoreScene
    {
        const float RoomWidth = 14f;
        const float RoomLength = 16f;
        const float RoomHeight = 4.2f;
        const float WallInset = 0.27f;
        static readonly Color LampWarm = new Color(1f, 0.82f, 0.62f);
        static readonly Vector3 PlayerSpawn = new Vector3(0f, 0.02f, -6.6f);

        static TidyDemoContext context;
        static Transform environment;
        static Transform props;
        static Transform targets;
        static Transform clutter;
        static System.Random random;
        static string meshFolder;

        public static void Build(TidyDemoContext demoContext)
        {
            context = demoContext;
            random = new System.Random(4242);
            meshFolder = TidyStoreContent.MeshFolder;
            TidyAssetFactory.EnsureFolder(meshFolder);
            var previous = SceneManager.GetActiveScene();
            var keepPrevious = previous.isDirty && previous.rootCount > 0 && previous.name != TidyDemoPaths.StoreScene;
            var scene = keepPrevious ? AdditiveScene() : TidySceneKit.NewScene(TidyDemoPaths.StoreScene);
            var level = AssetDatabase.LoadAssetAtPath<TidyLevelDefinition>(TidyStoreLevel.LevelPath);
            environment = TidySceneKit.Group("Environment");
            props = TidySceneKit.Group("Fixtures");
            targets = TidySceneKit.Group("Placement Targets");
            clutter = TidySceneKit.Group("Clutter");

            var volumes = BuildLighting();
            BuildShell();
            BuildStorefront();
            BuildValance();
            var lampLights = BuildPendants();
            BuildSpeciesShelves();
            BuildDecorShelves();
            BuildRainbowStand();
            BuildSetPedestals();
            BuildTripletTable();
            BuildPodBins();
            BuildCheckout();
            BuildSideTable();
            BuildIslands();
            BuildMascot();
            BuildHangingBanner();
            BuildClutter();
            BuildBeforeAfter(volumes, lampLights);
            TidySceneKit.MarkStatic(environment);
            TidySceneKit.MarkStatic(props);
            TidySceneKit.ReflectionProbe(new Vector3(0f, 2f, 0f), new Vector3(RoomWidth, RoomHeight, RoomLength), 0.7f);
            TidySceneKit.Gameplay(level, PlayerSpawn, 0f);
            TidySceneKit.Save(scene, TidyDemoPaths.StoreScene);
            if (keepPrevious)
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static Scene AdditiveScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = TidyDemoPaths.StoreScene;
            SceneManager.SetActiveScene(scene);
            return scene;
        }

        static (Volume messy, Volume organised) BuildLighting()
        {
            var spec = new TidyLightingSpec
            {
                SkyColor = new Color(0.86f, 0.8f, 0.76f),
                EquatorColor = new Color(0.78f, 0.68f, 0.6f),
                GroundColor = new Color(0.46f, 0.38f, 0.34f),
                SunColor = new Color(1f, 0.92f, 0.8f),
                SunIntensity = 0.55f,
                SunEuler = new Vector3(48f, 24f, 0f),
                FogColor = new Color(0.95f, 0.86f, 0.78f),
                FogDensity = 0.006f,
                PostExposure = 0.2f,
                Saturation = 14f,
                Contrast = 10f,
                Temperature = 8f,
                BloomIntensity = 0.5f,
                VignetteIntensity = 0.2f
            };
            var sun = TidySceneKit.Lighting(TidyDemoPaths.StoreScene, spec, out var organised);
            sun.shadows = LightShadows.None;
            var messySpec = new TidyLightingSpec
            {
                PostExposure = -0.05f,
                Saturation = -22f,
                Contrast = -4f,
                Temperature = -10f,
                BloomIntensity = 0.25f,
                VignetteIntensity = 0.32f
            };
            var messy = TidySceneKit.CreateVolume(TidyDemoPaths.StoreScene, "Messy Volume", messySpec, 1f, 1);
            organised.weight = 0f;
            organised.priority = 2;
            return (messy, organised);
        }

        static void BuildShell()
        {
            TidyRoomShell.Build(new TidyRoomSpec
            {
                Name = "ToyStore",
                Size = new Vector3(RoomWidth, RoomHeight, RoomLength),
                Floor = context.Material("Tiles_Store"),
                Wall = context.Material("Plaster_Store"),
                Ceiling = context.Material("Store_Cream"),
                Trim = context.Material("Store_Cream"),
                Wainscot = context.Material("Store_Mint"),
                WainscotHeight = 1.0f,
                BeamSpacing = 2.5f,
                FloorUvScale = new Vector2(0.075f, 0.075f),
                WallUvScale = new Vector2(0.5f, 0.5f)
            }, environment);

            var rug = new TidyMeshBuilder();
            rug.AddLathe(new[] { new Vector2(0f, 0f), new Vector2(1.75f, 0f), new Vector2(1.75f, 0.006f), new Vector2(0f, 0.006f) }, 64);
            TidySceneDressing.Decor("Round Rug", environment, TidyMeshUtility.SaveAsIs(rug, "SM_StoreRug", meshFolder), new[] { context.Material("Rug_Room") }, new Vector3(0f, 0f, 4.4f), Quaternion.identity);
        }

        static void BuildStorefront()
        {
            var windowMesh = WindowMesh(4.4f, 2.4f, 4, "SM_StoreWindow");
            var doorMesh = WindowMesh(1.7f, 2.7f, 1, "SM_StoreDoor");
            var materials = new[] { context.Material("Plastic_Teal"), context.Material("Store_Window") };
            var front = -RoomLength * 0.5f + 0.01f;
            foreach (var x in new[] { -4.5f, 4.5f })
            {
                TidySceneDressing.Decor("Shop Window", environment, windowMesh, materials, new Vector3(x, 0.55f, front), Quaternion.identity);
                BuildWindowDisplay(new Vector3(x, 0f, front + 0.32f));
                TidySceneKit.SpotLight("Window Daylight", environment, new Vector3(x, 3.3f, front + 0.4f), new Vector3(38f, 0f, 0f), new Color(1f, 0.95f, 0.86f), 14f, 14f, 80f, x < 0f);
            }

            TidySceneDressing.Decor("Shop Door", environment, doorMesh, materials, new Vector3(0f, 0f, front), Quaternion.identity);
            var sign = TidySceneDressing.Decor("Open Sign", environment, TidyStoreFixtures.SignBoard(meshFolder, 1.5f, 0.4f), new[] { context.Material("Store_Coral"), context.Material("Store_Cream") }, new Vector3(0f, 3.05f, front + 0.01f), Quaternion.identity);
            TidySceneDressing.Label("PEBBLE PALS", sign.transform, new Vector3(0f, 0f, 0.05f), Quaternion.Euler(0f, 180f, 0f), 2.2f, new Color(0.36f, 0.22f, 0.34f), new Vector2(1.4f, 0.3f));
        }

        static Mesh WindowMesh(float width, float height, int panes, string meshName)
        {
            var builder = new TidyMeshBuilder(2);
            const float frame = 0.1f;
            builder.AddBeveledBox(new Vector3(0f, frame * 0.5f, 0.05f), new Vector3(width, frame, 0.1f), 0.015f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, height - frame * 0.5f, 0.05f), new Vector3(width, frame, 0.1f), 0.015f, 2, 0);
            for (var index = 0; index <= panes; index++)
            {
                var x = -width * 0.5f + frame * 0.5f + index * (width - frame) / panes;
                builder.AddBeveledBox(new Vector3(x, height * 0.5f, 0.05f), new Vector3(index == 0 || index == panes ? frame : 0.06f, height, 0.1f), 0.012f, 2, 0);
            }

            builder.AddBeveledBox(new Vector3(0f, height * 0.72f, 0.04f), new Vector3(width - frame, 0.05f, 0.06f), 0.01f, 1, 0);
            builder.AddBox(new Vector3(0f, height * 0.5f, 0.012f), new Vector3(width - frame, height - frame, 0.01f), 1);
            return TidyMeshUtility.SaveAsIs(builder, meshName, meshFolder);
        }

        static void BuildWindowDisplay(Vector3 position)
        {
            var ledge = new TidyMeshBuilder();
            ledge.AddBeveledBox(new Vector3(0f, 0.27f, 0f), new Vector3(4.2f, 0.54f, 0.5f), 0.02f, 2);
            TidySceneDressing.Decor("Window Ledge", environment, TidyMeshUtility.SaveAsIs(ledge, "SM_StoreWindowLedge", meshFolder), new[] { context.Material("Store_Pastel_Bunny") }, position, Quaternion.identity);
            var boxes = TidyStoreFixtures.ToyBoxRow(3.9f, 0.3f, (int)(position.x * 10f) + 900, ToyBoxColours.Length);
            TidySceneDressing.Decor("Window Toys", environment, boxes, ToyBoxMaterials(), position + new Vector3(-1.95f, 0.54f, 0f), Quaternion.identity);
        }

        static readonly string[] ToyBoxColours = { "Red", "Blue", "Green", "Yellow", "Purple", "Pink", "Orange", "Teal" };

        static Material[] toyBoxMaterials;

        static Material[] ToyBoxMaterials()
        {
            if (toyBoxMaterials != null && toyBoxMaterials.Length > 0 && toyBoxMaterials[0] != null)
            {
                return toyBoxMaterials;
            }

            toyBoxMaterials = new Material[ToyBoxColours.Length + 2];
            for (var index = 0; index < ToyBoxColours.Length; index++)
            {
                toyBoxMaterials[index] = context.Material("Matte_" + ToyBoxColours[index]);
            }

            toyBoxMaterials[ToyBoxColours.Length] = context.Material("Store_Cream");
            toyBoxMaterials[ToyBoxColours.Length + 1] = context.Material("Matte_White");
            return toyBoxMaterials;
        }

        static void BuildValance()
        {
            var builder = new TidyMeshBuilder(2);
            const float scallop = 0.4f;
            const float top = 3.78f;
            AddValanceRun(builder, new Vector3(-RoomWidth * 0.5f, top, RoomLength * 0.5f - 0.05f), Vector3.right, RoomWidth, scallop);
            AddValanceRun(builder, new Vector3(RoomWidth * 0.5f, top, -RoomLength * 0.5f + 0.05f), Vector3.left, RoomWidth, scallop);
            AddValanceRun(builder, new Vector3(-RoomWidth * 0.5f + 0.05f, top, -RoomLength * 0.5f), Vector3.forward, RoomLength, scallop);
            AddValanceRun(builder, new Vector3(RoomWidth * 0.5f - 0.05f, top, RoomLength * 0.5f), Vector3.back, RoomLength, scallop);
            TidySceneDressing.Decor("Awning Valance", environment, TidyMeshUtility.SaveAsIs(builder, "SM_StoreValance", meshFolder), new[] { context.Material("Store_Coral"), context.Material("Store_Cream") }, Vector3.zero, Quaternion.identity);
        }

        static void AddValanceRun(TidyMeshBuilder builder, Vector3 start, Vector3 direction, float length, float scallop)
        {
            var inward = Vector3.Cross(direction, Vector3.up) * -1f;
            var rotation = Quaternion.LookRotation(inward, Vector3.up);
            var count = Mathf.FloorToInt(length / scallop);
            var half = new List<Vector2>();
            for (var index = 0; index <= 12; index++)
            {
                var angle = Mathf.PI + index * Mathf.PI / 12f;
                half.Add(new Vector2(Mathf.Cos(angle) * scallop * 0.5f, Mathf.Sin(angle) * scallop * 0.42f));
            }

            var outline = half.ToArray();
            System.Array.Reverse(outline);
            var bandCenter = start + direction * (length * 0.5f) + Vector3.up * 0.12f;
            builder.AddBeveledBox(Vector3.zero, new Vector3(length, 0.24f, 0.05f), 0.01f, 1, 0, Matrix4x4.TRS(bandCenter + inward * 0.03f, rotation * Quaternion.Euler(0f, 180f, 0f), Vector3.one));
            for (var index = 0; index < count; index++)
            {
                var center = start + direction * ((index + 0.5f) * scallop) + inward * 0.02f;
                builder.AddExtrusion(outline, 0.03f, index % 2, Matrix4x4.TRS(center, Quaternion.LookRotation(-inward, Vector3.up), Vector3.one));
            }
        }

        static List<Light> BuildPendants()
        {
            var lights = new List<Light>();
            var mesh = TidyStoreFixtures.PendantLamp(meshFolder, 0.85f);
            var shades = new[] { "Plastic_Teal", "Plastic_Yellow", "Plastic_Pink" };
            var index = 0;
            foreach (var z in new[] { -4.6f, -0.6f, 4.4f, 6.9f })
            {
                foreach (var x in new[] { -4f, 0f, 4f })
                {
                    var materials = new[] { context.Material(shades[index % shades.Length]), context.Material("Lamp_Glow"), context.Material("Dark") };
                    var lamp = TidySceneDressing.Decor("Pendant Lamp", environment, mesh, materials, new Vector3(x, RoomHeight, z), Quaternion.identity);
                    lamp.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    var light = TidySceneKit.PointLight("Pendant Light", lamp.transform, new Vector3(x, RoomHeight - 1.1f, z), LampWarm, 3.2f, 7.5f, x == 0f && z < 0f && z > -1f);
                    lights.Add(light);
                    index++;
                }
            }

            return lights;
        }
    }
}
