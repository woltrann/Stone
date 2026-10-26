using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using TidyUpFramework.Objects;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyStoreContent
    {
        public const string Theme = "Store";
        public const string ParentCategory = "StoreCollectibles";
        public const string PodCategory = "StorePalPods";
        public static readonly string MeshFolder = TidyDemoPaths.Meshes + "/" + Theme;
        static readonly string PrefabFolder = TidyDemoPaths.Prefabs + "/" + Theme;

        public static readonly TidyPalSpecies[] Species = { TidyPalSpecies.Bear, TidyPalSpecies.Bunny, TidyPalSpecies.Cat, TidyPalSpecies.Frog, TidyPalSpecies.Robot };
        public static readonly string[] SpeciesPlural = { "Bears", "Bunnies", "Cats", "Frogs", "Robots" };
        public static readonly string[] SpeciesAccent = { "Orange", "Pink", "Purple", "Green", "Teal" };
        public static readonly string[] Colors = { "Red", "Blue", "Green", "Yellow", "Purple", "Pink" };
        public static readonly string[] ColorFlavours = { "Cherry", "Blueberry", "Mint", "Lemon", "Grape", "Bubblegum" };
        public static readonly string[] PodColors = { "Red", "Blue", "Green", "Yellow" };

        static readonly Color[] SpeciesPastels =
        {
            new Color32(255, 222, 190, 255),
            new Color32(255, 214, 228, 255),
            new Color32(226, 212, 250, 255),
            new Color32(206, 238, 200, 255),
            new Color32(198, 232, 240, 255)
        };

        public static string SpeciesCategory(TidyPalSpecies species) => "Pal" + species;

        public static string PalId(TidyPalSpecies species, string colorTag) => "pal_" + species.ToString().ToLowerInvariant() + "_" + colorTag.ToLowerInvariant();

        public static string PodId(string colorTag) => "pod_" + colorTag.ToLowerInvariant();

        public static string ShelfPrefabName(TidyPalSpecies species) => "StoreShelf_" + species;

        public static string BinPrefabName(string colorTag) => "StorePodBin_" + colorTag;

        public static void Build(TidyDemoContext context)
        {
            TidyAssetFactory.EnsureFolder(MeshFolder);
            TidyAssetFactory.EnsureFolder(PrefabFolder);
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.Objects + "/" + Theme);
            BuildMaterials(context);
            BuildCategories(context);
            BuildPals(context);
            BuildPods(context);
            BuildFixtures(context);
            AssetDatabase.SaveAssets();
        }

        static void BuildMaterials(TidyDemoContext context)
        {
            TidyArtLibrary.Plain(context, "Pal_Cream", new Color32(255, 246, 230, 255), 0.55f);
            TidyArtLibrary.Plain(context, "Pal_Eye", new Color32(30, 24, 34, 255), 0.92f);
            TidyArtLibrary.Plain(context, "Pal_Blush", new Color32(255, 138, 150, 255), 0.4f);
            TidyArtLibrary.Plain(context, "Store_Mint", new Color32(196, 236, 222, 255), 0.45f);
            TidyArtLibrary.Plain(context, "Store_Cream", new Color32(255, 248, 236, 255), 0.4f);
            TidyArtLibrary.Plain(context, "Store_Coral", new Color32(255, 150, 140, 255), 0.45f);
            TidyArtLibrary.Plain(context, "Store_SignInk", new Color32(70, 46, 66, 255), 0.3f);
            for (var index = 0; index < Species.Length; index++)
            {
                TidyArtLibrary.Plain(context, "Store_Pastel_" + Species[index], SpeciesPastels[index], 0.35f);
            }

            foreach (var colorTag in PodColors)
            {
                var color = TidyArtLibrary.PaletteColor(colorTag);
                var texture = TidyTextureBuilder.Label(256, 64, new Color32(255, 248, 236, 255), color, new Color32(60, 40, 60, 255), 2, colorTag.Length * 17);
                var asset = TidyArtLibrary.SaveTexture("T_StorePodLabel_" + colorTag, texture, false, 256);
                TidyArtLibrary.Textured(context, "Store_PodLabel_" + colorTag, asset, 0.5f);
            }

            Emissive(context, "Store_Sparkle", new Color32(255, 236, 160, 255), new Color(2.2f, 1.8f, 0.8f));
            Emissive(context, "Store_Window", new Color32(255, 244, 222, 255), new Color(1.6f, 1.5f, 1.3f));
        }

        static void Emissive(TidyDemoContext context, string key, Color color, Color emission)
        {
            var material = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_" + key + ".mat", new TidyMaterialSpec { BaseColor = color, Emission = emission, Smoothness = 0.6f });
            context.AddMaterial(key, material);
        }

        static void BuildCategories(TidyDemoContext context)
        {
            var parent = TidyDemoDataBuilder.Category(context, ParentCategory, "Pebble Pals", new Color32(255, 170, 120, 255));
            for (var index = 0; index < Species.Length; index++)
            {
                TidyDemoDataBuilder.Category(context, SpeciesCategory(Species[index]), SpeciesPlural[index], TidyArtLibrary.PaletteColor(SpeciesAccent[index]), parent);
            }

            TidyDemoDataBuilder.Category(context, PodCategory, "Pal Pods", new Color32(255, 214, 110, 255));
        }

        static void BuildPals(TidyDemoContext context)
        {
            foreach (var species in Species)
            {
                var mesh = TidyStoreModels.Pal(species, MeshFolder);
                for (var colorIndex = 0; colorIndex < Colors.Length; colorIndex++)
                {
                    CreatePal(context, species, colorIndex, mesh);
                }
            }
        }

        static void CreatePal(TidyDemoContext context, TidyPalSpecies species, int colorIndex, Mesh mesh)
        {
            var colorTag = Colors[colorIndex];
            var id = PalId(species, colorTag);
            var display = ColorFlavours[colorIndex] + " " + species;
            var tags = new List<TidyTagDefinition> { context.Tag(colorTag), context.Tag("Small"), context.Tag("Collectible"), context.Tag(species == TidyPalSpecies.Robot ? "Boxy" : "Round") };
            var definition = TidyDemoDataBuilder.Definition(context, Theme, id, display, context.Category(SpeciesCategory(species)), tags, 1f, 0.15f, 0, display, 0, false, false, 6, InspectText(species, colorIndex));
            var materials = new[] { context.Material("Plastic_" + colorTag), context.Material("Pal_Cream"), context.Material("Pal_Eye"), context.Material("Pal_Blush") };
            TidyPropFactory.Interactive(PrefabFolder + "/Pal" + species + colorTag + ".prefab", mesh, materials, definition);
        }

        static string InspectText(TidyPalSpecies species, int colorIndex)
        {
            var flavour = ColorFlavours[colorIndex].ToLowerInvariant();
            switch (species)
            {
                case TidyPalSpecies.Bear:
                    return "A chubby Pebble Pal bear in " + flavour + " vinyl. Bears live on the orange shelves.";
                case TidyPalSpecies.Bunny:
                    return "Ears up! A " + flavour + " bunny. Bunnies hop onto the pink shelves.";
                case TidyPalSpecies.Cat:
                    return "A " + flavour + " cat with a curly tail. Cats nap on the lilac shelves.";
                case TidyPalSpecies.Frog:
                    return "A " + flavour + " frog with big round eyes. Frogs sit on the green shelves.";
                default:
                    return "Beep boop. A " + flavour + " robot. Robots dock on the teal shelves.";
            }
        }

        static void BuildPods(TidyDemoContext context)
        {
            var mesh = TidyStoreModels.Pod(MeshFolder);
            foreach (var colorTag in PodColors)
            {
                var id = PodId(colorTag);
                var display = colorTag + " Pal Pod";
                var tags = new[] { context.Tag(colorTag), context.Tag("Small"), context.Tag("Round"), context.Tag("Collectible") };
                var definition = TidyDemoDataBuilder.Definition(context, Theme, id, display, context.Category(PodCategory), tags, 0.9f, 0.1f, 0, display, 0, false, false, 8, "A mystery capsule with a surprise Pal inside. Pods go into the bin with the matching colour.");
                var materials = new[] { context.Material("Plastic_" + colorTag), context.Material("Glass_Clear"), context.Material("Store_PodLabel_" + colorTag), context.Material("Pal_Cream") };
                TidyPropFactory.Interactive(PrefabFolder + "/PalPod" + colorTag + ".prefab", mesh, materials, definition);
            }
        }

        static void BuildFixtures(TidyDemoContext context)
        {
            var shelf = TidyStoreFixtures.ShelfUnit(MeshFolder);
            for (var index = 0; index < Species.Length; index++)
            {
                Static(context, ShelfPrefabName(Species[index]), shelf, new[] { context.Material("Store_Cream"), context.Material("Store_Pastel_" + Species[index]), context.Material("Plastic_" + SpeciesAccent[index]) }, TidyColliderShape.Mesh);
            }

            Static(context, "StoreShelf_Decor", shelf, new[] { context.Material("Store_Cream"), context.Material("Store_Mint"), context.Material("Store_Coral") }, TidyColliderShape.Mesh);
            Static(context, "StoreTierStand", TidyStoreFixtures.TierStand(MeshFolder), new[] { context.Material("Store_Cream"), context.Material("Metal_Brass") }, TidyColliderShape.Mesh);
            Static(context, "StoreSetPedestal", TidyStoreFixtures.SetPedestal(MeshFolder), new[] { context.Material("Store_Mint"), context.Material("Plastic_Pink"), context.Material("Metal_Brass") }, TidyColliderShape.Box);
            Static(context, "StoreDisplayTable", TidyStoreFixtures.DisplayTable(MeshFolder, 2.4f, 0.8f), new[] { context.Material("Wood_Oak"), context.Material("Store_Coral") }, TidyColliderShape.Box);
            Static(context, "StoreTripletTray", TidyStoreFixtures.TripletTray(MeshFolder), new[] { context.Material("Wood_Walnut"), context.Material("Store_Cream") }, TidyColliderShape.Box);
            Static(context, "StoreCounter", TidyStoreFixtures.Counter(MeshFolder), new[] { context.Material("Store_Pastel_Bunny"), context.Material("Wood_Oak"), context.Material("Rubber_Dark") }, TidyColliderShape.Box);
            Static(context, "StoreRegister", TidyStoreFixtures.Register(MeshFolder), new[] { context.Material("Plastic_Teal"), context.Material("Dark"), context.Material("White") }, TidyColliderShape.Box);
            var bin = TidyStoreFixtures.PodBin(MeshFolder);
            foreach (var colorTag in PodColors)
            {
                Static(context, BinPrefabName(colorTag), bin, new[] { context.Material("Plastic_" + colorTag), context.Material("White") }, TidyColliderShape.Mesh);
            }
        }

        static void Static(TidyDemoContext context, string prefabName, Mesh mesh, Material[] materials, TidyColliderShape collider)
        {
            context.AddPrefab(TidyPropFactory.Static(PrefabFolder + "/" + prefabName + ".prefab", mesh, materials, collider));
        }
    }
}
