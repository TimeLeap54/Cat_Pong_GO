using UnityEngine;

namespace CatTennis.Rebuild.Config
{
    /// <summary>Maps audio events to clips and volume data.</summary>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Cat Tennis/Audio Config")]
    public sealed class AudioConfig : ScriptableObject
    {
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip smashClip;
        [SerializeField, Range(0f, 1f)] private float uiClickVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] private float hitVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float smashVolume = 0.95f;

        public AudioClip UiClickClip => uiClickClip;
        public AudioClip HitClip => hitClip;
        public AudioClip SmashClip => smashClip;
        public float UiClickVolume => uiClickVolume;
        public float HitVolume => hitVolume;
        public float SmashVolume => smashVolume;

        public void Configure(
            AudioClip uiClick,
            AudioClip hit,
            AudioClip smash,
            float uiVolume = 0.75f,
            float normalHitVolume = 0.85f,
            float smashHitVolume = 0.95f)
        {
            uiClickClip = uiClick;
            hitClip = hit;
            smashClip = smash;
            uiClickVolume = Mathf.Clamp01(uiVolume);
            hitVolume = Mathf.Clamp01(normalHitVolume);
            smashVolume = Mathf.Clamp01(smashHitVolume);
        }
    }
}
