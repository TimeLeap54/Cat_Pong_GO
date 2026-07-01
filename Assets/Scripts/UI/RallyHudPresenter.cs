using CatTennis.Rebuild.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CatTennis.Rebuild.UI
{
    public enum RallyHudLanguage
    {
        English = 0,
        Korean = 1,
        Japanese = 2,
        Chinese = 3
    }

    public sealed class RallyHudPresenter : MonoBehaviour
    {
        private const string BestRallyPrefsKey = "CatTennis.Rally.BestCount";

        [SerializeField] private RallyFlowManager rallyFlow;
        [SerializeField] private TMP_Text rallyLabel;
        [SerializeField] private TMP_Text rallyValue;
        [SerializeField] private TMP_Text bestLabel;
        [SerializeField] private TMP_Text bestValue;
        [SerializeField] private Button pauseButton;
        [SerializeField] private PauseMenuPresenter pauseMenu;
        [SerializeField] private RallyHudLanguage language = RallyHudLanguage.English;

        private int lastRallyCount = -1;
        private int bestRallyCount;
        private bool paused;

        private void Awake()
        {
            bestRallyCount = PlayerPrefs.GetInt(BestRallyPrefsKey, 0);
            ApplyLanguage();
            ApplyCounts(force: true);

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(TogglePause);
            }
        }

        private void OnDestroy()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(TogglePause);
            }

            if (paused)
            {
                Time.timeScale = 1f;
            }
        }

        private void Update()
        {
            ApplyCounts(force: false);
        }

        public void Bind(RallyFlowManager flow)
        {
            rallyFlow = flow;
            ApplyCounts(force: true);
        }

        public void SetLanguage(RallyHudLanguage nextLanguage)
        {
            language = nextLanguage;
            ApplyLanguage();
        }

        public void RegisterPauseMenu(PauseMenuPresenter presenter)
        {
            pauseMenu = presenter;
        }

        private void ApplyLanguage()
        {
            if (rallyLabel != null)
            {
                rallyLabel.text = GetRallyText(language);
            }

            if (bestLabel != null)
            {
                bestLabel.text = GetBestText(language);
            }
        }

        private void ApplyCounts(bool force)
        {
            int rallyCount = rallyFlow != null ? rallyFlow.RallyHitCount : 0;
            if (!force && rallyCount == lastRallyCount)
            {
                return;
            }

            lastRallyCount = rallyCount;
            if (rallyValue != null)
            {
                rallyValue.text = rallyCount.ToString();
            }

            if (rallyCount > bestRallyCount)
            {
                bestRallyCount = rallyCount;
                PlayerPrefs.SetInt(BestRallyPrefsKey, bestRallyCount);
                PlayerPrefs.Save();
            }

            if (bestValue != null)
            {
                bestValue.text = bestRallyCount.ToString();
            }
        }

        private void TogglePause()
        {
            if (pauseMenu != null)
            {
                pauseMenu.Show();
                return;
            }

            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        private static string GetRallyText(RallyHudLanguage selectedLanguage)
        {
            switch (selectedLanguage)
            {
                case RallyHudLanguage.Korean:
                    return "\uB7A0\uB9AC";
                case RallyHudLanguage.Japanese:
                    return "\u30E9\u30EA\u30FC";
                case RallyHudLanguage.Chinese:
                    return "\u56DE\u5408";
                default:
                    return "RALLY";
            }
        }

        private static string GetBestText(RallyHudLanguage selectedLanguage)
        {
            switch (selectedLanguage)
            {
                case RallyHudLanguage.Korean:
                    return "\uCD5C\uACE0";
                case RallyHudLanguage.Japanese:
                    return "\u30D9\u30B9\u30C8";
                case RallyHudLanguage.Chinese:
                    return "\u6700\u4F73";
                default:
                    return "BEST";
            }
        }
    }
}
