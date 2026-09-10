using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CADT.VR.Cybersecurity
{
    /// <summary>
    /// Scenario 1: The Disguised Attachment Controller.
    /// Manages the 4-choice decision dilemma, incoming Telegram threat trigger,
    /// Failure branch (C2 Dashboard, Command Prompt flash, Adobe error, worm spreading, red alarm lights),
    /// and Success branches (File inspection, Out-of-band phone call, SOC quarantine, green shield, +100 XP).
    /// </summary>
    [DisallowMultipleComponent]
    public class ScenarioChoiceController : MonoBehaviour
    {
        [Header("--- Scenario Information ---")]
        [SerializeField] private string phaseTitle = "PHASE 3: INVESTIGATION & DILEMMA";
        [SerializeField] private string phaseSubtitle = "A suspicious file was received via Telegram. How do you proceed?";

        [Header("--- Floating Dashboard UI Elements ---")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI statusFeedbackTMP;
        [SerializeField] private Slider timelineSlider;
        [SerializeField] private GameObject xpBannerObject;
        [SerializeField] private TextMeshProUGUI xpBannerTMP;

        [Header("--- Interactive Choice Buttons ---")]
        [SerializeField] private Button optionA_OpenArchiveButton;
        [SerializeField] private Button optionB_InspectPropertiesButton;
        [SerializeField] private Button optionC_CallPhoneButton;
        [SerializeField] private Button optionD_QuarantineButton;

        [Header("--- Centered Telegram Monitor Elements ---")]
        [SerializeField] private GameObject telegramMonitorRoot;
        [SerializeField] private GameObject incomingSovannPopup;
        [SerializeField] private GameObject filePropertiesModal;
        [SerializeField] private GameObject adobeErrorModal;
        [SerializeField] private GameObject cmdPromptFlashFX;
        [SerializeField] private GameObject telegramQuarantineBanner;
        [SerializeField] private GameObject telegramCompromisedBanner;
        [SerializeField] private GameObject telegramWormSpreadingBanner;

        [Header("--- 3D Holographic C2 Attacker Dashboard ---")]
        [SerializeField] private GameObject c2HologramDashboard;
        [SerializeField] private TextMeshProUGUI c2LogTMP;

        [Header("--- 3D World VR Effects & Props ---")]
        [SerializeField] private GameObject vrGreenShieldFX;
        [SerializeField] private GameObject deskPhoneObject;
        [SerializeField] private Light ceilingLight;
        [SerializeField] private Light directionalLight;

        [Header("--- Audio Feedback ---")]
        [SerializeField] private AudioSource spatialAudioSource;
        [SerializeField] private AudioClip incomingMessageSFX;
        [SerializeField] private AudioClip alarmAudioClip;
        [SerializeField] private AudioClip successAudioClip;
        [SerializeField] private AudioClip phoneRingDialogueSFX;
        [SerializeField] private AudioClip buttonClickSFX;
        [SerializeField] private AudioClip inspectSFX;

        [Header("--- Branch Events ---")]
        public UnityEvent onScenarioStarted;
        public UnityEvent onOptionA_Failed;
        public UnityEvent onOptionB_Inspected;
        public UnityEvent onOptionC_PhoneVerified;
        public UnityEvent onOptionD_Quarantined;

        private Coroutine c2LogCoroutine;
        private Coroutine alarmCoroutine;
        private Color initialCeilingColor = new Color(0.96f, 0.97f, 1.0f);
        private Color initialDirColor = new Color(1.0f, 0.95f, 0.83f);
        private bool isResolved = false;

        private void Awake()
        {
            if (spatialAudioSource == null)
            {
                spatialAudioSource = GetComponent<AudioSource>();
            }

            if (ceilingLight != null) initialCeilingColor = ceilingLight.color;
            if (directionalLight != null) initialDirColor = directionalLight.color;
        }

        private void Start()
        {
            InitializeScenario();
        }

        public void InitializeScenario()
        {
            isResolved = false;

            if (headerTMP != null) headerTMP.text = phaseTitle;
            if (subtitleTMP != null) subtitleTMP.text = phaseSubtitle;
            if (statusFeedbackTMP != null)
            {
                statusFeedbackTMP.text = "<color=#94A3B8>STATUS:</color> Incoming message from <b>Sovann</b> contains an archive. Choose your triage action.";
            }

            if (timelineSlider != null) timelineSlider.value = 0.45f;

            // Reset UI Overlays
            if (incomingSovannPopup != null) incomingSovannPopup.SetActive(true);
            if (filePropertiesModal != null) filePropertiesModal.SetActive(false);
            if (adobeErrorModal != null) adobeErrorModal.SetActive(false);
            if (cmdPromptFlashFX != null) cmdPromptFlashFX.SetActive(false);
            if (telegramQuarantineBanner != null) telegramQuarantineBanner.SetActive(false);
            if (telegramCompromisedBanner != null) telegramCompromisedBanner.SetActive(false);
            if (telegramWormSpreadingBanner != null) telegramWormSpreadingBanner.SetActive(false);
            if (c2HologramDashboard != null) c2HologramDashboard.SetActive(false);
            if (vrGreenShieldFX != null) vrGreenShieldFX.SetActive(false);
            if (xpBannerObject != null) xpBannerObject.SetActive(false);

            // Reset Lighting
            if (ceilingLight != null) ceilingLight.color = initialCeilingColor;
            if (directionalLight != null) directionalLight.color = initialDirColor;

            // Hook Button Listeners
            HookButtons();

            // Play incoming message chime
            if (incomingMessageSFX != null && spatialAudioSource != null)
            {
                spatialAudioSource.PlayOneShot(incomingMessageSFX);
            }

            onScenarioStarted?.Invoke();
        }

        private void HookButtons()
        {
            if (optionA_OpenArchiveButton != null)
            {
                optionA_OpenArchiveButton.onClick.RemoveAllListeners();
                optionA_OpenArchiveButton.onClick.AddListener(ExecuteOptionA_Failure);
                optionA_OpenArchiveButton.interactable = true;
            }

            if (optionB_InspectPropertiesButton != null)
            {
                optionB_InspectPropertiesButton.onClick.RemoveAllListeners();
                optionB_InspectPropertiesButton.onClick.AddListener(ExecuteOptionB_Inspect);
                optionB_InspectPropertiesButton.interactable = true;
            }

            if (optionC_CallPhoneButton != null)
            {
                optionC_CallPhoneButton.onClick.RemoveAllListeners();
                optionC_CallPhoneButton.onClick.AddListener(ExecuteOptionC_CallPhone);
                optionC_CallPhoneButton.interactable = true;
            }

            if (optionD_QuarantineButton != null)
            {
                optionD_QuarantineButton.onClick.RemoveAllListeners();
                optionD_QuarantineButton.onClick.AddListener(ExecuteOptionD_Quarantine);
                optionD_QuarantineButton.interactable = true;
            }
        }

        private void SetAllButtonsInteractable(bool interactable)
        {
            if (optionA_OpenArchiveButton != null) optionA_OpenArchiveButton.interactable = interactable;
            if (optionB_InspectPropertiesButton != null) optionB_InspectPropertiesButton.interactable = interactable;
            if (optionC_CallPhoneButton != null) optionC_CallPhoneButton.interactable = interactable;
            if (optionD_QuarantineButton != null) optionD_QuarantineButton.interactable = interactable;
        }

        #region --- OPTION A: FAILURE BRANCH ---
        /// <summary>
        /// Option A: Open File directly from Archive (Unsafe Failure Path)
        /// Triggers Adobe error, Command Prompt Flash, 3D C2 Dashboard, Worm propagation, Red alert lighting.
        /// </summary>
        public void ExecuteOptionA_Failure()
        {
            if (isResolved) return;
            isResolved = true;
            SetAllButtonsInteractable(false);

            if (buttonClickSFX != null && spatialAudioSource != null)
                spatialAudioSource.PlayOneShot(buttonClickSFX);

            if (statusFeedbackTMP != null)
            {
                statusFeedbackTMP.text = "<color=#EF4444><b>CRITICAL BREACH!</b></color> Payload executed: <i>Salary_Slip_August.pdf.exe</i> initiated C2 callback and worm spreading!";
            }

            // Start failure animation sequence
            StartCoroutine(FailureSequenceRoutine());
            onOptionA_Failed?.Invoke();
        }

        private IEnumerator FailureSequenceRoutine()
        {
            // 1. Show Fake Adobe Reader Crash / Error
            if (adobeErrorModal != null) adobeErrorModal.SetActive(true);
            yield return new WaitForSeconds(0.4f);

            // 2. Command Prompt rapid flashing FX
            if (cmdPromptFlashFX != null)
            {
                cmdPromptFlashFX.SetActive(true);
                yield return new WaitForSeconds(0.8f);
                cmdPromptFlashFX.SetActive(false);
            }

            // 3. Red emergency lighting & alarm audio
            if (alarmAudioClip != null && spatialAudioSource != null)
            {
                spatialAudioSource.clip = alarmAudioClip;
                spatialAudioSource.loop = true;
                spatialAudioSource.Play();
            }

            alarmCoroutine = StartCoroutine(PulseRedLightsRoutine());

            // 4. Spawn 3D Holographic Attacker C2 Dashboard
            if (c2HologramDashboard != null)
            {
                c2HologramDashboard.SetActive(true);
                if (c2LogTMP != null)
                {
                    c2LogCoroutine = StartCoroutine(AnimateC2LogsRoutine());
                }
            }

            // 5. Telegram Worm spreading simulation
            if (telegramCompromisedBanner != null) telegramCompromisedBanner.SetActive(true);
            if (telegramWormSpreadingBanner != null) telegramWormSpreadingBanner.SetActive(true);

            if (timelineSlider != null) timelineSlider.value = 0.15f;
        }

        private IEnumerator PulseRedLightsRoutine()
        {
            float timer = 0f;
            while (true)
            {
                timer += Time.deltaTime * 5f;
                float pingPong = Mathf.PingPong(timer, 1f);
                Color redCol = Color.Lerp(new Color(0.3f, 0.02f, 0.02f), new Color(1f, 0.05f, 0.05f), pingPong);

                if (ceilingLight != null) ceilingLight.color = redCol;
                if (directionalLight != null) directionalLight.color = redCol;
                yield return null;
            }
        }

        private IEnumerator AnimateC2LogsRoutine()
        {
            string[] logLines = new string[]
            {
                "<color=#EF4444>[C2 SESSION ESTABLISHED] Target IP: 192.168.1.104 (CADT-WS-01)</color>",
                "<color=#F59E0B>[+] Stage 2 Dropper: Invoking PowerShell in-memory payload...</color>",
                "<color=#10B981>[+] Dumping Chrome & Telegram Credential Stores: 14 Accounts harvested</color>",
                "<color=#F59E0B>[+] Auto-Propagating: Telegram worm sent to 10 active coworker chats</color>",
                "<color=#EF4444>[!] C2 Exfiltration: 48.2 MB confidential CADT HR data transmitted to 185.220.101.44</color>"
            };

            c2LogTMP.text = "";
            foreach (var line in logLines)
            {
                c2LogTMP.text += line + "\n";
                yield return new WaitForSeconds(0.5f);
            }
        }
        #endregion

        #region --- OPTION B: INSPECT PROPERTIES (SUCCESS PATH) ---
        /// <summary>
        /// Option B: Inspect File Properties (Check Double Extension)
        /// </summary>
        public void ExecuteOptionB_Inspect()
        {
            if (buttonClickSFX != null && spatialAudioSource != null)
                spatialAudioSource.PlayOneShot(buttonClickSFX);

            if (inspectSFX != null && spatialAudioSource != null)
                spatialAudioSource.PlayOneShot(inspectSFX);

            if (filePropertiesModal != null)
            {
                filePropertiesModal.SetActive(true);
            }

            if (statusFeedbackTMP != null)
            {
                statusFeedbackTMP.text = "<color=#00F0FF><b>ANALYSIS:</b></color> True file type is <color=#EF4444><b>Application (.exe)</b></color> disguised with a PDF icon and password-protected RAR to bypass gateway scanners.";
            }

            TriggerSuccessState(
                "+100 XP - Threat Neutralized\n<size=20><color=#A7F3D0>Identified Double Extension (.pdf.exe) & Password AV Bypass Technique</color></size>"
            );

            onOptionB_Inspected?.Invoke();
        }
        #endregion

        #region --- OPTION C: CALL SOVANN VIA PHONE (SUCCESS PATH) ---
        /// <summary>
        /// Option C: Call Sovann via Desk Phone (Ext 4023) - Out-of-Band Verification
        /// </summary>
        public void ExecuteOptionC_CallPhone()
        {
            if (buttonClickSFX != null && spatialAudioSource != null)
                spatialAudioSource.PlayOneShot(buttonClickSFX);

            if (phoneRingDialogueSFX != null && spatialAudioSource != null)
                spatialAudioSource.PlayOneShot(phoneRingDialogueSFX);

            if (statusFeedbackTMP != null)
            {
                statusFeedbackTMP.text = "<color=#10B981><b>OUT-OF-BAND VERIFICATION:</b></color> Sovann confirmed: <i>'My Telegram account was hijacked 10 minutes ago! Do not open that attachment!'</i>";
            }

            TriggerSuccessState(
                "+100 XP - Threat Neutralized\n<size=20><color=#A7F3D0>Out-of-Band Verification Verified Compromised Peer Account</color></size>"
            );

            onOptionC_PhoneVerified?.Invoke();
        }
        #endregion

        #region --- OPTION D: QUARANTINE ACTION (SUCCESS PATH) ---
        /// <summary>
        /// Option D: Submit to IT Security Quarantine
        /// </summary>
        public void ExecuteOptionD_Quarantine()
        {
            if (buttonClickSFX != null && spatialAudioSource != null)
                spatialAudioSource.PlayOneShot(buttonClickSFX);

            if (statusFeedbackTMP != null)
            {
                statusFeedbackTMP.text = "<color=#10B981><b>THREAT QUARANTINED:</b></color> Malicious archive submitted to CADT SOC sandbox. Endpoint isolated and IOCs broadcasted.";
            }

            TriggerSuccessState(
                "+100 XP - Threat Neutralized\n<size=20><color=#A7F3D0>Archive Quarantined & Incident Ticket Dispatched to SOC</color></size>"
            );

            onOptionD_Quarantined?.Invoke();
        }
        #endregion

        private void TriggerSuccessState(string xpMessage)
        {
            isResolved = true;
            SetAllButtonsInteractable(false);

            if (successAudioClip != null && spatialAudioSource != null)
            {
                spatialAudioSource.PlayOneShot(successAudioClip);
            }

            // Green Shield VR FX
            if (vrGreenShieldFX != null) vrGreenShieldFX.SetActive(true);

            // Quarantine banner on Telegram
            if (telegramQuarantineBanner != null) telegramQuarantineBanner.SetActive(true);

            // XP Banner
            if (xpBannerObject != null)
            {
                xpBannerObject.SetActive(true);
                if (xpBannerTMP != null) xpBannerTMP.text = xpMessage;
            }

            if (timelineSlider != null) StartCoroutine(SmoothSliderRoutine(0.85f, 1.0f));
        }

        private IEnumerator SmoothSliderRoutine(float targetValue, float duration)
        {
            float start = timelineSlider.value;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                timelineSlider.value = Mathf.Lerp(start, targetValue, elapsed / duration);
                yield return null;
            }
            timelineSlider.value = targetValue;
        }

        public void ClosePropertiesModal()
        {
            if (filePropertiesModal != null) filePropertiesModal.SetActive(false);
            if (adobeErrorModal != null) adobeErrorModal.SetActive(false);
        }
    }
}
