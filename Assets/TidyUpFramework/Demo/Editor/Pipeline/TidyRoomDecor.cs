using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyRoomDecor
    {
        public const string Folder = TidyRoomModels.Folder;
        public const float WindowWidth = 2.2f;
        public const float WindowHeight = 1.5f;
        public const float BinRadius = 0.16f;
        public const float BinHeight = 0.38f;
        public const float BasketRadius = 0.22f;
        public const float BasketHeight = 0.42f;

        public static Mesh Lamp()
        {
            var builder = new TidyMeshBuilder(4);
            builder.AddLathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(0.06f, 0f), new Vector2(0.075f, 0.03f), new Vector2(0.08f, 0.09f), new Vector2(0.06f, 0.15f), new Vector2(0.02f, 0.18f), new Vector2(0.015f, 0.19f)
            }, 24, 0, null, false, true);
            builder.AddCylinder(0.006f, 0.13f, 8, 1, Matrix4x4.Translate(new Vector3(0f, 0.19f, 0f)));
            builder.AddLathe(new[] { new Vector2(0.13f, 0.24f), new Vector2(0.085f, 0.4f) }, 28, 2, null, false, false);
            builder.AddLathe(new[] { new Vector2(0.083f, 0.398f), new Vector2(0.127f, 0.242f) }, 28, 2, null, false, false);
            builder.AddSphere(new Vector3(0f, 0.3f, 0f), 0.03f, 12, 8, 3);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Lamp", Folder);
        }

        public static Mesh FloorLamp()
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddLathe(TidyProfiles.RoundedCylinder(0.16f, 0.03f, 0.01f, 2), 24, 0);
            builder.AddCylinder(0.012f, 1.3f, 10, 0, Matrix4x4.Translate(new Vector3(0f, 0.03f, 0f)));
            builder.AddLathe(new[] { new Vector2(0.22f, 1.22f), new Vector2(0.15f, 1.5f) }, 30, 1, null, false, false);
            builder.AddLathe(new[] { new Vector2(0.148f, 1.497f), new Vector2(0.217f, 1.223f) }, 30, 1, null, false, false);
            builder.AddSphere(new Vector3(0f, 1.34f, 0f), 0.04f, 12, 8, 2);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_FloorLamp", Folder);
        }

        public static Mesh Pendant()
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddCylinder(0.005f, 0.75f, 6, 0, Matrix4x4.Translate(new Vector3(0f, -0.75f, 0f)));
            builder.AddLathe(new[] { new Vector2(0.06f, 0f), new Vector2(0.06f, -0.015f) }, 16, 0, null, true, true);
            builder.AddLathe(new[]
            {
                new Vector2(0.03f, -0.74f), new Vector2(0.09f, -0.78f), new Vector2(0.19f, -0.88f), new Vector2(0.24f, -0.98f), new Vector2(0.245f, -1.0f)
            }, 32, 1, null, false, false);
            builder.AddLathe(new[]
            {
                new Vector2(0.24f, -0.996f), new Vector2(0.235f, -0.976f), new Vector2(0.186f, -0.876f), new Vector2(0.087f, -0.776f), new Vector2(0.028f, -0.738f)
            }, 32, 1, null, false, false);
            builder.AddSphere(new Vector3(0f, -0.86f, 0f), 0.045f, 14, 10, 2);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Pendant", Folder);
        }

        public static Mesh PottedPlant(int kind)
        {
            var builder = new TidyMeshBuilder(4);
            var potRadius = kind == 2 ? 0.2f : kind == 1 ? 0.075f : 0.11f;
            var potHeight = kind == 2 ? 0.36f : kind == 1 ? 0.1f : 0.2f;
            builder.AddLathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(potRadius * 0.72f, 0f), new Vector2(potRadius * 0.95f, potHeight * 0.82f), new Vector2(potRadius * 1.06f, potHeight * 0.84f),
                new Vector2(potRadius * 1.06f, potHeight), new Vector2(potRadius * 0.92f, potHeight)
            }, 24, 0, null, false, false);
            builder.AddLathe(new[] { new Vector2(potRadius * 0.92f, potHeight - 0.02f), new Vector2(0f, potHeight - 0.02f) }, 24, 1, null, false, false);
            var random = new System.Random(kind * 31 + 7);
            switch (kind)
            {
                case 1:
                    TrailingLeaves(builder, random, potHeight, potRadius);
                    break;
                case 2:
                    FiddleLeaves(builder, random, potHeight);
                    break;
                default:
                    SnakeLeaves(builder, random, potHeight);
                    break;
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Plant_" + kind, Folder);
        }

        static void SnakeLeaves(TidyMeshBuilder builder, System.Random random, float potHeight)
        {
            for (var leaf = 0; leaf < 9; leaf++)
            {
                var height = 0.35f + (float)random.NextDouble() * 0.25f;
                var outline = new[]
                {
                    new Vector2(-0.016f, 0f), new Vector2(0.016f, 0f), new Vector2(0.028f, height * 0.4f), new Vector2(0.022f, height * 0.8f), new Vector2(0f, height), new Vector2(-0.022f, height * 0.8f), new Vector2(-0.028f, height * 0.4f)
                };
                var yaw = leaf * 40f + (float)random.NextDouble() * 20f;
                var radial = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 0.04f;
                builder.AddExtrusion(outline, 0.006f, 2 + leaf % 2, TidyMeshBuilder.TRS(radial + Vector3.up * (potHeight - 0.03f), new Vector3(-8f - (float)random.NextDouble() * 10f, yaw, 0f), Vector3.one), 0.002f);
            }
        }

        static void TrailingLeaves(TidyMeshBuilder builder, System.Random random, float potHeight, float potRadius)
        {
            for (var leaf = 0; leaf < 22; leaf++)
            {
                var yaw = (float)random.NextDouble() * 360f;
                var reach = potRadius * (0.3f + (float)random.NextDouble() * 1.1f);
                var drop = reach > potRadius ? -(float)random.NextDouble() * 0.08f : 0.02f + (float)random.NextDouble() * 0.06f;
                var center = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * reach + Vector3.up * (potHeight + drop);
                builder.AddSphere(Vector3.zero, 0.028f, 10, 6, 2 + leaf % 2, TidyMeshBuilder.TRS(center, new Vector3(-30f + (float)random.NextDouble() * 50f, yaw, 0f), new Vector3(1f, 0.18f, 1.3f)));
            }
        }

        static void FiddleLeaves(TidyMeshBuilder builder, System.Random random, float potHeight)
        {
            builder.AddLathe(new[] { new Vector2(0.025f, 0f), new Vector2(0.016f, 1.25f) }, 10, 1, Matrix4x4.Translate(new Vector3(0f, potHeight - 0.03f, 0f)));
            for (var leaf = 0; leaf < 26; leaf++)
            {
                var t = leaf / 25f;
                var y = potHeight + 0.35f + t * 1.0f;
                var yaw = leaf * 137.5f;
                var reach = 0.1f + (1f - Mathf.Abs(t - 0.55f)) * 0.12f;
                var center = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * reach + Vector3.up * y;
                var size = 0.85f + (float)random.NextDouble() * 0.3f;
                builder.AddSphere(Vector3.zero, 0.1f, 12, 6, 2 + leaf % 2, TidyMeshBuilder.TRS(center, new Vector3(-35f + (float)random.NextDouble() * 30f, yaw, 0f), new Vector3(0.8f * size, 0.06f, 1.15f * size)));
            }
        }

        public static Mesh Window(out Mesh glass)
        {
            var frame = new TidyMeshBuilder(2);
            var halfW = WindowWidth * 0.5f;
            const float bar = 0.08f;
            const float depth = 0.1f;
            frame.AddBeveledBox(new Vector3(-halfW + bar * 0.5f, WindowHeight * 0.5f, depth * 0.5f), new Vector3(bar, WindowHeight, depth), 0.012f, 2, 0);
            frame.AddBeveledBox(new Vector3(halfW - bar * 0.5f, WindowHeight * 0.5f, depth * 0.5f), new Vector3(bar, WindowHeight, depth), 0.012f, 2, 0);
            frame.AddBeveledBox(new Vector3(0f, WindowHeight - bar * 0.5f, depth * 0.5f), new Vector3(WindowWidth, bar, depth), 0.012f, 2, 0);
            frame.AddBeveledBox(new Vector3(0f, bar * 0.5f, depth * 0.5f), new Vector3(WindowWidth, bar, depth), 0.012f, 2, 0);
            frame.AddBeveledBox(new Vector3(0f, WindowHeight * 0.5f, 0.03f), new Vector3(0.05f, WindowHeight - bar, 0.05f), 0.008f, 1, 0);
            foreach (var x in new[] { -halfW * 0.5f, halfW * 0.5f })
            {
                frame.AddBeveledBox(new Vector3(x, WindowHeight * 0.5f, 0.03f), new Vector3(0.03f, WindowHeight - bar, 0.03f), 0.006f, 1, 0);
            }

            frame.AddBeveledBox(new Vector3(0f, WindowHeight * 0.62f, 0.03f), new Vector3(WindowWidth - bar, 0.04f, 0.04f), 0.008f, 1, 0);
            frame.AddBeveledBox(new Vector3(0f, -0.02f, 0.12f), new Vector3(WindowWidth + 0.2f, 0.045f, 0.26f), 0.012f, 2, 1);
            frame.AddBeveledBox(new Vector3(0f, -0.08f, 0.04f), new Vector3(WindowWidth + 0.06f, 0.09f, 0.06f), 0.012f, 2, 0);
            var glassBuilder = new TidyMeshBuilder();
            glassBuilder.AddBox(new Vector3(0f, WindowHeight * 0.5f, 0.006f), new Vector3(WindowWidth - 0.1f, WindowHeight - 0.1f, 0.01f));
            glass = TidyMeshUtility.SaveAsIs(glassBuilder, "SM_Room_WindowGlass", Folder);
            return TidyMeshUtility.SaveAsIs(frame, "SM_Room_Window", Folder);
        }

        public static Mesh Curtain(float width, float height)
        {
            var builder = new TidyMeshBuilder { UvScale = new Vector2(2f, 2f) };
            const int columns = 28;
            const int rows = 6;
            for (var column = 0; column < columns; column++)
            {
                for (var row = 0; row < rows; row++)
                {
                    var a = CurtainPoint(column, row, columns, rows, width, height);
                    var b = CurtainPoint(column + 1, row, columns, rows, width, height);
                    var c = CurtainPoint(column + 1, row + 1, columns, rows, width, height);
                    var d = CurtainPoint(column, row + 1, columns, rows, width, height);
                    builder.AddQuad(a, b, c, d);
                    builder.AddQuad(d, c, b, a);
                }
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Curtain", Folder);
        }

        static Vector3 CurtainPoint(int column, int row, int columns, int rows, float width, float height)
        {
            var u = column / (float)columns;
            var v = row / (float)rows;
            var gather = Mathf.Lerp(0.72f, 1f, v);
            var x = (u - 0.5f) * width * gather;
            var z = Mathf.Sin(u * Mathf.PI * 9f) * 0.035f * Mathf.Lerp(1.25f, 0.85f, v);
            return new Vector3(x, -height * (1f - v), z + 0.05f);
        }

        public static Mesh CurtainRod(float length)
        {
            var builder = new TidyMeshBuilder();
            builder.AddCylinder(0.012f, length, 12, 0, TidyMeshBuilder.TRS(new Vector3(-length * 0.5f, 0f, 0f), new Vector3(0f, 0f, -90f), Vector3.one));
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddSphere(new Vector3(side * (length * 0.5f + 0.02f), 0f, 0f), 0.028f, 12, 8);
                builder.AddBeveledBox(new Vector3(side * (length * 0.5f - 0.06f), 0f, -0.04f), new Vector3(0.02f, 0.03f, 0.1f), 0.005f, 1);
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_CurtainRod", Folder);
        }

        public static Mesh PictureFrame(string meshName, float width, float height, out Mesh canvas)
        {
            var builder = new TidyMeshBuilder();
            const float bar = 0.045f;
            const float depth = 0.035f;
            builder.AddBeveledBox(new Vector3(0f, height * 0.5f - bar * 0.5f, depth * 0.5f), new Vector3(width, bar, depth), 0.008f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, -height * 0.5f + bar * 0.5f, depth * 0.5f), new Vector3(width, bar, depth), 0.008f, 2, 0);
            builder.AddBeveledBox(new Vector3(width * 0.5f - bar * 0.5f, 0f, depth * 0.5f), new Vector3(bar, height, depth), 0.008f, 2, 0);
            builder.AddBeveledBox(new Vector3(-width * 0.5f + bar * 0.5f, 0f, depth * 0.5f), new Vector3(bar, height, depth), 0.008f, 2, 0);
            var w = width * 0.5f - bar * 0.7f;
            var h = height * 0.5f - bar * 0.7f;
            var canvasBuilder = new TidyMeshBuilder();
            canvasBuilder.AddPlane(new Vector2(w * 2f, h * 2f), 1, 1, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0f, 0.012f), new Vector3(-90f, 180f, 0f), Vector3.one), true);
            canvas = TidyMeshUtility.SaveAsIs(canvasBuilder, meshName + "_Canvas", Folder);
            return TidyMeshUtility.SaveAsIs(builder, meshName, Folder);
        }

        public static Mesh Bin()
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddLathe(new[] { new Vector2(0f, 0f), new Vector2(0.12f, 0f), new Vector2(0.13f, 0.01f), new Vector2(BinRadius, BinHeight) }, 28, 0, null, false, false);
            builder.AddLathe(new[] { new Vector2(BinRadius - 0.008f, BinHeight), new Vector2(0.122f, 0.02f), new Vector2(0f, 0.02f) }, 28, 1, null, false, false);
            builder.AddTorus(new Vector3(0f, BinHeight, 0f), BinRadius - 0.002f, 0.009f, 28, 6, 2);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Bin", Folder);
        }

        public static Mesh Bucket()
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddLathe(new[] { new Vector2(0f, 0f), new Vector2(0.12f, 0f), new Vector2(0.125f, 0.01f), new Vector2(0.15f, 0.24f) }, 26, 0, null, false, false);
            builder.AddLathe(new[] { new Vector2(0.143f, 0.24f), new Vector2(0.118f, 0.016f), new Vector2(0f, 0.016f) }, 26, 1, null, false, false);
            builder.AddTorus(new Vector3(0f, 0.24f, 0f), 0.148f, 0.008f, 26, 6, 0);
            builder.AddTorus(Vector3.zero, 0.15f, 0.005f, 24, 5, 2, TidyMeshBuilder.TRS(new Vector3(0f, 0.22f, 0f), new Vector3(70f, 0f, 0f), new Vector3(1f, 1f, 1f)));
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Bucket", Folder);
        }

        public static Mesh LaundryPile(int stage)
        {
            var random = new System.Random(stage * 17 + 3);
            var builder = new TidyMeshBuilder(3) { UvScale = new Vector2(5f, 5f) };
            var count = 4 + stage * 2;
            for (var clump = 0; clump < count; clump++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var reach = (float)random.NextDouble() * 0.1f;
                var center = new Vector3(Mathf.Cos(angle) * reach, (float)random.NextDouble() * 0.04f, Mathf.Sin(angle) * reach);
                builder.AddSphere(Vector3.zero, 0.07f, 12, 8, clump % 3, TidyMeshBuilder.TRS(center, new Vector3(0f, (float)random.NextDouble() * 180f, (float)random.NextDouble() * 20f), new Vector3(1.4f, 0.45f, 1f)));
            }

            var mesh = builder.Build("SM_Room_LaundryPile_" + stage, false);
            TidyMeshUtility.Jitter(mesh, 0.012f, stage + 40);
            TidyMeshUtility.RecenterBottom(mesh);
            return TidyMeshAssets.SaveMesh(mesh, Folder + "/" + mesh.name + ".asset");
        }

        public static Mesh Tray(float width, float depth)
        {
            var builder = new TidyMeshBuilder();
            builder.AddBeveledBox(new Vector3(0f, 0.006f, 0f), new Vector3(width, 0.012f, depth), 0.004f, 1);
            builder.AddBeveledBox(new Vector3(0f, 0.02f, depth * 0.5f - 0.008f), new Vector3(width, 0.04f, 0.016f), 0.006f, 2);
            builder.AddBeveledBox(new Vector3(0f, 0.02f, -depth * 0.5f + 0.008f), new Vector3(width, 0.04f, 0.016f), 0.006f, 2);
            builder.AddBeveledBox(new Vector3(width * 0.5f - 0.008f, 0.025f, 0f), new Vector3(0.016f, 0.05f, depth), 0.006f, 2);
            builder.AddBeveledBox(new Vector3(-width * 0.5f + 0.008f, 0.025f, 0f), new Vector3(0.016f, 0.05f, depth), 0.006f, 2);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Tray", Folder);
        }

        public static Mesh PlateBoard()
        {
            var builder = new TidyMeshBuilder();
            builder.AddLathe(TidyProfiles.RoundedCylinder(0.14f, 0.018f, 0.006f, 2), 32);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_PlateBoard", Folder);
        }

        public static Mesh Kettle()
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(0.08f, 0f), new Vector2(0.095f, 0.03f), new Vector2(0.095f, 0.1f), new Vector2(0.07f, 0.16f), new Vector2(0.03f, 0.18f), new Vector2(0f, 0.18f)
            }, 26, 0);
            builder.AddSphere(new Vector3(0f, 0.19f, 0f), 0.016f, 10, 6, 1);
            builder.AddTorus(Vector3.zero, 0.07f, 0.009f, 18, 6, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.17f, 0f), new Vector3(90f, 90f, 0f), new Vector3(1f, 1f, 0.8f)));
            builder.AddLathe(new[] { new Vector2(0.016f, 0f), new Vector2(0.008f, 0.1f) }, 10, 0, TidyMeshBuilder.TRS(new Vector3(0.08f, 0.06f, 0f), new Vector3(0f, 0f, -50f), Vector3.one));
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Kettle", Folder);
        }

        public static Mesh WallShelf(float width, float depth)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBeveledBox(new Vector3(0f, -0.015f, depth * 0.5f), new Vector3(width, 0.03f, depth), 0.008f, 2, 0);
            foreach (var x in new[] { -width * 0.35f, width * 0.35f })
            {
                builder.AddBeveledBox(new Vector3(x, -0.08f, 0.012f), new Vector3(0.02f, 0.14f, 0.024f), 0.004f, 1, 1);
                builder.AddBeveledBox(new Vector3(x, -0.04f, depth * 0.4f), new Vector3(0.02f, 0.02f, depth * 0.8f), 0.004f, 1, 1, TidyMeshBuilder.TRS(Vector3.zero, Vector3.zero, Vector3.one));
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_WallShelf_" + Mathf.RoundToInt(width * 100f), Folder);
        }

        public static Mesh Jars()
        {
            var builder = new TidyMeshBuilder(3);
            var heights = new[] { 0.16f, 0.12f, 0.2f };
            for (var index = 0; index < heights.Length; index++)
            {
                var offset = Matrix4x4.Translate(new Vector3((index - 1) * 0.13f, 0f, 0f));
                builder.AddLathe(TidyProfiles.Jar(0.05f, heights[index]), 20, 0, offset);
                builder.AddLathe(TidyProfiles.RoundedCylinder(0.044f, heights[index] * 0.55f, 0.01f, 2), 20, 2, offset);
                builder.AddBeveledCylinder(0.046f, 0.022f, 0.004f, 20, 1, 1, offset * Matrix4x4.Translate(new Vector3(0f, heights[index], 0f)));
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Jars", Folder);
        }

        public static Mesh Vase()
        {
            var builder = new TidyMeshBuilder(4);
            builder.AddLathe(TidyProfiles.Vase(0.06f, 0.2f), 24, 0);
            var random = new System.Random(19);
            for (var stem = 0; stem < 5; stem++)
            {
                var yaw = stem * 72f + (float)random.NextDouble() * 20f;
                var tilt = 8f + (float)random.NextDouble() * 14f;
                var length = 0.22f + (float)random.NextDouble() * 0.1f;
                var rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f);
                var basePoint = new Vector3(0f, 0.12f, 0f);
                builder.AddCylinder(0.0035f, length, 6, 1, Matrix4x4.TRS(basePoint, rotation, Vector3.one));
                var head = basePoint + rotation * Vector3.up * length;
                for (var petal = 0; petal < 5; petal++)
                {
                    var direction = Quaternion.Euler(0f, petal * 72f, 0f) * Vector3.forward;
                    builder.AddSphere(Vector3.zero, 0.022f, 10, 6, 2, Matrix4x4.TRS(head + rotation * (direction * 0.02f), rotation * Quaternion.LookRotation(direction), new Vector3(0.7f, 0.25f, 1f)));
                }

                builder.AddSphere(head + rotation * Vector3.up * 0.006f, 0.011f, 10, 6, 3);
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Vase", Folder);
        }

        public static Mesh CoatRail()
        {
            var builder = new TidyMeshBuilder(3) { UvScale = new Vector2(4f, 4f) };
            builder.AddBeveledBox(new Vector3(0f, 0f, 0.012f), new Vector3(0.8f, 0.1f, 0.024f), 0.006f, 2, 0);
            foreach (var x in new[] { -0.28f, 0f, 0.28f })
            {
                builder.AddBeveledBox(new Vector3(x, 0f, 0.05f), new Vector3(0.016f, 0.016f, 0.06f), 0.005f, 1, 1);
                builder.AddSphere(new Vector3(x, 0.012f, 0.08f), 0.012f, 8, 6, 1);
            }

            builder.AddBeveledBox(Vector3.zero, new Vector3(0.14f, 0.72f, 0.03f), 0.012f, 2, 2, TidyMeshBuilder.TRS(new Vector3(-0.28f, -0.34f, 0.075f), new Vector3(0f, 0f, 3f), Vector3.one));
            builder.AddBeveledBox(Vector3.zero, new Vector3(0.11f, 0.5f, 0.026f), 0.01f, 2, 2, TidyMeshBuilder.TRS(new Vector3(0.28f, -0.24f, 0.08f), new Vector3(0f, 0f, -2f), Vector3.one));
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_CoatRail", Folder);
        }

        public static Mesh FairyStrand(string meshName, Vector3[] points, float bulbRadius, bool glowOnly)
        {
            var builder = new TidyMeshBuilder(2);
            if (!glowOnly)
            {
                for (var index = 0; index < points.Length - 1; index++)
                {
                    var delta = points[index + 1] - points[index];
                    builder.AddCylinder(0.0025f, delta.magnitude, 5, 0, Matrix4x4.TRS(points[index], Quaternion.FromToRotation(Vector3.up, delta.normalized), Vector3.one), false, false);
                }
            }

            for (var index = 1; index < points.Length - 1; index += 2)
            {
                builder.AddSphere(points[index] + Vector3.down * 0.02f, bulbRadius, 8, 6, glowOnly ? 0 : 1);
            }

            return TidyMeshUtility.SaveAsIs(builder, meshName, Folder);
        }

        public static Vector3[] Catenary(Vector3 from, Vector3 to, float sag, int segments)
        {
            var points = new Vector3[segments + 1];
            for (var index = 0; index <= segments; index++)
            {
                var t = index / (float)segments;
                points[index] = Vector3.Lerp(from, to, t) + Vector3.down * (sag * 4f * t * (1f - t));
            }

            return points;
        }

        public static Mesh Stain()
        {
            var builder = new TidyMeshBuilder();
            builder.AddPlane(Vector2.one, 1, 1, 0, null, true);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Stain", Folder);
        }
    }
}
