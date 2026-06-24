using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.Shot;
using CatTennis.Rebuild.State;
using NUnit.Framework;
using UnityEngine;

namespace CatTennis.Rebuild.Tests
{
    public sealed class Phase4AdapterTests
    {
        private GameObject root;
        private BallPhysicsConfig physicsConfig;
        private CourtGeometryConfig geometryConfig;
        private Phase3PointLoopConfig loopConfig;
        private PlayerControlConfig playerConfig;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Phase4AdapterTests");
            root.SetActive(false);
            physicsConfig = ScriptableObject.CreateInstance<BallPhysicsConfig>();
            geometryConfig = ScriptableObject.CreateInstance<CourtGeometryConfig>();
            geometryConfig.Configure(-10f, 10f, -8f, -0.5f, 0.5f, 8f, 0.01f);
            playerConfig = ScriptableObject.CreateInstance<PlayerControlConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(physicsConfig);
            Object.DestroyImmediate(geometryConfig);
            Object.DestroyImmediate(loopConfig);
            Object.DestroyImmediate(playerConfig);
        }

        [Test]
        public void InputEdgesAreConsumedOnceWhileMoveValuePersists()
        {
            PlayerInputReader reader = root.AddComponent<PlayerInputReader>();
            reader.InjectDebugFrame(new PlayerInputFrame(1f, true, true, true));

            PlayerInputFrame first = reader.ConsumeFrame();
            PlayerInputFrame second = reader.ConsumeFrame();

            Assert.That(first.JumpPressed && first.SwingPressed && first.SmashPressed, Is.True);
            Assert.That(second.JumpPressed || second.SwingPressed || second.SmashPressed, Is.False);
            Assert.That(second.MoveX, Is.EqualTo(1f));
        }

        [Test]
        public void SuccessfulHitConsumesSwingIdAndEmitsOneRequest()
        {
            BallController ball = CreateBall();
            ball.ResetBall(new Vector2(1f, 1f));
            ball.Launch(Vector2.zero);
            PlayerHitDetector detector = root.AddComponent<PlayerHitDetector>();
            detector.Initialize(ball, playerConfig);
            int requests = 0;
            ShotRequest captured = default;
            detector.OnShotRequested += request => { requests++; captured = request; };
            PlayerActionFrame active = new PlayerActionFrame(
                LocomotionState.Grounded,
                SwingState.NormalActive,
                SwingKind.Normal,
                1,
                false,
                0f);

            Assert.That(detector.Evaluate(active, Vector2.zero, 1), Is.True);
            Assert.That(detector.Evaluate(active, Vector2.zero, 1), Is.False);
            Assert.That(requests, Is.EqualTo(1));
            Assert.That(captured.Intent, Is.EqualTo(ShotIntent.SafeReturn));
        }

        [Test]
        public void PlayerBodyAndBallLayersAreIgnored()
        {
            BallController ball = CreateBall();
            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CapsuleCollider2D>();
            PlayerInputReader reader = player.AddComponent<PlayerInputReader>();
            PlayerHitDetector detector = player.AddComponent<PlayerHitDetector>();
            detector.Initialize(ball, playerConfig);
            PlayerCatController controller = player.AddComponent<PlayerCatController>();
            controller.Initialize(reader, detector, playerConfig);

            Assert.That(player.layer, Is.EqualTo(playerConfig.PlayerBodyLayer));
            CapsuleCollider2D bodyCollider = player.GetComponent<CapsuleCollider2D>();
            Assert.That((bodyCollider.excludeLayers.value & (1 << playerConfig.BallLayer)) != 0, Is.True);
        }

        [Test]
        public void ControllerAppliesMovementAndOnlyOneJumpBeforeLeavingGround()
        {
            BallController ball = CreateBall();
            GameObject player = new GameObject("PlayerMovement");
            player.transform.SetParent(root.transform);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            player.AddComponent<CapsuleCollider2D>();
            PlayerInputReader reader = player.AddComponent<PlayerInputReader>();
            PlayerHitDetector detector = player.AddComponent<PlayerHitDetector>();
            detector.Initialize(ball, playerConfig);
            PlayerCatController controller = player.AddComponent<PlayerCatController>();
            controller.Initialize(reader, detector, playerConfig);
            root.SetActive(true);

            PlayerActionFrame first = controller.ApplyFixedTick(
                new PlayerInputFrame(1f, true, false, false),
                true);
            Assert.That(first.JumpRequested, Is.True);
            Assert.That(body.velocity.x, Is.EqualTo(playerConfig.MoveSpeed));
            Assert.That(body.velocity.y, Is.EqualTo(playerConfig.JumpSpeed));

            body.velocity = Vector2.zero;
            PlayerActionFrame second = controller.ApplyFixedTick(
                new PlayerInputFrame(0f, true, false, false),
                true);
            Assert.That(second.JumpRequested, Is.False);
            Assert.That(body.velocity.y, Is.Zero);
        }

