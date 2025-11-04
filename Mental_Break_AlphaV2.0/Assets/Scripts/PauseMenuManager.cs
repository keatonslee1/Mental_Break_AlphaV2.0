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

    [Tooltip("Warning text above pause menu buttons (optional - will be created if null)")]
    public Component pauseWarningText;

    [Tooltip("Run/Day tracker text on middle-left (optional - will be created if null)")]
    public Component runDayTrackerText;

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

    [Tooltip("Skip Day button (safety feature to jump to end of day)")]
    public Button skipDayButton;

    [Tooltip("Restart Day button (safety feature to jump to start of day, or previous day if already at start)")]
    public Button restartDayButton;

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

    private Image pauseMenuPanelImage;
    private bool pauseMenuPanelImageInitialEnabled = true;
    private bool pauseMenuPanelImageInitialRaycastTarget = true;

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

            pauseMenuPanelImage = pauseMenuPanel.GetComponent<Image>();
            if (pauseMenuPanelImage != null)
            {
                pauseMenuPanelImageInitialEnabled = pauseMenuPanelImage.enabled;
                pauseMenuPanelImageInitialRaycastTarget = pauseMenuPanelImage.raycastTarget;
                Debug.Log($"PauseMenuManager: Cached PauseMenuPanel Image (enabled={pauseMenuPanelImage.enabled}, raycastTarget={pauseMenuPanelImage.raycastTarget})");
            }
            else
            {
                Debug.LogWarning("PauseMenuManager: pauseMenuPanel does not have an Image component; background toggling will be skipped.");
            }
        }
        else
        {
            Debug.LogError("PauseMenuManager: pauseMenuPanel is not assigned. Pause menu UI will not display correctly.");
        }

        UpdateHintVisibility();

        // Setup warning text if needed
        SetupWarningText();

        // Setup run/day tracker if needed
        SetupRunDayTracker();

        // Setup button listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResume);
        }
        else
        {
            Debug.LogError("PauseMenuManager: resumeButton is not assigned.");
        }

        if (saveGameButton != null)
        {
            saveGameButton.onClick.AddListener(OnSaveGame);
        }
        else
        {
            Debug.LogError("PauseMenuManager: saveGameButton is not assigned.");
        }

        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(OnLoadGame);
        }
        else
        {
            Debug.LogError("PauseMenuManager: loadGameButton is not assigned.");
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
            // Hide main menu button for MVP (too buggy right now, but keep backend code)
            mainMenuButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("PauseMenuManager: mainMenuButton is not assigned.");
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitToDesktop);
        }
        else
        {
            Debug.LogError("PauseMenuManager: exitButton is not assigned.");
        }

        if (skipDayButton != null)
        {
            skipDayButton.onClick.AddListener(OnSkipDay);
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: skipDayButton is not assigned. Skip Day feature will not be available.");
        }

        if (restartDayButton != null)
        {
            restartDayButton.onClick.AddListener(OnRestartDay);
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: restartDayButton is not assigned. Restart Day feature will not be available.");
        }
    }

    private void Update()
    {
        // Check for ESC key to toggle pause (using new Input System)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }

        // Update run/day tracker
        UpdateRunDayTracker();
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
            ShowWarningText(); // Show warning text
            VerifyPauseMenuButtons("PauseGame");
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: PauseGame called but pauseMenuPanel is null.");
        }

        LogPauseMenuState("PauseGame - after ShowPauseMenuButtons");

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
            HideWarningText(); // Hide warning text
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
            ShowWarningText();
        }
    }

    /// <summary>
    /// Show warning text above pause menu
    /// </summary>
    private void ShowWarningText()
    {
        if (pauseWarningText == null) return;

        GameObject warningObj = null;
#if USE_TMP
        if (pauseWarningText is TextMeshProUGUI tmpText)
        {
            warningObj = tmpText.gameObject;
        }
#endif
        if (pauseWarningText is Text regularText)
        {
            warningObj = regularText.gameObject;
        }

        if (warningObj != null)
        {
            warningObj.SetActive(true);
        }
    }

    /// <summary>
    /// Hide warning text
    /// </summary>
    private void HideWarningText()
    {
        if (pauseWarningText == null) return;

        GameObject warningObj = null;
#if USE_TMP
        if (pauseWarningText is TextMeshProUGUI tmpText)
        {
            warningObj = tmpText.gameObject;
        }
#endif
        if (pauseWarningText is Text regularText)
        {
            warningObj = regularText.gameObject;
        }

        if (warningObj != null)
        {
            warningObj.SetActive(false);
        }
    }

    /// <summary>
    /// Hide individual pause menu buttons (but keep panel active for child UI)
    /// IMPORTANT: This does NOT affect LoadMenuPanel - only the main pause menu buttons
    /// </summary>
    private void HidePauseMenuButtons()
    {
        Debug.Log("PauseMenuManager: HidePauseMenuButtons() - before state: " + DescribeButtonStates());
        if (resumeButton != null) resumeButton.gameObject.SetActive(false);
        if (saveGameButton != null) saveGameButton.gameObject.SetActive(false);
        if (loadGameButton != null) loadGameButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(false);
        if (skipDayButton != null) skipDayButton.gameObject.SetActive(false);
        if (restartDayButton != null) restartDayButton.gameObject.SetActive(false);
        HideWarningText(); // Hide warning text when hiding buttons
        Debug.Log("PauseMenuManager: HidePauseMenuButtons() - after state: " + DescribeButtonStates());
        // NOTE: LoadMenuPanel is NOT affected by this method - it's a separate child of PauseMenuPanel
    }

    /// <summary>
    /// Show individual pause menu buttons
    /// </summary>
    private void ShowPauseMenuButtons()
    {
        Debug.Log("PauseMenuManager: ShowPauseMenuButtons() - before state: " + DescribeButtonStates());
        SetPauseMenuBackgroundVisible(true, "ShowPauseMenuButtons");
        if (resumeButton != null) resumeButton.gameObject.SetActive(true);
        if (saveGameButton != null) saveGameButton.gameObject.SetActive(true);
        if (loadGameButton != null) loadGameButton.gameObject.SetActive(true);
        // Main menu button hidden for MVP (too buggy, but backend code kept)
        // if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(true);
        if (exitButton != null) exitButton.gameObject.SetActive(true);
        if (skipDayButton != null) skipDayButton.gameObject.SetActive(true);
        if (restartDayButton != null) restartDayButton.gameObject.SetActive(true);
        ShowWarningText(); // Show warning text when showing buttons
        Debug.Log("PauseMenuManager: ShowPauseMenuButtons() - after state: " + DescribeButtonStates());
    }

    /// <summary>
    /// Log and verify button states for diagnostics
    /// </summary>
    private void VerifyPauseMenuButtons(string context)
    {
        string states = DescribeButtonStates();
        Debug.Log($"PauseMenuManager[{context}]: Button states -> {states}");

        if (!AreButtonsActive())
        {
            Debug.LogWarning($"PauseMenuManager[{context}]: One or more pause menu buttons are inactive. See states above for details.");
        }
    }

    private bool AreButtonsActive()
    {
        bool resumeActive = resumeButton != null && resumeButton.gameObject.activeInHierarchy;
        bool saveActive = saveGameButton != null && saveGameButton.gameObject.activeInHierarchy;
        bool loadActive = loadGameButton != null && loadGameButton.gameObject.activeInHierarchy;
        // Main menu button hidden for MVP, so don't check it
        // bool mainActive = mainMenuButton != null && mainMenuButton.gameObject.activeInHierarchy;
        bool exitActive = exitButton != null && exitButton.gameObject.activeInHierarchy;

        return resumeActive && saveActive && loadActive && exitActive; // Removed mainActive check
    }

    private string DescribeButtonStates()
    {
        return $"Resume={DescribeButton(resumeButton)} | Save={DescribeButton(saveGameButton)} | Load={DescribeButton(loadGameButton)} | Main={DescribeButton(mainMenuButton)} | Exit={DescribeButton(exitButton)}";
    }

    private string DescribeButton(Button button)
    {
        if (button == null)
        {
            return "null";
        }

        GameObject go = button.gameObject;
        return $"activeSelf={go.activeSelf}, activeInHierarchy={go.activeInHierarchy}";
    }

    private void SetPauseMenuBackgroundVisible(bool visible, string context)
    {
        if (pauseMenuPanel == null)
        {
            Debug.LogWarning($"PauseMenuManager[{context}]: Cannot change background visibility because pauseMenuPanel is null.");
            return;
        }

        if (pauseMenuPanelImage == null)
        {
            pauseMenuPanelImage = pauseMenuPanel.GetComponent<Image>();
            if (pauseMenuPanelImage == null)
            {
                Debug.LogWarning($"PauseMenuManager[{context}]: PauseMenuPanel Image component not found.");
                return;
            }
        }

        bool targetEnabled = visible ? pauseMenuPanelImageInitialEnabled : false;
        bool targetRaycastTarget = visible ? pauseMenuPanelImageInitialRaycastTarget : false;

        if (pauseMenuPanelImage.enabled != targetEnabled)
        {
            pauseMenuPanelImage.enabled = targetEnabled;
            Debug.Log($"PauseMenuManager[{context}]: Set PauseMenuPanel Image.enabled={targetEnabled}");
        }

        if (pauseMenuPanelImage.raycastTarget != targetRaycastTarget)
        {
            pauseMenuPanelImage.raycastTarget = targetRaycastTarget;
            Debug.Log($"PauseMenuManager[{context}]: Set PauseMenuPanel Image.raycastTarget={targetRaycastTarget}");
        }
    }

    private void LogPauseMenuState(string context)
    {
        if (pauseMenuPanel == null)
        {
            Debug.LogWarning($"PauseMenuManager[{context}]: pauseMenuPanel is null.");
            return;
        }

        Transform parent = pauseMenuPanel.transform.parent;
        RectTransform rectTransform = pauseMenuPanel.GetComponent<RectTransform>();
        Image image = pauseMenuPanel.GetComponent<Image>();
        Canvas canvas = pauseMenuPanel.GetComponentInParent<Canvas>();

        Debug.Log($"PauseMenuManager[{context}]: PauseMenuPanel activeSelf={pauseMenuPanel.activeSelf}, activeInHierarchy={pauseMenuPanel.activeInHierarchy}, siblingIndex={pauseMenuPanel.transform.GetSiblingIndex()}, childCount={pauseMenuPanel.transform.childCount}, parent={(parent != null ? parent.name : "<none>")}");

        if (rectTransform != null)
        {
            Debug.Log($"PauseMenuManager[{context}]: PauseMenuPanel Rect -> anchorMin={rectTransform.anchorMin}, anchorMax={rectTransform.anchorMax}, sizeDelta={rectTransform.sizeDelta}, anchoredPosition={rectTransform.anchoredPosition}");
        }

        if (image != null)
        {
            Debug.Log($"PauseMenuManager[{context}]: PauseMenuPanel Image -> enabled={image.enabled}, color={image.color}, raycastTarget={image.raycastTarget}");
        }

        if (canvas != null)
        {
            Debug.Log($"PauseMenuManager[{context}]: Parent Canvas -> name={canvas.name}, renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}, sortingLayerID={canvas.sortingLayerID}");
        }
    }

    private void LogLoadMenuPanelState(string context)
    {
        if (saveSlotSelectionUI == null)
        {
            Debug.LogWarning($"PauseMenuManager[{context}]: saveSlotSelectionUI is null, cannot log LoadMenuPanel state.");
            return;
        }

        if (saveSlotSelectionUI.selectionPanel == null)
        {
            Debug.LogWarning($"PauseMenuManager[{context}]: SaveSlotSelectionUI.selectionPanel is null.");
            return;
        }

        GameObject loadPanel = saveSlotSelectionUI.selectionPanel;
        Transform parent = loadPanel.transform.parent;
        RectTransform rectTransform = loadPanel.GetComponent<RectTransform>();
        Image image = loadPanel.GetComponent<Image>();
        Canvas canvas = loadPanel.GetComponentInParent<Canvas>();

        Debug.Log($"PauseMenuManager[{context}]: LoadMenuPanel activeSelf={loadPanel.activeSelf}, activeInHierarchy={loadPanel.activeInHierarchy}, siblingIndex={loadPanel.transform.GetSiblingIndex()}, childCount={loadPanel.transform.childCount}, parent={(parent != null ? parent.name : "<none>")}");

        if (rectTransform != null)
        {
            Debug.Log($"PauseMenuManager[{context}]: LoadMenuPanel Rect -> anchorMin={rectTransform.anchorMin}, anchorMax={rectTransform.anchorMax}, sizeDelta={rectTransform.sizeDelta}, anchoredPosition={rectTransform.anchoredPosition}");
        }

        if (image != null)
        {
            Debug.Log($"PauseMenuManager[{context}]: LoadMenuPanel Image -> enabled={image.enabled}, color={image.color}, raycastTarget={image.raycastTarget}");
        }

        if (canvas != null)
        {
            Debug.Log($"PauseMenuManager[{context}]: Parent Canvas -> name={canvas.name}, renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}, sortingLayerID={canvas.sortingLayerID}");
        }
    }

    /// <summary>
    /// Build the formatted keybind tips string
    /// </summary>
    private string GetKeybindTipsText()
    {
        return "esc = pause\n" +
               "space/enter = forward";
    }

    /// <summary>
    /// Setup warning text above pause menu buttons
    /// </summary>
    private void SetupWarningText()
    {
        if (pauseMenuPanel == null) return;

        // If warning text is not assigned, create it programmatically
        if (pauseWarningText == null)
        {
            GameObject warningObj = new GameObject("PauseWarningText");
            warningObj.transform.SetParent(pauseMenuPanel.transform, false);

            RectTransform rect = warningObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -30f); // Position above buttons
            rect.sizeDelta = new Vector2(600f, 40f);

#if USE_TMP
            TextMeshProUGUI tmpText = warningObj.AddComponent<TextMeshProUGUI>();
            if (TMPro.TMP_Settings.instance != null && TMPro.TMP_Settings.instance.defaultFontAsset != null)
            {
                tmpText.font = TMPro.TMP_Settings.instance.defaultFontAsset;
            }
            tmpText.text = "Save/load buttons don't work first time, idk lol";
            tmpText.fontSize = 20;
            tmpText.fontStyle = TMPro.FontStyles.Bold;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = new Color(1f, 0.8f, 0f, 1f); // Bright orange/yellow
            pauseWarningText = tmpText;
#else
            Text text = warningObj.AddComponent<Text>();
            text.text = "Save/load buttons don't work first time, idk lol";
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperCenter;
            text.color = new Color(1f, 0.8f, 0f, 1f); // Bright orange/yellow
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pauseWarningText = text;
#endif
        }
        else
        {
            // Update existing warning text
            UpdateWarningText();
        }

        // Initially hide the warning (will show when pause menu opens)
        GameObject warningGameObj = null;
