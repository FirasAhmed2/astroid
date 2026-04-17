// TrailColorPalette — ScriptableObject that owns the full list of selectable
// trail colors. Both CustomizationUI (preview) and PlayerController (in-game)
// pull from this same asset so they can never drift out of sync.

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrailColorPalette", menuName = "Asteroid Dodger/Trail Color Palette")]
public class TrailColorPalette : ScriptableObject
{
    // One color option — shown as a swatch chip in the customization screen
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Short display name shown on the swatch chip (e.g. 'Inferno').")]
        public string colorName;

        [Tooltip("Bright tip color — the part closest to the ship.")]
        public Color tipColor = Color.white;

        [Tooltip("Mid body color — fades toward this before going transparent.")]
        public Color midColor = Color.white;
    }

    // Colors
    [Header("Colors")]
    [Tooltip("All selectable trail colors, in order. Index must match 'SelectedTrailIndex' in PlayerPrefs.")]
    public List<Entry> colors = new List<Entry>();

    // -------------------------------------------------------------------------
    // Editor default — called when the asset is first created via the menu

    // Unity calls Reset() on a fresh ScriptableObject asset, same as on components.
    // This pre-fills the 12 default colors so the asset is ready to use immediately.
    private void Reset()
    {
        colors = new List<Entry>
        {
            MakeEntry("Inferno",  "#FF8800", "#FF2200"),
            MakeEntry("Solar",    "#FFFF88", "#FF9900"),
            MakeEntry("Venom",    "#AAFFAA", "#22CC00"),
            MakeEntry("Arctic",   "#AAEEFF", "#0066FF"),
            MakeEntry("Void",     "#CC88FF", "#5500CC"),
            MakeEntry("Rose",     "#FFAACC", "#CC0055"),
            MakeEntry("Glacier",  "#AAFFEE", "#00AA88"),
            MakeEntry("Crimson",  "#FF8888", "#AA0011"),
            MakeEntry("Ghost",    "#FFFFFF", "#AAAACC"),
            MakeEntry("Copper",   "#FFCC88", "#CC4400"),
            MakeEntry("Nebula",   "#EE88FF", "#8800CC"),
            MakeEntry("Default",  "#AACCFF", "#1A5AAA"),
        };
    }

    private static Entry MakeEntry(string name, string tipHex, string midHex)
    {
        ColorUtility.TryParseHtmlString(tipHex, out Color tip);
        ColorUtility.TryParseHtmlString(midHex, out Color mid);
        return new Entry { colorName = name, tipColor = tip, midColor = mid };
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the entry at the given index, clamped to a valid range.
    /// Falls back to the last entry if the list is empty.
    /// </summary>
    public Entry GetEntry(int index)
    {
        if (colors == null || colors.Count == 0)
        {
            Debug.LogWarning("[TrailColorPalette] Color list is empty — can't return an entry.");
            return null;
        }

        index = Mathf.Clamp(index, 0, colors.Count - 1);
        return colors[index];
    }

    /// <summary>
    /// Builds and returns the standard three-stop gradient for a given entry.
    /// Shared so both preview and in-game trail look exactly the same.
    /// </summary>
    public Gradient BuildGradient(Entry entry)
    {
        if (entry == null) return new Gradient();

        var gradient = new Gradient();

        // tip = full alpha, mid = 70%, tail = transparent
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(entry.tipColor, 0f),
                new GradientColorKey(entry.midColor, 0.5f),
                new GradientColorKey(entry.midColor, 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.7f, 0.5f),
                new GradientAlphaKey(0f,   1f),
            }
        );

        return gradient;
    }
}
