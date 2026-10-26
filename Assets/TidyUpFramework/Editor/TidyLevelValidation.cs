using System.Collections.Generic;
using System.Linq;
using TidyUpFramework.Input;
using TidyUpFramework.Interaction;
using TidyUpFramework.Levels;
using TidyUpFramework.Objectives;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TidyUpFramework.Editor
{
    public static class TidyLevelValidation
    {
        public static List<TidyValidationIssue> ValidateActiveScene()
        {
            var issues = new List<TidyValidationIssue>();
            var objects = Find<TidyObject>();
            var targets = Find<TidyPlacementTarget>();
            var manager = Find<TidyLevelManager>().FirstOrDefault();
            if (manager == null && objects.Count == 0 && targets.Count == 0)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Info, "This scene contains no Tidy objects, targets or Level Manager, so it is not validated as a gameplay level (for example a menu scene)."));
                return issues;
            }

            ValidateSceneSetup(issues, manager);
            ValidateObjects(issues, objects);
            ValidateTargets(issues, targets, objects);
            ValidateSortability(issues, objects, targets);
            if (manager != null && manager.Level != null)
            {
                ValidateLevelDefinition(issues, manager.Level, objects, targets);
            }

            if (issues.Count == 0)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Info, "No problems found. The level is ready to play."));
            }

            return issues;
        }

        public static List<TidyValidationIssue> ValidateProject()
        {
            var issues = new List<TidyValidationIssue>();
            ValidateDefinitionIds(issues);
            ValidateCategories(issues);
            ValidateFrameworkSettings(issues);
            return issues;
        }

        static void ValidateSceneSetup(List<TidyValidationIssue> issues, TidyLevelManager manager)
        {
            if (manager == null)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, "The scene has no Tidy Level Manager. Objectives, scoring and saving will not run.", null, "Add Level Systems", () => EditorApplication.ExecuteMenuItem(TidyEditorUtility.GameObjectMenuRoot + "Level Systems")));
            }
            else if (manager.Level == null)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, "The Level Manager has no Level Definition. The level completes when every counting target is complete and progress is saved under the scene name.", manager));
            }

            if (Find<TidyInteractor>().Count == 0)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, "No Tidy Interactor found. Add the Tidy Player prefab so the player can pick objects up.", null, "Add Player", () => EditorApplication.ExecuteMenuItem(TidyEditorUtility.GameObjectMenuRoot + "Player")));
            }

            if (Find<TidyInputReader>().Count == 0)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, "No Tidy Input Reader found. The player cannot receive input."));
            }

            if (Find<TidyOutlineHighlighter>().Count == 0)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, "No Outline Highlighter found. Hover and hint highlights will be invisible."));
            }

            if (Find<EventSystem>().Count == 0 && Find<Canvas>().Count > 0)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, "The scene has UI but no EventSystem. Buttons will not respond."));
            }

            var scenePath = SceneManager.GetActiveScene().path;
            if (!string.IsNullOrEmpty(scenePath) && EditorBuildSettings.scenes.All(scene => scene.path != scenePath))
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, "The scene is not in the build scene list, so it cannot be loaded from Level Select.", null, "Add To Build", () => AddSceneToBuild(scenePath)));
            }
        }

        static void ValidateObjects(List<TidyValidationIssue> issues, List<TidyObject> objects)
        {
            var seenIds = new Dictionary<string, TidyObject>();
            foreach (var tidyObject in objects)
            {
                if (tidyObject.Definition == null)
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, tidyObject.name + " has no Object Definition, so no rule can match it.", tidyObject));
                }

                if (tidyObject.GetComponentInChildren<Collider>(true) == null)
                {
                    var captured = tidyObject;
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, tidyObject.name + " has no collider and cannot be aimed at.", tidyObject, "Fit Collider", () => TidyObjectSetupUtility.FitBoxCollider(captured.gameObject)));
                }

                if (tidyObject.GetComponentInChildren<Renderer>(true) == null)
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, tidyObject.name + " has no renderer.", tidyObject));
                }

                var serialized = new SerializedObject(tidyObject);
                var id = serialized.FindProperty("instanceId").stringValue;
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (seenIds.TryGetValue(id, out var other))
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, "Duplicate object id '" + id + "' on " + tidyObject.name + " and " + other.name + ".", tidyObject, "Generate IDs", TidyIdGenerator.GenerateSceneIds));
                }
                else
                {
                    seenIds[id] = tidyObject;
                }
            }
        }

        static void ValidateTargets(List<TidyValidationIssue> issues, List<TidyPlacementTarget> targets, List<TidyObject> objects)
        {
            var seenIds = new Dictionary<string, TidyPlacementTarget>();
            foreach (var target in targets)
            {
                if (target.GetComponentsInChildren<Collider>(true).All(collider => collider.GetComponentInParent<TidyObject>() != null))
                {
                    var captured = target;
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, target.name + " has no aim collider. Players cannot point at it to place objects.", target, "Add Trigger", () => TidyEditorUtility.EnsureAimTrigger(captured.gameObject, new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.3f, 0.4f))));
                }

                if (!target.Acceptance.IsEmpty && !objects.Any(target.Belongs))
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, target.name + " accepts '" + target.Acceptance.Describe() + "' but no object in the scene matches.", target));
                }

                var matching = objects.Count(target.Belongs);
                if (target.CompletionMode == TidyCompletionMode.WhenAllMatchingObjectsPlaced && target.Capacity < matching)
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, target.name + " must hold all " + matching + " matching objects but its capacity is " + target.Capacity + ".", target));
                }

                foreach (var occupant in target.InitialOccupants)
                {
                    if (occupant == null)
                    {
                        issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, target.name + " has an empty initial occupant entry.", target));
                    }
                }

                var id = target.TargetId;
                if (seenIds.TryGetValue(id, out var other))
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, "Duplicate target id '" + id + "' on " + target.name + " and " + other.name + ".", target, "Generate IDs", TidyIdGenerator.GenerateSceneIds));
                }
                else
                {
                    seenIds[id] = target;
                }
            }
        }

        static void ValidateSortability(List<TidyValidationIssue> issues, List<TidyObject> objects, List<TidyPlacementTarget> targets)
        {
            var orphans = objects.Where(tidyObject => tidyObject.Definition != null && tidyObject.IsSortable && !tidyObject.CountsAsClutter && !targets.Any(target => target.Belongs(tidyObject) && target.IsCompatible(tidyObject))).ToList();
            if (orphans.Count == 0)
            {
                return;
            }

            var names = string.Join(", ", orphans.Take(5).Select(tidyObject => tidyObject.name));
            issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, orphans.Count + " sortable object(s) have no compatible target that accepts them: " + names + (orphans.Count > 5 ? "..." : string.Empty), orphans[0]));
        }

        static void ValidateLevelDefinition(List<TidyValidationIssue> issues, TidyLevelDefinition level, List<TidyObject> objects, List<TidyPlacementTarget> targets)
        {
            if (level.Objectives.Count == 0)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, level.name + " has no objectives. Completion falls back to target completion.", level));
            }

            if (level.Objectives.Any(entry => entry == null || entry.Objective == null))
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, level.name + " has an empty objective slot.", level));
            }

            if (level.Objectives.All(entry => entry == null || entry.Optional))
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, level.name + " has no required objective.", level));
            }

            foreach (var entry in level.Objectives)
            {
                if (entry?.Objective is TidyPlaceObjectsObjective place)
                {
                    var serialized = new SerializedObject(place);
                    var required = serialized.FindProperty("requiredCount").intValue;
                    var available = objects.Count(place.Counts);
                    if (available == 0)
                    {
                        issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, place.name + " counts no object in this scene.", place));
                    }
                    else if (required > available)
                    {
                        issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, place.name + " requires " + required + " objects but only " + available + " exist.", place));
                    }

                    var targetIds = serialized.FindProperty("restrictToTargetIds");
                    for (var index = 0; index < targetIds.arraySize; index++)
                    {
                        var targetId = targetIds.GetArrayElementAtIndex(index).stringValue;
                        if (targets.All(target => target.TargetId != targetId))
                        {
                            issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, place.name + " references missing target id '" + targetId + "'.", place));
                        }
                    }
                }
                else if (entry?.Objective is TidyCompleteTargetsObjective complete)
                {
                    var serialized = new SerializedObject(complete);
                    var ids = serialized.FindProperty("targetIds");
                    for (var index = 0; index < ids.arraySize; index++)
                    {
                        var targetId = ids.GetArrayElementAtIndex(index).stringValue;
                        if (targets.All(target => target.TargetId != targetId))
                        {
                            issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, complete.name + " references missing target id '" + targetId + "'.", complete));
                        }
                    }
                }
            }

            if (level.Tutorial != null)
            {
                foreach (var step in level.Tutorial.Steps)
                {
                    if (!string.IsNullOrEmpty(step.TargetId) && targets.All(target => target.TargetId != step.TargetId))
                    {
                        issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, "Tutorial step '" + step.Message + "' references missing target id '" + step.TargetId + "'.", level.Tutorial));
                    }
                }
            }

            if (!string.IsNullOrEmpty(level.SceneName) && level.SceneName != SceneManager.GetActiveScene().name)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, level.name + " points to scene '" + level.SceneName + "' but the open scene is '" + SceneManager.GetActiveScene().name + "'.", level));
            }

            var catalog = Resources.Load<TidyFrameworkSettings>(TidyFrameworkSettings.ResourcesPath)?.LevelCatalog;
            if (catalog != null && !catalog.Levels.Contains(level))
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Info, level.name + " is not listed in the level catalog, so it will not appear in Level Select.", catalog));
            }
        }

        static void ValidateDefinitionIds(List<TidyValidationIssue> issues)
        {
            var seen = new Dictionary<string, TidyDefinition>();
            foreach (var guid in AssetDatabase.FindAssets("t:TidyDefinition"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<TidyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition == null)
                {
                    continue;
                }

                var key = definition.GetType().Name + ":" + definition.Id;
                if (seen.TryGetValue(key, out var other))
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, "Duplicate " + definition.GetType().Name + " id '" + definition.Id + "' on " + definition.name + " and " + other.name + ".", definition, "Generate IDs", TidyIdGenerator.GenerateDefinitionIds));
                }
                else
                {
                    seen[key] = definition;
                }

                if (definition is TidyObjectDefinition objectDefinition && objectDefinition.Category == null)
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, objectDefinition.name + " has no category.", objectDefinition));
                }
            }
        }

        static void ValidateCategories(List<TidyValidationIssue> issues)
        {
            var definitions = AssetDatabase.FindAssets("t:TidyObjectDefinition").Select(guid => AssetDatabase.LoadAssetAtPath<TidyObjectDefinition>(AssetDatabase.GUIDToAssetPath(guid))).Where(definition => definition != null).ToList();
            foreach (var guid in AssetDatabase.FindAssets("t:TidyCategoryDefinition"))
            {
                var category = AssetDatabase.LoadAssetAtPath<TidyCategoryDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (category != null && !definitions.Any(definition => definition.IsInCategory(category)))
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Info, "Category " + category.name + " has no object definitions.", category));
                }
            }
        }

        static void ValidateFrameworkSettings(List<TidyValidationIssue> issues)
        {
            var settings = Resources.Load<TidyFrameworkSettings>(TidyFrameworkSettings.ResourcesPath);
            if (settings == null)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, "No TidyUpFrameworkSettings asset found in a Resources folder. Defaults are used and Level Select is empty."));
                return;
            }

            if (settings.LevelCatalog == null)
            {
                issues.Add(new TidyValidationIssue(TidyIssueSeverity.Warning, "Framework settings have no level catalog.", settings));
                return;
            }

            foreach (var level in settings.LevelCatalog.Levels)
            {
                if (level == null)
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, "The level catalog contains an empty entry.", settings.LevelCatalog));
                    continue;
                }

                if (EditorBuildSettings.scenes.All(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path) != level.SceneName))
                {
                    issues.Add(new TidyValidationIssue(TidyIssueSeverity.Error, level.name + " uses scene '" + level.SceneName + "' which is not in the build scene list.", level));
                }
            }
        }

        static void AddSceneToBuild(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static List<T> Find<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        }
    }
}
