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
                // 랠리 모드에서는 조작 불쾌감을 방지하기 위해 매뉴얼 히트박스(물리 콜라이더) 외에
                // 수학적 판정(Contains)도 하이브리드 백업 구제 수단으로 상시 가동합니다.
                if (ballController == null || ballController.PlayMode != BallPlayMode.Rally)
                {
                    return false;
                }
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

            if (UsesManualHitboxes)
            {
                // 랠리 모드 시 수동 지정한 BoxCollider2D의 실제 월드 영역을 기반으로 수학적 Contains 백업을 구동합니다.
                // 이로써 수동 콜라이더 영역과 수학적 백업 영역의 크기가 100% 완벽하게 일치하여 허공 타격이 전혀 발생하지 않습니다.
                if (ballController == null || ballController.PlayMode != BallPlayMode.Rally)
                {
                    return false;
                }

                ManualHitboxTrigger activeTrigger = (action.SwingKind == SwingKind.Smash || isServeToss)
                    ? manualHitboxes.SmashHitbox
                    : manualHitboxes.NormalHitbox;

                if (activeTrigger == null || activeTrigger.Box == null)
                {
                    return false;
                }

                Bounds bounds = activeTrigger.Box.bounds;
                // 헛방 방지 보정으로 수동 지정 콜라이더의 월드 바운즈에 20cm(0.2f) 버퍼만 더하여 프레임 관통 방지
                bounds.Expand(0.2f);

                if (!bounds.Contains(new Vector3(ball.PositionX, ball.PositionY, bounds.center.z)))
                {
                    return false;
                }
            }
            else
            {
                HitZoneDefinition zone = (action.SwingKind == SwingKind.Smash || isServeToss)
                    ? config.CreateSmashHitZone()
                    : config.CreateNormalHitZone();

                if (ballController.PlayMode == BallPlayMode.Rally)
                {
                    // 랠리 모드 시 타격 성공 체감과 수동 히트박스 일치를 위해 판정 범위를 12% 넓혀 후하게 판정합니다.
                    zone = new HitZoneDefinition(
                        zone.CenterX,
                        zone.CenterY,
                        zone.HalfWidth * 1.12f,
                        zone.HalfHeight * 1.12f,
                        zone.RequireForward
                    );
                }

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
            }

            EnsurePendingIntent(action, facingDirection);
            EmitShotRequest(action, ball, playerPosition);
            return true;
        }

        private float CalculateHitHeightRatio(BallSnapshot ball, SwingKind swingKind)
        {
            if (config == null) return 0.5f;

            if (UsesManualHitboxes)
            {
                ManualHitboxTrigger activeTrigger = (swingKind == SwingKind.Smash || IsServeToss)
                    ? manualHitboxes.SmashHitbox
                    : manualHitboxes.NormalHitbox;

                if (activeTrigger != null && activeTrigger.Box != null)
                {
                    Bounds bounds = activeTrigger.Box.bounds;
                    float relativeY = ball.PositionY - bounds.min.y;
                    float height = bounds.size.y;
                    if (height <= 0.0001f) return 0.5f;
                    return Mathf.Clamp01(relativeY / height);
                }
            }

            HitZoneDefinition zone = (swingKind == SwingKind.Smash || IsServeToss)
                ? config.CreateSmashHitZone()
                : config.CreateNormalHitZone();
            float relativeYVal = ball.PositionY - currentPlayerPosition.y;
            float yMin = zone.CenterY - zone.HalfHeight;
            float yMax = zone.CenterY + zone.HalfHeight;
            if (yMax <= yMin) return 0.5f;
            return Mathf.Clamp01((relativeYVal - yMin) / (yMax - yMin));
        }

        private void EmitShotRequest(PlayerActionFrame action, BallSnapshot ball, Vector2 playerPosition)
        {
            bool isServeToss = IsServeToss;
            bool isCounteringSmash = (ballController != null &&
                                      ballController.PlayMode == BallPlayMode.Rally &&
                                      ballController.LastShotIntent == ShotIntent.Smash);
            bool isCounteringKillSmash = isCounteringSmash && ballController.LastShotWasKillSmash;
            float ratio = CalculateHitHeightRatio(ball, action.SwingKind);
            ShotRequest request = new ShotRequest(pendingIntent, ball.StepIndex,
                HitterType.Player, ball, playerPosition.x, playerPosition.y,
                isServeToss, isCounteringSmash, ratio, false, isCounteringKillSmash);
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
