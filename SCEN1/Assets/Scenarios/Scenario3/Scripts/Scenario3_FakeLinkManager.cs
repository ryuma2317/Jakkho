using System;
using UnityEngine;
using UnityEngine.Events;
using CADT.VR.Core;

namespace CADT.VR.Scenarios.Scenario3
{
    /// <summary>
    /// Scenario 3: Fake Link / Lookalike Domain Credential Harvesting.
    /// Reference Scenario for URL analysis, homograph spoofing, and MFA tokens.
    /// </summary>
    [DisallowMultipleComponent]
    public class Scenario3_FakeLinkManager : ScenarioManager
    {
        [Header("--- Scenario 3 Elements ---")]
        [SerializeField] private GameObject emailClient;
        [SerializeField] private GameObject urlHoverTooltip;
        [SerializeField] private GameObject hardwareTokenSlot;

        [Header("--- Actions ---")]
        public UnityEvent onUrlHoverInspected;
        public UnityEvent onReportedPhishing;
        public UnityEvent onMfaTokenTapped;

        public void HoverInspectUrl()
        {
            if (scenarioData != null && scenarioData.RedFlags.Count > 0)
                scoreTracker.RecordRedFlag(scenarioData.RedFlags[0]);

            onUrlHoverInspected?.Invoke();
        }

        public void ReportPhishingEmail()
        {
            if (scenarioData != null && scenarioData.RedFlags.Count > 1)
                scoreTracker.RecordRedFlag(scenarioData.RedFlags[1]);

            onReportedPhishing?.Invoke();
            CompleteScenario(true);
        }

        public void ClickFakeLink()
        {
            TriggerBreach();
            CompleteScenario(false);
        }
    }
}
