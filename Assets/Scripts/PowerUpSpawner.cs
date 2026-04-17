// PowerUpSpawner — drops one random power-up at irregular intervals while the game is running.
// Only one power-up is ever on screen at once — if the active one is still floating around,
// the next spawn attempt is skipped. GameManager calls StartSpawning/StopSpawning.
// Uses instantiate (not a pool) because power-ups are rare enough that the overhead is fine.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    // Config
    [Header("Config")]
    [Tooltip("Seconds between each spawn attempt. Randomized between min and max.")]
    [SerializeField] private float spawnIntervalMin = 12f;

    [Tooltip("Upper bound for the random spawn interval (seconds).")]
    [SerializeField] private float spawnIntervalMax = 20f;

    [Tooltip("The 3 power-up config assets — drag all 3 in here.")]
    [SerializeField] private List<PowerUpConfig> powerUpConfigs;

    // References
    [Header("References")]
    [Tooltip("The PowerUpPrefab to instantiate — must have a PowerUp component.")]
    [SerializeField] private GameObject powerUpPrefab;

    // Internal
    private Camera mainCamera;
    private GameObject activePowerUp;
    private Coroutine spawnCoroutine;
    private bool isSpawning = false;

    private float spawnY;
    private float spawnMinX;
    private float spawnMaxX;

    // PlayerController listens to this to know when a power-up has been collected
    public event Action<PowerUpConfig> OnPowerUpCollected;

    // -------------------------------------------------------------------------
    // Lifecycle

    private void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
            Debug.LogWarning("[PowerUpSpawner] Camera.main is null — spawn bounds can't be calculated.");

        if (powerUpConfigs == null || powerUpConfigs.Count == 0)
            Debug.LogWarning("[PowerUpSpawner] powerUpConfigs list is empty — no power-ups will spawn.");

        if (powerUpPrefab == null)
            Debug.LogWarning("[PowerUpSpawner] powerUpPrefab is not assigned — spawner won't work.");

        CacheSpawnBounds();
    }

    private void Start()
    {
        // Start standalone if there's no GameManager (handy for isolated scene testing)
        if (GameManager.Instance == null)
            StartSpawning();
    }

    // -------------------------------------------------------------------------
    // Bounds

    private void CacheSpawnBounds()
    {
        if (mainCamera == null) return;

        float aspect = mainCamera.aspect;

        // aspect can be NaN or Infinity when the Game View has zero height at editor startup
        if (float.IsNaN(aspect) || float.IsInfinity(aspect) || aspect <= 0f)
        {
            Debug.LogWarning("[PowerUpSpawner] Camera aspect is invalid — spawn bounds not cached yet.");
            return;
        }

        float halfHeight = mainCamera.orthographicSize;
        float halfWidth  = halfHeight * aspect;
        Vector2 camPos   = mainCamera.transform.position;

        spawnY    = camPos.y + halfHeight + 0.5f;
        spawnMinX = camPos.x - halfWidth;
        spawnMaxX = camPos.x + halfWidth;
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Starts the spawn loop. Called by GameManager once the play session begins.
    /// Safe to call if already running — does nothing in that case.
    /// </summary>
    public void StartSpawning()
    {
        if (isSpawning) return;
        if (powerUpPrefab == null || powerUpConfigs == null || powerUpConfigs.Count == 0) return;

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnLoop());
        Debug.Log("[PowerUpSpawner] Spawn loop started.");
    }

    /// <summary>
    /// Stops the spawn loop. Any active power-up stays on screen but no new ones spawn.
    /// Called by GameManager when the player dies.
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

    // -------------------------------------------------------------------------
    // Spawn loop

    private IEnumerator SpawnLoop()
    {
        // Wait before the first spawn so the player has time to get settled
        yield return new WaitForSeconds(UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax));

        while (isSpawning)
        {
            // Only spawn if there's nothing active right now
            if (activePowerUp == null || !activePowerUp.activeInHierarchy)
                SpawnPowerUp();

            yield return new WaitForSeconds(UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax));
        }
    }

    private void SpawnPowerUp()
    {
        PowerUpConfig config = powerUpConfigs[UnityEngine.Random.Range(0, powerUpConfigs.Count)];

        CacheSpawnBounds();
        float spawnX = UnityEngine.Random.Range(spawnMinX, spawnMaxX);

        // Fall back to center if bounds weren't cached yet
        if (float.IsNaN(spawnX))
        {
            Debug.LogWarning("[PowerUpSpawner] Spawn X is NaN — using camera center as fallback.");
            spawnX = mainCamera != null ? mainCamera.transform.position.x : 0f;
        }

        GameObject instance = Instantiate(powerUpPrefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);

        PowerUp powerUp = instance.GetComponent<PowerUp>();
        if (powerUp == null)
        {
            Debug.LogWarning("[PowerUpSpawner] Instantiated prefab is missing a PowerUp component.");
            Destroy(instance);
            return;
        }

        powerUp.OnCollected += HandlePowerUpCollected;
        powerUp.OnExpired   += HandlePowerUpExpired;

        powerUp.Init(config);
        activePowerUp = instance;
        Debug.Log($"[PowerUpSpawner] Spawned {config.displayName} at x={spawnX:F1}, y={spawnY:F1}");
    }

    // -------------------------------------------------------------------------
    // Event handlers

    private void HandlePowerUpCollected(PowerUp powerUp)
    {
        // Unsubscribe before firing so we don't double-handle if events overlap
        powerUp.OnCollected -= HandlePowerUpCollected;
        powerUp.OnExpired   -= HandlePowerUpExpired;

        activePowerUp = null;

        OnPowerUpCollected?.Invoke(powerUp.Config);

        // Not pooled — clean it up
        Destroy(powerUp.gameObject);
    }

    private void HandlePowerUpExpired(PowerUp powerUp)
    {
        powerUp.OnCollected -= HandlePowerUpCollected;
        powerUp.OnExpired   -= HandlePowerUpExpired;

        activePowerUp = null;

        Destroy(powerUp.gameObject);
    }
}
