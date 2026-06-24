using System;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
using NUnit.Framework;

namespace CatTennis.Rebuild.Tests
{
    public sealed class CourtRuleJudgeTests
    {
        private CourtRuleJudge judge;

        [SetUp]
        public void SetUp()
        {
            judge = new CourtRuleJudge();
        }

        [TestCase(HitterType.Player, CourtSide.Opponent)]
        [TestCase(HitterType.Opponent, CourtSide.Player)]
        public void InitialHitStartsPoint(HitterType hitter, CourtSide receiver)
        {
            RuleTransition transition = judge.Evaluate(RallyContext.Create(1), Hit(1, 1, hitter));

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.HasPointResult, Is.False);
            Assert.That(transition.NextContext.State, Is.EqualTo(RallyState.InFlight));
            Assert.That(transition.NextContext.LastHitter, Is.EqualTo(hitter));
            Assert.That(transition.NextContext.ExpectedReceiver, Is.EqualTo(receiver));
        }

        [TestCase(RuleEventType.GroundTouch)]
        [TestCase(RuleEventType.NetContact)]
        [TestCase(RuleEventType.NetStopped)]
        [TestCase(RuleEventType.BoundaryExit)]
        public void IdleRejectsNonHitEvents(RuleEventType type)
        {
            RallyContext context = RallyContext.Create(1);
            AssertRejected(context, judge.Evaluate(context, EventForType(1, 1, type)));
        }

