using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Placement
{
    public enum TidyRowAlignment
    {
        Start,
        Center
    }

    [AddComponentMenu("Tidy Up Framework/Placement/Row Zone (Packed Shelf)")]
    public sealed class TidyRowZone : TidyPlacementTarget
    {
        [Header("Row")]
        [SerializeField, Min(0.01f), Tooltip("Usable length along the local X axis, starting at this transform.")]
        float length = 1f;

        [SerializeField, Min(0f), Tooltip("Gap between neighbouring objects.")]
        float gap = 0.004f;

        [SerializeField, Min(1)]
        int maxCount = 24;

        [SerializeField]
        TidyRowAlignment alignment = TidyRowAlignment.Start;

        [SerializeField, Tooltip("Orientation of every object in the row relative to this transform, for example spines facing outwards.")]
        Vector3 itemEulerRotation;

        [SerializeField, Min(0f), Tooltip("Depth drawn by the gizmo.")]
        float gizmoDepth = 0.25f;

        [SerializeField, Min(0f), Tooltip("Height drawn by the gizmo.")]
        float gizmoHeight = 0.3f;

        public override int Capacity => maxCount;
        public float Length => length;

        public float UsedLength
        {
            get
            {
                var used = 0f;
                foreach (var occupant in occupants)
                {
                    used += WidthOf(occupant) + gap;
                }

                return used;
            }
        }

        public void ConfigureRow(float newLength, float newGap, int newMaxCount, Vector3 itemRotation, TidyRowAlignment newAlignment)
        {
            length = Mathf.Max(0.01f, newLength);
            gap = Mathf.Max(0f, newGap);
            maxCount = Mathf.Max(1, newMaxCount);
            itemEulerRotation = itemRotation;
            alignment = newAlignment;
        }

        public void ConfigureGizmo(float depth, float height)
        {
            gizmoDepth = depth;
            gizmoHeight = height;
        }

        protected override bool TryFindPose(TidyPlacementQuery query, out Vector3 position, out Quaternion rotation, out int slotIndex)
        {
            var width = WidthOf(query.Object);
            rotation = ItemRotation(query.Object);
            if (UsedLength + width > length + 0.0001f)
            {
                position = default;
                slotIndex = -1;
                return false;
            }

            slotIndex = query.HasAimPoint ? InsertionIndexFor(query.AimPoint) : occupants.Count;
            var centerX = CenterOfIndexWithInsertion(slotIndex, width);
            position = PivotForBottomAt(query.Object, transform.TransformPoint(new Vector3(centerX, 0f, 0f)), rotation);
            return true;
        }

        protected override void InsertOccupant(TidyObject tidyObject, int slotIndex)
        {
            occupants.Insert(Mathf.Clamp(slotIndex, 0, occupants.Count), tidyObject);
        }

        protected override void OnOccupantsChanged(TidyObject insertedObject, bool animate)
        {
            var cursor = StartOffset(UsedLength);
            foreach (var occupant in occupants)
            {
                var width = WidthOf(occupant);
                var center = cursor + width * 0.5f;
                cursor += width + gap;
                if (occupant == insertedObject)
                {
                    continue;
                }

                var rotation = ItemRotation(occupant);
                MoveOccupant(occupant, PivotForBottomAt(occupant, transform.TransformPoint(new Vector3(center, 0f, 0f)), rotation), rotation, animate);
            }
        }

        int InsertionIndexFor(Vector3 aimPoint)
        {
            var localAim = transform.InverseTransformPoint(aimPoint).x;
            var cursor = StartOffset(UsedLength);
            for (var index = 0; index < occupants.Count; index++)
            {
                var width = WidthOf(occupants[index]);
                if (localAim < cursor + width * 0.5f)
                {
                    return index;
                }

                cursor += width + gap;
            }

            return occupants.Count;
        }

        float CenterOfIndexWithInsertion(int insertionIndex, float insertedWidth)
        {
            var cursor = StartOffset(UsedLength + insertedWidth + gap);
            for (var index = 0; index < insertionIndex && index < occupants.Count; index++)
            {
                cursor += WidthOf(occupants[index]) + gap;
            }

            return cursor + insertedWidth * 0.5f;
        }

        float StartOffset(float usedLength)
        {
            return alignment == TidyRowAlignment.Center ? Mathf.Max(0f, (length - usedLength) * 0.5f) : 0f;
        }

        Quaternion ItemRotation(TidyObject tidyObject)
        {
            return transform.rotation * Quaternion.Euler(itemEulerRotation) * tidyObject.PlacementRotationOffset;
        }

        float WidthOf(TidyObject tidyObject)
        {
            var localRotation = Quaternion.Euler(itemEulerRotation) * tidyObject.PlacementRotationOffset;
            return RotatedSize(tidyObject, localRotation).x;
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(length * 0.5f, gizmoHeight * 0.5f, 0f), new Vector3(length, gizmoHeight, gizmoDepth));
            Gizmos.DrawLine(Vector3.zero, Vector3.right * length);
        }
    }
}
