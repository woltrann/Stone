using TidyUpFramework.Core;
using TidyUpFramework.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Mobile/Touch Action Button")]
    public sealed class TidyTouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        TidyInputAction action = TidyInputAction.Drop;

        [SerializeField, Tooltip("Rotate buttons send a continuous axis while held. -1 or 1.")]
        float rotateDirection = 1f;

        TidyInputReader reader;
        bool isHeld;

        public void Configure(TidyInputAction newAction, float direction)
        {
            action = newAction;
            rotateDirection = direction;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            var target = Reader();
            if (target == null)
            {
                return;
            }

            isHeld = true;
            TidyTweenRunner.ScaleTo(transform, Vector3.one * 0.92f, 0.08f, TidyEaseType.OutQuad);
            if (action == TidyInputAction.Rotate)
            {
                target.SetVirtualRotate(rotateDirection);
            }
            else if (action == TidyInputAction.Sprint)
            {
                target.SetVirtualSprint(true);
            }
            else
            {
                target.TriggerVirtual(action);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        void OnDisable()
        {
            if (isHeld)
            {
                Release();
            }
        }

        void Release()
        {
            isHeld = false;
            TidyTweenRunner.ScaleTo(transform, Vector3.one, 0.12f, TidyEaseType.OutBack);
            var target = Reader();
            if (target == null)
            {
                return;
            }

            if (action == TidyInputAction.Rotate)
            {
                target.SetVirtualRotate(0f);
            }
            else if (action == TidyInputAction.Sprint)
            {
                target.SetVirtualSprint(false);
            }
        }

        TidyInputReader Reader()
        {
            if (reader == null)
            {
                TidyServices.TryGet(out reader);
            }

            return reader;
        }
    }
}
