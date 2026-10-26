using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public enum TidyPalSpecies
    {
        Bear,
        Bunny,
        Cat,
        Frog,
        Robot
    }

    public static class TidyStoreModels
    {
        public const int BodySubMesh = 0;
        public const int BellySubMesh = 1;
        public const int DarkSubMesh = 2;
        public const int AccentSubMesh = 3;
        const int Longitude = 18;
        const int Latitude = 12;

        public static Mesh Pal(TidyPalSpecies species, string folder)
        {
            var builder = new TidyMeshBuilder(4);
            switch (species)
            {
                case TidyPalSpecies.Robot:
                    BuildRobot(builder);
                    break;
                case TidyPalSpecies.Frog:
                    BuildSoftBody(builder, 0.013f);
                    BuildFrogHead(builder);
                    break;
                default:
                    BuildSoftBody(builder, 0.011f);
                    BuildSoftHead(builder);
                    BuildEars(builder, species);
                    break;
            }

            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Pal_" + species, folder);
        }

        static void Ellipsoid(TidyMeshBuilder builder, Vector3 center, Vector3 radii, int subMesh, Vector3 euler = default)
        {
            builder.AddSphere(Vector3.zero, 1f, Longitude, Latitude, subMesh, TidyMeshBuilder.TRS(center, euler, radii));
        }

        static void SmallSphere(TidyMeshBuilder builder, Vector3 center, Vector3 radii, int subMesh, Vector3 euler = default)
        {
            builder.AddSphere(Vector3.zero, 1f, 10, 7, subMesh, TidyMeshBuilder.TRS(center, euler, radii));
        }

        static void BuildSoftBody(TidyMeshBuilder builder, float footRadius)
        {
            Ellipsoid(builder, new Vector3(0f, 0.03f, 0f), new Vector3(0.031f, 0.031f, 0.028f), BodySubMesh);
            Ellipsoid(builder, new Vector3(0f, 0.027f, 0.014f), new Vector3(0.02f, 0.021f, 0.017f), BellySubMesh);
            foreach (var side in new[] { -1f, 1f })
            {
                Ellipsoid(builder, new Vector3(side * 0.014f, 0.006f, 0.016f), new Vector3(footRadius, 0.007f, footRadius * 1.2f), BodySubMesh);
                Ellipsoid(builder, new Vector3(side * 0.028f, 0.034f, 0.006f), new Vector3(0.009f, 0.014f, 0.009f), BodySubMesh, new Vector3(0f, 0f, side * 25f));
            }
        }

        static void BuildSoftHead(TidyMeshBuilder builder)
        {
            Ellipsoid(builder, new Vector3(0f, 0.075f, 0.002f), new Vector3(0.03f, 0.027f, 0.027f), BodySubMesh);
            Ellipsoid(builder, new Vector3(0f, 0.067f, 0.024f), new Vector3(0.013f, 0.009f, 0.007f), BellySubMesh);
            SmallSphere(builder, new Vector3(0f, 0.071f, 0.0305f), new Vector3(0.0042f, 0.003f, 0.0026f), DarkSubMesh);
            foreach (var side in new[] { -1f, 1f })
            {
                Eye(builder, new Vector3(side * 0.011f, 0.079f, 0.0262f), 0.0045f);
                SmallSphere(builder, new Vector3(side * 0.019f, 0.068f, 0.0228f), new Vector3(0.0048f, 0.003f, 0.0018f), AccentSubMesh, new Vector3(0f, side * 38f, 0f));
            }
        }

        static void Eye(TidyMeshBuilder builder, Vector3 center, float radius)
        {
            SmallSphere(builder, center, new Vector3(radius, radius * 1.15f, radius * 0.6f), DarkSubMesh);
            SmallSphere(builder, center + new Vector3(radius * 0.35f, radius * 0.45f, radius * 0.45f), Vector3.one * radius * 0.32f, BellySubMesh);
        }

        static void BuildEars(TidyMeshBuilder builder, TidyPalSpecies species)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                switch (species)
                {
                    case TidyPalSpecies.Bear:
                        Ellipsoid(builder, new Vector3(side * 0.02f, 0.097f, 0f), new Vector3(0.0095f, 0.0095f, 0.007f), BodySubMesh);
                        SmallSphere(builder, new Vector3(side * 0.02f, 0.097f, 0.0045f), new Vector3(0.0055f, 0.0055f, 0.003f), BellySubMesh);
                        break;
                    case TidyPalSpecies.Bunny:
                        Ellipsoid(builder, new Vector3(side * 0.011f, 0.115f, -0.002f), new Vector3(0.0078f, 0.025f, 0.0055f), BodySubMesh, new Vector3(0f, 0f, -side * 12f));
                        SmallSphere(builder, new Vector3(side * 0.0112f, 0.1155f, 0.0016f), new Vector3(0.0042f, 0.018f, 0.0025f), AccentSubMesh, new Vector3(0f, 0f, -side * 12f));
                        break;
                    case TidyPalSpecies.Cat:
                        CatEar(builder, side);
                        CatWhiskers(builder, side);
                        break;
                }
            }

            if (species == TidyPalSpecies.Bunny)
            {
                SmallSphere(builder, new Vector3(0f, 0.02f, -0.029f), Vector3.one * 0.0085f, BellySubMesh);
            }

            if (species == TidyPalSpecies.Cat)
            {
                CatTail(builder);
            }
        }

        static void CatEar(TidyMeshBuilder builder, float side)
        {
            var outer = new[] { new Vector2(0f, 0f), new Vector2(0.011f, 0f), new Vector2(0.0006f, 0.02f), new Vector2(0f, 0.02f) };
            var inner = new[] { new Vector2(0f, 0f), new Vector2(0.0065f, 0f), new Vector2(0.0004f, 0.013f), new Vector2(0f, 0.013f) };
            builder.AddLathe(outer, 8, BodySubMesh, TidyMeshBuilder.TRS(new Vector3(side * 0.017f, 0.089f, 0f), new Vector3(0f, 0f, -side * 22f), new Vector3(1f, 1f, 0.6f)));
            builder.AddLathe(inner, 8, AccentSubMesh, TidyMeshBuilder.TRS(new Vector3(side * 0.0172f, 0.0905f, 0.0045f), new Vector3(0f, 0f, -side * 22f), new Vector3(1f, 1f, 0.4f)));
        }

        static void CatWhiskers(TidyMeshBuilder builder, float side)
        {
            for (var index = 0; index < 2; index++)
            {
                var tilt = (index == 0 ? 8f : -8f) * side;
                builder.AddBox(Vector3.zero, new Vector3(0.014f, 0.0009f, 0.0009f), DarkSubMesh, TidyMeshBuilder.TRS(new Vector3(side * 0.019f, 0.0675f - index * 0.003f, 0.026f), new Vector3(0f, -side * 20f, tilt), Vector3.one));
            }
        }

        static void CatTail(TidyMeshBuilder builder)
        {
            for (var index = 0; index < 6; index++)
            {
                var t = index / 5f;
                var position = new Vector3(0.008f * Mathf.Sin(t * 2.4f), 0.012f + t * 0.04f, -0.028f - 0.01f * Mathf.Sin(t * Mathf.PI));
                SmallSphere(builder, position, Vector3.one * Mathf.Lerp(0.0062f, 0.0048f, t), BodySubMesh);
            }
        }

        static void BuildFrogHead(TidyMeshBuilder builder)
        {
            Ellipsoid(builder, new Vector3(0f, 0.071f, 0.002f), new Vector3(0.035f, 0.024f, 0.027f), BodySubMesh);
            SmallSphere(builder, new Vector3(0f, 0.0625f, 0.0262f), new Vector3(0.013f, 0.0014f, 0.0022f), DarkSubMesh);
            foreach (var side in new[] { -1f, 1f })
            {
                Ellipsoid(builder, new Vector3(side * 0.015f, 0.09f, 0.007f), Vector3.one * 0.0105f, BodySubMesh);
                SmallSphere(builder, new Vector3(side * 0.015f, 0.092f, 0.0138f), new Vector3(0.0072f, 0.0072f, 0.0045f), BellySubMesh);
                Eye(builder, new Vector3(side * 0.015f, 0.0925f, 0.0178f), 0.0042f);
                SmallSphere(builder, new Vector3(side * 0.023f, 0.067f, 0.0215f), new Vector3(0.005f, 0.003f, 0.0018f), AccentSubMesh, new Vector3(0f, side * 42f, 0f));
            }
        }

        static void BuildRobot(TidyMeshBuilder builder)
        {
            builder.AddBeveledBox(new Vector3(0f, 0.031f, 0f), new Vector3(0.052f, 0.044f, 0.042f), 0.009f, 2, BodySubMesh);
            builder.AddBeveledBox(new Vector3(0f, 0.032f, 0.0205f), new Vector3(0.03f, 0.022f, 0.004f), 0.0018f, 1, BellySubMesh);
            for (var index = 0; index < 3; index++)
            {
                SmallSphere(builder, new Vector3((index - 1) * 0.0085f, 0.032f, 0.0232f), new Vector3(0.0028f, 0.0028f, 0.0016f), AccentSubMesh);
            }

            builder.AddBeveledBox(new Vector3(0f, 0.078f, 0f), new Vector3(0.058f, 0.044f, 0.046f), 0.01f, 2, BodySubMesh);
            builder.AddBeveledBox(new Vector3(0f, 0.078f, 0.0215f), new Vector3(0.044f, 0.028f, 0.005f), 0.003f, 1, DarkSubMesh);
            foreach (var side in new[] { -1f, 1f })
            {
                SmallSphere(builder, new Vector3(side * 0.0105f, 0.08f, 0.0243f), new Vector3(0.0048f, 0.0058f, 0.0014f), BellySubMesh);
                builder.AddBeveledCylinder(0.0075f, 0.006f, 0.0015f, 12, 1, AccentSubMesh, TidyMeshBuilder.TRS(new Vector3(side * 0.0285f, 0.078f, 0f), new Vector3(0f, 0f, -side * 90f), Vector3.one));
                builder.AddBeveledBox(new Vector3(side * 0.031f, 0.029f, 0f), new Vector3(0.011f, 0.03f, 0.017f), 0.0045f, 1, BodySubMesh);
                builder.AddBeveledBox(new Vector3(side * 0.013f, 0.0045f, 0.003f), new Vector3(0.019f, 0.009f, 0.03f), 0.003f, 1, DarkSubMesh);
            }

            builder.AddBox(new Vector3(0f, 0.0705f, 0.0242f), new Vector3(0.012f, 0.0018f, 0.0012f), BellySubMesh);
            builder.AddCylinder(0.0018f, 0.016f, 8, DarkSubMesh, Matrix4x4.Translate(new Vector3(0f, 0.099f, 0f)));
            SmallSphere(builder, new Vector3(0f, 0.117f, 0f), Vector3.one * 0.0052f, AccentSubMesh);
        }

        public static Mesh Pod(string folder)
        {
            var builder = new TidyMeshBuilder(4);
            var lower = new List<Vector2>();
            TidyProfiles.AppendArc(lower, new Vector2(0f, 0.03f), 0.03f, -90f, 0f, 8);
            TidyProfiles.Append(lower, new Vector2(0.03f, 0.036f));
            builder.AddLathe(lower.ToArray(), 22, 0);
            var upper = new List<Vector2> { new Vector2(0.0302f, 0.036f), new Vector2(0.0302f, 0.042f) };
            TidyProfiles.AppendArc(upper, new Vector2(0f, 0.042f), 0.0302f, 0f, 90f, 8);
            builder.AddLathe(upper.ToArray(), 22, 1, null, false, false);
            builder.AddLathe(new[] { new Vector2(0.0308f, 0.018f), new Vector2(0.0308f, 0.035f) }, 22, 2, null, false, false);
            builder.AddTorus(new Vector3(0f, 0.036f, 0f), 0.0305f, 0.0018f, 22, 6, 0);
            builder.AddSphere(new Vector3(0f, 0.052f, 0f), 0.016f, 14, 10, 3);
            builder.AddSphere(new Vector3(-0.009f, 0.066f, 0f), 0.006f, 10, 7, 3);
            builder.AddSphere(new Vector3(0.009f, 0.066f, 0f), 0.006f, 10, 7, 3);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_PalPod", folder);
        }
    }
}
