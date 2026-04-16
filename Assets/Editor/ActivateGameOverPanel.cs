// One-shot fix — sets GameOverPanel back to active so Unity calls Awake/Start
// on GameOverUI, which then hides the panel itself after wiring everything.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ActivateGameOverPanel
{
    public static void Execute()
    {
        var hudCanvas = GameObject.Find("HUDCanvas");
        if (hudCanvas == null) { Debug.LogError("[ActivateGameOverPanel] HUDCanvas not found!"); return; }

        var panelT = hudCanvas.transform.Find("GameOverPanel");
        if (panelT == null) { Debug.LogError("[ActivateGameOverPanel] GameOverPanel not found!"); return; }

        // Must be active at scene start so Awake + Start fire on GameOverUI.
        // GameOverUI.Start() will call panel.SetActive(false) itself after wiring buttons.
        panelT.gameObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[ActivateGameOverPanel] GameOverPanel is now active — GameOverUI.Start() will hide it at runtime.");
    }
}
