using System;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    public readonly struct HitContact
    {
        public HitContact(long pointId, long swingId, long ballStepIndex,
            HitterType hitter, ShotIntent intent, Vector2 actorPosition,
            BallSnapshot ballSnapshot, int facingDirection, long inputTick = 0,
            bool isServeToss = false, bool isCounteringSmash = false, float hitHeightRatio = 0.5f,
            bool isCounteringKillSmash = false)
        {
            if (pointId <= 0 || swingId <= 0 || ballStepIndex != ballSnapshot.StepIndex ||
                hitter == HitterType.None || intent == ShotIntent.Undefined)
                throw new ArgumentException("Hit contact is invalid.");
            PointId = pointId; SwingId = swingId; BallStepIndex = ballStepIndex;
            Hitter = hitter; Intent = intent; ActorPosition = actorPosition;
            BallSnapshot = ballSnapshot; FacingDirection = facingDirection; InputTick = inputTick;
            IsServeToss = isServeToss; IsCounteringSmash = isCounteringSmash;
            HitHeightRatio = hitHeightRatio; IsCounteringKillSmash = isCounteringKillSmash;
        }
        public long PointId { get; }
        public long SwingId { get; }
        public long BallStepIndex { get; }
        public HitterType Hitter { get; }
        public ShotIntent Intent { get; }
        public Vector2 ActorPosition { get; }
        public BallSnapshot BallSnapshot { get; }
        public int FacingDirection { get; }
        public long InputTick { get; }
        public bool IsServeToss { get; }
        public bool IsCounteringSmash { get; }
        public float HitHeightRatio { get; }
        public bool IsCounteringKillSmash { get; }

        public ShotRequest ToShotRequest() => new ShotRequest(
            new SwingIntentSnapshot(PointId, SwingId, InputTick,
                new Vector2(FacingDirection, 0f), FacingDirection, Intent),
            BallStepIndex, Hitter, BallSnapshot, ActorPosition.x, ActorPosition.y,
            IsServeToss, IsCounteringSmash, HitHeightRatio, false, IsCounteringKillSmash);
    }
}
