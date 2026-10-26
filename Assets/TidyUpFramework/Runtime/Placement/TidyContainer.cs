using System;
using TidyUpFramework.Core;
using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Placement
{
    public enum TidyContainerLayout
    {
        Grid,
        Pile,
        Hidden
    }

    [AddComponentMenu("Tidy Up Framework/Placement/Container")]
    public sealed class TidyContainer : TidyPlacementTarget
    {
        const float HiddenShrinkDuration = 0.22f;
        const int PileSeed = 7919;

        [Header("Container")]
        [SerializeField, Min(1)]
        int capacity = 12;

        [SerializeField, Tooltip("Accept any number of objects. Useful for disposal bins.")]
        bool unlimitedCapacity;

        [SerializeField, Tooltip("Inner volume in local space. The bottom centre sits at the inner offset.")]
        Vector3 innerSize = new Vector3(0.4f, 0.25f, 0.3f);

        [SerializeField]
        Vector3 innerOffset;

        [SerializeField, Tooltip("Grid: neat rows. Pile: natural heap. Hidden: objects drop in and disappear.")]
        TidyContainerLayout layout = TidyContainerLayout.Pile;

        [SerializeField, Tooltip("Objects are consumed after landing, for example trash in a bin.")]
        bool consumeOnPlace;

        [Header("Fill Visual")]
        [SerializeField, Tooltip("Optional transform moved between the fill heights as the container fills.")]
        Transform fillIndicator;

        [SerializeField]
        Vector2 fillIndicatorHeights = new Vector2(0f, 0.2f);

        [SerializeField, Tooltip("Optional objects enabled one after another as the container fills.")]
        GameObject[] fillStages = Array.Empty<GameObject>();

        public event Action<TidyContainer, float> FillChanged;

        public override int Capacity => unlimitedCapacity ? int.MaxValue : capacity;
        public float FillRatio => unlimitedCapacity ? 0f : Mathf.Clamp01((float)occupants.Count / capacity);
        public int ObjectCount => occupants.Count;
        public bool ConsumesObjects => consumeOnPlace;

        public void ConfigureContainer(int newCapacity, bool unlimited, Vector3 newInnerSize, Vector3 newInnerOffset, TidyContainerLayout newLayout, bool consume)
        {
            capacity = Mathf.Max(1, newCapacity);
            unlimitedCapacity = unlimited;
            innerSize = newInnerSize;
            innerOffset = newInnerOffset;
            layout = newLayout;
            consumeOnPlace = consume;
        }

        public void ConfigureFillVisual(Transform indicator, Vector2 heights, GameObject[] stages)
        {
            fillIndicator = indicator;
            fillIndicatorHeights = heights;
            fillStages = stages ?? Array.Empty<GameObject>();
        }

        public override bool IsCompatible(TidyObject tidyObject)
        {
            return base.IsCompatible(tidyObject) && tidyObject.IsContainerCompatible;
        }

        public override bool AllowsRemoval(TidyObject tidyObject)
        {
            return layout != TidyContainerLayout.Hidden && !consumeOnPlace && base.AllowsRemoval(tidyObject);
        }

        protected override bool TryFindPose(TidyPlacementQuery query, out Vector3 position, out Quaternion rotation, out int slotIndex)
        {
            slotIndex = occupants.Count;
            switch (layout)
            {
                case TidyContainerLayout.Grid:
                    ComputeGridPose(query.Object, slotIndex, out position, out rotation);
                    break;
                case TidyContainerLayout.Hidden:
                    rotation = query.Object.transform.rotation;
                    position = PivotForBottomAt(query.Object, transform.TransformPoint(innerOffset + Vector3.up * innerSize.y), rotation);
                    break;
                default:
                    ComputePilePose(query.Object, slotIndex, out position, out rotation);
                    break;
            }

            return true;
        }

        protected override void OnPlacementCompleted(TidyObject tidyObject)
        {
            if (layout == TidyContainerLayout.Hidden || consumeOnPlace)
            {
                TidyTweenRunner.ScaleTo(tidyObject.VisualRoot, Vector3.zero, HiddenShrinkDuration, TidyEaseType.InQuad, () => HideConsumed(tidyObject));
                if (consumeOnPlace)
                {
                    tidyObject.MarkConsumed();
                }
            }

            RefreshFillVisual();
        }

        protected override void OnOccupantsChanged(TidyObject insertedObject, bool animate)
        {
            RefreshFillVisual();
        }

        void HideConsumed(TidyObject tidyObject)
        {
            if (tidyObject == null)
            {
                return;
            }

            if (consumeOnPlace)
            {
                tidyObject.gameObject.SetActive(false);
                return;
            }

            foreach (var renderer in tidyObject.Renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            tidyObject.VisualRoot.localScale = tidyObject.RestVisualScale;
        }

        void ComputeGridPose(TidyObject tidyObject, int index, out Vector3 position, out Quaternion rotation)
        {
            rotation = transform.rotation * tidyObject.PlacementRotationOffset;
            var footprint = RotatedSize(tidyObject, tidyObject.PlacementRotationOffset);
            var perRow = Mathf.Max(1, Mathf.FloorToInt(innerSize.x / Mathf.Max(0.01f, footprint.x)));
            var perLayer = perRow * Mathf.Max(1, Mathf.FloorToInt(innerSize.z / Mathf.Max(0.01f, footprint.z)));
            var layer = index / perLayer;
            var inLayer = index % perLayer;
            var column = inLayer % perRow;
            var row = inLayer / perRow;
            var rowsPerLayer = Mathf.Max(1, perLayer / perRow);
            var local = innerOffset + new Vector3(
                (column - (perRow - 1) * 0.5f) * footprint.x,
                layer * footprint.y,
                (row - (rowsPerLayer - 1) * 0.5f) * footprint.z);
            position = PivotForBottomAt(tidyObject, transform.TransformPoint(local), rotation);
        }

        void ComputePilePose(TidyObject tidyObject, int index, out Vector3 position, out Quaternion rotation)
        {
            var random = new System.Random(PileSeed + index * 31 + GetEntityId().GetHashCode());
            var yaw = (float)random.NextDouble() * 360f;
            var tilt = ((float)random.NextDouble() - 0.5f) * 20f;
            var localRotation = Quaternion.Euler(tilt, yaw, 0f) * tidyObject.PlacementRotationOffset;
            rotation = transform.rotation * localRotation;
            var footprint = RotatedSize(tidyObject, localRotation);
            var halfX = Mathf.Max(0f, (innerSize.x - footprint.x) * 0.5f);
            var halfZ = Mathf.Max(0f, (innerSize.z - footprint.z) * 0.5f);
            var capacityForHeight = unlimitedCapacity ? Mathf.Max(occupants.Count, 1) : capacity;
            var height = Mathf.Clamp01((float)index / capacityForHeight) * Mathf.Max(0f, innerSize.y - footprint.y * 0.5f);
            var local = innerOffset + new Vector3(
                ((float)random.NextDouble() * 2f - 1f) * halfX,
                height,
                ((float)random.NextDouble() * 2f - 1f) * halfZ);
            position = PivotForBottomAt(tidyObject, transform.TransformPoint(local), rotation);
        }

        void RefreshFillVisual()
        {
            var ratio = FillRatio;
            if (fillIndicator != null)
            {
                var local = fillIndicator.localPosition;
                local.y = Mathf.Lerp(fillIndicatorHeights.x, fillIndicatorHeights.y, ratio);
                fillIndicator.localPosition = local;
                fillIndicator.gameObject.SetActive(occupants.Count > 0);
            }

            for (var index = 0; index < fillStages.Length; index++)
            {
                if (fillStages[index] != null)
                {
                    fillStages[index].SetActive(ratio >= (index + 1f) / (fillStages.Length + 1f));
                }
            }

            FillChanged?.Invoke(this, ratio);
        }

        protected override void Start()
        {
            base.Start();
            RefreshFillVisual();
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(innerOffset + Vector3.up * innerSize.y * 0.5f, innerSize);
        }
    }
}
