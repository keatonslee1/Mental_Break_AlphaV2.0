using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor tool to automatically map BGM audio clips from Assets/Audio/BGM/ to AudioCommandHandler.
/// Access via: Tools > Yarn Spinner > Auto-Map BGM
/// </summary>
public class AutoMapBGM : EditorWindow
{
    [MenuItem("Tools/Yarn Spinner/Auto-Map BGM")]
    public static void ShowWindow()
    {
        GetWindow<AutoMapBGM>("Auto-Map BGM");
    }

    void OnGUI()
    {
        GUILayout.Label("BGM Audio Clip Auto-Mapping", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("This will automatically map BGM audio clips from Assets/Audio/BGM/");
        GUILayout.Label("to AudioCommandHandler based on filename patterns.");
        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Map BGM Clips", GUILayout.Height(30)))
        {
            MapBGMClips();
        }

        GUILayout.Space(10);
        GUILayout.Label("Note: This scans nested folders and maps clips based on filenames.");
        GUILayout.Label("Review the mappings in the AudioCommandHandler Inspector.");
    }

    private static void MapBGMClips()
    {
        // Find AudioCommandHandler
        AudioCommandHandler handler = FindAnyObjectByType<AudioCommandHandler>();
        if (handler == null)
        {
            Debug.LogError("AudioCommandHandler not found! Please ensure GameScene is open and AudioCommandHandler exists.");
            return;
        }

        // Find all audio clips in Assets/Audio/BGM/
        string bgmPath = "Assets/Audio/BGM";
        string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { bgmPath });
        
        if (audioGuids.Length == 0)
        {
            Debug.LogWarning($"No audio clips found in {bgmPath}");
            return;
        }

        // Build dictionary of existing clips by key
        Dictionary<string, AudioCommandHandler.AudioClipEntry> existingEntries = 
            handler.bgmClips.ToDictionary(e => e?.key ?? "", e => e);

        int mappedCount = 0;
        int existingCount = 0;

        // Map common BGM keys from Yarn files
        Dictionary<string, string[]> bgmMappings = new Dictionary<string, string[]>
        {
            // Key: abstract BGM key (as used in Yarn), Value: array of filename patterns to match
            { "cyberpunk_background_cyberpunk_background_main", new[] { "Cyberpunk Background Main", "cyberpunk_background_main", "Main" } },
            { "cyberpunk_background_main", new[] { "Cyberpunk Background Main", "cyberpunk_background_main", "Main" } },
            { "cyberpunk_background", new[] { "Cyberpunk Background", "cyberpunk_background", "cyberpunk" } },
            { "cyberpunk_background_short", new[] { "Cyberpunk Background Short", "Short" } },
            { "darkwave_intro", new[] { "Intro Version", "darkwave_intro", "Intro" } },
            { "darkwave_short", new[] { "Short Version", "darkwave_short", "Short" } },
            { "darkwave", new[] { "Difourks Darkwave", "darkwave" } },
            { "dark_synthwave", new[] { "Dark Synthwave", "dark_synthwave" } },
            { "synthwave_cyberpunk_blues", new[] { "Synthwave Cyberpunk Blues", "synthwave_cyberpunk_blues" } },
            { "deep_space", new[] { "Deep Space", "deep_space" } },
            { "dark_matter", new[] { "Dark Matter", "dark_matter" } },
            { "hospital_room", new[] { "Hospital Room", "hospital_room" } },
            { "beaming_light", new[] { "Beaming Light", "beaming_light" } },
            { "military_loudspeaker", new[] { "Military Loudspeaker", "military_loudspeaker", "Gibberish" } }
        };

        // Load all audio clips
        Dictionary<string, AudioClip> availableClips = new Dictionary<string, AudioClip>();
        foreach (string guid in audioGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null)
            {
                string clipName = clip.name.ToLower();
                availableClips[clipName] = clip;
                
                // Also index by path segments (for nested folders)
                string[] pathParts = assetPath.Split('/');
                foreach (string part in pathParts)
                {
                    string partLower = part.ToLower();
                    if (!availableClips.ContainsKey(partLower) && partLower.Contains(".wav") || partLower.Contains(".mp3"))
                    {
                        // Extract name from file
                    }
                    else if (!partLower.Contains(".") && partLower != "audio" && partLower != "bgm")
                    {
                        // Folder name as potential key
                        if (!availableClips.ContainsKey(partLower + "_" + clipName))
                        {
                            availableClips[partLower + "_" + clipName] = clip;
                        }
                    }
                }
            }
        }

