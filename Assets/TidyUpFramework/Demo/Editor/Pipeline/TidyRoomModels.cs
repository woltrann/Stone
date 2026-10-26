using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyRoomModels
    {
        public const string Folder = TidyDemoPaths.Meshes + "/Room";
        const int Smooth = 24;
        const int Coarse = 14;

        public static Mesh FoldedShirt()
        {
            var builder = new TidyMeshBuilder(2) { UvScale = new Vector2(5f, 5f) };
            builder.AddBeveledBox(new Vector3(0f, 0.016f, 0f), new Vector3(0.28f, 0.032f, 0.22f), 0.012f, 3, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.036f, -0.035f), new Vector3(0.27f, 0.012f, 0.15f), 0.005f, 2, 0);
            builder.AddBeveledBox(new Vector3(-0.11f, 0.044f, 0.02f), new Vector3(0.05f, 0.008f, 0.12f), 0.003f, 1, 0, TidyMeshBuilder.TRS(Vector3.zero, new Vector3(0f, -8f, 0f), Vector3.one));
            builder.AddBeveledBox(new Vector3(0.11f, 0.044f, 0.02f), new Vector3(0.05f, 0.008f, 0.12f), 0.003f, 1, 0, TidyMeshBuilder.TRS(Vector3.zero, new Vector3(0f, 8f, 0f), Vector3.one));
            builder.AddTorus(Vector3.zero, 0.04f, 0.007f, 20, 6, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.04f, 0.085f), Vector3.zero, new Vector3(1f, 0.6f, 0.55f)));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_FoldedShirt", Folder);
        }

        public static Mesh FoldedSweater()
        {
            var builder = new TidyMeshBuilder(2) { UvScale = new Vector2(5f, 5f) };
            builder.AddBeveledBox(new Vector3(0f, 0.028f, 0f), new Vector3(0.3f, 0.056f, 0.24f), 0.02f, 3, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.062f, -0.03f), new Vector3(0.29f, 0.016f, 0.17f), 0.007f, 2, 0);
            for (var rib = 0; rib < 9; rib++)
            {
                builder.AddBeveledBox(new Vector3(-0.12f + rib * 0.03f, 0.03f, 0.118f), new Vector3(0.018f, 0.05f, 0.012f), 0.005f, 1, 1);
            }

            builder.AddTorus(Vector3.zero, 0.045f, 0.01f, 20, 6, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.07f, 0.09f), Vector3.zero, new Vector3(1f, 0.6f, 0.5f)));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_FoldedSweater", Folder);
        }

        public static Mesh SockBall()
        {
            var builder = new TidyMeshBuilder(2) { UvScale = new Vector2(6f, 6f) };
            builder.AddSphere(Vector3.zero, 0.045f, 18, 12, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.035f, 0f), Vector3.zero, new Vector3(1.25f, 0.78f, 1f)));
            builder.AddTorus(Vector3.zero, 0.038f, 0.012f, 20, 8, 1, TidyMeshBuilder.TRS(new Vector3(0.012f, 0.036f, 0f), new Vector3(0f, 0f, 90f), new Vector3(1f, 1f, 0.82f)));
            builder.AddSphere(Vector3.zero, 0.02f, 10, 8, 0, TidyMeshBuilder.TRS(new Vector3(-0.045f, 0.03f, 0.012f), Vector3.zero, new Vector3(1.2f, 0.8f, 1f)));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_SockBall", Folder);
        }

        public static Mesh SneakerPair()
        {
            var single = new TidyMeshBuilder(3);
            AddSneaker(single);
            var shoe = single.Build("SM_Room_SneakerSingle");
            var pair = new TidyMeshBuilder(3);
            pair.AddMesh(shoe, TidyMeshBuilder.TRS(new Vector3(-0.062f, 0f, 0f), new Vector3(0f, -4f, 0f), Vector3.one));
            pair.AddMesh(shoe, TidyMeshBuilder.TRS(new Vector3(0.062f, 0f, 0.01f), new Vector3(0f, 4f, 0f), new Vector3(-1f, 1f, 1f)));
            Object.DestroyImmediate(shoe);
            return TidyMeshUtility.SaveBottomCentered(pair, "SM_Room_SneakerPair", Folder);
        }

        static void AddSneaker(TidyMeshBuilder builder)
        {
            var sole = new List<Vector2>();
            for (var index = 0; index < 28; index++)
            {
                var angle = index / 28f * Mathf.PI * 2f;
                var along = Mathf.Sin(angle);
                var width = along > 0f ? 0.05f : 0.042f;
                sole.Add(new Vector2(Mathf.Cos(angle) * width * (1f + along * 0.08f), along * 0.13f));
            }

            builder.AddExtrusion(sole.ToArray(), 0.022f, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.022f, 0f), new Vector3(90f, 0f, 0f), Vector3.one), 0.006f);
            var profile = new[]
            {
                new Vector2(-0.125f, 0.018f), new Vector2(0.126f, 0.018f), new Vector2(0.132f, 0.032f), new Vector2(0.124f, 0.052f),
                new Vector2(0.095f, 0.068f), new Vector2(0.05f, 0.078f), new Vector2(0.0f, 0.094f), new Vector2(-0.045f, 0.118f),
                new Vector2(-0.075f, 0.11f), new Vector2(-0.112f, 0.122f), new Vector2(-0.132f, 0.1f), new Vector2(-0.134f, 0.04f)
            };
            builder.AddExtrusion(profile, 0.084f, 0, TidyMeshBuilder.TRS(new Vector3(0.042f, 0f, 0f), new Vector3(0f, -90f, 0f), Vector3.one), 0.016f);
            builder.AddBeveledBox(Vector3.zero, new Vector3(0.088f, 0.02f, 0.05f), 0.008f, 2, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.04f, 0.104f), new Vector3(-18f, 0f, 0f), Vector3.one));
            for (var lace = 0; lace < 4; lace++)
            {
                builder.AddBeveledBox(Vector3.zero, new Vector3(0.05f, 0.006f, 0.008f), 0.0025f, 1, 2, TidyMeshBuilder.TRS(new Vector3(0f, 0.083f + lace * 0.008f, 0.04f - lace * 0.02f), new Vector3(-24f, 0f, 0f), Vector3.one));
            }

            builder.AddBeveledBox(Vector3.zero, new Vector3(0.088f, 0.012f, 0.006f), 0.003f, 1, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.06f, 0.0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f)));
        }

        public static Mesh Boots()
        {
            var builder = new TidyMeshBuilder(3);
            foreach (var side in new[] { -1f, 1f })
            {
                var offset = new Vector3(side * 0.06f, 0f, 0f);
                builder.AddBeveledBox(offset + new Vector3(0f, 0.012f, 0.02f), new Vector3(0.09f, 0.024f, 0.25f), 0.008f, 2, 1);
                builder.AddBeveledBox(offset + new Vector3(0f, 0.06f, 0.045f), new Vector3(0.086f, 0.08f, 0.17f), 0.035f, 3, 0);
                builder.AddBeveledBox(offset + new Vector3(0f, 0.13f, -0.045f), new Vector3(0.082f, 0.23f, 0.1f), 0.03f, 3, 0);
                builder.AddTorus(Vector3.zero, 0.046f, 0.012f, 18, 6, 2, TidyMeshBuilder.TRS(offset + new Vector3(0f, 0.24f, -0.045f), Vector3.zero, new Vector3(0.95f, 1f, 1.1f)));
            }

            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_Boots", Folder);
        }

        public static Mesh Mug()
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(0.036f, 0f), new Vector2(0.04f, 0.004f), new Vector2(0.041f, 0.03f), new Vector2(0.042f, 0.092f), new Vector2(0.0395f, 0.096f)
            }, Smooth, 0, null, false, false);
            builder.AddLathe(new[]
            {
                new Vector2(0.0395f, 0.096f), new Vector2(0.037f, 0.093f), new Vector2(0.036f, 0.012f), new Vector2(0.03f, 0.008f), new Vector2(0f, 0.008f)
            }, Smooth, 1, null, false, false);
            builder.AddTorus(Vector3.zero, 0.024f, 0.0065f, 18, 8, 0, TidyMeshBuilder.TRS(new Vector3(0.046f, 0.05f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.85f, 1f, 1.15f)));
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_Mug", Folder);
        }

        public static Mesh Plate()
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(new[]
            {
                new Vector2(0f, 0.006f), new Vector2(0.062f, 0.006f), new Vector2(0.065f, 0f), new Vector2(0.07f, 0.002f), new Vector2(0.09f, 0.012f)
            }, Smooth + 8, 0, null, false, false);
            builder.AddLathe(new[]
            {
                new Vector2(0.09f, 0.012f), new Vector2(0.112f, 0.02f), new Vector2(0.114f, 0.023f), new Vector2(0.11f, 0.024f), new Vector2(0.092f, 0.017f)
            }, Smooth + 8, 1, null, false, false);
            builder.AddLathe(new[]
            {
                new Vector2(0.092f, 0.017f), new Vector2(0.065f, 0.01f), new Vector2(0f, 0.01f)
            }, Smooth + 8, 0, null, false, false);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_Plate", Folder);
        }

        public static Mesh Plush(int kind)
        {
            var builder = new TidyMeshBuilder(3) { UvScale = new Vector2(6f, 6f) };
            builder.AddSphere(Vector3.zero, 0.075f, 18, 12, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.075f, 0f), Vector3.zero, new Vector3(1f, 1.05f, 0.9f)));
            builder.AddSphere(Vector3.zero, 0.064f, 18, 12, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.19f, 0.01f), Vector3.zero, new Vector3(1.05f, 0.95f, 0.95f)));
            builder.AddSphere(Vector3.zero, 0.042f, 14, 10, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.08f, 0.048f), Vector3.zero, new Vector3(1f, 1.1f, 0.5f)));
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddSphere(Vector3.zero, 0.028f, 12, 8, 0, TidyMeshBuilder.TRS(new Vector3(side * 0.07f, 0.1f, 0.02f), new Vector3(0f, 0f, side * 30f), new Vector3(0.8f, 1.4f, 0.8f)));
                builder.AddSphere(Vector3.zero, 0.03f, 12, 8, 0, TidyMeshBuilder.TRS(new Vector3(side * 0.042f, 0.022f, 0.05f), Vector3.zero, new Vector3(0.9f, 0.75f, 1.3f)));
                builder.AddSphere(new Vector3(side * 0.024f, 0.205f, 0.066f), 0.0085f, 8, 6, 2);
                AddEar(builder, kind, side);
            }

            builder.AddSphere(Vector3.zero, 0.022f, 12, 8, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.178f, 0.064f), Vector3.zero, new Vector3(1.2f, 0.85f, 0.8f)));
            builder.AddSphere(new Vector3(0f, 0.186f, 0.083f), 0.008f, 8, 6, 2);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_Plush_" + kind, Folder);
        }

        static void AddEar(TidyMeshBuilder builder, int kind, float side)
        {
            switch (kind)
            {
                case 1:
                    builder.AddSphere(Vector3.zero, 0.022f, 12, 8, 0, TidyMeshBuilder.TRS(new Vector3(side * 0.028f, 0.29f, 0f), new Vector3(0f, 0f, -side * 12f), new Vector3(0.75f, 2.8f, 0.55f)));
                    builder.AddSphere(Vector3.zero, 0.014f, 10, 6, 1, TidyMeshBuilder.TRS(new Vector3(side * 0.028f, 0.29f, 0.008f), new Vector3(0f, 0f, -side * 12f), new Vector3(0.7f, 2.6f, 0.4f)));
                    break;
                case 2:
                    builder.AddCrystal(0.026f, 0.01f, 3, 0.04f, 0, TidyMeshBuilder.TRS(new Vector3(side * 0.038f, 0.232f, 0f), new Vector3(0f, 0f, -side * 18f), new Vector3(1f, 1f, 0.6f)));
                    break;
                case 3:
                    builder.AddSphere(Vector3.zero, 0.024f, 12, 8, 0, TidyMeshBuilder.TRS(new Vector3(side * 0.034f, 0.245f, 0.018f), Vector3.zero, Vector3.one));
                    builder.AddSphere(new Vector3(side * 0.034f, 0.25f, 0.038f), 0.011f, 10, 6, 2);
                    break;
                default:
                    builder.AddSphere(Vector3.zero, 0.022f, 12, 8, 0, TidyMeshBuilder.TRS(new Vector3(side * 0.048f, 0.245f, 0f), Vector3.zero, new Vector3(1f, 1f, 0.6f)));
                    builder.AddSphere(Vector3.zero, 0.012f, 10, 6, 1, TidyMeshBuilder.TRS(new Vector3(side * 0.048f, 0.245f, 0.008f), Vector3.zero, new Vector3(1f, 1f, 0.5f)));
                    break;
            }
        }

        public static Mesh ToyBlock()
        {
            var builder = new TidyMeshBuilder(2);
            const float size = 0.075f;
            builder.AddBeveledBox(new Vector3(0f, size * 0.5f, 0f), Vector3.one * size, 0.008f, 2, 0);
            var normals = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.forward, Vector3.back };
            foreach (var normal in normals)
            {
                var panel = new Vector3(Mathf.Abs(normal.x) > 0.5f ? 0.004f : 0.054f, Mathf.Abs(normal.y) > 0.5f ? 0.004f : 0.054f, Mathf.Abs(normal.z) > 0.5f ? 0.004f : 0.054f);
                builder.AddBeveledBox(new Vector3(0f, size * 0.5f, 0f) + normal * (size * 0.5f + 0.0005f), panel, 0.0018f, 1, 1);
            }

            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_ToyBlock", Folder);
        }

        public static Mesh PaperBall(int seed)
        {
            var builder = new TidyMeshBuilder();
            builder.AddSphere(Vector3.zero, 0.036f, 10, 7, 0, TidyMeshBuilder.TRS(Vector3.zero, Vector3.zero, new Vector3(1f, 0.9f, 1.05f)));
            var mesh = builder.Build("SM_Room_PaperBall_" + seed, false);
            TidyMeshUtility.Jitter(mesh, 0.022f, seed);
            TidyMeshUtility.RecenterBottom(mesh);
            return TidyMeshAssets.SaveMesh(mesh, Folder + "/" + mesh.name + ".asset");
        }

        public static Mesh SodaCan(bool crushed)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(new[]
            {
                new Vector2(0f, 0.004f), new Vector2(0.02f, 0f), new Vector2(0.03f, 0.006f), new Vector2(0.033f, 0.014f)
            }, Smooth, 1, null, false, false);
            builder.AddLathe(new[] { new Vector2(0.033f, 0.014f), new Vector2(0.033f, 0.104f) }, Smooth, 0, null, false, false);
            builder.AddLathe(new[]
            {
                new Vector2(0.033f, 0.104f), new Vector2(0.026f, 0.118f), new Vector2(0.027f, 0.122f), new Vector2(0.024f, 0.121f), new Vector2(0.022f, 0.117f), new Vector2(0f, 0.117f)
            }, Smooth, 1, null, false, false);
            builder.AddBeveledBox(new Vector3(0.008f, 0.12f, 0f), new Vector3(0.02f, 0.003f, 0.012f), 0.001f, 1, 1);
            var mesh = builder.Build(crushed ? "SM_Room_SodaCanCrushed" : "SM_Room_SodaCan", false);
            if (crushed)
            {
                Squash(mesh);
                TidyMeshUtility.Jitter(mesh, 0.004f, 77);
            }

            TidyMeshUtility.RecenterBottom(mesh);
            return TidyMeshAssets.SaveMesh(mesh, Folder + "/" + mesh.name + ".asset");
        }

        static void Squash(Mesh mesh)
        {
            var vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            for (var index = 0; index < vertices.Count; index++)
            {
                var vertex = vertices[index];
                var pinch = 1f - 0.35f * Mathf.Exp(-Mathf.Pow((vertex.y - 0.06f) / 0.025f, 2f)) * (vertex.x > 0f ? 1f : 0.6f);
                vertices[index] = new Vector3(vertex.x * pinch, vertex.y * 0.82f, vertex.z);
            }

            mesh.SetVertices(vertices);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        public static Mesh Wrapper(int seed)
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBeveledBox(new Vector3(0f, 0.008f, 0f), new Vector3(0.12f, 0.016f, 0.07f), 0.007f, 2, 0);
            foreach (var side in new[] { -1f, 1f })
            {
                for (var tooth = 0; tooth < 6; tooth++)
                {
                    builder.AddBeveledBox(new Vector3(side * 0.066f, 0.008f, -0.03f + tooth * 0.012f), new Vector3(0.016f, 0.003f, 0.011f), 0.001f, 1, 1, TidyMeshBuilder.TRS(Vector3.zero, new Vector3(tooth % 2 == 0 ? 6f : -6f, 0f, 0f), Vector3.one));
                }
            }

            var mesh = builder.Build("SM_Room_Wrapper_" + seed, false);
            TidyMeshUtility.Jitter(mesh, 0.004f, seed);
            TidyMeshUtility.RecenterBottom(mesh);
            return TidyMeshAssets.SaveMesh(mesh, Folder + "/" + mesh.name + ".asset");
        }

        public static Mesh DustBunny(int seed)
        {
            var random = new System.Random(seed);
            var builder = new TidyMeshBuilder();
            builder.AddSphere(Vector3.zero, 0.05f, 12, 8, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.025f, 0f), Vector3.zero, new Vector3(1.3f, 0.55f, 1f)));
            for (var puff = 0; puff < 3; puff++)
            {
                var offset = new Vector3(((float)random.NextDouble() - 0.5f) * 0.08f, 0.02f, ((float)random.NextDouble() - 0.5f) * 0.06f);
                builder.AddSphere(offset, 0.026f + (float)random.NextDouble() * 0.012f, 10, 6);
            }

            var mesh = builder.Build("SM_Room_DustBunny_" + seed, false);
            TidyMeshUtility.Jitter(mesh, 0.018f, seed);
            TidyMeshUtility.RecenterBottom(mesh);
            return TidyMeshAssets.SaveMesh(mesh, Folder + "/" + mesh.name + ".asset");
        }

        public static Mesh Key()
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddTorus(Vector3.zero, 0.016f, 0.0045f, 18, 6, 0, TidyMeshBuilder.TRS(new Vector3(-0.035f, 0.0045f, 0f), Vector3.zero, Vector3.one));
            builder.AddBeveledBox(new Vector3(0.008f, 0.0045f, 0f), new Vector3(0.056f, 0.006f, 0.008f), 0.002f, 1, 0);
            builder.AddBeveledBox(new Vector3(0.026f, 0.0045f, -0.008f), new Vector3(0.006f, 0.006f, 0.012f), 0.0015f, 1, 0);
            builder.AddBeveledBox(new Vector3(0.034f, 0.0045f, -0.007f), new Vector3(0.006f, 0.006f, 0.01f), 0.0015f, 1, 0);
            builder.AddTorus(Vector3.zero, 0.011f, 0.0018f, 14, 5, 0, TidyMeshBuilder.TRS(new Vector3(-0.06f, 0.004f, 0f), Vector3.zero, Vector3.one));
            builder.AddBeveledBox(new Vector3(-0.085f, 0.004f, 0f), new Vector3(0.04f, 0.008f, 0.026f), 0.004f, 2, 1);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_Key", Folder);
        }

        public static Mesh PhotoFrame(out Mesh picture)
        {
            var builder = new TidyMeshBuilder();
            const float width = 0.16f;
            const float height = 0.12f;
            var tilt = TidyMeshBuilder.TRS(new Vector3(0f, 0f, 0.01f), new Vector3(-12f, 0f, 0f), Vector3.one);
            builder.AddBeveledBox(new Vector3(0f, 0.008f, 0f), new Vector3(width, 0.016f, 0.012f), 0.004f, 2, 0, tilt);
            builder.AddBeveledBox(new Vector3(0f, height - 0.008f, 0f), new Vector3(width, 0.016f, 0.012f), 0.004f, 2, 0, tilt);
            builder.AddBeveledBox(new Vector3(-width * 0.5f + 0.008f, height * 0.5f, 0f), new Vector3(0.016f, height, 0.012f), 0.004f, 2, 0, tilt);
            builder.AddBeveledBox(new Vector3(width * 0.5f - 0.008f, height * 0.5f, 0f), new Vector3(0.016f, height, 0.012f), 0.004f, 2, 0, tilt);
            builder.AddBox(new Vector3(0f, height * 0.5f, -0.003f), new Vector3(width - 0.03f, height - 0.03f, 0.003f), 0, tilt);
            builder.AddBeveledBox(Vector3.zero, new Vector3(0.03f, height * 0.8f, 0.006f), 0.002f, 1, 0, TidyMeshBuilder.TRS(new Vector3(0f, height * 0.4f, -0.025f), new Vector3(20f, 0f, 0f), Vector3.one));
            var halfW = (width - 0.03f) * 0.5f;
            var y0 = 0.015f;
            var pictureHeight = height - 0.03f;
            var pictureBuilder = new TidyMeshBuilder();
            pictureBuilder.AddPlane(new Vector2(halfW * 2f, pictureHeight), 1, 1, 0, tilt * TidyMeshBuilder.TRS(new Vector3(0f, y0 + pictureHeight * 0.5f, 0.001f), new Vector3(-90f, 180f, 0f), Vector3.one), true);
            picture = TidyMeshUtility.SaveAsIs(pictureBuilder, "SM_Room_PhotoPicture", Folder);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_PhotoFrame", Folder);
        }

        public static Mesh RingBox()
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddBeveledBox(new Vector3(0f, 0.018f, 0f), new Vector3(0.06f, 0.036f, 0.06f), 0.008f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.048f, 0f), new Vector3(0.062f, 0.022f, 0.062f), 0.01f, 3, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.037f, 0f), new Vector3(0.064f, 0.003f, 0.064f), 0.001f, 1, 1);
            builder.AddSphere(new Vector3(0f, 0.036f, 0.032f), 0.004f, 8, 6, 1);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_RingBox", Folder);
        }

        public static Mesh Sponge()
        {
            var builder = new TidyMeshBuilder(2) { UvScale = new Vector2(8f, 8f) };
            builder.AddBeveledBox(new Vector3(0f, 0.02f, 0f), new Vector3(0.11f, 0.04f, 0.072f), 0.012f, 3, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.047f, 0f), new Vector3(0.108f, 0.014f, 0.07f), 0.004f, 2, 1);
            var mesh = builder.Build("SM_Room_Sponge");
            TidyMeshUtility.Jitter(mesh, 0.0015f, 3);
            TidyMeshUtility.RecenterBottom(mesh);
            return TidyMeshAssets.SaveMesh(mesh, Folder + "/SM_Room_Sponge.asset");
        }

        public static Mesh HandVacuum()
        {
            var builder = new TidyMeshBuilder(4);
            var horizontal = new Vector3(90f, 0f, 0f);
            builder.AddLathe(new[]
            {
                new Vector2(0f, -0.13f), new Vector2(0.038f, -0.13f), new Vector2(0.046f, -0.118f), new Vector2(0.048f, -0.02f), new Vector2(0.044f, 0f)
            }, Smooth, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.05f, 0f), horizontal, Vector3.one), false, true);
            builder.AddLathe(new[]
            {
                new Vector2(0.044f, 0f), new Vector2(0.045f, 0.004f), new Vector2(0.045f, 0.09f), new Vector2(0.042f, 0.094f)
            }, Smooth, 1, TidyMeshBuilder.TRS(new Vector3(0f, 0.05f, 0f), horizontal, Vector3.one), false, true);
            builder.AddLathe(new[]
            {
                new Vector2(0.042f, 0.094f), new Vector2(0.034f, 0.12f), new Vector2(0.018f, 0.2f), new Vector2(0.02f, 0.21f), new Vector2(0.012f, 0.21f)
            }, Smooth, 2, TidyMeshBuilder.TRS(new Vector3(0f, 0.05f, 0f), horizontal, Vector3.one), false, true);
            builder.AddTorus(Vector3.zero, 0.05f, 0.011f, 20, 8, 2, TidyMeshBuilder.TRS(new Vector3(0f, 0.1f, -0.08f), new Vector3(0f, 0f, 90f), new Vector3(1f, 1f, 1.1f)));
            builder.AddBeveledBox(new Vector3(0f, 0.07f, -0.04f), new Vector3(0.022f, 0.06f, 0.05f), 0.008f, 2, 3);
            builder.AddBeveledBox(new Vector3(0f, 0.098f, -0.065f), new Vector3(0.012f, 0.008f, 0.02f), 0.003f, 1, 3);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Room_HandVacuum", Folder);
        }
    }
}
