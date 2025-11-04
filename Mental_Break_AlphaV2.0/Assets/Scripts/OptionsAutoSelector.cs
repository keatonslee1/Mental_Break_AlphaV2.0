#nullable enable

using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;
using System.Collections;

/// <summary>
/// Automatically selects the first option when options appear, or selects the option
/// the mouse is hovering over if one is being hovered.
/// </summary>
public class OptionsAutoSelector : MonoBehaviour
{
    [Tooltip("Delay in seconds before auto-selecting (to allow OptionsPresenter to finish setup)")]
    [SerializeField] private float selectionDelay = 0.05f;

    private EventSystem? eventSystem;
    private bool optionsWereActive = false;

    private void Start()
    {
        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogWarning("OptionsAutoSelector: EventSystem not found. Auto-selection may not work.");
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        // Check if options are currently active
        bool optionsActive = AreOptionsActive();

        // Detect when options first become active
        if (optionsActive && !optionsWereActive)
        {
            // Options just appeared - trigger auto-selection after a short delay
            StartCoroutine(SelectOptionAfterDelay());
        }

        optionsWereActive = optionsActive;
    }

    /// <summary>
    /// Check if any options are currently active
    /// </summary>
    private bool AreOptionsActive()
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
    /// Wait for OptionsPresenter to finish setup, then select the appropriate option
    /// </summary>
    private IEnumerator SelectOptionAfterDelay()
    {
        yield return new WaitForSeconds(selectionDelay);

        if (eventSystem == null)
        {
            yield break;
        }

        // Get all active options
        OptionItem[] allOptionItems = FindObjectsByType<OptionItem>(FindObjectsSortMode.None);
        
        if (allOptionItems.Length == 0)
        {
            yield break;
        }

        // First, check if mouse is hovering over any option
        OptionItem? hoveredOption = GetHoveredOption();

        if (hoveredOption != null && hoveredOption.isActiveAndEnabled && hoveredOption.IsInteractable())
        {
            // Mouse is hovering over an option - select it
            SelectOption(hoveredOption);
        }
        else
        {
            // No hover - select the first available option
            OptionItem? firstOption = GetFirstAvailableOption(allOptionItems);
            if (firstOption != null)
            {
                SelectOption(firstOption);
            }
        }
    }

    /// <summary>
    /// Check if mouse is currently hovering over any option using EventSystem raycasting
    /// </summary>
    private OptionItem? GetHoveredOption()
    {
        if (eventSystem == null)
        {
            return null;
        }

        // Create a pointer event data for the current mouse position
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        // Raycast to find what the mouse is over
        var results = new System.Collections.Generic.List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);

        // Check each result to see if it's an OptionItem
        foreach (RaycastResult result in results)
        {
            OptionItem optionItem = result.gameObject.GetComponent<OptionItem>();
            if (optionItem == null)
            {
                // Try parent (in case a child element was hit)
                optionItem = result.gameObject.GetComponentInParent<OptionItem>();
            }

            if (optionItem != null && optionItem.isActiveAndEnabled && optionItem.IsInteractable())
            {
                return optionItem;
            }
        }

        return null;
    }

    /// <summary>
    /// Get the first available (interactable) option from the list
    /// </summary>
    private OptionItem? GetFirstAvailableOption(OptionItem[] allOptions)
    {
        foreach (OptionItem option in allOptions)
        {
            if (option.isActiveAndEnabled && option.IsInteractable())
            {
                return option;
            }
        }

        return null;
    }

    /// <summary>
    /// Select an option by setting it as the EventSystem's selected GameObject
    /// </summary>
    private void SelectOption(OptionItem option)
    {
        if (eventSystem == null)
        {
            return;
        }

        // Set the EventSystem's selected GameObject to ensure proper highlighting
        eventSystem.SetSelectedGameObject(option.gameObject);
        
        // Also call Select() on the OptionItem to ensure it receives OnSelect event
        option.Select();
    }
}

