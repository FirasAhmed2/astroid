// Asteroid — one rock flying down the screen. The spawner calls Init() right after
// pulling this from the pool to hand off the current difficulty speed. When it
// leaves the bottom of the screen, it fires OnExitScreen so the pool can reclaim it.
// If it touches the player it fires OnHitPlayer and lets the GameManager sort out the rest.

using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Asteroid : MonoBehaviour
{
    // References
    private Rigidbody2D rigidBody;
    private Camera mainCamera;

    // State
    private float moveSpeed;
    private float screenBottomY;
    private bool hasExited = false;

    // Events — the spawner/pool subscribes to OnExitScreen, GameManager to OnHitPlayer
    public event Action<Asteroid> OnExitScreen;
    public event Action OnHitPlayer;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[Asteroid] Camera.main is null — screen exit detection won't work.");
            return;
        }

        CacheScreenBottom();
    }

    private void OnEnable()
    {
        // Reset the exit flag each time this object is pulled from the pool
        hasExited = false;
    }

    private void FixedUpdate()
    {
        CheckScreenExit();
    }

    // -------------------------------------------------------------------------
    // Init

    /// <summary>
    /// Called by the spawner right after pulling this asteroid from the pool.
    /// Sets the downward speed and kicks off movement immediately.
    /// </summary>
    public void Init(float speed)
    {
        moveSpeed = speed;

        // Refresh the bottom boundary in case the camera moved since Awake ran
        CacheScreenBottom();

        // Straight down — negative Y in Unity 2D
        rigidBody.linearVelocity = Vector2.down * moveSpeed;
    }

    // -------------------------------------------------------------------------
    // Screen exit

    private void CacheScreenBottom()
    {
        if (mainCamera == null) return;

        // Use orthographicSize directly — safe regardless of camera z position
        float padding = 1f;
        screenBottomY = mainCamera.transform.position.y - mainCamera.orthographicSize - padding;
    }

    private void CheckScreenExit()
    {
        if (hasExited) return;
        if (transform.position.y > screenBottomY) return;

        hasExited = true;
        rigidBody.linearVelocity = Vector2.zero;

        // Pass ourselves so the pool knows which instance to reclaim
        OnExitScreen?.Invoke(this);
    }

    // -------------------------------------------------------------------------
    // Collision

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // If the player has a shield the hit will be absorbed — keep moving so
        // the asteroid doesn't freeze in place while the player carries on
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || !player.IsShielded)
            rigidBody.linearVelocity = Vector2.zero;

        OnHitPlayer?.Invoke();
    }
}
