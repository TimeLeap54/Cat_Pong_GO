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
    [SerializeField] private float outOfBoundsY = -3.2f;
    [SerializeField] private float outOfBoundsX = 8.6f;

    private int playerScore;
    private int opponentScore;
    private bool pointResolving;
    private bool matchEnded;

    private void Awake()
    {
        UpgradeSerializedDefaults();
        EnsureCourtLayout();
    }

    private void UpgradeSerializedDefaults()
    {
        if (Mathf.Approximately(outOfBoundsX, 10.4f))
        {
            outOfBoundsX = 8.6f;
        }

        if (Mathf.Approximately(outOfBoundsY, -2.8f))
        {
            outOfBoundsY = -3.2f;
        }
    }

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

        if (ball.ConsumeDoubleTouchFault(out var faultSide))
        {
            ScoreAgainst(faultSide);
            return;
        }

        if (ball.ConsumeSideWallTouch())
        {
            ScoreOutAgainstLastTouch();
            return;
        }

        if (ball.ConsumeGroundTouch(out var groundPosition))
        {
            ResolveGroundTouch(groundPosition);
            return;
        }

        var ballPosition = ball.transform.position;
        if (ballPosition.y < outOfBoundsY)
        {
            ResolveGroundTouch(ballPosition);
            return;
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

    private void ScoreAgainst(BallTouchSide faultSide)
    {
        if (faultSide == BallTouchSide.Player)
        {
            ScoreOpponent();
        }
        else if (faultSide == BallTouchSide.Opponent)
        {
            ScorePlayer();
        }
    }

    private void ResolveGroundTouch(Vector2 groundPosition)
    {
        var landedOutside = Mathf.Abs(groundPosition.x) > outOfBoundsX;
        if (landedOutside)
        {
            ScoreOutAgainstLastTouch(groundPosition.x);
            return;
        }

        ScoreByCourtSide(groundPosition.x);
    }

    private void ScoreOutAgainstLastTouch(float fallbackX = 0f)
    {
        if (ball.LastTouchSide == BallTouchSide.None)
        {
            ScoreByCourtSide(fallbackX);
            return;
        }

        ScoreAgainst(ball.LastTouchSide);
    }

    private void ScoreByCourtSide(float x)
    {
        if (x < 0f)
        {
            ScoreOpponent();
        }
        else
        {
            ScorePlayer();
        }
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

    private void EnsureCourtLayout()
    {
        var courtSprite = FindCourtSprite();
        ConfigureCourtBox("GroundBase", new Vector2(0f, -1.35f), new Vector2(24f, 0.7f), new Color(0.12f, 0.34f, 0.27f), true, courtSprite, "Court");
        ConfigureCourtBox("CourtInBounds", new Vector2(0f, -0.96f), new Vector2(17.2f, 0.12f), new Color(0.18f, 0.56f, 0.36f), false, courtSprite);
        ConfigureCourtBox("LeftOutLine", new Vector2(-8.6f, -0.84f), new Vector2(0.08f, 0.42f), new Color(0.96f, 0.96f, 0.86f), false, courtSprite);
        ConfigureCourtBox("RightOutLine", new Vector2(8.6f, -0.84f), new Vector2(0.08f, 0.42f), new Color(0.96f, 0.96f, 0.86f), false, courtSprite);
        ConfigureCourtBox("CenterCourtLine", new Vector2(0f, -0.84f), new Vector2(0.05f, 0.32f), new Color(0.9f, 0.9f, 0.82f), false, courtSprite);
        ConfigureGoalZone("LeftScoreZone", new Vector2(-4.3f, -0.86f), new Vector2(8.6f, 0.22f));
        ConfigureGoalZone("RightScoreZone", new Vector2(4.3f, -0.86f), new Vector2(8.6f, 0.22f));
    }

    private Sprite FindCourtSprite()
    {
        var renderer = GameObject.Find("GroundBase")?.GetComponent<SpriteRenderer>()
            ?? GameObject.Find("Court")?.GetComponent<SpriteRenderer>();
        return renderer != null ? renderer.sprite : null;
    }

    private void ConfigureCourtBox(string name, Vector2 position, Vector2 size, Color color, bool collider, Sprite sprite, string fallbackName = null)
    {
        var obj = GameObject.Find(name);
        if (obj == null && !string.IsNullOrEmpty(fallbackName))
        {
            obj = GameObject.Find(fallbackName);
            if (obj != null)
            {
                obj.name = name;
            }
        }

        if (obj == null)
        {
            obj = new GameObject(name);
        }

        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = obj.GetComponent<SpriteRenderer>() ?? obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;

        var box = obj.GetComponent<BoxCollider2D>();
        if (collider)
        {
            if (box == null)
            {
                box = obj.AddComponent<BoxCollider2D>();
            }

            box.isTrigger = false;

            if (obj.CompareTag("Untagged"))
            {
                obj.tag = "Ground";
            }
        }
        else if (box != null)
        {
            Destroy(box);
        }
    }

    private void ConfigureGoalZone(string name, Vector2 position, Vector2 size)
    {
        var zone = GameObject.Find(name);
        if (zone == null)
        {
            return;
        }

        zone.transform.position = new Vector3(position.x, position.y, 0f);
        var box = zone.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = size;
            box.isTrigger = true;
        }
    }
}
