using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 6.6f;
    [SerializeField] private float minX = -8.2f;
    [SerializeField] private float maxX = -0.8f;
    [SerializeField] private Transform swingPoint;
    [SerializeField] private Vector2 swingBoxSize = new Vector2(2.75f, 2.95f);
    [SerializeField] private float liftSwingPower = 9.8f;
    [SerializeField] private float spikeSwingPower = 12f;
    [SerializeField] private Vector2 upwardServeVelocity = new Vector2(8.6f, 5.8f);
    [SerializeField] private Vector2 spikeServeVelocity = new Vector2(10.2f, -1.6f);
    [SerializeField] private float minServePowerMultiplier = 0.78f;
    [SerializeField] private float maxServePowerMultiplier = 1.35f;
    [SerializeField] private float fullChargeTime = 0.9f;
    [SerializeField] private float swingCooldown = 0.22f;
    [SerializeField] private BallController serveBall;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.72f, 0.08f);
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float jumpGroundLockout = 0.12f;

    private Rigidbody2D body;
    private bool grounded;
    private float nextSwingTime;
    private float groundCheckLockedUntil;
    private bool chargingServe;
    private float serveChargeStartedAt;
    private Vector2 chargedServeVelocity;

    private void Awake()
    {
        UpgradeSerializedDefaults();
        body = GetComponent<Rigidbody2D>();
    }

    private void UpgradeSerializedDefaults()
    {
        if (Approximately(upwardServeVelocity, new Vector2(9.5f, 6.4f)))
        {
            upwardServeVelocity = new Vector2(8.6f, 5.8f);
        }

        if (Approximately(spikeServeVelocity, new Vector2(11f, -2.2f)))
        {
            spikeServeVelocity = new Vector2(10.2f, -1.6f);
        }

        if (swingPoint != null && Approximately((Vector2)swingPoint.localPosition, new Vector2(0.72f, 0.3f)))
        {
            swingPoint.localPosition = new Vector3(0.88f, 0.18f, 0f);
        }

        if (swingPoint != null && Approximately((Vector2)swingPoint.localPosition, new Vector2(0.88f, 0.18f)))
        {
            swingPoint.localPosition = new Vector3(0.62f, 0.32f, 0f);
        }
    }

    private void Update()
    {
        UpdateGrounded();

        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            body.velocity = new Vector2(body.velocity.x, jumpForce);
            grounded = false;
            groundCheckLockedUntil = Time.time + jumpGroundLockout;
        }

        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
        {
            if (TryStartServeCharge(upwardServeVelocity))
            {
                return;
            }

            if (Time.time >= nextSwingTime)
            {
                Swing(new Vector2(1f, 0.72f).normalized * liftSwingPower);
            }
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (TryStartServeCharge(spikeServeVelocity))
            {
                return;
            }

            if (Time.time >= nextSwingTime)
            {
                Swing(new Vector2(1f, -0.25f).normalized * spikeSwingPower);
            }
        }

        if (Input.GetKeyUp(KeyCode.J) || Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.K))
        {
            CompleteServeCharge();
        }
    }

    private void FixedUpdate()
    {
        var input = Input.GetAxisRaw("Horizontal");
        body.velocity = new Vector2(input * moveSpeed, body.velocity.y);

        var position = body.position;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        body.position = position;
    }

    private bool TryStartServeCharge(Vector2 velocity)
    {
        if (serveBall == null || !serveBall.IsHeldForServe || Time.time < nextSwingTime)
        {
            return false;
        }

        chargingServe = true;
        serveChargeStartedAt = Time.time;
        chargedServeVelocity = velocity;
        return true;
    }

    private void CompleteServeCharge()
    {
        if (!chargingServe || serveBall == null || !serveBall.IsHeldForServe)
        {
            chargingServe = false;
            return;
        }

        var chargePercent = Mathf.Clamp01((Time.time - serveChargeStartedAt) / fullChargeTime);
        var powerMultiplier = Mathf.Lerp(minServePowerMultiplier, maxServePowerMultiplier, chargePercent);
        nextSwingTime = Time.time + swingCooldown;
        serveBall.LaunchServe(chargedServeVelocity * powerMultiplier);
        chargingServe = false;
    }

    private void UpdateGrounded()
    {
        if (groundCheck == null || Time.time < groundCheckLockedUntil)
        {
            return;
        }

        grounded = false;

        var hits = Physics2D.OverlapBoxAll(groundCheck.position, groundCheckSize, 0f, groundLayer);
        foreach (var hit in hits)
        {
            if (!hit.isTrigger && hit.CompareTag("Ground"))
            {
                grounded = true;
                return;
            }
        }
    }

    private void Swing(Vector2 velocity)
    {
        nextSwingTime = Time.time + swingCooldown;

        var hits = Physics2D.OverlapBoxAll(swingPoint.position, swingBoxSize, 0f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out BallController ball))
            {
                ball.Hit(velocity);
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (swingPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(swingPoint.position, swingBoxSize);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f;
    }
}
