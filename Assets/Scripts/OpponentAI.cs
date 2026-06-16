using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class OpponentAI : MonoBehaviour
{
    [SerializeField] private Transform ball;
    [SerializeField] private Transform swingPoint;
    [SerializeField] private float swingRadius = 1.05f;
    [SerializeField] private float courtMinX = 0.8f;
    [SerializeField] private float courtMaxX = 8.2f;

    private Rigidbody2D body;
    private OpponentProfile profile;
    private float targetX;
    private float nextSwingTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Init(Transform ballTransform, OpponentProfile opponentProfile)
    {
        ball = ballTransform;
        profile = opponentProfile;
        targetX = transform.position.x;
        StopAllCoroutines();
        StartCoroutine(ThinkLoop());
    }

    private IEnumerator ThinkLoop()
    {
        while (true)
        {
            var delay = profile != null ? profile.reactionDelay : 0.4f;
            yield return new WaitForSeconds(delay);

            if (ball == null || profile == null)
            {
                continue;
            }

            var mistakeOffset = Random.value < profile.mistakeRate ? Random.Range(-2.4f, 2.4f) : Random.Range(-0.45f, 0.45f);
            targetX = Mathf.Clamp(ball.position.x + mistakeOffset, courtMinX, courtMaxX);
        }
    }

    private void FixedUpdate()
    {
        if (profile == null)
        {
            return;
        }

        var delta = targetX - body.position.x;
        var speed = Mathf.Abs(delta) > 0.15f ? Mathf.Sign(delta) * profile.moveSpeed : 0f;
        body.velocity = new Vector2(speed, body.velocity.y);

        var position = body.position;
        position.x = Mathf.Clamp(position.x, courtMinX, courtMaxX);
        body.position = position;
    }

    private void Update()
    {
        if (profile == null || Time.time < nextSwingTime)
        {
            return;
        }

        var hits = Physics2D.OverlapCircleAll(swingPoint.position, swingRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out BallController ballController) && !ballController.IsHeldForServe)
            {
                var power = Mathf.Lerp(8.5f, 12.5f, profile.aggression);
                var vertical = Mathf.Lerp(0.55f, 0.95f, profile.hitAccuracy);
                var error = Random.Range(-0.7f, 0.7f) * (1f - profile.hitAccuracy);
                ballController.Hit(new Vector2(-1f, vertical + error).normalized * power);
                nextSwingTime = Time.time + 0.28f;
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (swingPoint == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(swingPoint.position, swingRadius);
    }
}

