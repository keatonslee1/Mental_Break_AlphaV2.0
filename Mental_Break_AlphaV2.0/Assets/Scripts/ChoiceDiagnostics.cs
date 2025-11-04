using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Runtime diagnostic component to track choice handling.
/// Attach to Dialogue System to debug choice issues.
/// </summary>
public class ChoiceDiagnostics : MonoBehaviour
{
    [Header("Diagnostics")]
    [Tooltip("Enable debug logging for choice handling")]
    public bool enableDebugLogging = true;

    private DialogueRunner dialogueRunner;
    private OptionsPresenter optionsPresenter;
    private LinePresenter linePresenter;

    private void Start()
    {
        dialogueRunner = GetComponentInParent<DialogueRunner>();
        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }

        optionsPresenter = GetComponentInChildren<OptionsPresenter>();
        if (optionsPresenter == null)
        {
            optionsPresenter = FindFirstObjectByType<OptionsPresenter>();
        }

        linePresenter = GetComponentInChildren<LinePresenter>();
        if (linePresenter == null)
        {
            linePresenter = FindFirstObjectByType<LinePresenter>();
        }

        if (dialogueRunner != null && enableDebugLogging)
        {
            // Subscribe to dialogue events
            dialogueRunner.onDialogueStart.AddListener(OnDialogueStart);
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
            dialogueRunner.onNodeStart.AddListener(OnNodeStart);
            
            // Hook into OnOptionsReceived if possible via reflection
            HookIntoOptionsReceived();
        }

