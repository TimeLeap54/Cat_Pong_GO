using System;
using CatTennis.Rebuild.Config;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    /// <summary>Applies pure action-state output to player Rigidbody2D movement.</summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerCatController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerHitDetector hitDetector;
        [SerializeField] private PlayerControlConfig config;

        private Rigidbody2D body;
        private CapsuleCollider2D bodyCollider;
        private PlayerActionStateMachine stateMachine;
        private bool initialized;

        public PlayerActionFrame CurrentAction { get; private set; }
        public int FacingDirection { get; private set; } = 1;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            if (config != null)
            {
                Initialize(inputReader, hitDetector, config);
            }
        }

        private void FixedUpdate()
        {
            EnsureInitialized();
            PlayerInputFrame input = inputReader.ConsumeFrame();
            bool grounded = Physics2D.OverlapCircle(
                body.position + config.GroundCheckOffset,
                config.GroundCheckRadius,
                config.GroundMask) != null;
            ApplyFixedTick(input, grounded);
        }

        public PlayerActionFrame ApplyFixedTick(PlayerInputFrame input, bool groundDetected)
        {
            EnsureInitialized();
            CurrentAction = stateMachine.Step(input, groundDetected);

            Vector2 velocity = body.velocity;
            velocity.x = CurrentAction.MoveX * config.MoveSpeed;
            if (CurrentAction.JumpRequested)
            {
                velocity.y = config.JumpSpeed;
            }

            body.velocity = velocity;
            hitDetector.Evaluate(CurrentAction, body.position, FacingDirection);
            return CurrentAction;
        }

        public void SetCourtFacingDirection(int direction)
        {
            if (direction != -1 && direction != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }

            FacingDirection = direction;
        }

        public void Initialize(
            PlayerInputReader reader,
            PlayerHitDetector detector,
            PlayerControlConfig playerConfig)
        {
            inputReader = reader;
            hitDetector = detector;
            config = playerConfig;
            EnsureReferences();
            config.ValidateOrThrow();

            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = config.GravityScale;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            gameObject.layer = config.PlayerBodyLayer;
            bodyCollider.excludeLayers |= 1 << config.BallLayer;
            stateMachine = new PlayerActionStateMachine(config.CreateActionSettings());
            initialized = true;
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                Initialize(inputReader, hitDetector, config);
            }
        }

        private void EnsureReferences()
        {
            if (inputReader == null || hitDetector == null || config == null)
            {
                throw new InvalidOperationException("Player controller references are incomplete.");
            }
        }
    }
}
