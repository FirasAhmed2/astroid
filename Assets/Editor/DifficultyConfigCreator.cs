// DifficultyConfigCreator — editor-only utility that stamps out the three preset
// DifficultyConfig assets (Easy / Medium / Hard) under Assets/ScriptableObjects/Difficulty.
// Run it once from the menu: Tools > Asteroid Dodger > Create Difficulty Presets.
// Safe to run again — it will overwrite the values but won't create duplicates.

using UnityEditor;
using UnityEngine;
using System.IO;

public static class DifficultyConfigCreator
{
    private const string OutputFolder = "Assets/ScriptableObjects/Difficulty";

    [MenuItem("Tools/Asteroid Dodger/Create Difficulty Presets")]
    public static void CreatePresets()
    {
        EnsureFolderExists(OutputFolder);

        CreateOrUpdatePreset("Easy",   difficultyName: "Easy",   asteroidSpeed: 3f, spawnInterval: 2f,   playerSpeed: 8f);
        CreateOrUpdatePreset("Medium", difficultyName: "Medium", asteroidSpeed: 5f, spawnInterval: 1f,   playerSpeed: 6f);
        CreateOrUpdatePreset("Hard",   difficultyName: "Hard",   asteroidSpeed: 8f, spawnInterval: 0.4f, playerSpeed: 5f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[DifficultyConfigCreator] Easy, Medium, and Hard presets created at: " + OutputFolder);
    }

    // Creates the asset if it doesn't exist yet, or refreshes its values if it does
    private static void CreateOrUpdatePreset(string fileName, string difficultyName,
        float asteroidSpeed, float spawnInterval, float playerSpeed)
    {
        string assetPath = $"{OutputFolder}/{fileName}.asset";

        // Load existing asset so we don't blow away any in-scene references
        var config = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(assetPath);

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<DifficultyConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
        }

        config.difficultyName  = difficultyName;
        config.asteroidSpeed   = asteroidSpeed;
        config.spawnInterval   = spawnInterval;
        config.playerSpeed     = playerSpeed;

        EditorUtility.SetDirty(config);
    }

    private static void EnsureFolderExists(string path)
    {
        // Split and walk each segment — AssetDatabase needs them created one at a time
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
