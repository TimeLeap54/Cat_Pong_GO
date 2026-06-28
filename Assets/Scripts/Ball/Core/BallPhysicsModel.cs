using System;

namespace CatTennis.BallPhysics.Core
{
    /// <summary>Advances immutable ball state using deterministic fixed-step arithmetic.</summary>
    public sealed class BallPhysicsModel
    {
        private static readonly System.Random random = new System.Random();

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

            // [네트 충돌 물리 반사 계산]
            // settings.NetX는 통상 0f, 네트 높이는 GroundY + 1.0f 부근
            float netX = 0f;
            float netHeightLimit = input.GroundY + 1.0f + settings.BallRadius;
            
            // 공의 이전 X 부호와 다음 X 부호가 바뀌는 순간 (네트 평면 통과 검출)
            if (System.Math.Sign(snapshot.PositionX - netX) != System.Math.Sign(positionX - netX))
            {
                // 교차 시점의 Y 높이를 선형 보간하여 정밀 추적 (터널링 방지)
                float dx = positionX - snapshot.PositionX;
                if (System.Math.Abs(dx) > 0.0001f)
                {
                    float t = (netX - snapshot.PositionX) / dx;
                    float yAtNet = snapshot.PositionY + (positionY - snapshot.PositionY) * t;
                    
                    // 네트 상단 높이보다 낮게 날아와 부딪힌 경우 (네트 옆면도 포함)
                    if (yAtNet <= netHeightLimit)
                    {
                        float topThreshold = netHeightLimit - 0.08f;

                        // 35% 확률로 네트 럭키 샷 (Net Cord) 발동하여 상대편 진영으로 기어감
                        if (yAtNet >= topThreshold && random.NextDouble() <= 0.35)
                        {
                            velocityX = velocityX * 0.35f; // 가던 방향 유지, 속도 65% 감속
                            velocityY = 1.6f;              // 수직 홉 튕김 높이
                            positionX = snapshot.PositionX > netX ? netX - 0.15f : netX + 0.15f; // 반대편 진영으로 통과
                        }
                        else
                        {
                            // 일반 네트 충돌 피격 (반대 방향 튕김)
                            velocityX = -velocityX * 0.5f;
                            velocityY = System.Math.Max(velocityY, 2.2f);
                            positionX = snapshot.PositionX > netX ? netX + 0.15f : netX - 0.15f; // 자기 진영으로 밀림
                        }
                    }
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
