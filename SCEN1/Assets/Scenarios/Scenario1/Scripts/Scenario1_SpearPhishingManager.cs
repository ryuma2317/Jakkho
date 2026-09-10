using System;
using UnityEngine;
using UnityEngine.Events;
using CADT.VR.Core;

namespace CADT.VR.Scenarios.Scenario1
{
    /// <summary>
    /// Scenario 1: Spear Phishing & Disguised Dropper.
    /// Controls the Telegram attachment dilemma, out-of-band phone call verification,
    /// and workstation breach mitigation.
    /// </summary>
    [DisallowMultipleComponent]
    public class Scenario1_SpearPhishingManager : ScenarioManager
    {
        [Header("--- Scenario 1 Elements ---")]
        [SerializeField] private GameObject workstationMonitor;
        [SerializeField] private GameObject suspiciousAttachment;
        [SerializeField] private GameObject deskPhone;

        [Header("--- Actions ---")]
        public UnityEvent onAttachmentInspected;
        public UnityEvent onPhoneVerified;
        public UnityEvent onQuarantineExecuted;

        public void InspectAttachment()
        {
            if (scenarioData != null && scenarioData.RedFlags.Count > 0)
                scoreTracker.RecordRedFlag(scenarioData.RedFlags[0]);

            onAttachmentInspected?.Invoke();
        }

        public void CallCoworkerToVerify()
        {
            if (scenarioData != null && scenarioData.RedFlags.Count > 1)
                scoreTracker.RecordRedFlag(scenarioData.RedFlags[1]);

            onPhoneVerified?.Invoke();
        }

        public void QuarantineFile()
        {
            if (scenarioData != null && scenarioData.RedFlags.Count > 2)
                scoreTracker.RecordRedFlag(scenarioData.RedFlags[2]);

            onQuarantineExecuted?.Invoke();
            CompleteScenario(true);
        }
    }
}
