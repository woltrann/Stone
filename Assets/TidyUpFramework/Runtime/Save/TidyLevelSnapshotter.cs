using System;
using System.Collections.Generic;
using TidyUpFramework.Cleaning;
using TidyUpFramework.Interaction;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Player;
using UnityEngine;

namespace TidyUpFramework.Save
{
    public static class TidyLevelSnapshotter
    {
        const float GroundProbeHeight = 0.5f;
        const float GroundProbeDistance = 20f;

        static readonly RaycastHit[] GroundHits = new RaycastHit[16];

        public static void CaptureWorld(TidyLevelSnapshot snapshot, TidyInteractor interactor, TidyFirstPersonController player)
        {
            snapshot.Objects.Clear();
            snapshot.Cleanables.Clear();
            var ids = BuildIds();
            var captured = new HashSet<TidyObject>();

            foreach (var target in TidyTargetRegistry.All)
            {
                if (target == null)
                {
                    continue;
                }

                var occupants = target.Occupants;
                for (var order = 0; order < occupants.Count; order++)
                {
                    var tidyObject = occupants[order];
                    if (tidyObject == null || !ids.TryGetValue(tidyObject, out var id) || !captured.Add(tidyObject))
                    {
                        continue;
                    }

                    var entry = CreateEntry(tidyObject, id, TidyObjectState.Placed);
                    entry.TargetId = target.TargetId;
                    entry.TargetOrder = order;
                    entry.HasPose = tidyObject.State == TidyObjectState.Placed || tidyObject.State == TidyObjectState.Consumed;
                    snapshot.Objects.Add(entry);
                }
            }

            if (interactor != null)
            {
                var stacked = interactor.StackedObjects;
                for (var index = 0; index < stacked.Count; index++)
                {
                    AddCarried(snapshot, ids, captured, stacked[index], index);
                }

                AddCarried(snapshot, ids, captured, interactor.HeldObject, stacked.Count);
            }

            foreach (var pair in ids)
            {
                var tidyObject = pair.Key;
                if (!captured.Add(tidyObject))
                {
                    continue;
                }

                switch (tidyObject.State)
                {
                    case TidyObjectState.Consumed:
                        snapshot.Objects.Add(CreateEntry(tidyObject, pair.Value, TidyObjectState.Consumed));
                        break;
                    case TidyObjectState.Moving:
                    case TidyObjectState.Held:
                    case TidyObjectState.Inspecting:
                        var settled = CreateEntry(tidyObject, pair.Value, TidyObjectState.Loose);
                        settled.Position = GroundedPivot(tidyObject, out settled.Rotation);
                        snapshot.Objects.Add(settled);
                        break;
                    default:
                        snapshot.Objects.Add(CreateEntry(tidyObject, pair.Value, TidyObjectState.Loose));
                        break;
                }
            }

            foreach (var cleanable in TidyCleanable.All)
            {
                if (cleanable != null)
                {
                    snapshot.Cleanables.Add(new TidyCleanableSnapshot { Id = cleanable.CleanableId, Dirt = cleanable.Dirt });
                }
            }

            snapshot.HasPlayer = player != null;
            if (player != null)
            {
                snapshot.PlayerPosition = player.transform.position;
                snapshot.PlayerYaw = player.Yaw;
                snapshot.PlayerPitch = player.Pitch;
            }
        }

        public static void RestoreWorld(TidyLevelSnapshot snapshot, TidyInteractor interactor, TidyFirstPersonController player)
        {
            var byId = new Dictionary<string, TidyObject>();
            foreach (var pair in BuildIds())
            {
                byId[pair.Value] = pair.Key;
            }

            if (interactor != null)
            {
                interactor.DropAll();
            }

            foreach (var entry in snapshot.Objects)
            {
                if (entry != null && byId.TryGetValue(entry.Id, out var tidyObject) && tidyObject.CurrentTarget != null)
                {
                    tidyObject.CurrentTarget.Release(tidyObject);
                }
            }

            var placed = new List<TidyObjectSnapshot>();
            var carried = new List<TidyObjectSnapshot>();
            foreach (var entry in snapshot.Objects)
            {
                if (entry == null || !byId.TryGetValue(entry.Id, out var tidyObject))
                {
                    continue;
                }

                tidyObject.RestoreProgressFlags(entry.PickedUp, entry.Mistakes);
                var state = (TidyObjectState)entry.State;
                if (!string.IsNullOrEmpty(entry.TargetId))
                {
                    placed.Add(entry);
                    continue;
                }

                if (state == TidyObjectState.Consumed)
                {
                    tidyObject.MarkConsumed();
                    tidyObject.gameObject.SetActive(false);
                    continue;
                }

                tidyObject.gameObject.SetActive(true);
                tidyObject.MarkLoose();
                SetPose(tidyObject, entry.Position, entry.Rotation);
                if (entry.CarryOrder >= 0)
                {
                    carried.Add(entry);
                }
            }

            placed.Sort((first, second) => first.TargetOrder.CompareTo(second.TargetOrder));
            foreach (var entry in placed)
            {
                RestorePlacement(entry, byId[entry.Id]);
            }

            foreach (var entry in snapshot.Cleanables)
            {
                var cleanable = FindCleanable(entry.Id);
                if (cleanable != null)
                {
                    cleanable.RestoreDirt(entry.Dirt);
                }
            }

            if (player != null && snapshot.HasPlayer)
            {
                player.Teleport(snapshot.PlayerPosition, snapshot.PlayerYaw, snapshot.PlayerPitch);
            }

            if (interactor != null && carried.Count > 0)
            {
                carried.Sort((first, second) => first.CarryOrder.CompareTo(second.CarryOrder));
                interactor.ForcePickUp(byId[carried[0].Id]);
                for (var index = 1; index < carried.Count; index++)
                {
                    if (!interactor.TryAddToStack(byId[carried[index].Id]))
                    {
                        break;
                    }
                }
            }
        }

