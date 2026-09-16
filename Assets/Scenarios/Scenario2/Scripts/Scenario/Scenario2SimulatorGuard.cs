// ===================================================================================
// Scenario2SimulatorGuard.cs
// Guard component that ensures test tools and duplicate simulators are disabled
// so real physical VR headset and VR controllers have exclusive control.
// ===================================================================================

using UnityEngine;

namespace Kannko.Scenario2
{
    /// <summary>
    /// Guard component that ensures test tools and duplicate simulators are disabled
    /// so real physical VR headset and VR controllers have exclusive control.
    /// </summary>
    [AddComponentMenu("Scenario2/Simulator Guard")]
    [DisallowMultipleComponent]
    public class Scenario2SimulatorGuard : MonoBehaviour
    {
        [Header("XR Simulator GameObjects to Disable")]
        [Tooltip("Optional reference to XR Interaction Simulator root GameObject if present.")]
        [SerializeField] private GameObject m_XRInteractionSimulator;

        [Tooltip("Optional reference to legacy DesktopTesting_TEMP if present.")]
        [SerializeField] private GameObject m_DesktopTestingTEMP;

        private void Awake()
        {
            EnforceVRReadyState();
        }

        private void EnforceVRReadyState()
        {
            GameObject sim = m_XRInteractionSimulator != null
                ? m_XRInteractionSimulator
                : GameObject.Find("XR Interaction Simulator");
            if (sim != null)
            {
                sim.SetActive(false);
                Debug.Log("[Scenario2SimulatorGuard] XR Interaction Simulator disabled to prevent duplicate simulator conflict.");
            }

            GameObject desktop = m_DesktopTestingTEMP != null
                ? m_DesktopTestingTEMP
                : GameObject.Find("DesktopTesting_TEMP");
            if (desktop != null)
            {
                desktop.SetActive(false);
                Debug.Log("[Scenario2SimulatorGuard] DesktopTesting_TEMP disabled for real VR controller readiness.");
            }
        }
    }
}

