using UnityEngine;

public class GoalZone : MonoBehaviour
{
    [SerializeField] private bool playerSide;
    [SerializeField] private MatchManager matchManager;

    public void Init(MatchManager manager, bool isPlayerSide)
    {
        matchManager = manager;
        playerSide = isPlayerSide;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out BallController ball))
        {
            return;
        }

        if (ball.IsHeldForServe)
        {
            return;
        }

        if (playerSide)
        {
            matchManager.ScoreOpponent();
        }
        else
        {
            matchManager.ScorePlayer();
        }
    }
}
