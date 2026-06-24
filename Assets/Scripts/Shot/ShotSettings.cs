using System;

namespace CatTennis.Rebuild.Shot
{
    public readonly struct ShotSettings
    {
        public ShotSettings(
            float safeHorizontalSpeed,
            float safeVerticalSpeed,
            float smashHorizontalSpeed,
            float smashVerticalSpeed,
            float maxHorizontalSpeed,
            float maxRiseSpeed,
            float maxFallSpeed)
        {
            SafeHorizontalSpeed = safeHorizontalSpeed;
            SafeVerticalSpeed = safeVerticalSpeed;
            SmashHorizontalSpeed = smashHorizontalSpeed;
            SmashVerticalSpeed = smashVerticalSpeed;
            MaxHorizontalSpeed = maxHorizontalSpeed;
            MaxRiseSpeed = maxRiseSpeed;
            MaxFallSpeed = maxFallSpeed;
            Validate();
        }

        public float SafeHorizontalSpeed { get; }
        public float SafeVerticalSpeed { get; }
        public float SmashHorizontalSpeed { get; }
        public float SmashVerticalSpeed { get; }
        public float MaxHorizontalSpeed { get; }
        public float MaxRiseSpeed { get; }
        public float MaxFallSpeed { get; }

        public void Validate()
        {
            if (!AllFinite() || SafeHorizontalSpeed <= 0f || SmashHorizontalSpeed <= 0f ||
                MaxHorizontalSpeed <= 0f || MaxRiseSpeed <= 0f || MaxFallSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(ShotSettings));
            }
        }

        private bool AllFinite()
        {
            return IsFinite(SafeHorizontalSpeed) && IsFinite(SafeVerticalSpeed) &&
                   IsFinite(SmashHorizontalSpeed) && IsFinite(SmashVerticalSpeed) &&
                   IsFinite(MaxHorizontalSpeed) && IsFinite(MaxRiseSpeed) &&
                   IsFinite(MaxFallSpeed);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
