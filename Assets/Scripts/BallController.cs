using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    [SerializeField] private Transform serveAnchor;
    [SerializeField] private Vector2 serveOffset = new Vector2(0.28f, 0.08f);

    private Rigidbody2D body;
    private Vector3 startPosition;
    private RigidbodyType2D defaultBodyType;
    private float defaultGravityScale;

    public bool IsHeldForServe { get; private set; }
    public Vector2 Velocity => body.velocity;

    private void Awake()
    {
        UpgradeSerializedDefaults();
        body = GetComponent<Rigidbody2D>();
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

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f;
    }
}
