using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CatMotor : MonoBehaviour
    {
        private Rigidbody2D body;
        private float speed;
        private float acceleration;
        private float deceleration;
        private float minX = float.NegativeInfinity;
        private float maxX = float.PositiveInfinity;
        public Vector2 Position => Body.position;
        public Vector2 Velocity => Body.velocity;
        private Rigidbody2D Body => body == null ? body = GetComponent<Rigidbody2D>() : body;

        public void Configure(float moveSpeed, float moveAcceleration, float moveDeceleration,
            float courtMinX = float.NegativeInfinity, float courtMaxX = float.PositiveInfinity)
        {
            speed = moveSpeed; acceleration = moveAcceleration; deceleration = moveDeceleration;
            minX = courtMinX; maxX = courtMaxX;
        }

        public void Apply(float moveX, bool jump, float jumpSpeed, float dt, bool immediateHorizontal = false)
        {
            Vector2 velocity = Body.velocity;
            float target = Mathf.Clamp(moveX, -1f, 1f) * speed;
            if (immediateHorizontal)
            {
                velocity.x = target;
            }
            else
            {
                float rate = Mathf.Abs(target) > 0.001f ? acceleration : deceleration;
                velocity.x = Mathf.MoveTowards(velocity.x, target, rate * dt);
            }
            if (jump) velocity.y = jumpSpeed;
            Body.velocity = velocity;
            Vector2 position = Body.position;
            position.x = Mathf.Clamp(position.x, minX, maxX);
            Body.position = position;
        }

        public void ResetMotor(Vector2 position)
        {
            Body.position = position; Body.velocity = Vector2.zero; Body.angularVelocity = 0f;
        }
    }
}
