using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4.6f;
    [SerializeField] private float moveAcceleration = 20f;
    [SerializeField] private float moveDeceleration = 28f;
    [SerializeField, Range(0f, 1f)] private float airControlMultiplier = 0.78f;
    [SerializeField] private float jumpForce = 7.25f;
    [SerializeField] private float minX = -8.2f;
    [SerializeField] private float maxX = -0.8f;
    [SerializeField] private Transform swingPoint;
    [SerializeField] private Vector2 swingBoxSize = new Vector2(2.2f, 2.3f);
    [SerializeField] private Vector2 spikeSwingOffset = new Vector2(0.08f, 0.82f);
    [SerializeField] private Vector2 spikeSwingBoxSize = new Vector2(1.9f, 1.75f);
    [Header("Continuous J Swing")]
    [SerializeField] private float jFullChargeTime = 0.9f;
    [SerializeField] private float minLiftSwingPower = 4.8f;
    [SerializeField] private float maxLiftSwingPower = 9.4f;
    [SerializeField] private float highLiftRatio = 1.02f;
    [SerializeField] private float lowLiftRatio = 0.46f;
    [SerializeField] private float verticalAimInfluence = 0.28f;
    [Header("K Smash Rhythm")]
    [SerializeField] private float spikeSwingPower = 8.4f;
    [SerializeField] private float chargedSpikePower = 10.8f;
    [SerializeField] private int smashHitsToCharge = 3;
    [SerializeField] private float smashChainWindow = 6f;
    [SerializeField] private Vector2 smashRecoilPerLevel = new Vector2(-0.35f, 0.12f);
    [SerializeField] private Vector2 upwardServeVelocity = new Vector2(7.3f, 4.9f);
    [SerializeField] private Vector2 spikeServeVelocity = new Vector2(8.8f, -0.35f);
    [SerializeField] private float minServePowerMultiplier = 0.78f;
    [SerializeField] private float maxServePowerMultiplier = 1.35f;
    [SerializeField] private float fullChargeTime = 0.9f;
    [SerializeField] private float swingCooldown = 0.22f;
    [SerializeField] private float swingInputBuffer = 0.14f;
    [SerializeField] private BallController serveBall;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.72f, 0.08f);
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private float jumpGroundLockout = 0.12f;
    [Header("Wall Step")]
    [SerializeField] private float wallContactDistance = 0.12f;
    [SerializeField] private float wallSlideSpeed = 2.4f;
    [SerializeField] private Vector2 wallKickVelocity = new Vector2(6.2f, 8.1f);
    [SerializeField] private float wallKickControlLock = 0.16f;
    [Header("Serve Presentation")]
    [SerializeField] private float serveTossDuration = 0.3f;
    [SerializeField] private float serveTossHeight = 0.72f;
    [SerializeField] private float serveSwingLeadTime = 0.16f;

    private Rigidbody2D body;
    private bool grounded;
    private int jumpsRemaining;
    private float nextSwingTime;
    private float groundCheckLockedUntil;
    private bool chargingServe;
    private bool chargingLiftSwing;
    private float serveChargeStartedAt;
    private float liftSwingStartedAt;
    private float liftAimInput;
    private Vector2 chargedServeVelocity;
    private bool chargedServeIsSmash;
    private bool servingInProgress;
    private bool wallKickAvailable = true;
    private float wallKickControlLockedUntil;
    private bool swingWindowActive;
    private float swingWindowEndsAt;
    private Vector2 bufferedSwingVelocity;
    private Vector2 bufferedSwingOffset;
    private Vector2 bufferedSwingBoxSize;
    private PawHitStyle bufferedHitStyle;
    private bool bufferedBuildsSmashRhythm;
    private int bufferedSmashLevel;
    private int smashRhythmHits;
    private float lastSmashHitTime;
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
        UpdateSwingWindow();

        if (Input.GetKeyDown(KeyCode.Space) && TryWallKick())
        {
            return;
        }

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
            if (TryStartServeCharge(upwardServeVelocity, false))
            {
                return;
            }

            TryStartLiftSwingCharge();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (TryStartServeCharge(spikeServeVelocity, true))
            {
                return;
            }

            if (Time.time >= nextSwingTime)
            {
                var smashLevel = GetNextSmashLevel();
                var smashProgress = smashHitsToCharge <= 1 ? 1f : (smashLevel - 1f) / (smashHitsToCharge - 1f);
                var power = Mathf.Lerp(spikeSwingPower, chargedSpikePower, smashProgress);
                var downwardRatio = Mathf.Lerp(-0.03f, -0.3f, smashProgress);
                animator?.SetTrigger(KSmashHash);
                Swing(
                    new Vector2(1f, downwardRatio).normalized * power,
                    spikeSwingOffset,
                    spikeSwingBoxSize,
                    smashLevel >= smashHitsToCharge ? PawHitStyle.Smash : PawHitStyle.Rally,
                    true,
                    smashLevel);
            }
        }

        if (chargingLiftSwing)
        {
            liftAimInput = Input.GetAxisRaw("Vertical");
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
        var verticalVelocity = body.velocity.y;
        if (!grounded && IsTouchingBackWall() && input < 0f && verticalVelocity < -wallSlideSpeed)
        {
            verticalVelocity = -wallSlideSpeed;
        }

        var targetHorizontalVelocity = input * moveSpeed;
        var acceleration = Mathf.Abs(input) > 0.01f ? moveAcceleration : moveDeceleration;
        if (!grounded)
        {
            acceleration *= airControlMultiplier;
        }

        var horizontalVelocity = Time.time < wallKickControlLockedUntil
            ? body.velocity.x
            : Mathf.MoveTowards(body.velocity.x, targetHorizontalVelocity, acceleration * Time.fixedDeltaTime);
        if (Time.time >= wallKickControlLockedUntil && IsTouchingBackWall() && input < 0f)
        {
            horizontalVelocity = 0f;
        }

        body.velocity = new Vector2(horizontalVelocity, verticalVelocity);
        animator?.SetFloat(MoveXHash, input);
        animator?.SetBool(GroundedHash, grounded);

        var position = body.position;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        body.position = position;
    }

    private bool TryStartServeCharge(Vector2 velocity, bool isSmash)
    {
        if (serveBall == null || !serveBall.IsHeldForServe || servingInProgress || Time.time < nextSwingTime)
        {
            return false;
        }

        chargingServe = true;
        serveChargeStartedAt = Time.time;
        chargedServeVelocity = velocity;
        chargedServeIsSmash = isSmash;
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
        StartCoroutine(PerformServe(chargedServeVelocity * powerMultiplier, chargedServeIsSmash));
        chargingServe = false;
    }

    private System.Collections.IEnumerator PerformServe(Vector2 velocity, bool isSmash)
    {
        servingInProgress = true;
        nextSwingTime = Time.time + serveTossDuration + swingCooldown;
        serveBall.BeginServeToss(serveTossDuration, serveTossHeight);

        yield return new WaitForSeconds(Mathf.Max(0f, serveSwingLeadTime));
        animator?.SetTrigger(isSmash ? KSmashHash : JSwingHash);

        yield return new WaitForSeconds(Mathf.Max(0f, serveTossDuration - serveSwingLeadTime));
        if (serveBall != null && serveBall.IsHeldForServe)
        {
            serveBall.LaunchServe(velocity, isSmash ? PawHitStyle.Smash : PawHitStyle.Serve);
        }

        servingInProgress = false;
    }

    private void TryStartLiftSwingCharge()
    {
        if (Time.time < nextSwingTime)
        {
            return;
        }

        chargingLiftSwing = true;
        liftSwingStartedAt = Time.time;
        liftAimInput = 0f;
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

        var charge = Mathf.Clamp01(holdTime / Mathf.Max(jFullChargeTime, 0.01f));
        var shapedCharge = charge * charge * (3f - 2f * charge);
        var power = Mathf.Lerp(minLiftSwingPower, maxLiftSwingPower, shapedCharge);
        var liftRatio = Mathf.Lerp(highLiftRatio, lowLiftRatio, shapedCharge);
        liftRatio = Mathf.Clamp(liftRatio + liftAimInput * verticalAimInfluence, 0.16f, 1.28f);
        var hitStyle = charge < 0.18f ? PawHitStyle.Soft : charge > 0.82f ? PawHitStyle.Smash : PawHitStyle.Rally;
        animator?.SetTrigger(JSwingHash);
        Swing(new Vector2(1f, liftRatio).normalized * power, Vector2.zero, swingBoxSize, hitStyle);
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
                wallKickAvailable = true;
                return;
            }
        }
    }

    private void Swing(Vector2 velocity)
    {
        Swing(velocity, Vector2.zero, swingBoxSize, PawHitStyle.Rally);
    }

    private void Swing(
        Vector2 velocity,
        Vector2 pointOffset,
        Vector2 boxSize,
        PawHitStyle hitStyle,
        bool buildsSmashRhythm = false,
        int smashLevel = 0)
    {
        nextSwingTime = Time.time + swingCooldown;
        bufferedSwingVelocity = velocity;
        bufferedSwingOffset = pointOffset;
        bufferedSwingBoxSize = boxSize;
        bufferedHitStyle = hitStyle;
        bufferedBuildsSmashRhythm = buildsSmashRhythm;
        bufferedSmashLevel = smashLevel;
        swingWindowEndsAt = Time.time + swingInputBuffer;
        swingWindowActive = !TryStrikeBufferedBall();
    }

    private void UpdateSwingWindow()
    {
        if (!swingWindowActive)
        {
            return;
        }

        if (Time.time > swingWindowEndsAt || TryStrikeBufferedBall())
        {
            swingWindowActive = false;
        }
    }

    private bool TryStrikeBufferedBall()
    {
        var swingCenter = (Vector2)swingPoint.position + bufferedSwingOffset;
        var hits = Physics2D.OverlapBoxAll(swingCenter, bufferedSwingBoxSize, 0f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out BallController ball))
            {
                ball.Hit(bufferedSwingVelocity, BallTouchSide.Player, bufferedHitStyle);
                if (bufferedBuildsSmashRhythm)
                {
                    CommitSmashRhythmHit();
                }
                return true;
            }
        }

        return false;
    }

    private int GetNextSmashLevel()
    {
        if (Time.time - lastSmashHitTime > smashChainWindow)
        {
            smashRhythmHits = 0;
        }

        return Mathf.Clamp(smashRhythmHits + 1, 1, Mathf.Max(1, smashHitsToCharge));
    }

    private void CommitSmashRhythmHit()
    {
        smashRhythmHits = bufferedSmashLevel;
        lastSmashHitTime = Time.time;
        body.velocity += smashRecoilPerLevel * smashRhythmHits;

        if (smashRhythmHits >= smashHitsToCharge)
        {
            smashRhythmHits = 0;
        }
    }

    private bool TryWallKick()
    {
        if (grounded || !wallKickAvailable || !IsTouchingBackWall())
        {
            return false;
        }

        body.velocity = wallKickVelocity;
        wallKickControlLockedUntil = Time.time + wallKickControlLock;
        wallKickAvailable = false;
        grounded = false;
        groundCheckLockedUntil = Time.time + jumpGroundLockout;
        animator?.SetTrigger(JumpHash);
        AudioManager.Instance?.PlayWallStep();
        return true;
    }

    private bool IsTouchingBackWall()
    {
        return body != null && body.position.x <= minX + wallContactDistance;
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
