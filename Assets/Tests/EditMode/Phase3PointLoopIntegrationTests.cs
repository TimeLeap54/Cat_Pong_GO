using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
using NUnit.Framework;
using UnityEngine;

namespace CatTennis.Rebuild.Tests
{
    public sealed class Phase3PointLoopIntegrationTests
    {
        private GameObject root;
        private BallPhysicsConfig physicsConfig;
        private CourtGeometryConfig geometryConfig;
        private Phase3PointLoopConfig loopConfig;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            Object.DestroyImmediate(physicsConfig);
            Object.DestroyImmediate(geometryConfig);
            Object.DestroyImmediate(loopConfig);
        }

        [Test]
        public void PhysicalServeFirstBounceThenSecondBounceScoresAndStartsNextPoint()
        {
            Harness harness = CreateHarness(5);
            harness.Bridge.StartInitialPoint();

            Assert.That(harness.Rally.GlobalPointId, Is.EqualTo(1));
            Assert.That(harness.Ball.CurrentSnapshot.IsActive, Is.True);

            bool observedFirstBounce = false;
            for (int step = 0; step < 400 && harness.Match.PlayerScore == 0; step++)
            {
                harness.Applier.StepOnce(0.02f);
                if (harness.Rally.CurrentContext.State == RallyState.ReceiverCourtBounced)
                {
                    observedFirstBounce = true;
                    Assert.That(harness.Match.PlayerScore, Is.Zero);
                }
            }

            Assert.That(observedFirstBounce, Is.True);
            Assert.That(harness.Match.PlayerScore, Is.EqualTo(1));
            Assert.That(harness.Rally.GlobalPointId, Is.EqualTo(2));
            Assert.That(harness.Ball.CurrentSnapshot.IsActive, Is.True);
        }

        [Test]
        public void MatchEndingPointStopsBallAndDoesNotCreateSixthPoint()
        {
            Harness harness = CreateHarness(1);
            harness.Bridge.StartInitialPoint();

            for (int step = 0; step < 400 && !harness.Match.MatchEnded; step++)
            {
                harness.Applier.StepOnce(0.02f);
            }

            Assert.That(harness.Match.MatchEnded, Is.True);
            Assert.That(harness.Match.PlayerScore, Is.EqualTo(1));
            Assert.That(harness.Rally.GlobalPointId, Is.EqualTo(1));
            Assert.That(harness.Ball.CurrentSnapshot.IsActive, Is.False);
        }

        private Harness CreateHarness(int targetScore)
        {
            root = new GameObject("Phase3IntegrationHarness");
            root.SetActive(false);

            physicsConfig = ScriptableObject.CreateInstance<BallPhysicsConfig>();
            geometryConfig = ScriptableObject.CreateInstance<CourtGeometryConfig>();
            geometryConfig.Configure(-10f, 10f, -8f, -0.5f, 0.5f, 8f, 0.01f);
            loopConfig = ScriptableObject.CreateInstance<Phase3PointLoopConfig>();
            loopConfig.Configure(
                targetScore,
                0f,
                HitterType.Player,
                new Vector2(-4f, 1f),
                new Vector2(4f, 6f));

            GameObject ballObject = new GameObject("Ball");
            ballObject.transform.SetParent(root.transform);
            ballObject.AddComponent<Rigidbody2D>();
            BallPhysicsApplier applier = ballObject.AddComponent<BallPhysicsApplier>();
            BallController ball = ballObject.AddComponent<BallController>();
            applier.Configure(physicsConfig, true, 0f);

            CourtZoneDetector detector = root.AddComponent<CourtZoneDetector>();
            detector.Initialize(geometryConfig);
            RallyFlowManager rally = root.AddComponent<RallyFlowManager>();
            MatchFlowManager match = root.AddComponent<MatchFlowManager>();
            match.Initialize(loopConfig);
            ResetFlowController reset = root.AddComponent<ResetFlowController>();
            reset.Initialize(ball, loopConfig);
            PointLoopEventBridge bridge = root.AddComponent<PointLoopEventBridge>();
            bridge.Configure(applier, ball, detector, rally, match, reset);

            root.SetActive(true);
            return new Harness(applier, ball, rally, match, bridge);
        }

        private readonly struct Harness
        {
            public Harness(
                BallPhysicsApplier applier,
                BallController ball,
                RallyFlowManager rally,
                MatchFlowManager match,
                PointLoopEventBridge bridge)
            {
                Applier = applier;
                Ball = ball;
                Rally = rally;
                Match = match;
                Bridge = bridge;
            }

            public BallPhysicsApplier Applier { get; }
            public BallController Ball { get; }
            public RallyFlowManager Rally { get; }
            public MatchFlowManager Match { get; }
            public PointLoopEventBridge Bridge { get; }
        }
    }
}
