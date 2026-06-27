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
                    ratio
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

            if (UsesManualHitboxes)
            {
                return false;
            }

            if(plan==null||plan.Consumed||plan.PointId!=pointId||
               System.Math.Abs(ball.CurrentSnapshot.StepIndex-plan.ExpectedBallStepIndex)>8) return false;
            if(action.SwingId<=0||action.SwingId==consumedSwingId) return false;

            // AI 타격 존을 수평/수직 1.25배 확장하여 마그네틱 흡입 보정 적용
            HitZoneDefinition normalZone = hitConfig.CreateNormalHitZone();
            HitZoneDefinition smashZone = hitConfig.CreateSmashHitZone();
            HitZoneDefinition expandedNormal = new HitZoneDefinition(
                normalZone.CenterX,
                normalZone.CenterY,
                normalZone.HalfWidth * 1.25f,
                normalZone.HalfHeight * 1.25f,
                normalZone.RequireForward
            );
            HitZoneDefinition expandedSmash = new HitZoneDefinition(
                smashZone.CenterX,
                smashZone.CenterY,
                smashZone.HalfWidth * 1.25f,
                smashZone.HalfHeight * 1.25f,
                smashZone.RequireForward
            );

            float relativeY = ball.CurrentSnapshot.PositionY - position.y;
            HitZoneDefinition activeZone = action.SwingKind == SwingKind.Smash ? expandedSmash : expandedNormal;
            float yMin = activeZone.CenterY - activeZone.HalfHeight;
            float yMax = activeZone.CenterY + activeZone.HalfHeight;
            float ratio = yMax > yMin ? Mathf.Clamp01((relativeY - yMin) / (yMax - yMin)) : 0.5f;
            bool isCounteringSmash = (ball != null && 
                                      ball.PlayMode == BallPlayMode.Rally && 
                                      ball.LastShotIntent == ShotIntent.Smash);

            if(!validator.TryCreate(pointId,action,HitterType.Opponent,plan.Intent,position,facing,
                ball.CurrentSnapshot,ball.PlayMode,expandedNormal,
                expandedSmash,out HitContact contact, ratio, isCounteringSmash)) return false;
            if(!executor.TryExecute(contact)) return false;
            consumedSwingId=action.SwingId; return true;
        }

        private float CalculateHitHeightRatio(BallSnapshot ballSnapshot, SwingKind swingKind)
        {
            if (hitConfig == null) return 0.5f;
            HitZoneDefinition zone = (swingKind == SwingKind.Smash || IsServeToss)
                ? hitConfig.CreateSmashHitZone()
                : hitConfig.CreateNormalHitZone();
            float relativeY = ballSnapshot.PositionY - currentOpponentPosition.y;
            float yMin = zone.CenterY - zone.HalfHeight;
            float yMax = zone.CenterY + zone.HalfHeight;
            if (yMax <= yMin) return 0.5f;
            return Mathf.Clamp01((relativeY - yMin) / (yMax - yMin));
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
