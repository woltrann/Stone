using TidyUpFramework.Core;
using TidyUpFramework.Input;
using UnityEngine;

namespace TidyUpFramework.Player
{
    [AddComponentMenu("Tidy Up Framework/Player/Cursor Controller")]
    public sealed class TidyCursorController : MonoBehaviour
    {
        [SerializeField, Tooltip("Lock and hide the cursor during gameplay on desktop.")]
        bool lockDuringGameplay = true;

        ITidyPlayerInput input;

        void OnEnable()
        {
            TidyControlLock.Changed += HandleLockChanged;
            Refresh();
        }

        void OnDisable()
        {
            TidyControlLock.Changed -= HandleLockChanged;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (input == null)
            {
                TidyServices.TryGet(out input);
            }

            if (Cursor.lockState != CursorLockMode.Locked && ShouldLock() && UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Refresh();
            }
        }

        void HandleLockChanged(TidyControlChannel channels)
        {
            Refresh();
        }

        bool ShouldLock()
        {
            var touch = input != null && input.ActiveScheme == TidyInputScheme.Touch;
            return lockDuringGameplay && !touch && !Application.isMobilePlatform && !TidyControlLock.IsLocked(TidyControlChannel.Cursor);
        }

        void Refresh()
        {
            var shouldLock = ShouldLock();
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }
    }
}
