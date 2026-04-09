// PowerUp — one floating pickup drifting down the screen.
// The spawner calls Init() after instantiating it, which stamps the config,
// sprite, color, and velocity. Fires events so the spawner and game logic
// stay decoupled — this script doesn't care what the effect actually does.

using System;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    // Config
    [Header("Config")]
    [Tooltip("Which power-up this is — set by the spawner via Init().")]
    [SerializeField] private PowerUpConfig config;

    // References
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // State
    private bool hasBeenCollected = false;
    private float spawnRealTime;

    // Events
    // OnCollected — fires when the player touches it
    // OnExpired   — fires only when it times out naturally (NOT on collection)
    public event Action<PowerUp> OnCollected;
    public event Action<PowerUp> OnExpired;

    // -------------------------------------------------------------------------
    // Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb == null)
            Debug.LogWarning("[PowerUp] Rigidbody2D is missing — movement won't work.");

        if (spriteRenderer == null)
            Debug.LogWarning("[PowerUp] SpriteRenderer is missing — visuals won't work.");
    }

    private void OnEnable()
    {
        hasBeenCollected = false;

        // Use unscaled real time so lifetime is consistent even during SlowMo
        spawnRealTime = Time.realtimeSinceStartup;
    }

    private void Update()
    {
        // Bail early if Init() hasn't run yet — no config means nothing to do
        if (config == null) return;

        if (Time.realtimeSinceStartup - spawnRealTime > config.lifetime)
        {
            Despawn(collected: false);
            return;
        }

        // Gentle breathing pulse — keeps the pickup feeling alive while floating
        float scale = Mathf.Sin(Time.time * 3f) * 0.08f + 1f;
        transform.localScale = Vector3.one * scale;
    }

    // -------------------------------------------------------------------------
    // Init

    /// <summary>
    /// Called by the spawner immediately after instantiation.
    /// Sets everything visual and kicks off downward movement.
    /// </summary>
    public void Init(PowerUpConfig powerUpConfig)
    {
        config = powerUpConfig;

        if (rb != null)
            rb.linearVelocity = Vector2.down * config.fallSpeed;

        if (spriteRenderer != null)
        {
            // The config icon doubles as the world sprite — distinct look per type
            spriteRenderer.sprite = config.icon;
            spriteRenderer.color  = config.primaryColor;
        }
    }

    // -------------------------------------------------------------------------
    // Collision

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Guard against the same trigger firing twice in one frame
        if (hasBeenCollected) return;

        hasBeenCollected = true;
        OnCollected?.Invoke(this);

        // Don't fire OnExpired here — collection and natural expiry are different events
        Despawn(collected: true);
    }

    // -------------------------------------------------------------------------
    // Despawn

    private void Despawn(bool collected)
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Only tell listeners it "expired" if it timed out — not if the player grabbed it
        if (!collected)
            OnExpired?.Invoke(this);

        gameObject.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Public accessors

    /// <summary>Config this power-up was initialised with — read by the spawner on collection.</summary>
    public PowerUpConfig Config => config;
}
