using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyStoreFixtures
    {
        public const float ShelfWidth = 1.9f;
        public const float ShelfHeight = 2.42f;
        public const float ShelfDepth = 0.4f;
        public const int ShelfColumns = 11;
        public const float ShelfColumnSpacing = 0.155f;
        public const float ShelfRowSpacing = 0.3f;
        public static readonly float[] ShelfTops = { 0.32f, 0.62f, 0.92f, 1.22f, 1.52f, 1.82f, 2.12f };

        public static readonly float[] TierHeights = { 0.46f, 0.82f, 1.18f };
        public static readonly float[] TierRadii = { 0.98f, 0.66f, 0.4f };
        public static readonly float[] TierSlotRadii = { 0.82f, 0.53f, 0.24f };
        public static readonly float[][] TierSlotAngles =
        {
            new[] { -22f, -11f, 0f, 11f, 22f },
            new[] { -15f, 0f, 15f },
            new[] { -15f, 15f }
        };

        public const float PedestalTop = 0.92f;
        public const float TableHeight = 0.78f;
        public const float CounterHeight = 0.96f;
        public const float BinHeight = 0.52f;
        public const float BinRadius = 0.3f;
        const string DecorFolder = TidyDemoPaths.Meshes + "/Store/Decor";

        public static Mesh ShelfUnit(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            var half = ShelfWidth * 0.5f;
            builder.AddBeveledBox(new Vector3(0f, 0.11f, 0.01f), new Vector3(ShelfWidth, 0.22f, ShelfDepth + 0.02f), 0.012f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.12f, ShelfDepth * 0.5f + 0.012f), new Vector3(ShelfWidth - 0.16f, 0.12f, 0.012f), 0.006f, 1, 2);
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBeveledBox(new Vector3(side * (half - 0.03f), ShelfHeight * 0.5f, 0f), new Vector3(0.06f, ShelfHeight, ShelfDepth), 0.008f, 2, 0);
            }

            builder.AddBox(new Vector3(0f, ShelfHeight * 0.5f, -ShelfDepth * 0.5f + 0.012f), new Vector3(ShelfWidth - 0.08f, ShelfHeight - 0.02f, 0.02f), 1);
            foreach (var top in ShelfTops)
            {
                builder.AddBeveledBox(new Vector3(0f, top - 0.016f, 0.006f), new Vector3(ShelfWidth - 0.11f, 0.032f, ShelfDepth - 0.03f), 0.006f, 1, 0);
                builder.AddBeveledBox(new Vector3(0f, top - 0.03f, ShelfDepth * 0.5f - 0.004f), new Vector3(ShelfWidth - 0.11f, 0.05f, 0.014f), 0.004f, 1, 2);
            }

            builder.AddBeveledBox(new Vector3(0f, ShelfHeight - 0.05f, 0.02f), new Vector3(ShelfWidth + 0.06f, 0.1f, ShelfDepth + 0.06f), 0.014f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, ShelfHeight - 0.115f, 0.03f), new Vector3(ShelfWidth + 0.02f, 0.03f, ShelfDepth + 0.04f), 0.008f, 1, 2);
            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreShelfUnit", folder);
        }

        public static Mesh TierStand(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            var profile = new[]
            {
                new Vector2(0f, 0f), new Vector2(0.96f, 0f), new Vector2(0.98f, 0.02f), new Vector2(0.98f, 0.44f), new Vector2(0.96f, 0.46f),
                new Vector2(0.66f, 0.46f), new Vector2(0.66f, 0.8f), new Vector2(0.64f, 0.82f),
                new Vector2(0.4f, 0.82f), new Vector2(0.4f, 1.16f), new Vector2(0.38f, 1.18f), new Vector2(0f, 1.18f)
            };
            builder.AddLathe(profile, 64, 0);
            for (var tier = 0; tier < TierHeights.Length; tier++)
            {
                builder.AddTorus(new Vector3(0f, TierHeights[tier] - 0.012f, 0f), TierRadii[tier], 0.016f, 64, 8, 1);
                builder.AddTorus(new Vector3(0f, 0.03f + (tier > 0 ? TierHeights[tier - 1] : 0f), 0f), TierRadii[tier] + 0.004f, 0.012f, 64, 6, 1);
            }

            builder.AddCylinder(0.022f, 0.7f, 12, 1, Matrix4x4.Translate(new Vector3(0f, 1.18f, 0f)));
            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreTierStand", folder);
        }

        public static Mesh TierInlays(string folder, int colours)
        {
            var builder = new TidyMeshBuilder(colours);
            var inner = new[] { 0.67f, 0.41f, 0.05f };
            var outer = new[] { 0.955f, 0.635f, 0.375f };
            for (var tier = 0; tier < TierHeights.Length; tier++)
            {
                for (var sector = 0; sector < colours; sector++)
                {
                    var center = sector * 360f / colours;
                    var outline = AnnularSector(inner[tier], outer[tier], center - 28.5f, center + 28.5f, 10);
                    builder.AddExtrusion(outline, 0.004f, sector, TidyMeshBuilder.TRS(new Vector3(0f, TierHeights[tier] + 0.004f, 0f), new Vector3(90f, 0f, 0f), Vector3.one));
                }
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreTierInlays", folder);
        }

        static Vector2[] AnnularSector(float inner, float outer, float fromDegrees, float toDegrees, int segments)
        {
            var points = new List<Vector2>();
            for (var index = 0; index <= segments; index++)
            {
                var angle = Mathf.Lerp(fromDegrees, toDegrees, index / (float)segments) * Mathf.Deg2Rad;
                points.Add(new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * outer);
            }

            for (var index = segments; index >= 0; index--)
            {
                var angle = Mathf.Lerp(fromDegrees, toDegrees, index / (float)segments) * Mathf.Deg2Rad;
                points.Add(new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * inner);
            }

            return points.ToArray();
        }

        public static Mesh SetPedestal(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            var profile = new[]
            {
                new Vector2(0f, 0f), new Vector2(0.34f, 0f), new Vector2(0.345f, 0.05f), new Vector2(0.32f, 0.075f), new Vector2(0.27f, 0.11f),
                new Vector2(0.24f, 0.16f), new Vector2(0.23f, 0.78f), new Vector2(0.27f, 0.82f), new Vector2(0.31f, 0.86f), new Vector2(0.32f, 0.9f), new Vector2(0f, 0.9f)
            };
            builder.AddLathe(profile, 40, 0);
            builder.AddLathe(TidyProfiles.RoundedCylinder(0.3f, 0.02f, 0.008f, 2), 40, 1, Matrix4x4.Translate(new Vector3(0f, 0.9f, 0f)));
            builder.AddTorus(new Vector3(0f, 0.845f, 0f), 0.29f, 0.012f, 40, 6, 2);
            builder.AddTorus(new Vector3(0f, 0.11f, 0f), 0.272f, 0.01f, 40, 6, 2);
            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreSetPedestal", folder);
        }

        public static Mesh DisplayTable(string folder, float width, float depth)
        {
            var builder = new TidyMeshBuilder(2);
            const float top = 0.05f;
            builder.AddBeveledBox(new Vector3(0f, TableHeight - top * 0.5f, 0f), new Vector3(width, top, depth), 0.016f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, TableHeight - top - 0.07f, 0f), new Vector3(width - 0.1f, 0.14f, depth - 0.1f), 0.01f, 1, 1);
            foreach (var corner in new[] { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f) })
            {
                var leg = new[] { new Vector2(0.03f, 0f), new Vector2(0.04f, 0.04f), new Vector2(0.03f, 0.1f), new Vector2(0.028f, TableHeight - top - 0.14f), new Vector2(0.038f, TableHeight - top) };
                builder.AddLathe(leg, 12, 0, Matrix4x4.Translate(new Vector3(corner.x * (width * 0.5f - 0.1f), 0f, corner.y * (depth * 0.5f - 0.1f))));
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreTable_" + Mathf.RoundToInt(width * 100f), folder);
        }

        public static Mesh TripletTray(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBeveledBox(new Vector3(0f, 0.008f, 0f), new Vector3(0.46f, 0.016f, 0.17f), 0.005f, 1, 0);
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBeveledBox(new Vector3(0f, 0.016f, side * 0.08f), new Vector3(0.46f, 0.03f, 0.012f), 0.004f, 1, 0);
                builder.AddBeveledBox(new Vector3(side * 0.224f, 0.016f, 0f), new Vector3(0.012f, 0.03f, 0.17f), 0.004f, 1, 0);
            }

            for (var index = -1; index <= 1; index++)
            {
                builder.AddBeveledCylinder(0.045f, 0.006f, 0.002f, 20, 1, 1, Matrix4x4.Translate(new Vector3(index * 0.13f, 0.014f, 0f)));
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreTripletTray", folder);
        }

        public static Mesh PodBin(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(TidyProfiles.Bowl(BinRadius, BinHeight, 0.02f), 32, 0);
            builder.AddTorus(new Vector3(0f, BinHeight, 0f), BinRadius - 0.008f, 0.02f, 32, 8, 1);
            builder.AddTorus(new Vector3(0f, BinHeight * 0.45f, 0f), BinRadius * 0.98f, 0.012f, 32, 6, 1);
            return TidyMeshUtility.SaveAsIs(builder, "SM_StorePodBin", folder);
        }

        public static Mesh Counter(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddBeveledBox(new Vector3(0f, 0.46f, 0f), new Vector3(2.4f, 0.84f, 0.62f), 0.02f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.05f, 0.02f), new Vector3(2.36f, 0.1f, 0.6f), 0.01f, 1, 2);
            builder.AddBeveledBox(new Vector3(0f, CounterHeight - 0.03f, 0.02f), new Vector3(2.52f, 0.06f, 0.74f), 0.018f, 2, 1);
            for (var index = 0; index < 5; index++)
            {
                builder.AddBeveledBox(new Vector3(-0.96f + index * 0.48f, 0.5f, 0.315f), new Vector3(0.38f, 0.6f, 0.02f), 0.01f, 1, 1);
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreCounter", folder);
        }

        public static Mesh Register(string folder)
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddBeveledBox(new Vector3(0f, 0.05f, 0f), new Vector3(0.38f, 0.1f, 0.32f), 0.015f, 2, 0);
            builder.AddBeveledBox(Vector3.zero, new Vector3(0.3f, 0.06f, 0.16f), 0.01f, 1, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.12f, 0.04f), new Vector3(18f, 0f, 0f), Vector3.one));
            builder.AddBeveledBox(new Vector3(0f, 0.2f, -0.1f), new Vector3(0.05f, 0.2f, 0.05f), 0.01f, 1, 0);
            builder.AddBeveledBox(Vector3.zero, new Vector3(0.26f, 0.16f, 0.025f), 0.008f, 1, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.34f, -0.09f), new Vector3(-12f, 0f, 0f), Vector3.one));
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    var key = new Vector3(-0.09f + column * 0.06f, 0.155f + row * 0.006f, 0.08f - row * 0.035f);
                    builder.AddBeveledBox(key, new Vector3(0.04f, 0.012f, 0.026f), 0.004f, 1, 2);
                }
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreRegister", folder);
        }

        public static Mesh PendantLamp(string folder, float drop)
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddCylinder(0.006f, drop, 6, 2, Matrix4x4.Translate(new Vector3(0f, -drop, 0f)));
            builder.AddBeveledCylinder(0.05f, 0.03f, 0.008f, 16, 1, 2, Matrix4x4.Translate(new Vector3(0f, -0.03f, 0f)));
            var shade = new[]
            {
                new Vector2(0.03f, 0f), new Vector2(0.06f, -0.01f), new Vector2(0.16f, -0.08f), new Vector2(0.24f, -0.2f), new Vector2(0.26f, -0.25f), new Vector2(0.25f, -0.255f)
            };
            builder.AddLathe(shade, 36, 0, Matrix4x4.Translate(new Vector3(0f, -drop, 0f)), false, false);
            var shadeInner = new[] { new Vector2(0.245f, -0.25f), new Vector2(0.23f, -0.2f), new Vector2(0.15f, -0.085f), new Vector2(0.03f, -0.012f) };
            builder.AddLathe(shadeInner, 36, 1, Matrix4x4.Translate(new Vector3(0f, -drop, 0f)), false, false);
            builder.AddSphere(new Vector3(0f, -drop - 0.13f, 0f), 0.06f, 16, 12, 1);
            return TidyMeshUtility.SaveAsIs(builder, "SM_StorePendant_" + Mathf.RoundToInt(drop * 100f), folder);
        }

        public static Mesh SignBoard(string folder, float width, float height)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBeveledBox(new Vector3(0f, 0f, 0.02f), new Vector3(width, height, 0.04f), 0.02f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0f, 0.043f), new Vector3(width - 0.08f, height - 0.08f, 0.01f), 0.004f, 1, 1);
            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreSign_" + Mathf.RoundToInt(width * 100f) + "x" + Mathf.RoundToInt(height * 100f), folder);
        }

        public static Mesh ToyBoxRow(float length, float maxHeight, int seed, int colours)
        {
            var path = DecorFolder + "/SM_ToyBoxes_" + Mathf.RoundToInt(length * 100f) + "_" + Mathf.RoundToInt(maxHeight * 100f) + "_" + seed + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return existing;
            }

            TidyAssetFactory.EnsureFolder(DecorFolder);
            var random = new System.Random(seed);
            var builder = new TidyMeshBuilder(colours + 2);
            var cursor = 0.02f;
            while (cursor < length - 0.12f)
            {
                var width = Range(random, 0.1f, 0.2f);
                var height = Mathf.Min(maxHeight, Range(random, 0.14f, 0.3f));
                var depth = Range(random, 0.1f, 0.2f);
                if (cursor + width > length - 0.02f)
                {
                    break;
                }

                var colour = random.Next(colours);
                var center = new Vector3(cursor + width * 0.5f, 0f, Range(random, -0.02f, 0.02f));
                var matrix = TidyMeshBuilder.TRS(center, new Vector3(0f, Range(random, -6f, 6f), 0f), Vector3.one);
                builder.AddBeveledBox(new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, depth), 0.006f, 1, colour, matrix);
                builder.AddBox(new Vector3(0f, height * 0.52f, depth * 0.5f + 0.001f), new Vector3(width * 0.74f, height * 0.56f, 0.003f), colours, matrix);
                builder.AddBox(new Vector3(0f, height * 0.12f, depth * 0.5f + 0.001f), new Vector3(width * 0.8f, height * 0.1f, 0.003f), colours + 1, matrix);
                cursor += width + Range(random, 0.008f, 0.03f);
            }

            return TidyMeshAssets.SaveMesh(builder.Build("SM_ToyBoxes_" + seed), path);
        }

        public static Mesh Balloon(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(0.018f, 0.004f), new Vector2(0.012f, 0.02f), new Vector2(0.08f, 0.07f), new Vector2(0.135f, 0.17f),
                new Vector2(0.145f, 0.26f), new Vector2(0.125f, 0.34f), new Vector2(0.075f, 0.39f), new Vector2(0f, 0.405f)
            }, 24, 0);
            builder.AddCylinder(0.002f, 0.9f, 4, 1, Matrix4x4.Translate(new Vector3(0f, -0.9f, 0f)));
            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreBalloon", folder);
        }

        public static Mesh Bunting(string folder, float length, float sag, int colours, out int flagCount)
        {
            var builder = new TidyMeshBuilder(colours + 1);
            flagCount = Mathf.Max(2, Mathf.RoundToInt(length / 0.32f));
            var previous = Vector3.zero;
            for (var index = 0; index <= flagCount; index++)
            {
                var t = index / (float)flagCount;
                var point = new Vector3(t * length, -4f * sag * t * (1f - t), 0f);
                if (index > 0)
                {
                    var delta = point - previous;
                    var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    builder.AddBox(Vector3.zero, new Vector3(delta.magnitude, 0.008f, 0.008f), colours, TidyMeshBuilder.TRS((point + previous) * 0.5f, new Vector3(0f, 0f, angle), Vector3.one));
                    var flag = new[] { new Vector2(-0.11f, 0f), new Vector2(0.11f, 0f), new Vector2(0f, -0.22f) };
                    builder.AddExtrusion(flag, 0.004f, (index - 1) % colours, TidyMeshBuilder.TRS((point + previous) * 0.5f, new Vector3(0f, 0f, angle * 0.6f), Vector3.one));
                }

                previous = point;
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreBunting_" + Mathf.RoundToInt(length * 100f), folder);
        }

        public static Mesh Star(string folder, float radius, float depth)
        {
            var builder = new TidyMeshBuilder();
            var points = new Vector2[10];
            for (var index = 0; index < 10; index++)
            {
                var angle = (index * 36f + 90f) * Mathf.Deg2Rad;
                var r = index % 2 == 0 ? radius : radius * 0.45f;
                points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            }

            builder.AddExtrusion(points, depth, 0, Matrix4x4.Translate(new Vector3(0f, 0f, -depth * 0.5f)), depth * 0.3f);
            return TidyMeshUtility.SaveAsIs(builder, "SM_StoreStar_" + Mathf.RoundToInt(radius * 100f), folder);
        }

        static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
