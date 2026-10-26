using TidyUpFramework.Core;
using TidyUpFramework.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Mobile/Touch Look Area")]
    public sealed class TidyTouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField, Min(0f), Tooltip("Seconds a touch may last and still count as a tap.")] float tapMaximumSeconds = 0.25f;
        [SerializeField, Min(0f), Tooltip("Pixels a touch may travel and still count as a tap.")] float tapMaximumPixels = 18f;
        [SerializeField, Tooltip("Tap aims at the touched point instead of the screen centre.")] bool tapAimsAtFinger = true;

        TidyInputReader reader;
        int pointerId = int.MinValue;
        float pressTime;
        Vector2 pressPosition;
        float travelled;
        bool isHolding;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != int.MinValue)
            {
                return;
            }

            pointerId = eventData.pointerId;
            pressTime = Time.unscaledTime;
            pressPosition = eventData.position;
            travelled = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId)
            {
                return;
            }

            travelled += eventData.delta.magnitude;
            Reader()?.AddVirtualLookPixels(eventData.delta);
        }

        void Update()
        {
            if (pointerId == int.MinValue || isHolding)
            {
                return;
            }

            if (Time.unscaledTime - pressTime > tapMaximumSeconds && travelled <= tapMaximumPixels)
            {
                isHolding = true;
                Reader()?.SetVirtualInteractHeld(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId)
            {
                return;
            }

            pointerId = int.MinValue;
            if (isHolding)
            {
                isHolding = false;
                Reader()?.SetVirtualInteractHeld(false);
                return;
            }

            var isTap = Time.unscaledTime - pressTime <= tapMaximumSeconds && travelled <= tapMaximumPixels;
            if (!isTap)
            {
                return;
            }

            var tapReader = Reader();
            if (tapReader == null)
            {
                return;
            }

            if (tapAimsAtFinger)
            {
                tapReader.RegisterTap(pressPosition);
            }
            else
            {
                tapReader.TriggerVirtual(TidyInputAction.Interact);
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
