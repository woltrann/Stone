using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public sealed class TidyRoomSpec
    {
        public string Name = "Room";
        public Vector3 Size = new Vector3(10f, 4f, 10f);
        public Material Floor;
        public Material Wall;
        public Material Ceiling;
        public Material Trim;
        public Material Wainscot;
        public float WainscotHeight = 1.1f;
        public float BeamSpacing = 2.5f;
        public bool CeilingBeams = true;
        public Vector2 FloorUvScale = Vector2.one;
        public Vector2 WallUvScale = Vector2.one;
    }

    public static class TidyRoomShell
    {
        const float WallThickness = 0.2f;

        public static Transform Build(TidyRoomSpec spec, Transform parent)
        {
            var root = TidySceneKit.Group(spec.Name + " Shell", parent);
            var folder = TidyDemoPaths.Meshes + "/Rooms";
            var size = spec.Size;

            var floor = new TidyMeshBuilder { UvScale = spec.FloorUvScale };
            floor.AddPlane(new Vector2(size.x, size.z), 8, 8);
            Add(root, "Floor", TidyMeshUtility.SaveAsIs(floor, "SM_" + spec.Name + "_Floor", folder), spec.Floor, true);

            var ceiling = new TidyMeshBuilder();
            ceiling.AddPlane(new Vector2(size.x, size.z), 4, 4, 0, TidyMeshBuilder.TRS(new Vector3(0f, size.y, 0f), new Vector3(180f, 0f, 0f), Vector3.one));
            if (spec.CeilingBeams)
            {
                for (var z = -size.z * 0.5f + spec.BeamSpacing; z < size.z * 0.5f - 0.1f; z += spec.BeamSpacing)
                {
                    ceiling.AddBeveledBox(new Vector3(0f, size.y - 0.15f, z), new Vector3(size.x, 0.3f, 0.24f), 0.02f, 1);
                }
            }

            Add(root, "Ceiling", TidyMeshUtility.SaveAsIs(ceiling, "SM_" + spec.Name + "_Ceiling", folder), spec.Ceiling, true);

            var walls = new TidyMeshBuilder { UvScale = spec.WallUvScale };
            walls.AddBox(new Vector3(0f, size.y * 0.5f, size.z * 0.5f + WallThickness * 0.5f), new Vector3(size.x + WallThickness * 2f, size.y, WallThickness));
            walls.AddBox(new Vector3(0f, size.y * 0.5f, -size.z * 0.5f - WallThickness * 0.5f), new Vector3(size.x + WallThickness * 2f, size.y, WallThickness));
            walls.AddBox(new Vector3(size.x * 0.5f + WallThickness * 0.5f, size.y * 0.5f, 0f), new Vector3(WallThickness, size.y, size.z));
            walls.AddBox(new Vector3(-size.x * 0.5f - WallThickness * 0.5f, size.y * 0.5f, 0f), new Vector3(WallThickness, size.y, size.z));
            Add(root, "Walls", TidyMeshUtility.SaveAsIs(walls, "SM_" + spec.Name + "_Walls", folder), spec.Wall, true);

            if (spec.Wainscot != null)
            {
                var wainscot = new TidyMeshBuilder();
                var h = spec.WainscotHeight;
                wainscot.AddBeveledBox(new Vector3(0f, h * 0.5f, size.z * 0.5f - 0.03f), new Vector3(size.x, h, 0.06f), 0.01f, 1);
                wainscot.AddBeveledBox(new Vector3(0f, h * 0.5f, -size.z * 0.5f + 0.03f), new Vector3(size.x, h, 0.06f), 0.01f, 1);
                wainscot.AddBeveledBox(new Vector3(size.x * 0.5f - 0.03f, h * 0.5f, 0f), new Vector3(0.06f, h, size.z), 0.01f, 1);
                wainscot.AddBeveledBox(new Vector3(-size.x * 0.5f + 0.03f, h * 0.5f, 0f), new Vector3(0.06f, h, size.z), 0.01f, 1);
                wainscot.AddBeveledBox(new Vector3(0f, h + 0.02f, size.z * 0.5f - 0.05f), new Vector3(size.x, 0.05f, 0.1f), 0.01f, 1);
                wainscot.AddBeveledBox(new Vector3(0f, h + 0.02f, -size.z * 0.5f + 0.05f), new Vector3(size.x, 0.05f, 0.1f), 0.01f, 1);
                wainscot.AddBeveledBox(new Vector3(size.x * 0.5f - 0.05f, h + 0.02f, 0f), new Vector3(0.1f, 0.05f, size.z), 0.01f, 1);
                wainscot.AddBeveledBox(new Vector3(-size.x * 0.5f + 0.05f, h + 0.02f, 0f), new Vector3(0.1f, 0.05f, size.z), 0.01f, 1);
                Add(root, "Wainscot", TidyMeshUtility.SaveAsIs(wainscot, "SM_" + spec.Name + "_Wainscot", folder), spec.Wainscot, true);
            }

            if (spec.Trim != null)
            {
                var trim = new TidyMeshBuilder();
                var y = size.y - 0.12f;
                trim.AddBeveledBox(new Vector3(0f, y, size.z * 0.5f - 0.06f), new Vector3(size.x, 0.24f, 0.12f), 0.02f, 2);
                trim.AddBeveledBox(new Vector3(0f, y, -size.z * 0.5f + 0.06f), new Vector3(size.x, 0.24f, 0.12f), 0.02f, 2);
                trim.AddBeveledBox(new Vector3(size.x * 0.5f - 0.06f, y, 0f), new Vector3(0.12f, 0.24f, size.z), 0.02f, 2);
                trim.AddBeveledBox(new Vector3(-size.x * 0.5f + 0.06f, y, 0f), new Vector3(0.12f, 0.24f, size.z), 0.02f, 2);
                trim.AddBeveledBox(new Vector3(0f, 0.06f, size.z * 0.5f - 0.04f), new Vector3(size.x, 0.12f, 0.08f), 0.01f, 1);
                trim.AddBeveledBox(new Vector3(0f, 0.06f, -size.z * 0.5f + 0.04f), new Vector3(size.x, 0.12f, 0.08f), 0.01f, 1);
                trim.AddBeveledBox(new Vector3(size.x * 0.5f - 0.04f, 0.06f, 0f), new Vector3(0.08f, 0.12f, size.z), 0.01f, 1);
                trim.AddBeveledBox(new Vector3(-size.x * 0.5f + 0.04f, 0.06f, 0f), new Vector3(0.08f, 0.12f, size.z), 0.01f, 1);
                Add(root, "Trim", TidyMeshUtility.SaveAsIs(trim, "SM_" + spec.Name + "_Trim", folder), spec.Trim, false);
            }

            TidySceneKit.MarkStatic(root);
            return root;
        }

        static void Add(Transform parent, string objectName, Mesh mesh, Material material, bool collider)
        {
            var created = TidyPropFactory.Visual(objectName, mesh, material);
            created.transform.SetParent(parent, false);
            if (collider)
            {
                TidyPropFactory.AddCollider(created, mesh, TidyColliderShape.Mesh);
            }
        }
    }
}
