#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Yarn.Unity;

/// <summary>
/// Editor utility to automatically add CharacterSpriteManager to the scene.
/// </summary>
public class SetupCharacterSpriteManager
{
    [MenuItem("Tools/Setup Character Sprite Manager")]
    public static void Setup()
    {
        // Find or create CharacterSpriteManager
        CharacterSpriteManager manager = Object.FindAnyObjectByType<CharacterSpriteManager>();
        
        if (manager == null)
        {
            // Try to find GameManager or DialogueRunner to attach to
            GameObject parentObject = null;
            
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                parentObject = gameManager.gameObject;
            }
            else
            {
                DialogueRunner dialogueRunner = Object.FindAnyObjectByType<DialogueRunner>();
                if (dialogueRunner != null)
                {
                    parentObject = dialogueRunner.gameObject;
                }
            }
            
            if (parentObject != null)
            {
                // Add to existing GameObject
                manager = parentObject.AddComponent<CharacterSpriteManager>();
                Debug.Log($"CharacterSpriteManager: Added to existing GameObject '{parentObject.name}'");
            }
            else
            {
                // Create a new GameObject for the manager
                GameObject managerObj = new GameObject("CharacterSpriteManager");
                manager = managerObj.AddComponent<CharacterSpriteManager>();
                Debug.Log("CharacterSpriteManager: Created new GameObject and added component.");
            }
            
            // Mark scene as dirty so changes are saved
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        else
        {
            Debug.Log("CharacterSpriteManager: Already exists in scene.");
        }
        
        // Auto-configure references if they're null
        if (manager != null)
        {
            SerializedObject so = new SerializedObject(manager);
            
            // Auto-find DialogueRunner if not set
            SerializedProperty dialogueRunnerProp = so.FindProperty("dialogueRunner");
            if (dialogueRunnerProp.objectReferenceValue == null)
            {
                DialogueRunner runner = Object.FindAnyObjectByType<DialogueRunner>();
                if (runner != null)
                {
                    dialogueRunnerProp.objectReferenceValue = runner;
                    Debug.Log("CharacterSpriteManager: Auto-assigned DialogueRunner reference.");
                }
            }
            
            // Auto-find Canvas if not set
            SerializedProperty canvasProp = so.FindProperty("targetCanvas");
            if (canvasProp.objectReferenceValue == null)
            {
                Canvas canvas = Object.FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    canvasProp.objectReferenceValue = canvas;
                    Debug.Log("CharacterSpriteManager: Auto-assigned Canvas reference.");
                }
            }
            
            so.ApplyModifiedProperties();
        }
        
        // Select it in the hierarchy
        if (manager != null)
        {
            Selection.activeGameObject = manager.gameObject;
            EditorGUIUtility.PingObject(manager.gameObject);
        }
        
        Debug.Log("CharacterSpriteManager setup complete!");
    }
}
#endif

