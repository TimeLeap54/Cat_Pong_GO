namespace CatTennis.Rebuild.State
{
    /// <summary>Classifies a ground contact independently of physical colliders.</summary>
    public enum CourtArea
    {
        None = 0,
        PlayerCourt = 1,
        OpponentCourt = 2,
        Out = 3
    }
}
