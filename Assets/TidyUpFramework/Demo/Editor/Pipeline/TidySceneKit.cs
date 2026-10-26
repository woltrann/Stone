using System.Collections.Generic;
using System.Linq;
using TidyUpFramework.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public sealed class TidyLightingSpec
    {
        public Color SkyColor = new Color(0.32f, 0.28f, 0.3f);
        public Color EquatorColor = new Color(0.25f, 0.2f, 0.18f);
        public Color GroundColor = new Color(0.12f, 0.09f, 0.08f);
        public Color SunColor = new Color(1f, 0.86f, 0.68f);
        public float SunIntensity = 1.2f;
        public Vector3 SunEuler = new Vector3(42f, -32f, 0f);
        public bool Fog = true;
        public Color FogColor = new Color(0.2f, 0.15f, 0.13f);
        public float FogDensity = 0.012f;
        public float PostExposure = 0.15f;
        public float Saturation = 8f;
        public float Contrast = 8f;
        public float Temperature = 12f;
        public float BloomIntensity = 0.55f;
        public float VignetteIntensity = 0.28f;
        public Color ColorFilter = Color.white;
    }

    public static class TidySceneKit
    {
        public static Scene NewScene(string sceneName)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneName;
            return scene;
        }

        public static void Save(Scene scene, string sceneName)
        {
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.Scenes);
            var path = TidyDemoPaths.Scenes + "/" + sceneName + ".unity";
            global::TidyUpFramework.Editor.TidyIdGenerator.AssignObjectIds();
            EditorSceneManager.SaveScene(scene, path);
            AddToBuildSettings(path);
        }

        public static void AddToBuildSettings(string scenePath, bool first = false)
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.path != scenePath && !string.IsNullOrEmpty(scene.path) && System.IO.File.Exists(scene.path)).ToList();
            var entry = new EditorBuildSettingsScene(scenePath, true);
            if (first)
            {
                scenes.Insert(0, entry);
            }
            else
            {
                scenes.Add(entry);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static Light Lighting(string sceneName, TidyLightingSpec spec, out Volume volume)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = spec.SkyColor;
            RenderSettings.ambientEquatorColor = spec.EquatorColor;
            RenderSettings.ambientGroundColor = spec.GroundColor;
            RenderSettings.skybox = null;
            RenderSettings.fog = spec.Fog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = spec.FogColor;
            RenderSettings.fogDensity = spec.FogDensity;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            Lightmapping.lightingSettings = LightingSettingsAsset(sceneName);

            var sun = new GameObject("Key Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = spec.SunColor;
            sun.intensity = spec.SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.transform.rotation = Quaternion.Euler(spec.SunEuler);
            RenderSettings.sun = sun;

            volume = CreateVolume(sceneName, "Global Volume", spec, 1f, 0);
            return sun;
        }

        static LightingSettings LightingSettingsAsset(string sceneName)
        {
            var folder = TidyDemoPaths.Data + "/Lighting";
            TidyAssetFactory.EnsureFolder(folder);
            var path = folder + "/" + sceneName + "_Lighting.lighting";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (settings == null)
            {
                settings = new LightingSettings { name = sceneName + "_Lighting" };
                AssetDatabase.CreateAsset(settings, path);
            }

            settings.bakedGI = false;
            settings.realtimeGI = false;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        public static Volume CreateVolume(string sceneName, string volumeName, TidyLightingSpec spec, float weight, int priority)
        {
            var profilePath = TidyDemoPaths.Data + "/Volumes/" + sceneName + "_" + volumeName.Replace(" ", string.Empty) + ".asset";
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.Data + "/Volumes");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            foreach (var component in profile.components.ToList())
            {
                profile.Remove(component.GetType());
                Object.DestroyImmediate(component, true);
            }

            var tonemapping = AddOverride<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);
            var bloom = AddOverride<Bloom>(profile);
            bloom.intensity.Override(spec.BloomIntensity);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(0.65f);
            var color = AddOverride<ColorAdjustments>(profile);
            color.postExposure.Override(spec.PostExposure);
            color.saturation.Override(spec.Saturation);
            color.contrast.Override(spec.Contrast);
            color.colorFilter.Override(spec.ColorFilter);
            var white = AddOverride<WhiteBalance>(profile);
            white.temperature.Override(spec.Temperature);
            var vignette = AddOverride<Vignette>(profile);
            vignette.intensity.Override(spec.VignetteIntensity);
            vignette.smoothness.Override(0.45f);
            EditorUtility.SetDirty(profile);

            var volumeObject = new GameObject(volumeName);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.weight = weight;
            volume.priority = priority;
            volume.sharedProfile = profile;
            return volume;
        }

        public static ReflectionProbe ReflectionProbe(Vector3 position, Vector3 size, float intensity = 0.8f)
        {
            var probe = new GameObject("Reflection Probe").AddComponent<ReflectionProbe>();
            probe.transform.position = position;
            probe.size = size;
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.intensity = intensity;
            probe.boxProjection = true;
            probe.resolution = 128;
            return probe;
        }

        public static Light PointLight(string lightName, Transform parent, Vector3 position, Color color, float intensity, float range, bool shadows = false)
        {
            var light = new GameObject(lightName).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.position = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return light;
        }

        public static Light SpotLight(string lightName, Transform parent, Vector3 position, Vector3 euler, Color color, float intensity, float range, float angle, bool shadows = true)
        {
            var light = new GameObject(lightName).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.6f;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return light;
        }

        public static GameObject Gameplay(TidyLevelDefinition level, Vector3 playerPosition, float playerYaw)
        {
            var systems = InstantiateCore("TidyLevelSystems");
            var manager = systems.GetComponent<TidyLevelManager>();
            manager.AssignLevel(level);
            TidySerialization.Set(manager, "level", level);
            var player = InstantiateCore("TidyPlayer");
            player.transform.SetPositionAndRotation(playerPosition, Quaternion.Euler(0f, playerYaw, 0f));
            InstantiateCore("TidyHUD");
            return systems;
        }

        public static GameObject InstantiateCore(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TidyDemoPaths.CorePrefabs + "/" + prefabName + ".prefab");
            if (prefab == null)
            {
                Debug.LogError("Tidy Up pipeline: core prefab " + prefabName + " missing.");
                return new GameObject(prefabName);
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        public static GameObject Place(GameObject prefab, Transform parent, Vector3 position, Vector3 euler, Vector3? scale = null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            if (scale.HasValue)
            {
                instance.transform.localScale = scale.Value;
            }

            return instance;
        }

        public static Transform Group(string groupName, Transform parent = null)
        {
            var group = new GameObject(groupName).transform;
            group.SetParent(parent, false);
            return group;
        }

        public static void MarkStatic(Transform root)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.ContributeGI);
            }
        }

        static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        public static IEnumerable<T> Shuffle<T>(IList<T> source, System.Random random)
        {
            var copy = source.ToList();
            for (var index = copy.Count - 1; index > 0; index--)
            {
                var swap = random.Next(index + 1);
                (copy[index], copy[swap]) = (copy[swap], copy[index]);
            }

            return copy;
        }
    }
}
