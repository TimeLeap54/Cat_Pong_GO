namespace CatTennis.BallPhysics.Core
{
    /// <summary>Immutable state required to advance the ball by one fixed step.</summary>
    public readonly struct BallSnapshot
    {
        public BallSnapshot(
            float positionX,
            float positionY,
            float velocityX,
            float velocityY,
            bool isActive,
            long stepIndex)
        {
            PositionX = positionX;
            PositionY = positionY;
            VelocityX = velocityX;
            VelocityY = velocityY;
            IsActive = isActive;
            StepIndex = stepIndex;
        }

        public float PositionX { get; }
        public float PositionY { get; }
        public float VelocityX { get; }
        public float VelocityY { get; }
        public bool IsActive { get; }
        public long StepIndex { get; }
    }
}
