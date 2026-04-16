using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class AssignTrailPalette
{
    public static void Execute()
    {
        string palettePath = "Assets/ScriptableObjects/TrailColorPalette.asset";
        var palette = AssetDatabase.LoadAssetAtPath<TrailColorPalette>(palettePath);

        if (palette == null)
        {
            Debug.LogError($"[AssignTrailPalette] Could not load palette at {palettePath}");
            return;
        }

        AssignToCustomizationScene(palette);
        AssignToGameScene(palette);
    }

    private static void AssignToCustomizationScene(TrailColorPalette palette)
    {
        string scenePath = "Assets/Scenes/CustomizationScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var ui = Object.FindFirstObjectByType<CustomizationUI>();
        if (ui == null)
        {
            Debug.LogError("[AssignTrailPalette] CustomizationUI not found in CustomizationScene.");
            return;
        }

        var so = new SerializedObject(ui);
        so.FindProperty("trailColorPalette").objectReferenceValue = palette;
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AssignTrailPalette] Palette assigned and CustomizationScene saved.");
    }

    private static void AssignToGameScene(TrailColorPalette palette)
    {
        string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("[AssignTrailPalette] PlayerController not found in SampleScene.");
            return;
        }

        var so = new SerializedObject(player);
        so.FindProperty("trailColorPalette").objectReferenceValue = palette;
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AssignTrailPalette] Palette assigned and SampleScene saved.");
    }
}
