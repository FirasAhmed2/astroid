// Login — handles all three auth paths on the LoginScene:
//   1. Email + password sign-in (existing account)
//   2. Email + password account creation (new player)
//   3. Anonymous guest sign-in (no credentials needed)
// Buttons are disabled while any request is in flight to prevent double-taps.
// All failures log a warning and re-enable the buttons — game never crashes.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using TMPro;

public class Login : MonoBehaviour
{
    // References — Input fields
    [Header("Input Fields")]
    [Tooltip("Email address input.")]
    [SerializeField] private TMP_InputField emailInput;

    [Tooltip("Password input — masked automatically by ContentType.Password.")]
    [SerializeField] private TMP_InputField passwordInput;

    // References — Buttons
    [Header("Buttons")]
    [Tooltip("Signs in with the entered email and password.")]
    [SerializeField] private Button loginButton;

    [Tooltip("Creates a new account with the entered email and password.")]
    [SerializeField] private Button createAccountButton;

    [Tooltip("Signs in anonymously — no credentials needed.")]
    [SerializeField] private Button guestButton;

    [Tooltip("Closes the application.")]
    [SerializeField] private Button quitButton;

    // Config
    [Header("Config")]
    [Tooltip("Scene to load after a successful login.")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Tooltip("How long to wait for Firebase operations before giving up (seconds).")]
    [SerializeField] private float authTimeout = 10f;

    // State
    private bool isBusy = false;

    // -------------------------------------------------------------------------

    private void Start()
    {
        ValidateReferences();

        if (loginButton         != null) loginButton.onClick.AddListener(OnLoginPressed);
        if (createAccountButton != null) createAccountButton.onClick.AddListener(OnCreateAccountPressed);
        if (guestButton         != null) guestButton.onClick.AddListener(OnGuestPressed);
        if (quitButton          != null) quitButton.onClick.AddListener(OnQuitPressed);
    }

    private void OnDisable()
    {
        if (loginButton         != null) loginButton.onClick.RemoveListener(OnLoginPressed);
        if (createAccountButton != null) createAccountButton.onClick.RemoveListener(OnCreateAccountPressed);
        if (guestButton         != null) guestButton.onClick.RemoveListener(OnGuestPressed);
        if (quitButton          != null) quitButton.onClick.RemoveListener(OnQuitPressed);
    }

    // -------------------------------------------------------------------------
    // Button handlers

    private void OnLoginPressed()
    {
        if (isBusy) return;

        if (!ValidateEmailPassword()) return;

        StartCoroutine(SignInWithEmail(emailInput.text.Trim(), passwordInput.text));
    }

    private void OnCreateAccountPressed()
    {
        if (isBusy) return;

        if (!ValidateEmailPassword()) return;

        StartCoroutine(CreateAccount(emailInput.text.Trim(), passwordInput.text));
    }

    private void OnGuestPressed()
    {
        if (isBusy) return;

        StartCoroutine(SignInAnonymously());
    }

    private void OnQuitPressed()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // -------------------------------------------------------------------------
    // Auth flows

    private IEnumerator SignInWithEmail(string email, string password)
    {
        SetAllButtonsInteractable(false);
        isBusy = true;

        yield return StartCoroutine(WaitForFirebase());
        if (!isBusy) yield break; // WaitForFirebase already re-enabled buttons on fail

        var auth      = FirebaseAuth.DefaultInstance;
        var signInTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return WaitForTask(signInTask);

        if (signInTask.Exception != null)
        {
            // Unwrap to FirebaseException so we get the specific AuthError code,
            // not just the generic "An internal error has occurred" message
            var firebaseEx = signInTask.Exception.GetBaseException() as Firebase.FirebaseException;
            if (firebaseEx != null)
            {
                var errorCode = (Firebase.Auth.AuthError)firebaseEx.ErrorCode;
                Debug.LogWarning($"[Login] Sign-in failed — AuthError.{errorCode}: {firebaseEx.Message}");
            }
            else
            {
                Debug.LogWarning($"[Login] Sign-in failed: {signInTask.Exception.GetBaseException().Message}");
            }

            SetAllButtonsInteractable(true);
            isBusy = false;
            yield break;
        }

        Debug.Log($"[Login] Signed in as {auth.CurrentUser.Email}");
        SceneManager.LoadScene(mainMenuScene);
    }

