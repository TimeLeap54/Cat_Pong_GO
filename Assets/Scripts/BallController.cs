using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    [SerializeField] private Transform serveAnchor;
    [SerializeField] private Vector2 serveOffset = new Vector2(0.28f, 0.08f);

    private Rigidbody2D body;
    private Collider2D ballCollider;
    private Vector3 startPosition;
    private RigidbodyType2D defaultBodyType;
    private float defaultGravityScale;
    private BallTouchSide lastTouchSide = BallTouchSide.None;
    private bool crossedNetSinceLastTouch;
    private bool doubleTouchFaultPending;
    private BallTouchSide doubleTouchFaultSide;
    private bool groundTouchPending;
    private Vector2 pendingGroundTouchPosition;

    public bool IsHeldForServe { get; private set; }
    public Vector2 Velocity => body.velocity;
    public BallTouchSide LastTouchSide => lastTouchSide;

    private void Awake()
    {
        UpgradeSerializedDefaults();
        body = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<Collider2D>();
        startPosition = transform.position;
        defaultBodyType = body.bodyType;
        defaultGravityScale = body.gravityScale;
    }

    private void UpgradeSerializedDefaults()
    {
        if (Approximately(serveOffset, new Vector2(0.85f, 0.45f)))
        {
            serveOffset = new Vector2(0.28f, 0.08f);
        }

        if (serveAnchor != null && Approximately((Vector2)serveAnchor.localPosition, new Vector2(0.58f, 0.22f)))
        {
            serveAnchor.localPosition = new Vector3(0.48f, 0.04f, 0f);
        }
    }

    private void Start()
    {
        BeginServe();
    }

    private void Update()
    {
        if (lastTouchSide == BallTouchSide.Player && transform.position.x > 0f)
        {
            crossedNetSinceLastTouch = true;
        }
        else if (lastTouchSide == BallTouchSide.Opponent && transform.position.x < 0f)
        {
            crossedNetSinceLastTouch = true;
        }
    }

    private void LateUpdate()
    {
        if (!IsHeldForServe || serveAnchor == null)
        {
            return;
        }

        body.position = (Vector2)serveAnchor.position + serveOffset;
        body.velocity = Vector2.zero;
    }

    public void ResetBall()
    {
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        ResetRallyState();
        BeginServe();
    }

    public void SetServeAnchor(Transform anchor)
    {
        serveAnchor = anchor;
        BeginServe();
    }

    public void BeginServe()
    {
        IsHeldForServe = true;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        SetColliderEnabled(false);
        ResetRallyState();

        if (serveAnchor == null)
        {
            transform.position = startPosition;
            return;
        }

        body.position = (Vector2)serveAnchor.position + serveOffset;
    }

    public void LaunchServe(Vector2 velocity)
    {
        Hit(velocity, BallTouchSide.Player);
    }

    public void Hit(Vector2 velocity)
    {
        Hit(velocity, BallTouchSide.None);
    }

    public void Hit(Vector2 velocity, BallTouchSide touchSide)
    {
        IsHeldForServe = false;
        body.bodyType = defaultBodyType;
        body.gravityScale = defaultGravityScale;
        SetColliderEnabled(true);

        if (touchSide != BallTouchSide.None)
        {
            if (lastTouchSide == touchSide && !crossedNetSinceLastTouch)
            {
                doubleTouchFaultPending = true;
                doubleTouchFaultSide = touchSide;
            }

            lastTouchSide = touchSide;
            crossedNetSinceLastTouch = false;
        }

        body.velocity = velocity;
    }

    public bool ConsumeGroundTouch(out Vector2 position)
    {
        position = pendingGroundTouchPosition;
        if (!groundTouchPending)
        {
            return false;
        }

        groundTouchPending = false;
        return true;
    }

    public bool ConsumeDoubleTouchFault(out BallTouchSide faultSide)
    {
        faultSide = doubleTouchFaultSide;
        if (!doubleTouchFaultPending)
        {
            return false;
        }

        doubleTouchFaultPending = false;
        return true;
    }

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f;
    }

    private void SetColliderEnabled(bool enabled)
    {
        if (ballCollider != null)
        {
            ballCollider.enabled = enabled;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsHeldForServe || !collision.collider.CompareTag("Ground"))
        {
            return;
        }

        groundTouchPending = true;
        pendingGroundTouchPosition = collision.GetContact(0).point;
    }

    private void ResetRallyState()
    {
        lastTouchSide = BallTouchSide.None;
        crossedNetSinceLastTouch = false;
        doubleTouchFaultPending = false;
        doubleTouchFaultSide = BallTouchSide.None;
        groundTouchPending = false;
    }
}
