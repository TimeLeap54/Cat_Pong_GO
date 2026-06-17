using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text roundText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        resultPanel.SetActive(false);
        nextButton.onClick.AddListener(NextRound);
        restartButton.onClick.AddListener(Restart);
    }

    public void SetScore(int playerScore, int opponentScore, string roundName)
    {
        scoreText.text = $"{playerScore} : {opponentScore}";
        roundText.text = roundName;
    }

    public void ShowResult(bool playerWon, bool tournamentWon)
    {
        if (tournamentWon)
        {
            resultText.text = "Tournament Champion!";
        }
        else
        {
            resultText.text = playerWon ? "You Win!" : "Game Over";
        }

        nextButton.gameObject.SetActive(playerWon && !tournamentWon);
        resultPanel.SetActive(true);
    }

    private void NextRound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        TournamentManager.Instance.ContinueAfterWin();
    }

    private void Restart()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        TournamentManager.Instance.RestartTournament();
    }
}
