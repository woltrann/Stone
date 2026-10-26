using System.Collections.Generic;
using TidyUpFramework.Placement;
using TidyUpFramework.Rules;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static partial class TidyRoomScene
    {
        static readonly Vector3 BedPosition = new Vector3(-3.16f, 0f, 1.6f);
        static readonly Vector3 NightstandPosition = new Vector3(-3.98f, 0f, 2.95f);
        static readonly Vector3 BasketPosition = new Vector3(-2.55f, 0f, 3.4f);
        static readonly Vector3 WindowCenter = new Vector3(-0.6f, 0.9f, 3.8f);
        static readonly Vector3 FiddlePosition = new Vector3(1.0f, 0f, 3.4f);
        static readonly Vector3 DeskPosition = new Vector3(2.75f, 0f, 3.47f);
        static readonly Vector3 ChairPosition = new Vector3(2.7f, 0f, 2.82f);
        static readonly Vector3 WardrobePosition = new Vector3(4.01f, 0f, 1.25f);
        static readonly Vector3 SideboardPosition = new Vector3(4.02f, 0f, -1.2f);
        static readonly Vector3 DoorPosition = new Vector3(3.2f, 0f, -3.8f);
        static readonly Vector3 RackPosition = new Vector3(1.75f, 0f, -3.62f);
        static readonly Vector3 BookshelfPosition = new Vector3(-1.2f, 0f, -3.62f);
        static readonly Vector3 ChestPosition = new Vector3(-3.95f, 0f, -0.55f);
        static readonly Vector3 CoffeeTablePosition = new Vector3(-0.5f, 0f, 0.2f);
        static readonly Vector3 PoufPosition = new Vector3(0.6f, 0f, -0.55f);
        static readonly Vector3 ArmchairPosition = new Vector3(-2.75f, 0f, -2.55f);
        static readonly Vector3 FloorLampPosition = new Vector3(-3.45f, 0f, -3.25f);
        static readonly Vector3 DeskBinPosition = new Vector3(1.72f, 0f, 3.5f);
        static readonly Vector3 KitchenBinPosition = new Vector3(4.02f, 0f, -0.05f);
        static readonly Vector3 BucketPosition = new Vector3(3.62f, 0f, -2.35f);

        static void BuildFurniture()
        {
            BuildBedCorner();
            BuildWindowWall();
            BuildDeskCorner();
            BuildWardrobe();
            BuildKitchenette();
            BuildEntrance();
            BuildBookshelf();
            BuildToyChest();
            BuildLounge();
        }

        static void BuildBedCorner()
        {
            Put("RoomBed", furniture, BedPosition, 0f);
            Put("RoomNightstand", furniture, NightstandPosition, 90f);
            Put("RoomLamp", furniture, NightstandPosition + new Vector3(-0.05f, TidyRoomFurniture.NightstandHeight, 0.1f), 20f);
            Put("RoomPictureSunset", decor, new Vector3(-4.28f, 1.66f, BedPosition.z), 90f);
            var basket = Put("RoomLaundryBasket", furniture, BasketPosition, 0f);
            var container = TidySceneDressing.Target<TidyContainer>("Laundry Basket", targets, Vector3.zero, Quaternion.identity, "room_laundry_basket", "Laundry Basket", TidyDemoLevels.Filter(context.Category("RoomSocks")), new Color(0.95f, 0.85f, 0.6f), new Vector3(0f, 0.22f, 0f), new Vector3(0.5f, 0.46f, 0.5f), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
            container.transform.position = basket.transform.position + Vector3.up * 0.05f;
            container.ConfigureContainer(10, false, new Vector3(0.28f, 0.34f, 0.28f), Vector3.zero, TidyContainerLayout.Hidden, false);
            var stages = new List<GameObject>();
            var materials = new[] { context.Material("Room_Knit_Blue"), context.Material("Room_Knit_Pink"), context.Material("Room_Knit_Yellow") };
            for (var stage = 0; stage < 3; stage++)
            {
                var pile = TidySceneDressing.Decor("Laundry Fill " + (stage + 1), basket.transform, TidyRoomDecor.LaundryPile(stage), materials, new Vector3(0f, 0.12f + stage * 0.1f, 0f), Quaternion.Euler(0f, stage * 70f, 0f));
                pile.transform.localScale = Vector3.one * (0.85f + stage * 0.12f);
                pile.SetActive(false);
                stages.Add(pile);
            }

            container.ConfigureFillVisual(null, Vector2.zero, stages.ToArray());
        }

        static void BuildWindowWall()
        {
            Put("RoomWindow", environment, WindowCenter, 180f);
            Put("RoomRadiator", environment, new Vector3(WindowCenter.x, 0f, 3.7f), 180f);
            Put("RoomCurtainRod", environment, new Vector3(WindowCenter.x, 2.58f, 3.66f), 180f);
            foreach (var side in new[] { -1f, 1f })
            {
                Put("RoomCurtain", decor, new Vector3(WindowCenter.x + side * 1.48f, 2.56f, 3.72f), 180f);
            }

            Put("RoomPlantFiddle", furniture, FiddlePosition, 30f);
            Put("RoomPlantTrailing", decor, new Vector3(WindowCenter.x - 0.6f, WindowCenter.y + 0.003f, 3.6f), 0f);
            Put("RoomPendant", environment, new Vector3(-0.4f, RoomHeight, 0.2f), 0f);
        }

        static void BuildDeskCorner()
        {
            var desk = Put("RoomDesk", furniture, DeskPosition, 180f);
            Put("RoomChair", furniture, ChairPosition, 8f);
            Put("RoomLamp", decor, DeskPosition + new Vector3(0.5f, TidyRoomFurniture.DeskHeight, 0.15f), 200f, 0.75f);
            Put("RoomPlantTrailing", decor, DeskPosition + new Vector3(-0.52f, TidyRoomFurniture.DeskHeight, 0.18f), 0f);
            Put("RoomBin", furniture, DeskBinPosition, 0f);
            Bin("Desk Bin", "room_bin_desk", DeskBinPosition);
            var shelf = Put("RoomWallShelf", environment, new Vector3(DeskPosition.x, 1.45f, 3.8f), 180f);
            var group = TidySceneDressing.Target<TidySlotGroup>("Memory Shelf", targets, Vector3.zero, Quaternion.identity, "room_memory_shelf", "Memory Shelf", TidyDemoLevels.Filter(context.Category("RoomKeepsakes")), new Color(1f, 0.85f, 0.55f), new Vector3(0f, 0.1f, 0.1f), new Vector3(0.9f, 0.22f, 0.22f), TidyCompletionMode.WhenFull);
            group.transform.SetParent(shelf.transform, false);
            var anchors = new List<Transform>();
            foreach (var x in new[] { -0.28f, 0f, 0.28f })
            {
                var anchor = new GameObject("Keepsake Spot").transform;
                anchor.SetParent(group.transform, false);
                anchor.localPosition = new Vector3(x, 0f, 0.1f);
                anchors.Add(anchor);
            }

            group.SetManualAnchors(anchors);
            Put("RoomPictureBotanical", decor, new Vector3(DeskPosition.x - 0.05f, 1.95f, 3.78f), 180f);
            desk.name = "Desk";
        }

        static void BuildWardrobe()
        {
            var wardrobe = Put("RoomWardrobe", furniture, WardrobePosition, -90f);
            var center = TidyRoomFurniture.WardrobeShelfCenter;
            var tops = TidyDemoLevels.Filter(context.Category("RoomTops"));
            var knits = TidyDemoLevels.Filter(context.Category("RoomKnitwear"));
            Stack(wardrobe.transform, "room_wardrobe_tshirts_a", "T-Shirt Shelf", tops, new Vector3(center - 0.18f, TidyRoomFurniture.WardrobeShelves[2], 0.02f), 4);
            Stack(wardrobe.transform, "room_wardrobe_tshirts_b", "T-Shirt Shelf", tops, new Vector3(center + 0.18f, TidyRoomFurniture.WardrobeShelves[2], 0.02f), 4);
            Stack(wardrobe.transform, "room_wardrobe_sweaters_a", "Sweater Shelf", knits, new Vector3(center - 0.18f, TidyRoomFurniture.WardrobeShelves[1], 0.02f), 2);
            Stack(wardrobe.transform, "room_wardrobe_sweaters_b", "Sweater Shelf", knits, new Vector3(center + 0.18f, TidyRoomFurniture.WardrobeShelves[1], 0.02f), 2);
            var boxes = new ContentKit.TidyMeshBuilder(2);
            boxes.AddBeveledBox(new Vector3(-0.16f, 0.11f, 0f), new Vector3(0.3f, 0.22f, 0.4f), 0.01f, 2, 0);
            boxes.AddBeveledBox(new Vector3(0.17f, 0.08f, 0.02f), new Vector3(0.28f, 0.16f, 0.36f), 0.01f, 2, 1);
            var boxMesh = TidyMeshUtility.SaveAsIs(boxes, "SM_Room_StorageBoxes", MeshFolder);
            TidySceneDressing.Decor("Storage Boxes", wardrobe.transform, boxMesh, new[] { context.Material("Cardboard"), context.Material("Wicker") }, new Vector3(center, TidyRoomFurniture.WardrobeShelves[3], 0f), Quaternion.identity);
            TidySceneDressing.Decor("Spare Blanket", wardrobe.transform, TidyRoomDecor.LaundryPile(0), new[] { context.Material("Room_Throw"), context.Material("Room_Throw"), context.Material("Room_Throw") }, new Vector3(center, TidyRoomFurniture.WardrobeShelves[0], 0f), Quaternion.identity);
        }

        static void Stack(Transform wardrobe, string targetId, string displayName, TidyMatchFilter filter, Vector3 localPosition, int height)
        {
            var stack = TidySceneDressing.Target<TidyStackPoint>(displayName, targets, Vector3.zero, Quaternion.identity, targetId, displayName, filter, new Color(0.95f, 0.7f, 0.75f), new Vector3(0f, 0.14f, 0f), new Vector3(0.34f, 0.3f, 0.36f), TidyCompletionMode.WhenFull);
            stack.transform.SetParent(wardrobe, false);
            stack.transform.localPosition = localPosition;
            stack.ConfigureStack(height, 0.002f, Vector3.up, false, Vector3.zero, 4f);
        }

        static void BuildKitchenette()
        {
            var sideboard = Put("RoomSideboard", furniture, SideboardPosition, -90f);
            var top = TidyRoomFurniture.SideboardHeight;
            var tray = Put("RoomTray", decor, SideboardPosition + new Vector3(-0.04f, top, -0.15f), -90f);
            var zone = TidySceneDressing.Target<TidyFreePlacementZone>("Mug Tray", targets, Vector3.zero, Quaternion.identity, "room_mug_tray", "Mug Tray", TidyDemoLevels.Filter(context.Category("RoomMugs")), new Color(0.6f, 0.85f, 1f), new Vector3(0f, 0.06f, 0f), new Vector3(0.56f, 0.14f, 0.34f), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
            zone.transform.SetParent(tray.transform, false);
            zone.transform.localPosition = new Vector3(0f, 0.013f, 0f);
            zone.ConfigureArea(new Vector2(0.5f, 0.28f), 8, 0f, 15f, true);
            var board = Put("RoomPlateBoard", decor, SideboardPosition + new Vector3(-0.04f, top, 0.38f), 0f);
            var stack = TidySceneDressing.Target<TidyStackPoint>("Plate Stack", targets, Vector3.zero, Quaternion.identity, "room_plate_stack", "Plate Stack", TidyDemoLevels.Filter(context.Category("RoomPlates")), new Color(0.85f, 0.9f, 1f), new Vector3(0f, 0.08f, 0f), new Vector3(0.28f, 0.18f, 0.28f), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
            stack.transform.SetParent(board.transform, false);
            stack.transform.localPosition = new Vector3(0f, 0.018f, 0f);
            stack.ConfigureStack(6, 0.001f, Vector3.up, true, Vector3.zero, 2f);
            Put("RoomKettle", decor, SideboardPosition + new Vector3(0.12f, top, -0.56f), -60f);
            Put("RoomWallShelf", environment, new Vector3(4.3f, 1.55f, SideboardPosition.z), -90f);
            Put("RoomJars", decor, new Vector3(4.18f, 1.55f, SideboardPosition.z), -90f);
            Put("RoomPictureBotanical", decor, new Vector3(4.28f, 1.55f, KitchenBinPosition.z), -90f);
            Put("RoomBin", furniture, KitchenBinPosition, 0f);
            Bin("Kitchen Bin", "room_bin_kitchen", KitchenBinPosition);
            sideboard.name = "Kitchenette";
        }

        static void Bin(string displayName, string targetId, Vector3 position)
        {
            var bin = TidySceneDressing.Target<TidyContainer>(displayName, targets, Vector3.zero, Quaternion.identity, targetId, displayName, TidyMatchFilter.Create(null, new[] { context.Tag("Trash") }), new Color(0.7f, 0.65f, 0.55f), new Vector3(0f, 0.22f, 0f), new Vector3(0.4f, 0.46f, 0.4f), TidyCompletionMode.Never);
            bin.transform.position = position + Vector3.up * 0.03f;
            bin.ConfigureContainer(1, true, new Vector3(0.2f, 0.3f, 0.2f), Vector3.zero, TidyContainerLayout.Hidden, true);
        }

        static void BuildEntrance()
        {
            Put("RoomDoor", environment, DoorPosition, 0f);
            var rack = Put("RoomShoeRack", furniture, RackPosition, 0f);
            Put("RoomCoatRail", decor, new Vector3(RackPosition.x, 1.62f, -3.8f), 0f);
            var group = TidySceneDressing.Target<TidySlotGroup>("Shoe Rack", targets, Vector3.zero, Quaternion.identity, "room_shoe_rack", "Shoe Rack", TidyDemoLevels.Filter(context.Category("RoomShoes")), new Color(0.6f, 0.8f, 1f), new Vector3(0f, 0.32f, 0f), new Vector3(TidyRoomFurniture.RackWidth, 0.66f, TidyRoomFurniture.RackDepth), TidyCompletionMode.WhenFull);
            group.transform.SetParent(rack.transform, false);
            var anchors = new List<Transform>();
            foreach (var tier in TidyRoomFurniture.RackTiers)
            {
                foreach (var x in new[] { -0.29f, 0f, 0.29f })
                {
                    var anchor = new GameObject("Shoe Slot").transform;
                    anchor.SetParent(group.transform, false);
                    anchor.localPosition = new Vector3(x, tier, 0f);
                    anchors.Add(anchor);
                }
            }

            group.SetManualAnchors(anchors);
            Put("RoomPlantSnake", decor, RackPosition + new Vector3(-0.28f, 0.75f, 0f), 0f);
            Put("RoomBucket", furniture, BucketPosition, 0f);
            var bucket = TidySceneDressing.Target<TidyContainer>("Cleaning Bucket", targets, Vector3.zero, Quaternion.identity, "room_cleaning_bucket", "Cleaning Bucket", TidyDemoLevels.Filter(context.Category("RoomCleaning")), new Color(0.6f, 1f, 0.85f), new Vector3(0f, 0.15f, 0f), new Vector3(0.36f, 0.32f, 0.36f), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
            bucket.transform.position = BucketPosition + Vector3.up * 0.02f;
            bucket.ConfigureContainer(2, false, new Vector3(0.18f, 0.2f, 0.18f), Vector3.zero, TidyContainerLayout.Pile, false);
        }

        static void BuildBookshelf()
        {
            var shelf = Put("RoomBookshelf", furniture, BookshelfPosition, 0f);
            var heights = TidyLibraryModels.BookcaseShelfHeights(TidyRoomContentFurniture.BookcaseHeight, TidyRoomContentFurniture.BookcaseShelves);
            var inner = TidyRoomContentFurniture.BookcaseWidth - 0.1f;
            var row = TidySceneDressing.Target<TidyRowZone>("Book Shelf", targets, Vector3.zero, Quaternion.identity, "room_bookshelf_row", "Book Shelf", TidyDemoLevels.Filter(context.Category("RoomBooks")), new Color(1f, 0.8f, 0.6f), new Vector3(inner * 0.5f, 0.16f, 0f), new Vector3(inner, 0.32f, 0.28f), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
            row.transform.SetParent(shelf.transform, false);
            row.transform.localPosition = new Vector3(-inner * 0.5f, heights[1], 0.03f);
            row.ConfigureRow(inner, 0.004f, 12, Vector3.zero, TidyRowAlignment.Start);
            row.ConfigureGizmo(0.2f, 0.3f);
            var books = DecorBookMaterials();
            TidySceneDressing.Decor("Decor Books", shelf.transform, TidyDecorBooks.Row(inner, heights[1] - heights[0] - 0.03f, 4411, 0.85f), books, new Vector3(-inner * 0.5f, heights[0], 0.03f), Quaternion.identity);
            TidySceneDressing.Decor("Decor Books", shelf.transform, TidyDecorBooks.Row(inner * 0.55f, heights[3] - heights[2] - 0.03f, 4412, 0.9f), books, new Vector3(-inner * 0.5f, heights[2], 0.03f), Quaternion.identity);
            var plant = Put("RoomPlantTrailing", decor, shelf.transform.TransformPoint(new Vector3(0.28f, heights[2], 0.02f)), 0f);
            plant.transform.SetParent(shelf.transform, true);
            Put("RoomPlantSnake", decor, shelf.transform.TransformPoint(new Vector3(-0.25f, TidyRoomContentFurniture.BookcaseHeight, 0f)), 0f, 0.8f);
            Put("RoomPictureShapes", decor, new Vector3(-2.75f, 1.45f, -3.78f), 0f);
        }

        static Material[] DecorBookMaterials()
        {
            var colors = new[] { "Red", "Blue", "Green", "Purple", "Brown", "Teal", "Orange", "Yellow" };
            var materials = new Material[10];
            for (var index = 0; index < colors.Length; index++)
            {
                materials[index] = context.Material("Room_Glaze_" + colors[index]);
            }

            materials[8] = context.Material("Metal_Brass");
            materials[9] = context.Material("Paper_Pages");
            return materials;
        }

        static void BuildToyChest()
        {
            var chest = Put("RoomToyChest", furniture, ChestPosition, 90f);
            var container = TidySceneDressing.Target<TidyContainer>("Toy Chest", targets, Vector3.zero, Quaternion.identity, "room_toy_chest", "Toy Chest", TidyDemoLevels.Filter(context.Category("RoomToys")), new Color(1f, 0.85f, 0.4f), new Vector3(0f, 0.22f, 0f), new Vector3(TidyRoomFurniture.ChestWidth, 0.44f, TidyRoomFurniture.ChestDepth), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
            container.transform.SetParent(chest.transform, false);
            container.transform.localPosition = new Vector3(0f, 0.075f, 0f);
            container.ConfigureContainer(14, false, new Vector3(TidyRoomFurniture.ChestWidth - 0.1f, 0.36f, TidyRoomFurniture.ChestDepth - 0.1f), Vector3.zero, TidyContainerLayout.Pile, false);
        }

        static void BuildLounge()
        {
            Put("RoomCoffeeTable", furniture, CoffeeTablePosition, 0f);
            Put("RoomPouf", furniture, PoufPosition, 0f);
            Put("RoomArmchair", furniture, ArmchairPosition, 38f);
            Put("RoomFloorLamp", furniture, FloorLampPosition, 0f);
            Put("RoomPlantSnake", furniture, ArmchairPosition + new Vector3(-0.95f, 0f, 0.55f), 40f, 1.3f);
        }
    }
}
