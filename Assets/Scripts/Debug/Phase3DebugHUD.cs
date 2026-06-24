using CatTennis.Rebuild.Flow;
using UnityEngine;

namespace CatTennis.Rebuild.Debugging
{
    /// <summary>Minimal lab-only readout for the Phase 3 point loop.</summary>
    public sealed class Phase3DebugHUD : MonoBehaviour
    {
        [SerializeField] private MatchFlowManager matchFlow;
        [SerializeField] private RallyFlowManager rallyFlow;
        [SerializeField] private PointLoopEventBridge bridge;

        public void Configure(
            MatchFlowManager match,
            RallyFlowManager rally,
            PointLoopEventBridge pointLoopBridge)
        {
            matchFlow = match;
            rallyFlow = rally;
            bridge = pointLoopBridge;
        }

        private void OnGUI()
        {
            if (matchFlow == null || rallyFlow == null || bridge == null)
            {
                return;
            }

            GUI.Box(new Rect(16f, 16f, 320f, 118f), "Phase 3 Point Loop Lab");
            GUI.Label(new Rect(32f, 46f, 280f, 22f),
                $"Score  Player {matchFlow.PlayerScore} : {matchFlow.OpponentScore} Opponent");
            GUI.Label(new Rect(32f, 68f, 280f, 22f),
                $"Point {rallyFlow.GlobalPointId} | {rallyFlow.CurrentContext.State}");
            GUI.Label(new Rect(32f, 90f, 280f, 22f),
                matchFlow.MatchEnded ? $"Winner: {matchFlow.MatchWinner}" : "Fixed serve loop active");

            if (GUI.Button(new Rect(232f, 98f, 88f, 26f), "Retry"))
            {
                bridge.RetryMatch();
            }
        }
    }
}
