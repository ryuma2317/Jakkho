using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using CADT.VR.UI;

namespace CADT.VR.Cybersecurity
{
    /// <summary>
    /// Simulates a Windows Command Prompt security breach batch execution sequence on the workstation monitor.
    /// Replicates the red terminal text, fastest line-by-line output, and continuous command loop requested.
    /// </summary>
    [DisallowMultipleComponent]
    public class CommandPromptBreachSimulator : MonoBehaviour
    {
        [Header("--- Configuration ---")]
        [Tooltip("Delay in seconds between printing each line (fastest execution)")]
        [SerializeField] private float lineDelay = 0.02f;

        [Tooltip("Pause duration between loops in seconds")]
        [SerializeField] private float loopPauseDuration = 0.4f;

        [Tooltip("Whether to restart/loop the breach output continuously")]
        [SerializeField] private bool loopSequence = true;

        [Tooltip("Color of the terminal text (Color 0C: light red)")]
        [SerializeField] private Color terminalTextColor = new Color(0.83f, 0.26f, 0.31f, 1.0f); // #D3424F

        [Header("--- UI References ---")]
        [SerializeField] private TextMeshProUGUI terminalTMP;
        [SerializeField] private ScrollRect scrollRect;

        [Header("--- Emergency Alarm Lighting ---")]
        [Tooltip("Whether to activate emergency red flashing alarm lights during breach sequence")]
        [SerializeField] private bool enableAlarmLighting = true;

        [Tooltip("Ceiling point light to turn red and strobe")]
        [SerializeField] private Light ceilingLight;

        [Tooltip("Directional sunlight to dim into red emergency ambience")]
        [SerializeField] private Light directionalLight;

        [Tooltip("Floor lamp to dim down during emergency")]
        [SerializeField] private Light floorLampLight;

        [Tooltip("Workstation overhead light directly above desk and player hands for realistic red emergency glow")]
        [SerializeField] private Light workstationAlarmLight;

        [Tooltip("Color of the emergency alarm light (pure vibrant red)")]
        [SerializeField] private Color alarmRedColor = new Color(1.0f, 0.04f, 0.04f, 1.0f);

        [Tooltip("Strobe rate in flashes per second (on/off cycles)")]
        [SerializeField] private float alarmFlashSpeed = 2.5f;

        [Tooltip("Peak intensity of the red ceiling strobe")]
        [SerializeField] private float ceilingAlarmIntensity = 4.2f;

        [Tooltip("Minimum intensity when light is 'off'")]
        [SerializeField] private float ceilingOffIntensity = 0.05f;

        [Header("--- Emergency Alarm Audio ---")]
        [Tooltip("Whether to play the security alarm siren in loop during breach")]
        [SerializeField] private bool playAlarmSirenAudio = true;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip alertBeepSFX;

        [Header("--- Emergency Shutdown Button ---")]
        [Tooltip("Emergency Shutdown button generated at the bottom right of the terminal")]
        [SerializeField] private Button shutdownButton;
        [SerializeField] private GameObject shutdownButtonObject;
        [SerializeField] private TextMeshProUGUI shutdownButtonTMP;

        [Header("--- Shutdown Overlay ---")]
        [SerializeField] private GameObject shutdownOverlay;
        [SerializeField] private TextMeshProUGUI shutdownStatusTMP;
        [SerializeField] private Image shutdownSpinnerIcon;
        [SerializeField] private Button powerOnButton;
        [SerializeField] private AudioClip shutdownClickSFX;
        [SerializeField] private AudioClip powerOffSFX;

        [Header("--- Events ---")]
        public UnityEvent onSequenceStarted;
        public UnityEvent onSequenceCompleted;
        public UnityEvent onShutdownClicked;
        public UnityEvent onWorkstationPoweredOff;
        public UnityEvent onWorkstationRebooted;

        private Coroutine pulseButtonCoroutine;
        private Coroutine shutdownRoutine;

        private Coroutine sequenceCoroutine;
        private Coroutine alarmLightCoroutine;
        private Color originalCeilingColor;
        private float originalCeilingIntensity;
        private Color originalDirColor;
        private float originalDirIntensity;
        private float originalLampIntensity;
        private bool hasSavedLightState = false;
#pragma warning disable CS0414
        private bool isRunning = false;
#pragma warning restore CS0414

        public bool IsRunning => isRunning;

        public float LineDelay
        {
            get => lineDelay;
            set => lineDelay = value;
        }

