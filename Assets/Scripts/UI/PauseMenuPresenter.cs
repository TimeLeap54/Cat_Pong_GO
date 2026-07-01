using CatTennis.Rebuild.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace CatTennis.Rebuild.UI
{
    public sealed class PauseMenuPresenter : MonoBehaviour
    {
        [SerializeField] private MatchBootstrapper bootstrapper;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            Hide();

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(Resume);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(Restart);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(Resume);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(Restart);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            }

            if (panelRoot != null && panelRoot.activeSelf)
            {
                Time.timeScale = 1f;
            }
        }

        public void Bind(MatchBootstrapper matchBootstrapper)
        {
            bootstrapper = matchBootstrapper;
        }

        public void Show()
        {
            Time.timeScale = 0f;
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void Resume()
        {
            Hide();
            Time.timeScale = 1f;
        }

        private void Restart()
        {
            Hide();
            Time.timeScale = 1f;
            bootstrapper?.RetryMatch();
        }

        private void ReturnToMainMenu()
        {
            Hide();
            Time.timeScale = 1f;
            bootstrapper?.ReturnToMainMenu();
        }
    }
}
