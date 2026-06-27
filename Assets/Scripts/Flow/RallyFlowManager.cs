using System;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    /// <summary>Owns point/event identity and delegates every rule decision.</summary>
    public sealed class RallyFlowManager : MonoBehaviour
    {
        private readonly CourtRuleJudge judge = new CourtRuleJudge();

        private long globalPointId;
        private long nextEventId;
        private RallyContext context;
        private bool hasPoint;
        private bool pointResultEmitted;
        private int rallyHitCount;

        public RallyContext CurrentContext => context;
        public long GlobalPointId => globalPointId;
        public int RallyHitCount => rallyHitCount;
        public bool HasActivePoint => hasPoint &&
                                      context.State != RallyState.Ended &&
                                      !pointResultEmitted;

        public RallyContext BeginPoint()
        {
            if (globalPointId == long.MaxValue)
            {
                throw new InvalidOperationException("Point id cannot advance beyond Int64.MaxValue.");
            }

            globalPointId++;
            nextEventId = 0;
            rallyHitCount = 0;
            context = RallyContext.Create(globalPointId);
            hasPoint = true;
            pointResultEmitted = false;
            return context;
        }

        public bool RegisterHit(HitterType hitter, out PointResult pointResult)
        {
            if (!CanProcess())
            {
                pointResult = default;
                return false;
            }

            bool success = TryEvaluate(
                new RuleEvent(CurrentPointId(), NextEventId(), RuleEventType.Hit, hitter),
                out pointResult);
            if (success)
            {
                rallyHitCount++;
            }
            return success;
        }

        public bool ProcessObservation(CourtObservation observation, out PointResult pointResult)
        {
            if (!CanProcess())
            {
                pointResult = default;
                return false;
            }

            RuleEvent ruleEvent;
            switch (observation.Type)
            {
                case CourtObservationType.GroundTouch:
                    ruleEvent = new RuleEvent(
                        CurrentPointId(),
                        NextEventId(),
                        RuleEventType.GroundTouch,
                        courtArea: observation.CourtArea);
                    break;
                case CourtObservationType.BallSettled:
                    ruleEvent = new RuleEvent(
                        CurrentPointId(),
                        NextEventId(),
                        RuleEventType.BallSettled);
                    break;
                case CourtObservationType.BoundaryExit:
                    ruleEvent = new RuleEvent(
                        CurrentPointId(),
                        NextEventId(),
                        RuleEventType.BoundaryExit,
                        boundaryType: observation.BoundaryType);
                    break;
                default:
                    pointResult = default;
                    return false;
            }

            return TryEvaluate(ruleEvent, out pointResult);
        }

        private bool TryEvaluate(RuleEvent ruleEvent, out PointResult pointResult)
        {
            pointResult = default;
            if (!hasPoint || context.State == RallyState.Ended || pointResultEmitted)
            {
                return false;
            }

            RuleTransition transition = judge.Evaluate(context, ruleEvent);
            if (!transition.Accepted)
            {
                return false;
            }

            context = transition.NextContext;
            if (transition.HasPointResult)
            {
                pointResultEmitted = true;
                pointResult = transition.PointResult;
            }

            return true;
        }

        private bool CanProcess()
        {
            return hasPoint && context.State != RallyState.Ended && !pointResultEmitted;
        }

        private long CurrentPointId()
        {
            return hasPoint ? context.PointId : 0;
        }

        private long NextEventId()
        {
            if (nextEventId == long.MaxValue)
            {
                throw new InvalidOperationException("Event id cannot advance beyond Int64.MaxValue.");
            }

            nextEventId++;
            return nextEventId;
        }
    }
}
