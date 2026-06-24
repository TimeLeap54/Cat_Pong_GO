using System;
using System.Collections;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Config;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    /// <summary>Stops the ball and emits one generation-safe next-point request.</summary>
    public sealed class ResetFlowController : MonoBehaviour
    {
        [SerializeField] private BallController ballController;
        [SerializeField] private Phase3PointLoopConfig config;

        private Coroutine pendingReset;
        private long resetGeneration;

        public event Action<NextPointRequest> OnNextPointReady;

        public void Initialize(BallController ball, Phase3PointLoopConfig pointLoopConfig)
        {
            ballController = ball;
            config = pointLoopConfig;
            config?.ValidateOrThrow();
        }

        public void HandlePointEnd(bool shouldRestart)
        {
            RequireReferences();
            long generation = BeginNewGeneration();
            ballController.StopBall();
            if (shouldRestart)
            {
                ScheduleNextPoint(generation);
            }
        }

        public void RequestRetry()
        {
            RequireReferences();
            long generation = BeginNewGeneration();
            ballController.StopBall();
            ScheduleNextPoint(generation);
        }

        public void RequestInitialPoint()
        {
            RequireReferences();
            long generation = BeginNewGeneration();
            ballController.StopBall();
            EmitNextPointIfCurrent(generation);
        }

        public void CancelPendingReset()
        {
            BeginNewGeneration();
        }

        private void OnDisable()
        {
            BeginNewGeneration();
        }

        private void ScheduleNextPoint(long generation)
        {
            if (config.ResetDelay <= 0f)
            {
                EmitNextPointIfCurrent(generation);
                return;
            }

            pendingReset = StartCoroutine(EmitAfterDelay(generation));
        }

        private IEnumerator EmitAfterDelay(long generation)
        {
            yield return new WaitForSeconds(config.ResetDelay);

            EmitNextPointIfCurrent(generation);
        }

        private void EmitNextPointIfCurrent(long generation)
        {
            if (generation != resetGeneration)
            {
                return;
            }

            pendingReset = null;
            OnNextPointReady?.Invoke(new NextPointRequest(
                config.Server,
                config.ResetPosition,
                config.FixedTestServeVelocity));
        }

        private long BeginNewGeneration()
        {
            if (pendingReset != null)
            {
                StopCoroutine(pendingReset);
                pendingReset = null;
            }

            if (resetGeneration == long.MaxValue)
            {
                resetGeneration = 0;
            }

            resetGeneration++;
            return resetGeneration;
        }

        private void RequireReferences()
        {
            if (ballController == null || config == null)
            {
                throw new InvalidOperationException(
                    "BallController and Phase3PointLoopConfig are required.");
            }

            config.ValidateOrThrow();
        }
    }
}
