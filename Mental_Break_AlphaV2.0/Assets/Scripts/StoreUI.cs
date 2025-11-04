using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using System.Collections.Generic;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Store item definition
/// </summary>
[System.Serializable]
public class StoreItem
{
    public string id; // Yarn variable name (e.g., "item_branded_mug")
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

    [Header("Store Configuration")]
    [Tooltip("List of available store items")]
    public List<StoreItem> storeItems = new List<StoreItem>
    {
        new StoreItem { id = "item_branded_mug", displayName = "Branded Mug", cost = 10, description = "A company-branded coffee mug. Increases morale." },
        new StoreItem { id = "item_extra_log_space", displayName = "Extra Log Space", cost = 15, description = "Expands your log storage capacity." },
        new StoreItem { id = "item_sticker_pack", displayName = "Sticker Pack", cost = 5, description = "A collection of company stickers. Purely cosmetic." }
    };

    private VariableStorageBehaviour variableStorage;
    private float lastCashValue = 0f;
    private Dictionary<string, Button> itemButtons = new Dictionary<string, Button>();

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

        // Load previously purchased items
        LoadPurchasedItems();
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
        // Check if this node mentions the store
        // Store appears after rapid_feedback_cash is awarded, usually at end of day nodes
        // This is a simple heuristic - you might want to tag specific nodes instead
    }

    private void ShowStore()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(true);
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

    private void UpdateStoreDisplay()
    {
        if (variableStorage == null) return;

        float cash = 0f;
        if (variableStorage.TryGetValue<float>("$rapid_feedback_cash", out var cashValue))
        {
            cash = cashValue;
        }

        if (cashText != null)
        {
            SetText(cashText, $"Available Credits: {cash:F0}");
        }

        // Update UI based on whether we're using buttons or text
        if (itemButtonContainer != null && itemButtonPrefab != null)
        {
            UpdateButtonBasedStore(cash);
        }
        else if (storeItemsText != null)
        {
            UpdateTextBasedStore(cash);
        }
    }

    private void UpdateButtonBasedStore(float cash)
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

        // Create buttons for each item
        foreach (var item in storeItems)
        {
            // Check if item is already owned
            bool isOwned = false;
            if (variableStorage.TryGetValue<bool>($"${item.id}", out var ownedValue))
            {
                isOwned = ownedValue;
            }

            GameObject buttonObj = Instantiate(itemButtonPrefab, itemButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.GetComponentInChildren<Button>();
            }

            if (button != null)
            {
                // Set button text
                Component textComponent = buttonObj.GetComponentInChildren<Component>();
                if (textComponent != null)
                {
                    string buttonText = isOwned 
                        ? $"{item.displayName} (OWNED)"
                        : $"{item.displayName} ({item.cost} credits)";
                    SetText(textComponent, buttonText);
                }

                // Enable/disable based on affordability and ownership
                button.interactable = !isOwned && cash >= item.cost;

                // Set up click handler
                string itemId = item.id; // Capture for closure
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => PurchaseItem(itemId));

                itemButtons[item.id] = button;
            }
        }
    }

    private void UpdateTextBasedStore(float cash)
    {
        string storeText = "Company Store\n\n";
        
        foreach (var item in storeItems)
        {
            // Check if item is already owned
            bool isOwned = false;
            if (variableStorage.TryGetValue<bool>($"${item.id}", out var ownedValue))
            {
                isOwned = ownedValue;
            }

            string status = isOwned ? "[OWNED]" : $"[{item.cost} credits]";
            string affordable = (!isOwned && cash >= item.cost) ? " (Available)" : (!isOwned ? " (Insufficient funds)" : "");
            storeText += $"• {item.displayName} {status}{affordable}\n";
        }

        SetText(storeItemsText, storeText);
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

        // Check if already owned
        bool isOwned = false;
        if (variableStorage.TryGetValue<bool>($"${item.id}", out var ownedValue))
        {
            isOwned = ownedValue;
        }

        if (isOwned)
        {
            Debug.Log($"Item '{item.displayName}' is already owned.");
            return;
        }

        // Check if player has enough cash
        float cash = 0f;
        if (variableStorage.TryGetValue<float>("$rapid_feedback_cash", out var cashValue))
        {
            cash = cashValue;
        }

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
        ApplyItemEffects(item.id);

        // Update display
        UpdateStoreDisplay();

        // Save ownership to PlayerPrefs for persistence across runs
        PlayerPrefs.SetInt(item.id, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Apply game effects when an item is purchased
    /// </summary>
    private void ApplyItemEffects(string itemId)
    {
        if (variableStorage == null) return;

        // Apply item-specific effects based on the story document
        // Items are supposed to provide "powerups, items, and upgrades for Alice"
        switch (itemId)
        {
            case "item_branded_mug":
                // Branded Mug: "Increases morale" - boosts sanity slightly
                float currentSanity = 0f;
                if (variableStorage.TryGetValue<float>("$sanity", out var sanityValue))
                {
                    currentSanity = sanityValue;
                }
                variableStorage.SetValue("$sanity", currentSanity + 2f);
                Debug.Log("Branded Mug: +2 Sanity (morale boost)");
                break;

            case "item_extra_log_space":
                // Extra Log Space: Expands capacity - could help with engagement tracking
                float currentEngagement = 0f;
                if (variableStorage.TryGetValue<float>("$engagement", out var engagementValue))
                {
                    currentEngagement = engagementValue;
                }
                variableStorage.SetValue("$engagement", currentEngagement + 1f);
                Debug.Log("Extra Log Space: +1 Engagement (better tracking)");
                break;

            case "item_sticker_pack":
                // Sticker Pack: "Purely cosmetic" - but could provide a small engagement boost
                // Since it's cosmetic, give a tiny boost to make it feel worthwhile
                float eng = 0f;
                if (variableStorage.TryGetValue<float>("$engagement", out var engValue))
                {
                    eng = engValue;
                }
                variableStorage.SetValue("$engagement", eng + 0.5f);
                Debug.Log("Sticker Pack: +0.5 Engagement (cosmetic boost)");
                break;
        }
    }

    /// <summary>
    /// Load purchased items from PlayerPrefs on game start
    /// </summary>
    public void LoadPurchasedItems()
    {
        if (variableStorage == null) return;

        foreach (var item in storeItems)
        {
            // Check if item was purchased in a previous run
            if (PlayerPrefs.GetInt(item.id, 0) == 1)
            {
                variableStorage.SetValue($"${item.id}", true);
                Debug.Log($"Loaded purchased item: {item.displayName}");
            }
        }
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
