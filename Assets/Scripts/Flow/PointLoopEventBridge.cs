using System;
using System.Collections.Generic;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
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

        private bool subscribed;

        private void OnEnable()
        {
            RequireReferences();
            Subscribe();
            if (startOnEnable)
            {
                resetFlowController.RequestInitialPoint();
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

        public void StartInitialPoint()
        {
            if (lifecycle != null && lifecycle.State != PointLoopState.StartingPoint)
            {
                return;
            }

            resetFlowController.RequestInitialPoint();
        }

        public void RetryMatch()
        {
            matchFlowManager.ResetMatch();
            lifecycle?.BeginRetry();
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

        public bool TrySubmitHit(
            HitterType hitter,
            long expectedBallStepIndex,
            Vector2 launchVelocity)
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

            ballController.Launch(launchVelocity);
            return true;
        }

        private void HandlePhysicsStep(BallStepResult result)
        {
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

            if (!matchFlowManager.TryApplyPoint(result))
            {
                throw new InvalidOperationException("Lifecycle accepted a duplicate point result.");
            }

            if (matchFlowManager.MatchEnded)
            {
                lifecycle?.MarkMatchEnded();
            }

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
            playerController?.ResetPlayer(playerResetPosition);
            ballController.ResetBall(request.ResetPosition);
            rallyFlowManager.BeginPoint();
            if (!rallyFlowManager.RegisterHit(request.Server, out PointResult pointResult) ||
                pointResult.HasWinner)
            {
                throw new InvalidOperationException("The fixed server hit could not start the point.");
            }

            ballController.Launch(request.LaunchVelocity);
            if (lifecycle != null && !lifecycle.TryActivateRally())
            {
                throw new InvalidOperationException("Point lifecycle could not activate the rally.");
            }
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
