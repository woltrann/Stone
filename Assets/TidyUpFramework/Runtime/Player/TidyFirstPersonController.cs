using TidyUpFramework.Core;
using TidyUpFramework.Input;
using UnityEngine;

namespace TidyUpFramework.Player
{
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Tidy Up Framework/Player/First Person Controller")]
    public sealed class TidyFirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Pitch pivot holding the camera. Usually a child named Head.")]
        Transform head;

        [Header("Movement")]
        [SerializeField, Min(0f)] float walkSpeed = 3.2f;
        [SerializeField, Min(0f)] float sprintSpeed = 5.2f;
        [SerializeField, Min(0f)] float crouchSpeed = 1.6f;
        [SerializeField, Min(0f)] float acceleration = 14f;
        [SerializeField] float gravity = -18f;
        [SerializeField] bool sprintEnabled = true;
        [SerializeField] bool crouchEnabled = true;

        [Header("Look")]
        [SerializeField, Range(10f, 90f)] float maximumPitch = 85f;
        [SerializeField, Min(0f), Tooltip("Smooths look input. Zero is raw.")] float lookSmoothing = 0f;

        [Header("Height")]
        [SerializeField, Min(0.5f)] float standingHeight = 1.75f;
        [SerializeField, Min(0.3f)] float crouchingHeight = 1.1f;
        [SerializeField, Min(0f)] float eyeOffsetFromTop = 0.12f;
        [SerializeField, Min(0f)] float heightChangeSpeed = 8f;

        [Header("Head Bob")]
        [SerializeField] bool headBobEnabled = true;
        [SerializeField, Min(0f)] float bobAmplitude = 0.03f;
        [SerializeField, Min(0f)] float bobFrequency = 1.9f;

        CharacterController characterController;
        ITidyPlayerInput input;
        Vector3 planarVelocity;
        float verticalVelocity;
        float pitch;
        float yaw;
        float currentHeight;
        float bobPhase;
        Vector2 smoothedLook;

        public bool HeadBobEnabled
        {
            get => headBobEnabled;
            set => headBobEnabled = value;
        }

        public Transform Head => head;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public bool IsGrounded => characterController.isGrounded;
        public Vector3 Velocity => planarVelocity + Vector3.up * verticalVelocity;

        public void Teleport(Vector3 position, float newYaw)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
            yaw = newYaw;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            planarVelocity = Vector3.zero;
        }

        public void Teleport(Vector3 position, float newYaw, float newPitch)
        {
            Teleport(position, newYaw);
            pitch = Mathf.Clamp(newPitch, -maximumPitch, maximumPitch);
            smoothedLook = Vector2.zero;
            if (head != null)
            {
                head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (head == null && transform.childCount > 0)
            {
                head = transform.GetChild(0);
            }

            currentHeight = standingHeight;
            yaw = transform.eulerAngles.y;
            ApplyHeight();
            TidyServices.Register(this);
        }

        void OnDestroy()
        {
            TidyServices.Unregister(this);
        }

        void Update()
        {
            if (input == null && !TidyServices.TryGet(out input))
            {
                return;
            }

            UpdateLook();
            UpdateMovement();
            UpdateHeight();
            UpdateHeadBob();
        }

        void UpdateLook()
        {
            if (TidyControlLock.IsLocked(TidyControlChannel.Look))
            {
                smoothedLook = Vector2.zero;
                return;
            }

            var look = input.LookDelta;
            smoothedLook = lookSmoothing > 0f ? Vector2.Lerp(smoothedLook, look, 1f - Mathf.Exp(-Time.unscaledDeltaTime / lookSmoothing)) : look;
            yaw += smoothedLook.x;
            pitch = Mathf.Clamp(pitch - smoothedLook.y, -maximumPitch, maximumPitch);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (head != null)
            {
                head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        void UpdateMovement()
        {
            var locked = TidyControlLock.IsLocked(TidyControlChannel.Movement);
            var move = locked ? Vector2.zero : input.Move;
            var speed = SelectSpeed(locked);
            var desired = (transform.right * move.x + transform.forward * move.y) * speed;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desired, acceleration * Time.deltaTime);
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            characterController.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        float SelectSpeed(bool locked)
        {
            if (locked)
            {
                return 0f;
            }

            if (crouchEnabled && input.CrouchHeld)
            {
                return crouchSpeed;
            }

            return sprintEnabled && input.SprintHeld ? sprintSpeed : walkSpeed;
        }

        void UpdateHeight()
        {
            var wantsCrouch = crouchEnabled && input.CrouchHeld && !TidyControlLock.IsLocked(TidyControlChannel.Movement);
            var targetHeight = wantsCrouch ? crouchingHeight : HasHeadroom() ? standingHeight : currentHeight;

            if (Mathf.Approximately(currentHeight, targetHeight))
            {
                return;
            }

            currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, heightChangeSpeed * Time.deltaTime);
            ApplyHeight();
        }

        bool HasHeadroom()
        {
            if (currentHeight >= standingHeight - 0.01f)
            {
                return true;
            }

            var origin = transform.position + Vector3.up * currentHeight;
            return !Physics.SphereCast(origin, characterController.radius * 0.9f, Vector3.up, out _, standingHeight - currentHeight, ~0, QueryTriggerInteraction.Ignore);
        }

        void ApplyHeight()
        {
            characterController.height = currentHeight;
            characterController.center = Vector3.up * (currentHeight * 0.5f);
            if (head != null)
            {
                var local = head.localPosition;
                local.y = currentHeight - eyeOffsetFromTop;
                head.localPosition = local;
            }
        }

        void UpdateHeadBob()
        {
            if (head == null)
            {
                return;
            }

            var speed = new Vector2(planarVelocity.x, planarVelocity.z).magnitude;
            var baseY = currentHeight - eyeOffsetFromTop;
            var local = head.localPosition;
            if (!headBobEnabled || speed < 0.1f || !characterController.isGrounded)
            {
                local.y = Mathf.Lerp(local.y, baseY, 10f * Time.deltaTime);
                head.localPosition = local;
                return;
            }

            bobPhase += Time.deltaTime * bobFrequency * Mathf.PI * 2f * (speed / walkSpeed);
            local.y = baseY + Mathf.Sin(bobPhase) * bobAmplitude;
            head.localPosition = local;
        }
    }
}
