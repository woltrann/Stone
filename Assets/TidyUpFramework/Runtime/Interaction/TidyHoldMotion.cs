using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Interaction
{
    public sealed class TidyHoldMotion
    {
        const float MaximumPhysicsSpeed = 20f;
        const float AngularCorrection = 0.6f;

        public void MoveControlled(TidyObject tidyObject, Vector3 targetCenter, Quaternion targetRotation, float deltaTime)
        {
            var sharpness = tidyObject.PhysicsProfile.FollowSharpness;
            var blend = 1f - Mathf.Exp(-sharpness * deltaTime);
            var transform = tidyObject.transform;
            var targetPivot = targetCenter - tidyObject.BoundsCenterOffset(targetRotation);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, targetPivot, blend),
                Quaternion.Slerp(transform.rotation, targetRotation, blend));
        }

        public void MovePhysics(TidyObject tidyObject, Vector3 targetCenter, Quaternion targetRotation, float fixedDeltaTime)
        {
            var body = tidyObject.Body;
            if (body == null || body.isKinematic)
            {
                MoveControlled(tidyObject, targetCenter, targetRotation, fixedDeltaTime);
                return;
            }

            var sharpness = tidyObject.PhysicsProfile.FollowSharpness;
            var currentCenter = body.worldCenterOfMass;
            var desiredVelocity = (targetCenter - currentCenter) * sharpness;
            body.linearVelocity = Vector3.ClampMagnitude(desiredVelocity, MaximumPhysicsSpeed);

            var delta = targetRotation * Quaternion.Inverse(body.rotation);
            delta.ToAngleAxis(out var angle, out var axis);
            if (angle > 180f)
            {
                angle -= 360f;
            }

            if (Mathf.Abs(angle) > 0.01f && !float.IsNaN(axis.x))
            {
                body.angularVelocity = axis * (angle * Mathf.Deg2Rad * sharpness * AngularCorrection);
            }
            else
            {
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}
