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
            selected = default;
            if (candidates == null || candidates.Count == 0) return false;

            // 1. 도달 가능한 안정적인 1바운드 수비 후보(BounceCount == 1)가 존재하는지 선행 탐색
            bool hasReachableBounce = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (isServeToss && candidates[i].ArrivalTime < 0.35f) continue;
                if (candidates[i].BounceCountBeforeArrival != 1) continue;

                float remaining = candidates[i].ArrivalTime - elapsedObservationAge;
                if (remaining > swingLead &&
                    Mathf.Abs(candidates[i].Position.x - currentX) / moveSpeed <= remaining)
                {
                    if (candidates[i].RequiresJump && remaining < jumpLead) continue;
                    hasReachableBounce = true;
                    break;
                }
            }

            // [0순위] 고궤도 체공 공(스파이크 기회 및 긴급 공중 수비) 선점
            if (!isServeToss)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].BounceCountBeforeArrival != 0) continue;
                    if (candidates[i].Position.y < 2.0f) continue;

                    // 1바운드 수비가 가능한 충분한 시간적 여유가 있을 때는, 헛점프 방지를 위해 아웃라인 근처(X >= 5.8f) 공중 후보 제외
                    // 1바운드 지점까지 물리적으로 도달할 수 없는 상황(hasReachableBounce == false)일 때는 긴급하게 공중에서 걷어내는 것 허용
                    if (hasReachableBounce && candidates[i].Position.x >= 5.8f) continue;

                    float remaining = candidates[i].ArrivalTime - elapsedObservationAge;
                    if (remaining > swingLead &&
                        Mathf.Abs(candidates[i].Position.x - currentX) / moveSpeed <= remaining)
                    {
                        if (candidates[i].RequiresJump && remaining < jumpLead) continue;
                        selected = candidates[i];
                        return true;
                    }
                }
            }

            // [1순위] 도달 가능하고 점프 리드타임이 확보된 안정적인 1바운드 후보
            for (int i = 0; i < candidates.Count; i++)
            {
                if (isServeToss && candidates[i].ArrivalTime < 0.35f) continue;
                if (candidates[i].BounceCountBeforeArrival != 1) continue;

                float remaining = candidates[i].ArrivalTime - elapsedObservationAge;
                if (remaining > swingLead &&
                    Mathf.Abs(candidates[i].Position.x - currentX) / moveSpeed <= remaining)
                {
                    if (candidates[i].RequiresJump && remaining < jumpLead) continue;
                    selected = candidates[i];
                    return true;
                }
            }

            // [2순위] 1바운드 수비가 물리적으로 늦을 때, 공중에 떠 있는 볼을 긴급하게 커트하는 일반 발리(바운스 0회) 후보
            for (int i = 0; i < candidates.Count; i++)
            {
                if (isServeToss) continue;
                if (candidates[i].BounceCountBeforeArrival != 0) continue;

                float remaining = candidates[i].ArrivalTime - elapsedObservationAge;
                if (remaining > swingLead &&
                    Mathf.Abs(candidates[i].Position.x - currentX) / moveSpeed <= remaining)
                {
                    if (candidates[i].RequiresJump && remaining < jumpLead) continue;
                    selected = candidates[i];
                    return true;
                }
            }

            // [3순위] 점프 리드타임 제약 해제, 도달 가능한 가장 빠른 1바운드 후보
            for (int i = 0; i < candidates.Count; i++)
            {
                if (isServeToss && candidates[i].ArrivalTime < 0.35f) continue;
                if (candidates[i].BounceCountBeforeArrival != 1) continue;

                float remaining = candidates[i].ArrivalTime - elapsedObservationAge;
                if (remaining > swingLead &&
                    Mathf.Abs(candidates[i].Position.x - currentX) / moveSpeed <= remaining)
                {
                    selected = candidates[i];
                    return true;
                }
            }

            // [4순위] 점프 리드타임 제약 해제, 도달 가능한 가장 빠른 발리(바운스 0회) 후보
            for (int i = 0; i < candidates.Count; i++)
            {
                if (isServeToss) continue;
                if (candidates[i].BounceCountBeforeArrival != 0) continue;

                float remaining = candidates[i].ArrivalTime - elapsedObservationAge;
                if (remaining > swingLead &&
                    Mathf.Abs(candidates[i].Position.x - currentX) / moveSpeed <= remaining)
                {
                    selected = candidates[i];
                    return true;
                }
            }

            // [5순위] 도달 불가 시 최종 낙하지점을 최대한 쫓아감 (1바운드 우선, 발리 차선)
            float maxArrivalTime = -1f;
            int bestIndex = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (isServeToss && candidates[i].ArrivalTime < 0.35f) continue;
                if (candidates[i].BounceCountBeforeArrival != 1 && candidates[i].BounceCountBeforeArrival != 0) continue;

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
                    if (candidates[i].BounceCountBeforeArrival != 1 && candidates[i].BounceCountBeforeArrival != 0) continue;

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
