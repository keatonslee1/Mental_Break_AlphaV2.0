using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor tool to automatically set up the audio system.
/// Creates an Audio Manager GameObject with AudioCommandHandler and AudioSource components.
/// Access via: Tools > Yarn Spinner > Setup Audio System
/// </summary>
public class SetupAudioSystem
{
    private const string AUDIO_MANAGER_NAME = "Audio Manager";

    [MenuItem("Tools/Yarn Spinner/Setup Audio System")]
    public static void Setup()
    {
        // Get active scene
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("No valid scene is open. Please open a scene first.");
            return;
        }

        // Try to find existing Audio Manager
        GameObject audioManagerGO = GameObject.Find(AUDIO_MANAGER_NAME);
        
        if (audioManagerGO != null)
        {
            Debug.Log($"Found existing '{AUDIO_MANAGER_NAME}' GameObject. Verifying configuration...");
            
            // Verify it has AudioCommandHandler
            AudioCommandHandler existingHandler = audioManagerGO.GetComponent<AudioCommandHandler>();
            if (existingHandler == null)
            {
                existingHandler = audioManagerGO.AddComponent<AudioCommandHandler>();
                Debug.Log("Added AudioCommandHandler to existing Audio Manager");
            }

            // Verify configuration
            VerifyAndFixConfiguration(audioManagerGO, existingHandler);
            
            EditorUtility.SetDirty(audioManagerGO);
            EditorUtility.SetDirty(existingHandler);
            EditorSceneManager.MarkSceneDirty(scene);
            
            Debug.Log($"✅ Verified and configured existing '{AUDIO_MANAGER_NAME}' GameObject.");
            return;
        }

