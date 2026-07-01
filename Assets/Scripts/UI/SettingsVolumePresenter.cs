using CatTennis.Rebuild.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace CatTennis.Rebuild.UI
{
    public sealed class SettingsVolumePresenter : MonoBehaviour
    {
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        private bool syncing;

        private void Awake()
        {
            ConfigureSlider(bgmSlider);
            ConfigureSlider(sfxSlider);

            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.AddListener(SetBgmVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);
            }
        }

        private void OnEnable()
        {
            SyncFromSavedValues();
        }

        private void OnDestroy()
        {
            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveListener(SetBgmVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
            }
        }

        private void ConfigureSlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }

        private void SyncFromSavedValues()
        {
            syncing = true;

            if (bgmSlider != null)
            {
                bgmSlider.value = SfxPlayer.BgmVolume;
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = SfxPlayer.SfxVolume;
            }

            syncing = false;
        }

        private void SetBgmVolume(float value)
        {
            if (!syncing)
            {
                SfxPlayer.SetBgmVolume(value);
            }
        }

        private void SetSfxVolume(float value)
        {
            if (!syncing)
            {
                SfxPlayer.SetSfxVolume(value);
            }
        }
    }
}
