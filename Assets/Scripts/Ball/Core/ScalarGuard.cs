using System;

namespace CatTennis.BallPhysics.Core
{
    internal static class ScalarGuard
    {
        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static void RequireFinite(float value, string parameterName)
        {
            if (!IsFinite(value))
            {
                throw new ArgumentException("Value must be finite.", parameterName);
            }
        }

        public static float Clamp(float value, float minimum, float maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }

        public static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }
    }
}
