using System.Collections.Generic;
using TidyUpFramework.Objects;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseContent
    {
        public const string Theme = "Warehouse";
        public const string MeshFolder = TidyDemoPaths.Meshes + "/" + Theme;
        public const string PrefabFolder = TidyDemoPaths.Prefabs + "/" + Theme;
        public const string MarkerId = "wh_parcel_red_s";

        public static readonly string[] ParcelSizes = { "s", "m" };
        public static readonly string[] CartonSizes = { "s", "m", "l" };
        public static readonly string[] CartonFinishes = { "kraft", "white" };
        public static readonly string[] ToolIds = { "wh_wrench_large", "wh_wrench_small", "wh_hammer", "wh_mallet", "wh_screwdriver_red", "wh_screwdriver_yellow", "wh_tape_measure", "wh_pliers" };

        static readonly Vector3[] ParcelDimensions = { new Vector3(0.26f, 0.16f, 0.2f), new Vector3(0.36f, 0.22f, 0.28f) };
        static readonly Vector3[] CartonDimensions = { new Vector3(0.32f, 0.24f, 0.3f), new Vector3(0.44f, 0.32f, 0.4f), new Vector3(0.56f, 0.42f, 0.46f) };
        static readonly float[] CartonSizeValues = { 2f, 4f, 6f };
        static readonly string[] SizeTags = { "Small", "Medium", "Large" };

        public static string ParcelId(string color, string size) => "wh_parcel_" + color.ToLowerInvariant() + "_" + size;
        public static string CartonId(string size, string finish) => "wh_carton_" + size + "_" + finish;
        public static string ToteId(string color) => "wh_tote_" + color.ToLowerInvariant();

        public static void Build(TidyDemoContext context)
        {
            TidyAssetFactory.EnsureFolder(MeshFolder);
            TidyAssetFactory.EnsureFolder(PrefabFolder);
            TidyWarehouseArt.Build(context);
            BuildCategories(context);
            BuildParcels(context);
            BuildCartons(context);
            BuildCrates(context);
            BuildTotes(context);
            BuildTools(context);
            BuildHazmat(context);
            BuildFixtures(context);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        static void BuildCategories(TidyDemoContext context)
        {
            var freight = TidyDemoDataBuilder.Category(context, "DepotFreight", "Freight", new Color32(196, 150, 96, 255));
            TidyDemoDataBuilder.Category(context, "DepotParcels", "Parcels", new Color32(214, 170, 112, 255), freight);
            TidyDemoDataBuilder.Category(context, "DepotCartons", "Cartons", new Color32(176, 128, 80, 255), freight);
            var crates = TidyDemoDataBuilder.Category(context, "DepotCrates", "Crates", new Color32(190, 140, 86, 255));
            TidyDemoDataBuilder.Category(context, "DepotFragile", "Fragile Goods", new Color32(214, 74, 64, 255), crates);
            TidyDemoDataBuilder.Category(context, "DepotSpareParts", "Spare Parts", new Color32(70, 120, 214, 255), crates);
            TidyDemoDataBuilder.Category(context, "DepotTotes", "Totes", new Color32(60, 170, 168, 255));
            TidyDemoDataBuilder.Category(context, "DepotTools", "Tools", new Color32(236, 132, 54, 255));
            var hazmat = TidyDemoDataBuilder.Category(context, "DepotHazmat", "Hazmat", new Color32(240, 196, 66, 255));
            TidyDemoDataBuilder.Category(context, "DepotDrums", "Drums", new Color32(70, 120, 214, 255), hazmat);
            TidyDemoDataBuilder.Category(context, "DepotCanisters", "Fuel Cans", new Color32(214, 74, 64, 255), hazmat);
        }

        static void BuildParcels(TidyDemoContext context)
        {
            for (var sizeIndex = 0; sizeIndex < ParcelSizes.Length; sizeIndex++)
            {
                var mesh = TidyWarehouseModels.Parcel(MeshFolder, ParcelSizes[sizeIndex].ToUpperInvariant(), ParcelDimensions[sizeIndex], sizeIndex == 0 ? 0.11f : 0.15f);
                foreach (var color in TidyWarehouseArt.LabelColors)
                {
                    var id = ParcelId(color, ParcelSizes[sizeIndex]);
                    var display = color + " Label Parcel" + (sizeIndex == 0 ? " (Small)" : " (Medium)");
                    var tags = new[] { context.Tag(color), context.Tag(SizeTags[sizeIndex]), context.Tag("Boxy") };
                    var definition = TidyDemoDataBuilder.Definition(context, Theme, id, display, context.Category("DepotParcels"), tags, sizeIndex == 0 ? 1.5f : 3f, sizeIndex == 0 ? 0.6f : 1.4f, 0, display, 0, false, false, 10, "The label colour tells you which bin it ships from.");
                    Interactive(id, mesh, new[] { context.Material("Cardboard"), context.Material("Tape"), context.Material("Warehouse_Label_" + color) }, definition, TidyColliderShape.Box);
                }
            }
        }

        static void BuildCartons(TidyDemoContext context)
        {
            for (var sizeIndex = 0; sizeIndex < CartonSizes.Length; sizeIndex++)
            {
                var mesh = TidyWarehouseModels.Carton(MeshFolder, CartonSizes[sizeIndex].ToUpperInvariant(), CartonDimensions[sizeIndex]);
                foreach (var finish in CartonFinishes)
                {
                    var id = CartonId(CartonSizes[sizeIndex], finish);
                    var display = (finish == "kraft" ? "Kraft" : "White") + " Carton " + CartonSizes[sizeIndex].ToUpperInvariant();
                    var tags = new List<TidyTagDefinition> { context.Tag(SizeTags[sizeIndex]), context.Tag("Boxy"), context.Tag(finish == "kraft" ? "Brown" : "White") };
                    if (sizeIndex == 2)
                    {
                        tags.Add(context.Tag("Heavy"));
                    }

                    var body = context.Material(finish == "kraft" ? "Cardboard" : "Warehouse_CardboardLight");
                    var definition = TidyDemoDataBuilder.Definition(context, Theme, id, display, context.Category("DepotCartons"), tags, CartonSizeValues[sizeIndex], 2f + sizeIndex * 4f, 0, display, 0, true, false, 12, "Stencilled size code " + CartonSizes[sizeIndex].ToUpperInvariant() + ". Stack it on the matching pallet.");
                    Interactive(id, mesh, new[] { body, context.Material("Tape"), context.Material("Warehouse_Print_" + CartonSizes[sizeIndex].ToUpperInvariant()) }, definition, TidyColliderShape.Box);
                }
            }
        }

        static void BuildCrates(TidyDemoContext context)
        {
            var small = TidyWarehouseModels.Crate(MeshFolder, "Small", new Vector3(0.52f, 0.36f, 0.4f));
            var large = TidyWarehouseModels.Crate(MeshFolder, "Large", new Vector3(0.74f, 0.48f, 0.52f));
            var wood = context.Material("Wood_Crate");
            CreateCrate(context, "wh_crate_fragile_small", "Glassware Crate (Small)", "DepotFragile", small, new[] { wood, context.Material("Warehouse_StencilFragile") }, false, "Fragile", "Small", "Glass inside. Keep it low on the rack.");
            CreateCrate(context, "wh_crate_fragile_large", "Glassware Crate (Large)", "DepotFragile", large, new[] { wood, context.Material("Warehouse_StencilFragile") }, false, "Fragile", "Large", "Rattles politely. Keep it low on the rack.");
            CreateCrate(context, "wh_crate_parts_small", "Spare Parts Crate (Small)", "DepotSpareParts", small, new[] { wood, context.Material("Warehouse_StencilParts") }, false, "Heavy", "Small", "Bolts, bearings and spare gears.");
            CreateCrate(context, "wh_crate_parts_large", "Spare Parts Crate (Large)", "DepotSpareParts", large, new[] { wood, context.Material("Warehouse_StencilParts") }, true, "Heavy", "Large", "Heavy machine parts. It moves with real physics.");
        }

        static void CreateCrate(TidyDemoContext context, string id, string display, string category, Mesh mesh, Material[] materials, bool physics, string trait, string size, string inspect)
        {
            var tags = new[] { context.Tag("Brown"), context.Tag(trait), context.Tag(size), context.Tag("Boxy") };
            var definition = TidyDemoDataBuilder.Definition(context, Theme, id, display, context.Category(category), tags, size == "Large" ? 6f : 3f, size == "Large" ? 14f : 6f, trait == "Fragile" ? 2 : 1, display, 0, false, physics, 15, inspect);
            Interactive(id, mesh, materials, definition, TidyColliderShape.Box);
        }

        static void BuildTotes(TidyDemoContext context)
        {
            var mesh = TidyWarehouseModels.Tote(MeshFolder);
            foreach (var color in TidyWarehouseArt.LabelColors)
            {
                var id = ToteId(color);
                var display = color + " Tote";
                var definition = TidyDemoDataBuilder.Definition(context, Theme, id, display, context.Category("DepotTotes"), new[] { context.Tag(color), context.Tag("Medium"), context.Tag("Boxy") }, 3f, 1f, 0, display, 0, true, false, 8, "An empty picking tote. They line up on the conveyor.");
                Interactive(id, mesh, new[] { context.Material("Plastic_" + color), context.Material("Rubber_Dark") }, definition, TidyColliderShape.Box);
            }
        }

        static void BuildTools(TidyDemoContext context)
        {
            var steel = context.Material("Metal_Steel");
            var rubber = context.Material("Rubber_Dark");
            var wood = context.Material("Wood_Oak");
            Tool(context, "wh_wrench_large", "Large Spanner", TidyWarehouseModels.Wrench(MeshFolder, "Large", 0.3f), new[] { steel }, "Silver", "A 24 mm combination spanner.");
            Tool(context, "wh_wrench_small", "Small Spanner", TidyWarehouseModels.Wrench(MeshFolder, "Small", 0.2f), new[] { steel }, "Silver", "A 13 mm combination spanner.");
            Tool(context, "wh_hammer", "Claw Hammer", TidyWarehouseModels.Hammer(MeshFolder), new[] { steel, wood, rubber }, "Brown", "Pulls nails, drives nails, opens crates.");
            Tool(context, "wh_mallet", "Rubber Mallet", TidyWarehouseModels.Mallet(MeshFolder), new[] { rubber, wood }, "Brown", "For persuading pallet boards gently.");
            var screwdriver = TidyWarehouseModels.Screwdriver(MeshFolder);
            Tool(context, "wh_screwdriver_red", "Red Screwdriver", screwdriver, new[] { context.Material("Plastic_Red"), rubber, steel }, "Red", "Flat blade, red grip.");
            Tool(context, "wh_screwdriver_yellow", "Yellow Screwdriver", screwdriver, new[] { context.Material("Plastic_Yellow"), rubber, steel }, "Yellow", "Flat blade, yellow grip.");
            Tool(context, "wh_tape_measure", "Tape Measure", TidyWarehouseModels.TapeMeasure(MeshFolder), new[] { context.Material("Plastic_Yellow"), rubber, steel }, "Yellow", "Five metres of springy steel tape.");
            Tool(context, "wh_pliers", "Pliers", TidyWarehouseModels.Pliers(MeshFolder), new[] { steel, context.Material("Plastic_Red") }, "Red", "Combination pliers with red grips.");
        }

        static void Tool(TidyDemoContext context, string id, string display, Mesh mesh, Material[] materials, string colorTag, string inspect)
        {
            var tags = new List<TidyTagDefinition> { context.Tag("Small") };
            if (colorTag != "Silver")
            {
                tags.Add(context.Tag(colorTag));
            }

            var definition = TidyDemoDataBuilder.Definition(context, Theme, id, display, context.Category("DepotTools"), tags, 1f, 0.5f, 1, display, 0, false, false, 12, inspect + " It hangs on the tool wall.");
            Interactive(id, mesh, materials, definition, TidyColliderShape.Box);
        }

        static void BuildHazmat(TidyDemoContext context)
        {
            var drum = TidyWarehouseModels.Drum(MeshFolder);
            var stencil = context.Material("Warehouse_StencilHazmat");
            foreach (var color in new[] { "Blue", "Red" })
            {
                var id = "wh_drum_" + color.ToLowerInvariant();
                var display = color + " Drum";
                var definition = TidyDemoDataBuilder.Definition(context, Theme, id, display, context.Category("DepotDrums"), new[] { context.Tag(color), context.Tag("Large"), context.Tag("Heavy"), context.Tag("Round") }, 7f, 30f, 2, display, 0, false, true, 18, "A 200 litre drum. It rolls, tips and bumps with real physics.");
                Interactive(id, drum, new[] { context.Material("Warehouse_Drum" + color), context.Material("Metal_Steel"), stencil }, definition, TidyColliderShape.Convex);
            }

            var can = TidyWarehouseModels.JerryCan(MeshFolder);
            foreach (var color in new[] { "Red", "Green" })
            {
                var id = "wh_canister_" + color.ToLowerInvariant();
                var display = color + " Fuel Can";
                var definition = TidyDemoDataBuilder.Definition(context, Theme, id, display, context.Category("DepotCanisters"), new[] { context.Tag(color), context.Tag("Medium"), context.Tag("Heavy") }, 3f, 12f, 2, display, 0, false, true, 14, "Twenty litres, sloshing. Store it inside the hazmat cage.");
                Interactive(id, can, new[] { context.Material("Plastic_" + color), context.Material("Rubber_Dark") }, definition, TidyColliderShape.Convex);
            }
        }

        static void BuildFixtures(TidyDemoContext context)
        {
            Static(context, "WarehouseRackBay", TidyWarehouseFixtures.RackBay(MeshFolder), new[] { context.Material("Metal_Rack"), context.Material("Warehouse_BeamOrange"), context.Material("Metal_Steel") }, TidyColliderShape.Mesh);
            Static(context, "WarehousePallet", TidyWarehouseFixtures.Pallet(MeshFolder), new[] { context.Material("Wood_Crate") }, TidyColliderShape.Box);
            var bin = TidyWarehouseFixtures.Bin(MeshFolder);
            foreach (var color in TidyWarehouseArt.LabelColors)
            {
                Static(context, "WarehouseBin_" + color, bin, new[] { context.Material("Plastic_" + color), context.Material("Warehouse_SteelDark") }, TidyColliderShape.Mesh);
            }

            TidyWarehouseFixtures.BinFill(MeshFolder);
            TidyWarehouseFixtures.Hook(MeshFolder);
            TidyWarehouseFixtures.Bulb(MeshFolder);
            TidyWarehouseFixtures.DeskScreen(MeshFolder);
            Static(context, "WarehouseWorkbench", TidyWarehouseFixtures.Workbench(MeshFolder, out var benchTop), new[] { context.Material("Warehouse_SteelDark"), context.Material("Wood_Oak"), context.Material("Metal_Rack") }, TidyColliderShape.Box);
            ClampColliderTop(PrefabFolder + "/WarehouseWorkbench.prefab", benchTop);
            Static(context, "WarehousePegboard", TidyWarehouseFixtures.Pegboard(MeshFolder, 2.6f, 1.3f), new[] { context.Material("Warehouse_Pegboard"), context.Material("Warehouse_SteelDark") }, TidyColliderShape.Box);
            Static(context, "WarehouseConveyor", TidyWarehouseFixtures.Conveyor(MeshFolder), new[] { context.Material("Metal_Rack"), context.Material("Warehouse_PaintYellow"), context.Material("Metal_Steel") }, TidyColliderShape.Box);
            Static(context, "WarehouseRollerDoor", TidyWarehouseFixtures.RollerDoor(MeshFolder, 4.2f, 4.4f, 3.0f), new[] { context.Material("Metal_Steel"), context.Material("Rubber_Dark"), context.Material("Warehouse_SteelDark") }, TidyColliderShape.None);
            Static(context, "WarehouseLamp", TidyWarehouseFixtures.Lamp(MeshFolder), new[] { context.Material("Warehouse_LampShade"), context.Material("Dark") }, TidyColliderShape.None);
            Static(context, "WarehouseBollard", TidyWarehouseFixtures.Bollard(MeshFolder), new[] { context.Material("Warehouse_PaintYellow"), context.Material("Dark") }, TidyColliderShape.Box);
            Static(context, "WarehouseFence", TidyWarehouseFixtures.FencePanel(MeshFolder, 2f, 1.7f), new[] { context.Material("Warehouse_PaintYellow"), context.Material("Metal_Steel") }, TidyColliderShape.Box);
            Static(context, "WarehouseDesk", TidyWarehouseFixtures.Desk(MeshFolder), new[] { context.Material("Wood_Oak"), context.Material("Warehouse_SteelDark"), context.Material("Dark") }, TidyColliderShape.Box);
            Static(context, "WarehouseOfficeChair", TidyWarehouseFixtures.OfficeChair(MeshFolder), new[] { context.Material("Fabric_Bed"), context.Material("Dark") }, TidyColliderShape.Box);
            Static(context, "WarehouseCabinet", TidyWarehouseFixtures.Cabinet(MeshFolder), new[] { context.Material("Warehouse_OfficeWall"), context.Material("Metal_Steel") }, TidyColliderShape.Box);
            Static(context, "WarehouseExtinguisher", TidyWarehouseFixtures.Extinguisher(MeshFolder), new[] { context.Material("Plastic_Red"), context.Material("Dark") }, TidyColliderShape.None);
            var partition = TidyWarehouseFixtures.Partition(MeshFolder, 3.6f, 2.6f, out var glass);
            var root = TidyPropFactory.Visual("WarehousePartition", partition, context.Material("Warehouse_OfficeWall"), context.Material("Warehouse_SteelDark"));
            TidyPropFactory.AddCollider(root, partition, TidyColliderShape.Mesh);
            var pane = TidyPropFactory.Visual("Glass", glass, context.Material("Glass_Clear"));
            pane.transform.SetParent(root.transform, false);
            context.AddPrefab(TidyAssetFactory.SavePrefab(root, PrefabFolder + "/WarehousePartition.prefab"));
        }

        static void Interactive(string id, Mesh mesh, Material[] materials, TidyObjectDefinition definition, TidyColliderShape collider)
        {
            TidyPropFactory.Interactive(PrefabFolder + "/" + PrefabName(id) + ".prefab", mesh, materials, definition, collider);
        }

        static void Static(TidyDemoContext context, string prefabName, Mesh mesh, Material[] materials, TidyColliderShape collider)
        {
            context.AddPrefab(TidyPropFactory.Static(PrefabFolder + "/" + prefabName + ".prefab", mesh, materials, collider));
        }

        static void ClampColliderTop(string prefabPath, float top)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var box = root.GetComponent<BoxCollider>();
            if (box != null)
            {
                var bottom = box.center.y - box.size.y * 0.5f;
                box.size = new Vector3(box.size.x, top - bottom, box.size.z);
                box.center = new Vector3(box.center.x, (top + bottom) * 0.5f, box.center.z);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        static string PrefabName(string id)
        {
            var builder = new System.Text.StringBuilder("Warehouse");
            foreach (var part in id.Substring(3).Split('_'))
            {
                if (part.Length > 0)
                {
                    builder.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1));
                }
            }

            return builder.ToString();
        }
    }
}
