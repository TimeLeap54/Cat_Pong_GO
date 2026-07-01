using System;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    public sealed class OpponentHitDetector : MonoBehaviour
    {
        private readonly CatHitContactValidator validator = new CatHitContactValidator();
        private BallController ball; private PlayerControlConfig hitConfig;
        private ShotExecutionController executor; private long consumedSwingId;

        private OpponentManualHitboxController manualHitboxes;
        private long currentPointId;
        private PlayerActionFrame currentAction;
        private Vector2 currentOpponentPosition;
        private int currentFacingDirection = -1;
        private AISwingPlan currentPlan;

        public bool UsesManualHitboxes => manualHitboxes != null && manualHitboxes.HasGameplayHitboxes;
        public bool IsServeToss => ball != null && ball.PlayMode == BallPlayMode.ServeToss;
        public string LastRejectReason { get; private set; } = string.Empty;

        public void Configure(BallController ballController, PlayerControlConfig config,
            ShotExecutionController shotExecutor)
        { ball=ballController; hitConfig=config; executor=shotExecutor; consumedSwingId=0; }

        public void SetManualHitboxController(OpponentManualHitboxController controller) => manualHitboxes = controller;

        public bool TryHandleManualHitboxOverlap(ManualHitboxKind kind, Collider2D other)
        {
            if (ball == null || !ball.CurrentSnapshot.IsActive)
            {
                LastRejectReason = "BallNotActive";
                return false;
            }

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
            if (!currentAction.IsHitActive)
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

            if (isServeToss)
            {
                ShotIntent intent = ShotIntent.Serve;
                float ratio = CalculateHitHeightRatio(ball.CurrentSnapshot, currentAction.SwingKind);
                HitContact contact = new HitContact(
                    currentPointId,
                    currentAction.SwingId,
                    ball.CurrentSnapshot.StepIndex,
                    HitterType.Opponent,
                    intent,
                    currentOpponentPosition,
                    ball.CurrentSnapshot,
                    currentFacingDirection,
                    currentAction.InputTick,
                    isServeToss,
                    false,
                    ratio
                );

                if (!executor.TryExecute(contact))
                {
                    LastRejectReason = "ExecutionFailed";
                    return false;
                }

                consumedSwingId = currentAction.SwingId;
                LastRejectReason = string.Empty;
                return true;
            }
            else
            {
                if (currentPlan == null || currentPlan.Consumed)
                {
                    LastRejectReason = "NoPlan";
                    return false;
                }

                bool isCounteringSmash = (ball != null && 
                                          ball.PlayMode == BallPlayMode.Rally && 
                                          ball.LastShotIntent == ShotIntent.Smash);
                bool isCounteringKillSmash = isCounteringSmash && ball.LastShotWasKillSmash;
                float ratio = CalculateHitHeightRatio(ball.CurrentSnapshot, currentAction.SwingKind);
                HitContact contact = new HitContact(
                    currentPointId,
                    currentAction.SwingId,
                    ball.CurrentSnapshot.StepIndex,
                    HitterType.Opponent,
                    currentPlan.Intent,
                    currentOpponentPosition,
                    ball.CurrentSnapshot,
                    currentFacingDirection,
                    currentAction.InputTick,
                    false,
                    isCounteringSmash,
                    ratio,
                    isCounteringKillSmash
                );

                if (!executor.TryExecute(contact))
                {
                    LastRejectReason = "ExecutionFailed";
                    return false;
                }

                consumedSwingId = currentAction.SwingId;
                currentPlan.Consumed = true;
                LastRejectReason = string.Empty;
                return true;
            }
        }

        public bool Evaluate(long pointId, PlayerActionFrame action, Vector2 position,
            int facing, AISwingPlan plan)
        {
            currentPointId = pointId;
            currentAction = action;
            currentOpponentPosition = position;
            currentFacingDirection = facing;
            currentPlan = plan;
            bool isCounteringSmash = (ball != null &&
                                      ball.PlayMode == BallPlayMode.Rally &&
                                      ball.LastShotIntent == ShotIntent.Smash);

            if (UsesManualHitboxes)
            {
                // 랠리 모드 시 수동 지정한 BoxCollider2D의 실제 월드 영역을 기반으로 수학적 Contains 백업을 구동합니다.
                // 이로써 수동 콜라이더 영역과 수학적 백업 영역의 크기가 100% 완벽하게 일치하여 허공 타격이 전혀 발생하지 않습니다.
                if (ball == null || ball.PlayMode != BallPlayMode.Rally)
                {
                    return false;
                }

                OpponentManualHitboxTrigger activeTrigger = (action.SwingKind == SwingKind.Smash)
                    ? manualHitboxes.SmashHitbox
                    : manualHitboxes.NormalHitbox;

                if (activeTrigger == null || activeTrigger.Box == null)
                {
                    return false;
                }

                Bounds bounds = activeTrigger.Box.bounds;
                // 헛방 방지 보정으로 수동 지정 콜라이더의 월드 바운즈에 20cm(0.2f) 버퍼만 더하여 프레임 관통 방지
                bounds.Expand(0.2f);

                if (!bounds.Contains(new Vector3(ball.CurrentSnapshot.PositionX, ball.CurrentSnapshot.PositionY, bounds.center.z)))
                {
                    float assistRatio = CalculateHitHeightRatio(ball.CurrentSnapshot, action.SwingKind);
                    if (!TryCreateRallyServeReceiveAssistContact(
                            pointId,
                            action,
                            plan,
                            position,
                            facing,
                            assistRatio,
                            isCounteringSmash,
                            out HitContact assistContact) ||
                        !executor.TryExecute(assistContact))
                    {
                        return false;
                    }

                    consumedSwingId = action.SwingId;
                    plan.Consumed = true;
                    return true;
                }
            }

            if(plan==null||plan.Consumed||plan.PointId!=pointId||
               System.Math.Abs(ball.CurrentSnapshot.StepIndex-plan.ExpectedBallStepIndex)>30) return false;
            if(action.SwingId<=0||action.SwingId==consumedSwingId) return false;

            // AI 타격 존을 랠리 모드에서는 1.18배 확장하여 수동 히트박스와 조화롭게 맞춤
            float scale = (ball != null && ball.PlayMode == BallPlayMode.Rally) ? 1.18f : 1.25f;
            HitZoneDefinition normalZone = hitConfig.CreateNormalHitZone();
            HitZoneDefinition smashZone = hitConfig.CreateSmashHitZone();
            HitZoneDefinition expandedNormal = new HitZoneDefinition(
                normalZone.CenterX,
                normalZone.CenterY,
                normalZone.HalfWidth * scale,
                normalZone.HalfHeight * scale,
                normalZone.RequireForward
            );
            HitZoneDefinition expandedSmash = new HitZoneDefinition(
                smashZone.CenterX,
                smashZone.CenterY,
                smashZone.HalfWidth * scale,
                smashZone.HalfHeight * scale,
                smashZone.RequireForward
            );

            float ratio = CalculateHitHeightRatio(ball.CurrentSnapshot, action.SwingKind);
            HitContact contact;
            if (UsesManualHitboxes)
            {
                contact = new HitContact(
                    pointId,
                    action.SwingId,
                    ball.CurrentSnapshot.StepIndex,
                    HitterType.Opponent,
                    plan.Intent,
                    position,
                    ball.CurrentSnapshot,
                    facing,
                    action.InputTick,
                    ball.PlayMode == BallPlayMode.ServeToss,
                    isCounteringSmash,
                    ratio
                );
            }
            else
            {
                if (!validator.TryCreate(pointId, action, HitterType.Opponent, plan.Intent, position, facing,
                    ball.CurrentSnapshot, ball.PlayMode, expandedNormal,
                    expandedSmash, out contact, ratio, isCounteringSmash))
                {
                    if (!TryCreateRallyServeReceiveAssistContact(
                            pointId,
                            action,
                            plan,
                            position,
                            facing,
                            ratio,
                            isCounteringSmash,
                            out contact))
                    {
                        return false;
                    }
                }
            }

            if(!executor.TryExecute(contact)) return false;
            consumedSwingId=action.SwingId;
            plan.Consumed = true; // 플랜 완료 처리 (스킬 연쇄 및 다음 상태 갱신 복구)
            return true;
        }

        private bool TryCreateRallyServeReceiveAssistContact(
            long pointId,
            PlayerActionFrame action,
            AISwingPlan plan,
            Vector2 position,
            int facing,
            float ratio,
            bool isCounteringSmash,
            out HitContact contact)
        {
            contact = default;
            if (ball == null ||
                ball.PlayMode != BallPlayMode.Rally ||
                plan == null ||
                plan.Consumed ||
                !action.IsHitActive ||
                ball.CurrentSnapshot.PositionX < 0f)
            {
                return false;
            }

            float horizontalGap = Mathf.Abs(ball.CurrentSnapshot.PositionX - position.x);
            float verticalGap = Mathf.Abs(ball.CurrentSnapshot.PositionY - position.y);
            float horizontalLimit = ball.LastShotWasKillSmash ? 4.2f : 3.2f;
            float verticalLimit = ball.LastShotWasKillSmash ? 3.6f : 3.0f;
            bool closeToActor = horizontalGap <= horizontalLimit && verticalGap <= verticalLimit;
            bool closeToPlan =
                Mathf.Abs(ball.CurrentSnapshot.PositionX - plan.InterceptPosition.x) <= 1.45f &&
                Mathf.Abs(ball.CurrentSnapshot.PositionY - plan.InterceptPosition.y) <= 1.25f;
            if (!closeToActor && !closeToPlan)
            {
                return false;
            }

            contact = new HitContact(
                pointId,
                action.SwingId,
                ball.CurrentSnapshot.StepIndex,
                HitterType.Opponent,
                plan.Intent,
                position,
                ball.CurrentSnapshot,
                facing,
                action.InputTick,
                false,
                isCounteringSmash,
                ratio,
                ball.LastShotWasKillSmash);
            return true;
        }

        private float CalculateHitHeightRatio(BallSnapshot ballSnapshot, SwingKind swingKind)
        {
            if (hitConfig == null) return 0.5f;

            if (UsesManualHitboxes)
            {
                OpponentManualHitboxTrigger activeTrigger = (swingKind == SwingKind.Smash || IsServeToss)
                    ? manualHitboxes.SmashHitbox
                    : manualHitboxes.NormalHitbox;

                if (activeTrigger != null && activeTrigger.Box != null)
                {
                    Bounds bounds = activeTrigger.Box.bounds;
                    float relativeY = ballSnapshot.PositionY - bounds.min.y;
                    float height = bounds.size.y;
                    if (height <= 0.0001f) return 0.5f;
                    return Mathf.Clamp01(relativeY / height);
                }
            }

            HitZoneDefinition zone = (swingKind == SwingKind.Smash || IsServeToss)
                ? hitConfig.CreateSmashHitZone()
                : hitConfig.CreateNormalHitZone();
            float relativeYVal = ballSnapshot.PositionY - currentOpponentPosition.y;
            float yMin = zone.CenterY - zone.HalfHeight;
            float yMax = zone.CenterY + zone.HalfHeight;
            if (yMax <= yMin) return 0.5f;
            return Mathf.Clamp01((relativeYVal - yMin) / (yMax - yMin));
        }
        public void ResetDetector() => consumedSwingId=0;

        private bool IsBallCollider(Collider2D other)
        {
            if (other == null || ball == null) return false;
            if (other.gameObject == ball.gameObject) return true;
            BallController candidate = other.GetComponentInParent<BallController>();
            return candidate == ball;
        }
    }
}
