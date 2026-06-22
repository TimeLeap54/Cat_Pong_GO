using System;
using CatTennis.Rebuild.State;

namespace CatTennis.Rebuild.Rules
{
    /// <summary>Owns all score mutation and rejects duplicate or stale point results.</summary>
    public sealed class ScoreSystem
    {
        public ScoreSystem(int targetScore = 5)
        {
            if (targetScore <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetScore));
            }

            TargetScore = targetScore;
        }

        public int TargetScore { get; }
        public int PlayerScore { get; private set; }
        public int OpponentScore { get; private set; }
        public long LastScoredPointId { get; private set; }
        public bool IsMatchEnded { get; private set; }
        public CourtSide MatchWinner { get; private set; }

        public bool TryApplyPoint(PointResult result)
        {
            if (IsMatchEnded ||
                !IsValidResult(result) ||
                result.PointId <= LastScoredPointId)
            {
                return false;
            }

            if (result.Winner == CourtSide.Player)
            {
                PlayerScore++;
            }
            else
            {
                OpponentScore++;
            }

            LastScoredPointId = result.PointId;
            if (PlayerScore >= TargetScore || OpponentScore >= TargetScore)
            {
                IsMatchEnded = true;
                MatchWinner = result.Winner;
            }

            return true;
        }

        public void ResetMatch()
        {
            PlayerScore = 0;
            OpponentScore = 0;
            IsMatchEnded = false;
            MatchWinner = CourtSide.None;
        }

        private static bool IsValidResult(PointResult result)
        {
            if (!result.HasWinner || result.PointId <= 0 || result.SourceEventId <= 0)
            {
                return false;
            }

            bool validWinner = result.Winner == CourtSide.Player ||
                               result.Winner == CourtSide.Opponent;
            bool validLoser = result.Loser == CourtSide.Player ||
                              result.Loser == CourtSide.Opponent;
            bool validReason = result.FailureReason != FailureReason.None &&
                               result.FailureReason != FailureReason.InvalidEvent;
            return validWinner &&
                   validLoser &&
                   validReason &&
                   result.Winner != result.Loser;
        }
    }
}
