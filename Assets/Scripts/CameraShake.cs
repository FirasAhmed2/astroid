// CameraShake — sits on the Main Camera and rattles it around when something
// dramatic happens (player death, big explosion, etc.). Call Shake() from
// anywhere and it handles the rest — including snapping back cleanly when done.

using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // State
    private Vector3 originalPosition;
    private Coroutine activeShake;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Snapshot the resting position so we always snap back to exactly here
        originalPosition = transform.localPosition;
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Shakes the camera for <paramref name="duration"/> seconds,
    /// randomly offsetting it by up to <paramref name="magnitude"/> units each frame.
    /// If a shake is already running it gets cancelled and replaced.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        // Cancel any in-flight shake so the new one starts from a clean position
        if (activeShake != null)
        {
            StopCoroutine(activeShake);
            transform.localPosition = originalPosition;
        }

        activeShake = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    // -------------------------------------------------------------------------
    // Internal

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Random point inside a unit circle, scaled by magnitude
            Vector2 randomOffset = Random.insideUnitCircle * magnitude;

            // Only shift X/Y — Z stays the same so 2D camera depth is untouched
            transform.localPosition = new Vector3(
                originalPosition.x + randomOffset.x,
                originalPosition.y + randomOffset.y,
                originalPosition.z
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Always land back on the exact resting position
        transform.localPosition = originalPosition;
        activeShake = null;
    }
}
