using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    [SerializeField] private Transform serveAnchor;
    [SerializeField] private Vector2 serveOffset = new Vector2(0.85f, 0.45f);
    [SerializeField] private Vector2 minServeOffset = new Vector2(0.45f, -0.25f);
    [SerializeField] private Vector2 maxServeOffset = new Vector2(1.45f, 1.35f);
    [SerializeField] private float serveAimSpeed = 1.7f;

    private Rigidbody2D body;
    private Vector3 startPosition;
    private RigidbodyType2D defaultBodyType;
    private float defaultGravityScale;

    public bool IsHeldForServe { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        defaultBodyType = body.bodyType;
        defaultGravityScale = body.gravityScale;
    }

    private void Start()
    {
        BeginServe();
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

        if (serveAnchor == null)
        {
            transform.position = startPosition;
            return;
        }

        body.position = (Vector2)serveAnchor.position + serveOffset;
    }

    public void AdjustServeOffset(Vector2 input, float deltaTime)
    {
        if (!IsHeldForServe)
        {
            return;
        }

        serveOffset += input * serveAimSpeed * deltaTime;
        serveOffset.x = Mathf.Clamp(serveOffset.x, minServeOffset.x, maxServeOffset.x);
        serveOffset.y = Mathf.Clamp(serveOffset.y, minServeOffset.y, maxServeOffset.y);
    }

    public void LaunchServe(Vector2 velocity)
    {
        IsHeldForServe = false;
        body.bodyType = defaultBodyType;
        body.gravityScale = defaultGravityScale;
        Hit(velocity);
    }

    public void Hit(Vector2 velocity)
    {
        IsHeldForServe = false;
        body.bodyType = defaultBodyType;
        body.gravityScale = defaultGravityScale;
        body.velocity = velocity;
    }
}
