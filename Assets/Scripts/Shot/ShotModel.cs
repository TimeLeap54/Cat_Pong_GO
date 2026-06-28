using CatTennis.Rebuild.State;

namespace CatTennis.Rebuild.Shot
{
    public sealed class ShotModel
    {
        private readonly ShotTrajectoryModel trajectory = new ShotTrajectoryModel();

        public ShotResult Resolve(ShotRequest request, ShotSettings settings, int rallyCount = 0, CatTennis.Rebuild.Config.RallyAiBalanceConfig rallyAiConfig = null, bool isRallyMode = false, bool isVolley = false)
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
                 // 9-10번 피드백: 랠리 모드 시 Deep/Drop/Lob의 타겟 낙하지점 배율을 보다 명확하고 깊게/얕게 재정의
                if (isRallyMode)
                {
                    if (request.Intent == ShotIntent.Deep)
                    {
                        targetX = min + (max - min) * 0.88f; // 아웃라인 구석 근처로 길게
                        flightTime = 0.65f; // 시원하게 날아가는 속도
                        apexHeight = 1.30f; // 적당한 드라이브 높이
                    }
                    else if (request.Intent == ShotIntent.Drop)
                    {
                        targetX = min + (max - min) * 0.12f; // 네트 바로 앞에 떨어지게 얕게
                        flightTime = 0.70f; // 가볍게 툭 떨어지는 비행시간
                        apexHeight = 1.15f; // 네트 높이와 거의 동일하게 얕음
                    }
                    else if (request.Intent == ShotIntent.Lob)
                    {
                        targetX = min + (max - min) * 0.86f; // 로브도 아웃라인 부근 깊숙이
                    }
                }

                int facing = request.FacingDirection;
                bool isCounteringSmash = request.IsCounteringSmash;

                // 4번: 상대 스매시 카운터 로직
                if (isCounteringSmash)
                {
                    if (request.Hitter == HitterType.Opponent && request.Intent != ShotIntent.Lob)
                    {
                        // AI가 플레이어의 스매시를 받아칠 때:
                        // 1. 만약 플레이어의 전 샷이 킬 스매시였다면 -> 플레이어 머리뒤쪽으로 길게 넘기는 고궤도 로브(Lob)로 대응!
                        // 2. 일반 스매시였다면 -> 네트 부근은 Drop, 딥 코트는 SafeReturn
                        ShotIntent resolvedIntent;
                        if (request.IsCounteringKillSmash)
                        {
                            resolvedIntent = ShotIntent.Lob;
                        }
                        else
                        {
                            float hitDist = System.Math.Abs(request.BallSnapshot.PositionX);
                            resolvedIntent = (hitDist <= 2.5f) ? ShotIntent.Drop : ShotIntent.SafeReturn;
                        }

                        profile = settings.Profile(resolvedIntent);
                        flightTime = profile.FlightTime;
                        apexHeight = profile.ApexHeight;
                        targetX = min + (max - min) * profile.LandingRatio;
                    }
                    else if (request.Hitter == HitterType.Player && request.Intent != ShotIntent.Lob && request.Intent != ShotIntent.Smash)
                    {
                        // 플레이어가 AI의 스매시를 받아쳐 득점 찬스를 만드는 보상 분기점 (저궤도 네트 앞 드롭샷)
                        profile = settings.Profile(ShotIntent.Drop);
                        flightTime = 0.65f; // 빠른 드롭 비행 시간
                        apexHeight = 1.1f;  // 낮게 날아가 네트(1.0f)를 겨우 넘김
                        targetX = min + (max - min) * 0.10f; // 네트 바로 앞에 툭 떨어지는 궤적
                    }
                }
                // 9번 피드백: 타점 높이에 따른 궤적 커스텀 분기 (SafeReturn 구질에만 적용하여 Deep/Drop/Lob 의도가 변질되는 것을 방지)
                else if (request.Intent == ShotIntent.SafeReturn)
                {
                    float ratio = request.HitHeightRatio;
 
                    if (isVolley && ratio >= 0.80f)
                    {
                        // 9번: 네트 앞 발리 상황에서 높은 타점의 SafeReturn은 붕 띄우지 않고, 네트 위를 낮고 가볍게 넘기는 스피디한 드라이브로 조율
                        flightTime *= 0.85f;
                        float minApexY = settings.NetHeight + 0.28f;
                        float currentMaxY = System.Math.Max(request.BallSnapshot.PositionY, settings.TargetY);
                        apexHeight = System.Math.Max(0.2f, minApexY - currentMaxY);
                        targetX = min + (max - min) * 0.65f; // 중간 깊이로 빠르게 튕겨냄
                    }
                    else if (ratio >= 0.80f) // 높은 타점 (High Contact - 고궤도로 머리 위를 넘기는 안정적인 루프 샷)
                    {
                        flightTime *= 1.25f; // 구속 감소 (체공시간 증가)
                        apexHeight += 0.8f;  // 높은 궤도로 포물선을 그림
                        targetX += facing * 0.4f; // 상대방 백코너 부근으로 안정적으로 배달
                    }
                    else if (ratio <= 0.30f) // 낮은 타점 (Low Contact - 고궤도의 매우 안정적인 고도와 안전한 구속의 샷)
                    {
                        flightTime *= 1.10f; // 구속 추가 완화 (평상시보다 10% 더 느리고 부드럽게 늦춰 체공시간 증가)
                        
                        // 네트를 완벽히 넘어가도록 정점 높이를 고궤도(1.12f + 0.85f = 1.97m)까지 높여 포물선을 크게 그리게 함
                        float minApexY = settings.NetHeight + 0.85f;
                        float currentMaxY = System.Math.Max(request.BallSnapshot.PositionY, settings.TargetY);
                        apexHeight = System.Math.Max(0.2f, minApexY - currentMaxY);
                        
                        targetX += facing * 0.8f; // 상대 코트 깊숙이 롱 샷
                    }
                }
            }
            // --------------------------------------------------------

