using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Placement
{
    [AddComponentMenu("Tidy Up Framework/Placement/Snap Slot")]
    public sealed class TidySnapSlot : TidyPlacementTarget
    {
        [SerializeField, Tooltip("Extra rotation applied to the placed object relative to this transform.")]
        Vector3 placedEulerOffset;

        [SerializeField, Min(0f)]
        float gizmoRadius = 0.08f;

        public override int Capacity => 1;

        public override bool IsCompatible(TidyObject tidyObject)
        {
            return base.IsCompatible(tidyObject) && tidyObject.IsSnapCompatible;
        }

        protected override bool TryFindPose(TidyPlacementQuery query, out Vector3 position, out Quaternion rotation, out int slotIndex)
        {
            rotation = transform.rotation * Quaternion.Euler(placedEulerOffset) * query.Object.PlacementRotationOffset;
            position = PivotForBottomAt(query.Object, transform.position, rotation);
            slotIndex = 0;
            return true;
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, gizmoRadius * 0.5f, 0f), new Vector3(gizmoRadius * 2f, gizmoRadius, gizmoRadius * 2f));
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * gizmoRadius * 1.5f);
        }
    }
}
