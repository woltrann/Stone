using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using TidyUpFramework.Transformation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static partial class TidyStoreScene
    {
        public const int CopiesPerPal = 10;
        public const int CopiesPerPod = 12;
        static readonly Vector3 HeapCenter = new Vector3(0f, 0f, -1.3f);
        const float HeapRadius = 0.85f;
        const float HeapPeak = 0.58f;
        const float HeapCell = 0.045f;
        const int CounterSpill = 10;
        const int TableSpill = 14;
        const int FloorSpill = 24;
        const int IslandSpill = 8;
        const int PodsInHeap = 24;

        static float[,] heapHeights;

        static void BuildClutter()
        {
            var pals = new List<GameObject>();
            foreach (var species in TidyStoreContent.Species)
            {
                foreach (var colorTag in TidyStoreContent.Colors)
                {
                    var prefab = context.ObjectPrefab(TidyStoreContent.PalId(species, colorTag));
                    for (var copy = 0; copy < CopiesPerPal; copy++)
                    {
                        pals.Add(prefab);
                    }
                }
            }

            var pods = new List<GameObject>();
            foreach (var colorTag in TidyStoreContent.PodColors)
            {
                var prefab = context.ObjectPrefab(TidyStoreContent.PodId(colorTag));
                for (var copy = 0; copy < CopiesPerPod; copy++)
                {
                    pods.Add(prefab);
                }
            }

            var palQueue = new Queue<GameObject>(TidySceneKit.Shuffle(pals, random));
            var podQueue = new Queue<GameObject>(TidySceneKit.Shuffle(pods, random));
            SpillOnCounter(palQueue, podQueue);
            SpillOnTable(palQueue);
            SpillOnIslands(palQueue);
            SpillOnFloor(palQueue, podQueue);
            var heap = new List<GameObject>(palQueue);
            heap.AddRange(podQueue);
            BuildHeap(new List<GameObject>(TidySceneKit.Shuffle(heap, random)));
        }

        static void SpillOnCounter(Queue<GameObject> pals, Queue<GameObject> pods)
        {
            var counter = new Rect(-0.4f, -0.22f, 0.75f, 0.44f);
            for (var index = 0; index < CounterSpill + 4; index++)
            {
                var queue = index < CounterSpill ? pals : pods;
                var local = new Vector3(Mathf.Lerp(counter.xMin, counter.xMax, Next()), 0f, Mathf.Lerp(counter.yMin, counter.yMax, Next()));
                var world = CounterPosition + Quaternion.Euler(0f, 90f, 0f) * local;
                PlaceLoose(queue.Dequeue(), world, TidyStoreFixtures.CounterHeight);
            }
        }

        static void SpillOnTable(Queue<GameObject> pals)
        {
            for (var index = 0; index < TableSpill; index++)
            {
                var world = SideTablePosition + new Vector3(Mathf.Lerp(-1.05f, 1.05f, Next()), 0f, Mathf.Lerp(-0.3f, 0.05f, Next()));
                PlaceLoose(pals.Dequeue(), world, TidyStoreFixtures.TableHeight);
            }
        }

        static void SpillOnIslands(Queue<GameObject> pals)
        {
            for (var island = 0; island < IslandPositions.Length; island++)
            {
                var rotation = Quaternion.Euler(0f, IslandYaws[island], 0f);
                for (var index = 0; index < IslandSpill; index++)
                {
                    var local = new Vector3(Mathf.Lerp(-1.05f, 1.05f, Next()), 0f, Mathf.Lerp(-0.3f, -0.08f, Next()));
                    PlaceLoose(pals.Dequeue(), IslandPositions[island] + rotation * local, TidyStoreFixtures.TableHeight);
                }
            }
        }

        static void SpillOnFloor(Queue<GameObject> pals, Queue<GameObject> pods)
        {
            var exclusions = new List<Rect>
            {
                new Rect(HeapCenter.x - HeapRadius, HeapCenter.z - HeapRadius, HeapRadius * 2f, HeapRadius * 2f),
                new Rect(StandCenter.x - 1.15f, StandCenter.z - 1.15f, 2.3f, 2.3f),
                new Rect(CounterPosition.x - 0.6f, CounterPosition.z - 1.35f, 1.2f, 2.7f),
                new Rect(PedestalX[0] - 0.5f, PedestalZ - 0.5f, 3.4f, 1.0f),
                new Rect(2.0f, -6.8f, 4.3f, 1.4f),
                new Rect(IslandPositions[0].x - 0.5f, IslandPositions[0].z - 1.3f, 1.0f, 2.6f),
                new Rect(IslandPositions[1].x - 0.5f, IslandPositions[1].z - 1.3f, 1.0f, 2.6f),
                new Rect(TripletTablePosition.x - 1.3f, TripletTablePosition.z - 0.5f, 2.6f, 1.0f),
                new Rect(SideTablePosition.x - 1.3f, SideTablePosition.z - 0.5f, 2.6f, 1.0f),
                new Rect(-0.9f, -7.7f, 1.8f, 1.9f),
                new Rect(-3.0f, -5.0f, 1.2f, 1.2f)
            };
            var area = new Rect(-5.9f, -7.1f, 11.8f, 13.2f);
            var points = TidySceneDressing.ScatterPoints(random, area, FloorSpill, 0.5f, exclusions, 0f);
            foreach (var point in points)
            {
                PlaceLoose(pals.Dequeue(), point, 0f);
            }

            var binArea = new Rect(1.8f, -5.35f, 4.6f, 1.15f);
            var podPoints = TidySceneDressing.ScatterPoints(random, binArea, pods.Count - PodsInHeap, 0.18f, null, 0f);
            foreach (var point in podPoints)
            {
                PlaceLoose(pods.Dequeue(), point, 0f);
            }
        }

        static void PlaceLoose(GameObject prefab, Vector3 position, float surface)
        {
            var lying = Next() < 0.45f;
            var euler = lying ? new Vector3(0f, Next() * 360f, Next() < 0.5f ? 90f : -90f) : new Vector3(0f, Next() * 360f, 0f);
            if (lying && Next() < 0.4f)
            {
                euler = new Vector3(Next() < 0.5f ? 90f : -90f, Next() * 360f, 0f);
            }

            TidySceneDressing.PlaceObject(prefab, clutter, position, euler, surface);
        }

        static void BuildHeap(List<GameObject> items)
        {
            var cells = Mathf.CeilToInt(HeapRadius * 2.6f / HeapCell);
            heapHeights = new float[cells, cells];
            var heapRoot = TidySceneKit.Group("Pal Heap", clutter);
            foreach (var prefab in items)
            {
                var point = PickHeapPoint();
                var euler = new Vector3(Mathf.Lerp(-85f, 85f, Next()), Next() * 360f, Mathf.Lerp(-85f, 85f, Next()));
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, heapRoot);
                instance.transform.SetPositionAndRotation(point, Quaternion.Euler(euler));
                var bounds = BoundsOf(instance);
                var surface = Mathf.Max(0f, SampleHeap(bounds, 0.15f) - 0.01f);
                TidySceneDressing.Settle(instance, surface);
                StampHeap(BoundsOf(instance));
            }
        }

        static Vector3 PickHeapPoint()
        {
            var best = HeapCenter;
            var bestScore = float.MaxValue;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var angle = Next() * Mathf.PI * 2f;
                var radius = HeapRadius * Mathf.Sqrt(Next());
                var candidate = HeapCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var falloff = 1f - radius / HeapRadius;
                var target = HeapPeak * falloff * falloff * (3f - 2f * falloff);
                var score = SampleHeap(new Bounds(candidate, new Vector3(0.1f, 0.1f, 0.1f)), 0f) - target + Next() * 0.03f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        static Bounds BoundsOf(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        static Vector2Int Cell(Vector3 world)
        {
            var size = heapHeights.GetLength(0);
            var x = Mathf.FloorToInt((world.x - HeapCenter.x) / HeapCell + size * 0.5f);
            var z = Mathf.FloorToInt((world.z - HeapCenter.z) / HeapCell + size * 0.5f);
            return new Vector2Int(Mathf.Clamp(x, 0, size - 1), Mathf.Clamp(z, 0, size - 1));
        }


        static float SampleHeap(Bounds bounds, float shrink)
        {
            var min = Cell(bounds.center - new Vector3(bounds.extents.x, 0f, bounds.extents.z) * (1f - shrink));
            var max = Cell(bounds.center + new Vector3(bounds.extents.x, 0f, bounds.extents.z) * (1f - shrink));
            var total = 0f;
            var highest = 0f;
            var count = 0;
            for (var x = min.x; x <= max.x; x++)
            {
                for (var z = min.y; z <= max.y; z++)
                {
                    total += heapHeights[x, z];
                    highest = Mathf.Max(highest, heapHeights[x, z]);
                    count++;
                }
            }

            return Mathf.Lerp(total / Mathf.Max(1, count), highest, 0.35f);
        }

        static void StampHeap(Bounds bounds)
        {
            var min = Cell(bounds.center - new Vector3(bounds.extents.x, 0f, bounds.extents.z) * 0.8f);
            var max = Cell(bounds.center + new Vector3(bounds.extents.x, 0f, bounds.extents.z) * 0.8f);
            var top = bounds.max.y - 0.03f;
            for (var x = min.x; x <= max.x; x++)
            {
                for (var z = min.y; z <= max.y; z++)
                {
                    heapHeights[x, z] = Mathf.Max(heapHeights[x, z], top);
                }
            }
        }

        static float Next()
        {
            return (float)random.NextDouble();
        }

        static void BuildBeforeAfter((Volume messy, Volume organised) volumes, List<Light> lampLights)
        {
            var controllerObject = new GameObject("Before After");
            var controller = controllerObject.AddComponent<TidyTransformationController>();
            var mood = new GameObject("Mood");
            mood.transform.SetParent(controllerObject.transform, false);
            mood.AddComponent<TidyVolumeReactor>().Configure(volumes.messy, volumes.organised);
            foreach (var light in lampLights)
            {
                light.gameObject.AddComponent<TidyLightReactor>().Configure(2.2f, 3.6f, new Color(0.9f, 0.82f, 0.74f), LampWarm);
            }

            var mess = BuildMess(controllerObject.transform);
            var festive = BuildFestiveDecor(controllerObject.transform);
            var sparkles = BuildSparkles(controllerObject.transform);
            HideUntilOrganised(festive, sparkles);
            var toggle = new GameObject("Festive Toggle").AddComponent<TidyToggleReactor>();
            toggle.transform.SetParent(controllerObject.transform, false);
            toggle.Configure(new[] { mess.crate }, festive.ToArray(), 0.6f);
            var reveal = new GameObject("Sparkles And Scraps").AddComponent<TidyGradualRevealReactor>();
            reveal.transform.SetParent(controllerObject.transform, false);
            reveal.Configure(sparkles.ToArray(), mess.scraps.ToArray());
            var reactors = new List<TidyProgressReactor>(controllerObject.GetComponentsInChildren<TidyProgressReactor>(true));
            foreach (var light in lampLights)
            {
                reactors.Add(light.GetComponent<TidyLightReactor>());
            }

            TidySerialization.SetArray(controller, "reactors", reactors);
        }

        static void HideUntilOrganised(List<GameObject> festive, List<Transform> sparkles)
        {
            foreach (var item in festive)
            {
                item.SetActive(false);
            }

            foreach (var item in sparkles)
            {
                item.gameObject.SetActive(false);
            }
        }

        static (GameObject crate, List<Transform> scraps) BuildMess(Transform parent)
        {
            var root = TidySceneKit.Group("Mess", parent);
            var crateBuilder = new TidyMeshBuilder();
            crateBuilder.AddBox(new Vector3(0f, 0.25f, -0.3f), new Vector3(0.9f, 0.5f, 0.02f));
            crateBuilder.AddBox(new Vector3(-0.44f, 0.25f, 0f), new Vector3(0.02f, 0.5f, 0.6f));
            crateBuilder.AddBox(new Vector3(0.44f, 0.25f, 0f), new Vector3(0.02f, 0.5f, 0.6f));
            crateBuilder.AddBox(new Vector3(0f, 0.01f, 0f), new Vector3(0.9f, 0.02f, 0.6f));
            crateBuilder.AddBox(Vector3.zero, new Vector3(0.9f, 0.02f, 0.3f), 0, TidyMeshBuilder.TRS(new Vector3(0f, 0.5f, -0.43f), new Vector3(-35f, 0f, 0f), Vector3.one));
            crateBuilder.AddBox(Vector3.zero, new Vector3(0.3f, 0.02f, 0.6f), 0, TidyMeshBuilder.TRS(new Vector3(-0.58f, 0.46f, 0f), new Vector3(0f, 0f, 40f), Vector3.one));
            crateBuilder.AddBox(Vector3.zero, new Vector3(0.3f, 0.02f, 0.6f), 0, TidyMeshBuilder.TRS(new Vector3(0.6f, 0.44f, 0f), new Vector3(0f, 0f, -48f), Vector3.one));
            var crateMesh = TidyMeshUtility.SaveAsIs(crateBuilder, "SM_StoreBurstCrate", meshFolder);
            var crate = TidySceneDressing.Decor("Burst Delivery Crate", root, crateMesh, new[] { context.Material("Cardboard") }, HeapCenter + new Vector3(0.55f, 0f, 1.25f), Quaternion.Euler(0f, 160f, 0f));
            TidyPropFactory.AddCollider(crate, crateMesh, TidyColliderShape.Box);

            var scrapBuilder = new TidyMeshBuilder();
            scrapBuilder.AddBox(new Vector3(0f, 0.004f, 0f), new Vector3(0.34f, 0.008f, 0.26f));
            var scrapMesh = TidyMeshUtility.SaveAsIs(scrapBuilder, "SM_StoreCardScrap", meshFolder);
            var scraps = new List<Transform>();
            var area = new Rect(-5.2f, -6.2f, 10.4f, 11f);
            foreach (var point in TidySceneDressing.ScatterPoints(random, area, 12, 1.2f, new List<Rect> { new Rect(-1.2f, 3.2f, 2.4f, 2.4f) }, 0f))
            {
                var scrap = TidySceneDressing.Decor("Cardboard Scrap", root, scrapMesh, new[] { context.Material("Cardboard") }, point + Vector3.up * 0.001f, Quaternion.Euler(0f, Next() * 360f, 0f));
                scrap.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                scraps.Add(scrap.transform);
            }

            return (crate, scraps);
        }

        static List<GameObject> BuildFestiveDecor(Transform parent)
        {
            var festive = new List<GameObject>();
            var buntingColours = new[] { "Red", "Yellow", "Teal", "Pink", "Blue", "Green" };
            var buntingMaterials = new Material[buntingColours.Length + 1];
            for (var index = 0; index < buntingColours.Length; index++)
            {
                buntingMaterials[index] = context.Material("Matte_" + buntingColours[index]);
            }

            buntingMaterials[buntingColours.Length] = context.Material("Store_Cream");
            var runs = new[]
            {
                (new Vector3(-6.9f, 3.55f, -5f), new Vector3(6.9f, 3.55f, -1.5f)),
                (new Vector3(-6.9f, 3.55f, -1.5f), new Vector3(6.9f, 3.55f, -5f)),
                (new Vector3(-6.9f, 3.55f, 2.5f), new Vector3(6.9f, 3.55f, 6f)),
                (new Vector3(-6.9f, 3.55f, 6f), new Vector3(6.9f, 3.55f, 2.5f))
            };
            for (var index = 0; index < runs.Length; index++)
            {
                var (start, end) = runs[index];
                var delta = end - start;
                var length = delta.magnitude;
                var mesh = TidyStoreFixtures.Bunting(meshFolder, length, 0.45f, buntingColours.Length, out _);
                var yaw = -Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
                var bunting = TidySceneDressing.Decor("Bunting", parent, mesh, buntingMaterials, start, Quaternion.Euler(0f, yaw, 0f));
                bunting.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                festive.Add(bunting);
            }

            var balloonMesh = TidyStoreFixtures.Balloon(meshFolder);
            var clusters = new[] { new Vector3(-3.6f, 0f, -5.4f), new Vector3(1.4f, 0f, 3.2f), new Vector3(-1.4f, 0f, 3.2f), new Vector3(1.9f, 0f, -4.4f) };
            var balloonColours = new[] { "Pink", "Yellow", "Teal", "Purple", "Red", "Blue" };
            var clusterIndex = 0;
            foreach (var anchor in clusters)
            {
                var cluster = TidySceneKit.Group("Balloons", parent);
                cluster.position = anchor;
                for (var index = 0; index < 5; index++)
                {
                    var offset = Quaternion.Euler(0f, index * 72f + clusterIndex * 20f, 0f) * new Vector3(0.18f, 0f, 0f);
                    var height = 1.55f + (index % 3) * 0.16f;
                    var colour = balloonColours[(index + clusterIndex) % balloonColours.Length];
                    var balloon = TidySceneDressing.Decor("Balloon", cluster, balloonMesh, new[] { context.Material("Plastic_" + colour), context.Material("White") }, offset * (index == 0 ? 0f : 1f) + Vector3.up * height, Quaternion.Euler(Mathf.Lerp(-8f, 8f, Next()), 0f, Mathf.Lerp(-8f, 8f, Next())));
                    balloon.AddComponent<TidyFloatingMotion>();
                }

                festive.Add(cluster.gameObject);
                clusterIndex++;
            }

            return festive;
        }

        static List<Transform> BuildSparkles(Transform parent)
        {
            var root = TidySceneKit.Group("Sparkles", parent);
            var sparkles = new List<Transform>();
            var bigStar = TidyStoreFixtures.Star(meshFolder, 0.18f, 0.04f);
            var smallStar = TidyStoreFixtures.Star(meshFolder, 0.06f, 0.015f);
            var crown = TidySceneDressing.Decor("Stand Star", root, bigStar, new[] { context.Material("Store_Sparkle") }, StandCenter + new Vector3(0f, 2.12f, 0f), Quaternion.identity);
            crown.AddComponent<TidyFloatingMotion>();
            sparkles.Add(crown.transform);
            var spots = new List<Vector3>();
            for (var index = 0; index < 5; index++)
            {
                spots.Add(ShelfSparkleSpot(index));
            }

            foreach (var point in TidySceneDressing.ScatterPoints(random, new Rect(-5f, -5.5f, 10f, 11f), 18, 1.4f, null, 0f))
            {
                spots.Add(point + Vector3.up * (2.2f + Next() * 1.1f));
            }

            foreach (var spot in spots)
            {
                var star = TidySceneDressing.Decor("Sparkle", root, smallStar, new[] { context.Material("Store_Sparkle") }, spot, Quaternion.Euler(0f, Next() * 360f, 0f));
                star.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                star.AddComponent<TidyFloatingMotion>();
                sparkles.Add(star.transform);
            }

            return sparkles;
        }

        static Vector3 ShelfSparkleSpot(int index)
        {
            switch (index)
            {
                case 0:
                    return new Vector3(LeftWallX + 0.3f, 3.25f, -1.05f + 0.75f);
                case 1:
                    return new Vector3(LeftWallX + 0.3f, 3.25f, 1.05f - 0.75f);
                case 2:
                    return new Vector3(RightWallX - 0.3f, 3.25f, 1.05f - 0.75f);
                case 3:
                    return new Vector3(RightWallX - 0.3f, 3.25f, -1.05f + 0.75f);
                default:
                    return new Vector3(0.75f, 3.25f, BackWallZ - 0.3f);
            }
        }
    }
}