            // 5번: 킬 스매시 적용 (3번째 연속 스매시일 때 각도를 더 깎고 극단적으로 빠른 속도 부여)
            bool isKillSmashApplied = false;
            float originalTargetX = targetX;
            float originalFlightTime = flightTime;

            if (request.Intent == ShotIntent.Smash && request.IsKillSmash)
            {
                flightTime = 0.32f; // 초고속 대포알 비행 시간 (일반 스매시 0.62초 대비 2배 가량 빠름)
                isKillSmashApplied = true;

                // 네트 바로 앞(0.24f)부터 시작하여 네트를 충돌 없이 통과하는 가장 가파른(가장 짧은) targetRatio를 검사하여 채택
                float bestRatio = 0.65f;
                for (float ratio = 0.24f; ratio <= 0.65f; ratio += 0.04f)
                {
                    float testX = min + (max - min) * ratio;
                    var testResolved = trajectory.ResolveTime(request.BallSnapshot.PositionX, request.BallSnapshot.PositionY,
                        testX, settings.TargetY, flightTime, settings);
                    if (testResolved.IsValid)
                    {
                        bestRatio = ratio;
                        break;
                    }
                }
                targetX = min + (max - min) * bestRatio;
            }

            // 8번 피드백: 랠리 진행도에 따른 AI의 난이도 동적 조율 (구속 증가 및 정확도/날카로움 증가)
            if (request.Hitter == HitterType.Opponent && rallyAiConfig != null)
            {
                // 1. 구속 증가 (비행시간 단축, 최소 비행시간 0.28초 캡 적용)
                float speedMultiplier = rallyAiConfig.GetShotSpeedMultiplier(rallyCount);
                flightTime = System.Math.Max(0.28f, flightTime / speedMultiplier);

                // 2. 정확도(날카로움) 조율: 초반(정확도 낮음)에는 코너 끝 대신 중앙 부근으로 치우치게 해 치기 쉽게 만듦
                float accuracy = rallyAiConfig.GetTargetAccuracy(rallyCount); // 0.5f ~ 0.9f
                float currentRatio = (targetX - min) / (max - min);
                
                // 타겟 낙하 지점이 코트 깊은 곳(백코너 부근)을 향해 갈 때
                if (currentRatio > 0.55f)
                {
                    currentRatio = UnityEngine.Mathf.Lerp(0.50f, currentRatio, accuracy);
                }
                // 타겟 낙하 지점이 네트 근처(짧은 드롭샷 부근)를 향해 갈 때
                else if (currentRatio < 0.35f)
                {
                    currentRatio = UnityEngine.Mathf.Lerp(0.40f, currentRatio, accuracy);
                }
                
                targetX = min + (max - min) * currentRatio;
            }

