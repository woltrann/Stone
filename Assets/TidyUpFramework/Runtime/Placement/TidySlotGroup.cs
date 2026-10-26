using System.Collections.Generic;
using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Placement
{
    public enum TidySlotLayoutMode
    {
        Grid,
        ManualAnchors
    }

    public enum TidyGridPlane
    {
        Vertical,
        Horizontal
    }

    [AddComponentMenu("Tidy Up Framework/Placement/Slot Group")]
    public sealed class TidySlotGroup : TidyPlacementTarget
    {
        [Header("Layout")]
        [SerializeField]
        TidySlotLayoutMode layoutMode = TidySlotLayoutMode.Grid;

        [SerializeField, Min(1)]
        int columns = 5;

        [SerializeField, Min(1)]
        int rows = 1;

        [SerializeField, Tooltip("Horizontal and vertical distance between slot centres.")]
        Vector2 spacing = new Vector2(0.18f, 0.32f);

        [SerializeField, Tooltip("Vertical: rows stack upwards like a shelf face. Horizontal: rows extend forward like a table grid.")]
        TidyGridPlane plane = TidyGridPlane.Vertical;

        [SerializeField, Tooltip("Manual slot anchors. Used when layout mode is Manual Anchors.")]
        List<Transform> anchors = new List<Transform>();

        [SerializeField, Tooltip("Extra rotation applied to placed objects relative to each slot.")]
        Vector3 placedEulerOffset;

        [Header("Assignment")]
        [SerializeField, Tooltip("Ignore where the player aims and always use the first free slot.")]
        bool fillInOrder;

        TidyObject[] slotContents;
        readonly List<TidyObject> orderedOccupants = new List<TidyObject>();

        public override int Capacity => SlotCount;

        public int SlotCount => layoutMode == TidySlotLayoutMode.ManualAnchors ? anchors.Count : columns * rows;

        public void ConfigureGrid(int newColumns, int newRows, Vector2 newSpacing, TidyGridPlane newPlane)
        {
            layoutMode = TidySlotLayoutMode.Grid;
            columns = Mathf.Max(1, newColumns);
            rows = Mathf.Max(1, newRows);
            spacing = newSpacing;
            plane = newPlane;
        }

        public void ConfigureAssignment(bool shouldFillInOrder, Vector3 eulerOffset)
        {
            fillInOrder = shouldFillInOrder;
            placedEulerOffset = eulerOffset;
        }

        public void SetManualAnchors(IEnumerable<Transform> newAnchors)
        {
            layoutMode = TidySlotLayoutMode.ManualAnchors;
            anchors = new List<Transform>(newAnchors);
        }

        public override bool IsCompatible(TidyObject tidyObject)
        {
            return base.IsCompatible(tidyObject) && tidyObject.IsSnapCompatible;
        }

        public TidyObject GetSlotContent(int slotIndex)
        {
            EnsureSlots();
            return slotIndex >= 0 && slotIndex < slotContents.Length ? slotContents[slotIndex] : null;
        }

        public Vector3 GetSlotWorldPosition(int slotIndex)
        {
            return transform.TransformPoint(GetSlotLocalPosition(slotIndex));
        }

        public Quaternion GetSlotWorldRotation(int slotIndex)
        {
            if (layoutMode == TidySlotLayoutMode.ManualAnchors && slotIndex < anchors.Count && anchors[slotIndex] != null)
            {
                return anchors[slotIndex].rotation;
            }

            return transform.rotation;
        }

        public override IReadOnlyList<TidyObject> GetOrderedOccupants()
        {
            EnsureSlots();
            orderedOccupants.Clear();
            foreach (var content in slotContents)
            {
                if (content != null)
                {
                    orderedOccupants.Add(content);
                }
            }

            return orderedOccupants;
        }

        protected override bool TryFindPose(TidyPlacementQuery query, out Vector3 position, out Quaternion rotation, out int slotIndex)
        {
            EnsureSlots();
            slotIndex = fillInOrder || !query.HasAimPoint ? FirstFreeSlot() : NearestFreeSlot(query.AimPoint);
            if (slotIndex < 0)
            {
                position = default;
                rotation = Quaternion.identity;
                return false;
            }

            rotation = GetSlotWorldRotation(slotIndex) * Quaternion.Euler(placedEulerOffset) * query.Object.PlacementRotationOffset;
            position = PivotForBottomAt(query.Object, GetSlotWorldPosition(slotIndex), rotation);
            return true;
        }

        protected override void InsertOccupant(TidyObject tidyObject, int slotIndex)
        {
            EnsureSlots();
            base.InsertOccupant(tidyObject, slotIndex);
            if (slotIndex >= 0 && slotIndex < slotContents.Length)
            {
                slotContents[slotIndex] = tidyObject;
            }
        }

        protected override void RemoveOccupant(TidyObject tidyObject)
        {
            EnsureSlots();
            base.RemoveOccupant(tidyObject);
            for (var index = 0; index < slotContents.Length; index++)
            {
                if (slotContents[index] == tidyObject)
                {
                    slotContents[index] = null;
                }
            }
        }

        int FirstFreeSlot()
        {
            for (var index = 0; index < slotContents.Length; index++)
            {
                if (slotContents[index] == null)
                {
                    return index;
                }
            }

            return -1;
        }

        int NearestFreeSlot(Vector3 aimPoint)
        {
            var bestIndex = -1;
            var bestDistance = float.MaxValue;
            for (var index = 0; index < slotContents.Length; index++)
            {
                if (slotContents[index] != null)
                {
                    continue;
                }

                var distance = (GetSlotWorldPosition(index) - aimPoint).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = index;
                }
            }

            return bestIndex;
        }

        Vector3 GetSlotLocalPosition(int slotIndex)
        {
            if (layoutMode == TidySlotLayoutMode.ManualAnchors)
            {
                if (slotIndex < anchors.Count && anchors[slotIndex] != null)
                {
                    return transform.InverseTransformPoint(anchors[slotIndex].position);
                }

                return Vector3.zero;
            }

            var column = slotIndex % columns;
            var row = slotIndex / columns;
            var x = (column - (columns - 1) * 0.5f) * spacing.x;
            return plane == TidyGridPlane.Vertical ? new Vector3(x, row * spacing.y, 0f) : new Vector3(x, 0f, row * spacing.y);
        }

        void EnsureSlots()
        {
            var count = SlotCount;
            if (slotContents != null && slotContents.Length == count)
            {
                return;
            }

            var previous = slotContents;
            slotContents = new TidyObject[count];
            if (previous == null)
            {
                return;
            }

            for (var index = 0; index < Mathf.Min(previous.Length, count); index++)
            {
                slotContents[index] = previous[index];
            }
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            for (var index = 0; index < SlotCount; index++)
            {
                var position = GetSlotWorldPosition(index);
                Gizmos.DrawWireCube(position + Vector3.up * 0.02f, new Vector3(0.06f, 0.04f, 0.06f));
            }
        }
    }
}
