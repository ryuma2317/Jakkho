using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CADT.VR.Cybersecurity
{
    /// <summary>
    /// Lead VR Architecture: Handles branching simulation logic, scenario evaluation,
    /// 3D spatial audio feedback, progress timeline animations, and dynamic state switching
    /// for both physical desk monitors and the holographic spatial dashboard.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRBranchManager : MonoBehaviour
    {
        [Header("--- Simulation State ---")]
        [Tooltip("Current training scenario phase title")]
        [SerializeField] private string scenarioPhaseTitle = "PHASE 2: Spear-Phishing & Extension Spoofing Detection";
        
        [Tooltip("Tracks whether the player has inspected the suspicious artifacts before deciding")]
        [SerializeField] private bool hasInspectedSuspiciousFile = false;
        
        [SerializeField] private ScenarioState currentState = ScenarioState.PendingAnalysis;

        public enum ScenarioState
        {
            PendingAnalysis,
            FileInspected,
            Passed,
            Failed
        }

        [Header("--- Diegetic Spatial UI Dashboard ---")]
        [Tooltip("Header label on holographic dashboard")]
        [SerializeField] private TextMeshProUGUI headerText;
        
        [Tooltip("Sub-header or status feedback text")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        [Tooltip("Timeline progress slider component")]
        [SerializeField] private Slider timelineSlider;

        [Tooltip("Value of slider at start of Phase 2 (0.0 to 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float initialTimelineProgress = 0.35f;

        [Tooltip("Value of slider upon passing Phase 2 (0.0 to 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float passTimelineProgress = 0.70f;

        [Tooltip("Branch A Button: Failed Path (Open payload / Ignore spoofing)")]
        [SerializeField] private Button branchAButton;

        [Tooltip("Branch B Button: Success Path (Quarantine & Report Threat)")]
        [SerializeField] private Button branchBButton;

        [Header("--- Monitor 1 (Left Screen - Windows 11 Desktop) ---")]
        [Tooltip("Root canvas or panel for Monitor 1")]
        [SerializeField] private GameObject monitor1RootCanvas;

        [Tooltip("File properties modal window on Monitor 1")]
        [SerializeField] private GameObject monitor1FilePropertiesWindow;

        [Tooltip("Warning overlay displayed on Monitor 1 if infected")]
        [SerializeField] private GameObject monitor1InfectionAlertOverlay;

        [Tooltip("Secured / Quarantined overlay on Monitor 1")]
        [SerializeField] private GameObject monitor1SecuredOverlay;

        [Header("--- Monitor 2 (Right Screen - Telegram Client) ---")]
        [Tooltip("Root canvas or panel for Monitor 2")]
        [SerializeField] private GameObject monitor2RootCanvas;

        [Tooltip("Suspicious chat thread container on Monitor 2")]
        [SerializeField] private GameObject monitor2ChatThread;

        [Tooltip("Quarantine banner on Monitor 2")]
        [SerializeField] private GameObject monitor2QuarantineBanner;

        [Tooltip("Data exfiltration / compromise simulation banner on Monitor 2")]
        [SerializeField] private GameObject monitor2CompromisedBanner;

        [Header("--- 3D Spatial Audio ---")]
        [Tooltip("Audio source with Spatial Blend set to 1.0 (3D)")]
        [SerializeField] private AudioSource spatialAudioSource;

        [Tooltip("Audio clip played when scenario is successfully solved")]
        [SerializeField] private AudioClip successAudioClip;

        [Tooltip("Audio clip played when scenario is failed / payload executed")]
        [SerializeField] private AudioClip failureAudioClip;

        [Tooltip("Audio clip played for UI navigation / inspection")]
        [SerializeField] private AudioClip inspectAudioClip;

        [Header("--- Custom Unity Events ---")]
        public UnityEvent onScenarioInitialized;
        public UnityEvent onFileInspected;
        public UnityEvent onScenarioPassed;
        public UnityEvent onScenarioFailed;
        public UnityEvent onScenarioReset;

        private Coroutine timelineLerpCoroutine;

        public ScenarioState CurrentState => currentState;
        public bool HasInspectedSuspiciousFile => hasInspectedSuspiciousFile;

        private void Awake()
        {
            if (spatialAudioSource == null)
            {
                spatialAudioSource = GetComponent<AudioSource>();
            }
        }

        private void Start()
        {
            InitializeScenario();
        }

        /// <summary>
        /// Sets up initial UI state, labels, and timeline slider position.
        /// </summary>
        public void InitializeScenario()
        {
            currentState = ScenarioState.PendingAnalysis;
            hasInspectedSuspiciousFile = false;

            if (headerText != null)
            {
                headerText.text = scenarioPhaseTitle;
            }

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "<color=#94A3B8>STATUS:</color> Inspect suspicious files on desktop monitors before making a triage decision.";
            }

            if (timelineSlider != null)
            {
                timelineSlider.value = initialTimelineProgress;
            }

            // Configure monitor default states
            if (monitor1InfectionAlertOverlay != null) monitor1InfectionAlertOverlay.SetActive(false);
            if (monitor1SecuredOverlay != null) monitor1SecuredOverlay.SetActive(false);
            if (monitor2QuarantineBanner != null) monitor2QuarantineBanner.SetActive(false);
            if (monitor2CompromisedBanner != null) monitor2CompromisedBanner.SetActive(false);

            // Hook button listeners
            if (branchAButton != null)
            {
                branchAButton.onClick.RemoveListener(OnFailScenario);
                branchAButton.onClick.AddListener(OnFailScenario);
                branchAButton.interactable = true;
            }

            if (branchBButton != null)
            {
                branchBButton.onClick.RemoveListener(OnPassScenario);
                branchBButton.onClick.AddListener(OnPassScenario);
                branchBButton.interactable = true;
            }

            onScenarioInitialized?.Invoke();
        }

        /// <summary>
        /// Called when the player interacts with suspicious files or inspects file properties.
        /// </summary>
        public void RegisterFileInspection()
        {
            if (hasInspectedSuspiciousFile) return;

            hasInspectedSuspiciousFile = true;
            currentState = ScenarioState.FileInspected;

            if (inspectAudioClip != null && spatialAudioSource != null)
            {
                spatialAudioSource.PlayOneShot(inspectAudioClip);
            }

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "<color=#00F0FF>ANALYSIS ACTIVE:</color> Suspicious double extension <color=#EF4444>[.pdf.exe]</color> identified. Choose your branch action.";
            }

            onFileInspected?.Invoke();
        }

        /// <summary>
        /// Branch A Execution: Player makes wrong triage decision (runs suspicious executable / ignores threat).
        /// </summary>
        public void OnFailScenario()
        {
            if (currentState == ScenarioState.Passed || currentState == ScenarioState.Failed) return;

            currentState = ScenarioState.Failed;

            // Audio feedback
            if (failureAudioClip != null && spatialAudioSource != null)
            {
                spatialAudioSource.PlayOneShot(failureAudioClip);
            }

            // HUD status feedback
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "<color=#EF4444>BREACH DETECTED!</color> Malicious payload executed: <i>document_invoice.pdf.exe</i> initiated command & control callback!";
            }

            // Disable interactive buttons
            SetBranchButtonsInteractable(false);

            // Trigger monitor compromise visual states
            if (monitor1InfectionAlertOverlay != null) monitor1InfectionAlertOverlay.SetActive(true);
            if (monitor2CompromisedBanner != null) monitor2CompromisedBanner.SetActive(true);

            // Timeline remains or flashes red indicator
            AnimateTimelineProgress(0.15f, 0.8f);

            onScenarioFailed?.Invoke();
            Debug.LogWarning("[VRBranchManager] Scenario FAILED: Malicious executable executed.");
        }

        /// <summary>
        /// Branch B Execution: Player correctly isolates threat and reports spoofed extension.
        /// </summary>
        public void OnPassScenario()
        {
            if (currentState == ScenarioState.Passed || currentState == ScenarioState.Failed) return;

            currentState = ScenarioState.Passed;

            // Audio feedback
            if (successAudioClip != null && spatialAudioSource != null)
            {
                spatialAudioSource.PlayOneShot(successAudioClip);
            }

            // HUD status feedback
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "<color=#10B981>THREAT NEUTRALIZED!</color> Spear-phishing payload quarantined. Digital forensics report dispatched to SOC.";
            }

            // Disable interactive buttons
            SetBranchButtonsInteractable(false);

            // Trigger monitor secured visual states
            if (monitor1SecuredOverlay != null) monitor1SecuredOverlay.SetActive(true);
            if (monitor2QuarantineBanner != null) monitor2QuarantineBanner.SetActive(true);

            // Advance progress bar smoothly to target
            AnimateTimelineProgress(passTimelineProgress, 1.2f);

            onScenarioPassed?.Invoke();
            Debug.Log("[VRBranchManager] Scenario PASSED: Phishing payload quarantined successfully.");
        }

        /// <summary>
        /// Resets the scenario to initial state for replayability and training loops.
        /// </summary>
        public void ResetScenario()
        {
            if (timelineLerpCoroutine != null)
            {
                StopCoroutine(timelineLerpCoroutine);
            }

            InitializeScenario();
            onScenarioReset?.Invoke();
        }

        private void SetBranchButtonsInteractable(bool interactable)
        {
            if (branchAButton != null) branchAButton.interactable = interactable;
            if (branchBButton != null) branchBButton.interactable = interactable;
        }

        private void AnimateTimelineProgress(float targetValue, float duration)
        {
            if (timelineSlider == null) return;

            if (timelineLerpCoroutine != null)
            {
                StopCoroutine(timelineLerpCoroutine);
            }

            timelineLerpCoroutine = StartCoroutine(SmoothSliderRoutine(targetValue, duration));
        }

        private IEnumerator SmoothSliderRoutine(float targetValue, float duration)
        {
            float startVal = timelineSlider.value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                timelineSlider.value = Mathf.Lerp(startVal, targetValue, t);
                yield return null;
            }

            timelineSlider.value = targetValue;
        }
    }
}