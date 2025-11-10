using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using System.Collections.Generic;
using System.Collections;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Store UI - handles display and user interaction only.
/// Business logic is delegated to StoreManager.
/// </summary>
public class StoreUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The DialogueRunner to get variable storage")]
    public DialogueRunner dialogueRunner;

    [Tooltip("The StoreManager component")]
    public StoreManager storeManager;

    [Header("UI Elements")]
    [Tooltip("The store panel GameObject")]
    public GameObject storePanel;

    [Tooltip("Close button for the store")]
    public Button closeButton;

    [Tooltip("Button to close store without purchasing")]
    public Button passWithoutBuyingButton;

    [Tooltip("Text showing available cash")]
    public Component cashText;

    [Tooltip("Container for store item buttons")]
    public Transform itemButtonContainer;

    [Tooltip("Prefab for store item buttons")]
    public GameObject itemButtonPrefab;

    [Tooltip("Optional text element for store notifications")]
    public Component notificationText;

    private Dictionary<string, Button> itemButtons = new Dictionary<string, Button>();
    private List<LineAdvancer> disabledLineAdvancers = new List<LineAdvancer>();
    private List<MonoBehaviour> disabledInputComponents = new List<MonoBehaviour>();
    private Coroutine notificationRoutine;
    private bool isStoreOpen = false;

    private bool commandRegistered = false;

    private void Start()
    {
        // Find DialogueRunner if not assigned
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        // Find StoreManager if not assigned
        if (storeManager == null)
        {
            storeManager = GetComponent<StoreManager>();
            if (storeManager == null)
            {
                storeManager = FindAnyObjectByType<StoreManager>();
            }
        }

        // Initialize StoreManager with variable storage
        if (storeManager != null && dialogueRunner != null)
        {
            storeManager.Initialize(dialogueRunner.VariableStorage);
        }

        // Hide store panel initially
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }

        RegisterStoreCommand();

        // Setup button listeners
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseStore);
        }

        if (passWithoutBuyingButton != null)
        {
            passWithoutBuyingButton.onClick.AddListener(CloseStore);
        }
    }

    private void OnDestroy()
    {
        UnregisterStoreCommand();
    }

    private void RegisterStoreCommand()
    {
        if (dialogueRunner == null || commandRegistered)
        {
            return;
        }

        try
        {
            dialogueRunner.RemoveCommandHandler("store");
        }
        catch
        {
            // ignore
        }

        try
        {
            dialogueRunner.AddCommandHandler("store", new System.Func<IEnumerator>(OpenStore));
            commandRegistered = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"StoreUI: Failed to register 'store' command: {ex.Message}");
        }
    }

    private void UnregisterStoreCommand()
    {
        if (dialogueRunner == null || !commandRegistered)
        {
            return;
        }

        try
        {
            dialogueRunner.RemoveCommandHandler("store");
        }
        catch
        {
            // ignore
        }

        commandRegistered = false;
    }

    /// <summary>
    /// Handles <<store>> command from Yarn scripts to open the store UI
    /// This command will wait until the store is closed before continuing dialogue
    /// Returns IEnumerator so Yarn Spinner waits for the store to close
    /// </summary>
    [YarnCommand("store")]
    public IEnumerator OpenStore()
    {
        ShowStore();
        
        // Wait until store is closed
        while (isStoreOpen)
        {
            yield return null;
        }
        
        // Re-enable dialogue input (dialogue will continue automatically after command completes)
        EnableDialogueInput();
    }

    /// <summary>
    /// Show the store UI
    /// </summary>
    public void ShowStore()
    {
        isStoreOpen = true;
        
        if (storePanel != null)
        {
            storePanel.SetActive(true);
        }

        // Disable dialogue input components to prevent advancing dialogue while store is open
        DisableDialogueInput();

        // Ensure Canvas has GraphicRaycaster for UI interaction
        Canvas canvas = storePanel?.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogWarning("StoreUI: Canvas missing GraphicRaycaster! Adding it now.");
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            // Ensure EventSystem exists
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                Debug.LogWarning("StoreUI: No EventSystem found! UI interactions may not work.");
            }
        }

        // Ensure scroll view has proper masking
        EnsureScrollViewMasking();

        UpdateStoreDisplay();
    }

    /// <summary>
    /// Close the store UI
    /// </summary>
    public void CloseStore()
    {
        isStoreOpen = false;

        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }

        // Clear any notifications
        if (notificationRoutine != null)
        {
            StopCoroutine(notificationRoutine);
            notificationRoutine = null;
        }
        if (notificationText != null)
        {
            SetText(notificationText, string.Empty);
        }
    }

    /// <summary>
    /// Update the store display with current items and cash
    /// </summary>
    private void UpdateStoreDisplay()
    {
        if (storeManager == null)
        {
            Debug.LogError("StoreUI: StoreManager is null!");
            return;
        }

        float cash = storeManager.GetCash();
        int currentRun = storeManager.GetCurrentRun();
        int currentDay = storeManager.GetCurrentDay();

        // Update cash display
        if (cashText != null)
        {
            SetText(cashText, $"Available Credits: {cash:F0}");
        }

        // Update item buttons
        if (itemButtonContainer != null && itemButtonPrefab != null)
        {
            UpdateItemButtons(cash, currentRun, currentDay);
        }
        else
        {
            Debug.LogError("StoreUI: itemButtonContainer or itemButtonPrefab not assigned!");
        }
    }

    /// <summary>
    /// Create/update buttons for all store items
    /// </summary>
    private void UpdateItemButtons(float cash, int currentRun, int currentDay)
    {
        // Clear existing buttons
        foreach (var button in itemButtons.Values)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        itemButtons.Clear();

        List<StoreItem> items = storeManager.GetStoreItems();
        if (items == null || items.Count == 0)
        {
            Debug.LogError("StoreUI: No store items found!");
            return;
        }

        // Create buttons for each item
        foreach (var item in items)
        {
            bool isOwned = storeManager.IsItemOwned(item.id);
            bool isAvailable = storeManager.IsItemAvailable(item.id);
            bool affordable = storeManager.CanAfford(item.id);

            GameObject buttonObj = Instantiate(itemButtonPrefab, itemButtonContainer);
            buttonObj.SetActive(true);

            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.GetComponentInChildren<Button>();
            }

            if (button != null)
            {
                // Find text component
                Component textComponent = null;
#if USE_TMP
                textComponent = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
#endif
                if (textComponent == null)
                {
                    textComponent = buttonObj.GetComponentInChildren<UnityEngine.UI.Text>();
                }

                if (textComponent != null)
                {
                    // Format: "Item title - Credit cost (conditional: you need x more credits)"
                    // Then description on next line
                    string titleLine;
                    if (isOwned)
                    {
                        titleLine = $"{item.displayName} - [OWNED]";
                    }
                    else if (!affordable && isAvailable)
                    {
                        float shortfall = item.cost - cash;
                        titleLine = $"{item.displayName} - {item.cost} Credits (you need {shortfall:F0} more credits)";
                    }
                    else
                    {
                        titleLine = $"{item.displayName} - {item.cost} Credits";
                    }
                    
                    string buttonText = $"{titleLine}\n{item.description}";
                    SetText(textComponent, buttonText);
                }

                // Visual styling for disabled state
                bool shouldBeInteractable = !isOwned && isAvailable && affordable;
                button.interactable = true; // Always allow clicking for feedback

                if (!shouldBeInteractable)
                {
                    ColorBlock colors = button.colors;
                    colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    button.colors = colors;
                }

                // Set up click handler
                string itemId = item.id;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnItemButtonClicked(itemId));

                // Ensure button can receive raycasts
                CanvasGroup canvasGroup = buttonObj.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = buttonObj.AddComponent<CanvasGroup>();
                }
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;

                itemButtons[item.id] = button;
            }
            else
            {
                Debug.LogError($"StoreUI: Could not find Button component in prefab for {item.displayName}");
            }
        }
    }

    /// <summary>
    /// Handle item button click
    /// </summary>
    private void OnItemButtonClicked(string itemId)
    {
        if (storeManager == null)
        {
            Debug.LogError("StoreUI: StoreManager is null!");
            return;
        }

        // Attempt purchase
        bool success = storeManager.PurchaseItem(itemId, out string errorMessage);

        if (success)
        {
            StoreItem item = storeManager.GetStoreItems().Find(i => i.id == itemId);
            QueueNotification($"Purchased {item.displayName}!");
            UpdateStoreDisplay(); // Refresh display
            
            // Close store after successful purchase
            StartCoroutine(CloseStoreAfterDelay(0.5f));
        }
        else
        {
            // Show error in notification area
            QueueNotification(errorMessage);
        }
    }

    /// <summary>
    /// Build status string for an item
    /// </summary>
    private string BuildItemStatus(StoreItem item, bool isOwned, bool isAvailable, bool affordable, float cash)
    {
        if (isOwned)
        {
            return "Owned";
        }

        if (!isAvailable)
        {
            return "Unavailable";
        }

        if (!affordable)
        {
            float shortfall = item.cost - cash;
            return $"Need {shortfall:F0} more credits";
        }

        return "Available";
    }

    /// <summary>
    /// Disable dialogue input components
    /// </summary>
    private void DisableDialogueInput()
    {
        disabledLineAdvancers.Clear();
        disabledInputComponents.Clear();

        // Find and disable all LineAdvancer components
        LineAdvancer[] lineAdvancers = FindObjectsByType<LineAdvancer>(FindObjectsSortMode.None);
        foreach (LineAdvancer advancer in lineAdvancers)
        {
            if (advancer != null && advancer.enabled)
            {
                disabledLineAdvancers.Add(advancer);
                advancer.enabled = false;
            }
        }

        // Find and disable BubbleInput components
        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in allBehaviours)
        {
            if (behaviour != null && behaviour.enabled)
            {
                string typeName = behaviour.GetType().Name;
                if (typeName == "BubbleInput")
                {
                    disabledInputComponents.Add(behaviour);
                    behaviour.enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// Re-enable dialogue input components
    /// </summary>
    private void EnableDialogueInput()
    {
        foreach (LineAdvancer advancer in disabledLineAdvancers)
        {
            if (advancer != null)
            {
                advancer.enabled = true;
            }
        }
        disabledLineAdvancers.Clear();

        foreach (MonoBehaviour component in disabledInputComponents)
        {
            if (component != null)
            {
                component.enabled = true;
            }
        }
        disabledInputComponents.Clear();
    }

    /// <summary>
    /// Show a notification message
    /// </summary>
    private void QueueNotification(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Debug.Log($"[StoreUI] {message}");

        if (notificationText != null)
        {
            SetText(notificationText, message);
            if (notificationRoutine != null)
            {
                StopCoroutine(notificationRoutine);
            }
            notificationRoutine = StartCoroutine(ClearNotificationAfterDelay(4f));
        }
    }

    private IEnumerator ClearNotificationAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (notificationText != null)
        {
            SetText(notificationText, string.Empty);
        }

        notificationRoutine = null;
    }

    private void SetText(Component textComponent, string text)
    {
        if (textComponent == null) return;

#if USE_TMP
        if (textComponent is TMPro.TextMeshProUGUI tmpText)
        {
            tmpText.text = text;
            return;
        }
#endif
        if (textComponent is UnityEngine.UI.Text regularText)
        {
            regularText.text = text;
        }
    }

    /// <summary>
    /// Ensure scroll view has RectMask2D for proper clipping
    /// </summary>
    private void EnsureScrollViewMasking()
    {
        if (itemButtonContainer == null) return;

        // Find the scroll view (parent of Content)
        Transform scrollViewTransform = itemButtonContainer.parent;
        if (scrollViewTransform == null) return;

        // Add RectMask2D if missing
        RectMask2D mask = scrollViewTransform.GetComponent<RectMask2D>();
        if (mask == null)
        {
            mask = scrollViewTransform.gameObject.AddComponent<RectMask2D>();
            Debug.Log("StoreUI: Added RectMask2D to scroll view for proper clipping");
        }
    }

    /// <summary>
    /// Close store after a short delay
    /// </summary>
    private IEnumerator CloseStoreAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseStore();
    }

}

