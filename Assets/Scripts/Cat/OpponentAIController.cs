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
            actionMachine=new PlayerActionStateMachine(cat.CreateActionSettings()); initialized=true;
            manualHitboxes=GetComponent<OpponentManualHitboxController>();
            resetPosition=transform.position; ResetOpponent(resetPosition);
        }

        private void FixedUpdate()
        {
            if(!initialized) return;

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
                // [원바운드 타겟 직접 추적]
                // 공이 네트를 돌파한 이후(X > 0)에만 바운스 낙하지점으로 이동 개시
                // 공이 아직 플레이어 코트에 있으면 홈 포지션에서 대기
                if (ball.CurrentSnapshot.PositionX > 0f)
                {
                    target = plan.InterceptPosition.x;
                }
                else
                {
                    target = aiConfig.HomeX;
                }
            }

            if (Mathf.Abs(target - motor.Position.x) > 0.08f)
            {
                moveX = Mathf.Sign(target - motor.Position.x);
            }
            if(plan!=null && !plan.Consumed)
            {
                plan.RemainingTime-=Time.fixedDeltaTime;
                bool grounded=IsGrounded();
                jump=plan.JumpRequired&&grounded&&plan.RemainingTime<=aiConfig.JumpLeadTime&&plan.PlanId!=lastTriggeredJumpPlanId;
                if(jump) lastTriggeredJumpPlanId=plan.PlanId;
                // [정적 타이머 기반 스윙 트리거]
                // 원바운드 이후 공이 느리게 솟구치므로 RemainingTime 타이머만으로 정확한 타이밍에 스윙 가능
                if (plan.RemainingTime <= aiConfig.SwingLeadTime && plan.PlanId != lastTriggeredSwingPlanId)
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
                aiConfig.CourtMinX,maxCourtX,aiConfig.JumpHeightThreshold);
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
                kind,intent,Mathf.Max(0f,candidate.ArrivalTime-age),candidate.Position,requiresJump);
        }

        private ShotIntent DetermineShotIntent(AiTacticalContext ctx)
        {
            if (ctx.ballArrivalRequiresJump && ctx.rallyCount >= 8 && UnityEngine.Random.value < 0.25f)
            {
                return ShotIntent.Smash;
            }

            if ((ctx.playerRecentlyJumped || ctx.playerOutOfPosition) &&
                rallyAiConfig != null &&
                UnityEngine.Random.value < rallyAiConfig.MercyShotChanceWhenPlayerStruggling)
            {
                return ShotIntent.SafeReturn;
            }

            float safeWeight = 0f;
            float deepWeight = 0f;
            float dropWeight = 0f;
            float lobWeight = 0f;

            if (ctx.rallyCount < 8)
            {
                return ShotIntent.SafeReturn;
            }

            if (ctx.opponentNearNet)
            {
                lobWeight = 0.70f;
                safeWeight = 0.30f;
                deepWeight = 0f;
                dropWeight = 0f;
            }
            else if (ctx.playerNearNet)
            {
                deepWeight = 0.55f;
                lobWeight = 0.30f;
                safeWeight = 0.15f;
            }
            else if (ctx.playerDeepCourt)
            {
                dropWeight = 0.45f;
                safeWeight = 0.35f;
                deepWeight = 0.20f;
            }
            else if (ctx.playerOutOfPosition)
            {
                deepWeight = 0.40f;
                dropWeight = 0.30f;
                safeWeight = 0.30f;
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
