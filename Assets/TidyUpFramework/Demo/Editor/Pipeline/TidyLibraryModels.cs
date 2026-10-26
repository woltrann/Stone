using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public enum TidyPotionShape
    {
        Round,
        Tall,
        Flat
    }

    public static class TidyLibraryModels
    {
        const int SmoothSegments = 24;
        const int CoarseSegments = 16;

        public static Mesh Potion(TidyPotionShape shape, string folder)
        {
            var builder = new TidyMeshBuilder(3);
            var glass = PotionProfile(shape, 1f, 0f);
            builder.AddLathe(glass, SmoothSegments, 0);
            builder.AddLathe(PotionLiquid(shape), SmoothSegments, 1);
            var neckTop = glass[glass.Length - 1].y;
            var neckRadius = glass[glass.Length - 1].x;
            builder.AddBeveledCylinder(neckRadius * 1.05f, 0.022f, 0.003f, CoarseSegments, 2, 2, Matrix4x4.Translate(new Vector3(0f, neckTop - 0.006f, 0f)));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Potion_" + shape, folder);
        }

        static Vector2[] PotionProfile(TidyPotionShape shape, float scale, float lift)
        {
            Vector2[] points;
            switch (shape)
            {
                case TidyPotionShape.Tall:
                    points = new[]
                    {
                        new Vector2(0.026f, 0f), new Vector2(0.03f, 0.004f), new Vector2(0.031f, 0.012f), new Vector2(0.031f, 0.1f),
                        new Vector2(0.027f, 0.115f), new Vector2(0.014f, 0.128f), new Vector2(0.011f, 0.135f), new Vector2(0.011f, 0.16f), new Vector2(0.013f, 0.164f)
                    };
                    break;
                case TidyPotionShape.Flat:
                    points = new[]
                    {
                        new Vector2(0.045f, 0f), new Vector2(0.052f, 0.006f), new Vector2(0.054f, 0.02f), new Vector2(0.05f, 0.036f),
                        new Vector2(0.032f, 0.05f), new Vector2(0.013f, 0.06f), new Vector2(0.011f, 0.066f), new Vector2(0.011f, 0.084f), new Vector2(0.013f, 0.088f)
                    };
                    break;
                default:
                    points = new[]
                    {
                        new Vector2(0.02f, 0f), new Vector2(0.034f, 0.006f), new Vector2(0.045f, 0.022f), new Vector2(0.048f, 0.042f),
                        new Vector2(0.043f, 0.064f), new Vector2(0.028f, 0.08f), new Vector2(0.013f, 0.088f), new Vector2(0.011f, 0.096f),
                        new Vector2(0.011f, 0.122f), new Vector2(0.014f, 0.126f)
                    };
                    break;
            }

            for (var index = 0; index < points.Length; index++)
            {
                points[index] = new Vector2(points[index].x * scale, points[index].y * scale + lift);
            }

            return points;
        }

        static Vector2[] PotionLiquid(TidyPotionShape shape)
        {
            var outer = PotionProfile(shape, 1f, 0f);
            var fillHeight = shape == TidyPotionShape.Tall ? 0.085f : shape == TidyPotionShape.Flat ? 0.035f : 0.055f;
            var points = new List<Vector2>();
            foreach (var point in outer)
            {
                if (point.y > fillHeight)
                {
                    break;
                }

                points.Add(new Vector2(Mathf.Max(0.004f, point.x - 0.0035f), point.y + 0.003f));
            }

            points.Add(new Vector2(Mathf.Max(0.004f, InterpolateRadius(outer, fillHeight) - 0.0035f), fillHeight));
            return points.ToArray();
        }

        static float InterpolateRadius(Vector2[] profile, float height)
        {
            for (var index = 1; index < profile.Length; index++)
            {
                if (profile[index].y >= height)
                {
                    var t = Mathf.InverseLerp(profile[index - 1].y, profile[index].y, height);
                    return Mathf.Lerp(profile[index - 1].x, profile[index].x, t);
                }
            }

            return profile[profile.Length - 1].x;
        }

        public static Mesh Scroll(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddRolledScroll(0.26f, 0.026f, 0.003f, 0, TidyMeshBuilder.TRS(Vector3.zero, new Vector3(0f, 0f, 90f), Vector3.one), 1);
            builder.AddTorus(Vector3.zero, 0.028f, 0.003f, 20, 6, 1, TidyMeshBuilder.TRS(Vector3.zero, new Vector3(0f, 0f, 90f), Vector3.one));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Scroll", folder);
        }

        public static Mesh Candle(string folder, float height)
        {
            var builder = new TidyMeshBuilder(3);
            builder.AddLathe(TidyProfiles.RoundedCylinder(0.018f, height, 0.004f, 2), CoarseSegments, 0, Matrix4x4.Translate(new Vector3(0f, 0.012f, 0f)));
            builder.AddCylinder(0.0015f, 0.012f, 6, 1, Matrix4x4.Translate(new Vector3(0f, height + 0.012f, 0f)));
            builder.AddLathe(new[]
            {
                new Vector2(0.04f, 0f), new Vector2(0.042f, 0.004f), new Vector2(0.036f, 0.008f), new Vector2(0.022f, 0.01f), new Vector2(0.021f, 0.016f)
            }, SmoothSegments, 2);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Candle_" + Mathf.RoundToInt(height * 100f), folder);
        }

        public static Mesh Flame(string folder)
        {
            var builder = new TidyMeshBuilder();
            builder.AddLathe(new[]
            {
                new Vector2(0.002f, 0f), new Vector2(0.006f, 0.006f), new Vector2(0.007f, 0.012f), new Vector2(0.004f, 0.022f), new Vector2(0.0005f, 0.03f)
            }, 10);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Flame", folder);
        }

        public static Mesh Crystal(string folder, int variant)
        {
            var builder = new TidyMeshBuilder(2);
            var random = new System.Random(variant * 97 + 5);
            builder.AddCrystal(0.022f, 0.08f, 6, 0.03f, 0);
            for (var index = 0; index < 3; index++)
            {
                var angle = index * 120f + (float)random.NextDouble() * 40f;
                var tilt = 22f + (float)random.NextDouble() * 15f;
                var scale = 0.55f + (float)random.NextDouble() * 0.25f;
                builder.AddCrystal(0.016f, 0.06f, 6, 0.022f, 0, TidyMeshBuilder.TRS(Quaternion.Euler(0f, angle, 0f) * new Vector3(0.012f, 0f, 0f), new Vector3(0f, angle, tilt), Vector3.one * scale));
            }

            builder.AddLathe(new[] { new Vector2(0.034f, 0f), new Vector2(0.036f, 0.006f), new Vector2(0.028f, 0.012f), new Vector2(0.02f, 0.014f) }, CoarseSegments, 1);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Crystal_" + variant, folder);
        }

        public static Mesh InkBottle(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(new[]
            {
                new Vector2(0.026f, 0f), new Vector2(0.03f, 0.004f), new Vector2(0.03f, 0.03f), new Vector2(0.024f, 0.038f),
                new Vector2(0.012f, 0.042f), new Vector2(0.012f, 0.05f)
            }, CoarseSegments, 0);
            builder.AddBeveledCylinder(0.014f, 0.012f, 0.002f, 12, 2, 1, Matrix4x4.Translate(new Vector3(0f, 0.048f, 0f)));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_InkBottle", folder);
        }

        public static Mesh Quill(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            var vane = new[]
            {
                new Vector2(0f, 0.04f), new Vector2(0.008f, 0.07f), new Vector2(0.014f, 0.12f), new Vector2(0.012f, 0.17f),
                new Vector2(0.004f, 0.21f), new Vector2(0f, 0.22f), new Vector2(-0.006f, 0.19f), new Vector2(-0.01f, 0.13f), new Vector2(-0.007f, 0.07f)
            };
            builder.AddExtrusion(vane, 0.002f, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0f, -0.001f), Vector3.zero, Vector3.one));
            builder.AddCylinder(0.0015f, 0.22f, 6, 1);
            var mesh = builder.Build("SM_Quill");
            var rotated = new TidyMeshBuilder(2);
            rotated.AddMesh(mesh, TidyMeshBuilder.TRS(Vector3.zero, new Vector3(0f, 0f, -90f), Vector3.one));
            Object.DestroyImmediate(mesh);
            return TidyMeshUtility.SaveBottomCentered(rotated, "SM_Quill", folder);
        }

        public static Mesh Bookcase(string folder, float width, float height, float depth, int shelves, out float[] shelfHeights)
        {
            var builder = new TidyMeshBuilder();
            const float side = 0.05f;
            const float board = 0.03f;
            const float plinth = 0.1f;
            const float crown = 0.09f;
            builder.AddBeveledBox(new Vector3(-width * 0.5f + side * 0.5f, height * 0.5f, 0f), new Vector3(side, height, depth), 0.006f, 2);
            builder.AddBeveledBox(new Vector3(width * 0.5f - side * 0.5f, height * 0.5f, 0f), new Vector3(side, height, depth), 0.006f, 2);
            builder.AddBox(new Vector3(0f, height * 0.5f, -depth * 0.5f + 0.01f), new Vector3(width - side, height - 0.02f, 0.02f));
            builder.AddBeveledBox(new Vector3(0f, plinth * 0.5f, 0.01f), new Vector3(width + 0.02f, plinth, depth + 0.02f), 0.01f, 2);
            builder.AddBeveledBox(new Vector3(0f, height - crown * 0.5f, 0.015f), new Vector3(width + 0.08f, crown, depth + 0.05f), 0.012f, 2);
            builder.AddBeveledBox(new Vector3(0f, height - crown - 0.015f, 0.01f), new Vector3(width + 0.03f, 0.03f, depth + 0.02f), 0.008f, 2);
            shelfHeights = BookcaseShelfHeights(height, shelves);
            foreach (var surface in shelfHeights)
            {
                builder.AddBeveledBox(new Vector3(0f, surface - board * 0.5f, 0.005f), new Vector3(width - side * 2f + 0.002f, board, depth - 0.03f), 0.006f, 2);
            }

            builder.AddBeveledBox(new Vector3(0f, height - crown - 0.03f - board * 0.5f, 0.005f), new Vector3(width - side * 2f + 0.002f, board, depth - 0.03f), 0.006f, 2);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Bookcase_" + Mathf.RoundToInt(width * 100f) + "x" + Mathf.RoundToInt(height * 100f), folder);
        }

        public static float[] BookcaseShelfHeights(float height, int shelves)
        {
            const float board = 0.03f;
            const float plinth = 0.1f;
            const float crown = 0.09f;
            var heights = new float[shelves];
            var usable = height - plinth - crown - 0.02f;
            for (var index = 0; index < shelves; index++)
            {
                heights[index] = plinth + usable * index / shelves + board;
            }

            return heights;
        }

        public static Vector3[] CandelabraCandlePoints(int arms)
        {
            var points = new Vector3[arms + 1];
            points[0] = new Vector3(0f, 0.27f, 0f);
            for (var index = 0; index < arms; index++)
            {
                var direction = Quaternion.Euler(0f, index * 360f / arms, 0f) * Vector3.right;
                points[index + 1] = new Vector3(0f, 0.2f, 0f) + direction * 0.14f + new Vector3(0f, 0.024f, 0f);
            }

            return points;
        }

        public static Vector3[] PotionRackSlotPoints(int columns, int rows, float cell, float rowHeight)
        {
            var slots = new Vector3[columns * rows];
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    slots[row * columns + column] = new Vector3((column - (columns - 1) * 0.5f) * cell, 0.044f + row * rowHeight, 0.01f);
                }
            }

            return slots;
        }

        public static Vector3[] ChandelierCandlePoints(int candles, float radius)
        {
            var points = new Vector3[candles];
            for (var index = 0; index < candles; index++)
            {
                var angle = index * Mathf.PI * 2f / candles;
                points[index] = new Vector3(Mathf.Cos(angle) * radius, -1.338f, Mathf.Sin(angle) * radius);
            }

            return points;
        }

        public static Mesh ReadingTable(string folder, float width, float depth, float height)
        {
            var builder = new TidyMeshBuilder();
            const float top = 0.06f;
            builder.AddBeveledBox(new Vector3(0f, height - top * 0.5f, 0f), new Vector3(width, top, depth), 0.015f, 2);
            builder.AddBeveledBox(new Vector3(0f, height - top - 0.05f, 0f), new Vector3(width - 0.2f, 0.1f, depth - 0.2f), 0.01f, 1);
            var legProfile = new[]
            {
                new Vector2(0.05f, 0f), new Vector2(0.055f, 0.03f), new Vector2(0.04f, 0.06f), new Vector2(0.035f, 0.2f), new Vector2(0.05f, 0.24f),
                new Vector2(0.035f, 0.28f), new Vector2(0.032f, height - top - 0.12f), new Vector2(0.045f, height - top - 0.1f), new Vector2(0.045f, height - top)
            };
            foreach (var corner in new[] { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f) })
            {
                builder.AddLathe(legProfile, 12, 0, Matrix4x4.Translate(new Vector3(corner.x * (width * 0.5f - 0.12f), 0f, corner.y * (depth * 0.5f - 0.12f))));
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Table_" + Mathf.RoundToInt(width * 100f), folder);
        }

        public static Mesh Chair(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBeveledBox(new Vector3(0f, 0.46f, 0f), new Vector3(0.46f, 0.05f, 0.44f), 0.012f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.52f, 0.01f), new Vector3(0.4f, 0.06f, 0.38f), 0.025f, 3, 1);
            foreach (var corner in new[] { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f) })
            {
                var height = corner.y < 0f ? 1.02f : 0.44f;
                builder.AddBeveledBox(new Vector3(corner.x * 0.19f, height * 0.5f, corner.y * 0.18f), new Vector3(0.04f, height, 0.04f), 0.008f, 1, 0);
            }

            builder.AddBeveledBox(new Vector3(0f, 0.86f, -0.18f), new Vector3(0.38f, 0.22f, 0.03f), 0.01f, 2, 0);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Chair", folder);
        }

        public static Mesh Candelabra(string folder, int arms, out Vector3[] candlePoints)
        {
            var builder = new TidyMeshBuilder();
            builder.AddLathe(new[]
            {
                new Vector2(0.07f, 0f), new Vector2(0.075f, 0.01f), new Vector2(0.05f, 0.02f), new Vector2(0.015f, 0.04f), new Vector2(0.012f, 0.16f),
                new Vector2(0.025f, 0.18f), new Vector2(0.012f, 0.2f), new Vector2(0.012f, 0.24f)
            }, SmoothSegments);
            candlePoints = new Vector3[arms + 1];
            candlePoints[0] = new Vector3(0f, 0.27f, 0f);
            builder.AddLathe(new[] { new Vector2(0.03f, 0f), new Vector2(0.032f, 0.01f), new Vector2(0.02f, 0.02f), new Vector2(0.02f, 0.03f) }, CoarseSegments, 0, Matrix4x4.Translate(new Vector3(0f, 0.24f, 0f)));
            for (var index = 0; index < arms; index++)
            {
                var angle = index * 360f / arms;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.right;
                builder.AddTorus(Vector3.zero, 0.07f, 0.007f, 16, 6, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.2f, 0f) + direction * 0.07f, new Vector3(90f, angle, 0f), new Vector3(1f, 1f, 1f)));
                var cup = new Vector3(0f, 0.2f, 0f) + direction * 0.14f;
                builder.AddLathe(new[] { new Vector2(0.024f, 0f), new Vector2(0.026f, 0.008f), new Vector2(0.018f, 0.016f), new Vector2(0.018f, 0.024f) }, CoarseSegments, 0, Matrix4x4.Translate(cup));
                candlePoints[index + 1] = cup + new Vector3(0f, 0.024f, 0f);
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Candelabra_" + arms, folder);
        }

        public static Mesh PotionRack(string folder, int columns, int rows, float cell, float rowHeight, out Vector3[] slots)
        {
            var builder = new TidyMeshBuilder();
            var width = columns * cell + 0.06f;
            var height = rows * rowHeight + 0.08f;
            const float depth = 0.16f;
            builder.AddBeveledBox(new Vector3(-width * 0.5f + 0.02f, height * 0.5f, 0f), new Vector3(0.04f, height, depth), 0.006f, 2);
            builder.AddBeveledBox(new Vector3(width * 0.5f - 0.02f, height * 0.5f, 0f), new Vector3(0.04f, height, depth), 0.006f, 2);
            builder.AddBox(new Vector3(0f, height * 0.5f, -depth * 0.5f + 0.008f), new Vector3(width - 0.04f, height, 0.016f));
            slots = new Vector3[columns * rows];
            for (var row = 0; row < rows; row++)
            {
                var y = 0.02f + row * rowHeight;
                builder.AddBeveledBox(new Vector3(0f, y + 0.012f, 0f), new Vector3(width - 0.06f, 0.024f, depth - 0.01f), 0.005f, 1);
                builder.AddBeveledBox(new Vector3(0f, y + 0.05f, depth * 0.5f - 0.01f), new Vector3(width - 0.06f, 0.016f, 0.012f), 0.004f, 1);
                for (var column = 0; column < columns; column++)
                {
                    slots[row * columns + column] = new Vector3((column - (columns - 1) * 0.5f) * cell, y + 0.024f, 0.01f);
                }
            }

            builder.AddBeveledBox(new Vector3(0f, height + 0.015f, 0f), new Vector3(width + 0.03f, 0.03f, depth + 0.02f), 0.008f, 2);
            return TidyMeshUtility.SaveAsIs(builder, "SM_PotionRack_" + columns + "x" + rows, folder);
        }

        public static Mesh Basket(string folder, float radius, float height)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(TidyProfiles.Bowl(radius, height, 0.012f), SmoothSegments, 0);
            builder.AddTorus(new Vector3(0f, height, 0f), radius + 0.002f, 0.012f, SmoothSegments, 8, 1);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Basket_" + Mathf.RoundToInt(radius * 100f), folder);
        }

        public static Mesh Pedestal(string folder, float topSize, float height)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBeveledBox(new Vector3(0f, 0.05f, 0f), new Vector3(topSize + 0.1f, 0.1f, topSize + 0.1f), 0.015f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, height * 0.5f, 0f), new Vector3(topSize - 0.08f, height - 0.15f, topSize - 0.08f), 0.01f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, height - 0.03f, 0f), new Vector3(topSize, 0.06f, topSize), 0.012f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, height + 0.004f, 0f), new Vector3(topSize - 0.05f, 0.008f, topSize - 0.05f), 0.003f, 1, 1);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Pedestal_" + Mathf.RoundToInt(height * 100f), folder);
        }

        public static Mesh Column(string folder, float height)
        {
            var builder = new TidyMeshBuilder();
            var profile = new List<Vector2>
            {
                new Vector2(0.3f, 0f), new Vector2(0.3f, 0.12f), new Vector2(0.26f, 0.16f), new Vector2(0.24f, 0.22f), new Vector2(0.2f, 0.26f),
                new Vector2(0.19f, height - 0.34f), new Vector2(0.22f, height - 0.3f), new Vector2(0.21f, height - 0.26f), new Vector2(0.26f, height - 0.2f),
                new Vector2(0.3f, height - 0.12f), new Vector2(0.3f, height)
            };
            builder.AddLathe(profile.ToArray(), 28);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Column_" + Mathf.RoundToInt(height * 100f), folder);
        }

        public static Mesh ArchedWindow(string folder, float width, float height, out Mesh glass)
        {
            var frame = new TidyMeshBuilder();
            const float post = 0.09f;
            const int arcPieces = 18;
            var half = width * 0.5f;
            var springLine = height - half;
            frame.AddBeveledBox(new Vector3(-half + post * 0.5f, springLine * 0.5f, 0.06f), new Vector3(post, springLine, 0.14f), 0.01f, 2);
            frame.AddBeveledBox(new Vector3(half - post * 0.5f, springLine * 0.5f, 0.06f), new Vector3(post, springLine, 0.14f), 0.01f, 2);
            frame.AddBeveledBox(new Vector3(0f, -0.03f, 0.08f), new Vector3(width + 0.12f, 0.06f, 0.2f), 0.01f, 2);
            var arcRadius = half - post * 0.5f;
            var pieceLength = Mathf.PI * arcRadius / arcPieces * 1.08f;
            for (var index = 0; index < arcPieces; index++)
            {
                var angle = Mathf.PI * (index + 0.5f) / arcPieces;
                var center = new Vector3(Mathf.Cos(angle) * arcRadius, springLine + Mathf.Sin(angle) * arcRadius, 0.06f);
                frame.AddBox(Vector3.zero, new Vector3(pieceLength, post, 0.14f), 0, TidyMeshBuilder.TRS(center, new Vector3(0f, 0f, angle * Mathf.Rad2Deg - 90f), Vector3.one));
            }

            frame.AddBox(new Vector3(0f, (springLine + arcRadius) * 0.5f, 0.06f), new Vector3(0.04f, springLine + arcRadius, 0.05f));
            frame.AddBox(new Vector3(0f, springLine * 0.55f, 0.06f), new Vector3(width - post * 2f, 0.04f, 0.05f));
            var glassBuilder = new TidyMeshBuilder();
            glassBuilder.AddExtrusion(ArchOutline(width - 0.12f, height - 0.06f, 20), 0.01f, 0, Matrix4x4.Translate(new Vector3(0f, 0.03f, 0.05f)));
            glass = TidyMeshUtility.SaveAsIs(glassBuilder, "SM_WindowGlass_" + Mathf.RoundToInt(width * 100f), folder);
            return TidyMeshUtility.SaveAsIs(frame, "SM_WindowFrame_" + Mathf.RoundToInt(width * 100f), folder);
        }

        static Vector2[] ArchOutline(float width, float height, int arcSegments)
        {
            var half = width * 0.5f;
            var springLine = height - half;
            var points = new List<Vector2> { new Vector2(-half, 0f), new Vector2(half, 0f), new Vector2(half, springLine) };
            for (var index = 1; index < arcSegments; index++)
            {
                var angle = Mathf.PI * index / arcSegments;
                points.Add(new Vector2(Mathf.Cos(angle) * half, springLine + Mathf.Sin(angle) * half));
            }

            points.Add(new Vector2(-half, springLine));
            return points.ToArray();
        }

        public static Mesh Chandelier(string folder, int candles, float radius, out Vector3[] candlePoints)
        {
            var builder = new TidyMeshBuilder();
            builder.AddCylinder(0.01f, 1.2f, 8, 0, Matrix4x4.Translate(new Vector3(0f, -1.2f, 0f)));
            builder.AddLathe(new[] { new Vector2(0.02f, -1.45f), new Vector2(0.06f, -1.4f), new Vector2(0.08f, -1.3f), new Vector2(0.05f, -1.22f), new Vector2(0.015f, -1.2f) }, SmoothSegments);
            builder.AddTorus(new Vector3(0f, -1.36f, 0f), radius, 0.014f, 48, 8);
            builder.AddTorus(new Vector3(0f, -1.3f, 0f), radius * 0.55f, 0.01f, 32, 6);
            candlePoints = new Vector3[candles];
            for (var index = 0; index < candles; index++)
            {
                var angle = index * Mathf.PI * 2f / candles;
                var point = new Vector3(Mathf.Cos(angle) * radius, -1.36f, Mathf.Sin(angle) * radius);
                builder.AddLathe(new[] { new Vector2(0.03f, 0f), new Vector2(0.032f, 0.008f), new Vector2(0.02f, 0.016f), new Vector2(0.02f, 0.022f) }, 12, 0, Matrix4x4.Translate(point));
                builder.AddCylinder(0.006f, radius * 0.9f, 6, 0, TidyMeshBuilder.TRS(new Vector3(0f, -1.36f, 0f), new Vector3(0f, -angle * Mathf.Rad2Deg, -90f), Vector3.one));
                candlePoints[index] = point + new Vector3(0f, 0.022f, 0f);
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Chandelier_" + candles, folder);
        }

        public static Mesh Lectern(string folder)
        {
            var builder = new TidyMeshBuilder();
            builder.AddBeveledBox(new Vector3(0f, 0.04f, 0f), new Vector3(0.5f, 0.08f, 0.42f), 0.012f, 2);
            builder.AddLathe(new[] { new Vector2(0.07f, 0.08f), new Vector2(0.05f, 0.14f), new Vector2(0.045f, 0.9f), new Vector2(0.06f, 0.95f), new Vector2(0.04f, 1f) }, 16);
            builder.AddBeveledBox(Vector3.zero, new Vector3(0.52f, 0.04f, 0.38f), 0.01f, 2, 0, TidyMeshBuilder.TRS(new Vector3(0f, 1.05f, 0f), new Vector3(-22f, 0f, 0f), Vector3.one));
            builder.AddBeveledBox(Vector3.zero, new Vector3(0.52f, 0.05f, 0.03f), 0.008f, 1, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.99f, 0.18f), new Vector3(-22f, 0f, 0f), Vector3.one));
            return TidyMeshUtility.SaveAsIs(builder, "SM_Lectern", folder);
        }

        public static Mesh Globe(string folder)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddSphere(new Vector3(0f, 0.36f, 0f), 0.16f, 24, 16, 1);
            builder.AddTorus(Vector3.zero, 0.18f, 0.008f, 32, 6, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.36f, 0f), new Vector3(0f, 0f, 70f), Vector3.one));
            builder.AddLathe(new[] { new Vector2(0.12f, 0f), new Vector2(0.12f, 0.02f), new Vector2(0.05f, 0.04f), new Vector2(0.025f, 0.08f), new Vector2(0.02f, 0.18f) }, 20);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Globe", folder);
        }
    }
}
