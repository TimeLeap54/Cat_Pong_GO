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

        private void OnGUI()
        {
            if (matchFlow == null || rallyFlow == null || bridge == null)
            {
                return;
            }

            GUI.Box(new Rect(16f, 16f, 360f, playerController == null ? 118f : 158f),
                playerController == null ? "Phase 3 Point Loop Lab" : "Phase 4 Player Hit Lab");
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

            if (GUI.Button(new Rect(232f, 98f, 88f, 26f), "Retry"))
            {
                bridge.RetryMatch();
            }
        }
    }
}
