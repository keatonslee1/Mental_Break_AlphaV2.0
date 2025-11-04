using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Handles keyboard input for option selection in Yarn Spinner.
/// Allows spacebar to select the currently highlighted option.
/// The first option is automatically highlighted by OptionsPresenter, so this just adds spacebar support.
/// </summary>
public class OptionsInputHandler : MonoBehaviour
{
    [Tooltip("Enable spacebar to select highlighted option")]
    [SerializeField] private bool enableSpacebarSelection = true;

    private bool inputSystemActive = false;

    private void Start()
    {
        // Check which input system is active
        CheckInputSystem();
    }

    private void CheckInputSystem()
    {
#if ENABLE_INPUT_SYSTEM
        // Check if Input System is the active input handler
        inputSystemActive = true;
#else
        inputSystemActive = false;
#endif
    }

    private void Update()
    {
        if (!enabled || !enableSpacebarSelection)
        {
            return;
        }

        // Check if options are currently being shown
        if (!IsOptionsShowing())
        {
            return;
        }

        // Handle spacebar input
        bool spacePressed = false;

        try
        {
#if ENABLE_INPUT_SYSTEM
            if (inputSystemActive)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    spacePressed = keyboard[Key.Space].wasPressedThisFrame;
                }
            }
            else
            {
                spacePressed = Input.GetKeyDown(KeyCode.Space);
            }
#else
            spacePressed = Input.GetKeyDown(KeyCode.Space);
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"OptionsInputHandler: Error detecting spacebar input: {ex.Message}");
            return;
        }

        if (spacePressed)
        {
            SelectCurrentlyHighlightedOption();
        }
    }

    /// <summary>
    /// Check if options are currently being displayed by looking for active OptionItems
    /// </summary>
    private bool IsOptionsShowing()
    {
        // Check if there are any active and enabled OptionItems in the scene
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
    /// Selects the currently highlighted option by invoking its selection
    /// </summary>
    private void SelectCurrentlyHighlightedOption()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            // No EventSystem, try to find and select first available option
            SelectFirstAvailableOption();
            return;
        }

        GameObject selected = eventSystem.currentSelectedGameObject;
        
        // If nothing is selected, select the first available option
        if (selected == null)
        {
            SelectFirstAvailableOption();
            return;
        }

        // Get the OptionItem component from the selected object
        OptionItem optionItem = selected.GetComponent<OptionItem>();
        if (optionItem == null)
        {
            // Try parent (in case a child like text is selected)
            optionItem = selected.GetComponentInParent<OptionItem>();
        }

        if (optionItem != null && optionItem.isActiveAndEnabled && optionItem.IsInteractable())
        {
            // Invoke the option selection
            optionItem.InvokeOptionSelected();
        }
        else
        {
            // Fallback: try to select first available option
            SelectFirstAvailableOption();
        }
    }

    /// <summary>
    /// Selects the first available option if no option is currently selected
    /// </summary>
    private void SelectFirstAvailableOption()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        // Find all OptionItems and select the first available one
        OptionItem[] allOptionItems = FindObjectsByType<OptionItem>(FindObjectsSortMode.None);
        
        foreach (OptionItem item in allOptionItems)
        {
            if (item.isActiveAndEnabled && item.IsInteractable())
            {
                // Select this option
                item.Select();
                eventSystem.SetSelectedGameObject(item.gameObject);
                break;
            }
        }
    }
}

