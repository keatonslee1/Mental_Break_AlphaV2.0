using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

/// <summary>
/// Helper script to automatically map abstract SFX keys to available audio clips.
/// Access via: Tools > Yarn Spinner > Auto-Map SFX Keys
/// </summary>
public class AutoMapSFX : EditorWindow
{
    [MenuItem("Tools/Yarn Spinner/Auto-Map SFX Keys")]
    public static void ShowWindow()
    {
        GetWindow<AutoMapSFX>("Auto-Map SFX Keys");
    }

    void OnGUI()
    {
        GUILayout.Label("SFX Key Auto-Mapping", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("This will automatically map abstract SFX keys (like 'sfx_censor_beep')");
        GUILayout.Label("to available audio clips based on best-match suggestions.");
        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Map Missing SFX Keys", GUILayout.Height(30)))
        {
            MapSFXKeys();
        }

        GUILayout.Space(10);
        GUILayout.Label("Note: This attempts to match keys to similar-named clips.");
        GUILayout.Label("Review the mappings in the AudioCommandHandler Inspector.");
    }

    private static void MapSFXKeys()
    {
        // Find AudioCommandHandler
        AudioCommandHandler handler = FindAnyObjectByType<AudioCommandHandler>();
        if (handler == null)
        {
            Debug.LogError("AudioCommandHandler not found! Run setup first.");
            return;
        }

        // Required SFX keys from Yarn files
        Dictionary<string, string[]> sfxMappings = new Dictionary<string, string[]>
        {
            // Key: abstract SFX key, Value: array of suggested clip keys to try (in priority order)
            { "sfx_censor_beep", new[] { "interface_error_click_01", "bright_popup", "menu_notification" } },
            { "sfx_shortwave_hiss", new[] { "unintelligible_female_radio_voice", "robotic_gibberish_comms", "ai_voice_connected_and_disconnected" } },
            { "sfx_cross_talk", new[] { "unintelligible_female_radio_voice", "robotic_gibberish_comms", "human_vocal_male_gibberish_scandinavian" } },
            { "sfx_patch_apply", new[] { "click", "menu_button", "swish_pop_up_1" } },
            { "sfx_patch_alert", new[] { "interface_error_click", "menu_notification", "bright_popup" } },
            { "sfx_vote_pass", new[] { "bright_popup", "swish_pop_up_1", "collect_gold", "menu_notification" } },
            { "sfx_vote_fail", new[] { "interface_error_click", "reverse_infographics", "reverse_swish" } },
            { "sfx_rank_up", new[] { "bright_popup", "collect_gold", "swish_pop_up_1", "menu_notification" } },
            { "sfx_rank_down", new[] { "reverse_infographics", "interface_error_click", "reverse_swish" } }
        };

        // Build dictionary of available clips by key
        Dictionary<string, AudioClip> availableClips = new Dictionary<string, AudioClip>();
        foreach (var entry in handler.sfxClips)
        {
            if (entry != null && entry.clip != null && !string.IsNullOrEmpty(entry.key))
            {
                availableClips[entry.key] = entry.clip;
            }
        }

        int mappedCount = 0;
        int existingCount = 0;

        foreach (var mapping in sfxMappings)
        {
            string abstractKey = mapping.Key;
            string[] suggestedKeys = mapping.Value;

            // Check if already mapped
            bool alreadyMapped = handler.sfxClips.Any(e => e != null && e.key == abstractKey && e.clip != null);
            if (alreadyMapped)
            {
                existingCount++;
                continue;
            }

            // Try to find a matching clip
            AudioClip matchedClip = null;
            foreach (string suggestedKey in suggestedKeys)
            {
                // Try exact match first
                if (availableClips.TryGetValue(suggestedKey, out matchedClip))
                {
                    break;
                }

                // Try partial match (contains)
                var partialMatch = availableClips.FirstOrDefault(kvp => 
                    kvp.Key.Contains(suggestedKey.Replace("_", ""), System.StringComparison.OrdinalIgnoreCase) ||
                    suggestedKey.Contains(kvp.Key.Replace("_", ""), System.StringComparison.OrdinalIgnoreCase)
                );
                if (partialMatch.Value != null)
                {
                    matchedClip = partialMatch.Value;
                    Debug.Log($"Partial match: {abstractKey} -> {partialMatch.Key}");
                    break;
                }

                // Try fuzzy match (similar words)
                var fuzzyMatch = availableClips.FirstOrDefault(kvp =>
                    ContainsSimilarWords(kvp.Key, suggestedKey)
                );
                if (fuzzyMatch.Value != null)
                {
                    matchedClip = fuzzyMatch.Value;
                    Debug.Log($"Fuzzy match: {abstractKey} -> {fuzzyMatch.Key}");
                    break;
                }
            }

            if (matchedClip != null)
            {
                // Add or update entry
                var existingEntry = handler.sfxClips.FirstOrDefault(e => e != null && e.key == abstractKey);
                if (existingEntry != null)
                {
                    existingEntry.clip = matchedClip;
                }
                else
                {
                    handler.sfxClips.Add(new AudioCommandHandler.AudioClipEntry
                    {
                        key = abstractKey,
                        clip = matchedClip
                    });
                }
                mappedCount++;
                Debug.Log($"✅ Mapped {abstractKey} -> {matchedClip.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not find suitable clip for {abstractKey}. Suggestions tried: {string.Join(", ", suggestedKeys)}");
            }
        }

        // Mark scene as dirty
        EditorUtility.SetDirty(handler);
        if (handler.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(handler.gameObject.scene);
        }

        Debug.Log($"SFX Mapping Complete: {mappedCount} new mappings, {existingCount} already mapped, {sfxMappings.Count - mappedCount - existingCount} not found.");
    }

    private static bool ContainsSimilarWords(string key1, string key2)
    {
        // Extract meaningful words (split by underscore, ignore common words)
        string[] words1 = key1.Split('_').Where(w => w.Length > 2 && !IsCommonWord(w)).ToArray();
        string[] words2 = key2.Split('_').Where(w => w.Length > 2 && !IsCommonWord(w)).ToArray();

        // Check if they share significant words
        int matches = words1.Count(w1 => words2.Any(w2 => 
            w1.Equals(w2, System.StringComparison.OrdinalIgnoreCase) ||
            w1.Contains(w2, System.StringComparison.OrdinalIgnoreCase) ||
            w2.Contains(w1, System.StringComparison.OrdinalIgnoreCase)
        ));

        return matches > 0 && matches >= Mathf.Min(words1.Length, words2.Length) / 2;
    }

    private static bool IsCommonWord(string word)
    {
        string[] commonWords = { "the", "and", "for", "with", "sfx", "audio", "sound", "clip", "file", "pack", "version" };
        return commonWords.Contains(word.ToLower());
    }
}
