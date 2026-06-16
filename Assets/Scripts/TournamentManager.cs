using UnityEngine;
using UnityEngine.SceneManagement;

public class TournamentManager : MonoBehaviour
{
    public static TournamentManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartTournament()
    {
        GameState.StartTournament();
    }

    public void ContinueAfterWin()
    {
        if (GameState.AdvanceRound())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void RestartTournament()
    {
        GameState.StartTournament();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