#if USE_TMP
        if (pauseWarningText is TextMeshProUGUI tmpText)
        {
            warningGameObj = tmpText.gameObject;
        }
#endif
        if (pauseWarningText is Text regularText)
        {
            warningGameObj = regularText.gameObject;
        }
        if (warningGameObj != null)
        {
            warningGameObj.SetActive(false);
        }
    }

    /// <summary>
    /// Update warning text content and style
    /// </summary>
    private void UpdateWarningText()
    {
        if (pauseWarningText == null) return;

        string warningMessage = "Save/load buttons don't work first time, idk lol";
        Color warningColor = new Color(1f, 0.8f, 0f, 1f); // Bright orange/yellow

#if USE_TMP
        if (pauseWarningText is TextMeshProUGUI tmpText)
        {
            tmpText.text = warningMessage;
            tmpText.color = warningColor;
            tmpText.fontStyle = TMPro.FontStyles.Bold;
            tmpText.fontSize = 20;
        }
#endif
        if (pauseWarningText is Text regularText)
        {
            regularText.text = warningMessage;
            regularText.color = warningColor;
            regularText.fontStyle = FontStyle.Bold;
            regularText.fontSize = 20;
        }
    }

    /// <summary>
    /// Setup run/day tracker text on middle-left
    /// </summary>
    private void SetupRunDayTracker()
    {
        // Find or create Canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // If tracker text is not assigned, create it programmatically
        if (runDayTrackerText == null)
        {
            GameObject trackerObj = new GameObject("RunDayTracker");
            trackerObj.transform.SetParent(canvas.transform, false);

            RectTransform rect = trackerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(20f, -40f); // 20 pixels from left edge, 40 pixels from top (same height as hint text, moved down)
            rect.sizeDelta = new Vector2(300f, 80f); // Increased size to accommodate larger text

#if USE_TMP
            TextMeshProUGUI tmpText = trackerObj.AddComponent<TextMeshProUGUI>();
            if (TMPro.TMP_Settings.instance != null && TMPro.TMP_Settings.instance.defaultFontAsset != null)
            {
                tmpText.font = TMPro.TMP_Settings.instance.defaultFontAsset;
            }
            tmpText.text = "Run 1, Day 1";
            tmpText.fontSize = 48; // Increased size to match hint text
            tmpText.alignment = TextAlignmentOptions.MidlineLeft;
            tmpText.color = Color.white;
            runDayTrackerText = tmpText;
#else
            Text text = trackerObj.AddComponent<Text>();
            text.text = "Run 1, Day 1";
            text.fontSize = 48; // Increased size to match hint text
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            runDayTrackerText = text;
#endif
        }
    }

    /// <summary>
    /// Update run/day tracker text with current values
    /// </summary>
    private void UpdateRunDayTracker()
    {
        if (runDayTrackerText == null || dialogueRunner == null || dialogueRunner.VariableStorage == null) return;

        // Get current run and day from Yarn variables
        float currentRun = 1f;
        float currentDay = 1f;
        dialogueRunner.VariableStorage.TryGetValue<float>("$current_run", out currentRun);
        dialogueRunner.VariableStorage.TryGetValue<float>("$current_day", out currentDay);

        string trackerText = $"Run {Mathf.RoundToInt(currentRun)}, Day {Mathf.RoundToInt(currentDay)}";

#if USE_TMP
        if (runDayTrackerText is TextMeshProUGUI tmpText)
        {
            tmpText.text = trackerText;
        }
#endif
        if (runDayTrackerText is Text regularText)
        {
            regularText.text = trackerText;
        }
    }

    /// <summary>
    /// Update hint text visibility and content based on pause state
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
            // Set text content
            tmpText.text = GetKeybindTipsText();
            // Set alignment to right-center for middle positioning
            tmpText.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            // Make text larger for readability
            tmpText.fontSize = 48;
        }
