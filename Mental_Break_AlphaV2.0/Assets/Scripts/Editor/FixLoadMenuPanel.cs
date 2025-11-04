using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Quick fix script to resize and configure the existing LoadMenuPanel
/// Usage: Select LoadMenuPanel in hierarchy, then Tools -> Fix Load Menu Panel Size
/// </summary>
public class FixLoadMenuPanel : EditorWindow
{
    [MenuItem("Tools/Fix Load Menu Panel Size")]
    public static void FixPanelSize()
    {
        // Try to find LoadMenuPanel in selection first
        GameObject panelObj = null;
        
        if (Selection.activeGameObject != null)
        {
            if (Selection.activeGameObject.name == "LoadMenuPanel")
            {
                panelObj = Selection.activeGameObject;
            }
            else
            {
                // Search in children
                Transform found = Selection.activeGameObject.transform.Find("LoadMenuPanel");
                if (found != null)
                {
                    panelObj = found.gameObject;
                }
            }
        }

        // If not found in selection, search scene
        if (panelObj == null)
        {
            // Try finding by name
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
            EditorUtility.DisplayDialog("Fix Load Menu Panel", 
                "LoadMenuPanel not found!\n\nPlease select the LoadMenuPanel GameObject in the hierarchy, or run Tools -> Setup Load Menu UI to create it.", 
                "OK");
            return;
        }

        // Fix RectTransform
        RectTransform rectTransform = panelObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(250f, 350f);
            rectTransform.anchoredPosition = Vector2.zero;
            
            Debug.Log($"Fixed LoadMenuPanel size and position");
        }

        // Ensure VerticalLayoutGroup is configured correctly
        VerticalLayoutGroup layoutGroup = panelObj.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.padding = new RectOffset(20, 20, 20, 20);
            layoutGroup.spacing = 10f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            
            Debug.Log($"Fixed LoadMenuPanel layout group settings");
        }

        // Remove ContentSizeFitter if it exists (causes issues)
        ContentSizeFitter fitter = panelObj.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            DestroyImmediate(fitter);
            Debug.Log($"Removed ContentSizeFitter from LoadMenuPanel");
        }

        // Fix button sizes
        foreach (Transform child in panelObj.transform)
        {
            if (child.name.Contains("Button"))
            {
                RectTransform buttonRect = child.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.sizeDelta = new Vector2(0f, 50f); // Width controlled by layout
                    Debug.Log($"Fixed button size: {child.name}");
                }
            }
        }

        EditorUtility.DisplayDialog("Fix Complete", 
            $"Fixed LoadMenuPanel sizing and layout!\n\n" +
            $"Panel is now center-anchored and sized to 250x350.\n" +
            $"Buttons will be sized by the layout group.\n\n" +
            $"Make sure SaveSlotSelectionUI component is on LoadMenuPanel with all button references assigned.", 
            "OK");
    }
}

