using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    [SerializeField] private Transform serveAnchor;
    [SerializeField] private Vector2 serveOffset = new Vector2(0.28f, 0.08f);
    [SerializeField] private float maxSpeed = 10.5f;
    [SerializeField] private float minHorizontalSpeed = 1.2f;
    [SerializeField] private float netDamping = 0.62f;
    [SerializeField] private float netHorizontalPush = 2.3f;
    [SerializeField] private float stuckSpeedThreshold = 0.55f;

    private Rigidbody2D body;
    private Collider2D ballCollider;
    private SpriteRenderer ballRenderer;
    private Vector3 startPosition;
    private RigidbodyType2D defaultBodyType;
    private float defaultGravityScale;
    private BallTouchSide lastTouchSide = BallTouchSide.None;
    private bool crossedNetSinceLastTouch;
    private bool doubleTouchFaultPending;
    private BallTouchSide doubleTouchFaultSide;
    private bool groundTouchPending;
    private Vector2 pendingGroundTouchPosition;
    private bool sideWallTouchPending;
    private bool serveTossActive;
    private float serveTossStartedAt;
    private float serveTossDuration;
    private float serveTossHeight;
    private readonly List<Collider2D> ignoredBodyColliders = new List<Collider2D>();

    public bool IsHeldForServe { get; private set; }
    public Vector2 Velocity => body.velocity;
    public BallTouchSide LastTouchSide => lastTouchSide;

    private void Awake()
    {
        UpgradeSerializedDefaults();
        body = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<Collider2D>();
        ballRenderer = GetComponent<SpriteRenderer>();
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

    private void FixedUpdate()
    {
        if (IsHeldForServe)
        {
            return;
        }

        ClampVelocity();
    }

    private void LateUpdate()
    {
        if (!IsHeldForServe || serveAnchor == null)
        {
            return;
        }

        var tossOffset = 0f;
        if (serveTossActive)
        {
            var progress = Mathf.Clamp01((Time.time - serveTossStartedAt) / Mathf.Max(serveTossDuration, 0.01f));
            tossOffset = 4f * serveTossHeight * progress * (1f - progress);
        }

        body.position = (Vector2)serveAnchor.position + serveOffset + Vector2.up * tossOffset;
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

    public void IgnoreBodyCollision(Collider2D bodyCollider)
    {
        if (ballCollider == null || bodyCollider == null)
        {
            return;
        }

        if (!ignoredBodyColliders.Contains(bodyCollider))
        {
            ignoredBodyColliders.Add(bodyCollider);
        }

        Physics2D.IgnoreCollision(ballCollider, bodyCollider, true);
    }

    public void BeginServe()
    {
        IsHeldForServe = true;
        serveTossActive = false;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        SetColliderEnabled(false);
        SetRendererEnabled(false);
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
        Hit(velocity, BallTouchSide.Player, PawHitStyle.Serve);
    }

    public void LaunchServe(Vector2 velocity, PawHitStyle hitStyle)
    {
        Hit(velocity, BallTouchSide.Player, hitStyle);
    }

    public void BeginServeToss(float duration, float height)
    {
        if (!IsHeldForServe)
        {
            return;
        }

        serveTossActive = true;
        serveTossStartedAt = Time.time;
        serveTossDuration = Mathf.Max(duration, 0.01f);
        serveTossHeight = Mathf.Max(0f, height);
        SetRendererEnabled(true);
    }

    public void Hit(Vector2 velocity)
    {
        Hit(velocity, BallTouchSide.None, PawHitStyle.Rally);
    }

    public void Hit(Vector2 velocity, BallTouchSide touchSide)
    {
        var style = velocity.magnitude >= 11.5f ? PawHitStyle.Smash : PawHitStyle.Rally;
        Hit(velocity, touchSide, style);
    }

    public void Hit(Vector2 velocity, BallTouchSide touchSide, PawHitStyle hitStyle)
    {
        IsHeldForServe = false;
        serveTossActive = false;
        body.bodyType = defaultBodyType;
        body.gravityScale = defaultGravityScale;
        SetColliderEnabled(true);
        SetRendererEnabled(true);

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

        body.velocity = SanitizeVelocity(velocity);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPawHit(hitStyle);
        }
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

    public bool ConsumeSideWallTouch()
    {
        if (!sideWallTouchPending)
        {
            return false;
        }

        sideWallTouchPending = false;
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
            ApplyIgnoredBodyCollisions();
        }
    }

    private void SetRendererEnabled(bool enabled)
    {
        if (ballRenderer != null)
        {
            ballRenderer.enabled = enabled;
        }
    }

    private void ApplyIgnoredBodyCollisions()
    {
        if (ballCollider == null)
        {
            return;
        }

        foreach (var ignoredCollider in ignoredBodyColliders)
        {
            if (ignoredCollider != null)
            {
                Physics2D.IgnoreCollision(ballCollider, ignoredCollider, true);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsHeldForServe)
        {
            return;
        }

        if (collision.collider.name == "BoundaryLeft" || collision.collider.name == "BoundaryRight")
        {
            sideWallTouchPending = true;
            return;
        }

        if (collision.collider.CompareTag("Net"))
        {
            DeflectFromNet();
            return;
        }

        if (!collision.collider.CompareTag("Ground"))
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
        sideWallTouchPending = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsHeldForServe || !collision.collider.CompareTag("Net"))
        {
            return;
        }

        if (body.velocity.magnitude < stuckSpeedThreshold)
        {
            DeflectFromNet();
        }
    }

    private Vector2 SanitizeVelocity(Vector2 velocity)
    {
        if (velocity.sqrMagnitude <= 0.001f)
        {
            return Vector2.zero;
        }

        var speed = Mathf.Min(velocity.magnitude, maxSpeed);
        var direction = velocity.normalized;
        var minHorizontalRatio = Mathf.Clamp01(minHorizontalSpeed / Mathf.Max(speed, 0.001f));
        if (Mathf.Abs(direction.x) < minHorizontalRatio)
        {
            direction.x = direction.x < 0f ? -minHorizontalRatio : minHorizontalRatio;
            direction.Normalize();
        }

        return direction * speed;
    }

    private void ClampVelocity()
    {
        body.velocity = SanitizeVelocity(body.velocity);
    }

    private void DeflectFromNet()
    {
        var side = transform.position.x < 0f ? -1f : 1f;
        var velocity = body.velocity * netDamping;
        velocity.x = side * Mathf.Max(Mathf.Abs(velocity.x), netHorizontalPush);
        velocity.y = Mathf.Max(velocity.y, 1.25f);
        body.velocity = SanitizeVelocity(velocity);
        body.position += new Vector2(side * 0.08f, 0.04f);
    }
}

public enum PawHitStyle
{
    Soft,
    Rally,
    Smash,
    Serve
}
