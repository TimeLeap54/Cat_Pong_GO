using System;

namespace CatTennis.BallPhysics.Core
{
    /// <summary>Advances immutable ball state using deterministic fixed-step arithmetic.</summary>
    public sealed class BallPhysicsModel
    {
        public BallStepResult Step(
            BallSnapshot snapshot,
            BallPhysicsSettings settings,
            BallStepInput input)
        {
            ValidateSettings(settings);
            ValidateSnapshot(snapshot);
            ValidateInput(input);

            if (!snapshot.IsActive)
            {
                return new BallStepResult(snapshot, false, false, false, 0f, 0f);
            }

            if (snapshot.StepIndex == long.MaxValue)
            {
                throw new InvalidOperationException("Step index cannot advance beyond Int64.MaxValue.");
            }

            float velocityX = ScalarGuard.Clamp(
                snapshot.VelocityX,
                -settings.MaxHorizontalSpeed,
                settings.MaxHorizontalSpeed);
            float velocityY = ScalarGuard.Clamp(
                snapshot.VelocityY + settings.Gravity * input.FixedDeltaTime,
                -settings.MaxFallSpeed,
                settings.MaxRiseSpeed);

            float positionX = snapshot.PositionX + velocityX * input.FixedDeltaTime;
            float positionY = snapshot.PositionY + velocityY * input.FixedDeltaTime;
            EnsureFiniteResult(positionX, positionY, velocityX, velocityY);

            bool hadGroundContact = false;
            bool didBounce = false;
            bool didSettle = false;
            float impactSpeed = 0f;
            float contactY = 0f;
            bool isActive = true;

            if (input.HasGroundPlane)
            {
                float ballBottom = positionY - settings.BallRadius;
                bool isPenetrating = ballBottom < input.GroundY;
                bool isDownwardContact =
                    velocityY <= 0f &&
                    ballBottom <= input.GroundY + settings.GroundSkin;

                if (isDownwardContact)
                {
                    hadGroundContact = true;
                    contactY = input.GroundY;
                    positionY = input.GroundY + settings.BallRadius;
                    impactSpeed = ScalarGuard.Abs(velocityY);
                    float reboundSpeed = impactSpeed * settings.GroundRestitution;

                    velocityX = ScalarGuard.Clamp(
                        velocityX * settings.HorizontalBounceRetention,
                        -settings.MaxHorizontalSpeed,
                        settings.MaxHorizontalSpeed);

                    if (reboundSpeed < settings.MinBounceSpeed)
                    {
                        velocityX = 0f;
                        velocityY = 0f;
                        isActive = false;
                        didSettle = true;
                    }
                    else
                    {
                        velocityY = ScalarGuard.Clamp(
                            reboundSpeed,
                            0f,
                            settings.MaxRiseSpeed);
                        didBounce = true;
                    }
                }
                else if (isPenetrating)
                {
                    hadGroundContact = true;
                    contactY = input.GroundY;
                    positionY = input.GroundY + settings.BallRadius;
                }
            }

            EnsureFiniteResult(positionX, positionY, velocityX, velocityY);
            BallSnapshot next = new BallSnapshot(
                positionX,
                positionY,
                velocityX,
                velocityY,
                isActive,
                snapshot.StepIndex + 1);
            return new BallStepResult(
                next,
                hadGroundContact,
                didBounce,
                didSettle,
                impactSpeed,
                contactY);
        }

        private static void ValidateSettings(BallPhysicsSettings settings)
        {
            ScalarGuard.RequireFinite(settings.Gravity, nameof(settings.Gravity));
            ScalarGuard.RequireFinite(settings.BallRadius, nameof(settings.BallRadius));
            ScalarGuard.RequireFinite(settings.GroundRestitution, nameof(settings.GroundRestitution));
            ScalarGuard.RequireFinite(
                settings.HorizontalBounceRetention,
                nameof(settings.HorizontalBounceRetention));
            ScalarGuard.RequireFinite(settings.MaxHorizontalSpeed, nameof(settings.MaxHorizontalSpeed));
            ScalarGuard.RequireFinite(settings.MaxRiseSpeed, nameof(settings.MaxRiseSpeed));
            ScalarGuard.RequireFinite(settings.MaxFallSpeed, nameof(settings.MaxFallSpeed));
            ScalarGuard.RequireFinite(settings.MinBounceSpeed, nameof(settings.MinBounceSpeed));
            ScalarGuard.RequireFinite(settings.GroundSkin, nameof(settings.GroundSkin));

            if (settings.Gravity >= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.Gravity), "Gravity must be negative.");
            }

            if (settings.BallRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.BallRadius));
            }

            RequireUnitInterval(settings.GroundRestitution, nameof(settings.GroundRestitution));
            RequireUnitInterval(
                settings.HorizontalBounceRetention,
                nameof(settings.HorizontalBounceRetention));

            if (settings.MaxHorizontalSpeed <= 0f ||
                settings.MaxRiseSpeed <= 0f ||
                settings.MaxFallSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "Speed limits must be positive.");
            }

            if (settings.MinBounceSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.MinBounceSpeed));
            }

            if (settings.GroundSkin < 0f || settings.GroundSkin >= settings.BallRadius)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settings.GroundSkin),
                    "Ground skin must be non-negative and smaller than ball radius.");
            }
        }

        private static void ValidateSnapshot(BallSnapshot snapshot)
        {
            ScalarGuard.RequireFinite(snapshot.PositionX, nameof(snapshot.PositionX));
            ScalarGuard.RequireFinite(snapshot.PositionY, nameof(snapshot.PositionY));
            ScalarGuard.RequireFinite(snapshot.VelocityX, nameof(snapshot.VelocityX));
            ScalarGuard.RequireFinite(snapshot.VelocityY, nameof(snapshot.VelocityY));
            if (snapshot.StepIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(snapshot.StepIndex));
            }
        }

        private static void ValidateInput(BallStepInput input)
        {
            ScalarGuard.RequireFinite(input.FixedDeltaTime, nameof(input.FixedDeltaTime));
            ScalarGuard.RequireFinite(input.GroundY, nameof(input.GroundY));
            if (input.FixedDeltaTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(input.FixedDeltaTime));
            }
        }

        private static void RequireUnitInterval(float value, string parameterName)
        {
            if (value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be between zero and one.");
            }
        }

        private static void EnsureFiniteResult(
            float positionX,
            float positionY,
            float velocityX,
            float velocityY)
        {
            if (!ScalarGuard.IsFinite(positionX) ||
                !ScalarGuard.IsFinite(positionY) ||
                !ScalarGuard.IsFinite(velocityX) ||
                !ScalarGuard.IsFinite(velocityY))
            {
                throw new InvalidOperationException("Physics step produced a non-finite result.");
            }
        }
    }
}
