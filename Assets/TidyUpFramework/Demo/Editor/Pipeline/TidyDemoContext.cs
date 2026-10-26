using System.Collections.Generic;
using System.IO;
using TidyUpFramework.Audio;
using TidyUpFramework.Feedback;
using TidyUpFramework.Hints;
using TidyUpFramework.Levels;
using TidyUpFramework.Objects;
using TidyUpFramework.Scoring;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public sealed class TidyDemoContext
    {
        readonly Dictionary<string, TidyTagDefinition> tags = new Dictionary<string, TidyTagDefinition>();
        readonly Dictionary<string, TidyCategoryDefinition> categories = new Dictionary<string, TidyCategoryDefinition>();
        readonly Dictionary<string, TidyObjectDefinition> definitions = new Dictionary<string, TidyObjectDefinition>();
        readonly Dictionary<string, TidyAudioCue> cues = new Dictionary<string, TidyAudioCue>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        public TidyPhysicsProfile ControlledProfile;
        public TidyPhysicsProfile PhysicsProfile;
        public TidyScoreDefinition Score;
        public TidyComboDefinition Combo;
        public TidyHintDefinition Hints;
        public TidyFeedbackDefinition Feedback;
        public TidyLevelCatalog Catalog;

        public void Add(TidyTagDefinition tag) => tags[tag.name] = tag;
        public void Add(TidyCategoryDefinition category) => categories[category.name] = category;
        public void Add(TidyObjectDefinition definition) => definitions[definition.Id] = definition;
        public void AddCue(string key, TidyAudioCue cue) => cues[key] = cue;
        public void AddMaterial(string key, Material material) => materials[key] = material;
        public void AddPrefab(GameObject prefab) => prefabs[prefab.name] = prefab;

        public TidyTagDefinition Tag(string tagName) => Require(tags, "Tag_" + tagName);
        public TidyCategoryDefinition Category(string categoryName) => Require(categories, "Category_" + categoryName);
        public TidyObjectDefinition Definition(string id) => Require(definitions, id);
        public GameObject ObjectPrefab(string id) => Definition(id)?.Prefab;
        public TidyAudioCue Cue(string key) => Find(cues, key);
        public Material Material(string key) => Require(materials, key);
        public GameObject Prefab(string prefabName) => Require(prefabs, prefabName);
        public bool HasDefinition(string id) => definitions.ContainsKey(id);
        public IEnumerable<TidyObjectDefinition> AllDefinitions => definitions.Values;

        public static TidyDemoContext LoadFromProject()
        {
            var context = new TidyDemoContext();
            foreach (var tag in LoadAll<TidyTagDefinition>(TidyDemoPaths.Tags))
            {
                context.Add(tag);
            }

            foreach (var category in LoadAll<TidyCategoryDefinition>(TidyDemoPaths.Categories))
            {
                context.Add(category);
            }

            foreach (var definition in LoadAll<TidyObjectDefinition>(TidyDemoPaths.Objects))
            {
                context.Add(definition);
            }

            foreach (var cue in LoadAll<TidyAudioCue>(TidyDemoPaths.AudioCues))
            {
                context.AddCue(cue.name.Replace("Cue_", string.Empty), cue);
            }

            foreach (var cue in LoadAll<TidyAudioCue>(TidyDemoPaths.DemoAudioCues))
            {
                context.AddCue(cue.name.Replace("Cue_", string.Empty), cue);
            }

            foreach (var material in LoadAll<Material>(TidyDemoPaths.Materials))
            {
                context.AddMaterial(material.name.StartsWith("M_") ? material.name.Substring(2) : material.name, material);
            }

            foreach (var prefab in LoadAll<GameObject>(TidyDemoPaths.Prefabs))
            {
                context.AddPrefab(prefab);
            }

            context.ControlledProfile = AssetDatabase.LoadAssetAtPath<TidyPhysicsProfile>(TidyDemoPaths.Systems + "/PhysicsProfile_Controlled.asset");
            context.PhysicsProfile = AssetDatabase.LoadAssetAtPath<TidyPhysicsProfile>(TidyDemoPaths.Systems + "/PhysicsProfile_Physical.asset");
            context.Score = AssetDatabase.LoadAssetAtPath<TidyScoreDefinition>(TidyDemoPaths.Systems + "/Score_Default.asset");
            context.Combo = AssetDatabase.LoadAssetAtPath<TidyComboDefinition>(TidyDemoPaths.Systems + "/Combo_Default.asset");
            context.Hints = AssetDatabase.LoadAssetAtPath<TidyHintDefinition>(TidyDemoPaths.Systems + "/Hints_Default.asset");
            context.Feedback = AssetDatabase.LoadAssetAtPath<TidyFeedbackDefinition>(TidyDemoPaths.Systems + "/Feedback_Default.asset");
            context.Catalog = AssetDatabase.LoadAssetAtPath<TidyLevelCatalog>(TidyDemoPaths.Levels + "/LevelCatalog.asset");
            return context;
        }

        static IEnumerable<T> LoadAll<T>(string folder) where T : Object
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                yield break;
            }

            var filter = typeof(T) == typeof(GameObject) ? "t:Prefab" : "t:" + typeof(T).Name;
            foreach (var guid in AssetDatabase.FindAssets(filter, new[] { folder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    yield return asset;
                }
            }
        }

        static T Require<T>(Dictionary<string, T> source, string key) where T : class
        {
            if (source.TryGetValue(key, out var value))
            {
                return value;
            }

            Debug.LogError("Tidy Up pipeline: missing '" + key + "'.");
            return null;
        }

        static T Find<T>(Dictionary<string, T> source, string key) where T : class
        {
            return source.TryGetValue(key, out var value) ? value : null;
        }

        public static string FileName(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}
