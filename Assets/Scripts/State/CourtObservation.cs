namespace CatTennis.Rebuild.State
{
    /// <summary>One deterministic physical observation awaiting rule interpretation.</summary>
    public readonly struct CourtObservation
    {
        public CourtObservation(
            CourtObservationType type,
            long sourceStepIndex,
            CourtArea courtArea = CourtArea.None,
            BoundaryType boundaryType = BoundaryType.None)
        {
            Type = type;
            SourceStepIndex = sourceStepIndex;
            CourtArea = courtArea;
            BoundaryType = boundaryType;
        }

        public CourtObservationType Type { get; }
        public long SourceStepIndex { get; }
        public CourtArea CourtArea { get; }
        public BoundaryType BoundaryType { get; }
    }
}
