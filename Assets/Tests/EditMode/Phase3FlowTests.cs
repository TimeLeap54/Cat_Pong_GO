using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.State;
using NUnit.Framework;
using UnityEngine;

namespace CatTennis.Rebuild.Tests
{
    public sealed class Phase3FlowTests
    {
        private GameObject root;
        private Phase3PointLoopConfig config;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Phase3FlowTests");
            config = ScriptableObject.CreateInstance<Phase3PointLoopConfig>();
            config.Configure(5, 0f, HitterType.Player, new Vector2(-4f, 1f), new Vector2(4f, 6f));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void BeginPointAlwaysAdvancesGlobalPointIdAndReplacesContext()
        {
            RallyFlowManager rally = root.AddComponent<RallyFlowManager>();
            RallyContext first = rally.BeginPoint();
            RallyContext second = rally.BeginPoint();

            Assert.That(second.PointId, Is.EqualTo(first.PointId + 1));
            Assert.That(second.State, Is.EqualTo(RallyState.Idle));
        }

        [Test]
        public void FirstBounceContinuesAndSecondBounceEmitsOnlyOneResult()
        {
            RallyFlowManager rally = root.AddComponent<RallyFlowManager>();
            rally.BeginPoint();
            Assert.That(rally.RegisterHit(HitterType.Player, out _), Is.True);

            Assert.That(rally.ProcessObservation(Ground(CourtArea.OpponentCourt, 1), out PointResult first), Is.True);
            Assert.That(first.HasWinner, Is.False);
            Assert.That(rally.CurrentContext.State, Is.EqualTo(RallyState.ReceiverCourtBounced));

            Assert.That(rally.ProcessObservation(Ground(CourtArea.OpponentCourt, 2), out PointResult second), Is.True);
            Assert.That(second.Winner, Is.EqualTo(CourtSide.Player));
            Assert.That(second.FailureReason, Is.EqualTo(FailureReason.DoubleBounce));
            Assert.That(rally.ProcessObservation(Ground(CourtArea.OpponentCourt, 3), out _), Is.False);
        }

        [Test]
        public void SettledAfterValidBounceUsesUnreturnedReason()
        {
            RallyFlowManager rally = root.AddComponent<RallyFlowManager>();
            rally.BeginPoint();
            rally.RegisterHit(HitterType.Player, out _);
            rally.ProcessObservation(Ground(CourtArea.OpponentCourt, 1), out _);

            Assert.That(rally.ProcessObservation(
                new CourtObservation(CourtObservationType.BallSettled, 2),
                out PointResult result), Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(FailureReason.UnreturnedAfterValidBounce));
        }

        [Test]
        public void MatchFlowEndsAtFiveAndRejectsFurtherResults()
        {
            MatchFlowManager match = root.AddComponent<MatchFlowManager>();
            match.Initialize(config);

            for (int point = 1; point <= 5; point++)
            {
                Assert.That(match.TryApplyPoint(Result(point, CourtSide.Player)), Is.True);
            }

            Assert.That(match.PlayerScore, Is.EqualTo(5));
            Assert.That(match.MatchEnded, Is.True);
            Assert.That(match.TryApplyPoint(Result(6, CourtSide.Player)), Is.False);
        }

        [Test]
        public void RetryResetsScoreWithoutAllowingOldPointIdAgain()
        {
            MatchFlowManager match = root.AddComponent<MatchFlowManager>();
            match.Initialize(config);
            Assert.That(match.TryApplyPoint(Result(4, CourtSide.Player)), Is.True);
            match.ResetMatch();

            Assert.That(match.PlayerScore, Is.Zero);
            Assert.That(match.TryApplyPoint(Result(4, CourtSide.Player)), Is.False);
            Assert.That(match.TryApplyPoint(Result(5, CourtSide.Opponent)), Is.True);
        }

        private static CourtObservation Ground(CourtArea area, long step)
        {
            return new CourtObservation(CourtObservationType.GroundTouch, step, area);
        }

        private static PointResult Result(long pointId, CourtSide winner)
        {
            CourtSide loser = winner == CourtSide.Player ? CourtSide.Opponent : CourtSide.Player;
            return new PointResult(pointId, 2, winner, loser, FailureReason.DoubleBounce);
        }
    }
}
