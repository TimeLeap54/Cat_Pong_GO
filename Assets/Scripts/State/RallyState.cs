namespace CatTennis.Rebuild.State
{
    /// <summary>Represents the complete rule-level lifecycle of one point.</summary>
    public enum RallyState
    {
        Idle = 0,
        InFlight = 1,
        ReceiverCourtBounced = 2,
        Ended = 3
    }
}
