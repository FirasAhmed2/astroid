using UnityEditor;
using UnityEngine;

public static class SceneSetup
{
    public static void Execute()
    {
        AssignSprites();
        CreateAsteroidPrefab();
        WireSceneReferences();
        AssetDatabase.SaveAssets();
        EditorApplication.ExecuteMenuItem("File/Save");
        Debug.Log("[SceneSetup] Scene setup complete.");
    }

    // -------------------------------------------------------------------------
    // Sprites

    static void AssignSprites()
    {
        SetSprite("Background",  "Assets/Sprites/space_background_map.svg");
        SetSprite("Player",      "Assets/Sprites/player_ship_sprite.svg");
        SetSprite("Asteroid",    "Assets/Sprites/asteroid_sprite.svg");
    }

    static void SetSprite(string goName, string assetPath)
    {
        var go = GameObject.Find(goName);
        if (go == null) { Debug.LogWarning($"[SceneSetup] GameObject '{goName}' not found."); return; }

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) { Debug.LogWarning($"[SceneSetup] No SpriteRenderer on '{goName}'."); return; }

        // Try loading directly as Sprite first
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        // SVG importers sometimes nest the sprite — scan all sub-assets
        if (sprite == null)
        {
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (sub is Sprite s) { sprite = s; break; }
            }
        }

        if (sprite == null)
        {
            Debug.LogWarning($"[SceneSetup] Could not load Sprite from '{assetPath}'. Assign it manually.");
            return;
        }

        sr.sprite = sprite;
        EditorUtility.SetDirty(go);
        Debug.Log($"[SceneSetup] Assigned sprite to '{goName}'.");
    }

    // -------------------------------------------------------------------------
    // Asteroid prefab

    static void CreateAsteroidPrefab()
    {
        var asteroid = GameObject.Find("Asteroid");
        if (asteroid == null) { Debug.LogWarning("[SceneSetup] Asteroid GameObject not found."); return; }

        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string prefabPath = $"{folder}/Asteroid.prefab";
        bool success;
        PrefabUtility.SaveAsPrefabAssetAndConnect(asteroid, prefabPath, InteractionMode.AutomatedAction, out success);

        if (success)
            Debug.Log($"[SceneSetup] Asteroid prefab saved to '{prefabPath}'.");
        else
            Debug.LogWarning("[SceneSetup] Failed to save Asteroid prefab.");
    }

    // -------------------------------------------------------------------------
    // Wire references

    static void WireSceneReferences()
    {
        // Load the Medium difficulty config as a sensible scene default
        var diffConfig = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(
            "Assets/ScriptableObjects/Difficulty/Medium.asset");

        if (diffConfig == null)
            Debug.LogWarning("[SceneSetup] Medium DifficultyConfig not found — run 'Tools > Asteroid Dodger > Create Difficulty Presets' first.");

        // Load the asteroid prefab
        var asteroidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Asteroid.prefab");

        // --- ObjectPool ---
        var poolGo = GameObject.Find("_ObjectPool");
        if (poolGo != null)
        {
            var pool = poolGo.GetComponent<ObjectPool>();
            if (pool != null && asteroidPrefab != null)
            {
                var so = new SerializedObject(pool);
                so.FindProperty("prefab").objectReferenceValue = asteroidPrefab;
                so.ApplyModifiedProperties();
            }
        }

        // --- AsteroidSpawner ---
        var spawnerGo = GameObject.Find("_AsteroidSpawner");
        if (spawnerGo != null)
        {
            var spawner = spawnerGo.GetComponent<AsteroidSpawner>();
            if (spawner != null)
            {
                var pool = GameObject.Find("_ObjectPool")?.GetComponent<ObjectPool>();
                var so = new SerializedObject(spawner);
                if (diffConfig != null)
                    so.FindProperty("difficultyConfig").objectReferenceValue = diffConfig;
                if (pool != null)
                    so.FindProperty("asteroidPool").objectReferenceValue = pool;
                so.ApplyModifiedProperties();
            }
        }

        // --- GameManager ---
        var gmGo = GameObject.Find("_GameManager");
        if (gmGo != null)
        {
            var gm = gmGo.GetComponent<GameManager>();
            if (gm != null)
            {
                var player   = GameObject.Find("Player")?.GetComponent<PlayerController>();
                var spawner  = GameObject.Find("_AsteroidSpawner")?.GetComponent<AsteroidSpawner>();
                var so = new SerializedObject(gm);
                if (player  != null) so.FindProperty("playerController").objectReferenceValue  = player;
                if (spawner != null) so.FindProperty("asteroidSpawner").objectReferenceValue   = spawner;
                so.ApplyModifiedProperties();
            }
        }

        // --- PlayerController ---
        var playerGo = GameObject.Find("Player");
        if (playerGo != null)
        {
            var pc = playerGo.GetComponent<PlayerController>();
            if (pc != null)
            {
                var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                    "Assets/InputSystem_Actions.inputactions");
                var so = new SerializedObject(pc);
                if (diffConfig  != null) so.FindProperty("difficultyConfig").objectReferenceValue   = diffConfig;
                if (inputAsset  != null) so.FindProperty("inputActionAsset").objectReferenceValue   = inputAsset;
                so.ApplyModifiedProperties();
            }
        }

        // Scale background to fill a standard 9x16 camera view
        var bg = GameObject.Find("Background");
        if (bg != null)
        {
            bg.transform.localScale = new Vector3(12f, 22f, 1f);
            EditorUtility.SetDirty(bg);
        }

        Debug.Log("[SceneSetup] All scene references wired.");
    }
}
