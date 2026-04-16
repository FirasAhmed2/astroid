// MainMenuSceneBuilder — one-click editor tool that assembles the MainMenu
// scene from scratch. Run it via Tools > Build Main Menu Scene. It creates
// every GameObject, wires sprites, sets up the Canvas + EventSystem, and
// hooks all button onClick events to MainMenuUI.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using TMPro;

public static class MainMenuSceneBuilder
{
    // Sprite paths
    private const string BackgroundSprite = "Assets/Sprites/space_background_map.svg";
    private const string TitleSprite      = "Assets/Sprites/title_banner.svg";
    private const string BtnPlaySprite    = "Assets/Sprites/btn_play.svg";
    private const string BtnEasySprite    = "Assets/Sprites/btn_easy.svg";
    private const string BtnMediumSprite  = "Assets/Sprites/btn_medium.svg";
    private const string BtnHardSprite    = "Assets/Sprites/btn_hard.svg";
    private const string BtnQuitSprite    = "Assets/Sprites/btn_quit.svg";

    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/Build Main Menu Scene")]
    public static void Build()
    {
        // Open the scene so all changes land in the right place
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Nuke everything that's already in the scene
        foreach (var root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        // ── Main Camera ──
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var cam = cameraGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.12f, 1f);
        cam.depth = -1f;
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        cameraGo.AddComponent<AudioListener>();

        // Add URP camera data so it renders properly
        var urpCam = cameraGo.AddComponent<UniversalAdditionalCameraData>();

        // ── Global Light 2D ──
        var lightGo = new GameObject("Global Light 2D");
        var light2D = lightGo.AddComponent<Light2D>();
        light2D.lightType = Light2D.LightType.Global;
        light2D.intensity = 1f;

        // ── Background ──
        var bgGo = new GameObject("Background");
        var bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite(BackgroundSprite);
        bgSr.sortingOrder = -1;
        bgGo.transform.position = Vector3.zero;

        // ── EventSystem (Input System, not legacy) ──
        var eventGo = new GameObject("EventSystem");
        eventGo.AddComponent<EventSystem>();
        eventGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // ── Canvas ──
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // ── TitleBanner ──
        var titleGo = CreateUIImage("TitleBanner", canvasGo.transform, LoadSprite(TitleSprite));
        var titleRect = titleGo.GetComponent<RectTransform>();
        SetAnchor(titleRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        titleRect.anchoredPosition = new Vector2(0f, -80f);
        titleRect.sizeDelta = new Vector2(900f, 200f);

        // ── DifficultyLabel ──
        var labelGo = CreateTMPText("DifficultyLabel", canvasGo.transform, "SELECT DIFFICULTY");
        var labelRect = labelGo.GetComponent<RectTransform>();
        SetAnchorCenter(labelRect);
        labelRect.anchoredPosition = new Vector2(0f, 80f);
        labelRect.sizeDelta = new Vector2(500f, 50f);

        var labelTmp = labelGo.GetComponent<TMP_Text>();
        labelTmp.fontSize = 28;
        labelTmp.color = HexColor("#8899CC");
        labelTmp.alignment = TextAlignmentOptions.Center;

        // ── ButtonGroup ──
        var groupGo = new GameObject("ButtonGroup", typeof(RectTransform));
        groupGo.transform.SetParent(canvasGo.transform, false);
        var groupRect = groupGo.GetComponent<RectTransform>();
        SetAnchorCenter(groupRect);
        groupRect.anchoredPosition = Vector2.zero;
        groupRect.sizeDelta = new Vector2(620f, 80f);

        var hlg = groupGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 40f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // ── Difficulty Buttons ──
        var easyGo   = CreateButton("EasyButton",   groupGo.transform, LoadSprite(BtnEasySprite),   180f, 68f);
        var mediumGo = CreateButton("MediumButton", groupGo.transform, LoadSprite(BtnMediumSprite), 180f, 68f);
        var hardGo   = CreateButton("HardButton",   groupGo.transform, LoadSprite(BtnHardSprite),   180f, 68f);

        // ── PlayButton ──
        var playGo = CreateButton("PlayButton", canvasGo.transform, LoadSprite(BtnPlaySprite), 300f, 82f);
        var playRect = playGo.GetComponent<RectTransform>();
        SetAnchorCenter(playRect);
        playRect.anchoredPosition = new Vector2(0f, -100f);

        // ── QuitButton ──
        var quitGo = CreateButton("QuitButton", canvasGo.transform, LoadSprite(BtnQuitSprite), 180f, 58f);
        var quitRect = quitGo.GetComponent<RectTransform>();
        SetAnchorCenter(quitRect);
        quitRect.anchoredPosition = new Vector2(0f, -195f);

        // ── MainMenuUI component on Canvas ──
        var menuUI = canvasGo.AddComponent<MainMenuUI>();
        var menuSO = new SerializedObject(menuUI);

        // Wire difficulty configs
        WireConfig(menuSO, "easyConfig",   "Assets/ScriptableObjects/Difficulty/Easy.asset");
        WireConfig(menuSO, "mediumConfig", "Assets/ScriptableObjects/Difficulty/Medium.asset");
        WireConfig(menuSO, "hardConfig",   "Assets/ScriptableObjects/Difficulty/Hard.asset");

        // Wire button references
        menuSO.FindProperty("easyButton").objectReferenceValue   = easyGo.GetComponent<Button>();
        menuSO.FindProperty("mediumButton").objectReferenceValue = mediumGo.GetComponent<Button>();
        menuSO.FindProperty("hardButton").objectReferenceValue   = hardGo.GetComponent<Button>();
        menuSO.FindProperty("playButton").objectReferenceValue   = playGo.GetComponent<Button>();
        menuSO.FindProperty("quitButton").objectReferenceValue   = quitGo.GetComponent<Button>();

        menuSO.ApplyModifiedPropertiesWithoutUndo();

        // ── Save ──
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[MainMenuSceneBuilder] MainMenu scene built successfully.");
    }

    // -------------------------------------------------------------------------
    // Helpers

    private static Sprite LoadSprite(string path)
    {
        // SVG importer sometimes nests the sprite as a sub-asset
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;

        foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (sub is Sprite s) return s;
        }

        Debug.LogWarning($"[MainMenuSceneBuilder] No sprite found at '{path}'.");
        return null;
    }

    private static GameObject CreateUIImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;

        return go;
    }

    private static GameObject CreateTMPText(string name, Transform parent, string text)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.raycastTarget = false;

        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, Sprite sprite, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = true;

        // No color tint — show sprites cleanly
        var btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        return go;
    }

    private static void SetAnchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetAnchorCenter(RectTransform rect)
    {
        SetAnchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    }

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var color);
        return color;
    }

    private static void WireConfig(SerializedObject so, string propertyName, string assetPath)
    {
        var config = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(assetPath);
        if (config == null)
        {
            Debug.LogWarning($"[MainMenuSceneBuilder] DifficultyConfig not found at '{assetPath}'.");
            return;
        }

        so.FindProperty(propertyName).objectReferenceValue = config;
    }
}
