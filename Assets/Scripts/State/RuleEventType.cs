namespace CatTennis.Rebuild.State
{
    /// <summary>Lists every event understood by the Phase 1 rule engine.</summary>
    public enum RuleEventType
    {
        Hit = 0,
        GroundTouch = 1,
        NetContact = 2,
        NetStopped = 3,
        BoundaryExit = 4,
        BallSettled = 5
    }
}