        // Try to map each BGM key
        foreach (var mapping in bgmMappings)
        {
            string abstractKey = mapping.Key;
            string[] patterns = mapping.Value;

            // Check if already mapped
            if (existingEntries.ContainsKey(abstractKey) && existingEntries[abstractKey].clip != null)
            {
                existingCount++;
                continue;
            }

            // Try to find matching clip
            AudioClip matchedClip = null;
            foreach (string pattern in patterns)
            {
                string patternLower = pattern.ToLower();
                
                // Try exact match
                if (availableClips.TryGetValue(patternLower, out matchedClip))
                {
                    break;
                }

                // Try partial match (clip name contains pattern)
                var partialMatch = availableClips.FirstOrDefault(kvp =>
                    kvp.Key.Contains(patternLower) || patternLower.Contains(kvp.Key)
                );
                if (partialMatch.Value != null)
                {
                    matchedClip = partialMatch.Value;
                    Debug.Log($"Partial BGM match: {abstractKey} -> {partialMatch.Key}");
                    break;
                }

                // Try fuzzy match on clip names
                var fuzzyMatch = availableClips.FirstOrDefault(kvp =>
                    ContainsSimilarWords(kvp.Key, patternLower)
                );
                if (fuzzyMatch.Value != null)
                {
                    matchedClip = fuzzyMatch.Value;
                    Debug.Log($"Fuzzy BGM match: {abstractKey} -> {fuzzyMatch.Key}");
                    break;
                }
            }

            if (matchedClip != null)
            {
                // Add or update entry
                if (existingEntries.ContainsKey(abstractKey))
                {
                    existingEntries[abstractKey].clip = matchedClip;
                }
                else
                {
                    handler.bgmClips.Add(new AudioCommandHandler.AudioClipEntry
                    {
                        key = abstractKey,
                        clip = matchedClip
                    });
                }
                mappedCount++;
                Debug.Log($"✅ Mapped BGM {abstractKey} -> {matchedClip.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not find suitable clip for BGM key '{abstractKey}'. Patterns tried: {string.Join(", ", patterns)}");
            }
        }

        // Also try to auto-map any remaining clips based on filename
        foreach (var clipPair in availableClips)
        {
            string clipKey = GenerateKeyFromFilename(clipPair.Value.name);
            if (!string.IsNullOrEmpty(clipKey) && 
                !handler.bgmClips.Any(e => e != null && e.key == clipKey))
            {
                handler.bgmClips.Add(new AudioCommandHandler.AudioClipEntry
                {
                    key = clipKey,
                    clip = clipPair.Value
                });
                mappedCount++;
                Debug.Log($"✅ Auto-mapped BGM: {clipKey} -> {clipPair.Value.name}");
            }
        }

        // Mark scene as dirty
        EditorUtility.SetDirty(handler);
        if (handler.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(handler.gameObject.scene);
        }

        Debug.Log($"BGM Mapping Complete: {mappedCount} new mappings, {existingCount} already mapped.");
    }

    private static bool ContainsSimilarWords(string key1, string key2)
    {
        string[] words1 = key1.Split(new[] { '_', ' ', '-' }).Where(w => w.Length > 2).ToArray();
        string[] words2 = key2.Split(new[] { '_', ' ', '-' }).Where(w => w.Length > 2).ToArray();

        int matches = words1.Count(w1 => words2.Any(w2 =>
            w1.Equals(w2, System.StringComparison.OrdinalIgnoreCase) ||
            w1.Contains(w2, System.StringComparison.OrdinalIgnoreCase) ||
            w2.Contains(w1, System.StringComparison.OrdinalIgnoreCase)
        ));

        return matches > 0 && matches >= Mathf.Min(words1.Length, words2.Length) / 2;
    }

    private static string GenerateKeyFromFilename(string filename)
    {
        // Convert filename to key format (lowercase, replace spaces/underscores)
        string key = filename.ToLower();
        key = key.Replace(" ", "_");
        key = key.Replace("-", "_");
        
        // Remove file extension
        int extIndex = key.LastIndexOf('.');
        if (extIndex > 0)
        {
            key = key.Substring(0, extIndex);
        }

        // Ensure it starts with bgm_ or is a recognized pattern
        if (!key.StartsWith("bgm_"))
        {
            key = "bgm_" + key;
        }

        return key;
    }
}

