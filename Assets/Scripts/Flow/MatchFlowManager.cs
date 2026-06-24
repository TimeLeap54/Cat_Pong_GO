using System;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
using UnityEngine;

namespace CatTennis.Rebuild.Flow
{
    /// <summary>Owns match score while leaving point decisions to the rule layer.</summary>
    public sealed class MatchFlowManager : MonoBehaviour
    {
        [SerializeField] private Phase3PointLoopConfig config;

        private ScoreSystem scoreSystem;

        public int PlayerScore => Score.PlayerScore;
        public int OpponentScore => Score.OpponentScore;
        public bool MatchEnded => Score.IsMatchEnded;
        public CourtSide MatchWinner => Score.MatchWinner;

        public void Initialize(Phase3PointLoopConfig pointLoopConfig)
        {
            config = pointLoopConfig;
            config?.ValidateOrThrow();
            scoreSystem = config == null ? null : new ScoreSystem(config.TargetScore);
        }

        public bool TryApplyPoint(PointResult result)
        {
            return Score.TryApplyPoint(result);
        }

        public void ResetMatch()
        {
            Score.ResetMatch();
        }

        private ScoreSystem Score
        {
            get
            {
                if (scoreSystem == null)
                {
                    if (config == null)
                    {
                        throw new InvalidOperationException("Phase3PointLoopConfig is required.");
                    }

                    config.ValidateOrThrow();
                    scoreSystem = new ScoreSystem(config.TargetScore);
                }

                return scoreSystem;
            }
        }
    }
}
