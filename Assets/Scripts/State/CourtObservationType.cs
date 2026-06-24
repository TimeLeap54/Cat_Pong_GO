namespace CatTennis.Rebuild.State
{
    /// <summary>Neutral court observations produced from one physics step.</summary>
    public enum CourtObservationType
    {
        GroundTouch = 0,
        BallSettled = 1,
        BoundaryExit = 2
    }
}
