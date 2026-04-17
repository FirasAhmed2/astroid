// HUDController — the in-game heads-up display that lives on a Canvas.
// Shows the live score and the active difficulty badge while the player is alive.
// Hides itself whenever we're not in the Playing state.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    // UI Elements — Score
    [Header("Score")]
    [Tooltip("The background panel image behind the live score number.")]
    [SerializeField] private Image scorePanelImage;

    [Tooltip("The TMP text that displays the current score number.")]
    [SerializeField] private TMP_Text scoreText;

    // UI Elements — Difficulty badges (only one is visible at a time)
    [Header("Difficulty Images")]
    [Tooltip("Image shown when Easy difficulty is active.")]
    [SerializeField] private Image difficultyImageEasy;

    [Tooltip("Image shown when Medium difficulty is active.")]
    [SerializeField] private Image difficultyImageMedium;

    [Tooltip("Image shown when Hard difficulty is active.")]
    [SerializeField] private Image difficultyImageHard;

    // Difficulty config references — drag each SO here so we can compare against the active one
    [Header("Difficulty Configs")]
    [Tooltip("The Easy DifficultyConfig ScriptableObject.")]
    [SerializeField] private DifficultyConfig easyConfig;

    [Tooltip("The Medium DifficultyConfig ScriptableObject.")]
    [SerializeField] private DifficultyConfig mediumConfig;

    [Tooltip("The Hard DifficultyConfig ScriptableObject.")]
    [SerializeField] private DifficultyConfig hardConfig;

    // References
    [Header("References")]
    [Tooltip("The root panel to show/hide. If left empty, uses this GameObject.")]
    [SerializeField] private GameObject hudPanel;

    // Formatting
    private const string ScorePrefix = "Score: ";

    // -------------------------------------------------------------------------
    // Setup

    private void Awake()
    {
        if (hudPanel == null)
            hudPanel = gameObject;

        ValidateReferences();
    }

    private void Start()
    {
        if (GameManager.Instance == null) return;

        // Subscribe once in Start so it survives the panel being toggled off
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        GameManager.Instance.OnScoreUpdated += HandleScoreUpdated;

        // Sync to whatever state we're joining mid-stream
        SyncToCurrentState();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.Instance.OnScoreUpdated -= HandleScoreUpdated;
    }

    private void ValidateReferences()
    {
        if (scoreText == null)
            Debug.LogWarning("[HUDController] Score text is not assigned.");

        if (scorePanelImage == null)
            Debug.LogWarning("[HUDController] Score panel image is not assigned.");

        if (difficultyImageEasy == null || difficultyImageMedium == null || difficultyImageHard == null)
            Debug.LogWarning("[HUDController] One or more difficulty images are not assigned.");

        if (easyConfig == null || mediumConfig == null || hardConfig == null)
            Debug.LogWarning("[HUDController] One or more difficulty configs are not assigned.");
    }

    // -------------------------------------------------------------------------
    // State listener

    private void HandleGameStateChanged(GameState newState)
    {
        bool isPlaying = newState == GameState.Playing;
        hudPanel.SetActive(isPlaying);

        if (isPlaying)
            RefreshDifficultyBadge();
    }

    // -------------------------------------------------------------------------
    // Score listener

    private void HandleScoreUpdated(int newScore)
    {
        if (scoreText == null) return;

        scoreText.text = ScorePrefix + newScore;
    }

    // -------------------------------------------------------------------------
    // Helpers

    /// <summary>
    /// Shows the badge image that matches the active difficulty, hides the other two.
    /// Called whenever we enter Playing so it stays correct across restarts.
    /// </summary>
    private void RefreshDifficultyBadge()
    {
        var config = GameManager.Instance.ActiveDifficultyConfig;

        if (difficultyImageEasy != null)
            difficultyImageEasy.gameObject.SetActive(config == easyConfig);

        if (difficultyImageMedium != null)
            difficultyImageMedium.gameObject.SetActive(config == mediumConfig);

        if (difficultyImageHard != null)
            difficultyImageHard.gameObject.SetActive(config == hardConfig);
    }

    /// <summary>
    /// Matches the HUD visibility and badge to the current game state.
    /// Useful when this object is enabled after the state has already been set.
    /// </summary>
    private void SyncToCurrentState()
    {
        var state = GameManager.Instance.CurrentState;
        bool isPlaying = state == GameState.Playing;

        hudPanel.SetActive(isPlaying);

        if (isPlaying)
        {
            RefreshDifficultyBadge();
            HandleScoreUpdated(GameManager.Instance.CurrentScore);
        }
    }
}
