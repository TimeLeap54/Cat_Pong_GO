using System;
using CatTennis.Rebuild.Config;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    /// <summary>Applies pure action-state output to player Rigidbody2D movement.</summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(CatMotor))]
    public sealed class PlayerCatController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerHitDetector hitDetector;
        [SerializeField] private PlayerManualHitboxController manualHitboxes;
        [SerializeField] private PlayerControlConfig config;
        [SerializeField] private MovementBalanceConfig movementBalanceConfig;

        private Rigidbody2D body;
        private CapsuleCollider2D bodyCollider;
        private PlayerActionStateMachine stateMachine;
        private CatMotor motor;
        private bool initialized;

        public PlayerActionFrame CurrentAction { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public event Action<PlayerActionFrame, Vector2> OnActionApplied;
        public bool InputLocked { get; set; }

        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public Vector2 Velocity => body != null ? body.velocity : Vector2.zero;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            motor = GetComponent<CatMotor>();
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
            if (InputLocked)
            {
                input = new PlayerInputFrame(0f, false, input.SwingPressed, input.SmashPressed, input.AimDirection.y, input.InputTick);
            }
            CurrentAction = stateMachine.Step(input, groundDetected);

            motor.Apply(CurrentAction.MoveX, CurrentAction.JumpRequested,
                config.JumpSpeed * MovementBalanceConfig.JumpVelocityMultiplierOrDefault(movementBalanceConfig),
                Time.fixedDeltaTime);
            OnActionApplied?.Invoke(CurrentAction, body.position);
            hitDetector.Evaluate(CurrentAction, body.position, FacingDirection);
            if (manualHitboxes != null)
            {
                manualHitboxes.ApplyAction(CurrentAction, FacingDirection);
            }
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

        public void SetMovementBalance(MovementBalanceConfig balanceConfig)
        {
            movementBalanceConfig = balanceConfig;
            if (initialized)
            {
                ConfigureMotor();
            }
        }

        public void ResetPlayer(Vector2 position)
        {
            EnsureInitialized();
            motor.ResetMotor(position);
            FacingDirection = 1;
            stateMachine.Reset();
            CurrentAction = new PlayerActionFrame(
                LocomotionState.Grounded,
                SwingState.Ready,
                SwingKind.None,
                stateMachine.SwingId,
                false,
                0f);
        }

        private float minX = float.NegativeInfinity;
        private float maxX = float.PositiveInfinity;

        public void Initialize(
            PlayerInputReader reader,
            PlayerHitDetector detector,
            PlayerControlConfig playerConfig,
            float courtMinX = float.NegativeInfinity,
            float courtMaxX = float.PositiveInfinity)
        {
            inputReader = reader;
            hitDetector = detector;
            config = playerConfig;
            minX = courtMinX;
            maxX = courtMaxX;
            EnsureReferences();
            config.ValidateOrThrow();

            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            motor = GetComponent<CatMotor>();
            if (manualHitboxes == null)
            {
                manualHitboxes = GetComponent<PlayerManualHitboxController>();
            }
            ConfigureMotor();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = config.GravityScale;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            gameObject.layer = config.PlayerBodyLayer;
            bodyCollider.excludeLayers |= 1 << config.BallLayer;
            if (manualHitboxes != null)
            {
                manualHitboxes.Bind(hitDetector);
                hitDetector.SetManualHitboxController(manualHitboxes);
            }
            stateMachine = new PlayerActionStateMachine(config.CreateActionSettings());
            initialized = true;
        }

        private void ConfigureMotor()
        {
            motor.Configure(
                config.MoveSpeed * MovementBalanceConfig.PlayerMoveSpeedMultiplierOrDefault(movementBalanceConfig),
                1000f,
                1000f,
                minX,
                maxX);
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
            if (inputReader == null)
            {
                throw new InvalidOperationException("PlayerInputReader is required.");
            }

            if (hitDetector == null)
            {
                throw new InvalidOperationException("PlayerHitDetector is required.");
            }

            if (config == null)
            {
                throw new InvalidOperationException("PlayerControlConfig is required.");
            }
        }
    }
}
