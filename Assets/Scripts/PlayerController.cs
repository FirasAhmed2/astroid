// PlayerController — handles everything the player ship does while alive:
// reading movement input via the new Input System, pushing velocity through
// a Rigidbody2D, clamping position inside camera bounds, and dying cleanly
// when something calls Die(). Speed values come from a DifficultyConfig asset
// so the GameManager can swap difficulty without touching this script.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // Config
    [Header("Config")]
    [Tooltip("Drag one of the Easy / Medium / Hard DifficultyConfig assets here.")]
    [SerializeField] private DifficultyConfig difficultyConfig;

    [Tooltip("The shared palette asset — must match the one used by CustomizationUI.")]
    [SerializeField] private TrailColorPalette trailColorPalette;

    // References
    [Header("References")]
    [Tooltip("Assign the InputSystem_Actions asset from the project. " +
             "Make sure 'Generate C# Class' is enabled on that asset.")]
    [SerializeField] private InputActionAsset inputActionAsset;

    // State
    private Rigidbody2D rigidBody;
    private InputAction moveAction;
    private InputActionMap playerMap;
    private Vector2 moveInput;
    private bool canMove = true;

    // Cached camera bounds — recalculated in Start() and on demand
    private float minX, maxX, minY, maxY;
    private Camera mainCamera;

    // Power-Up State
    [Header("Power-Up State")]
    private bool isShielded = false;
    private bool isInSlowMo = false;
    private float scoreMultiplier = 1f;
    private Coroutine activeEffectCoroutine;

    // Power-Up Visuals
    [Header("Power-Up Visuals")]
    [Tooltip("SpriteRenderer on the ship — used to flash shield color.")]
    [SerializeField] private SpriteRenderer shipSpriteRenderer;

    // Anyone who cares (e.g. GameManager) subscribes to this
    public event Action OnPlayerDied;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[PlayerController] Camera.main is null — screen bounds won't work.");
        }

        if (difficultyConfig == null)
        {
            Debug.LogWarning("[PlayerController] No DifficultyConfig assigned — player won't move.");
        }

        SetupInput();
    }

    private void Start()
    {
        // Camera is fully ready by Start() — safe to calculate bounds now
        RecalculateBounds();

        if (minX == 0f && maxX == 0f && minY == 0f && maxY == 0f)
        {
            Debug.LogWarning("[PlayerController] Bounds are still zero after Start() — " +
                             "check that your Camera is tagged MainCamera and is Orthographic.");
        }

        LoadAndApplyTrail();
    }

    private void OnEnable()
    {
        // Only activate input during actual gameplay — this is the key fix that
        // stops the Scene view camera from feeding garbage values into our input
        if (!Application.isPlaying) return;
        playerMap?.Enable();
    }

    private void OnDisable()
    {
        // Always disable the whole map, not just the action —
        // avoids the Input System continuing to process Scene view events
        playerMap?.Disable();
    }

    private void FixedUpdate()
    {
        // Don't run any physics or input logic while in the editor Scene view —
        // that's what was causing the NaN frustum errors when panning around
        if (!Application.isPlaying) return;
        if (!canMove) return;

        ApplyMovement();
        ClampToBounds();
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Swaps in a new difficulty config at runtime. GameManager calls this
    /// when the player picks a difficulty from the menu.
    /// </summary>
    public void SetDifficultyConfig(DifficultyConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("[PlayerController] Tried to set a null DifficultyConfig.");
            return;
        }

        difficultyConfig = config;
    }

    // -------------------------------------------------------------------------
    // Input setup

    private void SetupInput()
    {
        if (inputActionAsset == null)
        {
            Debug.LogWarning("[PlayerController] InputActionAsset is not assigned.");
            return;
        }

        playerMap = inputActionAsset.FindActionMap("Player", throwIfNotFound: false);
        if (playerMap == null)
        {
            Debug.LogWarning("[PlayerController] 'Player' action map not found in the InputActionAsset.");
            return;
        }

        moveAction = playerMap.FindAction("Move", throwIfNotFound: false);
        if (moveAction == null)
        {
            Debug.LogWarning("[PlayerController] 'Move' action not found in the Player action map.");
        }
    }

    // -------------------------------------------------------------------------
    // Movement

    private void ApplyMovement()
    {
        if (moveAction == null || difficultyConfig == null) return;

        moveInput = moveAction.ReadValue<Vector2>();

        Vector2 velocity = moveInput * difficultyConfig.playerSpeed;

        // NaN velocity would corrupt the Rigidbody and anything parented to it
        if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y))
        {
            Debug.LogWarning("[PlayerController] Velocity produced NaN — skipping frame.");
            return;
        }

        rigidBody.linearVelocity = velocity;
    }

    private void ClampToBounds()
    {
        if (mainCamera == null) return;

        Vector2 clampedPosition = rigidBody.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);

        if (float.IsNaN(clampedPosition.x) || float.IsNaN(clampedPosition.y))
        {
            Debug.LogWarning($"[PlayerController] Clamped position is NaN " +
                             $"(bounds: x[{minX},{maxX}] y[{minY},{maxY}]) — skipping.");
            return;
        }

        // Only write back if we actually hit a boundary — avoids
        // a tiny physics correction nudge every single frame
        if (clampedPosition != rigidBody.position)
        {
            rigidBody.position = clampedPosition;
        }
    }

    // Call this if the camera ever moves or the screen resolution changes at runtime
    public void RecalculateBounds()
    {
        if (mainCamera == null) return;

        float aspect = mainCamera.aspect;

        // aspect returns NaN or Infinity when the Game View has zero height —
        // common at editor startup, so bail out and keep previous bounds
        if (float.IsNaN(aspect) || float.IsInfinity(aspect) || aspect <= 0f)
        {
            Debug.LogWarning("[PlayerController] Camera aspect is invalid — skipping bounds recalculation.");
            return;
        }

        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * aspect;
        Vector2 camPos = mainCamera.transform.position;

        minX = camPos.x - halfWidth;
        maxX = camPos.x + halfWidth;
        minY = camPos.y - halfHeight;
        maxY = camPos.y + halfHeight;
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Call this when the player gets hit. Stops all movement and fires OnPlayerDied
    /// so the GameManager can react (show game-over screen, stop spawning, etc.).
    /// </summary>
    public void Die()
    {
        if (isShielded)
        {
            // Shield absorbs this hit — cancel death, flash and carry on
            StartCoroutine(ShieldHitFlash());
            return;
        }

        if (!canMove) return; // already dead — don't fire the event twice

        canMove = false;
        rigidBody.linearVelocity = Vector2.zero;

        // Disable the whole map so no stray input lingers after death
        playerMap?.Disable();

        OnPlayerDied?.Invoke();
    }

    /// <summary>
    /// Resets the player back to a moveable state. GameManager calls this at the
    /// start of each play session in case we're restarting without a scene reload.
    /// </summary>
    public void ResetPlayer()
    {
        canMove = true;
        playerMap?.Enable();

        // Re-cache bounds in case the camera changed since the last session
        RecalculateBounds();
    }

    // -------------------------------------------------------------------------
    // Power-up effects

    /// <summary>
    /// Routes an incoming power-up to the correct effect coroutine.
    /// Cancels any effect that's still running so they don't stack awkwardly.
    /// </summary>
    public void ApplyPowerUp(PowerUpConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("[PlayerController] ApplyPowerUp called with a null config.");
            return;
        }

        if (activeEffectCoroutine != null)
            StopCoroutine(activeEffectCoroutine);

        switch (config.powerUpType)
        {
            case PowerUpType.Shield:
                activeEffectCoroutine = StartCoroutine(ShieldRoutine(config));
                break;
            case PowerUpType.SlowMo:
                activeEffectCoroutine = StartCoroutine(SlowMoRoutine(config));
                break;
            case PowerUpType.ScoreBoost:
                activeEffectCoroutine = StartCoroutine(ScoreBoostRoutine(config));
                break;
        }
    }

    private IEnumerator ShieldRoutine(PowerUpConfig config)
    {
        isShielded = true;

        if (shipSpriteRenderer != null)
            shipSpriteRenderer.color = config.primaryColor;

        yield return new WaitForSeconds(config.effectDuration);

        isShielded = false;

        if (shipSpriteRenderer != null)
            shipSpriteRenderer.color = Color.white;
    }

    private IEnumerator SlowMoRoutine(PowerUpConfig config)
    {
        isInSlowMo = true;
        Time.timeScale = 0.3f;

        // Keep physics steps proportional — without this, FixedUpdate fires too
        // rarely relative to real time and movement feels sluggish even after unpausing
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(config.effectDuration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isInSlowMo = false;
    }

    private IEnumerator ScoreBoostRoutine(PowerUpConfig config)
    {
        scoreMultiplier = 2f;

        yield return new WaitForSeconds(config.effectDuration);

        scoreMultiplier = 1f;
    }

    private IEnumerator ShieldHitFlash()
    {
        // Capture the current shield color so we can restore it after each white flash
        Color shieldColor = shipSpriteRenderer != null ? shipSpriteRenderer.color : Color.cyan;

        for (int i = 0; i < 3; i++)
        {
            if (shipSpriteRenderer != null)
                shipSpriteRenderer.color = Color.white;

            yield return new WaitForSeconds(0.08f);

            if (shipSpriteRenderer != null)
                shipSpriteRenderer.color = shieldColor;

            yield return new WaitForSeconds(0.08f);
        }
    }

    // -------------------------------------------------------------------------
    // Read-only accessors

    /// <summary>Current score multiplier — 1 normally, 2 during a ScoreBoost.</summary>
    public float ScoreMultiplier => scoreMultiplier;

    /// <summary>True while a Shield power-up is active — asteroids check this before stopping.</summary>
    public bool IsShielded => isShielded;

    // -------------------------------------------------------------------------
    // Trail color

    private void LoadAndApplyTrail()
    {
        var trail = GetComponentInChildren<TrailRenderer>();
        if (trail == null)
        {
            Debug.LogWarning("[PlayerController] No TrailRenderer found on ship or children — skipping trail color.");
            return;
        }

        if (trailColorPalette == null)
        {
            Debug.LogWarning("[PlayerController] TrailColorPalette is not assigned — skipping trail color.");
            return;
        }

        int index = PlayerPrefs.GetInt("SelectedTrailIndex", trailColorPalette.colors.Count - 1);

        TrailColorPalette.Entry entry = trailColorPalette.GetEntry(index);
        if (entry == null) return;

        trail.colorGradient = trailColorPalette.BuildGradient(entry);
    }

    // -------------------------------------------------------------------------
    // Collision

    private void OnTriggerEnter2D(Collider2D other)
    {
        // The asteroid owns the "Asteroid" tag — this is the cleanest place to
        // react to it since the player owns its own death logic
        if (!other.CompareTag("Asteroid")) return;

        Die();
    }
}