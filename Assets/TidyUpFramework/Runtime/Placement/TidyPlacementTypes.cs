using TidyUpFramework.Objects;
using UnityEngine;

namespace TidyUpFramework.Placement
{
    public enum TidyPlacementPolicy
    {
        RejectIncorrect,
        AcceptIncorrect
    }

    public enum TidyCompletionMode
    {
        WhenFull,
        WhenRequiredCountReached,
        WhenAllMatchingObjectsPlaced,
        Never
    }

    public enum TidyRejectionReason
    {
        None,
        NotPlaceable,
        Incompatible,
        WrongObject,
        Full,
        NoSpace,
        Locked
    }

    public struct TidyPlacementQuery
    {
        public TidyObject Object;
        public Vector3 AimPoint;
        public Vector3 AimNormal;
        public float PreferredYaw;
        public bool HasAimPoint;

        public TidyPlacementQuery(TidyObject tidyObject, Vector3 aimPoint, Vector3 aimNormal, float preferredYaw)
        {
            Object = tidyObject;
            AimPoint = aimPoint;
            AimNormal = aimNormal;
            PreferredYaw = preferredYaw;
            HasAimPoint = true;
        }

        public static TidyPlacementQuery WithoutAim(TidyObject tidyObject)
        {
            return new TidyPlacementQuery { Object = tidyObject, AimNormal = Vector3.up };
        }
    }

    public struct TidyPlacementCandidate
    {
        public TidyPlacementTarget Target;
        public TidyObject Object;
        public Vector3 Position;
        public Quaternion Rotation;
        public int SlotIndex;
        public bool IsValid;
        public bool IsCorrect;
        public TidyRejectionReason Rejection;

        public bool HasPose => Target != null && Rejection != TidyRejectionReason.NoSpace && Rejection != TidyRejectionReason.Full && Rejection != TidyRejectionReason.Incompatible && Rejection != TidyRejectionReason.NotPlaceable;
    }

    public readonly struct TidyPlacementEvent
    {
        public readonly TidyObject Object;
        public readonly TidyPlacementTarget Target;
        public readonly bool IsCorrect;
        public readonly bool IsRemoval;

        public TidyPlacementEvent(TidyObject tidyObject, TidyPlacementTarget target, bool isCorrect, bool isRemoval)
        {
            Object = tidyObject;
            Target = target;
            IsCorrect = isCorrect;
            IsRemoval = isRemoval;
        }
    }
}
