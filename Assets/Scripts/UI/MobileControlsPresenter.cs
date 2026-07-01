using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Flow;
using UnityEngine;

namespace CatTennis.Rebuild.UI
{
    public sealed class MobileControlsPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private MobileJoystick joystick;
        [SerializeField] private MobileControlButton[] buttons;
        [SerializeField] private bool rallyModeOnly = true;

        private void Awake()
        {
            if (rallyModeOnly && !MatchBootstrapper.SelectedRallyMode)
            {
                gameObject.SetActive(false);
                return;
            }

            BindButtons();
        }

        public void Bind(PlayerInputReader reader)
        {
            inputReader = reader;
            BindButtons();
        }

        private void BindButtons()
        {
            if (inputReader == null)
            {
                return;
            }

            if (joystick != null)
            {
                joystick.Bind(inputReader);
            }

            if (buttons == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].Bind(inputReader);
                }
            }
        }
    }
}
