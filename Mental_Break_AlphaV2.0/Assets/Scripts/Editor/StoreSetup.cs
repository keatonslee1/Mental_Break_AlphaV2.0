using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Yarn.Unity;

#if USE_TMP
using TMPro;
#endif

/// <summary>
/// Editor script to automatically set up the Company Store UI in the current scene.
/// Usage: Unity Menu -> Tools -> Setup Store UI
/// </summary>
public class StoreSetup : EditorWindow
{
    [MenuItem("Tools/Setup Store UI")]
    public static void SetupStore()
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

        // Create Store Panel
        GameObject panelObj = null;
        Transform existingPanel = canvas.transform.Find("StorePanel");
        if (existingPanel != null)
        {
            panelObj = existingPanel.gameObject;
            Debug.Log("Found existing StorePanel");
        }
        else
        {
            panelObj = CreateStorePanel(canvas.transform);
            Debug.Log("Created StorePanel");
        }

        // Create UI elements inside the panel
        CreateStoreUIElements(panelObj.transform);

        // Setup StoreUI component
        SetupStoreUI(panelObj);

        Debug.Log("Store UI setup complete! Check the Canvas in the Hierarchy.");
        
        EditorUtility.DisplayDialog("Store UI Setup Complete",
            "Store UI has been created successfully!\n\n" +
            "Next steps:\n" +
            "1. Check the Canvas in Hierarchy\n" +
            "2. Verify StoreUI component has all references assigned\n" +
            "3. Adjust panel/button positions/sizes as needed\n" +
            "4. Ensure DialogueRunner is assigned to StoreUI\n" +
            "5. Test by triggering SHOP_Visit node in Play mode",
            "OK");
    }

    private static GameObject CreateStorePanel(Transform parent)
    {
        GameObject panelObj = new GameObject("StorePanel");
        panelObj.transform.SetParent(parent, false);

        // Add RectTransform and set size
        RectTransform rectTransform = panelObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(700, 600);
        rectTransform.anchoredPosition = Vector2.zero;

        // Add Image component for background
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Dark semi-transparent background

        return panelObj;
    }

    private static void CreateStoreUIElements(Transform panelTransform)
    {
        // Panel is 700x600
        // We'll use a clear vertical layout:
        // - Top: Title (40px) at y = -20
        // - Credits bar (60px reserved space) starting at y = -70
        // - Item scroll area (starts at y = -140, goes to y = -200 from bottom)
        // - Bottom: Pass button (50px) and notification (30px) = 80px reserved

        // Create Title Text
        GameObject titleObj = CreateTextElement(panelTransform, "StoreTitleText", "Company Store", 28, TextAnchor.UpperCenter);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = new Vector2(-40, 40);
        titleRect.anchoredPosition = new Vector2(0, -20);

        // Create Credits Display Area (reserved space at top)
        // Background for credits
        GameObject creditsBgObj = new GameObject("CreditsBackground");
        creditsBgObj.transform.SetParent(panelTransform, false);
        RectTransform creditsBgRect = creditsBgObj.AddComponent<RectTransform>();
        creditsBgRect.anchorMin = new Vector2(0, 1);
        creditsBgRect.anchorMax = new Vector2(1, 1);
        creditsBgRect.sizeDelta = new Vector2(-20, 60);
        creditsBgRect.anchoredPosition = new Vector2(0, -70);
        
        Image creditsBgImage = creditsBgObj.AddComponent<Image>();
        creditsBgImage.color = new Color(0.15f, 0.15f, 0.25f, 0.9f); // Dark background for contrast
        
        // Credits Text
        GameObject cashObj = CreateTextElement(panelTransform, "CashText", "Available Credits: 0", 22, TextAnchor.UpperLeft);
        RectTransform cashRect = cashObj.GetComponent<RectTransform>();
        cashRect.anchorMin = new Vector2(0, 1);
        cashRect.anchorMax = new Vector2(1, 1);
        cashRect.sizeDelta = new Vector2(-40, 50);
        cashRect.anchoredPosition = new Vector2(20, -75);

        // Create Item Buttons Container (Scroll View)
        // Position it BELOW the credits area with clear separation
        GameObject scrollViewObj = new GameObject("ItemButtonsContainer");
        scrollViewObj.transform.SetParent(panelTransform, false);
        
        RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
        // Anchor to fill space between credits (top) and pass button (bottom)
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        // offsetMin = bottom-left offset, offsetMax = top-right offset
        // Bottom: 140px for pass button + notification
        // Top: 140px for title + credits area
        scrollRect.offsetMin = new Vector2(20, 140); // Left: 20px, Bottom: 140px
        scrollRect.offsetMax = new Vector2(-20, -140); // Right: -20px, Top: -140px

        // Add Image component for raycasting (required for ScrollRect to receive input)
        Image scrollViewImage = scrollViewObj.AddComponent<Image>();
        scrollViewImage.color = new Color(0, 0, 0, 0); // Transparent but still receives raycasts

        // Add ScrollRect component
        ScrollRect scrollRectComponent = scrollViewObj.AddComponent<ScrollRect>();
        scrollRectComponent.horizontal = false;
        scrollRectComponent.vertical = true;

        // Create Content area for buttons
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollViewObj.transform, false);
        
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        contentRect.anchoredPosition = Vector2.zero;

        // Add Vertical Layout Group
        VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;

        // Add Content Size Fitter
        ContentSizeFitter sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRectComponent.content = contentRect;

        // Create Scrollbar
        GameObject scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(scrollViewObj.transform, false);
        
        RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.sizeDelta = new Vector2(20, 0);
        scrollbarRect.anchoredPosition = Vector2.zero;

        Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // Create Scrollbar background
        GameObject scrollbarBg = new GameObject("Background");
        scrollbarBg.transform.SetParent(scrollbarObj.transform, false);
        Image bgImage = scrollbarBg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform bgRect = scrollbarBg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        scrollbar.targetGraphic = bgImage;

        // Create Scrollbar handle
        GameObject scrollbarHandle = new GameObject("Handle");
        scrollbarHandle.transform.SetParent(scrollbarObj.transform, false);
        Image handleImage = scrollbarHandle.AddComponent<Image>();
        handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        RectTransform handleRect = scrollbarHandle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.sizeDelta = Vector2.zero;
        scrollbar.handleRect = handleRect;

        scrollRectComponent.verticalScrollbar = scrollbar;

        // Create Item Button Prefab (simple button)
        GameObject buttonPrefab = CreateItemButtonPrefab(contentObj.transform);
        buttonPrefab.SetActive(false); // Prefab should be inactive

        // Create Pass Without Buying Button (at bottom)
        GameObject passButtonObj = new GameObject("PassWithoutBuyingButton");
        passButtonObj.transform.SetParent(panelTransform, false);

        RectTransform passRect = passButtonObj.AddComponent<RectTransform>();
        passRect.anchorMin = new Vector2(0, 0);
        passRect.anchorMax = new Vector2(1, 0);
        passRect.sizeDelta = new Vector2(-40, 50);
        passRect.anchoredPosition = new Vector2(0, 80);

        Image passImage = passButtonObj.AddComponent<Image>();
        passImage.color = new Color(0.2f, 0.25f, 0.3f, 1f);

        Button passButton = passButtonObj.AddComponent<Button>();
        ColorBlock passColors = passButton.colors;
        passColors.normalColor = new Color(0.2f, 0.25f, 0.3f, 1f);
        passColors.highlightedColor = new Color(0.35f, 0.4f, 0.45f, 1f);
        passColors.pressedColor = new Color(0.15f, 0.2f, 0.25f, 1f);
        passColors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        passColors.colorMultiplier = 1f;
        passButton.colors = passColors;

        GameObject passTextObj = CreateTextElement(passButtonObj.transform, "PassText", "Pass Without Buying", 18, TextAnchor.MiddleCenter);
        RectTransform passTextRect = passTextObj.GetComponent<RectTransform>();
        passTextRect.anchorMin = Vector2.zero;
        passTextRect.anchorMax = Vector2.one;
        passTextRect.sizeDelta = Vector2.zero;
        passTextRect.anchoredPosition = Vector2.zero;

        // Create Notification Text (above pass button)
        GameObject notificationObj = CreateTextElement(panelTransform, "NotificationText", "", 14, TextAnchor.LowerCenter);
        RectTransform notificationRect = notificationObj.GetComponent<RectTransform>();
        notificationRect.anchorMin = new Vector2(0, 0);
        notificationRect.anchorMax = new Vector2(1, 0);
        notificationRect.sizeDelta = new Vector2(-40, 30);
        notificationRect.anchoredPosition = new Vector2(0, 140);

        // Create Close Button (X in top-right corner)
        GameObject closeButtonObj = new GameObject("CloseButton");
        closeButtonObj.transform.SetParent(panelTransform, false);
        
        RectTransform closeRect = closeButtonObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.sizeDelta = new Vector2(35, 35);
        closeRect.anchoredPosition = new Vector2(-17, -17);

        Image closeButtonImage = closeButtonObj.AddComponent<Image>();
        closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        Button closeButton = closeButtonObj.AddComponent<Button>();

        // Create Close Button Text (X)
        GameObject closeTextObj = CreateTextElement(closeButtonObj.transform, "CloseText", "X", 22, TextAnchor.MiddleCenter);
        RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.sizeDelta = Vector2.zero;
        closeTextRect.anchoredPosition = Vector2.zero;
    }

    private static GameObject CreateTextElement(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();

#if USE_TMP
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        
        // Convert TextAnchor to TextAlignmentOptions
        switch (alignment)
        {
            case TextAnchor.UpperLeft:
                textComponent.alignment = TextAlignmentOptions.TopLeft;
                break;
            case TextAnchor.UpperCenter:
                textComponent.alignment = TextAlignmentOptions.Top;
                break;
            case TextAnchor.UpperRight:
                textComponent.alignment = TextAlignmentOptions.TopRight;
                break;
            case TextAnchor.MiddleLeft:
                textComponent.alignment = TextAlignmentOptions.MidlineLeft;
                break;
            case TextAnchor.MiddleCenter:
                textComponent.alignment = TextAlignmentOptions.Midline;
                break;
            case TextAnchor.MiddleRight:
                textComponent.alignment = TextAlignmentOptions.MidlineRight;
                break;
            case TextAnchor.LowerLeft:
                textComponent.alignment = TextAlignmentOptions.BottomLeft;
                break;
            case TextAnchor.LowerCenter:
                textComponent.alignment = TextAlignmentOptions.Bottom;
                break;
            case TextAnchor.LowerRight:
                textComponent.alignment = TextAlignmentOptions.BottomRight;
                break;
            default:
                textComponent.alignment = TextAlignmentOptions.Midline;
                break;
        }
#else
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#endif

        return textObj;
    }

    private static GameObject CreateItemButtonPrefab(Transform parent)
    {
        GameObject buttonObj = new GameObject("ItemButtonPrefab");
        buttonObj.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0, 90); // Increased height for better readability

        // Create button background
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.25f, 1f); // Darker background for contrast

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.4f, 1f); // Brighter on hover
        colors.pressedColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        // Create button text - left aligned so cost appears at beginning
        GameObject textObj = CreateTextElement(buttonObj.transform, "ButtonText", "[10 Credits] Item Name\nDescription\nStatus", 14, TextAnchor.UpperLeft);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-20, -10); // Padding on all sides
        textRect.anchoredPosition = new Vector2(10, -5);

