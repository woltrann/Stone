using TidyUpFramework.Core;
using TidyUpFramework.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Mobile/Virtual Joystick")]
    public sealed class TidyVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform background;
        [SerializeField] RectTransform handle;
        [SerializeField, Min(10f)] float radius = 90f;
        [SerializeField, Range(0f, 0.5f)] float deadZone = 0.08f;
        [SerializeField, Range(0.5f, 1f), Tooltip("Deflection above which the player sprints.")] float sprintThreshold = 0.95f;
        [SerializeField, Tooltip("Moves the joystick to wherever the finger first touches inside this area.")] bool floating = true;

        TidyInputReader reader;
        Vector2 restPosition;
        int pointerId = int.MinValue;

        void Awake()
        {
            if (background != null)
            {
                restPosition = background.anchoredPosition;
            }
        }

        void OnDisable()
        {
            Release();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != int.MinValue)
            {
                return;
            }

            pointerId = eventData.pointerId;
            if (floating && background != null && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)background.parent, eventData.position, eventData.pressEventCamera, out var local))
            {
                background.anchoredPosition = local;
            }

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId || background == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out var local);
            var offset = Vector2.ClampMagnitude(local, radius);
            if (handle != null)
            {
                handle.anchoredPosition = offset;
            }

            var value = offset / radius;
            if (value.magnitude < deadZone)
            {
                value = Vector2.zero;
            }

            Reader()?.SetVirtualMove(value);
            Reader()?.SetVirtualSprint(value.magnitude >= sprintThreshold);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId)
            {
                Release();
            }
        }

        void Release()
        {
            pointerId = int.MinValue;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            if (background != null && floating)
            {
                background.anchoredPosition = restPosition;
            }

            Reader()?.SetVirtualMove(Vector2.zero);
            Reader()?.SetVirtualSprint(false);
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
