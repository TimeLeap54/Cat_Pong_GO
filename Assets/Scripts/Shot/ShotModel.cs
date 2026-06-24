using System;
using CatTennis.Rebuild.State;

namespace CatTennis.Rebuild.Shot
{
    /// <summary>Resolves shot requests into domain results.</summary>
    public sealed class ShotModel
    {
        public ShotResult Resolve(ShotRequest request, ShotSettings settings)
        {
            request.Validate();
            settings.Validate();

            float horizontal;
            float vertical;
            switch (request.Intent)
            {
                case ShotIntent.SafeReturn:
                    horizontal = settings.SafeHorizontalSpeed;
                    vertical = settings.SafeVerticalSpeed;
                    break;
                case ShotIntent.Smash:
                    horizontal = settings.SmashHorizontalSpeed;
                    vertical = settings.SmashVerticalSpeed;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported shot intent.");
            }

            horizontal = Clamp(horizontal * request.FacingDirection,
                -settings.MaxHorizontalSpeed, settings.MaxHorizontalSpeed);
            vertical = Clamp(vertical, -settings.MaxFallSpeed, settings.MaxRiseSpeed);
            return new ShotResult(
                request.SwingId,
                request.BallStepIndex,
                horizontal,
                vertical);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
    }
}
