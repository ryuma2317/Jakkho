using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Physical, grabbable 3D Smartphone simulation for VR.
/// Features 6-DOF hand grabbing, desk ringing/vibrations, proximity-to-ear detection,
/// live camera QR scanning aimed at the monitor, and Telegram desktop authorization prompts.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class VRInteractiveSmartphone : MonoBehaviour
{
    public enum PhoneState
    {
        LockScreen,
        IncomingCall,
        InCall,
        QRScanner,
        QRAuthorizationPrompt,
        Defended,
        Compromised
    }

    [Header("Current State")]
    [SerializeField] private PhoneState currentState = PhoneState.LockScreen;

    [Header("Screen UI Panels")]
    [SerializeField] private GameObject lockScreenPanel;
    [SerializeField] private GameObject incomingCallPanel;
    [SerializeField] private GameObject inCallPanel;
    [SerializeField] private GameObject qrScannerPanel;
    [SerializeField] private GameObject qrPromptPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("Screen UI Elements")]
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI callStatusText;
    [SerializeField] private TextMeshProUGUI inCallDialogueText;
    [SerializeField] private TextMeshProUGUI promptWarningText;
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultBodyText;
    [SerializeField] private Image scannerReticleImage;
    [SerializeField] private Button answerCallButton;
    [SerializeField] private Button rejectCallButton;
    [SerializeField] private Button authorizeLoginButton; // Failure path
    [SerializeField] private Button rejectLoginButton;    // Success path

    [Header("Scanning Targets")]
    [SerializeField] private Transform qrTargetTransform;
    [SerializeField] private float maxScanDistance = 2.5f;
    [SerializeField] private float scanAimAngleThreshold = 35f;

    [Header("Audio & Haptics")]
    [SerializeField] private AudioSource phoneAudioSource;
    [SerializeField] private AudioClip ringtoneClip;
    [SerializeField] private AudioClip scanBeepClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip alertClip;

    private XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor holdingInteractor;
    private float scanProgress = 0f;
    private const float ScanDurationNeeded = 0.85f;
    private Transform mainCameraTransform;

    public PhoneState CurrentState => currentState;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        SetupButtonListeners();
    }

    private void Start()
    {
        SetPhoneState(PhoneState.LockScreen);
    }

    private float hapticTimer = 0f;

    private void Update()
    {
        UpdateClock();
        CheckHeadsetProximity();

        if (currentState == PhoneState.QRScanner)
        {
            UpdateQRScanning();
        }

        if (currentState == PhoneState.IncomingCall && isGrabbed)
        {
            // Realistic phone dual-pulse vibration rhythm (pulse at 0s and 0.25s every 1.6s)
            hapticTimer += Time.deltaTime;
            float cycle = hapticTimer % 1.6f;
            if ((cycle > 0.02f && cycle < 0.12f) || (cycle > 0.22f && cycle < 0.32f))
            {
                TriggerHapticPulse(0.35f, 0.08f);
            }
        }
        else
        {
            hapticTimer = 0f;
        }
    }

    private void SetupButtonListeners()
    {
        if (answerCallButton != null) answerCallButton.onClick.AddListener(AnswerCall);
        if (rejectCallButton != null) rejectCallButton.onClick.AddListener(HangUpCall);
        if (authorizeLoginButton != null) authorizeLoginButton.onClick.AddListener(OnAuthorizeLoginSelected);
        if (rejectLoginButton != null) rejectLoginButton.onClick.AddListener(OnRejectLoginSelected);
    }

    private void UpdateClock()
    {
        if (clockText != null)
        {
            clockText.text = System.DateTime.Now.ToString("h:mm");
        }
    }

    // -------------------------------------------------------------------------
    // Grab & Release
    // -------------------------------------------------------------------------
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        holdingInteractor = args.interactorObject;
        TriggerHapticPulse(0.5f, 0.08f);

        // If ringing and grabbed, picking it up can auto-connect or mute ringtone
        if (currentState == PhoneState.IncomingCall)
        {
            if (phoneAudioSource != null && phoneAudioSource.isPlaying)
            {
                phoneAudioSource.volume = 0.4f; // Mute loud speaker to earpiece
            }
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        holdingInteractor = null;
    }

    private void TriggerHapticPulse(float amplitude, float duration)
    {
        if (holdingInteractor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputInteractor)
        {
            inputInteractor.SendHapticImpulse(amplitude, duration);
        }
    }

    // -------------------------------------------------------------------------
    // Proximity to Headset (Hold Phone to Ear)
    // -------------------------------------------------------------------------
    private void CheckHeadsetProximity()
    {
        if (mainCameraTransform == null && Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        if (mainCameraTransform == null) return;

        float distToHead = Vector3.Distance(transform.position, mainCameraTransform.position);

        if (distToHead < 0.22f) // Within 22cm of ear
        {
            if (currentState == PhoneState.IncomingCall)
            {
                AnswerCall(); // Bringing to ear auto-answers the call!
            }
        }
    }

    // -------------------------------------------------------------------------
    // QR Code Camera Aim & Scanning Logic
    // -------------------------------------------------------------------------
    private void UpdateQRScanning()
    {
        if (qrTargetTransform == null)
        {
            // Auto-locate QR Box if not assigned
            GameObject qrBox = GameObject.Find("QRBox");
            if (qrBox != null) qrTargetTransform = qrBox.transform;
            else return;
        }

        Vector3 toQR = qrTargetTransform.position - transform.position;
        float distance = toQR.magnitude;

        // Phone back camera points along local -Z (or +Z depending on model)
        Vector3 cameraForward = -transform.forward; 
        float angle = Vector3.Angle(cameraForward, toQR.normalized);

        bool isAimingAtQR = (distance <= maxScanDistance) && (angle <= scanAimAngleThreshold);

        if (isAimingAtQR)
        {
            scanProgress += Time.deltaTime;
            float fillPct = Mathf.Clamp01(scanProgress / ScanDurationNeeded);

            if (scannerReticleImage != null)
            {
                scannerReticleImage.color = Color.Lerp(new Color(0.2f, 0.8f, 1f, 0.5f), new Color(0.1f, 1f, 0.4f, 1f), fillPct);
                scannerReticleImage.transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 0.95f, fillPct);
            }

            TriggerHapticPulse(0.2f * fillPct + 0.1f, 0.04f);

            if (scanProgress >= ScanDurationNeeded)
            {
                // Successful Scan Lock-On!
                TriggerHapticPulse(0.8f, 0.2f);
                if (phoneAudioSource != null && scanBeepClip != null)
                {
                    phoneAudioSource.PlayOneShot(scanBeepClip);
                }
                SetPhoneState(PhoneState.QRAuthorizationPrompt);
            }
        }
        else
        {
            // Decay scan progress if aimed away
            scanProgress = Mathf.Max(0f, scanProgress - Time.deltaTime * 2f);
            if (scannerReticleImage != null)
            {
                scannerReticleImage.color = new Color(0.2f, 0.8f, 1f, 0.4f);
                scannerReticleImage.transform.localScale = Vector3.one;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Phase State Machine
    // -------------------------------------------------------------------------
    public void SetPhoneState(PhoneState newState)
    {
        currentState = newState;

        if (lockScreenPanel != null) lockScreenPanel.SetActive(newState == PhoneState.LockScreen);
        if (incomingCallPanel != null) incomingCallPanel.SetActive(newState == PhoneState.IncomingCall);
        if (inCallPanel != null) inCallPanel.SetActive(newState == PhoneState.InCall);
        if (qrScannerPanel != null) qrScannerPanel.SetActive(newState == PhoneState.QRScanner);
        if (qrPromptPanel != null) qrPromptPanel.SetActive(newState == PhoneState.QRAuthorizationPrompt);
        if (resultPanel != null) resultPanel.SetActive(newState == PhoneState.Defended || newState == PhoneState.Compromised);

        // Stop ringing whenever transitioning away from IncomingCall
        if (newState != PhoneState.IncomingCall && phoneAudioSource != null && phoneAudioSource.isPlaying)
        {
            phoneAudioSource.Stop();
            phoneAudioSource.loop = false;
        }

        switch (newState)
        {
            case PhoneState.IncomingCall:
                scanProgress = 0f;
                if (phoneAudioSource != null && ringtoneClip != null)
                {
                    phoneAudioSource.clip = ringtoneClip;
                    phoneAudioSource.loop = true;
                    phoneAudioSource.volume = 0.60f;
                    phoneAudioSource.Play();
                }
                break;

            case PhoneState.InCall:
                if (phoneAudioSource != null)
                {
                    phoneAudioSource.Stop();
                    phoneAudioSource.loop = false;
                }
                break;

            case PhoneState.QRScanner:
                scanProgress = 0f;
                break;

            case PhoneState.QRAuthorizationPrompt:
                if (promptWarningText != null)
                {
                    promptWarningText.text =
                        "<color=#FF5252><b>LINK DESKTOP DEVICE?</b></color>\n\n" +
                        "A browser requested access to your Telegram account:\n\n" +
                        "• <b>Device:</b> DESKTOP-ATTACKER-92\n" +
                        "• <b>IP:</b> 185.220.101.5 (Kyiv, Ukraine)\n" +
                        "• <b>Time:</b> Just now\n\n" +
                        "<i>Warning: Linking will allow this computer to read all messages and chats!</i>";
                }
                break;

            case PhoneState.Defended:
                if (resultTitleText != null)
                {
                    resultTitleText.text = "[DEFENSE SUCCESSFUL]";
                    resultTitleText.color = new Color(0.2f, 0.9f, 0.4f);
                }
                if (resultBodyText != null)
                {
                    resultBodyText.text = "Session link rejected! Attacker failed to clone your account.";
                }
                break;

            case PhoneState.Compromised:
                if (resultTitleText != null)
                {
                    resultTitleText.text = "[CRITICAL BREACH]";
                    resultTitleText.color = new Color(0.95f, 0.2f, 0.2f);
                }
                if (resultBodyText != null)
                {
                    resultBodyText.text = "Session authorized! Attacker cloned your Telegram account.";
                }
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Player Decision Actions
    // -------------------------------------------------------------------------
    public void StartIncomingCall(string callerName, string previewMsg)
    {
        if (callStatusText != null)
        {
            callStatusText.text = $"<b>{callerName}</b>\n<size=14>Incoming Telegram Voice Call...</size>";
        }
        SetPhoneState(PhoneState.IncomingCall);
    }

    public void AnswerCall()
    {
        if (currentState != PhoneState.IncomingCall) return;

        SetPhoneState(PhoneState.InCall);
        TriggerHapticPulse(0.6f, 0.12f);

        if (inCallDialogueText != null)
        {
            inCallDialogueText.text =
                "<b>Sophea:</b>\n" +
                "<i>\"Hey, don't click that link! My Telegram was hijacked 10 minutes ago by a phishing survey bot!\"</i>";
        }

        // Notify scenario manager
        var mgr = FindObjectOfType<Scenario3_FakeLinkManager>();
        if (mgr != null)
        {
            mgr.OnCallFriendClicked();
        }
    }

    public void HangUpCall()
    {
        if (phoneAudioSource != null)
        {
            phoneAudioSource.Stop();
            phoneAudioSource.loop = false;
        }
        SetPhoneState(PhoneState.LockScreen);
    }

    public void ActivateQRScanner(Transform targetQR)
    {
        qrTargetTransform = targetQR;
        SetPhoneState(PhoneState.QRScanner);
    }

    private void OnAuthorizeLoginSelected()
    {
        SetPhoneState(PhoneState.Compromised);
        TriggerHapticPulse(0.9f, 0.3f);
        if (phoneAudioSource != null && alertClip != null) phoneAudioSource.PlayOneShot(alertClip);

        var mgr = FindObjectOfType<Scenario3_FakeLinkManager>();
        if (mgr != null)
        {
            mgr.OnScanQRAction(); // Triggers failure consequence
        }
    }

    private void OnRejectLoginSelected()
    {
        SetPhoneState(PhoneState.Defended);
        TriggerHapticPulse(0.6f, 0.15f);
        if (phoneAudioSource != null && successClip != null) phoneAudioSource.PlayOneShot(successClip);

        var mgr = FindObjectOfType<Scenario3_FakeLinkManager>();
        if (mgr != null)
        {
            mgr.OnRefuseQRAction(); // Triggers victory consequence
        }
    }

    public void SetTargetQR(Transform target)
    {
        qrTargetTransform = target;
    }
}
