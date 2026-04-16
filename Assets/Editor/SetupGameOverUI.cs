// Quick one-shot editor tool that builds the GameOverUI hierarchy in the
// active scene. Run it once from Tools > Setup GameOver UI, then delete
// this file (or keep it around for re-runs). It wires up all the sprites
// and sizes things for a 2048x1536 canvas.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SetupGameOverUI
{
    // Entry point for the MCP execute_script tool
    public static void Execute() => Setup();

    [MenuItem("Tools/Setup GameOver UI")]
    public static void Setup()
    {
        var hudCanvas = GameObject.Find("HUDCanvas");
        if (hudCanvas == null)
        {
            Debug.LogError("[SetupGameOverUI] HUDCanvas not found in scene!");
            return;
        }

        // Remove any leftover panel from a previous run
        var existing = hudCanvas.transform.Find("GameOverPanel");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
            Debug.Log("[SetupGameOverUI] Removed old GameOverPanel.");
        }

        // -----------------------------------------------------------------
        // GameOverPanel — full-screen, holds everything
        // -----------------------------------------------------------------
        var panelGO = CreateImage("GameOverPanel", hudCanvas.transform,
            "Assets/Sprites/Gameoverscreen/game_over_panel_bg.svg");

        panelGO.AddComponent<CanvasGroup>(); // GameOverUI.cs adds one if missing, but nice to have it pre-wired

        var panelRT = panelGO.GetComponent<RectTransform>();
        Stretch(panelRT); // fills the entire canvas

        // -----------------------------------------------------------------
        // Banner — "Game Over" title image, pinned to top-centre
        // -----------------------------------------------------------------
        var bannerGO = CreateImage("Banner", panelGO.transform,
            "Assets/Sprites/Gameoverscreen/game_over_banner.svg");
        bannerGO.GetComponent<Image>().preserveAspect = true;

        var bannerRT = bannerGO.GetComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0.5f, 1f);
        bannerRT.anchorMax = new Vector2(0.5f, 1f);
        bannerRT.pivot    = new Vector2(0.5f, 1f);
        bannerRT.anchoredPosition = new Vector2(0f, -80f);
        bannerRT.sizeDelta        = new Vector2(700f, 200f);

        // -----------------------------------------------------------------
        // ScorePanel — decorative backing for the score text
        // -----------------------------------------------------------------
        var scorePanelGO = CreateImage("ScorePanel", panelGO.transform,
            "Assets/Sprites/Gameoverscreen/game_over_score_panel.svg");

        var scorePanelRT = scorePanelGO.GetComponent<RectTransform>();
        Center(scorePanelRT, new Vector2(0f, 60f), new Vector2(500f, 100f));

        // FinalScoreText lives inside the score panel so it auto-clips to it
        var scoreTextGO = new GameObject("FinalScoreText");
        scoreTextGO.transform.SetParent(scorePanelGO.transform, false);
        var scoreText = scoreTextGO.AddComponent<TextMeshProUGUI>();
        scoreText.text      = "Final Score: 0";
        scoreText.fontSize  = 48;
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.color     = Color.white;
        Stretch(scoreTextGO.GetComponent<RectTransform>(), new Vector2(10, 10));

        // -----------------------------------------------------------------
        // RetryButton — left of centre, bottom area
        // -----------------------------------------------------------------
        var retryGO = CreateButton("RetryButton", panelGO.transform,
            "Assets/Sprites/Gameoverscreen/btn_retry.svg",
            new Vector2(-160f, -100f), new Vector2(280f, 90f));

        AddButtonLabel(retryGO.transform, "Retry");

        // -----------------------------------------------------------------
        // MainMenuButton — right of centre, bottom area
        // -----------------------------------------------------------------
        var menuGO = CreateButton("MainMenuButton", panelGO.transform,
            "Assets/Sprites/Gameoverscreen/btn_main_menu.svg",
            new Vector2(160f, -100f), new Vector2(280f, 90f));

        AddButtonLabel(menuGO.transform, "Main Menu");

        // -----------------------------------------------------------------
        // Start hidden — GameOverUI.cs controls visibility at runtime
        // -----------------------------------------------------------------
        panelGO.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = panelGO;

        Debug.Log("[SetupGameOverUI] Done! Assign the fields in GameOverUI.cs via the Inspector.");
    }

    // -------------------------------------------------------------------------
    // Helpers

    private static GameObject CreateImage(string objName, Transform parent, string spritePath)
    {
        var go  = new GameObject(objName);
        go.transform.SetParent(parent, false);
        var img    = go.AddComponent<Image>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite != null)
            img.sprite = sprite;
        else
            Debug.LogWarning($"[SetupGameOverUI] Sprite not found at: {spritePath}");
        img.color = Color.white;
        return go;
    }

    private static GameObject CreateButton(string objName, Transform parent,
        string spritePath, Vector2 anchoredPos, Vector2 size)
    {
        var go  = CreateImage(objName, parent, spritePath);
        var img = go.GetComponent<Image>();
        img.preserveAspect = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var rt = go.GetComponent<RectTransform>();
        Center(rt, anchoredPos, size);
        return go;
    }

    private static void AddButtonLabel(Transform buttonParent, string labelText)
    {
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(buttonParent, false);
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = labelText;
        tmp.fontSize  = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        Stretch(labelGO.GetComponent<RectTransform>());
    }

    // Stretch to fill parent
    private static void Stretch(RectTransform rt, Vector2 inset = default)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = inset;
        rt.offsetMax = -inset;
    }

    // Centre anchor at (0.5, 0.5) with explicit position + size
    private static void Center(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
    }
}
