using System.Collections.Generic;
using System.Linq;
using TidyUpFramework.Levels;
using TidyUpFramework.Objectives;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyStoreLevel
    {
        public const string LevelPath = TidyDemoPaths.Levels + "/Level_02_ToyStore.asset";
        public const int ShelfGoal = 60;
        public const int StandGoal = 24;
        public const int StockGoal = 110;
        public const int ScoreGoal = 2000;

        public static IEnumerable<string> ShelfIds => TidyStoreContent.Species.Select(species => "store_shelf_" + species.ToString().ToLowerInvariant());
        public static IEnumerable<string> StandIds => TidyStoreContent.Colors.Select(color => "store_stand_" + color.ToLowerInvariant());
        public static readonly string[] SetIds = { "store_set_0", "store_set_1", "store_set_2" };
        public static readonly string[] TripletIds = { "store_triplet_0", "store_triplet_1", "store_triplet_2", "store_triplet_3" };
        public static IEnumerable<string> BinIds => TidyStoreContent.PodColors.Select(color => "store_pod_bin_" + color.ToLowerInvariant());

        public static TidyLevelDefinition Build(TidyDemoContext context, TidyLevelDefinition previous)
        {
            if (!context.HasDefinition(TidyStoreContent.PalId(TidyPalSpecies.Bear, "Red")))
            {
                return null;
            }

            var pals = context.Category(TidyStoreContent.ParentCategory);
            var shelves = TidyDemoLevels.Place(context, "Store_Shelves", "Shelve the Pals by family {current}/{target}", "Bears, bunnies, cats, frogs and robots each have their own shelving unit. Match the sign above it.", 300, TidyDemoLevels.Filter(pals), ShelfGoal, ShelfIds.ToArray());
            var stand = TidyDemoLevels.Place(context, "Store_RainbowStand", "Sort the Rainbow Stand by colour {current}/{target}", "Every slice of the tiered stand shows one colour. Any family can go there.", 250, TidyDemoLevels.Filter(pals), StandGoal, StandIds.ToArray());
            var sets = TidyDemoLevels.Complete(context, "Store_FamilySets", "Build 3 Family Sets", "Fill each set pedestal with six Pals of the same family.", 250, SetIds);
            var stock = TidyDemoLevels.Place(context, "Store_Stock", "Stock {target} Pebble Pals {current}/{target}", "Get the shop floor clear before opening time.", 400, TidyDemoLevels.Filter(pals), StockGoal);
            var pods = TidyDemoLevels.Place(context, "Store_Pods", "Sort the Pal Pods {current}/{target}", "Mystery capsules go into the bin with the matching colour.", 150, TidyDemoLevels.Filter(context.Category(TidyStoreContent.PodCategory)), 0, BinIds.ToArray());
            var triplets = TidyDemoLevels.Complete(context, "Store_Triplets", "Match the Triplets", "Each tray on the display table wants three identical Pals.", 200, TripletIds);
            var score = ReachScore();
            var time = TimeLimit();

            var level = TidyAssetFactory.CreateOrLoad<TidyLevelDefinition>(LevelPath);
            level.ConfigureIdentity("toy_store", "Pebble Pals Toy Shop", "A delivery of Pebble Pals burst open all over the shop floor. Shelve hundreds of collectibles by family, sort the rainbow stand by colour and build matching sets before the doors open.", TidyDemoPaths.StoreScene, TidyDifficulty.Normal, new Color32(255, 140, 170, 255));
            level.ConfigureObjectives(new[]
            {
                TidyDemoLevels.Entry(shelves, false),
                TidyDemoLevels.Entry(stand, false),
                TidyDemoLevels.Entry(sets, false),
                TidyDemoLevels.Entry(stock, false),
                TidyDemoLevels.Entry(pods, true),
                TidyDemoLevels.Entry(triplets, true),
                TidyDemoLevels.Entry(score, true),
                TidyDemoLevels.Entry(time, true)
            });
            level.ConfigureTiming(0f, false, TidyStarMetric.Score, new Vector2(1700f, 2500f));
            level.ConfigureSystems(context.Score, context.Combo, context.Hints, null);
            level.ConfigureAudio(context.Cue("Music_Store"), context.Cue("Ambience_Store"));
            level.ConfigureProgression(new TidyUnlockRequirement { UnlockedByDefault = previous == null, RequiredLevel = previous }, 150, "Shopkeeper's Apron");
            EditorUtility.SetDirty(level);
            return level;
        }

        static TidyReachScoreObjective ReachScore()
        {
            var objective = TidyAssetFactory.CreateOrLoad<TidyReachScoreObjective>(TidyDemoPaths.Objectives + "/Objective_Store_Score.asset");
            objective.Configure(ScoreGoal);
            objective.ConfigurePresentation("store_score", "Reach {target} points", "Chain correct placements to build combos.", 200);
            EditorUtility.SetDirty(objective);
            return objective;
        }

        static TidyTimeLimitObjective TimeLimit()
        {
            var objective = TidyAssetFactory.CreateOrLoad<TidyTimeLimitObjective>(TidyDemoPaths.Objectives + "/Objective_Store_Time.asset");
            objective.Configure(1200f);
            objective.ConfigurePresentation("store_time", "Open the doors within {limit}", "Customers are already queueing outside.", 250);
            objective.ConfigureCounterVisibility(true);
            EditorUtility.SetDirty(objective);
            return objective;
        }
    }
}
