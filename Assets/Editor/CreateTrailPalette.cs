using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CreateTrailPalette
{
    public static void Execute()
    {
        var palette = ScriptableObject.CreateInstance<TrailColorPalette>();

        palette.colors = new List<TrailColorPalette.Entry>
        {
            MakeEntry("Inferno", "#FF8800", "#FF2200"),
            MakeEntry("Solar",   "#FFFF88", "#FF9900"),
            MakeEntry("Venom",   "#AAFFAA", "#22CC00"),
            MakeEntry("Arctic",  "#AAEEFF", "#0066FF"),
            MakeEntry("Void",    "#CC88FF", "#5500CC"),
            MakeEntry("Rose",    "#FFAACC", "#CC0055"),
            MakeEntry("Glacier", "#AAFFEE", "#00AA88"),
            MakeEntry("Crimson", "#FF8888", "#AA0011"),
            MakeEntry("Ghost",   "#FFFFFF", "#AAAACC"),
            MakeEntry("Copper",  "#FFCC88", "#CC4400"),
            MakeEntry("Nebula",  "#EE88FF", "#8800CC"),
            MakeEntry("Default", "#AACCFF", "#1A5AAA"),
        };

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

        string path = "Assets/ScriptableObjects/TrailColorPalette.asset";
        AssetDatabase.CreateAsset(palette, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Setup] TrailColorPalette created at {path} with {palette.colors.Count} colors.");
    }

    private static TrailColorPalette.Entry MakeEntry(string name, string tipHex, string midHex)
    {
        ColorUtility.TryParseHtmlString(tipHex, out Color tip);
        ColorUtility.TryParseHtmlString(midHex, out Color mid);
        return new TrailColorPalette.Entry { colorName = name, tipColor = tip, midColor = mid };
    }
}
