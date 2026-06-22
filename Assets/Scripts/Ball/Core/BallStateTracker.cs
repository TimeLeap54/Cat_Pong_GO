using System;

namespace CatTennis.BallPhysics.Core
{
    /// <summary>Owns previous and current snapshots without performing physics.</summary>
    public sealed class BallStateTracker
    {
        public BallStateTracker()
        {
            Reset(0f, 0f);
        }

        public BallSnapshot Previous { get; private set; }
        public BallSnapshot Current { get; private set; }

        public void Reset(float positionX, float positionY)
        {
            RequireFinitePair(positionX, positionY);
            BallSnapshot reset = new BallSnapshot(positionX, positionY, 0f, 0f, false, 0);
            Previous = reset;
            Current = reset;
        }

        public void Launch(float velocityX, float velocityY)
        {
            RequireFinitePair(velocityX, velocityY);
            Previous = Current;
            Current = new BallSnapshot(
                Current.PositionX,
                Current.PositionY,
                velocityX,
                velocityY,
                true,
                Current.StepIndex);
        }

        public void Apply(BallStepResult result)
        {
            ValidateSnapshot(result.NextSnapshot);
            if (result.NextSnapshot.StepIndex < Current.StepIndex)
            {
                throw new ArgumentException("Step index cannot move backwards.", nameof(result));
            }

            Previous = Current;
            Current = result.NextSnapshot;
        }

        public void Stop()
        {
            Previous = Current;
            Current = new BallSnapshot(
                Current.PositionX,
                Current.PositionY,
                0f,
                0f,
                false,
                Current.StepIndex);
        }

        private static void ValidateSnapshot(BallSnapshot snapshot)
        {
            RequireFinitePair(snapshot.PositionX, snapshot.PositionY);
            RequireFinitePair(snapshot.VelocityX, snapshot.VelocityY);
            if (snapshot.StepIndex < 0)
            {
                throw new ArgumentException("Step index cannot be negative.", nameof(snapshot));
            }
        }

        private static void RequireFinitePair(float first, float second)
        {
            ScalarGuard.RequireFinite(first, nameof(first));
            ScalarGuard.RequireFinite(second, nameof(second));
        }
    }
}
