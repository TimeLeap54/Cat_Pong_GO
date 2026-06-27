using System;

namespace CatTennis.Rebuild.Shot
{
    public readonly struct ShotProfileSettings
    {
        public ShotProfileSettings(float landingRatio, float apexHeight, float flightTime)
        {
            LandingRatio = landingRatio;
            ApexHeight = apexHeight;
            FlightTime = flightTime;
        }

        public float LandingRatio { get; }
        public float ApexHeight { get; }
        public float FlightTime { get; }
    }

    public readonly struct ShotSettings
    {
        public ShotSettings(float gravity, float targetY, float playerMinX, float playerMaxX,
            float opponentMinX, float opponentMaxX, float netX, float netHeight,
            float clearance, float maxX, float maxRise, float maxFall,
            float deadZone, float verticalThreshold, ShotProfileSettings safe,
            ShotProfileSettings deep, ShotProfileSettings drop, ShotProfileSettings lob,
            ShotProfileSettings smash, ShotProfileSettings serve, float tossSpeed = 5.2f)
        {
            Gravity = gravity; TargetY = targetY;
            PlayerMinX = playerMinX; PlayerMaxX = playerMaxX;
            OpponentMinX = opponentMinX; OpponentMaxX = opponentMaxX;
            NetX = netX; NetHeight = netHeight; Clearance = clearance;
            MaxHorizontalSpeed = maxX; MaxRiseSpeed = maxRise; MaxFallSpeed = maxFall;
            AimDeadZone = deadZone; VerticalThreshold = verticalThreshold;
            Safe = safe; Deep = deep; Drop = drop; Lob = lob; Smash = smash; Serve = serve;
            TossSpeed = tossSpeed;
            Validate();
        }

        public float Gravity { get; }
        public float TargetY { get; }
        public float PlayerMinX { get; }
        public float PlayerMaxX { get; }
        public float OpponentMinX { get; }
        public float OpponentMaxX { get; }
        public float NetX { get; }
        public float NetHeight { get; }
        public float Clearance { get; }
        public float MaxHorizontalSpeed { get; }
        public float MaxRiseSpeed { get; }
        public float MaxFallSpeed { get; }
        public float AimDeadZone { get; }
        public float VerticalThreshold { get; }
        public ShotProfileSettings Safe { get; }
        public ShotProfileSettings Deep { get; }
        public ShotProfileSettings Drop { get; }
        public ShotProfileSettings Lob { get; }
        public ShotProfileSettings Smash { get; }
        public ShotProfileSettings Serve { get; }
        public float TossSpeed { get; }

        public void Validate()
        {
            if (Gravity >= 0f || PlayerMinX >= PlayerMaxX || OpponentMinX >= OpponentMaxX ||
                MaxHorizontalSpeed <= 0f || MaxRiseSpeed <= 0f || MaxFallSpeed <= 0f ||
                AimDeadZone < 0f || VerticalThreshold < 0f || Clearance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(ShotSettings));
            }
        }

        public ShotProfileSettings Profile(ShotIntent intent)
        {
            switch (intent)
            {
                case ShotIntent.SafeReturn: return Safe;
                case ShotIntent.Deep: return Deep;
                case ShotIntent.Drop: return Drop;
                case ShotIntent.Lob: return Lob;
                case ShotIntent.Smash: return Smash;
                case ShotIntent.Serve: return Serve;
                default: throw new InvalidOperationException("Unsupported shot intent.");
            }
        }
    }
}
