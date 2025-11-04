using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [Tooltip("Assign background sprites to their keys here (optional - will auto-load from Graphics/Backgrounds if empty)")]
    public List<SpriteEntry> backgroundSprites = new List<SpriteEntry>();
    
    [Header("Auto-Load Settings")]
    [Tooltip("Path to background sprites folder (relative to Assets/)")]
    public string backgroundFolderPath = "Graphics/Backgrounds";
    
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
        
        // First, add manually assigned sprites
        foreach (var entry in backgroundSprites)
        {
            if (entry.sprite != null && !string.IsNullOrEmpty(entry.key))
            {
                spriteDictionary[entry.key] = entry.sprite;
            }
        }
        
        // Then, try to auto-load from Resources or folder
        AutoLoadBackgrounds();
    }
    
    void AutoLoadBackgrounds()
    {
#if UNITY_EDITOR
        // In editor, load from folder path (works in both edit and play mode)
        LoadFromFolder();
        
        // Also try Resources as fallback
        LoadFromResources();
#else
        // Runtime: Only Resources.Load works
        LoadFromResources();
#endif
    }
    
    void LoadFromResources()
    {
        // Try Resources.Load with the folder path
        // Resources.Load only works if the folder is actually named "Resources"
        string resourcesPath = backgroundFolderPath.Replace("Assets/", "").Replace("\\", "/");
        
        // Remove "Resources/" prefix if present, since Resources.LoadAll expects path relative to Resources folder
        if (resourcesPath.StartsWith("Resources/", System.StringComparison.OrdinalIgnoreCase))
        {
            resourcesPath = resourcesPath.Substring("Resources/".Length);
        }
        
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesPath);
        
        if (sprites != null && sprites.Length > 0)
        {
            foreach (var sprite in sprites)
            {
                if (sprite != null)
                {
                    // Extract key from sprite name, handling sprite sheet suffixes like "_0"
                    string key = ExtractKeyFromSpriteName(sprite.name);
                    
                    // Only add if not already in dictionary (manual assignments take precedence)
                    if (!spriteDictionary.ContainsKey(key))
                    {
                        spriteDictionary[key] = sprite;
                        Debug.Log($"BackgroundCommandHandler: Auto-loaded background '{key}' from Resources (sprite: {sprite.name})");
                    }
                }
            }
        }
    }
    
#if UNITY_EDITOR
    void LoadFromFolder()
    {
        string fullPath = "Assets/" + backgroundFolderPath;
        
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            // Don't warn if Resources loading might work instead
            return;
        }
        
        // Find all texture files in the folder (not just sprites, since they might be imported as textures)
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { fullPath });
        
        foreach (string guid in textureGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            
            // Extract key from file name (not sprite name)
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLower();
            string key = ExtractKeyFromFileName(fileName);
            
            // Skip if already in dictionary (manual assignments take precedence)
            if (spriteDictionary.ContainsKey(key))
            {
                continue;
            }
            
            // Load the sprite - try to get all sprites from the texture (for sprite sheets)
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Sprite targetSprite = null;
            
            // Find the first sprite from the texture
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    targetSprite = sprite;
                    break; // Use the first sprite found (usually the main one)
                }
            }
            
            if (targetSprite != null)
            {
                spriteDictionary[key] = targetSprite;
                Debug.Log($"BackgroundCommandHandler: Auto-loaded background '{key}' from {assetPath} (sprite: {targetSprite.name})");
            }
        }
    }
#endif
    
    /// <summary>
    /// Extracts a background key from a sprite name, handling sprite sheet suffixes like "_0"
    /// </summary>
    string ExtractKeyFromSpriteName(string spriteName)
    {
        string key = spriteName.ToLower();
        
        // Remove sprite sheet suffixes like "_0", "_1", etc.
        if (System.Text.RegularExpressions.Regex.IsMatch(key, @"_\d+$"))
        {
            int lastUnderscore = key.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                string suffix = key.Substring(lastUnderscore + 1);
                if (int.TryParse(suffix, out _))
                {
                    key = key.Substring(0, lastUnderscore);
                }
            }
        }
        
        // Normalize key to match yarn format (bg_conferenceroom)
        if (!key.StartsWith("bg_"))
        {
            key = "bg_" + key;
        }
        
        return key;
    }
    
    /// <summary>
    /// Extracts a background key from a file name
    /// </summary>
    string ExtractKeyFromFileName(string fileName)
    {
        string key = fileName.ToLower();
        
        // Normalize key to match yarn format (bg_conferenceroom)
        if (!key.StartsWith("bg_"))
        {
            key = "bg_" + key;
        }
        
        return key;
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
        
        // Normalize the key to lowercase for matching
        string normalizedKey = key.ToLower();
        
        // Try exact match first
        if (!spriteDictionary.TryGetValue(normalizedKey, out Sprite newSprite))
        {
            // Try case-insensitive match
            var caseInsensitiveMatch = spriteDictionary.FirstOrDefault(kvp => 
                kvp.Key.Equals(normalizedKey, System.StringComparison.OrdinalIgnoreCase));
            if (caseInsensitiveMatch.Value != null)
            {
                newSprite = caseInsensitiveMatch.Value;
            }
            else
            {
                // Try matching with sprite sheet suffixes (e.g., bg_conferenceroom_0)
                var spriteSheetMatch = spriteDictionary.FirstOrDefault(kvp =>
                {
                    string baseKey = ExtractKeyFromSpriteName(kvp.Key);
                    return baseKey.Equals(normalizedKey, System.StringComparison.OrdinalIgnoreCase);
                });
                if (spriteSheetMatch.Value != null)
                {
                    newSprite = spriteSheetMatch.Value;
                }
                else
                {
                    // Try partial match as last resort
                    var partialMatch = spriteDictionary.FirstOrDefault(kvp =>
                        kvp.Key.Contains(normalizedKey, System.StringComparison.OrdinalIgnoreCase) ||
                        normalizedKey.Contains(kvp.Key, System.StringComparison.OrdinalIgnoreCase));
                    if (partialMatch.Value != null)
                    {
                        newSprite = partialMatch.Value;
                        Debug.LogWarning($"Background Command: Using partial match for key '{key}' -> '{partialMatch.Key}'. Consider updating Yarn file to use exact key.");
                    }
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
            Debug.LogWarning($"Background Command: No sprite found for key '{key}'. Available keys: {string.Join(", ", spriteDictionary.Keys.Take(10))}...");
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

