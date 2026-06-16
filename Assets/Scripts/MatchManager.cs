using System.Collections;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    [SerializeField] private int pointsToWin = 5;
    [SerializeField] private BallController ball;
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private Transform serveAnchor;

    private int playerScore;
    private int opponentScore;
    private bool pointResolving;
    private bool matchEnded;

    private void Start()
    {
        if (serveAnchor != null)
        {
            ball.SetServeAnchor(serveAnchor);
        }

        UpdateScoreUI();
    }

    public void ScorePlayer()
    {
        if (pointResolving || matchEnded)
        {
            return;
        }

        playerScore++;
        ResolvePoint();
    }

    public void ScoreOpponent()
    {
        if (pointResolving || matchEnded)
        {
            return;
        }

        opponentScore++;
        ResolvePoint();
    }

    private void ResolvePoint()
    {
        pointResolving = true;
        UpdateScoreUI();

        if (playerScore >= pointsToWin)
        {
            EndMatch(true);
            return;
        }

        if (opponentScore >= pointsToWin)
        {
            EndMatch(false);
            return;
        }

        StartCoroutine(ResetAfterDelay());
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(0.8f);
        ResetPoint();
        pointResolving = false;
    }

    private void ResetPoint()
    {
        if (player != null && playerSpawn != null)
        {
            player.position = playerSpawn.position;
            if (player.TryGetComponent(out Rigidbody2D playerBody))
            {
                playerBody.velocity = Vector2.zero;
            }
        }

        if (serveAnchor != null)
        {
            ball.SetServeAnchor(serveAnchor);
        }

        ball.ResetBall();
    }

    private void EndMatch(bool playerWon)
    {
        matchEnded = true;
        scoreUI.ShowResult(playerWon);
    }

    private void UpdateScoreUI()
    {
        scoreUI.SetScore(playerScore, opponentScore);
    }
}
