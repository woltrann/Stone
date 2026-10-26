using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyRoomContentFurniture
    {
        public const float BookcaseWidth = 1.0f;
        public const float BookcaseHeight = 1.7f;
        public const int BookcaseShelves = 4;

        static TidyDemoContext context;
        static string folder;

        public static void Build(TidyDemoContext demoContext, string prefabFolder)
        {
            context = demoContext;
            folder = prefabFolder;
            var oak = M("Wood_Oak");
            var painted = M("Wood_Painted");
            var brass = M("Metal_Brass");
            Static("RoomBed", TidyRoomFurniture.Bed(), new[] { oak, M("Fabric_Cream"), M("Fabric_Bed"), M("White"), M("Room_Throw"), M("Room_Pouf") }, TidyColliderShape.Mesh);
            Static("RoomWardrobe", TidyRoomFurniture.Wardrobe(), new[] { painted, oak, brass, M("Room_Knit_Blue"), M("Room_Curtain") }, TidyColliderShape.Mesh);
            Static("RoomDesk", TidyRoomFurniture.Desk(), new[] { oak, painted, brass }, TidyColliderShape.Mesh);
            Static("RoomNightstand", TidyRoomFurniture.Nightstand(), new[] { painted, oak, brass }, TidyColliderShape.Box);
            Static("RoomToyChest", TidyRoomFurniture.ToyChest(), new[] { M("Room_Sage"), oak, brass }, TidyColliderShape.Mesh);
            Static("RoomShoeRack", TidyRoomFurniture.ShoeRack(), new[] { oak, painted }, TidyColliderShape.Mesh);
            Static("RoomSideboard", TidyRoomFurniture.Sideboard(), new[] { M("Room_Sage"), oak, brass }, TidyColliderShape.Box);
            Static("RoomCoffeeTable", TidyRoomFurniture.CoffeeTable(), new[] { oak, M("Wood_Walnut") }, TidyColliderShape.Mesh);
            Static("RoomPouf", TidyRoomFurniture.Pouf(), new[] { M("Room_Pouf"), M("Room_Mustard") }, TidyColliderShape.Box);
            Static("RoomArmchair", TidyRoomFurniture.Armchair(), new[] { M("Room_Throw"), M("Wood_Walnut"), M("Room_Curtain") }, TidyColliderShape.Box);
            Static("RoomFloorLamp", TidyRoomDecor.FloorLamp(), new[] { brass, M("Fabric_Cream"), M("Lamp_Glow") }, TidyColliderShape.Box);
            Static("RoomDoor", TidyRoomFurniture.Door(), new[] { painted, brass }, TidyColliderShape.Box);
            Static("RoomRadiator", TidyRoomFurniture.Radiator(), new[] { M("White") }, TidyColliderShape.Box);
            Static("RoomBookshelf", TidyLibraryModels.Bookcase(TidyRoomModels.Folder, BookcaseWidth, BookcaseHeight, 0.32f, BookcaseShelves, out _), new[] { painted }, TidyColliderShape.Mesh);
            Static("RoomChair", AssetDatabase.LoadAssetAtPath<Mesh>(TidyDemoPaths.Meshes + "/Library/SM_Chair.asset"), new[] { oak, M("Room_Pouf") }, TidyColliderShape.Box);
            Static("RoomLaundryBasket", TidyLibraryModels.Basket(TidyRoomModels.Folder, TidyRoomDecor.BasketRadius, TidyRoomDecor.BasketHeight), new[] { M("Wicker"), oak }, TidyColliderShape.Mesh);
            Static("RoomBin", TidyRoomDecor.Bin(), new[] { M("Room_Sage"), M("Dark"), M("Metal_Steel") }, TidyColliderShape.Mesh);
            Static("RoomBucket", TidyRoomDecor.Bucket(), new[] { M("Plastic_Yellow"), M("Matte_Yellow"), M("Metal_Steel") }, TidyColliderShape.Mesh);
            Static("RoomLamp", TidyRoomDecor.Lamp(), new[] { M("Room_Glaze_Teal"), brass, M("Fabric_Cream"), M("Lamp_Glow") }, TidyColliderShape.Box);
            Static("RoomPendant", TidyRoomDecor.Pendant(), new[] { brass, M("Wicker"), M("Lamp_Glow") }, TidyColliderShape.None);
            Static("RoomTray", TidyRoomDecor.Tray(0.56f, 0.34f), new[] { M("Wood_Walnut") }, TidyColliderShape.Mesh);
            Static("RoomPlateBoard", TidyRoomDecor.PlateBoard(), new[] { M("Cork") }, TidyColliderShape.Box);
            Static("RoomKettle", TidyRoomDecor.Kettle(), new[] { M("Room_Glaze_White"), M("Wood_Walnut") }, TidyColliderShape.Box);
            Static("RoomJars", TidyRoomDecor.Jars(), new[] { M("Glass_Clear"), M("Wood_Oak"), M("Room_Mustard") }, TidyColliderShape.None);
            Static("RoomVase", TidyRoomDecor.Vase(), new[] { M("Glass_Clear"), M("Room_Stem"), M("Matte_Pink"), M("Matte_Yellow") }, TidyColliderShape.None);
            Static("RoomCoatRail", TidyRoomDecor.CoatRail(), new[] { oak, brass, M("Room_Knit_Red") }, TidyColliderShape.None);
            Static("RoomWallShelf", TidyRoomDecor.WallShelf(0.9f, 0.2f), new[] { oak, brass }, TidyColliderShape.Box);
            Static("RoomCurtain", TidyRoomDecor.Curtain(0.85f, 2.35f), new[] { M("Room_Curtain") }, TidyColliderShape.None);
            Static("RoomCurtainRod", TidyRoomDecor.CurtainRod(TidyRoomDecor.WindowWidth + 1.2f), new[] { brass }, TidyColliderShape.None);
            Picture("RoomPictureSunset", "SM_Room_Picture_Large", 0.9f, 0.62f, oak, M("Room_Painting_Sunset"));
            Picture("RoomPictureShapes", "SM_Room_Picture_Tall", 0.46f, 0.6f, M("White"), M("Room_Painting_Shapes"));
            Picture("RoomPictureBotanical", "SM_Room_Picture_Square", 0.44f, 0.44f, M("Wood_Walnut"), M("Room_Painting_Botanical"));
            var plantMaterials = new[] { M("Terracotta"), M("Room_Soil"), M("Plant_Green"), M("Room_LeafDark") };
            Plant("RoomPlantSnake", TidyRoomDecor.PottedPlant(0), plantMaterials, 0.12f, 0.22f);
            Plant("RoomPlantTrailing", TidyRoomDecor.PottedPlant(1), plantMaterials, 0.08f, 0.12f);
            Plant("RoomPlantFiddle", TidyRoomDecor.PottedPlant(2), plantMaterials, 0.21f, 0.38f);
            Window();
        }

        static void Static(string prefabName, Mesh mesh, Material[] materials, TidyColliderShape collider)
        {
            TidyRoomProps.Static(context, folder, prefabName, mesh, materials, collider);
        }

        static void Plant(string prefabName, Mesh mesh, Material[] materials, float potRadius, float potHeight)
        {
            var root = TidyPropFactory.Visual(prefabName, mesh, materials);
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, potHeight * 0.5f, 0f);
            box.size = new Vector3(potRadius * 2f, potHeight, potRadius * 2f);
            context.AddPrefab(TidyAssetFactory.SavePrefab(root, folder + "/" + prefabName + ".prefab"));
        }

        static void Picture(string prefabName, string meshName, float width, float height, Material frameMaterial, Material painting)
        {
            var frame = TidyRoomDecor.PictureFrame(meshName, width, height, out var canvas);
            var root = TidyPropFactory.Visual(prefabName, frame, frameMaterial);
            var canvasObject = TidyPropFactory.Visual("Canvas", canvas, painting);
            canvasObject.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            canvasObject.transform.SetParent(root.transform, false);
            context.AddPrefab(TidyAssetFactory.SavePrefab(root, folder + "/" + prefabName + ".prefab"));
        }

        static void Window()
        {
            var frameMesh = TidyRoomDecor.Window(out var glassMesh);
            var window = TidyPropFactory.Visual("RoomWindow", frameMesh, M("Wood_Painted"), M("Wood_Painted"));
            var glass = TidyPropFactory.Visual("Glass", glassMesh, M("Room_Sunset"));
            glass.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            glass.transform.SetParent(window.transform, false);
            context.AddPrefab(TidyAssetFactory.SavePrefab(window, folder + "/RoomWindow.prefab"));
        }

        static Material M(string key)
        {
            return context.Material(key);
        }
    }
}
