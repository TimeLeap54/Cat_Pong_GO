using System;

namespace CatTennis.Rebuild.State
{
    /// <summary>Immutable rule state for exactly one point.</summary>
    public readonly struct RallyContext
    {
        internal RallyContext(
            long pointId,
            long lastProcessedEventId,
            RallyState state,
            HitterType lastHitter,
            CourtSide expectedReceiver,
            CourtArea firstBounceArea,
            bool netTouched)
        {
            PointId = pointId;
            LastProcessedEventId = lastProcessedEventId;
            State = state;
            LastHitter = lastHitter;
            ExpectedReceiver = expectedReceiver;
            FirstBounceArea = firstBounceArea;
            NetTouched = netTouched;
        }

        public long PointId { get; }
        public long LastProcessedEventId { get; }
        public RallyState State { get; }
        public HitterType LastHitter { get; }
        public CourtSide ExpectedReceiver { get; }
        public CourtArea FirstBounceArea { get; }
        public bool NetTouched { get; }
        public bool PointEnded => State == RallyState.Ended;
        public int BounceCount => State == RallyState.ReceiverCourtBounced ? 1 : 0;

        public static RallyContext Create(long pointId)
        {
            if (pointId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pointId));
            }

            return new RallyContext(
                pointId,
                0,
                RallyState.Idle,
                HitterType.None,
                CourtSide.None,
                CourtArea.None,
                false);
        }
    }
}
