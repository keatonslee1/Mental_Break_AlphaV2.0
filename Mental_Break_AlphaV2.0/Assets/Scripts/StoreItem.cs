using UnityEngine;

/// <summary>
/// Data class representing a store item available for purchase
/// </summary>
[System.Serializable]
public class StoreItem
{
    [Tooltip("Unique identifier matching Yarn variable name (e.g., 'item_mental_break')")]
    public string id;

    [Tooltip("Display name shown to player")]
    public string displayName;

    [Tooltip("Cost in credits")]
    public int cost;

    [Tooltip("Description shown to player")]
    [TextArea(2, 4)]
    public string description;
}

