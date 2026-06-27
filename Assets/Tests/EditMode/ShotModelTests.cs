using System;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using NUnit.Framework;
using UnityEngine;

namespace CatTennis.Rebuild.Tests
{
    public sealed class ShotModelTests
    {
        private readonly ShotModel model = new ShotModel();
        private ShotSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = new ShotSettings(-9.81f, 0.15f, -8f, -0.5f, 0.5f, 8f,
                0f, 1f, 0.1f, 30f, 30f, 30f, 0.25f, 0.4f,
                P(0.55f, 2.3f, .9f), P(.88f, 2.1f, .85f),
                P(.12f, 1.35f, .75f), P(.72f, 4.2f, 1.2f),
                P(.72f, .2f, .62f), P(.72f, 2.5f, .95f));
        }

        [TestCase(ShotIntent.SafeReturn)]
        [TestCase(ShotIntent.Deep)]
        [TestCase(ShotIntent.Drop)]
        [TestCase(ShotIntent.Lob)]
        [TestCase(ShotIntent.Smash)]
        [TestCase(ShotIntent.Serve)]
        public void SameRequestAlwaysProducesSameTrajectory(ShotIntent intent)
        {
            ShotRequest request = Request(intent, 1);
            ShotResult first = model.Resolve(request, settings);
            Assert.That(first.IsValid, Is.True, first.InvalidReason.ToString());
            for (int run = 0; run < 20; run++)
            {
                ShotResult actual = model.Resolve(request, settings);
                Assert.That(actual.VelocityX, Is.EqualTo(first.VelocityX));
                Assert.That(actual.VelocityY, Is.EqualTo(first.VelocityY));
            }
        }

        [Test]
        public void IntentProducesOrderedLandingAndApexDifferences()
        {
            ShotResult drop = model.Resolve(Request(ShotIntent.Drop, 1), settings);
            ShotResult safe = model.Resolve(Request(ShotIntent.SafeReturn, 1), settings);
            ShotResult deep = model.Resolve(Request(ShotIntent.Deep, 1), settings);
            ShotResult lob = model.Resolve(Request(ShotIntent.Lob, 1), settings);
            Assert.That(drop.PredictedLandingX, Is.LessThan(safe.PredictedLandingX));
            Assert.That(safe.PredictedLandingX, Is.LessThan(deep.PredictedLandingX));
            Assert.That(lob.ApexY, Is.GreaterThan(safe.ApexY));
        }

        [Test]
        public void IntentResolverUsesFacingAndVerticalPriority()
        {
            var resolver = new ShotIntentResolver();
            Assert.That(resolver.Resolve(new Vector2(1f, 1f), 1, false, .25f, .4f),
                Is.EqualTo(ShotIntent.Lob));
            Assert.That(resolver.Resolve(Vector2.right, -1, false, .25f, .4f),
                Is.EqualTo(ShotIntent.Drop));
            Assert.That(resolver.Resolve(Vector2.left, -1, false, .25f, .4f),
                Is.EqualTo(ShotIntent.Deep));
        }

        [Test]
        public void MismatchedBallStepIsRejectedAtRequestBoundary()
        {
            BallSnapshot ball = new BallSnapshot(1f, 2f, 0f, 0f, true, 4);
            Assert.Throws<ArgumentException>(() => new ShotRequest(
                1, 3, HitterType.Player, ShotIntent.SafeReturn, ball,
                0f, 0f, 1f, 2f, 1));
        }

        [Test]
        public void ImpossibleNetClearanceReturnsInvalidInsteadOfLaunching()
        {
            ShotSettings blocked = new ShotSettings(-9.81f, .15f, -8f, -.5f, .5f, 8f,
                0f, 20f, 1f, 30f, 30f, 30f, .25f, .4f,
                P(.55f, 2.3f, .9f), P(.88f, 2.1f, .85f), P(.12f, 1.35f, .75f),
                P(.72f, 4.2f, 1.2f), P(.72f, .2f, .62f), P(.72f, 2.5f, .95f));
            ShotResult result = model.Resolve(Request(ShotIntent.SafeReturn, 1), blocked);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.InvalidReason, Is.EqualTo(FailureReason.FailedToClear));
        }

        private static ShotRequest Request(ShotIntent intent, int facing)
        {
            BallSnapshot ball = new BallSnapshot(-2f * facing, 2.2f, 0f, 0f, true, 4);
            return new ShotRequest(1, 4, facing == 1 ? HitterType.Player : HitterType.Opponent,
                intent, ball, 0f, 0f, ball.PositionX, ball.PositionY, facing);
        }

        private static ShotProfileSettings P(float ratio, float apex, float time) =>
            new ShotProfileSettings(ratio, apex, time);
    }
}
