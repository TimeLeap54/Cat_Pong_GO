using CatTennis.Rebuild.State;

namespace CatTennis.Rebuild.Rules
{
    /// <summary>Deterministically converts rally events into state transitions and point results.</summary>
    public sealed class CourtRuleJudge
    {
        public RuleTransition Evaluate(RallyContext context, RuleEvent ruleEvent)
        {
            if (!IsValidContext(context) ||
                context.PointEnded ||
                ruleEvent.PointId != context.PointId ||
                ruleEvent.EventId <= context.LastProcessedEventId ||
                !IsValidEventPayload(ruleEvent))
            {
                return RuleTransition.Rejected(context);
            }

            switch (context.State)
            {
                case RallyState.Idle:
                    return EvaluateIdle(context, ruleEvent);
                case RallyState.InFlight:
                    return EvaluateInFlight(context, ruleEvent);
                case RallyState.ReceiverCourtBounced:
                    return EvaluateAfterValidBounce(context, ruleEvent);
                default:
                    return RuleTransition.Rejected(context);
            }
        }

        private static RuleTransition EvaluateIdle(RallyContext context, RuleEvent ruleEvent)
        {
            if (ruleEvent.Type != RuleEventType.Hit || !IsValidHitter(ruleEvent.Hitter))
            {
                return RuleTransition.Rejected(context);
            }

            return ContinueAfterHit(context, ruleEvent);
        }

        private static RuleTransition EvaluateInFlight(RallyContext context, RuleEvent ruleEvent)
        {
            switch (ruleEvent.Type)
            {
                case RuleEventType.Hit:
                    return EvaluateHit(context, ruleEvent);
                case RuleEventType.GroundTouch:
                    return EvaluateFirstGroundTouch(context, ruleEvent);
                case RuleEventType.NetContact:
                    return ContinueWithNetContact(context, ruleEvent);
                case RuleEventType.NetStopped:
                    return EndWithLastHitterLoss(context, ruleEvent, FailureReason.NetStopped);
                case RuleEventType.BoundaryExit:
                    return IsValidBoundary(ruleEvent.BoundaryType)
                        ? EndWithLastHitterLoss(context, ruleEvent, FailureReason.OutBeforeValidBounce)
                        : RuleTransition.Rejected(context);
                default:
                    return RuleTransition.Rejected(context);
            }
        }

        private static RuleTransition EvaluateAfterValidBounce(RallyContext context, RuleEvent ruleEvent)
        {
            switch (ruleEvent.Type)
            {
                case RuleEventType.Hit:
                    return EvaluateHit(context, ruleEvent);
                case RuleEventType.GroundTouch:
                    return IsValidGroundArea(ruleEvent.CourtArea)
                        ? EndWithReceiverLoss(context, ruleEvent, FailureReason.DoubleBounce)
                        : RuleTransition.Rejected(context);
                case RuleEventType.NetContact:
                    return ContinueWithNetContact(context, ruleEvent);
                case RuleEventType.NetStopped:
                    return EndWithReceiverLoss(context, ruleEvent, FailureReason.NetStopped);
                case RuleEventType.BoundaryExit:
                    return IsValidBoundary(ruleEvent.BoundaryType)
                        ? EndWithReceiverLoss(context, ruleEvent, FailureReason.UnreturnedAfterValidBounce)
                        : RuleTransition.Rejected(context);
                default:
                    return RuleTransition.Rejected(context);
            }
        }

        private static RuleTransition EvaluateHit(RallyContext context, RuleEvent ruleEvent)
        {
            if (!IsValidHitter(ruleEvent.Hitter))
            {
                return RuleTransition.Rejected(context);
            }

            if (ruleEvent.Hitter == context.LastHitter)
            {
                return EndPoint(
                    context,
                    ruleEvent,
                    context.ExpectedReceiver,
                    ToCourtSide(context.LastHitter),
                    FailureReason.DoubleTouch);
            }

            if (ToCourtSide(ruleEvent.Hitter) != context.ExpectedReceiver)
            {
                return RuleTransition.Rejected(context);
            }

            return ContinueAfterHit(context, ruleEvent);
        }

        private static RuleTransition EvaluateFirstGroundTouch(RallyContext context, RuleEvent ruleEvent)
        {
            if (!IsValidGroundArea(ruleEvent.CourtArea))
            {
                return RuleTransition.Rejected(context);
            }

            if (ruleEvent.CourtArea == CourtArea.Out)
            {
                return EndWithLastHitterLoss(context, ruleEvent, FailureReason.OutBeforeValidBounce);
            }

            CourtArea receiverArea = ToCourtArea(context.ExpectedReceiver);
            if (ruleEvent.CourtArea == receiverArea)
            {
                RallyContext next = new RallyContext(
                    context.PointId,
                    ruleEvent.EventId,
                    RallyState.ReceiverCourtBounced,
                    context.LastHitter,
                    context.ExpectedReceiver,
                    ruleEvent.CourtArea,
                    context.NetTouched);
                return new RuleTransition(true, next, false, default);
            }

            return EndWithLastHitterLoss(context, ruleEvent, FailureReason.FailedToClear);
        }

