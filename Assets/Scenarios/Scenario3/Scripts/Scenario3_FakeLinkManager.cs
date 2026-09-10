using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Scenario3_FakeLinkManager : ScenarioManager
{
    public enum Phase
    {
        Phase1_Immersion,     // 0: Relaxing in study, ambient sound, seated prompt
        Phase2_Hook,          // 1: Phone buzzes, Telegram message arrives from friend
        Phase3_Investigation, // 2: Webpage opens, QR code displayed, inspect red flags
        Phase4_Consequence,   // 3: Immediate consequence based on decision
        Phase5_Debrief        // 4: 3 Holographic cards, XP breakdown, return/replay
    }

    [Header("Phase State")]
    [SerializeField] private Phase currentPhase = Phase.Phase1_Immersion;

    [Header("Phase 1: Immersion")]
    [SerializeField] private float immersionDuration = 4f;
    [SerializeField] private GameObject immersionNotificationPrompt;
    [SerializeField] private Button skipImmersionButton;

    [Header("Phase 2: Hook / Telegram Desktop UI")]
    [SerializeField] private GameObject smartphoneObject;
    [SerializeField] private VRInteractiveSmartphone physicalSmartphone;
    [SerializeField] private VRHardwareSecurityKey physical2FAKey;
    [SerializeField] private GameObject telegramChatScreen;
    [SerializeField] private GameObject emptyChatStatePanel;
    [SerializeField] private GameObject activeConversationPanel;
    [SerializeField] private Button sopheaChatItemButton;
    [SerializeField] private TextMeshProUGUI friendNameText;
    [SerializeField] private TextMeshProUGUI messagePreviewText;
    [SerializeField] private Button openLinkButton;
    [SerializeField] private Button callFriendButton;
    [SerializeField] private TextMeshProUGUI callFriendButtonText;
    [SerializeField] private AudioWaveformVisualizer callWaveformVisualizer;

    [Header("Phase 3: Investigation / Webpage & QR Screen")]
    [SerializeField] private GameObject fakeWebpageScreen;
    [SerializeField] private TextMeshProUGUI urlAddressBarText;
    [SerializeField] private GameObject httpWarningBadge;
    [SerializeField] private GameObject telegramOfficialWarningBanner;
    [SerializeField] private GameObject qrCodePromptPanel;
    [SerializeField] private Button scanQRButton;       // Bad choice -> Failure
    [SerializeField] private Button refuseQRButton;     // Good choice -> Continues to verify
    [SerializeField] private Button inspectUrlButton;   // Red flag 1
    [SerializeField] private Button inspectHttpButton;  // Red flag 2
    [SerializeField] private Button inspectBannerButton;// Red flag 3
    [SerializeField] private Button inspectDeviceLinkButton; // Red flag 4
    [SerializeField] private Button enable2FAButton;    // Ideal action
    [SerializeField] private TextMeshProUGUI redFlagNotificationText;

    [Header("Phase 4: Consequence")]
    [SerializeField] private GameObject consequencePanel;
    [SerializeField] private TextMeshProUGUI consequenceTitleText;
    [SerializeField] private TextMeshProUGUI consequenceBodyText;
    [SerializeField] private Image consequenceStatusIcon;
    [SerializeField] private Color failureColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color idealColor = new Color(0.2f, 0.6f, 1.0f);

    [Header("Phase 5: Debrief & Holographic Cards")]
    [SerializeField] private GameObject debriefPanel;
    [SerializeField] private GameObject cardWeaponizedTrust;
    [SerializeField] private GameObject cardLinkDevicePurpose;
    [SerializeField] private GameObject cardTwoStepVerification;
    [SerializeField] private TextMeshProUGUI earnedXPText;
    [SerializeField] private TextMeshProUGUI redFlagsFoundText;
    [SerializeField] private Button debriefNextCardButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Audio")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip phoneBuzzClip;
    [SerializeField] private AudioClip linkClickClip;
    [SerializeField] private AudioClip hackAlarmClip;
    [SerializeField] private AudioClip victoryFanfareClip;

    private int activeDebriefCardIndex = 0;
    private bool playerScannedQR = false;
    private bool playerCalledFriend = false;
    private bool playerEnabled2FA = false;
    private Coroutine phaseTimerCoroutine;

    protected override void Start()
    {
        base.Start();
        SetupUIListeners();
        StartPhase1Immersion(); // Show 4-second mission briefing before Telegram hook
    }

    private void SetupUIListeners()
    {
        if (skipImmersionButton != null) skipImmersionButton.onClick.AddListener(StartPhase2Hook);
        if (sopheaChatItemButton != null) sopheaChatItemButton.onClick.AddListener(OpenSopheaChat);
        if (openLinkButton != null) openLinkButton.onClick.AddListener(OnOpenLinkClicked);
        if (callFriendButton != null) callFriendButton.onClick.AddListener(OnCallFriendClicked);
        if (scanQRButton != null) scanQRButton.onClick.AddListener(OnScanQRAction);
        if (refuseQRButton != null) refuseQRButton.onClick.AddListener(OnRefuseQRAction);

        // Red flag inspection triggers
        if (inspectUrlButton != null)
        {
            inspectUrlButton.onClick.AddListener(() =>
            {
                if (RegisterRedFlag("fake_domain", "Spoofed domain: http://nextgen-survey-portal2026.net instead of telegram.org"))
                {
                    ShowRedFlagFeedback("[FLAG 1] Spoofed URL domain detected!");
                    SetButtonIdentified(inspectUrlButton, "[OK] Fake Domain Found");
                }
            });
        }

        if (inspectHttpButton != null)
        {
            inspectHttpButton.onClick.AddListener(() =>
            {
                if (RegisterRedFlag("insecure_http", "Insecure HTTP connection without SSL encryption"))
                {
                    ShowRedFlagFeedback("[FLAG 2] Unencrypted HTTP connection detected!");
                    SetButtonIdentified(inspectHttpButton, "[OK] Insecure HTTP Found");
                }
            });
        }

        if (inspectBannerButton != null)
        {
            inspectBannerButton.onClick.AddListener(() =>
            {
                if (RegisterRedFlag("telegram_alert", "Official Telegram banner warns against linking untrusted QR devices"))
                {
                    ShowRedFlagFeedback("[FLAG 3] Official Telegram security warning noted!");
                    SetButtonIdentified(inspectBannerButton, "[OK] Warning Noted");
                }
            });
        }

        if (inspectDeviceLinkButton != null)
        {
            inspectDeviceLinkButton.onClick.AddListener(() =>
            {
                if (RegisterRedFlag("link_device_misuse", "Survey abuse: 'Link Desktop' authorizes full account takeover, not a survey response"))
                {
                    ShowRedFlagFeedback("[FLAG 4] Misuse of 'Link Device' feature identified!");
                    SetButtonIdentified(inspectDeviceLinkButton, "[OK] Feature Misuse Found");
                }
            });
        }

        if (enable2FAButton != null)
        {
            enable2FAButton.onClick.AddListener(OnEnable2FAClicked);
        }

        if (debriefNextCardButton != null) debriefNextCardButton.onClick.AddListener(ShowNextDebriefCard);
        if (restartButton != null) restartButton.onClick.AddListener(RestartScenario);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void SetButtonIdentified(Button btn, string newText)
    {
        if (btn == null) return;
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = newText;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = new Color(0.15f, 0.6f, 0.35f);

        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.PlayClickSound();
        }
    }

    private void ShowRedFlagFeedback(string message)
    {
        if (redFlagNotificationText != null)
        {
            redFlagNotificationText.text = message;
            redFlagNotificationText.gameObject.SetActive(true);
        }
        Debug.Log($"[Scenario3] {message}");
    }

    #region Phase 1: Immersion
    public void StartPhase1Immersion()
    {
        currentPhase = Phase.Phase1_Immersion;
        SetPhase(1);

        if (immersionNotificationPrompt != null) immersionNotificationPrompt.SetActive(true);
        if (telegramChatScreen != null) telegramChatScreen.SetActive(false);
        if (fakeWebpageScreen != null) fakeWebpageScreen.SetActive(false);
        if (consequencePanel != null) consequencePanel.SetActive(false);
        if (debriefPanel != null) debriefPanel.SetActive(false);

        if (phaseTimerCoroutine != null) StopCoroutine(phaseTimerCoroutine);
        phaseTimerCoroutine = StartCoroutine(ImmersionTimerRoutine());
    }

    private IEnumerator ImmersionTimerRoutine()
    {
        yield return new WaitForSeconds(immersionDuration);
        StartPhase2Hook();
    }
    #endregion

    #region Phase 2: Hook
    public void StartPhase2Hook()
    {
        if (phaseTimerCoroutine != null)
        {
            StopCoroutine(phaseTimerCoroutine);
            phaseTimerCoroutine = null;
        }

        currentPhase = Phase.Phase2_Hook;
        SetPhase(2);

        if (immersionNotificationPrompt != null) immersionNotificationPrompt.SetActive(false);
        if (telegramChatScreen != null) telegramChatScreen.SetActive(true);
        if (activeConversationPanel != null) activeConversationPanel.SetActive(true);
        if (emptyChatStatePanel != null) emptyChatStatePanel.SetActive(false);

        if (sfxAudioSource != null && phoneBuzzClip != null)
        {
            sfxAudioSource.PlayOneShot(phoneBuzzClip);
        }

        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.TriggerBothHandsHaptic(0.7f, 0.3f);
            VRInteractionHandler.Instance.PlayAlertSound();
        }

        if (friendNameText != null) friendNameText.text = "<b>Sophea (Close Friend)</b>  --  Online";
        if (messagePreviewText != null)
        {
            messagePreviewText.text = "Hey! Can you please help me fill out a quick survey for our NextGen project?\nWe urgently need 15 more responses before midnight for our project submission! It only takes 1 minute:\n\nLink: <color=#4FC3F7><u>http://nextgen-survey-portal2026.net/survey/sophea</u></color>";
        }

        if (callWaveformVisualizer != null)
        {
            callWaveformVisualizer.SetCallActive(false, "[STANDBY - LINE AVAILABLE]");
        }

        if (physicalSmartphone != null)
        {
            physicalSmartphone.StartIncomingCall("Sophea (Close Friend)", "Hey! Can you help me with our NextGen survey...");
        }
    }

    public void OpenSopheaChat()
    {
        if (emptyChatStatePanel != null) emptyChatStatePanel.SetActive(false);
        if (activeConversationPanel != null) activeConversationPanel.SetActive(true);
        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.PlayClickSound();
        }
    }

    public void OnOpenLinkClicked()
    {
        if (sfxAudioSource != null && linkClickClip != null)
        {
            sfxAudioSource.PlayOneShot(linkClickClip);
        }
        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.PlayClickSound();
        }
        StartPhase3Investigation();
    }

    public void OnCallFriendClicked()
    {
        playerCalledFriend = true;
        RegisterRedFlag("called_friend", "Called friend via phone call -- friend confirmed their account was compromised!");

        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.PlayClickSound();
        }

        if (messagePreviewText != null)
        {
            messagePreviewText.text = "<color=#81C784><b>[PHONE CALL CONNECTED]</b></color>\n\nSophea: <i>\"Don't click that link! My Telegram was hijacked 10 minutes ago by a phishing bot sending fake NextGen survey links!\"</i>";
        }

        if (callFriendButtonText != null)
        {
            callFriendButtonText.text = "[OK] Friend Confirmed Hacked!";
        }
        if (callFriendButton != null)
        {
            var img = callFriendButton.GetComponent<Image>();
            if (img != null) img.color = new Color(0.15f, 0.6f, 0.35f);
        }

        if (callWaveformVisualizer != null)
        {
            callWaveformVisualizer.SetCallActive(true, "[CALL CONNECTED - SOPHEA (CADT)]");
        }
    }
    #endregion

    #region Phase 3: Investigation
    public void StartPhase3Investigation()
    {
        currentPhase = Phase.Phase3_Investigation;
        SetPhase(3);

        if (telegramChatScreen != null) telegramChatScreen.SetActive(false);
        if (fakeWebpageScreen != null) fakeWebpageScreen.SetActive(true);

        if (urlAddressBarText != null)
        {
            urlAddressBarText.text = "[!] <color=#FFB74D>http://</color><color=#E57373>nextgen-survey-portal2026.net</color>/survey/verify-token?id=92841";
        }

        if (physicalSmartphone != null)
        {
            GameObject qrBox = GameObject.Find("QRBox");
            if (qrBox != null) physicalSmartphone.ActivateQRScanner(qrBox.transform);
        }
    }

    public void OnScanQRAction()
    {
        playerScannedQR = true;
        if (physicalSmartphone != null)
        {
            physicalSmartphone.SetPhoneState(VRInteractiveSmartphone.PhoneState.Compromised);
        }
        StartPhase4Consequence(OutcomeType.Failure);
    }

    public void OnRefuseQRAction()
    {
        playerScannedQR = false;
        if (physicalSmartphone != null)
        {
            physicalSmartphone.SetPhoneState(VRInteractiveSmartphone.PhoneState.Defended);
        }

        if (DiscoveredRedFlagsCount >= 3 && (playerCalledFriend || playerEnabled2FA))
        {
            StartPhase4Consequence(OutcomeType.Ideal);
        }
        else
        {
            StartPhase4Consequence(OutcomeType.Success);
        }
    }

    public void OnEnable2FAClicked()
    {
        playerEnabled2FA = true;
        RegisterRedFlag("2fa_enabled", "Two-Step Verification cloud password configured.");

        SetButtonIdentified(enable2FAButton, "[2FA] Cloud Password Active!");
        ShowRedFlagFeedback("[2FA] Two-Step Verification Enabled! Account is now protected against single-step QR hijacks.");
    }
    #endregion

    #region Phase 4: Consequence
    public void StartPhase4Consequence(OutcomeType outcome)
    {
        currentPhase = Phase.Phase4_Consequence;
        SetPhase(4);

        if (fakeWebpageScreen != null) fakeWebpageScreen.SetActive(false);
        if (consequencePanel != null) consequencePanel.SetActive(true);

        switch (outcome)
        {
            case OutcomeType.Failure:
                if (consequenceTitleText != null)
                {
                    consequenceTitleText.text = "[CRITICAL] ACCOUNT COMPROMISED!";
                    consequenceTitleText.color = failureColor;
                }
                if (consequenceBodyText != null)
                {
                    consequenceBodyText.text = "You scanned the QR code with Telegram's 'Link Desktop Device'.\n\n<color=#EF5350><b>CRITICAL BREACH:</b></color> The attackers instantly cloned your active Telegram session, stole your private chats, and forwarded the phishing survey link to all your contacts!";
                }
                if (consequenceStatusIcon != null) consequenceStatusIcon.color = failureColor;
                if (sfxAudioSource != null && hackAlarmClip != null) sfxAudioSource.PlayOneShot(hackAlarmClip);
                if (VRInteractionHandler.Instance != null) VRInteractionHandler.Instance.PlayAlertSound();
                break;

            case OutcomeType.Success:
                if (consequenceTitleText != null)
                {
                    consequenceTitleText.text = "[DEFENSE] ATTACK DEFENDED!";
                    consequenceTitleText.color = successColor;
                }
                if (consequenceBodyText != null)
                {
                    consequenceBodyText.text = "You refused to scan the suspicious QR code!\n\n<color=#81C784><b>SESSION SECURED:</b></color> You recognized the session takeover attempt and prevented the unauthorized desktop pairing from cloning your account.";
                }
                if (consequenceStatusIcon != null) consequenceStatusIcon.color = successColor;
                if (sfxAudioSource != null && victoryFanfareClip != null) sfxAudioSource.PlayOneShot(victoryFanfareClip);
                if (VRInteractionHandler.Instance != null) VRInteractionHandler.Instance.PlaySuccessSound();
                break;

            case OutcomeType.Ideal:
                if (consequenceTitleText != null)
                {
                    consequenceTitleText.text = "[PERFECT] PERFECT DEFENSE (IDEAL)!";
                    consequenceTitleText.color = idealColor;
                }
                if (consequenceBodyText != null)
                {
                    consequenceBodyText.text = "Outstanding cybersecurity vigilance!\n\n<color=#64B5F6><b>FULL MASTERY:</b></color> You spotted all fake URL indicators, rejected unauthorized device linking, verified out-of-band with your friend, and secured your account with 2FA!";
                }
                if (consequenceStatusIcon != null) consequenceStatusIcon.color = idealColor;
                if (sfxAudioSource != null && victoryFanfareClip != null) sfxAudioSource.PlayOneShot(victoryFanfareClip);
                if (VRInteractionHandler.Instance != null) VRInteractionHandler.Instance.PlaySuccessSound();
                break;
        }

        StartCoroutine(TransitionToDebriefRoutine(outcome));
    }

    private IEnumerator TransitionToDebriefRoutine(OutcomeType outcome)
    {
        yield return new WaitForSeconds(4.5f);
        StartPhase5Debrief(outcome);
    }
    #endregion

    #region Phase 5: Debrief
    public void StartPhase5Debrief(OutcomeType outcome)
    {
        currentPhase = Phase.Phase5_Debrief;
        SetPhase(5);

        if (consequencePanel != null) consequencePanel.SetActive(false);
        if (debriefPanel != null) debriefPanel.SetActive(true);

        activeDebriefCardIndex = 0;
        UpdateDebriefCards();

        int earnedXP = outcome switch
        {
            OutcomeType.Failure => 0,
            OutcomeType.Partial => 50,
            OutcomeType.Success => 100,
            OutcomeType.Ideal => 200,
            _ => 0
        };

        if (earnedXPText != null) earnedXPText.text = $"<b>+{earnedXP} XP Earned</b>";
        if (redFlagsFoundText != null) redFlagsFoundText.text = $"<b>Red Flags Found: {DiscoveredRedFlagsCount}/{TotalRedFlags}</b>";

        CompleteScenario(outcome);
    }

    private void UpdateDebriefCards()
    {
        if (cardWeaponizedTrust != null) cardWeaponizedTrust.SetActive(activeDebriefCardIndex == 0);
        if (cardLinkDevicePurpose != null) cardLinkDevicePurpose.SetActive(activeDebriefCardIndex == 1);
        if (cardTwoStepVerification != null) cardTwoStepVerification.SetActive(activeDebriefCardIndex == 2);
    }

    public void ShowNextDebriefCard()
    {
        activeDebriefCardIndex = (activeDebriefCardIndex + 1) % 3;
        UpdateDebriefCards();

        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.PlayClickSound();
        }
    }
    #endregion
}
