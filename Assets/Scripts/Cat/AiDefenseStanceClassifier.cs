using System.Collections.Generic;
using CatTennis.BallPhysics.Core;
using UnityEngine;

namespace CatTennis.Rebuild.Cat
{
    public sealed class AiDefenseStanceClassifier
    {
        public AiDefenseStance Classify(
            AiRallySituation situation,
            IReadOnlyList<BallArrivalCandidate> candidates,
            BallSnapshot ball,
            Vector2 opponentPosition)
        {
            if (!situation.Receiving)
            {
                return AiDefenseStance.None;
            }

            if (situation.ServeToss || situation.ServeReturn)
            {
                return AiDefenseStance.ServeReceive;
            }

            if (candidates == null || candidates.Count == 0)
            {
                return AiDefenseStance.EmergencyReturn;
            }

            bool netDrop = false;
            bool deepLob = false;
            bool overheadSkim = false;
            bool hasBounceOption = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                BallArrivalCandidate candidate = candidates[i];
                if (candidate.Position.x < 0f)
                {
                    continue;
                }

                hasBounceOption |= candidate.BounceCountBeforeArrival > 0;

                if (candidate.Position.x <= 2.35f &&
                    candidate.Position.y <= 1.55f &&
                    candidate.BounceCountBeforeArrival <= 1)
                {
                    netDrop = true;
                }

                if (candidate.Position.x >= 5.8f &&
                    (candidate.Position.y >= 1.2f || candidate.RequiresJump))
                {
                    deepLob = true;
                }

                bool nearOrBehindHead = candidate.Position.x >= opponentPosition.x - 0.35f &&
                                        candidate.Position.x <= opponentPosition.x + 1.45f;
                if (nearOrBehindHead &&
                    candidate.Position.y >= 2f &&
                    candidate.BounceCountBeforeArrival == 0)
                {
                    overheadSkim = true;
                }
            }

            if (deepLob)
            {
                return AiDefenseStance.DeepLobDefense;
            }

            if (overheadSkim && !hasBounceOption)
            {
                return AiDefenseStance.OverheadSkimDefense;
            }

            bool ballAlreadyDeep = ball.PositionX >= 6.2f && ball.PositionY >= 1.6f;
            if (ballAlreadyDeep)
            {
                return AiDefenseStance.DeepLobDefense;
            }

            if (netDrop && ball.PositionX <= 4f)
            {
                return AiDefenseStance.NetDropDefense;
            }

            return AiDefenseStance.BounceDefense;
        }
    }
}
