using System.Collections;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    [SerializeField] private int pointsToWin = 5;
    [SerializeField] private BallController ball;
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private Transform player;
    [SerializeField] private Transform opponent;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private Transform opponentSpawn;
    [SerializeField] private Transform serveAnchor;
    [SerializeField] private OpponentAI opponentAI;
    [SerializeField] private float outOfBoundsY = -2.8f;
    [SerializeField] private float outOfBoundsX = 10.4f;

    private int playerScore;
    private int opponentScore;
    private bool pointResolving;
    private bool matchEnded;

    private void Start()
    {
        if (TournamentManager.Instance == null)
        {
            new GameObject("TournamentManager").AddComponent<TournamentManager>().StartTournament();
        }

        opponentAI.Init(ball.transform, GameState.CurrentOpponent);

        if (serveAnchor != null)
        {
            ball.SetServeAnchor(serveAnchor);
        }

        UpdateScoreUI();
    }

    private void Update()
    {
        if (pointResolving || matchEnded || ball.IsHeldForServe)
        {
            return;
        }

        var ballPosition = ball.transform.position;
        if (ballPosition.y < outOfBoundsY)
        {
            if (ballPosition.x < 0f)
            {
                ScoreOpponent();
            }
            else
            {
                ScorePlayer();
            }

            return;
        }

        if (ballPosition.x < -outOfBoundsX)
        {
            ScoreOpponent();
            return;
        }

        if (ballPosition.x > outOfBoundsX)
        {
            ScorePlayer();
        }
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

        if (opponent != null && opponentSpawn != null)
        {
            opponent.position = opponentSpawn.position;
            if (opponent.TryGetComponent(out Rigidbody2D opponentBody))
            {
                opponentBody.velocity = Vector2.zero;
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
        scoreUI.ShowResult(playerWon, playerWon && GameState.IsFinalRound);
    }

    private void UpdateScoreUI()
    {
        scoreUI.SetScore(playerScore, opponentScore, GameState.CurrentOpponent.displayName);
    }
}
