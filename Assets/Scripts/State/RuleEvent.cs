namespace CatTennis.Rebuild.State
{
    /// <summary>One ordered, point-scoped observation submitted to the rule engine.</summary>
    public readonly struct RuleEvent
    {
        public RuleEvent(
            long pointId,
            long eventId,
            RuleEventType type,
            HitterType hitter = HitterType.None,
            CourtArea courtArea = CourtArea.None,
            BoundaryType boundaryType = BoundaryType.None)
        {
            PointId = pointId;
            EventId = eventId;
            Type = type;
            Hitter = hitter;
            CourtArea = courtArea;
            BoundaryType = boundaryType;
        }

        public long PointId { get; }
        public long EventId { get; }
        public RuleEventType Type { get; }
        public HitterType Hitter { get; }
        public CourtArea CourtArea { get; }
        public BoundaryType BoundaryType { get; }
    }
}
