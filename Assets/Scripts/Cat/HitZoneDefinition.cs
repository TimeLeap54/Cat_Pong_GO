using System;

namespace CatTennis.Rebuild.Cat
{
    public readonly struct HitZoneDefinition
    {
        public HitZoneDefinition(
            float centerX,
            float centerY,
            float halfWidth,
            float halfHeight,
            bool requireForward)
        {
            if (!IsFinite(centerX) || !IsFinite(centerY) ||
                !IsFinite(halfWidth) || !IsFinite(halfHeight) ||
                halfWidth <= 0f || halfHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(HitZoneDefinition));
            }

            CenterX = centerX;
            CenterY = centerY;
            HalfWidth = halfWidth;
            HalfHeight = halfHeight;
            RequireForward = requireForward;
        }

        public float CenterX { get; }
        public float CenterY { get; }
        public float HalfWidth { get; }
        public float HalfHeight { get; }
        public bool RequireForward { get; }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
