using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Rules;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static partial class TidyStoreScene
    {
        static readonly Vector3 StandCenter = new Vector3(0f, 0f, 4.4f);
        static readonly Vector3 CounterPosition = new Vector3(-5.0f, 0f, -5.6f);
        static readonly Vector3 TripletTablePosition = new Vector3(-4.4f, 0f, 6.0f);
        static readonly Vector3 SideTablePosition = new Vector3(4.4f, 0f, 6.0f);
        static readonly float[] PedestalX = { 2.9f, 4.1f, 5.3f };
        const float PedestalZ = -3.6f;
        static readonly Vector3[] BinPositions = { new Vector3(2.4f, 0f, -5.9f), new Vector3(3.5f, 0f, -6.3f), new Vector3(4.7f, 0f, -6.3f), new Vector3(5.8f, 0f, -5.9f) };
        static readonly Color SignInk = new Color(0.3f, 0.18f, 0.28f);

        static readonly Vector3[] IslandPositions = { new Vector3(-3.4f, 0f, 0.1f), new Vector3(3.4f, 0f, 0.1f) };
        static readonly float[] IslandYaws = { -90f, 90f };

        static float LeftWallX => -RoomWidth * 0.5f + WallInset;
        static float RightWallX => RoomWidth * 0.5f - WallInset;
        static float BackWallZ => RoomLength * 0.5f - WallInset;

        static void BuildSpeciesShelves()
        {
            var poses = new[]
            {
                (new Vector3(LeftWallX, 0f, -1.05f), 90f),
                (new Vector3(LeftWallX, 0f, 1.05f), 90f),
                (new Vector3(RightWallX, 0f, 1.05f), -90f),
                (new Vector3(RightWallX, 0f, -1.05f), -90f),
                (new Vector3(0f, 0f, BackWallZ), 180f)
            };
            for (var index = 0; index < TidyStoreContent.Species.Length; index++)
            {
                BuildSpeciesShelf(index, poses[index].Item1, poses[index].Item2);
            }
        }

        static void BuildSpeciesShelf(int index, Vector3 position, float yaw)
        {
            var species = TidyStoreContent.Species[index];
            var plural = TidyStoreContent.SpeciesPlural[index];
            var accent = TidyArtLibrary.PaletteColor(TidyStoreContent.SpeciesAccent[index]);
            var unit = TidySceneKit.Place(context.Prefab(TidyStoreContent.ShelfPrefabName(species)), props, position, new Vector3(0f, yaw, 0f));
            unit.name = plural + " Shelf Unit";
            var filter = TidyDemoLevels.Filter(context.Category(TidyStoreContent.SpeciesCategory(species)));
            var group = TidySceneDressing.Target<TidySlotGroup>(plural + " Shelf", targets, Vector3.zero, Quaternion.identity, "store_shelf_" + species.ToString().ToLowerInvariant(), plural, filter, accent, new Vector3(0f, 0.8f, 0.02f), new Vector3(1.78f, 1.76f, 0.36f), TidyCompletionMode.WhenFull);
            group.transform.SetParent(unit.transform, false);
            group.transform.localPosition = new Vector3(0f, TidyStoreFixtures.ShelfTops[0], 0.02f);
            group.ConfigureGrid(TidyStoreFixtures.ShelfColumns, TidyStoreFixtures.ShelfTops.Length - 1, new Vector2(TidyStoreFixtures.ShelfColumnSpacing, TidyStoreFixtures.ShelfRowSpacing), TidyGridPlane.Vertical);
            ShelfSign(unit.transform, plural.ToUpperInvariant(), TidyStoreContent.SpeciesAccent[index]);
            PriceStrips(unit.transform, plural.ToUpperInvariant());
            var top = TidyStoreFixtures.ShelfTops[TidyStoreFixtures.ShelfTops.Length - 1];
            ToyBoxes(unit.transform, top, 0.16f, 300 + index * 13);
        }

        static void ShelfSign(Transform unit, string text, string colorTag)
        {
            var mesh = TidyStoreFixtures.SignBoard(meshFolder, 1.7f, 0.44f);
            var sign = TidySceneDressing.Decor("Sign " + text, unit, mesh, new[] { context.Material("Plastic_" + colorTag), context.Material("Store_Cream") }, new Vector3(0f, TidyStoreFixtures.ShelfHeight + 0.33f, -0.25f), Quaternion.identity);
            TidySceneDressing.Label(text, sign.transform, new Vector3(0f, 0f, 0.05f), Quaternion.Euler(0f, 180f, 0f), 2.8f, SignInk, new Vector2(1.5f, 0.34f));
        }

        static void PriceStrips(Transform unit, string text)
        {
            var depth = TidyStoreFixtures.ShelfDepth * 0.5f + 0.006f;
            for (var row = 0; row < TidyStoreFixtures.ShelfTops.Length - 1; row++)
            {
                var y = TidyStoreFixtures.ShelfTops[row] - 0.03f;
                foreach (var x in new[] { -0.55f, 0.55f })
                {
                    TidySceneDressing.Label(text + "  $" + (5 + row) + ".99", unit, new Vector3(x, y, depth), Quaternion.Euler(0f, 180f, 0f), 0.3f, Color.white, new Vector2(0.6f, 0.05f));
                }
            }
        }

        static void ToyBoxes(Transform unit, float surface, float maxHeight, int seed)
        {
            var length = TidyStoreFixtures.ShelfWidth - 0.16f;
            var mesh = TidyStoreFixtures.ToyBoxRow(length, maxHeight, seed, ToyBoxColours.Length);
            TidySceneDressing.Decor("Toy Boxes", unit, mesh, ToyBoxMaterials(), new Vector3(-length * 0.5f, surface, 0.02f), Quaternion.identity);
        }

        static void BuildDecorShelves()
        {
            var poses = new List<(Vector3 position, float yaw)>
            {
                (new Vector3(LeftWallX, 0f, -3.15f), 90f),
                (new Vector3(LeftWallX, 0f, 3.15f), 90f),
                (new Vector3(LeftWallX, 0f, 5.25f), 90f),
                (new Vector3(RightWallX, 0f, -3.15f), -90f),
                (new Vector3(RightWallX, 0f, 3.15f), -90f),
                (new Vector3(RightWallX, 0f, 5.25f), -90f),
                (new Vector3(-2.1f, 0f, BackWallZ), 180f),
                (new Vector3(2.1f, 0f, BackWallZ), 180f),
                (new Vector3(-4.2f, 0f, BackWallZ), 180f),
                (new Vector3(4.2f, 0f, BackWallZ), 180f)
            };
            var prefab = context.Prefab("StoreShelf_Decor");
            for (var index = 0; index < poses.Count; index++)
            {
                var unit = TidySceneKit.Place(prefab, props, poses[index].position, new Vector3(0f, poses[index].yaw, 0f));
                for (var row = 0; row < TidyStoreFixtures.ShelfTops.Length; row++)
                {
                    ToyBoxes(unit.transform, TidyStoreFixtures.ShelfTops[row], row == TidyStoreFixtures.ShelfTops.Length - 1 ? 0.16f : 0.27f, 500 + index * 31 + row * 7);
                }
            }
        }

        static void BuildRainbowStand()
        {
            TidySceneKit.Place(context.Prefab("StoreTierStand"), props, StandCenter, Vector3.zero);
            var colours = TidyStoreContent.Colors;
            var inlayMaterials = new Material[colours.Length];
            for (var index = 0; index < colours.Length; index++)
            {
                inlayMaterials[index] = context.Material("Plastic_" + colours[index]);
            }

            TidySceneDressing.Decor("Colour Inlays", props, TidyStoreFixtures.TierInlays(meshFolder, colours.Length), inlayMaterials, StandCenter, Quaternion.identity);
            var pals = context.Category(TidyStoreContent.ParentCategory);
            for (var sector = 0; sector < colours.Length; sector++)
            {
                BuildStandSector(sector, colours[sector], pals);
            }

            var signMesh = TidyStoreFixtures.SignBoard(meshFolder, 1.1f, 0.32f);
            foreach (var yaw in new[] { 180f, 0f })
            {
                var sign = TidySceneDressing.Decor("Rainbow Sign", props, signMesh, new[] { context.Material("Store_Coral"), context.Material("Store_Cream") }, StandCenter + new Vector3(0f, 1.72f, 0f), Quaternion.Euler(0f, yaw, 0f));
                TidySceneDressing.Label("RAINBOW STAND", sign.transform, new Vector3(0f, 0f, 0.05f), Quaternion.Euler(0f, 180f, 0f), 1.15f, SignInk, new Vector2(1f, 0.22f));
            }

            TidySceneKit.SpotLight("Stand Spot", props, StandCenter + new Vector3(0f, 3.9f, -1.6f), new Vector3(62f, 0f, 0f), new Color(1f, 0.9f, 0.78f), 9f, 7f, 60f, true);
        }

        static void BuildStandSector(int sector, string colorTag, TidyCategoryDefinition pals)
        {
            var angle = sector * 360f / TidyStoreContent.Colors.Length;
            var filter = TidyDemoLevels.FilterWithTags(new[] { pals }, context.Tag(colorTag));
            var color = TidyArtLibrary.PaletteColor(colorTag);
            var group = TidySceneDressing.Target<TidySlotGroup>(colorTag + " Stand Slice", targets, StandCenter, Quaternion.Euler(0f, angle, 0f), "store_stand_" + colorTag.ToLowerInvariant(), colorTag + " Pals", filter, color, new Vector3(0f, 0.5f, 0.86f), new Vector3(0.62f, 0.5f, 0.3f), TidyCompletionMode.WhenFull);
            TidyPropFactory.AimTrigger(group.gameObject, new Vector3(0f, 0.86f, 0.56f), new Vector3(0.42f, 0.44f, 0.28f));
            TidyPropFactory.AimTrigger(group.gameObject, new Vector3(0f, 1.22f, 0.27f), new Vector3(0.24f, 0.42f, 0.22f));
            var anchors = new List<Transform>();
            for (var tier = 0; tier < TidyStoreFixtures.TierHeights.Length; tier++)
            {
                foreach (var offset in TidyStoreFixtures.TierSlotAngles[tier])
                {
                    var anchor = new GameObject("Slot " + anchors.Count).transform;
                    anchor.SetParent(group.transform, false);
                    var direction = Quaternion.Euler(0f, offset, 0f) * Vector3.forward;
                    anchor.localPosition = direction * TidyStoreFixtures.TierSlotRadii[tier] + Vector3.up * (TidyStoreFixtures.TierHeights[tier] + 0.004f);
                    anchor.localRotation = Quaternion.Euler(0f, offset, 0f);
                    anchors.Add(anchor);
                }
            }

            group.SetManualAnchors(anchors);
        }

        static void BuildSetPedestals()
        {
            var pals = context.Category(TidyStoreContent.ParentCategory);
            var prefab = context.Prefab("StoreSetPedestal");
            for (var index = 0; index < PedestalX.Length; index++)
            {
                var position = new Vector3(PedestalX[index], 0f, PedestalZ);
                TidySceneKit.Place(prefab, props, position, Vector3.zero);
                var group = TidySceneDressing.Target<TidySlotGroup>("Family Set " + (index + 1), targets, position + new Vector3(0f, TidyStoreFixtures.PedestalTop + 0.001f, 0.075f), Quaternion.Euler(0f, 180f, 0f), TidyStoreLevel.SetIds[index], "Family Set", TidyDemoLevels.Filter(pals), new Color(1f, 0.6f, 0.75f), new Vector3(0f, 0.08f, 0.075f), new Vector3(0.58f, 0.22f, 0.58f), TidyCompletionMode.WhenFull);
                group.ConfigureGrid(3, 2, new Vector2(0.15f, 0.15f), TidyGridPlane.Horizontal);
                group.ConfigureOrdering(TidyOrderingMode.None, TidyOrderingDirection.Ascending, TidyGroupingMode.SameCategory, TidyTagGroup.Color);
                TidySceneDressing.Label("FAMILY SET", props, position + new Vector3(0f, 0.55f, -0.245f), Quaternion.identity, 0.7f, SignInk, new Vector2(0.44f, 0.1f));
                TidySceneDressing.Label("6 of one kind", props, position + new Vector3(0f, 0.47f, -0.245f), Quaternion.identity, 0.45f, SignInk, new Vector2(0.44f, 0.08f));
            }

            TidySceneKit.SpotLight("Set Spot", props, new Vector3(4.1f, 3.9f, -4.8f), new Vector3(60f, 0f, 0f), new Color(1f, 0.86f, 0.9f), 7f, 6f, 55f, false);
        }

        static void BuildTripletTable()
        {
            TidySceneKit.Place(context.Prefab("StoreDisplayTable"), props, TripletTablePosition, Vector3.zero);
            var tray = context.Prefab("StoreTripletTray");
            var pals = context.Category(TidyStoreContent.ParentCategory);
            for (var index = 0; index < TidyStoreLevel.TripletIds.Length; index++)
            {
                var position = TripletTablePosition + new Vector3(-0.84f + index * 0.56f, TidyStoreFixtures.TableHeight, -0.1f);
                TidySceneKit.Place(tray, props, position, Vector3.zero);
                var group = TidySceneDressing.Target<TidySlotGroup>("Triplet Tray " + (index + 1), targets, position + new Vector3(0f, 0.021f, 0f), Quaternion.Euler(0f, 180f, 0f), TidyStoreLevel.TripletIds[index], "Triplet Tray", TidyDemoLevels.Filter(pals), new Color(1f, 0.8f, 0.4f), new Vector3(0f, 0.06f, 0f), new Vector3(0.44f, 0.16f, 0.18f), TidyCompletionMode.WhenFull);
                group.ConfigureGrid(3, 1, new Vector2(0.13f, 0.13f), TidyGridPlane.Horizontal);
                group.ConfigureOrdering(TidyOrderingMode.None, TidyOrderingDirection.Ascending, TidyGroupingMode.SameDefinition, TidyTagGroup.Color);
            }

            var sign = TidySceneDressing.Decor("Triplets Sign", props, TidyStoreFixtures.SignBoard(meshFolder, 1.3f, 0.3f), new[] { context.Material("Plastic_Yellow"), context.Material("Store_Cream") }, TripletTablePosition + new Vector3(0f, TidyStoreFixtures.TableHeight + 0.17f, 0.28f), Quaternion.Euler(0f, 180f, 0f));
            TidySceneDressing.Label("MATCH THE TRIPLETS", sign.transform, new Vector3(0f, 0f, 0.05f), Quaternion.Euler(0f, 180f, 0f), 0.95f, SignInk, new Vector2(1.2f, 0.2f));
        }

        static void BuildPodBins()
        {
            var category = context.Category(TidyStoreContent.PodCategory);
            var focus = new Vector3(0.5f, 0f, -5.2f);
            for (var index = 0; index < TidyStoreContent.PodColors.Length; index++)
            {
                var colorTag = TidyStoreContent.PodColors[index];
                var position = BinPositions[index];
                TidySceneKit.Place(context.Prefab(TidyStoreContent.BinPrefabName(colorTag)), props, position, Vector3.zero);
                var filter = TidyDemoLevels.FilterWithTags(new[] { category }, context.Tag(colorTag));
                var bin = TidySceneDressing.Target<TidyContainer>(colorTag + " Pod Bin", targets, position + Vector3.up * 0.025f, Quaternion.identity, "store_pod_bin_" + colorTag.ToLowerInvariant(), colorTag + " Pal Pods", filter, TidyArtLibrary.PaletteColor(colorTag), new Vector3(0f, 0.28f, 0f), new Vector3(0.64f, 0.58f, 0.64f), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
                bin.ConfigureContainer(14, false, new Vector3(0.38f, 0.4f, 0.38f), Vector3.zero, TidyContainerLayout.Pile, false);
                var facing = (focus - position);
                facing.y = 0f;
                facing.Normalize();
                TidySceneDressing.Label(colorTag.ToUpperInvariant() + " PODS", props, position + facing * 0.315f + Vector3.up * 0.3f, Quaternion.LookRotation(-facing), 0.75f, Color.white, new Vector2(0.4f, 0.1f));
            }

            var sign = TidySceneDressing.Decor("Pods Sign", environment, TidyStoreFixtures.SignBoard(meshFolder, 1.5f, 0.4f), new[] { context.Material("Plastic_Yellow"), context.Material("Store_Cream") }, new Vector3(RoomWidth * 0.5f - 0.03f, 2.2f, -6.1f), Quaternion.Euler(0f, -90f, 0f));
            TidySceneDressing.Label("PAL PODS", sign.transform, new Vector3(0f, 0.04f, 0.05f), Quaternion.Euler(0f, 180f, 0f), 2.1f, SignInk, new Vector2(1.3f, 0.22f));
            TidySceneDressing.Label("mystery capsules", sign.transform, new Vector3(0f, -0.11f, 0.05f), Quaternion.Euler(0f, 180f, 0f), 0.9f, SignInk, new Vector2(1.3f, 0.1f));
        }

        static void BuildCheckout()
        {
            var counter = TidySceneKit.Place(context.Prefab("StoreCounter"), props, CounterPosition, new Vector3(0f, 90f, 0f));
            var register = TidySceneKit.Place(context.Prefab("StoreRegister"), props, counter.transform.TransformPoint(new Vector3(0.6f, TidyStoreFixtures.CounterHeight, -0.1f)), new Vector3(0f, 270f, 0f));
            register.name = "Register";
            var boxes = TidyStoreFixtures.ToyBoxRow(0.7f, 0.22f, 77, ToyBoxColours.Length);
            TidySceneDressing.Decor("Counter Boxes", counter.transform, boxes, ToyBoxMaterials(), new Vector3(-1.15f, TidyStoreFixtures.CounterHeight, -0.18f), Quaternion.identity);
            var sign = TidySceneDressing.Decor("Checkout Sign", environment, TidyStoreFixtures.SignBoard(meshFolder, 1.4f, 0.36f), new[] { context.Material("Plastic_Teal"), context.Material("Store_Cream") }, new Vector3(-RoomWidth * 0.5f + 0.03f, 2.3f, CounterPosition.z), Quaternion.Euler(0f, 90f, 0f));
            TidySceneDressing.Label("CHECKOUT", sign.transform, new Vector3(0f, 0f, 0.05f), Quaternion.Euler(0f, 180f, 0f), 2.1f, SignInk, new Vector2(1.3f, 0.26f));
        }

        static void BuildSideTable()
        {
            TidySceneKit.Place(context.Prefab("StoreDisplayTable"), props, SideTablePosition, Vector3.zero);
            var boxes = TidyStoreFixtures.ToyBoxRow(2.1f, 0.3f, 91, ToyBoxColours.Length);
            TidySceneDressing.Decor("Table Boxes", props, boxes, ToyBoxMaterials(), SideTablePosition + new Vector3(-1.05f, TidyStoreFixtures.TableHeight, 0.22f), Quaternion.identity);
            var sign = TidySceneDressing.Decor("New Arrivals Sign", props, TidyStoreFixtures.SignBoard(meshFolder, 1.3f, 0.3f), new[] { context.Material("Plastic_Pink"), context.Material("Store_Cream") }, SideTablePosition + new Vector3(0f, TidyStoreFixtures.TableHeight + 0.5f, 0.36f), Quaternion.Euler(0f, 180f, 0f));
            TidySceneDressing.Label("NEW ARRIVALS", sign.transform, new Vector3(0f, 0f, 0.05f), Quaternion.Euler(0f, 180f, 0f), 1.25f, SignInk, new Vector2(1.2f, 0.22f));
        }

        static void BuildIslands()
        {
            var table = context.Prefab("StoreDisplayTable");
            for (var index = 0; index < IslandPositions.Length; index++)
            {
                var island = TidySceneKit.Place(table, props, IslandPositions[index], new Vector3(0f, IslandYaws[index], 0f));
                island.name = "Island Display";
                var boxes = TidyStoreFixtures.ToyBoxRow(2.1f, 0.3f, 120 + index * 17, ToyBoxColours.Length);
                TidySceneDressing.Decor("Island Boxes", island.transform, boxes, ToyBoxMaterials(), new Vector3(-1.05f, TidyStoreFixtures.TableHeight, 0.2f), Quaternion.identity);
            }
        }

        static void BuildMascot()
        {
            var position = new Vector3(-2.4f, 0f, -4.4f);
            var plinth = new TidyMeshBuilder(2);
            plinth.AddLathe(TidyProfiles.RoundedCylinder(0.46f, 0.3f, 0.04f, 3), 40, 0);
            plinth.AddTorus(new Vector3(0f, 0.27f, 0f), 0.46f, 0.014f, 40, 6, 1);
            var plinthMesh = TidyMeshUtility.SaveAsIs(plinth, "SM_StoreMascotPlinth", meshFolder);
            var plinthObject = TidySceneDressing.Decor("Mascot Plinth", props, plinthMesh, new[] { context.Material("Store_Mint"), context.Material("Metal_Brass") }, position, Quaternion.identity);
            TidyPropFactory.AddCollider(plinthObject, plinthMesh, TidyColliderShape.Box);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshFolder + "/SM_Pal_Bear.asset");
            var materials = new[] { context.Material("Plastic_Orange"), context.Material("Pal_Cream"), context.Material("Pal_Eye"), context.Material("Pal_Blush") };
            var mascot = TidySceneDressing.Decor("Mascot Bear", props, mesh, materials, position + Vector3.up * 0.3f, Quaternion.Euler(0f, 132f, 0f));
            mascot.transform.localScale = Vector3.one * 9f;
            TidyPropFactory.AddCollider(mascot, mesh, TidyColliderShape.Box);
            TidySceneDressing.Label("MEET THE PEBBLE PALS", props, position + new Vector3(0f, 0.15f, -0.47f), Quaternion.identity, 0.55f, SignInk, new Vector2(0.8f, 0.1f));
        }

        static void BuildHangingBanner()
        {
            var center = new Vector3(0f, 2.95f, -3.2f);
            var cords = new TidyMeshBuilder();
            foreach (var x in new[] { -1.1f, 1.1f })
            {
                cords.AddCylinder(0.006f, RoomHeight - center.y - 0.3f, 6, 0, Matrix4x4.Translate(new Vector3(x, 0.3f, 0f)));
            }

            TidySceneDressing.Decor("Banner Cords", environment, TidyMeshUtility.SaveAsIs(cords, "SM_StoreBannerCords", meshFolder), new[] { context.Material("Dark") }, center, Quaternion.identity);
            var board = TidyStoreFixtures.SignBoard(meshFolder, 2.6f, 0.6f);
            foreach (var yaw in new[] { 180f, 0f })
            {
                var sign = TidySceneDressing.Decor("Collect Banner", environment, board, new[] { context.Material("Plastic_Pink"), context.Material("Store_Cream") }, center, Quaternion.Euler(0f, yaw, 0f));
                TidySceneDressing.Label("COLLECT ALL 30 PEBBLE PALS!", sign.transform, new Vector3(0f, 0.06f, 0.05f), Quaternion.Euler(0f, 180f, 0f), 1.3f, SignInk, new Vector2(2.4f, 0.28f));
                TidySceneDressing.Label("5 families  -  6 colours  -  sort them all", sign.transform, new Vector3(0f, -0.14f, 0.05f), Quaternion.Euler(0f, 180f, 0f), 0.85f, SignInk, new Vector2(2.4f, 0.14f));
            }
        }
    }
}