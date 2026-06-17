using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private void Awake()
    {
        if (startButton == null)
        {
            startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }
    }

    public void StartGame()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        if (TournamentManager.Instance == null)
        {
            new GameObject("TournamentManager").AddComponent<TournamentManager>();
        }

        TournamentManager.Instance.StartTournament();
        SceneManager.LoadScene("Match");
    }
}
