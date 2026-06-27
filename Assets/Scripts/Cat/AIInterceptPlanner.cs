using System.Collections.Generic;
using UnityEngine;
namespace CatTennis.Rebuild.Cat
{
    public sealed class AIInterceptPlanner
    {
        public bool TrySelect(IReadOnlyList<BallArrivalCandidate> candidates, float currentX,
            float moveSpeed, float elapsedObservationAge, float swingLead, float jumpLead,
            bool isServeToss, out BallArrivalCandidate selected)
        {
            // [1순위] 도달 가능하고 점프 리드타임이 확보된 안정적인 후보 우선
            for (int i = 0; i < candidates.Count; i++)
            {
                if (isServeToss && candidates[i].ArrivalTime < 0.35f) continue;

                float remaining = candidates[i].ArrivalTime - elapsedObservationAge;
                if (remaining > swingLead &&
                    Mathf.Abs(candidates[i].Position.x - currentX) / moveSpeed <= remaining)
                {
                    if (candidates[i].RequiresJump && remaining < jumpLead) continue;
                    selected = candidates[i];
                    return true;
                }
            }

            // [2순위] 점프 리드타임 제약 해제, 도달 가능한 가장 빠른 후보
            for (int i = 0; i < candidates.Count; i++)
            {
                if (isServeToss && candidates[i].ArrivalTime < 0.35f) continue;

                float remaining = candidates[i].ArrivalTime - elapsedObservationAge;
                if (remaining > swingLead &&
                    Mathf.Abs(candidates[i].Position.x - currentX) / moveSpeed <= remaining)
                {
                    selected = candidates[i];
                    return true;
                }
            }

            // [3순위] 도달 불가 시 가장 늦게 도착하는 최종 낙하지점을 쫓아감
            float maxArrivalTime = -1f;
            int bestIndex = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (isServeToss && candidates[i].ArrivalTime < 0.35f) continue;

                float remaining = candidates[i].ArrivalTime - elapsedObservationAge;
                if (remaining > swingLead && candidates[i].ArrivalTime > maxArrivalTime)
                {
                    maxArrivalTime = candidates[i].ArrivalTime;
                    bestIndex = i;
                }
            }

            if (bestIndex == -1)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (isServeToss && candidates[i].ArrivalTime < 0.35f) continue;

                    float remaining = candidates[i].ArrivalTime - elapsedObservationAge;
                    if (remaining > 0f && candidates[i].ArrivalTime > maxArrivalTime)
                    {
                        maxArrivalTime = candidates[i].ArrivalTime;
                        bestIndex = i;
                    }
                }
            }

            if (bestIndex != -1)
            {
                selected = candidates[bestIndex];
                return true;
            }

            selected = default;
            return false;
        }
    }
}
