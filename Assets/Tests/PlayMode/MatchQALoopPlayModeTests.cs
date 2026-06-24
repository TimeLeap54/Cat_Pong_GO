using System.Collections;
using CatTennis.Rebuild.Ball;
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
            menu.StartMatch();
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
                    bool launched = bridge.TrySubmitHit(
                        HitterType.Opponent,
                        ball.CurrentSnapshot.StepIndex,
                        Vector2.zero);
                    Assert.That(launched, Is.False);
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
            menu.StartMatch();
            yield return null;
            yield return null;

            MatchFlowManager reenteredMatch = Object.FindObjectOfType<MatchFlowManager>();
            PointLifecycleController reenteredLifecycle = Object.FindObjectOfType<PointLifecycleController>();
            Assert.That(reenteredMatch.PlayerScore, Is.Zero);
            Assert.That(reenteredMatch.OpponentScore, Is.Zero);
            Assert.That(reenteredLifecycle.State, Is.EqualTo(PointLoopState.RallyActive));
        }
    }
}
