using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using System.Collections.Generic;
using System.Linq;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Displays the Employee of the Month leaderboard at the top middle of the screen.
/// Shows top 3 fake entries plus the player's entry with their current rank.
/// Format: "Rank - Name - Engagement - Sanity"
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The DialogueRunner to read variables from")]
    public DialogueRunner dialogueRunner;

    [Header("Settings")]
    [Tooltip("Update interval in seconds")]
    public float updateInterval = 0.1f;

    [Tooltip("Minimum font size for leaderboard entries")]
    public int minFontSize = 26;

    [Tooltip("Font size for leaderboard text (clamped by Min Font Size)")]
    public int fontSize = 30;

    [Tooltip("Additional font size applied to the title")]
    public int titleFontBonus = 8;

    [Tooltip("Spacing between entries")]
    public float entrySpacing = 36f;

    [Tooltip("Preferred height for each leaderboard entry")]
    public float entryPreferredHeight = 48f;

    [Tooltip("Preferred height for the leaderboard title row")]
    public float titlePreferredHeight = 56f;

    [Header("Layout")]
    [Tooltip("Top margin from the top edge of the safe area")]
    public float topMargin = 36f;

    [Tooltip("Horizontal margin from the safe area's edges")]
    public float horizontalMargin = 32f;

    [Tooltip("Proportion of the safe-area width used by the leaderboard panel")]
    [Range(0.3f, 0.9f)]
    public float widthFraction = 0.65f;

    [Tooltip("Minimum width allowed for the leaderboard panel")]
    public float minPanelWidth = 520f;

    [Tooltip("Maximum width allowed for the leaderboard panel")]
    public float maxPanelWidth = 900f;

    [Tooltip("Padding inside the leaderboard panel (x = left/right, y = top/bottom)")]
    public Vector2 panelPadding = new Vector2(36f, 32f);

    private VariableStorageBehaviour variableStorage;
    private float lastUpdateTime = 0f;

    // UI References
    private GameObject leaderboardPanel;
    private Canvas canvas;
    private List<GameObject> entryObjects = new List<GameObject>();
    private VerticalLayoutGroup layoutGroup;
    private ContentSizeFitter contentSizeFitter;
    private Vector2 lastScreenSize = Vector2.zero;
    private Rect lastSafeArea = Rect.zero;

    // Fake employee data
    private class LeaderboardEntry
    {
        public int rank;
        public string name;
        public float engagement;
        public float sanity;

        public LeaderboardEntry(int rank, string name, float engagement, float sanity)
        {
            this.rank = rank;
            this.name = name;
            this.engagement = engagement;
            this.sanity = sanity;
        }
    }

    private List<LeaderboardEntry> fakeEntries = new List<LeaderboardEntry>();

    private void Awake()
    {
        // Generate fake data
        GenerateFakeEntries();
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

        // Create UI elements if they don't already exist
        if (leaderboardPanel == null)
        {
            CreateUI();
        }
    }

    private void Update()
    {
        EnsurePanelLayout();

        // Update leaderboard at intervals
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateLeaderboard();
            lastUpdateTime = Time.time;
        }
    }

    private int GetEntryFontSize()
    {
        return Mathf.Max(fontSize, minFontSize);
    }

    private int GetTitleFontSize()
    {
        return GetEntryFontSize() + Mathf.Max(titleFontBonus, 0);
    }

    private float GetEntryHeight()
    {
        return Mathf.Max(entryPreferredHeight, GetEntryFontSize() + 12f);
    }

    private float GetTitleHeight()
    {
        return Mathf.Max(titlePreferredHeight, GetTitleFontSize() + 12f);
    }

    /// <summary>
    /// Generate fake employee entries for top 3 positions
    /// </summary>
    private void GenerateFakeEntries()
    {
        fakeEntries.Clear();

        // Generate 3 fake entries with high engagement, low sanity (deterministic values)
        fakeEntries.Add(new LeaderboardEntry(1, "Sarah Chen", 92f, 45f));
        fakeEntries.Add(new LeaderboardEntry(2, "Marcus Rodriguez", 88f, 38f));
        fakeEntries.Add(new LeaderboardEntry(3, "Jessica Kim", 87f, 42f));

        // Sort by engagement descending
        fakeEntries = fakeEntries.OrderByDescending(e => e.engagement).ToList();
        
        // Reassign ranks after sorting
        for (int i = 0; i < fakeEntries.Count; i++)
        {
            fakeEntries[i].rank = i + 1;
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

        // Create leaderboard panel
        leaderboardPanel = new GameObject("LeaderboardPanel");
        leaderboardPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = leaderboardPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = Vector2.zero;

        Image bgImage = leaderboardPanel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f); // Semi-transparent black
        bgImage.raycastTarget = false;

        layoutGroup = leaderboardPanel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.spacing = entrySpacing;

        contentSizeFitter = leaderboardPanel.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Add title as the first child; entries are added during updates
        CreateTitle("Employee of the Month");

        lastScreenSize = Vector2.zero;
        lastSafeArea = new Rect();
        EnsurePanelLayout();
    }

    private void EnsurePanelLayout()
    {
        if (leaderboardPanel == null)
        {
            return;
        }

        if (layoutGroup != null)
        {
            int horizontalPadding = Mathf.RoundToInt(panelPadding.x);
            int verticalPadding = Mathf.RoundToInt(panelPadding.y);

            layoutGroup.padding.left = horizontalPadding;
            layoutGroup.padding.right = horizontalPadding;
            layoutGroup.padding.top = verticalPadding;
            layoutGroup.padding.bottom = verticalPadding;
            layoutGroup.spacing = entrySpacing;
        }

        RectTransform panelRect = leaderboardPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        if (screenSize != lastScreenSize || safeArea != lastSafeArea)
        {
            float safeWidth = Mathf.Max(safeArea.width, 200f);
            float availableWidth = Mathf.Max(safeWidth - (horizontalMargin * 2f), 200f);
            availableWidth = Mathf.Min(availableWidth, safeWidth);

            float clampedFraction = Mathf.Clamp01(widthFraction);
            float targetWidth = safeWidth * clampedFraction;

            float minWidth = Mathf.Min(minPanelWidth, availableWidth);
            float maxWidth = Mathf.Min(maxPanelWidth, availableWidth);
            if (maxWidth < minWidth)
            {
                maxWidth = minWidth;
            }

            targetWidth = Mathf.Clamp(targetWidth, minWidth, maxWidth);

            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

            float safeTopPadding = Screen.height - safeArea.yMax;
            float horizontalOffset = (safeArea.x + (safeArea.width * 0.5f)) - (Screen.width * 0.5f);
            panelRect.anchoredPosition = new Vector2(horizontalOffset, -(safeTopPadding + topMargin));

            lastScreenSize = screenSize;
            lastSafeArea = safeArea;
        }
    }

    /// <summary>
    /// Create the title text
    /// </summary>
    private void CreateTitle(string titleText)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(leaderboardPanel.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = Vector2.zero;
        float titleHeight = GetTitleHeight();
        titleRect.sizeDelta = new Vector2(0f, titleHeight);

        LayoutElement layoutElement = titleObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = titleHeight;
        layoutElement.minHeight = titleHeight;
        layoutElement.flexibleHeight = 0f;
        layoutElement.flexibleWidth = 1f;

#if USE_TMP
        TextMeshProUGUI titleTextComponent = titleObj.AddComponent<TextMeshProUGUI>();
        // Load default font if available
        if (TMPro.TMP_Settings.instance != null && TMPro.TMP_Settings.instance.defaultFontAsset != null)
        {
            titleTextComponent.font = TMPro.TMP_Settings.instance.defaultFontAsset;
        }
        titleTextComponent.text = titleText;
        titleTextComponent.fontSize = GetTitleFontSize();
        titleTextComponent.alignment = TextAlignmentOptions.Center;
        titleTextComponent.color = Color.white;
        titleTextComponent.enableWordWrapping = false;
#else
        Text titleTextComponent = titleObj.AddComponent<Text>();
        titleTextComponent.text = titleText;
        titleTextComponent.fontSize = GetTitleFontSize();
        titleTextComponent.alignment = TextAnchor.MiddleCenter;
        titleTextComponent.color = Color.white;
        // Use default Unity font
        titleTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleTextComponent.verticalOverflow = VerticalWrapMode.Truncate;
        titleTextComponent.raycastTarget = false;
#endif
    }

    /// <summary>
    /// Update the leaderboard with current player data
    /// </summary>
    private void UpdateLeaderboard()
    {
        if (variableStorage == null) return;

        // Get player's current values
        float playerEngagement = 0f;
        float playerSanity = 0f;
        
        variableStorage.TryGetValue<float>("$engagement", out playerEngagement);
        variableStorage.TryGetValue<float>("$sanity", out playerSanity);

        // Create combined list with player entry
        List<LeaderboardEntry> allEntries = new List<LeaderboardEntry>(fakeEntries);
        allEntries.Add(new LeaderboardEntry(0, "You", playerEngagement, playerSanity));

        // Sort by engagement descending
        allEntries = allEntries.OrderByDescending(e => e.engagement).ToList();

        // Reassign ranks
        for (int i = 0; i < allEntries.Count; i++)
        {
            allEntries[i].rank = i + 1;
        }

        // Get top 3 + player entry
        List<LeaderboardEntry> displayEntries = new List<LeaderboardEntry>();
        
        // Add top 3
        for (int i = 0; i < Mathf.Min(3, allEntries.Count); i++)
        {
            displayEntries.Add(allEntries[i]);
        }

        // Find player entry and add if not in top 3
        LeaderboardEntry playerEntry = allEntries.FirstOrDefault(e => e.name == "You");
        if (playerEntry != null && playerEntry.rank > 3)
        {
            displayEntries.Add(playerEntry);
        }

        // Update UI
        UpdateUIEntries(displayEntries);
    }

    /// <summary>
    /// Update or create UI entry objects
    /// </summary>
    private void UpdateUIEntries(List<LeaderboardEntry> entries)
    {
        // Clear existing entry objects
        foreach (GameObject entryObj in entryObjects)
        {
            if (entryObj != null)
            {
                Destroy(entryObj);
            }
        }
        entryObjects.Clear();

        // Create entry objects
        foreach (LeaderboardEntry entry in entries)
        {
            GameObject entryObj = CreateEntryObject(entry);
            entryObjects.Add(entryObj);
        }
    }

    /// <summary>
    /// Create a single entry object
    /// </summary>
    private GameObject CreateEntryObject(LeaderboardEntry entry)
    {
        GameObject entryObj = new GameObject($"Entry_{entry.rank}");
        entryObj.transform.SetParent(leaderboardPanel.transform, false);

        RectTransform entryRect = entryObj.AddComponent<RectTransform>();
        entryRect.anchorMin = new Vector2(0f, 0.5f);
        entryRect.anchorMax = new Vector2(1f, 0.5f);
        entryRect.pivot = new Vector2(0.5f, 0.5f);
        entryRect.anchoredPosition = Vector2.zero;
        float entryHeight = GetEntryHeight();
        entryRect.sizeDelta = new Vector2(0f, entryHeight);

        LayoutElement layoutElement = entryObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = entryHeight;
        layoutElement.minHeight = entryHeight;
        layoutElement.flexibleHeight = 0f;
        layoutElement.flexibleWidth = 1f;

        // Format: "Rank - Name - Engagement - Sanity"
        string entryText = $"{entry.rank} - {entry.name} - {entry.engagement:F0} - {entry.sanity:F0}";

#if USE_TMP
        TextMeshProUGUI text = entryObj.AddComponent<TextMeshProUGUI>();
        // Load default font if available
        if (TMPro.TMP_Settings.instance != null && TMPro.TMP_Settings.instance.defaultFontAsset != null)
        {
            text.font = TMPro.TMP_Settings.instance.defaultFontAsset;
        }
        text.text = entryText;
        text.fontSize = GetEntryFontSize();
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = Vector4.zero;
        text.raycastTarget = false;
        
        // Highlight player entry
        if (entry.name == "You")
        {
            text.color = Color.yellow;
        }
        else
        {
            text.color = Color.white;
        }
#else
        Text text = entryObj.AddComponent<Text>();
        text.text = entryText;
        text.fontSize = GetEntryFontSize();
        text.alignment = TextAnchor.MiddleCenter;
        // Use default Unity font
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        
        // Highlight player entry
        if (entry.name == "You")
        {
            text.color = Color.yellow;
        }
        else
        {
            text.color = Color.white;
        }
#endif

        return entryObj;
    }

    private void OnDestroy()
    {
        // Clean up entry objects
        foreach (GameObject entryObj in entryObjects)
        {
            if (entryObj != null)
            {
                Destroy(entryObj);
            }
        }
    }
}
