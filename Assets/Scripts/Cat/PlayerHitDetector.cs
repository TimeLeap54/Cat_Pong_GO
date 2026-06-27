using System;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    /// <summary>Creates one deterministic request from one active swing and explicit zone test.</summary>
    public sealed class PlayerHitDetector : MonoBehaviour
    {
        [SerializeField] private BallController ballController;
        [SerializeField] private PlayerControlConfig config;
        [SerializeField] private ShotBalanceConfig shotConfig;
        [SerializeField] private PlayerManualHitboxController manualHitboxes;

        private readonly PlayerHitZoneModel zoneModel = new PlayerHitZoneModel();
        private readonly ShotIntentResolver intentResolver = new ShotIntentResolver();
        private long consumedSwingId;
        private SwingIntentSnapshot pendingIntent;
        private long pendingSwingId;
        private Func<long> pointIdProvider;
        private PlayerActionFrame currentAction;
        private Vector2 currentPlayerPosition;
        private int currentFacingDirection = 1;

        public bool RallyHitsEnabled { get; set; } = true;
        public string LastRejectReason { get; private set; } = string.Empty;
        public bool UsesManualHitboxes => manualHitboxes != null && manualHitboxes.HasGameplayHitboxes;
        public bool IsServeToss => ballController != null && ballController.PlayMode == BallPlayMode.ServeToss;

        public event Action<ShotRequest> OnShotRequested;

        public void Initialize(BallController ball, PlayerControlConfig playerConfig,
            ShotBalanceConfig balanceConfig = null)
        {
            ballController = ball;
            config = playerConfig;
            if (balanceConfig != null) shotConfig = balanceConfig;
            config?.ValidateOrThrow();
            consumedSwingId = 0;
            pendingSwingId = 0;
            RallyHitsEnabled = true;
        }

        public void SetPointIdProvider(Func<long> provider) => pointIdProvider = provider;
        public void ConsumeSwing(long swingId) => consumedSwingId = swingId;
        public void SetManualHitboxController(PlayerManualHitboxController controller) => manualHitboxes = controller;

        public bool TryHandleManualHitboxOverlap(ManualHitboxKind kind, Collider2D other)
        {
            if (kind != ManualHitboxKind.Normal && kind != ManualHitboxKind.Smash)
            {
                LastRejectReason = "WrongHitboxKind";
                return false;
            }

            if (!IsBallCollider(other))
            {
                LastRejectReason = "NotBall";
                return false;
            }

            bool isServeToss = IsServeToss;
            if ((!RallyHitsEnabled && !isServeToss) || !currentAction.IsHitActive)
            {
                LastRejectReason = "NotActiveFrame";
                return false;
            }

            if (!isServeToss)
            {
                if ((kind == ManualHitboxKind.Normal && currentAction.SwingKind != SwingKind.Normal) ||
                    (kind == ManualHitboxKind.Smash && currentAction.SwingKind != SwingKind.Smash))
                {
                    LastRejectReason = "WrongHitboxKind";
                    return false;
                }
            }

            if (currentAction.SwingId <= 0 || currentAction.SwingId == consumedSwingId)
            {
                LastRejectReason = "AlreadyConsumedSwing";
                return false;
            }

            if (ballController == null || config == null ||
                (ballController.PlayMode != BallPlayMode.Rally && ballController.PlayMode != BallPlayMode.ServeToss) ||
                !ballController.CurrentSnapshot.IsActive)
            {
                LastRejectReason = "InvalidPointId";
                return false;
            }

            EnsurePendingIntent(currentAction, currentFacingDirection);
            EmitShotRequest(currentAction, ballController.CurrentSnapshot, currentPlayerPosition);
            LastRejectReason = string.Empty;
            return true;
        }

        public bool Evaluate(
            PlayerActionFrame action,
            Vector2 playerPosition,
            int facingDirection)
        {
            currentAction = action;
            currentPlayerPosition = playerPosition;
            currentFacingDirection = facingDirection;
            CaptureIntentOnStartup(action, facingDirection);

            if (UsesManualHitboxes)
            {
                return false;
            }

            bool isServeToss = IsServeToss;
            if ((!RallyHitsEnabled && !isServeToss) || !action.IsHitActive || action.SwingId <= 0 ||
                action.SwingId == consumedSwingId || ballController == null || config == null)
            {
                return false;
            }

            BallSnapshot ball = ballController.CurrentSnapshot;
            if (!ball.IsActive)
            {
                return false;
            }

            HitZoneDefinition zone = (action.SwingKind == SwingKind.Smash || isServeToss)
                ? config.CreateSmashHitZone()
                : config.CreateNormalHitZone();
            if (!zoneModel.Contains(
                    zone,
                    playerPosition.x,
                    playerPosition.y,
                    facingDirection,
                    ball.PositionX,
                    ball.PositionY))
            {
                return false;
            }

            EnsurePendingIntent(action, facingDirection);
            EmitShotRequest(action, ball, playerPosition);
            return true;
        }

        private float CalculateHitHeightRatio(BallSnapshot ball, SwingKind swingKind)
        {
            if (config == null) return 0.5f;
            HitZoneDefinition zone = (swingKind == SwingKind.Smash || IsServeToss)
                ? config.CreateSmashHitZone()
                : config.CreateNormalHitZone();
            float relativeY = ball.PositionY - currentPlayerPosition.y;
            float yMin = zone.CenterY - zone.HalfHeight;
            float yMax = zone.CenterY + zone.HalfHeight;
            if (yMax <= yMin) return 0.5f;
            return Mathf.Clamp01((relativeY - yMin) / (yMax - yMin));
        }

        private void EmitShotRequest(PlayerActionFrame action, BallSnapshot ball, Vector2 playerPosition)
        {
            bool isServeToss = IsServeToss;
            bool isCounteringSmash = (ballController != null &&
                                      ballController.PlayMode == BallPlayMode.Rally &&
                                      ballController.LastShotIntent == ShotIntent.Smash);
            float ratio = CalculateHitHeightRatio(ball, action.SwingKind);
            ShotRequest request = new ShotRequest(pendingIntent, ball.StepIndex,
                HitterType.Player, ball, playerPosition.x, playerPosition.y,
                isServeToss, isCounteringSmash, ratio);
            consumedSwingId = action.SwingId;
            OnShotRequested?.Invoke(request);
        }

        private void CaptureIntentOnStartup(PlayerActionFrame action, int facingDirection)
        {
            if (action.SwingId <= 0 || action.SwingId == pendingSwingId ||
                (action.SwingState != SwingState.NormalStartup &&
                 action.SwingState != SwingState.SmashStartup))
            {
                return;
            }

            CaptureIntent(action, facingDirection);
        }

        private void EnsurePendingIntent(PlayerActionFrame action, int facingDirection)
        {
            if (pendingSwingId != action.SwingId)
            {
                CaptureIntent(action, facingDirection);
            }
        }

        private void CaptureIntent(PlayerActionFrame action, int facingDirection)
        {
            ShotIntent intent = intentResolver.Resolve(action.AimDirection, facingDirection,
                action.SwingKind == SwingKind.Smash,
                shotConfig == null ? 0.25f : shotConfig.AimDeadZone,
                shotConfig == null ? 0.4f : shotConfig.VerticalPriorityThreshold);

            if (ballController != null && ballController.PlayMode == BallPlayMode.ServeToss)
            {
                intent = action.SwingKind == SwingKind.Smash ? ShotIntent.Smash : ShotIntent.Serve;
            }

            pendingIntent = new SwingIntentSnapshot(
                pointIdProvider == null ? 1 : pointIdProvider(), action.SwingId,
                action.InputTick, action.AimDirection, facingDirection, intent);
            pendingSwingId = action.SwingId;
        }

        private bool IsBallCollider(Collider2D other)
        {
            if (other == null || ballController == null)
            {
                return false;
            }

            if (other.gameObject == ballController.gameObject)
            {
                return true;
            }

            BallController candidate = other.GetComponentInParent<BallController>();
            return candidate == ballController;
        }
    }
}
