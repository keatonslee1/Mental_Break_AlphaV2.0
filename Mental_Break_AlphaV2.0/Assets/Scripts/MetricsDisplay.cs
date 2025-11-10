using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Displays game metrics (Engagement, Sanity, Leaderboard Rank) in the UI.
/// Updates in real-time as Yarn variables change.
/// </summary>
public class MetricsDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The DialogueRunner to read variables from")]
    public DialogueRunner dialogueRunner;

    [Header("Engagement Display")]
    public GameObject engagementContainer;
    public Component engagementValueText; // TextMeshProUGUI or Text
    public Image engagementBar;
    public float engagementMaxValue = 100f;

    [Header("Sanity Display")]
    public GameObject sanityContainer;
    public Component sanityValueText;
    public Image sanityBar;
    public float sanityMaxValue = 100f;

    [Header("Rank Display")]
    public GameObject rankContainer;
    public Component rankValueText;

    private VariableStorageBehaviour variableStorage;
    private DialogueRuntimeWatcher runtimeWatcher;
    private float updateInterval = 0.1f; // Update every 0.1 seconds
    private float lastUpdateTime = 0f;

    private void OnEnable()
    {
        runtimeWatcher = DialogueRuntimeWatcher.Instance;
        runtimeWatcher.Register(OnRuntimeReady, OnRuntimeLost);

        if (!runtimeWatcher.HasRuntime)
        {
            ApplyLoadingState();
        }
    }

    private void OnDisable()
    {
        if (runtimeWatcher != null)
        {
            runtimeWatcher.Unregister(OnRuntimeReady, OnRuntimeLost);
            runtimeWatcher = null;
        }
    }

    private void Start()
    {
        // Find DialogueRunner if not assigned
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (dialogueRunner != null)
        {
            variableStorage = dialogueRunner.VariableStorage;
        }

        if (variableStorage == null)
        {
            ApplyLoadingState();
        }
        else
        {
            UpdateMetrics();
        }
    }

    private void OnRuntimeReady(DialogueRunner runner, VariableStorageBehaviour storage)
    {
        if (runner != null)
        {
            dialogueRunner = runner;
        }

        variableStorage = storage;
        UpdateMetrics();
    }

    private void OnRuntimeLost()
    {
        variableStorage = null;
        ApplyLoadingState();
    }

    private void Update()
    {
        // Update metrics at intervals (not every frame for performance)
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateMetrics();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdateMetrics()
    {
        if (variableStorage == null)
        {
            ApplyLoadingState();
            return;
        }

        // Update Engagement
        float engagement = 0f;
        if (variableStorage.TryGetValue<float>("$engagement", out var engagementValue))
        {
            engagement = engagementValue;
        }
        UpdateMetric(engagementContainer, engagementValueText, engagementBar, engagement, engagementMaxValue, "Engagement");

        // Update Sanity
        float sanity = 0f;
        if (variableStorage.TryGetValue<float>("$sanity", out var sanityValue))
        {
            sanity = sanityValue;
        }
        UpdateMetric(sanityContainer, sanityValueText, sanityBar, sanity, sanityMaxValue, "Sanity");

        // Update Rank
        float rank = 0f;
        if (variableStorage.TryGetValue<float>("$leaderboard_rank", out var rankValue))
        {
            rank = rankValue;
        }
        UpdateRank(rankContainer, rankValueText, rank);
    }

    private void ApplyLoadingState()
    {
        SetMetricPlaceholder(engagementContainer, engagementValueText, engagementBar, "Engagement");
        SetMetricPlaceholder(sanityContainer, sanityValueText, sanityBar, "Sanity");
        SetRankPlaceholder();
    }

    private void SetMetricPlaceholder(GameObject container, Component textComponent, Image bar, string label)
    {
        if (container != null)
        {
            container.SetActive(true);
        }

        SetText(textComponent, $"{label}: --");

        if (bar != null)
        {
            bar.fillAmount = 0f;
            bar.color = Color.gray;
        }
    }

    private void SetRankPlaceholder()
    {
        if (rankContainer != null)
        {
            rankContainer.SetActive(true);
        }

        SetText(rankValueText, "Rank: --");
    }

    private void UpdateMetric(GameObject container, Component textComponent, Image bar, float value, float maxValue, string label)
    {
        if (container != null)
        {
            container.SetActive(true);
        }

        // Update text
        string text = $"{label}: {value:F0}";
        SetText(textComponent, text);

        // Update bar fill
        if (bar != null)
        {
            bar.fillAmount = Mathf.Clamp01(value / maxValue);
            
            // Color based on value (green for good, red for bad)
            if (label == "Engagement")
            {
                // Higher engagement = more green (but this might be morally ambiguous)
                bar.color = Color.Lerp(Color.red, Color.yellow, value / maxValue);
            }
            else if (label == "Sanity")
            {
                // Higher sanity = more green
                bar.color = Color.Lerp(Color.red, Color.green, value / maxValue);
            }
        }
    }

    private void UpdateRank(GameObject container, Component textComponent, float rank)
    {
        if (container != null)
        {
            container.SetActive(true);
        }

        string text = $"Rank: #{Mathf.RoundToInt(rank)}";
        SetText(textComponent, text);
    }

    private void SetText(Component textComponent, string text)
    {
        if (textComponent == null) return;

#if USE_TMP
        if (textComponent is TextMeshProUGUI tmpText)
        {
            tmpText.text = text;
            return;
        }
#endif
        if (textComponent is Text regularText)
        {
            regularText.text = text;
        }
    }
}
