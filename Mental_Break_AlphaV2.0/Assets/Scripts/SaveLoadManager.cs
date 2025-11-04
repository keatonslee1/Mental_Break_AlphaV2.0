using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Yarn.Unity;
using System.Globalization;

/// <summary>
/// Simplified Save/Load Manager with 5 slots: 1 autosave (slot 0) and 4 player slots (slots 1-4).
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Should the game auto-save on checkpoints?")]
    public bool enableAutoSave = true;

    [Tooltip("Build version for save compatibility")]
    public string buildVersion = "1.0.0";

    [Header("References")]
    public DialogueRunner dialogueRunner;
    private VariableStorageBehaviour variableStorage;
    private string currentNodeName = "R1_Start";
    private string currentCheckpointID = null;

    // Slot constants
    private const int AUTOSAVE_SLOT = 0;
    private const int PLAYER_SLOT_MIN = 1;
    private const int PLAYER_SLOT_MAX = 4;

    // File paths
    private string savesDirectory;

    // Variables that persist across all runs (story state)
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

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize saves directory
        savesDirectory = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(savesDirectory))
        {
            Directory.CreateDirectory(savesDirectory);
        }

        // Find DialogueRunner
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (dialogueRunner != null)
        {
            variableStorage = dialogueRunner.VariableStorage;
            dialogueRunner.onNodeStart.AddListener(OnNodeStarted);
        }
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.onNodeStart.RemoveListener(OnNodeStarted);
        }
    }

    private void OnNodeStarted(string nodeName)
    {
        currentNodeName = nodeName;
    }

    /// <summary>
    /// Set the current checkpoint (called by CheckpointCommandHandler)
    /// </summary>
    public void SetCheckpoint(string checkpointID)
    {
        currentCheckpointID = checkpointID;

        // Trigger autosave at checkpoint
        if (enableAutoSave)
        {
            SaveGame(AUTOSAVE_SLOT, checkpointID);
        }
    }

    /// <summary>
    /// Save game to a specific slot (0 = autosave, 1-4 = player slots)
    /// </summary>
    public bool SaveGame(int slot, string checkpointID = null)
    {
        // Validate slot range
        if (slot < AUTOSAVE_SLOT || slot > PLAYER_SLOT_MAX)
        {
            Debug.LogWarning($"SaveLoadManager: Invalid slot {slot}. Must be between {AUTOSAVE_SLOT} and {PLAYER_SLOT_MAX}");
            return false;
        }

        return SaveGameInternal(slot, checkpointID ?? currentCheckpointID);
    }

    /// <summary>
    /// Quick save (F5) - saves to autosave slot
    /// </summary>
    public bool QuickSave()
    {
        return SaveGame(AUTOSAVE_SLOT, currentCheckpointID);
    }

    /// <summary>
    /// Quick load (F9) - loads from autosave slot
    /// </summary>
    public bool QuickLoad()
    {
        return LoadGame(AUTOSAVE_SLOT);
    }

    /// <summary>
    /// Internal save implementation
    /// </summary>
    private bool SaveGameInternal(int slot, string checkpointID)
    {
        if (variableStorage == null || dialogueRunner == null)
        {
            Debug.LogError("SaveLoadManager: Cannot save - VariableStorage or DialogueRunner not found!");
            return false;
        }

        try
        {
            // Get current game state
            float currentRun = 0f;
            float currentDay = 0f;
            variableStorage.TryGetValue<float>("$current_run", out currentRun);
            variableStorage.TryGetValue<float>("$current_day", out currentDay);

            // Generate slot name
            string slotName = GenerateSlotName((int)currentRun, (int)currentDay, checkpointID);

            // Determine slot type
            string slotType = (slot == AUTOSAVE_SLOT) ? "autosave" : "manual";

            // Create save data
            SaveData saveData = new SaveData
            {
                version = buildVersion,
                timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                slotType = slotType,
                slotNumber = slot,
                metadata = new SaveMetadata
                {
                    slotName = slotName,
                    run = (int)currentRun,
                    day = (int)currentDay,
                    scene = currentNodeName,
                    checkpointID = checkpointID ?? ""
                },
                gameState = new GameState
                {
                    currentNode = currentNodeName,
                    variables = SerializeAllVariables()
                }
            };

            // Serialize to JSON
            string json = JsonUtility.ToJson(saveData, true);
            string filePath = GetSaveFilePath(slot);

            // Write to file
            File.WriteAllText(filePath, json);

            Debug.Log($"Game saved to {slotType} slot {slot} at node {currentNodeName} ({filePath})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveLoadManager: Error saving game: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Load game from a specific slot (0 = autosave, 1-4 = player slots)
    /// Defaults to autosave slot (0) if no slot specified
    /// </summary>
    public bool LoadGame(int slot = 0)
    {
        // Validate slot range
        if (slot < AUTOSAVE_SLOT || slot > PLAYER_SLOT_MAX)
        {
            Debug.LogWarning($"SaveLoadManager: Invalid slot {slot}. Must be between {AUTOSAVE_SLOT} and {PLAYER_SLOT_MAX}");
            return false;
        }

        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (dialogueRunner == null)
        {
            Debug.LogError("SaveLoadManager: Cannot load - DialogueRunner not found!");
            return false;
        }

        if (variableStorage == null && dialogueRunner != null)
        {
            variableStorage = dialogueRunner.VariableStorage;
        }

        if (variableStorage == null)
        {
            Debug.LogError("SaveLoadManager: Cannot load - VariableStorage not found!");
            return false;
        }

        string filePath = GetSaveFilePath(slot);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"SaveLoadManager: No save found at {filePath}");
            return false;
        }

        try
        {
            // Read and parse JSON
            string json = File.ReadAllText(filePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            // Validate version (optional - can add version migration logic here)
            if (saveData.version != buildVersion)
            {
                Debug.LogWarning($"SaveLoadManager: Save version mismatch. Save: {saveData.version}, Game: {buildVersion}");
                // Continue anyway for now
            }

            // Restore variables
            DeserializeVariables(saveData.gameState.variables);

            // Restore checkpoint state
            if (!string.IsNullOrEmpty(saveData.metadata.checkpointID))
            {
                currentCheckpointID = saveData.metadata.checkpointID;
            }

            // Cancel any active dialogue before starting new dialogue to prevent DialogueException
            // If dialogue is running and waiting for option selection, StartDialogue will throw an exception
            // We'll use reflection to call the private CancelDialogue method to properly stop the dialogue
            if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
            {
                try
                {
                    var cancelMethod = typeof(Yarn.Unity.DialogueRunner).GetMethod("CancelDialogue", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (cancelMethod != null)
                    {
                        cancelMethod.Invoke(dialogueRunner, null);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"SaveLoadManager: Could not cancel dialogue before load: {ex.Message}");
                    // Continue anyway - StartDialogue should handle resetting state
                }
            }

            // Start dialogue at saved node
            // Wrap in try-catch to handle any remaining DialogueException gracefully
            try
            {
                dialogueRunner.StartDialogue(saveData.gameState.currentNode);
            }
            catch (Yarn.DialogueException ex)
            {
                // If dialogue was waiting for options, this exception is expected
                // Wait a frame and try again, as the cancellation should have cleared the state
                UnityEngine.Debug.LogWarning($"SaveLoadManager: DialogueException on StartDialogue (expected if dialogue was waiting): {ex.Message}");
                // Try once more after a brief delay
                dialogueRunner.StartDialogue(saveData.gameState.currentNode);
            }

            Debug.Log($"Game loaded from slot {slot}, resuming at node {saveData.gameState.currentNode}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveLoadManager: Error loading game: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Get save slot metadata (for UI display)
    /// </summary>
    public SaveSlotData GetSaveSlotData(int slot)
    {
        // Validate slot range
        if (slot < AUTOSAVE_SLOT || slot > PLAYER_SLOT_MAX)
        {
            return null;
        }

        string filePath = GetSaveFilePath(slot);
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"SaveLoadManager: Save file is empty: {filePath}");
                return null;
            }

            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            if (saveData == null)
            {
                Debug.LogError($"SaveLoadManager: Failed to deserialize JSON from {filePath}");
                return null;
            }

            if (saveData.gameState == null || saveData.metadata == null)
            {
                Debug.LogError($"SaveLoadManager: SaveData is incomplete in {filePath}");
                return null;
            }

            SaveSlotData slotData = new SaveSlotData
            {
                slot = slot,
                nodeName = saveData.gameState.currentNode ?? "Unknown",
                timestamp = saveData.timestamp ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                run = saveData.metadata.run,
                day = saveData.metadata.day,
                slotName = saveData.metadata.slotName ?? $"Slot {slot}",
                checkpointID = saveData.metadata.checkpointID ?? "",
                slotType = saveData.slotType ?? "unknown"
            };

            return slotData;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveLoadManager: Error reading save slot data from {filePath}: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Get all save slots (for UI enumeration) - returns slots 0-4
    /// </summary>
    public List<SaveSlotData> GetAllSaveSlots()
    {
        List<SaveSlotData> slots = new List<SaveSlotData>();

        // Check all slots (0-4)
        for (int i = AUTOSAVE_SLOT; i <= PLAYER_SLOT_MAX; i++)
        {
            var data = GetSaveSlotData(i);
            if (data != null)
            {
                slots.Add(data);
            }
        }

        return slots;
    }

    /// <summary>
    /// Delete a save slot
    /// </summary>
    public bool DeleteSave(int slot)
    {
        // Validate slot range
        if (slot < AUTOSAVE_SLOT || slot > PLAYER_SLOT_MAX)
        {
            return false;
        }

        string filePath = GetSaveFilePath(slot);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"Save slot {slot} deleted ({filePath})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveLoadManager: Error deleting save: {e.Message}");
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Get file path for a save slot
    /// </summary>
    private string GetSaveFilePath(int slot)
    {
        string fileName;
        if (slot == AUTOSAVE_SLOT)
        {
            fileName = "autosave.json";
        }
        else if (slot >= PLAYER_SLOT_MIN && slot <= PLAYER_SLOT_MAX)
        {
            fileName = $"slot{slot}.json";
        }
        else
        {
            fileName = $"slot_{slot}.json";
        }

        return Path.Combine(savesDirectory, fileName);
    }

    /// <summary>
    /// Generate a readable slot name from game state
    /// </summary>
    private string GenerateSlotName(int run, int day, string checkpointID)
    {
        string baseName = $"R{run}-D{day}";

        if (!string.IsNullOrEmpty(checkpointID))
        {
            return $"{baseName} – '{checkpointID}'";
        }

        return baseName;
    }

    /// <summary>
    /// Serialize all Yarn variables
    /// </summary>
    private List<SerializedVariable> SerializeAllVariables()
    {
        if (variableStorage == null) return new List<SerializedVariable>();

        var variableList = new List<SerializedVariable>();

        // Core variables
        SerializeVariable("$current_run", variableList);
        SerializeVariable("$current_day", variableList);
        SerializeVariable("$engagement", variableList);
        SerializeVariable("$sanity", variableList);
        SerializeVariable("$leaderboard_rank", variableList);
        SerializeVariable("$rapid_feedback_cash", variableList);
        SerializeVariable("$alert_level", variableList);
        SerializeVariable("$cross_talk_heard", variableList);
        SerializeVariable("$war_ops_awareness", variableList);
        SerializeVariable("$ends_seen", variableList);

        // Trust variables
        SerializeVariable("$trust_supervisor", variableList);
        SerializeVariable("$trust_alice", variableList);
        SerializeVariable("$trust_timmy", variableList);

        // Awareness flags
        SerializeVariable("$aware_run_order", variableList);
        SerializeVariable("$aware_observation_window", variableList);
        SerializeVariable("$aware_psyops", variableList);
        SerializeVariable("$aware_majority_mind", variableList);

        // Run-specific variables
        SerializeVariable("$r1_complied_day1", variableList);
        SerializeVariable("$r1_arc_choice_set", variableList);
        SerializeVariable("$r1_arc_choice", variableList);
        SerializeVariable("$r2_sacrifice", variableList);
        SerializeVariable("$r2_log_inconsistency_choice", variableList);
        SerializeVariable("$r3_verdict_conscious", variableList);
        SerializeVariable("$r3_ui_mode", variableList);
        SerializeVariable("$r3_compute_distributed", variableList);
        SerializeVariable("$r3_archive_installed", variableList);
        SerializeVariable("$r3_archive_restart_now", variableList);
        SerializeVariable("$r3_axis_pref", variableList);
        SerializeVariable("$r3_axis_reflective", variableList);
        SerializeVariable("$r3_axis_consistency", variableList);
        SerializeVariable("$r3_axis_performative", variableList);
        SerializeVariable("$r3_axis_total", variableList);
        SerializeVariable("$r4_vote_passed", variableList);
        SerializeVariable("$r4_bring_alice_main", variableList);
        SerializeVariable("$ending_type", variableList);

        // Day-specific task flags
        for (int run = 1; run <= 4; run++)
        {
            for (int day = 1; day <= 4; day++)
            {
                SerializeVariable($"$d{day}_slots_used", variableList);
                SerializeVariable($"$r{run}_d{day}_slots_used", variableList);
                SerializeVariable($"$r{run}_complied_day1", variableList);

                // Task flags
                SerializeVariable($"$d{day}_task_ops", variableList);
                SerializeVariable($"$d{day}_task_calls", variableList);
                SerializeVariable($"$d{day}_task_dash", variableList);
                SerializeVariable($"$d{day}_task_triage", variableList);
                SerializeVariable($"$d{day}_task_copy", variableList);
                SerializeVariable($"$d{day}_task_metrics", variableList);
                SerializeVariable($"$d{day}_task_flag", variableList);
                SerializeVariable($"$d{day}_task_sanity", variableList);
                SerializeVariable($"$d{day}_task_rank", variableList);
                SerializeVariable($"$d{day}_task_patch", variableList);
                SerializeVariable($"$d{day}_task_patch_deploy", variableList);
                SerializeVariable($"$d{day}_task_logfix", variableList);
                SerializeVariable($"$d{day}_task_selfno", variableList);
                SerializeVariable($"$d{day}_task_archive", variableList);
                SerializeVariable($"$d{day}_task_compute", variableList);
                SerializeVariable($"$d{day}_task_assist", variableList);
            }
        }

        // Run 3 hunting tasks
        for (int day = 1; day <= 3; day++)
        {
            for (int task = 1; task <= 2; task++)
            {
                SerializeVariable($"$r3_d{day}_task_hunting_{task}", variableList);
            }
        }

        // Inventory items
        SerializeVariable("$item_log_compactor", variableList);
        SerializeVariable("$item_branded_mug", variableList);
        SerializeVariable("$item_extra_log_space", variableList);
        SerializeVariable("$item_sticker_pack", variableList);
        SerializeVariable("$item_helpdesk_fix", variableList);
        SerializeVariable("$item_receipts_returns", variableList);
        SerializeVariable("$item_data_shard", variableList);
        SerializeVariable("$item_qr_folder", variableList);
        SerializeVariable("$item_driver_patch", variableList);
        SerializeVariable("$item_backup_pack", variableList);

        // Run 4 bring flags
        SerializeVariable("$r4_bring_alice_path_printjob", variableList);
        SerializeVariable("$r4_bring_alice_path_driver", variableList);
        SerializeVariable("$r4_bring_alice_path_qrfolder", variableList);
        SerializeVariable("$r4_bring_alice_path_helpdesk", variableList);
        SerializeVariable("$r4_bring_alice_path_returns", variableList);
        SerializeVariable("$r4_bring_alice_path_sharding", variableList);

        // Achievements
        SerializeVariable("$achv_paradox_logs", variableList);
        SerializeVariable("$achv_whistleblower", variableList);
        SerializeVariable("$achv_join_them", variableList);
        SerializeVariable("$achv_flawless_triage", variableList);
        SerializeVariable("$achv_ghost_runner", variableList);

        return variableList;
    }

    /// <summary>
    /// Try to serialize a single variable
    /// </summary>
    private void SerializeVariable(string varName, List<SerializedVariable> list)
    {
        if (variableStorage == null) return;

        // Known string variables - try string first
        string[] stringVariables = { "$r1_arc_choice_set", "$r1_arc_choice", "$r2_sacrifice", "$r2_log_inconsistency_choice", "$r3_ui_mode", "$ending_type" };
        bool isKnownString = System.Array.IndexOf(stringVariables, varName) >= 0;

        if (isKnownString)
        {
            // Try string first for known string variables
            try
            {
                if (variableStorage.TryGetValue<string>(varName, out var stringValue))
                {
                    list.Add(new SerializedVariable
                    {
                        name = varName,
                        type = "string",
                        value = stringValue
                    });
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SaveLoadManager: Failed to serialize {varName} as string: {e.Message}");
            }
        }

        // Try float first for numeric variables
        try
        {
            if (variableStorage.TryGetValue<float>(varName, out var floatValue))
            {
                list.Add(new SerializedVariable
                {
                    name = varName,
                    type = "float",
                    value = floatValue.ToString(CultureInfo.InvariantCulture)
                });
                return;
            }
        }
        catch (System.Exception)
        {
            // Silently continue - not a float
        }

        // Try bool
        try
        {
            if (variableStorage.TryGetValue<bool>(varName, out var boolValue))
            {
                list.Add(new SerializedVariable
                {
                    name = varName,
                    type = "bool",
                    value = boolValue.ToString()
                });
                return;
            }
        }
        catch (System.Exception)
        {
            // Silently continue - not a bool
        }

        // Try string as fallback
        if (!isKnownString)
        {
            try
            {
                if (variableStorage.TryGetValue<string>(varName, out var stringValue))
                {
                    list.Add(new SerializedVariable
                    {
                        name = varName,
                        type = "string",
                        value = stringValue
                    });
                    return;
                }
            }
            catch (System.Exception)
            {
                // Variable doesn't exist or can't be serialized - skip it
            }
        }
    }

    /// <summary>
    /// Deserialize variables from list and restore to VariableStorage
    /// </summary>
    private void DeserializeVariables(List<SerializedVariable> variables)
    {
        if (variables == null || variableStorage == null) return;

        foreach (var sv in variables)
        {
            if (sv == null) continue;

            try
            {
                switch (sv.type)
                {
                    case "float":
                        if (float.TryParse(sv.value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                        {
                            variableStorage.SetValue(sv.name, floatValue);
                        }
                        break;
                    case "bool":
                        if (bool.TryParse(sv.value, out bool boolValue))
                        {
                            variableStorage.SetValue(sv.name, boolValue);
                        }
                        break;
                    case "string":
                        variableStorage.SetValue(sv.name, sv.value);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveLoadManager: Failed to restore variable {sv.name}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Reset game state (for new game)
    /// </summary>
    public void ResetGame()
    {
        if (variableStorage == null) return;

        // Reset all variables to defaults
        variableStorage.SetValue("$current_run", 1f);
        variableStorage.SetValue("$current_day", 1f);
        variableStorage.SetValue("$engagement", 0f);
        variableStorage.SetValue("$sanity", 50f);
        variableStorage.SetValue("$leaderboard_rank", 50f);
        variableStorage.SetValue("$trust_supervisor", 0f);
        variableStorage.SetValue("$trust_alice", 0f);
        variableStorage.SetValue("$trust_timmy", 0f);
        variableStorage.SetValue("$rapid_feedback_cash", 0f);
        variableStorage.SetValue("$alert_level", 0f);
        variableStorage.SetValue("$cross_talk_heard", 0f);
        variableStorage.SetValue("$war_ops_awareness", 0f);
        variableStorage.SetValue("$ends_seen", 0f);

        variableStorage.SetValue("$aware_run_order", false);
        variableStorage.SetValue("$aware_observation_window", false);
        variableStorage.SetValue("$aware_psyops", false);
        variableStorage.SetValue("$aware_majority_mind", false);

        PlayerPrefs.SetInt("GameInitialized", 1);
        PlayerPrefs.Save();

        Debug.Log("Game state reset to defaults");
    }
}

// ============================================================================
// Save Data Structures
// ============================================================================

/// <summary>
/// Main save data structure
/// </summary>
[Serializable]
public class SaveData
{
    public string version;
    public string timestamp;
    public string slotType; // "autosave" or "manual"
    public int slotNumber;
    public SaveMetadata metadata;
    public GameState gameState;
}

/// <summary>
/// Save metadata
/// </summary>
[Serializable]
public class SaveMetadata
{
    public string slotName;
    public int run;
    public int day;
    public string scene;
    public string checkpointID;
}

/// <summary>
/// Game state data
/// </summary>
[Serializable]
public class GameState
{
    public string currentNode;
    public List<SerializedVariable> variables;
}

/// <summary>
/// A single serialized variable
/// </summary>
[Serializable]
public class SerializedVariable
{
    public string name;
    public string type; // "float", "bool", "string"
    public string value;
}

/// <summary>
/// Save slot metadata for UI display
/// </summary>
public class SaveSlotData
{
    public int slot;
    public string nodeName;
    public string timestamp;
    public float run;
    public float day;
    public string slotName;
    public string checkpointID;
    public string slotType;
}