        [Test]
        public void StaleBallStepRejectsHitWithoutChangingVelocity()
        {
            Harness harness = CreatePointHarness(5, HitterType.Opponent);
            harness.Bridge.StartInitialPoint();
            long step = harness.Ball.CurrentSnapshot.StepIndex;
            float beforeX = harness.Ball.CurrentSnapshot.VelocityX;

            bool accepted = harness.Bridge.TrySubmitHit(
                HitterType.Player,
                step + 1,
                new Vector2(9f, 9f));

            Assert.That(accepted, Is.False);
            Assert.That(harness.Ball.CurrentSnapshot.VelocityX, Is.EqualTo(beforeX));
        }

        [Test]
        public void PointEndingDoubleTouchNeverLaunchesOutgoingShot()
        {
            Harness harness = CreatePointHarness(1, HitterType.Player);
            harness.Bridge.StartInitialPoint();

            bool launched = harness.Bridge.TrySubmitHit(
                HitterType.Player,
                harness.Ball.CurrentSnapshot.StepIndex,
                new Vector2(9f, 9f));

            Assert.That(launched, Is.False);
            Assert.That(harness.Match.OpponentScore, Is.EqualTo(1));
            Assert.That(harness.Match.MatchEnded, Is.True);
            Assert.That(harness.Ball.CurrentSnapshot.IsActive, Is.False);
        }

        [Test]
        public void ValidReceiverHitChangesBallVelocityExactlyOnce()
        {
            Harness harness = CreatePointHarness(5, HitterType.Opponent);
            harness.Bridge.StartInitialPoint();
            long step = harness.Ball.CurrentSnapshot.StepIndex;

            bool launched = harness.Bridge.TrySubmitHit(
                HitterType.Player,
                step,
                new Vector2(7f, 5f));

            Assert.That(launched, Is.True);
            Assert.That(harness.Ball.CurrentSnapshot.VelocityX, Is.EqualTo(7f));
            Assert.That(harness.Ball.CurrentSnapshot.VelocityY, Is.EqualTo(5f));
            Assert.That(harness.Bridge.TrySubmitHit(
                HitterType.Player,
                step,
                new Vector2(9f, 9f)), Is.False);
        }

        private BallController CreateBall()
        {
            GameObject ballObject = new GameObject("Ball");
            ballObject.transform.SetParent(root.transform);
            ballObject.layer = playerConfig.BallLayer;
            ballObject.AddComponent<Rigidbody2D>();
            BallPhysicsApplier applier = ballObject.AddComponent<BallPhysicsApplier>();
            BallController ball = ballObject.AddComponent<BallController>();
            applier.Configure(physicsConfig, true, 0f);
            return ball;
        }

        private Harness CreatePointHarness(int targetScore, HitterType server)
        {
            loopConfig = ScriptableObject.CreateInstance<Phase3PointLoopConfig>();
            Vector2 launch = server == HitterType.Player ? new Vector2(4f, 6f) : new Vector2(-4f, 6f);
            Vector2 reset = server == HitterType.Player ? new Vector2(-4f, 1f) : new Vector2(4f, 1f);
            loopConfig.Configure(targetScore, 0f, server, reset, launch);
            BallController ball = CreateBall();
            BallPhysicsApplier applier = ball.GetComponent<BallPhysicsApplier>();
            CourtZoneDetector detector = root.AddComponent<CourtZoneDetector>();
            detector.Initialize(geometryConfig);
            RallyFlowManager rally = root.AddComponent<RallyFlowManager>();
            MatchFlowManager match = root.AddComponent<MatchFlowManager>();
            match.Initialize(loopConfig);
            ResetFlowController resetFlow = root.AddComponent<ResetFlowController>();
            resetFlow.Initialize(ball, loopConfig);
            PointLoopEventBridge bridge = root.AddComponent<PointLoopEventBridge>();
            bridge.Configure(applier, ball, detector, rally, match, resetFlow);
            root.SetActive(true);
            return new Harness(ball, match, bridge);
        }

        private readonly struct Harness
        {
            public Harness(BallController ball, MatchFlowManager match, PointLoopEventBridge bridge)
            {
                Ball = ball;
                Match = match;
                Bridge = bridge;
            }

            public BallController Ball { get; }
            public MatchFlowManager Match { get; }
            public PointLoopEventBridge Bridge { get; }
        }
    }
}
