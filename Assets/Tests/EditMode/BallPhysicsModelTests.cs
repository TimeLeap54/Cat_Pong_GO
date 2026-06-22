using System;
using System.Linq;
using CatTennis.BallPhysics.Core;
using NUnit.Framework;

namespace CatTennis.Rebuild.Tests
{
    public sealed class BallPhysicsModelTests
    {
        private const float Tolerance = 0.00001f;
        private BallPhysicsModel model;

        [SetUp]
        public void SetUp()
        {
            model = new BallPhysicsModel();
        }

        [Test]
        public void InactiveBallRemainsUnchanged()
        {
            BallSnapshot snapshot = Snapshot(2f, 3f, 4f, 5f, false, 7);
            BallStepResult result = model.Step(snapshot, Settings(), Input(0.1f, false));

            AssertSnapshot(result.NextSnapshot, snapshot);
            Assert.That(result.HadGroundContact, Is.False);
            Assert.That(result.DidBounce, Is.False);
            Assert.That(result.DidSettle, Is.False);
        }

        [Test]
        public void GravityAndPositionUseFixedSemiImplicitOrder()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 10f, 2f, 3f),
                Settings(),
                Input(0.1f, false));

            Assert.That(result.NextSnapshot.VelocityY, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(result.NextSnapshot.PositionX, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(result.NextSnapshot.PositionY, Is.EqualTo(10.2f).Within(Tolerance));
            Assert.That(result.NextSnapshot.StepIndex, Is.EqualTo(1));
        }

        [Test]
        public void AirborneHorizontalVelocityDoesNotDrift()
        {
            BallSnapshot current = Snapshot(0f, 100f, 3f, 0f);
            for (int i = 0; i < 100; i++)
            {
                current = model.Step(current, Settings(), Input(0.02f, false)).NextSnapshot;
            }

            Assert.That(current.VelocityX, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(current.PositionX, Is.EqualTo(6f).Within(0.0001f));
        }

        [TestCase(20f, 10f)]
        [TestCase(-20f, -10f)]
        public void HorizontalSpeedIsClamped(float initialVelocity, float expected)
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 10f, initialVelocity, 0f),
                Settings(),
                Input(0.1f, false));

            Assert.That(result.NextSnapshot.VelocityX, Is.EqualTo(expected));
        }

        [Test]
        public void RiseSpeedIsClampedBeforePositionIntegration()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 10f, 0f, 20f),
                Settings(),
                Input(0.1f, false));

            Assert.That(result.NextSnapshot.VelocityY, Is.EqualTo(8f));
            Assert.That(result.NextSnapshot.PositionY, Is.EqualTo(10.8f).Within(Tolerance));
        }

        [Test]
        public void FallSpeedIsClampedBeforePositionIntegration()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 10f, 0f, -20f),
                Settings(),
                Input(0.1f, false));

            Assert.That(result.NextSnapshot.VelocityY, Is.EqualTo(-12f));
            Assert.That(result.NextSnapshot.PositionY, Is.EqualTo(8.8f).Within(Tolerance));
        }

        [Test]
        public void DownwardGroundContactCorrectsPenetrationAndBounces()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 0.6f, 4f, -2f),
                Settings(),
                Input(0.1f, true, 0f));

            Assert.That(result.HadGroundContact, Is.True);
            Assert.That(result.DidBounce, Is.True);
            Assert.That(result.DidSettle, Is.False);
            Assert.That(result.ImpactSpeed, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(result.ContactY, Is.EqualTo(0f));
            Assert.That(result.NextSnapshot.PositionY, Is.EqualTo(0.5f));
            Assert.That(result.NextSnapshot.VelocityX, Is.EqualTo(3.2f).Within(Tolerance));
            Assert.That(result.NextSnapshot.VelocityY, Is.EqualTo(1.5f).Within(Tolerance));
        }

        [Test]
        public void GroundSkinAllowsEarlyDownwardContact()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 0.54f, 0f, 0f),
                Settings(),
                Input(0.001f, true, 0f));

            Assert.That(result.HadGroundContact, Is.True);
            Assert.That(result.NextSnapshot.PositionY, Is.EqualTo(0.5f));
        }

        [Test]
        public void UpwardBallInsideSkinButNotPenetratingIsIgnored()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 0.54f, 0f, 1f),
                Settings(),
                Input(0.01f, true, 0f));

            Assert.That(result.HadGroundContact, Is.False);
            Assert.That(result.DidBounce, Is.False);
            Assert.That(result.NextSnapshot.PositionY, Is.GreaterThan(0.54f));
        }

        [Test]
        public void UpwardPenetrationCorrectsPositionWithoutBounce()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 0.4f, 2f, 1f),
                Settings(),
                Input(0.01f, true, 0f));

            Assert.That(result.HadGroundContact, Is.True);
            Assert.That(result.DidBounce, Is.False);
            Assert.That(result.DidSettle, Is.False);
            Assert.That(result.ImpactSpeed, Is.Zero);
            Assert.That(result.ContactY, Is.Zero);
            Assert.That(result.NextSnapshot.PositionY, Is.EqualTo(0.5f));
            Assert.That(result.NextSnapshot.VelocityX, Is.EqualTo(2f));
            Assert.That(result.NextSnapshot.VelocityY, Is.EqualTo(0.9f).Within(Tolerance));
        }

        [Test]
        public void LowReboundSettlesAndPreservesImpactSpeed()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 0.5f, 4f, 0f),
                Settings(minBounceSpeed: 1f),
                Input(0.02f, true, 0f));

            Assert.That(result.HadGroundContact, Is.True);
            Assert.That(result.DidSettle, Is.True);
            Assert.That(result.DidBounce, Is.False);
            Assert.That(result.ImpactSpeed, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(result.NextSnapshot.IsActive, Is.False);
            Assert.That(result.NextSnapshot.VelocityX, Is.Zero);
            Assert.That(result.NextSnapshot.VelocityY, Is.Zero);
        }

        [Test]
        public void SettledBallDoesNotBounceAgain()
        {
            BallStepResult first = model.Step(
                Snapshot(0f, 0.5f, 0f, 0f),
                Settings(minBounceSpeed: 1f),
                Input(0.02f, true, 0f));
            BallStepResult second = model.Step(
                first.NextSnapshot,
                Settings(minBounceSpeed: 1f),
                Input(0.02f, true, 0f));

            Assert.That(second.HadGroundContact, Is.False);
            Assert.That(second.DidBounce, Is.False);
            Assert.That(second.DidSettle, Is.False);
            AssertSnapshot(second.NextSnapshot, first.NextSnapshot);
        }

        [Test]
        public void ReboundIsClampedToMaximumRiseSpeed()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 1f, 0f, -20f),
                Settings(restitution: 1f, maxRiseSpeed: 4f),
                Input(0.1f, true, 0f));

            Assert.That(result.DidBounce, Is.True);
            Assert.That(result.NextSnapshot.VelocityY, Is.EqualTo(4f));
        }

        [Test]
        public void NoGroundContactUsesNeutralContactMetadata()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 10f, 0f, 0f),
                Settings(),
                Input(0.02f, true, 0f));

            Assert.That(result.HadGroundContact, Is.False);
            Assert.That(result.ContactY, Is.Zero);
            Assert.That(result.ImpactSpeed, Is.Zero);
        }

        [Test]
        public void ContactYUsesActualNonZeroGroundHeight()
        {
            BallStepResult result = model.Step(
                Snapshot(0f, 3f, 0f, -2f),
                Settings(),
                Input(0.1f, true, 2.5f));

            Assert.That(result.HadGroundContact, Is.True);
            Assert.That(result.ContactY, Is.EqualTo(2.5f));
            Assert.That(result.NextSnapshot.PositionY, Is.EqualTo(3f));
        }

        [Test]
        public void SameInitialConditionsProduceSameTenThousandStepTrajectory()
        {
            BallSnapshot first = Snapshot(1f, 5f, 3f, 7f);
            BallSnapshot second = first;
            BallPhysicsSettings settings = Settings();
            BallStepInput input = Input(0.02f, false);

            for (int i = 0; i < 10000; i++)
            {
                first = model.Step(first, settings, input).NextSnapshot;
                second = model.Step(second, settings, input).NextSnapshot;
            }

            AssertSnapshot(first, second);
            Assert.That(float.IsNaN(first.PositionX) || float.IsInfinity(first.PositionX), Is.False);
            Assert.That(float.IsNaN(first.PositionY) || float.IsInfinity(first.PositionY), Is.False);
        }

        [Test]
        public void StepDoesNotMutateInputSnapshot()
        {
            BallSnapshot original = Snapshot(1f, 2f, 3f, 4f, true, 9);
            model.Step(original, Settings(), Input(0.02f, false));

            Assert.That(original.PositionX, Is.EqualTo(1f));
            Assert.That(original.PositionY, Is.EqualTo(2f));
            Assert.That(original.VelocityX, Is.EqualTo(3f));
            Assert.That(original.VelocityY, Is.EqualTo(4f));
            Assert.That(original.StepIndex, Is.EqualTo(9));
        }

        [Test]
        public void InactiveBallStillRejectsInvalidInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => model.Step(
                Snapshot(0f, 0f, 0f, 0f, false),
                Settings(),
                Input(0f, false)));
        }

        [TestCase(0f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidFixedDeltaTimeIsRejected(float deltaTime)
        {
            Assert.Catch<ArgumentException>(() => model.Step(
                Snapshot(0f, 1f, 0f, 0f),
                Settings(),
                Input(deltaTime, false)));
        }

        [Test]
        public void NonFiniteGroundIsRejectedEvenWhenDisabled()
        {
            Assert.Throws<ArgumentException>(() => model.Step(
                Snapshot(0f, 1f, 0f, 0f),
                Settings(),
                Input(0.02f, false, float.NaN)));
        }

        [Test]
        public void NonFiniteSnapshotIsRejected()
        {
            Assert.Throws<ArgumentException>(() => model.Step(
                Snapshot(float.NaN, 1f, 0f, 0f),
                Settings(),
                Input(0.02f, false)));
        }

        [Test]
        public void NonFiniteSettingsAreRejected()
        {
            BallPhysicsSettings settings = new BallPhysicsSettings(
                float.NaN,
                0.5f,
                0.5f,
                0.8f,
                10f,
                8f,
                12f,
                0.1f,
                0.05f);

            Assert.Throws<ArgumentException>(() => model.Step(
                Snapshot(0f, 1f, 0f, 0f),
                settings,
                Input(0.02f, false)));
        }

        [Test]
        public void NegativeStepIndexIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => model.Step(
                Snapshot(0f, 1f, 0f, 0f, true, -1),
                Settings(),
                Input(0.02f, false)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void InvalidSettingsAreRejected(int invalidField)
        {
            BallPhysicsSettings settings = InvalidSettings(invalidField);
            Assert.Catch<ArgumentException>(() => model.Step(
                Snapshot(0f, 1f, 0f, 0f),
                settings,
                Input(0.02f, false)));
        }

        [Test]
        public void CoreAssemblyHasNoUnityOrGameplayAssemblyReferences()
        {
            string[] references = typeof(BallPhysicsModel)
                .Assembly
                .GetReferencedAssemblies()
                .Select(assembly => assembly.Name)
                .ToArray();

            Assert.That(references, Does.Not.Contain("UnityEngine"));
            Assert.That(references, Does.Not.Contain("UnityEngine.CoreModule"));
            Assert.That(references, Does.Not.Contain("CatTennis.Rebuild"));
        }

        private static BallPhysicsSettings InvalidSettings(int field)
        {
            return new BallPhysicsSettings(
                field == 0 ? 0f : -10f,
                field == 1 ? 0f : 0.5f,
                field == 2 ? 1.1f : 0.5f,
                field == 3 ? -0.1f : 0.8f,
                field == 4 ? 0f : 10f,
                field == 5 ? 0f : 8f,
                field == 6 ? 0f : 12f,
                field == 7 ? -1f : 0.1f,
                field == 8 ? 0.5f : 0.05f);
        }

        private static BallSnapshot Snapshot(
            float px,
            float py,
            float vx,
            float vy,
            bool active = true,
            long step = 0)
        {
            return new BallSnapshot(px, py, vx, vy, active, step);
        }

        private static BallStepInput Input(float deltaTime, bool ground, float groundY = 0f)
        {
            return new BallStepInput(deltaTime, ground, groundY);
        }

        private static BallPhysicsSettings Settings(
            float restitution = 0.5f,
            float maxRiseSpeed = 8f,
            float minBounceSpeed = 0.1f)
        {
            return new BallPhysicsSettings(
                -10f,
                0.5f,
                restitution,
                0.8f,
                10f,
                maxRiseSpeed,
                12f,
                minBounceSpeed,
                0.05f);
        }

        private static void AssertSnapshot(BallSnapshot actual, BallSnapshot expected)
        {
            Assert.That(actual.PositionX, Is.EqualTo(expected.PositionX).Within(Tolerance));
            Assert.That(actual.PositionY, Is.EqualTo(expected.PositionY).Within(Tolerance));
            Assert.That(actual.VelocityX, Is.EqualTo(expected.VelocityX).Within(Tolerance));
            Assert.That(actual.VelocityY, Is.EqualTo(expected.VelocityY).Within(Tolerance));
            Assert.That(actual.IsActive, Is.EqualTo(expected.IsActive));
            Assert.That(actual.StepIndex, Is.EqualTo(expected.StepIndex));
        }
    }
}
