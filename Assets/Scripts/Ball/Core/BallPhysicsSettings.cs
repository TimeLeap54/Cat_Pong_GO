namespace CatTennis.BallPhysics.Core
{
    /// <summary>Immutable tuning values consumed by the pure physics model.</summary>
    public readonly struct BallPhysicsSettings
    {
        public BallPhysicsSettings(
            float gravity,
            float ballRadius,
            float groundRestitution,
            float horizontalBounceRetention,
            float maxHorizontalSpeed,
            float maxRiseSpeed,
            float maxFallSpeed,
            float minBounceSpeed,
            float groundSkin)
        {
            Gravity = gravity;
            BallRadius = ballRadius;
            GroundRestitution = groundRestitution;
            HorizontalBounceRetention = horizontalBounceRetention;
            MaxHorizontalSpeed = maxHorizontalSpeed;
            MaxRiseSpeed = maxRiseSpeed;
            MaxFallSpeed = maxFallSpeed;
            MinBounceSpeed = minBounceSpeed;
            GroundSkin = groundSkin;
        }

        public float Gravity { get; }
        public float BallRadius { get; }
        public float GroundRestitution { get; }
        public float HorizontalBounceRetention { get; }
        public float MaxHorizontalSpeed { get; }
        public float MaxRiseSpeed { get; }
        public float MaxFallSpeed { get; }
        public float MinBounceSpeed { get; }
        public float GroundSkin { get; }
    }
}
