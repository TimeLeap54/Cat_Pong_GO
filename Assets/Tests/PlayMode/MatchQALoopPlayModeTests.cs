using System.Collections;
using CatTennis.Rebuild.Ball;
using CatTennis.Rebuild.Cat;
using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.State;
using CatTennis.Rebuild.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CatTennis.Rebuild.Tests
{
    public sealed class MatchQALoopPlayModeTests
    {
        [UnityTest]
        public IEnumerator ThreeMatchesRetryMenuReturnAndReentryRemainHealthy()
        {
            SceneManager.LoadScene("Rebuild_MainMenu");
            yield return null;
            MainMenuController menu = Object.FindObjectOfType<MainMenuController>();
            Assert.That(menu, Is.Not.Null);
            menu.StartMatch(true);
            yield return null;
            yield return null;

            for (int matchIndex = 0; matchIndex < 3; matchIndex++)
            {
                MatchBootstrapper bootstrapper = Object.FindObjectOfType<MatchBootstrapper>();
                MatchFlowManager match = Object.FindObjectOfType<MatchFlowManager>();
                PointLoopEventBridge bridge = Object.FindObjectOfType<PointLoopEventBridge>();
                PointLifecycleController lifecycle = Object.FindObjectOfType<PointLifecycleController>();
                BallController ball = Object.FindObjectOfType<BallController>();
                RallyFlowManager rally = Object.FindObjectOfType<RallyFlowManager>();
                PlayerInputReader input = Object.FindObjectOfType<PlayerInputReader>();
                ServeFlowController serve = Object.FindObjectOfType<ServeFlowController>();

                Assert.That(bootstrapper, Is.Not.Null);
                Assert.That(bootstrapper.IsInitialized, Is.True);
                if (matchIndex > 0)
                {
                    long previousPointId = rally.GlobalPointId;
                    bootstrapper.RetryMatch();
                    yield return new WaitForSeconds(0.4f);
                    Assert.That(rally.GlobalPointId, Is.GreaterThan(previousPointId));
                }

                Assert.That(match.PlayerScore, Is.Zero);
                Assert.That(match.OpponentScore, Is.Zero);
                Assert.That(lifecycle.State, Is.EqualTo(PointLoopState.RallyActive));
                for (int point = 0; point < 5; point++)
                {
                    yield return CompletePlayerServe(input, serve, ball);
                    ball.ResetBall(new Vector2(4f, 1f));
                    ball.SetPlayMode(BallPlayMode.Rally);
                    ball.Launch(new Vector2(0f, -1f));
                    long step = ball.CurrentSnapshot.StepIndex;
                    bool launched = bridge.TrySubmitHit(
                        HitterType.Opponent,
                        step,
                        new Vector2(0f, -1f));
                    Assert.That(launched, Is.True);
                    int expectedScore = point + 1;
                    for (int wait = 0; wait < 30 && match.PlayerScore < expectedScore; wait++)
                        yield return new WaitForFixedUpdate();
                    Assert.That(match.PlayerScore, Is.EqualTo(expectedScore));
                    if (point < 4)
                    {
                        yield return new WaitForSeconds(0.4f);
                    }
                }

                Assert.That(match.PlayerScore, Is.EqualTo(5));
                Assert.That(match.MatchEnded, Is.True);
                Assert.That(lifecycle.State, Is.EqualTo(PointLoopState.MatchEnded));
                Assert.That(ball.CurrentSnapshot.IsActive, Is.False);
            }

            MatchBootstrapper currentBootstrapper = Object.FindObjectOfType<MatchBootstrapper>();
            currentBootstrapper.ReturnToMainMenu();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Rebuild_MainMenu"));
            menu = Object.FindObjectOfType<MainMenuController>();
            menu.StartMatch(true);
            yield return null;
            yield return null;

            MatchFlowManager reenteredMatch = Object.FindObjectOfType<MatchFlowManager>();
            PointLifecycleController reenteredLifecycle = Object.FindObjectOfType<PointLifecycleController>();
            Assert.That(reenteredMatch.PlayerScore, Is.Zero);
            Assert.That(reenteredMatch.OpponentScore, Is.Zero);
            Assert.That(reenteredLifecycle.State, Is.EqualTo(PointLoopState.RallyActive));
        }

        [UnityTest]
        public IEnumerator OpponentReturnsAPlayerServeThroughTheRallyPipeline()
        {
            SceneManager.LoadScene("Rebuild_Match");
            yield return null; yield return null;
            PlayerInputReader input=Object.FindObjectOfType<PlayerInputReader>();
            ServeFlowController serve=Object.FindObjectOfType<ServeFlowController>();
            BallController ball=Object.FindObjectOfType<BallController>();
            RallyFlowManager rally=Object.FindObjectOfType<RallyFlowManager>();
            MatchFlowManager match=Object.FindObjectOfType<MatchFlowManager>();
            OpponentAIController opponent=Object.FindObjectOfType<OpponentAIController>();
            ShotExecutionController executor=Object.FindObjectOfType<ShotExecutionController>();
            Assert.That(opponent,Is.Not.Null); Assert.That(executor,Is.Not.Null);
            yield return CompletePlayerServe(input,serve,ball);
            bool returned=false;
            for(int step=0;step<250&&!returned;step++)
            {
                yield return new WaitForFixedUpdate();
                returned=rally.CurrentContext.LastHitter==HitterType.Opponent;
            }
            Assert.That(returned,Is.True,"AI did not produce a physical HitContact return.");
            Assert.That(ball.PlayMode,Is.EqualTo(BallPlayMode.Rally));
            for(int step=0;step<300&&match.OpponentScore==0;step++)
                yield return new WaitForFixedUpdate();
            Assert.That(match.OpponentScore,Is.EqualTo(1),
                "AI return did not resolve through the normal bounce and score rules.");
        }

        private static IEnumerator CompletePlayerServe(PlayerInputReader input,
            ServeFlowController serve, BallController ball)
        {
            Assert.That(serve.State, Is.EqualTo(ServeFlowState.WaitingForToss));
            Assert.That(ball.PlayMode, Is.EqualTo(BallPlayMode.Inactive));
            input.InjectDebugFrame(new PlayerInputFrame(0f, false, true, false));
            yield return new WaitForFixedUpdate();
            Assert.That(ball.PlayMode, Is.EqualTo(BallPlayMode.ServeToss));
            yield return new WaitForSeconds(0.32f);
            Assert.That(serve.State, Is.EqualTo(ServeFlowState.HitWindow));
            input.InjectDebugFrame(new PlayerInputFrame(0f, false, true, false));
            for (int wait = 0; wait < 20 && serve.State != ServeFlowState.ServeLaunched; wait++)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(serve.State, Is.EqualTo(ServeFlowState.ServeLaunched));
            Assert.That(ball.PlayMode, Is.EqualTo(BallPlayMode.Rally));
        }
    }
}
