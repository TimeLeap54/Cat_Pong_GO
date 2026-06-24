using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTennis.Rebuild.UI
{
    /// <summary>Minimal production navigation used until final UI integration.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public void StartMatch()
        {
            SceneManager.LoadScene("Rebuild_Match");
        }

        private void OnGUI()
        {
            const float width = 320f;
            const float height = 180f;
            Rect panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
            GUI.Box(panel, "CAT TENNIS - MATCH QA");
            GUI.Label(new Rect(panel.x + 52f, panel.y + 48f, 220f, 30f),
                "Phase 1-4 Integration Build");
            if (GUI.Button(new Rect(panel.x + 60f, panel.y + 96f, 200f, 48f), "START MATCH"))
            {
                StartMatch();
            }
        }
    }
}
