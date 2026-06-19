using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip softPawClip;
    [SerializeField] private AudioClip smashPawClip;
    [SerializeField] private AudioClip servePawClip;
    [SerializeField] private AudioClip wallStepClip;
    [SerializeField] private AudioClip scoreClip;
    [SerializeField] private AudioClip buttonClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [SerializeField] private AudioClip bgmClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
    }

    private void Start()
    {
        PlayMusic();
    }

    public void PlayHit()
    {
        PlayPawHit(PawHitStyle.Rally);
    }

    public void PlayPawHit(PawHitStyle style)
    {
        var clip = hitClip;
        var pitch = Random.Range(0.96f, 1.04f);
        var volume = 1f;

        switch (style)
        {
            case PawHitStyle.Soft:
                clip = softPawClip != null ? softPawClip : hitClip;
                pitch = Random.Range(1.02f, 1.1f);
                volume = 0.72f;
                break;
            case PawHitStyle.Smash:
                clip = smashPawClip != null ? smashPawClip : hitClip;
                pitch = Random.Range(0.9f, 0.98f);
                break;
            case PawHitStyle.Serve:
                clip = servePawClip != null ? servePawClip : hitClip;
                pitch = Random.Range(0.98f, 1.04f);
                volume = 0.9f;
                break;
        }

        PlayOneShot(clip, volume, pitch);
    }

    public void PlayWallStep()
    {
        PlayOneShot(wallStepClip, 0.75f, Random.Range(0.94f, 1.04f));
    }

    public void PlayScore()
    {
        PlayOneShot(scoreClip);
    }

    public void PlayButton()
    {
        PlayOneShot(buttonClip);
    }

    public void PlayWin()
    {
        PlayOneShot(winClip);
    }

    public void PlayLose()
    {
        PlayOneShot(loseClip);
    }

    private void PlayOneShot(AudioClip clip)
    {
        PlayOneShot(clip, 1f, 1f);
    }

    private void PlayOneShot(AudioClip clip, float volume, float pitch)
    {
        EnsureSources();

        if (clip != null)
        {
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    private void PlayMusic()
    {
        EnsureSources();

        if (bgmClip == null || musicSource.clip == bgmClip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = bgmClip;
        musicSource.loop = true;
        musicSource.volume = 0.28f;
        musicSource.Play();
    }

    private void EnsureSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        musicSource.playOnAwake = false;
    }
}
