using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class OpponentAI : MonoBehaviour
{
    [SerializeField] private Transform ball;
    [SerializeField] private Transform swingPoint;
    [SerializeField] private float swingRadius = 1.05f;
    [SerializeField] private Vector2 normalSwingBoxSize = new Vector2(1.75f, 2.1f);
    [SerializeField] private Vector2 recoverySwingOffset = new Vector2(0.82f, 0.62f);
    [SerializeField] private Vector2 recoverySwingBoxSize = new Vector2(2.45f, 2.55f);
    [SerializeField] private float courtMinX = 0.8f;
    [SerializeField] private float courtMaxX = 8.45f;
    [SerializeField] private float homeX = 5.7f;
    [SerializeField] private float predictionTime = 0.52f;
    [SerializeField] private float chaseBehindPadding = 0.35f;
    [SerializeField] private float emergencyThinkDistance = 0.35f;
    [SerializeField] private float emergencyTargetLead = 0.85f;
    [SerializeField] private float moveAcceleration = 8.5f;
    [SerializeField] private float moveDeceleration = 12f;
    [SerializeField] private float jumpForce = 6.2f;
    [SerializeField] private float jumpCooldown = 0.45f;
    [SerializeField] private float overheadJumpHeight = 1.15f;
    [SerializeField] private Animator animator;

    private Rigidbody2D body;
    private OpponentProfile profile;
    private BallController ballController;
    private float targetX;
    private float nextSwingTime;
    private float nextJumpTime;
    private bool grounded;
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int JSwingHash = Animator.StringToHash("JSwing");
    private static readonly int KSmashHash = Animator.StringToHash("KSmash");

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void Init(Transform ballTransform, OpponentProfile opponentProfile)
    {
        ball = ballTransform;
        ballController = ballTransform.GetComponent<BallController>();
        if (ballController != null && TryGetComponent(out Collider2D opponentCollider))
        {
            ballController.IgnoreBodyCollision(opponentCollider);
        }

        profile = opponentProfile;
        targetX = homeX;
        StopAllCoroutines();
        StartCoroutine(ThinkLoop());
    }

    private IEnumerator ThinkLoop()
    {
        while (true)
        {
            var delay = profile != null ? profile.reactionDelay : 0.4f;
            yield return new WaitForSeconds(delay);

            if (ball == null || profile == null || ballController == null)
            {
                continue;
            }

            if (ballController.IsHeldForServe)
            {
                targetX = homeX;
                continue;
            }

            var ballPosition = ball.position;
            var ballVelocity = ballController.Velocity;
            var ballInOpponentCourt = ballPosition.x > 0f;
            var ballIsComing = ballVelocity.x > 0.4f;
            if (!ballInOpponentCourt && !ballIsComing)
            {
                targetX = homeX;
                continue;
            }

            var mistakeOffset = Random.value < profile.mistakeRate ? Random.Range(-2.4f, 2.4f) : Random.Range(-0.45f, 0.45f);
            var predictedX = PredictDefensiveX(ballPosition, ballVelocity);

            targetX = Mathf.Clamp(predictedX + mistakeOffset, courtMinX, courtMaxX);
        }
    }

    private void FixedUpdate()
    {
        if (profile == null)
        {
            return;
        }

        UpdateEmergencyChaseTarget();

        var delta = targetX - body.position.x;
        var targetSpeed = Mathf.Abs(delta) > 0.15f ? Mathf.Sign(delta) * profile.moveSpeed : 0f;
        var acceleration = Mathf.Abs(targetSpeed) > 0.01f ? moveAcceleration : moveDeceleration;
        var speed = Mathf.MoveTowards(body.velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
        body.velocity = new Vector2(speed, body.velocity.y);
        animator?.SetFloat(MoveXHash, speed);
        animator?.SetBool(GroundedHash, grounded);

        var position = body.position;
        position.x = Mathf.Clamp(position.x, courtMinX, courtMaxX);
        body.position = position;
    }

    private void Update()
    {
        if (profile == null)
        {
            return;
        }

        TryJumpForOverheadBall();

        if (Time.time < nextSwingTime)
        {
            return;
        }

        if (ball == null || ball.position.x < courtMinX - 0.35f)
        {
            return;
        }

        if (TryHitBall(swingPoint.position, normalSwingBoxSize))
        {
            return;
        }

        if (IsBallBehind())
        {
            TryHitBall((Vector2)swingPoint.position + recoverySwingOffset, recoverySwingBoxSize);
        }
    }

    private void UpdateEmergencyChaseTarget()
    {
        if (ball == null || ballController == null || ballController.IsHeldForServe)
        {
            return;
        }

        var ballPosition = ball.position;
        if (ballPosition.x <= 0f)
        {
            return;
        }

        if (IsBallBehind() || Mathf.Abs(ballPosition.x - body.position.x) <= emergencyThinkDistance)
        {
            targetX = Mathf.Clamp(PredictDefensiveX(ballPosition, ballController.Velocity), courtMinX, courtMaxX);
        }
    }

    private bool IsBallBehind()
    {
        return ball != null && ball.position.x > body.position.x + chaseBehindPadding;
    }

    private float PredictDefensiveX(Vector3 ballPosition, Vector2 ballVelocity)
    {
        var predictedX = ballPosition.x + ballVelocity.x * predictionTime;
        if (ballPosition.x > body.position.x + chaseBehindPadding)
        {
            predictedX = Mathf.Max(predictedX, ballPosition.x + emergencyTargetLead);
        }

        return predictedX;
    }

    private bool TryHitBall(Vector2 center, Vector2 boxSize)
    {
        var hits = Physics2D.OverlapBoxAll(center, boxSize, 0f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out BallController ballController) && !ballController.IsHeldForServe)
            {
                var power = Mathf.Lerp(5.5f, 7.4f, profile.aggression);
                var vertical = Mathf.Lerp(0.55f, 0.95f, profile.hitAccuracy);
                var error = Random.Range(-0.7f, 0.7f) * (1f - profile.hitAccuracy);
                var hitStyle = grounded ? PawHitStyle.Rally : PawHitStyle.Smash;
                ballController.Hit(new Vector2(-1f, vertical + error).normalized * power, BallTouchSide.Opponent, hitStyle);
                animator?.SetTrigger(grounded ? JSwingHash : KSmashHash);
                nextSwingTime = Time.time + 0.28f;
                return true;
            }
        }

        return false;
    }

    private void TryJumpForOverheadBall()
    {
        if (!grounded || Time.time < nextJumpTime || ball == null || ballController == null || ballController.IsHeldForServe)
        {
            return;
        }

        var ballPosition = ball.position;
        var ballIsReachableHorizontally = Mathf.Abs(ballPosition.x - body.position.x) < 1.35f;
        var ballIsOverhead = ballPosition.y > swingPoint.position.y + overheadJumpHeight;
        if (ballPosition.x > 0f && ballIsReachableHorizontally && ballIsOverhead)
        {
            body.velocity = new Vector2(body.velocity.x, jumpForce);
            grounded = false;
            nextJumpTime = Time.time + jumpCooldown;
            animator?.SetTrigger(JumpHash);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            grounded = true;
            animator?.SetBool(GroundedHash, true);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            grounded = true;
            animator?.SetBool(GroundedHash, true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (swingPoint == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(swingPoint.position, normalSwingBoxSize);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube((Vector2)swingPoint.position + recoverySwingOffset, recoverySwingBoxSize);
    }
}
