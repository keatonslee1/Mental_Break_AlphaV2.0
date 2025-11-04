using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using UnityEngine.InputSystem;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Manages the in-game pause menu that appears when pressing ESC.
/// Handles game state pausing, UI visibility, and menu actions.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The pause menu panel (main menu container)")]
    public GameObject pauseMenuPanel;

    [Tooltip("Hint text showing 'Press ESC to pause' at top-right")]
    public Component pauseHintText;

    [Header("Menu Buttons")]
    [Tooltip("Resume button (closes pause menu)")]
    public Button resumeButton;

    [Tooltip("Save Game button")]
    public Button saveGameButton;

    [Tooltip("Load Game button")]
    public Button loadGameButton;

    [Tooltip("Settings button (optional, for future use)")]
    public Button settingsButton;

    [Tooltip("Main Menu button")]
    public Button mainMenuButton;

    [Tooltip("Exit to Desktop button")]
    public Button exitButton;

    [Header("Settings")]
    [Tooltip("Scene name for the main menu")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Should the hint text be visible when not paused?")]
    public bool showHintWhenNotPaused = true;

    [Tooltip("Time scale when paused (typically 0)")]
    public float pausedTimeScale = 0f;

    [Header("References")]
    [Tooltip("SaveLoadManager instance (auto-found if null)")]
    public SaveLoadManager saveLoadManager;

    [Tooltip("DialogueRunner instance (auto-found if null)")]
    public DialogueRunner dialogueRunner;

    [Tooltip("SaveSlotSelectionUI instance (for load game selection)")]
    public SaveSlotSelectionUI saveSlotSelectionUI;

    private bool isPaused = false;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-find references if not assigned
        if (saveLoadManager == null)
        {
            saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
            if (saveLoadManager == null && SaveLoadManager.Instance != null)
            {
                saveLoadManager = SaveLoadManager.Instance;
            }
        }

        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }

        if (saveSlotSelectionUI == null)
        {
            saveSlotSelectionUI = FindFirstObjectByType<SaveSlotSelectionUI>();
        }
    }

    private void Start()
    {
        // Initialize UI state
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        UpdateHintVisibility();

        // Setup button listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResume);
        }

        if (saveGameButton != null)
        {
            saveGameButton.onClick.AddListener(OnSaveGame);
        }

        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(OnLoadGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettings);
            // Hide settings button for now (not implemented yet)
            settingsButton.gameObject.SetActive(false);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitToDesktop);
        }
    }

    private void Update()
    {
        // Check for ESC key to toggle pause (using new Input System)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Check if the game is currently paused
    /// </summary>
    public bool IsPaused
    {
        get { return isPaused; }
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        // If LoadMenuPanel is visible, close it and show pause menu buttons instead
        if (saveSlotSelectionUI != null && saveSlotSelectionUI.selectionPanel != null && saveSlotSelectionUI.selectionPanel.activeSelf)
        {
            saveSlotSelectionUI.HideSelectionUI();
            ShowPauseMenuButtons();
            return; // Don't change pause state, just close the sub-menu
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = pausedTimeScale;

        // Show pause menu and ensure buttons are visible
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            ShowPauseMenuButtons(); // Always ensure buttons are visible when showing pause menu
        }

        // Hide hint text when paused
        UpdateHintVisibility();

        // Pause dialogue if running
        if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
        {
            // Dialogue will naturally pause when time scale is 0
            // But we can add additional dialogue pause logic here if needed
        }

        Debug.Log("Game paused");
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;

        // Ensure LoadMenuPanel is hidden if it's open
        if (saveSlotSelectionUI != null)
        {
            saveSlotSelectionUI.HideSelectionUI();
        }

        isPaused = false;
        Time.timeScale = previousTimeScale;

        // Hide pause menu (but restore buttons first so they're ready next time)
        if (pauseMenuPanel != null)
        {
            ShowPauseMenuButtons(); // Restore buttons before hiding
            pauseMenuPanel.SetActive(false);
        }

        // Show hint text when not paused
        UpdateHintVisibility();

        Debug.Log("Game resumed");
    }

    /// <summary>
    /// Show pause menu panel (without changing pause state - used when returning from sub-menus)
    /// </summary>
    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            ShowPauseMenuButtons();
        }
    }

    /// <summary>
    /// Hide individual pause menu buttons (but keep panel active for child UI)
    /// </summary>
    private void HidePauseMenuButtons()
    {
        if (resumeButton != null) resumeButton.gameObject.SetActive(false);
        if (saveGameButton != null) saveGameButton.gameObject.SetActive(false);
        if (loadGameButton != null) loadGameButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Show individual pause menu buttons
    /// </summary>
    private void ShowPauseMenuButtons()
    {
        if (resumeButton != null) resumeButton.gameObject.SetActive(true);
        if (saveGameButton != null) saveGameButton.gameObject.SetActive(true);
        if (loadGameButton != null) loadGameButton.gameObject.SetActive(true);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(true);
        if (exitButton != null) exitButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Update hint text visibility based on pause state
    /// </summary>
    private void UpdateHintVisibility()
    {
        if (pauseHintText == null) return;

        bool shouldShow = showHintWhenNotPaused && !isPaused;

        GameObject hintGameObject = null;
#if USE_TMP
        if (pauseHintText is TMPro.TextMeshProUGUI tmpText)
        {
            hintGameObject = tmpText.gameObject;
        }
#endif
        if (pauseHintText is UnityEngine.UI.Text regularText)
        {
            hintGameObject = regularText.gameObject;
        }

        if (hintGameObject != null)
        {
            hintGameObject.SetActive(shouldShow);
        }
    }

    /// <summary>
    /// Resume button handler
    /// </summary>
    public void OnResume()
    {
        ResumeGame();
    }

    /// <summary>
    /// Save Game button handler - opens save slot selection UI
    /// </summary>
    public void OnSaveGame()
    {
        if (saveSlotSelectionUI != null)
        {
            // Show save slot selection UI first (this will activate LoadMenuPanel)
            saveSlotSelectionUI.ShowSelectionUIForSave();
            
            // Hide pause menu buttons but keep panel active (so LoadMenuPanel stays active)
            HidePauseMenuButtons();
        }
        else
        {
            // Fallback: Direct autosave if selection UI not available
            if (saveLoadManager == null)
            {
                Debug.LogWarning("PauseMenuManager: Cannot save - SaveLoadManager not found!");
                return;
            }

            bool saved = saveLoadManager.SaveGame(0); // Save to autosave slot
            if (saved)
            {
                Debug.Log("Game saved successfully!");
            }
            else
            {
                Debug.LogWarning("Save failed!");
            }
        }
    }

    /// <summary>
    /// Load Game button handler - opens save slot selection UI
    /// </summary>
    public void OnLoadGame()
    {
        if (saveSlotSelectionUI != null)
        {
            // Show save slot selection UI first (this will activate LoadMenuPanel)
            saveSlotSelectionUI.ShowSelectionUI();
            
            // Hide pause menu buttons but keep panel active (so LoadMenuPanel stays active)
            // We'll hide individual buttons instead of the whole panel
            HidePauseMenuButtons();
        }
        else
        {
            // Fallback: Direct quicksave load if selection UI not available
            if (saveLoadManager == null)
            {
                Debug.LogWarning("PauseMenuManager: Cannot load - SaveLoadManager not found!");
                return;
            }

            bool loaded = saveLoadManager.QuickLoad();
            if (loaded)
            {
                Debug.Log("Game loaded successfully!");
                ResumeGame();
            }
            else
            {
                Debug.LogWarning("No save found to load!");
            }
        }
    }

    /// <summary>
    /// Settings button handler (placeholder for future implementation)
    /// </summary>
    public void OnSettings()
    {
        Debug.Log("Settings menu not yet implemented");
        // TODO: Open settings menu
    }

    /// <summary>
    /// Main Menu button handler
    /// </summary>
    public void OnMainMenu()
    {
        // Resume time scale before loading scene
        Time.timeScale = 1f;

        // Load main menu scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("PauseMenuManager: Main menu scene name not set!");
        }
    }

    /// <summary>
    /// Exit to Desktop button handler
    /// </summary>
    public void OnExitToDesktop()
    {
        // Resume time scale before quitting
        Time.timeScale = 1f;

        // Quit application
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        // Ensure time scale is restored if manager is destroyed
        if (isPaused)
        {
            Time.timeScale = previousTimeScale;
        }
    }

    /// <summary>
    /// Called when application loses focus (optional: auto-pause)
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        // Optional: Auto-pause when application loses focus
        // Uncomment if desired:
        // if (pauseStatus && !isPaused)
        // {
        //     PauseGame();
        // }
    }
}

