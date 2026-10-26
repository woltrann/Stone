using TidyUpFramework.Demo.Editor.ContentKit;
using TidyUpFramework.Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseScene
    {
        const float LampHeight = 4.7f;
        static readonly float[] TrussRows = { -9.6f, -6f, -2.4f, 1.2f, 4.8f, 8.4f };

        public static void Build(TidyDemoContext context)
        {
            if (!context.HasDefinition(TidyWarehouseContent.MarkerId))
            {
                Debug.LogWarning("Tidy Up pipeline: warehouse content missing, build the warehouse content first.");
                return;
            }

            var stage = new TidyWarehouseStage { Context = context, Random = new System.Random(3030) };
            var scene = TidySceneKit.NewScene(TidyDemoPaths.WarehouseScene);
            var level = AssetDatabase.LoadAssetAtPath<TidyLevelDefinition>(TidyWarehouseLevel.AssetPath);
            stage.Environment = TidySceneKit.Group("Environment");
            stage.Fixtures = TidySceneKit.Group("Fixtures");
            stage.Targets = TidySceneKit.Group("Placement Targets");
            stage.Clutter = TidySceneKit.Group("Clutter");

            BuildLighting(stage);
            BuildShell(stage);
            BuildStructure(stage);
            BuildDock(stage);
            BuildLamps(stage);
            TidyWarehouseZones.Build(stage);
            TidyWarehouseDressing.Build(stage);
            TidyWarehouseClutter.Build(stage);
            TidyWarehouseMood.Build(stage);
            TidySceneKit.MarkStatic(stage.Environment);
            TidySceneKit.MarkStatic(stage.Fixtures);
            TidySceneKit.ReflectionProbe(new Vector3(0f, 3f, 0f), new Vector3(TidyWarehouseStage.HallWidth, TidyWarehouseStage.HallHeight, TidyWarehouseStage.HallLength), 0.7f);
            TidySceneKit.Gameplay(level, new Vector3(0f, 0.02f, -10.1f), 0f);
            TidySceneKit.Save(scene, TidyDemoPaths.WarehouseScene);
        }

        static void BuildLighting(TidyWarehouseStage stage)
        {
            var spec = new TidyLightingSpec
            {
                SkyColor = new Color(0.82f, 0.84f, 0.88f),
                EquatorColor = new Color(0.66f, 0.62f, 0.56f),
                GroundColor = new Color(0.52f, 0.5f, 0.47f),
                SunColor = new Color(1f, 0.95f, 0.86f),
                SunIntensity = 0.4f,
                SunEuler = new Vector3(58f, 160f, 0f),
                FogColor = new Color(0.62f, 0.6f, 0.56f),
                FogDensity = 0.006f,
                PostExposure = 0.2f,
                Saturation = 12f,
                Contrast = 10f,
                Temperature = 6f,
                BloomIntensity = 0.6f,
                VignetteIntensity = 0.24f
            };
            var sun = TidySceneKit.Lighting(TidyDemoPaths.WarehouseScene, spec, out var organised);
            sun.shadows = LightShadows.None;
            var messySpec = new TidyLightingSpec
            {
                PostExposure = -0.2f,
                Saturation = -32f,
                Contrast = -4f,
                Temperature = -16f,
                BloomIntensity = 0.25f,
                VignetteIntensity = 0.42f
            };
            stage.MessyVolume = TidySceneKit.CreateVolume(TidyDemoPaths.WarehouseScene, "Messy Volume", messySpec, 1f, 1);
            organised.weight = 0f;
            organised.priority = 2;
            stage.OrganisedVolume = organised;
        }

        static void BuildShell(TidyWarehouseStage stage)
        {
            TidyRoomShell.Build(new TidyRoomSpec
            {
                Name = "Warehouse",
                Size = new Vector3(TidyWarehouseStage.HallWidth, TidyWarehouseStage.HallHeight, TidyWarehouseStage.HallLength),
                Floor = stage.Material("Warehouse_Floor"),
                Wall = stage.Material("Warehouse_Cladding"),
                Ceiling = stage.Material("Warehouse_Roof"),
                Trim = stage.Material("Warehouse_SteelDark"),
                Wainscot = stage.Material("Warehouse_Block"),
                WainscotHeight = 1.6f,
                CeilingBeams = false,
                FloorUvScale = new Vector2(1f / 6f, 1f / 6f),
                WallUvScale = new Vector2(0.5f, 0.5f)
            }, stage.Environment);
        }

        static void BuildStructure(TidyWarehouseStage stage)
        {
            var truss = TidyWarehouseFixtures.Truss(TidyWarehouseStage.MeshFolder, TidyWarehouseStage.HallWidth);
            foreach (var z in TrussRows)
            {
                TidySceneDressing.Decor("Truss", stage.Environment, truss, new[] { stage.Material("Metal_Steel") }, new Vector3(0f, TidyWarehouseStage.HallHeight - 0.78f, z), Quaternion.identity);
            }

            var purlins = new TidyMeshBuilder();
            foreach (var x in new[] { -6f, -2f, 2f, 6f })
            {
                purlins.AddBeveledBox(new Vector3(x, TidyWarehouseStage.HallHeight - 0.08f, 0f), new Vector3(0.1f, 0.12f, TidyWarehouseStage.HallLength), 0.01f, 1);
            }

            TidySceneDressing.Decor("Purlins", stage.Environment, TidyMeshUtility.SaveAsIs(purlins, "SM_Warehouse_Purlins", TidyWarehouseStage.MeshFolder), new[] { stage.Material("Metal_Steel") }, Vector3.zero, Quaternion.identity);
            BuildColumns(stage);
            BuildSkylights(stage);
        }

        static void BuildColumns(TidyWarehouseStage stage)
        {
            var column = new TidyMeshBuilder(2);
            var height = TidyWarehouseStage.HallHeight;
            column.AddBeveledBox(new Vector3(0f, height * 0.5f, 0.11f), new Vector3(0.26f, height, 0.025f), 0.006f, 1, 0);
            column.AddBeveledBox(new Vector3(0f, height * 0.5f, -0.11f), new Vector3(0.26f, height, 0.025f), 0.006f, 1, 0);
            column.AddBox(new Vector3(0f, height * 0.5f, 0f), new Vector3(0.02f, height, 0.2f), 0);
            column.AddBeveledBox(new Vector3(0f, 0.6f, 0f), new Vector3(0.34f, 1.2f, 0.32f), 0.02f, 2, 1);
            var mesh = TidyMeshUtility.SaveAsIs(column, "SM_Warehouse_Column", TidyWarehouseStage.MeshFolder);
            var materials = new[] { stage.Material("Metal_Rack"), stage.Material("Warehouse_Hazard") };
            var x = TidyWarehouseStage.HallWidth * 0.5f - 0.24f;
            foreach (var z in new[] { -9.6f, 8.4f })
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    Column(stage, mesh, materials, new Vector3(side * x, 0f, z));
                }
            }

            foreach (var z in new[] { -6.6f, 4.4f })
            {
                Column(stage, mesh, materials, new Vector3(x, 0f, z));
            }
        }

        static void Column(TidyWarehouseStage stage, Mesh mesh, Material[] materials, Vector3 position)
        {
            var instance = TidySceneDressing.Decor("Column", stage.Environment, mesh, materials, position, Quaternion.Euler(0f, 90f, 0f));
            TidyPropFactory.AddCollider(instance, mesh, TidyColliderShape.Box);
            stage.Block(position.x - 0.3f, position.z - 0.3f, position.x + 0.3f, position.z + 0.3f);
        }

        static void BuildSkylights(TidyWarehouseStage stage)
        {
            var frame = new TidyMeshBuilder();
            const float width = 2.2f;
            const float length = 3.0f;
            frame.AddBeveledBox(new Vector3(0f, 0f, length * 0.5f), new Vector3(width + 0.12f, 0.16f, 0.08f), 0.01f, 1);
            frame.AddBeveledBox(new Vector3(0f, 0f, -length * 0.5f), new Vector3(width + 0.12f, 0.16f, 0.08f), 0.01f, 1);
            frame.AddBeveledBox(new Vector3(width * 0.5f, 0f, 0f), new Vector3(0.08f, 0.16f, length), 0.01f, 1);
            frame.AddBeveledBox(new Vector3(-width * 0.5f, 0f, 0f), new Vector3(0.08f, 0.16f, length), 0.01f, 1);
            frame.AddBeveledBox(new Vector3(0f, 0f, 0f), new Vector3(0.05f, 0.1f, length), 0.008f, 1);
            frame.AddBeveledBox(new Vector3(0f, 0f, 0f), new Vector3(width, 0.1f, 0.05f), 0.008f, 1);
            var frameMesh = TidyMeshUtility.SaveAsIs(frame, "SM_Warehouse_SkylightFrame", TidyWarehouseStage.MeshFolder);
            var glass = stage.Plane(width, length);
            foreach (var x in new[] { -4f, 4f })
            {
                foreach (var z in new[] { -7.8f, -0.6f, 6.6f })
                {
                    var y = TidyWarehouseStage.HallHeight - 0.03f;
                    TidySceneDressing.Decor("Skylight Frame", stage.Environment, frameMesh, new[] { stage.Material("Warehouse_SteelDark") }, new Vector3(x, y - 0.06f, z), Quaternion.identity);
                    var pane = TidySceneDressing.Decor("Skylight", stage.Environment, glass, new[] { stage.Material("Warehouse_Skylight") }, new Vector3(x, y, z), Quaternion.Euler(180f, 0f, 0f));
                    pane.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    TidySceneKit.SpotLight("Skylight Light", stage.Environment, new Vector3(x, y - 0.2f, z), new Vector3(90f, 0f, 0f), new Color(0.86f, 0.92f, 1f), 3.2f, 9f, 120f, false);
                }
            }
        }

        static void BuildDock(TidyWarehouseStage stage)
        {
            var southZ = -TidyWarehouseStage.HallLength * 0.5f;
            TidySceneKit.Place(stage.Context.Prefab("WarehouseRollerDoor"), stage.Environment, new Vector3(0f, 0f, southZ + 0.09f), new Vector3(0f, 180f, 0f));
            var daylight = TidySceneDressing.Decor("Daylight", stage.Environment, stage.Plane(4.2f, 3.0f), new[] { stage.Material("Warehouse_Daylight") }, new Vector3(0f, 1.5f, southZ + 0.1f), Quaternion.Euler(90f, 0f, 0f));
            daylight.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var leveler = new TidyMeshBuilder();
            leveler.AddBeveledBox(new Vector3(0f, 0.01f, 0f), new Vector3(4.0f, 0.02f, 1.1f), 0.006f, 1);
            TidySceneDressing.Decor("Dock Leveler", stage.Environment, TidyMeshUtility.SaveAsIs(leveler, "SM_Warehouse_DockLeveler", TidyWarehouseStage.MeshFolder), new[] { stage.Material("Metal_Steel") }, new Vector3(0f, 0f, southZ + 0.65f), Quaternion.identity);
            stage.HazardBand("Dock Hazard", stage.Environment, new Vector3(0f, 0f, southZ + 1.35f), new Vector2(4.2f, 0.3f), 0f);
            foreach (var side in new[] { -1f, 1f })
            {
                TidySceneKit.Place(stage.Context.Prefab("WarehouseBollard"), stage.Environment, new Vector3(side * 2.55f, 0f, southZ + 0.5f), Vector3.zero);
                TidySceneKit.Place(stage.Context.Prefab("WarehouseBollard"), stage.Environment, new Vector3(side * 2.55f, 0f, southZ + 1.5f), Vector3.zero);
            }

            TidySceneKit.SpotLight("Daylight Spill", stage.Environment, new Vector3(0f, 3.4f, southZ + 0.4f), new Vector3(32f, 0f, 0f), new Color(1f, 0.93f, 0.8f), 30f, 18f, 85f, true);
            TidySceneKit.PointLight("Daylight Bounce", stage.Environment, new Vector3(0f, 1.2f, southZ + 1.2f), new Color(1f, 0.92f, 0.8f), 3f, 6f);
            stage.Block(-2.9f, southZ, 2.9f, southZ + 1.9f);
        }

        static void BuildLamps(TidyWarehouseStage stage)
        {
            var lamp = stage.Context.Prefab("WarehouseLamp");
            var bulb = AssetDatabase.LoadAssetAtPath<Mesh>(TidyWarehouseStage.MeshFolder + "/SM_Warehouse_Bulb.asset");
            foreach (var x in new[] { -5f, 0f, 5f })
            {
                foreach (var z in new[] { -8f, -2.6f, 2.8f, 8.2f })
                {
                    var instance = TidySceneKit.Place(lamp, stage.Environment, new Vector3(x, LampHeight, z), Vector3.zero);
                    TidySceneDressing.Decor("Bulb", instance.transform, bulb, new[] { stage.Material("Warehouse_Bulb") }, Vector3.zero, Quaternion.identity).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    var shadows = x == 0f && (z == -2.6f || z == 2.8f);
                    var light = TidySceneKit.SpotLight("Lamp Light", instance.transform, instance.transform.position + new Vector3(0f, 0.05f, 0f), new Vector3(90f, 0f, 0f), TidyWarehouseStage.Warm, 12f, 12f, 120f, shadows);
                    stage.LampLights.Add(light);
                }
            }
        }
    }
}
