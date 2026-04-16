// CustomizationSceneWire — one-shot editor setup for CustomizationScene.
// Run via the menu: Tools > Wire Customization Scene
// Creates Background + ShipPreview world objects, adds CustomizationUI to
// Canvas, and populates all 12 trail color entries. Safe to re-run.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class CustomizationSceneWire
{
    private const string ScenePath = "Assets/Scenes/CustomizationScene.unity";

    [MenuItem("Tools/Wire Customization Scene")]
    public static void Run()
    {
        // Make sure the scene is open and active
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (EditorUtility.DisplayDialog("Switch Scene?",
                $"This will open {ScenePath}. Any unsaved changes in the current scene will be lost.",
                "OK", "Cancel"))
            {
                EditorSceneManager.OpenScene(ScenePath);
                scene = EditorSceneManager.GetActiveScene();
            }
            else return;
        }

        WireScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CustomizationSceneWire] Scene wired and saved.");
    }

    private static void WireScene()
    {
        SetupBackground();
        var trailRenderer = SetupShipPreview();
        SetupCustomizationUI(trailRenderer);
    }

    // -------------------------------------------------------------------------
    // Background

    private static void SetupBackground()
    {
        var existing = GameObject.Find("Background");
        if (existing == null)
        {
            existing = new GameObject("Background");
            Undo.RegisterCreatedObjectUndo(existing, "Create Background");
        }

        var sr = existing.GetComponent<SpriteRenderer>();
        if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(existing);

        var sprite = LoadSprite("Assets/Sprites/space_background_map.svg");
        if (sprite != null) sr.sprite = sprite;
        else Debug.LogWarning("[CustomizationSceneWire] space_background_map.svg not found.");

        sr.sortingOrder = -1;
        existing.transform.position = Vector3.zero;
    }

    // -------------------------------------------------------------------------
    // ShipPreview + TrailRenderer

    private static TrailRenderer SetupShipPreview()
    {
        var preview = GameObject.Find("ShipPreview");
        if (preview == null)
        {
            preview = new GameObject("ShipPreview");
            Undo.RegisterCreatedObjectUndo(preview, "Create ShipPreview");
        }

        // SpriteRenderer for the ship
        var sr = preview.GetComponent<SpriteRenderer>();
        if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(preview);

        var shipSprite = LoadSprite("Assets/Sprites/player_ship.svg");
        if (shipSprite != null) sr.sprite = shipSprite;
        else Debug.LogWarning("[CustomizationSceneWire] player_ship.svg not found.");

        sr.sortingOrder = 1;
        preview.transform.position = new Vector3(0f, 0f, 0f);

        // ShipPreviewAnimator
        if (preview.GetComponent<ShipPreviewAnimator>() == null)
            Undo.AddComponent<ShipPreviewAnimator>(preview);

        // TrailPreview child
        var trailChild = preview.transform.Find("TrailPreview");
        if (trailChild == null)
        {
            var trailGO = new GameObject("TrailPreview");
            Undo.RegisterCreatedObjectUndo(trailGO, "Create TrailPreview");
            Undo.SetTransformParent(trailGO.transform, preview.transform, "Parent TrailPreview");
            trailGO.transform.localPosition = Vector3.zero;
            trailChild = trailGO.transform;
        }

        var trail = trailChild.GetComponent<TrailRenderer>();
        if (trail == null) trail = Undo.AddComponent<TrailRenderer>(trailChild.gameObject);

        // Trail settings — short, slim, fades quickly
        trail.time = 0.6f;
        trail.minVertexDistance = 0.05f;
        trail.startWidth = 0.3f;
        trail.endWidth   = 0f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        // Default gradient (Default/blue — index 11)
        ApplyDefaultGradient(trail);

        EditorUtility.SetDirty(trail);
        return trail;
    }

    private static void ApplyDefaultGradient(TrailRenderer trail)
    {
        ColorUtility.TryParseHtmlString("#aaccff", out Color tip);
        ColorUtility.TryParseHtmlString("#1a5aaa", out Color mid);

        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(tip, 0f),
                new GradientColorKey(mid, 0.5f),
                new GradientColorKey(mid, 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.7f, 0.5f),
                new GradientAlphaKey(0f,   1f),
            }
        );
        trail.colorGradient = gradient;
    }

    // -------------------------------------------------------------------------
    // CustomizationUI component on Canvas

    private static void SetupCustomizationUI(TrailRenderer trailRenderer)
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[CustomizationSceneWire] Canvas not found in scene.");
            return;
        }

        var ui = canvas.GetComponent<CustomizationUI>();
        if (ui == null) ui = Undo.AddComponent<CustomizationUI>(canvas);

        var so = new SerializedObject(ui);

        // Wire UI references
        AssignTMPField(so, "titleLabel",      "Canvas/SafeAreaContainer/TitleLabel");
        AssignTMPField(so, "subLabel",        "Canvas/SafeAreaContainer/SubLabel");
        AssignTransformField(so, "colorGridParent", "Canvas/SafeAreaContainer/ColorGrid");
        AssignButtonField(so, "confirmButton","Canvas/SafeAreaContainer/ConfirmButton");
        AssignButtonField(so, "backButton",   "Canvas/SafeAreaContainer/BackButton");

        // Swatch prefab
        var swatchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ColorSwatchPrefab.prefab");
        if (swatchPrefab != null)
        {
            var prefabProp = so.FindProperty("colorSwatchPrefab");
            if (prefabProp != null) prefabProp.objectReferenceValue = swatchPrefab;
        }
        else Debug.LogWarning("[CustomizationSceneWire] ColorSwatchPrefab.prefab not found.");

        // TrailRenderer on ShipPreview
        if (trailRenderer != null)
        {
            var trailProp = so.FindProperty("trailPreview");
            if (trailProp != null) trailProp.objectReferenceValue = trailRenderer;
        }

        // Populate the 12 trail colors
        PopulateTrailColors(so);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);
    }

    // -------------------------------------------------------------------------
    // Helpers for wiring serialized fields

    private static void AssignTMPField(SerializedObject so, string fieldName, string goPath)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null) { Debug.LogWarning($"[Wire] Field '{fieldName}' not found."); return; }

        var go = GameObject.Find(goPath);
        if (go == null) { Debug.LogWarning($"[Wire] GameObject '{goPath}' not found."); return; }

        var tmp = go.GetComponent<TMPro.TMP_Text>();
        prop.objectReferenceValue = tmp;
    }

    private static void AssignTransformField(SerializedObject so, string fieldName, string goPath)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null) { Debug.LogWarning($"[Wire] Field '{fieldName}' not found."); return; }

        var go = GameObject.Find(goPath);
        if (go == null) { Debug.LogWarning($"[Wire] GameObject '{goPath}' not found."); return; }

        prop.objectReferenceValue = go.transform;
    }

    private static void AssignButtonField(SerializedObject so, string fieldName, string goPath)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null) { Debug.LogWarning($"[Wire] Field '{fieldName}' not found."); return; }

        var go = GameObject.Find(goPath);
        if (go == null) { Debug.LogWarning($"[Wire] GameObject '{goPath}' not found."); return; }

        var btn = go.GetComponent<Button>();
        prop.objectReferenceValue = btn;
    }

    // -------------------------------------------------------------------------
    // 12 trail colors

    private static void PopulateTrailColors(SerializedObject so)
    {
        // Each entry: name, tipHex, midHex
        var colors = new (string name, string tip, string mid)[]
        {
            ("Inferno", "#ff8800", "#ff2200"),
            ("Solar",   "#ffff88", "#ff9900"),
            ("Venom",   "#aaffaa", "#22cc00"),
            ("Arctic",  "#aaeeff", "#0066ff"),
            ("Void",    "#cc88ff", "#5500cc"),
            ("Rose",    "#ffaacc", "#cc0055"),
            ("Glacier", "#aaffee", "#00aa88"),
            ("Crimson", "#ff8888", "#aa0011"),
            ("Ghost",   "#ffffff", "#aaaacc"),
            ("Copper",  "#ffcc88", "#cc4400"),
            ("Nebula",  "#ee88ff", "#8800cc"),
            ("Default", "#aaccff", "#1a5aaa"),
        };

        var listProp = so.FindProperty("trailColors");
        if (listProp == null)
        {
            Debug.LogWarning("[CustomizationSceneWire] 'trailColors' property not found on CustomizationUI.");
            return;
        }

        listProp.arraySize = colors.Length;

        for (int i = 0; i < colors.Length; i++)
        {
            var element = listProp.GetArrayElementAtIndex(i);

            element.FindPropertyRelative("colorName").stringValue = colors[i].name;

            ColorUtility.TryParseHtmlString(colors[i].tip, out Color tipColor);
            ColorUtility.TryParseHtmlString(colors[i].mid, out Color midColor);

            SerializedPropertyToColor(element.FindPropertyRelative("tipColor"), tipColor);
            SerializedPropertyToColor(element.FindPropertyRelative("midColor"), midColor);
        }
    }

    private static void SerializedPropertyToColor(SerializedProperty prop, Color c)
    {
        if (prop == null) return;
        prop.colorValue = c;
    }

    // -------------------------------------------------------------------------
    // Sprite loading — SVG assets may have sub-objects; try both approaches

    private static Sprite LoadSprite(string path)
    {
        // Try direct Sprite load first
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;

        // SVGs sometimes import as a Texture2D main asset with a Sprite sub-asset
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in allAssets)
        {
            if (asset is Sprite s) return s;
        }

        return null;
    }
}
