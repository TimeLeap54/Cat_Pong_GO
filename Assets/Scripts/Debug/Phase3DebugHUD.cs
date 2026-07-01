using CatTennis.Rebuild.Flow;
using CatTennis.Rebuild.Cat;
using UnityEngine;

namespace CatTennis.Rebuild.Debugging
{
    /// <summary>Minimal lab-only readout for the Phase 3 point loop.</summary>
    public sealed class Phase3DebugHUD : MonoBehaviour
    {
        [SerializeField] private MatchFlowManager matchFlow;
        [SerializeField] private RallyFlowManager rallyFlow;
        [SerializeField] private PointLoopEventBridge bridge;
        [SerializeField] private PlayerCatController playerController;
        [SerializeField] private MatchBootstrapper bootstrapper;
        [SerializeField] private bool visible;

        public void Configure(
            MatchFlowManager match,
            RallyFlowManager rally,
            PointLoopEventBridge pointLoopBridge)
        {
            matchFlow = match;
            rallyFlow = rally;
            bridge = pointLoopBridge;
        }

        public void ConfigurePlayer(PlayerCatController player)
        {
            playerController = player;
        }

        public void ConfigureNavigation(MatchBootstrapper matchBootstrapper)
        {
            bootstrapper = matchBootstrapper;
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            if (matchFlow == null || rallyFlow == null || bridge == null)
            {
                return;
            }

            GUI.Box(new Rect(16f, 16f, 380f, playerController == null ? 146f : 186f),
                playerController == null ? "Point Loop QA" : "CAT TENNIS - MATCH QA");
            GUI.Label(new Rect(32f, 46f, 280f, 22f),
                $"Score  Player {matchFlow.PlayerScore} : {matchFlow.OpponentScore} Opponent");
            GUI.Label(new Rect(32f, 68f, 280f, 22f),
                $"Point {rallyFlow.GlobalPointId} | {rallyFlow.CurrentContext.State}");
            GUI.Label(new Rect(32f, 90f, 280f, 22f),
                matchFlow.MatchEnded ? $"Winner: {matchFlow.MatchWinner}" : "Fixed serve loop active");

            if (playerController != null)
            {
                GUI.Label(new Rect(32f, 112f, 320f, 22f),
                    $"{playerController.CurrentAction.LocomotionState} | {playerController.CurrentAction.SwingState}");
                GUI.Label(new Rect(32f, 134f, 320f, 22f),
                    "A/D Move  Space Jump  J Return  K Smash");
            }

            if (GUI.Button(new Rect(200f, playerController == null ? 124f : 164f, 80f, 26f), "Retry"))
            {
                if (bootstrapper != null)
                {
                    bootstrapper.RetryMatch();
                }
                else
                {
                    bridge.RetryMatch();
                }
            }

            if (bootstrapper != null &&
                GUI.Button(new Rect(288f, playerController == null ? 124f : 164f, 92f, 26f), "Main Menu"))
            {
                bootstrapper.ReturnToMainMenu();
            }
        }
    }
}