#if USE_TMP
        // Enable word wrapping for TMP
        TMPro.TextMeshProUGUI tmpText = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.enableWordWrapping = true;
        }
#else
        // Enable word wrapping for legacy Text
        Text regularText = textObj.GetComponent<Text>();
        if (regularText != null)
        {
            regularText.horizontalOverflow = HorizontalWrapMode.Wrap;
            regularText.verticalOverflow = VerticalWrapMode.Truncate;
        }
#endif

        return buttonObj;
    }

    private static void SetupStoreUI(GameObject panelObj)
    {
        // Find or create StoreUI component
        StoreUI storeUI = panelObj.GetComponent<StoreUI>();
        if (storeUI == null)
        {
            storeUI = panelObj.AddComponent<StoreUI>();
            Debug.Log("Added StoreUI component to StorePanel");
        }

        // Find DialogueRunner
        DialogueRunner dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        if (dialogueRunner != null)
        {
            storeUI.dialogueRunner = dialogueRunner;
            Debug.Log("Assigned DialogueRunner to StoreUI");
        }
        else
        {
            Debug.LogWarning("DialogueRunner not found in scene. Please assign it manually to StoreUI.");
        }

        // Assign panel reference
        storeUI.storePanel = panelObj;

        // Find and assign UI element references
        Transform panelTransform = panelObj.transform;

        // Close Button
        Transform closeButtonTransform = panelTransform.Find("CloseButton");
        if (closeButtonTransform != null)
        {
            storeUI.closeButton = closeButtonTransform.GetComponent<Button>();
            Debug.Log("Assigned CloseButton to StoreUI");
        }

        // Pass Without Buying Button
        Transform passButtonTransform = panelTransform.Find("PassWithoutBuyingButton");
        if (passButtonTransform != null)
        {
            storeUI.passWithoutBuyingButton = passButtonTransform.GetComponent<Button>();
            Debug.Log("Assigned PassWithoutBuyingButton to StoreUI");
        }

        // Cash Text
        Transform cashTextTransform = panelTransform.Find("CashText");
        if (cashTextTransform != null)
        {
#if USE_TMP
            storeUI.cashText = cashTextTransform.GetComponent<TMPro.TextMeshProUGUI>();
#else
            storeUI.cashText = cashTextTransform.GetComponent<Text>();
#endif
            Debug.Log("Assigned CashText to StoreUI");
        }

        // Item Button Container
        Transform containerTransform = panelTransform.Find("ItemButtonsContainer");
        if (containerTransform != null)
        {
            storeUI.itemButtonContainer = containerTransform.Find("Content");
            if (storeUI.itemButtonContainer == null)
            {
                storeUI.itemButtonContainer = containerTransform;
            }
            Debug.Log("Assigned ItemButtonContainer to StoreUI");
        }

        // Item Button Prefab
        Transform prefabTransform = panelTransform.Find("ItemButtonsContainer/Content/ItemButtonPrefab");
        if (prefabTransform != null)
        {
            storeUI.itemButtonPrefab = prefabTransform.gameObject;
            Debug.Log("Assigned ItemButtonPrefab to StoreUI");
        }

        // Notification Text
        Transform notificationTransform = panelTransform.Find("NotificationText");
        if (notificationTransform != null)
        {
#if USE_TMP
            storeUI.notificationText = notificationTransform.GetComponent<TMPro.TextMeshProUGUI>();
#else
            storeUI.notificationText = notificationTransform.GetComponent<Text>();
#endif
            Debug.Log("Assigned NotificationText to StoreUI");
        }

        Debug.Log("StoreUI component setup complete!");
    }
}
