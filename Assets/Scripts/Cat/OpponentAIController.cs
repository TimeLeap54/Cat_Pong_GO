using System.Collections.Generic;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(CatMotor))]
    public sealed class OpponentAIController : MonoBehaviour
    {
        private const float RallyCoverageExtraX = 2.4f;
        private const float RallyHighReceiveAbsoluteY = 1.32f;
        private const float RallyHighReceiveRelativeY = 0.62f;

        [SerializeField] private bool logRallyDiagnostics = true;

        private readonly DelayedBallObserver observer = new DelayedBallObserver();
        private readonly BallArrivalPredictor predictor = new BallArrivalPredictor();
        private readonly AIInterceptPlanner interceptPlanner = new AIInterceptPlanner();
        private readonly AiRallySituationClassifier situationClassifier = new AiRallySituationClassifier();
        private readonly AiDefenseStanceClassifier defenseStanceClassifier = new AiDefenseStanceClassifier();
        private readonly AiDefensivePositioner defensivePositioner = new AiDefensivePositioner();
        private readonly AiOffensiveShotSelector offensiveShotSelector = new AiOffensiveShotSelector();
        private readonly List<BallArrivalCandidate> cachedCandidates = new List<BallArrivalCandidate>();

        private BallController ball;
        private BallPhysicsConfig physicsConfig;
        private CourtGeometryConfig courtConfig;
        private PlayerControlConfig catConfig;
        private AIBalanceConfig aiConfig;
        private RallyFlowManager rally;
        private OpponentHitDetector hitDetector;
        private CatMotor motor;
        private PlayerActionStateMachine actionMachine;
        private OpponentManualHitboxController manualHitboxes;
        private PlayerCatController player;
        private RallyAiBalanceConfig rallyAiConfig;

        private AISwingPlan plan;
        private Vector2 resetPosition;
        private long nextPlanId;
        private long inputTick;
        private bool initialized;
        private int lastPredictedRallyCount = -1;
        private float lastPredictionTime = float.NegativeInfinity;
        private ShotIntent lastAiShotIntent = ShotIntent.Undefined;
        private int consecutivePressureShotsCount;
        private long lastRecordedPlanId = -1;
        private long lastTriggeredSwingPlanId = -1;
        private long lastTriggeredJumpPlanId = -1;
        private int lastTriggeredRallyCount = -1;
        private AiRallySituation lastSituation;
        private AiDiagnosticReason lastDiagnosticReason = AiDiagnosticReason.None;
        private int planMissCount;
        private int planExpiredCount;
        private int swingBlockedCount;
        private int swingTriggeredCount;
        private int hitAcceptedCount;
        private float lastMoveTarget;
        private float lastMoveInput;

        public PlayerActionFrame CurrentAction { get; private set; }
        public AISwingPlan CurrentPlan => plan;
        public bool InputLocked { get; set; }

        public void Configure(
            BallController ballController,
            BallPhysicsConfig physics,
            CourtGeometryConfig court,
            PlayerControlConfig cat,
            AIBalanceConfig ai,
            RallyFlowManager rallyManager,
            OpponentHitDetector detector,
            PlayerCatController playerController = null,
            RallyAiBalanceConfig rallyBalance = null)
        {
            ball = ballController;
            physicsConfig = physics;
            courtConfig = court;
            catConfig = cat;
            aiConfig = ai;
            rally = rallyManager;
            hitDetector = detector;
            player = playerController;
            rallyAiConfig = rallyBalance;

            ai.ValidateOrThrow();
            motor = GetComponent<CatMotor>();
            motor.Configure(ai.MoveSpeed, ai.Acceleration, ai.Deceleration, ai.CourtMinX, ai.CourtMaxX + RallyCoverageExtraX);
            GetComponent<Rigidbody2D>().gravityScale = cat.GravityScale;

            actionMachine = new PlayerActionStateMachine(cat.CreateActionSettings());
            manualHitboxes = GetComponent<OpponentManualHitboxController>();
            initialized = true;

            resetPosition = transform.position;
            ResetOpponent(resetPosition);
        }

        private void FixedUpdate()
        {
            if (!initialized || actionMachine == null)
            {
                return;
            }

            if (rally != null && !rally.HasActivePoint)
            {
                StepInactive();
                return;
            }

            AiRallySituation situation = situationClassifier.Classify(ball, rally);
            RefreshPredictionIfNeeded(situation);
            situation = situation.WithDefenseStance(
                defenseStanceClassifier.Classify(situation, cachedCandidates, ball.CurrentSnapshot, motor.Position));
            lastSituation = situation;
            EnsureDefensePlan(situation);

            float target = defensivePositioner.SelectTargetX(
                situation, plan, cachedCandidates, ball.CurrentSnapshot, aiConfig, GetCoverageMaxX());
            float moveX = CalculateMoveInput(target);
            lastMoveTarget = target;
            lastMoveInput = moveX;

            bool jump = false;
            bool swing = false;
            bool smash = false;
            TryBuildAttackInput(situation, ref jump, ref swing, ref smash);

            if (InputLocked)
            {
                moveX = 0f;
                lastMoveInput = 0f;
                jump = false;
                swing = false;
                smash = false;
            }

            StepAction(situation.PointId, moveX, jump, swing, smash);
            RecordConsumedPlanIntent();
        }

        private void StepInactive()
        {
            plan = null;
            manualHitboxes?.DisableAll();
            actionMachine.Step(new PlayerInputFrame(0f, false, false, false, 0f, ++inputTick), IsGrounded());
            motor.Apply(0f, false, 0f, Time.fixedDeltaTime);
        }

        private void RefreshPredictionIfNeeded(AiRallySituation situation)
        {
            if (ShouldRefreshPrediction(situation))
            {
                RefreshPrediction(situation);
            }
        }

        private void EnsureDefensePlan(AiRallySituation situation)
        {
            if (situation.CanPlanDefense && (plan == null || plan.Consumed))
            {
                UpdatePlan(situation);
            }
        }

        private bool ShouldRefreshPrediction(AiRallySituation situation)
        {
            if (!situation.CanPlanDefense)
            {
                return false;
            }

            if (situation.RallyHitCount != lastPredictedRallyCount)
            {
                return true;
            }

            if (cachedCandidates.Count == 0)
            {
                return true;
            }

            float refreshDelay = rallyAiConfig == null
                ? aiConfig.ReactionDelay
                : Mathf.Lerp(rallyAiConfig.ReactionDelayMin, rallyAiConfig.ReactionDelayMax, 0.5f);
            return (plan == null || plan.Consumed) && Time.time - lastPredictionTime >= refreshDelay;
        }

        private void RefreshPrediction(AiRallySituation situation)
        {
            lastPredictedRallyCount = situation.RallyHitCount;
            lastPredictionTime = Time.time;

            float maxCourtX = GetCoverageMaxX();
            var freshCandidates = predictor.Predict(
                ball.CurrentSnapshot,
                physicsConfig.CreateSettings(),
                courtConfig.GroundY,
                aiConfig.PredictionStep,
                aiConfig.PredictionHorizon,
                0.15f,
                maxCourtX,
                aiConfig.JumpHeightThreshold);

            cachedCandidates.Clear();
            if (freshCandidates != null)
            {
                for (int i = 0; i < freshCandidates.Count; i++)
                {
                    cachedCandidates.Add(freshCandidates[i]);
                }
            }

            plan = null;
        }

        private void UpdatePlan(AiRallySituation situation)
        {
            float observationAge = Mathf.Max(0f, Time.time - lastPredictionTime);
            if (!interceptPlanner.TrySelect(
                    cachedCandidates,
                    motor.Position.x,
                    aiConfig.MoveSpeed,
                    observationAge,
                    aiConfig.SwingLeadTime,
                    aiConfig.JumpLeadTime,
                    situation.ServeToss,
                    situation.DefenseStance,
                    false,
                    GetCoverageMaxX(),
                    out var candidate))
            {
                if (situation.ServeReturn && TryCreateServeReceiveFallback(situation, observationAge, out candidate))
                {
                    CreatePlanFromCandidate(situation, candidate, observationAge);
                }
                else if (TryCreateEmergencyFallback(situation, observationAge, out candidate))
                {
                    CreatePlanFromCandidate(situation.WithDefenseStance(AiDefenseStance.EmergencyReturn), candidate, observationAge);
                }
                return;
            }

            CreatePlanFromCandidate(situation, candidate, observationAge);
        }

        private bool TryCreateServeReceiveFallback(
            AiRallySituation situation,
            float observationAge,
            out BallArrivalCandidate candidate)
        {
            candidate = default;
            if (!situation.ServeReturn || ball == null || !ball.CurrentSnapshot.IsActive)
            {
                return false;
            }

            float targetX = Mathf.Clamp(ball.CurrentSnapshot.PositionX, aiConfig.CourtMinX, GetCoverageMaxX() - 0.2f);
            float targetY = Mathf.Clamp(ball.CurrentSnapshot.PositionY, courtConfig.GroundY + 0.35f, 2.35f);
            float remaining = Mathf.Max(aiConfig.SwingLeadTime + 0.1f, 0.18f);
            candidate = new BallArrivalCandidate(
                new Vector2(targetX, targetY),
                observationAge + remaining,
                ball.CurrentSnapshot.StepIndex + 6,
                1,
                targetY >= aiConfig.JumpHeightThreshold);
            return true;
        }

        private bool TryCreateEmergencyFallback(
            AiRallySituation situation,
            float observationAge,
            out BallArrivalCandidate candidate)
        {
            candidate = default;
            if (!situation.Receiving || ball == null || !ball.CurrentSnapshot.IsActive)
            {
                return false;
            }

            float targetX = Mathf.Clamp(ball.CurrentSnapshot.PositionX, aiConfig.CourtMinX, GetCoverageMaxX() - 0.2f);
            float targetY = Mathf.Clamp(ball.CurrentSnapshot.PositionY, courtConfig.GroundY + 0.35f, 2.6f);
            float remaining = Mathf.Max(aiConfig.SwingLeadTime + 0.08f, 0.16f);
            candidate = new BallArrivalCandidate(
                new Vector2(targetX, targetY),
                observationAge + remaining,
                ball.CurrentSnapshot.StepIndex + 5,
                ball.CurrentSnapshot.PositionY <= courtConfig.GroundY + 0.6f ? 1 : 0,
                targetY >= aiConfig.JumpHeightThreshold);
            return true;
        }

        private void CreatePlanFromCandidate(
            AiRallySituation situation,
            BallArrivalCandidate candidate,
            float observationAge)
        {
            long id = ++nextPlanId;
            bool requiresJump = candidate.RequiresJump || IsHighRallyReceive(situation, candidate.Position.y);
            SwingKind kind = requiresJump ? SwingKind.Smash : SwingKind.Normal;
            float remaining = Mathf.Max(0f, candidate.ArrivalTime - observationAge);
            plan = new AISwingPlan(
                situation.PointId,
                id,
                0,
                candidate.StepIndex,
                kind,
                ShotIntent.Undefined,
                remaining,
                candidate.Position,
                requiresJump,
                candidate.BounceCountBeforeArrival);
        }

        private float GetCoverageMaxX()
        {
            return aiConfig.CourtMaxX + (rally != null && rally.IsRallyMode ? RallyCoverageExtraX : 0f);
        }

        private bool IsActionLocked()
        {
            return !IsGrounded() || CurrentAction.SwingState != SwingState.Ready;
        }

        private float CalculateMoveInput(float target)
        {
            float distance = target - motor.Position.x;
            return Mathf.Abs(distance) > 0.05f ? Mathf.Sign(distance) : 0f;
        }

        private void TryBuildAttackInput(
            AiRallySituation situation,
            ref bool jump,
            ref bool swing,
            ref bool smash)
        {
            if (!situation.CanAttack || plan == null || plan.Consumed)
            {
                if (situation.CanAttack)
                {
                    planMissCount++;
                    CaptureDiagnostic(AiDiagnosticReason.NoPlan);
                }
                return;
            }

            if (CurrentAction.SwingState != SwingState.Ready)
            {
                return;
            }

            plan.RemainingTime = Mathf.Max(-0.2f, plan.RemainingTime - Time.fixedDeltaTime);
            if (plan.RemainingTime < -0.15f)
            {
                planExpiredCount++;
                CaptureDiagnostic(AiDiagnosticReason.PlanExpired);
                plan.Consumed = true;
                plan = null;
                return;
            }

            if (!ShouldTriggerSwing(situation) || plan.PlanId == lastTriggeredSwingPlanId)
            {
                return;
            }

            lastTriggeredSwingPlanId = plan.PlanId;
            lastTriggeredRallyCount = situation.RallyHitCount;
            swingTriggeredCount++;
            CaptureDiagnostic(AiDiagnosticReason.SwingTriggered);
            ShotIntent intent = situation.ServeToss
                ? ShotIntent.Serve
                : offensiveShotSelector.Select(
                    BuildTacticalContext(situation),
                    aiConfig,
                    rallyAiConfig,
                    lastAiShotIntent,
                    consecutivePressureShotsCount);
            if (rally != null && rally.IsRallyMode && intent == ShotIntent.Smash)
            {
                intent = ShotIntent.SafeReturn;
            }

            bool requiresJump = intent != ShotIntent.Serve &&
                                (plan.JumpRequired ||
                                 IsHighRallyReceive(situation, plan.InterceptPosition.y) ||
                                 IsHighRallyReceive(situation, ball.CurrentSnapshot.PositionY));
            if (requiresJump)
            {
                jump = true;
                smash = true;
                lastTriggeredJumpPlanId = plan.PlanId;
            }
            else
            {
                swing = true;
            }

            plan = WithIntent(plan, intent);
        }

        private bool ShouldTriggerSwing(AiRallySituation situation)
        {
            if (plan == null)
            {
                return false;
            }

            float swingLead = GetSwingLeadTime(situation.DefenseStance);
            if (plan.RemainingTime > swingLead)
            {
                swingBlockedCount++;
                CaptureDiagnostic(AiDiagnosticReason.WaitingForSwingLead);
                return false;
            }

            long stepDelta = plan.ExpectedBallStepIndex - ball.CurrentSnapshot.StepIndex;
            if (stepDelta < -GetMaxLateStepWindow(situation.DefenseStance))
            {
                planExpiredCount++;
                CaptureDiagnostic(AiDiagnosticReason.PlanStepMissed);
                plan.Consumed = true;
                return false;
            }

            if (plan.BounceCountBeforeArrival > 0 &&
                stepDelta > GetMaxEarlyStepWindow(situation.DefenseStance))
            {
                swingBlockedCount++;
                CaptureDiagnostic(AiDiagnosticReason.WaitingForBounceWindow);
                return false;
            }

            SwingTriggerWindow window = GetSwingTriggerWindow(situation.DefenseStance);
            float actorToPlanX = Mathf.Abs(plan.InterceptPosition.x - motor.Position.x);
            if (actorToPlanX > window.ActorPlanXTolerance)
            {
                swingBlockedCount++;
                CaptureDiagnostic(AiDiagnosticReason.ActorNotAtPlan);
                return false;
            }

            if (situation.DefenseStance == AiDefenseStance.ServeReceive &&
                stepDelta >= 0 &&
                stepDelta <= GetTriggerStepWindow(situation.DefenseStance))
            {
                return true;
            }

            float ballToActorX = Mathf.Abs(ball.CurrentSnapshot.PositionX - motor.Position.x);
            float ballToActorY = Mathf.Abs(ball.CurrentSnapshot.PositionY - motor.Position.y);
            if (ballToActorX > window.BallActorXTolerance ||
                ballToActorY > window.BallActorYTolerance)
            {
                swingBlockedCount++;
                CaptureDiagnostic(AiDiagnosticReason.BallNotNearActor);
                return false;
            }

            float ballToPlanX = Mathf.Abs(ball.CurrentSnapshot.PositionX - plan.InterceptPosition.x);
            float ballToPlanY = Mathf.Abs(ball.CurrentSnapshot.PositionY - plan.InterceptPosition.y);
            bool ready = ballToPlanX <= window.BallPlanXTolerance ||
                         ballToPlanY <= window.BallPlanYTolerance ||
                         (stepDelta >= 0 && stepDelta <= GetTriggerStepWindow(situation.DefenseStance));
            if (!ready)
            {
                swingBlockedCount++;
                CaptureDiagnostic(AiDiagnosticReason.BallNotNearPlan);
            }

            return ready;
        }

        private float GetSwingLeadTime(AiDefenseStance stance)
        {
            switch (stance)
            {
                case AiDefenseStance.ServeReceive:
                    return aiConfig.SwingLeadTime + 0.14f;
                case AiDefenseStance.NetDropDefense:
                case AiDefenseStance.EmergencyReturn:
                    return aiConfig.SwingLeadTime + 0.08f;
                case AiDefenseStance.DeepLobDefense:
                case AiDefenseStance.OverheadSkimDefense:
                    return aiConfig.SwingLeadTime + 0.16f;
                default:
                    return aiConfig.SwingLeadTime;
            }
        }

        private bool IsHighRallyReceive(AiRallySituation situation, float ballY)
        {
            if (rally == null || !rally.IsRallyMode || !situation.Receiving || situation.ServeToss)
            {
                return false;
            }

            return ballY >= RallyHighReceiveAbsoluteY ||
                   ballY - motor.Position.y >= RallyHighReceiveRelativeY;
        }

        private static long GetMaxEarlyStepWindow(AiDefenseStance stance)
        {
            switch (stance)
            {
                case AiDefenseStance.ServeReceive:
                    return 22;
                case AiDefenseStance.NetDropDefense:
                    return 10;
                case AiDefenseStance.DeepLobDefense:
                case AiDefenseStance.OverheadSkimDefense:
                    return 8;
                default:
                    return 7;
            }
        }

        private static long GetTriggerStepWindow(AiDefenseStance stance)
        {
            switch (stance)
            {
                case AiDefenseStance.ServeReceive:
                    return 14;
                case AiDefenseStance.NetDropDefense:
                case AiDefenseStance.DeepLobDefense:
                case AiDefenseStance.OverheadSkimDefense:
                    return 6;
                default:
                    return 5;
            }
        }

        private static long GetMaxLateStepWindow(AiDefenseStance stance)
        {
            switch (stance)
            {
                case AiDefenseStance.ServeReceive:
                    return 3;
                case AiDefenseStance.EmergencyReturn:
                    return 5;
                default:
                    return 4;
            }
        }

        private static SwingTriggerWindow GetSwingTriggerWindow(AiDefenseStance stance)
        {
            switch (stance)
            {
                case AiDefenseStance.ServeReceive:
                    return new SwingTriggerWindow(2.35f, 2.2f, 2.55f, 1.45f, 1.75f);
                case AiDefenseStance.NetDropDefense:
                    return new SwingTriggerWindow(1.45f, 1.35f, 1.8f, 0.95f, 1.15f);
                case AiDefenseStance.DeepLobDefense:
                    return new SwingTriggerWindow(1.7f, 1.55f, 2.35f, 1.15f, 1.55f);
                case AiDefenseStance.OverheadSkimDefense:
                    return new SwingTriggerWindow(1.65f, 1.55f, 2.55f, 1.2f, 1.75f);
                case AiDefenseStance.EmergencyReturn:
                    return new SwingTriggerWindow(1.85f, 1.7f, 2.6f, 1.4f, 1.9f);
                default:
                    return new SwingTriggerWindow(1.35f, 1.2f, 2.2f, 0.95f, 1.3f);
            }
        }

        private AiTacticalContext BuildTacticalContext(AiRallySituation situation)
        {
            Vector2 playerPosition = player != null ? player.Position : Vector2.zero;
            return new AiTacticalContext
            {
                playerPosition = playerPosition,
                opponentPosition = motor.Position,
                ballPosition = new Vector2(ball.CurrentSnapshot.PositionX, ball.CurrentSnapshot.PositionY),
                predictedBallArrival = plan.InterceptPosition,
                playerNearNet = player != null && playerPosition.x > -2.5f,
                playerDeepCourt = player != null && playerPosition.x < -5.5f,
                playerLeftSide = player != null && playerPosition.x < -4.25f,
                playerRightSide = player != null && playerPosition.x >= -4.25f,
                playerRecentlyJumped = player != null &&
                                       player.CurrentAction.LocomotionState == LocomotionState.Airborne,
                playerOutOfPosition = player != null &&
                                      Mathf.Abs(playerPosition.x - plan.InterceptPosition.x) > 2.5f,
                rallyCount = situation.RallyHitCount,
                ballArrivalRequiresJump = plan.JumpRequired || ball.CurrentSnapshot.PositionY >= 2.1f,
                opponentNearNet = motor.Position.x < 2.5f
            };
        }

        private void StepAction(long pointId, float moveX, bool jump, bool swing, bool smash)
        {
            bool planWasConsumed = plan != null && plan.Consumed;
            bool actionReady = CurrentAction.SwingState == SwingState.Ready;
            var input = actionReady
                ? new PlayerInputFrame(moveX, jump, swing, smash, 0f, ++inputTick)
                : new PlayerInputFrame(moveX, false, false, false, 0f, ++inputTick);
            CurrentAction = actionMachine.Step(input, IsGrounded());

            manualHitboxes?.ApplyAction(CurrentAction, -1);
            bool immediateHorizontal = lastSituation.Receiving && !InputLocked;
            motor.Apply(
                CurrentAction.MoveX,
                CurrentAction.JumpRequested,
                aiConfig.JumpSpeed,
                Time.fixedDeltaTime,
                immediateHorizontal);
            bool evaluatedHit = hitDetector.Evaluate(pointId, CurrentAction, motor.Position, -1, plan);
            if (evaluatedHit || (!planWasConsumed && plan != null && plan.Consumed))
            {
                hitAcceptedCount++;
                CaptureDiagnostic(AiDiagnosticReason.HitAccepted);
            }
            else if (CurrentAction.IsHitActive && plan != null && !plan.Consumed)
            {
                CaptureDiagnostic(AiDiagnosticReason.HitActiveNoContact);
            }
        }

        private void RecordConsumedPlanIntent()
        {
            if (plan == null || !plan.Consumed || plan.PlanId == lastRecordedPlanId)
            {
                return;
            }

            lastRecordedPlanId = plan.PlanId;
            lastAiShotIntent = plan.Intent;
            consecutivePressureShotsCount = AiOffensiveShotSelector.IsPressureShot(plan.Intent)
                ? consecutivePressureShotsCount + 1
                : 0;
        }

        private static AISwingPlan WithIntent(AISwingPlan source, ShotIntent intent)
        {
            if (source == null)
            {
                return null;
            }

            var updated = new AISwingPlan(
                source.PointId,
                source.PlanId,
                source.SourceObservationId,
                source.ExpectedBallStepIndex,
                source.SwingKind,
                intent,
                source.RemainingTime,
                source.InterceptPosition,
                source.JumpRequired,
                source.BounceCountBeforeArrival);
            updated.Consumed = source.Consumed;
            return updated;
        }

        public void ReportRallyPointEnd(CatTennis.Rebuild.State.PointResult result)
        {
            if (!logRallyDiagnostics || !result.HasWinner)
            {
                return;
            }

            string hitReject = hitDetector != null ? hitDetector.LastRejectReason : string.Empty;
            string manualReject = manualHitboxes != null ? manualHitboxes.LastRejectReason : string.Empty;
            string planSummary = plan == null
                ? "plan=null"
                : $"planId={plan.PlanId} consumed={plan.Consumed} remaining={plan.RemainingTime:0.000} " +
                  $"bounce={plan.BounceCountBeforeArrival} jump={plan.JumpRequired} " +
                  $"intercept=({plan.InterceptPosition.x:0.00},{plan.InterceptPosition.y:0.00}) " +
                  $"expectedStep={plan.ExpectedBallStepIndex}";
            string ballSummary = ball == null
                ? "ball=null"
                : $"ball=({ball.CurrentSnapshot.PositionX:0.00},{ball.CurrentSnapshot.PositionY:0.00}) " +
                  $"step={ball.CurrentSnapshot.StepIndex} mode={ball.PlayMode} lastShot={ball.LastShotIntent}";
            string motorSummary = motor == null
                ? "motor=null"
                : $"motor=({motor.Position.x:0.00},{motor.Position.y:0.00}) velocity=({motor.Velocity.x:0.00},{motor.Velocity.y:0.00}) " +
                  $"moveTarget={lastMoveTarget:0.00} moveInput={lastMoveInput:0.00} inputLocked={InputLocked}";
            string actionSummary = CurrentAction.SwingId <= 0
                ? "action=none"
                : $"action={CurrentAction.SwingState}/{CurrentAction.SwingKind} swingId={CurrentAction.SwingId} " +
                  $"hitActive={CurrentAction.IsHitActive}";

            Debug.Log(
                $"[RallyAI Diagnostics] point={result.PointId} winner={result.Winner} loser={result.Loser} " +
                $"failure={result.FailureReason} rally={lastSituation.RallyHitCount} phase={lastSituation.Phase} " +
                $"stance={lastSituation.DefenseStance} reason={lastDiagnosticReason} " +
                $"candidates={cachedCandidates.Count} noPlan={planMissCount} expired={planExpiredCount} " +
                $"blocked={swingBlockedCount} triggered={swingTriggeredCount} hits={hitAcceptedCount} " +
                $"{planSummary} {ballSummary} {motorSummary} {actionSummary} " +
                $"hitReject={hitReject} manualReject={manualReject}",
                this);
        }

        private void CaptureDiagnostic(AiDiagnosticReason reason)
        {
            lastDiagnosticReason = reason;
        }

        private readonly struct SwingTriggerWindow
        {
            public SwingTriggerWindow(
                float actorPlanXTolerance,
                float ballActorXTolerance,
                float ballActorYTolerance,
                float ballPlanXTolerance,
                float ballPlanYTolerance)
            {
                ActorPlanXTolerance = actorPlanXTolerance;
                BallActorXTolerance = ballActorXTolerance;
                BallActorYTolerance = ballActorYTolerance;
                BallPlanXTolerance = ballPlanXTolerance;
                BallPlanYTolerance = ballPlanYTolerance;
            }

            public float ActorPlanXTolerance { get; }
            public float BallActorXTolerance { get; }
            public float BallActorYTolerance { get; }
            public float BallPlanXTolerance { get; }
            public float BallPlanYTolerance { get; }
        }

        private bool IsGrounded()
        {
            return Physics2D.OverlapCircle(
                motor.Position + catConfig.GroundCheckOffset,
                catConfig.GroundCheckRadius,
                catConfig.GroundMask) != null;
        }

        public void ResetOpponent(Vector2 position)
        {
            resetPosition = position;
            observer.Reset();
            plan = null;
            nextPlanId = 0;
            inputTick = 0;
            lastPredictedRallyCount = -1;
            lastPredictionTime = float.NegativeInfinity;
            actionMachine?.Reset();
            motor?.ResetMotor(position);
            hitDetector?.ResetDetector();
            CurrentAction = new PlayerActionFrame(
                LocomotionState.Grounded,
                SwingState.Ready,
                SwingKind.None,
                0,
                false,
                0f);
            cachedCandidates.Clear();
            manualHitboxes?.DisableAll();
            lastAiShotIntent = ShotIntent.Undefined;
            consecutivePressureShotsCount = 0;
            lastRecordedPlanId = -1;
            lastTriggeredSwingPlanId = -1;
            lastTriggeredJumpPlanId = -1;
            lastTriggeredRallyCount = -1;
            lastSituation = default;
            lastDiagnosticReason = AiDiagnosticReason.None;
            planMissCount = 0;
            planExpiredCount = 0;
            swingBlockedCount = 0;
            swingTriggeredCount = 0;
            hitAcceptedCount = 0;
            lastMoveTarget = position.x;
            lastMoveInput = 0f;
        }

        private enum AiDiagnosticReason
        {
            None = 0,
            NoPlan = 1,
            PlanExpired = 2,
            WaitingForSwingLead = 3,
            WaitingForBounceWindow = 4,
            ActorNotAtPlan = 5,
            BallNotNearActor = 6,
            BallNotNearPlan = 7,
            SwingTriggered = 8,
            HitActiveNoContact = 9,
            HitAccepted = 10,
            PlanStepMissed = 11
        }
    }
}
