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

            // 랠리 히트 수가 변경되었을 때만 모터 속도를 재조정하여 성능 낭비를 방지 (가/감속 1000f 고정)
            int currentHitCount = rally != null ? rally.RallyHitCount : 0;
            if (currentHitCount != lastRallyHitCount)
            {
                lastRallyHitCount = currentHitCount;
                float baseSpeed = aiConfig.MoveSpeed * (1f + Mathf.Min(0.35f, currentHitCount * 0.03f));
                float maxCourtX = Mathf.Max(aiConfig.CourtMaxX, 8.5f);
                motor.Configure(baseSpeed, 1000f, 1000f, aiConfig.CourtMinX, maxCourtX);
            }

            long pointId=rally.GlobalPointId;

            bool isAiServeToss = ball.PlayMode == BallPlayMode.ServeToss && ball.CurrentSnapshot.PositionX > 0f;
            bool receiving = (ball.PlayMode == BallPlayMode.Rally && rally.CurrentContext.ExpectedReceiver == CourtSide.Opponent) || isAiServeToss;

            observer.Record(pointId,Time.fixedTime,ball.CurrentSnapshot,ball.PlayMode);

            // 모든 상황(서브/랠리)에서 즉각 반응(1틱 지연 = 0.02s)하도록 통일하여 고궤도 굳음 현상 완치
            float reaction = 0.02f;

            decisionTimer -= Time.fixedDeltaTime;
            bool canUpdatePlan = decisionTimer <= 0f;
            bool mustUpdate = plan == null || plan.Consumed;

            if (receiving && (canUpdatePlan || mustUpdate))
            {
                if (observer.TryGet(Time.fixedTime, reaction, out var delayed) &&
                    delayed.PointId == pointId && (delayed.PlayMode == BallPlayMode.Rally || delayed.PlayMode == BallPlayMode.ServeToss))
                {
                    decisionTimer = 0.12f;
                    UpdatePlan(delayed);
                }
            }

            float moveX = 0f; bool jump = false; bool swing = false; bool smash = false;
            float target = aiConfig.HomeX;

            if (receiving && plan != null && !plan.Consumed)
            {
                // [원바운드 수비 시 후방 선제 회군 및 대기]
                // 1바운드 수비 계획이고, 공이 AI의 머리를 넘어 등 뒤로 떨어질 예정일 때(최종 낙하지점이 AI의 현재 위치 부근이거나 그 뒤일 때),
                // 타격 여유 시간(RemainingTime > 0.4초)이 넉넉하다면 미리 낙하지점보다 약 0.7m 뒤로 물러나 대기하게 만듭니다.
                // 이로 인해 어중간하게 스쳐 지나가듯 떨어지는 하프 로브 샷도 헛스윙 없이 정면에서 안전하게 받아낼 수 있습니다.
                if (plan.BounceCountBeforeArrival == 1 && plan.RemainingTime > 0.4f && plan.InterceptPosition.x >= motor.Position.x - 0.2f)
                {
                    target = Mathf.Clamp(plan.InterceptPosition.x + 0.7f, aiConfig.HomeX, aiConfig.CourtMaxX - 0.2f);
                }
                else
                {
                    target = plan.InterceptPosition.x;

                    // 타격 성공률을 높이기 위해, AI가 사용하는 히트박스의 수평 오프셋(CenterX)을 반영하여 
                    // 공의 중심이 아닌 히트박스의 중심에 공이 정렬되도록 서는 위치(target)를 미세 조정합니다.
                    float offsetVal = 0.5f;
                    if (UsesManualHitboxes)
                    {
                        OpponentManualHitboxTrigger activeTrigger = (plan.SwingKind == SwingKind.Smash)
                            ? manualHitboxes.SmashHitbox
                            : manualHitboxes.NormalHitbox;
                        if (activeTrigger != null && activeTrigger.Box != null)
                        {
                            offsetVal = Mathf.Abs(activeTrigger.Box.offset.x);
                        }
                    }
                    else
                    {
                        HitZoneDefinition activeZone = plan.SwingKind == SwingKind.Smash 
                            ? catConfig.CreateSmashHitZone() 
                            : catConfig.CreateNormalHitZone();
                        offsetVal = activeZone.CenterX;
                    }

                    // AI는 항상 왼쪽(-1방향)을 바라보므로, 월드 기준 공보다 오른쪽(값 증가)에 서야 히트박스가 공에 맞닿습니다.
                    target += offsetVal;

                    // [전방 오버슛(지나침) 방지용 강력 가드]
                    // 네트 앞으로 떨어지는 짧은 공(Short Drop)을 받기 위해 앞으로 돌진할 때, 
                    // 속도 관성으로 인해 낙하지점보다 지나치게 네트 쪽(왼쪽)으로 미끄러져 들어가 뒤통수로 공을 흘리는 현상을 막습니다.
                    // 목표 위치(target)가 공의 예측 낙하지점 X보다 네트 방향(왼쪽)으로 침범하는 것을 원천 금지합니다.
                    if (plan.BounceCountBeforeArrival == 1)
                    {
                        target = Mathf.Max(target, plan.InterceptPosition.x + (offsetVal * 0.8f));
                    }
                }
            }

            if (Mathf.Abs(target - motor.Position.x) > 0.08f)
            {
                moveX = Mathf.Sign(target - motor.Position.x);
            }
            if(plan!=null && !plan.Consumed)
            {
                plan.RemainingTime-=Time.fixedDeltaTime;

                // [플랜 만료 시간 가드]
                // 계획된 볼 도달 시간보다 0.15초(7.5틱) 이상 지나갔는데도 타격에 실패했다면, 
                // 해당 플랜은 완전히 실패한(Whiff/Outdated) 것으로 간주하여 즉시 폐기하고 신규 수비 플랜을 수립하도록 허용합니다.
                if (plan.RemainingTime < -0.15f)
                {
                    plan.Consumed = true;
                    plan = null;
                }
            }

            if(plan!=null && !plan.Consumed)
            {
                bool grounded=IsGrounded();
                
                // 점프 리드타임 동적 보정: AI의 점프 속도(7m/s)와 중력(3배 scale) 하에서 
                // 최고 도달점(Peak)인 약 0.23s 부근에서 타격이 정확히 맞아떨어지도록 리드타임 보정 (기존 0.35s는 너무 일찍 뛰어 먼저 내려옴)
                float optimalJumpLead = UsesManualHitboxes ? 0.23f : aiConfig.JumpLeadTime;
                jump = plan.JumpRequired && grounded && plan.RemainingTime <= optimalJumpLead && plan.PlanId != lastTriggeredJumpPlanId;
                if(jump) lastTriggeredJumpPlanId=plan.PlanId;

                // [하이브리드 & 실시간 오버랩 스윙 판정]
                // 1. 공이 AI의 실제 타격 범위(HitZone) 내로 진입했는지 실시간 체크
                bool isBallOverlapping = false;
                if (UsesManualHitboxes)
                {
                    OpponentManualHitboxTrigger activeTrigger = (plan.SwingKind == SwingKind.Smash)
                        ? manualHitboxes.SmashHitbox
                        : manualHitboxes.NormalHitbox;

                    if (activeTrigger != null && activeTrigger.Box != null)
                    {
                        Bounds bounds = activeTrigger.Box.bounds;
                        // 스윙 선동작(Startup)을 감안하여 실제 콜라이더 바운즈에 20cm 마진을 확장하여 오버랩 체크
                        bounds.Expand(0.2f);
                        isBallOverlapping = bounds.Contains(new Vector3(ball.CurrentSnapshot.PositionX, ball.CurrentSnapshot.PositionY, bounds.center.z));
                    }
                }
                else
                {
                    HitZoneDefinition activeZone = plan.SwingKind == SwingKind.Smash 
                        ? catConfig.CreateSmashHitZone() 
                        : catConfig.CreateNormalHitZone();
                    
                    HitZoneDefinition triggerZone = new HitZoneDefinition(
                        activeZone.CenterX,
                        activeZone.CenterY,
                        activeZone.HalfWidth * 1.40f,
                        activeZone.HalfHeight * 1.40f,
                        activeZone.RequireForward
                    );

                    isBallOverlapping = hitZoneModel.Contains(
                        triggerZone,
                        motor.Position.x,
                        motor.Position.y,
                        -1,
                        ball.CurrentSnapshot.PositionX,
                        ball.CurrentSnapshot.PositionY
                    );
                }

                // 2. 시간/거리 하이브리드 트리거 조건 만족 여부
                // 예측 오차로 허공에 스윙하는 방지용 거리 가드 (X축 2.0f 이내, Y축 2.2f 이내)
                float relX = Mathf.Abs(ball.CurrentSnapshot.PositionX - motor.Position.x);
                float relY = Mathf.Abs(ball.CurrentSnapshot.PositionY - motor.Position.y);
                bool isCloseEnough = relX <= 2.0f && relY <= 2.2f;

                bool timeTrigger = plan.RemainingTime <= aiConfig.SwingLeadTime && isCloseEnough;
                bool instantTrigger = isBallOverlapping; // 오버랩 시 즉각 격발

                if ((timeTrigger || instantTrigger) && plan.PlanId != lastTriggeredSwingPlanId)
                {
                    smash = plan.SwingKind == SwingKind.Smash;
                    swing = !smash;
                    lastTriggeredSwingPlanId = plan.PlanId;
                }
            }
            if (InputLocked)
            {
                moveX = 0f;
                jump = false;
                swing = false;
                smash = false;
            }
            bool onGround=IsGrounded();
            CurrentAction=actionMachine.Step(new PlayerInputFrame(moveX,jump,swing,smash,0f,++inputTick),onGround);
            if (manualHitboxes != null)
            {
                manualHitboxes.ApplyAction(CurrentAction, -1);
            }
            
            motor.Apply(CurrentAction.MoveX,CurrentAction.JumpRequested,aiConfig.JumpSpeed,Time.fixedDeltaTime);
            
            hitDetector.Evaluate(pointId, CurrentAction, motor.Position, -1, plan);

            if (plan != null && plan.Consumed && plan.PlanId != lastRecordedPlanId)
            {
                lastRecordedPlanId = plan.PlanId;
                RecordShotIntent(plan.Intent);
            }
        }

        private void UpdatePlan(DelayedBallObservation observation)
        {
            bool forceBounce = false;
            if (plan != null && !plan.Consumed && plan.PointId == observation.PointId)
            {
                // [1바운드 수비 굳건화 잠금]
                // 1바운드 수비 도중 발리 수비로 번복하여 갈팡질팡(지터링)하는 오작동은 전면 차단하되,
                // 실시간 낙하지점의 미세 수정(갱신)은 1바운드 궤적 내에서 지속적으로 허용하여 타격 정확도를 올립니다.
                if (plan.BounceCountBeforeArrival == 1)
                {
                    forceBounce = true;
                }

                bool airborne = !IsGrounded();
                bool swingActive = CurrentAction.SwingState != SwingState.Ready;
                if (airborne || swingActive)
                {
                    return;
                }
            }
            float maxCourtX = Mathf.Max(aiConfig.CourtMaxX, 8.5f);
            var candidates=predictor.Predict(observation.Snapshot,physicsConfig.CreateSettings(),
                courtConfig.GroundY,aiConfig.PredictionStep,aiConfig.PredictionHorizon,
                0.15f,maxCourtX,aiConfig.JumpHeightThreshold);
            float age=Time.fixedTime-observation.Time;
            
            float baseSpeed = aiConfig.MoveSpeed;
            if (rally != null)
            {
                baseSpeed *= (1f + Mathf.Min(0.35f, rally.RallyHitCount * 0.03f));
            }

            bool isServe = ball.PlayMode == BallPlayMode.ServeToss;
            if(!planner.TrySelect(candidates,motor.Position.x,baseSpeed,age,
                    aiConfig.SwingLeadTime,aiConfig.JumpLeadTime,isServe,forceBounce,out var candidate)) return;
            long id=++nextPlanId;

            AiTacticalContext ctx = new AiTacticalContext();
            ctx.playerPosition = player != null ? player.Position : Vector2.zero;
            ctx.opponentPosition = motor.Position;
            ctx.ballPosition = new Vector2(ball.CurrentSnapshot.PositionX, ball.CurrentSnapshot.PositionY);
            ctx.predictedBallArrival = candidate.Position;
            ctx.playerNearNet = player != null && player.Position.x > -2.5f;
            ctx.playerDeepCourt = player != null && player.Position.x < -5.5f;
            ctx.playerLeftSide = player != null && player.Position.x < -4.25f;
            ctx.playerRightSide = player != null && player.Position.x >= -4.25f;
            ctx.playerRecentlyJumped = player != null && player.CurrentAction.LocomotionState == LocomotionState.Airborne;
            ctx.rallyCount = rally != null ? rally.RallyHitCount : 0;
            ctx.playerOutOfPosition = player != null && Mathf.Abs(player.Position.x - candidate.Position.x) > 2.5f;
            ctx.ballArrivalRequiresJump = candidate.RequiresJump;
            ctx.opponentNearNet = motor.Position.x < 2.5f;

            ShotIntent intent = DetermineShotIntent(ctx);

            if (ball.PlayMode == BallPlayMode.ServeToss)
            {
                intent = ShotIntent.Serve;
            }

            // [물리적 요건과 스윙 종류의 강제 일치화]
            // 점프가 요구되는 상황(RequiresJump == true)이라면, 
            // 전술적 샷 종류(intent)에 상관없이 무조건 공중용 스매시 히트박스(SwingKind.Smash)를 활성화하여 헛스윙을 차단합니다.
            bool requiresJump = candidate.RequiresJump;
            if (intent == ShotIntent.Serve || ball.PlayMode == BallPlayMode.ServeToss)
            {
                requiresJump = false;
            }

            SwingKind kind = requiresJump ? SwingKind.Smash : SwingKind.Normal;

            plan=new AISwingPlan(observation.PointId,id,observation.ObservationId,candidate.StepIndex,
                kind,intent,Mathf.Max(0f,candidate.ArrivalTime-age),candidate.Position,requiresJump,
                candidate.BounceCountBeforeArrival);
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
