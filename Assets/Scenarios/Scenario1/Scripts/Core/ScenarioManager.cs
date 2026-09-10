using System;
using UnityEngine;
using UnityEngine.Events;

namespace CADT.VR.Core
{
    public enum ScenarioPhase
    {
        NotStarted,
        Briefing,
        ActiveInvestigation,
        BreachContainment,
        DebriefResults
    }

    /// <summary>
    /// Abstract coordinator managing scenario lifecycle, phase transitions,
    /// score tracking, and debrief resolution across all training modules.
    /// </summary>
    [RequireComponent(typeof(ScoreTracker))]
    public abstract class ScenarioManager : MonoBehaviour
    {
        [Header("--- Scenario Config ---")]
        [SerializeField] protected ScenarioDataSO scenarioData;

        [Header("--- Lifecycle Phases ---")]
        [SerializeField] protected ScenarioPhase currentPhase = ScenarioPhase.NotStarted;

        [Header("--- Common Events ---")]
        public UnityEvent onBriefingStarted;
        public UnityEvent onInvestigationStarted;
        public UnityEvent onBreachTriggered;
        public UnityEvent onScenarioResolved;
        public UnityEvent onDebriefCompleted;

        protected ScoreTracker scoreTracker;

        public ScenarioDataSO ScenarioData => scenarioData;
        public ScenarioPhase CurrentPhase => currentPhase;
        public ScoreTracker Tracker => scoreTracker;

        protected virtual void Awake()
        {
            scoreTracker = GetComponent<ScoreTracker>();
        }

        protected virtual void Start()
        {
            StartBriefing();
        }

        public virtual void StartBriefing()
        {
            currentPhase = ScenarioPhase.Briefing;
            onBriefingStarted?.Invoke();
        }

        public virtual void BeginInvestigation()
        {
            currentPhase = ScenarioPhase.ActiveInvestigation;
            if (scoreTracker != null) scoreTracker.StartTracking();
            onInvestigationStarted?.Invoke();
        }

        public virtual void TriggerBreach()
        {
            currentPhase = ScenarioPhase.BreachContainment;
            if (scoreTracker != null) scoreTracker.RecordMistake();
            onBreachTriggered?.Invoke();
        }

        public virtual void CompleteScenario(bool success)
        {
            currentPhase = ScenarioPhase.DebriefResults;
            if (scoreTracker != null) scoreTracker.StopTracking();

            int xp = scoreTracker != null && scenarioData != null 
                ? scoreTracker.CalculateTotalXP(scenarioData) 
                : 50;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteScenario(xp);
            }

            onScenarioResolved?.Invoke();
        }

        public virtual void ReturnToMenu()
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
    }
}
