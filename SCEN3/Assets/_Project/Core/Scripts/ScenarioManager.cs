using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ScenarioManager : MonoBehaviour
{
    public enum OutcomeType
    {
        Failure,
        Partial,
        Success,
        Ideal
    }

    [Header("Scenario Configuration")]
    [SerializeField] private int scenarioNumber = 3;
    [SerializeField] private string scenarioTitle = "The Fake Link Hijacking";
    [TextArea(2, 4)]
    [SerializeField] private string scenarioDescription = "Identify social engineering tactics, inspect suspicious URLs, and prevent Telegram session hijacking.";

    [Header("Phase Management")]
    [SerializeField] protected int currentPhaseIndex = 0;
    public int CurrentPhaseIndex => currentPhaseIndex;

    [Header("Red Flags")]
    [SerializeField] private int totalRedFlags = 4;
    private readonly HashSet<string> discoveredRedFlags = new HashSet<string>();

    [Header("Events")]
    public UnityEvent<int> onPhaseChanged;
    public UnityEvent<string, int, int> onRedFlagDiscovered; // flagId, count, total
    public UnityEvent<OutcomeType, int> onScenarioEnded;     // outcome, totalXP

    public int DiscoveredRedFlagsCount => discoveredRedFlags.Count;
    public int TotalRedFlags => totalRedFlags;
    public string ScenarioTitle => scenarioTitle;
    public int ScenarioNumber => scenarioNumber;

    protected virtual void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectScenario(scenarioNumber);
        }
    }

    public virtual void SetPhase(int phaseIndex)
    {
        currentPhaseIndex = phaseIndex;
        Debug.Log($"[ScenarioManager] Entered Phase {phaseIndex} for Scenario {scenarioNumber}: {scenarioTitle}");
        onPhaseChanged?.Invoke(phaseIndex);
    }

    public virtual bool RegisterRedFlag(string flagId, string flagDescription = "")
    {
        if (string.IsNullOrEmpty(flagId)) return false;

        if (discoveredRedFlags.Add(flagId))
        {
            Debug.Log($"[ScenarioManager] Red Flag Discovered [{discoveredRedFlags.Count}/{totalRedFlags}]: {flagId} - {flagDescription}");
            onRedFlagDiscovered?.Invoke(flagId, discoveredRedFlags.Count, totalRedFlags);
            return true;
        }

        return false;
    }

    public virtual void CompleteScenario(OutcomeType outcome)
    {
        Debug.Log($"[ScenarioManager] Scenario {scenarioNumber} finished with outcome: {outcome}");

        if (ScoreTracker.Instance != null)
        {
            ScoreTracker.Instance.RecordOutcome(outcome);
        }

        int earnedXP = outcome switch
        {
            OutcomeType.Failure => 0,
            OutcomeType.Partial => 50,
            OutcomeType.Success => 100,
            OutcomeType.Ideal => 200,
            _ => 0
        };

        onScenarioEnded?.Invoke(outcome, earnedXP);
    }

    public virtual void ReturnToMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    public virtual void ReturnToScenarioSelection()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScenarioSelection();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("ScenarioSelection");
        }
    }

    public virtual void RestartScenario()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
