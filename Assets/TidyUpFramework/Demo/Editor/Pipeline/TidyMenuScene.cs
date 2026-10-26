using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyMenuScene
    {
        const string MeshFolder = TidyDemoPaths.Meshes + "/Library";

        public static void Build(TidyDemoContext context)
        {
            var scene = TidySceneKit.NewScene(TidyDemoPaths.MainMenuScene);
            var spec = new TidyLightingSpec
            {
                SkyColor = new Color(0.5f, 0.42f, 0.42f),
                EquatorColor = new Color(0.42f, 0.3f, 0.24f),
                GroundColor = new Color(0.18f, 0.12f, 0.1f),
                SunIntensity = 0.4f,
                FogDensity = 0.03f,
                PostExposure = 0.3f,
                BloomIntensity = 0.9f,
                VignetteIntensity = 0.35f
            };
            var sun = TidySceneKit.Lighting(TidyDemoPaths.MainMenuScene, spec, out _);
            sun.shadows = LightShadows.None;
            var set = TidySceneKit.Group("Backdrop");
            BuildBackdrop(context, set);
            BuildCamera();
            TidySceneKit.InstantiateCore("TidyMainMenu");
            TidySceneKit.MarkStatic(set);
            TidySceneKit.Save(scene, TidyDemoPaths.MainMenuScene);
            TidySceneKit.AddToBuildSettings(TidyDemoPaths.Scenes + "/" + TidyDemoPaths.MainMenuScene + ".unity", true);
        }

        static void BuildBackdrop(TidyDemoContext context, Transform set)
        {
            TidyRoomShell.Build(new TidyRoomSpec
            {
                Name = "Menu",
                Size = new Vector3(8f, 4.5f, 8f),
                Floor = context.Material("Floor_Herringbone"),
                Wall = context.Material("Wallpaper_Library"),
                Ceiling = context.Material("Wood_Walnut"),
                Trim = context.Material("Wood_Walnut"),
                Wainscot = context.Material("Wood_Mahogany"),
                WainscotHeight = 1.1f
            }, set);

            var bookcase = context.Prefab("Bookcase");
            var decor = TidyDecorBooks.Materials(context);
            var shelves = TidyLibraryModels.BookcaseShelfHeights(2.6f, 5);
            for (var index = 0; index < 4; index++)
            {
                var instance = TidySceneKit.Place(bookcase, set, new Vector3(-2.4f + index * 1.62f, 0f, 3.75f), new Vector3(0f, 180f, 0f));
                for (var shelf = 0; shelf < shelves.Length; shelf++)
                {
                    var spacing = shelf < shelves.Length - 1 ? shelves[shelf + 1] - shelves[shelf] - 0.03f : 0.42f;
                    TidySceneDressing.Decor("Books", instance.transform, TidyDecorBooks.Row(1.48f, spacing, 900 + index * 10 + shelf), decor, new Vector3(-0.74f, shelves[shelf], 0.06f), Quaternion.identity);
                }
            }

            TidySceneKit.Place(context.Prefab("ReadingTable"), set, new Vector3(0.9f, 0f, 1.2f), new Vector3(0f, 12f, 0f));
            var candelabra = TidySceneKit.Place(context.Prefab("Candelabra"), set, new Vector3(1.4f, 0.78f, 1.25f), Vector3.zero);
            var flame = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/SM_Flame.asset");
            var candle = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/SM_Candle_12.asset");
            foreach (var point in TidyLibraryModels.CandelabraCandlePoints(4))
            {
                TidySceneDressing.Decor("Candle", candelabra.transform, candle, new[] { context.Material("Wax_Cream"), context.Material("Wick"), context.Material("Metal_Brass") }, point, Quaternion.identity);
                TidySceneDressing.Decor("Flame", candelabra.transform, flame, new[] { context.Material("Flame") }, point + new Vector3(0f, 0.12f, 0f), Quaternion.identity).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            var candleLight = TidySceneKit.PointLight("Candle Light", set, candelabra.transform.position + Vector3.up * 0.45f, new Color(1f, 0.72f, 0.42f), 3.5f, 6f, true);
            candleLight.gameObject.AddComponent<TidyCandleFlicker>();
            TidySceneKit.Place(context.Prefab("Chair"), set, new Vector3(0.2f, 0f, 0.5f), new Vector3(0f, 40f, 0f));
            TidySceneKit.Place(context.Prefab("Globe"), set, new Vector3(-2.8f, 0f, 2.2f), new Vector3(0f, 20f, 0f));
            TidySceneKit.Place(context.Prefab("Lectern"), set, new Vector3(2.9f, 0f, 2.3f), new Vector3(0f, 200f, 0f));

            var random = new System.Random(77);
            var books = new[] { "book_arcane_basics", "book_moss", "book_kingdoms", "book_constellations", "book_wards", "book_herbal", "book_comets" };
            var pileSurface = 0.78f;
            for (var index = 0; index < 5; index++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(context.ObjectPrefab(books[index]), set);
                instance.transform.SetPositionAndRotation(new Vector3(0.45f, 0f, 1.05f), Quaternion.Euler(0f, (float)random.NextDouble() * 50f, 90f));
                TidySceneDressing.Settle(instance, pileSurface);
                pileSurface = TidySceneDressing.TopOf(instance);
                StripGameplay(instance);
            }

            foreach (var id in new[] { "potion_blue_round", "potion_red_tall", "crystal_purple", "scroll_0", "ink_bottle", "quill" })
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(context.ObjectPrefab(id), set);
                var position = new Vector3(0.9f + ((float)random.NextDouble() - 0.5f) * 1.6f, 0f, 1.2f + ((float)random.NextDouble() - 0.5f) * 0.6f);
                instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
                TidySceneDressing.Settle(instance, 0.78f);
                StripGameplay(instance);
            }

            TidySceneKit.SpotLight("Book Light", set, new Vector3(-1f, 4f, 0.5f), new Vector3(60f, 20f, 0f), new Color(1f, 0.85f, 0.65f), 9f, 9f, 60f);
            TidySceneKit.ReflectionProbe(new Vector3(0f, 2f, 0f), new Vector3(8f, 4.5f, 8f), 0.6f);
        }

        static void StripGameplay(GameObject instance)
        {
            foreach (var component in instance.GetComponents<Component>())
            {
                if (component is TidyUpFramework.Objects.TidyObject || component is Rigidbody)
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        static void BuildCamera()
        {
            var cameraObject = new GameObject("Menu Camera") { tag = "MainCamera" };
            cameraObject.transform.SetPositionAndRotation(new Vector3(-1.6f, 1.45f, -1.9f), Quaternion.Euler(8f, 28f, 0f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.05f;
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<TidyMenuCameraDrift>();
        }
    }
}
