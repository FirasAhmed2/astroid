using UnityEditor;
using UnityEngine;

public static class SceneSetupFix
{
    // Step 1 — find out what type the SVG importer actually produces
    public static void DiagnoseSVG()
    {
        string[] paths = {
            "Assets/Sprites/asteroid_sprite.svg",
            "Assets/Sprites/player_ship_sprite.svg",
            "Assets/Sprites/space_background_map.svg"
        };

        foreach (var path in paths)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (allAssets == null || allAssets.Length == 0)
            {
                Debug.Log($"[Diagnose] {path} — no sub-assets found at all.");
                continue;
            }

            foreach (var a in allAssets)
            {
                if (a != null)
                    Debug.Log($"[Diagnose] {path} → {a.GetType().FullName}  name='{a.name}'");
            }
        }
    }

    // Step 2 — create DifficultyConfig presets then wire everything
    public static void Execute()
    {
        // Create the difficulty presets if they don't exist yet
        DifficultyConfigCreator.CreatePresets();

        // Now reload and wire all references
        WireSceneReferences();

        EditorApplication.ExecuteMenuItem("File/Save");
        Debug.Log("[SceneSetupFix] Fix complete.");
    }

    static void WireSceneReferences()
    {
        var diffConfig = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(
            "Assets/ScriptableObjects/Difficulty/Medium.asset");

        if (diffConfig == null)
        {
            Debug.LogWarning("[SceneSetupFix] DifficultyConfig still not found after creation attempt.");
            return;
        }

        var asteroidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Asteroid.prefab");
        var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");

        // ObjectPool — set prefab
        var poolGo = GameObject.Find("_ObjectPool");
        if (poolGo != null)
        {
            var so = new SerializedObject(poolGo.GetComponent<ObjectPool>());
            if (asteroidPrefab != null)
                so.FindProperty("prefab").objectReferenceValue = asteroidPrefab;
            so.ApplyModifiedProperties();
            Debug.Log("[SceneSetupFix] ObjectPool wired.");
        }

        // AsteroidSpawner — set config + pool
        var spawnerGo = GameObject.Find("_AsteroidSpawner");
        if (spawnerGo != null)
        {
            var so = new SerializedObject(spawnerGo.GetComponent<AsteroidSpawner>());
            so.FindProperty("difficultyConfig").objectReferenceValue = diffConfig;
            so.FindProperty("asteroidPool").objectReferenceValue = poolGo?.GetComponent<ObjectPool>();
            so.ApplyModifiedProperties();
            Debug.Log("[SceneSetupFix] AsteroidSpawner wired.");
        }

        // PlayerController — set config + input asset
        var playerGo = GameObject.Find("Player");
        if (playerGo != null)
        {
            var so = new SerializedObject(playerGo.GetComponent<PlayerController>());
            so.FindProperty("difficultyConfig").objectReferenceValue = diffConfig;
            if (inputAsset != null)
                so.FindProperty("inputActionAsset").objectReferenceValue = inputAsset;
            so.ApplyModifiedProperties();
            Debug.Log("[SceneSetupFix] PlayerController wired.");
        }

        // GameManager — set player + spawner
        var gmGo = GameObject.Find("_GameManager");
        if (gmGo != null)
        {
            var so = new SerializedObject(gmGo.GetComponent<GameManager>());
            so.FindProperty("playerController").objectReferenceValue = playerGo?.GetComponent<PlayerController>();
            so.FindProperty("asteroidSpawner").objectReferenceValue = spawnerGo?.GetComponent<AsteroidSpawner>();
            so.ApplyModifiedProperties();
            Debug.Log("[SceneSetupFix] GameManager wired.");
        }
    }
}
