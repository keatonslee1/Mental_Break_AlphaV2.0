using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Editor script to automatically set up the pause menu UI in the current scene.
/// Usage: Unity Menu -> Tools -> Setup Pause Menu UI
/// </summary>
public class PauseMenuSetup : EditorWindow
{
    [MenuItem("Tools/Setup Pause Menu UI")]
    public static void SetupPauseMenu()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Create EventSystem if it doesn't exist
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            Debug.Log("Created Canvas and EventSystem");
        }

        // Create Pause Hint Text
        GameObject hintTextObj = null;
        Transform existingHint = canvas.transform.Find("PauseHintText");
        if (existingHint != null)
        {
            hintTextObj = existingHint.gameObject;
            Debug.Log("Found existing PauseHintText");
        }
        else
        {
            hintTextObj = CreateHintText(canvas.transform);
            Debug.Log("Created PauseHintText");
        }

        // Create Pause Menu Panel
        GameObject panelObj = null;
        Transform existingPanel = canvas.transform.Find("PauseMenuPanel");
        if (existingPanel != null)
        {
            panelObj = existingPanel.gameObject;
            Debug.Log("Found existing PauseMenuPanel");
        }
        else
        {
            panelObj = CreatePausePanel(canvas.transform);
            Debug.Log("Created PauseMenuPanel");
        }

        // Create buttons
        CreateMenuButtons(panelObj.transform);

        // Setup PauseMenuManager
        SetupPauseMenuManager(hintTextObj, panelObj);

        Debug.Log("Pause Menu UI setup complete! Check the Canvas in the Hierarchy.");
        
        // Check if LoadMenuPanel exists
        bool loadMenuExists = false;
        Transform loadMenuPanel = canvas.transform.Find("LoadMenuPanel");
        if (loadMenuPanel == null)
        {
            // Also check if it's a child of PauseMenuPanel
            loadMenuPanel = panelObj.transform.Find("LoadMenuPanel");
        }
        loadMenuExists = loadMenuPanel != null;
        
        string dialogMessage = "Pause Menu UI has been created successfully!\n\n" +
            "Next steps:\n" +
            "1. Check the Canvas in Hierarchy\n" +
            "2. Verify PauseMenuManager component has all references assigned\n" +
            "3. Adjust button positions/sizes as needed\n";
        
        if (!loadMenuExists)
        {
            dialogMessage += "4. Run 'Tools -> Setup Load Menu UI' to create the Load Menu Panel\n";
            dialogMessage += "5. Test by pressing ESC in Play mode";
        }
        else
        {
            dialogMessage += "4. Test by pressing ESC in Play mode";
        }
        
        EditorUtility.DisplayDialog("Pause Menu Setup", dialogMessage, "OK");
    }

    private static GameObject CreateHintText(Transform parent)
    {
        GameObject hintObj = new GameObject("PauseHintText");
        hintObj.transform.SetParent(parent, false);

        RectTransform rectTransform = hintObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-20f, -20f);
        rectTransform.sizeDelta = new Vector2(200f, 30f);

