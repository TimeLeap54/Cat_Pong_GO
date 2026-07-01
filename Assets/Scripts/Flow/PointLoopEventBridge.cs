using System;
using System.Collections.Generic;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
using CatTennis.Rebuild.UI;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    /// <summary>Scene wiring only: forwards events and executes fixed command order.</summary>
    public sealed class PointLoopEventBridge : MonoBehaviour
    {
        [SerializeField] private BallPhysicsApplier physicsApplier;
        [SerializeField] private BallController ballController;
        [SerializeField] private CourtZoneDetector courtZoneDetector;
        [SerializeField] private RallyFlowManager rallyFlowManager;
        [SerializeField] private MatchFlowManager matchFlowManager;
        [SerializeField] private ResetFlowController resetFlowController;
        [SerializeField] private bool startOnEnable;
        [SerializeField] private PointLifecycleController lifecycle;
        [SerializeField] private PlayerCatController playerController;
        [SerializeField] private Vector2 playerResetPosition;
        [SerializeField] private ServeFlowController serveFlowController;
        [SerializeField] private OpponentAIController opponentController;
        [SerializeField] private Vector2 opponentResetPosition;
        [SerializeField] private ShotExecutionController shotExecutionController;
        [SerializeField] private MovementBalanceConfig movementBalanceConfig;
        [SerializeField] private OpponentServeFlowController opponentServeFlowController;

        private Phase3PointLoopConfig pointLoopConfig;
        private HitterType activeServer = HitterType.Player;

        private bool subscribed;
        public long CurrentPointId => rallyFlowManager.GlobalPointId;
        public int RallyHitCount => rallyFlowManager != null ? rallyFlowManager.RallyHitCount : 0;
        public bool IsRallyMode => rallyFlowManager != null && rallyFlowManager.IsRallyMode;
        public bool IsVolley => rallyFlowManager != null && rallyFlowManager.CurrentContext.State == RallyState.InFlight;
        public bool IsServeWaitingForToss =>
            (serveFlowController != null && serveFlowController.enabled && serveFlowController.State == ServeFlowState.WaitingForToss) ||
            (opponentServeFlowController != null && opponentServeFlowController.enabled && opponentServeFlowController.IsWaitingForToss);

        private void FixedUpdate()
        {
            // 서버(플레이어)는 서브 토스를 띄우기 전(WaitingForToss)인 동안에만 락
            bool playerLock = (serveFlowController != null && serveFlowController.enabled && 
                               serveFlowController.State == ServeFlowState.WaitingForToss);
            
            // 리시버(AI)는 상대 서버가 공을 띄워서(ServeToss) 실제로 때려 런칭하기 전까지 계속 락
            // 즉, Inactive(비활성) 상태이거나 ServeToss(토스 공중 비행) 상태인 동안 락을 유지하고,
            // 플레이 모드가 Rally로 활성화되어 발사되는 순간에 락을 완전히 해제
            bool aiLock = (ballController.PlayMode == BallPlayMode.Inactive || 
                           ballController.PlayMode == BallPlayMode.ServeToss);

            if (playerController != null) playerController.InputLocked = playerLock;
            if (opponentController != null) opponentController.InputLocked = aiLock;
        }

        private void OnEnable()
        {
            RequireReferences();
            Subscribe();
            if (startOnEnable)
            {
                if (pointLoopConfig != null && serveFlowController != null)
                {
                    activeServer = pointLoopConfig.IsRallyMode ? HitterType.Player : pointLoopConfig.Server;
                    resetFlowController.SetNextServer(activeServer);
                    resetFlowController.RequestInitialPoint();
                }
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            BallPhysicsApplier applier,
            BallController ball,
            CourtZoneDetector detector,
            RallyFlowManager rally,
            MatchFlowManager match,
            ResetFlowController reset)
        {
            physicsApplier = applier;
            ballController = ball;
            courtZoneDetector = detector;
            rallyFlowManager = rally;
            matchFlowManager = match;
            resetFlowController = reset;
            
            Subscribe();
        }

        public void SetConfig(Phase3PointLoopConfig config)
        {
            pointLoopConfig = config;
            if (rallyFlowManager != null && config != null)
            {
                rallyFlowManager.IsRallyMode = config.IsRallyMode;
            }
            activeServer = pointLoopConfig != null
                ? (pointLoopConfig.IsRallyMode ? HitterType.Player : pointLoopConfig.Server)
                : HitterType.Player;
            if (resetFlowController != null)
            {
                resetFlowController.SetNextServer(activeServer);
            }
        }

        public void StartInitialPoint()
        {
            if (lifecycle != null && lifecycle.State != PointLoopState.StartingPoint)
            {
                return;
            }

            activeServer = pointLoopConfig != null
                ? (pointLoopConfig.IsRallyMode ? HitterType.Player : pointLoopConfig.Server)
                : HitterType.Player;
            resetFlowController.SetNextServer(activeServer);
            resetFlowController.RequestInitialPoint();
        }

        public void RetryMatch()
        {
            matchFlowManager.ResetMatch();
            lifecycle?.BeginRetry();

            activeServer = pointLoopConfig != null
                ? (pointLoopConfig.IsRallyMode ? HitterType.Player : pointLoopConfig.Server)
                : HitterType.Player;
            resetFlowController.SetNextServer(activeServer);
            resetFlowController.RequestRetry();
        }

        public void SetLifecycle(PointLifecycleController pointLifecycle)
        {
            lifecycle = pointLifecycle;
        }

        public void SetPlayerReset(PlayerCatController player, Vector2 resetPosition)
        {
            playerController = player;
            playerResetPosition = resetPosition;
        }

        public void SetServeFlow(ServeFlowController serveFlow) => serveFlowController = serveFlow;
        public void SetOpponentServeFlow(OpponentServeFlowController serveFlow) => opponentServeFlowController = serveFlow;
        public void SetOpponentReset(OpponentAIController opponent, Vector2 position)
        { opponentController=opponent; opponentResetPosition=position; }
        public void SetShotExecutor(ShotExecutionController executor) => shotExecutionController=executor;
        public void SetMovementBalance(MovementBalanceConfig balanceConfig) =>
            movementBalanceConfig = balanceConfig;

        public BallController Ball => ballController;

        public bool TrySubmitHit(
            HitterType hitter,
            long expectedBallStepIndex,
            Vector2 launchVelocity)
        {
            if (ballController.PlayMode == BallPlayMode.ServeToss)
            {
                return TrySubmitApprovedHit(hitter, expectedBallStepIndex, launchVelocity, true);
            }
            if (ballController.PlayMode == BallPlayMode.Rally)
            {
                return TrySubmitApprovedHit(hitter, expectedBallStepIndex, launchVelocity, false);
            }
            return false;
        }

        public bool TrySubmitServeHit(HitterType hitter, long expectedBallStepIndex,
            Vector2 launchVelocity)
        {
            return TrySubmitHit(hitter, expectedBallStepIndex, launchVelocity);
        }

        private bool TrySubmitApprovedHit(HitterType hitter, long expectedBallStepIndex,
            Vector2 launchVelocity, bool activateRally)
        {
            if (ballController.CurrentSnapshot.StepIndex != expectedBallStepIndex)
            {
                return false;
            }

            if (!rallyFlowManager.RegisterHit(hitter, out PointResult pointResult))
            {
                return false;
            }

            if (pointResult.HasWinner)
            {
                HandlePointResult(pointResult);
                return false;
            }

            if (activateRally) ballController.SetPlayMode(BallPlayMode.Rally);
            
            // 상대 AI가 친 공은 기획 밸런싱 배율(0.9배 감속 등)을 적용하지 않고, 
            // ShotModel이 네트를 무조건 넘어가도록 연산한 정밀 속도 그대로 발사합니다.
            Vector2 finalVelocity = (hitter == HitterType.Opponent && IsRallyMode)
                ? launchVelocity
                : ApplyBallBalance(launchVelocity);

            ballController.Launch(finalVelocity);
            return true;
        }

        private void HandlePhysicsStep(BallStepResult result)
        {
            if (ballController.PlayMode == BallPlayMode.ServeToss)
            {
                if (result.HadGroundContact)
                {
                    HandlePointResult(new PointResult(CurrentPointId, ballController.CurrentSnapshot.StepIndex, CourtSide.Opponent, CourtSide.Player, FailureReason.DoubleBounce));
                }
                return;
            }

            if (ballController.PlayMode != BallPlayMode.Rally) return;
            if (lifecycle != null && !lifecycle.AllowsRallyEvents)
            {
                return;
            }

            IReadOnlyList<CourtObservation> observations = courtZoneDetector.Evaluate(result);
            for (int index = 0; index < observations.Count; index++)
            {
                if (!rallyFlowManager.ProcessObservation(observations[index], out PointResult pointResult) ||
                    !pointResult.HasWinner)
                {
                    continue;
                }

                HandlePointResult(pointResult);
                break;
            }
        }

        private void HandlePointResult(PointResult result)
        {
            if (lifecycle != null && !lifecycle.TryBeginReset())
            {
                return;
            }

            if (pointLoopConfig != null &&
                pointLoopConfig.IsRallyMode &&
                result.Winner == CourtSide.Player)
            {
                opponentController?.ReportRallyPointEnd(result);
            }

            if (pointLoopConfig != null &&
                pointLoopConfig.IsRallyMode &&
                result.Winner == CourtSide.Opponent)
            {
                RallyGameOverPresenter.NotifyRallyPointEnded(rallyFlowManager.RallyHitCount);
            }

            if (!matchFlowManager.TryApplyPoint(result))
            {
                throw new InvalidOperationException("Lifecycle accepted a duplicate point result.");
            }

            if (matchFlowManager.MatchEnded)
            {
                lifecycle?.MarkMatchEnded();
            }

            if (pointLoopConfig != null && pointLoopConfig.IsRallyMode)
            {
                // 랠리 모드 시 서브권 플레이어 강제 고정하여 AI 서브 우회
                activeServer = HitterType.Player;
            }
            else
            {
                if (result.Winner == CourtSide.Player)
                {
                    activeServer = HitterType.Player;
                }
                else if (result.Winner == CourtSide.Opponent)
                {
                    activeServer = HitterType.Opponent;
                }
            }
            resetFlowController.SetNextServer(activeServer);

            resetFlowController.HandlePointEnd(!matchFlowManager.MatchEnded);
        }

        private void HandleNextPointReady(NextPointRequest request)
        {
            if (lifecycle != null &&
                lifecycle.State == PointLoopState.ResetPending &&
                !lifecycle.TryBeginNextPoint())
            {
                return;
            }

            courtZoneDetector.ResetLatches();
            Vector2 pResetPos = new Vector2(-7.5f, playerResetPosition.y);
            Vector2 oResetPos = new Vector2(7.5f, opponentResetPosition.y);
            playerController?.ResetPlayer(pResetPos);
            opponentController?.ResetOpponent(oResetPos);
            rallyFlowManager.BeginPoint();
            shotExecutionController?.ResetPoint();
            
            if (request.Server == HitterType.Player)
            {
                if (serveFlowController != null)
                {
                    serveFlowController.BeginServe(request.ResetPosition);
                    if (lifecycle != null && !lifecycle.TryActivateRally())
                        throw new InvalidOperationException("Point lifecycle could not enter serve-ready state.");
                }
                return;
            }

            if (serveFlowController != null)
            {
                serveFlowController.enabled = false;
            }

            if (opponentController != null)
            {
                if (opponentServeFlowController != null)
                {
                    opponentServeFlowController.BeginServe(request.ResetPosition);
                    if (lifecycle != null && !lifecycle.TryActivateRally())
                        throw new InvalidOperationException("Point lifecycle could not enter serve-ready state for opponent.");
                }
            }
            else
            {
                ballController.ResetBall(request.ResetPosition);

                if (!rallyFlowManager.RegisterHit(request.Server, out PointResult pointResult) ||
                    pointResult.HasWinner)
                {
                    throw new InvalidOperationException("The fixed server hit could not start the point.");
                }

                ballController.SetPlayMode(BallPlayMode.Rally);
                ballController.Launch(ApplyBallBalance(request.LaunchVelocity));
                if (lifecycle != null && !lifecycle.TryActivateRally())
                {
                    throw new InvalidOperationException("Point lifecycle could not activate the rally.");
                }
            }
        }

        private Vector2 ApplyBallBalance(Vector2 velocity)
        {
            return new Vector2(
                velocity.x * MovementBalanceConfig.BallHorizontalSpeedMultiplierOrDefault(movementBalanceConfig),
                velocity.y * MovementBalanceConfig.BallVerticalSpeedMultiplierOrDefault(movementBalanceConfig));
        }

        private void RequireReferences()
        {
            if (physicsApplier == null || ballController == null ||
                courtZoneDetector == null || rallyFlowManager == null ||
                matchFlowManager == null || resetFlowController == null)
            {
                throw new InvalidOperationException("PointLoopEventBridge references are incomplete.");
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            physicsApplier.OnPhysicsStep += HandlePhysicsStep;
            resetFlowController.OnNextPointReady += HandleNextPointReady;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (physicsApplier != null)
            {
                physicsApplier.OnPhysicsStep -= HandlePhysicsStep;
            }

            if (resetFlowController != null)
            {
                resetFlowController.OnNextPointReady -= HandleNextPointReady;
            }

            subscribed = false;
        }
    }
}
