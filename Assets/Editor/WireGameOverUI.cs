// One-shot editor helper — wires the GameOverUI serialized fields after
// SetupGameOverUI.cs has already built the hierarchy.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class WireGameOverUI
{
    public static void Execute() => Wire();

    [MenuItem("Tools/Wire GameOver UI Fields")]
    public static void Wire()
    {
        // Find the panel using Resources (works on inactive objects)
        var panelGO = GameObject.Find("HUDCanvas/GameOverPanel");
        if (panelGO == null)
        {
            // GameObject.Find skips inactive — use transform search instead
            var hudCanvas = GameObject.Find("HUDCanvas");
            if (hudCanvas != null)
            {
                var t = hudCanvas.transform.Find("GameOverPanel");
                if (t != null) panelGO = t.gameObject;
            }
        }

        if (panelGO == null)
        {
            Debug.LogError("[WireGameOverUI] GameOverPanel not found!");
            return;
        }

        var ui = panelGO.GetComponent<GameOverUI>();
        if (ui == null)
        {
            Debug.LogError("[WireGameOverUI] GameOverUI component not found on GameOverPanel!");
            return;
        }

        var so = new SerializedObject(ui);

        // panel field — point at itself
        so.FindProperty("panel").objectReferenceValue = panelGO;

        // finalScoreText — inside ScorePanel
        var scoreTextT = panelGO.transform.Find("ScorePanel/FinalScoreText");
        if (scoreTextT != null)
            so.FindProperty("finalScoreText").objectReferenceValue =
                scoreTextT.GetComponent<TextMeshProUGUI>();
        else
            Debug.LogWarning("[WireGameOverUI] FinalScoreText not found.");

        // retryButton
        var retryT = panelGO.transform.Find("RetryButton");
        if (retryT != null)
            so.FindProperty("retryButton").objectReferenceValue =
                retryT.GetComponent<Button>();
        else
            Debug.LogWarning("[WireGameOverUI] RetryButton not found.");

        // mainMenuButton
        var menuT = panelGO.transform.Find("MainMenuButton");
        if (menuT != null)
            so.FindProperty("mainMenuButton").objectReferenceValue =
                menuT.GetComponent<Button>();
        else
            Debug.LogWarning("[WireGameOverUI] MainMenuButton not found.");

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[WireGameOverUI] All fields wired successfully!");
    }
}
