using System.Collections.Generic;
using TidyUpFramework.Cleaning;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static partial class TidyRoomScene
    {
        enum Pose
        {
            Upright,
            Side,
            Mixed,
            Tumbled
        }

        static readonly List<Vector3> occupiedSpots = new List<Vector3>();
        static readonly Rect FloorArea = new Rect(-4.0f, -3.45f, 7.95f, 6.95f);

        static void BuildStains()
        {
            var root = TidySceneKit.Group("Stains", clutter);
            var mesh = TidyRoomDecor.Stain();
            var floorStains = new[]
            {
                new Vector4(2.55f, -2.55f, 0.55f, 0f), new Vector4(3.25f, -0.95f, 0.5f, 0f), new Vector4(0.75f, 1.15f, 0.6f, 0f), new Vector4(-1.85f, -1.3f, 0.7f, 0f),
                new Vector4(0.35f, 2.55f, 0.5f, 0f), new Vector4(-1.45f, 2.95f, 0.45f, 0f), new Vector4(2.05f, 0.85f, 0.65f, 0f), new Vector4(-0.15f, -2.65f, 0.5f, 0f)
            };
            var index = 0;
            foreach (var stain in floorStains)
            {
                Stain(root, mesh, index++, new Vector3(stain.x, SurfaceAt(stain.x, stain.y) + 0.006f, stain.y), Quaternion.Euler(0f, Yaw(), 0f), stain.z);
            }

            Stain(root, mesh, index++, new Vector3(-4.285f, 0.85f, -2.2f), Quaternion.Euler(0f, 0f, -90f) * Quaternion.Euler(0f, 30f, 0f), 0.5f);
            Stain(root, mesh, index++, new Vector3(DeskPosition.x + 0.1f, TidyRoomFurniture.DeskHeight + 0.003f, DeskPosition.z - 0.12f), Quaternion.Euler(0f, 40f, 0f), 0.24f);
            Stain(root, mesh, index, new Vector3(-0.45f, TidyRoomFurniture.CoffeeTableHeight + 0.003f, 0.25f), Quaternion.Euler(0f, 110f, 0f), 0.26f);
        }

        static void Stain(Transform parent, Mesh mesh, int index, Vector3 position, Quaternion rotation, float size)
        {
            var stain = TidySceneDressing.Decor("Stain " + (index + 1), parent, mesh, new[] { context.Material("Grime") }, position, rotation);
            stain.transform.localScale = new Vector3(size, 1f, size * Range(0.75f, 1.05f));
            stain.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var collider = stain.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.001f, 0f);
            collider.size = new Vector3(0.9f, 0.008f, 0.9f);
            var cleanable = stain.AddComponent<TidyCleanable>();
            cleanable.Configure("room_stain_" + (index + 1), "Room", 1.6f);
            cleanable.ConfigureMethods(TidyCleaningMethod.Scrubbing);
        }

        static void BuildClutter()
        {
            occupiedSpots.Clear();
            BuildToolsAndKeepsakes();
            BuildBedClutter();
            BuildFurnitureTopClutter();
            BuildUnderFurnitureDust();
            BuildFloorClutter();
        }

        static void BuildToolsAndKeepsakes()
        {
            Spawn("room_sponge", new Vector3(3.86f, 0f, -1.8f), new Vector3(0f, 25f, 0f), TidyRoomFurniture.SideboardHeight);
            var vacuumSpot = new Vector3(2.55f, 0f, -3.25f);
            Spawn("room_vacuum", vacuumSpot, new Vector3(0f, 120f, 0f), 0f);
            occupiedSpots.Add(new Vector3(vacuumSpot.x, 0.35f, vacuumSpot.z));
            Spawn("room_keepsake_ring", new Vector3(-2.95f, 0f, 0.97f), new Vector3(0f, 20f, 0f), 0f);
            Spawn("room_keepsake_key", new Vector3(1.2f, 0f, 3.68f), new Vector3(0f, 70f, 0f), 0f);
            Spawn("room_keepsake_photo", new Vector3(-2.98f, 0f, 3.62f), new Vector3(0f, 160f, 0f), 0f);
            occupiedSpots.Add(new Vector3(-2.95f, 0.2f, 0.97f));
        }

        static void BuildBedClutter()
        {
            var ids = new[] { "room_tshirt_red", "room_tshirt_blue", "room_sweater_pink", "room_tshirt_teal", "room_book_poems", "room_plush_bunny", "room_plate", "room_socks_pink", "room_paper_0", "room_wrapper_orange" };
            var spots = new List<Vector3>();
            for (var index = 0; index < ids.Length; index++)
            {
                var spot = FindSpot(new Rect(BedPosition.x - 0.55f, BedPosition.z - 0.6f, 1.45f, 1.2f), 0.22f, spots);
                spots.Add(spot);
                PlaceWithPose(ids[index], spot, PoseFor(ids[index]));
            }
        }

        static void BuildFurnitureTopClutter()
        {
            Surface(new[] { "room_mug_blue", "room_mug_yellow", "room_plate", "room_book_garden", "room_paper_1", "room_paper_2", "room_soda_red", "room_block_red" }, new Rect(DeskPosition.x - 0.4f, DeskPosition.z - 0.24f, 0.75f, 0.36f), 0.13f);
            Surface(new[] { "room_mug_pink", "room_plate", "room_plate", "room_soda_teal", "room_wrapper_purple", "room_book_recipes", "room_block_blue" }, new Rect(CoffeeTablePosition.x - 0.26f, CoffeeTablePosition.z - 0.26f, 0.52f, 0.52f), 0.14f);
            Surface(new[] { "room_mug_green", "room_soda_red" }, new Rect(NightstandPosition.x - 0.12f, NightstandPosition.z - 0.18f, 0.2f, 0.3f), 0.12f);
            Surface(new[] { "room_tshirt_yellow" }, new Rect(ChairPosition.x - 0.05f, ChairPosition.z - 0.02f, 0.1f, 0.1f), 0.1f);
            Surface(new[] { "room_sweater_green" }, new Rect(PoufPosition.x - 0.05f, PoufPosition.z - 0.05f, 0.1f, 0.1f), 0.1f);
        }

        static void Surface(string[] ids, Rect area, float spacing)
        {
            var spots = new List<Vector3>();
            foreach (var id in ids)
            {
                var spot = FindSpot(area, spacing, spots);
                spots.Add(spot);
                PlaceWithPose(id, spot, PoseFor(id));
            }
        }

        static void BuildUnderFurnitureDust()
        {
            var spots = new[]
            {
                new Vector3(-3.55f, 0f, 1.25f), new Vector3(-3.0f, 0f, 2.05f), new Vector3(-2.45f, 0f, 1.5f),
                new Vector3(2.3f, 0f, 3.5f), new Vector3(2.68f, 0f, 3.6f)
            };
            for (var index = 0; index < spots.Length; index++)
            {
                Spawn(TidyRoomContent.DustBunnies[index % TidyRoomContent.DustBunnies.Length], spots[index], new Vector3(0f, Yaw(), 0f), 0f);
                occupiedSpots.Add(new Vector3(spots[index].x, 0.15f, spots[index].z));
            }
        }

        static void BuildFloorClutter()
        {
            var ids = new List<string>();
            ids.AddRange(new[] { "room_tshirt_red", "room_tshirt_blue", "room_tshirt_yellow", "room_tshirt_teal", "room_sweater_purple", "room_sweater_orange" });
            foreach (var color in TidyRoomContent.SockColors)
            {
                ids.Add("room_socks_" + color.ToLowerInvariant());
            }

            ids.AddRange(new[] { "room_socks_white", "room_socks_blue", "room_socks_yellow" });
            foreach (var color in TidyRoomContent.SneakerColors)
            {
                ids.Add("room_sneakers_" + color.ToLowerInvariant());
            }

            ids.AddRange(new[] { "room_boots_brown", "room_mug_teal", "room_plate", "room_book_knitting", "room_book_stars", "room_book_travel" });
            ids.AddRange(new[] { "room_plush_bear", "room_plush_cat", "room_plush_frog" });
            ids.AddRange(new[] { "room_block_red", "room_block_blue", "room_block_yellow", "room_block_yellow", "room_block_green", "room_block_green" });
            ids.AddRange(new[] { "room_paper_0", "room_paper_1", "room_paper_2", "room_paper_0", "room_paper_1" });
            ids.AddRange(new[] { "room_soda_teal", "room_soda_red", "room_soda_teal" });
            ids.AddRange(new[] { "room_wrapper_orange", "room_wrapper_purple", "room_wrapper_orange", "room_wrapper_purple" });
            for (var index = 0; index < 7; index++)
            {
                ids.Add(TidyRoomContent.DustBunnies[index % TidyRoomContent.DustBunnies.Length]);
            }

            foreach (var id in TidySceneKit.Shuffle(ids, random))
            {
                var spacing = id.StartsWith("room_dust") ? 0.5f : 0.3f;
                var spot = FindFloorSpot(spacing, id.StartsWith("room_dust"));
                PlaceWithPose(id, spot, PoseFor(id));
            }
        }

        static Vector3 FindFloorSpot(float spacing, bool nearWalls)
        {
            for (var attempt = 0; attempt < 400; attempt++)
            {
                var x = Range(FloorArea.xMin, FloorArea.xMax);
                var z = Range(FloorArea.yMin, FloorArea.yMax);
                if (nearWalls && Mathf.Min(Mathf.Min(x - FloorArea.xMin, FloorArea.xMax - x), Mathf.Min(z - FloorArea.yMin, FloorArea.yMax - z)) > 1.2f)
                {
                    continue;
                }

                if (Vector2.Distance(new Vector2(x, z), new Vector2(SpawnPoint.x, SpawnPoint.z)) < 0.8f || !IsClearFloor(x, z, 0.16f))
                {
                    continue;
                }

                if (IsFree(x, z, attempt > 300 ? spacing * 0.6f : spacing))
                {
                    var spot = new Vector3(x, SurfaceAt(x, z), z);
                    occupiedSpots.Add(spot);
                    return spot;
                }
            }

            var fallback = new Vector3(Range(-1.5f, 1.5f), 0f, Range(-2.5f, -1.5f));
            fallback.y = SurfaceAt(fallback.x, fallback.z);
            return fallback;
        }

        static bool IsClearFloor(float x, float z, float radius)
        {
            foreach (var offset in new[] { Vector2.zero, new Vector2(radius, 0f), new Vector2(-radius, 0f), new Vector2(0f, radius), new Vector2(0f, -radius) })
            {
                if (SurfaceAt(x + offset.x, z + offset.y) > 0.02f)
                {
                    return false;
                }
            }

            return true;
        }

        static bool IsFree(float x, float z, float spacing)
        {
            foreach (var spot in occupiedSpots)
            {
                if ((new Vector2(spot.x, spot.z) - new Vector2(x, z)).sqrMagnitude < spacing * spacing)
                {
                    return false;
                }
            }

            return true;
        }

        static Vector3 FindSpot(Rect area, float spacing, List<Vector3> local)
        {
            var best = new Vector3(area.center.x, 0f, area.center.y);
            for (var attempt = 0; attempt < 120; attempt++)
            {
                var x = Range(area.xMin, area.xMax);
                var z = Range(area.yMin, area.yMax);
                var free = true;
                foreach (var spot in local)
                {
                    if ((new Vector2(spot.x, spot.z) - new Vector2(x, z)).sqrMagnitude < spacing * spacing)
                    {
                        free = false;
                        break;
                    }
                }

                if (free)
                {
                    best = new Vector3(x, 0f, z);
                    break;
                }
            }

            best.y = SurfaceAt(best.x, best.z);
            return best;
        }

        static Pose PoseFor(string id)
        {
            if (id.StartsWith("room_book"))
            {
                return Pose.Side;
            }

            if (id.StartsWith("room_mug") || id.StartsWith("room_soda") || id.StartsWith("room_plush"))
            {
                return Pose.Mixed;
            }

            if (id.StartsWith("room_block"))
            {
                return Pose.Tumbled;
            }

            return Pose.Upright;
        }

        static void PlaceWithPose(string id, Vector3 spot, Pose pose)
        {
            var yaw = Yaw();
            Vector3 euler;
            switch (pose)
            {
                case Pose.Side:
                    euler = new Vector3(0f, yaw, 90f);
                    break;
                case Pose.Mixed:
                    euler = random.NextDouble() < 0.4 ? new Vector3(id.StartsWith("room_plush") ? -80f : 0f, yaw, id.StartsWith("room_plush") ? 0f : 90f) : new Vector3(0f, yaw, 0f);
                    break;
                case Pose.Tumbled:
                    euler = new Vector3(random.Next(4) * 90f, yaw, random.Next(4) * 90f);
                    break;
                default:
                    euler = new Vector3(Range(-3f, 3f), yaw, Range(-3f, 3f));
                    break;
            }

            Spawn(id, new Vector3(spot.x, spot.y, spot.z), euler, spot.y);
        }
    }
}