#endif
        if (pauseHintText is UnityEngine.UI.Text regularText)
        {
            hintGameObject = regularText.gameObject;
            // Set text content
            regularText.text = GetKeybindTipsText();
            // Set alignment to right-center for middle positioning
            regularText.alignment = TextAnchor.MiddleRight;
            // Make text larger for readability
            regularText.fontSize = 48;
        }

        if (hintGameObject != null)
        {
            // Position hint text at top-right, moved up from center
            RectTransform rectTransform = hintGameObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Anchor to top-right (1, 1) and pivot at right-center (1, 0.5)
                rectTransform.anchorMin = new Vector2(1f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(1f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(-20f, -200f); // 200 pixels from top (moved down)
                // Increase size to accommodate larger text and multiple lines
                rectTransform.sizeDelta = new Vector2(500f, 120f); // Taller to prevent line cutoff
            }
            
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
        LogPauseMenuState("OnSaveGame - before");
        LogLoadMenuPanelState("OnSaveGame - before");
        VerifyPauseMenuButtons("OnSaveGame - before");

        if (saveSlotSelectionUI != null)
        {
            // CRITICAL: Hide pause menu buttons FIRST, then show selection UI
            // This ensures LoadMenuPanel can be activated without interference
            HidePauseMenuButtons();
            SetPauseMenuBackgroundVisible(false, "OnSaveGame - before ShowSelectionUI");
            
            // DIAGNOSTIC: Log LoadMenuPanel state BEFORE ShowSelectionUI
            LogLoadMenuPanelState("OnSaveGame - before ShowSelectionUIForSave");
            
            // Show save slot selection UI (this will activate LoadMenuPanel)
            saveSlotSelectionUI.ShowSelectionUIForSave();
            
            // DIAGNOSTIC: Log LoadMenuPanel state AFTER ShowSelectionUI
            LogLoadMenuPanelState("OnSaveGame - after ShowSelectionUIForSave");
            
            // Verify final state
            VerifyPauseMenuButtons("OnSaveGame - after ShowSelectionUI");
            LogPauseMenuState("OnSaveGame - after ShowSelectionUI");
            LogLoadMenuPanelState("OnSaveGame - final");
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
        LogPauseMenuState("OnLoadGame - before");
        LogLoadMenuPanelState("OnLoadGame - before");
        VerifyPauseMenuButtons("OnLoadGame - before");

        if (saveSlotSelectionUI != null)
        {
            // CRITICAL: Hide pause menu buttons FIRST, then show selection UI
            // This ensures LoadMenuPanel can be activated without interference
            HidePauseMenuButtons();
            SetPauseMenuBackgroundVisible(false, "OnLoadGame - before ShowSelectionUI");
            
            // DIAGNOSTIC: Log LoadMenuPanel state BEFORE ShowSelectionUI
            LogLoadMenuPanelState("OnLoadGame - before ShowSelectionUIForLoad");
            
            // Show save slot selection UI in load mode (this will activate LoadMenuPanel)
            saveSlotSelectionUI.ShowSelectionUIForLoad();
            
            // DIAGNOSTIC: Log LoadMenuPanel state AFTER ShowSelectionUI
            LogLoadMenuPanelState("OnLoadGame - after ShowSelectionUIForLoad");
            
            // Verify final state
            VerifyPauseMenuButtons("OnLoadGame - after ShowSelectionUI");
            LogPauseMenuState("OnLoadGame - after ShowSelectionUI");
            LogLoadMenuPanelState("OnLoadGame - final");
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

    /// <summary>
    /// Skip Day button handler - jumps to end of current day node
    /// Safety feature to help users get unstuck from story glitches or loops
    /// </summary>
    public void OnSkipDay()
    {
        if (dialogueRunner == null || dialogueRunner.VariableStorage == null)
        {
            Debug.LogError("PauseMenuManager: Cannot skip day - DialogueRunner or VariableStorage is null.");
            return;
        }

        // Get current run and day from Yarn variables
        float currentRun = 1f;
        float currentDay = 1f;
        
        bool hasRun = dialogueRunner.VariableStorage.TryGetValue<float>("$current_run", out currentRun);
        bool hasDay = dialogueRunner.VariableStorage.TryGetValue<float>("$current_day", out currentDay);

        // Default to 1 if values are not found (safety fallback)
        if (!hasRun)
        {
            Debug.LogWarning("PauseMenuManager: $current_run not found, defaulting to 1.");
            currentRun = 1f;
        }
        if (!hasDay)
        {
            Debug.LogWarning("PauseMenuManager: $current_day not found, defaulting to 1.");
            currentDay = 1f;
        }

        // Construct the end-of-day node name: R{run}_D{day}_EndOfDay
        int runInt = Mathf.RoundToInt(currentRun);
        int dayInt = Mathf.RoundToInt(currentDay);
        string nodeName = $"R{runInt}_D{dayInt}_EndOfDay";

        Debug.Log($"PauseMenuManager: Skipping to end of day - Jumping to node: {nodeName}");

        // Stop current dialogue if it's running (especially if waiting for option selection)
        // This prevents DialogueException when trying to start new dialogue while waiting for options
        if (dialogueRunner.IsDialogueRunning)
        {
            Debug.Log("PauseMenuManager: Stopping current dialogue before skipping to end of day");
            try
            {
                if (dialogueRunner.Dialogue != null)
                {
                    dialogueRunner.Dialogue.Stop();
                    Debug.Log("PauseMenuManager: Current dialogue stopped successfully");
                }
            }
            catch (System.Exception stopEx)
            {
                Debug.LogWarning($"PauseMenuManager: Error stopping dialogue (continuing anyway): {stopEx.Message}");
            }
        }

        // Jump to the end-of-day node
        // This works at any time, even if dialogue isn't running or tasks aren't completed
        try
        {
            dialogueRunner.StartDialogue(nodeName);
            Debug.Log($"PauseMenuManager: Successfully jumped to {nodeName}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PauseMenuManager: Failed to jump to node {nodeName}: {ex.Message}");
            return;
        }

        // Close the pause menu
        ResumeGame();
    }

    /// <summary>
    /// Restart Day button handler - jumps to start of current day node, or previous day if already at start
    /// Safety feature to help users restart the day or go back to previous day
    /// </summary>
    public void OnRestartDay()
    {
        if (dialogueRunner == null || dialogueRunner.VariableStorage == null)
        {
            Debug.LogError("PauseMenuManager: Cannot restart day - DialogueRunner or VariableStorage is null.");
            return;
        }

        if (dialogueRunner.Dialogue == null)
        {
            Debug.LogError("PauseMenuManager: Cannot restart day - Dialogue is null.");
            return;
        }

        // Get current run and day from Yarn variables
        float currentRun = 1f;
        float currentDay = 1f;
        
        bool hasRun = dialogueRunner.VariableStorage.TryGetValue<float>("$current_run", out currentRun);
        bool hasDay = dialogueRunner.VariableStorage.TryGetValue<float>("$current_day", out currentDay);

        // Default to 1 if values are not found (safety fallback)
        if (!hasRun)
        {
            Debug.LogWarning("PauseMenuManager: $current_run not found, defaulting to 1.");
            currentRun = 1f;
        }
        if (!hasDay)
        {
            Debug.LogWarning("PauseMenuManager: $current_day not found, defaulting to 1.");
            currentDay = 1f;
        }

        int runInt = Mathf.RoundToInt(currentRun);
        int dayInt = Mathf.RoundToInt(currentDay);

        // Construct expected start node name for current day
        string expectedStartNode = $"R{runInt}_D{dayInt}_Start";

        // Check current node to see if we're already at the day start
        string currentNode = dialogueRunner.Dialogue.CurrentNode;
        string targetNode;

        if (!string.IsNullOrEmpty(currentNode) && currentNode == expectedStartNode)
        {
            // Already on the day start node - go to previous day if possible
            if (dayInt > 1)
            {
                // Go to previous day's start
                int previousDay = dayInt - 1;
                targetNode = $"R{runInt}_D{previousDay}_Start";
                Debug.Log($"PauseMenuManager: Already at {expectedStartNode}, going to previous day: {targetNode}");
            }
            else
            {
                // Can't go back further (already on day 1)
                targetNode = expectedStartNode;
                Debug.LogWarning($"PauseMenuManager: Already at {expectedStartNode} (Day 1), cannot go back further. Staying on current node.");
            }
        }
        else
        {
            // Not on day start - go to current day's start
            targetNode = expectedStartNode;
            Debug.Log($"PauseMenuManager: Restarting day - Jumping to node: {targetNode}");
        }

        // Stop current dialogue if it's running (especially if waiting for option selection)
        // This prevents DialogueException when trying to start new dialogue while waiting for options
        if (dialogueRunner.IsDialogueRunning)
        {
            Debug.Log("PauseMenuManager: Stopping current dialogue before restarting day");
            try
            {
                if (dialogueRunner.Dialogue != null)
                {
                    dialogueRunner.Dialogue.Stop();
                    Debug.Log("PauseMenuManager: Current dialogue stopped successfully");
                }
            }
            catch (System.Exception stopEx)
            {
                Debug.LogWarning($"PauseMenuManager: Error stopping dialogue (continuing anyway): {stopEx.Message}");
            }
        }

        // Jump to the target node
        // This works at any time, even if dialogue isn't running or tasks aren't completed
        try
        {
            dialogueRunner.StartDialogue(targetNode);
            Debug.Log($"PauseMenuManager: Successfully jumped to {targetNode}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PauseMenuManager: Failed to jump to node {targetNode}: {ex.Message}");
            return;
        }

        // Close the pause menu
        ResumeGame();
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

