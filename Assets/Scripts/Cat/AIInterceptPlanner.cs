using System.Collections.Generic;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    public sealed class AIInterceptPlanner
    {
        public bool TrySelect(
            IReadOnlyList<BallArrivalCandidate> candidates,
            float currentX,
            float moveSpeed,
            float elapsedObservationAge,
            float swingLead,
            float jumpLead,
            bool isServeToss,
            bool forceBounceMode,
            out BallArrivalCandidate selected)
        {
            return TrySelect(
                candidates,
                currentX,
                moveSpeed,
                elapsedObservationAge,
                swingLead,
                jumpLead,
                isServeToss,
                AiDefenseStance.BounceDefense,
                forceBounceMode,
                out selected);
        }

        public bool TrySelect(
            IReadOnlyList<BallArrivalCandidate> candidates,
            float currentX,
            float moveSpeed,
            float elapsedObservationAge,
            float swingLead,
            float jumpLead,
            bool isServeToss,
            AiDefenseStance stance,
            bool forceBounceMode,
            out BallArrivalCandidate selected)
        {
            return TrySelect(
                candidates,
                currentX,
                moveSpeed,
                elapsedObservationAge,
                swingLead,
                jumpLead,
                isServeToss,
                stance,
                forceBounceMode,
                float.PositiveInfinity,
                out selected);
        }

        public bool TrySelect(
            IReadOnlyList<BallArrivalCandidate> candidates,
            float currentX,
            float moveSpeed,
            float elapsedObservationAge,
            float swingLead,
            float jumpLead,
            bool isServeToss,
            AiDefenseStance stance,
            bool forceBounceMode,
            float courtMaxX,
            out BallArrivalCandidate selected)
        {
            selected = default;
            if (candidates == null || candidates.Count == 0)
            {
                return false;
            }

            bool preferBounce = ShouldPreferBounce(stance, forceBounceMode, isServeToss);
            if (TrySelectBest(candidates, currentX, moveSpeed, elapsedObservationAge, swingLead, jumpLead,
                    isServeToss, stance, preferBounce, true, courtMaxX, out selected))
            {
                return true;
            }

            if (preferBounce && TrySelectBest(candidates, currentX, moveSpeed, elapsedObservationAge, swingLead,
                    jumpLead, isServeToss, stance, false, true, courtMaxX, out selected))
            {
                return true;
            }

            if (TrySelectBest(candidates, currentX, moveSpeed, elapsedObservationAge, 0f, 0f,
                    isServeToss, stance, preferBounce, false, courtMaxX, out selected))
            {
                return true;
            }

            if (preferBounce && TrySelectBest(candidates, currentX, moveSpeed, elapsedObservationAge, 0f, 0f,
                    isServeToss, stance, false, false, courtMaxX, out selected))
            {
                return true;
            }

            return false;
        }

        private static bool TrySelectBest(
            IReadOnlyList<BallArrivalCandidate> candidates,
            float currentX,
            float moveSpeed,
            float elapsedObservationAge,
            float swingLead,
            float jumpLead,
            bool isServeToss,
            AiDefenseStance stance,
            bool preferBounce,
            bool requireReachable,
            float courtMaxX,
            out BallArrivalCandidate selected)
        {
            selected = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                BallArrivalCandidate candidate = candidates[i];
                if (!IsEligible(candidate, currentX, moveSpeed, elapsedObservationAge, swingLead, jumpLead,
                        isServeToss, preferBounce, requireReachable, courtMaxX))
                {
                    continue;
                }

                float score = ScoreCandidate(candidate, currentX, elapsedObservationAge, stance, preferBounce,
                    requireReachable);
                if (score > bestScore)
                {
                    bestScore = score;
                    selected = candidate;
                    found = true;
                }
            }

            return found;
        }

        private static bool IsEligible(
            BallArrivalCandidate candidate,
            float currentX,
            float moveSpeed,
            float elapsedObservationAge,
            float swingLead,
            float jumpLead,
            bool isServeToss,
            bool preferBounce,
            bool requireReachable,
            float courtMaxX)
        {
            if (candidate.Position.x < 0f)
            {
                return false;
            }

            if (candidate.Position.x > courtMaxX)
            {
                return false;
            }

            if (isServeToss && candidate.ArrivalTime < 0.35f)
            {
                return false;
            }

            bool isBounce = candidate.BounceCountBeforeArrival == 1;
            bool isVolley = candidate.BounceCountBeforeArrival == 0;
            if (preferBounce && !isBounce)
            {
                return false;
            }

            if (!preferBounce && !isBounce && !isVolley)
            {
                return false;
            }

            float remaining = candidate.ArrivalTime - elapsedObservationAge;
            if (remaining <= swingLead)
            {
                return false;
            }

            if (requireReachable)
            {
                float travelTime = Mathf.Abs(candidate.Position.x - currentX) / Mathf.Max(moveSpeed, 0.01f);
                if (travelTime > remaining + 0.08f)
                {
                    return false;
                }
            }

            if (candidate.RequiresJump && remaining < jumpLead)
            {
                return false;
            }

            return true;
        }

        private static float ScoreCandidate(
            BallArrivalCandidate candidate,
            float currentX,
            float elapsedObservationAge,
            AiDefenseStance stance,
            bool preferBounce,
            bool requireReachable)
        {
            float remaining = Mathf.Max(0f, candidate.ArrivalTime - elapsedObservationAge);
            float distance = Mathf.Abs(candidate.Position.x - currentX);
            bool isBounce = candidate.BounceCountBeforeArrival == 1;
            bool isVolley = candidate.BounceCountBeforeArrival == 0;

            float score = remaining * 12f - distance * 4f;
            if (isBounce)
            {
                score += preferBounce ? 500f : 120f;
            }
            else if (isVolley)
            {
                score += preferBounce ? -120f : 80f;
            }

            if (candidate.RequiresJump)
            {
                score -= 35f;
            }

            if (!requireReachable)
            {
                score -= distance * 12f;
            }

            switch (stance)
            {
                case AiDefenseStance.NetDropDefense:
                    score += Mathf.Clamp01((3.2f - candidate.Position.x) / 3.2f) * 180f;
                    score += Mathf.Clamp01((1.7f - candidate.Position.y) / 1.7f) * 90f;
                    break;
                case AiDefenseStance.DeepLobDefense:
                    score += Mathf.Clamp01((candidate.Position.x - 4.8f) / 3f) * 220f;
                    score += isBounce ? 180f : -80f;
                    break;
                case AiDefenseStance.OverheadSkimDefense:
                    score += isBounce ? 220f : -160f;
                    score += Mathf.Clamp01((candidate.Position.x - currentX) / 2.5f) * 80f;
                    break;
                case AiDefenseStance.EmergencyReturn:
                    score += Mathf.Clamp01(1.6f - distance) * 140f;
                    break;
                case AiDefenseStance.ServeReceive:
                    score += isBounce ? 220f : -90f;
                    break;
                case AiDefenseStance.BounceDefense:
                case AiDefenseStance.None:
                default:
                    score += isBounce ? 160f : 0f;
                    break;
            }

            return score;
        }

        private static bool ShouldPreferBounce(
            AiDefenseStance stance,
            bool forceBounceMode,
            bool isServeToss)
        {
            if (isServeToss)
            {
                return false;
            }

            if (forceBounceMode)
            {
                return true;
            }

            return stance == AiDefenseStance.BounceDefense ||
                   stance == AiDefenseStance.ServeReceive ||
                   stance == AiDefenseStance.NetDropDefense ||
                   stance == AiDefenseStance.DeepLobDefense ||
                   stance == AiDefenseStance.OverheadSkimDefense;
        }
    }
}
