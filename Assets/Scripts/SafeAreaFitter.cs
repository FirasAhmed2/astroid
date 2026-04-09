// SafeAreaFitter — adjusts this RectTransform to stay inside the device's safe area.
// Handles notches, home bars, and status bars on mobile. Attach it to any full-screen
// panel and its children will automatically avoid the OS-reserved edges.

using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    // References
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    // State — track last applied area so we only recalculate when it changes
    private Rect lastSafeArea = Rect.zero;
    private Vector2 lastScreenSize = Vector2.zero;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Walk up the hierarchy to find the owning Canvas
        parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            Debug.LogWarning("[SafeAreaFitter] No parent Canvas found — safe area won't be applied.");
            return;
        }

        ApplySafeArea();
    }

    private void Update()
    {
        // Screen resolution or safe area can change at runtime on mobile
        // (e.g. device rotation, split-screen). Only recalculate when needed.
        if (Screen.safeArea != lastSafeArea || ScreenSizeChanged())
        {
            ApplySafeArea();
        }
    }

    // -------------------------------------------------------------------------

    private void ApplySafeArea()
    {
        if (rectTransform == null || parentCanvas == null) return;

        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        if (screenSize.x <= 0f || screenSize.y <= 0f) return;

        // Convert safe area from screen pixels to 0–1 anchor space
        Vector2 anchorMin = safeArea.position / screenSize;
        Vector2 anchorMax = (safeArea.position + safeArea.size) / screenSize;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
    }

    private bool ScreenSizeChanged()
    {
        return (int)lastScreenSize.x != Screen.width || (int)lastScreenSize.y != Screen.height;
    }
}
