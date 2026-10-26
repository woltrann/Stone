using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyRoomFurniture
    {
        public const string Folder = TidyRoomModels.Folder;
        public const float BedLength = 2.1f;
        public const float BedWidth = 1.55f;
        public const float WardrobeWidth = 1.6f;
        public const float WardrobeHeight = 2.1f;
        public const float WardrobeDepth = 0.58f;
        public const float WardrobeDivider = -0.05f;
        public static readonly float[] WardrobeShelves = { 0.105f, 0.625f, 1.125f, 1.625f };
        public const float DeskWidth = 1.3f;
        public const float DeskDepth = 0.62f;
        public const float DeskHeight = 0.75f;
        public const float NightstandHeight = 0.56f;
        public const float ChestWidth = 0.85f;
        public const float ChestDepth = 0.45f;
        public const float ChestHeight = 0.48f;
        public const float RackWidth = 0.92f;
        public const float RackDepth = 0.32f;
        public static readonly float[] RackTiers = { 0.1f, 0.42f };
        public const float SideboardWidth = 1.3f;
        public const float SideboardDepth = 0.55f;
        public const float SideboardHeight = 0.93f;
        public const float CoffeeTableHeight = 0.4f;

        public static Mesh Bed()
        {
            var builder = new TidyMeshBuilder(6);
            var halfL = BedLength * 0.5f;
            var halfW = BedWidth * 0.5f;
            foreach (var corner in new[] { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f) })
            {
                builder.AddBeveledBox(new Vector3(corner.x * (halfL - 0.06f), 0.13f, corner.y * (halfW - 0.05f)), new Vector3(0.07f, 0.26f, 0.07f), 0.012f, 2, 0);
            }

            builder.AddBeveledBox(new Vector3(0f, 0.29f, halfW - 0.025f), new Vector3(BedLength, 0.14f, 0.05f), 0.012f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.29f, -halfW + 0.025f), new Vector3(BedLength, 0.14f, 0.05f), 0.012f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.27f, 0f), new Vector3(BedLength - 0.1f, 0.03f, BedWidth - 0.1f), 0.008f, 1, 0);
            var head = new System.Collections.Generic.List<Vector2> { new Vector2(-halfW - 0.04f, 0f), new Vector2(halfW + 0.04f, 0f), new Vector2(halfW + 0.04f, 0.92f) };
            for (var step = 1; step < 16; step++)
            {
                var angle = Mathf.PI * step / 16f;
                head.Add(new Vector2(Mathf.Cos(angle) * (halfW + 0.04f), 0.92f + Mathf.Sin(angle) * 0.26f));
            }

            head.Add(new Vector2(-halfW - 0.04f, 0.92f));
            builder.AddExtrusion(head.ToArray(), 0.07f, 0, TidyMeshBuilder.TRS(new Vector3(-halfL - 0.07f, 0f, 0f), new Vector3(0f, 90f, 0f), Vector3.one), 0.02f);
            builder.AddBeveledBox(new Vector3(-halfL + 0.03f, 0.86f, 0f), new Vector3(0.07f, 0.5f, BedWidth - 0.12f), 0.035f, 3, 5);
            builder.AddBeveledBox(new Vector3(halfL + 0.02f, 0.3f, 0f), new Vector3(0.06f, 0.56f, BedWidth + 0.08f), 0.02f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.45f, 0f), new Vector3(BedLength - 0.06f, 0.2f, BedWidth - 0.06f), 0.05f, 3, 1);
            builder.AddBeveledBox(new Vector3(0.22f, 0.565f, 0f), new Vector3(BedLength - 0.5f, 0.07f, BedWidth + 0.04f), 0.03f, 3, 2);
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBeveledBox(new Vector3(0.22f, 0.44f, side * (halfW + 0.025f)), new Vector3(BedLength - 0.5f, 0.24f, 0.03f), 0.012f, 2, 2);
                builder.AddBeveledBox(new Vector3(halfL - 0.32f, 0.47f, side * (halfW + 0.05f)), new Vector3(0.46f, 0.26f, 0.025f), 0.01f, 2, 4);
                builder.AddBeveledBox(Vector3.zero, new Vector3(0.32f, 0.14f, 0.5f), 0.065f, 3, 3, TidyMeshBuilder.TRS(new Vector3(-halfL + 0.28f, 0.66f, side * 0.36f), new Vector3(0f, side * 4f, 12f), Vector3.one));
            }

            builder.AddBeveledBox(new Vector3(-0.5f, 0.6f, 0f), new Vector3(0.2f, 0.06f, BedWidth + 0.05f), 0.025f, 2, 1);
            builder.AddBeveledBox(new Vector3(halfL - 0.32f, 0.61f, 0f), new Vector3(0.46f, 0.035f, BedWidth + 0.1f), 0.015f, 2, 4);
            builder.AddBeveledBox(Vector3.zero, new Vector3(0.12f, 0.24f, 0.24f), 0.05f, 3, 5, TidyMeshBuilder.TRS(new Vector3(-halfL + 0.48f, 0.68f, 0.05f), new Vector3(0f, -10f, -20f), Vector3.one));
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Bed", Folder);
        }

        public static Mesh Wardrobe()
        {
            var builder = new TidyMeshBuilder(5);
            var halfW = WardrobeWidth * 0.5f;
            var halfD = WardrobeDepth * 0.5f;
            builder.AddBeveledBox(new Vector3(-halfW + 0.02f, WardrobeHeight * 0.5f, 0f), new Vector3(0.04f, WardrobeHeight, WardrobeDepth), 0.008f, 2, 0);
            builder.AddBeveledBox(new Vector3(halfW - 0.02f, WardrobeHeight * 0.5f, 0f), new Vector3(0.04f, WardrobeHeight, WardrobeDepth), 0.008f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, WardrobeHeight - 0.03f, 0.01f), new Vector3(WardrobeWidth + 0.06f, 0.06f, WardrobeDepth + 0.04f), 0.012f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.04f, -0.01f), new Vector3(WardrobeWidth - 0.02f, 0.08f, WardrobeDepth - 0.02f), 0.01f, 2, 0);
            builder.AddBox(new Vector3(0f, WardrobeHeight * 0.5f, -halfD + 0.01f), new Vector3(WardrobeWidth - 0.04f, WardrobeHeight - 0.02f, 0.02f), 1);
            builder.AddBeveledBox(new Vector3(WardrobeDivider, WardrobeHeight * 0.5f, 0f), new Vector3(0.03f, WardrobeHeight - 0.1f, WardrobeDepth - 0.02f), 0.006f, 1, 0);
            var shelfWidth = halfW - 0.04f - (WardrobeDivider + 0.015f);
            var shelfCenter = (WardrobeDivider + 0.015f + halfW - 0.04f) * 0.5f;
            foreach (var surface in WardrobeShelves)
            {
                builder.AddBeveledBox(new Vector3(shelfCenter, surface - 0.0125f, 0.005f), new Vector3(shelfWidth + 0.002f, 0.025f, WardrobeDepth - 0.04f), 0.005f, 1, 1);
            }

            var leftCenter = (-halfW + 0.04f + WardrobeDivider - 0.015f) * 0.5f;
            var leftWidth = WardrobeDivider - 0.015f - (-halfW + 0.04f);
            builder.AddBeveledBox(new Vector3(leftCenter, 0.25f, 0.02f), new Vector3(leftWidth - 0.01f, 0.32f, WardrobeDepth - 0.06f), 0.012f, 2, 0);
            builder.AddBeveledBox(new Vector3(leftCenter, 0.25f, halfD - 0.03f), new Vector3(leftWidth - 0.06f, 0.24f, 0.02f), 0.008f, 2, 1);
            builder.AddSphere(new Vector3(leftCenter, 0.25f, halfD - 0.008f), 0.014f, 10, 6, 2);
            builder.AddBeveledBox(new Vector3(leftCenter, 0.425f, 0.005f), new Vector3(leftWidth + 0.002f, 0.025f, WardrobeDepth - 0.04f), 0.005f, 1, 1);
            builder.AddCylinder(0.012f, leftWidth, 12, 2, TidyMeshBuilder.TRS(new Vector3(-halfW + 0.04f, 1.8f, 0f), new Vector3(0f, 0f, -90f), Vector3.one));
            var hanging = new[] { -0.26f, -0.14f, 0.0f, 0.13f, 0.25f };
            for (var index = 0; index < hanging.Length; index++)
            {
                var x = leftCenter + hanging[index] * (leftWidth / 0.66f) * 0.95f;
                var subMesh = index % 2 == 0 ? 3 : 4;
                builder.AddTorus(Vector3.zero, 0.05f, 0.004f, 14, 4, 2, TidyMeshBuilder.TRS(new Vector3(x, 1.76f, 0f), new Vector3(0f, 0f, 90f), new Vector3(1f, 0.5f, 3.2f)));
                builder.AddBeveledBox(Vector3.zero, new Vector3(0.05f, 0.7f, 0.44f), 0.02f, 2, subMesh, TidyMeshBuilder.TRS(new Vector3(x, 1.38f, 0f), new Vector3(0f, (index - 2) * 4f, (index - 2) * 2f), Vector3.one));
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Wardrobe", Folder);
        }

        public static float WardrobeShelfCenter => (WardrobeDivider + 0.015f + WardrobeWidth * 0.5f - 0.04f) * 0.5f;

        public static Mesh Desk()
        {
            var builder = new TidyMeshBuilder(3);
            var halfW = DeskWidth * 0.5f;
            var halfD = DeskDepth * 0.5f;
            builder.AddBeveledBox(new Vector3(0f, DeskHeight - 0.0175f, 0f), new Vector3(DeskWidth, 0.035f, DeskDepth), 0.01f, 2, 0);
            var pedestalX = -halfW + 0.23f;
            builder.AddBeveledBox(new Vector3(pedestalX, (DeskHeight - 0.035f) * 0.5f, -0.01f), new Vector3(0.42f, DeskHeight - 0.035f, DeskDepth - 0.06f), 0.01f, 2, 1);
            for (var drawer = 0; drawer < 3; drawer++)
            {
                var y = 0.14f + drawer * 0.21f;
                builder.AddBeveledBox(new Vector3(pedestalX, y, halfD - 0.03f), new Vector3(0.38f, 0.19f, 0.02f), 0.008f, 2, 1);
                builder.AddBeveledBox(new Vector3(pedestalX, y + 0.03f, halfD - 0.012f), new Vector3(0.1f, 0.014f, 0.014f), 0.005f, 1, 2);
            }

            foreach (var z in new[] { -1f, 1f })
            {
                builder.AddLathe(new[] { new Vector2(0.018f, 0f), new Vector2(0.024f, DeskHeight - 0.08f), new Vector2(0.028f, DeskHeight - 0.035f) }, 12, 1, Matrix4x4.Translate(new Vector3(halfW - 0.06f, 0f, z * (halfD - 0.06f))));
            }

            builder.AddBeveledBox(new Vector3(0.18f, DeskHeight - 0.08f, -halfD + 0.06f), new Vector3(0.84f, 0.09f, 0.02f), 0.006f, 1, 1);
            builder.AddBeveledBox(new Vector3(0.2f, DeskHeight - 0.075f, 0.02f), new Vector3(0.6f, 0.07f, DeskDepth - 0.12f), 0.006f, 1, 1);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Desk", Folder);
        }

        public static Mesh Nightstand()
        {
            var builder = new TidyMeshBuilder(3);
            const float width = 0.48f;
            const float depth = 0.4f;
            builder.AddBeveledBox(new Vector3(0f, NightstandHeight - 0.015f, 0f), new Vector3(width + 0.02f, 0.03f, depth + 0.02f), 0.008f, 2, 1);
            builder.AddBeveledBox(new Vector3(0f, 0.32f, 0f), new Vector3(width, 0.44f, depth), 0.01f, 2, 0);
            builder.AddBox(new Vector3(0f, 0.2f, 0.12f), new Vector3(width - 0.06f, 0.14f, depth - 0.1f), 1);
            builder.AddBeveledBox(new Vector3(0f, 0.43f, depth * 0.5f + 0.004f), new Vector3(width - 0.05f, 0.16f, 0.02f), 0.007f, 2, 0);
            builder.AddSphere(new Vector3(0f, 0.43f, depth * 0.5f + 0.022f), 0.014f, 10, 6, 2);
            foreach (var corner in new[] { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f) })
            {
                builder.AddLathe(new[] { new Vector2(0.012f, 0f), new Vector2(0.018f, 0.1f) }, 10, 1, Matrix4x4.Translate(new Vector3(corner.x * (width * 0.5f - 0.04f), 0f, corner.y * (depth * 0.5f - 0.04f))));
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Nightstand", Folder);
        }

        public static Mesh ToyChest()
        {
            var builder = new TidyMeshBuilder(3);
            var halfW = ChestWidth * 0.5f;
            var halfD = ChestDepth * 0.5f;
            const float wall = 0.03f;
            builder.AddBeveledBox(new Vector3(0f, ChestHeight * 0.5f + 0.02f, halfD - wall * 0.5f), new Vector3(ChestWidth, ChestHeight - 0.04f, wall), 0.008f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, ChestHeight * 0.5f + 0.02f, -halfD + wall * 0.5f), new Vector3(ChestWidth, ChestHeight - 0.04f, wall), 0.008f, 2, 0);
            builder.AddBeveledBox(new Vector3(halfW - wall * 0.5f, ChestHeight * 0.5f + 0.02f, 0f), new Vector3(wall, ChestHeight - 0.04f, ChestDepth), 0.008f, 2, 0);
            builder.AddBeveledBox(new Vector3(-halfW + wall * 0.5f, ChestHeight * 0.5f + 0.02f, 0f), new Vector3(wall, ChestHeight - 0.04f, ChestDepth), 0.008f, 2, 0);
            builder.AddBox(new Vector3(0f, 0.06f, 0f), new Vector3(ChestWidth - wall, 0.02f, ChestDepth - wall), 1);
            foreach (var y in new[] { 0.08f, ChestHeight - 0.02f })
            {
                builder.AddBeveledBox(new Vector3(0f, y, 0f), new Vector3(ChestWidth + 0.02f, 0.04f, ChestDepth + 0.02f), 0.01f, 2, 1);
            }

            foreach (var corner in new[] { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f) })
            {
                builder.AddBeveledBox(new Vector3(corner.x * (halfW - 0.04f), 0.025f, corner.y * (halfD - 0.04f)), new Vector3(0.06f, 0.05f, 0.06f), 0.012f, 2, 1);
            }

            var hinge = new Vector3(0f, ChestHeight, -halfD);
            var lid = TidyMeshBuilder.TRS(hinge, new Vector3(-100f, 0f, 0f), Vector3.one);
            builder.AddBeveledBox(new Vector3(0f, 0.02f, halfD), new Vector3(ChestWidth + 0.03f, 0.04f, ChestDepth + 0.03f), 0.012f, 2, 0, lid);
            builder.AddBeveledBox(new Vector3(0f, -0.004f, halfD), new Vector3(ChestWidth - 0.08f, 0.012f, ChestDepth - 0.08f), 0.004f, 1, 1, lid);
            foreach (var x in new[] { -0.26f, 0.26f })
            {
                builder.AddBeveledCylinder(0.014f, 0.08f, 0.003f, 10, 1, 2, TidyMeshBuilder.TRS(hinge + new Vector3(x - 0.04f, 0f, 0f), new Vector3(0f, 0f, -90f), Vector3.one));
            }

            builder.AddBeveledBox(new Vector3(0f, ChestHeight * 0.55f, halfD + 0.006f), new Vector3(0.12f, 0.05f, 0.012f), 0.004f, 1, 2);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_ToyChest", Folder);
        }

        public static Mesh ShoeRack()
        {
            var builder = new TidyMeshBuilder(2);
            var halfW = RackWidth * 0.5f;
            foreach (var side in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -1f, 1f })
                {
                    builder.AddBeveledBox(new Vector3(side * (halfW - 0.02f), 0.36f, z * (RackDepth * 0.5f - 0.02f)), new Vector3(0.04f, 0.72f, 0.04f), 0.008f, 2, 1);
                }

                builder.AddBeveledBox(new Vector3(side * (halfW - 0.02f), 0.36f, 0f), new Vector3(0.02f, 0.03f, RackDepth - 0.04f), 0.005f, 1, 1);
            }

            foreach (var tier in RackTiers)
            {
                for (var slat = 0; slat < 5; slat++)
                {
                    var z = -RackDepth * 0.5f + 0.04f + slat * (RackDepth - 0.08f) / 4f;
                    builder.AddBeveledBox(new Vector3(0f, tier - 0.012f, z), new Vector3(RackWidth - 0.06f, 0.024f, 0.045f), 0.006f, 1, 0);
                }
            }

            builder.AddBeveledBox(new Vector3(0f, 0.735f, 0f), new Vector3(RackWidth + 0.02f, 0.03f, RackDepth + 0.02f), 0.008f, 2, 0);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_ShoeRack", Folder);
        }

        public static Mesh Sideboard()
        {
            var builder = new TidyMeshBuilder(3);
            var halfW = SideboardWidth * 0.5f;
            var halfD = SideboardDepth * 0.5f;
            builder.AddBeveledBox(new Vector3(0f, 0.47f, -0.01f), new Vector3(SideboardWidth, 0.84f, SideboardDepth - 0.02f), 0.01f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.04f, 0f), new Vector3(SideboardWidth - 0.06f, 0.08f, SideboardDepth - 0.06f), 0.008f, 1, 1);
            builder.AddBeveledBox(new Vector3(0f, SideboardHeight - 0.02f, 0.01f), new Vector3(SideboardWidth + 0.04f, 0.04f, SideboardDepth + 0.04f), 0.01f, 2, 1);
            foreach (var side in new[] { -1f, 1f })
            {
                var x = side * halfW * 0.5f;
                builder.AddBeveledBox(new Vector3(x, 0.47f, halfD - 0.01f), new Vector3(halfW - 0.04f, 0.74f, 0.02f), 0.008f, 2, 0);
                builder.AddBeveledBox(new Vector3(x, 0.47f, halfD + 0.004f), new Vector3(halfW - 0.16f, 0.6f, 0.012f), 0.006f, 2, 0);
                builder.AddBeveledBox(new Vector3(side * 0.06f, 0.62f, halfD + 0.02f), new Vector3(0.016f, 0.1f, 0.016f), 0.006f, 1, 2);
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Sideboard", Folder);
        }

        public static Mesh CoffeeTable()
        {
            var builder = new TidyMeshBuilder(2);
            builder.AddLathe(TidyProfiles.RoundedCylinder(0.4f, 0.04f, 0.015f, 2), 40, 0, Matrix4x4.Translate(new Vector3(0f, CoffeeTableHeight - 0.04f, 0f)));
            for (var leg = 0; leg < 3; leg++)
            {
                var angle = leg * 120f + 30f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.right;
                builder.AddLathe(new[] { new Vector2(0.012f, 0f), new Vector2(0.022f, CoffeeTableHeight - 0.02f) }, 10, 1, Matrix4x4.Translate(direction * 0.27f));
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_CoffeeTable", Folder);
        }

        public static Mesh Armchair()
        {
            var builder = new TidyMeshBuilder(3) { UvScale = new Vector2(2f, 2f) };
            builder.AddBeveledBox(new Vector3(0f, 0.3f, 0f), new Vector3(0.78f, 0.24f, 0.74f), 0.06f, 3, 0);
            builder.AddBeveledBox(new Vector3(0f, 0.47f, 0.04f), new Vector3(0.56f, 0.13f, 0.62f), 0.06f, 3, 2);
            builder.AddBeveledBox(Vector3.zero, new Vector3(0.78f, 0.56f, 0.18f), 0.08f, 3, 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.66f, -0.3f), new Vector3(-8f, 0f, 0f), Vector3.one));
            builder.AddBeveledBox(Vector3.zero, new Vector3(0.5f, 0.36f, 0.12f), 0.05f, 3, 2, TidyMeshBuilder.TRS(new Vector3(0f, 0.66f, -0.2f), new Vector3(-10f, 0f, 0f), Vector3.one));
            foreach (var side in new[] { -1f, 1f })
            {
                builder.AddBeveledBox(new Vector3(side * 0.33f, 0.47f, 0.0f), new Vector3(0.13f, 0.32f, 0.72f), 0.06f, 3, 0);
                foreach (var z in new[] { -0.28f, 0.28f })
                {
                    builder.AddLathe(new[] { new Vector2(0.016f, 0f), new Vector2(0.024f, 0.18f) }, 10, 1, Matrix4x4.Translate(new Vector3(side * 0.32f, 0f, z)));
                }
            }

            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Armchair", Folder);
        }

        public static Mesh Pouf()
        {
            var builder = new TidyMeshBuilder(2) { UvScale = new Vector2(3f, 3f) };
            builder.AddLathe(TidyProfiles.RoundedCylinder(0.24f, 0.34f, 0.09f, 5), 32, 0);
            builder.AddSphere(new Vector3(0f, 0.34f, 0f), 0.018f, 10, 6, 1);
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Pouf", Folder);
        }

        public static Mesh Door()
        {
            var builder = new TidyMeshBuilder(2);
            const float width = 0.9f;
            const float height = 2.08f;
            builder.AddBeveledBox(new Vector3(-width * 0.5f - 0.05f, height * 0.5f + 0.03f, 0.02f), new Vector3(0.1f, height + 0.06f, 0.04f), 0.01f, 2, 0);
            builder.AddBeveledBox(new Vector3(width * 0.5f + 0.05f, height * 0.5f + 0.03f, 0.02f), new Vector3(0.1f, height + 0.06f, 0.04f), 0.01f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, height + 0.06f, 0.02f), new Vector3(width + 0.22f, 0.12f, 0.045f), 0.01f, 2, 0);
            builder.AddBeveledBox(new Vector3(0f, height * 0.5f, 0.01f), new Vector3(width, height, 0.02f), 0.006f, 1, 0);
            foreach (var row in new[] { 0.5f, 1.45f })
            {
                foreach (var column in new[] { -0.2f, 0.2f })
                {
                    builder.AddBeveledBox(new Vector3(column, row, 0.024f), new Vector3(0.3f, row < 1f ? 0.7f : 0.95f, 0.012f), 0.006f, 2, 0);
                }
            }

            builder.AddSphere(new Vector3(width * 0.5f - 0.09f, 1.02f, 0.06f), 0.03f, 14, 10, 1);
            builder.AddBeveledCylinder(0.035f, 0.02f, 0.004f, 14, 1, 1, TidyMeshBuilder.TRS(new Vector3(width * 0.5f - 0.09f, 1.02f, 0.02f), new Vector3(90f, 0f, 0f), Vector3.one));
            builder.AddBeveledCylinder(0.008f, 0.04f, 0.002f, 8, 1, 1, TidyMeshBuilder.TRS(new Vector3(width * 0.5f - 0.09f, 1.02f, 0.02f), new Vector3(90f, 0f, 0f), Vector3.one));
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Door", Folder);
        }

        public static Mesh Radiator()
        {
            var builder = new TidyMeshBuilder();
            const int fins = 14;
            for (var fin = 0; fin < fins; fin++)
            {
                builder.AddBeveledBox(new Vector3((fin - (fins - 1) * 0.5f) * 0.075f, 0.4f, 0f), new Vector3(0.055f, 0.56f, 0.07f), 0.024f, 3, 0);
            }

            builder.AddCylinder(0.015f, fins * 0.075f + 0.04f, 10, 0, TidyMeshBuilder.TRS(new Vector3(-fins * 0.0375f - 0.02f, 0.16f, 0f), new Vector3(0f, 0f, -90f), Vector3.one));
            builder.AddCylinder(0.015f, fins * 0.075f + 0.04f, 10, 0, TidyMeshBuilder.TRS(new Vector3(-fins * 0.0375f - 0.02f, 0.64f, 0f), new Vector3(0f, 0f, -90f), Vector3.one));
            return TidyMeshUtility.SaveAsIs(builder, "SM_Room_Radiator", Folder);
        }
    }
}
