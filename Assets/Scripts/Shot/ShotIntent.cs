namespace CatTennis.Rebuild.Shot
{
    /// <summary>Names player or AI shot intent without trajectory logic.</summary>
    public enum ShotIntent
    {
        Undefined = 0,
        SafeReturn = 1,
        Deep = 2,
        Drop = 3,
        Lob = 4,
        Smash = 5,
        Serve = 6
    }
}
