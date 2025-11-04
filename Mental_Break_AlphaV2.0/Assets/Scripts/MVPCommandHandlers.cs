using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Command handlers that delegate to BackgroundCommandHandler and AudioCommandHandler if they exist,
/// otherwise provides no-op fallbacks to prevent command execution errors.
/// </summary>
public class MVPCommandHandlers : MonoBehaviour
{
    private DialogueRunner dialogueRunner;
    private BackgroundCommandHandler backgroundHandler;
    private AudioCommandHandler audioHandler;

    private void Awake()
    {
        dialogueRunner = GetComponent<DialogueRunner>();
        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }
        
        // Find the real command handlers if they exist
        backgroundHandler = FindFirstObjectByType<BackgroundCommandHandler>();
        audioHandler = FindFirstObjectByType<AudioCommandHandler>();
        
        // Register commands in Awake() to run before DialogueRunner initializes its command dispatcher
        // This ensures our handlers are in place before Yarn's auto-discovery runs
        RegisterCommands();
    }

    private void OnEnable()
    {
        // Also try registering in OnEnable() as a fallback
        // This handles cases where Awake() ran before DialogueRunner was ready
        // Re-find handlers in case they were added after Awake()
        if (backgroundHandler == null)
        {
            backgroundHandler = FindFirstObjectByType<BackgroundCommandHandler>();
        }
        if (audioHandler == null)
        {
            audioHandler = FindFirstObjectByType<AudioCommandHandler>();
        }
        
        if (dialogueRunner != null)
        {
            RegisterCommands();
        }
    }

    private void RegisterCommands()
    {
        if (dialogueRunner == null)
        {
            return; // DialogueRunner not ready yet
        }

        // Wait for DialogueRunner to be fully initialized
        if (dialogueRunner.YarnProject == null)
        {
            return; // Not ready yet
        }

        // Try to remove existing handlers first (safe to call, may not exist)
        // This handles cases where commands were auto-registered via [YarnCommand] attributes
        // Note: RemoveCommandHandler may not work on auto-registered commands, but it's worth trying
        try { dialogueRunner.RemoveCommandHandler("bg"); } catch { }
        try { dialogueRunner.RemoveCommandHandler("bgm"); } catch { }
        try { dialogueRunner.RemoveCommandHandler("sfx"); } catch { }

        // Register no-op handlers using bound delegates (prevents Yarn from looking for GameObjects)
        // Using bound delegates ensures Yarn passes the string directly to our methods instead of
        // searching for GameObjects with those names
        int registeredCount = 0;
        
        try
        {
            dialogueRunner.AddCommandHandler("bg", new System.Action<string>(HandleBG));
            registeredCount++;
        }
        catch (System.Exception e)
        {
            // Command may already be registered - that's OK, it will fail gracefully
            Debug.LogWarning($"MVPCommandHandlers: Could not register 'bg': {e.Message}");
        }
        
        try
        {
            dialogueRunner.AddCommandHandler("bgm", new System.Action<string>(HandleBGM));
            registeredCount++;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"MVPCommandHandlers: Could not register 'bgm': {e.Message}");
        }
        
        try
        {
            dialogueRunner.AddCommandHandler("sfx", new System.Action<string>(HandleSFX));
            registeredCount++;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"MVPCommandHandlers: Could not register 'sfx': {e.Message}");
        }
        
        if (registeredCount > 0)
        {
            string handlerInfo = "";
            if (backgroundHandler != null) handlerInfo += " (bg->BackgroundCommandHandler)";
            if (audioHandler != null) handlerInfo += " (bgm/sfx->AudioCommandHandler)";
            if (handlerInfo == "") handlerInfo = " (no-op fallbacks)";
            Debug.Log($"MVPCommandHandlers: Registered {registeredCount} command handler(s){handlerInfo}");
        }
    }

    // Note: We do NOT use [YarnCommand] attributes here to avoid auto-registration conflicts.
    // We manually register via AddCommandHandler using bound delegates to prevent Yarn
    // from looking for GameObjects with these names.
    private void HandleBG(string key)
    {
        // Try to find handler again in case it was added after Awake/OnEnable
        if (backgroundHandler == null)
        {
            backgroundHandler = FindFirstObjectByType<BackgroundCommandHandler>();
        }
        
        if (backgroundHandler != null)
        {
            // Delegate to the real BackgroundCommandHandler
            backgroundHandler.ChangeBackground(key);
        }
        else
        {
            // Fallback no-op if handler doesn't exist
            Debug.LogWarning($"[MVP] bg command: {key} (no-op - BackgroundCommandHandler not found in scene. Please ensure BackgroundCommandHandler component exists and is active.)");
        }
    }

    private void HandleBGM(string key)
    {
        // Try to find handler again in case it was added after Awake/OnEnable
        if (audioHandler == null)
        {
            audioHandler = FindFirstObjectByType<AudioCommandHandler>();
        }
        
        if (audioHandler != null)
        {
            // Delegate to the real AudioCommandHandler
            audioHandler.PlayBGM(key);
        }
        else
        {
            // Fallback no-op if handler doesn't exist
            Debug.LogWarning($"[MVP] bgm command: {key} (no-op - AudioCommandHandler not found in scene. Please ensure AudioCommandHandler component exists and is active.)");
        }
    }

    private void HandleSFX(string key)
    {
        // Try to find handler again in case it was added after Awake/OnEnable
        if (audioHandler == null)
        {
            audioHandler = FindFirstObjectByType<AudioCommandHandler>();
        }
        
        if (audioHandler != null)
        {
            // Delegate to the real AudioCommandHandler
            audioHandler.PlaySFX(key);
        }
        else
        {
            // Fallback no-op if handler doesn't exist
            Debug.LogWarning($"[MVP] sfx command: {key} (no-op - AudioCommandHandler not found in scene. Please ensure AudioCommandHandler component exists and is active.)");
        }
    }
}

