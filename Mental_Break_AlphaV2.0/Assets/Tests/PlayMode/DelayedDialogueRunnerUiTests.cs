using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Yarn.Unity;

public class DelayedDialogueRunnerUiTests
{
    [UnityTest]
    public IEnumerator LeaderboardAndMetricsRecoverWhenDialogueRunnerAppears()
    {
        var leaderboardGO = new GameObject("LeaderboardUI_Test");
        var leaderboard = leaderboardGO.AddComponent<LeaderboardUI>();

        var metricsPanelGO = new GameObject("MetricsPanelUI_Test");
        var metricsPanel = metricsPanelGO.AddComponent<MetricsPanelUI>();

        // Allow Awake/OnEnable + watcher registration to complete.
        yield return null;
        yield return new WaitForSeconds(0.1f);

        var initialLeaderboardTexts = GetAllText(leaderboardGO).ToList();
        Assert.IsTrue(initialLeaderboardTexts.Any(text => text.Contains("Loading")), "Leaderboard should display loading placeholder before DialogueRunner exists.");

        var initialMetricTexts = GetAllText(metricsPanelGO).ToList();
        Assert.IsTrue(initialMetricTexts.Any(text => text.Contains("--")), "Metrics panel should display placeholder values before DialogueRunner exists.");

        var runnerGO = new GameObject("DialogueRunner_Test");
        var runner = runnerGO.AddComponent<DialogueRunner>();
        var storage = runnerGO.AddComponent<InMemoryVariableStorage>();
        runner.VariableStorage = storage;

        storage.SetValue("$engagement", 73f);
        storage.SetValue("$sanity", 42f);

        // Allow the watcher and UI scripts to observe the new runtime and refresh.
        yield return new WaitForSeconds(0.6f);

        var updatedLeaderboardTexts = GetAllText(leaderboardGO).ToList();
        Assert.IsTrue(updatedLeaderboardTexts.Any(text => text.Contains("You") && text.Contains("73") && text.Contains("42")),
            "Leaderboard should include the player entry once DialogueRunner is present.");

        var updatedMetricTexts = GetAllText(metricsPanelGO).ToList();
        Assert.IsTrue(updatedMetricTexts.Any(text => text.Contains("Engagement: 73")), "Metrics panel should show updated engagement value.");
        Assert.IsTrue(updatedMetricTexts.Any(text => text.Contains("Sanity: 42")), "Metrics panel should show updated sanity value.");

        // Cleanup
        Object.DestroyImmediate(leaderboardGO);
        Object.DestroyImmediate(metricsPanelGO);
        Object.DestroyImmediate(runnerGO);

        var watcher = Object.FindObjectOfType<DialogueRuntimeWatcher>();
        if (watcher != null)
        {
            Object.DestroyImmediate(watcher.gameObject);
        }
    }

    private static IEnumerable<string> GetAllText(GameObject root)
    {
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp != null)
            {
                yield return tmp.text;
            }
        }

        foreach (var text in root.GetComponentsInChildren<Text>(true))
        {
            if (text != null)
            {
                yield return text.text;
            }
        }
    }
}

