using System;
using System.Collections.Generic;
using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
using NUnit.Framework;
using UnityEngine;

namespace CatTennis.Rebuild.Tests
{
    public sealed class CourtZoneDetectorTests
    {
        private GameObject detectorObject;
        private CourtGeometryConfig geometry;
        private CourtZoneDetector detector;

        [SetUp]
        public void SetUp()
        {
            detectorObject = new GameObject("CourtZoneDetectorTests");
            detector = detectorObject.AddComponent<CourtZoneDetector>();
            geometry = ScriptableObject.CreateInstance<CourtGeometryConfig>();
            geometry.Configure(-10f, 10f, -8f, -0.5f, 0.5f, 8f, 0.05f);
            detector.Initialize(geometry);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(detectorObject);
            UnityEngine.Object.DestroyImmediate(geometry);
        }

        [TestCase(-8.04f, CourtArea.PlayerCourt)]
        [TestCase(-0.46f, CourtArea.PlayerCourt)]
        [TestCase(0f, CourtArea.Out)]
        [TestCase(0.46f, CourtArea.OpponentCourt)]
        [TestCase(8.04f, CourtArea.OpponentCourt)]
        [TestCase(9f, CourtArea.Out)]
        public void GroundContactUsesTolerantNonOverlappingCourtRanges(float x, CourtArea area)
        {
            IReadOnlyList<CourtObservation> observations = detector.Evaluate(Contact(1, x, false));

            Assert.That(observations, Has.Count.EqualTo(1));
            Assert.That(observations[0].Type, Is.EqualTo(CourtObservationType.GroundTouch));
            Assert.That(observations[0].CourtArea, Is.EqualTo(area));
        }

        [Test]
        public void SettleReturnsGroundThenSettledAndSuppressesBoundary()
        {
            IReadOnlyList<CourtObservation> observations = detector.Evaluate(Contact(1, 11f, true));

            Assert.That(observations, Has.Count.EqualTo(2));
            Assert.That(observations[0].Type, Is.EqualTo(CourtObservationType.GroundTouch));
            Assert.That(observations[1].Type, Is.EqualTo(CourtObservationType.BallSettled));
        }

        [Test]
        public void UpwardGroundCorrectionDoesNotEmitOrLatchGroundTouch()
        {
            BallStepResult correction = Result(1, -2f, 0.15f, 0f, 2f, true, false, false, true);
            Assert.That(detector.Evaluate(correction), Is.Empty);

            Assert.That(detector.Evaluate(Contact(2, -2f, false)), Has.Count.EqualTo(1));
        }

        [Test]
        public void GroundLatchReleasesOnlyAfterActiveBallLeavesGround()
        {
            Assert.That(detector.Evaluate(Contact(1, -2f, false)), Has.Count.EqualTo(1));
            Assert.That(detector.Evaluate(Contact(2, -2f, false)), Is.Empty);

            detector.Evaluate(Result(3, -2f, 1f, 0f, 1f, false, false, false, true));
            Assert.That(detector.Evaluate(Contact(4, -2f, false)), Has.Count.EqualTo(1));
        }

        [Test]
        public void SameOrOlderStepIsIgnored()
        {
            detector.Evaluate(Result(5, 0f, 1f, 0f, 0f, false, false, false, true));
            Assert.That(detector.Evaluate(Contact(5, -2f, false)), Is.Empty);
            Assert.That(detector.Evaluate(Contact(4, -2f, false)), Is.Empty);
        }

        [Test]
        public void BoundaryExitLatchesUntilBallReturnsInside()
        {
            Assert.That(detector.Evaluate(Result(1, 11f, 2f, 1f, 0f, false, false, false, true)), Has.Count.EqualTo(1));
            Assert.That(detector.Evaluate(Result(2, 12f, 2f, 1f, 0f, false, false, false, true)), Is.Empty);
            detector.Evaluate(Result(3, 9f, 2f, -1f, 0f, false, false, false, true));
            Assert.That(detector.Evaluate(Result(4, 11f, 2f, 1f, 0f, false, false, false, true)), Has.Count.EqualTo(1));
        }

        [Test]
        public void InvalidTolerantCourtOverlapFailsAtRuntime()
        {
            geometry.Configure(-10f, 10f, -8f, 0f, 0.05f, 8f, 0.1f);
            Assert.Throws<InvalidOperationException>(() => detector.Evaluate(
                Result(1, 0f, 1f, 0f, 0f, false, false, false, true)));
        }

        [Test]
        public void ResetClearsEveryLatchAndStepIndex()
        {
            detector.Evaluate(Contact(8, -2f, true));
            detector.ResetLatches();
            Assert.That(detector.Evaluate(Contact(1, -2f, true)), Has.Count.EqualTo(2));
        }

        private static BallStepResult Contact(long step, float x, bool settled)
        {
            return Result(step, x, 0.15f, 1f, settled ? 0f : 2f, true, !settled, settled, !settled);
        }

        private static BallStepResult Result(
            long step,
            float x,
            float y,
            float velocityX,
            float velocityY,
            bool hadGround,
            bool bounced,
            bool settled,
            bool active)
        {
            return new BallStepResult(
                new BallSnapshot(x, y, velocityX, velocityY, active, step),
                hadGround,
                bounced,
                settled,
                settled || bounced ? Math.Abs(velocityY) : 0f,
                hadGround ? 0f : 0f);
        }
    }
}
