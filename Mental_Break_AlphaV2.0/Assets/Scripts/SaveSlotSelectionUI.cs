using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Simple save slot selection UI with 5 fixed buttons (Autosave + Slot 1-4) styled like pause menu
/// </summary>
public class SaveSlotSelectionUI : MonoBehaviour
{
    public enum Mode
    {
        Save,
        Load
    }

    [Header("UI References")]
    [Tooltip("The main save selection panel")]
    public GameObject selectionPanel;

    [Header("Slot Buttons")]
    [Tooltip("Button for Autosave (Slot 0)")]
    public Button slot1Button;

    [Tooltip("Button for Player Slot 1")]
    public Button slot2Button;

    [Tooltip("Button for Player Slot 2")]
    public Button slot3Button;

    [Tooltip("Button for Player Slot 3")]
    public Button slot4Button;

    [Tooltip("Button for Player Slot 4")]
    public Button slot5Button;

    [Tooltip("Cancel/Back button")]
    public Button cancelButton;

    [Header("References")]
    [Tooltip("SaveLoadManager instance (auto-found if null)")]
    public SaveLoadManager saveLoadManager;

    [Tooltip("PauseMenuManager instance (for closing pause menu when loading)")]
    public PauseMenuManager pauseMenuManager;

    private Mode currentMode = Mode.Load;

    private void Awake()
    {
        // Auto-find references
        if (saveLoadManager == null)
        {
            saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
            if (saveLoadManager == null && SaveLoadManager.Instance != null)
            {
                saveLoadManager = SaveLoadManager.Instance;
            }
        }

        if (pauseMenuManager == null)
        {
            pauseMenuManager = FindFirstObjectByType<PauseMenuManager>();
            if (pauseMenuManager == null && PauseMenuManager.Instance != null)
            {
                pauseMenuManager = PauseMenuManager.Instance;
            }
        }
    }

    private void Start()
    {
        // Initialize UI state
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        // Setup button listeners
        // Slot 1 button → Slot 0 (Autosave)
        if (slot1Button != null)
        {
            slot1Button.onClick.AddListener(() => OnSlotSelected(0));
        }

        // Slot 2 button → Slot 1 (Player Slot 1)
        if (slot2Button != null)
        {
            slot2Button.onClick.AddListener(() => OnSlotSelected(1));
        }

        // Slot 3 button → Slot 2 (Player Slot 2)
        if (slot3Button != null)
        {
            slot3Button.onClick.AddListener(() => OnSlotSelected(2));
        }

        // Slot 4 button → Slot 3 (Player Slot 3)
        if (slot4Button != null)
        {
            slot4Button.onClick.AddListener(() => OnSlotSelected(3));
        }

        // Slot 5 button → Slot 4 (Player Slot 4)
        if (slot5Button != null)
        {
            slot5Button.onClick.AddListener(() => OnSlotSelected(4));
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancel);
        }

