namespace CatTennis.Rebuild.State
{
    /// <summary>Describes a resolved shot without applying physics.</summary>
    public readonly struct ShotResult
    {
        public ShotResult(long swingId, long ballStepIndex, float velocityX, float velocityY,
            bool isValid = true, FailureReason invalidReason = FailureReason.None,
            float predictedLandingX = 0f, float flightTime = 0f, float apexY = 0f,
            float netCrossY = 0f)
        {
            SwingId = swingId;
            BallStepIndex = ballStepIndex;
            VelocityX = velocityX;
            VelocityY = velocityY;
            IsValid = isValid; InvalidReason = invalidReason;
            PredictedLandingX = predictedLandingX; FlightTime = flightTime;
            ApexY = apexY; NetCrossY = netCrossY;
        }

        public long SwingId { get; }
        public long BallStepIndex { get; }
        public float VelocityX { get; }
        public float VelocityY { get; }
        public bool IsValid { get; }
        public FailureReason InvalidReason { get; }
        public float PredictedLandingX { get; }
        public float FlightTime { get; }
        public float ApexY { get; }
        public float NetCrossY { get; }
    }
}
