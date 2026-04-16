using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class FixSampleScene
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

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Debug.Log($"[Fix] Active scene: {scene.path}");

        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("[Fix] PlayerController not found in active scene.");
            return;
        }

        var so = new SerializedObject(player);
        var prop = so.FindProperty("trailColorPalette");
        prop.objectReferenceValue = palette;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(player);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[Fix] Done — trailColorPalette set to: {prop.objectReferenceValue?.name}");
    }
}
