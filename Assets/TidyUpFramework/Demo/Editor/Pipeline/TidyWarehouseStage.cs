using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public sealed class TidyWarehouseStage
    {
        public const float HallWidth = 18f;
        public const float HallLength = 24f;
        public const float HallHeight = 7f;
        public const float WallInset = 0.07f;
        public const float DecalHeight = 0.004f;
        public const string MeshFolder = TidyWarehouseContent.MeshFolder;
        public static readonly Color Warm = new Color(1f, 0.84f, 0.62f);
        public static readonly Color Cool = new Color(0.78f, 0.84f, 0.92f);

        public TidyDemoContext Context;
        public System.Random Random;
        public Transform Environment;
        public Transform Fixtures;
        public Transform Targets;
        public Transform Clutter;
        public Transform Mood;
        public Volume MessyVolume;
        public Volume OrganisedVolume;
        public readonly List<Light> LampLights = new List<Light>();
        public readonly List<Transform> ZoneGlows = new List<Transform>();
        public readonly List<Rect> Blocked = new List<Rect>();
        public readonly List<GameObject> MessyOnly = new List<GameObject>();
        public readonly List<GameObject> OrganisedOnly = new List<GameObject>();
        readonly Dictionary<string, Mesh> planes = new Dictionary<string, Mesh>();

        public float Next(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)Random.NextDouble());
        }

        public Material Material(string key)
        {
            return Context.Material(key);
        }

        public Mesh Plane(float width, float depth)
        {
            var key = Mathf.RoundToInt(width * 100f) + "x" + Mathf.RoundToInt(depth * 100f);
            if (planes.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var builder = new TidyMeshBuilder();
            builder.AddPlane(new Vector2(width, depth), 1, 1, 0, null, true);
            var mesh = TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_Plane_" + key, MeshFolder);
            planes[key] = mesh;
            return mesh;
        }

        public GameObject HazardBand(string bandName, Transform parent, Vector3 center, Vector2 size, float yaw)
        {
            var key = "Hazard_" + Mathf.RoundToInt(size.x * 100f) + "x" + Mathf.RoundToInt(size.y * 100f);
            if (!planes.TryGetValue(key, out var mesh))
            {
                var builder = new TidyMeshBuilder { UvScale = new Vector2(2f, 2f) };
                builder.AddPlane(size, 1, 1);
                mesh = TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_" + key, MeshFolder);
                planes[key] = mesh;
            }

            var band = TidySceneDressing.Decor(bandName, parent, mesh, new[] { Material("Warehouse_Hazard") }, new Vector3(center.x, center.y + DecalHeight + 0.001f, center.z), Quaternion.Euler(0f, yaw, 0f));
            band.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return band;
        }

        public GameObject FloorDecal(string decalName, Transform parent, Vector3 center, Vector2 size, float yaw, string material, float lift = 0f)
        {
            var decal = TidySceneDressing.Decor(decalName, parent, Plane(size.x, size.y), new[] { Material(material) }, new Vector3(center.x, DecalHeight + lift, center.z), Quaternion.Euler(0f, yaw, 0f));
            decal.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return decal;
        }

        public void FloorOutline(string outlineName, Transform parent, Vector3 center, Vector2 size, float width, string material)
        {
            var group = TidySceneKit.Group(outlineName, parent);
            FloorDecal("Edge", group, center + new Vector3(0f, 0f, size.y * 0.5f), new Vector2(size.x + width, width), 0f, material);
            FloorDecal("Edge", group, center - new Vector3(0f, 0f, size.y * 0.5f), new Vector2(size.x + width, width), 0f, material);
            FloorDecal("Edge", group, center + new Vector3(size.x * 0.5f, 0f, 0f), new Vector2(width, size.y - width), 0f, material, 0.0005f);
            FloorDecal("Edge", group, center - new Vector3(size.x * 0.5f, 0f, 0f), new Vector2(width, size.y - width), 0f, material, 0.0005f);
        }

        public void Block(float xMin, float zMin, float xMax, float zMax)
        {
            Blocked.Add(Rect.MinMaxRect(xMin, zMin, xMax, zMax));
        }
    }
}
