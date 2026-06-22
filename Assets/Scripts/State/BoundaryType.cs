namespace CatTennis.Rebuild.State
{
    /// <summary>Identifies which non-ground boundary the ball exited.</summary>
    public enum BoundaryType
    {
        None = 0,
        PlayerBack = 1,
        OpponentBack = 2,
        Ceiling = 3,
        KillPlane = 4
    }
}
