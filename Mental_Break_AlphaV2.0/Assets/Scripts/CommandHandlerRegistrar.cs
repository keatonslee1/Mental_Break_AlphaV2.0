using UnityEngine;
using Yarn.Unity;
using System.Reflection;
using System.Collections;

/// <summary>
/// Ensures command handlers are properly registered with the DialogueRunner.
/// This script manually registers [YarnCommand] methods to ensure they're discovered.
/// </summary>
public class CommandHandlerRegistrar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("DialogueRunner to register commands with")]
    public DialogueRunner dialogueRunner;

    [Header("Command Handlers")]
    [Tooltip("BackgroundCommandHandler component")]
    public BackgroundCommandHandler backgroundHandler;

    [Tooltip("AudioCommandHandler component")]
    public AudioCommandHandler audioHandler;

    [Tooltip("CheckpointCommandHandler component")]
    public CheckpointCommandHandler checkpointHandler;

    [Tooltip("StoreUI component")]
    public StoreUI storeHandler;

    private void Awake()
    {
        // Find DialogueRunner if not assigned
        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }

        // Find command handlers if not assigned
        if (backgroundHandler == null)
        {
            backgroundHandler = FindFirstObjectByType<BackgroundCommandHandler>();
        }

        if (audioHandler == null)
        {
            audioHandler = FindFirstObjectByType<AudioCommandHandler>();
        }

        if (checkpointHandler == null)
        {
            checkpointHandler = FindFirstObjectByType<CheckpointCommandHandler>();
        }

        if (storeHandler == null)
        {
            storeHandler = ResolveStoreHandler();
        }
    }

    /// <summary>
    /// Attempt to locate the StoreUI component, including on inactive objects.
    /// </summary>
    private StoreUI ResolveStoreHandler()
    {
        if (storeHandler != null)
        {
            return storeHandler;
        }

#if UNITY_2022_2_OR_NEWER
        storeHandler = FindFirstObjectByType<StoreUI>(FindObjectsInactive.Include);
        if (storeHandler != null)
        {
            return storeHandler;
        }
#endif

        // Fallback for older Unity versions
        var storeCandidates = FindObjectsOfType<StoreUI>(true);
        foreach (var candidate in storeCandidates)
        {
            // Skip prefabs or assets not in a valid scene
            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            storeHandler = candidate;
            break;
        }

        return storeHandler;
    }

    private void Start()
    {
        // Double-check registration in Start() in case OnEnable() didn't run (component added at runtime)
        // This is safe because RemoveCommandHandler is safe to call even if not registered
        RegisterCommands();

        // In WebGL, SceneManagers can be stripped; ensure the store handler exists and registers (again) after start.
        StartCoroutine(DeferredStoreRegistration());
    }

    private System.Collections.IEnumerator DeferredStoreRegistration()
    {
        // Wait until end of frame to ensure StoreUI canvas objects have instantiated.
        yield return null;
        yield return new WaitForEndOfFrame();

        if (storeHandler == null || !storeHandler.gameObject.scene.IsValid())
        {
            storeHandler = ResolveStoreHandler();
        }

        if (storeHandler == null)
        {
            Debug.LogWarning("CommandHandlerRegistrar: Deferred store registration skipped; StoreUI still missing.");
            yield break;
        }

        try
        {
            dialogueRunner.RemoveCommandHandler("store");
        }
        catch { }

        try
        {
            dialogueRunner.AddCommandHandler("store", new System.Func<System.Collections.IEnumerator>(storeHandler.OpenStore));
            Debug.Log("CommandHandlerRegistrar: Deferred 'store' command registration succeeded.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CommandHandlerRegistrar: Deferred 'store' registration failed: {e.Message}");
        }
    }

    /// <summary>
    /// Manually register command handlers with the DialogueRunner
    /// </summary>
    private void RegisterCommands()
    {
        if (dialogueRunner == null)
        {
            Debug.LogError("CommandHandlerRegistrar: DialogueRunner not found!");
            return;
        }

        // Ensure DialogueRunner has YarnProject (needed for command dispatcher initialization)
        if (dialogueRunner.YarnProject == null)
        {
            Debug.LogWarning("CommandHandlerRegistrar: DialogueRunner.YarnProject is null. Commands may not work correctly.");
        }

        bool allRegistered = true;

        // Register BackgroundCommandHandler method via bound delegate
        if (backgroundHandler != null)
        {
            try
            {
                // Remove if already registered (safe to call)
                try { dialogueRunner.RemoveCommandHandler("bg"); } catch { }
                dialogueRunner.AddCommandHandler("bg", new System.Action<string>(backgroundHandler.ChangeBackground));
                Debug.Log("CommandHandlerRegistrar: Registered 'bg' command");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CommandHandlerRegistrar: Failed to register 'bg': {e.Message}");
                allRegistered = false;
            }
        }
        else
        {
            Debug.LogWarning("CommandHandlerRegistrar: BackgroundCommandHandler not found!");
            allRegistered = false;
        }

        // Register AudioCommandHandler methods via bound delegates
        if (audioHandler != null)
        {
            try
            {
                // Remove if already registered (safe to call)
                try { dialogueRunner.RemoveCommandHandler("bgm"); } catch { }
                try { dialogueRunner.RemoveCommandHandler("sfx"); } catch { }
                dialogueRunner.AddCommandHandler("bgm", new System.Action<string>(audioHandler.PlayBGM));
                dialogueRunner.AddCommandHandler("sfx", new System.Action<string>(audioHandler.PlaySFX));
                Debug.Log("CommandHandlerRegistrar: Registered 'bgm' and 'sfx' commands");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CommandHandlerRegistrar: Failed to register audio commands: {e.Message}");
                allRegistered = false;
            }
        }
        else
        {
            Debug.LogWarning("CommandHandlerRegistrar: AudioCommandHandler not found!");
            allRegistered = false;
        }

        // Register CheckpointCommandHandler method via bound delegate
        // Note: The [YarnCommand] attribute should auto-discover this, but explicit registration ensures it works
        if (checkpointHandler != null)
        {
            try
            {
                // Remove if already registered (safe to call)
                try { dialogueRunner.RemoveCommandHandler("checkpoint"); } catch { }
                dialogueRunner.AddCommandHandler("checkpoint", new System.Action<string>(checkpointHandler.Checkpoint));
                Debug.Log("CommandHandlerRegistrar: Registered 'checkpoint' command");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CommandHandlerRegistrar: Failed to register 'checkpoint': {e.Message}");
                allRegistered = false;
            }
        }
        else
        {
            Debug.LogWarning("CommandHandlerRegistrar: CheckpointCommandHandler not found!");
            // Don't mark as failed - checkpoint is optional for backwards compatibility
        }

        // Register StoreUI method via bound delegate
        // Note: OpenStore returns IEnumerator, so we use Func<IEnumerator> instead of Action
        var resolvedStore = ResolveStoreHandler();
        if (resolvedStore != null)
        {
            try
            {
                // Remove if already registered (safe to call)
                try { dialogueRunner.RemoveCommandHandler("store"); } catch { }
                dialogueRunner.AddCommandHandler("store", new System.Func<IEnumerator>(resolvedStore.OpenStore));
                Debug.Log("CommandHandlerRegistrar: Registered 'store' command");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CommandHandlerRegistrar: Failed to register 'store': {e.Message}");
                allRegistered = false;
            }
        }
        else
        {
            Debug.LogWarning("CommandHandlerRegistrar: StoreUI not found!");
            // Don't mark as failed - store is optional if UI isn't set up yet
        }

        if (allRegistered)
        {
            Debug.Log("CommandHandlerRegistrar: All commands registered successfully!");
        }
    }

    // Note: using bound delegates above ensures Yarn does not interpret the first
    // parameter as a GameObject target; instead, the string is passed to the
    // instance methods directly.
}

