using System;
using UnityEngine;
using UnityEngine.Events;
using CADT.VR.Core;

namespace CADT.VR.Templates
{
    /// <summary>
    /// Copy-paste template for building new interactive cybersecurity scenarios.
    /// Inherits from ScenarioManager to ensure universal score tracking, briefing,
    /// and debrief compatibility.
    /// </summary>
    [DisallowMultipleComponent]
    public class _ScenarioTemplate_Manager : ScenarioManager
    {
        [Header("--- Template Scenario Specifics ---")]
        [Tooltip("Interactive physical prop or workstation monitor associated with this scenario")]
        [SerializeField] private GameObject scenarioPropRoot;

        [Header("--- Custom Dilemma Decisions ---")]
        public UnityEvent onSafeChoiceSelected;
        public UnityEvent onCompromiseChoiceSelected;

        protected override void Start()
        {
            base.Start();
            Debug.Log($"<color=#38BDF8>[Scenario Template]</color> Initialized scenario: {scenarioData?.ScenarioTitle}");
        }

        public void HandleSafeChoice()
        {
            if (currentPhase != ScenarioPhase.ActiveInvestigation) return;

            // Record discovered flags
            if (scenarioData != null && scenarioData.RedFlags.Count > 0)
            {
                scoreTracker.RecordRedFlag(scenarioData.RedFlags[0]);
            }

            onSafeChoiceSelected?.Invoke();
            CompleteScenario(true);
        }

        public void HandleCompromisedChoice()
        {
            if (currentPhase != ScenarioPhase.ActiveInvestigation) return;

            TriggerBreach();
            onCompromiseChoiceSelected?.Invoke();
            CompleteScenario(false);
        }
    }
}
