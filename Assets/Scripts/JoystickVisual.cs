// JoystickVisual — sits on JoystickBackground and smoothly lerps the handle
// back to the center of the joystick whenever no touch is active. Keeps the
// visual in sync with the OnScreenStick without fighting its internal logic.

using UnityEngine;

public class JoystickVisual : MonoBehaviour
{
    // References
    [Header("References")]
    [Tooltip("The RectTransform of the inner handle circle.")]
    [SerializeField] private RectTransform handle;

    // State
    private RectTransform backgroundRect;
    private Vector2 defaultHandlePos;

    // How fast the handle lerps back to center — feels snappy but not instant
    private const float ReturnSpeed = 12f;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        backgroundRect = GetComponent<RectTransform>();

        if (handle == null)
        {
            Debug.LogWarning("[JoystickVisual] Handle RectTransform is not assigned — assign JoystickHandle in the Inspector.");
            return;
        }

        // Store the resting position so we always know where "center" is
        defaultHandlePos = handle.anchoredPosition;
    }

    private void Update()
    {
        if (handle == null) return;

        // Only pull the handle back when no finger is on the screen.
        // OnScreenStick owns handle position while a touch is active, so
        // we only lerp when it's safe to do so (no touches present).
        if (Input.touchCount == 0)
        {
            handle.anchoredPosition = Vector2.Lerp(
                handle.anchoredPosition,
                defaultHandlePos,
                ReturnSpeed * Time.deltaTime
            );
        }
    }
}
