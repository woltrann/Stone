using TidyUpFramework.Levels;
using TidyUpFramework.Objectives;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseLevel
    {
        public const string AssetPath = TidyDemoPaths.Levels + "/Level_03_Warehouse.asset";
        public const string ToolWallId = "tool_wall";

        public static TidyLevelDefinition Build(TidyDemoContext context, TidyLevelDefinition previous)
        {
            if (!context.HasDefinition(TidyWarehouseContent.MarkerId))
            {
                return null;
            }

            var parcels = TidyDemoLevels.Place(context, "Warehouse_Parcels", "Sort parcels by label colour {current}/{target}", "Red, blue, green and yellow labels each have their own bin along the east wall.", 150, TidyDemoLevels.Filter(context.Category("DepotParcels")), 0);
            var cartons = TidyDemoLevels.Place(context, "Warehouse_Cartons", "Stack cartons by size {current}/{target}", "Small, medium and large cartons each have a pallet. Read the stencilled S, M or L.", 200, TidyDemoLevels.Filter(context.Category("DepotCartons")), 0);
            var crates = TidyDemoLevels.Place(context, "Warehouse_Crates", "Rack the crates {current}/{target}", "Fragile glassware on the low beam, spare parts on the middle beam.", 180, TidyDemoLevels.Filter(context.Category("DepotCrates")), 0);
            var tools = TidyDemoLevels.Complete(context, "Warehouse_Tools", "Hang every tool on the tool wall", "Each hook on the pegboard holds one tool.", 120, ToolWallId);
            var hazmat = TidyDemoLevels.Place(context, "Warehouse_Hazmat", "Fence in the drums and fuel cans {current}/{target}", "Hazardous goods belong inside the yellow cage.", 160, TidyDemoLevels.Filter(context.Category("DepotHazmat")), 0);
            var totes = TidyDemoLevels.Place(context, "Warehouse_Totes", "Line up the totes on the conveyor {current}/{target}", "Empty picking totes ride back on the roller conveyor.", 90, TidyDemoLevels.Filter(context.Category("DepotTotes")), 0);
            var careful = TidyAssetFactory.CreateOrLoad<TidyNoMistakesObjective>(TidyDemoPaths.Objectives + "/Objective_Warehouse_Careful.asset");
            TidySerialization.SetInt(careful, "allowedMistakes", 3);
            careful.ConfigurePresentation("warehouse_careful", "Make no more than 3 mistakes", "A careful clerk ships it right the first time.", 200);
            EditorUtility.SetDirty(careful);
            var express = TidyAssetFactory.CreateOrLoad<TidyTimeLimitObjective>(TidyDemoPaths.Objectives + "/Objective_Warehouse_Express.asset");
            express.Configure(600f);
            express.ConfigurePresentation("warehouse_express", "Express service: finish within {limit}", "Beat the evening truck with time to spare.", 250);
            express.ConfigureCounterVisibility(true);
            EditorUtility.SetDirty(express);

            var level = TidyAssetFactory.CreateOrLoad<TidyLevelDefinition>(AssetPath);
            level.ConfigureIdentity("warehouse", "Northwind Parcel Depot", "The morning delivery was dumped all over the depot floor. Sort parcels by label colour, stack cartons by size, rack the crates and fence in the hazardous goods before the evening truck arrives.", TidyDemoPaths.WarehouseScene, TidyDifficulty.Hard, new Color32(240, 150, 60, 255));
            level.ConfigureObjectives(new[]
            {
                TidyDemoLevels.Entry(parcels, false),
                TidyDemoLevels.Entry(cartons, false),
                TidyDemoLevels.Entry(crates, false),
                TidyDemoLevels.Entry(tools, false),
                TidyDemoLevels.Entry(hazmat, false),
                TidyDemoLevels.Entry(totes, true),
                TidyDemoLevels.Entry(careful, true),
                TidyDemoLevels.Entry(express, true)
            });
            level.ConfigureTiming(900f, true, TidyStarMetric.TimeSeconds, new Vector2(540f, 360f));
            level.ConfigureSystems(context.Score, context.Combo, context.Hints, null);
            level.ConfigureAudio(context.Cue("Music_Warehouse"), context.Cue("Ambience_Store"));
            level.ConfigureProgression(new TidyUnlockRequirement { UnlockedByDefault = previous == null, RequiredLevel = previous }, 200, "Depot Supervisor Badge");
            EditorUtility.SetDirty(level);
            return level;
        }
    }
}
