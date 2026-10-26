using System.Collections.Generic;
using TidyUpFramework.Transformation;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseMood
    {
        public static void Build(TidyWarehouseStage stage)
        {
            var controllerObject = new GameObject("Before After");
            var controller = controllerObject.AddComponent<TidyTransformationController>();
            var mood = new GameObject("Mood");
            mood.transform.SetParent(controllerObject.transform, false);
            mood.AddComponent<TidyVolumeReactor>().Configure(stage.MessyVolume, stage.OrganisedVolume);

            foreach (var light in stage.LampLights)
            {
                light.gameObject.AddComponent<TidyLightReactor>().Configure(5f, 12f, new Color(0.78f, 0.84f, 0.95f), TidyWarehouseStage.Warm);
            }

            var glowRoot = TidySceneKit.Group("Zone Lights", controllerObject.transform);
            foreach (var glow in stage.ZoneGlows)
            {
                glow.SetParent(glowRoot, true);
            }

            var signRoot = TidySceneKit.Group("Status Signs", controllerObject.transform);
            foreach (var sign in stage.MessyOnly)
            {
                sign.transform.SetParent(signRoot, true);
            }

            foreach (var sign in stage.OrganisedOnly)
            {
                sign.transform.SetParent(signRoot, true);
            }

            var dust = BuildDust(stage, controllerObject.transform);
            var reveal = new GameObject("Zone Lights And Dust").AddComponent<TidyGradualRevealReactor>();
            reveal.transform.SetParent(controllerObject.transform, false);
            reveal.Configure(stage.ZoneGlows.ToArray(), dust.ToArray());

            var toggle = new GameObject("All Clear Sign").AddComponent<TidyToggleReactor>();
            toggle.transform.SetParent(controllerObject.transform, false);
            toggle.Configure(stage.MessyOnly.ToArray(), stage.OrganisedOnly.ToArray(), 0.99f);

            var reactors = new List<TidyProgressReactor>(controllerObject.GetComponentsInChildren<TidyProgressReactor>(true));
            foreach (var light in stage.LampLights)
            {
                reactors.Add(light.GetComponent<TidyLightReactor>());
            }

            TidySerialization.SetArray(controller, "reactors", reactors);
        }

        static List<Transform> BuildDust(TidyWarehouseStage stage, Transform parent)
        {
            var root = TidySceneKit.Group("Dust And Scuffs", parent);
            var patches = new List<Transform>();
            var area = new Rect(-7f, -10.5f, 14f, 20f);
            var points = TidySceneDressing.ScatterPoints(stage.Random, area, 26, 1.3f, stage.Blocked, 0f);
            for (var index = 0; index < points.Count; index++)
            {
                var material = index % 3 == 0 ? "Grime" : "Warehouse_FloorDust";
                var size = stage.Next(1.0f, 2.2f);
                var patch = stage.FloorDecal(material + " Patch", root, points[index], new Vector2(size, size * stage.Next(0.7f, 1.2f)), stage.Next(0f, 360f), material, 0.0015f + index * 0.00005f);
                patch.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                patches.Add(patch.transform);
            }

            return patches;
        }
    }
}