            ShotTrajectoryResult resolved = request.Intent == ShotIntent.Smash
                ? trajectory.ResolveTime(request.BallSnapshot.PositionX, request.BallSnapshot.PositionY,
                    targetX, settings.TargetY, flightTime, settings)
                : trajectory.ResolveApex(request.BallSnapshot.PositionX, request.BallSnapshot.PositionY,
                    targetX, settings.TargetY,
                    System.Math.Max(request.BallSnapshot.PositionY, settings.TargetY) + apexHeight,
                    settings);

            // 랠리 모드에서 플레이어의 낮은 타점 샷이 네트에 걸리는 경우, 안전하게 네트를 넘길 때까지 궤적 보정
            if (isRallyMode && request.Hitter == HitterType.Player && request.HitHeightRatio <= 0.30f && !resolved.IsValid)
            {
                int attempts = 0;
                while (!resolved.IsValid && attempts < 10)
                {
                    apexHeight += 0.15f;
                    resolved = trajectory.ResolveApex(request.BallSnapshot.PositionX, request.BallSnapshot.PositionY,
                        targetX, settings.TargetY,
                        System.Math.Max(request.BallSnapshot.PositionY, settings.TargetY) + apexHeight,
                        settings);
                    attempts++;
                }
            }

            // 랠리 모드에서 상대 AI의 리턴이 네트에 걸리거나 유효하지 않은 경우, 안전하게 네트를 넘길 때까지 궤적 보정
            if (isRallyMode && request.Hitter == HitterType.Opponent)
            {
                if (!resolved.IsValid)
                {
                    int attempts = 0;
                    if (request.Intent == ShotIntent.Smash)
                    {
                        while (!resolved.IsValid && attempts < 10)
                        {
                            flightTime += 0.05f;
                            resolved = trajectory.ResolveTime(request.BallSnapshot.PositionX, request.BallSnapshot.PositionY,
                                targetX, settings.TargetY, flightTime, settings);
                            attempts++;
                        }
                    }
                    else
                    {
                        while (!resolved.IsValid && attempts < 10)
                        {
                            apexHeight += 0.25f;
                            resolved = trajectory.ResolveApex(request.BallSnapshot.PositionX, request.BallSnapshot.PositionY,
                                targetX, settings.TargetY,
                                System.Math.Max(request.BallSnapshot.PositionY, settings.TargetY) + apexHeight,
                                settings);
                            attempts++;
                        }
                    }

                    // 최후의 복구 수단: 10회 루프 후에도 궤적 실패 시 플레이어 정중앙을 타겟팅하는 높은 수비용 로브(Lob)로 대체
                    if (!resolved.IsValid)
                    {
                        float safeTargetX = min + (max - min) * 0.50f;
                        float safeApexHeight = 3.0f;
                        resolved = trajectory.ResolveApex(request.BallSnapshot.PositionX, request.BallSnapshot.PositionY,
                            safeTargetX, settings.TargetY,
                            System.Math.Max(request.BallSnapshot.PositionY, settings.TargetY) + safeApexHeight,
                            settings);
                    }
                }

                // AI 타격 보증 가드: 랠리 모드에서는 어떤 상황에서도 AI의 타격이 무시(Ghosting)되지 않고 강제 통과 처리
                resolved = new ShotTrajectoryResult(
                    resolved.VelocityX,
                    resolved.VelocityY,
                    resolved.LandingX,
                    resolved.LandingY,
                    resolved.FlightTime,
                    resolved.ApexY,
                    resolved.NetCrossY,
                    true,
                    FailureReason.None);
            }

            // 만약 킬 스매시가 네트 높이를 넘지 못해 무효(아웃/네트걸림) 판정이 난 경우, 일반 스매시 궤적으로 안전하게 폴백
            if (isKillSmashApplied && !resolved.IsValid)
            {
                targetX = originalTargetX;
                flightTime = originalFlightTime;
                resolved = trajectory.ResolveTime(request.BallSnapshot.PositionX, request.BallSnapshot.PositionY,
                    targetX, settings.TargetY, flightTime, settings);
            }
            return new ShotResult(request.SwingId, request.BallStepIndex,
                resolved.VelocityX, resolved.VelocityY, resolved.IsValid, resolved.InvalidReason,
                resolved.LandingX, resolved.FlightTime, resolved.ApexY, resolved.NetCrossY);
        }
    }
}
