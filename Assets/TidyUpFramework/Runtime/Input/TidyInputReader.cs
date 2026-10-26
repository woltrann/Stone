using System;
using TidyUpFramework.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TidyUpFramework.Input
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Tidy Up Framework/Input/Input Reader")]
    public sealed class TidyInputReader : MonoBehaviour, ITidyPlayerInput
    {
        [SerializeField, Tooltip("Input Action asset containing a 'Player' map. The framework ships TidyUpInputActions.")]
        InputActionAsset actions;

        [SerializeField]
        string actionMapName = TidyInputActionNames.PlayerMap;

        [Header("Look")]
        [SerializeField, Min(0f), Tooltip("Degrees per pixel of mouse movement before the sensitivity multiplier.")]
        float mouseDegreesPerPixel = 0.08f;

        [SerializeField, Min(0f), Tooltip("Degrees per second at full stick deflection before the sensitivity multiplier.")]
        float gamepadDegreesPerSecond = 160f;

        [SerializeField, Min(0f), Tooltip("Degrees per pixel of touch drag before the sensitivity multiplier.")]
        float touchDegreesPerPixel = 0.12f;

        [SerializeField, Min(0.01f)]
        float lookSensitivity = 1f;

        [SerializeField]
        bool invertY;

        [SerializeField]
        TidyInputScheme initialScheme = TidyInputScheme.KeyboardMouse;

        InputActionMap map;
        InputAction moveAction;
        InputAction lookAction;
        InputAction interactAction;
        InputAction dropAction;
        InputAction throwAction;
        InputAction rotateAction;
        InputAction inspectAction;
        InputAction sprintAction;
        InputAction crouchAction;
        InputAction hintAction;
        InputAction pauseAction;

        Vector2 virtualMove;
        Vector2 pendingVirtualLook;
        bool pendingVirtualInteract;
        bool pendingVirtualDrop;
        bool pendingVirtualThrow;
        bool pendingVirtualInspect;
        bool pendingVirtualHint;
        bool pendingVirtualPause;
        float virtualRotate;
        bool virtualSprint;
        bool virtualInteractHeld;
        bool hasPendingPointerAim;
        Vector2 pendingPointerAim;

        public event Action<TidyInputScheme> SchemeChanged;

        public TidyInputScheme ActiveScheme { get; private set; }
        public Vector2 Move { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool InteractHeld { get; private set; }
        public bool DropPressed { get; private set; }
        public bool ThrowPressed { get; private set; }
        public float RotateAxis { get; private set; }
        public int RotateSteps { get; private set; }
        public bool InspectPressed { get; private set; }
        public bool HintPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public bool HasPointerAim { get; private set; }
        public Vector2 PointerAimPosition { get; private set; }
        public InputActionAsset Actions => actions;

        public float LookSensitivity
        {
            get => lookSensitivity;
            set => lookSensitivity = Mathf.Max(0.01f, value);
        }

        public bool InvertY
        {
            get => invertY;
            set => invertY = value;
        }

        public void AssignActions(InputActionAsset asset)
        {
            DisableMap();
            actions = asset;
            if (isActiveAndEnabled)
            {
                BindActions();
            }
        }

        public TidyInputScheme? ForcedScheme { get; private set; }

        public void ForceScheme(TidyInputScheme? scheme)
        {
            ForcedScheme = scheme;
            if (scheme.HasValue)
            {
                SetSchemeInternal(scheme.Value);
            }
        }

        public void SetVirtualMove(Vector2 value)
        {
            virtualMove = Vector2.ClampMagnitude(value, 1f);
            if (value.sqrMagnitude > 0f)
            {
                SetScheme(TidyInputScheme.Touch);
            }
        }

        public void AddVirtualLookPixels(Vector2 pixels)
        {
            pendingVirtualLook += pixels;
            SetScheme(TidyInputScheme.Touch);
        }

        public void SetVirtualRotate(float value)
        {
            virtualRotate = Mathf.Clamp(value, -1f, 1f);
        }

        public void SetVirtualSprint(bool held)
        {
            virtualSprint = held;
        }

        public void SetVirtualInteractHeld(bool held)
        {
            virtualInteractHeld = held;
        }

        public void TriggerVirtual(TidyInputAction action)
        {
            SetScheme(TidyInputScheme.Touch);
            switch (action)
            {
                case TidyInputAction.Interact:
                    pendingVirtualInteract = true;
                    break;
                case TidyInputAction.Drop:
                    pendingVirtualDrop = true;
                    break;
                case TidyInputAction.Throw:
                    pendingVirtualThrow = true;
                    break;
                case TidyInputAction.Inspect:
                    pendingVirtualInspect = true;
                    break;
                case TidyInputAction.Hint:
                    pendingVirtualHint = true;
                    break;
                case TidyInputAction.Pause:
                    pendingVirtualPause = true;
                    break;
            }
        }

        public void RegisterTap(Vector2 screenPosition)
        {
            hasPendingPointerAim = true;
            pendingPointerAim = screenPosition;
            pendingVirtualInteract = true;
            SetScheme(TidyInputScheme.Touch);
        }

        public InputAction FindAction(TidyInputAction action)
        {
            return map?.FindAction(TidyInputActionNames.For(action));
        }

        void Awake()
        {
            ActiveScheme = initialScheme;
            if (Application.isMobilePlatform)
            {
                ActiveScheme = TidyInputScheme.Touch;
            }
        }

        void OnEnable()
        {
            BindActions();
            TidyServices.Register<ITidyPlayerInput>(this);
            TidyServices.Register(this);
        }

        void OnDisable()
        {
            DisableMap();
            TidyServices.Unregister<ITidyPlayerInput>(this);
            TidyServices.Unregister(this);
        }

        void BindActions()
        {
            if (actions == null)
            {
                return;
            }

            map = actions.FindActionMap(actionMapName, false);
            if (map == null)
            {
                Debug.LogWarning("Tidy Up Framework: action map '" + actionMapName + "' was not found on " + actions.name, this);
                return;
            }

            moveAction = map.FindAction(TidyInputActionNames.Move);
            lookAction = map.FindAction(TidyInputActionNames.Look);
            interactAction = map.FindAction(TidyInputActionNames.Interact);
            dropAction = map.FindAction(TidyInputActionNames.Drop);
            throwAction = map.FindAction(TidyInputActionNames.Throw);
            rotateAction = map.FindAction(TidyInputActionNames.Rotate);
            inspectAction = map.FindAction(TidyInputActionNames.Inspect);
            sprintAction = map.FindAction(TidyInputActionNames.Sprint);
            crouchAction = map.FindAction(TidyInputActionNames.Crouch);
            hintAction = map.FindAction(TidyInputActionNames.Hint);
            pauseAction = map.FindAction(TidyInputActionNames.Pause);
            map.Enable();
        }

        void DisableMap()
        {
            map?.Disable();
        }

        void Update()
        {
            DetectSchemeFromDevices();
            var deviceMove = ReadVector(moveAction);
            Move = Vector2.ClampMagnitude(deviceMove + virtualMove, 1f);
            LookDelta = ReadLookDegrees();
            SprintHeld = IsHeld(sprintAction) || virtualSprint;
            CrouchHeld = IsHeld(crouchAction);
            InteractPressed = WasPressed(interactAction) || pendingVirtualInteract;
            InteractHeld = IsHeld(interactAction) || pendingVirtualInteract || virtualInteractHeld;
            DropPressed = WasPressed(dropAction) || pendingVirtualDrop;
            ThrowPressed = WasPressed(throwAction) || pendingVirtualThrow;
            ReadRotation();
            InspectPressed = WasPressed(inspectAction) || pendingVirtualInspect;
            HintPressed = WasPressed(hintAction) || pendingVirtualHint;
            PausePressed = WasPressed(pauseAction) || pendingVirtualPause;
            HasPointerAim = hasPendingPointerAim;
            PointerAimPosition = pendingPointerAim;
            ClearPendingVirtualInput();
        }

        void ReadRotation()
        {
            var raw = ReadFloat(rotateAction);
            var fromScrollWheel = rotateAction?.activeControl?.device is Mouse;
            RotateSteps = fromScrollWheel && Mathf.Abs(raw) > 0.01f ? (int)Mathf.Sign(raw) : 0;
            RotateAxis = Mathf.Clamp((fromScrollWheel ? 0f : raw) + virtualRotate, -1f, 1f);
        }

        Vector2 ReadLookDegrees()
        {
            var raw = ReadVector(lookAction);
            Vector2 degrees;
            var device = lookAction?.activeControl?.device;
            if (device is Gamepad)
            {
                degrees = raw * gamepadDegreesPerSecond * Time.unscaledDeltaTime;
            }
            else
            {
                degrees = raw * mouseDegreesPerPixel;
            }

            degrees += pendingVirtualLook * touchDegreesPerPixel;
            degrees *= lookSensitivity;
            if (invertY)
            {
                degrees.y = -degrees.y;
            }

            return degrees;
        }

        void DetectSchemeFromDevices()
        {
            if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame && IsGamepadActive(Gamepad.current))
            {
                SetScheme(TidyInputScheme.Gamepad);
                return;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                SetScheme(TidyInputScheme.Touch);
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var keyboardUsed = keyboard != null && keyboard.anyKey.wasPressedThisFrame;
            var mouseUsed = mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.delta.ReadValue().sqrMagnitude > 4f);
            if ((keyboardUsed || mouseUsed) && !IsTouchSimulatedMouse())
            {
                SetScheme(TidyInputScheme.KeyboardMouse);
            }
        }

        static bool IsGamepadActive(Gamepad gamepad)
        {
            const float StickThreshold = 0.25f;
            return gamepad.leftStick.ReadValue().sqrMagnitude > StickThreshold * StickThreshold
                || gamepad.rightStick.ReadValue().sqrMagnitude > StickThreshold * StickThreshold
                || gamepad.buttonSouth.wasPressedThisFrame
                || gamepad.buttonEast.wasPressedThisFrame
                || gamepad.buttonWest.wasPressedThisFrame
                || gamepad.buttonNorth.wasPressedThisFrame
                || gamepad.startButton.wasPressedThisFrame
                || gamepad.leftTrigger.wasPressedThisFrame
                || gamepad.rightTrigger.wasPressedThisFrame;
        }

        bool IsTouchSimulatedMouse()
        {
            return ActiveScheme == TidyInputScheme.Touch && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        }

        void SetScheme(TidyInputScheme scheme)
        {
            if (ForcedScheme.HasValue)
            {
                return;
            }

            SetSchemeInternal(scheme);
        }

        void SetSchemeInternal(TidyInputScheme scheme)
        {
            if (ActiveScheme == scheme)
            {
                return;
            }

            ActiveScheme = scheme;
            SchemeChanged?.Invoke(scheme);
        }

        void ClearPendingVirtualInput()
        {
            pendingVirtualLook = Vector2.zero;
            pendingVirtualInteract = false;
            pendingVirtualDrop = false;
            pendingVirtualThrow = false;
            pendingVirtualInspect = false;
            pendingVirtualHint = false;
            pendingVirtualPause = false;
            hasPendingPointerAim = false;
        }

        static Vector2 ReadVector(InputAction action)
        {
            return action != null && action.enabled ? action.ReadValue<Vector2>() : Vector2.zero;
        }

        static float ReadFloat(InputAction action)
        {
            return action != null && action.enabled ? action.ReadValue<float>() : 0f;
        }

        static bool WasPressed(InputAction action)
        {
            return action != null && action.enabled && action.WasPressedThisFrame();
        }

        static bool IsHeld(InputAction action)
        {
            return action != null && action.enabled && action.IsPressed();
        }
    }
}
