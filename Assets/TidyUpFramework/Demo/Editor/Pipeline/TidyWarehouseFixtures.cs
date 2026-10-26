using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseFixtures
    {
        public const float RackBayWidth = 2.7f;
        public const float RackDepth = 1.1f;
        public const float RackHeight = 4.6f;
        public static readonly float[] RackLevels = { 0.16f, 1.5f, 2.8f, 4.1f };
        public const float PalletHeight = 0.144f;
        public static readonly Vector3 BinSize = new Vector3(1.4f, 0.82f, 1.1f);
        public const float BenchHeight = 0.92f;
        public const float ConveyorHeight = 0.82f;
        public const float ConveyorLength = 4.2f;

        public static Mesh RackBay(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            var halfDepth = RackDepth * 0.5f - 0.04f;
            foreach (var side in new[] { -1f, 1f })
            {
                var x = side * RackBayWidth * 0.5f;
                foreach (var z in new[] { -halfDepth, halfDepth })
                {
                    builder.AddBeveledBox(new Vector3(x, RackHeight * 0.5f, z), new Vector3(0.08f, RackHeight, 0.07f), 0.008f, 1, 0);
                    builder.AddBeveledBox(new Vector3(x, 0.006f, z), new Vector3(0.14f, 0.012f, 0.14f), 0.003f, 1, 0);
                    for (var hole = 0.25f; hole < RackHeight - 0.1f; hole += 0.1f)
                    {
                        builder.AddBox(new Vector3(x - side * 0.0f, hole, z + (z > 0f ? 0.0355f : -0.0355f)), new Vector3(0.016f, 0.03f, 0.002f), 1);
                    }
                }

                for (var y = 0.35f; y < RackHeight - 0.2f; y += 1.1f)
                {
                    builder.AddBeveledBox(new Vector3(x, y, 0f), new Vector3(0.035f, 0.035f, halfDepth * 2f), 0.006f, 1, 0);
                    var rise = 0.55f;
                    var length = Mathf.Sqrt(rise * rise + halfDepth * halfDepth * 4f);
                    var angle = Mathf.Atan2(rise, halfDepth * 2f) * Mathf.Rad2Deg;
                    builder.AddBeveledBox(Vector3.zero, new Vector3(0.03f, 0.03f, length), 0.005f, 1, 0, TidyMeshBuilder.TRS(new Vector3(x, y + rise * 0.5f, 0f), new Vector3(-angle, 0f, 0f), Vector3.one));
                    builder.AddBeveledBox(Vector3.zero, new Vector3(0.03f, 0.03f, length), 0.005f, 1, 0, TidyMeshBuilder.TRS(new Vector3(x, y + rise * 1.5f, 0f), new Vector3(angle, 0f, 0f), Vector3.one));
                }
            }

            foreach (var level in RackLevels)
            {
                foreach (var z in new[] { -halfDepth, halfDepth })
                {
                    builder.AddBeveledBox(new Vector3(0f, level - 0.06f, z), new Vector3(RackBayWidth - 0.08f, 0.11f, 0.05f), 0.01f, 1, 1);
                }

                AddWireDeck(builder, level);
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_RackBay", folder);
        }

        static void AddWireDeck(TidyMeshBuilder builder, float level)
        {
            var width = RackBayWidth - 0.1f;
            var depth = RackDepth - 0.08f;
            for (var x = -width * 0.5f; x <= width * 0.5f + 0.001f; x += width / 24f)
            {
                builder.AddBox(new Vector3(x, level - 0.006f, 0f), new Vector3(0.006f, 0.006f, depth), 2);
            }

            for (var z = -depth * 0.5f; z <= depth * 0.5f + 0.001f; z += depth / 8f)
            {
                builder.AddBox(new Vector3(0f, level - 0.003f, z), new Vector3(width, 0.006f, 0.006f), 2);
            }

            foreach (var x in new[] { -0.9f, -0.3f, 0.3f, 0.9f })
            {
                builder.AddBox(new Vector3(x, level - 0.03f, 0f), new Vector3(0.03f, 0.04f, depth), 2);
            }
        }

        public static Mesh Pallet(string folder)
        {
            var builder = new TidyMeshBuilder();
            const float length = 1.2f;
            const float width = 1.0f;
            for (var index = 0; index < 7; index++)
            {
                var z = -width * 0.5f + 0.06f + index * (width - 0.12f) / 6f;
                builder.AddBeveledBox(new Vector3(0f, PalletHeight - 0.011f, z), new Vector3(length, 0.022f, 0.1f), 0.004f, 1);
            }

            foreach (var x in new[] { -0.55f, 0f, 0.55f })
            {
                builder.AddBeveledBox(new Vector3(x, PalletHeight - 0.033f, 0f), new Vector3(0.1f, 0.022f, width), 0.004f, 1);
                foreach (var z in new[] { -0.44f, 0f, 0.44f })
                {
                    builder.AddBeveledBox(new Vector3(x, 0.022f + 0.039f, z), new Vector3(0.1f, 0.078f, 0.12f), 0.006f, 1);
                }

                builder.AddBeveledBox(new Vector3(x, 0.011f, 0f), new Vector3(0.12f, 0.022f, width), 0.004f, 1);
            }

            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Pallet", folder);
        }

        public static Mesh Bin(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            var size = BinSize;
            const float wall = 0.05f;
            const float skid = 0.1f;
            var half = size * 0.5f;
            foreach (var x in new[] { -0.5f, 0f, 0.5f })
            {
                builder.AddBeveledBox(new Vector3(x * (size.x - 0.12f), skid * 0.5f, 0f), new Vector3(0.12f, skid, size.z), 0.01f, 1, 1);
            }

            builder.AddBeveledBox(new Vector3(0f, skid + 0.02f, 0f), new Vector3(size.x, 0.04f, size.z), 0.012f, 1, 0);
            var wallHeight = size.y - skid;
            var wallCenter = skid + wallHeight * 0.5f;
            builder.AddBeveledBox(new Vector3(0f, wallCenter, -half.z + wall * 0.5f), new Vector3(size.x, wallHeight, wall), 0.014f, 2, 0);
            var front = wallHeight;
            builder.AddBeveledBox(new Vector3(0f, skid + front * 0.5f, half.z - wall * 0.5f), new Vector3(size.x, front, wall), 0.014f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, size.y - 0.03f, half.z + 0.01f), new Vector3(size.x + 0.02f, 0.06f, 0.05f), 0.015f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, size.y - 0.03f, -half.z - 0.01f), new Vector3(size.x + 0.02f, 0.06f, 0.05f), 0.015f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, skid + wallHeight * 0.5f, half.z + 0.012f), new Vector3(0.5f, 0.3f, 0.02f), 0.006f, 1, 1);
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBeveledBox(new Vector3(side * (half.x - wall * 0.5f), wallCenter, 0f), new Vector3(wall, wallHeight, size.z), 0.014f, 2, 0);
                builder.AddBeveledBox(new Vector3(side * (half.x - 0.03f), wallCenter, half.z - 0.03f), new Vector3(0.08f, wallHeight + 0.01f, 0.08f), 0.02f, 2, 0);
                builder.AddBeveledBox(new Vector3(side * (half.x - 0.03f), wallCenter, -half.z + 0.03f), new Vector3(0.08f, wallHeight + 0.01f, 0.08f), 0.02f, 2, 0);
                for (var rib = 1; rib <= 3; rib++)
                {
                    var z = -half.z + rib * size.z / 4f;
                    builder.AddBeveledBox(new Vector3(side * (half.x + 0.008f), wallCenter, z), new Vector3(0.02f, wallHeight - 0.08f, 0.04f), 0.006f, 1, 0);
                }
            }

            for (var rib = 1; rib <= 4; rib++)
            {
                var x = -half.x + rib * size.x / 5f;
                builder.AddBeveledBox(new Vector3(x, skid + front * 0.5f, half.z + 0.008f), new Vector3(0.04f, front - 0.06f, 0.02f), 0.006f, 1, 0);
                builder.AddBeveledBox(new Vector3(x, wallCenter, -half.z - 0.008f), new Vector3(0.04f, wallHeight - 0.08f, 0.02f), 0.006f, 1, 0);
            }

            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Bin", folder);
        }

        public static Mesh BinFill(string folder)
        {
            var builder = new TidyMeshBuilder();
            builder.AddBeveledBox(new Vector3(0f, 0.02f, 0f), new Vector3(BinSize.x - 0.14f, 0.04f, BinSize.z - 0.14f), 0.015f, 2);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_BinFill", folder);
        }

        public static Mesh Workbench(string folder, out float topHeight)
        {
            var builder = new TidyMeshBuilder(3);
            const float length = 2.4f;
            const float depth = 0.8f;
            topHeight = BenchHeight;
            builder.AddBeveledBox(new Vector3(0f, BenchHeight - 0.03f, 0f), new Vector3(length, 0.06f, depth), 0.01f, 2, 1);
            builder.AddBeveledBox(new Vector3(0f, 0.2f, 0f), new Vector3(length - 0.12f, 0.03f, depth - 0.1f), 0.006f, 1, 1);
            foreach (var x in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -1f, 1f })
                {
                    builder.AddBeveledBox(new Vector3(x * (length * 0.5f - 0.06f), (BenchHeight - 0.06f) * 0.5f, z * (depth * 0.5f - 0.06f)), new Vector3(0.05f, BenchHeight - 0.06f, 0.05f), 0.006f, 1, 0);
                }

                builder.AddBeveledBox(new Vector3(x * (length * 0.5f - 0.06f), 0.12f, 0f), new Vector3(0.04f, 0.04f, depth - 0.1f), 0.005f, 1, 0);
            }

            builder.AddBeveledBox(new Vector3(0f, BenchHeight - 0.1f, -depth * 0.5f + 0.06f), new Vector3(length - 0.12f, 0.06f, 0.04f), 0.005f, 1, 0);
            builder.AddBeveledBox(new Vector3(0.7f, 0.52f, 0.02f), new Vector3(0.62f, 0.56f, depth - 0.14f), 0.012f, 2, 2);
            for (var drawer = 0; drawer < 3; drawer++)
            {
                var y = 0.34f + drawer * 0.17f;
                builder.AddBeveledBox(new Vector3(0.7f, y, depth * 0.5f - 0.05f), new Vector3(0.58f, 0.15f, 0.02f), 0.006f, 1, 2);
                builder.AddBeveledBox(new Vector3(0.7f, y + 0.03f, depth * 0.5f - 0.03f), new Vector3(0.22f, 0.02f, 0.02f), 0.006f, 1, 0);
            }

            builder.AddBeveledBox(new Vector3(-1.0f, BenchHeight + 0.04f, depth * 0.5f - 0.1f), new Vector3(0.16f, 0.08f, 0.14f), 0.01f, 1, 2);
            builder.AddBeveledBox(new Vector3(-1.0f, BenchHeight + 0.09f, depth * 0.5f - 0.03f), new Vector3(0.18f, 0.06f, 0.03f), 0.006f, 1, 0);
            builder.AddCylinder(0.01f, 0.18f, 10, 0, TidyMeshBuilder.TRS(new Vector3(-1.0f, BenchHeight + 0.06f, depth * 0.5f + 0.1f), new Vector3(90f, 0f, 0f), Vector3.one));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Workbench", folder);
        }

        public static Mesh Pegboard(string folder, float width, float height)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBox(new Vector3(0f, height * 0.5f, 0.01f), new Vector3(width, height, 0.012f), 0);
            builder.AddBeveledBox(new Vector3(0f, height + 0.02f, 0.02f), new Vector3(width + 0.08f, 0.04f, 0.04f), 0.008f, 1, 1);
            builder.AddBeveledBox(new Vector3(0f, -0.02f, 0.02f), new Vector3(width + 0.08f, 0.04f, 0.04f), 0.008f, 1, 1);
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBeveledBox(new Vector3(side * (width * 0.5f + 0.02f), height * 0.5f, 0.02f), new Vector3(0.04f, height + 0.08f, 0.04f), 0.008f, 1, 1);
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_Pegboard", folder);
        }

        public static Mesh Hook(string folder)
        {
            var builder = new TidyMeshBuilder();
            builder.AddCylinder(0.003f, 0.05f, 8, 0, TidyMeshBuilder.TRS(Vector3.zero, new Vector3(90f, 0f, 0f), Vector3.one));
            builder.AddCylinder(0.003f, 0.018f, 8, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0f, 0.05f), new Vector3(-30f, 0f, 0f), Vector3.one));
            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_Hook", folder);
        }

        public static Mesh Conveyor(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            const float width = 0.62f;
            var half = ConveyorLength * 0.5f;
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBeveledBox(new Vector3(0f, ConveyorHeight - 0.04f, side * width * 0.5f), new Vector3(ConveyorLength, 0.12f, 0.04f), 0.008f, 1, 0);
                builder.AddBeveledBox(new Vector3(0f, ConveyorHeight + 0.05f, side * (width * 0.5f + 0.01f)), new Vector3(ConveyorLength, 0.03f, 0.02f), 0.006f, 1, 1);
                for (var x = -half + 0.2f; x <= half - 0.19f; x += 1.0f)
                {
                    builder.AddBeveledBox(new Vector3(x, (ConveyorHeight - 0.1f) * 0.5f, side * (width * 0.5f - 0.03f)), new Vector3(0.05f, ConveyorHeight - 0.1f, 0.05f), 0.006f, 1, 0);
                    builder.AddBeveledBox(new Vector3(x, 0.01f, side * (width * 0.5f - 0.03f)), new Vector3(0.12f, 0.02f, 0.1f), 0.004f, 1, 0);
                }
            }

            for (var x = -half + 0.2f; x <= half - 0.19f; x += 1.0f)
            {
                builder.AddBeveledBox(new Vector3(x, 0.3f, 0f), new Vector3(0.04f, 0.04f, width - 0.06f), 0.005f, 1, 0);
            }

            for (var x = -half + 0.06f; x <= half - 0.05f; x += 0.1f)
            {
                builder.AddCylinder(0.024f, width - 0.06f, 14, 2, TidyMeshBuilder.TRS(new Vector3(x, ConveyorHeight - 0.024f, -width * 0.5f + 0.03f), new Vector3(90f, 0f, 0f), Vector3.one));
            }

            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Conveyor", folder);
        }

        public static Mesh RollerDoor(string folder, float width, float height, float openHeight)
        {
            var builder = new TidyMeshBuilder(3);
            for (var y = openHeight; y < height - 0.02f; y += 0.1f)
            {
                builder.AddBeveledBox(new Vector3(0f, y + 0.045f, 0f), new Vector3(width, 0.09f, 0.03f), 0.012f, 2, 0);
            }

            builder.AddBeveledBox(new Vector3(0f, openHeight + 0.02f, 0.005f), new Vector3(width + 0.02f, 0.05f, 0.05f), 0.01f, 1, 1);
            builder.AddBeveledBox(new Vector3(0f, height + 0.28f, -0.12f), new Vector3(width + 0.5f, 0.56f, 0.5f), 0.04f, 2, 2);
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBeveledBox(new Vector3(side * (width * 0.5f + 0.08f), height * 0.5f, 0f), new Vector3(0.16f, height, 0.14f), 0.015f, 2, 2);
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_RollerDoor", folder);
        }

        public static Mesh Lamp(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            var shade = new[]
            {
                new Vector2(0.03f, 0.3f), new Vector2(0.06f, 0.29f), new Vector2(0.07f, 0.24f), new Vector2(0.12f, 0.2f), new Vector2(0.24f, 0.11f),
                new Vector2(0.3f, 0.04f), new Vector2(0.31f, 0.0f), new Vector2(0.29f, 0.0f), new Vector2(0.28f, 0.03f), new Vector2(0.22f, 0.1f)
            };
            builder.AddLathe(shade, 32, 0, null, false, false);
            builder.AddCylinder(0.006f, 2.2f, 8, 1, Matrix4x4.Translate(new Vector3(0f, 0.3f, 0f)));
            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_Lamp", folder);
        }

        public static Mesh Bulb(string folder)
        {
            var builder = new TidyMeshBuilder();
            builder.AddSphere(new Vector3(0f, 0.12f, 0f), 0.075f, 16, 10);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_Bulb", folder);
        }

        public static Mesh Bollard(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(new[] { new Vector2(0.1f, 0f), new Vector2(0.1f, 0.95f), new Vector2(0.085f, 1.02f), new Vector2(0.04f, 1.06f), new Vector2(0.001f, 1.065f) }, 24, 0);
            builder.AddCylinder(0.103f, 0.08f, 24, 1, Matrix4x4.Translate(new Vector3(0f, 0.78f, 0f)), false, false);
            builder.AddCylinder(0.103f, 0.08f, 24, 1, Matrix4x4.Translate(new Vector3(0f, 0.58f, 0f)), false, false);
            builder.AddBeveledCylinder(0.16f, 0.02f, 0.004f, 24, 1, 1);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Bollard", folder);
        }

        public static Mesh FencePanel(string folder, float width, float height)
        {
            var builder = new TidyMeshBuilder(2);
            foreach (var x in new[] { -width * 0.5f, width * 0.5f })
            {
                builder.AddBeveledBox(new Vector3(x, height * 0.5f, 0f), new Vector3(0.06f, height, 0.06f), 0.008f, 1, 0);
                builder.AddBeveledBox(new Vector3(x, 0.006f, 0f), new Vector3(0.16f, 0.012f, 0.16f), 0.003f, 1, 0);
            }

            foreach (var y in new[] { 0.12f, height - 0.04f })
            {
                builder.AddBeveledBox(new Vector3(0f, y, 0f), new Vector3(width, 0.035f, 0.035f), 0.006f, 1, 0);
            }

            for (var x = -width * 0.5f + 0.06f; x < width * 0.5f - 0.03f; x += 0.06f)
            {
                builder.AddBox(new Vector3(x, (height + 0.1f) * 0.5f, 0f), new Vector3(0.005f, height - 0.18f, 0.005f), 1);
            }

            for (var y = 0.18f; y < height - 0.06f; y += 0.06f)
            {
                builder.AddBox(new Vector3(0f, y, 0.005f), new Vector3(width - 0.06f, 0.005f, 0.005f), 1);
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_Fence_" + Mathf.RoundToInt(width * 100f), folder);
        }

        public static Mesh Desk(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddBeveledBox(new Vector3(0f, 0.73f, 0f), new Vector3(1.6f, 0.04f, 0.75f), 0.008f, 2, 0);
            foreach (var x in new[] { -0.76f, 0.76f })
            {
                builder.AddBeveledBox(new Vector3(x, 0.355f, 0f), new Vector3(0.04f, 0.71f, 0.7f), 0.006f, 1, 1);
            }

            builder.AddBeveledBox(new Vector3(0.45f, 0.36f, 0.0f), new Vector3(0.42f, 0.66f, 0.66f), 0.01f, 2, 1);
            for (var drawer = 0; drawer < 3; drawer++)
            {
                builder.AddBeveledBox(new Vector3(0.45f, 0.15f + drawer * 0.21f, 0.335f), new Vector3(0.38f, 0.19f, 0.015f), 0.004f, 1, 1);
                builder.AddBeveledBox(new Vector3(0.45f, 0.2f + drawer * 0.21f, 0.35f), new Vector3(0.14f, 0.015f, 0.015f), 0.004f, 1, 2);
            }

            builder.AddBeveledBox(new Vector3(-0.2f, 0.98f, -0.22f), new Vector3(0.56f, 0.34f, 0.03f), 0.01f, 1, 2);
            builder.AddBeveledBox(new Vector3(-0.2f, 0.8f, -0.24f), new Vector3(0.05f, 0.14f, 0.05f), 0.006f, 1, 2);
            builder.AddBeveledBox(new Vector3(-0.2f, 0.755f, -0.24f), new Vector3(0.22f, 0.012f, 0.16f), 0.004f, 1, 2);
            builder.AddBeveledBox(new Vector3(-0.2f, 0.76f, 0.08f), new Vector3(0.44f, 0.02f, 0.15f), 0.004f, 1, 2);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Desk", folder);
        }

        public static Mesh DeskScreen(string folder)
        {
            var builder = new TidyMeshBuilder();
            builder.AddBox(new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.28f, 0.002f));
            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_DeskScreen", folder);
        }

        public static Mesh OfficeChair(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            for (var arm = 0; arm < 5; arm++)
            {
                var angle = arm * 72f;
                builder.AddBeveledBox(Vector3.zero, new Vector3(0.04f, 0.03f, 0.3f), 0.008f, 1, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.07f, 0f), new Vector3(0f, angle, 0f), Vector3.one) * Matrix4x4.Translate(new Vector3(0f, 0f, 0.15f)));
                builder.AddSphere(Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.03f, 0.28f), 0.03f, 10, 6, 1);
            }

            builder.AddCylinder(0.025f, 0.4f, 12, 1, Matrix4x4.Translate(new Vector3(0f, 0.07f, 0f)));
            builder.AddBeveledBox(new Vector3(0f, 0.48f, 0f), new Vector3(0.5f, 0.08f, 0.48f), 0.03f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.82f, -0.24f), new Vector3(0.46f, 0.52f, 0.07f), 0.03f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.6f, -0.23f), new Vector3(0.06f, 0.2f, 0.03f), 0.008f, 1, 1);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_OfficeChair", folder);
        }

        public static Mesh Cabinet(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBeveledBox(new Vector3(0f, 0.65f, 0f), new Vector3(0.46f, 1.3f, 0.6f), 0.01f, 2, 0);
            for (var drawer = 0; drawer < 4; drawer++)
            {
                builder.AddBeveledBox(new Vector3(0f, 0.17f + drawer * 0.31f, 0.302f), new Vector3(0.42f, 0.28f, 0.01f), 0.004f, 1, 0);
                builder.AddBeveledBox(new Vector3(0f, 0.25f + drawer * 0.31f, 0.312f), new Vector3(0.14f, 0.02f, 0.015f), 0.005f, 1, 1);
            }

            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Cabinet", folder);
        }

        public static Mesh Partition(string folder, float width, float height, out Mesh glass)
        {
            var builder = new TidyMeshBuilder(2);
            const float sill = 1.0f;
            builder.AddBeveledBox(new Vector3(0f, sill * 0.5f, 0f), new Vector3(width, sill, 0.08f), 0.01f, 1, 0);
            builder.AddBeveledBox(new Vector3(0f, height - 0.05f, 0f), new Vector3(width, 0.1f, 0.08f), 0.01f, 1, 1);
            builder.AddBeveledBox(new Vector3(0f, sill + 0.02f, 0f), new Vector3(width, 0.04f, 0.1f), 0.008f, 1, 1);
            var mullions = Mathf.Max(1, Mathf.RoundToInt(width / 1.2f));
            for (var index = 0; index <= mullions; index++)
            {
                var x = -width * 0.5f + index * width / mullions;
                builder.AddBeveledBox(new Vector3(x, height * 0.5f, 0f), new Vector3(0.05f, height, 0.07f), 0.008f, 1, 1);
            }

            var pane = new TidyMeshBuilder();
            pane.AddBox(new Vector3(0f, (sill + height) * 0.5f, 0f), new Vector3(width, height - sill - 0.1f, 0.01f));
            glass = TidyMeshUtility.SaveAsIs(pane, "SM_Warehouse_PartitionGlass_" + Mathf.RoundToInt(width * 100f), folder);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_Partition_" + Mathf.RoundToInt(width * 100f), folder);
        }

        public static Mesh Extinguisher(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(new[] { new Vector2(0.07f, 0f), new Vector2(0.08f, 0.02f), new Vector2(0.08f, 0.45f), new Vector2(0.06f, 0.5f), new Vector2(0.025f, 0.52f), new Vector2(0.02f, 0.55f) }, 20, 0);
            builder.AddBeveledBox(new Vector3(0.0f, 0.58f, 0f), new Vector3(0.03f, 0.05f, 0.12f), 0.006f, 1, 1);
            builder.AddCylinder(0.01f, 0.3f, 8, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.56f, 0.06f), new Vector3(150f, 0f, 0f), Vector3.one));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Warehouse_Extinguisher", folder);
        }

        public static Mesh Truss(string folder, float span)
        {
            var builder = new TidyMeshBuilder();
            const float depth = 0.7f;
            builder.AddBeveledBox(new Vector3(0f, 0f, 0f), new Vector3(span, 0.08f, 0.1f), 0.008f, 1);
            builder.AddBeveledBox(new Vector3(0f, depth, 0f), new Vector3(span, 0.08f, 0.1f), 0.008f, 1);
            var panels = Mathf.RoundToInt(span / 0.9f);
            var step = span / panels;
            for (var index = 0; index < panels; index++)
            {
                var x = -span * 0.5f + index * step;
                builder.AddBox(new Vector3(x, depth * 0.5f, 0f), new Vector3(0.05f, depth, 0.06f));
                var length = Mathf.Sqrt(step * step + depth * depth);
                var angle = Mathf.Atan2(depth, step) * Mathf.Rad2Deg * (index % 2 == 0 ? 1f : -1f);
                builder.AddBox(Vector3.zero, new Vector3(length, 0.04f, 0.05f), 0, TidyMeshBuilder.TRS(new Vector3(x + step * 0.5f, depth * 0.5f, 0f), new Vector3(0f, 0f, angle), Vector3.one));
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_Truss_" + Mathf.RoundToInt(span * 10f), folder);
        }

        public static Mesh SignBox(string folder, Vector2 size)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBeveledBox(new Vector3(0f, 0f, 0f), new Vector3(size.x, size.y, 0.08f), 0.015f, 2, 0);
            builder.AddBox(new Vector3(0f, 0f, 0.041f), new Vector3(size.x - 0.08f, size.y - 0.08f, 0.002f), 1);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Warehouse_Sign_" + Mathf.RoundToInt(size.x * 100f) + "x" + Mathf.RoundToInt(size.y * 100f), folder);
        }
    }
}
