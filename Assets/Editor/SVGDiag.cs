using UnityEditor;
using UnityEngine;

public static class SVGDiag
{
    public static void Execute()
    {
        string[] paths = {
            "Assets/Sprites/btn_easy.svg",
            "Assets/Sprites/btn_medium.svg",
            "Assets/Sprites/btn_hard.svg",
            "Assets/Sprites/btn_play.svg",
            "Assets/Sprites/btn_quit.svg",
            "Assets/Sprites/title_banner.svg",
            "Assets/Sprites/space_background_map.svg"
        };

        foreach (var path in paths)
        {
            Debug.Log($"[SVGDiag] === {path} ===");

            var main = AssetDatabase.LoadMainAssetAtPath(path);
            if (main != null)
                Debug.Log($"  Main asset: {main.GetType().FullName} name='{main.name}'");
            else
                Debug.Log($"  Main asset: NULL");

            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in all)
            {
                if (a == null) continue;
                Debug.Log($"  Sub-asset: {a.GetType().FullName} name='{a.name}' isSprite={a is Sprite}");

                if (a is Texture2D tex)
                    Debug.Log($"    Texture2D: {tex.width}x{tex.height}");
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Debug.Log($"  LoadAssetAtPath<Sprite>: {(sprite != null ? sprite.name : "NULL")}");
        }
    }
}
