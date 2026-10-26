using System.Collections.Generic;
using TidyUpFramework.Cleaning;
using TidyUpFramework.Objects;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyRoomContent
    {
        public const string Theme = "Room";
        public static readonly string PrefabFolder = TidyDemoPaths.Prefabs + "/" + Theme;
        static readonly string LibraryMeshFolder = TidyDemoPaths.Meshes + "/Library";

        public static readonly string[] ShirtColors = { "Red", "Blue", "Yellow", "Teal" };
        public static readonly string[] SweaterColors = { "Pink", "Green", "Purple", "Orange" };
        public static readonly string[] SockColors = { "White", "Pink", "Blue", "Yellow" };
        public static readonly string[] SneakerColors = { "Red", "Blue", "White", "Green", "Pink" };
        public static readonly string[] MugColors = { "Blue", "Yellow", "Pink", "Green", "Teal" };
        public static readonly string[] BlockColors = { "Red", "Blue", "Yellow", "Green" };
        public static readonly (string id, string name, string color, int kind)[] Plushies =
        {
            ("room_plush_bear", "Honey Bear", "Brown", 0),
            ("room_plush_bunny", "Velvet Bunny", "Pink", 1),
            ("room_plush_cat", "Marmalade Cat", "Orange", 2),
            ("room_plush_frog", "Pond Frog", "Green", 3)
        };

        public static readonly (string id, string title, string color, string shape, int style)[] Books =
        {
            ("room_book_recipes", "Cosy Soups and Stews", "Orange", "Standard", 1),
            ("room_book_garden", "Windowsill Gardens", "Green", "Thick", 3),
            ("room_book_knitting", "Knitting for Rainy Days", "Pink", "Slim", 0),
            ("room_book_stars", "Stargazing from Bed", "Blue", "Tall", 2),
            ("room_book_poems", "Little Poems", "Purple", "Pocket", 4),
            ("room_book_travel", "Tea Around the World", "Teal", "Tome", 5)
        };

        public static readonly string[] PaperBalls = { "room_paper_0", "room_paper_1", "room_paper_2" };
        public static readonly string[] SodaCans = { "room_soda_red", "room_soda_teal" };
        public static readonly string[] Wrappers = { "room_wrapper_orange", "room_wrapper_purple" };
        public static readonly string[] DustBunnies = { "room_dust_0", "room_dust_1", "room_dust_2" };

        public static void Build(TidyDemoContext context)
        {
            TidyAssetFactory.EnsureFolder(TidyRoomModels.Folder);
            TidyAssetFactory.EnsureFolder(PrefabFolder);
            TidyRoomArt.Build(context);
            BuildCategories(context);
            BuildClothes(context);
            BuildShoes(context);
            BuildDishes(context);
            BuildBooks(context);
            BuildToys(context);
            BuildKeepsakes(context);
            BuildTools(context);
            BuildTrash(context);
            TidyRoomContentFurniture.Build(context, PrefabFolder);
            AssetDatabase.SaveAssets();
        }

        static void BuildCategories(TidyDemoContext context)
        {
            var clothing = TidyDemoDataBuilder.Category(context, "RoomClothing", "Clothes", new Color32(236, 170, 160, 255));
            TidyDemoDataBuilder.Category(context, "RoomTops", "T-Shirts", new Color32(240, 150, 140, 255), clothing);
            TidyDemoDataBuilder.Category(context, "RoomKnitwear", "Sweaters", new Color32(200, 160, 220, 255), clothing);
            TidyDemoDataBuilder.Category(context, "RoomSocks", "Socks", new Color32(250, 220, 170, 255), clothing);
            TidyDemoDataBuilder.Category(context, "RoomShoes", "Shoes", new Color32(150, 190, 230, 255));
            var dishes = TidyDemoDataBuilder.Category(context, "RoomDishes", "Dishes", new Color32(170, 220, 230, 255));
            TidyDemoDataBuilder.Category(context, "RoomMugs", "Mugs", new Color32(150, 210, 230, 255), dishes);
            TidyDemoDataBuilder.Category(context, "RoomPlates", "Plates", new Color32(230, 236, 240, 255), dishes);
            TidyDemoDataBuilder.Category(context, "RoomBooks", "Books", new Color32(200, 150, 110, 255));
            TidyDemoDataBuilder.Category(context, "RoomToys", "Toys", new Color32(250, 200, 110, 255));
            TidyDemoDataBuilder.Category(context, "RoomKeepsakes", "Keepsakes", new Color32(255, 214, 140, 255));
            TidyDemoDataBuilder.Category(context, "RoomCleaning", "Cleaning Tools", new Color32(140, 220, 200, 255));
            TidyDemoDataBuilder.Category(context, "RoomTrash", "Rubbish", new Color32(160, 150, 130, 255));
        }

        static void BuildClothes(TidyDemoContext context)
        {
            var shirt = TidyRoomModels.FoldedShirt();
            foreach (var color in ShirtColors)
            {
                var id = "room_tshirt_" + color.ToLowerInvariant();
                var definition = Define(context, id, color + " T-Shirt", "RoomTops", new[] { color, "Medium" }, 1f, 0.2f, true, 10, "Freshly washed and folded. It smells of sunshine.");
                Interactive(id, shirt, new[] { Mat(context, "Room_Knit_" + color), Mat(context, "Room_Knit_White") }, definition);
            }

            var sweater = TidyRoomModels.FoldedSweater();
            foreach (var color in SweaterColors)
            {
                var id = "room_sweater_" + color.ToLowerInvariant();
                var definition = Define(context, id, color + " Sweater", "RoomKnitwear", new[] { color, "Medium" }, 1.3f, 0.4f, true, 12, "A chunky hand-knitted jumper for chilly evenings.");
                Interactive(id, sweater, new[] { Mat(context, "Room_Knit_" + color), Mat(context, "Room_Knit_" + color) }, definition);
            }

            var sock = TidyRoomModels.SockBall();
            foreach (var color in SockColors)
            {
                var id = "room_socks_" + color.ToLowerInvariant();
                var accent = color == "White" ? "Room_Knit_Pink" : "Room_Knit_White";
                var definition = Define(context, id, color + " Socks", "RoomSocks", new[] { color, "Small", "Round" }, 0.4f, 0.05f, false, 6, "A rolled-up pair. They definitely need a wash.");
                Interactive(id, sock, new[] { Mat(context, "Room_Knit_" + color), Mat(context, accent) }, definition);
            }
        }

        static void BuildShoes(TidyDemoContext context)
        {
            var sneaker = TidyRoomModels.SneakerPair();
            foreach (var color in SneakerColors)
            {
                var id = "room_sneakers_" + color.ToLowerInvariant();
                var definition = Define(context, id, color + " Sneakers", "RoomShoes", new[] { color, "Medium" }, 1.2f, 0.6f, false, 12, "A comfy pair of canvas sneakers.");
                Interactive(id, sneaker, new[] { Mat(context, "Matte_" + color), Mat(context, "Room_RubberWhite"), Mat(context, "White") }, definition);
            }

            var leather = TidyArtLibrary.Plain(context, "Room_Leather", new Color32(132, 84, 56, 255), 0.45f);
            var boots = Define(context, "room_boots_brown", "Ankle Boots", "RoomShoes", new[] { "Brown", "Medium" }, 1.5f, 0.9f, false, 14, "Fleece-lined boots for autumn walks.");
            Interactive("room_boots_brown", TidyRoomModels.Boots(), new[] { leather, Mat(context, "Rubber_Dark"), Mat(context, "Room_Knit_White") }, boots);
        }

        static void BuildDishes(TidyDemoContext context)
        {
            var mug = TidyRoomModels.Mug();
            foreach (var color in MugColors)
            {
                var id = "room_mug_" + color.ToLowerInvariant();
                var definition = Define(context, id, color + " Mug", "RoomMugs", new[] { color, "Small", "Fragile" }, 0.6f, 0.3f, false, 10, "Still a little cocoa at the bottom.");
                Interactive(id, mug, new[] { Mat(context, "Room_Glaze_" + color), Mat(context, "Ceramic_White") }, definition);
            }

            var plate = Define(context, "room_plate", "Dinner Plate", "RoomPlates", new[] { "White", "Medium", "Fragile", "Round" }, 1f, 0.4f, true, 10, "A speckled plate with a blue rim.");
            Interactive("room_plate", TidyRoomModels.Plate(), new[] { Mat(context, "Ceramic_White"), Mat(context, "Room_Glaze_Blue") }, plate);
        }

        static void BuildBooks(TidyDemoContext context)
        {
            var seed = 900;
            foreach (var (id, title, color, shapeKey, style) in Books)
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(LibraryMeshFolder + "/SM_Book_" + shapeKey + ".asset");
                var shape = ShapeFor(shapeKey);
                var cover = Color.Lerp(TidyArtLibrary.PaletteColor(color), Color.white, 0.12f);
                var atlas = TidyArtLibrary.SaveTexture("T_" + id, TidyBookModels.BuildAtlas(cover, new Color32(250, 236, 200, 255), style, seed++));
                var leather = TidyArtLibrary.Plain(context, "Room_Cloth_" + id, cover, 0.25f);
                var decor = TidyArtLibrary.Textured(context, "Room_Cover_" + id, atlas, 0.3f);
                var size = shape.Height >= 0.28f ? "Large" : shape.Height <= 0.2f ? "Small" : "Medium";
                var definition = Define(context, id, title, "RoomBooks", new[] { color, size, "Boxy" }, shape.Height * 10f, shape.Thickness * 20f, true, 10, "A well-loved book with a ribbon bookmark.", title);
                Interactive(id, mesh, new[] { leather, decor, Mat(context, "Paper_Pages") }, definition);
            }
        }

        static TidyBookShape ShapeFor(string key)
        {
            foreach (var shape in TidyBookModels.Shapes)
            {
                if (shape.Key == key)
                {
                    return shape;
                }
            }

            return TidyBookModels.Shapes[1];
        }

        static void BuildToys(TidyDemoContext context)
        {
            foreach (var (id, name, color, kind) in Plushies)
            {
                var definition = Define(context, id, name, "RoomToys", new[] { color, "Medium", "Round" }, 1.2f, 0.2f, false, 12, "Soft, squishy and very huggable.");
                Interactive(id, TidyRoomModels.Plush(kind), new[] { Mat(context, "Room_Knit_" + color), Mat(context, "Room_Knit_White"), Mat(context, "Room_Nose") }, definition);
            }

            var block = TidyRoomModels.ToyBlock();
            foreach (var color in BlockColors)
            {
                var id = "room_block_" + color.ToLowerInvariant();
                var definition = Define(context, id, color + " Toy Block", "RoomToys", new[] { color, "Small", "Boxy" }, 0.5f, 0.1f, true, 6, "A wooden alphabet block, worn smooth at the corners.");
                Interactive(id, block, new[] { Mat(context, "Matte_" + color), Mat(context, "Wood_Painted") }, definition);
            }
        }

        static void BuildKeepsakes(TidyDemoContext context)
        {
            var key = Define(context, "room_keepsake_key", "Grandma's Key", "RoomKeepsakes", new[] { "Yellow", "Small", "Collectible" }, 0.3f, 0.05f, false, 30, "The key to Grandma's old cottage. You thought it was lost forever.", null, 3);
            TidyRoomProps.Interactive(PrefabPath("room_keepsake_key"), TidyRoomModels.Key(), new[] { Mat(context, "Metal_Brass"), Mat(context, "Plastic_Red") }, key, TidyColliderShape.Box, true, false);
            var photo = Define(context, "room_keepsake_photo", "Holiday Photo", "RoomKeepsakes", new[] { "Brown", "Small", "Collectible", "Fragile" }, 0.5f, 0.2f, false, 30, "That summer at the seaside. Everybody was laughing.", null, 3);
            var frame = TidyRoomModels.PhotoFrame(out var picture);
            var photoMaterial = Mat(context, "Room_Photo");
            TidyRoomProps.Interactive(PrefabPath("room_keepsake_photo"), frame, new[] { Mat(context, "Wood_Oak") }, photo, TidyColliderShape.Box, true, false, root => TidyPropFactory.Visual("Photo", picture, photoMaterial).transform.SetParent(root.transform, false));
            var ring = Define(context, "room_keepsake_ring", "Velvet Ring Box", "RoomKeepsakes", new[] { "Purple", "Small", "Collectible", "Boxy" }, 0.3f, 0.1f, false, 30, "Something precious is waiting inside.", null, 3);
            TidyRoomProps.Interactive(PrefabPath("room_keepsake_ring"), TidyRoomModels.RingBox(), new[] { Mat(context, "Room_Velvet"), Mat(context, "Metal_Brass") }, ring, TidyColliderShape.Box, true, false);
        }

        static void BuildTools(TidyDemoContext context)
        {
            var sponge = Define(context, "room_sponge", "Scrubbing Sponge", "RoomCleaning", new[] { "Yellow", "Small" }, 0.4f, 0.05f, false, 5, "Hold it against a stain and keep scrubbing until it sparkles.");
            sponge.ConfigureCarrying(false);
            TidyRoomProps.Interactive(PrefabPath("room_sponge"), TidyRoomModels.Sponge(), new[] { Mat(context, "Matte_Yellow"), Mat(context, "Matte_Green") }, sponge, TidyColliderShape.Box, false, false, root => root.AddComponent<TidyCleaningTool>().Configure(3f, 0.32f, false, 0f));
            var vacuum = Define(context, "room_vacuum", "Handheld Vacuum", "RoomCleaning", new[] { "Teal", "Medium" }, 1.2f, 0.8f, false, 5, "Point it at dust bunnies and keep it running. Whoosh!");
            vacuum.ConfigureCarrying(false);
            TidyRoomProps.Interactive(PrefabPath("room_vacuum"), TidyRoomModels.HandVacuum(), new[] { Mat(context, "Plastic_White"), Mat(context, "Glass_Teal"), Mat(context, "Plastic_Teal"), Mat(context, "Rubber_Dark") }, vacuum, TidyColliderShape.Box, false, false, root => root.AddComponent<TidyCleaningTool>().Configure(1.5f, 0.2f, true, 0.75f));
        }

        static void BuildTrash(TidyDemoContext context)
        {
            for (var index = 0; index < PaperBalls.Length; index++)
            {
                var definition = Define(context, PaperBalls[index], "Crumpled Note", "RoomTrash", new[] { "White", "Small", "Trash", "Round" }, 0.3f, 0.02f, false, 4, "A scribbled shopping list. Into the bin it goes.");
                Clutter(PaperBalls[index], TidyRoomModels.PaperBall(40 + index), new[] { Mat(context, "Paper_Parchment") }, definition);
            }

            var cans = new[] { ("Red", false), ("Teal", true) };
            for (var index = 0; index < cans.Length; index++)
            {
                var definition = Define(context, SodaCans[index], cans[index].Item1 + " Soda Can", "RoomTrash", new[] { cans[index].Item1, "Small", "Trash" }, 0.4f, 0.03f, false, 4, "Empty. Somebody forgot to recycle it.");
                Clutter(SodaCans[index], TidyRoomModels.SodaCan(cans[index].Item2), new[] { Mat(context, "Plastic_" + cans[index].Item1), Mat(context, "Room_Foil") }, definition);
            }

            var wrappers = new[] { "Orange", "Purple" };
            for (var index = 0; index < wrappers.Length; index++)
            {
                var definition = Define(context, Wrappers[index], "Snack Wrapper", "RoomTrash", new[] { wrappers[index], "Small", "Trash" }, 0.3f, 0.01f, false, 4, "Crinkly and covered in crumbs.");
                Clutter(Wrappers[index], TidyRoomModels.Wrapper(60 + index), new[] { Mat(context, "Plastic_" + wrappers[index]), Mat(context, "Room_Foil") }, definition);
            }

            for (var index = 0; index < DustBunnies.Length; index++)
            {
                var definition = Define(context, DustBunnies[index], "Dust Bunny", "RoomTrash", new[] { "White", "Small", "Trash" }, 0.3f, 0.01f, false, 3, "A fluffy ball of dust. The vacuum will make short work of it.");
                Clutter(DustBunnies[index], TidyRoomModels.DustBunny(80 + index), new[] { Mat(context, "Room_DustFluff") }, definition);
            }
        }

        static TidyObjectDefinition Define(TidyDemoContext context, string id, string displayName, string category, string[] tagNames, float size, float weight, bool stackable, int score, string inspect, string sortKey = null, int priority = 0)
        {
            var tags = new List<TidyTagDefinition>();
            foreach (var tagName in tagNames)
            {
                tags.Add(context.Tag(tagName));
            }

            return TidyDemoDataBuilder.Definition(context, Theme, id, displayName, context.Category(category), tags, size, weight, priority, sortKey ?? displayName, 0, stackable, false, score, inspect);
        }

        static void Interactive(string id, Mesh mesh, Material[] materials, TidyObjectDefinition definition)
        {
            TidyRoomProps.Interactive(PrefabPath(id), mesh, materials, definition, TidyColliderShape.Box, false, false);
        }

        static void Clutter(string id, Mesh mesh, Material[] materials, TidyObjectDefinition definition)
        {
            TidyRoomProps.Interactive(PrefabPath(id), mesh, materials, definition, TidyColliderShape.Box, false, true);
        }

        static string PrefabPath(string id)
        {
            return PrefabFolder + "/" + TidyRoomProps.PrefabName(id) + ".prefab";
        }

        static Material Mat(TidyDemoContext context, string key)
        {
            return context.Material(key);
        }
    }
}
