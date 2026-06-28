using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTennis.Rebuild.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject modePanel;

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
