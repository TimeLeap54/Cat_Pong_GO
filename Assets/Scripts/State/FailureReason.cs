namespace CatTennis.Rebuild.State
{
    /// <summary>Explains why a point ended or an event was rejected.</summary>
    public enum FailureReason
    {
        None = 0,
        FailedToClear = 1,
        OutBeforeValidBounce = 2,
        UnreturnedAfterValidBounce = 3,
        DoubleBounce = 4,
        DoubleTouch = 5,
        NetStopped = 6,
        InvalidEvent = 7
    }
}
