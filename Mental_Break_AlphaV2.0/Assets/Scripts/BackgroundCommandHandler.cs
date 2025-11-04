using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

/// <summary>
/// Handles background image commands from Yarn scripts.
/// Command: <<bg key>>
/// </summary>
public class BackgroundCommandHandler : MonoBehaviour
{
    [Header("Background Image")]
    [Tooltip("Image component that displays background images")]
    public Image backgroundImage;
    
    [Header("Background Sprites")]
    [Tooltip("Assign background sprites to their keys here")]
    public List<SpriteEntry> backgroundSprites = new List<SpriteEntry>();
    
    // Cache dictionary for fast lookup
    private Dictionary<string, Sprite> spriteDictionary;
    
    [System.Serializable]
    public class SpriteEntry
    {
        public string key;
        public Sprite sprite;
    }
    
    void Awake()
    {
        BuildDictionary();
        
        // Find Image component if not assigned
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
            if (backgroundImage == null)
            {
                Debug.LogWarning("BackgroundCommandHandler: No Image component found. Please assign one in the Inspector.");
            }
        }
    }
    
    void BuildDictionary()
    {
        spriteDictionary = new Dictionary<string, Sprite>();
        
        foreach (var entry in backgroundSprites)
        {
            if (entry.sprite != null && !string.IsNullOrEmpty(entry.key))
            {
                spriteDictionary[entry.key] = entry.sprite;
            }
        }
    }
    
    /// <summary>
    /// Handles <<bg key>> commands from Yarn scripts
    /// Usage in Yarn: <<bg bg_office>>
    /// </summary>
    [YarnCommand("bg")]
    public void ChangeBackground(string key)
    {
        if (spriteDictionary == null)
        {
            BuildDictionary();
        }
        
        // Try exact match first
        if (!spriteDictionary.TryGetValue(key, out Sprite newSprite))
        {
            // Try case-insensitive match
            var caseInsensitiveMatch = spriteDictionary.FirstOrDefault(kvp => 
                kvp.Key.Equals(key, System.StringComparison.OrdinalIgnoreCase));
            if (caseInsensitiveMatch.Value != null)
            {
                newSprite = caseInsensitiveMatch.Value;
            }
            else
            {
                // Try partial match
                var partialMatch = spriteDictionary.FirstOrDefault(kvp =>
                    kvp.Key.Contains(key, System.StringComparison.OrdinalIgnoreCase) ||
                    key.Contains(kvp.Key, System.StringComparison.OrdinalIgnoreCase));
                if (partialMatch.Value != null)
                {
                    newSprite = partialMatch.Value;
                    Debug.LogWarning($"Background Command: Using partial match for key '{key}' -> '{partialMatch.Key}'. Consider updating Yarn file to use exact key.");
                }
            }
        }
        
        if (newSprite != null)
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = newSprite;
                Debug.Log($"Background: Changed to {key} (sprite: {newSprite.name})");
            }
            else
            {
                Debug.LogWarning($"Background Command: No Image component assigned for key '{key}'");
            }
        }
        else
        {
            Debug.LogWarning($"Background Command: No sprite found for key '{key}'. Available keys: {string.Join(", ", spriteDictionary.Keys.Take(5))}...");
        }
    }
    
    // Editor helper method
    void OnValidate()
    {
        // Rebuild dictionary when changes are made in the editor
        if (Application.isPlaying)
        {
            BuildDictionary();
        }
    }
}

