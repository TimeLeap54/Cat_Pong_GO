using CatTennis.Rebuild.State;

namespace CatTennis.Rebuild.Shot
{
    public sealed class ShotModel
    {
        private readonly ShotTrajectoryModel trajectory = new ShotTrajectoryModel();

        public ShotResult Resolve(ShotRequest request, ShotSettings settings)
        {
            request.Validate(); settings.Validate();
            ShotProfileSettings profile = settings.Profile(request.Intent);
            bool toOpponent = request.Hitter == HitterType.Player;
            float min = toOpponent ? settings.OpponentMinX : settings.PlayerMaxX;
            float max = toOpponent ? settings.OpponentMaxX : settings.PlayerMinX;
            
            float targetX = min + (max - min) * profile.LandingRatio;
            float flightTime = profile.FlightTime;
            float apexHeight = profile.ApexHeight;

            // --- 서브 상황 (Serve / Smash Serve) 커스텀 물리 보정 ---
            if (request.IsServeToss || request.Intent == ShotIntent.Serve)
            {
                // 1. 토스 타이밍(정점과의 오차) 계산
                float tPeak = settings.TossSpeed / System.Math.Abs(settings.Gravity); // 약 0.53초
                float tElapsed = request.BallSnapshot.StepIndex * 0.02f; // fixedDeltaTime = 0.02f
                float deltaT = System.Math.Abs(tElapsed - tPeak);

                float targetRatio = 0.72f;
                float randomWidth = 0.40f;

                if (deltaT <= 0.08f) // Perfect 타이밍 (정점 부근)
                {
                    // J(일반 서브)이면 숏 서브, K(스매시)이면 백코너 롱 서브
                    targetRatio = (request.Intent == ShotIntent.Serve) ? 0.35f : 0.88f;
                    flightTime = (request.Intent == ShotIntent.Serve) ? 0.75f : 0.58f;
                    randomWidth = 0.15f; // Perfect 타격 시 흔들림 최소화
                }
                else if (deltaT <= 0.18f) // Good 타이밍 (안정 타격)
                {
                    targetRatio = (request.Intent == ShotIntent.Serve) ? 0.60f : 0.72f;
                    flightTime = (request.Intent == ShotIntent.Serve) ? 0.95f : 0.65f;
                    randomWidth = 0.40f; // 표준 흔들림
                }
                else // Bad 타이밍 (Early / Late)
                {
                    targetRatio = UnityEngine.Random.Range(0.40f, 0.60f); // 엉성하게 들어감
                    flightTime = profile.FlightTime * 1.25f; // 속도 대폭 감소 (매우 느림)
                    randomWidth = 1.20f; // 흔들림 극대화 (아웃/네트 걸림 유발)
                }

                // 2. 최종 Landing Target X 보정
                targetX = min + (max - min) * targetRatio;

                // 3. 통제된 우연성 오프셋 적용
                float noise = UnityEngine.Random.Range(-randomWidth, randomWidth);
                targetX += noise;
            }
            // --- 랠리 상황 (3번 Y축 타점 세기 & 4번 스매시 카운터 드롭) ---
            else
            {
                int facing = request.FacingDirection;
                bool isCounteringSmash = request.IsCounteringSmash;

                // 4번: 상대 스매시 카운터 (내 샷이 Lob이 아닐 때 강제 Drop 치환)
                if (isCounteringSmash && request.Intent != ShotIntent.Lob)
                {
                    profile = settings.Profile(ShotIntent.Drop);
                    flightTime = profile.FlightTime;
                    apexHeight = profile.ApexHeight;
                    targetX = min + (max - min) * profile.LandingRatio;
                }
                // 3번: 타점에 따른 세기 조절 (카운터 상황이 아닐 때만 적용)
                else
                {
                    float ratio = request.HitHeightRatio;

                    if (ratio >= 0.80f) // 높은 타점 (Sweet Spot)
                    {
                        flightTime *= 0.85f; // 15% 구속 증가
                        apexHeight = System.Math.Max(0.2f, apexHeight - 0.3f); // 예리하게 덮쳐 침
                        targetX += facing * 0.8f; // 코너 깊숙이 롱 샷
                    }
                    else if (ratio <= 0.30f) // 낮은 타점 (Defensive 걷어올리기)
                    {
                        flightTime *= 1.25f; // 25% 구속 감소
                        apexHeight += 0.8f;  // 높게 걷어 올리기
                        targetX -= facing * 0.8f; // 네트 근처로 얕게 떨어짐
                    }
                }
            }
            // --------------------------------------------------------

            ShotTrajectoryResult resolved = request.Intent == ShotIntent.Smash
                ? trajectory.ResolveTime(request.BallSnapshot.PositionX, request.BallSnapshot.PositionY,
                    targetX, settings.TargetY, flightTime, settings)
                : trajectory.ResolveApex(request.BallSnapshot.PositionX, request.BallSnapshot.PositionY,
                    targetX, settings.TargetY,
                    System.Math.Max(request.BallSnapshot.PositionY, settings.TargetY) + apexHeight,
                    settings);
            return new ShotResult(request.SwingId, request.BallStepIndex,
                resolved.VelocityX, resolved.VelocityY, resolved.IsValid, resolved.InvalidReason,
                resolved.LandingX, resolved.FlightTime, resolved.ApexY, resolved.NetCrossY);
        }
    }
}
