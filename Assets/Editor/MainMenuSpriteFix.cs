// MainMenuSpriteFix — forces SVG reimport, then assigns sprites to the
// existing MainMenu scene UI elements. Run via Tools > Fix Main Menu Sprites.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuSpriteFix
{
    public static void Execute()
    {
        // Force reimport all SVGs so Unity generates fresh sprite sub-assets
        string[] svgPaths = {
            "Assets/Sprites/title_banner.svg",
            "Assets/Sprites/btn_play.svg",
            "Assets/Sprites/btn_easy.svg",
            "Assets/Sprites/btn_medium.svg",
            "Assets/Sprites/btn_hard.svg",
            "Assets/Sprites/btn_quit.svg",
            "Assets/Sprites/space_background_map.svg"
        };

        foreach (var path in svgPaths)
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        // Log what we find after reimport
        foreach (var path in svgPaths)
        {
            var sprite = FindSprite(path);
            Debug.Log($"[SpriteFix] {path} → {(sprite != null ? $"Sprite '{sprite.name}' ({sprite.texture.width}x{sprite.texture.height})" : "NULL")}");
        }

        // Assign to UI elements
        AssignImageSprite("Canvas/TitleBanner",              "Assets/Sprites/title_banner.svg");
        AssignImageSprite("Canvas/ButtonGroup/EasyButton",   "Assets/Sprites/btn_easy.svg");
        AssignImageSprite("Canvas/ButtonGroup/MediumButton", "Assets/Sprites/btn_medium.svg");
        AssignImageSprite("Canvas/ButtonGroup/HardButton",   "Assets/Sprites/btn_hard.svg");
        AssignImageSprite("Canvas/PlayButton",               "Assets/Sprites/btn_play.svg");
        AssignImageSprite("Canvas/QuitButton",               "Assets/Sprites/btn_quit.svg");

        // Assign background SpriteRenderer too, just in case
        var bgGo = GameObject.Find("Background");
        if (bgGo != null)
        {
            var sr = bgGo.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var bgSprite = FindSprite("Assets/Sprites/space_background_map.svg");
                if (bgSprite != null)
                {
                    sr.sprite = bgSprite;
                    EditorUtility.SetDirty(bgGo);
                }
            }
        }

        // Save
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log("[SpriteFix] Done — all sprites assigned.");
    }

    private static void AssignImageSprite(string goPath, string svgPath)
    {
        var go = GameObject.Find(goPath);
        if (go == null)
        {
            // Try finding by just the last part of the path
            string name = System.IO.Path.GetFileName(goPath);
            var all = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (var img in all)
            {
                if (img.gameObject.name == name)
                {
                    go = img.gameObject;
                    break;
                }
            }
        }

        if (go == null)
        {
            Debug.LogWarning($"[SpriteFix] GameObject '{goPath}' not found.");
            return;
        }

        var image = go.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning($"[SpriteFix] No Image on '{goPath}'.");
            return;
        }

        var sprite = FindSprite(svgPath);
        if (sprite == null)
        {
            Debug.LogWarning($"[SpriteFix] No sprite from '{svgPath}'.");
            return;
        }

        image.sprite = sprite;
        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(go);
        Debug.Log($"[SpriteFix] Assigned '{sprite.name}' to '{goPath}'.");
    }

    private static Sprite FindSprite(string path)
    {
        // Try direct load first
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;

        // Walk all sub-assets looking for a Sprite
        var all = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in all)
        {
            if (asset is Sprite s) return s;
        }

        // Some SVG importers put the sprite under a Texture2D — try loading that
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            // Re-walk with the texture loaded to trigger lazy import
            all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in all)
            {
                if (asset is Sprite s2) return s2;
            }
        }

        return null;
    }
}
