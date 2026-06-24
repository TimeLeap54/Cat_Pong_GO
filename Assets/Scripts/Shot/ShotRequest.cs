using System;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.State;

namespace CatTennis.Rebuild.Shot
{
    /// <summary>Carries a requested shot into the shot pipeline.</summary>
    public readonly struct ShotRequest
    {
        public ShotRequest(
            long swingId,
            long ballStepIndex,
            HitterType hitter,
            ShotIntent intent,
            BallSnapshot ballSnapshot,
            float playerX,
            float playerY,
            float contactX,
            float contactY,
            int facingDirection)
        {
            SwingId = swingId;
            BallStepIndex = ballStepIndex;
            Hitter = hitter;
            Intent = intent;
            BallSnapshot = ballSnapshot;
            PlayerX = playerX;
            PlayerY = playerY;
            ContactX = contactX;
            ContactY = contactY;
            FacingDirection = facingDirection;
            Validate();
        }

        public long SwingId { get; }
        public long BallStepIndex { get; }
        public HitterType Hitter { get; }
        public ShotIntent Intent { get; }
        public BallSnapshot BallSnapshot { get; }
        public float PlayerX { get; }
        public float PlayerY { get; }
        public float ContactX { get; }
        public float ContactY { get; }
        public int FacingDirection { get; }

        public void Validate()
        {
            if (SwingId <= 0 || BallStepIndex < 0 ||
                BallSnapshot.StepIndex != BallStepIndex || !BallSnapshot.IsActive ||
                Hitter == HitterType.None || Intent == ShotIntent.Undefined ||
                (FacingDirection != -1 && FacingDirection != 1) ||
                !AllFinite())
            {
                throw new ArgumentException("Shot request is invalid.");
            }
        }

        private bool AllFinite()
        {
            return IsFinite(PlayerX) && IsFinite(PlayerY) &&
                   IsFinite(ContactX) && IsFinite(ContactY);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
