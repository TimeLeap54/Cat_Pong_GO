using System;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using NUnit.Framework;

namespace CatTennis.Rebuild.Tests
{
    public sealed class ShotModelTests
    {
        private readonly ShotModel model = new ShotModel();
        private readonly ShotSettings settings = new ShotSettings(6f, 5f, 12f, -3f, 10f, 8f, 7f);

        [TestCase(ShotIntent.SafeReturn, 6f, 5f)]
        [TestCase(ShotIntent.Smash, 10f, -3f)]
        public void SameRequestAlwaysProducesSameClampedVelocity(
            ShotIntent intent,
            float expectedX,
            float expectedY)
        {
            ShotRequest request = Request(intent, 1);
            ShotResult first = model.Resolve(request, settings);

            for (int run = 0; run < 20; run++)
            {
                ShotResult actual = model.Resolve(request, settings);
                Assert.That(actual.VelocityX, Is.EqualTo(first.VelocityX));
                Assert.That(actual.VelocityY, Is.EqualTo(first.VelocityY));
            }

            Assert.That(first.VelocityX, Is.EqualTo(expectedX));
            Assert.That(first.VelocityY, Is.EqualTo(expectedY));
        }

        [Test]
        public void FacingLeftReversesOnlyHorizontalDirection()
        {
            ShotResult result = model.Resolve(Request(ShotIntent.SafeReturn, -1), settings);
            Assert.That(result.VelocityX, Is.EqualTo(-6f));
            Assert.That(result.VelocityY, Is.EqualTo(5f));
        }

        [Test]
        public void MismatchedBallStepIsRejectedAtRequestBoundary()
        {
            BallSnapshot ball = new BallSnapshot(1f, 2f, 0f, 0f, true, 4);
            Assert.Throws<ArgumentException>(() => new ShotRequest(
                1, 3, HitterType.Player, ShotIntent.SafeReturn, ball,
                0f, 0f, 1f, 2f, 1));
        }

        private static ShotRequest Request(ShotIntent intent, int facing)
        {
            BallSnapshot ball = new BallSnapshot(1f, 2f, -1f, 0f, true, 4);
            return new ShotRequest(
                1, 4, HitterType.Player, intent, ball,
                0f, 0f, 1f, 2f, facing);
        }
    }
}
