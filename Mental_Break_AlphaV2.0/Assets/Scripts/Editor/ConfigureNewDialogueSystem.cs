using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Editor tool to configure the new Yarn Spinner Dialogue System prefab.
/// </summary>
public class ConfigureNewDialogueSystem
{
    [MenuItem("Tools/Yarn Spinner/Configure New Dialogue System")]
    public static void Configure()
    {
        // Ensure MVPScene is open
        if (EditorSceneManager.GetActiveScene().name != "MVPScene")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MVPScene.unity/MVPScene.unity", OpenSceneMode.Single);
        }

        // Find the new Dialogue System (not the _OLD one)
        GameObject dialogueSystem = GameObject.Find("Dialogue System");
        if (dialogueSystem == null)
        {
            Debug.LogError("Dialogue System not found in MVPScene!");
            return;
        }

        DialogueRunner runner = dialogueSystem.GetComponent<DialogueRunner>();
        if (runner == null)
        {
            Debug.LogError("DialogueRunner not found on Dialogue System!");
            return;
        }

        // Load YarnProject
        YarnProject project = AssetDatabase.LoadAssetAtPath<YarnProject>("Assets/Dialogue/MentalBreakDialogue.yarnproject");
        if (project == null)
        {
            Debug.LogError("Could not find MentalBreakDialogue.yarnproject!");
            return;
        }

        // Configure DialogueRunner
        var so = new SerializedObject(runner);
        so.FindProperty("yarnProject").objectReferenceValue = project;
        so.FindProperty("startNode").stringValue = "R1_Start";
        so.ApplyModifiedProperties();

        // Ensure MVPCommandHandlers is added
        if (dialogueSystem.GetComponent<MVPCommandHandlers>() == null)
        {
            dialogueSystem.AddComponent<MVPCommandHandlers>();
            Debug.Log("Added MVPCommandHandlers to Dialogue System");
        }

        // Ensure StartDialogueOnPlay is added
        StartDialogueOnPlay startDialogue = dialogueSystem.GetComponent<StartDialogueOnPlay>();
        if (startDialogue == null)
        {
            startDialogue = dialogueSystem.AddComponent<StartDialogueOnPlay>();
        }
        var startSO = new SerializedObject(startDialogue);
        startSO.FindProperty("startNode").stringValue = "R1_Start";
        startSO.FindProperty("dialogueRunner").objectReferenceValue = runner;
        startSO.ApplyModifiedProperties();
        Debug.Log("Configured StartDialogueOnPlay");

        // Configure Line Advancer (for dialogue navigation)
        LineAdvancer lineAdvancer = dialogueSystem.GetComponentInChildren<LineAdvancer>();
        if (lineAdvancer != null)
        {
            var advancerSO = new SerializedObject(lineAdvancer);
            var runnerProp = advancerSO.FindProperty("runner");
            var presenterProp = advancerSO.FindProperty("presenter");
            
            if (runnerProp.objectReferenceValue == null && runner != null)
            {
                runnerProp.objectReferenceValue = runner;
                Debug.Log("Configured Line Advancer: assigned DialogueRunner");
            }
            
            LinePresenter linePresenter = dialogueSystem.GetComponentInChildren<LinePresenter>();
            if (presenterProp.objectReferenceValue == null && linePresenter != null)
            {
                presenterProp.objectReferenceValue = linePresenter;
                Debug.Log("Configured Line Advancer: assigned Line Presenter");
            }
            
            advancerSO.ApplyModifiedProperties();
        }