        private static RuleTransition ContinueAfterHit(RallyContext context, RuleEvent ruleEvent)
        {
            HitterType hitter = ruleEvent.Hitter;
            RallyContext next = new RallyContext(
                context.PointId,
                ruleEvent.EventId,
                RallyState.InFlight,
                hitter,
                Opposite(ToCourtSide(hitter)),
                CourtArea.None,
                false);
            return new RuleTransition(true, next, false, default);
        }

        private static RuleTransition ContinueWithNetContact(RallyContext context, RuleEvent ruleEvent)
        {
            RallyContext next = new RallyContext(
                context.PointId,
                ruleEvent.EventId,
                context.State,
                context.LastHitter,
                context.ExpectedReceiver,
                context.FirstBounceArea,
                true);
            return new RuleTransition(true, next, false, default);
        }

        private static RuleTransition EndWithLastHitterLoss(
            RallyContext context,
            RuleEvent ruleEvent,
            FailureReason reason)
        {
            return EndPoint(
                context,
                ruleEvent,
                context.ExpectedReceiver,
                ToCourtSide(context.LastHitter),
                reason);
        }

        private static RuleTransition EndWithReceiverLoss(
            RallyContext context,
            RuleEvent ruleEvent,
            FailureReason reason)
        {
            return EndPoint(
                context,
                ruleEvent,
                ToCourtSide(context.LastHitter),
                context.ExpectedReceiver,
                reason);
        }

        private static RuleTransition EndPoint(
            RallyContext context,
            RuleEvent ruleEvent,
            CourtSide winner,
            CourtSide loser,
            FailureReason reason)
        {
            if (winner == CourtSide.None || loser == CourtSide.None || winner == loser)
            {
                return RuleTransition.Rejected(context);
            }

            RallyContext next = new RallyContext(
                context.PointId,
                ruleEvent.EventId,
                RallyState.Ended,
                context.LastHitter,
                context.ExpectedReceiver,
                context.FirstBounceArea,
                context.NetTouched);
            PointResult result = new PointResult(
                context.PointId,
                ruleEvent.EventId,
                winner,
                loser,
                reason);
            return new RuleTransition(true, next, true, result);
        }

        private static bool IsValidContext(RallyContext context)
        {
            if (context.PointId <= 0 || context.LastProcessedEventId < 0)
            {
                return false;
            }

            if (context.State == RallyState.Idle)
            {
                return context.LastHitter == HitterType.None &&
                       context.ExpectedReceiver == CourtSide.None;
            }

            return IsValidHitter(context.LastHitter) &&
                   context.ExpectedReceiver == Opposite(ToCourtSide(context.LastHitter));
        }

        private static bool IsValidHitter(HitterType hitter)
        {
            return hitter == HitterType.Player || hitter == HitterType.Opponent;
        }

        private static bool IsValidEventPayload(RuleEvent ruleEvent)
        {
            switch (ruleEvent.Type)
            {
                case RuleEventType.Hit:
                    return IsValidHitter(ruleEvent.Hitter) &&
                           ruleEvent.CourtArea == CourtArea.None &&
                           ruleEvent.BoundaryType == BoundaryType.None;
                case RuleEventType.GroundTouch:
                    return ruleEvent.Hitter == HitterType.None &&
                           IsValidGroundArea(ruleEvent.CourtArea) &&
                           ruleEvent.BoundaryType == BoundaryType.None;
                case RuleEventType.NetContact:
                case RuleEventType.NetStopped:
                    return ruleEvent.Hitter == HitterType.None &&
                           ruleEvent.CourtArea == CourtArea.None &&
                           ruleEvent.BoundaryType == BoundaryType.None;
                case RuleEventType.BoundaryExit:
                    return ruleEvent.Hitter == HitterType.None &&
                           ruleEvent.CourtArea == CourtArea.None &&
                           IsValidBoundary(ruleEvent.BoundaryType);
                default:
                    return false;
            }
        }

        private static bool IsValidGroundArea(CourtArea area)
        {
            return area == CourtArea.PlayerCourt ||
                   area == CourtArea.OpponentCourt ||
                   area == CourtArea.Out;
        }

        private static bool IsValidBoundary(BoundaryType boundaryType)
        {
            return boundaryType != BoundaryType.None;
        }

        private static CourtSide ToCourtSide(HitterType hitter)
        {
            return hitter == HitterType.Player
                ? CourtSide.Player
                : hitter == HitterType.Opponent
                    ? CourtSide.Opponent
                    : CourtSide.None;
        }

        private static CourtArea ToCourtArea(CourtSide side)
        {
            return side == CourtSide.Player
                ? CourtArea.PlayerCourt
                : side == CourtSide.Opponent
                    ? CourtArea.OpponentCourt
                    : CourtArea.None;
        }

        private static CourtSide Opposite(CourtSide side)
        {
            return side == CourtSide.Player
                ? CourtSide.Opponent
                : side == CourtSide.Opponent
                    ? CourtSide.Player
                    : CourtSide.None;
        }
    }
}