        // Update button labels initially
        UpdateButtonLabels();
    }

    private void Update()
    {
        // Check for ESC key to close selection UI (using new Input System)
        if (selectionPanel != null && selectionPanel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnCancel();
            }
        }
    }

    /// <summary>
    /// Show the save slot selection UI in save mode
    /// </summary>
    public void ShowSelectionUIForSave()
    {
        currentMode = Mode.Save;
        ShowSelectionUI();
    }

    /// <summary>
    /// Show the save slot selection UI in load mode
    /// </summary>
    public void ShowSelectionUI()
    {
        currentMode = Mode.Load;
        if (selectionPanel != null)
        {
            // Ensure parent is active (in case LoadMenuPanel is a child of PauseMenuPanel)
            Transform parent = selectionPanel.transform.parent;
            if (parent != null && !parent.gameObject.activeSelf)
            {
                parent.gameObject.SetActive(true);
            }
            
            selectionPanel.SetActive(true);
            UpdateButtonLabels();
        }
        else
        {
            Debug.LogError("SaveSlotSelectionUI: selectionPanel is null! Make sure it's assigned in the Inspector.");
        }
    }

    /// <summary>
    /// Hide the save slot selection UI
    /// </summary>
    public void HideSelectionUI()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Update button labels with save metadata
    /// </summary>
    private void UpdateButtonLabels()
    {
        if (saveLoadManager == null)
        {
            Debug.LogWarning("SaveSlotSelectionUI: SaveLoadManager not found, cannot update button labels");
            return;
        }

        // Update each button with slot info
        UpdateButtonLabel(slot1Button, 0, "AUTOSAVE");
        UpdateButtonLabel(slot2Button, 1, "SLOT 1");
        UpdateButtonLabel(slot3Button, 2, "SLOT 2");
        UpdateButtonLabel(slot4Button, 3, "SLOT 3");
        UpdateButtonLabel(slot5Button, 4, "SLOT 4");
    }

    /// <summary>
    /// Update a single button's label with save metadata
    /// </summary>
    private void UpdateButtonLabel(Button button, int slot, string defaultLabel)
    {
        if (button == null) return;

        // Get save data if it exists
        SaveSlotData slotData = saveLoadManager.GetSaveSlotData(slot);

        string buttonText = defaultLabel;
        if (slotData != null)
        {
            // Format: "AUTOSAVE\nR1-D2\n2024-01-15 10:30"
            buttonText = $"{defaultLabel}\nR{slotData.run}-D{slotData.day}";
            
            // Parse and format timestamp
            if (DateTime.TryParse(slotData.timestamp, out DateTime timestamp))
            {
                buttonText += $"\n{timestamp:MM/dd HH:mm}";
            }
        }
        else
        {
            // Empty slot
            buttonText = $"{defaultLabel}\n(Empty)";
        }

        // Update button text
        Transform textChild = button.transform.Find("Text");
        if (textChild != null)
        {
#if USE_TMP
            TextMeshProUGUI tmpText = textChild.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = buttonText;
            }
            else
            {
                Text text = textChild.GetComponent<Text>();
                if (text != null)
                {
                    text.text = buttonText;
                }
            }
#else
            Text text = textChild.GetComponent<Text>();
            if (text != null)
            {
                text.text = buttonText;
            }
#endif
        }
    }

    /// <summary>
    /// Handle slot selection (save or load depending on mode)
    /// </summary>
    public void OnSlotSelected(int slot)
    {
        if (saveLoadManager == null)
        {
            Debug.LogError("SaveSlotSelectionUI: Cannot save/load - SaveLoadManager not found!");
            return;
        }

        bool success = false;

        if (currentMode == Mode.Save)
        {
            // Save to the selected slot
            success = saveLoadManager.SaveGame(slot);
            if (success)
            {
                Debug.Log($"Saved game to slot {slot}");
                // Update button labels to show new save
                UpdateButtonLabels();
                // Hide selection UI
                HideSelectionUI();
                
                // Return to pause menu if it exists
                if (pauseMenuManager != null)
                {
                    pauseMenuManager.ShowPauseMenu();
                }
            }
            else
            {
                Debug.LogWarning($"Failed to save to slot {slot}");
                // Could show an error message to the player here
            }
        }
        else // Load mode
        {
            // Load from the selected slot
            success = saveLoadManager.LoadGame(slot);
            if (success)
            {
                Debug.Log($"Loaded save from slot {slot}");
                
                // Hide selection UI
                HideSelectionUI();

                // Resume game (unpause) after loading
                if (pauseMenuManager != null)
                {
                    pauseMenuManager.ResumeGame();
                }
            }
            else
            {
                Debug.LogWarning($"No save found in slot {slot} or failed to load");
                // Could show an error message to the player here
            }
        }
    }

    /// <summary>
    /// Handle cancel button
    /// </summary>
    public void OnCancel()
    {
        HideSelectionUI();
        
        // Return to pause menu if it exists
        if (pauseMenuManager != null)
        {
            // Show pause menu buttons again
            pauseMenuManager.ShowPauseMenu();
        }
    }
}
