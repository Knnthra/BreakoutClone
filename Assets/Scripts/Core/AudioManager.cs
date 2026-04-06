using UnityEngine;

public class AudioManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance that persists across scenes.
    /// </summary>
    public static AudioManager Instance { get; private set; }

    /// <summary>
    /// AudioSource used for background music playback.
    /// </summary>
    [SerializeField] private AudioSource musicSource;

    /// <summary>
    /// AudioSource used for one-shot sound effects.
    /// </summary>
    [SerializeField] private AudioSource sfxSource;

    /// <summary>
    /// Music clip played on the main menu.
    /// </summary>
    /// <summary>
    /// Music clip played during gameplay.
    /// </summary>
    [SerializeField] private AudioClip menuMusic, ingameMusic;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Plays a music clip on loop, skipping if the same clip is already playing.
    /// </summary>
    /// <param name="clip">The music track to loop.</param>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    /// <summary>
    /// Stops the currently playing music.
    /// </summary>
    public void StopMusic()
    {
        musicSource.Stop();
    }

    /// <summary>
    /// Plays the main menu music.
    /// </summary>
    public void PlayMenuMusic() => PlayMusic(menuMusic);

    /// <summary>
    /// Plays the in-game music.
    /// </summary>
    public void PlayIngameMusic() => PlayMusic(ingameMusic);

    /// <summary>
    /// Plays a one-shot sound effect.
    /// </summary>
    /// <param name="clip">The sound effect to play once.</param>
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }
}
