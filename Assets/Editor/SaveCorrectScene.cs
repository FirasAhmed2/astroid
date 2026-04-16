using UnityEditor;
using UnityEditor.SceneManagement;

public static class SaveCorrectScene
{
    public static void Execute()
    {
        var scene = EditorSceneManager.GetActiveScene();
        bool saved = EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity");
        UnityEngine.Debug.Log(saved
            ? "[SaveCorrectScene] Saved to Assets/Scenes/SampleScene.unity"
            : "[SaveCorrectScene] Save FAILED!");
    }
}
