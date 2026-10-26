using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using TidyUpFramework.Placement;
using TidyUpFramework.Rules;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseZones
    {
        public const float RackX = -8.35f;
        public static readonly float[] RackBays = { -5.4f, -2.7f, 0f, 2.7f, 5.4f };
        public static readonly float[] BinRows = { -4.5f, -2.4f, -0.3f, 1.8f };
        public const float BinX = 8.2f;
        public static readonly float[] PalletColumns = { -3.6f, -1.4f, 0.8f };
        public const float PalletZ = 0.9f;
        public static readonly Vector3 ConveyorCenter = new Vector3(2.75f, 0f, 5.4f);
        public static readonly Vector3 BenchCenter = new Vector3(-1.2f, 0f, 11.5f);
        public static readonly Vector3 CageCenter = new Vector3(-6.95f, 0f, 9.95f);

        static readonly string[] BayLetters = { "a", "b", "c" };
        static readonly string[] SizeNames = { "Small", "Medium", "Large" };
        static readonly int[] StackHeights = { 4, 3, 2 };
        static readonly Vector2[] SizeRanges = { new Vector2(0f, 2.5f), new Vector2(2.5f, 4.5f), new Vector2(4.5f, 10f) };

        public static void Build(TidyWarehouseStage stage)
        {
            BuildRacks(stage);
            BuildBins(stage);
            BuildPallets(stage);
            BuildConveyor(stage);
            BuildToolWall(stage);
            BuildCage(stage);
        }

        static void BuildRacks(TidyWarehouseStage stage)
        {
            var bay = stage.Context.Prefab("WarehouseRackBay");
            for (var index = 0; index < RackBays.Length; index++)
            {
                TidySceneKit.Place(bay, stage.Fixtures, new Vector3(RackX, 0f, RackBays[index]), new Vector3(0f, 90f, 0f));
            }

            for (var index = 1; index <= 3; index++)
            {
                var z = RackBays[index];
                var letter = BayLetters[index - 1];
                RackRow(stage, "rack_fragile_" + letter, "Fragile Goods", "DepotFragile", TidyWarehouseFixtures.RackLevels[0], z, new Color(0.95f, 0.4f, 0.35f));
                RackRow(stage, "rack_parts_" + letter, "Spare Parts", "DepotSpareParts", TidyWarehouseFixtures.RackLevels[1], z, new Color(0.4f, 0.6f, 1f));
                BeamSign(stage, "FRAGILE  -  LOW BEAM", "Matte_Red", Color.white, TidyWarehouseFixtures.RackLevels[0], z);
                BeamSign(stage, "SPARE PARTS", "Matte_Blue", Color.white, TidyWarehouseFixtures.RackLevels[1], z);
            }

            stage.Block(-9f, -7f, -7.55f, 7f);
        }

        static void RackRow(TidyWarehouseStage stage, string id, string display, string category, float level, float z, Color gizmo)
        {
            const float length = 2.5f;
            var row = TidySceneDressing.Target<TidyRowZone>(display + " Row " + id, stage.Targets, Vector3.zero, Quaternion.identity, id, display, TidyDemoLevels.Filter(stage.Context.Category(category)), gizmo, new Vector3(length * 0.5f, 0.3f, 0f), new Vector3(length, 0.62f, 0.95f), TidyCompletionMode.Never);
            row.transform.SetPositionAndRotation(new Vector3(RackX, level, z + length * 0.5f), Quaternion.Euler(0f, 90f, 0f));
            row.ConfigureRow(length, 0.04f, 6, Vector3.zero, TidyRowAlignment.Center);
            row.ConfigureGizmo(0.9f, 0.5f);
        }

        static void BeamSign(TidyWarehouseStage stage, string text, string face, Color color, float level, float z)
        {
            var x = RackX + TidyWarehouseFixtures.RackDepth * 0.5f + 0.03f;
            TidyWarehouseSigns.Sign(stage, "Beam Label", stage.Fixtures, new Vector3(x, level - 0.07f, z), 90f, new Vector2(1.2f, 0.16f), face, text, color, 1.1f);
        }

        static void BuildBins(TidyWarehouseStage stage)
        {
            var dot = new TidyMeshBuilder();
            dot.AddSphere(Vector3.zero, 0.045f, 14, 8);
            var dotMesh = TidyMeshUtility.SaveAsIs(dot, "SM_Warehouse_Dot", TidyWarehouseStage.MeshFolder);
            var fillMesh = AssetDatabase.LoadAssetAtPath<Mesh>(TidyWarehouseStage.MeshFolder + "/SM_Warehouse_BinFill.asset");
            for (var index = 0; index < BinRows.Length; index++)
            {
                var color = TidyWarehouseArt.LabelColors[index];
                var z = BinRows[index];
                var tint = TidyArtLibrary.PaletteColor(color);
                var ink = color == "Yellow" ? new Color(0.16f, 0.14f, 0.1f) : Color.white;
                var bin = TidySceneKit.Place(stage.Context.Prefab("WarehouseBin_" + color), stage.Fixtures, new Vector3(BinX, 0f, z), new Vector3(0f, -90f, 0f));
                var filter = TidyMatchFilter.Create(new[] { stage.Context.Category("DepotParcels") }, new[] { stage.Context.Tag(color) });
                var container = TidySceneDressing.Target<TidyContainer>(color + " Bin", stage.Targets, Vector3.zero, Quaternion.identity, "bin_" + color.ToLowerInvariant(), color + " Label Bin", filter, tint, new Vector3(0f, 0.3f, 0f), new Vector3(1.4f, 0.85f, 1.1f), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
                container.transform.SetPositionAndRotation(bin.transform.position + Vector3.up * 0.14f, bin.transform.rotation);
                container.ConfigureContainer(12, false, new Vector3(1.18f, 0.6f, 0.88f), Vector3.zero, TidyContainerLayout.Pile, false);
                var fill = TidySceneDressing.Decor("Fill Level", container.transform, fillMesh, new[] { stage.Material("Cardboard") }, Vector3.zero, Quaternion.identity);
                var sign = TidyWarehouseSigns.Sign(stage, color + " Bin Sign", stage.Fixtures, new Vector3(TidyWarehouseStage.HallWidth * 0.5f - 0.11f, 2.3f, z), -90f, new Vector2(1.3f, 0.62f), "Matte_" + color, color.ToUpperInvariant(), ink, 3.2f, "OUTBOUND BAY " + (index + 1), ink);
                TidyWarehouseSigns.Text(color.ToUpperInvariant(), bin.transform, new Vector3(0f, 0.46f, TidyWarehouseFixtures.BinSize.z * 0.5f + 0.024f), 1.5f, Color.white, new Vector2(0.46f, 0.26f));
                var stages = new List<GameObject>();
                for (var step = 0; step < 3; step++)
                {
                    var lamp = TidySceneDressing.Decor("Fill Lamp " + (step + 1), sign.transform, dotMesh, new[] { stage.Material("Liquid_" + color) }, new Vector3(-0.2f + step * 0.2f, -0.43f, 0.03f), Quaternion.identity);
                    lamp.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    stages.Add(lamp);
                }

                container.ConfigureFillVisual(fill.transform, new Vector2(0.0f, 0.42f), stages.ToArray());
                stage.FloorDecal(color + " Bin Pad", stage.Fixtures, new Vector3(7.2f, 0f, z), new Vector2(0.8f, 1.5f), 0f, "Matte_" + color);
                stage.FloorOutline(color + " Bin Outline", stage.Fixtures, new Vector3(7.95f, 0f, z), new Vector2(2.1f, 1.8f), 0.07f, "Warehouse_PaintWhite");
                var glow = stage.FloorDecal(color + " Zone Glow", stage.Fixtures, new Vector3(6.88f, 0f, z), new Vector2(0.06f, 1.7f), 0f, "Liquid_" + color, 0.001f);
                stage.ZoneGlows.Add(glow.transform);
                stage.Block(6.8f, z - 1f, 9f, z + 1f);
            }
        }

        static void BuildPallets(TidyWarehouseStage stage)
        {
            var pallet = stage.Context.Prefab("WarehousePallet");
            for (var index = 0; index < PalletColumns.Length; index++)
            {
                var center = new Vector3(PalletColumns[index], 0f, PalletZ);
                TidySceneKit.Place(pallet, stage.Fixtures, center, Vector3.zero);
                var rule = SizeRule(SizeNames[index], SizeRanges[index]);
                var filter = TidyMatchFilter.Create(new[] { stage.Context.Category("DepotCartons") }, null, rule);
                var stackIndex = 0;
                foreach (var x in new[] { -0.3f, 0.3f })
                {
                    foreach (var z in new[] { 0.25f, -0.25f })
                    {
                        stackIndex++;
                        var id = "pallet_" + SizeNames[index].ToLowerInvariant() + "_" + stackIndex;
                        var stack = TidySceneDressing.Target<TidyStackPoint>(SizeNames[index] + " Stack " + stackIndex, stage.Targets, center + new Vector3(x, TidyWarehouseFixtures.PalletHeight + 0.002f, z), Quaternion.identity, id, SizeNames[index] + " Cartons", filter, new Color(0.95f, 0.8f, 0.4f), new Vector3(0f, 0.45f, 0f), new Vector3(0.58f, 0.9f, 0.48f), TidyCompletionMode.Never);
                        stack.ConfigureStack(StackHeights[index], 0.004f, Vector3.up, false, Vector3.zero, 2.5f);
                    }
                }

                stage.FloorOutline(SizeNames[index] + " Pallet Outline", stage.Fixtures, center, new Vector2(1.55f, 1.35f), 0.07f, "Warehouse_PaintWhite");
                TidyWarehouseSigns.FloorText(SizeNames[index].Substring(0, 1), stage.Fixtures, center + new Vector3(0f, 0f, -1.25f), 0f, 6f, new Color(0.96f, 0.74f, 0.16f), new Vector2(1f, 0.8f));
                var glow = stage.FloorDecal(SizeNames[index] + " Zone Glow", stage.Fixtures, center + new Vector3(0f, 0f, -0.78f), new Vector2(1.5f, 0.05f), 0f, "Liquid_Orange", 0.001f);
                stage.ZoneGlows.Add(glow.transform);
                TidyWarehouseSigns.Sign(stage, SizeNames[index] + " Banner", stage.Fixtures, center + new Vector3(0f, 2.75f, 0f), 180f, new Vector2(1.2f, 0.5f), "Warehouse_BeamOrange", SizeNames[index].ToUpperInvariant(), Color.white, 2.2f, "CARTONS  " + SizeNames[index].Substring(0, 1), new Color(1f, 0.95f, 0.85f));
                Hanger(stage, center + new Vector3(0f, 2.75f, 0f));
                stage.Block(center.x - 0.85f, center.z - 0.75f, center.x + 0.85f, center.z + 0.75f);
            }

            stage.Block(-4.6f, -0.6f, 1.8f, 0.1f);
        }

        static void Hanger(TidyWarehouseStage stage, Vector3 signCenter)
        {
            var cable = new TidyMeshBuilder();
            foreach (var x in new[] { -0.5f, 0.5f })
            {
                cable.AddCylinder(0.006f, TidyWarehouseStage.HallHeight - signCenter.y - 0.25f, 6, 0, Matrix4x4.Translate(new Vector3(x, 0.25f, 0f)));
            }

            var mesh = TidyMeshUtility.SaveAsIs(cable, "SM_Warehouse_Hanger", TidyWarehouseStage.MeshFolder);
            TidySceneDressing.Decor("Hanger", stage.Fixtures, mesh, new[] { stage.Material("Dark") }, signCenter, Quaternion.identity);
        }

        static TidyNumericRule SizeRule(string sizeName, Vector2 range)
        {
            var rule = TidyAssetFactory.CreateOrLoad<TidyNumericRule>(TidyDemoPaths.Rules + "/Rule_Warehouse_Size" + sizeName + ".asset");
            rule.Configure(TidyNumericProperty.Size, range.x, range.y);
            rule.AssignDescription(sizeName + " cartons");
            EditorUtility.SetDirty(rule);
            return rule;
        }

        static void BuildConveyor(TidyWarehouseStage stage)
        {
            TidySceneKit.Place(stage.Context.Prefab("WarehouseConveyor"), stage.Fixtures, ConveyorCenter, Vector3.zero);
            const float length = 4.1f;
            var start = ConveyorCenter + new Vector3(-length * 0.5f, TidyWarehouseFixtures.ConveyorHeight + 0.002f, 0f);
            var row = TidySceneDressing.Target<TidyRowZone>("Tote Return Conveyor", stage.Targets, start, Quaternion.identity, "conveyor_totes", "Tote Return", TidyDemoLevels.Filter(stage.Context.Category("DepotTotes")), new Color(0.4f, 0.9f, 0.9f), new Vector3(length * 0.5f, 0.15f, 0f), new Vector3(length, 0.4f, 0.62f), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
            row.ConfigureRow(length, 0.04f, 8, Vector3.zero, TidyRowAlignment.Center);
            row.ConfigureGizmo(0.6f, 0.3f);
            TidyWarehouseSigns.FloorText("TOTE RETURN", stage.Fixtures, ConveyorCenter + new Vector3(0f, 0f, -0.95f), 0f, 2.6f, new Color(0.96f, 0.74f, 0.16f), new Vector2(3f, 0.45f));
            var glow = stage.FloorDecal("Conveyor Glow", stage.Fixtures, ConveyorCenter + new Vector3(0f, 0f, -0.55f), new Vector2(4.3f, 0.05f), 0f, "Liquid_Teal", 0.001f);
            stage.ZoneGlows.Add(glow.transform);
            stage.Block(ConveyorCenter.x - 2.3f, ConveyorCenter.z - 0.6f, ConveyorCenter.x + 2.3f, ConveyorCenter.z + 0.6f);
        }

        static void BuildToolWall(TidyWarehouseStage stage)
        {
            var northZ = TidyWarehouseStage.HallLength * 0.5f - 0.08f;
            TidySceneKit.Place(stage.Context.Prefab("WarehouseWorkbench"), stage.Fixtures, BenchCenter, new Vector3(0f, 180f, 0f));
            TidySceneKit.Place(stage.Context.Prefab("WarehousePegboard"), stage.Fixtures, new Vector3(BenchCenter.x, 1.2f, northZ), new Vector3(0f, 180f, 0f));
            var boardFront = northZ - 0.017f;
            var group = TidySceneDressing.Target<TidySlotGroup>("Tool Wall", stage.Targets, new Vector3(BenchCenter.x, 1.85f, boardFront), Quaternion.identity, TidyWarehouseLevel.ToolWallId, "Tool Wall", TidyDemoLevels.Filter(stage.Context.Category("DepotTools")), new Color(1f, 0.6f, 0.25f), new Vector3(0f, 0f, -0.12f), new Vector3(2.6f, 1.3f, 0.26f), TidyCompletionMode.WhenFull);
            var hook = AssetDatabase.LoadAssetAtPath<Mesh>(TidyWarehouseStage.MeshFolder + "/SM_Warehouse_Hook.asset");
            var anchors = new List<Transform>();
            var outline = new TidyMeshBuilder();
            foreach (var y in new[] { 0.28f, -0.3f })
            {
                foreach (var x in new[] { -0.96f, -0.32f, 0.32f, 0.96f })
                {
                    var anchor = new GameObject("Hook " + (anchors.Count + 1)).transform;
                    anchor.SetParent(group.transform, false);
                    anchor.localPosition = new Vector3(x, y, -0.001f);
                    anchor.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    anchors.Add(anchor);
                    TidySceneDressing.Decor("Peg", group.transform, hook, new[] { stage.Material("Metal_Steel") }, new Vector3(x, y + 0.2f, 0f), Quaternion.Euler(0f, 180f, 0f));
                    Silhouette(outline, new Vector3(x, y, -0.002f));
                }
            }

            group.SetManualAnchors(anchors);
            group.ConfigureAssignment(false, Vector3.zero);
            var outlineMesh = TidyMeshUtility.SaveAsIs(outline, "SM_Warehouse_ToolOutlines", TidyWarehouseStage.MeshFolder);
            TidySceneDressing.Decor("Tool Outlines", group.transform, outlineMesh, new[] { stage.Material("Warehouse_SignPanel") }, Vector3.zero, Quaternion.identity).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            TidyWarehouseSigns.Sign(stage, "Tool Wall Sign", stage.Fixtures, new Vector3(BenchCenter.x, 2.75f, northZ - 0.05f), 180f, new Vector2(1.4f, 0.3f), "Warehouse_BeamOrange", "TOOL WALL", Color.white, 1.6f);
            TidySceneKit.SpotLight("Bench Light", stage.Fixtures, new Vector3(BenchCenter.x, 3.2f, northZ - 1.6f), new Vector3(50f, 0f, 0f), TidyWarehouseStage.Warm, 6f, 6f, 70f, false);
            stage.Block(BenchCenter.x - 1.4f, 10.9f, BenchCenter.x + 1.4f, 12f);
        }

        static void Silhouette(TidyMeshBuilder outline, Vector3 center)
        {
            const float width = 0.17f;
            const float height = 0.36f;
            const float line = 0.008f;
            outline.AddBox(center + new Vector3(0f, height * 0.5f, 0f), new Vector3(width, line, 0.001f));
            outline.AddBox(center + new Vector3(0f, -height * 0.5f, 0f), new Vector3(width, line, 0.001f));
            outline.AddBox(center + new Vector3(width * 0.5f, 0f, 0f), new Vector3(line, height, 0.001f));
            outline.AddBox(center + new Vector3(-width * 0.5f, 0f, 0f), new Vector3(line, height, 0.001f));
        }

        static void BuildCage(TidyWarehouseStage stage)
        {
            var fence = stage.Context.Prefab("WarehouseFence");
            TidySceneKit.Place(fence, stage.Fixtures, new Vector3(-7.92f, 0f, 7.9f), Vector3.zero);
            TidySceneKit.Place(fence, stage.Fixtures, new Vector3(-4.92f, 0f, 8.9f), new Vector3(0f, 90f, 0f));
            TidySceneKit.Place(fence, stage.Fixtures, new Vector3(-4.92f, 0f, 10.9f), new Vector3(0f, 90f, 0f));
            var filter = TidyDemoLevels.Filter(stage.Context.Category("DepotHazmat"));
            var zone = TidySceneDressing.Target<TidyFreePlacementZone>("Hazmat Cage", stage.Targets, CageCenter + new Vector3(0f, 0.002f, 0f), Quaternion.identity, "hazmat_cage", "Hazmat Cage", filter, new Color(1f, 0.85f, 0.2f), new Vector3(0f, 0.45f, 0f), new Vector3(3.6f, 0.9f, 3.6f), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
            zone.ConfigureArea(new Vector2(3.6f, 3.6f), 14, 0.1f, 45f, true);
            stage.FloorOutline("Cage Outline", stage.Fixtures, CageCenter, new Vector2(3.7f, 3.7f), 0.1f, "Warehouse_PaintYellow");
            stage.HazardBand("Cage Gate Hazard", stage.Fixtures, new Vector3(-5.95f, 0f, 7.75f), new Vector2(1.9f, 0.3f), 0f);
            var glow = stage.FloorDecal("Cage Glow", stage.Fixtures, CageCenter + new Vector3(0f, 0f, -1.92f), new Vector2(3.6f, 0.05f), 0f, "Liquid_Yellow", 0.001f);
            stage.ZoneGlows.Add(glow.transform);
            TidyWarehouseSigns.Sign(stage, "Hazmat Sign", stage.Fixtures, new Vector3(-8.9f, 2.4f, CageCenter.z), 90f, new Vector2(1.5f, 0.5f), "Warehouse_PaintYellow", "HAZMAT", new Color(0.1f, 0.1f, 0.1f), 2.4f, "DRUMS  &  FUEL CANS", new Color(0.15f, 0.15f, 0.15f));
            stage.Block(-9f, 7.6f, -4.7f, 12f);
        }
    }
}
