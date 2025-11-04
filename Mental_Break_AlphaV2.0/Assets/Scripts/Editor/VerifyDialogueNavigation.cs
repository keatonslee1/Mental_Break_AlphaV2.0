using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Editor tool to verify dialogue navigation components are properly configured.
/// Checks Line Advancer, Options Presenter, and Line Presenter connections.
/// </summary>
public class VerifyDialogueNavigation
{
    [MenuItem("Tools/Yarn Spinner/Verify Dialogue Navigation")]
    public static void Verify()
    {
        // Ensure MVPScene is open
        if (EditorSceneManager.GetActiveScene().name != "MVPScene")
        {
            if (!EditorUtility.DisplayDialog("Scene Not Open", 
                "MVPScene is not the active scene. Open it now?", 
                "Yes", "No"))
            {
                return;
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MVPScene.unity/MVPScene.unity", OpenSceneMode.Single);
        }

        // Find the Dialogue System
        GameObject dialogueSystem = GameObject.Find("Dialogue System");
        if (dialogueSystem == null)
        {
            Debug.LogError("VerifyDialogueNavigation: Dialogue System not found in MVPScene!");
            EditorUtility.DisplayDialog("Error", "Dialogue System not found in MVPScene!", "OK");
            return;
        }

        Debug.Log("=== Verifying Dialogue Navigation Setup ===");

        bool allGood = true;

        // 1. Verify Dialogue Runner
        DialogueRunner runner = dialogueSystem.GetComponent<DialogueRunner>();
        if (runner == null)
        {
            Debug.LogError("VerifyDialogueNavigation: DialogueRunner not found!");
            allGood = false;
        }
        else
        {
            Debug.Log("✓ DialogueRunner found");
        }

        // 2. Verify Line Presenter
        LinePresenter linePresenter = dialogueSystem.GetComponentInChildren<LinePresenter>();
        if (linePresenter == null)
        {
            Debug.LogError("VerifyDialogueNavigation: Line Presenter not found!");
            allGood = false;
        }
        else
        {
            Debug.Log("✓ Line Presenter found");
        }

        // 3. Verify Line Advancer
        LineAdvancer lineAdvancer = dialogueSystem.GetComponentInChildren<LineAdvancer>();
        if (lineAdvancer == null)
        {
            Debug.LogError("VerifyDialogueNavigation: Line Advancer not found!");
            allGood = false;
        }
        else
        {
            Debug.Log("✓ Line Advancer found");

            // Check Line Advancer configuration
            var advancerSO = new SerializedObject(lineAdvancer);
            var runnerProp = advancerSO.FindProperty("runner");
            var presenterProp = advancerSO.FindProperty("presenter");

            if (runnerProp.objectReferenceValue == null)
            {
                Debug.LogWarning("VerifyDialogueNavigation: Line Advancer 'runner' field is not assigned!");
                if (runner != null)
                {
                    runnerProp.objectReferenceValue = runner;
                    advancerSO.ApplyModifiedProperties();
                    Debug.Log("✓ Fixed: Assigned DialogueRunner to Line Advancer");
                }
                else
                {
                    allGood = false;
                }
            }
            else
            {
                Debug.Log("✓ Line Advancer 'runner' field is assigned");
            }

            if (presenterProp.objectReferenceValue == null)
            {
                Debug.LogWarning("VerifyDialogueNavigation: Line Advancer 'presenter' field is not assigned!");
                if (linePresenter != null)
                {
                    presenterProp.objectReferenceValue = linePresenter;
                    advancerSO.ApplyModifiedProperties();
                    Debug.Log("✓ Fixed: Assigned Line Presenter to Line Advancer");
                }
                else
                {
                    allGood = false;
                }
            }
            else
            {
                Debug.Log("✓ Line Advancer 'presenter' field is assigned");
            }

            // Check input mode
            var inputModeProp = advancerSO.FindProperty("inputMode");
            if (inputModeProp != null)
            {
                int inputMode = inputModeProp.enumValueIndex;
                Debug.Log($"✓ Line Advancer input mode: {(LineAdvancer.InputMode)inputMode}");
            }
        }

        // 4. Verify Options Presenter
        OptionsPresenter optionsPresenter = dialogueSystem.GetComponentInChildren<OptionsPresenter>();
        if (optionsPresenter == null)
        {
            Debug.LogError("VerifyDialogueNavigation: Options Presenter not found!");
            allGood = false;
        }
        else
        {
            Debug.Log("✓ Options Presenter found");
            
            // Check if Options Presenter is enabled
            if (!optionsPresenter.isActiveAndEnabled)
            {
                Debug.LogError("VerifyDialogueNavigation: Options Presenter is not active/enabled!");
                allGood = false;
            }
            else
            {
                Debug.Log("✓ Options Presenter is active and enabled");
            }

            // Check Options Presenter configuration
            var optionsSO = new SerializedObject(optionsPresenter);
            var optionViewPrefabProp = optionsSO.FindProperty("optionViewPrefab");

            if (optionViewPrefabProp.objectReferenceValue == null)
            {
                Debug.LogWarning("VerifyDialogueNavigation: Options Presenter 'optionViewPrefab' is not assigned!");
                // Try to load the Option Item prefab from the package
                GameObject optionItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Packages/dev.yarnspinner.unity/Prefabs/Option Item.prefab");
                
                if (optionItemPrefab != null)
                {
                    optionViewPrefabProp.objectReferenceValue = optionItemPrefab;
                    optionsSO.ApplyModifiedProperties();
                    Debug.Log("✓ Fixed: Assigned Option Item prefab to Options Presenter");
                }
                else
                {
                    Debug.LogError("VerifyDialogueNavigation: Could not find Option Item prefab at 'Packages/dev.yarnspinner.unity/Prefabs/Option Item.prefab'");
                    allGood = false;
                }
            }
            else
            {
                Debug.Log("✓ Options Presenter 'optionViewPrefab' is assigned");
                // Verify it's the correct prefab
                GameObject prefab = optionViewPrefabProp.objectReferenceValue as GameObject;
                if (prefab != null && prefab.GetComponent<OptionItem>() == null)
                {
                    Debug.LogWarning("VerifyDialogueNavigation: Assigned prefab does not have OptionItem component!");
                }
            }

            // Check canvas group
            var canvasGroupProp = optionsSO.FindProperty("canvasGroup");
            CanvasGroup canvasGroup = optionsPresenter.GetComponent<CanvasGroup>();
            if (canvasGroupProp.objectReferenceValue == null)
            {
                if (canvasGroup != null)
                {
                    canvasGroupProp.objectReferenceValue = canvasGroup;
                    optionsSO.ApplyModifiedProperties();
                    Debug.Log("✓ Fixed: Assigned Canvas Group to Options Presenter");
                }
                else
                {
                    Debug.LogWarning("VerifyDialogueNavigation: Options Presenter 'canvasGroup' is not assigned and no CanvasGroup component found!");
                }
            }
            else
            {
                Debug.Log("✓ Options Presenter 'canvasGroup' is assigned");
            }
        }

        // 5. Verify Dialogue Runner has presenters registered
        if (runner != null)
        {
            var runnerSO = new SerializedObject(runner);
            var presentersProp = runnerSO.FindProperty("dialoguePresenters");
            
            if (presentersProp != null && presentersProp.isArray)
            {
                int presenterCount = presentersProp.arraySize;
                Debug.Log($"✓ Dialogue Runner has {presenterCount} presenter(s) registered");

                bool hasLinePresenter = false;
                bool hasOptionsPresenter = false;
                bool hasLineAdvancer = false;

                for (int i = 0; i < presenterCount; i++)
                {
                    var element = presentersProp.GetArrayElementAtIndex(i);
                    var objRef = element.objectReferenceValue;
                    
                    if (objRef is LinePresenter)
                    {
                        hasLinePresenter = true;
                    }
                    if (objRef is OptionsPresenter)
                    {
                        hasOptionsPresenter = true;
                    }
                    if (objRef is LineAdvancer)
                    {
                        hasLineAdvancer = true;
                    }
                }

                // Try to fix missing registrations
                bool needsFix = false;

                if (linePresenter != null && !hasLinePresenter)
                {
                    Debug.LogWarning("VerifyDialogueNavigation: Line Presenter is not registered with Dialogue Runner!");
                    presentersProp.arraySize = presenterCount + 1;
                    presentersProp.GetArrayElementAtIndex(presenterCount).objectReferenceValue = linePresenter;
                    needsFix = true;
                }

                if (optionsPresenter != null && !hasOptionsPresenter)
                {
                    Debug.LogWarning("VerifyDialogueNavigation: Options Presenter is not registered with Dialogue Runner!");
                    if (!needsFix)
                    {
                        presentersProp.arraySize = presenterCount + 1;
                        presentersProp.GetArrayElementAtIndex(presenterCount).objectReferenceValue = optionsPresenter;
                    }
                    else
                    {
                        presentersProp.arraySize = presenterCount + 2;
                        presentersProp.GetArrayElementAtIndex(presenterCount + 1).objectReferenceValue = optionsPresenter;
                    }
                    needsFix = true;
                }

                if (lineAdvancer != null && !hasLineAdvancer)
                {
                    Debug.LogWarning("VerifyDialogueNavigation: Line Advancer is not registered with Dialogue Runner!");
                    if (!needsFix)
                    {
                        presentersProp.arraySize = presenterCount + 1;
                        presentersProp.GetArrayElementAtIndex(presenterCount).objectReferenceValue = lineAdvancer;
                    }
                    else
                    {
                        int newSize = presentersProp.arraySize;
                        presentersProp.arraySize = newSize + 1;
                        presentersProp.GetArrayElementAtIndex(newSize).objectReferenceValue = lineAdvancer;
                    }
                    needsFix = true;
                }

                if (needsFix)
                {
                    runnerSO.ApplyModifiedProperties();
                    Debug.Log("✓ Fixed: Registered missing presenters with Dialogue Runner");
                }

                if (hasLinePresenter) Debug.Log("✓ Line Presenter is registered");
                if (hasOptionsPresenter) 
                {
                    Debug.Log("✓ Options Presenter is registered");
                    
                    // Verify Options Presenter is not null in the list
                    for (int i = 0; i < presentersProp.arraySize; i++)
                    {
                        var element = presentersProp.GetArrayElementAtIndex(i);
                        var objRef = element.objectReferenceValue;
                        if (objRef is OptionsPresenter op && op == optionsPresenter)
                        {
                            if (op == null)
                            {
                                Debug.LogError($"VerifyDialogueNavigation: Options Presenter at index {i} is null!");
                                allGood = false;
                            }
                            else if (!op.isActiveAndEnabled)
                            {
                                Debug.LogError($"VerifyDialogueNavigation: Options Presenter at index {i} is not active/enabled!");
                                allGood = false;
                            }
                        }
                    }
                }
                if (hasLineAdvancer) Debug.Log("✓ Line Advancer is registered");
            }
        }

        // 6. Check for Line Presenter button handler (optional, for mouse click)
        if (linePresenter != null)
        {
            var buttonHandler = linePresenter.GetComponent<LinePresenterButtonHandler>();
            if (buttonHandler == null)
            {
                Debug.Log("ℹ Line Presenter does not have a button handler (mouse click won't advance dialogue)");
                Debug.Log("  This is OK if using keyboard input only");
            }
            else
            {
                Debug.Log("✓ Line Presenter has button handler (mouse click support)");
            }
        }

        // 7. Verify Options Presenter can handle choices
        if (optionsPresenter != null && runner != null)
        {
            var optionsSO = new SerializedObject(optionsPresenter);
            var optionViewPrefabProp = optionsSO.FindProperty("optionViewPrefab");
            
            if (optionViewPrefabProp.objectReferenceValue == null)
            {
                Debug.LogError("VerifyDialogueNavigation: CRITICAL - Options Presenter 'optionViewPrefab' is NULL!");
                Debug.LogError("  This will cause choices to fail! The Options Presenter cannot create option buttons without a prefab.");
                Debug.LogError("  Run 'Tools > Yarn Spinner > Configure New Dialogue System' to fix this.");
                allGood = false;
            }
            else
            {
                GameObject prefab = optionViewPrefabProp.objectReferenceValue as GameObject;
                if (prefab != null)
                {
                    OptionItem optionItem = prefab.GetComponent<OptionItem>();
                    if (optionItem == null)
                    {
                        Debug.LogError("VerifyDialogueNavigation: Option Item prefab does not have OptionItem component!");
                        allGood = false;
                    }
                    else
                    {
                        Debug.Log("✓ Option Item prefab is valid and has OptionItem component");
                    }
                }
            }
        }

        // Mark scene as dirty if we made changes
        if (allGood)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("=== Verification Complete: All checks passed! ===");
            EditorUtility.DisplayDialog("Verification Complete", 
                "Dialogue navigation verification complete!\nAll components are properly configured.", 
                "OK");
        }
        else
        {
            Debug.LogWarning("=== Verification Complete: Some issues found (see above) ===");
            EditorUtility.DisplayDialog("Verification Complete", 
                "Dialogue navigation verification complete.\nSome issues were found - see Console for details.\nAttempted fixes have been applied.", 
                "OK");
        }
    }
}
