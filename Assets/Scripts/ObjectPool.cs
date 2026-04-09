// ObjectPool — a simple, reusable pool for one type of prefab. Pre-warms a set
// of instances on Awake so there's no stutter on the first spawn wave. If the pool
// runs dry mid-game it creates an extra object rather than returning null, so gameplay
// never breaks — but a warning fires so you know to raise the initial size.

using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    // Config
    [Header("Config")]
    [Tooltip("The prefab every object in this pool will be cloned from.")]
    [SerializeField] private GameObject prefab;

    [Tooltip("How many instances to create at startup before the game starts spawning.")]
    [SerializeField] private int prewarmCount = 15;

    // References
    private Queue<GameObject> availableObjects;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (prefab == null)
        {
            Debug.LogWarning("[ObjectPool] No prefab assigned — pool will not pre-warm.");
            return;
        }

        availableObjects = new Queue<GameObject>(prewarmCount);
        Prewarm();
    }

    // -------------------------------------------------------------------------
    // Setup

    private void Prewarm()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            availableObjects.Enqueue(CreateInstance());
        }
    }

    private GameObject CreateInstance()
    {
        // Parent to this transform so the hierarchy stays tidy in the editor
        GameObject instance = Instantiate(prefab, transform);
        instance.SetActive(false);
        return instance;
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Returns a ready-to-use GameObject from the pool, activating it first.
    /// If the pool is empty a new instance is created on the fly (and a warning fires).
    /// </summary>
    public GameObject GetObject()
    {
        if (availableObjects == null)
        {
            Debug.LogWarning("[ObjectPool] Pool was not initialized — returning null.");
            return null;
        }

        GameObject instance;

        if (availableObjects.Count > 0)
        {
            instance = availableObjects.Dequeue();
        }
        else
        {
            // Pool ran dry — expand rather than stall the game
            Debug.LogWarning($"[ObjectPool] Pool for '{prefab.name}' is empty. Creating an extra instance — consider raising prewarmCount.");
            instance = CreateInstance();
        }

        instance.SetActive(true);
        return instance;
    }

    /// <summary>
    /// Disables the object and returns it to the pool for later reuse.
    /// Safe to call even if the object didn't originally come from this pool,
    /// though that would be a bug worth knowing about.
    /// </summary>
    public void ReturnObject(GameObject instance)
    {
        if (instance == null)
        {
            Debug.LogWarning("[ObjectPool] Tried to return a null object — ignoring.");
            return;
        }

        instance.SetActive(false);

        // Re-parent in case something moved it out of the pool's hierarchy
        instance.transform.SetParent(transform);

        // Reset local position so the object doesn't sit at its death spot for
        // a single frame when it's next pulled from the pool before Init() moves it
        instance.transform.localPosition = Vector3.zero;

        availableObjects.Enqueue(instance);
    }
}
