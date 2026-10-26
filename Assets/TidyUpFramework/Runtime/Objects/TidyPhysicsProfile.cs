using UnityEngine;

namespace TidyUpFramework.Objects
{
    public enum TidyInteractionMode
    {
        Controlled,
        Physics
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Physics Profile", fileName = "PhysicsProfile_New", order = 20)]
    public sealed class TidyPhysicsProfile : ScriptableObject
    {
        [SerializeField, Tooltip("Controlled: arcade style, kinematic and fully predictable. Physics: simulated rigidbody that can tumble, be pushed and thrown.")]
        TidyInteractionMode interactionMode = TidyInteractionMode.Controlled;

        [SerializeField, Min(0.01f)]
        float mass = 0.5f;

        [SerializeField, Min(0f)]
        float linearDamping = 0.5f;

        [SerializeField, Min(0f)]
        float angularDamping = 2f;

        [SerializeField, Min(0f), Tooltip("How quickly a held object follows the hold point.")]
        float followSharpness = 18f;

        [SerializeField, Min(0f), Tooltip("Velocity applied when the object is thrown.")]
        float throwSpeed = 6f;

        [SerializeField, Tooltip("Continuous collision prevents small fast objects from tunnelling through shelves.")]
        CollisionDetectionMode collisionDetection = CollisionDetectionMode.ContinuousSpeculative;

        [SerializeField, Min(0f), Tooltip("Seconds a controlled object takes to settle on the ground after being dropped.")]
        float controlledDropDuration = 0.35f;

        public TidyInteractionMode InteractionMode => interactionMode;
        public float Mass => mass;
        public float LinearDamping => linearDamping;
        public float AngularDamping => angularDamping;
        public float FollowSharpness => followSharpness;
        public float ThrowSpeed => throwSpeed;
        public CollisionDetectionMode CollisionDetection => collisionDetection;
        public float ControlledDropDuration => controlledDropDuration;

        public void Configure(TidyInteractionMode mode, float newMass)
        {
            interactionMode = mode;
            mass = newMass;
        }
    }
}
