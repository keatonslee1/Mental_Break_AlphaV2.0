using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Editor script to automatically set up the load menu UI in the current scene.
/// Usage: Unity Menu -> Tools -> Setup Load Menu UI
/// </summary>
public class LoadMenuSetup : EditorWindow
{
    [MenuItem("Tools/Setup Load Menu UI")]
    public static void SetupLoadMenu()
    {
        // Find or create Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Create EventSystem if it doesn't exist
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            Debug.Log("Created Canvas and EventSystem");
        }

        // Create Load Menu Panel
        GameObject panelObj = null;
        Transform existingPanel = canvas.transform.Find("LoadMenuPanel");
        if (existingPanel != null)
        {
            panelObj = existingPanel.gameObject;
            Debug.Log("Found existing LoadMenuPanel");
        }
        else
        {
            panelObj = CreateLoadPanel(canvas.transform);
            Debug.Log("Created LoadMenuPanel");
        }

        // Create buttons
        CreateSlotButtons(panelObj.transform);

        // Setup SaveSlotSelectionUI component
        SetupSaveSlotSelectionUI(panelObj);

        Debug.Log("Load Menu UI setup complete! Check the Canvas in the Hierarchy.");
        EditorUtility.DisplayDialog("Load Menu Setup", 
            "Load Menu UI has been created successfully!\n\n" + 
            "Next steps:\n" +
            "1. Check the Canvas in Hierarchy\n" +
            "2. Verify SaveSlotSelectionUI component has all references assigned\n" +
            "3. Adjust button positions/sizes as needed\n" +
            "4. Test by pressing Load Game in the pause menu", 
            "OK");
    }

    private static GameObject CreateLoadPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("LoadMenuPanel");
        panelObj.transform.SetParent(parent, false);

        // RectTransform - center anchored, sized to content (like pause menu)
        RectTransform rectTransform = panelObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(250f, 350f); // Sized for 5 buttons + spacing
        rectTransform.anchoredPosition = Vector2.zero;

        // Image - semi-transparent background (matching pause menu style)
        Image image = panelObj.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 200f / 255f);

        // Vertical Layout Group
        VerticalLayoutGroup layoutGroup = panelObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);
        layoutGroup.spacing = 10f;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        // Set inactive by default
        panelObj.SetActive(false);

        return panelObj;
    }

    private static void CreateSlotButtons(Transform panelParent)
    {
        string[] buttonNames = { "Slot1Button", "Slot2Button", "Slot3Button", "Slot4Button", "Slot5Button", "CancelButton" };
        string[] buttonTexts = { "AUTOSAVE", "SLOT 1", "SLOT 2", "SLOT 3", "SLOT 4", "Cancel" };

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
            rectTransform.sizeDelta = new Vector2(0f, 50f); // Width will be controlled by layout group

            // Image (button background)
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Button component
            Button button = buttonObj.AddComponent<Button>();
            
            // Set button colors (matching pause menu style)
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

    private static void SetupSaveSlotSelectionUI(GameObject panelObj)
    {
        // Find existing SaveSlotSelectionUI
        SaveSlotSelectionUI selectionUI = panelObj.GetComponent<SaveSlotSelectionUI>();
        if (selectionUI == null)
        {
            // Create new SaveSlotSelectionUI component
            selectionUI = panelObj.AddComponent<SaveSlotSelectionUI>();
            Debug.Log("Created SaveSlotSelectionUI component");
        }

        // Assign references using SerializedObject for proper undo support
        SerializedObject serializedUI = new SerializedObject(selectionUI);
        
        // Assign selectionPanel
        SerializedProperty panelProp = serializedUI.FindProperty("selectionPanel");
        if (panelProp != null)
        {
            panelProp.objectReferenceValue = panelObj;
        }

        // Assign slot buttons
        AssignButtonReference(serializedUI, "slot1Button", "Slot1Button", panelObj.transform);
        AssignButtonReference(serializedUI, "slot2Button", "Slot2Button", panelObj.transform);
        AssignButtonReference(serializedUI, "slot3Button", "Slot3Button", panelObj.transform);
        AssignButtonReference(serializedUI, "slot4Button", "Slot4Button", panelObj.transform);
        AssignButtonReference(serializedUI, "slot5Button", "Slot5Button", panelObj.transform);
        AssignButtonReference(serializedUI, "cancelButton", "CancelButton", panelObj.transform);

        serializedUI.ApplyModifiedProperties();

        // Auto-find SaveLoadManager and PauseMenuManager (they'll be found at runtime)
        Debug.Log("SaveSlotSelectionUI references assigned. SaveLoadManager and PauseMenuManager will be auto-found at runtime.");
    }

    private static void AssignButtonReference(SerializedObject serializedObject, string propertyName, string buttonName, Transform panelParent)
    {
        SerializedProperty prop = serializedObject.FindProperty(propertyName);
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

