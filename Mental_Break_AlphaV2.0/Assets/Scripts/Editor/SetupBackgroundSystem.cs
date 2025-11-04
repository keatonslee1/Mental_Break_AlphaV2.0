using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool to automatically set up the scene background system.
/// Creates a full-screen background GameObject with BackgroundCommandHandler.
/// Access via: Tools > Yarn Spinner > Setup Background System
/// </summary>
public class SetupBackgroundSystem
{
    private const string BACKGROUND_NAME = "Scene Background";

    [MenuItem("Tools/Yarn Spinner/Setup Background System")]
    public static void Setup()
    {
        // Get active scene
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("No valid scene is open. Please open a scene first.");
            return;
        }

        // Find Canvas - try Dialogue System's Canvas first, then any Canvas
        Canvas canvas = null;
        
        GameObject dialogueSystem = GameObject.Find("Dialogue System");
        if (dialogueSystem != null)
        {
            canvas = dialogueSystem.GetComponentInChildren<Canvas>();
        }
        
        if (canvas == null)
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene! Please ensure a Canvas exists (or add Dialogue System first).");
            return;
        }

        Debug.Log($"Found Canvas: {canvas.name}");

        // Check if Scene Background already exists
        Transform canvasTransform = canvas.transform;
        Transform existingBackground = canvasTransform.Find(BACKGROUND_NAME);
        
        GameObject backgroundGO;
        
        if (existingBackground != null)
        {
            backgroundGO = existingBackground.gameObject;
            Debug.Log($"Found existing '{BACKGROUND_NAME}' GameObject. Verifying configuration...");
            
            // Verify it has Image component
            Image existingImage = backgroundGO.GetComponent<Image>();
            if (existingImage == null)
            {
                existingImage = backgroundGO.AddComponent<Image>();
                Debug.Log("Added Image component to existing Scene Background");
            }

            // Verify it has BackgroundCommandHandler
            BackgroundCommandHandler existingHandler = backgroundGO.GetComponent<BackgroundCommandHandler>();
            if (existingHandler == null)
            {
                existingHandler = backgroundGO.AddComponent<BackgroundCommandHandler>();
                Debug.Log("Added BackgroundCommandHandler to existing Scene Background");
            }

            // Verify configuration
            VerifyAndFixConfiguration(backgroundGO, existingImage, existingHandler);
            
            EditorUtility.SetDirty(backgroundGO);
            EditorUtility.SetDirty(existingHandler);
            EditorSceneManager.MarkSceneDirty(scene);
            
            Debug.Log($"✅ Verified and configured existing '{BACKGROUND_NAME}' GameObject.");
            return;
        }

        // Create new Scene Background GameObject
        Debug.Log($"Creating new '{BACKGROUND_NAME}' GameObject...");
        backgroundGO = new GameObject(BACKGROUND_NAME);
        backgroundGO.transform.SetParent(canvasTransform, false);

        // Add RectTransform component (automatically added, but ensure it exists)
        RectTransform rectTransform = backgroundGO.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = backgroundGO.AddComponent<RectTransform>();
        }

        // Configure RectTransform to stretch across entire screen
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        Debug.Log("Configured RectTransform to stretch across entire screen");

        // Move to top of Canvas children (renders behind dialogue UI)
        backgroundGO.transform.SetSiblingIndex(0);
        Debug.Log("Moved Scene Background to render behind other UI elements");

        // Add Image component
        Image image = backgroundGO.AddComponent<Image>();
        Debug.Log("Added Image component");

        // Add BackgroundCommandHandler component
        BackgroundCommandHandler handler = backgroundGO.AddComponent<BackgroundCommandHandler>();
        Debug.Log("Added BackgroundCommandHandler component");

        // Configure BackgroundCommandHandler to reference the Image component
        var handlerSO = new SerializedObject(handler);
        var imageProp = handlerSO.FindProperty("backgroundImage");
        if (imageProp != null)
        {
            imageProp.objectReferenceValue = image;
            handlerSO.ApplyModifiedProperties();
            Debug.Log("Configured BackgroundCommandHandler to reference Image component");
        }
        else
        {
            Debug.LogWarning("Could not find 'backgroundImage' property on BackgroundCommandHandler. Please assign it manually in the Inspector.");
        }

        // Mark objects as dirty
        EditorUtility.SetDirty(backgroundGO);
        EditorUtility.SetDirty(handler);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"✅ Successfully created and configured '{BACKGROUND_NAME}' GameObject!");
        Debug.Log("Next steps:");
        Debug.Log("1. Use 'Tools > Yarn Spinner > Auto-Map Backgrounds' to map background sprites");
        Debug.Log("2. Or manually assign background sprites in the BackgroundCommandHandler Inspector");
    }

    /// <summary>
    /// Verifies and fixes the configuration of an existing Scene Background GameObject.
    /// </summary>
    private static void VerifyAndFixConfiguration(GameObject backgroundGO, Image image, BackgroundCommandHandler handler)
    {
        bool needsUpdate = false;

        // Verify RectTransform configuration
        RectTransform rectTransform = backgroundGO.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            if (rectTransform.anchorMin != Vector2.zero || 
                rectTransform.anchorMax != Vector2.one ||
                rectTransform.offsetMin != Vector2.zero ||
                rectTransform.offsetMax != Vector2.zero)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                needsUpdate = true;
                Debug.Log("Fixed RectTransform configuration to stretch across screen");
            }

            // Ensure it's at the top (renders behind)
            if (rectTransform.GetSiblingIndex() != 0)
            {
                rectTransform.SetSiblingIndex(0);
                needsUpdate = true;
                Debug.Log("Moved Scene Background to render behind other UI elements");
            }
        }

        // Verify BackgroundCommandHandler has Image reference
        var handlerSO = new SerializedObject(handler);
        var imageProp = handlerSO.FindProperty("backgroundImage");
        if (imageProp != null && imageProp.objectReferenceValue != image)
        {
            imageProp.objectReferenceValue = image;
            handlerSO.ApplyModifiedProperties();
            needsUpdate = true;
            Debug.Log("Fixed BackgroundCommandHandler Image reference");
        }

        if (needsUpdate)
        {
            EditorUtility.SetDirty(backgroundGO);
            EditorUtility.SetDirty(handler);
        }
    }
}

