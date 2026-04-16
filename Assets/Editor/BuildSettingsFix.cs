using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class BuildSettingsFix
{
    public static void Execute()
    {
        // Log current build scenes
        var current = EditorBuildSettings.scenes;
        Debug.Log($"[BuildSettings] Current scene count: {current.Length}");
        foreach (var s in current)
            Debug.Log($"  [{(s.enabled ? "ON" : "OFF")}] {s.path}");

        // Ensure MainMenu is index 0, SampleScene is index 1
        var scenes = new List<EditorBuildSettingsScene>();

        var mainMenu = new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true);
        var sampleScene = new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true);

        scenes.Add(mainMenu);
        scenes.Add(sampleScene);

        // Keep any other scenes that were already there
        foreach (var s in current)
        {
            if (s.path == "Assets/Scenes/MainMenu.unity") continue;
            if (s.path == "Assets/Scenes/SampleScene.unity") continue;
            scenes.Add(s);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[BuildSettings] Updated: MainMenu=0, SampleScene=1.");
    }
}
