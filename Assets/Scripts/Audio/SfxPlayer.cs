using UnityEngine;
using CatTennis.Rebuild.Config;

namespace CatTennis.Rebuild.Audio
{
    /// <summary>Plays approved sound effects without gameplay decisions.</summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioConfig config;
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioSource bgmSource;

        private const string BgmVolumeKey = "CatTennis.BgmVolume";
        private const string SfxVolumeKey = "CatTennis.SfxVolume";
        private const float DefaultVolume = 1f;

        public static SfxPlayer Instance { get; private set; }
        public static float BgmVolume => PlayerPrefs.GetFloat(BgmVolumeKey, DefaultVolume);
        public static float SfxVolume => PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume);

        private void Awake()
        {
            Instance = this;
            EnsureAudioListener();

            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }

            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;

            ApplyBgmVolume(BgmVolume);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Configure(AudioConfig audioConfig)
        {
            config = audioConfig;
        }

        public static void SetBgmVolume(float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(BgmVolumeKey, clamped);
            PlayerPrefs.Save();

            if (Instance != null)
            {
                Instance.ApplyBgmVolume(clamped);
            }
        }

        public static void SetSfxVolume(float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SfxVolumeKey, clamped);
            PlayerPrefs.Save();
        }

        public void PlayUiClick()
        {
            if (config == null)
            {
                return;
            }

            PlayOneShot(config.UiClickClip, config.UiClickVolume);
        }

        public void PlayHit()
        {
            if (config == null)
            {
                return;
            }

            PlayOneShot(config.HitClip, config.HitVolume);
        }

        public void PlaySmash()
        {
            if (config == null)
            {
                return;
            }

            PlayOneShot(config.SmashClip, config.SmashVolume);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.PlayOneShot(clip, Mathf.Clamp01(volume) * SfxVolume);
        }

        private void ApplyBgmVolume(float volume)
        {
            if (bgmSource != null)
            {
                bgmSource.volume = Mathf.Clamp01(volume);
            }
        }

        private void EnsureAudioListener()
        {
            if (FindObjectOfType<AudioListener>(true) != null)
            {
                return;
            }

            gameObject.AddComponent<AudioListener>();
        }
    }
}
