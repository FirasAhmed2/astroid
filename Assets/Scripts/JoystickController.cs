// JoystickController — sits on JoystickContainer and shows or hides the joystick
// visuals based on game state. Polls GameManager each frame instead of relying
// on a one-shot event subscription, which breaks when GameManager arrives via
// DontDestroyOnLoad after this script's OnEnable has already fired.
//
// On desktop builds (non-editor) the joystick is always hidden — keyboard/mouse only.
// In the editor it always shows so you can see and tweak it without a phone.

using UnityEngine;

public class JoystickController : MonoBehaviour
{
    // References
    [Header("References")]
    [Tooltip("The visual root of the joystick — JoystickBackground. Gets toggled active/inactive.")]
    [SerializeField] private GameObject joystickRoot;

    // State
    private bool isMobilePlatform;

    // Start at -1 so the first Update always runs a sync regardless of initial state
    private GameState lastKnownState = (GameState)(-1);

    // -------------------------------------------------------------------------

    private void Start()
    {
        isMobilePlatform = Application.isMobilePlatform;

        // Always treat the editor as mobile so layout is visible without deploying
#if UNITY_EDITOR
        isMobilePlatform = true;
#endif

        // Non-mobile, non-editor: hide immediately and don't update further
        if (!isMobilePlatform)
        {
            SetJoystickVisible(false);
        }
    }

    private void Update()
    {
        if (!isMobilePlatform) return;

        // In editor with no GameManager present (e.g. SampleScene played directly),
        // keep the joystick visible so it's easy to test without going through menus
#if UNITY_EDITOR
        if (GameManager.Instance == null)
        {
            SetJoystickVisible(true);
            return;
        }
#endif

        if (GameManager.Instance == null) return;

        GameState currentState = GameManager.Instance.CurrentState;

        // Only call SetActive when the state actually changes — avoids thrashing every frame
        if (currentState != lastKnownState)
        {
            lastKnownState = currentState;
            SetJoystickVisible(currentState == GameState.Playing);
        }
    }

    // -------------------------------------------------------------------------

    private void SetJoystickVisible(bool visible)
    {
        if (joystickRoot == null)
        {
            Debug.LogWarning("[JoystickController] joystickRoot is not assigned.");
            return;
        }

        joystickRoot.SetActive(visible);
    }
}
