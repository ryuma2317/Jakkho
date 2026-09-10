using UnityEngine;
using Kannko.Scenario;

namespace Kannko.Laptop
{
    /// <summary>
    /// Enforces visibility rules on the FloatingLaptopPanel:
    /// The FloatingLaptopPanel CANNOT be visible/active until the scenario has started from the IntroPanel.
    /// Even if the player uses a VR controller or mouse to click the laptop before START is pressed,
    /// this guard ensures the panel remains completely hidden.
    /// Once the scenario begins, normal interaction on Laptop 1 is enabled to toggle the panel open and closed.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingLaptopPanelGuard : MonoBehaviour
    {
        [Header("Scenario Intro Reference")]
        [Tooltip("Reference to Scenario2IntroController to check if scenario gameplay has started.")]
        [SerializeField] private Scenario2IntroController m_IntroController;

        [Header("Settings")]
        [Tooltip("When enabled, prevents the panel from being visible before START is clicked.")]
        [SerializeField] private bool m_EnforceIntroGate = true;

        public bool IsScenarioStarted
        {
            get
            {
                if (!m_EnforceIntroGate) return true;
                if (m_IntroController == null)
                {
                    m_IntroController = Object.FindFirstObjectByType<Scenario2IntroController>(FindObjectsInactive.Include);
                }
                return m_IntroController == null || m_IntroController.IsScenarioStarted;
            }
        }

        public Scenario2IntroController IntroController
        {
            get => m_IntroController;
            set => m_IntroController = value;
        }

        private void Awake()
        {
            FindIntroControllerIfNull();

            // At Awake, if scenario hasn't started, guarantee panel is inactive
            if (!IsScenarioStarted)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            FindIntroControllerIfNull();

            // If an external event or trigger tried to activate this panel before START:
            if (!IsScenarioStarted)
            {
                Debug.LogWarning("[FloatingLaptopPanelGuard] FloatingLaptopPanel activation blocked: START has not been clicked yet on IntroPanel. Panel forced to inactive.");
                gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            FindIntroControllerIfNull();

            if (!IsScenarioStarted)
            {
                gameObject.SetActive(false);
            }
        }

        public void FindIntroControllerIfNull()
        {
            if (m_IntroController == null)
            {
                m_IntroController = Object.FindFirstObjectByType<Scenario2IntroController>(FindObjectsInactive.Include);
            }
        }
    }
}
