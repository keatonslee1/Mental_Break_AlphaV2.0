using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Handles the <<checkpoint>> Yarn command to mark save points in the narrative.
/// When a checkpoint is reached, it enables manual saves and triggers an autosave.
/// </summary>
public class CheckpointCommandHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("SaveLoadManager instance (auto-found if null)")]
    public SaveLoadManager saveLoadManager;

    private void Awake()
    {
        if (saveLoadManager == null)
        {
            saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
            if (saveLoadManager == null && SaveLoadManager.Instance != null)
            {
                saveLoadManager = SaveLoadManager.Instance;
            }
        }
    }

    /// <summary>
    /// Yarn command handler for <<checkpoint "checkpointID">>
    /// Called from Yarn dialogue when a checkpoint is reached.
    /// </summary>
    [YarnCommand("checkpoint")]
    public void Checkpoint(string checkpointID)
    {
        if (saveLoadManager == null)
        {
            saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
            if (saveLoadManager == null && SaveLoadManager.Instance != null)
            {
                saveLoadManager = SaveLoadManager.Instance;
            }
        }

        if (saveLoadManager == null)
        {
            Debug.LogError("CheckpointCommandHandler: SaveLoadManager not found!");
            return;
        }

        // Set the checkpoint in SaveLoadManager
        saveLoadManager.SetCheckpoint(checkpointID);

        Debug.Log($"Checkpoint reached: {checkpointID}");
    }
}

