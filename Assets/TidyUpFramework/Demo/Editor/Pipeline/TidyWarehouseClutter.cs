using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseClutter
    {
        static readonly Rect DockArea = Rect.MinMaxRect(-6.6f, -10.9f, 6.4f, -5.2f);
        static readonly Rect FloorArea = Rect.MinMaxRect(-6.9f, -6.8f, 6.3f, 9.6f);
        static readonly Rect AisleArea = Rect.MinMaxRect(-7.1f, -6.6f, -4.8f, 6.8f);
        static readonly Rect NorthArea = Rect.MinMaxRect(-4.4f, 6.4f, 4.2f, 10.6f);
        static readonly Rect EastArea = Rect.MinMaxRect(0.6f, -6.6f, 6.3f, 8.2f);

        sealed class Spot
        {
            public Vector2 Center;
            public float Radius;
        }

        static TidyWarehouseStage stage;
        static readonly List<Spot> Occupied = new List<Spot>();

        public static void Build(TidyWarehouseStage warehouseStage)
        {
            stage = warehouseStage;
            Occupied.Clear();
            stage.Block(-1.3f, -11.9f, 1.3f, -8.7f);
            var parcels = Shuffle(Parcels());
            var cartons = Shuffle(Cartons());
            var cursorParcels = 0;
            var cursorCartons = 0;
            BuildDeliveryHeaps(parcels, cartons, ref cursorParcels, ref cursorCartons);
            var totes = BuildTotes();
            DropIntoTotes(totes, parcels, ref cursorParcels);
            ScatterRemaining(parcels, cursorParcels, FloorArea, 0.42f);
            ScatterRemaining(cartons, cursorCartons, FloorArea, 0.55f);
            BuildCrates();
            BuildTools();
            BuildHazmat();
        }

        static List<string> Parcels()
        {
            var ids = new List<string>();
            foreach (var color in TidyWarehouseArt.LabelColors)
            {
                for (var copy = 0; copy < 5; copy++)
                {
                    ids.Add(TidyWarehouseContent.ParcelId(color, "s"));
                    ids.Add(TidyWarehouseContent.ParcelId(color, "m"));
                }
            }

            return ids;
        }

        static List<string> Cartons()
        {
            var ids = new List<string>();
            Add(ids, TidyWarehouseContent.CartonId("s", "kraft"), 6);
            Add(ids, TidyWarehouseContent.CartonId("s", "white"), 6);
            Add(ids, TidyWarehouseContent.CartonId("m", "kraft"), 5);
            Add(ids, TidyWarehouseContent.CartonId("m", "white"), 4);
            Add(ids, TidyWarehouseContent.CartonId("l", "kraft"), 3);
            Add(ids, TidyWarehouseContent.CartonId("l", "white"), 3);
            return ids;
        }

        static void Add(List<string> ids, string id, int count)
        {
            for (var index = 0; index < count; index++)
            {
                ids.Add(id);
            }
        }

        static List<string> Shuffle(List<string> ids)
        {
            return new List<string>(TidySceneKit.Shuffle(ids, stage.Random));
        }

        static void BuildDeliveryHeaps(List<string> parcels, List<string> cartons, ref int parcelCursor, ref int cartonCursor)
        {
            var pallet = stage.Context.Prefab("WarehousePallet");
            var palletSpots = new[] { new Vector3(-4.4f, 0f, -9.5f), new Vector3(3.9f, 0f, -9.7f), new Vector3(-3.0f, 0f, -6.1f) };
            foreach (var spot in palletSpots)
            {
                TidySceneKit.Place(pallet, stage.Fixtures, spot, new Vector3(0f, stage.Next(-12f, 12f), 0f));
                Reserve(new Vector2(spot.x, spot.z), 0.8f);
                Heap(spot + new Vector3(-0.25f, 0f, 0f), TidyWarehouseFixtures.PalletHeight, 3, parcels, cartons, ref parcelCursor, ref cartonCursor);
                Heap(spot + new Vector3(0.3f, 0f, 0.1f), TidyWarehouseFixtures.PalletHeight, 2, parcels, cartons, ref parcelCursor, ref cartonCursor);
            }

            for (var heap = 0; heap < 9; heap++)
            {
                if (!TryFind(DockArea, 0.75f, out var center))
                {
                    continue;
                }

                Heap(new Vector3(center.x, 0f, center.y), 0f, 2 + stage.Random.Next(3), parcels, cartons, ref parcelCursor, ref cartonCursor);
            }
        }

        static void Heap(Vector3 center, float baseHeight, int size, List<string> parcels, List<string> cartons, ref int parcelCursor, ref int cartonCursor)
        {
            Reserve(new Vector2(center.x, center.z), 0.6f);
            var surface = baseHeight;
            for (var layer = 0; layer < size; layer++)
            {
                var useCarton = layer == 0 ? stage.Random.NextDouble() < 0.7 : stage.Random.NextDouble() < 0.35;
                string id = null;
                if (useCarton && cartonCursor < cartons.Count - 8)
                {
                    id = cartons[cartonCursor++];
                }
                else if (parcelCursor < parcels.Count - 10)
                {
                    id = parcels[parcelCursor++];
                }

                if (id == null)
                {
                    return;
                }

                var offset = new Vector3(stage.Next(-0.12f, 0.12f), 0f, stage.Next(-0.12f, 0.12f));
                var euler = layer == 0 ? new Vector3(0f, stage.Next(0f, 360f), 0f) : Tumbled();
                var instance = TidySceneDressing.PlaceObject(stage.Context.ObjectPrefab(id), stage.Clutter, center + offset, euler, surface - (layer == 0 ? 0f : 0.025f));
                surface = TidySceneDressing.TopOf(instance);
            }
        }

        static Vector3 Tumbled()
        {
            var roll = stage.Random.NextDouble() < 0.3 ? 90f : stage.Next(-8f, 8f);
            return new Vector3(stage.Next(-8f, 8f), stage.Next(0f, 360f), roll);
        }

        static List<GameObject> BuildTotes()
        {
            var totes = new List<GameObject>();
            foreach (var color in TidyWarehouseArt.LabelColors)
            {
                for (var copy = 0; copy < 2; copy++)
                {
                    var area = copy == 0 ? EastArea : DockArea;
                    if (!TryFind(area, 0.42f, out var point))
                    {
                        continue;
                    }

                    var tipped = totes.Count == 3;
                    var euler = tipped ? new Vector3(0f, stage.Next(0f, 360f), 90f) : new Vector3(0f, stage.Next(0f, 360f), 0f);
                    totes.Add(TidySceneDressing.PlaceObject(stage.Context.ObjectPrefab(TidyWarehouseContent.ToteId(color)), stage.Clutter, new Vector3(point.x, 0f, point.y), euler, 0f));
                }
            }

            return totes;
        }

        static void DropIntoTotes(List<GameObject> totes, List<string> parcels, ref int parcelCursor)
        {
            var filled = 0;
            foreach (var tote in totes)
            {
                if (filled >= 2 || parcelCursor >= parcels.Count || Mathf.Abs(tote.transform.eulerAngles.z) > 1f)
                {
                    continue;
                }

                var id = parcels[parcelCursor];
                if (!id.EndsWith("_s"))
                {
                    continue;
                }

                parcels.RemoveAt(parcelCursor);
                TidySceneDressing.PlaceObject(stage.Context.ObjectPrefab(id), stage.Clutter, tote.transform.position, new Vector3(0f, tote.transform.eulerAngles.y + 12f, 0f), 0.026f);
                filled++;
            }
        }

        static void ScatterRemaining(List<string> ids, int start, Rect area, float radius)
        {
            for (var index = start; index < ids.Count; index++)
            {
                if (!TryFind(area, radius, out var point))
                {
                    TryFind(DockArea, radius * 0.6f, out point);
                }

                var euler = stage.Random.NextDouble() < 0.25 ? new Vector3(0f, stage.Next(0f, 360f), 90f) : new Vector3(0f, stage.Next(0f, 360f), 0f);
                TidySceneDressing.PlaceObject(stage.Context.ObjectPrefab(ids[index]), stage.Clutter, new Vector3(point.x, 0f, point.y), euler, 0f);
            }
        }

        static void BuildCrates()
        {
            var pairs = new[] { ("wh_crate_parts_large", "wh_crate_fragile_small"), ("wh_crate_fragile_large", "wh_crate_parts_small") };
            foreach (var (bottom, top) in pairs)
            {
                if (!TryFind(AisleArea, 0.75f, out var point))
                {
                    continue;
                }

                var yaw = stage.Next(-25f, 25f) + 90f;
                var lower = TidySceneDressing.PlaceObject(stage.Context.ObjectPrefab(bottom), stage.Clutter, new Vector3(point.x, 0f, point.y), new Vector3(0f, yaw, 0f), 0f);
                TidySceneDressing.PlaceObject(stage.Context.ObjectPrefab(top), stage.Clutter, new Vector3(point.x + 0.06f, 0f, point.y - 0.04f), new Vector3(stage.Next(-6f, 6f), yaw + stage.Next(-30f, 30f), stage.Next(-6f, 6f)), TidySceneDressing.TopOf(lower) - 0.01f);
            }

            var singles = new List<string>();
            Add(singles, "wh_crate_fragile_small", 3);
            Add(singles, "wh_crate_fragile_large", 2);
            Add(singles, "wh_crate_parts_small", 4);
            Add(singles, "wh_crate_parts_large", 2);
            for (var index = 0; index < singles.Count; index++)
            {
                var area = index % 3 == 2 ? DockArea : AisleArea;
                if (!TryFind(area, 0.6f, out var point))
                {
                    TryFind(FloorArea, 0.6f, out point);
                }

                var tipped = index == 4 || index == 8;
                var euler = tipped ? new Vector3(0f, stage.Next(0f, 360f), 90f) : new Vector3(0f, stage.Next(0f, 360f), 0f);
                TidySceneDressing.PlaceObject(stage.Context.ObjectPrefab(singles[index]), stage.Clutter, new Vector3(point.x, 0f, point.y), euler, 0f);
            }
        }

        static void BuildTools()
        {
            var bench = TidyWarehouseZones.BenchCenter;
            var benchSpots = new[] { new Vector3(-0.55f, 0f, -0.12f), new Vector3(0.05f, 0f, 0.1f), new Vector3(0.6f, 0f, -0.05f) };
            for (var index = 0; index < TidyWarehouseContent.ToolIds.Length; index++)
            {
                var prefab = stage.Context.ObjectPrefab(TidyWarehouseContent.ToolIds[index]);
                if (index < benchSpots.Length)
                {
                    TidySceneDressing.PlaceObject(prefab, stage.Clutter, bench + benchSpots[index], new Vector3(0f, stage.Next(0f, 360f), 0f), TidyWarehouseFixtures.BenchHeight);
                    continue;
                }

                var area = index % 2 == 0 ? NorthArea : DockArea;
                if (!TryFind(area, 0.3f, out var point))
                {
                    TryFind(FloorArea, 0.3f, out point);
                }

                TidySceneDressing.PlaceObject(prefab, stage.Clutter, new Vector3(point.x, 0f, point.y), new Vector3(0f, stage.Next(0f, 360f), 0f), 0f);
            }
        }

        static void BuildHazmat()
        {
            var drums = new[] { "wh_drum_blue", "wh_drum_red", "wh_drum_blue", "wh_drum_red", "wh_drum_blue", "wh_drum_red" };
            for (var index = 0; index < drums.Length; index++)
            {
                var toppled = index % 2 == 1;
                var area = index < 3 ? EastArea : NorthArea;
                if (!TryFind(area, toppled ? 0.7f : 0.5f, out var point))
                {
                    TryFind(FloorArea, 0.5f, out point);
                }

                var euler = toppled ? new Vector3(0f, stage.Next(0f, 360f), 90f) : new Vector3(0f, stage.Next(0f, 360f), 0f);
                TidySceneDressing.PlaceObject(stage.Context.ObjectPrefab(drums[index]), stage.Clutter, new Vector3(point.x, 0f, point.y), euler, 0f);
            }

            var cans = new[] { "wh_canister_red", "wh_canister_green", "wh_canister_red", "wh_canister_green" };
            for (var index = 0; index < cans.Length; index++)
            {
                if (!TryFind(index < 2 ? NorthArea : DockArea, 0.35f, out var point))
                {
                    TryFind(FloorArea, 0.35f, out point);
                }

                var euler = index == 1 ? new Vector3(0f, stage.Next(0f, 360f), 90f) : new Vector3(0f, stage.Next(0f, 360f), 0f);
                TidySceneDressing.PlaceObject(stage.Context.ObjectPrefab(cans[index]), stage.Clutter, new Vector3(point.x, 0f, point.y), euler, 0f);
            }
        }

        static bool TryFind(Rect area, float radius, out Vector2 point)
        {
            for (var attempt = 0; attempt < 300; attempt++)
            {
                var candidate = new Vector2(stage.Next(area.xMin + radius, area.xMax - radius), stage.Next(area.yMin + radius, area.yMax - radius));
                if (IsFree(candidate, radius))
                {
                    Reserve(candidate, radius);
                    point = candidate;
                    return true;
                }
            }

            point = area.center;
            return false;
        }

        static bool IsFree(Vector2 candidate, float radius)
        {
            foreach (var rect in stage.Blocked)
            {
                var expanded = Rect.MinMaxRect(rect.xMin - radius, rect.yMin - radius, rect.xMax + radius, rect.yMax + radius);
                if (expanded.Contains(candidate))
                {
                    return false;
                }
            }

            foreach (var spot in Occupied)
            {
                var minimum = spot.Radius + radius;
                if ((spot.Center - candidate).sqrMagnitude < minimum * minimum)
                {
                    return false;
                }
            }

            return true;
        }

        static void Reserve(Vector2 center, float radius)
        {
            Occupied.Add(new Spot { Center = center, Radius = radius });
        }
    }
}
