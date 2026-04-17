// GameOverUI — the "you died" screen. Fades in with a punchy scale animation,
// shows the final score, and gives the player two choices: retry the same
// difficulty or bail back to the main menu. No external tween library needed —
// the scale punch runs on a simple coroutine with eased lerps.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    // UI elements
    [Header("UI Elements")]
    [Tooltip("Text that displays the player's final score.")]
    [SerializeField] private TMP_Text finalScoreText;

    [Tooltip("Button that restarts the game with the same difficulty.")]
    [SerializeField] private Button retryButton;

    [Tooltip("Button that returns to the main menu / difficulty select.")]
    [SerializeField] private Button mainMenuButton;

    // References
    [Header("References")]
    [Tooltip("The root panel to show/hide. If left empty, uses this GameObject.")]
    [SerializeField] private GameObject panel;

    // Animation
    [Header("Animation")]
    [Tooltip("How long the scale-punch intro takes in seconds.")]
    [SerializeField] private float punchDuration = 0.35f;

    [Tooltip("How far past 1.0 the panel overshoots before settling (e.g. 0.15 = 115% scale).")]
    [SerializeField] private float punchOvershoot = 0.15f;

    // State
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Coroutine animationCoroutine;

    // Formatting
    private const string ScorePrefix = "Final Score: ";

    // -------------------------------------------------------------------------
    // Setup

    private void Awake()
    {
        if (panel == null)
            panel = gameObject;

        panelRect = panel.GetComponent<RectTransform>();

        // Grab or add a CanvasGroup so we can fade the whole panel
        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        ValidateReferences();
    }

    private void Start()
    {
        // Wire button listeners once — they stay connected for the scene lifetime
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryPressed);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuPressed);

        // Subscribe to state changes before hiding, otherwise OnEnable
        // never fires again and we'd miss the GameOver event
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        // Start hidden — we only show on GameOver
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryPressed);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuPressed);

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void ValidateReferences()
    {
        if (finalScoreText == null)
            Debug.LogWarning("[GameOverUI] Final score text is not assigned.");

        if (retryButton == null)
            Debug.LogWarning("[GameOverUI] Retry button is not assigned.");

        if (mainMenuButton == null)
            Debug.LogWarning("[GameOverUI] Main Menu button is not assigned.");

        if (panelRect == null)
            Debug.LogWarning("[GameOverUI] Panel is missing a RectTransform — scale animation won't work.");
    }

    // -------------------------------------------------------------------------
    // State listener

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.GameOver)
        {
            ShowWithAnimation();
        }
        else
        {
            Hide();
        }
    }

    // -------------------------------------------------------------------------
    // Show / hide

    private void ShowWithAnimation()
    {
        RefreshFinalScore();

        panel.SetActive(true);

        // Kill any running animation so they don't overlap on rapid state changes
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ScalePunchRoutine());
    }

    private void Hide()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        panel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Animation — scale punch + fade in

    /// <summary>
    /// Scales the panel from 0 → (1 + overshoot) → 1 while fading alpha 0 → 1.
    /// Uses a smooth-step ease so it feels snappy, not linear.
    /// </summary>
    private IEnumerator ScalePunchRoutine()
    {
        float elapsed = 0f;

        // Start from nothing
        SetScale(0f);
        SetAlpha(0f);

        while (elapsed < punchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / punchDuration);

            // Smooth step gives a nice ease-in-out
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            // Scale overshoots then settles: goes up to (1 + overshoot) then back to 1
            float scale = EvaluatePunchCurve(smooth);

            SetScale(scale);
            SetAlpha(smooth);

            yield return null;
        }

        // Snap to final values so there's no floating point drift
        SetScale(1f);
        SetAlpha(1f);

        animationCoroutine = null;
    }

    /// <summary>
    /// Maps a 0-1 input to a scale that peaks above 1 then returns to 1.
    /// Think of it like a ball that bounces slightly past its target.
    /// At t=0.6 we hit peak overshoot, then ease back down.
    /// </summary>
    private float EvaluatePunchCurve(float t)
    {
        float peakTime = 0.6f;

        if (t < peakTime)
        {
            // Rising phase: 0 → (1 + overshoot)
            float rising = t / peakTime;
            return Mathf.Lerp(0f, 1f + punchOvershoot, rising);
        }
        else
        {
            // Settling phase: (1 + overshoot) → 1
            float settling = (t - peakTime) / (1f - peakTime);
            return Mathf.Lerp(1f + punchOvershoot, 1f, settling);
        }
    }

    private void SetScale(float scale)
    {
        if (panelRect != null)
            panelRect.localScale = new Vector3(scale, scale, scale);
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }

    // -------------------------------------------------------------------------
    // Score display

    private void RefreshFinalScore()
    {
        if (finalScoreText == null) return;

        if (GameManager.Instance != null)
            finalScoreText.text = ScorePrefix + GameManager.Instance.CurrentScore;
        else
            finalScoreText.text = ScorePrefix + "0";
    }

    // -------------------------------------------------------------------------
    // Button handlers

    private void OnRetryPressed()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameOverUI] No GameManager instance found.");
            return;
        }

        GameManager.Instance.RestartGame();
    }

    private void OnMainMenuPressed()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameOverUI] No GameManager instance found.");
            return;
        }

        GameManager.Instance.GoToMainMenu();
    }
}