        [TestCase(HitterType.Player, HitterType.Opponent)]
        [TestCase(HitterType.Opponent, HitterType.Player)]
        public void ReceiverCanReturnBeforeFirstBounce(HitterType firstHitter, HitterType receiver)
        {
            RuleTransition transition = judge.Evaluate(Start(firstHitter), Hit(1, 2, receiver));

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.HasPointResult, Is.False);
            Assert.That(transition.NextContext.State, Is.EqualTo(RallyState.InFlight));
            Assert.That(transition.NextContext.LastHitter, Is.EqualTo(receiver));
            Assert.That(transition.NextContext.BounceCount, Is.Zero);
        }

        [TestCase(HitterType.Player, HitterType.Opponent)]
        [TestCase(HitterType.Opponent, HitterType.Player)]
        public void ReceiverCanReturnAfterFirstBounce(HitterType firstHitter, HitterType receiver)
        {
            RallyContext context = FirstValidBounce(Start(firstHitter), 2);
            RuleTransition transition = judge.Evaluate(context, Hit(1, 3, receiver));

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.HasPointResult, Is.False);
            Assert.That(transition.NextContext.State, Is.EqualTo(RallyState.InFlight));
            Assert.That(transition.NextContext.LastHitter, Is.EqualTo(receiver));
            Assert.That(transition.NextContext.FirstBounceArea, Is.EqualTo(CourtArea.None));
        }

        [TestCase(HitterType.Player, false)]
        [TestCase(HitterType.Opponent, false)]
        [TestCase(HitterType.Player, true)]
        [TestCase(HitterType.Opponent, true)]
        public void SameHitterTwiceLosesByDoubleTouch(HitterType hitter, bool afterBounce)
        {
            RallyContext context = Start(hitter);
            if (afterBounce)
            {
                context = FirstValidBounce(context, 2);
            }

            RuleTransition transition = judge.Evaluate(
                context,
                Hit(1, afterBounce ? 3 : 2, hitter));

            AssertPoint(transition, Opposite(ToSide(hitter)), ToSide(hitter), FailureReason.DoubleTouch);
        }

        [TestCase(HitterType.Player, CourtArea.OpponentCourt)]
        [TestCase(HitterType.Opponent, CourtArea.PlayerCourt)]
        public void FirstBounceOnReceiverCourtContinues(HitterType hitter, CourtArea receiverArea)
        {
            RuleTransition transition = judge.Evaluate(Start(hitter), Ground(1, 2, receiverArea));

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.HasPointResult, Is.False);
            Assert.That(transition.NextContext.State, Is.EqualTo(RallyState.ReceiverCourtBounced));
            Assert.That(transition.NextContext.BounceCount, Is.EqualTo(1));
            Assert.That(transition.NextContext.FirstBounceArea, Is.EqualTo(receiverArea));
        }

        [TestCase(HitterType.Player, CourtArea.PlayerCourt)]
        [TestCase(HitterType.Opponent, CourtArea.OpponentCourt)]
        public void FirstBounceOnHitterCourtLosesByFailedToClear(HitterType hitter, CourtArea hitterArea)
        {
            RuleTransition transition = judge.Evaluate(Start(hitter), Ground(1, 2, hitterArea));
            AssertPoint(transition, Opposite(ToSide(hitter)), ToSide(hitter), FailureReason.FailedToClear);
        }

        [TestCase(HitterType.Player)]
        [TestCase(HitterType.Opponent)]
        public void FirstGroundOutLosesByOutBeforeValidBounce(HitterType hitter)
        {
            RuleTransition transition = judge.Evaluate(Start(hitter), Ground(1, 2, CourtArea.Out));
            AssertPoint(transition, Opposite(ToSide(hitter)), ToSide(hitter), FailureReason.OutBeforeValidBounce);
        }

        [TestCase(HitterType.Player)]
        [TestCase(HitterType.Opponent)]
        public void BoundaryExitBeforeValidBounceMakesHitterLose(HitterType hitter)
        {
            RuleTransition transition = judge.Evaluate(Start(hitter), Boundary(1, 2));
            AssertPoint(transition, Opposite(ToSide(hitter)), ToSide(hitter), FailureReason.OutBeforeValidBounce);
        }

        [TestCase(HitterType.Player, CourtArea.PlayerCourt)]
        [TestCase(HitterType.Player, CourtArea.OpponentCourt)]
        [TestCase(HitterType.Player, CourtArea.Out)]
        [TestCase(HitterType.Opponent, CourtArea.PlayerCourt)]
        [TestCase(HitterType.Opponent, CourtArea.OpponentCourt)]
        [TestCase(HitterType.Opponent, CourtArea.Out)]
        public void AnySecondGroundTouchLosesByDoubleBounce(HitterType hitter, CourtArea secondArea)
        {
            RallyContext context = FirstValidBounce(Start(hitter), 2);
            RuleTransition transition = judge.Evaluate(context, Ground(1, 3, secondArea));

            AssertPoint(transition, ToSide(hitter), Opposite(ToSide(hitter)), FailureReason.DoubleBounce);
        }

        [TestCase(HitterType.Player)]
        [TestCase(HitterType.Opponent)]
        public void BoundaryExitAfterValidBounceMakesReceiverLose(HitterType hitter)
        {
            RallyContext context = FirstValidBounce(Start(hitter), 2);
            RuleTransition transition = judge.Evaluate(context, Boundary(1, 3));

            AssertPoint(transition, ToSide(hitter), Opposite(ToSide(hitter)), FailureReason.UnreturnedAfterValidBounce);
        }

        [TestCase(HitterType.Player)]
        [TestCase(HitterType.Opponent)]
        public void BallSettledAfterValidBounceMakesReceiverLose(HitterType hitter)
        {
            RallyContext context = FirstValidBounce(Start(hitter), 2);
            RuleTransition transition = judge.Evaluate(context, Settled(1, 3));

            AssertPoint(
                transition,
                ToSide(hitter),
                Opposite(ToSide(hitter)),
                FailureReason.UnreturnedAfterValidBounce);
        }

        [Test]
        public void BallSettledBeforeValidBounceIsRejected()
        {
            RallyContext context = Start(HitterType.Player);
            AssertRejected(context, judge.Evaluate(context, Settled(1, 2)));
        }

        [TestCase(HitterType.Player, false)]
        [TestCase(HitterType.Opponent, false)]
        [TestCase(HitterType.Player, true)]
        [TestCase(HitterType.Opponent, true)]
        public void NetContactNeverEndsPoint(HitterType hitter, bool afterBounce)
        {
            RallyContext context = Start(hitter);
            if (afterBounce)
            {
                context = FirstValidBounce(context, 2);
            }

            RuleTransition transition = judge.Evaluate(
                context,
                NetContact(1, afterBounce ? 3 : 2));

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.HasPointResult, Is.False);
            Assert.That(transition.NextContext.State, Is.EqualTo(context.State));
            Assert.That(transition.NextContext.NetTouched, Is.True);
        }

        [TestCase(HitterType.Player)]
        [TestCase(HitterType.Opponent)]
        public void NetContactThenReceiverCourtBounceRemainsLegal(HitterType hitter)
        {
            RallyContext context = judge.Evaluate(Start(hitter), NetContact(1, 2)).NextContext;
            RuleTransition transition = judge.Evaluate(context, Ground(1, 3, ReceiverArea(hitter)));

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.HasPointResult, Is.False);
            Assert.That(transition.NextContext.State, Is.EqualTo(RallyState.ReceiverCourtBounced));
            Assert.That(transition.NextContext.NetTouched, Is.True);
        }

        [TestCase(HitterType.Player)]
        [TestCase(HitterType.Opponent)]
        public void NetContactThenHitterCourtBounceFailsToClear(HitterType hitter)
        {
            RallyContext context = judge.Evaluate(Start(hitter), NetContact(1, 2)).NextContext;
            RuleTransition transition = judge.Evaluate(context, Ground(1, 3, HitterArea(hitter)));

            AssertPoint(transition, Opposite(ToSide(hitter)), ToSide(hitter), FailureReason.FailedToClear);
        }

        [TestCase(HitterType.Player)]
        [TestCase(HitterType.Opponent)]
        public void NetStoppedBeforeValidBounceMakesLastHitterLose(HitterType hitter)
        {
            RuleTransition transition = judge.Evaluate(Start(hitter), NetStopped(1, 2));
            AssertPoint(transition, Opposite(ToSide(hitter)), ToSide(hitter), FailureReason.NetStopped);
        }

        [TestCase(HitterType.Player)]
        [TestCase(HitterType.Opponent)]
        public void NetStoppedAfterValidBounceMakesReceiverLose(HitterType hitter)
        {
            RallyContext context = FirstValidBounce(Start(hitter), 2);
            RuleTransition transition = judge.Evaluate(context, NetStopped(1, 3));

            AssertPoint(transition, ToSide(hitter), Opposite(ToSide(hitter)), FailureReason.NetStopped);
        }

        [Test]
        public void ReceiverHitResetsBounceAndNetMetadata()
        {
            RallyContext context = judge.Evaluate(Start(HitterType.Player), NetContact(1, 2)).NextContext;
            context = judge.Evaluate(context, Ground(1, 3, CourtArea.OpponentCourt)).NextContext;
            RuleTransition transition = judge.Evaluate(context, Hit(1, 4, HitterType.Opponent));

            Assert.That(transition.NextContext.State, Is.EqualTo(RallyState.InFlight));
            Assert.That(transition.NextContext.BounceCount, Is.Zero);
            Assert.That(transition.NextContext.FirstBounceArea, Is.EqualTo(CourtArea.None));
            Assert.That(transition.NextContext.NetTouched, Is.False);
        }

        [Test]
        public void EventFromDifferentPointIsIgnored()
        {
            RallyContext context = Start(HitterType.Player);
            AssertRejected(context, judge.Evaluate(context, Ground(2, 2, CourtArea.OpponentCourt)));
        }

        [Test]
        public void DuplicateOrOlderEventIsIgnored()
        {
            RallyContext context = judge.Evaluate(Start(HitterType.Player), NetContact(1, 5)).NextContext;

            AssertRejected(context, judge.Evaluate(context, Boundary(1, 5)));
            AssertRejected(context, judge.Evaluate(context, Boundary(1, 4)));
        }

        [Test]
        public void EndedPointIgnoresEveryEventType()
        {
            RallyContext context = judge.Evaluate(
                Start(HitterType.Player),
                Ground(1, 2, CourtArea.PlayerCourt)).NextContext;

            foreach (RuleEventType type in System.Enum.GetValues(typeof(RuleEventType)))
            {
                AssertRejected(context, judge.Evaluate(context, EventForType(1, 3, type)));
            }
        }

        [Test]
        public void InvalidEventPayloadsAreRejected()
        {
            RallyContext idle = RallyContext.Create(1);
            AssertRejected(idle, judge.Evaluate(idle, Hit(1, 1, HitterType.None)));

            RallyContext active = Start(HitterType.Player);
            AssertRejected(active, judge.Evaluate(active, Ground(1, 2, CourtArea.None)));
            AssertRejected(active, judge.Evaluate(active, new RuleEvent(1, 2, RuleEventType.BoundaryExit)));
            AssertRejected(active, judge.Evaluate(active, new RuleEvent(
                1,
                2,
                RuleEventType.Hit,
                HitterType.Opponent,
                CourtArea.PlayerCourt)));
            AssertRejected(active, judge.Evaluate(active, new RuleEvent(
                1,
                2,
                RuleEventType.GroundTouch,
                HitterType.Opponent,
                CourtArea.PlayerCourt)));
            AssertRejected(active, judge.Evaluate(active, new RuleEvent(
                1,
                2,
                RuleEventType.NetContact,
                boundaryType: BoundaryType.KillPlane)));
        }

        [Test]
        public void PointIdMustBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RallyContext.Create(0));
        }

        [Test]
        public void TerminalTransitionMaintainsResultInvariants()
        {
            RuleTransition transition = judge.Evaluate(
                Start(HitterType.Player),
                Ground(1, 2, CourtArea.PlayerCourt));

            Assert.That(transition.NextContext.PointEnded, Is.True);
            Assert.That(transition.PointResult.HasWinner, Is.True);
            Assert.That(transition.PointResult.Winner, Is.Not.EqualTo(transition.PointResult.Loser));
            Assert.That(transition.PointResult.PointId, Is.EqualTo(transition.NextContext.PointId));
            Assert.That(transition.PointResult.SourceEventId, Is.EqualTo(transition.NextContext.LastProcessedEventId));
        }

        private RallyContext Start(HitterType hitter)
        {
            return judge.Evaluate(RallyContext.Create(1), Hit(1, 1, hitter)).NextContext;
        }

        private RallyContext FirstValidBounce(RallyContext context, long eventId)
        {
            return judge.Evaluate(context, Ground(1, eventId, ReceiverArea(context.LastHitter))).NextContext;
        }

        private static RuleEvent Hit(long pointId, long eventId, HitterType hitter)
        {
            return new RuleEvent(pointId, eventId, RuleEventType.Hit, hitter);
        }

        private static RuleEvent Ground(long pointId, long eventId, CourtArea area)
        {
            return new RuleEvent(pointId, eventId, RuleEventType.GroundTouch, courtArea: area);
        }

        private static RuleEvent NetContact(long pointId, long eventId)
        {
            return new RuleEvent(pointId, eventId, RuleEventType.NetContact);
        }

        private static RuleEvent NetStopped(long pointId, long eventId)
        {
            return new RuleEvent(pointId, eventId, RuleEventType.NetStopped);
        }

        private static RuleEvent Boundary(long pointId, long eventId)
        {
            return new RuleEvent(
                pointId,
                eventId,
                RuleEventType.BoundaryExit,
                boundaryType: BoundaryType.KillPlane);
        }

        private static RuleEvent Settled(long pointId, long eventId)
        {
            return new RuleEvent(pointId, eventId, RuleEventType.BallSettled);
        }

        private static RuleEvent EventForType(long pointId, long eventId, RuleEventType type)
        {
            switch (type)
            {
                case RuleEventType.Hit:
                    return Hit(pointId, eventId, HitterType.Player);
                case RuleEventType.GroundTouch:
                    return Ground(pointId, eventId, CourtArea.PlayerCourt);
                case RuleEventType.BoundaryExit:
                    return Boundary(pointId, eventId);
                default:
                    return new RuleEvent(pointId, eventId, type);
            }
        }

        private static CourtArea ReceiverArea(HitterType hitter)
        {
            return hitter == HitterType.Player ? CourtArea.OpponentCourt : CourtArea.PlayerCourt;
        }

        private static CourtArea HitterArea(HitterType hitter)
        {
            return hitter == HitterType.Player ? CourtArea.PlayerCourt : CourtArea.OpponentCourt;
        }

        private static CourtSide ToSide(HitterType hitter)
        {
            return hitter == HitterType.Player ? CourtSide.Player : CourtSide.Opponent;
        }

        private static CourtSide Opposite(CourtSide side)
        {
            return side == CourtSide.Player ? CourtSide.Opponent : CourtSide.Player;
        }

        private static void AssertPoint(
            RuleTransition transition,
            CourtSide winner,
            CourtSide loser,
            FailureReason reason)
        {
            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.HasPointResult, Is.True);
            Assert.That(transition.NextContext.State, Is.EqualTo(RallyState.Ended));
            Assert.That(transition.PointResult.Winner, Is.EqualTo(winner));
            Assert.That(transition.PointResult.Loser, Is.EqualTo(loser));
            Assert.That(transition.PointResult.FailureReason, Is.EqualTo(reason));
        }

        private static void AssertRejected(RallyContext expected, RuleTransition transition)
        {
            Assert.That(transition.Accepted, Is.False);
            Assert.That(transition.HasPointResult, Is.False);
            Assert.That(transition.NextContext.PointId, Is.EqualTo(expected.PointId));
            Assert.That(transition.NextContext.LastProcessedEventId, Is.EqualTo(expected.LastProcessedEventId));
            Assert.That(transition.NextContext.State, Is.EqualTo(expected.State));
            Assert.That(transition.NextContext.LastHitter, Is.EqualTo(expected.LastHitter));
            Assert.That(transition.NextContext.ExpectedReceiver, Is.EqualTo(expected.ExpectedReceiver));
            Assert.That(transition.NextContext.FirstBounceArea, Is.EqualTo(expected.FirstBounceArea));
            Assert.That(transition.NextContext.NetTouched, Is.EqualTo(expected.NetTouched));
        }
    }
}
