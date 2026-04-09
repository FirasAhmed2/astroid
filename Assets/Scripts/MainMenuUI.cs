// MainMenuUI — the title / difficulty-select screen. The player picks a
// difficulty, then hits Play to start the game. Also has a Quit button.
// Hides itself when the game state leaves MainMenu and reappears when
// the player returns from Game Over.

using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    // Difficulty assets
    [Header("Difficulty Configs")]
    [Tooltip("The Easy difficulty ScriptableObject asset.")]
    [SerializeField] private DifficultyConfig easyConfig;

    [Tooltip("The Medium difficulty ScriptableObject asset.")]
    [SerializeField] private DifficultyConfig mediumConfig;

    [Tooltip("The Hard difficulty ScriptableObject asset.")]
    [SerializeField] private DifficultyConfig hardConfig;

    // Buttons
    [Header("Buttons")]
    [Tooltip("Button that selects Easy difficulty.")]
    [SerializeField] private Button easyButton;

    [Tooltip("Button that selects Medium difficulty.")]
    [SerializeField] private Button mediumButton;

    [Tooltip("Button that selects Hard difficulty.")]
    [SerializeField] private Button hardButton;

    [Tooltip("Button that starts the game with the selected difficulty.")]
    [SerializeField] private Button playButton;

    [Tooltip("Button that quits the application.")]
    [SerializeField] private Button quitButton;

    // References
    [Header("References")]
    [Tooltip("The root Canvas or panel to show/hide. If left empty, uses this GameObject.")]
    [SerializeField] private GameObject menuPanel;

    // State
    private DifficultyConfig selectedConfig;

    // Visual feedback — slight scale bump on the selected button
    private const float SelectedScale = 1.12f;
    private const float NormalScale = 1f;

    // -------------------------------------------------------------------------
    // Setup

    private void Awake()
    {
        if (menuPanel == null)
            menuPanel = gameObject;

        ValidateReferences();

        // Default to Medium so the player can just press Play immediately.
        // Fall back to Easy if Medium wasn't assigned in the Inspector.
        selectedConfig = mediumConfig != null ? mediumConfig : easyConfig;
    }

    private void OnEnable()
    {
        // Wire up button clicks
        if (easyButton != null) easyButton.onClick.AddListener(OnEasySelected);
        if (mediumButton != null) mediumButton.onClick.AddListener(OnMediumSelected);
        if (hardButton != null) hardButton.onClick.AddListener(OnHardSelected);
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        RefreshSelectionVisual();
    }

    private void Start()
    {
        // OnEnable() fires before GameManager.Awake() sets Instance on first load,
        // so we missed the subscription. Start() runs after all Awake() calls — catch up here.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        if (easyButton != null) easyButton.onClick.RemoveListener(OnEasySelected);
        if (mediumButton != null) mediumButton.onClick.RemoveListener(OnMediumSelected);
        if (hardButton != null) hardButton.onClick.RemoveListener(OnHardSelected);
        if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void ValidateReferences()
    {
        if (easyConfig == null) Debug.LogWarning("[MainMenuUI] Easy config is not assigned.");
        if (mediumConfig == null) Debug.LogWarning("[MainMenuUI] Medium config is not assigned.");
        if (hardConfig == null) Debug.LogWarning("[MainMenuUI] Hard config is not assigned.");

        if (easyButton == null) Debug.LogWarning("[MainMenuUI] Easy button is not assigned.");
        if (mediumButton == null) Debug.LogWarning("[MainMenuUI] Medium button is not assigned.");
        if (hardButton == null) Debug.LogWarning("[MainMenuUI] Hard button is not assigned.");
        if (playButton == null) Debug.LogWarning("[MainMenuUI] Play button is not assigned.");
        if (quitButton == null) Debug.LogWarning("[MainMenuUI] Quit button is not assigned.");
    }

    // -------------------------------------------------------------------------
    // Button handlers — public so they can be wired via onClick in the Inspector

    public void OnEasySelected()
    {
        AudioManager.Instance?.PlayButtonClick();
        selectedConfig = easyConfig;
        RefreshSelectionVisual();
    }

    public void OnMediumSelected()
    {
        AudioManager.Instance?.PlayButtonClick();
        selectedConfig = mediumConfig;
        RefreshSelectionVisual();
    }

    public void OnHardSelected()
    {
        AudioManager.Instance?.PlayButtonClick();
        selectedConfig = hardConfig;
        RefreshSelectionVisual();
    }

    public void OnPlayClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (selectedConfig == null)
        {
            Debug.LogWarning("[MainMenuUI] No difficulty selected — pick one first.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[MainMenuUI] No GameManager instance found in the scene.");
            return;
        }

        GameManager.Instance.SetPendingDifficulty(selectedConfig);
        GameManager.Instance.GoToCustomization();
    }

    public void OnQuitClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Debug.Log("[MainMenuUI] Quit requested.");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // -------------------------------------------------------------------------
    // Selection visual — scales the active button up slightly

    private void RefreshSelectionVisual()
    {
        SetButtonScale(easyButton, selectedConfig == easyConfig);
        SetButtonScale(mediumButton, selectedConfig == mediumConfig);
        SetButtonScale(hardButton, selectedConfig == hardConfig);
    }

    private void SetButtonScale(Button button, bool isSelected)
    {
        if (button == null) return;

        float scale = isSelected ? SelectedScale : NormalScale;
        button.transform.localScale = new Vector3(scale, scale, 1f);
    }

    // -------------------------------------------------------------------------
    // State listener

    private void HandleGameStateChanged(GameState newState)
    {
        bool shouldShow = newState == GameState.MainMenu;
        menuPanel.SetActive(shouldShow);
    }
}
