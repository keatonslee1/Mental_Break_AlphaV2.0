using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Displays Engagement and Sanity percentages on the right side of the screen.
/// Engagement is displayed in fuchsia, Sanity in cyan.
/// Updates in real-time from Yarn variables.
/// </summary>
public class MetricsPanelUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The DialogueRunner to read variables from")]
    public DialogueRunner dialogueRunner;

    [Header("Settings")]
    [Tooltip("Update interval in seconds")]
    public float updateInterval = 0.1f;

    [Tooltip("Font size for metric text")]
    public int fontSize = 24;

    [Tooltip("Spacing between metrics")]
    public float metricSpacing = 40f;

    [Tooltip("Distance from right edge")]
    public float rightMargin = 20f;

    [Tooltip("Distance from top edge")]
    public float topMargin = 20f;

    // Colors
    private readonly Color engagementColor = new Color(1f, 0f, 1f, 1f); // Fuchsia #FF00FF
    private readonly Color sanityColor = new Color(0f, 1f, 1f, 1f); // Cyan #00FFFF

    private VariableStorageBehaviour variableStorage;
    private float lastUpdateTime = 0f;

    // UI References
    private GameObject engagementPanel;
    private GameObject sanityPanel;
    private Canvas canvas;

#if USE_TMP
    private TextMeshProUGUI engagementText;
    private TextMeshProUGUI sanityText;
#else
    private Text engagementText;
    private Text sanityText;
#endif

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

        // Create UI elements if they don't already exist
        if (engagementPanel == null || sanityPanel == null)
        {
            CreateUI();
        }
    }

    private void Update()
    {
        // Update metrics at intervals
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateMetrics();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Create the UI elements programmatically
    /// </summary>
    private void CreateUI()
    {
        // Find or create Canvas (check for existing one first to share with other UI scripts)
        canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MetricsCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            // Use existing canvas, ensure it has required components
            if (canvas.GetComponent<CanvasScaler>() == null)
            {
                canvas.gameObject.AddComponent<CanvasScaler>();
            }
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        // Create Engagement panel
        engagementPanel = CreateMetricPanel("EngagementPanel", 0);
        engagementText = CreateMetricText(engagementPanel, "Engagement", engagementColor);

        // Create Sanity panel
        sanityPanel = CreateMetricPanel("SanityPanel", 1);
        sanityText = CreateMetricText(sanityPanel, "Sanity", sanityColor);
    }

    /// <summary>
    /// Create a metric panel
    /// </summary>
    private GameObject CreateMetricPanel(string name, int index)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        
        // Position at top right
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-rightMargin, -topMargin - (index * metricSpacing));
        rect.sizeDelta = new Vector2(200f, 30f);

        // Add semi-transparent background
        Image bgImage = panel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.5f);

        return panel;
    }

    /// <summary>
    /// Create text component for a metric
    /// </summary>
#if USE_TMP
    private TextMeshProUGUI CreateMetricText(GameObject panel, string label, Color color)
#else
    private Text CreateMetricText(GameObject panel, string label, Color color)
#endif
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panel.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

#if USE_TMP
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        // Load default font if available
        if (TMPro.TMP_Settings.instance != null && TMPro.TMP_Settings.instance.defaultFontAsset != null)
        {
            text.font = TMPro.TMP_Settings.instance.defaultFontAsset;
        }
        text.text = $"{label}: 0%";
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.color = color;
#else
        Text text = textObj.AddComponent<Text>();
        text.text = $"{label}: 0%";
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleRight;
        text.color = color;
        // Use default Unity font
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#endif

        return text;
    }

    /// <summary>
    /// Update the metric displays with current values
    /// </summary>
    private void UpdateMetrics()
    {
        if (variableStorage == null) return;

        // Get Engagement value
        float engagement = 0f;
        if (variableStorage.TryGetValue<float>("$engagement", out var engagementValue))
        {
            engagement = engagementValue;
        }

        // Get Sanity value
        float sanity = 0f;
        if (variableStorage.TryGetValue<float>("$sanity", out var sanityValue))
        {
            sanity = sanityValue;
        }

        // Update text
        if (engagementText != null)
        {
            engagementText.text = $"Engagement: {engagement:F0}%";
        }

        if (sanityText != null)
        {
            sanityText.text = $"Sanity: {sanity:F0}%";
        }
    }
}