        // Configure Options Presenter (for choices)
        OptionsPresenter optionsPresenter = dialogueSystem.GetComponentInChildren<OptionsPresenter>();
        if (optionsPresenter != null)
        {
            var optionsSO = new SerializedObject(optionsPresenter);
            var optionViewPrefabProp = optionsSO.FindProperty("optionViewPrefab");
            
            // Load and assign Option Item prefab if not already assigned
            if (optionViewPrefabProp.objectReferenceValue == null)
            {
                GameObject optionItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Packages/dev.yarnspinner.unity/Prefabs/Option Item.prefab");
                
                if (optionItemPrefab != null)
                {
                    optionViewPrefabProp.objectReferenceValue = optionItemPrefab;
                    Debug.Log("Configured Options Presenter: assigned Option Item prefab");
                }
                else
                {
                    Debug.LogWarning("Could not find Option Item prefab at 'Packages/dev.yarnspinner.unity/Prefabs/Option Item.prefab'");
                }
            }
            
            // Ensure Canvas Group is assigned
            var canvasGroupProp = optionsSO.FindProperty("canvasGroup");
            CanvasGroup canvasGroup = optionsPresenter.GetComponent<CanvasGroup>();
            if (canvasGroupProp.objectReferenceValue == null && canvasGroup != null)
            {
                canvasGroupProp.objectReferenceValue = canvasGroup;
                Debug.Log("Configured Options Presenter: assigned Canvas Group");
            }
            
            optionsSO.ApplyModifiedProperties();
        }

        // Ensure presenters are registered with Dialogue Runner
        LinePresenter linePresenterCheck = dialogueSystem.GetComponentInChildren<LinePresenter>();
        var runnerSO = new SerializedObject(runner);
        var presentersProp = runnerSO.FindProperty("dialoguePresenters");
        
        if (presentersProp != null && presentersProp.isArray)
        {
            bool hasLinePresenter = false;
            bool hasOptionsPresenter = false;
            bool hasLineAdvancer = false;
            
            for (int i = 0; i < presentersProp.arraySize; i++)
            {
                var element = presentersProp.GetArrayElementAtIndex(i);
                var objRef = element.objectReferenceValue;
                
                if (objRef == linePresenterCheck) hasLinePresenter = true;
                if (objRef == optionsPresenter) hasOptionsPresenter = true;
                if (objRef == lineAdvancer) hasLineAdvancer = true;
            }
            
            // Add missing presenters (order matters - Options Presenter should be registered)
            if (linePresenterCheck != null && !hasLinePresenter)
            {
                presentersProp.arraySize++;
                presentersProp.GetArrayElementAtIndex(presentersProp.arraySize - 1).objectReferenceValue = linePresenterCheck;
                Debug.Log("Registered Line Presenter with Dialogue Runner");
            }
            if (optionsPresenter != null && !hasOptionsPresenter)
            {
                presentersProp.arraySize++;
                presentersProp.GetArrayElementAtIndex(presentersProp.arraySize - 1).objectReferenceValue = optionsPresenter;
                Debug.Log("Registered Options Presenter with Dialogue Runner (CRITICAL for choices to work!)");
            }
            if (lineAdvancer != null && !hasLineAdvancer)
            {
                presentersProp.arraySize++;
                presentersProp.GetArrayElementAtIndex(presentersProp.arraySize - 1).objectReferenceValue = lineAdvancer;
                Debug.Log("Registered Line Advancer with Dialogue Runner");
            }
            
            runnerSO.ApplyModifiedProperties();
        }

        // Add Choice Diagnostics component (optional, for debugging)
        ChoiceDiagnostics diagnostics = dialogueSystem.GetComponent<ChoiceDiagnostics>();
        if (diagnostics == null)
        {
            diagnostics = dialogueSystem.AddComponent<ChoiceDiagnostics>();
            var diagSO = new SerializedObject(diagnostics);
            diagSO.FindProperty("enableDebugLogging").boolValue = true;
            diagSO.ApplyModifiedProperties();
            Debug.Log("Added ChoiceDiagnostics component for debugging");
        }

        EditorUtility.SetDirty(runner);
        EditorUtility.SetDirty(startDialogue);
        if (lineAdvancer != null) EditorUtility.SetDirty(lineAdvancer);
        if (optionsPresenter != null) EditorUtility.SetDirty(optionsPresenter);
        if (diagnostics != null) EditorUtility.SetDirty(diagnostics);
        EditorUtility.SetDirty(dialogueSystem);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        
        Debug.Log("Configured new Dialogue System successfully!");
        Debug.Log("NOTE: If choices still don't work, check the Console for errors when choices appear.");
        Debug.Log("      Look for 'No dialogue view returned an option selection!' - this means Options Presenter failed.");
    }
}

