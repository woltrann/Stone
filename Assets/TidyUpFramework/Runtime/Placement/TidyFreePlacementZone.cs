using System.Collections.Generic;
using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Placement
{
    [AddComponentMenu("Tidy Up Framework/Placement/Free Placement Zone")]
    public sealed class TidyFreePlacementZone : TidyPlacementTarget
    {
        const int SearchResolution = 8;

        [Header("Area")]
        [SerializeField, Tooltip("Width and depth of the placement surface in local X and Z. The surface height is this transform.")]
        Vector2 areaSize = new Vector2(1f, 0.6f);

        [SerializeField, Min(1)]
        int maxCount = 30;

        [SerializeField, Min(0f), Tooltip("Snaps positions to a grid of this size. Zero disables position snapping.")]
        float gridSnap;

        [SerializeField, Min(0f), Tooltip("Snaps the player's rotation to this angle step. Zero keeps free rotation.")]
        float rotationSnapDegrees = 15f;

        [SerializeField]
        bool preventOverlap = true;

        [SerializeField, Min(0f)]
        float overlapPadding = 0.005f;

        readonly Dictionary<TidyObject, Rect> footprints = new Dictionary<TidyObject, Rect>();
        Rect pendingFootprint;

        public override int Capacity => maxCount;
        public Vector2 AreaSize => areaSize;

        public void ConfigureArea(Vector2 newAreaSize, int newMaxCount, float newGridSnap, float newRotationSnap, bool newPreventOverlap)
        {
            areaSize = newAreaSize;
            maxCount = Mathf.Max(1, newMaxCount);
            gridSnap = Mathf.Max(0f, newGridSnap);
            rotationSnapDegrees = Mathf.Max(0f, newRotationSnap);
            preventOverlap = newPreventOverlap;
        }

        protected override bool TryFindPose(TidyPlacementQuery query, out Vector3 position, out Quaternion rotation, out int slotIndex)
        {
            slotIndex = occupants.Count;
            var localYaw = Mathf.DeltaAngle(transform.eulerAngles.y, query.PreferredYaw);
            var yaw = rotationSnapDegrees > 0f ? Mathf.Round(localYaw / rotationSnapDegrees) * rotationSnapDegrees : localYaw;
            var localRotation = Quaternion.Euler(0f, yaw, 0f) * query.Object.PlacementRotationOffset;
            rotation = transform.rotation * localRotation;
            var footprintSize = RotatedSize(query.Object, localRotation);
            var footprint2D = new Vector2(footprintSize.x, footprintSize.z);

            if (query.HasAimPoint)
            {
                var local = transform.InverseTransformPoint(query.AimPoint);
                var center = ClampInside(Snap(new Vector2(local.x, local.z)), footprint2D);
                if (IsFree(center, footprint2D))
                {
                    position = PivotForBottomAt(query.Object, transform.TransformPoint(new Vector3(center.x, 0f, center.y)), rotation);
                    pendingFootprint = MakeRect(center, footprint2D);
                    return true;
                }
            }

            if (TrySearchFreeCenter(footprint2D, out var freeCenter))
            {
                position = PivotForBottomAt(query.Object, transform.TransformPoint(new Vector3(freeCenter.x, 0f, freeCenter.y)), rotation);
                pendingFootprint = MakeRect(freeCenter, footprint2D);
                return true;
            }

            position = default;
            return false;
        }

        protected override void InsertOccupant(TidyObject tidyObject, int slotIndex)
        {
            base.InsertOccupant(tidyObject, slotIndex);
            footprints[tidyObject] = pendingFootprint;
        }

        protected override void RemoveOccupant(TidyObject tidyObject)
        {
            base.RemoveOccupant(tidyObject);
            footprints.Remove(tidyObject);
        }

        bool TrySearchFreeCenter(Vector2 footprint, out Vector2 center)
        {
            for (var row = 0; row < SearchResolution; row++)
            {
                for (var column = 0; column < SearchResolution; column++)
                {
                    var candidate = new Vector2(
                        Mathf.Lerp(-areaSize.x * 0.5f, areaSize.x * 0.5f, (column + 0.5f) / SearchResolution),
                        Mathf.Lerp(-areaSize.y * 0.5f, areaSize.y * 0.5f, (row + 0.5f) / SearchResolution));
                    candidate = ClampInside(Snap(candidate), footprint);
                    if (IsFree(candidate, footprint))
                    {
                        center = candidate;
                        return true;
                    }
                }
            }

            center = default;
            return false;
        }

        bool IsFree(Vector2 center, Vector2 footprint)
        {
            if (!preventOverlap)
            {
                return true;
            }

            var rect = MakeRect(center, footprint + Vector2.one * overlapPadding);
            foreach (var pair in footprints)
            {
                if (pair.Key != null && rect.Overlaps(pair.Value))
                {
                    return false;
                }
            }

            return true;
        }

        Vector2 Snap(Vector2 local)
        {
            if (gridSnap <= 0f)
            {
                return local;
            }

            return new Vector2(Mathf.Round(local.x / gridSnap) * gridSnap, Mathf.Round(local.y / gridSnap) * gridSnap);
        }

        Vector2 ClampInside(Vector2 local, Vector2 footprint)
        {
            var halfX = Mathf.Max(0f, (areaSize.x - footprint.x) * 0.5f);
            var halfZ = Mathf.Max(0f, (areaSize.y - footprint.y) * 0.5f);
            return new Vector2(Mathf.Clamp(local.x, -halfX, halfX), Mathf.Clamp(local.y, -halfZ, halfZ));
        }

        static Rect MakeRect(Vector2 center, Vector2 size)
        {
            return new Rect(center - size * 0.5f, size);
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, 0.002f, areaSize.y));
        }
    }
}
