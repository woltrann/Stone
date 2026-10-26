using System.Collections.Generic;
using TidyUpFramework.Cleaning;
using TidyUpFramework.Demo.Editor.ContentKit;
using TidyUpFramework.Levels;
using TidyUpFramework.Objects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static partial class TidyRoomScene
    {
        const float RoomWidth = 8.6f;
        const float RoomDepth = 7.6f;
        const float RoomHeight = 2.9f;
        const string MeshFolder = TidyRoomModels.Folder;
        static readonly Vector3 SpawnPoint = new Vector3(3.2f, 0.02f, -3.1f);
        static readonly Color Golden = new Color(1f, 0.72f, 0.45f);
        static readonly Color LampWarm = new Color(1f, 0.78f, 0.52f);

        static TidyDemoContext context;
        static System.Random random;
        static Transform environment;
        static Transform furniture;
        static Transform targets;
        static Transform clutter;
        static Transform decor;
        static readonly Dictionary<string, int> instanceCounters = new Dictionary<string, int>();

        public static void Build(TidyDemoContext demoContext)
        {
            if (!demoContext.HasDefinition("room_sponge"))
            {
                Debug.LogError("Tidy Up pipeline: Cozy Room content is missing. Run the Cozy Room Content step first.");
                return;
            }

            context = demoContext;
            random = new System.Random(4404);
            instanceCounters.Clear();
            var previousActive = SceneManager.GetActiveScene();
            var keepOthers = HasUnsavedScenes();
            var scene = keepOthers ? NewAdditiveScene() : TidySceneKit.NewScene(TidyDemoPaths.CozyRoomScene);
            roomScene = scene;
            var level = AssetDatabase.LoadAssetAtPath<TidyLevelDefinition>(TidyRoomLevel.LevelPath);
            environment = TidySceneKit.Group("Environment");
            furniture = TidySceneKit.Group("Furniture");
            decor = TidySceneKit.Group("Decor");
            targets = TidySceneKit.Group("Placement Targets");
            clutter = TidySceneKit.Group("Clutter");
            var mood = BuildLighting();
            BuildShell();
            BuildFurniture();
            var lights = BuildDecorLights();
            TidySceneKit.MarkStatic(environment);
            TidySceneKit.MarkStatic(furniture);
            Physics.SyncTransforms();
            BuildStains();
            BuildClutter();
            BuildBeforeAfter(mood, lights);
            TidySceneKit.ReflectionProbe(new Vector3(0f, 1.4f, 0f), new Vector3(RoomWidth, RoomHeight, RoomDepth), 0.5f);
            TidySceneKit.Gameplay(level, SpawnPoint, -38f);
            TidySceneKit.Save(scene, TidyDemoPaths.CozyRoomScene);
            if (keepOthers && previousActive.IsValid())
            {
                SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static Scene roomScene;

        static bool HasUnsavedScenes()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                {
                    return true;
                }
            }

            return false;
        }

        static Scene NewAdditiveScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            return scene;
        }

        static (Volume messy, Volume organised, Light sun) BuildLighting()
        {
            var spec = new TidyLightingSpec
            {
                SkyColor = new Color(0.62f, 0.52f, 0.5f),
                EquatorColor = new Color(0.56f, 0.44f, 0.36f),
                GroundColor = new Color(0.3f, 0.22f, 0.17f),
                SunColor = new Color(1f, 0.8f, 0.6f),
                SunIntensity = 0.35f,
                SunEuler = new Vector3(32f, 160f, 0f),
                FogColor = new Color(0.4f, 0.3f, 0.24f),
                FogDensity = 0.01f,
                PostExposure = 0.12f,
                Saturation = 6f,
                Contrast = 6f,
                Temperature = 10f,
                BloomIntensity = 0.85f,
                VignetteIntensity = 0.22f
            };
            var sun = TidySceneKit.Lighting(TidyDemoPaths.CozyRoomScene, spec, out var organised);
            sun.shadows = LightShadows.None;
            var messySpec = new TidyLightingSpec
            {
                PostExposure = -0.12f,
                Saturation = -42f,
                Contrast = -8f,
                Temperature = -22f,
                BloomIntensity = 0.15f,
                VignetteIntensity = 0.42f,
                ColorFilter = new Color(0.84f, 0.88f, 0.96f)
            };
            var messy = TidySceneKit.CreateVolume(TidyDemoPaths.CozyRoomScene, "Messy Volume", messySpec, 1f, 1);
            organised.weight = 0f;
            organised.priority = 2;
            return (messy, organised, sun);
        }

        static void BuildShell()
        {
            TidyRoomShell.Build(new TidyRoomSpec
            {
                Name = "CozyRoom",
                Size = new Vector3(RoomWidth, RoomHeight, RoomDepth),
                Floor = context.Material("Floor_LightOak"),
                Wall = context.Material("Wallpaper_Room"),
                Ceiling = context.Material("Plaster_Cream"),
                Trim = context.Material("Wood_Painted"),
                CeilingBeams = false,
                FloorUvScale = new Vector2(0.8f, 0.8f),
                WallUvScale = new Vector2(0.6f, 0.6f)
            }, environment);
            var rug = new TidyMeshBuilder();
            rug.AddPlane(new Vector2(3.2f, 2.4f), 1, 1, 0, null, true);
            var rugMesh = TidyMeshUtility.SaveAsIs(rug, "SM_Room_Rug", MeshFolder);
            TidySceneDressing.Decor("Rug", environment, rugMesh, new[] { context.Material("Rug_Room") }, new Vector3(-0.6f, 0.004f, 0.2f), Quaternion.identity).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var bedRug = new TidyMeshBuilder();
            bedRug.AddPlane(new Vector2(1.1f, 2.0f), 1, 1, 0, null, true);
            var bedRugMesh = TidyMeshUtility.SaveAsIs(bedRug, "SM_Room_BedRug", MeshFolder);
            TidySceneDressing.Decor("Bedside Rug", environment, bedRugMesh, new[] { context.Material("Rug_Room") }, new Vector3(-1.65f, 0.004f, 1.6f), Quaternion.identity).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        static GameObject Put(string prefabName, Transform parent, Vector3 position, float yaw, float scale = 1f)
        {
            return TidySceneKit.Place(context.Prefab(prefabName), parent, position, new Vector3(0f, yaw, 0f), Vector3.one * scale);
        }

        static GameObject Spawn(string definitionId, Vector3 position, Vector3 euler, float surfaceY, Transform parent = null)
        {
            var instance = TidySceneDressing.PlaceObject(context.ObjectPrefab(definitionId), parent != null ? parent : clutter, position, euler, surfaceY);
            instanceCounters.TryGetValue(definitionId, out var count);
            instanceCounters[definitionId] = count + 1;
            var tidyObject = instance.GetComponent<TidyObject>();
            tidyObject.AssignInstanceId(definitionId + "_" + count);
            instance.name = tidyObject.DisplayName + " " + (count + 1);
            return instance;
        }

        static float SurfaceAt(float x, float z, float fromHeight = 2.4f)
        {
            var hits = Physics.RaycastAll(new Vector3(x, fromHeight, z), Vector3.down, fromHeight + 1f, ~0, QueryTriggerInteraction.Ignore);
            var best = 0f;
            var bestDistance = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.scene != roomScene || hit.collider.GetComponentInParent<TidyObject>() != null ||hit.collider.GetComponentInParent<TidyCleanable>() != null)
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    best = hit.point.y;
                }
            }

            return best;
        }

        static float Range(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        static float Yaw()
        {
            return (float)random.NextDouble() * 360f;
        }
    }
}
