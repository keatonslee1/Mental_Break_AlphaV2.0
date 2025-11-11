#nullable enable

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yarn.Unity;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Handles Enter key and click/tap-to-advance functionality for dialogue progression.
/// Adds Enter key support and click/tap detection on the dialogue text box to advance dialogue.
/// Unity 6 Web: IPointerClickHandler automatically handles both mouse clicks and touch taps on mobile browsers
/// through Unity's EventSystem, so no special touch input handling is required.
/// </summary>
public class DialogueAdvanceHandler : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("The DialogueRunner component that manages dialogue")]
    [SerializeField] private DialogueRunner? dialogueRunner;

    [Tooltip("The LinePresenter component that displays dialogue lines")]
    [SerializeField] private LinePresenter? linePresenter;

    [Tooltip("Enable Enter key to advance dialogue")]
    [SerializeField] private bool enableEnterKey = true;

    [Tooltip("Enable clicking the text box to advance dialogue")]
    [SerializeField] private bool enableClickToAdvance = true;

    private bool inputSystemActive = false;
    private Component? dialogueText;
    private LineAdvancer? lineAdvancer;

    private void Start()
    {
        // Find DialogueRunner if not assigned
        if (dialogueRunner == null)
        {
            dialogueRunner = GetComponent<DialogueRunner>();
            if (dialogueRunner == null)
            {
                dialogueRunner = FindFirstObjectByType<DialogueRunner>();
            }
        }

        if (dialogueRunner == null)
        {
            Debug.LogWarning("DialogueAdvanceHandler: DialogueRunner not found. Enter key and click-to-advance will not work.");
            enabled = false;
            return;
        }

        // Find LinePresenter if not assigned
        if (linePresenter == null)
        {
            linePresenter = GetComponent<LinePresenter>();
            if (linePresenter == null)
            {
                linePresenter = FindFirstObjectByType<LinePresenter>();
            }
        }

        if (linePresenter != null)
        {
            // Get the text component from LinePresenter
            dialogueText = linePresenter.lineText;
            
            // If we have a text component and this component is on the text GameObject, it can handle clicks
            if (dialogueText != null && dialogueText.gameObject == this.gameObject)
            {
                // This component is on the text GameObject, so it can handle clicks
                // No additional setup needed
            }
            else if (dialogueText != null && enableClickToAdvance)
            {
                // This component is NOT on the text GameObject, so we need to add a handler to the text GameObject
                DialogueAdvanceHandler textHandler = dialogueText.gameObject.GetComponent<DialogueAdvanceHandler>();
                if (textHandler == null)
                {
                    textHandler = dialogueText.gameObject.AddComponent<DialogueAdvanceHandler>();
                    textHandler.dialogueRunner = this.dialogueRunner;
                    textHandler.linePresenter = this.linePresenter;
                    textHandler.enableEnterKey = false; // Only handle Enter in the main component
                    textHandler.enableClickToAdvance = true;
                    textHandler.dialogueText = dialogueText;
                }
            }
        }

        // Find LineAdvancer to disable ESC key
        lineAdvancer = FindFirstObjectByType<LineAdvancer>();
        if (lineAdvancer != null)
        {
            // Disable ESC by setting nextLineKeyCode to None via reflection
            // Since LineAdvancer is sealed, we use reflection to modify the field
            var field = typeof(LineAdvancer).GetField("nextLineKeyCode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(lineAdvancer, KeyCode.None);
                Debug.Log("DialogueAdvanceHandler: Disabled ESC key in LineAdvancer");
            }
            else
            {
                Debug.LogWarning("DialogueAdvanceHandler: Could not disable ESC key in LineAdvancer (field not found)");
            }
        }

        // Check which input system is active
        CheckInputSystem();

        EnsureClickAdvanceConfiguration();
    }

    private void CheckInputSystem()
    {
#if ENABLE_INPUT_SYSTEM
        inputSystemActive = true;
#else
        inputSystemActive = false;
#endif
    }

    private void Update()
    {
        // Block ESC key from LineAdvancer
        if (lineAdvancer != null)
        {
            // We can't prevent LineAdvancer from processing ESC directly,
            // but we can check if ESC is pressed and ignore it
            // Actually, we already disabled it via reflection in Start()
        }

        if (!enabled || !enableEnterKey)
        {
            return;
        }

        // Check if dialogue is running first
        if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
        {
            return;
        }

        // Only process Enter key if we're showing dialogue (not options)
        if (IsShowingOptions())
        {
            return;
        }

        // Check if we're currently showing a line using improved detection
        if (!IsShowingDialogueLineImproved())
        {
            return;
        }

        // Handle Enter key input
        bool enterPressed = false;

        try
        {
#if ENABLE_INPUT_SYSTEM
            if (inputSystemActive)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    enterPressed = keyboard[Key.Enter].wasPressedThisFrame || keyboard[Key.NumpadEnter].wasPressedThisFrame;
                }
            }
            else
            {
                enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            }
