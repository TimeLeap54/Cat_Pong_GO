using System.Collections.Generic;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Config;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    public sealed class AiDefensivePositioner
    {
        public float SelectTargetX(
            AiRallySituation situation,
            AISwingPlan plan,
            IReadOnlyList<BallArrivalCandidate> candidates,
            BallSnapshot ball,
            AIBalanceConfig aiConfig,
            float coverageMaxX)
        {
            if (!situation.Receiving)
            {
                return aiConfig.HomeX;
            }

            if (situation.ServeReturn)
            {
                float serveTarget = plan == null || plan.Consumed
                    ? ball.PositionX + 0.35f
                    : plan.InterceptPosition.x + 0.35f;
                return ClampServeReceiveTarget(serveTarget, aiConfig, coverageMaxX);
            }

            float fallbackTarget = ClampToCourt(ball.PositionX + 0.85f, aiConfig, coverageMaxX);
            if (plan == null || plan.Consumed)
            {
                return SelectFallbackTarget(situation.DefenseStance, fallbackTarget, ball, aiConfig, coverageMaxX);
            }

            switch (situation.DefenseStance)
            {
                case AiDefenseStance.NetDropDefense:
                    if (plan.InterceptPosition.x > 3.5f || ball.PositionX > 4f)
                    {
                        return ClampToCourt(plan.InterceptPosition.x + 0.35f, aiConfig, coverageMaxX);
                    }

                    return ClampToCourt(Mathf.Min(ball.PositionX + 0.45f, 3.2f), aiConfig, coverageMaxX);
                case AiDefenseStance.DeepLobDefense:
                    return ClampToCourt(Mathf.Max(plan.InterceptPosition.x + 0.5f, 6.8f), aiConfig, coverageMaxX);
                case AiDefenseStance.OverheadSkimDefense:
                    return ClampToCourt(Mathf.Max(plan.InterceptPosition.x + 0.65f, 4.8f), aiConfig, coverageMaxX);
                case AiDefenseStance.EmergencyReturn:
                    return ClampToCourt(plan.InterceptPosition.x + 0.35f, aiConfig, coverageMaxX);
                case AiDefenseStance.ServeReceive:
                    return ClampServeReceiveTarget(plan.InterceptPosition.x + 0.55f, aiConfig, coverageMaxX);
                case AiDefenseStance.BounceDefense:
                case AiDefenseStance.None:
                default:
                    return ClampToCourt(Mathf.Max(plan.InterceptPosition.x + 0.85f, 2.3f), aiConfig, coverageMaxX);
            }
        }

        private static float SelectFallbackTarget(
            AiDefenseStance stance,
            float fallbackTarget,
            BallSnapshot ball,
            AIBalanceConfig aiConfig,
            float coverageMaxX)
        {
            switch (stance)
            {
                case AiDefenseStance.NetDropDefense:
                    return ClampToCourt(Mathf.Min(ball.PositionX + 0.45f, 3.2f), aiConfig, coverageMaxX);
                case AiDefenseStance.DeepLobDefense:
                    return ClampToCourt(Mathf.Max(ball.PositionX + 0.35f, 6.8f), aiConfig, coverageMaxX);
                case AiDefenseStance.OverheadSkimDefense:
                    return ClampToCourt(Mathf.Max(ball.PositionX + 0.45f, 4.8f), aiConfig, coverageMaxX);
                case AiDefenseStance.EmergencyReturn:
                    return ClampToCourt(ball.PositionX + 0.35f, aiConfig, coverageMaxX);
                default:
                    return fallbackTarget;
            }
        }

        private static float ClampToCourt(float target, AIBalanceConfig aiConfig, float coverageMaxX)
        {
            return Mathf.Clamp(target, aiConfig.CourtMinX, coverageMaxX);
        }

        private static float ClampServeReceiveTarget(float target, AIBalanceConfig aiConfig, float coverageMaxX)
        {
            return Mathf.Clamp(target, aiConfig.CourtMinX, coverageMaxX - 0.2f);
        }
    }
}
