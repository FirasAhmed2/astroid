// CustomizationUI — color picker for the ship's engine trail. Spawns one
// ColorSwatchPrefab per trail color, lets the player pick, and shows the
// result live on the ShipPreview's TrailRenderer. Confirm starts the game;
// Back returns to the main menu.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomizationUI : MonoBehaviour
{
    // UI
    [Header("UI")]
    [Tooltip("Big title label at the top.")]
    [SerializeField] private TMP_Text titleLabel;

    [Tooltip("Smaller subtitle / instruction line.")]
    [SerializeField] private TMP_Text subLabel;

    [Tooltip("The GridLayoutGroup parent that holds all the color swatches.")]
    [SerializeField] private Transform colorGridParent;

    [Tooltip("Confirms the selected color and loads the game.")]
    [SerializeField] private Button confirmButton;

    [Tooltip("Cancels and returns to the main menu.")]
    [SerializeField] private Button backButton;

    [Tooltip("Swatch prefab — instantiated once per color option.")]
    [SerializeField] private GameObject colorSwatchPrefab;

    // Preview
    [Header("Preview")]
    [Tooltip("TrailRenderer on the ShipPreview — updated live when the player picks a color.")]
    [SerializeField] private TrailRenderer trailPreview;

    // Config
    [Header("Trail Colors")]
    [Tooltip("The shared palette asset — must match the one used by PlayerController.")]
    [SerializeField] private TrailColorPalette trailColorPalette;

    // State
    private int selectedIndex;
    private readonly List<RectTransform> swatchTransforms = new List<RectTransform>();

    // A selected swatch gets a slight scale bump so it pops visually
    private const float SelectedScale = 1.15f;
    private const float NormalScale   = 1f;

    // -------------------------------------------------------------------------
    // Setup

    private void Awake()
    {
        ValidateReferences();
        BuildColorGrid();
    }

    private void Start()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmPressed);
        if (backButton   != null) backButton.onClick.AddListener(OnBackPressed);

        int count = trailColorPalette != null ? trailColorPalette.colors.Count : 0;

        // Restore whatever the player last picked, falling back to the last entry (Default)
        selectedIndex = PlayerPrefs.GetInt("SelectedTrailIndex", Mathf.Max(0, count - 1));
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, count - 1));

        SelectColor(selectedIndex);
    }

    private void OnDisable()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmPressed);
        if (backButton   != null) backButton.onClick.RemoveListener(OnBackPressed);
    }

    private void ValidateReferences()
    {
        if (colorGridParent == null)
            Debug.LogWarning("[CustomizationUI] colorGridParent is not assigned.");
        if (colorSwatchPrefab == null)
            Debug.LogWarning("[CustomizationUI] colorSwatchPrefab is not assigned.");
        if (confirmButton == null)
            Debug.LogWarning("[CustomizationUI] confirmButton is not assigned.");
        if (backButton == null)
            Debug.LogWarning("[CustomizationUI] backButton is not assigned.");
        if (trailPreview == null)
            Debug.LogWarning("[CustomizationUI] trailPreview is not assigned — live preview won't work.");
        if (trailColorPalette == null)
            Debug.LogWarning("[CustomizationUI] trailColorPalette is not assigned — no colors will appear.");
        else if (trailColorPalette.colors == null || trailColorPalette.colors.Count == 0)
            Debug.LogWarning("[CustomizationUI] TrailColorPalette has no entries — add colors to the asset.");
    }

    // -------------------------------------------------------------------------
    // Color grid

    private void BuildColorGrid()
    {
        if (colorGridParent == null || colorSwatchPrefab == null || trailColorPalette == null) return;

        for (int i = 0; i < trailColorPalette.colors.Count; i++)
        {
            // Capture the index so the lambda closes over the right value
            int capturedIndex = i;
            TrailColorPalette.Entry entry = trailColorPalette.colors[i];

            GameObject swatchGO = Instantiate(colorSwatchPrefab, colorGridParent);
            swatchGO.name = $"Swatch_{entry.colorName}";

            // Tint the swatch image to the tip color so it reads as a color chip
            var img = swatchGO.GetComponent<Image>();
            if (img != null) img.color = entry.tipColor;

            // Update the label
            var label = swatchGO.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = entry.colorName;

            // Wire the click
            var btn = swatchGO.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => SelectColor(capturedIndex));

            var rt = swatchGO.GetComponent<RectTransform>();
            if (rt != null) swatchTransforms.Add(rt);
        }
    }

    // -------------------------------------------------------------------------
    // Selection

    /// <summary>
    /// Picks a color by index, saves to PlayerPrefs, updates the trail preview,
    /// and bumps the selected swatch's scale. Public so buttons can call it directly.
    /// </summary>
    public void SelectColor(int index)
    {
        if (trailColorPalette == null || index < 0 || index >= trailColorPalette.colors.Count) return;

        selectedIndex = index;

        // Persist right away — survives force-quits too
        PlayerPrefs.SetInt("SelectedTrailIndex", selectedIndex);
        PlayerPrefs.Save();

        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsReady)
            FirebaseManager.Instance.SaveTrailColor(index);

        // Push the new gradient to the live preview trail using the shared palette
        if (trailPreview != null)
        {
            TrailColorPalette.Entry entry = trailColorPalette.GetEntry(selectedIndex);
            trailPreview.colorGradient = trailColorPalette.BuildGradient(entry);
        }

        // Scale the selected swatch up, reset all others
        for (int i = 0; i < swatchTransforms.Count; i++)
        {
            float s = (i == selectedIndex) ? SelectedScale : NormalScale;
            swatchTransforms[i].localScale = Vector3.one * s;
        }

        AudioManager.Instance?.PlayButtonClick();
    }

    // -------------------------------------------------------------------------
    // Button handlers

    public void OnConfirmPressed()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[CustomizationUI] GameManager not found — can't start the game.");
            return;
        }

        var config = GameManager.Instance.ActiveDifficultyConfig;

        if (config == null)
        {
            Debug.LogWarning("[CustomizationUI] No difficulty config set — go back to the main menu and pick one.");
            return;
        }

        GameManager.Instance.StartGame(config);
    }

    public void OnBackPressed()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[CustomizationUI] GameManager not found — can't navigate back.");
            return;
        }

        GameManager.Instance.GoToMainMenu();
    }
}
