using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles keyboard input for quick save (F5) and quick load (F9) shortcuts.
/// F5 saves to autosave slot (slot 0), F9 loads from autosave slot (slot 0).
/// </summary>
public class SaveLoadInputHandler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Enable/disable quick save/load shortcuts")]
    public bool enableQuickSaveLoad = true;

    [Tooltip("Enable F5 for quick save")]
    public bool useF5ForQuickSave = true;

    [Tooltip("Enable F9 for quick load")]
    public bool useF9ForQuickLoad = true;

    private SaveLoadManager saveLoadManager;

    private void Awake()
    {
        saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
        if (saveLoadManager == null && SaveLoadManager.Instance != null)
        {
            saveLoadManager = SaveLoadManager.Instance;
        }
    }

    private void Update()
    {
        if (!enableQuickSaveLoad || saveLoadManager == null) return;

        // Check keyboard input (using new Input System)
        if (Keyboard.current == null) return;

        // Check for quick save (F5)
        if (useF5ForQuickSave && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            if (saveLoadManager.QuickSave())
            {
                Debug.Log("Quick save successful!");
                // Could show a brief UI notification here
            }
            else
            {
                Debug.LogWarning("Quick save failed or not allowed!");
            }
        }

        // Check for quick load (F9)
        if (useF9ForQuickLoad && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            if (saveLoadManager.QuickLoad())
            {
                Debug.Log("Quick load successful!");
                // Could show a brief UI notification here
            }
            else
            {
                Debug.LogWarning("Quick load failed - no autosave found!");
            }
        }
    }
}

