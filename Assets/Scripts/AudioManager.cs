// AudioManager — one place that owns all game audio. Other scripts just call
// PlayExplosion() or PlayButtonClick() and don't need to know the details.
// Persists across scene loads so the background music never restarts mid-session.

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Singleton
    public static AudioManager Instance { get; private set; }

    // Audio sources — each one is a separate child AudioSource so they can
    // play and volume-control independently without fighting each other
    [Header("Audio Sources")]
    [Tooltip("AudioSource for the background music track (looping).")]
    [SerializeField] private AudioSource bgMusic;

    [Tooltip("AudioSource for the explosion one-shot when the player dies.")]
    [SerializeField] private AudioSource explosionSFX;

    [Tooltip("AudioSource for button click feedback sounds.")]
    [SerializeField] private AudioSource buttonClickSFX;

    // -------------------------------------------------------------------------
    // Singleton setup + boot audio

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A second AudioManager loaded (e.g. scene reload) — kill the
            // duplicate so there's only ever one playing music
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ValidateReferences();
        StartBgMusic();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void Start()
    {
        // OnEnable fires before GameManager.Awake() on first load, so we may
        // have missed the subscription — re-wire safely here after all Awakes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Plays the explosion one-shot. Called on player death.
    /// </summary>
    public void PlayExplosion()
    {
        if (explosionSFX == null)
        {
            Debug.LogWarning("[AudioManager] explosionSFX is not assigned.");
            return;
        }

        explosionSFX.Play();
    }

    /// <summary>
    /// Plays the button click one-shot. Call this from any button handler.
    /// </summary>
    public void PlayButtonClick()
    {
        if (buttonClickSFX == null)
        {
            Debug.LogWarning("[AudioManager] buttonClickSFX is not assigned.");
            return;
        }

        buttonClickSFX.Play();
    }

    /// <summary>
    /// Sets the background music volume (0–1). Handy for a settings slider.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        if (bgMusic == null)
        {
            Debug.LogWarning("[AudioManager] bgMusic is not assigned.");
            return;
        }

        bgMusic.volume = Mathf.Clamp01(volume);
    }

    // -------------------------------------------------------------------------
    // Internal

    private void StartBgMusic()
    {
        if (bgMusic == null) return;

        // Make absolutely sure it's configured to loop before we start it
        bgMusic.loop = true;
        bgMusic.Play();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver)
            PlayExplosion();
    }

    private void ValidateReferences()
    {
        if (bgMusic == null)
            Debug.LogWarning("[AudioManager] bgMusic AudioSource is not assigned.");

        if (explosionSFX == null)
            Debug.LogWarning("[AudioManager] explosionSFX AudioSource is not assigned.");

        if (buttonClickSFX == null)
            Debug.LogWarning("[AudioManager] buttonClickSFX AudioSource is not assigned.");
    }
}
