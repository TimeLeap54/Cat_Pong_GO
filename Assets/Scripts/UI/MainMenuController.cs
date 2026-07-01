using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTennis.Rebuild.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject modePanel;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject settingsPanel;

        private void Awake()
        {
            ShowMainMenu();
        }

        public void ShowModeSelection()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            if (modePanel != null)
            {
                modePanel.SetActive(true);
            }

            if (howToPlayPanel != null)
            {
                howToPlayPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void ShowMainMenu()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
            }

            if (modePanel != null)
            {
                modePanel.SetActive(false);
            }

            if (howToPlayPanel != null)
            {
                howToPlayPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void ShowHowToPlay()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            if (modePanel != null)
            {
                modePanel.SetActive(false);
            }

            if (howToPlayPanel != null)
            {
                howToPlayPanel.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void ShowSettings()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            if (modePanel != null)
            {
                modePanel.SetActive(false);
            }

            if (howToPlayPanel != null)
            {
                howToPlayPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        public void StartRally()
        {
            StartMatch(true);
        }

        public void StartTournament()
        {
            StartMatch(false);
        }

        public void StartMatch(bool isRally)
        {
            CatTennis.Rebuild.Flow.MatchBootstrapper.SelectedRallyMode = isRally;
            SceneManager.LoadScene("Rebuild_Match");
        }

        public void StartMatch()
        {
            StartRally();
        }
    }
}
