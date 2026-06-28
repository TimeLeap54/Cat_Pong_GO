using System;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.State;

namespace CatTennis.Rebuild.Shot
{
    public readonly struct ShotRequest
    {
        public ShotRequest(SwingIntentSnapshot intentSnapshot, long ballStepIndex,
            HitterType hitter, BallSnapshot ballSnapshot, float originX, float originY,
            bool isServeToss = false, bool isCounteringSmash = false, float hitHeightRatio = 0.5f,
            bool isKillSmash = false, bool isCounteringKillSmash = false)
        {
            IntentSnapshot = intentSnapshot;
            BallStepIndex = ballStepIndex;
            Hitter = hitter;
            BallSnapshot = ballSnapshot;
            OriginX = originX;
            OriginY = originY;
            IsServeToss = isServeToss;
            IsCounteringSmash = isCounteringSmash;
            HitHeightRatio = hitHeightRatio;
            IsKillSmash = isKillSmash;
            IsCounteringKillSmash = isCounteringKillSmash;
            Validate();
        }

        public ShotRequest(long swingId, long ballStepIndex, HitterType hitter,
            ShotIntent intent, BallSnapshot ballSnapshot, float playerX, float playerY,
            float contactX, float contactY, int facingDirection)
            : this(new SwingIntentSnapshot(1, swingId, 0,
                    new UnityEngine.Vector2(facingDirection, 0f), facingDirection, intent),
                ballStepIndex, hitter, ballSnapshot, playerX, playerY, false, false, 0.5f, false, false) { }

        public SwingIntentSnapshot IntentSnapshot { get; }
        public long PointId => IntentSnapshot.PointId;
        public long SwingId => IntentSnapshot.SwingId;
        public long InputTick => IntentSnapshot.InputTick;
        public long BallStepIndex { get; }
        public HitterType Hitter { get; }
        public ShotIntent Intent => IntentSnapshot.Intent;
        public BallSnapshot BallSnapshot { get; }
        public float OriginX { get; }
        public float OriginY { get; }
        public int FacingDirection => IntentSnapshot.FacingDirection;
        public bool IsServeToss { get; }
        public bool IsCounteringSmash { get; }
        public float HitHeightRatio { get; }
        public bool IsKillSmash { get; }
        public bool IsCounteringKillSmash { get; }

        public void Validate()
        {
            if (BallStepIndex < 0 || BallSnapshot.StepIndex != BallStepIndex ||
                !BallSnapshot.IsActive || Hitter == HitterType.None ||
                float.IsNaN(OriginX) || float.IsNaN(OriginY))
            {
                throw new ArgumentException("Shot request is invalid.");
            }
        }
    }
}