    private IEnumerator CreateAccount(string email, string password)
    {
        SetAllButtonsInteractable(false);
        isBusy = true;

        yield return StartCoroutine(WaitForFirebase());
        if (!isBusy) yield break;

        var auth       = FirebaseAuth.DefaultInstance;
        var createTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

        yield return WaitForTask(createTask);

        if (createTask.Exception != null)
        {
            Debug.LogWarning($"[Login] Account creation failed: {createTask.Exception.GetBaseException().Message}");
            SetAllButtonsInteractable(true);
            isBusy = false;
            yield break;
        }

        Debug.Log($"[Login] Account created for {auth.CurrentUser.Email} — loading game.");
        SceneManager.LoadScene(mainMenuScene);
    }

    private IEnumerator SignInAnonymously()
    {
        SetAllButtonsInteractable(false);
        isBusy = true;

        yield return StartCoroutine(WaitForFirebase());
        if (!isBusy) yield break;

        var auth       = FirebaseAuth.DefaultInstance;
        var signInTask = auth.SignInAnonymouslyAsync();

        yield return WaitForTask(signInTask);

        if (signInTask.Exception != null)
        {
            Debug.LogWarning($"[Login] Anonymous sign-in failed: {signInTask.Exception.GetBaseException().Message}");
            SetAllButtonsInteractable(true);
            isBusy = false;
            yield break;
        }

        Debug.Log($"[Login] Signed in anonymously. UID: {auth.CurrentUser.UserId}");
        SceneManager.LoadScene(mainMenuScene);
    }

    // -------------------------------------------------------------------------
    // Shared helpers

    // Makes sure Firebase dependencies are resolved before any auth call
    private IEnumerator WaitForFirebase()
    {
        var depTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return WaitForTask(depTask);

        if (depTask.Exception != null || depTask.Result != DependencyStatus.Available)
        {
            Debug.LogWarning($"[Login] Firebase not available: {depTask.Result}");
            SetAllButtonsInteractable(true);
            isBusy = false;
        }
    }

    // Polls a task frame-by-frame up to authTimeout seconds
    private IEnumerator WaitForTask(System.Threading.Tasks.Task task)
    {
        float elapsed = 0f;

        while (!task.IsCompleted)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= authTimeout)
            {
                Debug.LogWarning("[Login] Operation timed out.");
                SetAllButtonsInteractable(true);
                isBusy = false;
                yield break;
            }
            yield return null;
        }
    }

    // Basic client-side check before even hitting Firebase
    private bool ValidateEmailPassword()
    {
        if (emailInput == null || string.IsNullOrWhiteSpace(emailInput.text))
        {
            Debug.LogWarning("[Login] Email field is empty.");
            return false;
        }

        if (passwordInput == null || passwordInput.text.Length < 6)
        {
            Debug.LogWarning("[Login] Password must be at least 6 characters.");
            return false;
        }

        return true;
    }

    private void SetAllButtonsInteractable(bool state)
    {
        if (loginButton         != null) loginButton.interactable         = state;
        if (createAccountButton != null) createAccountButton.interactable = state;
        if (guestButton         != null) guestButton.interactable         = state;
        if (quitButton          != null) quitButton.interactable          = state;
    }

    private void ValidateReferences()
    {
        if (emailInput          == null) Debug.LogWarning("[Login] emailInput not assigned.");
        if (passwordInput       == null) Debug.LogWarning("[Login] passwordInput not assigned.");
        if (loginButton         == null) Debug.LogWarning("[Login] loginButton not assigned.");
        if (createAccountButton == null) Debug.LogWarning("[Login] createAccountButton not assigned.");
        if (guestButton         == null) Debug.LogWarning("[Login] guestButton not assigned.");
        if (quitButton          == null) Debug.LogWarning("[Login] quitButton not assigned.");
    }
}
