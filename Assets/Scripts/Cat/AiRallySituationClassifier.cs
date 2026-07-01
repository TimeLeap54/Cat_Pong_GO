using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;

namespace CatTennis.Rebuild.Cat
{
    public sealed class AiRallySituationClassifier
    {
        public AiRallySituation Classify(BallController ball, RallyFlowManager rally)
        {
            if (ball == null || rally == null || !rally.HasActivePoint)
            {
                long pointId = rally != null ? rally.GlobalPointId : 0;
                return new AiRallySituation(AiRallyPhase.Idle, pointId, 0, false, false);
            }

            bool serveToss = ball.PlayMode == BallPlayMode.ServeToss && ball.CurrentSnapshot.PositionX > 0f;
            bool serveReturn = ball.PlayMode == BallPlayMode.Rally &&
                               ball.LastShotIntent == ShotIntent.Serve &&
                               rally.CurrentContext.ExpectedReceiver == CourtSide.Opponent;
            bool receiving = serveToss ||
                             (ball.PlayMode == BallPlayMode.Rally &&
                              rally.CurrentContext.ExpectedReceiver == CourtSide.Opponent);
            AiRallyPhase phase = receiving
                ? (serveToss || serveReturn ? AiRallyPhase.ServeReceive : AiRallyPhase.Defense)
                : AiRallyPhase.Recovery;

            return new AiRallySituation(phase, rally.GlobalPointId, rally.RallyHitCount, receiving, serveToss, serveReturn);
        }
    }
}
