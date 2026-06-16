using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 6.6f;
    [SerializeField] private float minX = -8.2f;
    [SerializeField] private float maxX = -0.8f;
    [SerializeField] private Transform swingPoint;
    [SerializeField] private float swingRadius = 1.05f;
    [SerializeField] private float swingPower = 12f;
    [SerializeField] private Vector2 upwardServeVelocity = new Vector2(9.5f, 6.4f);
    [SerializeField] private Vector2 spikeServeVelocity = new Vector2(11f, -2.2f);
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

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
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

        if ((Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) && Time.time >= nextSwingTime)
        {
            if (TryServe(upwardServeVelocity))
            {
                return;
            }

            Swing(new Vector2(1f, 0.72f).normalized * swingPower);
        }

        if (Input.GetKeyDown(KeyCode.K) && Time.time >= nextSwingTime)
        {
            if (TryServe(spikeServeVelocity))
            {
                return;
            }

            Swing(new Vector2(1f, -0.25f).normalized * swingPower);
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

    private bool TryServe(Vector2 velocity)
    {
        if (serveBall == null || !serveBall.IsHeldForServe)
        {
            return false;
        }

        nextSwingTime = Time.time + swingCooldown;
        serveBall.LaunchServe(velocity);
        return true;
    }

    private void UpdateGrounded()
    {
        if (groundCheck == null || Time.time < groundCheckLockedUntil)
        {
            return;
        }

        grounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    private void Swing(Vector2 velocity)
    {
        nextSwingTime = Time.time + swingCooldown;

        var hits = Physics2D.OverlapCircleAll(swingPoint.position, swingRadius);
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
            Gizmos.DrawWireSphere(swingPoint.position, swingRadius);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}