        public bool LoopSequence
        {
            get => loopSequence;
            set => loopSequence = value;
        }

        public TextMeshProUGUI TerminalTMP
        {
            get => terminalTMP;
            set => terminalTMP = value;
        }

        public ScrollRect ScrollRectComp
        {
            get => scrollRect;
            set => scrollRect = value;
        }

        private static readonly string[] TerminalLines = new string[]
        {
            "echo.",
            "",
            "echo ================================================================",
            "================================================================",
            "echo              !!! SECURITY BREACH DETECTED !!!",
            "              !!! SECURITY BREACH DETECTED !!!",
            "echo ================================================================",
            "================================================================",
            "echo.",
            "",
            "echo [WARNING] Unauthorized activity detected...",
            "[WARNING] Unauthorized activity detected...",
            "echo [WARNING] Your computer has been compromised!",
            "[WARNING] Your computer has been compromised!",
            "echo.",
            "",
            "echo Target: DESKTOP-PC",
            "Target: DESKTOP-PC",
            "echo Status: SYSTEM LOCKED",
            "Status: SYSTEM LOCKED",
            "echo Threat Level: CRITICAL",
            "Threat Level: CRITICAL",
            "echo.",
            "",
            "echo [SYSTEM SCAN]",
            "[SYSTEM SCAN]",
            "echo Scanning personal files................. 100%%",
            "Scanning personal files................. 100%%",
            "echo Scanning documents...................... 100%%",
            "Scanning documents...................... 100%%",
            "echo Scanning system files................... 100%%",
            "Scanning system files................... 100%%",
            "echo Checking security status................ FAILED",
            "Checking security status................ FAILED",
            "echo.",
            "",
            "echo [ALERT]",
            "[ALERT]",
            "echo Your files are currently being accessed...",
            "Your files are currently being accessed...",
            "echo Your system security has been disabled...",
            "Your system security has been disabled...",
            "echo Network connection: COMPROMISED",
            "Network connection: COMPROMISED",
            "echo.",
            "",
            "echo ---------------------------------------------------------------",
            "---------------------------------------------------------------",
            "echo             ALL YOUR FILES ARE AT RISK!",
            "            ALL YOUR FILES ARE AT RISK!",
            "echo ---------------------------------------------------------------",
            "---------------------------------------------------------------",
            "echo.",
            "",
            "echo [ERROR] Windows Security Service: OFFLINE",
            "[ERROR] Windows Security Service: OFFLINE",
            "echo [ERROR] Firewall Protection: DISABLED",
            "[ERROR] Firewall Protection: DISABLED",
            "echo [ERROR] System Protection: FAILED",
            "[ERROR] System Protection: FAILED",
            "echo.",
            "",
            "echo !!! DO NOT TURN OFF THIS COMPUTER !!!",
            "!!! DO NOT TURN OFF THIS COMPUTER !!!",
            "echo !!! DO NOT CLOSE THIS WINDOW !!!",
            "!!! DO NOT CLOSE THIS WINDOW !!!",
            "echo.",
            "",
            "echo ================================================================",
            "================================================================",
            "echo             THIS IS A SIMULATION - JUST A PRANK :)",
            "            THIS IS A SIMULATION - JUST A PRANK :)",
            "echo ================================================================",
            "================================================================",
            "echo.",
            "",
            "Press any key to continue . . ."
        };

        private void Awake()
        {
            if (terminalTMP == null)
            {
                terminalTMP = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (scrollRect == null)
            {
                scrollRect = GetComponentInChildren<ScrollRect>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1.0f;
                    audioSource.minDistance = 0.5f;
                    audioSource.maxDistance = 12f;
                }
            }

            EnsureShutdownUI();
            ResolveLights();
        }

        private void OnEnable()
        {
            StartBreachSequence();
        }

        private void OnDisable()
        {
            StopBreachSequence();
        }

        private void OnDestroy()
        {
            RestoreLights();
            StopButtonPulse();
        }

