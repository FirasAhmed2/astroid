// FirebaseManager — owns all Firebase read/write for the game.
// Think of it as the game's save/load desk: every other system
// hands data to this one and it deals with the cloud.
// Persists across scenes and enforces a single instance.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;

[System.Serializable]
public class PlayerData
{
    public int    highScore;
    public int    trailColorIndex;
    public int    totalGamesPlayed;
    public int    totalAsteroids;
    public string lastDifficulty;
    public string lastPlayed;
}

public class FirebaseManager : MonoBehaviour
{
    // ── Config ──────────────────────────────────────────────
    [Header("Config")]
    [Tooltip("How long to wait for Firebase init before giving up (seconds).")]
    [SerializeField] private float initTimeout = 10f;

    // ── State ────────────────────────────────────────────────
    private bool              isInitialized = false;
    private FirebaseFirestore db;
    private string            playerId;
    private PlayerData        cachedData;

    // ── Singleton ────────────────────────────────────────────
    public static FirebaseManager Instance { get; private set; }

    // ── Events ───────────────────────────────────────────────
    public event Action             OnFirebaseReady;
    public event Action<PlayerData> OnDataLoaded;
    public event Action<string>     OnSaveFailed;

    // ── Public property ──────────────────────────────────────
    public bool IsReady => isInitialized;

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        // Classic singleton — first one wins, extras self-destruct
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerId = SystemInfo.deviceUniqueIdentifier;

        StartCoroutine(InitFirebase());
    }

    // Wait for Firebase dependencies, then hook everything up
    private IEnumerator InitFirebase()
    {
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();

        float elapsed = 0f;

        // Poll until the task finishes or we hit the timeout
        while (!dependencyTask.IsCompleted)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= initTimeout)
            {
                Debug.LogWarning("[FirebaseManager] Init timed out waiting for dependency check.");
                isInitialized = false;
                yield break;
            }

            yield return null;
        }

        if (dependencyTask.Result == DependencyStatus.Available)
        {
            db            = FirebaseFirestore.DefaultInstance;
            isInitialized = true;

            OnFirebaseReady?.Invoke();
            LoadPlayerData();
        }
        else
        {
            Debug.LogWarning($"[FirebaseManager] Firebase dependencies not met: {dependencyTask.Result}");
            isInitialized = false;
        }
    }

    // Pull the player record from Firestore — create a fresh one if missing
    private async void LoadPlayerData()
    {
        try
        {
            var docRef  = db.Collection("players").Document(playerId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                cachedData = SnapshotToPlayerData(snapshot);
            }
            else
            {
                // First time this device has played — give them a blank slate
                cachedData = new PlayerData();
                SavePlayerData(cachedData);
            }

            OnDataLoaded?.Invoke(cachedData);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FirebaseManager] LoadPlayerData failed: {e.Message}");
        }
    }

    // Push the full player record up to Firestore
    public async void SavePlayerData(PlayerData data)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[FirebaseManager] Tried to save before Firebase was ready.");
            return;
        }

        // Always stamp when we last saved
        data.lastPlayed = DateTime.UtcNow.ToString("o");

        try
        {
            var dict   = PlayerDataToDictionary(data);
            var docRef = db.Collection("players").Document(playerId);

            await docRef.SetAsync(dict);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FirebaseManager] SavePlayerData failed: {e.Message}");
            OnSaveFailed?.Invoke(e.Message);
        }
    }

    // Only write a new high score if it actually beats the old one
    public void SaveHighScore(int score)
    {
        var data = GetCachedData();

        if (score <= data.highScore)
            return;

        // Update local cache right away so the UI doesn't feel laggy
        data.highScore = score;
        cachedData     = data;

        SavePlayerData(data);
    }

    public void SaveTrailColor(int index)
    {
        var data = GetCachedData();
        data.trailColorIndex = index;
        cachedData = data;
        SavePlayerData(data);
    }

    public void IncrementGamesPlayed()
    {
        var data = GetCachedData();
        data.totalGamesPlayed++;
        cachedData = data;
        SavePlayerData(data);
    }

    public void IncrementAsteroidsAvoided(int count)
    {
        var data = GetCachedData();
        data.totalAsteroids += count;
        cachedData = data;
        SavePlayerData(data);
    }

    // Safe to call even before Firebase loads — just returns defaults
    public PlayerData GetCachedData()
    {
        return cachedData ?? new PlayerData();
    }

    // ── Helpers ──────────────────────────────────────────────

    private PlayerData SnapshotToPlayerData(DocumentSnapshot snapshot)
    {
        // Firestore can deserialize directly into a Dictionary — much cleaner than manual parsing
        var dict = snapshot.ToDictionary();
        var data = new PlayerData();

        data.highScore        = GetInt(dict,    "highScore");
        data.trailColorIndex  = GetInt(dict,    "trailColorIndex");
        data.totalGamesPlayed = GetInt(dict,    "totalGamesPlayed");
        data.totalAsteroids   = GetInt(dict,    "totalAsteroids");
        data.lastDifficulty   = GetString(dict, "lastDifficulty");
        data.lastPlayed       = GetString(dict, "lastPlayed");

        return data;
    }

    private Dictionary<string, object> PlayerDataToDictionary(PlayerData data)
    {
        return new Dictionary<string, object>
        {
            { "highScore",        data.highScore        },
            { "trailColorIndex",  data.trailColorIndex  },
            { "totalGamesPlayed", data.totalGamesPlayed },
            { "totalAsteroids",   data.totalAsteroids   },
            { "lastDifficulty",   data.lastDifficulty   },
            { "lastPlayed",       data.lastPlayed       }
        };
    }

    private int GetInt(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
            return Convert.ToInt32(value);

        return 0;
    }

    private string GetString(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
            return value.ToString();

        return string.Empty;
    }
}