#if USE_TMP
        TextMeshProUGUI text = hintObj.AddComponent<TextMeshProUGUI>();
        text.text = "Press ESC to pause";
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopRight;
#else
        Text text = hintObj.AddComponent<Text>();
        text.text = "Press ESC to pause";
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperRight;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

        return hintObj;
    }

    private static GameObject CreatePausePanel(Transform parent)
    {
        GameObject panelObj = new GameObject("PauseMenuPanel");
        panelObj.transform.SetParent(parent, false);

        // RectTransform - full screen
        RectTransform rectTransform = panelObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        // Image - semi-transparent background
        Image image = panelObj.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 200f / 255f);

        // Vertical Layout Group
        VerticalLayoutGroup layoutGroup = panelObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(50, 50, 50, 50);
        layoutGroup.spacing = 20f;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        // Content Size Fitter (optional, helps with layout)
        ContentSizeFitter fitter = panelObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Set inactive by default
        panelObj.SetActive(false);

        return panelObj;
    }

    private static void CreateMenuButtons(Transform panelParent)
    {
        string[] buttonNames = { "ResumeButton", "SaveGameButton", "LoadGameButton", "MainMenuButton", "ExitButton", "SkipDayButton", "RestartDayButton" };
        string[] buttonTexts = { "Resume", "Save Game", "Load Game", "Main Menu", "Exit to Desktop", "Skip Day", "Restart Day" };

        for (int i = 0; i < buttonNames.Length; i++)
        {
            // Check if button already exists
            Transform existingButton = panelParent.Find(buttonNames[i]);
            if (existingButton != null)
            {
                Debug.Log($"Button {buttonNames[i]} already exists, skipping");
                continue;
            }

            GameObject buttonObj = new GameObject(buttonNames[i]);
            buttonObj.transform.SetParent(panelParent, false);

            // RectTransform
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200f, 50f);

            // Image (button background)
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Button component
            Button button = buttonObj.AddComponent<Button>();
            
            // Set button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            button.colors = colors;

            // Create text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

#if USE_TMP
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = buttonTexts[i];
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
#else
            Text text = textObj.AddComponent<Text>();
            text.text = buttonTexts[i];
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#endif

            Debug.Log($"Created button: {buttonNames[i]}");
        }
    }

    private static void SetupPauseMenuManager(GameObject hintTextObj, GameObject panelObj)
    {
        // Find existing PauseMenuManager
        PauseMenuManager manager = FindObjectOfType<PauseMenuManager>();
        if (manager == null)
        {
            // Create new GameObject for PauseMenuManager
            GameObject managerObj = new GameObject("PauseMenuManager");
            manager = managerObj.AddComponent<PauseMenuManager>();
            Debug.Log("Created PauseMenuManager GameObject");
        }

        // Assign references using SerializedObject for proper undo support
        SerializedObject serializedManager = new SerializedObject(manager);
        
        // Assign hint text
        SerializedProperty hintProp = serializedManager.FindProperty("pauseHintText");
        if (hintProp != null)
        {
#if USE_TMP
            TextMeshProUGUI hintText = hintTextObj.GetComponent<TextMeshProUGUI>();
            if (hintText != null)
            {
                hintProp.objectReferenceValue = hintText;
            }
#else
            Text hintText = hintTextObj.GetComponent<Text>();
            if (hintText != null)
            {
                hintProp.objectReferenceValue = hintText;
            }
#endif
        }

        // Assign panel
        SerializedProperty panelProp = serializedManager.FindProperty("pauseMenuPanel");
        if (panelProp != null)
        {
            panelProp.objectReferenceValue = panelObj;
        }

        // Assign buttons
        AssignButtonReference(serializedManager, "resumeButton", "ResumeButton", panelObj.transform);
        AssignButtonReference(serializedManager, "saveGameButton", "SaveGameButton", panelObj.transform);
        AssignButtonReference(serializedManager, "loadGameButton", "LoadGameButton", panelObj.transform);
        AssignButtonReference(serializedManager, "mainMenuButton", "MainMenuButton", panelObj.transform);
        AssignButtonReference(serializedManager, "exitButton", "ExitButton", panelObj.transform);
        AssignButtonReference(serializedManager, "skipDayButton", "SkipDayButton", panelObj.transform);
        AssignButtonReference(serializedManager, "restartDayButton", "RestartDayButton", panelObj.transform);

        serializedManager.ApplyModifiedProperties();

        // Auto-find SaveLoadManager and DialogueRunner (they'll be found at runtime)
        Debug.Log("PauseMenuManager references assigned. SaveLoadManager and DialogueRunner will be auto-found at runtime.");
    }

    private static void AssignButtonReference(SerializedObject serializedManager, string propertyName, string buttonName, Transform panelParent)
    {
        SerializedProperty prop = serializedManager.FindProperty(propertyName);
        if (prop != null)
        {
            Transform buttonTransform = panelParent.Find(buttonName);
            if (buttonTransform != null)
            {
                Button button = buttonTransform.GetComponent<Button>();
                if (button != null)
                {
                    prop.objectReferenceValue = button;
                }
            }
        }
    }
}

