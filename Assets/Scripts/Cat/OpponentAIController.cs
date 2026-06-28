using System;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    [RequireComponent(typeof(Rigidbody2D),typeof(CapsuleCollider2D),typeof(CatMotor))]
    public sealed class OpponentAIController : MonoBehaviour
    {
        private readonly DelayedBallObserver observer=new DelayedBallObserver();
        private readonly BallArrivalPredictor predictor=new BallArrivalPredictor();
        private readonly AIInterceptPlanner planner=new AIInterceptPlanner();
        private readonly PlayerHitZoneModel hitZoneModel = new PlayerHitZoneModel();
        private BallController ball; private BallPhysicsConfig physicsConfig;
        private CourtGeometryConfig courtConfig; private PlayerControlConfig catConfig;
        private AIBalanceConfig aiConfig; private RallyFlowManager rally;
        private OpponentHitDetector hitDetector; private CatMotor motor;
        private PlayerActionStateMachine actionMachine; private AISwingPlan plan;
        private long nextPlanId; private long inputTick; private bool initialized;
        private Vector2 resetPosition;
        private OpponentManualHitboxController manualHitboxes;

        private PlayerCatController player;
        private RallyAiBalanceConfig rallyAiConfig;
        private float decisionTimer;
        private ShotIntent lastAiShotIntent = ShotIntent.Undefined;
        private ShotIntent secondLastAiShotIntent = ShotIntent.Undefined;
        private int consecutivePressureShotsCount;
        private long lastRecordedPlanId = -1;
        private long lastTriggeredSwingPlanId = -1;
        private long lastTriggeredJumpPlanId = -1;
        private int lastRallyHitCount = -1;

        // [자료구조/복잡도 다이어트] 궤적 캐싱용 전용 버퍼 및 상태 플래그
        private System.Collections.Generic.List<BallArrivalCandidate> cachedCandidates = new System.Collections.Generic.List<BallArrivalCandidate>();
        private int lastPredictedRallyCount = -1;

        public PlayerActionFrame CurrentAction { get; private set; }
        public AISwingPlan CurrentPlan=>plan;
        public bool InputLocked { get; set; }
        private bool UsesManualHitboxes => manualHitboxes != null && manualHitboxes.HasGameplayHitboxes;

        public void Configure(BallController ballController,BallPhysicsConfig physics,
            CourtGeometryConfig court,PlayerControlConfig cat,AIBalanceConfig ai,
            RallyFlowManager rallyManager,OpponentHitDetector detector,
            PlayerCatController playerController = null, RallyAiBalanceConfig rallyBalance = null)
        {
            ball=ballController;physicsConfig=physics;courtConfig=court;catConfig=cat;
            aiConfig=ai;rally=rallyManager;hitDetector=detector;
            player=playerController; rallyAiConfig=rallyBalance;
            ai.ValidateOrThrow(); motor=GetComponent<CatMotor>();
            motor.Configure(ai.MoveSpeed,ai.Acceleration,ai.Deceleration,ai.CourtMinX,ai.CourtMaxX);
            
            // AI의 물리 엔진 중력 배율을 플레이어와 완전히 동기화 (3.0f)
            GetComponent<Rigidbody2D>().gravityScale = cat.GravityScale;

            actionMachine=new PlayerActionStateMachine(cat.CreateActionSettings()); initialized=true;
            manualHitboxes=GetComponent<OpponentManualHitboxController>();
            resetPosition=transform.position; ResetOpponent(resetPosition);
        }

        private void FixedUpdate()
        {
            if(!initialized || actionMachine == null) return;

            if (rally != null && !rally.HasActivePoint)
            {
                plan = null;
                manualHitboxes?.DisableAll();
                actionMachine.Step(new PlayerInputFrame(0f, false, false, false, 0f, ++inputTick), IsGrounded());
                motor.Apply(0f, false, 0f, Time.fixedDeltaTime);
                return;
            }

            int currentHitCount = rally != null ? rally.RallyHitCount : 0;
            long pointId = rally.GlobalPointId;

            // AI 서브 상황인지 체크
            bool isAiServeToss = ball.PlayMode == BallPlayMode.ServeToss && ball.CurrentSnapshot.PositionX > 0f;
            bool receiving = (ball.PlayMode == BallPlayMode.Rally && rally.CurrentContext.ExpectedReceiver == CourtSide.Opponent) || isAiServeToss;

            // ----------------------------------------------------
            // [자료구조/복잡도 다이어트] 궤적 캐싱 연동
            // ----------------------------------------------------
            // 랠리 히트수가 변경되었을 때(공이 타격되어 새로 날아오기 시작한 순간) 
            // 딱 1회만 예측 시뮬레이션을 돌려 궤적 전체를 캐싱합니다. (매 프레임 125회 루프 낭비 차단)
            if (currentHitCount != lastPredictedRallyCount)
            {
                lastPredictedRallyCount = currentHitCount;
                float maxCourtX = Mathf.Max(aiConfig.CourtMaxX, 8.5f);
                var freshCandidates = predictor.Predict(ball.CurrentSnapshot, physicsConfig.CreateSettings(),
                    courtConfig.GroundY, aiConfig.PredictionStep, aiConfig.PredictionHorizon,
                    0.15f, maxCourtX, aiConfig.JumpHeightThreshold);

                cachedCandidates.Clear();
                if (freshCandidates != null)
                {
                    for (int i = 0; i < freshCandidates.Count; i++)
                    {
                        cachedCandidates.Add(freshCandidates[i]);
                    }
                }
                
                // 새로운 궤적이 들어왔으므로 기존 수비 플랜은 즉시 폐기하여 갱신을 유도합니다.
                plan = null;
            }

            // 수비 상황이고 현재 세워진 계획이 없거나 만료되었다면, 캐싱된 버퍼를 기반으로 굳건한 수비 계획 수립
            if (receiving && (plan == null || plan.Consumed))
            {
                UpdatePlan(currentHitCount);
            }

            // ----------------------------------------------------
            // 1층: 물리 락 (Action Lock) 레이어
            // ----------------------------------------------------
            // 스윙 중이거나 점프 공중 상태인 경우, AI의 새로운 조작 판단을 원천 중단하여 
            // 꼬임 없이 물리 액션이 끝까지 완수되도록 락(Lock)을 겁니다.
            bool airborne = !IsGrounded();
            bool swingActive = CurrentAction != null && CurrentAction.SwingState != SwingState.Ready;
            if (airborne || swingActive)
            {
                // 점프/스윙 락 상태에서는 motor.Apply에 기존 결정된 명령만 연속 연동되도록 early return합니다.
                bool onGround = IsGrounded();
                CurrentAction = actionMachine.Step(new PlayerInputFrame(0f, false, false, false, 0f, ++inputTick), onGround);
                if (manualHitboxes != null)
                {
                    manualHitboxes.ApplyAction(CurrentAction, -1);
                }
                motor.Apply(CurrentAction.MoveX, CurrentAction.JumpRequested, aiConfig.JumpSpeed, Time.fixedDeltaTime);
                hitDetector.Evaluate(pointId, CurrentAction, motor.Position, -1, plan);
                return;
            }

            // ----------------------------------------------------
            // 2층: 수평 이동 (Movement) 레이어
            // ----------------------------------------------------
            // 이동 목표(target)는 공격용 점프/스윙 샷 결정 여부와 완전히 무관하게 작동합니다.
            float target = aiConfig.HomeX;

            if (receiving)
            {
                if (plan != null && !plan.Consumed)
                {
                    float targetX = plan.InterceptPosition.x;

                    // [수비 4대 시나리오 하드코딩 물리 가드 적용]
                    // 1. 머리 위 뒤로 슥 넘어가는 깊은 롭 (딥 롭 / S2-3, S2-5)
                    // 최고 높이 Y >= 2.5f 이상이거나 낙하지점이 5.8f 이상인 딥 볼은 
                    // 공중에서 가로채려 하지 않고 즉시 백코트 최후방 6.8f로 전속력 후퇴합니다.
                    bool isDeepLob = false;
                    for (int i = 0; i < cachedCandidates.Count; i++)
                    {
                        // 공이 AI 코트(우측 진영, X >= 0f)를 지나가는 궤적 중에서만 롭 여부를 판정하여 유저 진역의 드롭 샷 오판을 차단합니다.
                        if (cachedCandidates[i].Position.x >= 0f && 
                            (cachedCandidates[i].Position.y >= 2.5f || cachedCandidates[i].Position.x >= 5.8f))
                        {
                            isDeepLob = true;
                            break;
                        }
                    }

                    if (isDeepLob)
                    {
                        target = 6.8f;
                    }
                    else
                    {
                        target = targetX;

                        // 타격 성공률 제고용 히트박스 절대 offsetVal(약 0.85m) 마진 반영
                        float offsetVal = 0.85f;
                        target += offsetVal;

                        // 2. 네트 앞 짧은 공 오버런 가드 (숏 드롭 / S2-1, S2-7)
                        // 네트 앞 2.3m 안전 제동 한계선을 강제 고정하여 과도한 전방 돌진 헛방을 방지합니다.
                        target = Mathf.Max(target, 2.3f);
                    }
                }
                else
                {
                    // [수비 중 플랜 부재 시 필사적 폴백 추적]
                    // 계획 수립이 일시적으로 지연되더라도 홈으로 복귀하지 않고 끝까지 공을 쫓아갑니다.
                    target = Mathf.Max(ball.CurrentSnapshot.PositionX + 0.85f, 2.3f);
                }
            }

            // 이동 축 가중치 결정 및 모터 조작
            float distance = target - motor.Position.x;
            float moveX = 0f;
            if (Mathf.Abs(distance) > 0.05f)
            {
                moveX = Mathf.Sign(distance);
            }

            // ----------------------------------------------------
            // 3층: 격발 트리거 (Trigger) 레이어
            // ----------------------------------------------------
            // 이동 중에는 샷 결정을 전혀 하지 않고 일관되게 달리며,
            // 공이 타격 사정거리(1.1m) 내로 임팩트한 정확한 1프레임에만 의사결정을 격발합니다.
            bool jump = false; bool swing = false; bool smash = false;

            if (receiving && plan != null && !plan.Consumed)
            {
                // 실시간 공과의 잔여 타이밍 감쇠
                plan.RemainingTime = Mathf.Max(-0.2f, plan.RemainingTime - Time.fixedDeltaTime);

                // 플랜 타격 지연 초과 가드
                if (plan.RemainingTime < -0.15f)
                {
                    plan.Consumed = true;
                    plan = null;
                }
                else
                {
                    float relX = Mathf.Abs(ball.CurrentSnapshot.PositionX - motor.Position.x);
                    float relY = Mathf.Abs(ball.CurrentSnapshot.PositionY - motor.Position.y);
                    bool isCloseEnough = relX <= 1.1f && relY <= 2.2f;

                    // 남은 시간 및 근접성 트리거 연동
                    bool timeTrigger = plan.RemainingTime <= aiConfig.SwingLeadTime && isCloseEnough;

                    if (timeTrigger && plan.PlanId != lastTriggeredSwingPlanId)
                    {
                        lastTriggeredSwingPlanId = plan.PlanId;

                        // [타격 시점 1회 전술 의사결정 수행]
                        AiTacticalContext ctx = new AiTacticalContext();
                        ctx.playerPosition = player != null ? player.Position : Vector2.zero;
                        ctx.opponentPosition = motor.Position;
                        ctx.ballPosition = new Vector2(ball.CurrentSnapshot.PositionX, ball.CurrentSnapshot.PositionY);
                        ctx.predictedBallArrival = plan.InterceptPosition;
                        ctx.playerNearNet = player != null && player.Position.x > -2.5f;
                        ctx.playerDeepCourt = player != null && player.Position.x < -5.5f;
                        ctx.playerLeftSide = player != null && player.Position.x < -4.25f;
                        ctx.playerRightSide = player != null && player.Position.x >= -4.25f;
                        ctx.playerRecentlyJumped = player != null && player.CurrentAction.LocomotionState == LocomotionState.Airborne;
                        ctx.rallyCount = currentHitCount;
                        ctx.playerOutOfPosition = player != null && Mathf.Abs(player.Position.x - plan.InterceptPosition.x) > 2.5f;
                        ctx.ballArrivalRequiresJump = plan.JumpRequired || ball.CurrentSnapshot.PositionY >= 2.1f;
                        ctx.opponentNearNet = motor.Position.x < 2.5f;

                        ShotIntent intent = DetermineShotIntent(ctx);
                        if (ball.PlayMode == BallPlayMode.ServeToss || isAiServeToss)
                        {
                            intent = ShotIntent.Serve;
                        }

                        bool requiresJump = ctx.ballArrivalRequiresJump;
                        if (intent == ShotIntent.Serve)
                        {
                            requiresJump = false;
                        }

                        if (requiresJump && intent.Swing == SwingKind.Smash)
                        {
                            jump = true;
                            smash = true;
                            lastTriggeredJumpPlanId = plan.PlanId;
                        }
                        else
                        {
                            swing = true;
                        }

                        // 플랜 소모 완료 처리 및 의도 주입
                        plan.Intent = intent;
                        plan.MarkConsumed();

                        // 물리 격발 동작 인가
                        if (jump) motor.Jump();
                        
                        if (smash)
                        {
                            CurrentAction = PlayerActionFrame.CreateSmash(pointId);
                        }
                        else if (swing)
                        {
                            CurrentAction = PlayerActionFrame.CreateReturn(pointId, intent.Shot);
                        }
                    }
                }
            }

            if (InputLocked)
            {
                moveX = 0f;
                jump = false;
                swing = false;
                smash = false;
            }

            // 락이 걸리지 않은 평범한 프레임인 경우, 모터 조작 값 업데이트
            if (CurrentAction == null || CurrentAction.SwingState == SwingState.Ready)
            {
                bool onGround = IsGrounded();
                CurrentAction = actionMachine.Step(new PlayerInputFrame(moveX, jump, swing, smash, 0f, ++inputTick), onGround);
            }

            if (manualHitboxes != null)
            {
                manualHitboxes.ApplyAction(CurrentAction, -1);
            }

            motor.Apply(CurrentAction.MoveX, CurrentAction.JumpRequested, aiConfig.JumpSpeed, Time.fixedDeltaTime);
            hitDetector.Evaluate(pointId, CurrentAction, motor.Position, -1, plan);

            if (plan != null && plan.Consumed && plan.PlanId != lastRecordedPlanId)
            {
                lastRecordedPlanId = plan.PlanId;
                RecordShotIntent(plan.Intent);
            }
        }

        private void UpdatePlan(int currentHitCount)
        {
            float baseSpeed = aiConfig.MoveSpeed;
            bool isServe = ball.PlayMode == BallPlayMode.ServeToss;

            if (!planner.TrySelect(cachedCandidates, motor.Position.x, baseSpeed, 0f,
                    aiConfig.SwingLeadTime, aiConfig.JumpLeadTime, isServe, false, out var candidate))
            {
                return;
            }

            long id = ++nextPlanId;
            bool requiresJump = candidate.RequiresJump;
            SwingKind kind = requiresJump ? SwingKind.Smash : SwingKind.Normal;

            plan = new AISwingPlan(id, candidate, kind, requiresJump, rally.GlobalPointId);
        }

        private ShotIntent DetermineShotIntent(AiTacticalContext ctx)
        {
            // 랠리 극초반(3회 이하)에는 무조건 랠리 유지를 위해 안전하게 리턴
            if (ctx.rallyCount < 3)
            {
                return ShotIntent.SafeReturn;
            }

            // 랠리 진행도에 따른 무자비도(Ruthlessness) 계산 (0.0 ~ 1.0)
            // 랠리가 20회 이상 지속될수록 자비 없는 찌르기 빈도를 극대화합니다.
            float ruthlessness = Mathf.Clamp01(ctx.rallyCount / 20f);

            // [공중 찬스 볼 상황] 
            // 점프가 필요한 높은 체공 볼인 경우, 무자비도에 따라 스매시(Spike) 격발 확률 적용 (최대 85%)
            if (ctx.ballArrivalRequiresJump)
            {
                float smashChance = Mathf.Lerp(0.05f, 0.85f, ruthlessness);
                if (UnityEngine.Random.value < smashChance)
                {
                    return ShotIntent.Smash;
                }
            }

            // [상황별 기본 전술 가중치 테이블 구성]
            float safeWeight = 0f;
            float deepWeight = 0f;
            float dropWeight = 0f;
            float lobWeight = 0f;

            // 1. 유저가 네트 근처에 있음 -> 패싱 샷 유도 (머리 뒤 아웃라인 깊숙이 넘기기)
            if (ctx.playerNearNet)
            {
                deepWeight = Mathf.Lerp(0.20f, 0.70f, ruthlessness);
                lobWeight = Mathf.Lerp(0.10f, 0.15f, ruthlessness);
                safeWeight = 1.0f - deepWeight - lobWeight;
            }
            // 2. 유저가 백코트 깊숙이 있음 -> 드롭 샷 유도 (네트 앞에 툭 떨어뜨리기)
            else if (ctx.playerDeepCourt)
            {
                dropWeight = Mathf.Lerp(0.10f, 0.65f, ruthlessness);
                deepWeight = Mathf.Lerp(0.40f, 0.15f, ruthlessness);
                safeWeight = 1.0f - dropWeight - deepWeight;
            }
            // 3. AI 본인이 수비하러 네트 부근에 있음 -> 길게 찔러서 역습
            else if (ctx.opponentNearNet)
            {
                deepWeight = Mathf.Lerp(0.35f, 0.60f, ruthlessness);
                lobWeight = Mathf.Lerp(0.15f, 0.15f, ruthlessness);
                safeWeight = 1.0f - deepWeight - lobWeight;
            }
            // 4. 유저가 좌우/전후 위기 상황임 -> 빈 공간 깊거나 짧게 찌르기
            else if (ctx.playerOutOfPosition)
            {
                deepWeight = Mathf.Lerp(0.30f, 0.50f, ruthlessness);
                dropWeight = Mathf.Lerp(0.20f, 0.40f, ruthlessness);
                safeWeight = 1.0f - deepWeight - dropWeight;
            }
            // 5. 일반 랠리 상황 -> Config 에 정의된 기본 밸런스 가중치 적용
            else
            {
                if (rallyAiConfig != null)
                {
                    safeWeight = rallyAiConfig.GetSafeChance(ctx.rallyCount);
                    deepWeight = rallyAiConfig.GetDeepChance(ctx.rallyCount);
                    dropWeight = rallyAiConfig.GetDropChance(ctx.rallyCount);
                    lobWeight = rallyAiConfig.GetLobChance(ctx.rallyCount);
                }
                else
                {
                    safeWeight = aiConfig.SafeWeight;
                    deepWeight = aiConfig.DeepWeight;
                    dropWeight = aiConfig.DropWeight;
                    lobWeight = aiConfig.LobWeight;
                }
            }

            // [상황 6: 플레이어 위기 시 자비(Mercy) 샷 보정]
            // 유저가 점프 후 착지 중이거나 위기 상황일 때 가벼운 로브 등으로 살려줄 자비 확률 (무자비도에 따라 0%로 감쇄)
            if (ctx.playerRecentlyJumped || ctx.playerOutOfPosition)
            {
                float mercyChance = Mathf.Lerp(0.50f, 0.00f, ruthlessness);
                if (UnityEngine.Random.value < mercyChance)
                {
                    // 자비 발동 시 가중치를 로브 및 안전 샷으로 강제 전환
                    deepWeight = 0f;
                    dropWeight = 0f;
                    safeWeight = 0.4f;
                    lobWeight = 0.6f;
                }
            }

            // [상황 7: 연속적인 동일/압박 전술 반복 금지]
            // 드롭이나 로브는 연속해서 두 번 사용하지 않게 방지하여 단조로움을 피합니다.
            if (lastAiShotIntent == ShotIntent.Drop) dropWeight = 0f;
            if (lastAiShotIntent == ShotIntent.Lob) lobWeight = 0f;

            // 극단적인 공격 샷(Deep, Drop, Lob)이 2회 연속 남발되는 것을 감쇄
            if (consecutivePressureShotsCount >= 2)
            {
                deepWeight = 0f;
                dropWeight = 0f;
                lobWeight = 0f;
            }

            return WeightedPick(
                (ShotIntent.SafeReturn, safeWeight),
                (ShotIntent.Deep, deepWeight),
                (ShotIntent.Drop, dropWeight),
                (ShotIntent.Lob, lobWeight)
            );
        }

        private ShotIntent WeightedPick(params (ShotIntent intent, float weight)[] options)
        {
            float total = 0f;
            for (int i = 0; i < options.Length; i++)
            {
                total += options[i].weight;
            }

            if (total <= 0f) return ShotIntent.SafeReturn;

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < options.Length; i++)
            {
                roll -= options[i].weight;
                if (roll <= 0f)
                {
                    return options[i].intent;
                }
            }

            return ShotIntent.SafeReturn;
        }

        private void RecordShotIntent(ShotIntent intent)
        {
            secondLastAiShotIntent = lastAiShotIntent;
            lastAiShotIntent = intent;

            bool isPressure = intent == ShotIntent.Deep || intent == ShotIntent.Drop || intent == ShotIntent.Lob;
            if (isPressure)
            {
                consecutivePressureShotsCount++;
            }
            else
            {
                consecutivePressureShotsCount = 0;
            }
        }

        private bool IsGrounded()=>Physics2D.OverlapCircle(
            motor.Position+catConfig.GroundCheckOffset,catConfig.GroundCheckRadius,catConfig.GroundMask)!=null;

        public void ResetOpponent(Vector2 position)
        {
            resetPosition=position; observer.Reset();plan=null;nextPlanId=0;inputTick=0;
            actionMachine?.Reset();motor?.ResetMotor(position);hitDetector?.ResetDetector();
            CurrentAction=new PlayerActionFrame(LocomotionState.Grounded,SwingState.Ready,
                SwingKind.None,0,false,0f);
            manualHitboxes?.DisableAll();
            lastAiShotIntent = ShotIntent.Undefined;
            secondLastAiShotIntent = ShotIntent.Undefined;
            consecutivePressureShotsCount = 0;
            lastRecordedPlanId = -1;
            lastTriggeredSwingPlanId = -1;
            lastTriggeredJumpPlanId = -1;
            lastRallyHitCount = -1;
            decisionTimer = 0f;
        }
    }
}
