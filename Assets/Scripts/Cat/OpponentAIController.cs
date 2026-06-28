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
                // [딥 샷 원바운드 수비 시 아웃라인 선제 회군 및 대기]
                // 1바운드 수비 계획이고 최종 낙하지점이 깊은 아웃라인 구석(X >= 5.8f)인 경우,
                // 타격 여유 시간(RemainingTime > 0.4초)이 충분할 때 미리 아웃라인 근방(7.3f~7.7f)으로 전속력으로 복귀하여 대기하게 만듭니다.
                if (plan.BounceCountBeforeArrival == 1 && plan.InterceptPosition.x >= 5.8f && plan.RemainingTime > 0.4f)
                {
                    target = Mathf.Clamp(aiConfig.CourtMaxX - 0.2f, 7.3f, 7.7f);
                }
                else
                {
                    target = plan.InterceptPosition.x;

                    // 타격 성공률을 높이기 위해, AI가 사용하는 히트박스의 수평 오프셋(CenterX)을 반영하여 
                    // 공의 중심이 아닌 히트박스의 중심에 공이 정렬되도록 서는 위치(target)를 미세 조정합니다.
                    if (UsesManualHitboxes)
                    {
                        OpponentManualHitboxTrigger activeTrigger = (plan.SwingKind == SwingKind.Smash)
                            ? manualHitboxes.SmashHitbox
                            : manualHitboxes.NormalHitbox;
                        if (activeTrigger != null && activeTrigger.Box != null)
                        {
                            // AI는 항상 왼쪽(-1방향)을 바라보므로, 월드 기준 공보다 오른쪽(값 증가)에 서야 히트박스가 공에 맞닿습니다.
                            // 따라서 수동 콜라이더 offset.x의 절댓값을 추가하여 정렬을 정밀하게 제어합니다.
                            target += Mathf.Abs(activeTrigger.Box.offset.x);
                        }
                    }
                    else
                    {
                        HitZoneDefinition activeZone = plan.SwingKind == SwingKind.Smash 
                            ? catConfig.CreateSmashHitZone() 
                            : catConfig.CreateNormalHitZone();
                        // AI는 왼쪽(-1방향)을 바라보므로 CenterX의 부호를 반전하여 오프셋 적용
                        target += activeZone.CenterX;
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
            if (plan != null && !plan.Consumed && plan.PointId == observation.PointId)
            {
                // 만약 현재 계획이 백코트 회군(BounceCountBeforeArrival == 1) 계획이라면, 
                // 도중에 마음을 바꾸어 공중 차단(Volley)으로 전환하는 것을 전면 금지합니다.
                // 이는 달리다가 중간에 갑자기 점프 헛방(Flail)을 치는 현상을 방지합니다.
                if (plan.BounceCountBeforeArrival == 1)
                {
                    return;
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
                    aiConfig.SwingLeadTime,aiConfig.JumpLeadTime,isServe,out var candidate)) return;
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

            SwingKind kind=intent==ShotIntent.Smash?SwingKind.Smash:SwingKind.Normal;
            bool requiresJump = candidate.RequiresJump;
            if (intent == ShotIntent.Serve || ball.PlayMode == BallPlayMode.ServeToss)
            {
                requiresJump = false;
            }
            plan=new AISwingPlan(observation.PointId,id,observation.ObservationId,candidate.StepIndex,
                kind,intent,Mathf.Max(0f,candidate.ArrivalTime-age),candidate.Position,requiresJump,
                candidate.BounceCountBeforeArrival);
        }

        private ShotIntent DetermineShotIntent(AiTacticalContext ctx)
        {
            // 랠리 진행도에 따른 무자비도(Ruthlessness) 계산 (0.0 ~ 1.0)
            // 랠리 초반(0~5회)에는 플레이어가 랠리를 이어갈 수 있도록 무난한 안전 리턴 위주로 치며, 
            // 20회 이상 진행 시 100% 무자비 압박 모드로 돌입합니다.
            float ruthlessness = Mathf.Clamp01(ctx.rallyCount / 20f);

            // [상황 3: 공중 찬스 볼] 고궤도 체공 공 발생 시 스매시(Spike) 결정 (무자비도에 따라 스매시 격발 확률 85%까지 보간)
            if (ctx.ballArrivalRequiresJump)
            {
                float smashChance = Mathf.Lerp(0.05f, 0.85f, ruthlessness);
                if (UnityEngine.Random.value < smashChance)
                {
                    return ShotIntent.Smash;
                }
            }

            // 플레이어 실점 상태 위기 시의 자비 샷 발동 확률도 무자비도에 따라 0%로 감쇄
            if (ctx.playerRecentlyJumped || ctx.playerOutOfPosition)
            {
                float mercyChance = Mathf.Lerp(0.50f, 0.00f, ruthlessness);
                if (UnityEngine.Random.value < mercyChance)
                {
                    return UnityEngine.Random.value < 0.6f ? ShotIntent.Lob : ShotIntent.SafeReturn;
                }
            }

            float safeWeight = 0f;
            float deepWeight = 0f;
            float dropWeight = 0f;
            float lobWeight = 0f;

            // 랠리 극초반(3회 이하)에는 랠리 정착을 위해 강제 안전 복구 샷
            if (ctx.rallyCount < 3)
            {
                return ShotIntent.SafeReturn;
            }

            // [상황 2: 플레이어가 네트 앞 전진 시] 머리 뒤 아웃라인 근처로 길게 넘겨서 패싱 (Deep 가중치를 무자비도에 따라 70%까지 점진적 강화)
            if (ctx.playerNearNet)
            {
                deepWeight = Mathf.Lerp(0.20f, 0.70f, ruthlessness);
                lobWeight = Mathf.Lerp(0.10f, 0.15f, ruthlessness);
                safeWeight = 1.0f - deepWeight - lobWeight;
            }
            // [상황 1: 플레이어가 깊숙이 물러나 백코트에 있을 때] 네트 앞에 톡 떨어뜨림 (Drop 가중치를 무자비도에 따라 65%까지 점진적 강화)
            else if (ctx.playerDeepCourt)
            {
                dropWeight = Mathf.Lerp(0.10f, 0.65f, ruthlessness);
                deepWeight = Mathf.Lerp(0.40f, 0.15f, ruthlessness);
                safeWeight = 1.0f - dropWeight - deepWeight;
            }
            // AI가 네트 근처에 수비하러 붙어있을 때: 길게 찌르기 빈도를 무자비도에 비례해 최대 60%까지 상향
            else if (ctx.opponentNearNet)
            {
                deepWeight = Mathf.Lerp(0.35f, 0.60f, ruthlessness);
                lobWeight = Mathf.Lerp(0.15f, 0.15f, ruthlessness);
                safeWeight = 1.0f - deepWeight - lobWeight;
            }
            // 플레이어가 좌우/전후 위기 상황일 때 빈 공간 찌르기
            else if (ctx.playerOutOfPosition)
            {
                deepWeight = Mathf.Lerp(0.30f, 0.50f, ruthlessness);
                dropWeight = Mathf.Lerp(0.20f, 0.40f, ruthlessness);
                safeWeight = 1.0f - deepWeight - dropWeight;
            }
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

            if (lastAiShotIntent == ShotIntent.Drop)
            {
                dropWeight = 0f;
            }
            if (lastAiShotIntent == ShotIntent.Lob)
            {
                lobWeight = 0f;
            }
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
