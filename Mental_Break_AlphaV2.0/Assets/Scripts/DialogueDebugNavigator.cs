using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Developer-only debugging tool for navigating dialogue nodes quickly.
/// Provides keyboard shortcuts to go back one node or resume to the last visited node.
/// Only active in Editor or Development builds.
/// </summary>
public class DialogueDebugNavigator : MonoBehaviour
{
    [Header("Developer Navigation Settings")]
    [Tooltip("Enable developer navigation shortcuts (Editor/Development builds only)")]
    public bool enableNavigation = true;

    [Tooltip("Show debug overlay with current node info")]
    public bool showDebugOverlay = false;

    [Header("Keyboard Shortcuts")]
    [Tooltip("Go back one node in history")]
    public KeyCode backKey = KeyCode.LeftArrow;

    [Tooltip("Resume to last visited node")]
    public KeyCode resumeKey = KeyCode.R;

    [Tooltip("Clear navigation history")]
    public KeyCode clearHistoryKey = KeyCode.C;

    [Tooltip("Open jump to day menu")]
    public KeyCode jumpMenuKey = KeyCode.J;

    private DialogueRunner dialogueRunner;
    private List<string> nodeHistory = new List<string>();
    private string lastNodeName = null;
    private string currentNodeName = null;
    private string savedResumeNode = null; // Persisted resume position
    private bool isNavigating = false; // Prevent history updates during navigation
    private bool inputSystemActive = false; // Track which input system is active
    private bool showJumpMenu = false; // Track jump menu visibility

    // For keyboard shortcuts - require Ctrl/Cmd modifier
    private bool requiresModifier = true;

    // Jump menu data
    private struct JumpMenuItem
    {
        public string displayName;
        public string nodeName;
    }

    private JumpMenuItem[] jumpMenuItems = new JumpMenuItem[]
    {
        new JumpMenuItem { displayName = "Run 1 Day 1", nodeName = "R1_D1_Start" },
        new JumpMenuItem { displayName = "Run 1 Day 2", nodeName = "R1_D2_Start" },
        new JumpMenuItem { displayName = "Run 1 Day 3", nodeName = "R1_D3_Start" },
        new JumpMenuItem { displayName = "Run 2 Day 1", nodeName = "R2_D1_Start" },
        new JumpMenuItem { displayName = "Run 2 Day 2", nodeName = "R2_D2_Start" },
        new JumpMenuItem { displayName = "Run 2 Day 3", nodeName = "R2_D3_Start" },
        new JumpMenuItem { displayName = "Run 3 Day 1", nodeName = "R3_D1_Start" },
        new JumpMenuItem { displayName = "Run 3 Day 2", nodeName = "R3_D2_Start" },
        new JumpMenuItem { displayName = "Run 3 Day 3", nodeName = "R3_D3_Start" },
        new JumpMenuItem { displayName = "Run 4 Day 1", nodeName = "R4_D1_Start" },
    };

    // PlayerPrefs key for saving resume position
    private const string RESUME_NODE_KEY = "DebugNav_ResumeNode";

    private void Awake()
    {
        // Only enable in Editor or Development builds
#if !UNITY_EDITOR
        if (!Debug.isDebugBuild)
        {
            enableNavigation = false;
            this.enabled = false;
            return;
        }
#endif
    }

    private void Start()
    {
        if (!enableNavigation)
        {
            return;
        }

        // Check which input system is active
        CheckInputSystem();

        // Find DialogueRunner
        dialogueRunner = GetComponentInParent<DialogueRunner>();
        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }

        if (dialogueRunner == null)
        {
            Debug.LogWarning("DialogueDebugNavigator: DialogueRunner not found. Navigation disabled.");
            enabled = false;
            return;
        }

        // Subscribe to node events
        dialogueRunner.onNodeStart.AddListener(OnNodeStart);
        
        // Load saved resume position
        LoadResumePosition();
        
