// AsteroidSpawner — picks a random X along the top of the screen and drops an
// asteroid from the pool every spawnInterval seconds. The interval and speed both
// come straight from DifficultyConfig, so swapping difficulty presets is all you need
// to change how brutal the game feels. Call StopSpawning() on death and
// RestartSpawning() if you add a retry flow.

using System.Collections;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    // Config — set at runtime by GameManager, or drag a fallback in the Inspector
    [Header("Config")]
    [Tooltip("Fallback difficulty preset if GameManager hasn't pushed one yet.")]
    [SerializeField] private DifficultyConfig difficultyConfig;

    // References
    [Header("References")]
    [Tooltip("The ObjectPool that owns the asteroid prefab instances.")]
    [SerializeField] private ObjectPool asteroidPool;

    // State
    private Camera mainCamera;
    private Coroutine spawnCoroutine;
    private bool isSpawning = false;

    // Cached spawn-row bounds so we're not hitting ViewportToWorldPoint every spawn
    private float spawnY;
    private float spawnMinX;
    private float spawnMaxX;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("[AsteroidSpawner] Camera.main is null — can't calculate spawn bounds.");
        }

        if (difficultyConfig == null)
        {
            Debug.LogWarning("[AsteroidSpawner] No DifficultyConfig assigned — spawner won't start.");
        }

        if (asteroidPool == null)
        {
            Debug.LogWarning("[AsteroidSpawner] No ObjectPool assigned — spawner won't start.");
        }

        CacheSpawnBounds();
    }

    private void Start()
    {
        // Give the camera one more chance before the spawn loop fires —
        // Awake() may have bailed if aspect was invalid at editor startup.
        CacheSpawnBounds();

        // Warn if bounds are still zero — asteroids would spawn at world origin
        if (spawnMinX == 0f && spawnMaxX == 0f && spawnY == 0f)
        {
            Debug.LogWarning("[AsteroidSpawner] Spawn bounds are still zero after Start() — camera may not be set up correctly.");
        }

        // Don't auto-start — GameManager will call RestartSpawning() after
        // pushing the selected difficulty config. If there's no GameManager
        // (e.g. testing the scene standalone), start with whatever is serialized.
        if (GameManager.Instance == null)
            StartSpawning();
    }

    // -------------------------------------------------------------------------
    // Bounds

    private void CacheSpawnBounds()
    {
        if (mainCamera == null) return;

        float aspect = mainCamera.aspect;

        // aspect can be Infinity or NaN when the Game View has zero pixel height at editor startup —
        // Random.Range(-∞, +∞) produces NaN via IEEE 754 arithmetic (-∞ + ∞ = NaN)
        if (float.IsNaN(aspect) || float.IsInfinity(aspect) || aspect <= 0f)
        {
            Debug.LogWarning("[AsteroidSpawner] Camera aspect is invalid — spawn bounds not cached yet.");
            return;
        }

        float halfHeight = mainCamera.orthographicSize;
        float halfWidth  = halfHeight * aspect;
        Vector2 camPos   = mainCamera.transform.position;
        float topPadding = 1f;

        spawnY    = camPos.y + halfHeight + topPadding;
        spawnMinX = camPos.x - halfWidth;
        spawnMaxX = camPos.x + halfWidth;
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Swaps in a new difficulty config at runtime. Call this before
    /// RestartSpawning() so the new interval and speed take effect.
    /// </summary>
    public void SetDifficultyConfig(DifficultyConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("[AsteroidSpawner] Tried to set a null DifficultyConfig.");
            return;
        }

        difficultyConfig = config;
    }

    /// <summary>
    /// Begins the spawn loop. Called automatically on Start, but also safe
    /// to call manually if you need to kick it off at a specific moment.
    /// Does nothing if already spawning.
    /// </summary>
    public void StartSpawning()
    {
        if (isSpawning) return;
        if (difficultyConfig == null || asteroidPool == null) return;

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    /// <summary>
    /// Stops the spawn loop immediately. Any asteroid already in flight keeps moving.
    /// </summary>
    public void StopSpawning()
    {
        if (!isSpawning) return;

        isSpawning = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    /// <summary>
    /// Stops and immediately restarts the spawn loop. Useful for retrying a run
    /// or applying a new DifficultyConfig mid-session.
    /// </summary>
    public void RestartSpawning()
    {
        StopSpawning();
        StartSpawning();
    }

    // -------------------------------------------------------------------------
    // Spawn loop

    private IEnumerator SpawnLoop()
    {
        // Wait one interval before the very first spawn so the player has a moment to breathe
        yield return new WaitForSeconds(difficultyConfig.spawnInterval);

        while (isSpawning)
        {
            SpawnAsteroid();
            yield return new WaitForSeconds(difficultyConfig.spawnInterval);
        }
    }

    private void SpawnAsteroid()
    {
        GameObject asteroidObject = asteroidPool.GetObject();
        if (asteroidObject == null) return;

        asteroidObject.transform.position = PickSpawnPosition();

        // Make sure we can actually talk to the Asteroid component
        Asteroid asteroid = asteroidObject.GetComponent<Asteroid>();
        if (asteroid == null)
        {
            Debug.LogWarning("[AsteroidSpawner] Pooled object is missing an Asteroid component.");
            asteroidPool.ReturnObject(asteroidObject);
            return;
        }

        // Unsubscribe before subscribing — prevents double-subscription if this
        // asteroid was used in a previous wave without being fully cleaned up
        asteroid.OnExitScreen -= HandleAsteroidExitScreen;
        asteroid.OnExitScreen += HandleAsteroidExitScreen;

        asteroid.Init(difficultyConfig.asteroidSpeed);
    }

    private Vector3 PickSpawnPosition()
    {
        float randomX = Random.Range(spawnMinX, spawnMaxX);

        // If bounds were never set (camera not ready yet), Random.Range can produce NaN —
        // fall back to center screen so we at least see the asteroid rather than crashing
        if (float.IsNaN(randomX))
        {
            Debug.LogWarning("[AsteroidSpawner] Spawn X is NaN — bounds were not cached. Falling back to camera center.");
            randomX = mainCamera != null ? mainCamera.transform.position.x : 0f;
        }

        return new Vector3(randomX, spawnY, 0f);
    }

    // -------------------------------------------------------------------------
    // Event handlers

    private void HandleAsteroidExitScreen(Asteroid asteroid)
    {
        // Clean up the listener so it doesn't accumulate across reuses
        asteroid.OnExitScreen -= HandleAsteroidExitScreen;

        asteroidPool.ReturnObject(asteroid.gameObject);

        // Asteroid made it past the bottom — player dodged it successfully
        GameManager.Instance?.AsteroidAvoided();
    }
}
