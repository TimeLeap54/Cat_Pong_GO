using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float minX = -8.2f;
    [SerializeField] private float maxX = -0.8f;

    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        var input = Input.GetAxisRaw("Horizontal");
        body.velocity = new Vector2(input * moveSpeed, body.velocity.y);

        var position = body.position;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        body.position = position;
    }
}
