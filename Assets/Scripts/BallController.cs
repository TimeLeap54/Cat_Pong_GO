using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    [SerializeField] private Vector2 initialVelocity = new Vector2(5.5f, 4.5f);

    private Rigidbody2D body;
    private Vector3 startPosition;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    private void Start()
    {
        Serve();
    }

    public void Serve()
    {
        body.velocity = initialVelocity;
    }

    public void ResetBall()
    {
        transform.position = startPosition;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        Serve();
    }

    public void Hit(Vector2 velocity)
    {
        body.velocity = velocity;
    }
}
