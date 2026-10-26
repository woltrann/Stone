using TidyUpFramework.Core;
using TidyUpFramework.Input;
using TidyUpFramework.Interaction;
using UnityEngine;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Mobile/Mobile Controls")]
    public sealed class TidyMobileControls : MonoBehaviour
    {
        [SerializeField, Tooltip("Root shown only while the touch scheme is active.")]
        GameObject controlsRoot;

        [SerializeField, Tooltip("Buttons shown only while an object is held, such as Drop, Rotate and Inspect.")]
        GameObject heldObjectButtons;

        ITidyPlayerInput input;
        TidyInteractor interactor;

        void Start()
        {
            if (TidyServices.TryGet(out input))
            {
                input.SchemeChanged += HandleSchemeChanged;
            }

            if (TidyServices.TryGet(out interactor))
            {
                interactor.HeldChanged += HandleHeldChanged;
            }

            Refresh();
            HandleHeldChanged(interactor != null ? interactor.HeldObject : null);
        }

        void OnDestroy()
        {
            if (input != null)
            {
                input.SchemeChanged -= HandleSchemeChanged;
            }

            if (interactor != null)
            {
                interactor.HeldChanged -= HandleHeldChanged;
            }
        }

        void HandleSchemeChanged(TidyInputScheme scheme)
        {
            Refresh();
        }

        void HandleHeldChanged(Objects.TidyObject held)
        {
            if (heldObjectButtons != null)
            {
                heldObjectButtons.SetActive(held != null);
            }
        }

        void Refresh()
        {
            var touch = input != null ? input.ActiveScheme == TidyInputScheme.Touch : Application.isMobilePlatform;
            if (controlsRoot != null)
            {
                controlsRoot.SetActive(touch);
            }
        }
    }
}
