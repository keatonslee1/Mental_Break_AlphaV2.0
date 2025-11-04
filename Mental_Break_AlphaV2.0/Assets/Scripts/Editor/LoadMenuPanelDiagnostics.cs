using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Diagnostic tool to check LoadMenuPanel configuration and identify issues
/// Usage: Select LoadMenuPanel in hierarchy, then Tools -> Diagnose LoadMenuPanel
/// </summary>
public class LoadMenuPanelDiagnostics : EditorWindow
{
    [MenuItem("Tools/Diagnose LoadMenuPanel")]
    public static void DiagnosePanel()
    {
        GameObject panelObj = null;
        
        // Try to find LoadMenuPanel
        if (Selection.activeGameObject != null)
        {
            if (Selection.activeGameObject.name == "LoadMenuPanel")
            {
                panelObj = Selection.activeGameObject;
            }
            else
            {
                Transform found = Selection.activeGameObject.transform.Find("LoadMenuPanel");
                if (found != null)
                {
                    panelObj = found.gameObject;
                }
            }
        }

        // Search scene
        if (panelObj == null)
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "LoadMenuPanel")
                {
                    panelObj = obj;
                    break;
                }
            }
        }

        if (panelObj == null)
        {
            EditorUtility.DisplayDialog("Diagnostic", "LoadMenuPanel not found in scene!", "OK");
            return;
        }

        string report = GenerateDiagnosticReport(panelObj);
        
        Debug.Log("=== LoadMenuPanel Diagnostic Report ===\n" + report);
        
        EditorUtility.DisplayDialog("Diagnostic Complete", 
            "Diagnostic report logged to Console.\n\nCheck the Console window for detailed information.", 
            "OK");
    }

    private static string GenerateDiagnosticReport(GameObject panel)
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        
        report.AppendLine($"=== LoadMenuPanel Diagnostic: {panel.name} ===");
        report.AppendLine($"Active: {panel.activeSelf}");
        report.AppendLine($"Active in Hierarchy: {panel.activeInHierarchy}");
        report.AppendLine();
        
        // RectTransform
        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            report.AppendLine("--- RectTransform ---");
            report.AppendLine($"Anchor Min: {rect.anchorMin}");
            report.AppendLine($"Anchor Max: {rect.anchorMax}");
            report.AppendLine($"Pivot: {rect.pivot}");
            report.AppendLine($"Size Delta: {rect.sizeDelta}");
            report.AppendLine($"Anchored Position: {rect.anchoredPosition}");
            report.AppendLine($"Rect: {rect.rect}");
            report.AppendLine($"Is Full-Screen Stretch: {(rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one)}");
            report.AppendLine();
        }
        else
        {
            report.AppendLine("ERROR: No RectTransform component!");
            report.AppendLine();
        }

        // Image Component
        Image image = panel.GetComponent<Image>();
        if (image != null)
        {
            report.AppendLine("--- Image Component ---");
            report.AppendLine($"Color: {image.color}");
            report.AppendLine($"Alpha: {image.color.a}");
            report.AppendLine($"Raycast Target: {image.raycastTarget}");
            report.AppendLine($"Enabled: {image.enabled}");
            report.AppendLine();
        }
        else
        {
            report.AppendLine("WARNING: No Image component!");
            report.AppendLine();
        }

        // VerticalLayoutGroup
        VerticalLayoutGroup layoutGroup = panel.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            report.AppendLine("--- VerticalLayoutGroup ---");
            report.AppendLine($"Padding: {layoutGroup.padding}");
            report.AppendLine($"Spacing: {layoutGroup.spacing}");
            report.AppendLine($"Child Alignment: {layoutGroup.childAlignment}");
            report.AppendLine($"Child Control Width: {layoutGroup.childControlWidth}");
            report.AppendLine($"Child Control Height: {layoutGroup.childControlHeight}");
            report.AppendLine($"Child Force Expand Width: {layoutGroup.childForceExpandWidth}");
            report.AppendLine($"Child Force Expand Height: {layoutGroup.childForceExpandHeight}");
            report.AppendLine($"Enabled: {layoutGroup.enabled}");
            report.AppendLine();
        }
        else
        {
            report.AppendLine("WARNING: No VerticalLayoutGroup component!");
            report.AppendLine();
        }

        // ContentSizeFitter
        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            report.AppendLine("--- ContentSizeFitter ---");
            report.AppendLine($"Horizontal Fit: {fitter.horizontalFit}");
            report.AppendLine($"Vertical Fit: {fitter.verticalFit}");
            report.AppendLine($"Enabled: {fitter.enabled}");
            report.AppendLine();
        }

        // Canvas/CanvasGroup
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            report.AppendLine("--- Parent Canvas ---");
            report.AppendLine($"Name: {canvas.name}");
            report.AppendLine($"Render Mode: {canvas.renderMode}");
            report.AppendLine($"Sorting Order: {canvas.sortingOrder}");
            report.AppendLine();
        }

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            report.AppendLine("--- CanvasGroup ---");
            report.AppendLine($"Alpha: {canvasGroup.alpha}");
            report.AppendLine($"Interactable: {canvasGroup.interactable}");
            report.AppendLine($"Blocks Raycasts: {canvasGroup.blocksRaycasts}");
            report.AppendLine();
        }

        // Parent
        Transform parent = panel.transform.parent;
        if (parent != null)
        {
            report.AppendLine("--- Parent ---");
            report.AppendLine($"Name: {parent.name}");
            report.AppendLine($"Active: {parent.gameObject.activeSelf}");
            report.AppendLine($"Active in Hierarchy: {parent.gameObject.activeInHierarchy}");
            
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                report.AppendLine($"Parent Rect: {parentRect.rect}");
                report.AppendLine($"Parent Anchor Min: {parentRect.anchorMin}");
                report.AppendLine($"Parent Anchor Max: {parentRect.anchorMax}");
            }
            report.AppendLine();
        }

        // Child Buttons
        report.AppendLine("--- Child Buttons ---");
        int childCount = panel.transform.childCount;
        report.AppendLine($"Total Children: {childCount}");
        
        string[] expectedButtons = { "Slot1Button", "Slot2Button", "Slot3Button", "Slot4Button", "Slot5Button", "CancelButton" };
        
        foreach (string buttonName in expectedButtons)
        {
            Transform child = panel.transform.Find(buttonName);
            if (child != null)
            {
                report.AppendLine($"  {buttonName}:");
                report.AppendLine($"    Active: {child.gameObject.activeSelf}");
                report.AppendLine($"    Active in Hierarchy: {child.gameObject.activeInHierarchy}");
                
                RectTransform childRect = child.GetComponent<RectTransform>();
                if (childRect != null)
                {
                    report.AppendLine($"    Size Delta: {childRect.sizeDelta}");
                    report.AppendLine($"    Rect: {childRect.rect}");
                }
                
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    report.AppendLine($"    Button Enabled: {button.enabled}");
                    report.AppendLine($"    Button Interactable: {button.interactable}");
                }
                else
                {
                    report.AppendLine($"    WARNING: No Button component!");
                }
                
                Image buttonImage = child.GetComponent<Image>();
                if (buttonImage != null)
                {
                    report.AppendLine($"    Image Color: {buttonImage.color}");
                    report.AppendLine($"    Image Alpha: {buttonImage.color.a}");
                    report.AppendLine($"    Image Enabled: {buttonImage.enabled}");
                }
                else
                {
                    report.AppendLine($"    WARNING: No Image component on button!");
                }
                
                // Check for Text component
                Transform textChild = child.Find("Text");
                if (textChild != null)
                {
                    report.AppendLine($"    Has Text Child: Yes (Active: {textChild.gameObject.activeSelf})");
                }
                else
                {
                    report.AppendLine($"    WARNING: No Text child found!");
                }
                
                report.AppendLine();
            }
            else
            {
                report.AppendLine($"  {buttonName}: NOT FOUND!");
                report.AppendLine();
            }
        }

        // Sibling Index (for draw order)
        report.AppendLine("--- Sibling Index (Draw Order) ---");
        report.AppendLine($"Panel Sibling Index: {panel.transform.GetSiblingIndex()}");
        if (parent != null)
        {
            report.AppendLine($"Total Siblings: {parent.childCount}");
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform sibling = parent.GetChild(i);
                report.AppendLine($"  [{i}] {sibling.name} (Active: {sibling.gameObject.activeSelf})");
            }
        }
        
        return report.ToString();
    }
}

