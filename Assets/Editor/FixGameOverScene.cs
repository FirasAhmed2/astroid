// Fixes two remaining issues in SampleScene:
// 1. Adds an EventSystem so UI clicks actually register
// 2. Turns off Raycast Target on decorative images so they
//    can't eat click events meant for the buttons

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class FixGameOverScene
{
    public static void Execute()
    {
        FixEventSystem();
        FixRaycastTargets();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[FixGameOverScene] Done.");
    }

    private static void FixEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            Debug.Log("[FixGameOverScene] EventSystem already present — skipping.");
            return;
        }

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        Debug.Log("[FixGameOverScene] EventSystem added.");
    }

    private static void FixRaycastTargets()
    {
        // These are purely decorative — they should never block button clicks
        string[] decorativePaths = new[]
        {
            "HUDCanvas/GameOverPanel/Banner",
            "HUDCanvas/GameOverPanel/ScorePanel",
        };

        foreach (var path in decorativePaths)
        {
            var t = GameObject.Find(path)?.transform;
            // GameObject.Find skips inactive — try via parent transform if needed
            if (t == null)
            {
                var panel = GameObject.Find("HUDCanvas/GameOverPanel");
                if (panel != null)
                {
                    var name = path.Substring(path.LastIndexOf('/') + 1);
                    t = panel.transform.Find(name);
                }
            }

            if (t == null)
            {
                Debug.LogWarning($"[FixGameOverScene] Could not find '{path}'");
                continue;
            }

            var img = t.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = false;
                Debug.Log($"[FixGameOverScene] Raycast Target disabled on '{path}'");
            }
        }
    }
}
