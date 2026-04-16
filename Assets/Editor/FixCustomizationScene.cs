using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class FixCustomizationScene
{
    public static void Execute()
    {
        string palettePath = "Assets/ScriptableObjects/TrailColorPalette.asset";
        var palette = AssetDatabase.LoadAssetAtPath<TrailColorPalette>(palettePath);
        if (palette == null)
        {
            Debug.LogError($"[Fix] Palette not found at {palettePath}");
            return;
        }

        // Work on the currently active scene — must already be CustomizationScene
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Debug.Log($"[Fix] Active scene: {scene.path}");

        var ui = Object.FindFirstObjectByType<CustomizationUI>();
        if (ui == null)
        {
            Debug.LogError("[Fix] CustomizationUI not found in active scene.");
            return;
        }

        var so = new SerializedObject(ui);
        var prop = so.FindProperty("trailColorPalette");
        prop.objectReferenceValue = palette;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(ui);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[Fix] Done — trailColorPalette set to: {prop.objectReferenceValue?.name}");
    }
}
