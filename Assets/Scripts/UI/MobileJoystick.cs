using CatTennis.Rebuild.Cat;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatTennis.Rebuild.UI
{
    public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform baseRect;
        [SerializeField] private RectTransform knobRect;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private float deadZone = 0.16f;
        [SerializeField] private float knobTravelRatio = 0.42f;
        [SerializeField] private float jumpThreshold = 0.72f;
        [SerializeField] private bool upTriggersJump = true;

        private bool jumpArmed = true;

        public void Configure(RectTransform joystickBase, RectTransform knob, PlayerInputReader reader)
        {
            baseRect = joystickBase;
            knobRect = knob;
            inputReader = reader;
        }

        public void Bind(PlayerInputReader reader)
        {
            inputReader = reader;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateJoystick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateJoystick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetJoystick();
        }

        private void OnDisable()
        {
            ResetJoystick();
        }

        private void UpdateJoystick(PointerEventData eventData)
        {
            if (baseRect == null || knobRect == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    baseRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            float radius = Mathf.Min(baseRect.rect.width, baseRect.rect.height) * 0.5f;
            Vector2 normalized = radius > 0f ? Vector2.ClampMagnitude(localPoint / radius, 1f) : Vector2.zero;
            if (normalized.magnitude < deadZone)
            {
                normalized = Vector2.zero;
            }

            knobRect.anchoredPosition = normalized * radius * knobTravelRatio;
            inputReader?.SetMobileMove(normalized);

            if (upTriggersJump && normalized.y >= jumpThreshold && jumpArmed)
            {
                inputReader?.PressMobileJump();
                jumpArmed = false;
            }
            else if (normalized.y < jumpThreshold * 0.55f)
            {
                jumpArmed = true;
            }
        }

        private void ResetJoystick()
        {
            if (knobRect != null)
            {
                knobRect.anchoredPosition = Vector2.zero;
            }

            inputReader?.SetMobileMove(Vector2.zero);
            jumpArmed = true;
        }
    }
}
