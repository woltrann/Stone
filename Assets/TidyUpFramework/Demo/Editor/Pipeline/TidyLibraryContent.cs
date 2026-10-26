using System.Collections.Generic;
using TidyUpFramework.Objects;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public readonly struct TidyBookSpec
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Subject;
        public readonly string ColorTag;
        public readonly int ShapeIndex;
        public readonly int Style;
        public readonly int Sequence;

        public TidyBookSpec(string id, string title, string subject, string colorTag, int shapeIndex, int style, int sequence = 0)
        {
            Id = id;
            Title = title;
            Subject = subject;
            ColorTag = colorTag;
            ShapeIndex = shapeIndex;
            Style = style;
            Sequence = sequence;
        }
    }

    public static class TidyLibraryContent
    {
        public const string Theme = "Library";
        static readonly string MeshFolder = TidyDemoPaths.Meshes + "/" + Theme;
        static readonly string PrefabFolder = TidyDemoPaths.Prefabs + "/" + Theme;

        public static readonly TidyBookSpec[] Books =
        {
            new TidyBookSpec("book_arcane_basics", "Arcane Basics", "SpellBooks", "Purple", 1, 0),
            new TidyBookSpec("book_cantrips", "Cantrips for Beginners", "SpellBooks", "Purple", 0, 1),
            new TidyBookSpec("book_wards", "Wards and Seals", "SpellBooks", "Purple", 2, 2),
            new TidyBookSpec("book_illusions", "Gentle Illusions", "SpellBooks", "Purple", 3, 3),
            new TidyBookSpec("book_summoning", "Polite Summoning", "SpellBooks", "Purple", 4, 4),
            new TidyBookSpec("book_moss", "Moss and Fern", "Botany", "Green", 1, 5),
            new TidyBookSpec("book_herbal", "Herbal Remedies", "Botany", "Green", 2, 1),
            new TidyBookSpec("book_mushrooms", "A Field of Mushrooms", "Botany", "Green", 0, 3),
            new TidyBookSpec("book_roots", "Roots and Tubers", "Botany", "Green", 3, 0),
            new TidyBookSpec("book_kingdoms", "Old Kingdoms", "History", "Red", 2, 2),
            new TidyBookSpec("book_voyages", "Sea Voyages", "History", "Red", 1, 4),
            new TidyBookSpec("book_castles", "Castles of the North", "History", "Red", 3, 5),
            new TidyBookSpec("book_coins", "Coinage Through Time", "History", "Red", 5, 1),
            new TidyBookSpec("book_constellations", "Constellations", "StarCharts", "Blue", 3, 0),
            new TidyBookSpec("book_moon", "Phases of the Moon", "StarCharts", "Blue", 1, 2),
            new TidyBookSpec("book_comets", "Comets and Their Tails", "StarCharts", "Blue", 0, 4),
            new TidyBookSpec("book_tides", "Tides and Stars", "StarCharts", "Blue", 5, 3)
        };

        public static readonly string[] Subjects = { "SpellBooks", "Botany", "History", "StarCharts" };
        public const int EncyclopaediaVolumes = 8;

        public static void Build(TidyDemoContext context)
        {
            TidyAssetFactory.EnsureFolder(MeshFolder);
            TidyAssetFactory.EnsureFolder(PrefabFolder);
            BuildCategories(context);
            BuildBooks(context);
            BuildEncyclopaedia(context);
            BuildPotions(context);
            BuildSmallProps(context);
            BuildFurniture(context);
            AssetDatabase.SaveAssets();
        }

        static void BuildCategories(TidyDemoContext context)
        {
            var books = TidyDemoDataBuilder.Category(context, "Books", "Books", new Color32(176, 120, 74, 255));
            TidyDemoDataBuilder.Category(context, "SpellBooks", "Spell Books", TidyArtLibrary.PaletteColor("Purple"), books);
            TidyDemoDataBuilder.Category(context, "Botany", "Botany", TidyArtLibrary.PaletteColor("Green"), books);
            TidyDemoDataBuilder.Category(context, "History", "History", TidyArtLibrary.PaletteColor("Red"), books);
            TidyDemoDataBuilder.Category(context, "StarCharts", "Star Charts", TidyArtLibrary.PaletteColor("Blue"), books);
            TidyDemoDataBuilder.Category(context, "Encyclopaedia", "Encyclopaedia Arcana", new Color32(60, 170, 168, 255), books);
            TidyDemoDataBuilder.Category(context, "Potions", "Potions", new Color32(126, 205, 255, 255));
            TidyDemoDataBuilder.Category(context, "Scrolls", "Scrolls", new Color32(236, 220, 186, 255));
            TidyDemoDataBuilder.Category(context, "Candles", "Candles", new Color32(255, 200, 120, 255));
            TidyDemoDataBuilder.Category(context, "Curiosities", "Curiosities", new Color32(170, 120, 255, 255));
        }

        static void BuildBooks(TidyDemoContext context)
        {
            var shapes = new Mesh[TidyBookModels.Shapes.Length];
            for (var index = 0; index < shapes.Length; index++)
            {
                shapes[index] = TidyBookModels.Build(TidyBookModels.Shapes[index], MeshFolder);
            }

            var seed = 100;
            foreach (var spec in Books)
            {
                var color = TidyArtLibrary.PaletteColor(spec.ColorTag);
                var cover = Color.Lerp(color, new Color(0.12f, 0.08f, 0.08f), 0.35f);
                var accent = new Color32(226, 186, 110, 255);
                CreateBook(context, spec, shapes[spec.ShapeIndex], cover, accent, seed++);
            }
        }

        static void CreateBook(TidyDemoContext context, TidyBookSpec spec, Mesh mesh, Color cover, Color accent, int seed)
        {
            var atlas = TidyArtLibrary.SaveTexture("T_" + spec.Id, TidyBookModels.BuildAtlas(cover, accent, spec.Style, seed));
            var leather = TidyArtLibrary.Plain(context, "Leather_" + spec.Id, cover, 0.35f);
            var decor = TidyArtLibrary.Textured(context, "Cover_" + spec.Id, atlas, 0.4f);
            var tags = new List<TidyTagDefinition> { context.Tag(spec.ColorTag) };
            if (spec.Subject == "SpellBooks")
            {
                tags.Add(context.Tag("Magic"));
            }

            var shape = TidyBookModels.Shapes[spec.ShapeIndex];
            tags.Add(context.Tag(shape.Height >= 0.28f ? "Large" : shape.Height <= 0.2f ? "Small" : "Medium"));
            var definition = TidyDemoDataBuilder.Definition(context, Theme, spec.Id, spec.Title, context.Category(spec.Subject), tags, shape.Height * 10f, shape.Thickness * 20f, 0, spec.Title, spec.Sequence, true, false, 12, InspectText(spec.Subject));
            TidyPropFactory.Interactive(PrefabFolder + "/" + PrefabName(spec.Id) + ".prefab", mesh, new[] { leather, decor, context.Material("Paper_Pages") }, definition);
        }

        static void BuildEncyclopaedia(TidyDemoContext context)
        {
            var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/SM_Book_Tall.asset");
            var teal = new Color32(40, 110, 108, 255);
            for (var volume = 1; volume <= EncyclopaediaVolumes; volume++)
            {
                var id = "book_encyclopaedia_" + volume;
                var spec = new TidyBookSpec(id, "Encyclopaedia Arcana, Vol. " + Roman(volume), "Encyclopaedia", "Teal", 3, 2, volume);
                CreateBook(context, spec, mesh, teal, new Color32(236, 204, 120, 255), 300 + volume);
            }
        }

        static void BuildPotions(TidyDemoContext context)
        {
            var shapes = new[] { TidyPotionShape.Round, TidyPotionShape.Tall, TidyPotionShape.Flat };
            var meshes = new Mesh[shapes.Length];
            for (var index = 0; index < shapes.Length; index++)
            {
                meshes[index] = TidyLibraryModels.Potion(shapes[index], MeshFolder);
            }

            var colors = new[] { ("Red", "Ember Draught"), ("Blue", "Moonwater"), ("Green", "Verdant Tonic") };
            foreach (var (colorTag, potionName) in colors)
            {
                for (var shapeIndex = 0; shapeIndex < shapes.Length; shapeIndex++)
                {
                    var id = "potion_" + colorTag.ToLowerInvariant() + "_" + shapes[shapeIndex].ToString().ToLowerInvariant();
                    var tags = new List<TidyTagDefinition> { context.Tag(colorTag), context.Tag("Fragile"), context.Tag("Magic"), context.Tag(shapeIndex == 1 ? "Large" : "Small") };
                    var display = potionName + " (" + shapes[shapeIndex] + ")";
                    var definition = TidyDemoDataBuilder.Definition(context, Theme, id, display, context.Category("Potions"), tags, shapeIndex == 1 ? 1.6f : 1f, 0.4f, 1, display, 0, false, false, 15, "A carefully corked " + colorTag.ToLowerInvariant() + " potion. Handle with care.");
                    TidyPropFactory.Interactive(PrefabFolder + "/" + PrefabName(id) + ".prefab", meshes[shapeIndex], new[] { context.Material("Glass_" + colorTag), context.Material("Liquid_" + colorTag), context.Material("Cork") }, definition, TidyColliderShape.Box);
                }
            }
        }

        static void BuildSmallProps(TidyDemoContext context)
        {
            var scrollMesh = TidyLibraryModels.Scroll(MeshFolder);
            for (var index = 0; index < 3; index++)
            {
                var id = "scroll_" + index;
                var names = new[] { "Map Scroll", "Recipe Scroll", "Decree Scroll" };
                var definition = TidyDemoDataBuilder.Definition(context, Theme, id, names[index], context.Category("Scrolls"), new[] { context.Tag("White"), context.Tag("Small") }, 0.8f, 0.2f, 0, names[index], 0, true, false, 8, "Rolled parchment tied with brass.");
                TidyPropFactory.Interactive(PrefabFolder + "/" + PrefabName(id) + ".prefab", scrollMesh, new[] { context.Material("Paper_Parchment"), context.Material("Metal_Brass") }, definition);
            }

            var candleMesh = TidyLibraryModels.Candle(MeshFolder, 0.12f);
            var candle = TidyDemoDataBuilder.Definition(context, Theme, "candle", "Beeswax Candle", context.Category("Candles"), new[] { context.Tag("White"), context.Tag("Small") }, 0.6f, 0.3f, 0, "Candle", 0, false, false, 10, "Smells faintly of honey.");
            TidyPropFactory.Interactive(PrefabFolder + "/Candle.prefab", candleMesh, new[] { context.Material("Wax_Cream"), context.Material("Wick"), context.Material("Metal_Brass") }, candle);
            TidyLibraryModels.Flame(MeshFolder);

            var crystalColors = new[] { "Purple", "Teal", "Pink" };
            for (var index = 0; index < crystalColors.Length; index++)
            {
                var id = "crystal_" + crystalColors[index].ToLowerInvariant();
                var mesh = TidyLibraryModels.Crystal(MeshFolder, index);
                var definition = TidyDemoDataBuilder.Definition(context, Theme, id, crystalColors[index] + " Crystal Cluster", context.Category("Curiosities"), new[] { context.Tag(crystalColors[index]), context.Tag("Magic"), context.Tag("Fragile") }, 0.6f, 0.5f, 2, "Crystal " + crystalColors[index], 0, false, false, 20, "It hums when nobody is listening.");
                TidyPropFactory.Interactive(PrefabFolder + "/" + PrefabName(id) + ".prefab", mesh, new[] { context.Material("Liquid_" + crystalColors[index]), context.Material("Metal_Brass") }, definition);
            }

            var inkMesh = TidyLibraryModels.InkBottle(MeshFolder);
            var ink = TidyDemoDataBuilder.Definition(context, Theme, "ink_bottle", "Ink Bottle", context.Category("Curiosities"), new[] { context.Tag("Blue"), context.Tag("Small"), context.Tag("Fragile") }, 0.4f, 0.3f, 0, "Ink", 0, false, false, 8, "Midnight blue, never quite dry.");
            TidyPropFactory.Interactive(PrefabFolder + "/InkBottle.prefab", inkMesh, new[] { context.Material("Glass_Blue"), context.Material("Dark") }, ink);

            var quillMesh = TidyLibraryModels.Quill(MeshFolder);
            var quill = TidyDemoDataBuilder.Definition(context, Theme, "quill", "Quill", context.Category("Curiosities"), new[] { context.Tag("White"), context.Tag("Small") }, 0.3f, 0.05f, 0, "Quill", 0, false, false, 8, "A goose feather sharpened to a fine nib.");
            TidyPropFactory.Interactive(PrefabFolder + "/Quill.prefab", quillMesh, new[] { context.Material("White"), context.Material("Metal_Brass") }, quill);
        }

        static void BuildFurniture(TidyDemoContext context)
        {
            var walnut = context.Material("Wood_Walnut");
            var mahogany = context.Material("Wood_Mahogany");
            var brass = context.Material("Metal_Brass");
            Static(context, "Bookcase", TidyLibraryModels.Bookcase(MeshFolder, 1.6f, 2.6f, 0.42f, 5, out _), new[] { walnut }, TidyColliderShape.Mesh);
            Static(context, "BookcaseTall", TidyLibraryModels.Bookcase(MeshFolder, 1.6f, 3.6f, 0.42f, 7, out _), new[] { walnut }, TidyColliderShape.Mesh);
            Static(context, "ReadingTable", TidyLibraryModels.ReadingTable(MeshFolder, 2.6f, 1.1f, 0.78f), new[] { mahogany }, TidyColliderShape.Box);
            Static(context, "SideTable", TidyLibraryModels.ReadingTable(MeshFolder, 1.2f, 0.8f, 0.76f), new[] { mahogany }, TidyColliderShape.Box);
            Static(context, "Chair", TidyLibraryModels.Chair(MeshFolder), new[] { walnut, context.Material("Fabric_Red") }, TidyColliderShape.Box);
            Static(context, "Candelabra", TidyLibraryModels.Candelabra(MeshFolder, 4, out _), new[] { brass }, TidyColliderShape.Box);
            Static(context, "PotionRack", TidyLibraryModels.PotionRack(MeshFolder, 3, 2, 0.15f, 0.24f, out _), new[] { mahogany }, TidyColliderShape.Box);
            Static(context, "ScrollBasket", TidyLibraryModels.Basket(MeshFolder, 0.26f, 0.32f), new[] { context.Material("Wicker"), context.Material("Wood_Oak") }, TidyColliderShape.Box);
            Static(context, "Pedestal", TidyLibraryModels.Pedestal(MeshFolder, 0.5f, 1.0f), new[] { context.Material("Stone_Wall"), context.Material("Fabric_Red") }, TidyColliderShape.Box);
            Static(context, "Column", TidyLibraryModels.Column(MeshFolder, 5.2f), new[] { context.Material("Plaster_Cream") }, TidyColliderShape.Convex);
            Static(context, "Chandelier", TidyLibraryModels.Chandelier(MeshFolder, 8, 0.7f, out _), new[] { brass }, TidyColliderShape.None);
            Static(context, "Lectern", TidyLibraryModels.Lectern(MeshFolder), new[] { walnut }, TidyColliderShape.Box);
            Static(context, "Globe", TidyLibraryModels.Globe(MeshFolder), new[] { brass, context.Material("Paper_Parchment") }, TidyColliderShape.Box);
            var frame = TidyLibraryModels.ArchedWindow(MeshFolder, 2.2f, 3.8f, out var glass);
            var window = TidyPropFactory.Visual("ArchedWindow", frame, walnut);
            var glassObject = TidyPropFactory.Visual("Glass", glass, context.Material("Window_Glow"));
            glassObject.transform.SetParent(window.transform, false);
            context.AddPrefab(TidyAssetFactory.SavePrefab(window, PrefabFolder + "/ArchedWindow.prefab"));
        }

        static void Static(TidyDemoContext context, string prefabName, Mesh mesh, Material[] materials, TidyColliderShape collider)
        {
            context.AddPrefab(TidyPropFactory.Static(PrefabFolder + "/" + prefabName + ".prefab", mesh, materials, collider));
        }

        static string InspectText(string subject)
        {
            switch (subject)
            {
                case "SpellBooks":
                    return "The pages shimmer faintly. It belongs with the other spell books.";
                case "Botany":
                    return "Pressed leaves fall out when you open it.";
                case "History":
                    return "Dusty, heavy and full of dates.";
                case "StarCharts":
                    return "Fold-out maps of the night sky.";
                default:
                    return "Part of the great Encyclopaedia Arcana. Keep the volumes in order.";
            }
        }

        static string PrefabName(string id)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var part in id.Split('_'))
            {
                if (part.Length > 0)
                {
                    builder.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1));
                }
            }

            return builder.ToString();
        }

        static string Roman(int value)
        {
            var numerals = new[] { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return value >= 1 && value <= numerals.Length ? numerals[value - 1] : value.ToString();
        }
    }
}
