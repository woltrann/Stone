using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseModels
    {
        const int RoundSegments = 28;
        const int SmallSegments = 14;
        const float DecalLift = 0.0012f;

        public static Mesh Parcel(string folder, string key, Vector3 size, float labelWidth)
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddBeveledBox(new Vector3(0f, size.y * 0.5f, 0f), size, 0.006f, 1, 0);
            var tapeWidth = Mathf.Min(0.05f, size.z * 0.25f);
            builder.AddBox(new Vector3(0f, size.y + 0.0008f, 0f), new Vector3(size.x + 0.003f, 0.0016f, tapeWidth), 1);
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBox(new Vector3(side * (size.x * 0.5f + 0.0008f), size.y * 0.78f, 0f), new Vector3(0.0016f, size.y * 0.44f, tapeWidth), 1);
            }

            var quads = new TidyQuadMesh();
            var labelHeight = labelWidth * 0.68f;
            var top = size.y + 0.0018f;
            var labelZ = size.z * 0.5f - labelHeight * 0.5f - 0.015f;
            var labelX = -size.x * 0.5f + labelWidth * 0.5f + 0.02f;
            TopQuad(quads, new Vector3(labelX, top, labelZ), labelWidth, labelHeight);
            FrontQuad(quads, new Vector3(size.x * 0.12f, size.y * 0.42f, size.z * 0.5f + DecalLift), labelWidth * 0.9f, labelHeight * 0.9f);
            builder.AddMesh(quads.ToMesh("ParcelLabels"), Matrix4x4.identity, 2);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Parcel_" + key, folder);
        }

        public static Mesh Carton(string folder, string key, Vector3 size)
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddBeveledBox(new Vector3(0f, size.y * 0.5f, 0f), size, 0.008f, 1, 0);
            var tapeWidth = Mathf.Min(0.06f, size.x * 0.18f);
            builder.AddBox(new Vector3(0f, size.y + 0.0008f, 0f), new Vector3(tapeWidth, 0.0016f, size.z + 0.003f), 1);
            builder.AddBox(new Vector3(0f, 0.0008f - 0.0016f, 0f), new Vector3(tapeWidth, 0.0016f, size.z + 0.003f), 1);
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBox(new Vector3(0f, size.y * 0.86f, side * (size.z * 0.5f + 0.0008f)), new Vector3(tapeWidth, size.y * 0.28f, 0.0016f), 1);
                builder.AddBox(new Vector3(0f, size.y * 0.14f, side * (size.z * 0.5f + 0.0008f)), new Vector3(tapeWidth, size.y * 0.28f, 0.0016f), 1);
            }

            var quads = new TidyQuadMesh();
            var print = Mathf.Min(size.z, size.y) * 0.78f;
            SideQuad(quads, new Vector3(size.x * 0.5f + DecalLift, size.y * 0.5f, 0f), print, print, 1f);
            SideQuad(quads, new Vector3(-size.x * 0.5f - DecalLift, size.y * 0.5f, 0f), print, print, -1f);
            builder.AddMesh(quads.ToMesh("CartonPrints"), Matrix4x4.identity, 2);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Carton_" + key, folder);
        }

        public static Mesh Crate(string folder, string key, Vector3 size)
        {
            var builder = new TidyMeshBuilder(2);
            const float board = 0.018f;
            const float post = 0.045f;
            var half = size * 0.5f;
            builder.AddBox(new Vector3(0f, 0.012f, 0f), new Vector3(size.x - 0.02f, 0.024f, size.z - 0.02f), 0);
            foreach (var x in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -1f, 1f })
                {
                    builder.AddBeveledBox(new Vector3(x * (half.x - post * 0.5f), half.y, z * (half.z - post * 0.5f)), new Vector3(post, size.y, post), 0.005f, 1, 0);
                }
            }

            var rows = Mathf.Max(3, Mathf.RoundToInt(size.y / 0.11f));
            var pitch = size.y / rows;
            for (var row = 0; row < rows; row++)
            {
                var y = pitch * (row + 0.5f);
                foreach (var side in new[] { -1f, 1f })
                {
                    builder.AddBeveledBox(new Vector3(0f, y, side * (half.z - board * 0.5f)), new Vector3(size.x - post * 1.2f, pitch - 0.008f, board), 0.004f, 1, 0);
                    builder.AddBeveledBox(new Vector3(side * (half.x - board * 0.5f), y, 0f), new Vector3(board, pitch - 0.008f, size.z - post * 1.2f), 0.004f, 1, 0);
                }
            }

            var lidBoards = Mathf.Max(3, Mathf.RoundToInt(size.z / 0.12f));
            var lidPitch = size.z / lidBoards;
            for (var index = 0; index < lidBoards; index++)
            {
                var z = -half.z + lidPitch * (index + 0.5f);
                builder.AddBeveledBox(new Vector3(0f, size.y - board * 0.5f + 0.004f, z), new Vector3(size.x - 0.004f, board, lidPitch - 0.008f), 0.004f, 1, 0);
            }

            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var y in new[] { 0.035f, size.y - 0.035f })
                {
                    builder.AddBeveledBox(new Vector3(0f, y, side * (half.z + 0.006f)), new Vector3(size.x + 0.008f, 0.06f, 0.014f), 0.004f, 1, 0);
                    builder.AddBeveledBox(new Vector3(side * (half.x + 0.006f), y, 0f), new Vector3(0.014f, 0.06f, size.z + 0.008f), 0.004f, 1, 0);
                }

                var diagonal = Mathf.Atan2(size.y - 0.12f, size.z - 0.1f) * Mathf.Rad2Deg;
                var length = Mathf.Sqrt((size.y - 0.12f) * (size.y - 0.12f) + (size.z - 0.1f) * (size.z - 0.1f));
                builder.AddBeveledBox(Vector3.zero, new Vector3(0.014f, 0.055f, length), 0.004f, 1, 0, TidyMeshBuilder.TRS(new Vector3(side * (half.x + 0.006f), half.y, 0f), new Vector3(-diagonal * side, 0f, 0f), Vector3.one));
            }

            var quads = new TidyQuadMesh();
            var stencil = Mathf.Min(size.y - 0.15f, size.x * 0.6f);
            FrontQuad(quads, new Vector3(0f, half.y, half.z + DecalLift), stencil, stencil);
            BackQuad(quads, new Vector3(0f, half.y, -half.z - DecalLift), stencil, stencil);
            builder.AddMesh(quads.ToMesh("CrateStencils"), Matrix4x4.identity, 1);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Crate_" + key, folder);
        }

        public static Mesh Tote(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            var size = new Vector3(0.44f, 0.24f, 0.33f);
            const float wall = 0.012f;
            var half = size * 0.5f;
            builder.AddBeveledBox(new Vector3(0f, 0.012f, 0f), new Vector3(size.x - 0.03f, 0.024f, size.z - 0.03f), 0.006f, 1, 0);
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBeveledBox(new Vector3(0f, half.y, side * (half.z - wall * 0.5f)), new Vector3(size.x, size.y, wall), 0.004f, 1, 0);
                builder.AddBeveledBox(new Vector3(side * (half.x - wall * 0.5f), half.y, 0f), new Vector3(wall, size.y, size.z - wall * 2f), 0.004f, 1, 0);
                builder.AddBeveledBox(new Vector3(0f, size.y - 0.008f, side * (half.z + 0.004f)), new Vector3(size.x + 0.016f, 0.016f, 0.02f), 0.005f, 1, 0);
                builder.AddBeveledBox(new Vector3(side * (half.x + 0.004f), size.y - 0.008f, 0f), new Vector3(0.02f, 0.016f, size.z + 0.016f), 0.005f, 1, 0);
                for (var rib = -2; rib <= 2; rib++)
                {
                    builder.AddBeveledBox(new Vector3(rib * 0.085f, half.y - 0.01f, side * (half.z + 0.003f)), new Vector3(0.012f, size.y - 0.05f, 0.008f), 0.003f, 1, 0);
                }

                builder.AddBeveledBox(new Vector3(side * (half.x + 0.0015f), size.y - 0.05f, 0f), new Vector3(0.006f, 0.035f, 0.12f), 0.008f, 1, 1);
            }

            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Tote", folder);
        }

        public static Mesh Drum(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            const float r = 0.29f;
            var profile = new List<Vector2>
            {
                new Vector2(0.26f, 0f), new Vector2(0.282f, 0.004f), new Vector2(r, 0.02f), new Vector2(r, 0.27f),
                new Vector2(r + 0.012f, 0.28f), new Vector2(r + 0.012f, 0.3f), new Vector2(r, 0.31f), new Vector2(r, 0.57f),
                new Vector2(r + 0.012f, 0.58f), new Vector2(r + 0.012f, 0.6f), new Vector2(r, 0.61f), new Vector2(r, 0.86f),
                new Vector2(r + 0.004f, 0.875f), new Vector2(r - 0.008f, 0.885f), new Vector2(r - 0.02f, 0.87f)
            };
            builder.AddLathe(profile.ToArray(), RoundSegments, 0);
            builder.AddBeveledCylinder(0.03f, 0.018f, 0.004f, SmallSegments, 1, 1, Matrix4x4.Translate(new Vector3(0.17f, 0.868f, 0f)));
            builder.AddBeveledCylinder(0.022f, 0.016f, 0.004f, SmallSegments, 1, 1, Matrix4x4.Translate(new Vector3(-0.18f, 0.868f, 0.04f)));
            var quads = new TidyQuadMesh();
            FrontQuad(quads, new Vector3(0f, 0.44f, r + 0.002f), 0.15f, 0.15f);
            builder.AddMesh(quads.ToMesh("DrumLabel"), Matrix4x4.identity, 2);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Drum", folder);
        }

        public static Mesh JerryCan(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            const float depth = 0.16f;
            var body = new[]
            {
                new Vector2(-0.17f, 0f), new Vector2(0.17f, 0f), new Vector2(0.17f, 0.33f), new Vector2(0.11f, 0.39f), new Vector2(-0.17f, 0.39f)
            };
            builder.AddExtrusion(body, depth, 0, Matrix4x4.Translate(new Vector3(0f, 0f, -depth * 0.5f)), 0.012f);
            var handle = new[]
            {
                new Vector2(-0.16f, 0.385f), new Vector2(-0.13f, 0.385f), new Vector2(-0.12f, 0.42f), new Vector2(0.0f, 0.42f), new Vector2(0.01f, 0.385f),
                new Vector2(0.04f, 0.385f), new Vector2(0.03f, 0.45f), new Vector2(-0.15f, 0.45f)
            };
            builder.AddExtrusion(handle, 0.03f, 0, Matrix4x4.Translate(new Vector3(0f, 0f, -0.015f)), 0.006f);
            builder.AddLathe(new[] { new Vector2(0.026f, 0f), new Vector2(0.026f, 0.05f), new Vector2(0.03f, 0.052f), new Vector2(0.03f, 0.075f), new Vector2(0.022f, 0.08f) }, SmallSegments, 1, TidyMeshBuilder.TRS(new Vector3(0.125f, 0.36f, 0f), new Vector3(0f, 0f, -45f), Vector3.one));
            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var angle in new[] { 38f, -38f })
                {
                    builder.AddBeveledBox(Vector3.zero, new Vector3(0.36f, 0.022f, 0.012f), 0.004f, 1, 0, TidyMeshBuilder.TRS(new Vector3(-0.01f, 0.18f, side * (depth * 0.5f + 0.002f)), new Vector3(0f, 0f, angle), Vector3.one));
                }
            }

            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_JerryCan", folder);
        }

        public static Mesh Wrench(string folder, string key, float length)
        {
            var builder = new TidyMeshBuilder(1);
            var scale = length / 0.24f;
            var head = 0.034f * scale;
            var thickness = 0.008f * scale;
            var flat = TidyMeshBuilder.TRS(new Vector3(0f, thickness, 0f), new Vector3(90f, 0f, 0f), Vector3.one);
            var handle = RoundedRect(new Vector2(0f, 0f), new Vector2(0.011f * scale, length * 0.5f - head * 0.6f), 0.008f * scale);
            builder.AddExtrusion(handle, thickness * 0.8f, 0, flat * Matrix4x4.Translate(new Vector3(0f, 0f, thickness * 0.1f)), 0.0015f);
            builder.AddExtrusion(OpenJaw(new Vector2(0f, length * 0.5f - head * 0.6f), head, head * 0.38f), thickness, 0, flat, 0.002f);
            var ring = new[] { new Vector2(head * 0.36f, thickness), new Vector2(head * 0.36f, 0f), new Vector2(head * 0.78f, 0f), new Vector2(head * 0.78f, thickness), new Vector2(head * 0.36f, thickness) };
            builder.AddLathe(ring, 20, 0, Matrix4x4.Translate(new Vector3(0f, 0f, -length * 0.5f + head * 0.7f)), false, false);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Wrench_" + key, folder);
        }

        public static Mesh Hammer(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            var along = TidyMeshBuilder.TRS(new Vector3(0f, 0.016f, -0.15f), new Vector3(90f, 0f, 0f), Vector3.one);
            builder.AddLathe(new[] { new Vector2(0.001f, 0f), new Vector2(0.014f, 0.002f), new Vector2(0.016f, 0.02f), new Vector2(0.015f, 0.12f), new Vector2(0.012f, 0.13f) }, SmallSegments, 2, along);
            builder.AddLathe(new[] { new Vector2(0.012f, 0.12f), new Vector2(0.011f, 0.28f), new Vector2(0.013f, 0.3f), new Vector2(0.006f, 0.302f) }, SmallSegments, 1, along);
            builder.AddBeveledBox(new Vector3(0.0f, 0.016f, 0.155f), new Vector3(0.07f, 0.03f, 0.03f), 0.006f, 1, 0);
            builder.AddBeveledCylinder(0.017f, 0.035f, 0.004f, SmallSegments, 1, 0, TidyMeshBuilder.TRS(new Vector3(0.03f, 0.016f, 0.155f), new Vector3(0f, 0f, -90f), Vector3.one));
            foreach (var offset in new[] { -0.009f, 0.009f })
            {
                builder.AddBeveledBox(Vector3.zero, new Vector3(0.06f, 0.026f, 0.008f), 0.003f, 1, 0, TidyMeshBuilder.TRS(new Vector3(-0.055f, 0.016f, 0.155f + offset), new Vector3(0f, 0f, -24f), Vector3.one));
            }

            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Hammer", folder);
        }

        public static Mesh Mallet(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            var along = TidyMeshBuilder.TRS(new Vector3(0f, 0.03f, -0.15f), new Vector3(90f, 0f, 0f), Vector3.one);
            builder.AddLathe(new[] { new Vector2(0.001f, 0f), new Vector2(0.012f, 0.003f), new Vector2(0.013f, 0.26f), new Vector2(0.011f, 0.27f) }, SmallSegments, 1, along);
            builder.AddLathe(TidyProfiles.RoundedCylinder(0.03f, 0.11f, 0.008f, 2), 20, 0, TidyMeshBuilder.TRS(new Vector3(-0.055f, 0.03f, 0.135f), new Vector3(0f, 0f, -90f), Vector3.one));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Mallet", folder);
        }

        public static Mesh Screwdriver(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            var along = TidyMeshBuilder.TRS(new Vector3(0f, 0.017f, -0.11f), new Vector3(90f, 0f, 0f), Vector3.one);
            var handle = new List<Vector2> { new Vector2(0.001f, 0f), new Vector2(0.012f, 0.002f), new Vector2(0.016f, 0.012f) };
            for (var step = 1; step <= 6; step++)
            {
                handle.Add(new Vector2(step % 2 == 0 ? 0.017f : 0.0155f, 0.012f + step * 0.014f));
            }

            handle.Add(new Vector2(0.012f, 0.11f));
            handle.Add(new Vector2(0.006f, 0.118f));
            builder.AddLathe(handle.ToArray(), 16, 0, along);
            builder.AddLathe(new[] { new Vector2(0.0165f, 0.02f), new Vector2(0.0172f, 0.024f), new Vector2(0.0172f, 0.034f), new Vector2(0.0165f, 0.038f) }, 16, 1, along, false, false);
            builder.AddCylinder(0.0032f, 0.11f, 10, 2, along * Matrix4x4.Translate(new Vector3(0f, 0.115f, 0f)));
            builder.AddBeveledBox(new Vector3(0f, 0.017f, 0.128f), new Vector3(0.008f, 0.0025f, 0.014f), 0.0008f, 1, 2);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Screwdriver", folder);
        }

        public static Mesh TapeMeasure(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddBeveledBox(new Vector3(0f, 0.02f, 0f), new Vector3(0.075f, 0.04f, 0.075f), 0.016f, 3, 0);
            builder.AddBeveledCylinder(0.026f, 0.006f, 0.002f, 24, 1, 1, Matrix4x4.Translate(new Vector3(0f, 0.038f, 0f)));
            builder.AddBeveledBox(new Vector3(0f, 0.01f, 0.039f), new Vector3(0.022f, 0.005f, 0.012f), 0.001f, 1, 2);
            builder.AddBeveledBox(new Vector3(0f, 0.012f, 0.046f), new Vector3(0.024f, 0.012f, 0.003f), 0.001f, 1, 2);
            builder.AddBeveledBox(new Vector3(0.0f, 0.002f, -0.01f), new Vector3(0.022f, 0.004f, 0.05f), 0.0015f, 1, 1);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_TapeMeasure", folder);
        }

        public static Mesh Pliers(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBeveledBox(Vector3.zero, new Vector3(0.014f, 0.012f, 0.13f), 0.005f, 2, 1, TidyMeshBuilder.TRS(new Vector3(side * 0.018f, 0.007f, -0.055f), new Vector3(0f, side * 8f, 0f), Vector3.one));
                builder.AddBeveledBox(Vector3.zero, new Vector3(0.01f, 0.008f, 0.07f), 0.002f, 1, 0, TidyMeshBuilder.TRS(new Vector3(side * 0.008f, 0.007f, 0.035f), new Vector3(0f, -side * 5f, 0f), Vector3.one));
                builder.AddBeveledBox(Vector3.zero, new Vector3(0.008f, 0.007f, 0.06f), 0.002f, 1, 0, TidyMeshBuilder.TRS(new Vector3(side * 0.004f, 0.007f, 0.095f), new Vector3(0f, -side * 2f, 0f), Vector3.one));
            }

            builder.AddBeveledCylinder(0.011f, 0.016f, 0.003f, SmallSegments, 1, 0, Matrix4x4.Translate(new Vector3(0f, -0.001f, 0.01f)));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Pliers", folder);
        }

        static Vector2[] RoundedRect(Vector2 center, Vector2 half, float radius)
        {
            var points = new List<Vector2>();
            TidyProfiles.AppendArc(points, center + new Vector2(half.x - radius, -half.y + radius), radius, -90f, 0f, 3);
            TidyProfiles.AppendArc(points, center + new Vector2(half.x - radius, half.y - radius), radius, 0f, 90f, 3);
            TidyProfiles.AppendArc(points, center + new Vector2(-half.x + radius, half.y - radius), radius, 90f, 180f, 3);
            TidyProfiles.AppendArc(points, center + new Vector2(-half.x + radius, -half.y + radius), radius, 180f, 270f, 3);
            return points.ToArray();
        }

        static Vector2[] OpenJaw(Vector2 center, float outer, float inner)
        {
            var points = new List<Vector2>();
            const int steps = 18;
            for (var step = 0; step <= steps; step++)
            {
                var angle = Mathf.Lerp(112f, 428f, step / (float)steps) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * outer);
            }

            for (var step = steps; step >= 0; step--)
            {
                var angle = Mathf.Lerp(112f, 428f, step / (float)steps) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * inner);
            }

            return points.ToArray();
        }

        public static void TopQuad(TidyQuadMesh quads, Vector3 center, float width, float depth)
        {
            var hx = width * 0.5f;
            var hz = depth * 0.5f;
            quads.AddQuad(center + new Vector3(-hx, 0f, -hz), center + new Vector3(hx, 0f, -hz), center + new Vector3(hx, 0f, hz), center + new Vector3(-hx, 0f, hz), new Rect(0f, 0f, 1f, 1f));
        }

        public static void FrontQuad(TidyQuadMesh quads, Vector3 center, float width, float height)
        {
            var hx = width * 0.5f;
            var hy = height * 0.5f;
            quads.AddQuad(center + new Vector3(hx, -hy, 0f), center + new Vector3(-hx, -hy, 0f), center + new Vector3(-hx, hy, 0f), center + new Vector3(hx, hy, 0f), new Rect(0f, 0f, 1f, 1f));
        }

        public static void BackQuad(TidyQuadMesh quads, Vector3 center, float width, float height)
        {
            var hx = width * 0.5f;
            var hy = height * 0.5f;
            quads.AddQuad(center + new Vector3(-hx, -hy, 0f), center + new Vector3(hx, -hy, 0f), center + new Vector3(hx, hy, 0f), center + new Vector3(-hx, hy, 0f), new Rect(0f, 0f, 1f, 1f));
        }

        public static void SideQuad(TidyQuadMesh quads, Vector3 center, float width, float height, float facing)
        {
            var hz = width * 0.5f * facing;
            var hy = height * 0.5f;
            quads.AddQuad(center + new Vector3(0f, -hy, -hz), center + new Vector3(0f, -hy, hz), center + new Vector3(0f, hy, hz), center + new Vector3(0f, hy, -hz), new Rect(0f, 0f, 1f, 1f));
        }
    }
}
