using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 7.25f;
    [SerializeField] private float minX = -8.2f;
    [SerializeField] private float maxX = -0.8f;
    [SerializeField] private Transform swingPoint;
    [SerializeField] private Vector2 swingBoxSize = new Vector2(3.15f, 3.35f);
    [SerializeField] private Vector2 spikeSwingOffset = new Vector2(0.12f, 0.72f);
    [SerializeField] private Vector2 spikeSwingBoxSize = new Vector2(2.45f, 2.2f);
    [SerializeField] private Vector2 dropSwingDirection = new Vector2(1f, 0.86f);
    [SerializeField] private Vector2 normalLiftSwingDirection = new Vector2(1f, 0.72f);
    [SerializeField] private Vector2 strongLiftSwingDirection = new Vector2(1f, 0.56f);
    [SerializeField] private float dropTapTime = 0.08f;
    [SerializeField] private float strongHoldTime = 0.45f;
    [SerializeField] private float dropSwingPower = 6.1f;
    [SerializeField] private float liftSwingPower = 9.8f;
    [SerializeField] private float strongLiftSwingPower = 11.2f;
    [SerializeField] private float spikeSwingPower = 12f;
    [SerializeField] private Vector2 upwardServeVelocity = new Vector2(8.6f, 5.8f);
    [SerializeField] private Vector2 spikeServeVelocity = new Vector2(10.4f, -0.65f);
    [SerializeField] private float minServePowerMultiplier = 0.78f;
    [SerializeField] private float maxServePowerMultiplier = 1.35f;
    [SerializeField] private float fullChargeTime = 0.9f;
    [SerializeField] private float swingCooldown = 0.22f;
    [SerializeField] private BallController serveBall;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.72f, 0.08f);
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private float jumpGroundLockout = 0.12f;

    private Rigidbody2D body;
    private bool grounded;
    private int jumpsRemaining;
    private float nextSwingTime;
    private float groundCheckLockedUntil;
    private bool chargingServe;
    private bool chargingLiftSwing;
    private float serveChargeStartedAt;
    private float liftSwingStartedAt;
    private Vector2 chargedServeVelocity;
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int JSwingHash = Animator.StringToHash("JSwing");
    private static readonly int KSmashHash = Animator.StringToHash("KSmash");

    private void Awake()
    {
        UpgradeSerializedDefaults();
        body = GetComponent<Rigidbody2D>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        jumpsRemaining = maxJumpCount;
    }

    private void Start()
    {
        if (serveBall != null && TryGetComponent(out Collider2D playerCollider))
        {
            serveBall.IgnoreBodyCollision(playerCollider);
        }
    }

    private void UpgradeSerializedDefaults()
    {
        if (Mathf.Approximately(jumpForce, 6.6f))
        {
            jumpForce = 7.25f;
        }

        if (Approximately(upwardServeVelocity, new Vector2(9.5f, 6.4f)))
        {
            upwardServeVelocity = new Vector2(8.6f, 5.8f);
        }

        if (Approximately(spikeServeVelocity, new Vector2(11f, -2.2f)))
        {
            spikeServeVelocity = new Vector2(10.4f, -0.65f);
        }

        if (Approximately(spikeServeVelocity, new Vector2(10.2f, -1.6f)))
        {
            spikeServeVelocity = new Vector2(10.4f, -0.65f);
        }

        if (Approximately(swingBoxSize, new Vector2(2.75f, 2.95f)))
        {
            swingBoxSize = new Vector2(3.15f, 3.35f);
        }

        if (Mathf.Approximately(dropTapTime, 0.13f))
        {
            dropTapTime = 0.08f;
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

        if (Input.GetKeyDown(KeyCode.Space) && jumpsRemaining > 0)
        {
            body.velocity = new Vector2(body.velocity.x, jumpForce);
            grounded = false;
            jumpsRemaining--;
            groundCheckLockedUntil = Time.time + jumpGroundLockout;
            animator?.SetTrigger(JumpHash);
        }

        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
        {
            if (TryStartServeCharge(upwardServeVelocity))
            {
                return;
            }

            TryStartLiftSwingCharge();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (TryStartServeCharge(spikeServeVelocity))
            {
                return;
            }

            if (Time.time >= nextSwingTime)
            {
                animator?.SetTrigger(KSmashHash);
                Swing(new Vector2(1f, -0.08f).normalized * spikeSwingPower, spikeSwingOffset, spikeSwingBoxSize);
            }
        }

        if (Input.GetKeyUp(KeyCode.J) || Input.GetMouseButtonUp(0))
        {
            CompleteLiftSwingCharge();
            CompleteServeCharge();
        }

        if (Input.GetKeyUp(KeyCode.K))
        {
            CompleteServeCharge();
        }
    }

    private void FixedUpdate()
    {
        var input = Input.GetAxisRaw("Horizontal");
        body.velocity = new Vector2(input * moveSpeed, body.velocity.y);
        animator?.SetFloat(MoveXHash, input);
        animator?.SetBool(GroundedHash, grounded);

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

    private void TryStartLiftSwingCharge()
    {
        if (Time.time < nextSwingTime)
        {
            return;
        }

        chargingLiftSwing = true;
        liftSwingStartedAt = Time.time;
    }

    private void CompleteLiftSwingCharge()
    {
        if (!chargingLiftSwing || serveBall != null && serveBall.IsHeldForServe)
        {
            chargingLiftSwing = false;
            return;
        }

        var holdTime = Time.time - liftSwingStartedAt;
        chargingLiftSwing = false;

        if (Time.time < nextSwingTime)
        {
            return;
        }

        if (holdTime <= dropTapTime)
        {
            animator?.SetTrigger(JSwingHash);
            Swing(dropSwingDirection.normalized * dropSwingPower);
            return;
        }

        if (holdTime >= strongHoldTime)
        {
            animator?.SetTrigger(JSwingHash);
            Swing(strongLiftSwingDirection.normalized * strongLiftSwingPower);
            return;
        }

        animator?.SetTrigger(JSwingHash);
        Swing(normalLiftSwingDirection.normalized * liftSwingPower);
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
                jumpsRemaining = maxJumpCount;
                return;
            }
        }
    }

    private void Swing(Vector2 velocity)
    {
        Swing(velocity, Vector2.zero, swingBoxSize);
    }

    private void Swing(Vector2 velocity, Vector2 pointOffset, Vector2 boxSize)
    {
        nextSwingTime = Time.time + swingCooldown;

        var swingCenter = (Vector2)swingPoint.position + pointOffset;
        var hits = Physics2D.OverlapBoxAll(swingCenter, boxSize, 0f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out BallController ball))
            {
                ball.Hit(velocity, BallTouchSide.Player);
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
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube((Vector2)swingPoint.position + spikeSwingOffset, spikeSwingBoxSize);
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