        // Try to find AudioCommandHandler on Dialogue System (in case it was manually added there)
        GameObject dialogueSystem = GameObject.Find("Dialogue System");
        if (dialogueSystem != null)
        {
            AudioCommandHandler existingHandlerOnDialogue = dialogueSystem.GetComponent<AudioCommandHandler>();
            if (existingHandlerOnDialogue != null)
            {
                Debug.Log($"Found AudioCommandHandler on Dialogue System. Verifying configuration...");
                VerifyAndFixConfiguration(dialogueSystem, existingHandlerOnDialogue);
                EditorUtility.SetDirty(dialogueSystem);
                EditorUtility.SetDirty(existingHandlerOnDialogue);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"✅ Verified and configured AudioCommandHandler on Dialogue System.");
                return;
            }
        }

        // Create new Audio Manager GameObject
        Debug.Log($"Creating new '{AUDIO_MANAGER_NAME}' GameObject...");
        audioManagerGO = new GameObject(AUDIO_MANAGER_NAME);

        // Add AudioCommandHandler component
        AudioCommandHandler handler = audioManagerGO.AddComponent<AudioCommandHandler>();
        Debug.Log("Added AudioCommandHandler component");

        // Create and configure BGM AudioSource
        AudioSource bgmSource = audioManagerGO.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = 0.5f; // Set BGM volume to half (0.5) to reduce loudness
        bgmSource.name = "BGM Source";
        Debug.Log("Created BGM AudioSource (loop=true, volume=0.5)");

        // Create and configure SFX AudioSource
        AudioSource sfxSource = audioManagerGO.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.name = "SFX Source";
        Debug.Log("Created SFX AudioSource (loop=false)");

        // Configure AudioCommandHandler to reference the AudioSources
        var handlerSO = new SerializedObject(handler);
        var bgmSourceProp = handlerSO.FindProperty("bgmSource");
        var sfxSourceProp = handlerSO.FindProperty("sfxSource");
        
        if (bgmSourceProp != null)
        {
            bgmSourceProp.objectReferenceValue = bgmSource;
            Debug.Log("Configured AudioCommandHandler BGM Source reference");
        }
        else
        {
            Debug.LogWarning("Could not find 'bgmSource' property on AudioCommandHandler. AudioSources will be created automatically in Awake().");
        }
        
        if (sfxSourceProp != null)
        {
            sfxSourceProp.objectReferenceValue = sfxSource;
            Debug.Log("Configured AudioCommandHandler SFX Source reference");
        }
        else
        {
            Debug.LogWarning("Could not find 'sfxSource' property on AudioCommandHandler. AudioSources will be created automatically in Awake().");
        }
        
        if (bgmSourceProp != null || sfxSourceProp != null)
        {
            handlerSO.ApplyModifiedProperties();
        }

        // Mark objects as dirty
        EditorUtility.SetDirty(audioManagerGO);
        EditorUtility.SetDirty(handler);
        EditorUtility.SetDirty(bgmSource);
        EditorUtility.SetDirty(sfxSource);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"✅ Successfully created and configured '{AUDIO_MANAGER_NAME}' GameObject!");
        Debug.Log("Next steps:");
        Debug.Log("1. Use 'Tools > Yarn Spinner > Auto-Map BGM' to map BGM audio clips");
        Debug.Log("2. Use 'Tools > Yarn Spinner > Auto-Map SFX Keys' to map SFX audio clips");
        Debug.Log("3. Or manually assign audio clips in the AudioCommandHandler Inspector");
    }

    /// <summary>
    /// Verifies and fixes the configuration of an existing AudioCommandHandler.
    /// </summary>
    private static void VerifyAndFixConfiguration(GameObject go, AudioCommandHandler handler)
    {
        bool needsUpdate = false;

        // Verify AudioSources exist and are configured correctly
        AudioSource[] allSources = go.GetComponents<AudioSource>();
        AudioSource bgmSource = null;
        AudioSource sfxSource = null;

        // Try to identify BGM and SFX sources by name or configuration
        foreach (AudioSource source in allSources)
        {
            if (source.name.Contains("BGM") || source.loop)
            {
                if (bgmSource == null)
                {
                    bgmSource = source;
                }
            }
            else if (source.name.Contains("SFX") || !source.loop)
            {
                if (sfxSource == null)
                {
                    sfxSource = source;
                }
            }
        }

        // If we only found one source, try to determine which it is based on properties
        if (allSources.Length == 1)
        {
            if (allSources[0].loop)
            {
                bgmSource = allSources[0];
            }
            else
            {
                sfxSource = allSources[0];
            }
        }

        // Create missing AudioSources
        if (bgmSource == null)
        {
            bgmSource = go.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 0.5f; // Set BGM volume to half (0.5) to reduce loudness
            bgmSource.name = "BGM Source";
            needsUpdate = true;
            Debug.Log("Created missing BGM AudioSource (volume=0.5)");
        }
        else
        {
            // Verify BGM configuration
            if (!bgmSource.loop)
            {
                bgmSource.loop = true;
                needsUpdate = true;
                Debug.Log("Fixed BGM AudioSource: enabled looping");
            }
            // Ensure volume is set correctly
            if (bgmSource.volume != 0.5f)
            {
                bgmSource.volume = 0.5f;
                needsUpdate = true;
                Debug.Log("Fixed BGM AudioSource: set volume to 0.5");
            }
        }

        if (sfxSource == null)
        {
            sfxSource = go.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.name = "SFX Source";
            needsUpdate = true;
            Debug.Log("Created missing SFX AudioSource");
        }
        else
        {
            // Verify SFX configuration
            if (sfxSource.loop)
            {
                sfxSource.loop = false;
                needsUpdate = true;
                Debug.Log("Fixed SFX AudioSource: disabled looping");
            }
        }

        // Verify AudioCommandHandler references
        var handlerSO = new SerializedObject(handler);
        var bgmSourceProp = handlerSO.FindProperty("bgmSource");
        var sfxSourceProp = handlerSO.FindProperty("sfxSource");
        
        if (bgmSourceProp != null && bgmSourceProp.objectReferenceValue != bgmSource)
        {
            bgmSourceProp.objectReferenceValue = bgmSource;
            needsUpdate = true;
            Debug.Log("Fixed AudioCommandHandler BGM Source reference");
        }
        
        if (sfxSourceProp != null && sfxSourceProp.objectReferenceValue != sfxSource)
        {
            sfxSourceProp.objectReferenceValue = sfxSource;
            needsUpdate = true;
            Debug.Log("Fixed AudioCommandHandler SFX Source reference");
        }
        
        if (needsUpdate && (bgmSourceProp != null || sfxSourceProp != null))
        {
            handlerSO.ApplyModifiedProperties();
        }

        if (needsUpdate)
        {
            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(handler);
            if (bgmSource != null) EditorUtility.SetDirty(bgmSource);
            if (sfxSource != null) EditorUtility.SetDirty(sfxSource);
        }
    }
}

