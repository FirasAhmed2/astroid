// PowerUpHUDIndicator — shows which power-up is active and how much time is left.
// ShowPowerUp() updates the icon, name, and fill color, then starts the countdown.
// The timer bar uses unscaled delta so it drains at real speed even during slow-mo.
// A brief full-screen flash fires on collection so it never feels unresponsive.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpHUDIndicator : MonoBehaviour
{
    // UI
    [Header("UI")]
    [Tooltip("Root GameObject that gets toggled on/off as power-ups come and go.")]
    [SerializeField] private GameObject indicatorRoot;

    [Tooltip("Icon image — sprite and tint come from the PowerUpConfig.")]
    [SerializeField] private Image iconImage;

    [Tooltip("Text label showing the power-up display name (e.g. 'SHIELD').")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("Horizontal fill image that drains from full to empty over effectDuration.")]
    [SerializeField] private Image timerBar;

    [Tooltip("Full-screen overlay for the collection flash — Raycast Target should be off.")]
    [SerializeField] private Image screenFlashImage;

    // State
    private float effectDuration;
    private float timeRemaining;
    private bool isActive = false;
    private PowerUpConfig currentConfig;
    private Coroutine flashCoroutine;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (indicatorRoot == null)
            Debug.LogWarning("[PowerUpHUDIndicator] indicatorRoot is not assigned.");

        if (iconImage == null)
            Debug.LogWarning("[PowerUpHUDIndicator] iconImage is not assigned.");

        if (nameText == null)
            Debug.LogWarning("[PowerUpHUDIndicator] nameText is not assigned.");

        if (timerBar == null)
            Debug.LogWarning("[PowerUpHUDIndicator] timerBar is not assigned.");

        if (screenFlashImage == null)
            Debug.LogWarning("[PowerUpHUDIndicator] screenFlashImage not assigned — collection flash won't play.");

        // Start hidden — nothing is active yet
        if (indicatorRoot != null)
            indicatorRoot.SetActive(false);

        if (screenFlashImage != null)
            screenFlashImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isActive) return;

        // Unscaled so the bar always drains at real-world speed, even mid slow-mo
        timeRemaining -= Time.unscaledDeltaTime;

        if (timerBar != null)
            timerBar.fillAmount = Mathf.Clamp01(timeRemaining / effectDuration);

        if (timeRemaining <= 0f)
            HidePowerUp();
    }

    // -------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Activates the indicator for the given config. Safe to call mid-effect —
    /// it just resets everything to the new power-up.
    /// </summary>
    public void ShowPowerUp(PowerUpConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("[PowerUpHUDIndicator] ShowPowerUp called with a null config.");
            return;
        }

        currentConfig  = config;
        effectDuration = config.effectDuration;
        timeRemaining  = config.effectDuration;

        if (iconImage != null)
        {
            iconImage.sprite = config.icon;
            iconImage.color  = config.primaryColor;
        }

        if (nameText != null)
        {
            nameText.text  = config.displayName;
            nameText.color = config.primaryColor;
        }

        if (timerBar != null)
        {
            timerBar.fillAmount = 1f;
            timerBar.color      = config.primaryColor;
        }

        isActive = true;

        if (indicatorRoot != null)
            indicatorRoot.SetActive(true);

        // Flash the screen so collection never feels silent
        if (screenFlashImage != null)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(ScreenFlashRoutine(config.primaryColor));
        }
    }

    private void HidePowerUp()
    {
        isActive = false;

        if (indicatorRoot != null)
            indicatorRoot.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Screen flash

    private IEnumerator ScreenFlashRoutine(Color flashColor)
    {
        Color c = flashColor;
        c.a = 0.4f;

        screenFlashImage.color = c;
        screenFlashImage.gameObject.SetActive(true);

        float elapsed  = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            // Unscaled — slow-mo shouldn't stretch the flash out for 2 seconds
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0.4f, 0f, elapsed / duration);
            screenFlashImage.color = c;
            yield return null;
        }

        screenFlashImage.gameObject.SetActive(false);
    }
}
