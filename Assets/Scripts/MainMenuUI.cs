using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
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
