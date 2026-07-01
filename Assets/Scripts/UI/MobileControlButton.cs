using CatTennis.Rebuild.Cat;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatTennis.Rebuild.UI
{
    public sealed class MobileControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private MobileControlAction action;
        [SerializeField] private PlayerInputReader inputReader;

        public void Configure(MobileControlAction controlAction, PlayerInputReader reader)
        {
            action = controlAction;
            inputReader = reader;
        }

        public void Bind(PlayerInputReader reader)
        {
            inputReader = reader;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (inputReader == null)
            {
                return;
            }

            switch (action)
            {
                case MobileControlAction.Left:
                    inputReader.SetMobileLeftHeld(true);
                    break;
                case MobileControlAction.Right:
                    inputReader.SetMobileRightHeld(true);
                    break;
                case MobileControlAction.Hit:
                    inputReader.PressMobileSwing();
                    break;
                case MobileControlAction.Smash:
                    inputReader.PressMobileSmash();
                    break;
                case MobileControlAction.Jump:
                    inputReader.PressMobileJump();
                    break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ReleaseHold();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ReleaseHold();
        }

        private void OnDisable()
        {
            ReleaseHold();
        }

        private void ReleaseHold()
        {
            if (inputReader == null)
            {
                return;
            }

            if (action == MobileControlAction.Left)
            {
                inputReader.SetMobileLeftHeld(false);
            }
            else if (action == MobileControlAction.Right)
            {
                inputReader.SetMobileRightHeld(false);
            }
        }
    }
}
