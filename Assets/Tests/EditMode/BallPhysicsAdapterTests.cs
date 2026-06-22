using CatTennis.BallPhysics.Core;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Config;
using NUnit.Framework;
using UnityEngine;

namespace CatTennis.Rebuild.Tests
{
    public sealed class BallPhysicsAdapterTests
    {
        private GameObject ballObject;
        private BallPhysicsConfig config;

        [TearDown]
        public void TearDown()
        {
            if (ballObject != null)
            {
                Object.DestroyImmediate(ballObject);
            }

            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ApplierConfiguresRigidbodyAsOneWayKinematicView()
        {
            BallPhysicsApplier applier = CreateApplier();
            Rigidbody2D body = ballObject.GetComponent<Rigidbody2D>();

            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(body.gravityScale, Is.Zero);
            Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));
            Assert.That((body.constraints & RigidbodyConstraints2D.FreezeRotation) != 0, Is.True);
            Assert.That(applier.CurrentSnapshot.IsActive, Is.False);
        }

        [Test]
        public void StepMirrorsSnapshotPositionAndIgnoresRigidbodyVelocity()
        {
            BallPhysicsApplier applier = CreateApplier();
            Rigidbody2D body = ballObject.GetComponent<Rigidbody2D>();
            applier.ResetBall(new Vector2(0f, 10f));
            applier.Launch(new Vector2(1f, 2f));
            body.velocity = new Vector2(99f, 99f);

            BallStepResult result = applier.StepOnce(0.1f);

            Assert.That(result.NextSnapshot.VelocityX, Is.EqualTo(1f).Within(0.00001f));
            Assert.That(body.position.x, Is.EqualTo(result.NextSnapshot.PositionX).Within(0.00001f));
            Assert.That(body.position.y, Is.EqualTo(result.NextSnapshot.PositionY).Within(0.00001f));
            Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ControllerDelegatesLifecycleWithoutCalculatingPhysics()
        {
            CreateApplier();
            BallController controller = ballObject.AddComponent<BallController>();

            controller.ResetBall(new Vector2(2f, 3f));
            controller.Launch(new Vector2(4f, 5f));
            Assert.That(controller.CurrentSnapshot.PositionX, Is.EqualTo(2f));
            Assert.That(controller.CurrentSnapshot.VelocityX, Is.EqualTo(4f));
            Assert.That(controller.CurrentSnapshot.IsActive, Is.True);

            controller.StopBall();
            Assert.That(controller.CurrentSnapshot.IsActive, Is.False);
            Assert.That(controller.CurrentSnapshot.VelocityX, Is.Zero);
            Assert.That(controller.CurrentSnapshot.VelocityY, Is.Zero);
        }

        [Test]
        public void DefaultConfigProducesSettingsAcceptedByCore()
        {
            config = ScriptableObject.CreateInstance<BallPhysicsConfig>();
            BallPhysicsModel model = new BallPhysicsModel();

            Assert.DoesNotThrow(() => model.Step(
                new BallSnapshot(0f, 1f, 0f, 0f, true, 0),
                config.CreateSettings(),
                new BallStepInput(0.02f, false, 0f)));
        }

        [Test]
        public void RepeatedIdenticalLaunchesProduceIdenticalAdapterResults()
        {
            BallPhysicsApplier applier = CreateApplier();
            BallSnapshot expected = default;

            for (int run = 0; run < 10; run++)
            {
                applier.ResetBall(new Vector2(1f, 5f));
                applier.Launch(new Vector2(3f, 7f));
                BallSnapshot actual = applier.StepOnce(0.02f).NextSnapshot;
                if (run == 0)
                {
                    expected = actual;
                }
                else
                {
                    Assert.That(actual.PositionX, Is.EqualTo(expected.PositionX));
                    Assert.That(actual.PositionY, Is.EqualTo(expected.PositionY));
                    Assert.That(actual.VelocityX, Is.EqualTo(expected.VelocityX));
                    Assert.That(actual.VelocityY, Is.EqualTo(expected.VelocityY));
                    Assert.That(actual.StepIndex, Is.EqualTo(expected.StepIndex));
                }
            }
        }

        private BallPhysicsApplier CreateApplier()
        {
            ballObject = new GameObject("Phase2Ball");
            ballObject.AddComponent<Rigidbody2D>();
            BallPhysicsApplier applier = ballObject.AddComponent<BallPhysicsApplier>();
            config = ScriptableObject.CreateInstance<BallPhysicsConfig>();
            applier.Configure(config, false, 0f);
            return applier;
        }
    }
}
