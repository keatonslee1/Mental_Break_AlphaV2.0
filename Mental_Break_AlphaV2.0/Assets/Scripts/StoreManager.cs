using UnityEngine;
using Yarn.Unity;
using System.Collections.Generic;

/// <summary>
/// Core store logic - handles purchases, item availability, and effect application.
/// No UI dependencies - pure business logic.
/// </summary>
public class StoreManager : MonoBehaviour
{
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
            description = "Warm Timmy's screens. +15 Sanity tomorrow night; -1 Engagement each node tomorrow."
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
    private Dictionary<string, StoreItem> itemLookup = new Dictionary<string, StoreItem>();

    private void Awake()
    {
        // Build lookup dictionary
        itemLookup.Clear();
        foreach (var item in storeItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.id))
            {
                itemLookup[item.id] = item;
            }
        }
    }

    /// <summary>
    /// Initialize with DialogueRunner's variable storage
    /// </summary>
    public void Initialize(VariableStorageBehaviour storage)
    {
        variableStorage = storage;
    }

    /// <summary>
    /// Get all store items
    /// </summary>
    public List<StoreItem> GetStoreItems()
    {
        return new List<StoreItem>(storeItems);
    }

    /// <summary>
    /// Get current cash amount
    /// </summary>
    public float GetCash()
    {
        return GetFloat("$rapid_feedback_cash", 0f);
    }

    /// <summary>
    /// Check if an item is owned
    /// </summary>
    public bool IsItemOwned(string itemId)
    {
        return GetBool($"${itemId}");
    }

    /// <summary>
    /// Check if an item is currently available for purchase
    /// </summary>
    public bool IsItemAvailable(string itemId)
    {
        if (!itemLookup.ContainsKey(itemId))
        {
            return false;
        }

        StoreItem item = itemLookup[itemId];

        // Already owned items cannot be repurchased
        if (IsItemOwned(itemId))
        {
            return false;
        }

        // Corporate Bond unavailable when entering Run 4 or if an active bond is already pending
        if (itemId == "item_corporate_bond")
        {
            int currentRun = GetCurrentRun();
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

    /// <summary>
    /// Check if player can afford an item
    /// </summary>
    public bool CanAfford(string itemId)
    {
        if (!itemLookup.ContainsKey(itemId))
        {
            return false;
        }

        StoreItem item = itemLookup[itemId];
        return GetCash() >= item.cost;
    }

    /// <summary>
    /// Attempt to purchase an item. Returns true if successful, false otherwise.
    /// </summary>
    public bool PurchaseItem(string itemId, out string errorMessage)
    {
        errorMessage = "";

        if (variableStorage == null)
        {
            errorMessage = "Store system not initialized";
            return false;
        }

        if (!itemLookup.ContainsKey(itemId))
        {
            errorMessage = $"Item '{itemId}' not found";
            return false;
        }

        StoreItem item = itemLookup[itemId];

        // Check if already owned
        if (IsItemOwned(itemId))
        {
            errorMessage = $"{item.displayName} is already owned";
            return false;
        }

        // Check availability
        if (!IsItemAvailable(itemId))
        {
            errorMessage = $"{item.displayName} is not available";
            return false;
        }

        // Check affordability
        float cash = GetCash();
        if (cash < item.cost)
        {
            float shortfall = item.cost - cash;
            errorMessage = $"Insufficient credits. Need {shortfall:F0} more credits.";
            return false;
        }

        // Deduct cost and set ownership flag
        SetFloat("$rapid_feedback_cash", cash - item.cost);
        SetBool($"${itemId}", true);

        // Apply item effects
        ApplyItemEffects(item);

        Debug.Log($"Purchased '{item.displayName}' for {item.cost} credits.");
        return true;
    }

    /// <summary>
    /// Apply immediate effects when an item is purchased
    /// Delayed effects are handled by setting Yarn flags for other systems to process
    /// </summary>
    private void ApplyItemEffects(StoreItem item)
    {
        if (variableStorage == null || item == null) return;

        int currentRun = GetCurrentRun();
        int currentDay = GetCurrentDay();

        switch (item.id)
        {
            case "item_mental_break":
                AdjustSanity(10f);
                break;

            case "item_blackout_curtains":
                AdjustSanity(14f);
                SetBool("$store_blackout_pending", true);
                break;

            case "item_blue_light_filter":
                ScheduleBlueLightEffects(currentRun, currentDay);
                break;

            case "item_screen_protector":
                SetFloat("$store_screen_protector_heat_modifier", -1f);
                break;

            case "item_priority_shipping":
                // Flag is set via ownership flag, no additional effect needed
                break;

            case "item_bow_for_alice":
                // Effect handled by engagement tracking system
                break;

            case "item_corporate_bond":
                ScheduleCorporateBond(currentRun, currentDay, item.cost);
                break;
        }
    }

    /// <summary>
    /// Schedule blue light filter effects for tomorrow
    /// </summary>
    private void ScheduleBlueLightEffects(int currentRun, int currentDay)
    {
        SetBool("$store_blue_filter_active", true);
        int targetRun = currentRun;
        int targetDay = currentDay + 1;

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

    /// <summary>
    /// Schedule corporate bond payout for next day
    /// </summary>
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

    // Helper methods for Yarn variable access
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

    private void SetFloat(string variableName, float value)
    {
        variableStorage?.SetValue(variableName, value);
    }

    private void SetBool(string variableName, bool value)
    {
        variableStorage?.SetValue(variableName, value);
    }

    /// <summary>
    /// Get current run number
    /// </summary>
    public int GetCurrentRun()
    {
        return Mathf.Max(1, Mathf.RoundToInt(GetFloat("$current_run", 1f)));
    }

    /// <summary>
    /// Get current day number
    /// </summary>
    public int GetCurrentDay()
    {
        return Mathf.Max(1, Mathf.RoundToInt(GetFloat("$current_day", 1f)));
    }

    private void AdjustSanity(float delta)
    {
        float sanity = GetFloat("$sanity", 0f);
        SetFloat("$sanity", sanity + delta);
    }
}