        public void EnsureShutdownUI()
        {
            if (shutdownButtonObject == null)
            {
                Transform existing = transform.Find("Shutdown_Button");
                if (existing != null)
                {
                    shutdownButtonObject = existing.gameObject;
                    shutdownButton = shutdownButtonObject.GetComponent<Button>();
                    shutdownButtonTMP = shutdownButtonObject.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (shutdownButtonObject == null)
            {
                shutdownButtonObject = new GameObject("Shutdown_Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
                shutdownButtonObject.transform.SetParent(transform, false);

                RectTransform rt = shutdownButtonObject.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-40f, 40f);
                rt.sizeDelta = new Vector2(250f, 68f);

                Image img = shutdownButtonObject.GetComponent<Image>();
                img.color = new Color(0.85f, 0.15f, 0.15f, 0.95f);

                Outline outline = shutdownButtonObject.GetComponent<Outline>();
                outline.effectColor = new Color(1.0f, 0.45f, 0.45f, 0.75f);
                outline.effectDistance = new Vector2(2f, -2f);

                shutdownButton = shutdownButtonObject.GetComponent<Button>();
                ColorBlock cb = shutdownButton.colors;
                cb.normalColor = new Color(0.88f, 0.18f, 0.18f, 1f);
                cb.highlightedColor = new Color(1.0f, 0.35f, 0.35f, 1f);
                cb.pressedColor = new Color(0.65f, 0.1f, 0.1f, 1f);
                cb.selectedColor = new Color(0.88f, 0.18f, 0.18f, 1f);
                shutdownButton.colors = cb;

                GameObject textObj = new GameObject("Shutdown_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(shutdownButtonObject.transform, false);

                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(10f, 5f);
                textRt.offsetMax = new Vector2(-10f, -5f);

                shutdownButtonTMP = textObj.GetComponent<TextMeshProUGUI>();
                if (terminalTMP != null && terminalTMP.font != null)
                {
                    shutdownButtonTMP.font = terminalTMP.font;
                }
                shutdownButtonTMP.text = "<b>\u23FB SHUT DOWN</b>\n<size=13><color=#FECACA>EMERGENCY POWER OFF</color></size>";
                shutdownButtonTMP.fontSize = 20;
                shutdownButtonTMP.alignment = TextAlignmentOptions.Center;
                shutdownButtonTMP.color = Color.white;
            }

            if (shutdownButton != null)
            {
                shutdownButton.onClick.RemoveListener(ExecuteShutdown);
                shutdownButton.onClick.AddListener(ExecuteShutdown);
            }

            EnsureShutdownOverlay();
        }

        public void EnsureShutdownOverlay()
        {
            if (shutdownOverlay == null)
            {
                Transform existing = transform.Find("Shutdown_Overlay");
                if (existing != null)
                {
                    shutdownOverlay = existing.gameObject;
                    shutdownStatusTMP = shutdownOverlay.GetComponentInChildren<TextMeshProUGUI>();
                    Button[] btns = shutdownOverlay.GetComponentsInChildren<Button>(true);
                    if (btns.Length > 0) powerOnButton = btns[0];
                }
            }

            if (shutdownOverlay == null)
            {
                shutdownOverlay = new GameObject("Shutdown_Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                shutdownOverlay.transform.SetParent(transform, false);

                RectTransform rt = shutdownOverlay.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image bg = shutdownOverlay.GetComponent<Image>();
                bg.color = new Color(0.04f, 0.06f, 0.12f, 1f);

                GameObject content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(shutdownOverlay.transform, false);
                RectTransform crt = content.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f);
                crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = new Vector2(900f, 400f);

                GameObject statusObj = new GameObject("Status_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                statusObj.transform.SetParent(content.transform, false);
                RectTransform srt = statusObj.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0f, 0.3f);
                srt.anchorMax = new Vector2(1f, 1f);
                srt.offsetMin = Vector2.zero;
                srt.offsetMax = Vector2.zero;

                shutdownStatusTMP = statusObj.GetComponent<TextMeshProUGUI>();
                if (terminalTMP != null && terminalTMP.font != null)
                {
                    shutdownStatusTMP.font = terminalTMP.font;
                }
                shutdownStatusTMP.alignment = TextAlignmentOptions.Center;
                shutdownStatusTMP.text = "<size=44><b>Shutting down...</b></size>\n\n<size=20><color=#94A3B8>Terminating unauthorized network connections...\nStopping malicious subprocesses...</color></size>";
                shutdownStatusTMP.color = Color.white;

                GameObject btnObj = new GameObject("Power_On_Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
                btnObj.transform.SetParent(content.transform, false);
                RectTransform brt = btnObj.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 0f);
                brt.anchorMax = new Vector2(0.5f, 0f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(0f, 40f);
                brt.sizeDelta = new Vector2(260f, 54f);

                Image btnImg = btnObj.GetComponent<Image>();
                btnImg.color = new Color(0.12f, 0.16f, 0.23f, 1f);

                Outline btnOutline = btnObj.GetComponent<Outline>();
                btnOutline.effectColor = new Color(0.22f, 0.74f, 0.97f, 0.8f);
                btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

                powerOnButton = btnObj.GetComponent<Button>();
                powerOnButton.onClick.RemoveListener(PowerOnWorkstation);
                powerOnButton.onClick.AddListener(PowerOnWorkstation);

                GameObject pTextObj = new GameObject("Power_On_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                pTextObj.transform.SetParent(btnObj.transform, false);
                RectTransform prt = pTextObj.GetComponent<RectTransform>();
                prt.anchorMin = Vector2.zero;
                prt.anchorMax = Vector2.one;
                prt.offsetMin = Vector2.zero;
                prt.offsetMax = Vector2.zero;

                TextMeshProUGUI pTMP = pTextObj.GetComponent<TextMeshProUGUI>();
                if (terminalTMP != null && terminalTMP.font != null)
                {
                    pTMP.font = terminalTMP.font;
                }
                pTMP.alignment = TextAlignmentOptions.Center;
                pTMP.text = "<color=#38BDF8><b>\u23FB Power On Workstation</b></color>";
                pTMP.fontSize = 19;

                btnObj.SetActive(false);
                shutdownOverlay.SetActive(false);
            }
        }

        [ContextMenu("Execute Shutdown")]
        public void ExecuteShutdown()
        {
            Debug.Log("<color=#22C55E><b>[CADT VR]</b> Emergency Shutdown initiated! Halting malware breach sequence...</color>");

            if (audioSource != null)
            {
                if (shutdownClickSFX != null) audioSource.PlayOneShot(shutdownClickSFX);
                else if (alertBeepSFX != null) audioSource.PlayOneShot(alertBeepSFX);
            }

            StopBreachSequence();
            StopButtonPulse();

            if (shutdownButtonObject != null)
            {
                shutdownButtonObject.SetActive(false);
            }

            onShutdownClicked?.Invoke();

            if (shutdownRoutine != null) StopCoroutine(shutdownRoutine);
            shutdownRoutine = StartCoroutine(ShutdownSequenceRoutine());
        }

        private IEnumerator ShutdownSequenceRoutine()
        {
            EnsureShutdownOverlay();
            if (shutdownOverlay != null)
            {
                shutdownOverlay.SetActive(true);
                Image bg = shutdownOverlay.GetComponent<Image>();
                if (bg != null) bg.color = new Color(0.04f, 0.06f, 0.12f, 1f);
            }

            if (powerOnButton != null) powerOnButton.gameObject.SetActive(false);

            if (shutdownStatusTMP != null)
            {
                shutdownStatusTMP.text = "<size=44><b>Shutting down...</b></size>\n\n<size=20><color=#94A3B8>Terminating unauthorized network connections...\nStopping malicious subprocesses...</color></size>";
            }

            yield return new WaitForSeconds(2.0f);

            if (shutdownOverlay != null)
            {
                Image bg = shutdownOverlay.GetComponent<Image>();
                if (bg != null) bg.color = Color.black;
            }

            if (shutdownStatusTMP != null)
            {
                shutdownStatusTMP.text = "<size=36><color=#22C55E><b>\u25CF WORKSTATION POWERED OFF</b></color></size>\n\n<size=19><color=#94A3B8>Malware execution halted. Command Prompt terminated.\nNetwork interface disabled \u2014 C2 beacon isolated.</color></size>";
            }

            if (powerOnButton != null)
            {
                powerOnButton.gameObject.SetActive(true);
            }

            onWorkstationPoweredOff?.Invoke();
        }

        public void PowerOnWorkstation()
        {
            Debug.Log("<color=#38BDF8><b>[CADT VR]</b> Workstation powering back on...</color>");

            if (shutdownRoutine != null)
            {
                StopCoroutine(shutdownRoutine);
                shutdownRoutine = null;
            }

            if (shutdownOverlay != null)
            {
                shutdownOverlay.SetActive(false);
            }

            gameObject.SetActive(false);

            TelegramFileDownloadController downloadCtrl = FindFirstObjectByType<TelegramFileDownloadController>();
            if (downloadCtrl != null)
            {
                downloadCtrl.ResetDownloadState();
                if (downloadCtrl.TelegramWindowDisplay != null)
                {
                    downloadCtrl.TelegramWindowDisplay.SetActive(true);
                }
            }

            onWorkstationRebooted?.Invoke();
        }

        private void StartButtonPulse()
        {
            if (pulseButtonCoroutine != null) StopCoroutine(pulseButtonCoroutine);
            pulseButtonCoroutine = StartCoroutine(ButtonPulseRoutine());
        }

        private void StopButtonPulse()
        {
            if (pulseButtonCoroutine != null)
            {
                StopCoroutine(pulseButtonCoroutine);
                pulseButtonCoroutine = null;
            }
            if (shutdownButtonObject != null)
            {
                shutdownButtonObject.transform.localScale = Vector3.one;
            }
        }

        private IEnumerator ButtonPulseRoutine()
        {
            float timer = 0f;
            while (true)
            {
                timer += Time.deltaTime * 3.5f;
                float scale = 1f + (Mathf.Sin(timer) * 0.04f);
                if (shutdownButtonObject != null)
                {
                    shutdownButtonObject.transform.localScale = new Vector3(scale, scale, 1f);
                }
                yield return null;
            }
        }

        public void ResolveLights()
        {
            if (ceilingLight == null)
            {
                GameObject go = GameObject.Find("Ceiling_Recessed_Light");
                if (go != null) ceilingLight = go.GetComponent<Light>();
            }

            if (directionalLight == null)
            {
                GameObject go = GameObject.Find("Directional Light");
                if (go != null) directionalLight = go.GetComponent<Light>();
            }

            if (floorLampLight == null)
            {
                GameObject go = GameObject.Find("Lamp_WarmGlow");
                if (go != null) floorLampLight = go.GetComponent<Light>();
            }

            if (workstationAlarmLight == null)
            {
                GameObject go = GameObject.Find("Workstation_Emergency_Alarm_Light");
                if (go == null)
                {
                    go = new GameObject("Workstation_Emergency_Alarm_Light");
                    Light l = go.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 8f;
                    l.intensity = 0f;
                    l.enabled = false;
                    go.transform.position = new Vector3(-1.58f, 2.3f, 5.5f);
                }
                workstationAlarmLight = go.GetComponent<Light>();
            }

            if (!hasSavedLightState)
            {
                if (ceilingLight != null)
                {
                    originalCeilingColor = ceilingLight.color;
                    originalCeilingIntensity = ceilingLight.intensity;
                }
                if (directionalLight != null)
                {
                    originalDirColor = directionalLight.color;
                    originalDirIntensity = directionalLight.intensity;
                }
                if (floorLampLight != null)
                {
                    originalLampIntensity = floorLampLight.intensity;
                }
                hasSavedLightState = true;
            }
        }

        [ContextMenu("Start Sequence")]
        public void StartBreachSequence()
        {
            EnsureShutdownUI();
            if (shutdownButtonObject != null)
            {
                shutdownButtonObject.SetActive(true);
                StartButtonPulse();
            }
            if (shutdownOverlay != null)
            {
                shutdownOverlay.SetActive(false);
            }

            StartAlarmLighting();
            if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = StartCoroutine(BreachLoopRoutine());
        }

        public void StartBreachSequence(float delay)
        {
            lineDelay = delay;
            EnsureShutdownUI();
            if (shutdownButtonObject != null)
            {
                shutdownButtonObject.SetActive(true);
                StartButtonPulse();
            }
            if (shutdownOverlay != null)
            {
                shutdownOverlay.SetActive(false);
            }

            StartAlarmLighting();
            if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = StartCoroutine(BreachLoopRoutine());
        }

        public void StopBreachSequence()
        {
            StopAlarmLighting();
            StopButtonPulse();
            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
            }
            isRunning = false;
        }

        public void StartAlarmLighting()
        {
            if (!enableAlarmLighting) return;
            ResolveLights();

            if (alarmLightCoroutine != null) StopCoroutine(alarmLightCoroutine);
            alarmLightCoroutine = StartCoroutine(AlarmLightStrobeRoutine());

            if (playAlarmSirenAudio && audioSource != null && alertBeepSFX != null)
            {
                audioSource.clip = alertBeepSFX;
                audioSource.loop = true;
                if (!audioSource.isPlaying) audioSource.Play();
            }
        }

        public void StopAlarmLighting()
        {
            if (alarmLightCoroutine != null)
            {
                StopCoroutine(alarmLightCoroutine);
                alarmLightCoroutine = null;
            }

            if (audioSource != null && audioSource.isPlaying && audioSource.clip == alertBeepSFX)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }

            RestoreLights();
        }

        public void RestoreLights()
        {
            if (!hasSavedLightState) return;

            if (ceilingLight != null)
            {
                ceilingLight.color = originalCeilingColor;
                ceilingLight.intensity = originalCeilingIntensity;
            }

            if (directionalLight != null)
            {
                directionalLight.color = originalDirColor;
                directionalLight.intensity = originalDirIntensity;
            }

            if (floorLampLight != null)
            {
                floorLampLight.intensity = originalLampIntensity;
            }

            if (workstationAlarmLight != null)
            {
                workstationAlarmLight.intensity = 0f;
                workstationAlarmLight.enabled = false;
            }
        }

        private IEnumerator AlarmLightStrobeRoutine()
        {
            // Dim ambient light into deep emergency red
            if (directionalLight != null)
            {
                directionalLight.color = new Color(0.7f, 0.05f, 0.05f, 1.0f);
            }

            if (floorLampLight != null)
            {
                floorLampLight.intensity = 0.05f;
            }

            if (ceilingLight != null)
            {
                ceilingLight.color = alarmRedColor;
            }

            if (workstationAlarmLight != null)
            {
                workstationAlarmLight.enabled = true;
                workstationAlarmLight.color = alarmRedColor;
            }

            float timer = 0f;

            while (true)
            {
                timer += Time.deltaTime * alarmFlashSpeed;
                float cycle = timer % 1.0f;

                // Emergency alarm strobe:
                // 55% ON (bright red), 45% OFF (dark)
                bool isOn = cycle < 0.55f;

                if (ceilingLight != null)
                {
                    if (isOn)
                    {
                        float pulseFraction = cycle / 0.55f;
                        float brightness = Mathf.Lerp(ceilingAlarmIntensity, ceilingAlarmIntensity * 0.7f, pulseFraction);
                        ceilingLight.intensity = brightness;
                    }
                    else
                    {
                        ceilingLight.intensity = ceilingOffIntensity;
                    }
                }

                if (workstationAlarmLight != null)
                {
                    if (isOn)
                    {
                        float pulseFraction = cycle / 0.55f;
                        float brightness = Mathf.Lerp(ceilingAlarmIntensity, ceilingAlarmIntensity * 0.7f, pulseFraction);
                        workstationAlarmLight.intensity = brightness;
                    }
                    else
                    {
                        workstationAlarmLight.intensity = ceilingOffIntensity;
                    }
                }

                if (directionalLight != null)
                {
                    directionalLight.intensity = isOn ? 0.25f : 0.06f;
                }

                yield return null;
            }
        }

        private IEnumerator BreachLoopRoutine()
        {
            isRunning = true;
            onSequenceStarted?.Invoke();

            do
            {
                if (terminalTMP != null)
                {
                    terminalTMP.color = terminalTextColor;
                    terminalTMP.text = "";
                }

                StringBuilder sb = new StringBuilder();
                int totalLines = TerminalLines.Length;

                for (int i = 0; i < totalLines; i++)
                {
                    string line = TerminalLines[i];
                    sb.AppendLine(line);

                    if (terminalTMP != null)
                    {
                        terminalTMP.text = "<mspace=0.6em>" + sb.ToString() + "</mspace>_";
                    }

                    if (scrollRect != null)
                    {
                        Canvas.ForceUpdateCanvases();
                        scrollRect.verticalNormalizedPosition = 0f;
                    }

                    yield return new WaitForSeconds(lineDelay);
                }

                // Brief pause with cursor before loop
                float remainingTime = loopPauseDuration;
                float blinkTimer = 0f;
                bool cursorVisible = true;

                while (remainingTime > 0f)
                {
                    blinkTimer += Time.deltaTime;
                    remainingTime -= Time.deltaTime;

                    if (blinkTimer >= 0.15f)
                    {
                        blinkTimer = 0f;
                        cursorVisible = !cursorVisible;
                        if (terminalTMP != null)
                        {
                            terminalTMP.text = "<mspace=0.6em>" + sb.ToString() + "</mspace>" + (cursorVisible ? "_" : " ");
                        }
                    }

                    yield return null;
                }

                onSequenceCompleted?.Invoke();

            } while (loopSequence);

            isRunning = false;
        }
    }
}