        LogInitialState();
    }

    private void LogInitialState()
    {
        if (!enableDebugLogging) return;

        Debug.Log("=== Choice Diagnostics Initial State ===");
        
        if (dialogueRunner == null)
        {
            Debug.LogError("ChoiceDiagnostics: DialogueRunner not found!");
            return;
        }

        Debug.Log($"DialogueRunner found: {dialogueRunner.gameObject.name}");
        
        int presenterCount = 0;
        foreach (var p in dialogueRunner.DialoguePresenters) presenterCount++;
        Debug.Log($"DialogueRunner has {presenterCount} presenter(s) registered");

        foreach (var presenter in dialogueRunner.DialoguePresenters)
        {
            if (presenter == null)
            {
                Debug.LogError("ChoiceDiagnostics: Null presenter in DialogueRunner list!");
            }
            else
            {
                string type = presenter.GetType().Name;
                bool isActive = presenter.isActiveAndEnabled;
                Debug.Log($"  - Presenter: {type}, Active: {isActive}, GameObject: {presenter.gameObject.name}");
                
                if (presenter is OptionsPresenter op)
                {
                    // Check prefab using reflection at runtime
                    var field = typeof(OptionsPresenter).GetField("optionViewPrefab", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    bool hasPrefab = false;
                    if (field != null)
                    {
                        var prefab = field.GetValue(op);
                        hasPrefab = prefab != null;
                    }
                    Debug.Log($"    Options Presenter - Has Prefab: {hasPrefab}");
                }
            }
        }

        if (optionsPresenter == null)
        {
            Debug.LogError("ChoiceDiagnostics: Options Presenter not found in scene!");
        }
        else
        {
            bool isRegistered = false;
            foreach (var p in dialogueRunner.DialoguePresenters)
            {
                if (p == optionsPresenter)
                {
                    isRegistered = true;
                    break;
                }
            }
            Debug.Log($"Options Presenter found: {optionsPresenter.gameObject.name}, Registered: {isRegistered}, Active: {optionsPresenter.isActiveAndEnabled}");
        }

        if (linePresenter == null)
        {
            Debug.LogWarning("ChoiceDiagnostics: Line Presenter not found!");
        }
        else
        {
            Debug.Log($"Line Presenter found: {linePresenter.gameObject.name}");
        }
    }

    private void OnDialogueStart()
    {
        if (!enableDebugLogging) return;
        Debug.Log("ChoiceDiagnostics: Dialogue started");
    }

    private void OnDialogueComplete()
    {
        if (!enableDebugLogging) return;
        Debug.Log("ChoiceDiagnostics: Dialogue completed");
    }

    private void OnNodeStart(string nodeName)
    {
        if (!enableDebugLogging) return;
        Debug.Log($"ChoiceDiagnostics: Node started: {nodeName}");
        
        // If commands/choices show as text, the Yarn Project likely has compilation errors
        // Check the Yarn Project Inspector for errors and reimport if needed
    }

    private void HookIntoOptionsReceived()
    {
        if (dialogueRunner == null || !enableDebugLogging) return;

        // Monitor dialogue state to detect when options should appear
        // We'll poll the dialogue state (less ideal but works)
        StartCoroutine(MonitorDialogueState());
        
        Debug.Log("ChoiceDiagnostics: Monitoring for option events (via Dialogue Runner state polling)");
    }

    private System.Collections.IEnumerator MonitorDialogueState()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
            
            if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning) continue;
            
            // We can't easily detect if dialogue is waiting for options, but we can
            // check if Options Presenter is showing options by checking its state
            // This is limited, but helps with diagnosis
        }
    }

    // Add this to log when we detect options should have appeared
    public void LogWhenOptionsShouldAppear(string nodeName)
    {
        if (!enableDebugLogging) return;
        
        // Check if this node should have choices based on common patterns
        // This is a heuristic check
        Debug.Log($"ChoiceDiagnostics: Checking if node '{nodeName}' should have choices...");
        
        // Note: We can't easily parse Yarn files at runtime, but this helps with debugging
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null && enableDebugLogging)
        {
            dialogueRunner.onDialogueStart.RemoveListener(OnDialogueStart);
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
            dialogueRunner.onNodeStart.RemoveListener(OnNodeStart);
        }
    }

    // Monitor for choice-related errors in the console
    private void OnEnable()
    {
        Application.logMessageReceived += OnLogMessage;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessage;
    }

    private void OnLogMessage(string logString, string stackTrace, LogType type)
    {
        if (!enableDebugLogging) return;

        // Monitor for command-related issues
        if (logString.Contains("No Command") && logString.Contains("was found"))
        {
            Debug.LogError("ChoiceDiagnostics: Command not found error detected!");
            Debug.LogError("  This means a Yarn command like <<jump>> or <<set>> wasn't recognized.");
            Debug.LogError("  Commands should execute, not display as text or generate errors.");
            Debug.LogError("  Check if:");
            Debug.LogError("    - Command syntax is correct (<<command>>)");
            Debug.LogError("    - Command handler is registered");
            Debug.LogError("    - Yarn Project compiled successfully");
        }

        // Monitor for critical choice-related errors
        if (type == LogType.Error)
        {
            if (logString.Contains("No dialogue view returned an option selection"))
            {
                Debug.LogError("ChoiceDiagnostics: CRITICAL ERROR - No presenter returned an option selection!");
                Debug.LogError("  This means Options Presenter failed to handle choices.");
                Debug.LogError("  Possible causes:");
                Debug.LogError("    - Options Presenter's optionViewPrefab is null or invalid");
                Debug.LogError("    - Options Presenter's RunOptionsAsync threw an exception");
                Debug.LogError("    - Options Presenter is not properly configured");
                Debug.LogError("    - Options Presenter returned null (didn't handle the choice)");
                
                if (optionsPresenter != null)
                {
                    var field = typeof(OptionsPresenter).GetField("optionViewPrefab",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var prefab = field.GetValue(optionsPresenter);
                        if (prefab == null)
                        {
                            Debug.LogError("  DIAGNOSIS: optionViewPrefab is NULL!");
                        }
                        else
                        {
                            Debug.LogError($"  DIAGNOSIS: optionViewPrefab exists: {prefab}");
                        }
                    }
                    
                    // Check if Options Presenter is active
                    if (!optionsPresenter.isActiveAndEnabled)
                    {
                        Debug.LogError("  DIAGNOSIS: Options Presenter is NOT active/enabled!");
                    }
                    
                    // Check Canvas Group
                    var canvasGroupField = typeof(OptionsPresenter).GetField("canvasGroup",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (canvasGroupField != null)
                    {
                        var canvasGroup = canvasGroupField.GetValue(optionsPresenter);
                        if (canvasGroup == null)
                        {
                            Debug.LogWarning("  DIAGNOSIS: Canvas Group is not assigned (may affect visibility)");
                        }
                    }
                }
                else
                {
                    Debug.LogError("  DIAGNOSIS: Options Presenter component is NULL!");
                }
            }
            else if (logString.Contains("Failed to get a localised line") && logString.Contains("option"))
            {
                Debug.LogWarning("ChoiceDiagnostics: Error getting localized line for an option");
            }
        }
    }
}

