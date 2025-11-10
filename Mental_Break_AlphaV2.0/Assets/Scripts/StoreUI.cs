using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using System.Collections.Generic;
using System.Collections;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Store item definition
/// </summary>
[System.Serializable]
public class StoreItem
{
    public string id; // Yarn variable name (e.g., "item_mental_break")
    public string displayName;
    public int cost;
    public string description;
}

/// <summary>
/// Displays the Company Store UI when rapid_feedback_cash is awarded.
/// Handles purchasing items and updating Yarn variables.
/// </summary>
public class StoreUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The DialogueRunner to monitor for cash awards")]
    public DialogueRunner dialogueRunner;

    [Header("UI Elements")]
    [Tooltip("The store panel GameObject")]
    public GameObject storePanel;

    [Tooltip("Close button for the store")]
    public Button closeButton;

    [Tooltip("Text showing available cash")]
    public Component cashText;

    [Tooltip("Container for store item buttons (optional - if null, uses text display)")]
    public Transform itemButtonContainer;

    [Tooltip("Prefab for store item buttons (if using button-based UI)")]
    public GameObject itemButtonPrefab;

    [Tooltip("Text showing store items (used if itemButtonContainer is null)")]
    public Component storeItemsText;

    [Tooltip("Optional text element for store effect notifications")]
    public Component notificationText;

    [Header("Store Configuration")]
    [Tooltip("List of available store items")]
    public List<StoreItem> storeItems = new List<StoreItem>
    {
        new StoreItem
        {
            id = "item_mental_break",
            displayName = "Mental Break",
            cost = 10,
            description = "Give Timmy a breather. +10 Sanity immediately."
        },
        new StoreItem
        {
            id = "item_blackout_curtains",
            displayName = "Blackout Curtains",
            cost = 14,
            description = "Send Timmy blackout curtains. +14 Sanity now; -6 Engagement on the next node."
        },
        new StoreItem
        {
            id = "item_blue_light_filter",
            displayName = "Blue-Light Filter",
            cost = 16,
            description = "Warm Timmy’s screens. +15 Sanity tomorrow night; -1 Engagement each node tomorrow."
        },
        new StoreItem
        {
            id = "item_screen_protector",
            displayName = "Screen Protector",
            cost = 12,
            description = "Be less interesting to supervisors. Adds heat damping for the rest of the run."
        },
        new StoreItem
        {
            id = "item_priority_shipping",
            displayName = "Priority Shipping Label",
            cost = 18,
            description = "Parcel gets waved through. Unlock a dual escape during the mailroom scene."
        },
        new StoreItem
        {
            id = "item_bow_for_alice",
            displayName = "Bow for Alice",
            cost = 11,
            description = "Cute accessory. +1 Engagement each time you choose a pro-engagement option."
        },
        new StoreItem
        {
            id = "item_corporate_bond",
            displayName = "Corporate Bond",
            cost = 10,
            description = "Earn 10% interest in one day. Not available going into Run 4."
        }
    };

    private VariableStorageBehaviour variableStorage;
    private float lastCashValue = 0f;
    private Dictionary<string, Button> itemButtons = new Dictionary<string, Button>();
    private Dictionary<string, StoreItem> storeItemLookup = new Dictionary<string, StoreItem>();
    private Coroutine notificationRoutine;
    private HashSet<string> blueLightPenalizedNodes = new HashSet<string>();
    private const string StoreNotificationVar = "$store_last_notification";

    private void Start()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (dialogueRunner != null)
        {
            variableStorage = dialogueRunner.VariableStorage;
        }

        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseStore);
        }

        // Subscribe to node start to detect when store should appear
        if (dialogueRunner != null && dialogueRunner.onNodeStart != null)
        {
            dialogueRunner.onNodeStart.AddListener(OnNodeStarted);
        }

        // Cache quick lookup for store items
        storeItemLookup.Clear();
        foreach (var item in storeItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.id))
            {
                storeItemLookup[item.id] = item;
            }
        }

        InitializeStoreState();
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null && dialogueRunner.onNodeStart != null)
        {
            dialogueRunner.onNodeStart.RemoveListener(OnNodeStarted);
        }
    }

    private void Update()
    {
        // Check if cash value changed (indicates store should open)
        if (variableStorage != null)
        {
            float currentCash = 0f;
            if (variableStorage.TryGetValue<float>("$rapid_feedback_cash", out var cashValue))
            {
                currentCash = cashValue;
            }

            // If cash increased, show store
            if (currentCash > lastCashValue && lastCashValue > 0)
            {
                ShowStore();
            }

            lastCashValue = currentCash;
        }
    }

    private void OnNodeStarted(string nodeName)
    {
        if (variableStorage == null) return;

        int currentRun = GetCurrentRun();
        int currentDay = GetCurrentDay();

        ProcessBlackoutCurtains();
        ProcessBlueLightFilter(currentRun, currentDay, nodeName);
        ProcessCorporateBond(currentRun, currentDay);
        ProcessBowForAliceBonus();

        RefreshEngagementBaseline();
    }

    private void ShowStore()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(true);
        }

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

        UpdateStoreDisplay();
    }

    private void CloseStore()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Handles <<store>> command from Yarn scripts to open the store UI
    /// Usage in Yarn: <<store>>
    /// </summary>
    [YarnCommand("store")]
    public void OpenStore()
    {
        ShowStore();
    }

    private void UpdateStoreDisplay()
    {
        if (variableStorage == null)
        {
            Debug.LogError("StoreUI: VariableStorage is null!");
            return;
        }

        float cash = GetFloat("$rapid_feedback_cash", 0f);
        int currentRun = GetCurrentRun();
        int currentDay = GetCurrentDay();

        if (cashText != null)
        {
            SetText(cashText, $"Available Credits: {cash:F0}");
        }

        // Update UI based on whether we're using buttons or text
        if (itemButtonContainer != null && itemButtonPrefab != null)
        {
            Debug.Log($"StoreUI: Updating button-based store. Container: {itemButtonContainer.name}, Prefab: {itemButtonPrefab.name}, Items: {storeItems.Count}");
            UpdateButtonBasedStore(cash, currentRun, currentDay);
        }
        else if (storeItemsText != null)
        {
            Debug.Log("StoreUI: Updating text-based store (fallback)");
            UpdateTextBasedStore(cash, currentRun, currentDay);
        }
        else
        {
            Debug.LogError("StoreUI: Neither itemButtonContainer/itemButtonPrefab nor storeItemsText are assigned! Cannot display store items.");
        }
    }

    private void UpdateButtonBasedStore(float cash, int currentRun, int currentDay)
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

        if (storeItems == null || storeItems.Count == 0)
        {
            Debug.LogError("StoreUI: storeItems list is empty! No items to display.");
            return;
        }

        Debug.Log($"StoreUI: Creating buttons for {storeItems.Count} items");

        // Create buttons for each item
        foreach (var item in storeItems)
        {
            bool isOwned = IsItemOwned(item);
            bool isAvailable = IsItemCurrentlyAvailable(item, currentRun, currentDay);
            bool affordable = cash >= item.cost;

            GameObject buttonObj = Instantiate(itemButtonPrefab, itemButtonContainer);
            buttonObj.SetActive(true); // Ensure button is active
            
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.GetComponentInChildren<Button>();
            }

            if (button != null)
            {
                // Find text component - try TextMeshPro first, then Text
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
                    string status = BuildItemStatus(item, isOwned, isAvailable, cash, currentRun, currentDay);
                    // Show cost prominently at the beginning
                    string costDisplay = isOwned ? "[OWNED]" : $"[{item.cost} Credits]";
                    string buttonText = $"{costDisplay} {item.displayName}\n{item.description}\n{status}";
                    SetText(textComponent, buttonText);
                }
                else
                {
                    Debug.LogWarning($"StoreUI: Could not find text component in button for {item.displayName}");
                }

                // Determine if button should be enabled (for visual styling)
                bool shouldBeInteractable = !isOwned && isAvailable && affordable;
                
                // Keep button interactable so it can be clicked for feedback, but visually style it as disabled
                button.interactable = true; // Always allow clicking for feedback
                
                // Apply visual styling to show disabled state
                if (!shouldBeInteractable)
                {
                    // Make button appear disabled visually
                    ColorBlock colors = button.colors;
                    colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Grayed out
                    colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    button.colors = colors;
                    
                    Debug.Log($"StoreUI: Button for {item.displayName} appears disabled. Owned: {isOwned}, Available: {isAvailable}, Affordable: {affordable}, Cash: {cash}, Cost: {item.cost}");
                }

                // Set up click handler - always allow clicking to show feedback
                string itemId = item.id; // Capture for closure
                
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    Debug.Log($"StoreUI: Button clicked for {item.displayName} (ID: {itemId})");
                    
                    // Re-check current state (values may have changed)
                    float currentCash = GetFloat("$rapid_feedback_cash", 0f);
                    bool currentlyOwned = IsItemOwned(item);
                    bool currentlyAvailable = IsItemCurrentlyAvailable(item, GetCurrentRun(), GetCurrentDay());
                    bool currentlyAffordable = currentCash >= item.cost;
                    
                    // Check if purchase is valid before attempting
                    if (currentlyOwned)
                    {
                        QueueNotification($"{item.displayName} is already owned.");
                        return;
                    }
                    
                    if (!currentlyAvailable)
                    {
                        QueueNotification($"{item.displayName} is not available.");
                        return;
                    }
                    
                    if (!currentlyAffordable)
                    {
                        float shortfall = item.cost - currentCash;
                        QueueNotification($"Insufficient credits for {item.displayName}. Need {shortfall:F0} more credits. (You have {currentCash:F0})");
                        return;
                    }
                    
                    // All checks passed, proceed with purchase
                    PurchaseItem(itemId);
                });

                // Ensure button can receive raycasts
                CanvasGroup canvasGroup = buttonObj.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = buttonObj.AddComponent<CanvasGroup>();
                }
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true; // Always allow interaction

                itemButtons[item.id] = button;
            }
            else
            {
                Debug.LogError($"StoreUI: Could not find Button component in prefab for {item.displayName}");
            }
        }
    }

    private void UpdateTextBasedStore(float cash, int currentRun, int currentDay)
    {
        string storeText = "Company Store\n\n";
        
        foreach (var item in storeItems)
        {
            bool isOwned = IsItemOwned(item);
            bool isAvailable = IsItemCurrentlyAvailable(item, currentRun, currentDay);
            string status = BuildItemStatus(item, isOwned, isAvailable, cash, currentRun, currentDay);
            storeText += $"- {item.displayName} [{status}]\n  {item.description}\n";
        }

        SetText(storeItemsText, storeText);
    }

    /// <summary>
    /// Purchase an item from the store
    /// </summary>
    public void PurchaseItem(string itemId)
    {
        if (variableStorage == null)
        {
            Debug.LogError("Cannot purchase item: VariableStorage not available!");
            return;
        }

        // Find the item
        StoreItem item = storeItems.Find(i => i.id == itemId);
        if (item == null)
        {
            Debug.LogError($"Cannot purchase item: Item '{itemId}' not found!");
            return;
        }

        int currentRun = GetCurrentRun();
        int currentDay = GetCurrentDay();

        // Check if already owned
        bool isOwned = IsItemOwned(item);

        if (isOwned)
        {
            Debug.Log($"Item '{item.displayName}' is already owned.");
            return;
        }

        if (!IsItemCurrentlyAvailable(item, currentRun, currentDay))
        {
            Debug.Log($"Item '{item.displayName}' is currently unavailable.");
            return;
        }

        // Check if player has enough cash
        float cash = GetFloat("$rapid_feedback_cash", 0f);

        if (cash < item.cost)
        {
            Debug.Log($"Insufficient funds to purchase '{item.displayName}'. Need {item.cost}, have {cash}.");
            return;
        }

        // Deduct cost and set ownership flag
        variableStorage.SetValue("$rapid_feedback_cash", cash - item.cost);
        variableStorage.SetValue($"${item.id}", true);

        Debug.Log($"Purchased '{item.displayName}' for {item.cost} credits.");

        // Apply item effects
        ApplyItemEffects(item, currentRun, currentDay);
        RefreshEngagementBaseline();

        // Update display
        UpdateStoreDisplay();
    }

    /// <summary>
    /// Apply game effects when an item is purchased
    /// </summary>
    private void ApplyItemEffects(StoreItem item, int currentRun, int currentDay)
    {
        if (variableStorage == null || item == null) return;

        switch (item.id)
        {
            case "item_mental_break":
                AdjustSanity(10f);
                Debug.Log("Mental Break: +10 Sanity applied immediately.");
                break;

            case "item_blackout_curtains":
                AdjustSanity(14f);
                SetBool("$store_blackout_pending", true);
                Debug.Log("Blackout Curtains: +14 Sanity applied; engagement penalty scheduled for the next node.");
                break;

            case "item_blue_light_filter":
                ScheduleBlueLightEffects(currentRun, currentDay);
                Debug.Log("Blue-Light Filter: Scheduled sanity bonus and engagement penalties for tomorrow.");
                break;

            case "item_screen_protector":
                SetFloat("$store_screen_protector_heat_modifier", -1f);
                Debug.Log("Screen Protector: Heat modifier set to -1 for the rest of the run.");
                break;

            case "item_priority_shipping":
                Debug.Log("Priority Shipping Label: Mailroom dual-escape unlock flagged.");
                break;

            case "item_bow_for_alice":
                Debug.Log("Bow for Alice: Engagement bonus tracking enabled.");
                break;

            case "item_corporate_bond":
                ScheduleCorporateBond(currentRun, currentDay, item.cost);
                Debug.Log("Corporate Bond: Payout scheduled for the next in-run day.");
                break;
        }
    }

    private void InitializeStoreState()
    {
        if (variableStorage == null) return;

        // Ensure we have a baseline for engagement delta tracking.
        if (!variableStorage.TryGetValue<float>("$store_prev_engagement", out _))
        {
            float engagement = GetFloat("$engagement", 0f);
            variableStorage.SetValue("$store_prev_engagement", engagement);
        }
    }

    private void RefreshEngagementBaseline()
    {
        if (variableStorage == null) return;
        float engagement = GetFloat("$engagement", 0f);
        variableStorage.SetValue("$store_prev_engagement", engagement);
    }

    private float GetFloat(string variableName, float defaultValue = 0f)
    {
        if (variableStorage != null && variableStorage.TryGetValue<float>(variableName, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    private bool GetBool(string variableName)
    {
        if (variableStorage != null && variableStorage.TryGetValue<bool>(variableName, out var value))
        {
            return value;
        }

        return false;
    }

    private void SetBool(string variableName, bool value)
    {
        variableStorage?.SetValue(variableName, value);
    }

    private void SetFloat(string variableName, float value)
    {
        variableStorage?.SetValue(variableName, value);
    }

    private int GetCurrentRun()
    {
        return Mathf.Max(1, Mathf.RoundToInt(GetFloat("$current_run", 1f)));
    }

    private int GetCurrentDay()
    {
        return Mathf.Max(1, Mathf.RoundToInt(GetFloat("$current_day", 1f)));
    }

    private bool IsItemOwned(StoreItem item)
    {
        if (item == null) return false;
        return GetBool($"${item.id}");
    }

    private bool IsItemCurrentlyAvailable(StoreItem item, int currentRun, int currentDay)
    {
        if (item == null) return false;

        // Already owned items cannot be repurchased.
        if (IsItemOwned(item))
        {
            return false;
        }

        // Corporate Bond unavailable when entering Run 4 or if an active bond is already pending.
        if (item.id == "item_corporate_bond")
        {
            if (currentRun >= 4)
            {
                return false;
            }

            if (GetBool("$store_corporate_bond_active"))
            {
                return false;
            }
        }

        return true;
    }

    private string BuildItemStatus(StoreItem item, bool isOwned, bool isAvailable, float cash, int currentRun, int currentDay)
    {
        if (item == null)
        {
            return "Unavailable";
        }

        if (isOwned)
        {
            switch (item.id)
            {
                case "item_blackout_curtains":
                    return GetBool("$store_blackout_pending") ? "Owned (penalty pending)" : "Owned";
                case "item_blue_light_filter":
                    if (GetBool("$store_blue_filter_active"))
                    {
                        int targetRun = Mathf.RoundToInt(GetFloat("$store_blue_filter_target_run", currentRun));
                        int targetDay = Mathf.RoundToInt(GetFloat("$store_blue_filter_target_day", currentDay));
                        return $"Owned (effects target R{targetRun}D{targetDay})";
                    }
                    return "Owned";
                case "item_screen_protector":
                    return "Owned (heat damping active)";
                case "item_priority_shipping":
                    return "Owned (mailroom unlock)";
                case "item_bow_for_alice":
                    return "Owned (engagement bonus active)";
                case "item_corporate_bond":
                    if (GetBool("$store_corporate_bond_active"))
                    {
                        int targetRun = Mathf.RoundToInt(GetFloat("$store_corporate_bond_mature_run", currentRun));
                        int targetDay = Mathf.RoundToInt(GetFloat("$store_corporate_bond_mature_day", currentDay));
                        return $"Owned (matures R{targetRun}D{targetDay})";
                    }
                    return "Owned (payout collected)";
                default:
                    return "Owned";
            }
        }

        if (!isAvailable)
        {
            if (item.id == "item_corporate_bond" && currentRun >= 4)
            {
                return "Unavailable in Run 4";
            }

            if (item.id == "item_corporate_bond" && GetBool("$store_corporate_bond_active"))
            {
                return "Unavailable (bond pending)";
            }

            return "Unavailable";
        }

        if (cash < item.cost)
        {
            float shortfall = Mathf.Max(0f, item.cost - cash);
            return $"Cost: {item.cost} (Need {shortfall:F0} more)";
        }

        return $"Cost: {item.cost} (Available)";
    }

    private void AdjustSanity(float delta)
    {
        float sanity = GetFloat("$sanity", 0f);
        SetFloat("$sanity", sanity + delta);
    }

    private void AdjustEngagement(float delta)
    {
        float engagement = GetFloat("$engagement", 0f);
        SetFloat("$engagement", engagement + delta);
    }

    private void ScheduleBlueLightEffects(int currentRun, int currentDay)
    {
        SetBool("$store_blue_filter_active", true);
        int targetRun = currentRun;
        int targetDay = currentDay + 1;
        blueLightPenalizedNodes.Clear();

        if (targetDay > 4)
        {
            targetDay = 1;
            targetRun += 1;
        }

        SetFloat("$store_blue_filter_target_run", targetRun);
        SetFloat("$store_blue_filter_target_day", targetDay);
        SetFloat("$store_blue_filter_penalties_applied", 0f);
        SetBool("$store_blue_filter_bonus_applied", false);
    }

    private void ScheduleCorporateBond(int currentRun, int currentDay, int cost)
    {
        SetBool("$store_corporate_bond_active", true);
        SetFloat("$store_corporate_bond_principal", cost);

        int targetRun = currentRun;
        int targetDay = currentDay + 1;

        if (targetDay > 4)
        {
            targetDay = 1;
            targetRun += 1;
        }

        SetFloat("$store_corporate_bond_mature_run", targetRun);
        SetFloat("$store_corporate_bond_mature_day", targetDay);
    }

    private void ProcessBlackoutCurtains()
    {
        if (!GetBool("$store_blackout_pending"))
        {
            return;
        }

        AdjustEngagement(-6f);
        SetBool("$store_blackout_pending", false);
        QueueNotification("Blackout Curtains: Engagement drops by 6 while Timmy rests.");
    }

    private void ProcessBlueLightFilter(int currentRun, int currentDay, string nodeName)
    {
        if (!GetBool("$store_blue_filter_active"))
        {
            return;
        }

        int targetRun = Mathf.RoundToInt(GetFloat("$store_blue_filter_target_run", currentRun));
        int targetDay = Mathf.RoundToInt(GetFloat("$store_blue_filter_target_day", currentDay));

        // Not yet time to apply the effect.
        if (currentRun < targetRun || (currentRun == targetRun && currentDay < targetDay))
        {
            return;
        }

        // Apply the scheduled bonuses/penalties on the target day.
        if (currentRun == targetRun && currentDay == targetDay)
        {
            if (!GetBool("$store_blue_filter_bonus_applied"))
            {
                AdjustSanity(15f);
                SetBool("$store_blue_filter_bonus_applied", true);
                QueueNotification("Blue-Light Filter: Timmy sleeps deeply (+15 sanity).");
            }

            if (!string.IsNullOrEmpty(nodeName) && !blueLightPenalizedNodes.Contains(nodeName))
            {
                blueLightPenalizedNodes.Add(nodeName);
                float applied = GetFloat("$store_blue_filter_penalties_applied", 0f) + 1f;
                SetFloat("$store_blue_filter_penalties_applied", applied);
                AdjustEngagement(-1f);
                QueueNotification("Blue-Light Filter: Engagement dips by 1 while Timmy powers down.");
            }

            return;
        }

        // Past the scheduled day: clear the effect.
        SetBool("$store_blue_filter_active", false);
        blueLightPenalizedNodes.Clear();
        QueueNotification("Blue-Light Filter effect has ended.");
    }

    private void ProcessCorporateBond(int currentRun, int currentDay)
    {
        if (!GetBool("$store_corporate_bond_active"))
        {
            return;
        }

        int targetRun = Mathf.RoundToInt(GetFloat("$store_corporate_bond_mature_run", currentRun));
        int targetDay = Mathf.RoundToInt(GetFloat("$store_corporate_bond_mature_day", currentDay));

        bool shouldPayout = currentRun > targetRun || (currentRun == targetRun && currentDay >= targetDay);
        if (!shouldPayout)
        {
            return;
        }

        float principal = GetFloat("$store_corporate_bond_principal", 0f);
        if (principal <= 0f)
        {
            SetBool("$store_corporate_bond_active", false);
            return;
        }

        int payout = Mathf.RoundToInt(principal * 1.1f);
        float currentCash = GetFloat("$rapid_feedback_cash", 0f);
        SetFloat("$rapid_feedback_cash", currentCash + payout);

        SetBool("$store_corporate_bond_active", false);
        SetFloat("$store_corporate_bond_principal", 0f);
        SetFloat("$store_corporate_bond_mature_run", 0f);
        SetFloat("$store_corporate_bond_mature_day", 0f);

        QueueNotification($"Corporate Bond matured: +{payout} credits.");
    }

    private void ProcessBowForAliceBonus()
    {
        if (!GetBool("$item_bow_for_alice"))
        {
            return;
        }

        float previousEngagement = GetFloat("$store_prev_engagement", GetFloat("$engagement", 0f));
        float currentEngagement = GetFloat("$engagement", 0f);
        float delta = currentEngagement - previousEngagement;

        // Bow for Alice: +1 engagement per pro-engagement choice (any engagement increase)
        if (delta > 0.05f)
        {
            AdjustEngagement(1f);
            QueueNotification("Bow for Alice: Engagement nudges up by 1.");
        }
    }

    private void QueueNotification(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Debug.Log($"[StoreUI] {message}");
        SetString(StoreNotificationVar, message);

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

    private void SetString(string variableName, string value)
    {
        variableStorage?.SetValue(variableName, value ?? string.Empty);
    }

    private string GetString(string variableName, string defaultValue = "")
    {
        if (variableStorage != null && variableStorage.TryGetValue<string>(variableName, out var value))
        {
            return value;
        }

        return defaultValue;
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
}
