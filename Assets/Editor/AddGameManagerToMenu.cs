using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AddGameManagerToMenu
{
    public static void Execute()
    {
        // Make sure we're in the MainMenu scene
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "MainMenu")
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        }

        // Check if one already exists
        if (Object.FindAnyObjectByType<GameManager>() != null)
        {
            Debug.Log("[AddGM] GameManager already exists in the scene.");
            return;
        }

        // Create the GameManager object
        var gmGo = new GameObject("_GameManager");
        var gm = gmGo.AddComponent<GameManager>();

        // Wire the scene names via SerializedObject
        var so = new SerializedObject(gm);
        so.FindProperty("gameSceneName").stringValue = "SampleScene";
        so.FindProperty("menuSceneName").stringValue = "MainMenu";
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(gmGo);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[AddGM] GameManager added to MainMenu scene.");
    }
}
