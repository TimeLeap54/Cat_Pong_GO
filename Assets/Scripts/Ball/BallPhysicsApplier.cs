using System;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Config;
using UnityEngine;

namespace CatTennis.Rebuild.Ball
{
    /// <summary>Runs the pure model at fixed time and mirrors snapshots to Rigidbody2D.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BallPhysicsApplier : MonoBehaviour
    {
        [SerializeField] private BallPhysicsConfig config;
        [SerializeField] private bool hasGroundPlane;
        [SerializeField] private float groundY;
        [SerializeField] private bool simulateOnFixedUpdate = true;

        private readonly BallPhysicsModel model = new BallPhysicsModel();
        private readonly BallStateTracker tracker = new BallStateTracker();
        private Rigidbody2D body;

        public BallSnapshot CurrentSnapshot => tracker.Current;
        public BallStepResult LastStepResult { get; private set; }
        public BallPhysicsConfig Config => config;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ConfigureBody();
            tracker.Reset(body.position.x, body.position.y);
        }

        private void FixedUpdate()
        {
            if (simulateOnFixedUpdate && config != null)
            {
                StepOnce(Time.fixedDeltaTime);
            }
        }

        public void Configure(
            BallPhysicsConfig physicsConfig,
            bool groundPlaneEnabled,
            float physicalGroundY)
        {
            EnsureInitialized();
            config = physicsConfig;
            hasGroundPlane = groundPlaneEnabled;
            groundY = physicalGroundY;
        }

        public BallStepResult StepOnce(float fixedDeltaTime)
        {
            EnsureInitialized();
            if (config == null)
            {
                throw new InvalidOperationException("BallPhysicsConfig is required.");
            }

            BallStepInput input = new BallStepInput(fixedDeltaTime, hasGroundPlane, groundY);
            BallStepResult result = model.Step(tracker.Current, config.CreateSettings(), input);
            tracker.Apply(result);
            LastStepResult = result;
            body.position = new Vector2(
                result.NextSnapshot.PositionX,
                result.NextSnapshot.PositionY);
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            return result;
        }

        public void ResetBall(Vector2 position)
        {
            EnsureInitialized();
            tracker.Reset(position.x, position.y);
            LastStepResult = default;
            body.position = position;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        public void Launch(Vector2 velocity)
        {
            EnsureInitialized();
            tracker.Launch(velocity.x, velocity.y);
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        public void StopBall()
        {
            EnsureInitialized();
            tracker.Stop();
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        public void SetGroundPlane(bool enabled, float physicalGroundY)
        {
            hasGroundPlane = enabled;
            groundY = physicalGroundY;
        }

        private void EnsureInitialized()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
                ConfigureBody();
                tracker.Reset(body.position.x, body.position.y);
            }
        }

        private void ConfigureBody()
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }
}
