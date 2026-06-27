using System.Collections.Generic;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Config;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.Rules;
using CatTennis.Rebuild.State;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTennis.Rebuild.Tests
{
    public sealed class Phase45MatchIntegrationTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void LifecycleRejectsInvalidOrderAndTracksMatchEnd()
        {
            GameObject root = Track(new GameObject("LifecycleTest"));
            PointLifecycleController lifecycle = root.AddComponent<PointLifecycleController>();

            Assert.That(lifecycle.TryActivateRally(), Is.False);
            lifecycle.Initialize();
            Assert.That(lifecycle.State, Is.EqualTo(PointLoopState.StartingPoint));
            Assert.That(lifecycle.TryActivateRally(), Is.True);
            Assert.That(lifecycle.TryBeginNextPoint(), Is.False);
            Assert.That(lifecycle.TryBeginReset(), Is.True);
            lifecycle.MarkMatchEnded();
            Assert.That(lifecycle.State, Is.EqualTo(PointLoopState.MatchEnded));
            Assert.That(lifecycle.TryActivateRally(), Is.False);
        }

        [Test]
        public void ProductionMatchSceneBootstrapsAndValidatesWithoutErrors()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Rebuild_Match.unity", OpenSceneMode.Single);
            MatchBootstrapper bootstrapper = Object.FindObjectOfType<MatchBootstrapper>(true);
            MatchSceneValidator validator = Object.FindObjectOfType<MatchSceneValidator>(true);
            PointLifecycleController lifecycle = Object.FindObjectOfType<PointLifecycleController>(true);

            Assert.That(bootstrapper, Is.Not.Null);
            Assert.That(bootstrapper.InitializeMatch(), Is.True);
            Assert.That(bootstrapper.InitializeMatch(), Is.False);
            Assert.That(validator.ValidateScene(), Is.Empty);
            Assert.That(lifecycle.State, Is.EqualTo(PointLoopState.RallyActive));
            Assert.That(Object.FindObjectsOfType<CatAnimationPresenter>(true), Has.Length.EqualTo(2));
            Assert.That(GameObject.Find("Player").transform.Find("VisualRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("Opponent").transform.Find("VisualRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("Ball").transform.Find("VisualRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("Court").transform.Find("BackgroundVisual"), Is.Not.Null);
            Assert.That(GameObject.Find("Court").transform.Find("NetVisual"), Is.Not.Null);
        }

        [Test]
        public void BuildSettingsContainOnlyProductionScenes()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.That(scenes, Has.Length.EqualTo(2));
            Assert.That(scenes[0].path, Is.EqualTo("Assets/Scenes/Rebuild_MainMenu.unity"));
            Assert.That(scenes[1].path, Is.EqualTo("Assets/Scenes/Rebuild_Match.unity"));
            Assert.That(scenes[0].enabled && scenes[1].enabled, Is.True);
        }

        [TestCase("Assets/Art/Animations/Player/PlayerCat.controller",
            "Assets/Art/Animations/Player")]
        [TestCase("Assets/Art/Animations/Opponent/OpponentCat.controller",
            "Assets/Art/Animations/Opponent")]
        public void CharacterAnimatorAssetsHaveRequiredParametersAndResolvedFrames(
            string controllerPath,
            string clipFolder)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                controllerPath);
            Assert.That(controller, Is.Not.Null);

            string[] expectedParameters = { "MoveX", "Grounded", "Jump", "JSwing", "KSmash" };
            foreach (string expected in expectedParameters)
            {
                Assert.That(System.Array.Exists(controller.parameters,
                    parameter => parameter.name == expected), Is.True,
                    $"Animator parameter is missing: {expected}");
            }

            string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { clipFolder });
            Assert.That(clipGuids, Has.Length.EqualTo(6));
            foreach (string guid in clipGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Assert.That(bindings, Is.Not.Empty, $"Sprite curve is missing: {path}");
                foreach (EditorCurveBinding binding in bindings)
                {
                    ObjectReferenceKeyframe[] frames =
                        AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    Assert.That(frames, Is.Not.Empty, $"Animation has no frames: {path}");
                    foreach (ObjectReferenceKeyframe frame in frames)
                    {
                        Assert.That(frame.value, Is.Not.Null,
                            $"Animation has a missing sprite: {path}");
                    }
                }
            }
        }

        [Test]
        public void ThreeRetriesKeepPointScoreBallAndPlayerStateConsistent()
        {
            RetryHarness harness = CreateRetryHarness();
            harness.Lifecycle.Initialize();
            harness.Bridge.StartInitialPoint();
            AssertHealthy(harness, 1);

            for (int retry = 1; retry <= 3; retry++)
            {
                harness.Player.ApplyFixedTick(
                    new PlayerInputFrame(0f, false, true, false),
                    true);
                Assert.That(harness.Player.CurrentAction.SwingState,
                    Is.EqualTo(SwingState.NormalStartup));

                harness.Bridge.RetryMatch();
                AssertHealthy(harness, retry + 1);
            }
        }

        private RetryHarness CreateRetryHarness()
        {
            GameObject root = Track(new GameObject("RetryHarness"));
            root.SetActive(false);
            BallPhysicsConfig physicsConfig = Track(ScriptableObject.CreateInstance<BallPhysicsConfig>());
            CourtGeometryConfig geometry = Track(ScriptableObject.CreateInstance<CourtGeometryConfig>());
            geometry.Configure(-10f, 10f, -8f, -0.5f, 0.5f, 8f, 0.01f);
            Phase3PointLoopConfig loop = Track(ScriptableObject.CreateInstance<Phase3PointLoopConfig>());
            loop.Configure(5, 0f, HitterType.Opponent, new Vector2(4f, 1f), new Vector2(-4f, 6f));
            loop.SetPlayerResetPosition(new Vector2(-4f, 0.75f));
            PlayerControlConfig playerConfig = Track(ScriptableObject.CreateInstance<PlayerControlConfig>());

            GameObject ballObject = new GameObject("Ball");
            ballObject.transform.SetParent(root.transform);
            ballObject.AddComponent<Rigidbody2D>();
            BallPhysicsApplier applier = ballObject.AddComponent<BallPhysicsApplier>();
            BallController ball = ballObject.AddComponent<BallController>();
            applier.Configure(physicsConfig, true, 0f);

            GameObject playerObject = new GameObject("Player");
            playerObject.transform.SetParent(root.transform);
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<CapsuleCollider2D>();
            PlayerInputReader input = playerObject.AddComponent<PlayerInputReader>();
            PlayerHitDetector hitDetector = playerObject.AddComponent<PlayerHitDetector>();
            hitDetector.Initialize(ball, playerConfig);
            PlayerCatController player = playerObject.AddComponent<PlayerCatController>();
            player.Initialize(input, hitDetector, playerConfig);

            CourtZoneDetector detector = root.AddComponent<CourtZoneDetector>();
            detector.Initialize(geometry);
            RallyFlowManager rally = root.AddComponent<RallyFlowManager>();
            MatchFlowManager match = root.AddComponent<MatchFlowManager>();
            match.Initialize(loop);
            ResetFlowController reset = root.AddComponent<ResetFlowController>();
            reset.Initialize(ball, loop);
            PointLifecycleController lifecycle = root.AddComponent<PointLifecycleController>();
            PointLoopEventBridge bridge = root.AddComponent<PointLoopEventBridge>();
            bridge.Configure(applier, ball, detector, rally, match, reset);
            bridge.SetLifecycle(lifecycle);
            bridge.SetPlayerReset(player, loop.PlayerResetPosition);
            root.SetActive(true);
            return new RetryHarness(ball, player, rally, match, lifecycle, bridge);
        }

        private static void AssertHealthy(RetryHarness harness, long expectedPointId)
        {
            Assert.That(harness.Rally.GlobalPointId, Is.EqualTo(expectedPointId));
            Assert.That(harness.Match.PlayerScore, Is.Zero);
            Assert.That(harness.Match.OpponentScore, Is.Zero);
            Assert.That(harness.Ball.CurrentSnapshot.IsActive, Is.True);
            Assert.That(harness.Player.CurrentAction.LocomotionState, Is.EqualTo(LocomotionState.Grounded));
            Assert.That(harness.Player.CurrentAction.SwingState, Is.EqualTo(SwingState.Ready));
            Assert.That(harness.Lifecycle.State, Is.EqualTo(PointLoopState.RallyActive));
        }

        private T Track<T>(T instance) where T : Object
        {
            createdObjects.Add(instance);
            return instance;
        }

        private readonly struct RetryHarness
        {
            public RetryHarness(
                BallController ball,
                PlayerCatController player,
                RallyFlowManager rally,
                MatchFlowManager match,
                PointLifecycleController lifecycle,
                PointLoopEventBridge bridge)
            {
                Ball = ball;
                Player = player;
                Rally = rally;
                Match = match;
                Lifecycle = lifecycle;
                Bridge = bridge;
            }

            public BallController Ball { get; }
            public PlayerCatController Player { get; }
            public RallyFlowManager Rally { get; }
            public MatchFlowManager Match { get; }
            public PointLifecycleController Lifecycle { get; }
            public PointLoopEventBridge Bridge { get; }
        }
    }
}
