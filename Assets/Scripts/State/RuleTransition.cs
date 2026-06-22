namespace CatTennis.Rebuild.State
{
    /// <summary>Contains the complete result of evaluating one rule event.</summary>
    public readonly struct RuleTransition
    {
        public RuleTransition(
            bool accepted,
            RallyContext nextContext,
            bool hasPointResult,
            PointResult pointResult,
            FailureReason rejectionReason = FailureReason.None)
        {
            Accepted = accepted;
            NextContext = nextContext;
            HasPointResult = hasPointResult;
            PointResult = pointResult;
            RejectionReason = rejectionReason;
        }

        public bool Accepted { get; }
        public RallyContext NextContext { get; }
        public bool HasPointResult { get; }
        public PointResult PointResult { get; }
        public FailureReason RejectionReason { get; }

        public static RuleTransition Rejected(RallyContext context, FailureReason reason = FailureReason.InvalidEvent)
        {
            return new RuleTransition(false, context, false, default, reason);
        }
    }
}
