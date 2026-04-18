// GameManager — the single source of truth for game state. Everything else reacts
// to what this says. It owns the state machine (MainMenu → Playing → GameOver),
// drives the score timer, and wires up the player death event. Lives in the MainMenu
// scene and persists across scene loads via DontDestroyOnLoad. Finds gameplay
// references (player, spawner) automatically when a new scene finishes loading.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }

    // Scene names
    [Header("Scenes")]
    [Tooltip("The scene that contains the actual gameplay (player, asteroids, etc).")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Tooltip("The scene with the main menu UI.")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Tooltip("The scene where the player customizes their ship before the run starts.")]
    [SerializeField] private string customizationSceneName = "CustomizationScene";

    // References — found automatically when the game scene loads
    private PlayerController playerController;
    private AsteroidSpawner asteroidSpawner;
    private PowerUpSpawner powerUpSpawner;
    private ParticleSpawner particleSpawner;
    private CameraShake cameraShake;

    // State
    private GameState currentState = GameState.MainMenu;
    private DifficultyConfig activeDifficultyConfig;
    private int currentScore = 0;
    private Coroutine scoreCoroutine;
    private int asteroidsAvoidedThisRun = 0;

    // Set true right before loading the game scene so we know to start
    // playing once references are found — survives across the scene transition
    private bool hasPendingStart = false;

    // How often the score ticks up — 0.1s gives a smooth feeling counter
    private const float ScoreTickInterval = 0.1f;
    private const int ScorePerTick = 1;

    // Events — UI and other systems subscribe to these rather than polling
    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnScoreUpdated;

    // -------------------------------------------------------------------------
    // Singleton setup

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A duplicate snuck in — destroy it so there's only ever one
            Destroy(gameObject);
            return;
        }
        Application.targetFrameRate = 90;
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // -------------------------------------------------------------------------
    // Scene loading callback — auto-discover gameplay references

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene loaded: '{scene.name}' (expecting '{gameSceneName}')");

        if (scene.name != gameSceneName) return;

        // Wait a frame so all Awake/Start calls finish before we go looking
        StartCoroutine(FindReferencesAndStart());
    }

    private IEnumerator FindReferencesAndStart()
    {
        yield return null;

        playerController = FindAnyObjectByType<PlayerController>();
        asteroidSpawner = FindAnyObjectByType<AsteroidSpawner>();
        powerUpSpawner = FindAnyObjectByType<PowerUpSpawner>();
        particleSpawner = FindAnyObjectByType<ParticleSpawner>();
        cameraShake = FindAnyObjectByType<CameraShake>();

        if (playerController == null)
            Debug.LogWarning("[GameManager] PlayerController not found in the game scene.");

        if (asteroidSpawner == null)
            Debug.LogWarning("[GameManager] AsteroidSpawner not found in the game scene.");

        if (powerUpSpawner == null)
        {
            Debug.LogWarning("[GameManager] PowerUpSpawner not found in the game scene — power-ups won't spawn.");
        }
        else
        {
            // Unsubscribe before re-subscribing so restarts never stack duplicate listeners
            powerUpSpawner.OnPowerUpCollected -= HandlePowerUpCollected;
            powerUpSpawner.OnPowerUpCollected += HandlePowerUpCollected;
        }

        if (particleSpawner == null)
            Debug.LogWarning("[GameManager] ParticleSpawner not found in the game scene — explosions won't play.");

        if (cameraShake == null)
            Debug.LogWarning("[GameManager] CameraShake not found — attach it to the Main Camera for screen shake.");

        Debug.Log($"[GameManager] References found. Pending start: {hasPendingStart}, Config: {activeDifficultyConfig?.difficultyName ?? "null"}");

        if (hasPendingStart && activeDifficultyConfig != null)
        {
            hasPendingStart = false;
            BeginPlaySession();
        }
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Stores the chosen difficulty, loads the game scene, and starts playing
    /// once the scene is ready. Safe to call from MainMenuUI.
    /// </summary>
    public void StartGame(DifficultyConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("[GameManager] StartGame called with a null DifficultyConfig — aborting.");
            return;
        }

        activeDifficultyConfig = config;
        hasPendingStart = true;

        Debug.Log($"[GameManager] StartGame — loading '{gameSceneName}' with difficulty '{config.difficultyName}'.");

        // If we're already in the game scene (e.g. called from a debug button),
        // find references and start immediately instead of reloading
        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            StartCoroutine(FindReferencesAndStart());
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Restarts the current run using the same difficulty that was active before.
    /// Reloads the game scene for a clean slate.
    /// </summary>
    public void RestartGame()
    {
        if (activeDifficultyConfig == null)
        {
            Debug.LogWarning("[GameManager] RestartGame called but no difficulty was set — call StartGame first.");
            return;
        }

        StopScoreTimer();
        UnsubscribeFromPlayer();

        hasPendingStart = true;

        Debug.Log("[GameManager] RestartGame — reloading game scene.");

        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Stops everything and returns to the main menu scene.
    /// </summary>
    public void GoToMainMenu()
    {
        StopScoreTimer();
        UnsubscribeFromPlayer();

        playerController = null;
        asteroidSpawner = null;
        powerUpSpawner = null;
        hasPendingStart = false;

        ResetScore();
        SetState(GameState.MainMenu);

        SceneManager.LoadScene(menuSceneName);
    }

    /// <summary>
    /// Stores the chosen difficulty so it survives the trip through
    /// CustomizationScene. Call this before GoToCustomization().
    /// </summary>
    public void SetPendingDifficulty(DifficultyConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("[GameManager] SetPendingDifficulty called with a null config — ignoring.");
            return;
        }

        activeDifficultyConfig = config;
    }

    /// <summary>
    /// Loads the customization screen. Expects SetPendingDifficulty() to have
    /// been called first so the config is ready when CustomizationUI calls StartGame.
    /// </summary>
    public void GoToCustomization()
    {
        if (activeDifficultyConfig == null)
        {
            Debug.LogWarning("[GameManager] GoToCustomization called but no difficulty is set — call SetPendingDifficulty first.");
            return;
        }

        Debug.Log($"[GameManager] GoToCustomization — loading '{customizationSceneName}'.");

        SceneManager.LoadScene(customizationSceneName);
    }

    // -------------------------------------------------------------------------
    // Internal — actually starts gameplay after references are ready

    private void BeginPlaySession()
    {
        Debug.Log("[GameManager] BeginPlaySession — pushing difficulty and starting spawner.");

        // Push the selected difficulty to gameplay systems so they use the
        // config the player actually picked, not whatever was serialized
        playerController?.SetDifficultyConfig(activeDifficultyConfig);
        asteroidSpawner?.SetDifficultyConfig(activeDifficultyConfig);

        // Reset the player in case we're restarting without a full scene reload —
        // this re-enables movement and input that Die() locked down last session
        playerController?.ResetPlayer();

        asteroidsAvoidedThisRun = 0;

        ResetScore();
        SubscribeToPlayer();
        StartScoreTimer();
        asteroidSpawner?.RestartSpawning();
        powerUpSpawner?.StartSpawning();

        SetState(GameState.Playing);
    }

    // -------------------------------------------------------------------------
    // State machine

    private void SetState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        OnGameStateChanged?.Invoke(currentState);
    }

    // -------------------------------------------------------------------------
    // Player events

    private void SubscribeToPlayer()
    {
        if (playerController == null) return;

        // Unsubscribe first so we never stack duplicate listeners across restarts
        playerController.OnPlayerDied -= HandlePlayerDied;
        playerController.OnPlayerDied += HandlePlayerDied;
    }

    private void UnsubscribeFromPlayer()
    {
        if (playerController == null) return;

        playerController.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandlePowerUpCollected(PowerUpConfig config)
    {
        playerController?.ApplyPowerUp(config);
        
    }

    private void HandlePlayerDied()
    {
        // Always restore time scale first — SlowMo power-up may still be active
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        StopScoreTimer();
        asteroidSpawner?.StopSpawning();
        powerUpSpawner?.StopSpawning();

        SaveRunData();

        // Spawn the explosion where the player was before anything moves it
        if (playerController != null)
        {
            particleSpawner?.SpawnExplosion(playerController.transform.position);
        }

        // Shake the camera so the death feels impactful
        cameraShake?.Shake(0.4f, 0.3f);

        SetState(GameState.GameOver);
    }

    // -------------------------------------------------------------------------
    // Score timer

    private void StartScoreTimer()
    {
        StopScoreTimer();
        scoreCoroutine = StartCoroutine(ScoreTickLoop());
    }

    private void StopScoreTimer()
    {
        if (scoreCoroutine == null) return;

        StopCoroutine(scoreCoroutine);
        scoreCoroutine = null;
    }

    private IEnumerator ScoreTickLoop()
    {
        var wait = new WaitForSeconds(ScoreTickInterval);

        while (true)
        {
            yield return wait;

            // ScoreMultiplier is 2 during a ScoreBoost power-up, 1 otherwise
            currentScore += Mathf.RoundToInt(ScorePerTick *
                (playerController != null ? playerController.ScoreMultiplier : 1f));
            OnScoreUpdated?.Invoke(currentScore);
        }
    }

    private void ResetScore()
    {
        currentScore = 0;
        OnScoreUpdated?.Invoke(currentScore);
    }

    // -------------------------------------------------------------------------
    // Asteroid tracking

    // Called by whatever detects an asteroid passing the bottom of the screen
    public void AsteroidAvoided()
    {
        asteroidsAvoidedThisRun++;
    }

    // -------------------------------------------------------------------------
    // Firebase save — game must still work fine if Firebase isn't up

    private void SaveRunData()
    {
        if (FirebaseManager.Instance == null)
        {
            Debug.LogWarning("[GameManager] SaveRunData skipped — FirebaseManager not found.");
            return;
        }

        if (!FirebaseManager.Instance.IsReady)
        {
            Debug.LogWarning("[GameManager] SaveRunData skipped — Firebase not initialized yet.");
            return;
        }

        FirebaseManager.Instance.SaveHighScore(currentScore);
        FirebaseManager.Instance.IncrementGamesPlayed();
        FirebaseManager.Instance.IncrementAsteroidsAvoided(asteroidsAvoidedThisRun);

        // save the difficulty name so we know what they last played
        if (activeDifficultyConfig != null)
        {
            var data = FirebaseManager.Instance.GetCachedData();
            data.lastDifficulty = activeDifficultyConfig.difficultyName;
            FirebaseManager.Instance.SavePlayerData(data);
        }
    }

    // -------------------------------------------------------------------------
    // Read-only accessors

    public GameState CurrentState => currentState;
    public int CurrentScore => currentScore;
    public DifficultyConfig ActiveDifficultyConfig => activeDifficultyConfig;
}
