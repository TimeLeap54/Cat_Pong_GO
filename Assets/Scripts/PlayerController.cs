using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 9f;
    [SerializeField] private float minX = -8.2f;
    [SerializeField] private float maxX = -0.8f;
    [SerializeField] private Transform swingPoint;
    [SerializeField] private float swingRadius = 1.05f;
    [SerializeField] private float swingPower = 12f;
    [SerializeField] private float swingCooldown = 0.22f;

    private Rigidbody2D body;
    private bool grounded;
    private float nextSwingTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            body.velocity = new Vector2(body.velocity.x, jumpForce);
            grounded = false;
        }

        if ((Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) && Time.time >= nextSwingTime)
        {
            Swing();
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

    private void Swing()
    {
        nextSwingTime = Time.time + swingCooldown;

        var hits = Physics2D.OverlapCircleAll(swingPoint.position, swingRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out BallController ball))
            {
                ball.Hit(new Vector2(1f, 0.72f).normalized * swingPower);
                break;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            grounded = true;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            grounded = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (swingPoint == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(swingPoint.position, swingRadius);
    }
}
