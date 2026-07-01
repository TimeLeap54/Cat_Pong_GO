using CatTennis.Rebuild.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CatTennis.Rebuild.UI
{
    public sealed class RallyGameOverPresenter : MonoBehaviour
    {
        private const string BestRallyPrefsKey = "CatTennis.Rally.BestCount";

        [SerializeField] private RallyFlowManager rallyFlow;
        [SerializeField] private MatchBootstrapper bootstrapper;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject newBestPanelRoot;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text finalScoreLabel;
        [SerializeField] private TMP_Text finalScoreValue;
        [SerializeField] private TMP_Text bestScoreLabel;
        [SerializeField] private TMP_Text bestScoreValue;
        [SerializeField] private TMP_Text newBestScoreValue;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button newBestRetryButton;
        [SerializeField] private Button newBestMainMenuButton;
        [SerializeField] private RallyHudLanguage language = RallyHudLanguage.English;

        private long trackedPointId = -1;
        private int bestAtPointStart;

        private void Awake()
        {
            ApplyLanguage();
            Hide();

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(Retry);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }

            if (newBestRetryButton != null)
            {
                newBestRetryButton.onClick.AddListener(Retry);
            }

            if (newBestMainMenuButton != null)
            {
                newBestMainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        private void OnDestroy()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(Retry);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            }

            if (newBestRetryButton != null)
            {
                newBestRetryButton.onClick.RemoveListener(Retry);
            }

            if (newBestMainMenuButton != null)
            {
                newBestMainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            }
        }

        private void Update()
        {
            TrackPointStart();
        }

        public void Bind(RallyFlowManager flow, MatchBootstrapper matchBootstrapper)
        {
            rallyFlow = flow;
            bootstrapper = matchBootstrapper;
            TrackPointStart(force: true);
        }

        public void SetLanguage(RallyHudLanguage nextLanguage)
        {
            language = nextLanguage;
            ApplyLanguage();
        }

        public static bool NotifyRallyPointEnded(int finalScore)
        {
            RallyGameOverPresenter[] presenters = Object.FindObjectsOfType<RallyGameOverPresenter>(true);
            if (presenters.Length == 0)
            {
                Debug.LogWarning("Rally Game Over UI was requested, but no RallyGameOverPresenter exists in the open scene.");
                return false;
            }

            for (int i = 0; i < presenters.Length; i++)
            {
                presenters[i].ShowIfNotBest(finalScore);
            }

            return true;
        }

        private void ShowIfNotBest(int finalScore)
        {
            int storedBest = PlayerPrefs.GetInt(BestRallyPrefsKey, 0);
            if (finalScore > bestAtPointStart)
            {
                if (finalScore > storedBest)
                {
                    PlayerPrefs.SetInt(BestRallyPrefsKey, finalScore);
                    PlayerPrefs.Save();
                }

                if (newBestScoreValue != null)
                {
                    newBestScoreValue.text = finalScore.ToString();
                }

                ShowNewBest();
                return;
            }

            int bestScore = Mathf.Max(storedBest, bestAtPointStart);
            if (finalScoreValue != null)
            {
                finalScoreValue.text = finalScore.ToString();
            }

            if (bestScoreValue != null)
            {
                bestScoreValue.text = bestScore.ToString();
            }

            Time.timeScale = 0f;
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        private void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (newBestPanelRoot != null)
            {
                newBestPanelRoot.SetActive(false);
            }
        }

        private void ShowNewBest()
        {
            Time.timeScale = 0f;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (newBestPanelRoot != null)
            {
                newBestPanelRoot.SetActive(true);
            }
        }

        private void Retry()
        {
            Hide();
            Time.timeScale = 1f;
            bootstrapper?.RetryMatch();
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            bootstrapper?.ReturnToMainMenu();
        }

        private void TrackPointStart(bool force = false)
        {
            if (rallyFlow == null || !rallyFlow.HasActivePoint)
            {
                return;
            }

            if (!force && trackedPointId == rallyFlow.GlobalPointId)
            {
                return;
            }

            trackedPointId = rallyFlow.GlobalPointId;
            bestAtPointStart = PlayerPrefs.GetInt(BestRallyPrefsKey, 0);
            Hide();
        }

        private void ApplyLanguage()
        {
            if (titleLabel != null)
            {
                titleLabel.text = GetTitleText(language);
            }

            if (finalScoreLabel != null)
            {
                finalScoreLabel.text = GetFinalScoreText(language);
            }

            if (bestScoreLabel != null)
            {
                bestScoreLabel.text = GetBestScoreText(language);
            }
        }

        private static string GetTitleText(RallyHudLanguage selectedLanguage)
        {
            switch (selectedLanguage)
            {
                case RallyHudLanguage.Korean:
                    return "\uB7A0\uB9AC \uC885\uB8CC";
                case RallyHudLanguage.Japanese:
                    return "\u30E9\u30EA\u30FC\u7D42\u4E86";
                case RallyHudLanguage.Chinese:
                    return "\u56DE\u5408\u7ED3\u675F";
                default:
                    return "RALLY OVER";
            }
        }

        private static string GetFinalScoreText(RallyHudLanguage selectedLanguage)
        {
            switch (selectedLanguage)
            {
                case RallyHudLanguage.Korean:
                    return "\uCD5C\uC885 \uC810\uC218";
                case RallyHudLanguage.Japanese:
                    return "\u6700\u7D42\u30B9\u30B3\u30A2";
                case RallyHudLanguage.Chinese:
                    return "\u6700\u7EC8\u5F97\u5206";
                default:
                    return "FINAL SCORE";
            }
        }

        private static string GetBestScoreText(RallyHudLanguage selectedLanguage)
        {
            switch (selectedLanguage)
            {
                case RallyHudLanguage.Korean:
                    return "\uCD5C\uACE0 \uC810\uC218";
                case RallyHudLanguage.Japanese:
                    return "\u30D9\u30B9\u30C8\u30B9\u30B3\u30A2";
                case RallyHudLanguage.Chinese:
                    return "\u6700\u4F73\u5F97\u5206";
                default:
                    return "BEST SCORE";
            }
        }
    }
}
