// Adds an EventSystem to the active scene if one doesn't already exist.
// Without it, Unity can't route any UI input — buttons simply won't respond.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AddEventSystem
{
    public static void Execute()
    {
        // Don't add a duplicate
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            Debug.Log("[AddEventSystem] EventSystem already exists in this scene.");
            return;
        }

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();

        // Use InputSystemUIInputModule if the Input System package is present,
        // otherwise fall back to the legacy StandaloneInputModule
        go.AddComponent<InputSystemUIInputModule>();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[AddEventSystem] EventSystem added to scene.");
    }
}
