using UnityEngine;
using UnityEngine.UI;
using CatTennis.Rebuild.Shot;

namespace CatTennis.Rebuild.Audio
{
    /// <summary>Maps gameplay events to semantic audio requests.</summary>
    public sealed class AudioEventRouter : MonoBehaviour
    {
        [SerializeField] private SfxPlayer sfxPlayer;

        private Button[] registeredButtons;

        private void Awake()
        {
            if (sfxPlayer == null)
            {
                sfxPlayer = GetComponent<SfxPlayer>();
            }

            if (sfxPlayer == null)
            {
                sfxPlayer = FindObjectOfType<SfxPlayer>(true);
            }

            RegisterSceneButtons();
        }

        private void OnDestroy()
        {
            UnregisterSceneButtons();
        }

        public void RegisterSceneButtons()
        {
            UnregisterSceneButtons();
            registeredButtons = FindObjectsOfType<Button>(true);
            for (int i = 0; i < registeredButtons.Length; i++)
            {
                if (registeredButtons[i] == null)
                {
                    continue;
                }

                registeredButtons[i].onClick.AddListener(PlayUiClick);
            }
        }

        public void PlayUiClick()
        {
            if (sfxPlayer != null)
            {
                sfxPlayer.PlayUiClick();
                return;
            }

            SfxPlayer.Instance?.PlayUiClick();
        }

        public static void NotifyShotExecuted(ShotIntent intent)
        {
            SfxPlayer player = SfxPlayer.Instance;
            if (player == null)
            {
                return;
            }

            if (intent == ShotIntent.Smash)
            {
                player.PlaySmash();
            }
            else
            {
                player.PlayHit();
            }
        }

        private void UnregisterSceneButtons()
        {
            if (registeredButtons == null)
            {
                return;
            }

            for (int i = 0; i < registeredButtons.Length; i++)
            {
                if (registeredButtons[i] != null)
                {
                    registeredButtons[i].onClick.RemoveListener(PlayUiClick);
                }
            }

            registeredButtons = null;
        }
    }
}
