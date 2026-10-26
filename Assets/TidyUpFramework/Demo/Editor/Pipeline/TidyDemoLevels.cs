using System.Collections.Generic;
using System.Linq;
using TidyUpFramework.Input;
using TidyUpFramework.Levels;
using TidyUpFramework.Objectives;
using TidyUpFramework.Objects;
using TidyUpFramework.Rules;
using TidyUpFramework.Tutorial;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyDemoLevels
    {
        public static void Build(TidyDemoContext context)
        {
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.Levels);
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.Objectives);
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.Rules);
            var levels = new List<TidyLevelDefinition>();
            TidyLevelDefinition previous = null;
            foreach (var builder in LevelBuilders)
            {
                var level = builder(context, previous);
                if (level == null)
                {
                    continue;
                }

                levels.Add(level);
                previous = level;
            }

            context.Catalog = TidyAssetFactory.CreateOrLoad<TidyLevelCatalog>(TidyDemoPaths.Levels + "/LevelCatalog.asset");
            context.Catalog.Configure(levels, TidyDemoPaths.MainMenuScene);
            EditorUtility.SetDirty(context.Catalog);
            AssetDatabase.SaveAssets();
        }

        public delegate TidyLevelDefinition LevelBuilder(TidyDemoContext context, TidyLevelDefinition previous);

        public static readonly List<LevelBuilder> LevelBuilders = new List<LevelBuilder> { BuildLibrary, TidyStoreLevel.Build, TidyWarehouseLevel.Build, TidyRoomLevel.Build };

        public static TidyLevelDefinition BuildLibrary(TidyDemoContext context, TidyLevelDefinition previous)
        {
            if (!context.HasDefinition("book_arcane_basics"))
            {
                return null;
            }

            var subjects = new[] { context.Category("SpellBooks"), context.Category("Botany"), context.Category("History"), context.Category("StarCharts") };
            var books = Place(context, "Library_Books", "Shelve the books by subject {current}/{target}", "Every subject has its own bookcase. Brass plaques show which is which.", 150, Filter(subjects), 0);
            var encyclopaedia = Complete(context, "Library_Encyclopaedia", "Order the Encyclopaedia Arcana", "Volumes I to VIII belong together, in order.", 200, "shelf_encyclopaedia");
            var potions = Place(context, "Library_Potions", "Sort the potions by colour {current}/{target}", "Each rack holds one colour.", 100, Filter(context.Category("Potions")), 0);
            var scrolls = Place(context, "Library_Scrolls", "Gather the scrolls {current}/{target}", "Scrolls go into the wicker baskets.", 80, Filter(context.Category("Scrolls")), 0);
            var candles = Place(context, "Library_Candles", "Light the candelabras {current}/{target}", "Candles belong on the brass candelabras.", 120, Filter(context.Category("Candles")), 0);
            var curios = Place(context, "Library_Curiosities", "Display the curiosities {current}/{target}", "Crystals on the pedestals, ink and quill on the writing desk.", 120, Filter(context.Category("Curiosities")), 0);
            var time = TidyAssetFactory.CreateOrLoad<TidyTimeLimitObjective>(TidyDemoPaths.Objectives + "/Objective_Library_Time.asset");
            time.Configure(600f);
            time.ConfigurePresentation("library_time", "Finish within {limit}", "A tidy librarian is a quick librarian.", 250);
            time.ConfigureCounterVisibility(true);
            EditorUtility.SetDirty(time);

            var level = TidyAssetFactory.CreateOrLoad<TidyLevelDefinition>(TidyDemoPaths.Levels + "/Level_01_ArcaneLibrary.asset");
            level.ConfigureIdentity("arcane_library", "The Arcane Library", "A storm of books has swept through the reading hall. Shelve every volume by subject, put the encyclopaedia in order and sort the potions before the scholars return.", TidyDemoPaths.LibraryScene, TidyDifficulty.Easy, new Color32(170, 120, 255, 255));
            level.ConfigureObjectives(new[]
            {
                Entry(books, false),
                Entry(encyclopaedia, false),
                Entry(potions, false),
                Entry(scrolls, false),
                Entry(candles, true),
                Entry(curios, true),
                Entry(time, true)
            });
            level.ConfigureTiming(0f, false, TidyStarMetric.Score, new Vector2(1300f, 1900f));
            level.ConfigureSystems(context.Score, context.Combo, context.Hints, LibraryTutorial(context));
            level.ConfigureAudio(context.Cue("Music_Library"), context.Cue("Ambience_Library"));
            level.ConfigureProgression(new TidyUnlockRequirement { UnlockedByDefault = true }, 100, "Librarian's Badge");
            EditorUtility.SetDirty(level);
            return level;
        }

        static TidyTutorialDefinition LibraryTutorial(TidyDemoContext context)
        {
            var tutorial = TidyAssetFactory.CreateOrLoad<TidyTutorialDefinition>(TidyDemoPaths.Levels + "/Tutorial_ArcaneLibrary.asset");
            tutorial.Configure("tutorial_library", true, new[]
            {
                Step("Welcome to the Arcane Library. Look around with {Look}.", TidyInputAction.Look, TidyTutorialCompletion.LookAround),
                Step("Walk between the bookcases with {Move}.", TidyInputAction.Move, TidyTutorialCompletion.MovePlayer),
                Step("Books are scattered everywhere. Aim at a glowing spell book and press {Interact} to pick it up.", TidyInputAction.Interact, TidyTutorialCompletion.PickUpMatching, Filter(context.Category("SpellBooks"))),
                Step("Spell books live on the purple bookcase. Aim at its shelf and press {Interact}. The ghost shows where it will go.", TidyInputAction.Interact, TidyTutorialCompletion.PlaceCorrectly, null, "shelf_spell_am"),
                Step("Pick up any object and turn it with {Rotate}. Press {Inspect} to take a closer look.", TidyInputAction.Inspect, TidyTutorialCompletion.Inspect),
                Step("Not sure where something goes? Press {Hint}.", TidyInputAction.Hint, TidyTutorialCompletion.UseHint),
                Step("Correct placements in quick succession build a combo. Tidy up the whole hall!", TidyInputAction.Interact, TidyTutorialCompletion.PressContinue)
            });
            EditorUtility.SetDirty(tutorial);
            return tutorial;
        }

        public static TidyTutorialStep Step(string message, TidyInputAction prompt, TidyTutorialCompletion completion, TidyMatchFilter filter = null, string targetId = null)
        {
            return new TidyTutorialStep
            {
                Message = message,
                PromptAction = prompt,
                ShowPrompt = true,
                Completion = completion,
                ObjectFilter = filter ?? new TidyMatchFilter(),
                TargetId = targetId,
                DurationSeconds = 3f,
                HighlightObjects = filter != null,
                HighlightTarget = !string.IsNullOrEmpty(targetId),
                ShowArrow = filter != null || !string.IsNullOrEmpty(targetId)
            };
        }

        public static TidyMatchFilter Filter(params TidyCategoryDefinition[] categories)
        {
            return TidyMatchFilter.Create(categories.Where(category => category != null), null);
        }

        public static TidyMatchFilter FilterWithTags(IEnumerable<TidyCategoryDefinition> categories, params TidyTagDefinition[] tags)
        {
            return TidyMatchFilter.Create(categories, tags);
        }

        public static TidyPlaceObjectsObjective Place(TidyDemoContext context, string key, string title, string description, int bonus, TidyMatchFilter filter, int count, params string[] targetIds)
        {
            var objective = TidyAssetFactory.CreateOrLoad<TidyPlaceObjectsObjective>(TidyDemoPaths.Objectives + "/Objective_" + key + ".asset");
            objective.Configure(filter, count, true, targetIds);
            objective.ConfigurePresentation(TidyUpFramework.Editor.TidyEditorUtility.Slug(key), title, description, bonus);
            EditorUtility.SetDirty(objective);
            return objective;
        }

        public static TidyCompleteTargetsObjective Complete(TidyDemoContext context, string key, string title, string description, int bonus, params string[] targetIds)
        {
            var objective = TidyAssetFactory.CreateOrLoad<TidyCompleteTargetsObjective>(TidyDemoPaths.Objectives + "/Objective_" + key + ".asset");
            objective.Configure(targetIds, null);
            objective.ConfigurePresentation(TidyUpFramework.Editor.TidyEditorUtility.Slug(key), title, description, bonus);
            EditorUtility.SetDirty(objective);
            return objective;
        }

        public static TidyObjectiveEntry Entry(TidyObjectiveDefinition objective, bool optional)
        {
            return new TidyObjectiveEntry { Objective = objective, Optional = optional };
        }

        public static TidySortKeyRangeRule AlphabeticalRule(string key, char first, char last)
        {
            var rule = TidyAssetFactory.CreateOrLoad<TidySortKeyRangeRule>(TidyDemoPaths.Rules + "/Rule_" + key + ".asset");
            rule.Configure(first, last);
            rule.AssignDescription("Titles " + first + "-" + last);
            EditorUtility.SetDirty(rule);
            return rule;
        }
    }
}