        public static Dictionary<TidyObject, string> BuildIds()
        {
            var result = new Dictionary<TidyObject, string>();
            var counts = new Dictionary<string, int>();
            foreach (var tidyObject in TidyObjectRegistry.All)
            {
                if (tidyObject == null)
                {
                    continue;
                }

                var id = tidyObject.InstanceId;
                counts.TryGetValue(id, out var seen);
                counts[id] = seen + 1;
                result[tidyObject] = seen == 0 ? id : id + "#" + seen;
            }

            return result;
        }

        static void AddCarried(TidyLevelSnapshot snapshot, Dictionary<TidyObject, string> ids, HashSet<TidyObject> captured, TidyObject tidyObject, int order)
        {
            if (tidyObject == null || !ids.TryGetValue(tidyObject, out var id) || !captured.Add(tidyObject))
            {
                return;
            }

            var entry = CreateEntry(tidyObject, id, TidyObjectState.Loose);
            entry.Position = GroundedPivot(tidyObject, out entry.Rotation);
            entry.CarryOrder = order;
            snapshot.Objects.Add(entry);
        }

        static TidyObjectSnapshot CreateEntry(TidyObject tidyObject, string id, TidyObjectState state)
        {
            var transform = tidyObject.transform;
            return new TidyObjectSnapshot
            {
                Id = id,
                State = (int)state,
                Position = transform.position,
                Rotation = transform.rotation,
                PickedUp = tidyObject.HasBeenPickedUp,
                Mistakes = tidyObject.MistakeCount
            };
        }

        static void RestorePlacement(TidyObjectSnapshot entry, TidyObject tidyObject)
        {
            var target = TidyTargetRegistry.FindById(entry.TargetId);
            if (target == null)
            {
                tidyObject.MarkLoose();
                SetPose(tidyObject, entry.Position, entry.Rotation);
                return;
            }

            tidyObject.gameObject.SetActive(true);
            tidyObject.MarkLoose();
            var query = entry.HasPose
                ? new TidyPlacementQuery(tidyObject, entry.Position, Vector3.up, entry.Rotation.eulerAngles.y)
                : TidyPlacementQuery.WithoutAim(tidyObject);
            var candidate = target.Evaluate(query);
            if (!candidate.IsValid && candidate.HasPose && candidate.Rejection == TidyRejectionReason.WrongObject)
            {
                candidate.IsValid = true;
            }

            if (!target.TryPlace(candidate, false))
            {
                SetPose(tidyObject, entry.Position, entry.Rotation);
                return;
            }

            if (entry.HasPose && tidyObject.State == TidyObjectState.Placed)
            {
                tidyObject.transform.SetPositionAndRotation(entry.Position, entry.Rotation);
            }
        }

        static void SetPose(TidyObject tidyObject, Vector3 position, Quaternion rotation)
        {
            tidyObject.transform.SetPositionAndRotation(position, rotation);
            var body = tidyObject.Body;
            if (body != null && !body.isKinematic)
            {
                body.position = position;
                body.rotation = rotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.Sleep();
            }
        }

        static Vector3 GroundedPivot(TidyObject tidyObject, out Quaternion rotation)
        {
            rotation = Quaternion.Euler(0f, tidyObject.transform.eulerAngles.y, 0f);
            var center = tidyObject.CalculateWorldBounds().center;
            var ray = new Ray(center + Vector3.up * GroundProbeHeight, Vector3.down);
            var count = Physics.RaycastNonAlloc(ray, GroundHits, GroundProbeDistance, ~0, QueryTriggerInteraction.Ignore);
            var bestDistance = float.MaxValue;
            var found = false;
            var ground = center;
            for (var index = 0; index < count; index++)
            {
                var hit = GroundHits[index];
                if (hit.collider.GetComponentInParent<CharacterController>() != null)
                {
                    continue;
                }

                if (TidyObjectRegistry.TryGetFromCollider(hit.collider, out var other) && (other == tidyObject || (other.State != TidyObjectState.Loose && other.State != TidyObjectState.Placed)))
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    ground = hit.point;
                    found = true;
                }
            }

            return found ? tidyObject.PivotPositionForSurface(ground, rotation) : tidyObject.transform.position;
        }

        static TidyCleanable FindCleanable(string id)
        {
            foreach (var cleanable in TidyCleanable.All)
            {
                if (cleanable != null && cleanable.CleanableId == id)
                {
                    return cleanable;
                }
            }

            return null;
        }

        public static string UtcNow()
        {
            return DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
