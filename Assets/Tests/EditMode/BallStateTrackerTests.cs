using System;
using CatTennis.BallPhysics.Core;
using NUnit.Framework;

namespace CatTennis.Rebuild.Tests
{
    public sealed class BallStateTrackerTests
    {
        [Test]
        public void ResetCreatesInactiveZeroVelocityState()
        {
            BallStateTracker tracker = new BallStateTracker();
            tracker.Reset(2f, 3f);

            Assert.That(tracker.Current.PositionX, Is.EqualTo(2f));
            Assert.That(tracker.Current.PositionY, Is.EqualTo(3f));
            Assert.That(tracker.Current.VelocityX, Is.Zero);
            Assert.That(tracker.Current.VelocityY, Is.Zero);
            Assert.That(tracker.Current.IsActive, Is.False);
            Assert.That(tracker.Current.StepIndex, Is.Zero);
        }

        [Test]
        public void LaunchActivatesWithoutChangingPositionOrStep()
        {
            BallStateTracker tracker = new BallStateTracker();
            tracker.Reset(2f, 3f);
            tracker.Launch(4f, 5f);

            Assert.That(tracker.Current.PositionX, Is.EqualTo(2f));
            Assert.That(tracker.Current.PositionY, Is.EqualTo(3f));
            Assert.That(tracker.Current.VelocityX, Is.EqualTo(4f));
            Assert.That(tracker.Current.VelocityY, Is.EqualTo(5f));
            Assert.That(tracker.Current.IsActive, Is.True);
            Assert.That(tracker.Current.StepIndex, Is.Zero);
        }

        [Test]
        public void ApplyMovesCurrentStateToPrevious()
        {
            BallStateTracker tracker = new BallStateTracker();
            tracker.Reset(1f, 2f);
            tracker.Launch(3f, 4f);
            BallSnapshot before = tracker.Current;
            BallSnapshot next = new BallSnapshot(2f, 3f, 3f, 3f, true, 1);

            tracker.Apply(new BallStepResult(next, false, false, false, 0f, 0f));

            Assert.That(tracker.Previous.PositionX, Is.EqualTo(before.PositionX));
            Assert.That(tracker.Current.PositionX, Is.EqualTo(2f));
            Assert.That(tracker.Current.StepIndex, Is.EqualTo(1));
        }

        [Test]
        public void StopZerosVelocityAndDeactivates()
        {
            BallStateTracker tracker = new BallStateTracker();
            tracker.Launch(3f, 4f);
            tracker.Stop();

            Assert.That(tracker.Current.VelocityX, Is.Zero);
            Assert.That(tracker.Current.VelocityY, Is.Zero);
            Assert.That(tracker.Current.IsActive, Is.False);
        }

        [Test]
        public void BackwardStepIsRejected()
        {
            BallStateTracker tracker = new BallStateTracker();
            tracker.Apply(new BallStepResult(
                new BallSnapshot(0f, 0f, 0f, 0f, true, 2),
                false,
                false,
                false,
                0f,
                0f));

            Assert.Throws<ArgumentException>(() => tracker.Apply(new BallStepResult(
                new BallSnapshot(0f, 0f, 0f, 0f, true, 1),
                false,
                false,
                false,
                0f,
                0f)));
        }

        [Test]
        public void NonFiniteCommandsAreRejected()
        {
            BallStateTracker tracker = new BallStateTracker();
            Assert.Throws<ArgumentException>(() => tracker.Reset(float.NaN, 0f));
            Assert.Throws<ArgumentException>(() => tracker.Launch(0f, float.PositiveInfinity));
        }
    }
}
