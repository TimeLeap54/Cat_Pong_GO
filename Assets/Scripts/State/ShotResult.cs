namespace CatTennis.Rebuild.State
{
    /// <summary>Describes a resolved shot without applying physics.</summary>
    public readonly struct ShotResult
    {
        public ShotResult(long swingId, long ballStepIndex, float velocityX, float velocityY)
        {
            SwingId = swingId;
            BallStepIndex = ballStepIndex;
            VelocityX = velocityX;
            VelocityY = velocityY;
        }

        public long SwingId { get; }
        public long BallStepIndex { get; }
        public float VelocityX { get; }
        public float VelocityY { get; }
    }
}
