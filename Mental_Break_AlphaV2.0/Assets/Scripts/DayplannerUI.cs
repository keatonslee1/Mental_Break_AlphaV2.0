using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Displays a dayplanner UI indicator when dayplanner nodes are active.
/// The actual task selection is handled by Yarn's choice system via OptionsPresenter.
/// </summary>
public class DayplannerUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The DialogueRunner to monitor")]
    public DialogueRunner dialogueRunner;

    [Header("UI Elements")]
    [Tooltip("Panel that appears when dayplanner is active")]
    public GameObject dayplannerPanel;

    [Tooltip("Text showing current day and slots info")]
    public Component dayplannerTitleText;

    private VariableStorageBehaviour variableStorage;

    // Dayplanner node patterns (nodes tagged with #mechanic_dayplanner)
    private readonly string[] dayplannerNodePatterns = {
        "R1_D1_Dayplanner", "R1_D2_Dayplanner", "R1_D3_Dayplanner",
        "R2_D1_Dayplanner", "R2_D2_Dayplanner", "R2_D3_Dayplanner",
        "R3_D1_Dayplanner", "R3_D2_Dayplanner", "R3_D3_Dayplanner",
        "R4_", // Run 4 might have different patterns
    };

    private void Start()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (dialogueRunner != null)
        {
            variableStorage = dialogueRunner.VariableStorage;
            
            // Subscribe to node start events
            dialogueRunner.onNodeStart.AddListener(OnNodeStarted);
        }

        if (dayplannerPanel != null)
        {
            dayplannerPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null && dialogueRunner.onNodeStart != null)
        {
            dialogueRunner.onNodeStart.RemoveListener(OnNodeStarted);
        }
    }

    private void OnNodeStarted(string nodeName)
    {
        // Check if this is a dayplanner node
        bool isDayplanner = false;
        foreach (string pattern in dayplannerNodePatterns)
        {
            if (nodeName.Contains(pattern))
            {
                isDayplanner = true;
                break;
            }
        }

        if (isDayplanner)
        {
            ShowDayplannerUI(nodeName);
        }
        else
        {
            HideDayplannerUI();
        }
    }

    private void ShowDayplannerUI(string nodeName)
    {
        if (dayplannerPanel != null)
        {
            dayplannerPanel.SetActive(true);
        }

        // Update title text
        if (variableStorage != null && dayplannerTitleText != null)
        {
            int currentDay = 1;
            if (variableStorage.TryGetValue<float>("$current_day", out var dayValue))
            {
                currentDay = Mathf.RoundToInt(dayValue);
            }

            int slotsUsed = 0;
            string slotsVar = $"$d{currentDay}_slots_used";
            if (variableStorage.TryGetValue<float>(slotsVar, out var slotsValue))
            {
                slotsUsed = Mathf.RoundToInt(slotsValue);
            }

            string titleText = $"Day {currentDay} - Dayplanner\n{slotsUsed}/3 slots used";
            SetText(dayplannerTitleText, titleText);
        }
    }

    private void HideDayplannerUI()
    {
        if (dayplannerPanel != null)
        {
            dayplannerPanel.SetActive(false);
        }
    }

    private void SetText(Component textComponent, string text)
    {
        if (textComponent == null) return;

#if USE_TMP
        if (textComponent is TMPro.TextMeshProUGUI tmpText)
        {
            tmpText.text = text;
            return;
        }
#endif
        if (textComponent is UnityEngine.UI.Text regularText)
        {
            regularText.text = text;
        }
    }
}
