using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using TidyUpFramework.Levels;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Rules;
using TidyUpFramework.Transformation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyLibraryScene
    {
        const float HallWidth = 14f;
        const float HallLength = 20f;
        const float HallHeight = 6f;
        const float BookcaseHeight = 2.6f;
        const float BookcaseInnerWidth = 1.48f;
        const float RowDepthOffset = 0.06f;
        const float GalleryHeight = 2.7f;
        const string MeshFolder = TidyDemoPaths.Meshes + "/Library";
        static readonly Color Warm = new Color(1f, 0.78f, 0.52f);

        static TidyDemoContext context;
        static Material[] decorMaterials;
        static Transform environment;
        static Transform props;
        static Transform targets;
        static Transform clutter;
        static System.Random random;

        public static void Build(TidyDemoContext demoContext)
        {
            context = demoContext;
            random = new System.Random(2024);
            var scene = TidySceneKit.NewScene(TidyDemoPaths.LibraryScene);
            var level = AssetDatabase.LoadAssetAtPath<TidyLevelDefinition>(TidyDemoPaths.Levels + "/Level_01_ArcaneLibrary.asset");
            decorMaterials = TidyDecorBooks.Materials(context);
            environment = TidySceneKit.Group("Environment");
            props = TidySceneKit.Group("Furniture");
            targets = TidySceneKit.Group("Placement Targets");
            clutter = TidySceneKit.Group("Clutter");

            var volumes = BuildLighting();
            BuildShell();
            BuildColumnsAndGallery();
            BuildWindows();
            var chandelierLights = BuildChandeliers();
            BuildBookcases();
            BuildReadingArea();
            BuildPotionCorner();
            BuildCuriosities();
            BuildClutter();
            BuildBeforeAfter(volumes, chandelierLights);
            TidySceneKit.MarkStatic(environment);
            TidySceneKit.MarkStatic(props);
            TidySceneKit.ReflectionProbe(new Vector3(0f, 2.5f, 0f), new Vector3(HallWidth, HallHeight, HallLength), 0.6f);
            TidySceneKit.Gameplay(level, new Vector3(0f, 0.02f, -8.4f), 0f);
            TidySceneKit.Save(scene, TidyDemoPaths.LibraryScene);
        }

        static (Volume messy, Volume organised) BuildLighting()
        {
            var spec = new TidyLightingSpec
            {
                SkyColor = new Color(0.52f, 0.44f, 0.44f),
                EquatorColor = new Color(0.46f, 0.34f, 0.27f),
                GroundColor = new Color(0.2f, 0.14f, 0.11f),
                SunColor = new Color(1f, 0.88f, 0.74f),
                SunIntensity = 0.45f,
                SunEuler = new Vector3(38f, 200f, 0f),
                FogColor = new Color(0.22f, 0.16f, 0.13f),
                FogDensity = 0.018f,
                PostExposure = 0.25f,
                Saturation = 10f,
                Temperature = 14f,
                BloomIntensity = 0.7f
            };
            var sun = TidySceneKit.Lighting(TidyDemoPaths.LibraryScene, spec, out var organised);
            sun.shadows = LightShadows.None;
            var messySpec = new TidyLightingSpec
            {
                PostExposure = -0.1f,
                Saturation = -28f,
                Contrast = -5f,
                Temperature = -12f,
                BloomIntensity = 0.2f,
                VignetteIntensity = 0.4f
            };
            var messy = TidySceneKit.CreateVolume(TidyDemoPaths.LibraryScene, "Messy Volume", messySpec, 1f, 1);
            organised.weight = 0f;
            organised.priority = 2;
            return (messy, organised);
        }

        static void BuildShell()
        {
            TidyRoomShell.Build(new TidyRoomSpec
            {
                Name = "Library",
                Size = new Vector3(HallWidth, HallHeight, HallLength),
                Floor = context.Material("Floor_Herringbone"),
                Wall = context.Material("Wallpaper_Library"),
                Ceiling = context.Material("Wood_Walnut"),
                Trim = context.Material("Wood_Walnut"),
                Wainscot = context.Material("Wood_Mahogany"),
                WainscotHeight = 1.2f,
                BeamSpacing = 2.5f
            }, environment);

            var rug = new TidyMeshBuilder();
            rug.AddPlane(new Vector2(3.4f, 5f), 1, 1, 0, null, true);
            var rugMesh = TidyMeshUtility.SaveAsIs(rug, "SM_Library_Rug", MeshFolder);
            foreach (var z in new[] { -3f, 3f })
            {
                TidySceneDressing.Decor("Rug", environment, rugMesh, new[] { context.Material("Rug_Library") }, new Vector3(0f, 0.004f, z), Quaternion.identity);
            }
        }

        static void BuildColumnsAndGallery()
        {
            var column = context.Prefab("Column");
            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -7.5f, -2.5f, 2.5f, 7.5f })
                {
                    TidySceneKit.Place(column, environment, new Vector3(side * 6.62f, 0f, z), Vector3.zero, new Vector3(0.8f, 1.15f, 0.8f));
                }
            }

            var gallery = new TidyMeshBuilder(2);
            foreach (var side in new[] { -1f, 1f })
            {
                gallery.AddBeveledBox(new Vector3(side * 6.3f, GalleryHeight - 0.06f, 0f), new Vector3(1.0f, 0.14f, HallLength), 0.02f, 2, 0);
                gallery.AddBeveledBox(new Vector3(side * 5.84f, GalleryHeight + 0.98f, 0f), new Vector3(0.09f, 0.07f, HallLength), 0.02f, 2, 0);
                for (var z = -HallLength * 0.5f + 0.2f; z < HallLength * 0.5f; z += 0.24f)
                {
                    gallery.AddLathe(new[] { new Vector2(0.03f, 0f), new Vector2(0.028f, 0.06f), new Vector2(0.018f, 0.12f), new Vector2(0.03f, 0.4f), new Vector2(0.016f, 0.8f), new Vector2(0.026f, 0.86f), new Vector2(0.026f, 0.95f) }, 8, 1, Matrix4x4.Translate(new Vector3(side * 5.84f, GalleryHeight, z)));
                }
            }

            var candle = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/SM_Candle_12.asset");
            var flame = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/SM_Flame.asset");
            var sconce = new TidyMeshBuilder();
            sconce.AddBeveledBox(new Vector3(0f, 0f, 0f), new Vector3(0.12f, 0.24f, 0.03f), 0.01f, 2);
            sconce.AddBeveledBox(new Vector3(0f, -0.06f, 0.09f), new Vector3(0.03f, 0.03f, 0.18f), 0.008f, 1);
            sconce.AddLathe(new[] { new Vector2(0.05f, 0f), new Vector2(0.055f, 0.01f), new Vector2(0.035f, 0.02f), new Vector2(0.03f, 0.028f) }, 14, 0, Matrix4x4.Translate(new Vector3(0f, -0.06f, 0.18f)));
            var sconceMesh = TidyMeshUtility.SaveAsIs(sconce, "SM_Sconce", MeshFolder);
            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -7.5f, -2.5f, 2.5f, 7.5f })
                {
                    var holder = new GameObject("Sconce").transform;
                    holder.SetParent(environment, false);
                    holder.position = new Vector3(side * 6.24f, 2.05f, z);
                    holder.rotation = Quaternion.Euler(0f, side < 0f ? 90f : -90f, 0f);
                    TidySceneDressing.Decor("Bracket", holder, sconceMesh, new[] { context.Material("Metal_Brass") }, Vector3.zero, Quaternion.identity);
                    TidySceneDressing.Decor("Candle", holder, candle, new[] { context.Material("Wax_Cream"), context.Material("Wick"), context.Material("Metal_Brass") }, new Vector3(0f, -0.04f, 0.18f), Quaternion.identity);
                    TidySceneDressing.Decor("Flame", holder, flame, new[] { context.Material("Flame") }, new Vector3(0f, 0.09f, 0.18f), Quaternion.identity).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    var light = TidySceneKit.PointLight("Sconce Light", holder, holder.TransformPoint(new Vector3(0f, 0.18f, 0.32f)), Warm, 2.6f, 5.5f);
                    light.gameObject.AddComponent<TidyCandleFlicker>();
                }
            }

            var galleryMesh = TidyMeshUtility.SaveAsIs(gallery, "SM_Library_Gallery", MeshFolder);
            var galleryObject = TidySceneDressing.Decor("Gallery", environment, galleryMesh, new[] { context.Material("Wood_Walnut"), context.Material("Wood_Mahogany") }, Vector3.zero, Quaternion.identity);
            TidyPropFactory.AddCollider(galleryObject, galleryMesh, TidyColliderShape.Mesh);
        }

        static void BuildWindows()
        {
            var window = context.Prefab("ArchedWindow");
            foreach (var x in new[] { -2.6f, 2.6f })
            {
                TidySceneKit.Place(window, environment, new Vector3(x, 1.5f, HallLength * 0.5f - 0.02f), new Vector3(0f, 180f, 0f));
                TidySceneKit.SpotLight("Window Light", environment, new Vector3(x, 4.2f, HallLength * 0.5f - 0.3f), new Vector3(28f, 180f, 0f), new Color(1f, 0.86f, 0.66f), 18f, 18f, 70f);
            }

            TidySceneKit.Place(window, environment, new Vector3(0f, 2.2f, -HallLength * 0.5f + 0.02f), Vector3.zero, new Vector3(0.8f, 0.8f, 1f));
        }

        static List<Light> BuildChandeliers()
        {
            var lights = new List<Light>();
            var chandelier = context.Prefab("Chandelier");
            var flame = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/SM_Flame.asset");
            var candle = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/SM_Candle_12.asset");
            var points = TidyLibraryModels.ChandelierCandlePoints(8, 0.7f);
            var index = 0;
            foreach (var z in new[] { -5f, 0f, 5f })
            {
                var instance = TidySceneKit.Place(chandelier, environment, new Vector3(0f, HallHeight, z), Vector3.zero);
                foreach (var point in points)
                {
                    TidySceneDressing.Decor("Candle", instance.transform, candle, new[] { context.Material("Wax_Cream"), context.Material("Wick"), context.Material("Metal_Brass") }, point, Quaternion.identity).transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                    TidySceneDressing.Decor("Flame", instance.transform, flame, new[] { context.Material("Flame") }, point + new Vector3(0f, 0.12f, 0f), Quaternion.identity).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }

                var light = TidySceneKit.PointLight("Chandelier Light", instance.transform, instance.transform.position + new Vector3(0f, -1.3f, 0f), Warm, 9f, 13f, index == 1);
                lights.Add(light);
                index++;
            }

            return lights;
        }

        static void BuildBookcases()
        {
            var bookcase = context.Prefab("Bookcase");
            var shelves = TidyLibraryModels.BookcaseShelfHeights(BookcaseHeight, 5);
            var bays = new[] { -5f, 0f, 5f };
            var assignments = new Dictionary<(int side, int bay, int slot), string>
            {
                { (-1, 0, 0), "SpellBooks" },
                { (-1, 1, 0), "Botany" },
                { (-1, 2, 0), "History" },
                { (1, 0, 0), "StarCharts" },
                { (1, 1, 0), "Encyclopaedia" }
            };

            foreach (var side in new[] { -1, 1 })
            {
                for (var bay = 0; bay < bays.Length; bay++)
                {
                    for (var slot = 0; slot < 2; slot++)
                    {
                        var z = bays[bay] + (slot == 0 ? -0.84f : 0.84f) * -side;
                        var position = new Vector3(side * 6.76f, 0f, z);
                        var rotation = Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f);
                        var instance = TidySceneKit.Place(bookcase, props, position, rotation.eulerAngles);
                        assignments.TryGetValue((side, bay, slot), out var subject);
                        DressBookcase(instance.transform, shelves, subject, bay * 10 + slot + (side > 0 ? 100 : 0));
                        var upper = TidySceneKit.Place(bookcase, props, position + new Vector3(0f, GalleryHeight + 0.02f, 0f), rotation.eulerAngles);
                        DressBookcase(upper.transform, shelves, null, 500 + bay * 10 + slot + (side > 0 ? 100 : 0));
                    }
                }
            }
        }

        static void DressBookcase(Transform bookcase, float[] shelves, string subject, int seed)
        {
            var interactiveShelves = new HashSet<int>();
            if (subject == "SpellBooks")
            {
                interactiveShelves.Add(2);
                interactiveShelves.Add(3);
                AddRow(bookcase, shelves[2], "shelf_spell_am", "Spell Books A-M", SubjectFilter("SpellBooks", TidyDemoLevels.AlphabeticalRule("SpellBooks_AM", 'A', 'M')), TidyOrderingMode.None);
                AddRow(bookcase, shelves[3], "shelf_spell_nz", "Spell Books N-Z", SubjectFilter("SpellBooks", TidyDemoLevels.AlphabeticalRule("SpellBooks_NZ", 'N', 'Z')), TidyOrderingMode.None);
                Plaque(bookcase, "Spell Books", "Purple");
            }
            else if (subject == "Encyclopaedia")
            {
                interactiveShelves.Add(2);
                var row = AddRow(bookcase, shelves[2], "shelf_encyclopaedia", "Encyclopaedia Arcana", SubjectFilter("Encyclopaedia", null), TidyOrderingMode.Sequence);
                row.ConfigureBehaviour(true, false, true);
                Plaque(bookcase, "Encyclopaedia", "Teal");
            }
            else if (subject != null)
            {
                interactiveShelves.Add(2);
                var display = context.Category(subject).DisplayName;
                AddRow(bookcase, shelves[2], "shelf_" + TidyUpFramework.Editor.TidyEditorUtility.Slug(subject), display, SubjectFilter(subject, null), TidyOrderingMode.None);
                Plaque(bookcase, display, PlaqueColor(subject));
            }

            for (var index = 0; index < shelves.Length; index++)
            {
                if (interactiveShelves.Contains(index))
                {
                    continue;
                }

                var spacing = index < shelves.Length - 1 ? shelves[index + 1] - shelves[index] - 0.03f : 0.42f;
                var mesh = TidyDecorBooks.Row(BookcaseInnerWidth, spacing, seed * 7 + index, subject == null ? 0.92f : 0.6f);
                TidySceneDressing.Decor("Decor Books", bookcase, mesh, decorMaterials, new Vector3(-BookcaseInnerWidth * 0.5f, shelves[index], RowDepthOffset), Quaternion.identity);
            }
        }

        static TidyRowZone AddRow(Transform bookcase, float shelfHeight, string targetId, string displayName, TidyMatchFilter filter, TidyOrderingMode ordering)
        {
            var row = TidySceneDressing.Target<TidyRowZone>("Shelf " + displayName, targets, Vector3.zero, Quaternion.identity, targetId, displayName, filter, new Color(0.8f, 0.6f, 1f), new Vector3(BookcaseInnerWidth * 0.5f, 0.16f, 0f), new Vector3(BookcaseInnerWidth, 0.34f, 0.38f));
            row.transform.SetParent(bookcase, false);
            row.transform.localPosition = new Vector3(BookcaseInnerWidth * 0.5f, shelfHeight, RowDepthOffset);
            row.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            row.ConfigureRow(BookcaseInnerWidth, 0.003f, 30, new Vector3(0f, 180f, 0f), TidyRowAlignment.Start);
            row.ConfigureGizmo(0.22f, 0.32f);
            row.ConfigureOrdering(ordering, TidyOrderingDirection.Ascending, TidyGroupingMode.None, TidyTagGroup.Color);
            return row;
        }

        static TidyMatchFilter SubjectFilter(string subject, TidyRuleDefinition rule)
        {
            return TidyMatchFilter.Create(new[] { context.Category(subject) }, null, rule);
        }

        static string PlaqueColor(string subject)
        {
            switch (subject)
            {
                case "Botany":
                    return "Green";
                case "History":
                    return "Red";
                case "StarCharts":
                    return "Blue";
                default:
                    return "Purple";
            }
        }

        static void Plaque(Transform bookcase, string text, string colorTag)
        {
            var plaque = new TidyMeshBuilder(2);
            plaque.AddBeveledBox(new Vector3(0f, 0f, 0f), new Vector3(0.7f, 0.16f, 0.02f), 0.01f, 2, 0);
            plaque.AddSphere(new Vector3(-0.29f, 0f, 0.016f), 0.022f, 12, 8, 1);
            plaque.AddSphere(new Vector3(0.29f, 0f, 0.016f), 0.022f, 12, 8, 1);
            var mesh = TidyMeshUtility.SaveAsIs(plaque, "SM_Plaque_" + colorTag, MeshFolder);
            var plaqueObject = TidySceneDressing.Decor("Plaque", bookcase, mesh, new[] { context.Material("Metal_Brass"), context.Material("Liquid_" + colorTag) }, new Vector3(0f, BookcaseHeight - 0.05f, 0.265f), Quaternion.identity);
            TidySceneDressing.Label(text, plaqueObject.transform, new Vector3(0f, 0f, 0.0115f), Quaternion.Euler(0f, 180f, 0f), 0.75f, new Color(0.22f, 0.13f, 0.06f), new Vector2(0.52f, 0.12f));
        }

        static void BuildReadingArea()
        {
            var table = context.Prefab("ReadingTable");
            var chair = context.Prefab("Chair");
            var candelabra = context.Prefab("Candelabra");
            var candlePoints = TidyLibraryModels.CandelabraCandlePoints(4);
            var tableIndex = 0;
            foreach (var z in new[] { -3f, 3f })
            {
                TidySceneKit.Place(table, props, new Vector3(0f, 0f, z), new Vector3(0f, 90f, 0f));
                for (var seat = 0; seat < 3; seat++)
                {
                    var seatZ = z + (seat - 1) * 0.8f;
                    TidySceneKit.Place(chair, props, new Vector3(-0.95f, 0f, seatZ), new Vector3(0f, 90f + (float)random.NextDouble() * 12f - 6f, 0f));
                    TidySceneKit.Place(chair, props, new Vector3(0.95f, 0f, seatZ), new Vector3(0f, -90f + (float)random.NextDouble() * 12f - 6f, 0f));
                }

                var holder = TidySceneKit.Place(candelabra, props, new Vector3(0f, 0.78f, z), Vector3.zero);
                for (var point = 0; point < candlePoints.Length; point++)
                {
                    var slot = TidySceneDressing.Target<TidySnapSlot>("Candle Slot", targets, Vector3.zero, Quaternion.identity, "candle_slot_" + tableIndex + "_" + point, "Candelabra", TidyDemoLevels.Filter(context.Category("Candles")), new Color(1f, 0.8f, 0.4f), new Vector3(0f, 0.06f, 0f), new Vector3(0.09f, 0.14f, 0.09f), TidyCompletionMode.WhenFull);
                    slot.transform.position = holder.transform.TransformPoint(candlePoints[point]);
                }

                var basket = TidySceneKit.Place(context.Prefab("ScrollBasket"), props, new Vector3(z < 0 ? -1.9f : 1.9f, 0f, z + (z < 0 ? -1.8f : 1.8f)), Vector3.zero);
                var container = TidySceneDressing.Target<TidyContainer>("Scroll Basket", targets, Vector3.zero, Quaternion.identity, "scroll_basket_" + tableIndex, "Scroll Basket", TidyDemoLevels.Filter(context.Category("Scrolls")), new Color(0.9f, 0.8f, 0.6f), new Vector3(0f, 0.2f, 0f), new Vector3(0.6f, 0.45f, 0.6f), TidyCompletionMode.Never);
                container.transform.position = basket.transform.position + Vector3.up * 0.02f;
                container.ConfigureContainer(8, false, new Vector3(0.36f, 0.26f, 0.36f), Vector3.zero, TidyContainerLayout.Pile, false);
                tableIndex++;
            }

            var desk = TidySceneKit.Place(context.Prefab("SideTable"), props, new Vector3(-4.6f, 0f, 8.7f), new Vector3(0f, 0f, 0f));
            TidySceneKit.Place(chair, props, new Vector3(-4.6f, 0f, 7.95f), new Vector3(0f, 6f, 0f));
            var deskZone = TidySceneDressing.Target<TidyFreePlacementZone>("Writing Desk", targets, Vector3.zero, Quaternion.identity, "writing_desk", "Writing Desk", TidyMatchFilter.ForDefinitions(new[] { context.Definition("ink_bottle"), context.Definition("quill") }), new Color(0.6f, 0.8f, 1f), new Vector3(0f, 0.05f, 0f), new Vector3(1.1f, 0.12f, 0.7f), TidyCompletionMode.WhenAllMatchingObjectsPlaced);
            deskZone.transform.position = desk.transform.position + new Vector3(0f, 0.762f, 0f);
            deskZone.ConfigureArea(new Vector2(1.1f, 0.7f), 6, 0f, 15f, true);
            TidySceneKit.Place(context.Prefab("Lectern"), props, new Vector3(0f, 0f, 8.6f), new Vector3(0f, 180f, 0f));
            TidySceneKit.Place(context.Prefab("Globe"), props, new Vector3(4.6f, 0f, 8.7f), new Vector3(0f, 30f, 0f));
            TidySceneKit.PointLight("Desk Glow", props, new Vector3(-4.6f, 1.5f, 8.5f), Warm, 1.2f, 4f);
        }

        static void BuildPotionCorner()
        {
            var table = context.Prefab("SideTable");
            var rack = context.Prefab("PotionRack");
            var slots = TidyLibraryModels.PotionRackSlotPoints(3, 2, 0.15f, 0.24f);
            var colors = new[] { "Red", "Blue", "Green" };
            var tables = new[] { new Vector3(2.4f, 0f, -9.35f), new Vector3(4.6f, 0f, -9.35f) };
            foreach (var position in tables)
            {
                TidySceneKit.Place(table, props, position, Vector3.zero);
            }

            var rackPositions = new[] { new Vector3(1.95f, 0.762f, -9.45f), new Vector3(2.85f, 0.762f, -9.45f), new Vector3(4.6f, 0.762f, -9.45f) };
            for (var index = 0; index < colors.Length; index++)
            {
                var instance = TidySceneKit.Place(rack, props, rackPositions[index], Vector3.zero);
                var filter = TidyMatchFilter.Create(new[] { context.Category("Potions") }, new[] { context.Tag(colors[index]) });
                var group = TidySceneDressing.Target<TidySlotGroup>(colors[index] + " Potion Rack", targets, Vector3.zero, Quaternion.identity, "potion_rack_" + colors[index].ToLowerInvariant(), colors[index] + " Potions", filter, TidyArtLibrary.PaletteColor(colors[index]), new Vector3(0f, 0.3f, 0.05f), new Vector3(0.52f, 0.6f, 0.26f), TidyCompletionMode.WhenFull);
                group.transform.SetParent(instance.transform, false);
                var anchors = new List<Transform>();
                for (var slot = 0; slot < slots.Length; slot++)
                {
                    var anchor = new GameObject("Slot " + slot).transform;
                    anchor.SetParent(group.transform, false);
                    anchor.localPosition = slots[slot];
                    anchors.Add(anchor);
                }

                group.SetManualAnchors(anchors);
                var gem = new TidyMeshBuilder();
                gem.AddSphere(Vector3.zero, 0.03f, 12, 8);
                TidySceneDressing.Decor("Colour Gem", instance.transform, TidyMeshUtility.SaveAsIs(gem, "SM_Gem", MeshFolder), new[] { context.Material("Liquid_" + colors[index]) }, new Vector3(0f, 0.62f, 0.08f), Quaternion.identity);
            }

            TidySceneKit.PointLight("Potion Glow", props, new Vector3(3.4f, 1.6f, -9f), new Color(0.7f, 0.85f, 1f), 1.2f, 4f);
        }

        static void BuildCuriosities()
        {
            var pedestal = context.Prefab("Pedestal");
            var colors = new[] { "Purple", "Teal", "Pink" };
            for (var index = 0; index < colors.Length; index++)
            {
                var position = new Vector3(5.2f, 0f, 3.9f + index * 1.1f);
                TidySceneKit.Place(pedestal, props, position, Vector3.zero);
                var filter = TidyMatchFilter.Create(new[] { context.Category("Curiosities") }, new[] { context.Tag(colors[index]) });
                var slot = TidySceneDressing.Target<TidySnapSlot>(colors[index] + " Pedestal", targets, position + new Vector3(0f, 1.008f, 0f), Quaternion.identity, "pedestal_" + colors[index].ToLowerInvariant(), colors[index] + " Display", filter, TidyArtLibrary.PaletteColor(colors[index]), new Vector3(0f, 0.08f, 0f), new Vector3(0.42f, 0.18f, 0.42f), TidyCompletionMode.WhenFull);
                TidySceneKit.SpotLight("Pedestal Light", props, position + new Vector3(-0.6f, 2.4f, 0f), new Vector3(70f, 90f, 0f), TidyArtLibrary.PaletteColor(colors[index]), 3f, 4f, 30f, false);
                slot.name = colors[index] + " Pedestal Slot";
            }
        }

        static void BuildClutter()
        {
            var floorArea = new Rect(-4.6f, -8.2f, 9.2f, 16.6f);
            var exclusions = new List<Rect>
            {
                new Rect(-1.5f, -4.6f, 3f, 3.2f),
                new Rect(-1.5f, 1.4f, 3f, 3.2f),
                new Rect(-0.8f, -8.9f, 1.6f, 1.4f)
            };
            var books = new List<GameObject>();
            foreach (var spec in TidyLibraryContent.Books)
            {
                books.Add(context.ObjectPrefab(spec.Id));
                books.Add(context.ObjectPrefab(spec.Id));
            }

            for (var volume = 1; volume <= TidyLibraryContent.EncyclopaediaVolumes; volume++)
            {
                books.Add(context.ObjectPrefab("book_encyclopaedia_" + volume));
            }

            var shuffled = new List<GameObject>(TidySceneKit.Shuffle(books, random));
            var pileCenters = TidySceneDressing.ScatterPoints(random, floorArea, 16, 1.1f, exclusions, 0f);
            var cursor = 0;
            foreach (var center in pileCenters)
            {
                var pileSize = 1 + random.Next(3);
                var surface = 0f;
                for (var layer = 0; layer < pileSize && cursor < shuffled.Count - 6; layer++)
                {
                    var yaw = (float)random.NextDouble() * 360f;
                    var offset = new Vector3(((float)random.NextDouble() - 0.5f) * 0.05f, 0f, ((float)random.NextDouble() - 0.5f) * 0.05f);
                    var instance = TidySceneDressing.PlaceObject(shuffled[cursor++], clutter, center + offset, new Vector3(0f, yaw, 90f), surface);
                    surface = TidySceneDressing.TopOf(instance);
                }
            }

            var tableSpots = new[] { new Vector3(-0.3f, 0f, -3.6f), new Vector3(0.25f, 0f, -2.2f), new Vector3(-0.2f, 0f, 2.4f), new Vector3(0.3f, 0f, 3.7f), new Vector3(0.1f, 0f, -4.0f), new Vector3(-0.25f, 0f, 4.1f) };
            foreach (var spot in tableSpots)
            {
                if (cursor >= shuffled.Count)
                {
                    break;
                }

                var upright = random.NextDouble() < 0.5;
                TidySceneDressing.PlaceObject(shuffled[cursor++], clutter, spot, upright ? new Vector3(0f, (float)random.NextDouble() * 360f, 0f) : new Vector3(0f, (float)random.NextDouble() * 360f, 90f), 0.78f);
            }

            while (cursor < shuffled.Count)
            {
                var extra = TidySceneDressing.ScatterPoints(random, floorArea, 1, 0.1f, exclusions, 0f);
                TidySceneDressing.PlaceObject(shuffled[cursor++], clutter, extra.Count > 0 ? extra[0] : Vector3.zero, new Vector3(0f, (float)random.NextDouble() * 360f, 90f), 0f);
            }

            ScatterSet(new[] { "potion_red_round", "potion_red_tall", "potion_red_flat", "potion_blue_round", "potion_blue_tall", "potion_blue_flat", "potion_green_round", "potion_green_tall", "potion_green_flat" }, 2, new Rect(-3.8f, -8.2f, 7.6f, 3.4f), exclusions, 0.45f);
            ScatterSet(new[] { "scroll_0", "scroll_1", "scroll_2" }, 3, floorArea, exclusions, 0.6f);
            ScatterSet(new[] { "candle" }, 10, floorArea, exclusions, 0.7f);
            ScatterSet(new[] { "crystal_purple", "crystal_teal", "crystal_pink", "ink_bottle", "quill" }, 1, floorArea, exclusions, 0.9f);
        }

        static void ScatterSet(string[] ids, int copies, Rect area, List<Rect> exclusions, float spacing)
        {
            var count = ids.Length * copies;
            var points = TidySceneDressing.ScatterPoints(random, area, count, spacing, exclusions, 0f);
            for (var index = 0; index < points.Count; index++)
            {
                var id = ids[index % ids.Length];
                var lying = id.StartsWith("scroll") || random.NextDouble() < 0.4;
                var euler = lying && !id.StartsWith("scroll") ? new Vector3(0f, (float)random.NextDouble() * 360f, 90f) : new Vector3(0f, (float)random.NextDouble() * 360f, 0f);
                TidySceneDressing.PlaceObject(context.ObjectPrefab(id), clutter, points[index], euler, 0f);
            }
        }

        static void BuildBeforeAfter((Volume messy, Volume organised) volumes, List<Light> chandelierLights)
        {
            var controllerObject = new GameObject("Before After");
            var controller = controllerObject.AddComponent<TidyTransformationController>();
            var volumeReactorObject = new GameObject("Mood");
            volumeReactorObject.transform.SetParent(controllerObject.transform, false);
            volumeReactorObject.AddComponent<TidyVolumeReactor>().Configure(volumes.messy, volumes.organised);
            foreach (var light in chandelierLights)
            {
                var reactor = light.gameObject.AddComponent<TidyLightReactor>();
                reactor.Configure(5.5f, 10f, new Color(0.85f, 0.72f, 0.6f), Warm);
            }

            var dustRoot = TidySceneKit.Group("Dust Patches", controllerObject.transform);
            var dustMesh = new TidyMeshBuilder();
            dustMesh.AddPlane(new Vector2(1.2f, 1.2f), 1, 1, 0, null, true);
            var dust = TidyMeshUtility.SaveAsIs(dustMesh, "SM_DustPatch", MeshFolder);
            var dustPatches = new List<Transform>();
            foreach (var point in TidySceneDressing.ScatterPoints(random, new Rect(-5.5f, -8.5f, 11f, 17f), 14, 1.4f, null, 0.006f))
            {
                var patch = TidySceneDressing.Decor("Dust", dustRoot, dust, new[] { context.Material("Dust") }, point, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
                patch.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                dustPatches.Add(patch.transform);
            }

            var motesRoot = TidySceneKit.Group("Magic Motes", controllerObject.transform);
            var moteMesh = new TidyMeshBuilder();
            moteMesh.AddSphere(Vector3.zero, 0.035f, 10, 6);
            var mote = TidyMeshUtility.SaveAsIs(moteMesh, "SM_Mote", MeshFolder);
            var motes = new List<Transform>();
            foreach (var point in TidySceneDressing.ScatterPoints(random, new Rect(-5f, -8f, 10f, 16f), 24, 1f, null, 0f))
            {
                var moteObject = TidySceneDressing.Decor("Mote", motesRoot, mote, new[] { context.Material("Liquid_" + (random.NextDouble() < 0.5 ? "Purple" : "Teal")) }, point + Vector3.up * (1.6f + (float)random.NextDouble() * 2.4f), Quaternion.identity);
                moteObject.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                moteObject.AddComponent<TidyFloatingMotion>();
                motes.Add(moteObject.transform);
            }

            var reveal = new GameObject("Dust And Motes").AddComponent<TidyGradualRevealReactor>();
            reveal.transform.SetParent(controllerObject.transform, false);
            reveal.Configure(motes.ToArray(), dustPatches.ToArray());
            var reactors = new List<TidyProgressReactor>(controllerObject.GetComponentsInChildren<TidyProgressReactor>(true));
            foreach (var light in chandelierLights)
            {
                reactors.Add(light.GetComponent<TidyLightReactor>());
            }

            TidySerialization.SetArray(controller, "reactors", reactors);
        }
    }
}
