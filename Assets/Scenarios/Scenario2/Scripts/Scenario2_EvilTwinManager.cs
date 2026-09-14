using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Kannko.Scenario;

namespace Kannko.Scenario2
{
    /// <summary>
    /// Core gameplay manager for Scenario 2 (Evil Twin Wi-Fi Attack).
    /// Listens for the START button event from Scenario2IntroController,
    /// starts the 3-minute timer, and tracks red flag discoveries without rebuilding existing objects.
    /// </summary>
    [DisallowMultipleComponent]
    public class Scenario2_EvilTwinManager : MonoBehaviour
    {
        [Header("Scenario Configuration")]
        [SerializeField] private ScenarioDataSO m_ScenarioData;
        [SerializeField] private Scenario2Config m_Config;

        [Header("UI & Intro Panel References")]
        [SerializeField] private Scenario2IntroController m_IntroController;

        [Header("Scenario Progression")]
        [SerializeField] private float m_TimeRemaining = 180f; // 3 minutes limit
        [SerializeField] private bool m_IsScenarioActive = false;
        [SerializeField] private int m_RedFlagsDiscovered = 0;

        [Header("Events")]
        public UnityEvent OnScenarioStarted = new UnityEvent();
        public UnityEvent OnScenarioTimeExpired = new UnityEvent();

        public float TimeRemaining => m_TimeRemaining;
        public bool IsScenarioActive => m_IsScenarioActive;
        public int RedFlagsDiscovered => m_RedFlagsDiscovered;

        private void Awake()
        {
            // Find existing components in the scene without instantiating duplicates
            if (m_IntroController == null)
            {
                m_IntroController = FindFirstObjectByType<Scenario2IntroController>();
            }
        }

        private void OnEnable()
        {
            if (m_IntroController != null)
            {
                m_IntroController.OnScenarioStarted.RemoveListener(StartScenario);
                m_IntroController.OnScenarioStarted.AddListener(StartScenario);
            }
        }

        private void OnDisable()
        {
            if (m_IntroController != null)
            {
                m_IntroController.OnScenarioStarted.RemoveListener(StartScenario);
            }
        }

        private void Start()
        {
            Debug.Log("[Scenario2_EvilTwinManager] Scenario 2 Evil Twin Manager initialized. Waiting for START button...");
        }

        /// <summary>
        /// Called when the player clicks the START button on the Intro Panel.
        /// </summary>
        public void StartScenario()
        {
            if (m_IsScenarioActive) return;

            m_IsScenarioActive = true;
            m_TimeRemaining = m_Config != null ? m_Config.timeLimitSeconds : 180f;

            // Deactivate intro panel
            if (m_IntroController != null && m_IntroController.gameObject.activeSelf)
            {
                m_IntroController.gameObject.SetActive(false);
            }

            Debug.Log("[Scenario2_EvilTwinManager] Scenario 2 gameplay started! Timer running (3 minutes = 180s).");
            OnScenarioStarted?.Invoke();
        }

        private void Update()
        {
            if (!m_IsScenarioActive) return;

            if (m_TimeRemaining > 0f)
            {
                m_TimeRemaining -= Time.deltaTime;
                if (m_TimeRemaining <= 0f)
                {
                    m_TimeRemaining = 0f;
                    OnTimeExpired();
                }
            }
        }

        private void OnTimeExpired()
        {
            m_IsScenarioActive = false;
            Debug.LogWarning("[Scenario2_EvilTwinManager] Time expired! 3 minutes limit reached.");
            OnScenarioTimeExpired?.Invoke();
        }

        public ScenarioDataSO ScenarioData => m_ScenarioData;

        public void DiscoverRedFlag(RedFlagData redFlag)
        {
            if (redFlag != null)
            {
                m_RedFlagsDiscovered++;
                Debug.Log($"[Scenario2_EvilTwinManager] Red flag discovered: '{redFlag.title}' ({redFlag.flagId}). Total: {m_RedFlagsDiscovered}");
            }
        }

        public void DiscoverRedFlag(RedFlagSO redFlag)
        {
            if (redFlag != null)
            {
                m_RedFlagsDiscovered++;
                Debug.Log($"[Scenario2_EvilTwinManager] Red flag discovered: '{redFlag.flagName}' ({redFlag.flagId}). Total: {m_RedFlagsDiscovered}");
            }
        }
    }
}

// Global class for compatibility with ScenarioUIOverlay (Type.GetType("Scenario2_EvilTwinManager"))
public class Scenario2_EvilTwinManager : Kannko.Scenario2.Scenario2_EvilTwinManager {}

