using UnityEngine;
using UnityEngine.Events;

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
        [SerializeField] private ScenarioDataSO scenarioData;
        [SerializeField] private GameObject workstationMonitor;
        [SerializeField] private GameObject suspiciousAttachment;
        [SerializeField] private GameObject deskPhone;

        [Header("--- Actions ---")]
        public UnityEvent onAttachmentInspected;
        public UnityEvent onPhoneVerified;
        public UnityEvent onQuarantineExecuted;

        public void InspectAttachment()
        {
            RegisterRedFlag("RF1_EXT");
            onAttachmentInspected?.Invoke();
        }

        public void CallCoworkerToVerify()
        {
            RegisterRedFlag("RF1_HDR");
            onPhoneVerified?.Invoke();
        }

        public void QuarantineFile()
        {
            RegisterRedFlag("RF1_CMD");
            onQuarantineExecuted?.Invoke();
            CompleteScenario(OutcomeType.Success);
        }
    }
}
