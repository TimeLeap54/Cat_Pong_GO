using CatTennis.Rebuild.State;

namespace CatTennis.Rebuild.Shot
{
    public readonly struct ShotTrajectoryResult
    {
        public ShotTrajectoryResult(float vx, float vy, float landingX, float landingY,
            float flightTime, float apexY, float netCrossY, bool valid, FailureReason reason)
        {
            VelocityX = vx; VelocityY = vy; LandingX = landingX; LandingY = landingY;
            FlightTime = flightTime; ApexY = apexY; NetCrossY = netCrossY;
            IsValid = valid; InvalidReason = reason;
        }
        public float VelocityX { get; }
        public float VelocityY { get; }
        public float LandingX { get; }
        public float LandingY { get; }
        public float FlightTime { get; }
        public float ApexY { get; }
        public float NetCrossY { get; }
        public bool IsValid { get; }
        public FailureReason InvalidReason { get; }
    }
}
