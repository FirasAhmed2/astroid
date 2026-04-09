// ParticleSpawner — lives in the game scene and knows how to pop an explosion
// effect at any world position. Other systems (like GameManager) just call
// SpawnExplosion() and forget it — cleanup happens automatically once the
// effect finishes playing.

using System.Collections;
using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    // References
    [Header("References")]
    [Tooltip("The ParticleSystem prefab to instantiate when an explosion is triggered.")]
    [SerializeField] private ParticleSystem explosionPrefab;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (explosionPrefab == null)
        {
            Debug.LogWarning("[ParticleSpawner] No explosion prefab assigned — explosions won't play.");
        }
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Spawns the explosion particle effect at the given world position.
    /// The spawned instance auto-destroys once the effect finishes.
    /// </summary>
    public void SpawnExplosion(Vector3 position)
    {
        if (explosionPrefab == null)
        {
            Debug.LogWarning("[ParticleSpawner] SpawnExplosion called but explosionPrefab is not set.");
            return;
        }

        ParticleSystem instance = Instantiate(explosionPrefab, position, Quaternion.identity);
        instance.Play();

        float lifetime = CalculateEffectDuration(instance);
        StartCoroutine(DestroyAfterDelay(instance.gameObject, lifetime));
    }

    // -------------------------------------------------------------------------
    // Internal helpers

    // Total time = how long the system emits + the max a single particle can live.
    // constantMax covers both flat values and curve-based startLifetime settings.
    private float CalculateEffectDuration(ParticleSystem particles)
    {
        var main = particles.main;
        return main.duration + main.startLifetime.constantMax;
    }

    private IEnumerator DestroyAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Guard against the object already being gone (e.g. scene unloaded)
        if (target != null)
        {
            Destroy(target);
        }
    }
}
