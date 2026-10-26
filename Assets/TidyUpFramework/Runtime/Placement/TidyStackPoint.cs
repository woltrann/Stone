using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Placement
{
    [AddComponentMenu("Tidy Up Framework/Placement/Stack Point")]
    public sealed class TidyStackPoint : TidyPlacementTarget
    {
        [Header("Stack")]
        [SerializeField, Min(1)]
        int maxHeight = 6;

        [SerializeField, Min(0f)]
        float spacing = 0.002f;

        [SerializeField, Tooltip("Local direction the stack grows in.")]
        Vector3 direction = Vector3.up;

        [SerializeField, Tooltip("Only objects with the same definition as the first object can join the stack.")]
        bool identicalOnly = true;

        [SerializeField, Tooltip("Only the top object can be taken from the stack.")]
        bool onlyTopRemovable = true;

        [SerializeField, Tooltip("Orientation of every object in the stack relative to this transform.")]
        Vector3 itemEulerRotation;

        [SerializeField, Range(0f, 15f), Tooltip("Small deterministic yaw variation for a natural look.")]
        float yawJitter = 3f;

        public override int Capacity => maxHeight;
        public TidyObject TopObject => occupants.Count > 0 ? occupants[occupants.Count - 1] : null;

        public void ConfigureStack(int newMaxHeight, float newSpacing, Vector3 newDirection, bool identical, Vector3 itemRotation, float jitter)
        {
            maxHeight = Mathf.Max(1, newMaxHeight);
            spacing = Mathf.Max(0f, newSpacing);
            direction = newDirection.sqrMagnitude > 0f ? newDirection.normalized : Vector3.up;
            identicalOnly = identical;
            itemEulerRotation = itemRotation;
            yawJitter = jitter;
        }

        public override bool IsCompatible(TidyObject tidyObject)
        {
            if (!base.IsCompatible(tidyObject) || !tidyObject.IsStackable)
            {
                return false;
            }

            return !identicalOnly || occupants.Count == 0 || occupants[0].Definition == tidyObject.Definition;
        }

        public override bool AllowsRemoval(TidyObject tidyObject)
        {
            return base.AllowsRemoval(tidyObject) && (!onlyTopRemovable || tidyObject == TopObject);
        }

        protected override bool TryFindPose(TidyPlacementQuery query, out Vector3 position, out Quaternion rotation, out int slotIndex)
        {
            slotIndex = occupants.Count;
            ComputePose(query.Object, slotIndex, HeightBelow(slotIndex), out position, out rotation);
            return true;
        }

        protected override void OnOccupantsChanged(TidyObject insertedObject, bool animate)
        {
            for (var index = 0; index < occupants.Count; index++)
            {
                var occupant = occupants[index];
                if (occupant == insertedObject)
                {
                    continue;
                }

                ComputePose(occupant, index, HeightBelow(index), out var position, out var rotation);
                MoveOccupant(occupant, position, rotation, animate);
            }
        }

        float HeightBelow(int index)
        {
            var height = 0f;
            for (var below = 0; below < index && below < occupants.Count; below++)
            {
                height += ExtentAlongDirection(occupants[below], LocalItemRotation(occupants[below], below)) + spacing;
            }

            return height;
        }

        void ComputePose(TidyObject tidyObject, int index, float height, out Vector3 position, out Quaternion rotation)
        {
            var localRotation = LocalItemRotation(tidyObject, index);
            rotation = transform.rotation * localRotation;
            var basePoint = transform.TransformPoint(direction.normalized * height);
            position = PivotForBottomAt(tidyObject, basePoint, rotation);
        }

        Quaternion LocalItemRotation(TidyObject tidyObject, int index)
        {
            var jitter = Mathf.Sin(index * 12.9898f) * yawJitter;
            return Quaternion.Euler(0f, jitter, 0f) * Quaternion.Euler(itemEulerRotation) * tidyObject.PlacementRotationOffset;
        }

        float ExtentAlongDirection(TidyObject tidyObject, Quaternion localRotation)
        {
            var size = RotatedSize(tidyObject, localRotation);
            var axis = direction.normalized;
            return Mathf.Abs(axis.x) * size.x + Mathf.Abs(axis.y) * size.y + Mathf.Abs(axis.z) * size.z;
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Gizmos.DrawLine(transform.position, transform.TransformPoint(direction.normalized * 0.4f));
            Gizmos.DrawWireSphere(transform.position, 0.04f);
        }
    }
}
