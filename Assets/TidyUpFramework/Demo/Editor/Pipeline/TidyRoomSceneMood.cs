using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using TidyUpFramework.Transformation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static partial class TidyRoomScene
    {
        sealed class RoomLights
        {
            public readonly List<(Light light, float messy, float organised)> Reactive = new List<(Light, float, float)>();
            public readonly List<Transform> Reveals = new List<Transform>();
            public readonly List<GameObject> MessyOnly = new List<GameObject>();
        }

        static RoomLights BuildDecorLights()
        {
            var lights = new RoomLights();
            var root = TidySceneKit.Group("Lights", decor);
            var window = TidySceneKit.SpotLight("Golden Hour Window", root, new Vector3(WindowCenter.x, 2.5f, 3.45f), new Vector3(36f, 180f, 0f), Golden, 3f, 9f, 85f);
            lights.Reactive.Add((window, 3f, 8f));
            var glow = TidySceneKit.PointLight("Window Fill", root, new Vector3(WindowCenter.x, 1.6f, 3.1f), Golden, 0.4f, 5f);
            lights.Reactive.Add((glow, 0.4f, 1.2f));
            var bedside = TidySceneKit.PointLight("Bedside Lamp", root, NightstandPosition + new Vector3(-0.05f, 0.87f, 0.1f), LampWarm, 0.25f, 3.5f);
            lights.Reactive.Add((bedside, 0.25f, 1.3f));
            var deskLamp = TidySceneKit.PointLight("Desk Lamp", root, DeskPosition + new Vector3(0.5f, 0.98f, 0.1f), LampWarm, 0.2f, 3f);
            lights.Reactive.Add((deskLamp, 0.2f, 0.8f));
            var pendant = TidySceneKit.PointLight("Pendant", root, new Vector3(-0.4f, 1.75f, 0.2f), LampWarm, 1.2f, 7f);
            lights.Reactive.Add((pendant, 1.2f, 1.8f));
            var reading = TidySceneKit.PointLight("Reading Lamp", root, FloorLampPosition + new Vector3(0f, 1.3f, 0f), LampWarm, 0.2f, 4f);
            lights.Reactive.Add((reading, 0.2f, 1.2f));
            BuildFairyLights(lights);
            BuildCandle(lights);
            BuildMovingBoxes(lights);
            return lights;
        }

        static void BuildFairyLights(RoomLights lights)
        {
            var strands = new[]
            {
                (from: new Vector3(-4.26f, 2.32f, 0.62f), to: new Vector3(-4.26f, 2.32f, 2.58f), sag: 0.2f, segments: 26),
                (from: new Vector3(-2.35f, 2.72f, 3.76f), to: new Vector3(1.15f, 2.72f, 3.76f), sag: 0.22f, segments: 40),
                (from: new Vector3(4.26f, 2.45f, -0.4f), to: new Vector3(4.26f, 2.45f, 2.4f), sag: 0.24f, segments: 30)
            };
            var root = TidySceneKit.Group("Fairy Lights", decor);
            for (var index = 0; index < strands.Length; index++)
            {
                var points = TidyRoomDecor.Catenary(strands[index].from, strands[index].to, strands[index].sag, strands[index].segments);
                var wire = TidyRoomDecor.FairyStrand("SM_Room_FairyWire_" + index, points, 0.011f, false);
                var bulbs = TidyRoomDecor.FairyStrand("SM_Room_FairyGlow_" + index, points, 0.014f, true);
                var wireObject = TidySceneDressing.Decor("Fairy Wire " + (index + 1), root, wire, new[] { context.Material("Room_Wire"), context.Material("Room_RubberWhite") }, Vector3.zero, Quaternion.identity);
                wireObject.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                var glowObject = TidySceneDressing.Decor("Fairy Glow " + (index + 1), root, bulbs, new[] { context.Material("Room_FairyGlow") }, Vector3.zero, Quaternion.identity);
                glowObject.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                for (var lamp = 1; lamp <= 3; lamp++)
                {
                    var point = points[points.Length * lamp / 4];
                    var inward = new Vector3(-point.x, 0f, -point.z).normalized * 0.45f;
                    var light = TidySceneKit.PointLight("Fairy Light", glowObject.transform, point + inward + Vector3.down * 0.15f, new Color(1f, 0.76f, 0.46f), 0.4f, 2.8f);
                    light.transform.SetParent(glowObject.transform, true);
                }

                lights.Reveals.Add(PivotAtCenter(glowObject.transform, points[points.Length / 2]));
            }
        }

        static Transform PivotAtCenter(Transform target, Vector3 pivot)
        {
            var holder = new GameObject(target.name).transform;
            holder.SetParent(target.parent, false);
            holder.position = pivot;
            target.SetParent(holder, true);
            target.name = "Bulbs";
            return holder;
        }

        static void BuildCandle(RoomLights lights)
        {
            var candle = AssetDatabase.LoadAssetAtPath<Mesh>(TidyDemoPaths.Meshes + "/Library/SM_Candle_12.asset");
            var flame = AssetDatabase.LoadAssetAtPath<Mesh>(TidyDemoPaths.Meshes + "/Library/SM_Flame.asset");
            var vase = Put("RoomVase", decor, NightstandPosition + new Vector3(0.08f, TidyRoomFurniture.NightstandHeight, -0.14f), 0f);
            lights.Reveals.Insert(0, vase.transform);
            var holder = new GameObject("Desk Candle").transform;
            holder.SetParent(decor, false);
            holder.position = DeskPosition + new Vector3(-0.3f, TidyRoomFurniture.DeskHeight, 0.2f);
            if (candle == null || flame == null)
            {
                return;
            }

            TidySceneDressing.Decor("Candle", holder, candle, new[] { context.Material("Wax_Cream"), context.Material("Wick"), context.Material("Metal_Brass") }, Vector3.zero, Quaternion.identity);
            var glow = new GameObject("Candle Glow").transform;
            glow.SetParent(holder, false);
            glow.localPosition = new Vector3(0f, 0.14f, 0f);
            TidySceneDressing.Decor("Flame", glow, flame, new[] { context.Material("Flame") }, Vector3.zero, Quaternion.identity).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var light = TidySceneKit.PointLight("Candle Light", glow, glow.position + Vector3.up * 0.08f, new Color(1f, 0.7f, 0.4f), 0.5f, 2.2f);
            light.gameObject.AddComponent<TidyCandleFlicker>();
            lights.Reveals.Insert(1, glow);
        }

        static void BuildMovingBoxes(RoomLights lights)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBeveledBox(new Vector3(0f, 0.2f, 0f), new Vector3(0.55f, 0.4f, 0.42f), 0.012f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.401f, 0f), new Vector3(0.08f, 0.004f, 0.43f), 0.001f, 1, 1);
            builder.AddBeveledBox(new Vector3(0f, 0.3f, 0.211f), new Vector3(0.08f, 0.2f, 0.004f), 0.001f, 1, 1);
            builder.AddBeveledBox(new Vector3(0.08f, 0.61f, 0.02f), new Vector3(0.46f, 0.32f, 0.36f), 0.012f, 2, 0, TidyMeshBuilder.TRS(Vector3.zero, new Vector3(0f, 12f, 0f), Vector3.one));
            builder.AddBeveledBox(new Vector3(0.08f, 0.771f, 0.02f), new Vector3(0.07f, 0.004f, 0.37f), 0.001f, 1, 1, TidyMeshBuilder.TRS(Vector3.zero, new Vector3(0f, 12f, 0f), Vector3.one));
            var mesh = TidyMeshUtility.SaveAsIs(builder, "SM_Room_MovingBoxes", MeshFolder);
            foreach (var spot in new[] { (position: new Vector3(0.25f, 0f, -3.45f), yaw: 4f), (position: new Vector3(-2.75f, 0f, -0.2f), yaw: -18f) })
            {
                var boxes = TidySceneDressing.Decor("Moving Boxes", furniture, mesh, new[] { context.Material("Cardboard"), context.Material("Tape") }, spot.position, Quaternion.Euler(0f, spot.yaw, 0f));
                TidyPropFactory.AddCollider(boxes, mesh, TidyColliderShape.Box);
                lights.MessyOnly.Add(boxes);
            }
        }

        static void BuildBeforeAfter((Volume messy, Volume organised, Light sun) mood, RoomLights lights)
        {
            var controllerObject = new GameObject("Before After");
            var controller = controllerObject.AddComponent<TidyTransformationController>();
            var volumeReactor = new GameObject("Mood").AddComponent<TidyVolumeReactor>();
            volumeReactor.transform.SetParent(controllerObject.transform, false);
            volumeReactor.Configure(mood.messy, mood.organised);
            var reactors = new List<TidyProgressReactor> { volumeReactor };
            var sunReactor = mood.sun.gameObject.AddComponent<TidyLightReactor>();
            sunReactor.Configure(0.18f, 0.45f, new Color(0.72f, 0.78f, 0.9f), new Color(1f, 0.8f, 0.6f));
            reactors.Add(sunReactor);
            foreach (var (light, messy, organised) in lights.Reactive)
            {
                var reactor = light.gameObject.AddComponent<TidyLightReactor>();
                reactor.Configure(messy, organised, new Color(0.8f, 0.82f, 0.9f), light.color);
                light.intensity = messy;
                reactors.Add(reactor);
            }

            var dust = BuildDustPatches(controllerObject.transform);
            var reveal = new GameObject("Cosy Reveal").AddComponent<TidyGradualRevealReactor>();
            reveal.transform.SetParent(controllerObject.transform, false);
            reveal.Configure(lights.Reveals.ToArray(), dust.ToArray());
            reactors.Add(reveal);
            var toggle = new GameObject("Unpacked").AddComponent<TidyToggleReactor>();
            toggle.transform.SetParent(controllerObject.transform, false);
            toggle.Configure(lights.MessyOnly.ToArray(), new GameObject[0], 0.9f);
            reactors.Add(toggle);
            foreach (var item in lights.Reveals)
            {
                item.gameObject.SetActive(false);
            }

            TidySerialization.SetArray(controller, "reactors", reactors);
        }

        static List<Transform> BuildDustPatches(Transform parent)
        {
            var root = TidySceneKit.Group("Dust Patches", parent);
            var builder = new TidyMeshBuilder();
            builder.AddPlane(Vector2.one, 1, 1, 0, null, true);
            var mesh = TidyMeshUtility.SaveAsIs(builder, "SM_Room_DustPatch", MeshFolder);
            var patches = new List<Transform>();
            var spots = TidySceneDressing.ScatterPoints(random, new Rect(-4.1f, -3.6f, 8.2f, 7.2f), 40, 1.3f, null, 0f);
            foreach (var spot in spots)
            {
                if (patches.Count >= 14)
                {
                    break;
                }

                if (SurfaceAt(spot.x, spot.z) > 0.02f)
                {
                    continue;
                }

                var patch = TidySceneDressing.Decor("Dust", root, mesh, new[] { context.Material("Dust") }, new Vector3(spot.x, 0.007f + patches.Count * 0.0002f, spot.z), Quaternion.Euler(0f, Yaw(), 0f));
                patch.transform.localScale = new Vector3(Range(0.9f, 1.5f), 1f, Range(0.9f, 1.5f));
                patch.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                patches.Add(patch.transform);
            }

            return patches;
        }
    }
}
