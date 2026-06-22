using System;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
using NUnit.Framework;

namespace CatTennis.Rebuild.Tests
{
    public sealed class ScoreSystemTests
    {
        [TestCase(CourtSide.Player)]
        [TestCase(CourtSide.Opponent)]
        public void ValidPointAddsExactlyOneScore(CourtSide winner)
        {
            ScoreSystem score = new ScoreSystem();

            Assert.That(score.TryApplyPoint(Result(1, winner)), Is.True);
            Assert.That(score.PlayerScore, Is.EqualTo(winner == CourtSide.Player ? 1 : 0));
            Assert.That(score.OpponentScore, Is.EqualTo(winner == CourtSide.Opponent ? 1 : 0));
        }

        [Test]
        public void SamePointIdCannotScoreTwice()
        {
            ScoreSystem score = new ScoreSystem();

            Assert.That(score.TryApplyPoint(Result(1, CourtSide.Player)), Is.True);
            Assert.That(score.TryApplyPoint(Result(1, CourtSide.Opponent)), Is.False);
            Assert.That(score.PlayerScore, Is.EqualTo(1));
            Assert.That(score.OpponentScore, Is.Zero);
        }

        [Test]
        public void OlderPointIdIsRejected()
        {
            ScoreSystem score = new ScoreSystem();
            score.TryApplyPoint(Result(2, CourtSide.Player));

            Assert.That(score.TryApplyPoint(Result(1, CourtSide.Opponent)), Is.False);
            Assert.That(score.PlayerScore, Is.EqualTo(1));
            Assert.That(score.OpponentScore, Is.Zero);
        }

        [Test]
        public void InvalidPointResultsAreRejected()
        {
            ScoreSystem score = new ScoreSystem();
            PointResult sameSides = new PointResult(
                1,
                1,
                CourtSide.Player,
                CourtSide.Player,
                FailureReason.DoubleBounce);
            PointResult noWinner = new PointResult(
                2,
                2,
                CourtSide.None,
                CourtSide.Player,
                FailureReason.InvalidEvent);
            PointResult noFailure = new PointResult(
                3,
                3,
                CourtSide.Player,
                CourtSide.Opponent,
                FailureReason.None);
            PointResult invalidFailure = new PointResult(
                4,
                4,
                CourtSide.Player,
                CourtSide.Opponent,
                FailureReason.InvalidEvent);

            Assert.That(score.TryApplyPoint(sameSides), Is.False);
            Assert.That(score.TryApplyPoint(noWinner), Is.False);
            Assert.That(score.TryApplyPoint(noFailure), Is.False);
            Assert.That(score.TryApplyPoint(invalidFailure), Is.False);
            Assert.That(score.PlayerScore + score.OpponentScore, Is.Zero);
        }

        [TestCase(CourtSide.Player)]
        [TestCase(CourtSide.Opponent)]
        public void FirstToFiveEndsMatch(CourtSide winner)
        {
            ScoreSystem score = new ScoreSystem();
            for (long pointId = 1; pointId <= 5; pointId++)
            {
                Assert.That(score.TryApplyPoint(Result(pointId, winner)), Is.True);
            }

            Assert.That(score.IsMatchEnded, Is.True);
            Assert.That(score.MatchWinner, Is.EqualTo(winner));
        }

        [Test]
        public void EndedMatchRejectsAdditionalPoints()
        {
            ScoreSystem score = new ScoreSystem(1);
            score.TryApplyPoint(Result(1, CourtSide.Player));

            Assert.That(score.TryApplyPoint(Result(2, CourtSide.Opponent)), Is.False);
            Assert.That(score.PlayerScore, Is.EqualTo(1));
            Assert.That(score.OpponentScore, Is.Zero);
        }

        [Test]
        public void ResetMatchClearsScoreButPreservesPointDeduplication()
        {
            ScoreSystem score = new ScoreSystem(1);
            score.TryApplyPoint(Result(10, CourtSide.Player));
            score.ResetMatch();

            Assert.That(score.PlayerScore, Is.Zero);
            Assert.That(score.OpponentScore, Is.Zero);
            Assert.That(score.IsMatchEnded, Is.False);
            Assert.That(score.LastScoredPointId, Is.EqualTo(10));
            Assert.That(score.TryApplyPoint(Result(10, CourtSide.Opponent)), Is.False);
            Assert.That(score.TryApplyPoint(Result(11, CourtSide.Opponent)), Is.True);
        }

        [Test]
        public void TargetScoreMustBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ScoreSystem(0));
        }

        private static PointResult Result(long pointId, CourtSide winner)
        {
            CourtSide loser = winner == CourtSide.Player
                ? CourtSide.Opponent
                : CourtSide.Player;
            return new PointResult(
                pointId,
                pointId,
                winner,
                loser,
                FailureReason.DoubleBounce);
        }
    }
}
