using UnityEditor;
using UnityEngine;

public static class SceneSetupFinal
{
    public static void Execute()
    {
        // Force Unity to re-read the changed meta files and reimport as Sprite
        AssetDatabase.ImportAsset("Assets/Sprites/asteroid_sprite.svg",    ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Sprites/player_ship_sprite.svg", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Sprites/space_background_map.svg", ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        // Diagnose what we got after reimport
        LogAssetType("Assets/Sprites/asteroid_sprite.svg");
        LogAssetType("Assets/Sprites/player_ship_sprite.svg");
        LogAssetType("Assets/Sprites/space_background_map.svg");

        // Create difficulty presets
        DifficultyConfigCreator.CreatePresets();

        // Assign sprites to SpriteRenderers
        SetSprite("Background", "Assets/Sprites/space_background_map.svg");
        SetSprite("Player",     "Assets/Sprites/player_ship_sprite.svg");
        SetSprite("Asteroid",   "Assets/Sprites/asteroid_sprite.svg");

        // Wire all inspector references
        WireReferences();

        AssetDatabase.SaveAssets();
        EditorApplication.ExecuteMenuItem("File/Save");
        Debug.Log("[SceneSetupFinal] Complete.");
    }

    static void LogAssetType(string path)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
            if (a != null) Debug.Log($"[Final] {path} → {a.GetType().Name}  '{a.name}'");
    }

    static void SetSprite(string goName, string assetPath)
    {
        var go = GameObject.Find(goName);
        if (go == null) { Debug.LogWarning($"[Final] '{goName}' not found."); return; }

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) { Debug.LogWarning($"[Final] No SpriteRenderer on '{goName}'."); return; }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (sprite == null)
        {
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                if (sub is Sprite s) { sprite = s; break; }
        }

        if (sprite == null) { Debug.LogWarning($"[Final] No Sprite in '{assetPath}' after reimport."); return; }

        sr.sprite = sprite;
        EditorUtility.SetDirty(go);
        Debug.Log($"[Final] Sprite set on '{goName}'.");
    }

    static void WireReferences()
    {
        var diffConfig  = AssetDatabase.LoadAssetAtPath<DifficultyConfig>("Assets/ScriptableObjects/Difficulty/Medium.asset");
        var prefab      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Asteroid.prefab");
        var inputAsset  = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");

        var playerGo    = GameObject.Find("Player");
        var spawnerGo   = GameObject.Find("_AsteroidSpawner");
        var poolGo      = GameObject.Find("_ObjectPool");
        var gmGo        = GameObject.Find("_GameManager");

        // ObjectPool
        if (poolGo != null)
        {
            var so = new SerializedObject(poolGo.GetComponent<ObjectPool>());
            if (prefab != null) so.FindProperty("prefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
        }

        // AsteroidSpawner
        if (spawnerGo != null)
        {
            var so = new SerializedObject(spawnerGo.GetComponent<AsteroidSpawner>());
            if (diffConfig != null) so.FindProperty("difficultyConfig").objectReferenceValue = diffConfig;
            if (poolGo    != null) so.FindProperty("asteroidPool").objectReferenceValue = poolGo.GetComponent<ObjectPool>();
            so.ApplyModifiedProperties();
        }

        // PlayerController
        if (playerGo != null)
        {
            var so = new SerializedObject(playerGo.GetComponent<PlayerController>());
            if (diffConfig != null) so.FindProperty("difficultyConfig").objectReferenceValue = diffConfig;
            if (inputAsset != null) so.FindProperty("inputActionAsset").objectReferenceValue = inputAsset;
            so.ApplyModifiedProperties();
        }

        // GameManager
        if (gmGo != null)
        {
            var so = new SerializedObject(gmGo.GetComponent<GameManager>());
            if (playerGo  != null) so.FindProperty("playerController").objectReferenceValue = playerGo.GetComponent<PlayerController>();
            if (spawnerGo != null) so.FindProperty("asteroidSpawner").objectReferenceValue  = spawnerGo.GetComponent<AsteroidSpawner>();
            so.ApplyModifiedProperties();
        }

        Debug.Log("[Final] All references wired.");
    }
}
