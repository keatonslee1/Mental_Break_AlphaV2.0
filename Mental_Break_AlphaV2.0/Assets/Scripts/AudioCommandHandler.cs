using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Handles audio commands from Yarn scripts.
/// Commands: <<bgm key>> and <<sfx key>>
/// </summary>
public class AudioCommandHandler : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("AudioSource component for playing background music")]
    public AudioSource bgmSource;
    
    [Tooltip("AudioSource component for playing sound effects")]
    public AudioSource sfxSource;
    
    [Header("Audio Clips - BGM")]
    [Tooltip("Assign BGM audio clips to their keys here")]
    public List<AudioClipEntry> bgmClips = new List<AudioClipEntry>();
    
    [Header("Audio Clips - SFX")]
    [Tooltip("Assign SFX audio clips to their keys here")]
    public List<AudioClipEntry> sfxClips = new List<AudioClipEntry>();
    
    // Cache dictionaries for fast lookup
    private Dictionary<string, AudioClip> bgmDictionary;
    private Dictionary<string, AudioClip> sfxDictionary;
    
    [System.Serializable]
    public class AudioClipEntry
    {
        public string key;
        public AudioClip clip;
    }
    
    void Awake()
    {
        // Initialize dictionaries from lists
        BuildDictionaries();
        
        // Create AudioSource components if not assigned
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true; // BGM typically loops
        }
        bgmSource.volume = 0.5f; // Set BGM volume to half (0.5) to reduce loudness
        
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false; // SFX typically don't loop
        }
    }
    
    void BuildDictionaries()
    {
        bgmDictionary = new Dictionary<string, AudioClip>();
        sfxDictionary = new Dictionary<string, AudioClip>();
        
        foreach (var entry in bgmClips)
        {
            if (entry.clip != null && !string.IsNullOrEmpty(entry.key))
            {
                bgmDictionary[entry.key] = entry.clip;
            }
        }
        
        foreach (var entry in sfxClips)
        {
            if (entry.clip != null && !string.IsNullOrEmpty(entry.key))
            {
                sfxDictionary[entry.key] = entry.clip;
            }
        }
    }
    
    /// <summary>
    /// Handles <<bgm key>> commands from Yarn scripts
    /// Usage in Yarn: <<bgm bgm_theme_office>>
    /// </summary>
    [YarnCommand("bgm")]
    public void PlayBGM(string key)
    {
        if (bgmDictionary == null)
        {
            BuildDictionaries();
        }
        
        // Try exact match first
        if (!bgmDictionary.TryGetValue(key, out AudioClip clip))
        {
            // Try case-insensitive match
            var caseInsensitiveMatch = bgmDictionary.FirstOrDefault(kvp => 
                kvp.Key.Equals(key, System.StringComparison.OrdinalIgnoreCase));
            if (caseInsensitiveMatch.Value != null)
            {
                clip = caseInsensitiveMatch.Value;
            }
            else
            {
                // Try partial match (key contains or is contained by dictionary key)
                var partialMatch = bgmDictionary.FirstOrDefault(kvp =>
                    kvp.Key.Contains(key, System.StringComparison.OrdinalIgnoreCase) ||
                    key.Contains(kvp.Key, System.StringComparison.OrdinalIgnoreCase));
                if (partialMatch.Value != null)
                {
                    clip = partialMatch.Value;
                    Debug.LogWarning($"BGM Command: Using partial match for key '{key}' -> '{partialMatch.Key}'. Consider updating Yarn file to use exact key.");
                }
            }
        }
        
        if (clip != null)
        {
            if (bgmSource != null)
            {
                bgmSource.clip = clip;
                bgmSource.Play();
                Debug.Log($"BGM: Playing {key} (clip: {clip.name})");
            }
            else
            {
                Debug.LogWarning($"BGM Command: No BGM AudioSource assigned for key '{key}'");
            }
        }
        else
        {
            Debug.LogWarning($"BGM Command: No audio clip found for key '{key}'. Available keys: {string.Join(", ", bgmDictionary.Keys.Take(5))}...");
        }
    }
    
    /// <summary>
    /// Handles <<sfx key>> commands from Yarn scripts
    /// Usage in Yarn: <<sfx sfx_censor_beep>>
    /// </summary>
    [YarnCommand("sfx")]
    public void PlaySFX(string key)
    {
        if (sfxDictionary == null)
        {
            BuildDictionaries();
        }
        
        if (sfxDictionary.TryGetValue(key, out AudioClip clip))
        {
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
                Debug.Log($"SFX: Playing {key}");
            }
            else
            {
                Debug.LogWarning($"SFX Command: No SFX AudioSource assigned for key '{key}'");
            }
        }
        else
        {
            Debug.LogWarning($"SFX Command: No audio clip found for key '{key}'");
        }
    }
    
    // Editor helper method
    void OnValidate()
    {
        // Rebuild dictionaries when changes are made in the editor
        if (Application.isPlaying)
        {
            BuildDictionaries();
        }
    }
}

