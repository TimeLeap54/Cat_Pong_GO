using System;
using System.Collections;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.State;
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
        private HitterType nextServer = HitterType.Player;

        public event Action<NextPointRequest> OnNextPointReady;

        public void Initialize(BallController ball, Phase3PointLoopConfig pointLoopConfig)
        {
            ballController = ball;
            config = pointLoopConfig;
            config?.ValidateOrThrow();
            nextServer = config != null ? config.Server : HitterType.Player;
        }

        public void SetNextServer(HitterType server)
        {
            nextServer = server;
        }

        public void HandlePointEnd(bool shouldRestart)
        {
            RequireReferences();
            long generation = BeginNewGeneration();
            if (shouldRestart)
            {
                ScheduleNextPoint(generation);
            }
        }

        public void RequestRetry()
        {
            RequireReferences();
            long generation = BeginNewGeneration();
            ScheduleNextPoint(generation);
        }

        public void RequestInitialPoint()
        {
            RequireReferences();
            long generation = BeginNewGeneration();
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
            ballController.StopBall();

            Vector2 position = config.ResetPosition;
            Vector2 velocity = config.FixedTestServeVelocity;

            if (nextServer == HitterType.Opponent)
            {
                position.x = -position.x;
                velocity.x = -velocity.x;
            }

            OnNextPointReady?.Invoke(new NextPointRequest(
                nextServer,
                position,
                velocity));
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