#else
            enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"DialogueAdvanceHandler: Error detecting Enter key input: {ex.Message}");
            return;
        }

        if (enterPressed && dialogueRunner != null)
        {
            dialogueRunner.RequestNextLine();
        }
    }

    /// <summary>
    /// Handles click/tap events on the dialogue text box.
    /// Unity 6 Web: IPointerClickHandler works for both mouse clicks and touch taps on mobile browsers.
    /// The EventSystem automatically routes touch input to pointer events, so no special touch handling is needed.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enabled || !enableClickToAdvance)
        {
            return;
        }

        // Check if dialogue is running
        if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
        {
            return;
        }

        // Only process clicks/taps if we're showing dialogue (not options)
        // Options have their own click handlers, so we should not advance when they're visible
        if (IsShowingOptions())
        {
            return;
        }

        // Simplified check: if dialogue is running and no options are showing, allow advance.
        // The previous IsShowingDialogueLineImproved() check was too restrictive and could fail
        // when reflection couldn't access internal Yarn Spinner state, blocking legitimate taps/clicks.
        // Unity 6 Web: This simpler approach works reliably for clicks/taps anywhere on the dialogue box.
        
        // Advance dialogue
        dialogueRunner.RequestNextLine();
    }

    /// <summary>
    /// Check if options are currently being displayed
    /// </summary>
    private bool IsShowingOptions()
    {
        OptionItem[] allOptionItems = FindObjectsByType<OptionItem>(FindObjectsSortMode.None);
        
        foreach (OptionItem item in allOptionItems)
        {
            if (item.isActiveAndEnabled)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if dialogue is currently showing a line (not options)
    /// Improved version that uses DialogueRunner state
    /// </summary>
    private bool IsShowingDialogueLineImproved()
    {
        // First check if dialogue is running
        if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
        {
            return false;
        }

        // Use reflection to check if currentLineCancellationSource is not null
        // This indicates a line is currently being displayed
        var field = typeof(DialogueRunner).GetField("currentLineCancellationSource",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var cancellationSource = field.GetValue(dialogueRunner);
            if (cancellationSource == null)
            {
                // No line is currently being displayed
                return false;
            }
        }

        // Also check visual state if we have LinePresenter
        if (linePresenter != null && dialogueText != null)
        {
            // Check if the canvas group is visible (if present)
            if (linePresenter.canvasGroup != null)
            {
                if (linePresenter.canvasGroup.alpha <= 0.01f)
                {
                    return false;
                }
            }

            // Check if text has content
            string textContent = "";
#if USE_TMP
            if (dialogueText is TMPro.TextMeshProUGUI tmpText)
            {
                textContent = tmpText.text;
            }
#endif
            if (string.IsNullOrEmpty(textContent) && dialogueText is UnityEngine.UI.Text uiText)
            {
                textContent = uiText.text;
            }
            
            // If text is empty, might still be valid (typewriter effect)
            // But we already checked cancellation source, so this is just a secondary check
        }

        return true;
    }

    private void EnsureClickAdvanceConfiguration()
    {
        // Unity 6 Web: even if the prefab disables click-to-advance, we force enable it here so
        // mouse/touch taps always advance dialogue on WebGL/mobile browsers.
        if (!enableClickToAdvance)
        {
            enableClickToAdvance = true;
            Debug.Log("DialogueAdvanceHandler: enableClickToAdvance was disabled; re-enabling to support tap-to-progress across mouse and touch.");
        }

        if (!enableClickToAdvance)
        {
            return;
        }

        EnsureGraphicRaycastTarget(gameObject);

        if (dialogueText != null)
        {
            EnsureGraphicRaycastTarget(dialogueText.gameObject);
        }
    }

    private static void EnsureGraphicRaycastTarget(GameObject target)
    {
        if (target.TryGetComponent<Graphic>(out var graphic))
        {
            graphic.raycastTarget = true;
            return;
        }

        var image = target.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
    }
}

