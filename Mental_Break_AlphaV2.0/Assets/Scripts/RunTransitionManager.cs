using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

/// <summary>
/// Manages transitions between runs, preserving story state and resetting run-specific variables.
/// </summary>
public class RunTransitionManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Name of the main menu scene")]
    public string mainMenuSceneName = "MainMenu";

    private DialogueRunner dialogueRunner;
    private VariableStorageBehaviour variableStorage;
    private SaveLoadManager saveLoadManager;

    // Variables that persist across all runs (story progression)
    private static readonly string[] PersistentVariables = {
        "$aware_run_order",
        "$aware_observation_window",
        "$aware_psyops",
        "$aware_majority_mind",
        "$cross_talk_heard",
        "$war_ops_awareness",
        "$ends_seen",
        "$trust_alice",
        "$trust_timmy",
        "$trust_supervisor"
    };

    // Variables that reset each run
    private static readonly string[] RunSpecificVariables = {
        "$current_day",
        "$engagement",
        "$sanity",
        "$leaderboard_rank",
        "$rapid_feedback_cash",
        "$alert_level"
    };

    private void Awake()
    {
        dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        if (dialogueRunner != null)
        {
            variableStorage = dialogueRunner.VariableStorage;
        }
        
        saveLoadManager = FindAnyObjectByType<SaveLoadManager>();
    }


    /// <summary>
    /// Called when a run completes. Handles transition to next run or back to menu.
    /// </summary>
    public void OnRunComplete(int completedRunNumber)
    {
        if (variableStorage == null)
        {
            Debug.LogError("RunTransitionManager: VariableStorage not found!");
            return;
        }

        // Save persistent variables before resetting
        SavePersistentVariables();

        // Update current run number
        float nextRun = completedRunNumber + 1;
        variableStorage.SetValue("$current_run", nextRun);

        // Reset run-specific variables
        ResetRunSpecificVariables();

        // Auto-save progress
        if (saveLoadManager != null)
        {
            saveLoadManager.SaveGame(0); // Save to autosave slot
        }

        // Mark run as completed
        MenuManager.MarkRunCompleted(completedRunNumber);

        // Return to main menu
        ReturnToMainMenu();
    }

    /// <summary>
    /// Called when transitioning from one run to the next.
    /// Preserves story state, resets run metrics.
    /// </summary>
    public void TransitionToNextRun(int fromRun, int toRun)
    {
        if (variableStorage == null)
        {
            Debug.LogError("RunTransitionManager: VariableStorage not found!");
            return;
        }

        // Save persistent variables
        SavePersistentVariables();

        // Reset run-specific variables
        ResetRunSpecificVariables();

        // Set new run number
        variableStorage.SetValue("$current_run", (float)toRun);
        variableStorage.SetValue("$current_day", 1f);

        // Update awareness flags based on completed run
        UpdateAwarenessFlags(fromRun);

        // Auto-save
        if (saveLoadManager != null)
        {
            saveLoadManager.SaveGame(0); // Save to autosave slot
        }

        Debug.Log($"Transitioned from Run {fromRun} to Run {toRun}");
    }

    /// <summary>
    /// Save persistent variables to PlayerPrefs so they survive scene transitions
    /// </summary>
    private void SavePersistentVariables()
    {
        if (variableStorage == null) return;

        foreach (string varName in PersistentVariables)
        {
            // Try float
            if (variableStorage.TryGetValue<float>(varName, out var floatValue))
            {
                PlayerPrefs.SetFloat($"Persistent_{varName}", floatValue);
            }
            // Try bool
            else if (variableStorage.TryGetValue<bool>(varName, out var boolValue))
            {
                PlayerPrefs.SetInt($"Persistent_{varName}", boolValue ? 1 : 0);
            }
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load persistent variables from PlayerPrefs
    /// </summary>
    public void LoadPersistentVariables()
    {
        if (variableStorage == null) return;

        foreach (string varName in PersistentVariables)
        {
            string key = $"Persistent_{varName}";
            
            // Check if variable was saved as float or bool
            if (PlayerPrefs.HasKey(key))
            {
                // Try float first (most common)
                float floatValue = PlayerPrefs.GetFloat(key, float.NaN);
                if (!float.IsNaN(floatValue))
                {
                    variableStorage.SetValue(varName, floatValue);
                }
                else
                {
                    // Try as int/bool
                    int intValue = PlayerPrefs.GetInt(key, -1);
                    if (intValue >= 0)
                    {
                        variableStorage.SetValue(varName, intValue == 1);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reset variables that are specific to each run
    /// </summary>
    private void ResetRunSpecificVariables()
    {
        if (variableStorage == null) return;

        // Reset to default values
        variableStorage.SetValue("$current_day", 1f);
        variableStorage.SetValue("$engagement", 0f);
        variableStorage.SetValue("$sanity", 50f); // Start at neutral
        variableStorage.SetValue("$leaderboard_rank", 50f); // Middle of pack
        variableStorage.SetValue("$rapid_feedback_cash", 0f);
        variableStorage.SetValue("$alert_level", 0f);
        variableStorage.SetValue("$item_mental_break", false);
        variableStorage.SetValue("$item_blackout_curtains", false);
        variableStorage.SetValue("$item_blue_light_filter", false);
        variableStorage.SetValue("$item_screen_protector", false);
        variableStorage.SetValue("$item_priority_shipping", false);
        variableStorage.SetValue("$item_bow_for_alice", false);
        variableStorage.SetValue("$item_corporate_bond", false);
        variableStorage.SetValue("$store_blackout_pending", false);
        variableStorage.SetValue("$store_blue_filter_active", false);
        variableStorage.SetValue("$store_blue_filter_target_run", 0f);
        variableStorage.SetValue("$store_blue_filter_target_day", 0f);
        variableStorage.SetValue("$store_blue_filter_penalties_applied", 0f);
        variableStorage.SetValue("$store_blue_filter_bonus_applied", false);
        variableStorage.SetValue("$store_screen_protector_heat_modifier", 0f);
        variableStorage.SetValue("$store_prev_engagement", 0f);
        variableStorage.SetValue("$store_corporate_bond_active", false);
        variableStorage.SetValue("$store_corporate_bond_principal", 0f);
        variableStorage.SetValue("$store_corporate_bond_mature_run", 0f);
        variableStorage.SetValue("$store_corporate_bond_mature_day", 0f);
    }

    /// <summary>
    /// Update awareness flags based on which run was just completed
    /// </summary>
    private void UpdateAwarenessFlags(int completedRun)
    {
        if (variableStorage == null) return;

        switch (completedRun)
        {
            case 1:
                // Run 1 completes - player now knows they're an AI
                variableStorage.SetValue("$aware_run_order", true);
                break;
            case 2:
                // Run 2 completes - player knows about Alice's observation window
                variableStorage.SetValue("$aware_observation_window", true);
                break;
            case 3:
                // Run 3 completes - player knows about psyops
                variableStorage.SetValue("$aware_psyops", true);
                variableStorage.SetValue("$war_ops_awareness", 1f);
                break;
            case 4:
                // Run 4 completes - player has seen Majority Mind
                variableStorage.SetValue("$aware_majority_mind", true);
                float endsSeen = 0f;
                if (variableStorage.TryGetValue<float>("$ends_seen", out var endsValue))
                {
                    endsSeen = endsValue;
                }
                variableStorage.SetValue("$ends_seen", endsSeen + 1f);
                break;
        }
    }

    /// <summary>
    /// Return to main menu scene
    /// </summary>
    private void ReturnToMainMenu()
    {
        Debug.Log($"[RunTransitionManager] Returning to main menu... Scene name: '{mainMenuSceneName}'");
        
        // Verify scene exists in build settings
        bool sceneExists = false;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName == mainMenuSceneName)
            {
                sceneExists = true;
                break;
            }
        }
        
        if (!sceneExists)
        {
            Debug.LogError($"[RunTransitionManager] Scene '{mainMenuSceneName}' not found in build settings! Available scenes:");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                Debug.LogError($"  - {sceneName}");
            }
            return;
        }
        
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Yarn command handler for <<return_to_menu>>
    /// Called from Yarn scripts to return to the main menu after completing a run.
    /// Usage in Yarn: <<return_to_menu>>
    /// This will save all persistent state and transition to the main menu.
    /// Auto-discovered by Yarn Spinner via [YarnCommand] attribute.
    /// Static method doesn't require GameObject target - Yarn can call it directly.
    /// </summary>
    [YarnCommand("return_to_menu")]
    public static void ReturnToMenuCommand()
    {
        Debug.Log("[RunTransitionManager] return_to_menu command called!");
        
        // Find RunTransitionManager instance in the scene
        RunTransitionManager instance = FindFirstObjectByType<RunTransitionManager>();
        if (instance == null)
        {
            Debug.LogError("[RunTransitionManager] RunTransitionManager component not found in scene! Cannot execute return_to_menu command. Please ensure RunTransitionManager component is attached to a GameObject in MVPScene.");
            return;
        }
        
        // Get the current run number from variables
        int currentRun = 1;
        if (instance.variableStorage != null)
        {
            if (instance.variableStorage.TryGetValue<float>("$current_run", out var runValue))
            {
                // current_run is set to 2 in R1_To_Run2 before calling this command
                // So the completed run is runValue - 1
                currentRun = Mathf.Max(1, Mathf.RoundToInt(runValue) - 1);
                Debug.Log($"[RunTransitionManager] Current run from variable: {runValue}, Completed run: {currentRun}");
            }
            else
            {
                Debug.LogWarning("[RunTransitionManager] $current_run variable not found, defaulting to run 1");
            }
        }
        else
        {
            Debug.LogError("[RunTransitionManager] VariableStorage is null! Cannot read $current_run.");
        }

        // Call OnRunComplete to save state and return to menu
        Debug.Log($"[RunTransitionManager] Calling OnRunComplete({currentRun})");
        instance.OnRunComplete(currentRun);
    }

    /// <summary>
    /// Initialize variables for a new run based on previous runs' persistent state
    /// </summary>
    public void InitializeForRun(int runNumber)
    {
        if (variableStorage == null) return;

        // Load persistent variables
        LoadPersistentVariables();

        // Set run and day
        variableStorage.SetValue("$current_run", (float)runNumber);
        variableStorage.SetValue("$current_day", 1f);

        // Initialize run-specific variables to defaults
        ResetRunSpecificVariables();

        Debug.Log($"Initialized for Run {runNumber}");
    }

    /// <summary>
    /// Get the appropriate start node for a given run number
    /// </summary>
    public static string GetStartNodeForRun(int runNumber)
    {
        switch (runNumber)
        {
            case 1:
                return "R1_Start";
            case 2:
                return "R2_Start";
            case 3:
                return "R3_Start";
            case 4:
                return "R4_Start";
            default:
                return "R1_Start";
        }
    }
}

