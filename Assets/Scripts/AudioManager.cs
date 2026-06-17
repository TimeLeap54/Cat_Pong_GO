using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip hitClip;
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
        PlayOneShot(hitClip);
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
        EnsureSources();

        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
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
