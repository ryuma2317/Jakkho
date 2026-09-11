using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Scenario3_FakeLinkManager : ScenarioManager
{
    public enum Phase
    {
        Phase1_Immersion,     // 0: Relaxing in study, atmospheric intro
        Phase2_Hook,          // 1: Phone buzzes, Telegram message arrives from friend
        Phase3_Investigation, // 2: Webpage opens, QR code displayed, inspect red flags
        Phase4_Consequence,   // 3: Immediate consequence based on decision
        Phase5_Debrief        // 4: 3 Holographic cards, XP breakdown, return/replay
    }

    [Header("Phase State")]
    [SerializeField] private Phase currentPhase = Phase.Phase1_Immersion;
    public Phase CurrentPhase => currentPhase;

    // ─────────────────────────────────────────────
    //  Phase 1: Immersion
    // ─────────────────────────────────────────────
    [Header("Phase 1: Immersion")]
    [SerializeField] private float immersionDuration = 5f;
    [SerializeField] private GameObject immersionNotificationPrompt;
    [SerializeField] private Button skipImmersionButton;

    // ─────────────────────────────────────────────
    //  Phase 2: Hook — Telegram Desktop UI
    // ─────────────────────────────────────────────
    [Header("Phase 2: Hook / Telegram Desktop UI")]
    [SerializeField] private GameObject smartphoneObject;
    [SerializeField] private VRInteractiveSmartphone physicalSmartphone;
    [SerializeField] private VRHardwareSecurityKey physical2FAKey;
    [SerializeField] private GameObject telegramChatScreen;
    [SerializeField] private GameObject emptyChatStatePanel;
    [SerializeField] private GameObject activeConversationPanel;
    [SerializeField] private Button sopheaChatItemButton;
    [SerializeField] private TextMeshProUGUI friendNameText;
    [SerializeField] private GameObject typingIndicator;     // "Sophea is typing..."
    [SerializeField] private GameObject messageBubble;       // Sophea's chat bubble
    [SerializeField] private TextMeshProUGUI messagePreviewText;
    [SerializeField] private Button openLinkButton;
    [SerializeField] private Button refuseLinkButton;        // NEW — refuse in Phase 2
    [SerializeField] private Button callFriendButton;
    [SerializeField] private TextMeshProUGUI callFriendButtonText;
    [SerializeField] private AudioWaveformVisualizer callWaveformVisualizer;
    [SerializeField] private TextMeshProUGUI linkWarningText; // NEW — shown after call

    // ─────────────────────────────────────────────
    //  Phase 3: Investigation — Webpage & QR
    // ─────────────────────────────────────────────
    [Header("Phase 3: Investigation / Webpage & QR Screen")]
    [SerializeField] private GameObject fakeWebpageScreen;
    [SerializeField] private TextMeshProUGUI urlAddressBarText;
    [SerializeField] private GameObject httpWarningBadge;
    [SerializeField] private GameObject telegramOfficialWarningBanner;
    [SerializeField] private GameObject qrCodePromptPanel;
    [SerializeField] private Button scanQRButton;
    [SerializeField] private Button refuseQRButton;
    [SerializeField] private Button inspectUrlButton;
    [SerializeField] private Button inspectHttpButton;
    [SerializeField] private Button inspectBannerButton;
    [SerializeField] private Button inspectDeviceLinkButton;
    [SerializeField] private Button enable2FAButton;
    [SerializeField] private TextMeshProUGUI redFlagNotificationText;
    [SerializeField] private TextMeshProUGUI flagHistoryText; // NEW — accumulating flag log

    [Header("Phase 3: Decision Confirmation Overlay")]
    [SerializeField] private GameObject decisionConfirmOverlay;
    [SerializeField] private TextMeshProUGUI confirmQuestionText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmCancelButton;

    // ─────────────────────────────────────────────
    //  Phase 4: Consequence
    // ─────────────────────────────────────────────
    [Header("Phase 4: Consequence")]
    [SerializeField] private GameObject consequencePanel;
    [SerializeField] private TextMeshProUGUI consequenceTitleText;
    [SerializeField] private TextMeshProUGUI consequenceBodyText;
    [SerializeField] private Image consequenceStatusIcon;
    [SerializeField] private Button continueToDebriefButton; // NEW — replaces auto-advance
    [SerializeField] private Color failureColor  = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color successColor  = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color idealColor    = new Color(0.2f, 0.6f, 1.0f);

    // ─────────────────────────────────────────────
    //  Phase 5: Debrief
    // ─────────────────────────────────────────────
    [Header("Phase 5: Debrief & Holographic Cards")]
    [SerializeField] private GameObject debriefPanel;
    [SerializeField] private GameObject cardWeaponizedTrust;
    [SerializeField] private GameObject cardLinkDevicePurpose;
    [SerializeField] private GameObject cardTwoStepVerification;
    [SerializeField] private TextMeshProUGUI earnedXPText;
    [SerializeField] private TextMeshProUGUI redFlagsFoundText;
    [SerializeField] private TextMeshProUGUI cardCounterText;  // NEW — "1 / 3"
    [SerializeField] private Button debriefNextCardButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    // ─────────────────────────────────────────────
    //  Audio
    // ─────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip phoneBuzzClip;
    [SerializeField] private AudioClip linkClickClip;
    [SerializeField] private AudioClip hackAlarmClip;
    [SerializeField] private AudioClip victoryFanfareClip;

    // ─────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────
    private int activeDebriefCardIndex = 0;
    private bool playerScannedQR    = false;
    private bool playerCalledFriend = false; // separate — not a red flag
    private bool playerEnabled2FA   = false; // separate — not a red flag
    private bool linkButtonGated    = false; // true after Sophea confirms hack
    private OutcomeType pendingOutcome;
    private System.Action pendingConfirmAction;
    private Coroutine phaseTimerCoroutine;
    private Coroutine typewriterCoroutine;
    private Coroutine typingSequenceCoroutine;
    private List<string> discoveredFlagMessages = new List<string>();

    // ─────────────────────────────────────────────
    //  Startup
    // ─────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();
        SetupUIListeners();
        StartPhase1Immersion();
    }

    private void SetupUIListeners()
    {
        if (skipImmersionButton   != null) skipImmersionButton.onClick.AddListener(StartPhase2Hook);
        if (sopheaChatItemButton  != null) sopheaChatItemButton.onClick.AddListener(OpenSopheaChat);
        if (openLinkButton        != null) openLinkButton.onClick.AddListener(OnOpenLinkClicked);
        if (refuseLinkButton      != null) refuseLinkButton.onClick.AddListener(OnRefuseLinkInPhase2);
        if (callFriendButton      != null) callFriendButton.onClick.AddListener(OnCallFriendClicked);

        if (scanQRButton    != null) scanQRButton.onClick.AddListener(PromptScanQRConfirmation);
        if (refuseQRButton  != null) refuseQRButton.onClick.AddListener(PromptRefuseQRConfirmation);
        if (confirmYesButton    != null) confirmYesButton.onClick.AddListener(OnConfirmDecision);
        if (confirmCancelButton != null) confirmCancelButton.onClick.AddListener(OnCancelDecision);

        // Red flag inspection triggers
        if (inspectUrlButton != null)
        {
            inspectUrlButton.onClick.AddListener(() =>
            {
                if (RegisterRedFlag("fake_domain", "Spoofed domain detected — not telegram.org"))
                {
                    AppendFlagMessage("FLAG 1: Spoofed domain — http://nextgen-survey-portal2026.net");
                    SetButtonIdentified(inspectUrlButton, "[✓] Fake Domain Found");
                    UpdateTelemetry();
                }
            });
        }

        if (inspectHttpButton != null)
        {
            inspectHttpButton.onClick.AddListener(() =>
            {
                if (RegisterRedFlag("insecure_http", "Unencrypted HTTP — no SSL/TLS"))
                {
                    AppendFlagMessage("FLAG 2: Insecure HTTP — no HTTPS encryption");
                    SetButtonIdentified(inspectHttpButton, "[✓] Insecure HTTP Found");
                    UpdateTelemetry();
                }
            });
        }

        if (inspectBannerButton != null)
        {
            inspectBannerButton.onClick.AddListener(() =>
            {
                if (RegisterRedFlag("telegram_alert", "Security banner warns against linking QR devices"))
                {
                    AppendFlagMessage("FLAG 3: Official warning — do NOT link QR devices");
                    SetButtonIdentified(inspectBannerButton, "[✓] Warning Noted");
                    UpdateTelemetry();
                }
            });
        }

        if (inspectDeviceLinkButton != null)
        {
            inspectDeviceLinkButton.onClick.AddListener(() =>
            {
                if (RegisterRedFlag("link_device_misuse", "QR code is a session-pairing token, not a survey vote"))
                {
                    AppendFlagMessage("FLAG 4: 'Link Device' = full account takeover, not a survey");
                    SetButtonIdentified(inspectDeviceLinkButton, "[✓] Feature Misuse Found");
                    UpdateTelemetry();
                }
            });
        }

        if (enable2FAButton != null) enable2FAButton.onClick.AddListener(OnEnable2FAClicked);

        if (continueToDebriefButton != null) continueToDebriefButton.onClick.AddListener(OnContinueToDebrief);

        if (debriefNextCardButton != null) debriefNextCardButton.onClick.AddListener(ShowNextDebriefCard);
        if (restartButton         != null) restartButton.onClick.AddListener(RestartScenario);
        if (mainMenuButton        != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    // ─────────────────────────────────────────────
    //  PHASE 1: Immersion — atmospheric only, no spoilers
    // ─────────────────────────────────────────────
    #region Phase 1: Immersion
    public void StartPhase1Immersion()
    {
        currentPhase = Phase.Phase1_Immersion;
        SetPhase(1);

        if (immersionNotificationPrompt != null) immersionNotificationPrompt.SetActive(true);
        if (telegramChatScreen          != null) telegramChatScreen.SetActive(false);
        if (fakeWebpageScreen           != null) fakeWebpageScreen.SetActive(false);
        if (consequencePanel            != null) consequencePanel.SetActive(false);
        if (debriefPanel                != null) debriefPanel.SetActive(false);

        if (phaseTimerCoroutine != null) StopCoroutine(phaseTimerCoroutine);
        phaseTimerCoroutine = StartCoroutine(ImmersionTimerRoutine());
    }

    private IEnumerator ImmersionTimerRoutine()
    {
        yield return new WaitForSeconds(immersionDuration);
        StartPhase2Hook();
    }
    #endregion

    // ─────────────────────────────────────────────
    //  PHASE 2: Hook — Telegram message arrives
    // ─────────────────────────────────────────────
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
        if (telegramChatScreen          != null) telegramChatScreen.SetActive(true);
        if (activeConversationPanel     != null) activeConversationPanel.SetActive(true);
        if (emptyChatStatePanel         != null) emptyChatStatePanel.SetActive(false);

        // Haptic + sound: incoming message
        if (sfxAudioSource != null && phoneBuzzClip != null)
            sfxAudioSource.PlayOneShot(phoneBuzzClip);

        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.TriggerBothHandsHaptic(0.7f, 0.3f);
            VRInteractionHandler.Instance.PlayAlertSound();
        }

        if (friendNameText != null)
            friendNameText.text = "<b>Sophea (Close Friend)</b>  --  <color=#00C853>Online</color>";

        // Show typing indicator first, then reveal bubble with typewriter
        if (typingIndicator != null) typingIndicator.SetActive(true);
        if (messageBubble   != null) messageBubble.SetActive(false);

        if (typingSequenceCoroutine != null) StopCoroutine(typingSequenceCoroutine);
        typingSequenceCoroutine = StartCoroutine(SopheaTypingSequence());

        if (callWaveformVisualizer != null)
            callWaveformVisualizer.SetCallActive(false, "[STANDBY - LINE AVAILABLE]");

        // Hide link warning initially
        if (linkWarningText != null) linkWarningText.gameObject.SetActive(false);

        if (physicalSmartphone != null)
            physicalSmartphone.OfferCallToContact("Sophea Kem");
    }

    private IEnumerator SopheaTypingSequence()
    {
        yield return new WaitForSeconds(1.8f);

        if (typingIndicator != null) typingIndicator.SetActive(false);
        if (messageBubble   != null) messageBubble.SetActive(true);

        string sopheaMessage =
            "Hey! Can you please help me fill out a quick survey for our NextGen project?\n" +
            "We urgently need 15 more responses before midnight for our project submission! It only takes 1 minute:\n\n" +
            "Link: <color=#4FC3F7><u>http://nextgen-survey-portal2026.net/survey/sophea</u></color>";

        if (messagePreviewText != null)
        {
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriterReveal(messagePreviewText, sopheaMessage, 0.025f));
        }
        typingSequenceCoroutine = null;
    }

    public void OpenSopheaChat()
    {
        if (emptyChatStatePanel         != null) emptyChatStatePanel.SetActive(false);
        if (activeConversationPanel     != null) activeConversationPanel.SetActive(true);
        PlayClick();
    }

    public void OnCallFriendClicked()
    {
        if (playerCalledFriend) return; // prevent double-fire
        playerCalledFriend = true;

        PlayClick();

        // The call itself happens on the physical smartphone (dialogue shown there).
        // On the Telegram screen we only surface the warning under the link card.
        linkButtonGated = true;
        if (linkWarningText != null)
        {
            linkWarningText.gameObject.SetActive(true);
            linkWarningText.text = "<b>Sophea said DO NOT click this link!</b> Her account was hijacked.";
        }
        if (openLinkButton != null)
        {
            var img = openLinkButton.GetComponent<Image>();
            if (img != null) img.color = new Color(1f, 0.92f, 0.92f); // red tint warning
        }
    }

    public void OnOpenLinkClicked()
    {
        if (sfxAudioSource != null && linkClickClip != null)
            sfxAudioSource.PlayOneShot(linkClickClip);

        if (linkButtonGated && VRInteractionHandler.Instance != null)
            VRInteractionHandler.Instance.PlayAlertSound();
        else
            PlayClick();

        StartPhase3Investigation();
    }

    /// <summary>
    /// Player refuses the link in Phase 2 without even opening it — smart early exit.
    /// </summary>
    public void OnRefuseLinkInPhase2()
    {
        PlayClick();

        // Register the "called_friend" bonus to track if they called too
        // Outcome: if they called → Ideal; else → Success
        if (playerCalledFriend)
            StartPhase4Consequence(OutcomeType.Ideal);
        else
            StartPhase4Consequence(OutcomeType.Success);
    }
    #endregion

    // ─────────────────────────────────────────────
    //  PHASE 3: Investigation — Webpage + QR
    // ─────────────────────────────────────────────
    #region Phase 3: Investigation
    public void StartPhase3Investigation()
    {
        currentPhase = Phase.Phase3_Investigation;
        SetPhase(3);

        discoveredFlagMessages.Clear();

        if (telegramChatScreen  != null) telegramChatScreen.SetActive(false);
        if (fakeWebpageScreen   != null) fakeWebpageScreen.SetActive(true);

        if (urlAddressBarText != null)
        {
            urlAddressBarText.text =
                "<color=#EF5350>[!]</color> <color=#FFB74D>http://</color>" +
                "<color=#E57373>nextgen-survey-portal2026.net</color>/survey/verify-token?id=92841";
        }

        UpdateTelemetry();

        if (physicalSmartphone != null)
        {
            GameObject qrBox = GameObject.Find("QRBox");
            if (qrBox != null) physicalSmartphone.ActivateQRScanner(qrBox.transform);
        }
    }

    public void PromptScanQRConfirmation()
    {
        PlayClick();
        pendingConfirmAction = ExecuteScanQR;

        if (confirmQuestionText != null)
        {
            confirmQuestionText.text =
                "<size=20><color=#EF5350><b>CRITICAL WARNING: QR DEVICE PAIRING</b></color></size>\n\n" +
                "You are about to scan this QR code with Telegram's <b>'Link Desktop Device'</b>.\n\n" +
                "<color=#FFE082>This will authorize an external server to download your chats and hijack your active session.\n\n" +
                "Are you sure you want to authorize?</color>";
        }

        if (confirmYesButton != null)
        {
            var txt = confirmYesButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = "<b>[ AUTHORIZE & PAIR ]</b>";
            var img = confirmYesButton.GetComponent<Image>();
            if (img != null) img.color = new Color(0.75f, 0.22f, 0.18f); // red
        }

        if (decisionConfirmOverlay != null) decisionConfirmOverlay.SetActive(true);
    }

    public void PromptRefuseQRConfirmation()
    {
        PlayClick();
        pendingConfirmAction = ExecuteRefuseQR;

        if (confirmQuestionText != null)
        {
            confirmQuestionText.text =
                "<size=20><color=#81C784><b>DEFENSIVE ACTION: REFUSE PAIRING</b></color></size>\n\n" +
                "You are about to refuse the QR session pairing and lock in your cyber defense.\n\n" +
                $"<color=#80D8FF>Red Flags Discovered: <b>{DiscoveredRedFlagsCount}/4</b>\n" +
                $"Called Friend: <b>{(playerCalledFriend ? "Yes" : "No")}</b> | 2FA Enabled: <b>{(playerEnabled2FA ? "Yes" : "No")}</b>\n\n" +
                "Are you ready to submit your defense?</color>";
        }

        if (confirmYesButton != null)
        {
            var txt = confirmYesButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = "<b>[ CONFIRM & SECURE ]</b>";
            var img = confirmYesButton.GetComponent<Image>();
            if (img != null) img.color = new Color(0.18f, 0.65f, 0.38f); // green
        }

        if (decisionConfirmOverlay != null) decisionConfirmOverlay.SetActive(true);
    }

    public void OnConfirmDecision()
    {
        PlayClick();
        if (decisionConfirmOverlay != null) decisionConfirmOverlay.SetActive(false);
        pendingConfirmAction?.Invoke();
        pendingConfirmAction = null;
    }

    public void OnCancelDecision()
    {
        PlayClick();
        if (decisionConfirmOverlay != null) decisionConfirmOverlay.SetActive(false);
        pendingConfirmAction = null;
    }

    public void OnScanQRAction() => ExecuteScanQR();
    public void OnRefuseQRAction() => ExecuteRefuseQR();

    private void ExecuteScanQR()
    {
        playerScannedQR = true;
        if (physicalSmartphone != null)
            physicalSmartphone.SetPhoneState(VRInteractiveSmartphone.PhoneState.Compromised);

        StartPhase4Consequence(OutcomeType.Failure);
    }

    private void ExecuteRefuseQR()
    {
        playerScannedQR = false;
        if (physicalSmartphone != null)
            physicalSmartphone.SetPhoneState(VRInteractiveSmartphone.PhoneState.Defended);

        // Ideal: found all 4 flags AND (called friend OR enabled 2FA)
        if (DiscoveredRedFlagsCount >= 4 && (playerCalledFriend || playerEnabled2FA))
            StartPhase4Consequence(OutcomeType.Ideal);
        else if (DiscoveredRedFlagsCount >= 2)
            StartPhase4Consequence(OutcomeType.Success);
        else
            StartPhase4Consequence(OutcomeType.Partial);
    }

    public void OnEnable2FAClicked()
    {
        if (playerEnabled2FA) return;
        playerEnabled2FA = true;
        PlayClick();

        SetButtonIdentified(enable2FAButton, "[✓] 2FA Active — Cloud Password Set!");
        UpdateTelemetry();

        if (VRInteractionHandler.Instance != null)
            VRInteractionHandler.Instance.PlaySuccessSound();
    }

    private void AppendFlagMessage(string msg)
    {
        discoveredFlagMessages.Add(msg);
        if (redFlagNotificationText != null)
        {
            // Show last-found as highlighted headline
            redFlagNotificationText.text = "<color=#FFE082><b>FOUND:</b></color> " + msg;
        }
        if (flagHistoryText != null)
        {
            flagHistoryText.text = string.Join("\n", discoveredFlagMessages);
        }
    }

    private void UpdateTelemetry()
    {
        if (redFlagNotificationText == null) return;

        // Only update if no flag was just found (avoid overwriting the "FOUND:" message)
        // Telemetry is instead shown in the flagHistoryText — this method updates the sidebar summary
        string twoFA  = playerEnabled2FA   ? "<color=#00E676>ON</color>"  : "<color=#EF5350>OFF</color>";
        string qrState = playerScannedQR   ? "<color=#EF5350>COMPROMISED</color>" : "<color=#FFE082>Pending</color>";

        if (flagHistoryText != null)
        {
            string history = discoveredFlagMessages.Count > 0
                ? string.Join("\n", discoveredFlagMessages)
                : "<color=#78909C>No flags found yet.</color>";

            flagHistoryText.text =
                $"<color=#80D8FF><b>TELEMETRY — {DiscoveredRedFlagsCount}/4 flags</b></color>\n" +
                $"2FA: {twoFA}    QR: {qrState}\n" +
                "─────────────────\n" +
                history;
        }
    }
    #endregion

    // ─────────────────────────────────────────────
    //  PHASE 4: Consequence — manual Continue button
    // ─────────────────────────────────────────────
    #region Phase 4: Consequence
    public void StartPhase4Consequence(OutcomeType outcome)
    {
        pendingOutcome = outcome;
        currentPhase = Phase.Phase4_Consequence;
        SetPhase(4);

        if (fakeWebpageScreen      != null) fakeWebpageScreen.SetActive(false);
        if (telegramChatScreen     != null) telegramChatScreen.SetActive(false);
        if (decisionConfirmOverlay != null) decisionConfirmOverlay.SetActive(false);
        if (consequencePanel       != null) consequencePanel.SetActive(true);

        // Show continue button, hide it initially until audio finishes
        if (continueToDebriefButton != null)
        {
            continueToDebriefButton.gameObject.SetActive(true);
            continueToDebriefButton.interactable = false; // enabled after short delay
            StartCoroutine(EnableContinueButtonAfterDelay(2.5f));
        }

        switch (outcome)
        {
            case OutcomeType.Failure:
                if (consequenceTitleText != null)
                {
                    consequenceTitleText.text = "<b>[CRITICAL] ACCOUNT COMPROMISED!</b>";
                    consequenceTitleText.color = failureColor;
                }
                if (consequenceBodyText != null)
                {
                    consequenceBodyText.text =
                        "You scanned the QR code with Telegram's <b>'Link Desktop Device'</b>.\n\n" +
                        "<color=#EF5350><b>CRITICAL BREACH:</b></color> The attacker's server instantly cloned your Telegram session. " +
                        "Your private chats have been downloaded and the phishing link is now being forwarded to all your contacts.\n\n" +
                        "Read the attack breakdown on the left before continuing.";
                }
                if (consequenceStatusIcon   != null) consequenceStatusIcon.color = failureColor;
                if (sfxAudioSource != null && hackAlarmClip != null) sfxAudioSource.PlayOneShot(hackAlarmClip);
                if (VRInteractionHandler.Instance != null) VRInteractionHandler.Instance.PlayAlertSound();
                break;

            case OutcomeType.Partial:
                if (consequenceTitleText != null)
                {
                    consequenceTitleText.text = "<b>[PARTIAL] LINK REFUSED — BUT GAPS REMAIN</b>";
                    consequenceTitleText.color = new Color(1f, 0.7f, 0.2f);
                }
                if (consequenceBodyText != null)
                {
                    consequenceBodyText.text =
                        "You refused to scan the QR code — good instinct.\n\n" +
                        "<color=#FFB74D><b>PARTIAL DEFENSE:</b></color> However, you didn't verify the URL indicators or call Sophea to confirm the compromise. " +
                        "In a real attack, you might have been caught off-guard by the next attempt.\n\n" +
                        "Review the lesson cards to understand what you missed.";
                }
                if (consequenceStatusIcon   != null) consequenceStatusIcon.color = new Color(1f, 0.7f, 0.2f);
                if (sfxAudioSource != null && victoryFanfareClip != null) sfxAudioSource.PlayOneShot(victoryFanfareClip);
                break;

            case OutcomeType.Success:
                if (consequenceTitleText != null)
                {
                    consequenceTitleText.text = "<b>[DEFENSE] ATTACK BLOCKED!</b>";
                    consequenceTitleText.color = successColor;
                }
                if (consequenceBodyText != null)
                {
                    consequenceBodyText.text =
                        "You refused to scan the suspicious QR code!\n\n" +
                        "<color=#81C784><b>SESSION SECURED:</b></color> You recognized the session takeover attempt and blocked the unauthorized desktop pairing. " +
                        "Your account remains intact.\n\n" +
                        "For maximum protection, try calling Sophea and enabling 2FA next time.";
                }
                if (consequenceStatusIcon   != null) consequenceStatusIcon.color = successColor;
                if (sfxAudioSource != null && victoryFanfareClip != null) sfxAudioSource.PlayOneShot(victoryFanfareClip);
                if (VRInteractionHandler.Instance != null) VRInteractionHandler.Instance.PlaySuccessSound();
                break;

            case OutcomeType.Ideal:
                if (consequenceTitleText != null)
                {
                    consequenceTitleText.text = "<b>[PERFECT] FULL CYBER DEFENSE!</b>";
                    consequenceTitleText.color = idealColor;
                }
                if (consequenceBodyText != null)
                {
                    consequenceBodyText.text =
                        "Outstanding cybersecurity vigilance!\n\n" +
                        "<color=#64B5F6><b>FULL MASTERY:</b></color> You spotted all the red flags, called Sophea to verify out-of-band, " +
                        "rejected the unauthorized QR device link, and secured the account with Two-Step Verification.\n\n" +
                        "This is the gold standard response to a reverse-proxy phishing attack.";
                }
                if (consequenceStatusIcon   != null) consequenceStatusIcon.color = idealColor;
                if (sfxAudioSource != null && victoryFanfareClip != null) sfxAudioSource.PlayOneShot(victoryFanfareClip);
                if (VRInteractionHandler.Instance != null) VRInteractionHandler.Instance.PlaySuccessSound();
                break;
        }
    }

    private IEnumerator EnableContinueButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (continueToDebriefButton != null)
            continueToDebriefButton.interactable = true;
    }

    public void OnContinueToDebrief()
    {
        PlayClick();
        StartPhase5Debrief(pendingOutcome);
    }
    #endregion

    // ─────────────────────────────────────────────
    //  PHASE 5: Debrief — lesson cards + scorecard
    // ─────────────────────────────────────────────
    #region Phase 5: Debrief
    public void StartPhase5Debrief(OutcomeType outcome)
    {
        currentPhase = Phase.Phase5_Debrief;
        SetPhase(5);

        if (consequencePanel != null) consequencePanel.SetActive(false);
        if (debriefPanel     != null) debriefPanel.SetActive(true);

        activeDebriefCardIndex = 0;
        UpdateDebriefCards();

        int earnedXP = outcome switch
        {
            OutcomeType.Failure => 0,
            OutcomeType.Partial => 50,
            OutcomeType.Success => 100,
            OutcomeType.Ideal   => 200,
            _                   => 0
        };

        if (earnedXPText != null)
        {
            StartCoroutine(AnimateXPEarned(earnedXP));
        }

        // Show only the 4 actual red flags count — not call/2FA bonus actions
        if (redFlagsFoundText != null)
        {
            int realFlags = DiscoveredRedFlagsCount; // only 4 RegisterRedFlag calls
            string flagColor = realFlags >= 4 ? "#00E676" : realFlags >= 2 ? "#FFB74D" : "#EF5350";
            redFlagsFoundText.text =
                $"Red Flags: <color={flagColor}><b>{realFlags}/4</b></color>\n" +
                $"Called Friend: <b>{(playerCalledFriend ? "Yes" : "No")}</b>\n" +
                $"2FA Enabled: <b>{(playerEnabled2FA ? "Yes" : "No")}</b>";
        }

        CompleteScenario(outcome);
    }

    private IEnumerator AnimateXPEarned(int targetXP)
    {
        if (earnedXPText == null) yield break;

        string xpColor = targetXP >= 200 ? "#00E676" : targetXP >= 100 ? "#81C784" : targetXP > 0 ? "#FFB74D" : "#EF5350";
        float duration = 1.0f;
        float elapsed = 0f;

        Transform textTransform = earnedXPText.transform;
        Vector3 origScale = textTransform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int currentXP = Mathf.RoundToInt(Mathf.Lerp(0, targetXP, t));
            earnedXPText.text = $"<color={xpColor}><b>+{currentXP} XP</b></color>";
            yield return null;
        }

        earnedXPText.text = $"<color={xpColor}><b>+{targetXP} XP</b></color>";

        // Rewarding scale punch
        float punchDuration = 0.2f;
        float pElapsed = 0f;
        while (pElapsed < punchDuration)
        {
            pElapsed += Time.deltaTime;
            float punch = Mathf.Sin((pElapsed / punchDuration) * Mathf.PI) * 0.15f;
            textTransform.localScale = origScale * (1f + punch);
            yield return null;
        }
        textTransform.localScale = origScale;
    }

    private void UpdateDebriefCards()
    {
        if (cardWeaponizedTrust      != null) cardWeaponizedTrust.SetActive(activeDebriefCardIndex == 0);
        if (cardLinkDevicePurpose    != null) cardLinkDevicePurpose.SetActive(activeDebriefCardIndex == 1);
        if (cardTwoStepVerification  != null) cardTwoStepVerification.SetActive(activeDebriefCardIndex == 2);

        if (cardCounterText != null)
            cardCounterText.text = $"<b>{activeDebriefCardIndex + 1} / 3</b>";
    }

    public void ShowNextDebriefCard()
    {
        activeDebriefCardIndex = (activeDebriefCardIndex + 1) % 3;
        UpdateDebriefCards();
        PlayClick();
    }
    #endregion

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    private void SetButtonIdentified(Button btn, string newText)
    {
        if (btn == null) return;
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = newText;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = new Color(0.12f, 0.65f, 0.45f); // Glowing emerald
        btn.interactable = false; // prevent re-clicking

        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.TriggerBothHandsHaptic(0.45f, 0.12f);
            VRInteractionHandler.Instance.PlaySuccessSound();
        }
    }

    private void PlayClick()
    {
        if (VRInteractionHandler.Instance != null)
            VRInteractionHandler.Instance.PlayClickSound();
    }

    /// <summary>Reveals text one character at a time (typewriter effect).</summary>
    private IEnumerator TypewriterReveal(TextMeshProUGUI label, string fullText, float charDelay)
    {
        label.text = "";
        // We need to write plain text but TMP needs the tags intact.
        // Use TMP's maxVisibleCharacters trick for rich text safety.
        label.text = fullText;
        label.maxVisibleCharacters = 0;

        int total = label.textInfo.characterCount;
        for (int i = 0; i <= total; i++)
        {
            label.maxVisibleCharacters = i;
            yield return new WaitForSeconds(charDelay);
        }
        label.maxVisibleCharacters = int.MaxValue;
        typewriterCoroutine = null;
    }
}
