using System;
using System.Collections;
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
    private CanvasGroup selectionPanelCanvasGroup;

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

        if (selectionPanel != null)
        {
            selectionPanelCanvasGroup = selectionPanel.GetComponent<CanvasGroup>();
            if (selectionPanelCanvasGroup == null)
            {
                selectionPanelCanvasGroup = selectionPanel.AddComponent<CanvasGroup>();
                selectionPanelCanvasGroup.alpha = 0f;
                selectionPanelCanvasGroup.interactable = false;
                selectionPanelCanvasGroup.blocksRaycasts = false;
                Debug.Log("SaveSlotSelectionUI: Added CanvasGroup to selectionPanel for visibility control.");
            }
        }

        EnsureSelectionPanelParent();
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
    public void ShowSelectionUIForLoad()
    {
        currentMode = Mode.Load;
        ShowSelectionUI();
    }

    /// <summary>
    /// Show the save slot selection UI (mode should be set by caller)
    /// </summary>
    public void ShowSelectionUI()
    {
        if (selectionPanel != null)
        {
            // CRITICAL: Verify selectionPanel reference is correct
            Debug.Log($"SaveSlotSelectionUI: ShowSelectionUI() - selectionPanel reference: name='{selectionPanel.name}', instanceID={selectionPanel.GetInstanceID()}, activeSelf={selectionPanel.activeSelf}, activeInHierarchy={selectionPanel.activeInHierarchy}");
            
            // CRITICAL FIX: Activate entire parent chain FIRST (before activating panel)
            // This ensures LoadMenuPanel becomes active in hierarchy even if PauseMenuPanel was inactive
            // Hierarchy: DontDestroyOnLoad/DialogueSystem/Canvas/PauseMenuPanel/LoadMenuPanel
            Transform current = selectionPanel.transform.parent; // Start with parent, not LoadMenuPanel itself
            while (current != null)
            {
                bool initiallyActive = current.gameObject.activeSelf;
                Debug.Log($"SaveSlotSelectionUI: Inspecting parent {current.name} (activeSelf={initiallyActive}, activeInHierarchy={current.gameObject.activeInHierarchy})");

                if (!initiallyActive)
                {
                    current.gameObject.SetActive(true);
                    Debug.Log($"SaveSlotSelectionUI: Activated parent {current.name} to enable LoadMenuPanel");
                }
                
                // CRITICAL: If parent is PauseMenuPanel, disable its Image component to prevent it covering LoadMenuPanel
                if (current.name == "PauseMenuPanel")
                {
                    Image pausePanelImage = current.GetComponent<Image>();
                    if (pausePanelImage != null)
                    {
                        Debug.Log($"SaveSlotSelectionUI: PauseMenuPanel Image state before disable -> enabled={pausePanelImage.enabled}, color={pausePanelImage.color}, raycastTarget={pausePanelImage.raycastTarget}");
                        if (pausePanelImage.enabled)
                        {
                            pausePanelImage.enabled = false;
                            Debug.LogWarning("SaveSlotSelectionUI: Disabled PauseMenuPanel Image to prevent covering LoadMenuPanel");
                        }
                    }
                    else
                    {
                        Debug.Log("SaveSlotSelectionUI: PauseMenuPanel does not have an Image component.");
                    }
                }
                
                current = current.parent;
            }

            // Set CanvasGroup alpha BEFORE activating panel to ensure proper rendering
            if (selectionPanelCanvasGroup != null)
            {
                selectionPanelCanvasGroup.alpha = 1f;
                selectionPanelCanvasGroup.interactable = true;
                selectionPanelCanvasGroup.blocksRaycasts = true;
                Debug.Log($"SaveSlotSelectionUI: Enabled CanvasGroup (alpha={selectionPanelCanvasGroup.alpha}, interactable={selectionPanelCanvasGroup.interactable}).");
            }

            // NOW activate the panel using multiple methods to ensure it works
            // Method 1: Direct GameObject.SetActive
            selectionPanel.SetActive(true);
            bool activeSelf1 = selectionPanel.activeSelf;
            bool activeInHierarchy1 = selectionPanel.activeInHierarchy;
            Debug.Log($"SaveSlotSelectionUI: Method 1 (GameObject.SetActive) - activeSelf={activeSelf1}, activeInHierarchy={activeInHierarchy1}");
            
            // If Method 1 failed, try Method 2: Via Transform
            if (!activeSelf1)
            {
                Debug.LogWarning($"SaveSlotSelectionUI: Method 1 failed! Trying Method 2 (Transform.gameObject.SetActive)");
                selectionPanel.transform.gameObject.SetActive(true);
                bool activeSelf2 = selectionPanel.activeSelf;
                bool activeInHierarchy2 = selectionPanel.activeInHierarchy;
                Debug.Log($"SaveSlotSelectionUI: Method 2 (Transform.gameObject.SetActive) - activeSelf={activeSelf2}, activeInHierarchy={activeInHierarchy2}");
                
                // If Method 2 failed, try Method 3: Direct reference
                if (!activeSelf2)
                {
                    Debug.LogError($"SaveSlotSelectionUI: Method 2 also failed! Trying Method 3 (direct reference)");
                    GameObject panelObj = selectionPanel;
                    if (panelObj != null)
                    {
                        panelObj.SetActive(true);
                        bool activeSelf3 = panelObj.activeSelf;
                        bool activeInHierarchy3 = panelObj.activeInHierarchy;
                        Debug.Log($"SaveSlotSelectionUI: Method 3 (direct reference) - activeSelf={activeSelf3}, activeInHierarchy={activeInHierarchy3}");
                        
                        if (!activeSelf3)
                        {
                            Debug.LogError($"SaveSlotSelectionUI: ALL activation methods failed! selectionPanel.name='{selectionPanel.name}', instanceID={selectionPanel.GetInstanceID()}");
                        }
                    }
                }
            }
            
            // Final verification
            bool finalActiveSelf = selectionPanel.activeSelf;
            bool finalActiveInHierarchy = selectionPanel.activeInHierarchy;
            Debug.Log($"SaveSlotSelectionUI: FINAL STATE - activeSelf={finalActiveSelf}, activeInHierarchy={finalActiveInHierarchy}");
            
            if (!finalActiveInHierarchy && finalActiveSelf)
            {
                Debug.LogError($"SaveSlotSelectionUI: LoadMenuPanel.activeInHierarchy is FALSE even though activeSelf is TRUE! Parent chain may not be fully active.");
                // Verify parent chain again
                Transform parentCheck = selectionPanel.transform.parent;
                while (parentCheck != null)
                {
                    Debug.Log($"SaveSlotSelectionUI: Parent {parentCheck.name} - activeSelf={parentCheck.gameObject.activeSelf}, activeInHierarchy={parentCheck.gameObject.activeInHierarchy}");
                    if (!parentCheck.gameObject.activeSelf)
                    {
                        Debug.LogWarning($"SaveSlotSelectionUI: Reactivating parent {parentCheck.name}");
                        parentCheck.gameObject.SetActive(true);
                    }
                    parentCheck = parentCheck.parent;
                }
                // Try activating again after fixing parent chain
                selectionPanel.SetActive(true);
                Debug.Log($"SaveSlotSelectionUI: After parent chain fix - activeSelf={selectionPanel.activeSelf}, activeInHierarchy={selectionPanel.activeInHierarchy}");
            }
            
            // Start coroutine as fallback if still not active
            if (!selectionPanel.activeSelf)
            {
                Debug.LogError($"SaveSlotSelectionUI: LoadMenuPanel still inactive after all attempts! Starting delayed activation coroutine.");
                StartCoroutine(DelayedActivationFallback());
            }
            
            // Handle prefab overrides and Editor refresh (Editor-only)
#if UNITY_EDITOR
            try
            {
                // Check if LoadMenuPanel is a prefab instance
                UnityEditor.PrefabAssetType prefabType = UnityEditor.PrefabUtility.GetPrefabAssetType(selectionPanel);
                UnityEditor.PrefabInstanceStatus prefabStatus = UnityEditor.PrefabUtility.GetPrefabInstanceStatus(selectionPanel);
                
                Debug.Log($"SaveSlotSelectionUI: Prefab status - assetType={prefabType}, instanceStatus={prefabStatus}");
                
                // If it's a prefab instance, we need to record the property modification
                if (prefabStatus == UnityEditor.PrefabInstanceStatus.Connected || 
                    prefabStatus == UnityEditor.PrefabInstanceStatus.MissingAsset)
                {
                    Debug.Log($"SaveSlotSelectionUI: LoadMenuPanel is a prefab instance. Recording property modification to save active state override.");
                    
                    // Record the active state modification so it persists
                    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(selectionPanel);
                    
                    // Mark as dirty to force Editor refresh
                    UnityEditor.EditorUtility.SetDirty(selectionPanel);
                    
                    Debug.Log($"SaveSlotSelectionUI: Recorded prefab modification and marked as dirty. Inspector should now show active state.");
                }
                else if (prefabType == UnityEditor.PrefabAssetType.Regular || 
                         prefabType == UnityEditor.PrefabAssetType.Variant)
                {
                    Debug.LogWarning($"SaveSlotSelectionUI: LoadMenuPanel appears to be a prefab asset, not an instance! This might be why activation isn't working.");
                    
                    // Try to find the scene instance instead
                    GameObject sceneInstance = GameObject.Find("LoadMenuPanel");
                    if (sceneInstance != null && sceneInstance != selectionPanel)
                    {
                        Debug.LogWarning($"SaveSlotSelectionUI: Found different LoadMenuPanel instance in scene! Activating scene instance instead.");
                        sceneInstance.SetActive(true);
                        UnityEditor.EditorUtility.SetDirty(sceneInstance);
                    }
                }
                else
                {
                    // Not a prefab, just mark as dirty for refresh
                    UnityEditor.EditorUtility.SetDirty(selectionPanel);
                    Debug.Log($"SaveSlotSelectionUI: LoadMenuPanel is not a prefab instance. Marked as dirty for Editor refresh.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"SaveSlotSelectionUI: Could not handle prefab override (Editor-only code failed): {ex.Message}");
            }
#endif
            
            // CRITICAL: Verify reference is correct and activate scene instance as fallback
            // The selectionPanel reference might be pointing to a prefab or wrong instance
            GameObject actualSceneInstance = GameObject.Find("LoadMenuPanel");
            if (actualSceneInstance != null)
            {
                int referenceInstanceID = selectionPanel != null ? selectionPanel.GetInstanceID() : -1;
                int actualSceneInstanceID = actualSceneInstance.GetInstanceID();
                
                Debug.Log($"SaveSlotSelectionUI: Reference verification - selectionPanel instanceID={referenceInstanceID}, actualSceneInstance instanceID={actualSceneInstanceID}");
                
                if (referenceInstanceID != actualSceneInstanceID)
                {
                    Debug.LogError($"SaveSlotSelectionUI: INSTANCE MISMATCH! selectionPanel reference (ID={referenceInstanceID}) is NOT the scene instance (ID={actualSceneInstanceID})! This is why activation isn't working!");
                    Debug.LogError($"SaveSlotSelectionUI: selectionPanel name='{selectionPanel?.name}', actualSceneInstance name='{actualSceneInstance.name}'");
                    
                    // Activate the ACTUAL scene instance
                    Debug.LogWarning($"SaveSlotSelectionUI: Activating scene instance instead of reference!");
                    actualSceneInstance.SetActive(true);
                    
                    // Also update the reference to point to the correct instance
                    selectionPanel = actualSceneInstance;
                    Debug.LogWarning($"SaveSlotSelectionUI: Updated selectionPanel reference to point to scene instance!");
                }
                else
                {
                    Debug.Log($"SaveSlotSelectionUI: Reference verification passed - selectionPanel matches scene instance.");
                    // Even if they match, activate the scene instance as well to be absolutely sure
                    actualSceneInstance.SetActive(true);
                    Debug.Log($"SaveSlotSelectionUI: Also activated scene instance directly to ensure it's active.");
                }
                
                // Final verification after activating scene instance
                bool sceneActive = actualSceneInstance.activeSelf;
                bool sceneInHierarchy = actualSceneInstance.activeInHierarchy;
                Debug.Log($"SaveSlotSelectionUI: Scene instance final state - activeSelf={sceneActive}, activeInHierarchy={sceneInHierarchy}");
                
                if (!sceneActive)
                {
                    Debug.LogError($"SaveSlotSelectionUI: Scene instance still inactive after all attempts! This is a critical failure.");
                }
            }
            else
            {
                Debug.LogError($"SaveSlotSelectionUI: Could not find 'LoadMenuPanel' in scene using GameObject.Find! This means the GameObject doesn't exist or has a different name.");
            }
            
            // Ensure RectTransform is configured correctly (center-anchored, fixed size)
            RectTransform rectTransform = selectionPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Set to middle-center anchor if not already set
                Vector2 anchorMin = rectTransform.anchorMin;
                Vector2 anchorMax = rectTransform.anchorMax;
                
                // Check if anchors are set to full-screen stretch (0,0) to (1,1)
                // Also check for near-zero/near-one values (floating point comparison)
                bool isFullScreenStretch = (anchorMin.x < 0.01f && anchorMin.y < 0.01f && 
                                          anchorMax.x > 0.99f && anchorMax.y > 0.99f);
                
                if (isFullScreenStretch)
                {
                    Debug.LogWarning($"SaveSlotSelectionUI: LoadMenuPanel has full-screen stretch anchors ({anchorMin} to {anchorMax}). Fixing to center-anchored.");
                    // Fix anchors to center
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(250f, 350f);
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }
            
            // CRITICAL: Ensure LoadMenuPanel renders on top by moving it to end of sibling list
            // Unity UI renders children in sibling order - later siblings render on top
            Transform parent = selectionPanel.transform.parent;
            if (parent != null)
            {
                selectionPanel.transform.SetAsLastSibling();
                Debug.Log($"SaveSlotSelectionUI: Moved LoadMenuPanel to sibling index {selectionPanel.transform.GetSiblingIndex()} (last) to ensure it renders on top");
            }
            
            // CRITICAL FIX: Disable or make transparent the Image component on LoadMenuPanel
            // The black Image is covering the buttons, making it appear as a black square
            Image panelImage = selectionPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                // Option 1: Disable the Image (recommended - buttons provide their own backgrounds)
                panelImage.enabled = false;
                Debug.Log("SaveSlotSelectionUI: Disabled LoadMenuPanel Image component to prevent covering buttons");
                
                // Alternative: Make it fully transparent instead of disabling
                // panelImage.color = new Color(0, 0, 0, 0);
                // panelImage.raycastTarget = false;
            }
            
            // DIAGNOSTIC: Log state AFTER activation
            if (rectTransform != null)
            {
                LogPanelDiagnostics(rectTransform);
            }
            
            // Force layout rebuild to ensure proper sizing and button layout
            if (rectTransform != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                
                // Also rebuild all children
                foreach (Transform child in selectionPanel.transform)
                {
                    RectTransform childRect = child.GetComponent<RectTransform>();
                    if (childRect != null)
                    {
                        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(childRect);
                    }
                }
            }
            
            // Update button labels
            UpdateButtonLabels();
            
            // DIAGNOSTIC: Verify buttons are visible
            VerifyButtonsVisible();
        }
        else
        {
            Debug.LogError("SaveSlotSelectionUI: selectionPanel is null! Make sure it's assigned in the Inspector.");
        }
    }

    /// <summary>
    /// Diagnostic: Log panel configuration
    /// </summary>
    private void LogPanelDiagnostics(RectTransform rectTransform)
    {
        Debug.Log($"=== LoadMenuPanel Diagnostics ===");
        Debug.Log($"Active: {selectionPanel.activeSelf}, ActiveInHierarchy: {selectionPanel.activeInHierarchy}");
        Debug.Log($"RectTransform - Anchor: ({rectTransform.anchorMin.x:F2}, {rectTransform.anchorMin.y:F2}) to ({rectTransform.anchorMax.x:F2}, {rectTransform.anchorMax.y:F2})");
        Debug.Log($"RectTransform - Size: {rectTransform.sizeDelta}, Pos: {rectTransform.anchoredPosition}");
        Debug.Log($"RectTransform - Rect: {rectTransform.rect}");
        
        Image image = selectionPanel.GetComponent<Image>();
        if (image != null)
        {
            Debug.Log($"Image - Color: {image.color}, Alpha: {image.color.a}, Enabled: {image.enabled}");
        }
        
        VerticalLayoutGroup layout = selectionPanel.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            Debug.Log($"LayoutGroup - Enabled: {layout.enabled}, Spacing: {layout.spacing}, Child Control Width: {layout.childControlWidth}");
        }
        
        Debug.Log($"Child Count: {selectionPanel.transform.childCount}");
        foreach (Transform child in selectionPanel.transform)
        {
            Debug.Log($"  Child: {child.name} - Active: {child.gameObject.activeSelf}, ActiveInHierarchy: {child.gameObject.activeInHierarchy}");
            RectTransform childRect = child.GetComponent<RectTransform>();
            if (childRect != null)
            {
                Debug.Log($"    Rect: {childRect.rect}, Size: {childRect.sizeDelta}");
            }
            Image childImage = child.GetComponent<Image>();
            if (childImage != null)
            {
                Debug.Log($"    Image Color: {childImage.color}, Alpha: {childImage.color.a}");
            }
        }
    }

    /// <summary>
    /// Diagnostic: Verify buttons are visible
    /// </summary>
    private void VerifyButtonsVisible()
    {
        Button[] buttons = { slot1Button, slot2Button, slot3Button, slot4Button, slot5Button, cancelButton };
        int visibleCount = 0;
        
        Debug.Log("=== Button Position Diagnostics ===");
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.gameObject.activeInHierarchy)
            {
                RectTransform rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    bool hasSize = rect.rect.width > 0 && rect.rect.height > 0;
                    Debug.Log($"Button {i+1} ({button.name}): Position={rect.anchoredPosition}, Size={rect.rect.size}, HasSize={hasSize}, LocalPosition={rect.localPosition}");
                    
                    if (hasSize)
                    {
                        visibleCount++;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Button {i+1}: {(button == null ? "NULL" : button.name)} - ActiveInHierarchy: {button != null && button.gameObject.activeInHierarchy}");
            }
        }
        
        Debug.Log($"SaveSlotSelectionUI: {visibleCount}/{buttons.Length} buttons are visible and have size > 0");
        
        if (visibleCount == 0)
        {
            Debug.LogError("SaveSlotSelectionUI: NO BUTTONS ARE VISIBLE! This is likely the cause of the black square issue.");
        }
        else if (visibleCount < buttons.Length)
        {
            Debug.LogWarning($"SaveSlotSelectionUI: Only {visibleCount}/{buttons.Length} buttons are visible. Some buttons may be positioned incorrectly.");
        }
    }

    /// <summary>
    /// Hide the save slot selection UI
    /// </summary>
    public void HideSelectionUI()
    {
        if (selectionPanel != null)
        {
            if (selectionPanelCanvasGroup != null)
            {
                selectionPanelCanvasGroup.alpha = 0f;
                selectionPanelCanvasGroup.interactable = false;
                selectionPanelCanvasGroup.blocksRaycasts = false;
                Debug.Log("SaveSlotSelectionUI: Disabled CanvasGroup for selectionPanel.");
            }

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

        // Debug log to track mode
        Debug.Log($"SaveSlotSelectionUI: OnSlotSelected called with slot {slot}, currentMode = {currentMode}");

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
                
                // CRITICAL: Return early to prevent any fall-through or double execution
                return;
            }
            else
            {
                Debug.LogWarning($"Failed to save to slot {slot}");
                // Could show an error message to the player here
                return;
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

    /// <summary>
    /// Coroutine fallback to activate LoadMenuPanel after a frame delay
    /// This ensures activation happens after any other code that might be deactivating it
    /// </summary>
    private IEnumerator DelayedActivationFallback()
    {
        yield return null; // Wait one frame
        
        if (selectionPanel != null)
        {
            Debug.Log($"SaveSlotSelectionUI: DelayedActivationFallback - attempting activation after frame delay");
            selectionPanel.SetActive(true);
            
            // Verify after delay
            bool activeSelf = selectionPanel.activeSelf;
            bool activeInHierarchy = selectionPanel.activeInHierarchy;
            Debug.Log($"SaveSlotSelectionUI: DelayedActivationFallback - after delay: activeSelf={activeSelf}, activeInHierarchy={activeInHierarchy}");
            
            if (!activeSelf)
            {
                Debug.LogError($"SaveSlotSelectionUI: DelayedActivationFallback FAILED! LoadMenuPanel still inactive. name='{selectionPanel.name}', instanceID={selectionPanel.GetInstanceID()}");
            }
            else
            {
                Debug.Log($"SaveSlotSelectionUI: DelayedActivationFallback SUCCESS! LoadMenuPanel is now active.");
            }
        }
    }

    private void EnsureSelectionPanelParent()
    {
        if (selectionPanel == null)
        {
            Debug.LogWarning("SaveSlotSelectionUI: selectionPanel is null; cannot reparent to pause menu panel.");
            return;
        }

        if (pauseMenuManager == null || pauseMenuManager.pauseMenuPanel == null)
        {
            Debug.LogWarning("SaveSlotSelectionUI: pauseMenuManager or pauseMenuPanel is null; skipping reparent.");
            return;
        }

        Transform desiredParent = pauseMenuManager.pauseMenuPanel.transform;
        if (selectionPanel.transform.parent != desiredParent)
        {
            Debug.Log($"SaveSlotSelectionUI: Reparenting LoadMenuPanel from {selectionPanel.transform.parent?.name ?? "<none>"} to {desiredParent.name}.");
            selectionPanel.transform.SetParent(desiredParent, false);
        }

        LayoutElement layoutElement = selectionPanel.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = selectionPanel.AddComponent<LayoutElement>();
            layoutElement.minWidth = -1;
            layoutElement.minHeight = -1;
            layoutElement.preferredWidth = -1;
            layoutElement.preferredHeight = -1;
        }

        if (!layoutElement.ignoreLayout)
        {
            layoutElement.ignoreLayout = true;
            Debug.Log("SaveSlotSelectionUI: Set LayoutElement.ignoreLayout=true so PauseMenuPanel layout won't reposition LoadMenuPanel.");
        }
    }
}
