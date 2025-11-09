using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;
using System.Linq;
using System.Collections;

/// <summary>
/// Manages game state and coordinates all game systems.
/// Handles initialization, run transitions, and dialogue startup.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The DialogueRunner component")]
    public DialogueRunner dialogueRunner;

    [Tooltip("The SaveLoadManager component")]
    public SaveLoadManager saveLoadManager;

    [Tooltip("The RunTransitionManager component")]
    public RunTransitionManager runTransitionManager;

    [Tooltip("The start node name for dialogue")]
    public string startNode = "R1_Start";

    [Header("Settings")]
    [Tooltip("Should dialogue start automatically when scene loads?")]
    public bool startDialogueOnLoad = true;

    [Tooltip("Should the game attempt to load a save on start?")]
    public bool loadSaveOnStart = false;

    private int completedRun = 0;
    private int startingRun = 1;

    private void Awake()
    {
        // Remove duplicate EventSystems and AudioListeners first
        RemoveDuplicateComponents();

        // Find components if not assigned
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (saveLoadManager == null)
        {
            saveLoadManager = FindAnyObjectByType<SaveLoadManager>();
        }

        if (runTransitionManager == null)
        {
            runTransitionManager = FindAnyObjectByType<RunTransitionManager>();
        }

        // Ensure command handlers are registered (fallback if CommandHandlerRegistrar is missing)
        RegisterCommandsIfNeeded();
    }

    /// <summary>
    /// Register commands directly if CommandHandlerRegistrar is missing
    /// </summary>
    private void RegisterCommandsIfNeeded()
    {
        // Check if CommandHandlerRegistrar exists
        CommandHandlerRegistrar commandRegistrar = FindAnyObjectByType<CommandHandlerRegistrar>();
        
        if (commandRegistrar == null && dialogueRunner != null)
        {
            Debug.LogWarning("GameManager: CommandHandlerRegistrar not found. Registering commands directly...");
            
            // Find command handlers
            BackgroundCommandHandler backgroundHandler = FindAnyObjectByType<BackgroundCommandHandler>();
            AudioCommandHandler audioHandler = FindAnyObjectByType<AudioCommandHandler>();
            
            // Register commands directly with DialogueRunner
            if (backgroundHandler != null)
            {
                try
                {
                    dialogueRunner.AddCommandHandler("bg", new System.Action<string>(backgroundHandler.ChangeBackground));
                    Debug.Log("GameManager: Registered 'bg' command directly");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"GameManager: Failed to register 'bg' command: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("GameManager: BackgroundCommandHandler not found!");
            }
            
            if (audioHandler != null)
            {
                try
                {
                    dialogueRunner.AddCommandHandler("bgm", new System.Action<string>(audioHandler.PlayBGM));
                    dialogueRunner.AddCommandHandler("sfx", new System.Action<string>(audioHandler.PlaySFX));
                    Debug.Log("GameManager: Registered 'bgm' and 'sfx' commands directly");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"GameManager: Failed to register audio commands: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("GameManager: AudioCommandHandler not found!");
            }
        }
    }

    private void Start()
    {
        // Ensure SaveLoadManager has initialized its references (all Awake() methods have completed)
        if (saveLoadManager != null)
        {
            // Force SaveLoadManager to ensure it has references
            if (saveLoadManager.dialogueRunner == null && dialogueRunner != null)
            {
                saveLoadManager.dialogueRunner = dialogueRunner;
            }
        }

        // Try to load save if enabled and available (now that all Awake() methods have run)
        bool saveLoaded = false;
        if (loadSaveOnStart && saveLoadManager != null)
        {
            saveLoaded = saveLoadManager.LoadGame();
            if (saveLoaded)
            {
                Debug.Log("GameManager: Loaded save game");
                // Save loaded - start node will be set by SaveLoadManager
                // Update startNode from DialogueRunner after load
                if (dialogueRunner != null)
                {
                    startNode = dialogueRunner.startNode;
                    // Get current run from variable storage after load
                    if (dialogueRunner.VariableStorage != null && dialogueRunner.VariableStorage.TryGetValue<float>("$current_run", out var runValue))
                    {
                        startingRun = Mathf.RoundToInt(runValue);
                    }
                }
            }
        }

        // No save loaded - initialize fresh game state
        if (!saveLoaded)
        {
            InitializeVariables();
            DetermineStartNode();
        }

        if (dialogueRunner != null)
        {
            // Subscribe to dialogue completion to detect run endings
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
            dialogueRunner.onNodeComplete.AddListener(OnNodeComplete);
        }

        // Initialize run transition manager if available
        if (runTransitionManager != null && dialogueRunner != null && dialogueRunner.VariableStorage != null)
        {
            // Get current run number
            float currentRun = 1f;
            dialogueRunner.VariableStorage.TryGetValue<float>("$current_run", out currentRun);
            startingRun = Mathf.RoundToInt(currentRun);
            
            // Initialize for this run
            runTransitionManager.InitializeForRun(startingRun);
        }

        // Auto-save initial state (legacy - checkpoints will handle autosaves now)
        if (saveLoadManager != null && saveLoadManager.enableAutoSave)
        {
            // Only autosave if we're not loading a save
            if (!saveLoaded)
            {
                // Initial checkpoint for fresh start
                saveLoadManager.SetCheckpoint("game_start");
            }
        }

        if (startDialogueOnLoad && dialogueRunner != null)
        {
            // Ensure CommandHandlerRegistrar has initialized before starting dialogue
            CommandHandlerRegistrar commandRegistrar = FindAnyObjectByType<CommandHandlerRegistrar>();
            if (commandRegistrar != null)
            {
                // Commands should be registered in CommandHandlerRegistrar.OnEnable()
                // which runs before Start(), so they should be ready
                Debug.Log("GameManager: CommandHandlerRegistrar found. Commands should be registered.");
            }
            else
            {
                Debug.LogWarning("GameManager: CommandHandlerRegistrar not found. Commands may not work!");
            }

            // Mark that a run has started
            MenuManager.MarkRunStarted();

            // Ensure dialogue UI is visible before starting
            EnsureDialogueUIReady();

            // Debug: Check if YarnProject is properly set
            if (dialogueRunner.YarnProject == null)
            {
                Debug.LogError("GameManager: Cannot start dialogue - YarnProject is null!");
                return;
            }

            if (dialogueRunner.YarnProject.Program == null)
            {
                Debug.LogError("GameManager: Cannot start dialogue - YarnProject.Program is null! Check for compilation errors.");
                return;
            }

            // Debug: Log the start node
            Debug.Log($"GameManager: Starting dialogue at node: {startNode}");

            // Start dialogue
            dialogueRunner.StartDialogue(startNode);
        }
    }

    /// <summary>
    /// Ensure dialogue UI components are ready and visible
    /// </summary>
    private void EnsureDialogueUIReady()
    {
        // Find all LinePresenters and ensure their CanvasGroups are initialized
        LinePresenter[] linePresenters = FindObjectsByType<LinePresenter>(FindObjectsSortMode.None);
        foreach (var presenter in linePresenters)
        {
            if (presenter != null && presenter.canvasGroup != null)
            {
                // Reset alpha to 0 initially (LinePresenter will fade in)
                // This prevents stuck intermediate states
                presenter.canvasGroup.alpha = 0f;
                presenter.canvasGroup.interactable = true;
                presenter.canvasGroup.blocksRaycasts = true;
            }
        }
        
        // Subscribe to dialogue start to monitor and force visibility
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.AddListener(OnDialogueStarted);
        }
        
        // Start monitoring LinePresenter visibility immediately
        StartCoroutine(MonitorLinePresenterVisibility());
        
        // Also start a coroutine to fix alpha immediately after dialogue starts
        StartCoroutine(FixAlphaAfterDialogueStart());
    }
    
    /// <summary>
    /// Called when dialogue starts - ensure UI is ready
    /// </summary>
    private void OnDialogueStarted()
    {
        ForceLinePresenterVisibility();
    }
    
    /// <summary>
    /// Coroutine to continuously monitor and ensure LinePresenter visibility
    /// Only corrects stuck states (low alpha when dialogue is running)
    /// </summary>
    private System.Collections.IEnumerator MonitorLinePresenterVisibility()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds instead of every frame
            
            // Only check visibility if dialogue is running
            if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
            {
                LinePresenter[] linePresenters = FindObjectsByType<LinePresenter>(FindObjectsSortMode.None);
                foreach (var presenter in linePresenters)
                {
                    if (presenter != null && presenter.canvasGroup != null)
                    {
                        // Only fix if alpha is stuck at a low value (below 0.5)
                        // This allows normal fade animations to complete, but fixes stuck states
                        if (presenter.canvasGroup.alpha < 0.5f && presenter.canvasGroup.alpha > 0.01f)
                        {
                            Debug.LogWarning($"GameManager: LinePresenter alpha stuck at {presenter.canvasGroup.alpha}. Forcing to 1.0");
                            presenter.canvasGroup.alpha = 1.0f;
                        }
                        // Ensure interactability and raycast blocking
                        presenter.canvasGroup.interactable = true;
                        presenter.canvasGroup.blocksRaycasts = true;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Forces all LinePresenters to be fully visible (called when dialogue starts)
    /// </summary>
    private void ForceLinePresenterVisibility()
    {
        LinePresenter[] linePresenters = FindObjectsByType<LinePresenter>(FindObjectsSortMode.None);
        foreach (var presenter in linePresenters)
        {
            if (presenter != null && presenter.canvasGroup != null)
            {
                // Force full visibility when dialogue starts
                presenter.canvasGroup.alpha = 1.0f;
                presenter.canvasGroup.interactable = true;
                presenter.canvasGroup.blocksRaycasts = true;
            }
        }
    }
    
    /// <summary>
    /// Coroutine to fix alpha immediately after dialogue starts
    /// Waits for end of frame to ensure LinePresenter has processed, then fixes alpha
    /// </summary>
    private System.Collections.IEnumerator FixAlphaAfterDialogueStart()
    {
        // Wait until dialogue starts running
        while (dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
        {
            yield return null;
        }
        
        // Wait for end of frame to ensure LinePresenter has set initial alpha
        yield return new WaitForEndOfFrame();
        
        // Wait a small additional frame to ensure fade animations have started
        yield return new WaitForSeconds(0.1f);
        
        // Now check and fix alpha if it's stuck
        LinePresenter[] linePresenters = FindObjectsByType<LinePresenter>(FindObjectsSortMode.None);
        foreach (var presenter in linePresenters)
        {
            if (presenter != null && presenter.canvasGroup != null)
            {
                // If alpha is stuck at a low value, force it to 1.0
                if (presenter.canvasGroup.alpha < 0.5f && presenter.canvasGroup.alpha > 0.01f)
                {
                    Debug.LogWarning($"GameManager: FixAlphaAfterDialogueStart - LinePresenter alpha stuck at {presenter.canvasGroup.alpha}. Forcing to 1.0");
                    presenter.canvasGroup.alpha = 1.0f;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
            dialogueRunner.onNodeComplete.RemoveListener(OnNodeComplete);
            dialogueRunner.onDialogueStart.RemoveListener(OnDialogueStarted);
        }
        
        StopAllCoroutines();
    }

    /// <summary>
    /// Initialize all Yarn variables with default values
    /// Only initializes if this is a truly fresh start
    /// </summary>
    private void InitializeVariables()
    {
        if (dialogueRunner == null || dialogueRunner.VariableStorage == null) return;

        var storage = dialogueRunner.VariableStorage;

        // Check if this is a fresh start (no variables set)
        if (!PlayerPrefs.HasKey("GameInitialized"))
        {
            // Use SaveLoadManager reset if available, otherwise manual init
            if (saveLoadManager != null)
            {
                saveLoadManager.ResetGame();
            }
            else
            {
                // Initialize default values
                storage.SetValue("$current_run", 1);
                storage.SetValue("$current_day", 1);
                storage.SetValue("$engagement", 0);
                storage.SetValue("$sanity", 50); // Start at neutral
                storage.SetValue("$leaderboard_rank", 50); // Middle of the pack
                storage.SetValue("$trust_supervisor", 0);
                storage.SetValue("$trust_alice", 0);
                storage.SetValue("$trust_timmy", 0);
                storage.SetValue("$rapid_feedback_cash", 0);
                storage.SetValue("$alert_level", 0);
                storage.SetValue("$cross_talk_heard", 0);
                storage.SetValue("$war_ops_awareness", 0);
                storage.SetValue("$item_mental_break", false);
                storage.SetValue("$item_blackout_curtains", false);
                storage.SetValue("$item_blue_light_filter", false);
                storage.SetValue("$item_screen_protector", false);
                storage.SetValue("$item_priority_shipping", false);
                storage.SetValue("$item_bow_for_alice", false);
                storage.SetValue("$item_corporate_bond", false);
                storage.SetValue("$store_blackout_pending", false);
                storage.SetValue("$store_blue_filter_active", false);
                storage.SetValue("$store_blue_filter_target_run", 0);
                storage.SetValue("$store_blue_filter_target_day", 0);
                storage.SetValue("$store_blue_filter_penalties_applied", 0);
                storage.SetValue("$store_blue_filter_bonus_applied", false);
                storage.SetValue("$store_screen_protector_heat_modifier", 0);
                storage.SetValue("$store_prev_engagement", 0);
                storage.SetValue("$store_corporate_bond_active", false);
                storage.SetValue("$store_corporate_bond_principal", 0);
                storage.SetValue("$store_corporate_bond_mature_run", 0);
                storage.SetValue("$store_corporate_bond_mature_day", 0);
                storage.SetValue("$ends_seen", 0);

                // Boolean flags
                storage.SetValue("$aware_run_order", false);
                storage.SetValue("$aware_observation_window", false);
                storage.SetValue("$aware_psyops", false);
                storage.SetValue("$aware_majority_mind", false);

                PlayerPrefs.SetInt("GameInitialized", 1);
                PlayerPrefs.Save();
            }
        }
        else
        {
            // Game was initialized before - load persistent variables
            if (runTransitionManager != null)
            {
                runTransitionManager.LoadPersistentVariables();
            }
        }
    }


    /// <summary>
    /// Called when dialogue completes (may indicate run completion)
    /// </summary>
    private void OnDialogueComplete()
    {
        // Check if we just completed a run by checking the current run variable
        if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
        {
            float currentRun = 0f;
            if (dialogueRunner.VariableStorage.TryGetValue<float>("$current_run", out var runValue))
            {
                currentRun = runValue;
                
                // If we're transitioning to a new run (current_run > what we started with)
                // or if we've reached the end (Run 4 epilogue), mark completion
                // This is a simple heuristic - can be refined
            }
        }
    }

    /// <summary>
    /// Called when a node completes - check if it's a run transition node
    /// </summary>
    private void OnNodeComplete(string nodeName)
    {
        // Check if this is a run transition node
        if (nodeName.Contains("To_Run") || nodeName.Contains("_End") || nodeName == "EP_Stinger")
        {
            // Detect which run just completed by checking current_run variable
            if (dialogueRunner != null && dialogueRunner.VariableStorage != null)
            {
                float currentRun = 0f;
                if (dialogueRunner.VariableStorage.TryGetValue<float>("$current_run", out var runValue))
                {
                    currentRun = runValue;
                    
                    // If current_run was just set (transitioning to next run), the previous run is current_run - 1
                    // But if we're at a transition node, the dialogue has already set the next run number
                    // So we need to detect the completed run differently
                    
                    // Check if we're at a transition node that indicates completion
                    // When dialogue sets current_run = 2, that means Run 1 just completed
                    if (nodeName.StartsWith("R1_To_Run"))
                    {
                        // Dialogue sets current_run = 2, so Run 1 just completed
                        completedRun = 1;
                        MarkRunCompleted(1);
                    }
                    else if (nodeName.StartsWith("R2_To_Run") || nodeName.StartsWith("R2_End"))
                    {
                        // Run 2 completed, dialogue sets current_run = 3
                        completedRun = 2;
                        MarkRunCompleted(2);
                    }
                    else if (nodeName.StartsWith("R3_To_Run") || nodeName.StartsWith("R3_End"))
                    {
                        // Run 3 completed, dialogue sets current_run = 4
                        completedRun = 3;
                        MarkRunCompleted(3);
                    }
                    else if (nodeName.StartsWith("R4_End") || nodeName == "EP_Stinger")
                    {
                        // Run 4 completed - final run
                        completedRun = 4;
                        MarkRunCompleted(4);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Determine which dialogue node to start based on save state
    /// </summary>
    private void DetermineStartNode()
    {
        if (dialogueRunner == null || dialogueRunner.VariableStorage == null) return;

        int savedRun = PlayerPrefs.GetInt("CurrentRun", 0);
        bool runInProgress = PlayerPrefs.GetInt("RunInProgress", 0) == 1;

        // If no run is in progress, check which run to start
        if (!runInProgress)
        {
            if (savedRun == 0)
            {
                // First time - start Run 1
                startNode = "R1_Start";
                dialogueRunner.VariableStorage.SetValue("$current_run", 1f);
            }
            else if (savedRun == 1)
            {
                // Run 1 complete - start Run 2
                startNode = "R2_Start";
                dialogueRunner.VariableStorage.SetValue("$current_run", 2f);
            }
            else if (savedRun == 2)
            {
                // Run 2 complete - start Run 3
                startNode = "R3_Start";
                dialogueRunner.VariableStorage.SetValue("$current_run", 3f);
            }
            else if (savedRun == 3)
            {
                // Run 3 complete - start Run 4
                startNode = "R4_Start";
                dialogueRunner.VariableStorage.SetValue("$current_run", 4f);
            }
            else if (savedRun >= 4)
            {
                // All runs complete - restart from Run 1 or allow replay
                startNode = "R1_Start";
                dialogueRunner.VariableStorage.SetValue("$current_run", 1f);
                // Optionally reset all progress for a fresh playthrough
                // ResetProgress();
            }
        }
        else
        {
            // Run is in progress - determine which run we're in and continue from appropriate node
            // This would require more sophisticated save/load system
            // For now, default to the saved run's start node
            if (savedRun == 0) startNode = "R1_Start";
            else if (savedRun == 1) startNode = "R2_Start";
            else if (savedRun == 2) startNode = "R3_Start";
            else if (savedRun == 3) startNode = "R4_Start";
            else startNode = "R1_Start";
        }
    }

    /// <summary>
    /// Mark a run as completed and update save state
    /// </summary>
    private void MarkRunCompleted(int runNumber)
    {
        completedRun = runNumber;
        
        // Use RunTransitionManager if available
        if (runTransitionManager != null)
        {
            runTransitionManager.OnRunComplete(runNumber);
        }
        else
        {
            // Fallback to basic completion
            MenuManager.MarkRunCompleted(runNumber);
        }
        
        // Auto-save before transition
        if (saveLoadManager != null)
        {
            saveLoadManager.SaveGame(0); // Save to autosave slot
        }
        
        Debug.Log($"Run {runNumber} marked as completed!");
    }

    /// <summary>
    /// Manually start dialogue (called from menu or other triggers)
    /// </summary>
    public void StartDialogue(string nodeName = null)
    {
        if (dialogueRunner == null)
        {
            Debug.LogError("Cannot start dialogue: DialogueRunner not found!");
            return;
        }

        string targetNode = nodeName ?? startNode;
        dialogueRunner.StartDialogue(targetNode);
    }

    /// <summary>
    /// Remove duplicate EventSystems and AudioListeners to ensure only one of each exists.
    /// </summary>
    private void RemoveDuplicateComponents()
    {
        // Remove duplicate EventSystems
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length > 1)
        {
            Debug.LogWarning($"GameManager: Found {eventSystems.Length} EventSystems. Removing {eventSystems.Length - 1} duplicate(s).");
            
            // Keep the first one, destroy the rest
            for (int i = 1; i < eventSystems.Length; i++)
            {
                Debug.Log($"GameManager: Destroying duplicate EventSystem: {eventSystems[i].gameObject.name}");
                Destroy(eventSystems[i].gameObject);
            }
        }

        // Remove duplicate AudioListeners
        AudioListener[] audioListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (audioListeners.Length > 1)
        {
            Debug.LogWarning($"GameManager: Found {audioListeners.Length} AudioListeners. Removing {audioListeners.Length - 1} duplicate(s).");
            
            // Prefer keeping one on Main Camera, otherwise keep the first
            AudioListener keepListener = null;
            
            foreach (var listener in audioListeners)
            {
                if (listener.gameObject.CompareTag("MainCamera"))
                {
                    keepListener = listener;
                    break;
                }
            }
            
            if (keepListener == null)
            {
                keepListener = audioListeners[0];
            }
            
            // Remove all others (just the component, not the GameObject)
            foreach (var listener in audioListeners)
            {
                if (listener != keepListener)
                {
                    Debug.Log($"GameManager: Destroying duplicate AudioListener on: {listener.gameObject.name}");
                    Destroy(listener);
                }
            }
        }
    }
}
