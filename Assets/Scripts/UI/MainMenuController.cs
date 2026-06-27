using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTennis.Rebuild.UI
{
    /// <summary>Minimal production navigation used until final UI integration.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private bool showModeSelection = false; // 2단계 UI 모드 선택 제어 플래그

        public void StartMatch(bool isRally)
        {
            // 메인 메뉴 버튼 선택에 따른 스태틱 모드 상태 주입
            CatTennis.Rebuild.Flow.MatchBootstrapper.SelectedRallyMode = isRally;
            SceneManager.LoadScene("Rebuild_Match");
        }

        public void StartMatch()
        {
            StartMatch(true); // 기존 테스트 코드들의 매개변수 없는 호출에 호환하기 위한 폴백
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

            if (!showModeSelection)
            {
                // [1단계] 간결한 START 버튼
                GUI.Label(new Rect(panel.x + 52f, panel.y + 48f, 220f, 30f),
                    "Phase 1-4 Integration Build");
                if (GUI.Button(new Rect(panel.x + 60f, panel.y + 96f, 200f, 48f), "START"))
                {
                    showModeSelection = true; // 2단계 세부 메뉴 활성화
                }
            }
            else
            {
                // [2단계] START 클릭 시 튀어나오는 세부 메뉴 버튼들
                if (GUI.Button(new Rect(panel.x + 60f, panel.y + 32f, 200f, 40f), "TOURNAMENT"))
                {
                    StartMatch(false); // 토너먼트 모드로 시작
                }

                if (GUI.Button(new Rect(panel.x + 60f, panel.y + 80f, 200f, 40f), "RALLY"))
                {
                    StartMatch(true); // 랠리 모드로 시작
                }

                if (GUI.Button(new Rect(panel.x + 60f, panel.y + 128f, 200f, 32f), "BACK"))
                {
                    showModeSelection = false; // 메인 화면으로 돌아가기
                }
            }
        }
    }
}
