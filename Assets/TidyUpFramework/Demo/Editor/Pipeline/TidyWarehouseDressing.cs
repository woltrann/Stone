using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseDressing
    {
        static Mesh pallet;
        static Mesh[] cartons;
        static Mesh crate;

        public static void Build(TidyWarehouseStage stage)
        {
            LoadMeshes();
            BuildRackLoads(stage);
            BuildFloorMarkings(stage);
            BuildOffice(stage);
            BuildSignage(stage);
            BuildPalletStacks(stage);
            BuildSafety(stage);
        }

        static void LoadMeshes()
        {
            var folder = TidyWarehouseStage.MeshFolder;
            pallet = AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/SM_Warehouse_Pallet.asset");
            cartons = new[]
            {
                AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/SM_Warehouse_Carton_S.asset"),
                AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/SM_Warehouse_Carton_M.asset"),
                AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/SM_Warehouse_Carton_L.asset")
            };
            crate = AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/SM_Warehouse_Crate_Large.asset");
        }

        static void BuildRackLoads(TidyWarehouseStage stage)
        {
            var root = TidySceneKit.Group("Rack Stock", stage.Environment);
            for (var bay = 0; bay < TidyWarehouseZones.RackBays.Length; bay++)
            {
                var outer = bay == 0 || bay == TidyWarehouseZones.RackBays.Length - 1;
                for (var level = outer ? 0 : 2; level < TidyWarehouseFixtures.RackLevels.Length; level++)
                {
                    foreach (var offset in new[] { -0.66f, 0.66f })
                    {
                        if (stage.Random.NextDouble() < 0.15)
                        {
                            continue;
                        }

                        var basePoint = new Vector3(TidyWarehouseZones.RackX, TidyWarehouseFixtures.RackLevels[level], TidyWarehouseZones.RackBays[bay] + offset);
                        LoadedPallet(stage, root, basePoint, 90f, level < TidyWarehouseFixtures.RackLevels.Length - 1 ? 2 : 1);
                    }
                }
            }
        }

        public static void LoadedPallet(TidyWarehouseStage stage, Transform parent, Vector3 basePoint, float yaw, int maxLayers)
        {
            var holder = TidySceneKit.Group("Stock Pallet", parent);
            holder.SetPositionAndRotation(basePoint, Quaternion.Euler(0f, yaw, 0f));
            TidySceneDressing.Decor("Pallet", holder, pallet, new[] { stage.Material("Wood_Crate") }, Vector3.zero, Quaternion.identity);
            var kind = stage.Random.Next(4);
            if (kind == 3)
            {
                foreach (var x in new[] { -0.3f, 0.3f })
                {
                    TidySceneDressing.Decor("Crate", holder, crate, new[] { stage.Material("Wood_Crate"), stage.Material(stage.Random.NextDouble() < 0.5 ? "Warehouse_StencilParts" : "Warehouse_StencilFragile") }, new Vector3(x, TidyWarehouseFixtures.PalletHeight, 0f), Quaternion.Euler(0f, 90f, 0f));
                }

                return;
            }

            var sizeIndex = kind == 0 ? 2 : kind == 1 ? 1 : 0;
            var mesh = cartons[sizeIndex];
            var size = mesh.bounds.size;
            var columns = Mathf.Max(1, Mathf.FloorToInt(1.18f / (size.x + 0.01f)));
            var rows = Mathf.Max(1, Mathf.FloorToInt(0.98f / (size.z + 0.01f)));
            var layers = Mathf.Min(maxLayers + (sizeIndex == 0 ? 1 : 0), Mathf.FloorToInt(1.0f / size.y));
            var white = stage.Random.NextDouble() < 0.4;
            var materials = new[] { stage.Material(white ? "Warehouse_CardboardLight" : "Cardboard"), stage.Material("Tape"), stage.Material("Warehouse_Print_" + TidyWarehouseArt.SizeCodes[sizeIndex]) };
            for (var layer = 0; layer < layers; layer++)
            {
                var count = layer == layers - 1 ? Mathf.Max(1, columns * rows - stage.Random.Next(2)) : columns * rows;
                for (var index = 0; index < count; index++)
                {
                    var column = index % columns;
                    var row = index / columns;
                    var local = new Vector3((column - (columns - 1) * 0.5f) * (size.x + 0.01f), TidyWarehouseFixtures.PalletHeight + layer * (size.y + 0.002f), (row - (rows - 1) * 0.5f) * (size.z + 0.01f));
                    var decor = TidySceneDressing.Decor("Stock Carton", holder, mesh, materials, local, Quaternion.Euler(0f, stage.Next(-2f, 2f), 0f));
                    if (size.y < 0.3f)
                    {
                        decor.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    }
                }
            }
        }

        static void BuildFloorMarkings(TidyWarehouseStage stage)
        {
            var root = TidySceneKit.Group("Floor Markings", stage.Environment);
            stage.FloorDecal("Rack Aisle Line", root, new Vector3(-7.3f, 0f, 0f), new Vector2(0.1f, 14.2f), 0f, "Warehouse_PaintYellow");
            stage.FloorDecal("Bin Aisle Line", root, new Vector3(6.6f, 0f, -1.35f), new Vector2(0.1f, 8.6f), 0f, "Warehouse_PaintYellow");
            stage.FloorDecal("Dock Line", root, new Vector3(0f, 0f, -7.4f), new Vector2(17.6f, 0.1f), 0f, "Warehouse_PaintYellow");
            for (var z = -10.6f; z < -1.4f; z += 1.0f)
            {
                foreach (var x in new[] { -1.5f, 1.5f })
                {
                    stage.FloorDecal("Walkway Dash", root, new Vector3(x, 0f, z), new Vector2(0.1f, 0.6f), 0f, "Warehouse_PaintWhite");
                }
            }

            TidyWarehouseSigns.FloorText("LOADING DOCK", root, new Vector3(-4.6f, 0f, -11.0f), 0f, 3.2f, new Color(0.95f, 0.8f, 0.2f), new Vector2(3f, 0.5f));
            TidyWarehouseSigns.FloorText("SIZE STAGING", root, new Vector3(-1.4f, 0f, 2.4f), 0f, 2.6f, new Color(0.95f, 0.95f, 0.9f), new Vector2(3f, 0.5f));
            TidyWarehouseSigns.FloorText("OUTBOUND", root, new Vector3(6.1f, 0f, -1.35f), 90f, 3.2f, new Color(0.95f, 0.8f, 0.2f), new Vector2(3f, 0.5f));
            TidyWarehouseSigns.FloorText("RACK A", root, new Vector3(-6.7f, 0f, -5.2f), -90f, 3.2f, new Color(0.95f, 0.8f, 0.2f), new Vector2(2f, 0.5f));
            stage.HazardBand("Rack End Guard", root, new Vector3(-8.1f, 0f, -7.15f), new Vector2(1.6f, 0.25f), 0f);
            stage.HazardBand("Rack End Guard", root, new Vector3(-8.1f, 0f, 7.15f), new Vector2(1.6f, 0.25f), 0f);
        }

        static void BuildOffice(TidyWarehouseStage stage)
        {
            var root = TidySceneKit.Group("Office", stage.Fixtures);
            var partition = stage.Context.Prefab("WarehousePartition");
            TidySceneKit.Place(partition, root, new Vector3(7.08f, 0f, 8.25f), Vector3.zero);
            TidySceneKit.Place(partition, root, new Vector3(4.66f, 0f, 10.13f), new Vector3(0f, 90f, 0f));
            var floor = stage.FloorDecal("Office Floor", root, new Vector3(6.82f, 0f, 10.1f), new Vector2(4.24f, 3.7f), 0f, "Floor_LightOak", -0.002f);
            floor.GetComponent<MeshRenderer>().receiveShadows = true;
            TidySceneKit.Place(stage.Context.Prefab("WarehouseDesk"), root, new Vector3(6.9f, 0f, 11.45f), new Vector3(0f, 180f, 0f));
            TidySceneKit.Place(stage.Context.Prefab("WarehouseOfficeChair"), root, new Vector3(6.85f, 0f, 10.75f), new Vector3(0f, -8f, 0f));
            TidySceneKit.Place(stage.Context.Prefab("WarehouseCabinet"), root, new Vector3(8.6f, 0f, 9.3f), new Vector3(0f, -90f, 0f));
            TidySceneKit.Place(stage.Context.Prefab("WarehouseCabinet"), root, new Vector3(8.6f, 0f, 9.8f), new Vector3(0f, -90f, 0f));
            var screen = AssetDatabase.LoadAssetAtPath<Mesh>(TidyWarehouseStage.MeshFolder + "/SM_Warehouse_DeskScreen.asset");
            TidySceneDressing.Decor("Screen", root, screen, new[] { stage.Material("Liquid_Teal") }, new Vector3(7.1f, 0.98f, 11.65f - 0.018f), Quaternion.identity);
            var board = new TidyMeshBuilder(2);
            board.AddBeveledBox(Vector3.zero, new Vector3(1.6f, 0.9f, 0.04f), 0.01f, 1, 1);
            board.AddBox(new Vector3(0f, 0f, 0.021f), new Vector3(1.5f, 0.8f, 0.002f), 0);
            var boardObject = TidySceneDressing.Decor("Whiteboard", root, TidyMeshUtility.SaveAsIs(board, "SM_Warehouse_Whiteboard", TidyWarehouseStage.MeshFolder), new[] { stage.Material("White"), stage.Material("Metal_Steel") }, new Vector3(5.6f, 1.6f, 11.9f), Quaternion.Euler(0f, 180f, 0f));
            TidyWarehouseSigns.Text("SHIP BY 18:00", boardObject.transform, new Vector3(0f, 0.2f, 0.024f), 1.6f, new Color(0.15f, 0.3f, 0.7f), new Vector2(1.4f, 0.25f));
            TidyWarehouseSigns.Text("DOCK 3  -  PRIORITY", boardObject.transform, new Vector3(0f, -0.1f, 0.024f), 1.0f, new Color(0.75f, 0.2f, 0.15f), new Vector2(1.4f, 0.2f));
            BuildPlant(stage, root, new Vector3(5.2f, 0f, 11.5f));
            TidySceneKit.PointLight("Office Light", root, new Vector3(6.8f, 2.6f, 10.2f), new Color(1f, 0.88f, 0.72f), 2.4f, 5f);
            BuildClearSign(stage, root);
            stage.Block(4.5f, 8.1f, 9f, 12f);
        }

        static void BuildPlant(TidyWarehouseStage stage, Transform parent, Vector3 position)
        {
            var plant = new TidyMeshBuilder(2);
            plant.AddLathe(new[] { new Vector2(0.13f, 0f), new Vector2(0.17f, 0.36f), new Vector2(0.18f, 0.38f), new Vector2(0.16f, 0.38f), new Vector2(0.15f, 0.33f) }, 20, 0);
            var random = new System.Random(5);
            for (var leaf = 0; leaf < 22; leaf++)
            {
                var angle = leaf * 137.5f;
                var tilt = 20f + (float)random.NextDouble() * 40f;
                var length = 0.35f + (float)random.NextDouble() * 0.4f;
                plant.AddBeveledBox(new Vector3(0f, length * 0.5f, 0f), new Vector3(0.07f, length, 0.012f), 0.005f, 1, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.32f, 0f), new Vector3(tilt, angle, 0f), Vector3.one));
            }

            var mesh = TidyMeshUtility.SaveAsIs(plant, "SM_Warehouse_Plant", TidyWarehouseStage.MeshFolder);
            TidySceneDressing.Decor("Plant", parent, mesh, new[] { stage.Material("Terracotta"), stage.Material("Plant_Green") }, position, Quaternion.identity);
        }

        static void BuildClearSign(TidyWarehouseStage stage, Transform parent)
        {
            var position = new Vector3(7.08f, 3.2f, 8.25f);
            var posts = new TidyMeshBuilder();
            foreach (var x in new[] { -0.8f, 0.8f })
            {
                posts.AddBeveledBox(new Vector3(x, 2.73f, 0f), new Vector3(0.05f, 0.26f, 0.05f), 0.008f, 1);
            }

            TidySceneDressing.Decor("Sign Posts", parent, TidyMeshUtility.SaveAsIs(posts, "SM_Warehouse_SignPosts", TidyWarehouseStage.MeshFolder), new[] { stage.Material("Warehouse_SteelDark") }, new Vector3(position.x, 0f, position.z), Quaternion.identity);
            var messy = TidyWarehouseSigns.Sign(stage, "Backlog Sign", parent, position, 180f, new Vector2(2.4f, 0.62f), "Warehouse_SignAmber", "BACKLOG", new Color(0.16f, 0.08f, 0.03f), 3.4f);
            var clear = TidyWarehouseSigns.Sign(stage, "All Clear Sign", parent, position, 180f, new Vector2(2.4f, 0.62f), "Warehouse_SignGreen", "ALL CLEAR", new Color(0.03f, 0.14f, 0.06f), 3.4f);
            TidySceneKit.PointLight("All Clear Glow", clear.transform, position + new Vector3(0f, 0f, -0.6f), new Color(0.4f, 1f, 0.55f), 2.5f, 4.5f);
            stage.MessyOnly.Add(messy);
            clear.SetActive(false);
            stage.OrganisedOnly.Add(clear);
        }

        static void BuildSignage(TidyWarehouseStage stage)
        {
            var root = TidySceneKit.Group("Signage", stage.Fixtures);
            var half = TidyWarehouseStage.HallLength * 0.5f;
            TidyWarehouseSigns.Sign(stage, "Depot Banner", root, new Vector3(-1.2f, 5.55f, half - 0.1f), 180f, new Vector2(6.2f, 1.2f), "Warehouse_SignPanel", "NORTHWIND PARCEL DEPOT", new Color(1f, 0.62f, 0.2f), 5.2f, "SORT  -  STACK  -  SHIP", new Color(0.92f, 0.92f, 0.88f));
            TidyWarehouseSigns.Sign(stage, "Dock Sign", root, new Vector3(0f, 5.3f, -half + 0.1f), 0f, new Vector2(2.4f, 0.8f), "Warehouse_PaintYellow", "DOCK 3", new Color(0.1f, 0.1f, 0.1f), 4.6f);
            TidyWarehouseSigns.Sign(stage, "Racking Sign", root, new Vector3(-TidyWarehouseStage.HallWidth * 0.5f + 0.1f, 5.4f, 0f), 90f, new Vector2(3f, 0.8f), "Metal_Rack", "RACKING  A", Color.white, 4f);
            TidyWarehouseSigns.Sign(stage, "Outbound Sign", root, new Vector3(TidyWarehouseStage.HallWidth * 0.5f - 0.1f, 4.0f, -1.35f), -90f, new Vector2(4.2f, 0.9f), "Warehouse_SignPanel", "OUTBOUND  BY  COLOUR", new Color(1f, 0.62f, 0.2f), 3.4f);
        }

        static void BuildPalletStacks(TidyWarehouseStage stage)
        {
            var palletPrefab = stage.Context.Prefab("WarehousePallet");
            var root = TidySceneKit.Group("Empty Pallets", stage.Fixtures);
            StackPallets(palletPrefab, root, new Vector3(-7.6f, 0f, -10.8f), 8, 4f);
            StackPallets(palletPrefab, root, new Vector3(-7.6f, 0f, -9.3f), 5, -6f);
            StackPallets(palletPrefab, root, new Vector3(7.7f, 0f, -10.8f), 6, 3f);
            LoadedPallet(stage, root, new Vector3(7.7f, 0f, -8.9f), 90f, 2);
            LoadedPallet(stage, root, new Vector3(2.6f, 0f, 11.2f), 0f, 1);
            stage.Block(1.9f, 10.6f, 3.3f, 11.9f);
            stage.Block(-8.9f, -11.9f, -6.6f, -8.5f);
            stage.Block(6.9f, -11.9f, 8.9f, -8.1f);
        }

        static void StackPallets(GameObject prefab, Transform parent, Vector3 basePoint, int count, float twist)
        {
            for (var index = 0; index < count; index++)
            {
                var yaw = 90f + Mathf.Sin(index * 1.7f) * twist;
                TidySceneKit.Place(prefab, parent, basePoint + new Vector3(Mathf.Sin(index * 2.3f) * 0.03f, index * TidyWarehouseFixtures.PalletHeight, Mathf.Cos(index * 1.9f) * 0.03f), new Vector3(0f, yaw, 0f));
            }
        }

        static void BuildSafety(TidyWarehouseStage stage)
        {
            var root = TidySceneKit.Group("Safety", stage.Fixtures);
            var extinguisher = stage.Context.Prefab("WarehouseExtinguisher");
            var half = new Vector2(TidyWarehouseStage.HallWidth * 0.5f, TidyWarehouseStage.HallLength * 0.5f);
            Extinguisher(stage, root, extinguisher, new Vector3(4.0f, 0.75f, half.y - 0.2f), 180f);
            Extinguisher(stage, root, extinguisher, new Vector3(-4.0f, 0.75f, -half.y + 0.2f), 0f);
            Extinguisher(stage, root, extinguisher, new Vector3(half.x - 0.2f, 0.75f, 6.4f), -90f);
            Extinguisher(stage, root, extinguisher, new Vector3(-half.x + 0.2f, 0.75f, -8.2f), 90f);
        }

        static void Extinguisher(TidyWarehouseStage stage, Transform root, GameObject prefab, Vector3 position, float yaw)
        {
            TidySceneKit.Place(prefab, root, position, new Vector3(0f, yaw, 0f));
            var forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            TidyWarehouseSigns.Sign(stage, "Fire Sign", root, position + Vector3.up * 1.0f - forward * 0.08f, yaw, new Vector2(0.32f, 0.32f), "Plastic_Red", "FIRE", Color.white, 1.1f);
        }
    }
}
