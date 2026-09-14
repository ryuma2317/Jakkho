// ===================================================================================
// Scenario2SimulatorGuard.cs
// Scenario2-specific guard that disables Editor-only testing tools when
// running on a real VR headset device (non-Editor build).
//
// In the Unity Editor:   XR Interaction Simulator = ON, DesktopTesting_TEMP = ON
// On a real VR device:   XR Interaction Simulator = OFF, DesktopTesting_TEMP = OFF
//
// Attach this component to any persistent Scenario2 GameObject (e.g., XR Origin (VR)).
// ===================================================================================

using UnityEngine;

namespace Kannko.Scenario2
{
    /// <summary>
    /// Disables the XR Interaction Simulator and DesktopTesting_TEMP components/GameObjects
    /// when executing on a real VR device (non-Editor runtime).
    /// In the Unity Editor all simulator tools remain active for testing.
    /// </summary>
    [AddComponentMenu("Scenario2/Simulator Guard")]
    [DisallowMultipleComponent]
    public class Scenario2SimulatorGuard : MonoBehaviour
    {
        [Header("XR Simulator GameObjects to Disable on Device")]
        [Tooltip("Drag the XR Interaction Simulator root GameObject here.")]
        [SerializeField] private GameObject m_XRInteractionSimulator;

        [Tooltip("Drag the DesktopTesting_TEMP child GameObject here (under XR Origin).")]
        [SerializeField] private GameObject m_DesktopTestingTEMP;

        private void Awake()
        {
#if !UNITY_EDITOR
            DisableEditorOnlyTools();
#else
            Debug.Log("[Scenario2SimulatorGuard] Running in Unity Editor - XR Interaction Simulator and DesktopTesting_TEMP remain ACTIVE.");
#endif
        }

        private void DisableEditorOnlyTools()
        {
            GameObject sim = m_XRInteractionSimulator != null
                ? m_XRInteractionSimulator
                : GameObject.Find("XR Interaction Simulator");
            if (sim != null)
            {
                sim.SetActive(false);
                Debug.Log("[Scenario2SimulatorGuard] XR Interaction Simulator DISABLED on device build.");
            }

            GameObject desktop = m_DesktopTestingTEMP != null
                ? m_DesktopTestingTEMP
                : GameObject.Find("DesktopTesting_TEMP");
            if (desktop != null)
            {
                desktop.SetActive(false);
                Debug.Log("[Scenario2SimulatorGuard] DesktopTesting_TEMP DISABLED on device build.");
            }
        }
    }
}

