using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultText;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        resultPanel.SetActive(false);
        restartButton.onClick.AddListener(Restart);
    }

    public void SetScore(int playerScore, int opponentScore)
    {
        scoreText.text = $"{playerScore} : {opponentScore}";
    }

    public void ShowResult(bool playerWon)
    {
        resultText.text = playerWon ? "You Win!" : "You Lose";
        resultPanel.SetActive(true);
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

