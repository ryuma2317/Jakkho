using System;
using UnityEngine;
using UnityEngine.Events;
using CADT.VR.Core;

namespace CADT.VR.Scenarios.Scenario2
{
    /// <summary>
    /// Scenario 2: Rogue AP / Evil Twin Wi-Fi Attack.
    /// Simulates public cafe Wi-Fi spoofing, captive portals, and VPN enforcement.
    /// </summary>
    [DisallowMultipleComponent]
    public class Scenario2_EvilTwinManager : ScenarioManager
    {
        [Header("--- Evil Twin Attack Elements ---")]
        [SerializeField] private GameObject cafeLaptop;
        [SerializeField] private GameObject captivePortalModal;
        [SerializeField] private GameObject vpnClientToggle;

        [Header("--- Actions ---")]
        public UnityEvent onSsidAnalyzed;
        public UnityEvent onVpnActivated;
        public UnityEvent onCellularFallbackUsed;

        public void InspectSsidCertificate()
        {
            if (scenarioData != null && scenarioData.RedFlags.Count > 0)
                scoreTracker.RecordRedFlag(scenarioData.RedFlags[0]);

            onSsidAnalyzed?.Invoke();
        }

        public void EnableCorporateVpn()
        {
            if (scenarioData != null && scenarioData.RedFlags.Count > 1)
                scoreTracker.RecordRedFlag(scenarioData.RedFlags[1]);

            onVpnActivated?.Invoke();
            CompleteScenario(true);
        }

        public void ConnectToUnsecuredWifi()
        {
            TriggerBreach();
            CompleteScenario(false);
        }
    }
}
