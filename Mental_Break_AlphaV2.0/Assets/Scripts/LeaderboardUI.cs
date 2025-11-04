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

    [Tooltip("Font size for leaderboard text")]
    public int fontSize = 18;

    [Tooltip("Spacing between entries")]
    public float entrySpacing = 30f;

    private VariableStorageBehaviour variableStorage;
    private float lastUpdateTime = 0f;

    // UI References
    private GameObject leaderboardPanel;
    private Canvas canvas;
    private List<GameObject> entryObjects = new List<GameObject>();

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
        // Update leaderboard at intervals
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateLeaderboard();
            lastUpdateTime = Time.time;
        }
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
        
        // Position at top middle
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -20f); // 20 pixels from top
        panelRect.sizeDelta = new Vector2(600f, 200f);

        // Add background
        Image bgImage = leaderboardPanel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f); // Semi-transparent black

        // Add title
        CreateTitle("Employee of the Month");

        // Create entry objects will be done in UpdateLeaderboard
    }

    /// <summary>
    /// Create the title text
    /// </summary>
    private void CreateTitle(string titleText)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(leaderboardPanel.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(0f, 30f);

#if USE_TMP
        TextMeshProUGUI titleTextComponent = titleObj.AddComponent<TextMeshProUGUI>();
        // Load default font if available
        if (TMPro.TMP_Settings.instance != null && TMPro.TMP_Settings.instance.defaultFontAsset != null)
        {
            titleTextComponent.font = TMPro.TMP_Settings.instance.defaultFontAsset;
        }
        titleTextComponent.text = titleText;
        titleTextComponent.fontSize = fontSize + 4;
        titleTextComponent.alignment = TextAlignmentOptions.Center;
        titleTextComponent.color = Color.white;
#else
        Text titleTextComponent = titleObj.AddComponent<Text>();
        titleTextComponent.text = titleText;
        titleTextComponent.fontSize = fontSize + 4;
        titleTextComponent.alignment = TextAnchor.UpperCenter;
        titleTextComponent.color = Color.white;
        // Use default Unity font
        titleTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];
            GameObject entryObj = CreateEntryObject(entry, i);
            entryObjects.Add(entryObj);
        }
    }

    /// <summary>
    /// Create a single entry object
    /// </summary>
    private GameObject CreateEntryObject(LeaderboardEntry entry, int index)
    {
        GameObject entryObj = new GameObject($"Entry_{entry.rank}");
        entryObj.transform.SetParent(leaderboardPanel.transform, false);

        RectTransform entryRect = entryObj.AddComponent<RectTransform>();
        entryRect.anchorMin = new Vector2(0f, 1f);
        entryRect.anchorMax = new Vector2(1f, 1f);
        entryRect.pivot = new Vector2(0.5f, 1f);
        entryRect.anchoredPosition = new Vector2(0f, -50f - (index * entrySpacing)); // Start below title
        entryRect.sizeDelta = new Vector2(0f, 25f);

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
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        
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
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        // Use default Unity font
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
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
