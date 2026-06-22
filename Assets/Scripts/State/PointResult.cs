namespace CatTennis.Rebuild.State
{
    /// <summary>Describes one terminal rule decision without changing score state.</summary>
    public readonly struct PointResult
    {
        public PointResult(
            long pointId,
            long sourceEventId,
            CourtSide winner,
            CourtSide loser,
            FailureReason failureReason)
        {
            PointId = pointId;
            SourceEventId = sourceEventId;
            HasWinner = winner != CourtSide.None;
            Winner = winner;
            Loser = loser;
            FailureReason = failureReason;
        }

        public long PointId { get; }
        public long SourceEventId { get; }
        public bool HasWinner { get; }
        public CourtSide Winner { get; }
        public CourtSide Loser { get; }
        public FailureReason FailureReason { get; }
    }
}