        Debug.Log("=== DialogueDebugNavigator: Navigation Enabled ===");
        Debug.Log($"  Ctrl+{backKey} - Go back one node");
        Debug.Log($"  Ctrl+{resumeKey} - Resume to saved position");
        Debug.Log($"  Ctrl+Shift+{clearHistoryKey} - Clear history");
        Debug.Log($"  Ctrl+{jumpMenuKey} - Jump to day menu");
        if (!string.IsNullOrEmpty(savedResumeNode))
        {
            Debug.Log($"  Saved resume position: {savedResumeNode}");
        }
        Debug.Log("==================================================");
    }

    private void CheckInputSystem()
    {
#if ENABLE_INPUT_SYSTEM
        // Check if Input System is the active input handler
        inputSystemActive = true;
#else
        inputSystemActive = false;
#endif
    }

    private void OnEnable()
    {
        if (dialogueRunner != null && enableNavigation)
        {
            dialogueRunner.onNodeStart.AddListener(OnNodeStart);
        }
    }

    private void OnDisable()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.onNodeStart.RemoveListener(OnNodeStart);
        }
    }

    private void OnDestroy()
    {
        // Ensure cleanup on destroy
        if (dialogueRunner != null)
        {
            dialogueRunner.onNodeStart.RemoveListener(OnNodeStart);
        }
        
        // Cancel any pending Invoke calls
        CancelInvoke();
    }

    private void Update()
    {
        // Early exit checks
        if (!enableNavigation || dialogueRunner == null || !this.enabled || !this.gameObject.activeInHierarchy)
        {
            return;
        }

        bool modifierPressed = false;
        bool backPressed = false;
        bool resumePressed = false;
        bool clearPressed = false;
        bool shiftPressed = false;
        bool jumpMenuPressed = false;

        // Use appropriate input system
        try
        {
#if ENABLE_INPUT_SYSTEM
            if (inputSystemActive)
            {
                // Use Input System API - convert KeyCode to Key
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    modifierPressed = keyboard.ctrlKey.isPressed || keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed;
                    shiftPressed = keyboard.shiftKey.isPressed;
                    
                    // Convert KeyCode to Key for Input System
                    Key backKeyInput = (Key)System.Enum.Parse(typeof(Key), backKey.ToString(), true);
                    Key resumeKeyInput = (Key)System.Enum.Parse(typeof(Key), resumeKey.ToString(), true);
                    Key clearKeyInput = (Key)System.Enum.Parse(typeof(Key), clearHistoryKey.ToString(), true);
                    Key jumpMenuKeyInput = (Key)System.Enum.Parse(typeof(Key), jumpMenuKey.ToString(), true);
                    
                    backPressed = keyboard[backKeyInput].wasPressedThisFrame;
                    resumePressed = keyboard[resumeKeyInput].wasPressedThisFrame;
                    clearPressed = keyboard[clearKeyInput].wasPressedThisFrame;
                    jumpMenuPressed = keyboard[jumpMenuKeyInput].wasPressedThisFrame;
                }
            }
            else
            {
                // Fallback to legacy Input (if Both is selected)
                modifierPressed = Input.GetKey(KeyCode.LeftControl) || 
                                   Input.GetKey(KeyCode.RightControl) ||
                                   Input.GetKey(KeyCode.LeftCommand) || 
                                   Input.GetKey(KeyCode.RightCommand);
                shiftPressed = Input.GetKey(KeyCode.LeftShift);
                backPressed = Input.GetKeyDown(backKey);
                resumePressed = Input.GetKeyDown(resumeKey);
                clearPressed = Input.GetKeyDown(clearHistoryKey);
                jumpMenuPressed = Input.GetKeyDown(jumpMenuKey);
            }
#else
            // Legacy Input only
            modifierPressed = Input.GetKey(KeyCode.LeftControl) || 
                               Input.GetKey(KeyCode.RightControl) ||
                               Input.GetKey(KeyCode.LeftCommand) || 
                               Input.GetKey(KeyCode.RightCommand);
            shiftPressed = Input.GetKey(KeyCode.LeftShift);
            backPressed = Input.GetKeyDown(backKey);
            resumePressed = Input.GetKeyDown(resumeKey);
            clearPressed = Input.GetKeyDown(clearHistoryKey);
            jumpMenuPressed = Input.GetKeyDown(jumpMenuKey);
#endif
        }
        catch (System.Exception ex)
        {
            // If input system throws exception, disable this component to prevent spam
            Debug.LogWarning($"DialogueDebugNavigator: Input error detected. Disabling navigation to prevent error spam: {ex.Message}");
            enableNavigation = false;
            this.enabled = false;
            return;
        }

        // Handle jump menu input FIRST (doesn't require modifiers)
        // This must be checked before the modifier requirement check
        if (showJumpMenu)
        {
            HandleJumpMenuInput();
            // Don't process other shortcuts when menu is open
            return;
        }

        // Toggle jump menu (requires modifier)
        if (jumpMenuPressed && modifierPressed)
        {
            showJumpMenu = !showJumpMenu;
            if (showJumpMenu)
            {
                Debug.Log("[Debug Nav] Jump menu opened. Press number keys 1-9 to select, Escape to close.");
            }
            else
            {
                Debug.Log("[Debug Nav] Jump menu closed.");
            }
            return;
        }

        // All other shortcuts require modifier
        if (!modifierPressed && requiresModifier)
        {
            return;
        }

        // Go back one node
        if (backPressed && modifierPressed)
        {
            GoBack();
        }

        // Resume to last node
        if (resumePressed && modifierPressed)
        {
            ResumeToLast();
        }

        // Clear history
        if (clearPressed && modifierPressed && shiftPressed)
        {
            ClearHistory();
        }
    }

    private void HandleJumpMenuInput()
    {
        bool escapePressed = false;
        int numberKeyPressed = -1;

        try
        {
#if ENABLE_INPUT_SYSTEM
            if (inputSystemActive)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    escapePressed = keyboard[Key.Escape].wasPressedThisFrame;
                    
                    // Check number keys 1-9 (both Digit1-9 and Numpad1-9)
                    for (int i = 1; i <= 9; i++)
                    {
                        // Try main keyboard number row
                        Key numKey = (Key)((int)Key.Digit1 + i - 1);
                        if (keyboard[numKey].wasPressedThisFrame)
                        {
                            numberKeyPressed = i;
                            Debug.Log($"[Debug Nav] Number key {i} pressed (Digit{i})");
                            break;
                        }
                        
                        // Try numpad (if available)
                        Key numpadKey = (Key)((int)Key.Numpad1 + i - 1);
                        if (keyboard[numpadKey].wasPressedThisFrame)
                        {
                            numberKeyPressed = i;
                            Debug.Log($"[Debug Nav] Number key {i} pressed (Numpad{i})");
                            break;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[Debug Nav] Keyboard is null when trying to handle jump menu input.");
                }
            }
            else
            {
                // Fallback to legacy Input when Input System is not active
                escapePressed = Input.GetKeyDown(KeyCode.Escape);
                // Check number keys 1-9 (both Alpha1-9 and Keypad1-9)
                for (int i = 1; i <= 9; i++)
                {
                    KeyCode numKey = (KeyCode)((int)KeyCode.Alpha1 + i - 1);
                    if (Input.GetKeyDown(numKey))
                    {
                        numberKeyPressed = i;
                        Debug.Log($"[Debug Nav] Number key {i} pressed (Alpha{i})");
                        break;
                    }
                    
                    // Try numpad
                    KeyCode numpadKey = (KeyCode)((int)KeyCode.Keypad1 + i - 1);
                    if (Input.GetKeyDown(numpadKey))
                    {
                        numberKeyPressed = i;
                        Debug.Log($"[Debug Nav] Number key {i} pressed (Keypad{i})");
                        break;
                    }
                }
            }
#else
            // Legacy Input only
            escapePressed = Input.GetKeyDown(KeyCode.Escape);
            // Check number keys 1-9 (both Alpha1-9 and Keypad1-9)
            for (int i = 1; i <= 9; i++)
            {
                KeyCode numKey = (KeyCode)((int)KeyCode.Alpha1 + i - 1);
                if (Input.GetKeyDown(numKey))
                {
                    numberKeyPressed = i;
                    Debug.Log($"[Debug Nav] Number key {i} pressed (Alpha{i})");
                    break;
                }
                
                // Try numpad
                KeyCode numpadKey = (KeyCode)((int)KeyCode.Keypad1 + i - 1);
                if (Input.GetKeyDown(numpadKey))
                {
                    numberKeyPressed = i;
                    Debug.Log($"[Debug Nav] Number key {i} pressed (Keypad{i})");
                    break;
                }
            }
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"DialogueDebugNavigator: Error handling jump menu input: {ex.Message}");
        }

        if (escapePressed)
        {
            showJumpMenu = false;
            Debug.Log("[Debug Nav] Jump menu closed (Escape pressed).");
        }
        else if (numberKeyPressed > 0 && numberKeyPressed <= jumpMenuItems.Length)
        {
            int index = numberKeyPressed - 1; // Convert 1-9 to 0-8 array index
            JumpToDay(jumpMenuItems[index].nodeName, jumpMenuItems[index].displayName);
            showJumpMenu = false;
        }
    }

    private void JumpToDay(string nodeName, string displayName)
    {
        if (dialogueRunner == null || !enableNavigation)
        {
            Debug.LogWarning("[Debug Nav] Cannot jump: DialogueRunner not found or navigation disabled.");
            return;
        }

        // Validate basic requirements
        if (dialogueRunner.YarnProject == null || dialogueRunner.YarnProject.Program == null)
        {
            Debug.LogError("[Debug Nav] Cannot jump: YarnProject or Program is null.");
            return;
        }

        Debug.Log($"[Debug Nav] Jumping to: {displayName} ({nodeName})");

        // Navigate
        isNavigating = true;
        dialogueRunner.StartDialogue(nodeName);
        
        // Reset flag after a short delay
        Invoke(nameof(ResetNavigationFlag), 0.1f);
    }

    private void OnNodeStart(string nodeName)
    {
        if (!enableNavigation || isNavigating)
        {
            currentNodeName = nodeName;
            return;
        }

        // Update current node
        currentNodeName = nodeName;

        // Add to history (avoid duplicates if same node visited immediately)
        if (nodeHistory.Count == 0 || nodeHistory[nodeHistory.Count - 1] != nodeName)
        {
            nodeHistory.Add(nodeName);
            Debug.Log($"[Debug Nav] Node visited: {nodeName} (History: {nodeHistory.Count} nodes)");
        }

        // Update last node (for resume functionality)
        if (lastNodeName != nodeName)
        {
            lastNodeName = nodeName;
        }

        // Save resume position (skip start nodes)
        if (!IsStartNode(nodeName))
        {
            SaveResumePosition(nodeName);
        }
    }

    /// <summary>
    /// Check if a node is a start node (should not be saved as resume point)
    /// </summary>
    private bool IsStartNode(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName))
        {
            return true;
        }

        // Match patterns like: R1_Start, R2_Start, R3_Start, etc.
        // Also match generic "Start" nodes
        return nodeName.EndsWith("_Start") || nodeName == "Start";
    }

    /// <summary>
    /// Save resume position to PlayerPrefs
    /// </summary>
    private void SaveResumePosition(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName))
        {
            return;
        }

        if (savedResumeNode != nodeName)
        {
            savedResumeNode = nodeName;
            PlayerPrefs.SetString(RESUME_NODE_KEY, nodeName);
            PlayerPrefs.Save();
            Debug.Log($"[Debug Nav] Saved resume position: {nodeName}");
        }
    }

    /// <summary>
    /// Load resume position from PlayerPrefs
    /// </summary>
    private void LoadResumePosition()
    {
        if (PlayerPrefs.HasKey(RESUME_NODE_KEY))
        {
            savedResumeNode = PlayerPrefs.GetString(RESUME_NODE_KEY);
            if (!string.IsNullOrEmpty(savedResumeNode))
            {
                Debug.Log($"[Debug Nav] Loaded saved resume position: {savedResumeNode}");
            }
        }
    }

    /// <summary>
    /// Navigate back one node in history
    /// </summary>
    public void GoBack()
    {
        if (dialogueRunner == null || !enableNavigation)
        {
            Debug.LogWarning("[Debug Nav] Cannot go back: DialogueRunner not found or navigation disabled.");
            return;
        }

        if (nodeHistory.Count <= 1)
        {
            Debug.LogWarning("[Debug Nav] Cannot go back: No previous nodes in history.");
            return;
        }

        // Remove current node from history
        if (nodeHistory.Count > 0)
        {
            nodeHistory.RemoveAt(nodeHistory.Count - 1);
        }

        // Get previous node
        string previousNode = nodeHistory[nodeHistory.Count - 1];

        // Validate basic requirements
        if (dialogueRunner.YarnProject == null || dialogueRunner.YarnProject.Program == null)
        {
            Debug.LogError("[Debug Nav] Cannot go back: YarnProject or Program is null.");
            return;
        }

        Debug.Log($"[Debug Nav] Going back to: {previousNode}");

        // Navigate
        isNavigating = true;
        
        // Stop current dialogue if running
        if (dialogueRunner.IsDialogueRunning)
        {
            // Note: DialogueRunner doesn't have a public Stop method, but StartDialogue will replace it
            // We'll just start the new dialogue
        }

        dialogueRunner.StartDialogue(previousNode);
        
        // Reset flag after a short delay (allows node to start)
        Invoke(nameof(ResetNavigationFlag), 0.1f);
    }

    /// <summary>
    /// Resume to the saved persistent position, or fallback to last in-memory node
    /// </summary>
    public void ResumeToLast()
    {
        if (dialogueRunner == null || !enableNavigation)
        {
            Debug.LogWarning("[Debug Nav] Cannot resume: DialogueRunner not found or navigation disabled.");
            return;
        }

        // Validate basic requirements
        if (dialogueRunner.YarnProject == null || dialogueRunner.YarnProject.Program == null)
        {
            Debug.LogError("[Debug Nav] Cannot resume: YarnProject or Program is null.");
            return;
        }

        // Prioritize saved persistent position over in-memory last node
        string resumeNode = null;
        string source = "";

        if (!string.IsNullOrEmpty(savedResumeNode))
        {
            resumeNode = savedResumeNode;
            source = "saved persistent position";
        }
        else if (!string.IsNullOrEmpty(lastNodeName) && !IsStartNode(lastNodeName))
        {
            resumeNode = lastNodeName;
            source = "in-memory last node";
        }

        if (string.IsNullOrEmpty(resumeNode))
        {
            Debug.LogWarning("[Debug Nav] Cannot resume: No saved position or last node available.");
            return;
        }

        Debug.Log($"[Debug Nav] Resuming to: {resumeNode} (from {source})");

        // Navigate
        isNavigating = true;
        dialogueRunner.StartDialogue(resumeNode);
        
        // Reset flag after a short delay
        Invoke(nameof(ResetNavigationFlag), 0.1f);
    }

    /// <summary>
    /// Clear navigation history and saved resume position
    /// </summary>
    public void ClearHistory()
    {
        nodeHistory.Clear();
        savedResumeNode = null;
        PlayerPrefs.DeleteKey(RESUME_NODE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[Debug Nav] History and saved resume position cleared.");
    }

    private void ResetNavigationFlag()
    {
        isNavigating = false;
    }

    /// <summary>
    /// Get current node name
    /// </summary>
    public string GetCurrentNode()
    {
        return currentNodeName;
    }

    /// <summary>
    /// Get navigation history count
    /// </summary>
    public int GetHistoryCount()
    {
        return nodeHistory.Count;
    }

    /// <summary>
    /// Get last node name
    /// </summary>
    public string GetLastNode()
    {
        return lastNodeName;
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!enableNavigation)
        {
            return;
        }

        // Draw jump menu
        if (showJumpMenu)
        {
            DrawJumpMenu();
        }

        // Draw debug overlay
        if (showDebugOverlay)
        {
            DrawDebugOverlay();
        }
    }

    private void DrawJumpMenu()
    {
        // Semi-transparent background
        GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 150, 400, 300), "");
        
        // Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 140, 400, 30), "Jump to Day", titleStyle);

        // Instructions
        GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
        instructionStyle.fontSize = 11;
        instructionStyle.normal.textColor = Color.yellow;
        instructionStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 110, 400, 20), "Press number key to select, Escape to close", instructionStyle);

        // Menu items
        GUIStyle itemStyle = new GUIStyle(GUI.skin.label);
        itemStyle.fontSize = 14;
        itemStyle.normal.textColor = Color.white;
        itemStyle.alignment = TextAnchor.MiddleLeft;

        int yOffset = Screen.height / 2 - 80;
        for (int i = 0; i < jumpMenuItems.Length; i++)
        {
            string itemText = $"{i + 1}. {jumpMenuItems[i].displayName}";
            GUI.Label(new Rect(Screen.width / 2 - 180, yOffset + i * 25, 360, 25), itemText, itemStyle);
        }
    }

    private void DrawDebugOverlay()
    {
        // Draw debug overlay in top-left corner
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;
        style.normal.textColor = Color.yellow;
        style.alignment = TextAnchor.UpperLeft;

        Rect rect = new Rect(10, 10, 400, 150);
        string info = $"Debug Navigation:\n";
        info += $"Current: {currentNodeName ?? "None"}\n";
        info += $"Last (memory): {lastNodeName ?? "None"}\n";
        info += $"Saved Resume: {savedResumeNode ?? "None"}\n";
        info += $"History: {nodeHistory.Count} nodes";

        GUI.Label(rect, info, style);
    }
#endif
}

