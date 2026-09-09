using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Scenario 1: Office Spear Phishing
/// Setting: Corporate Office
/// Threat: Email Phishing & Malware
///
/// The player receives a convincing spear-phishing email appearing to come
/// from their CEO/IT department. They must inspect the sender domain, verify
/// attachments, and avoid clicking malicious links in a corporate environment.
///
/// Red Flags (4):
///   1. spoofed_sender   — Sender domain doesn't match company domain
///   2. urgent_pressure  — Artificial urgency ("immediate action required")
///   3. suspicious_attachment — .exe/.scr disguised as .pdf
///   4. mismatched_link  — Hover reveals different URL than displayed text
/// </summary>
public class Scenario1_SpearPhishingManager : ScenarioManager
{
    public enum Phase
    {
        Phase1_Immersion,     // 0: Settle into corporate office desk, ambient sounds
        Phase2_Hook,          // 1: Email notification arrives, inbox opens
        Phase3_Investigation, // 2: Inspect email details, attachments, links
        Phase4_Consequence,   // 3: Immediate result based on player actions
        Phase5_Debrief        // 4: Educational cards, XP breakdown, return/replay
    }

    [Header("Phase State")]
    [SerializeField] private Phase currentPhase = Phase.Phase1_Immersion;

    // ─────────────────────────────────────────────
    //  Phase 1: Immersion — Corporate Office
    // ─────────────────────────────────────────────
    [Header("Phase 1: Immersion")]
    [SerializeField] private float immersionDuration = 4f;
    [SerializeField] private GameObject immersionNotificationPrompt;
    [SerializeField] private Button skipImmersionButton;

    // ─────────────────────────────────────────────
    //  Phase 2: Hook — Email Arrives
    // ─────────────────────────────────────────────
    [Header("Phase 2: Hook / Email Client UI")]
    [SerializeField] private GameObject emailClientScreen;
    [SerializeField] private GameObject inboxListPanel;
    [SerializeField] private GameObject emailDetailPanel;
    [SerializeField] private Button phishingEmailButton;       // The malicious email in inbox
    [SerializeField] private TextMeshProUGUI senderNameText;
    [SerializeField] private TextMeshProUGUI senderAddressText;
    [SerializeField] private TextMeshProUGUI subjectLineText;
    [SerializeField] private TextMeshProUGUI emailBodyText;
    [SerializeField] private TextMeshProUGUI attachmentNameText;

    // ─────────────────────────────────────────────
    //  Phase 3: Investigation — Inspect Red Flags
    // ─────────────────────────────────────────────
    [Header("Phase 3: Investigation")]
    [SerializeField] private Button inspectSenderButton;       // Red flag 1: spoofed_sender
    [SerializeField] private Button inspectUrgencyButton;      // Red flag 2: urgent_pressure
    [SerializeField] private Button inspectAttachmentButton;   // Red flag 3: suspicious_attachment
    [SerializeField] private Button inspectLinkButton;         // Red flag 4: mismatched_link
    [SerializeField] private Button openAttachmentButton;      // Bad choice → Failure
    [SerializeField] private Button reportPhishingButton;      // Good choice → Success
    [SerializeField] private Button verifyWithITButton;        // Ideal action
    [SerializeField] private TextMeshProUGUI redFlagNotificationText;
    [SerializeField] private GameObject senderDetailPopup;
    [SerializeField] private GameObject attachmentWarningPopup;
    [SerializeField] private GameObject linkPreviewPopup;

    // ─────────────────────────────────────────────
    //  Phase 4: Consequence
    // ─────────────────────────────────────────────
    [Header("Phase 4: Consequence")]
    [SerializeField] private GameObject consequencePanel;
    [SerializeField] private TextMeshProUGUI consequenceTitleText;
    [SerializeField] private TextMeshProUGUI consequenceBodyText;
    [SerializeField] private Image consequenceStatusIcon;
    [SerializeField] private Color failureColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.4f);
    [SerializeField] private Color idealColor = new Color(0.3f, 0.6f, 1.0f);
    [SerializeField] private float consequenceDisplayDuration = 4.5f;

    // ─────────────────────────────────────────────
    //  Phase 5: Debrief
    // ─────────────────────────────────────────────
    [Header("Phase 5: Debrief")]
    [SerializeField] private GameObject debriefPanel;
    [SerializeField] private GameObject cardSpoofedSender;
    [SerializeField] private GameObject cardSocialEngineering;
    [SerializeField] private GameObject cardMalwareAttachments;
    [SerializeField] private TextMeshProUGUI earnedXPText;
    [SerializeField] private TextMeshProUGUI redFlagsFoundText;
    [SerializeField] private Button debriefNextCardButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    // ─────────────────────────────────────────────
    //  Audio
    // ─────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip emailNotificationClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip warningAlertClip;
    [SerializeField] private AudioClip victoryFanfareClip;

    // ─────────────────────────────────────────────
    //  Runtime State
    // ─────────────────────────────────────────────
    private bool hasCalledIT = false;
    private bool hasReportedPhishing = false;
    private bool hasOpenedAttachment = false;
    private int debriefCardIndex = 0;
    private readonly GameObject[] debriefCards = new GameObject[3];

    protected override void Start()
    {
        base.Start();
        InitializeDebriefCards();
        WireButtons();
        StartCoroutine(RunImmersionPhase());
    }

    private void InitializeDebriefCards()
    {
        debriefCards[0] = cardSpoofedSender;
        debriefCards[1] = cardSocialEngineering;
        debriefCards[2] = cardMalwareAttachments;
    }

    private void WireButtons()
    {
        // Phase 1
        if (skipImmersionButton != null)
            skipImmersionButton.onClick.AddListener(() => StopAllCoroutines());

        // Phase 2
        if (phishingEmailButton != null)
            phishingEmailButton.onClick.AddListener(OnEmailClicked);

        // Phase 3 — Red flag inspectors
        if (inspectSenderButton != null)
            inspectSenderButton.onClick.AddListener(() => OnInspectRedFlag("spoofed_sender", "Sender domain doesn't match company domain"));
        if (inspectUrgencyButton != null)
            inspectUrgencyButton.onClick.AddListener(() => OnInspectRedFlag("urgent_pressure", "Artificial urgency language detected"));
        if (inspectAttachmentButton != null)
            inspectAttachmentButton.onClick.AddListener(() => OnInspectRedFlag("suspicious_attachment", "File extension disguised (.pdf.exe)"));
        if (inspectLinkButton != null)
            inspectLinkButton.onClick.AddListener(() => OnInspectRedFlag("mismatched_link", "Hover URL doesn't match display text"));

        // Phase 3 — Decision buttons
        if (openAttachmentButton != null)
            openAttachmentButton.onClick.AddListener(OnOpenAttachment);
        if (reportPhishingButton != null)
            reportPhishingButton.onClick.AddListener(OnReportPhishing);
        if (verifyWithITButton != null)
            verifyWithITButton.onClick.AddListener(OnVerifyWithIT);

        // Phase 5
        if (debriefNextCardButton != null)
            debriefNextCardButton.onClick.AddListener(CycleDebriefCard);
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartScenario);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    // ═══════════════════════════════════════════════
    //  PHASE 1: Immersion
    // ═══════════════════════════════════════════════
    private IEnumerator RunImmersionPhase()
    {
        currentPhase = Phase.Phase1_Immersion;
        SetPhase(0);
        ShowOnly(immersionNotificationPrompt);

        yield return new WaitForSeconds(immersionDuration);
        TransitionToHook();
    }

    // ═══════════════════════════════════════════════
    //  PHASE 2: Hook — Email Notification
    // ═══════════════════════════════════════════════
    private void TransitionToHook()
    {
        currentPhase = Phase.Phase2_Hook;
        SetPhase(1);

        if (sfxAudioSource != null && emailNotificationClip != null)
            sfxAudioSource.PlayOneShot(emailNotificationClip);

        // TODO: Trigger VR haptics via VRInteractionHandler
        // TODO: Show email notification on workstation monitor
        // TODO: Populate inbox with phishing email + legitimate emails

        ShowOnly(emailClientScreen);
        if (inboxListPanel != null) inboxListPanel.SetActive(true);
        if (emailDetailPanel != null) emailDetailPanel.SetActive(false);
    }

    private void OnEmailClicked()
    {
        if (inboxListPanel != null) inboxListPanel.SetActive(false);
        if (emailDetailPanel != null) emailDetailPanel.SetActive(true);

        // TODO: Populate email detail view with phishing content
        // TODO: Show sender, subject, body, attachment
    }

    // ═══════════════════════════════════════════════
    //  PHASE 3: Investigation
    // ═══════════════════════════════════════════════
    private void OnInspectRedFlag(string flagId, string description)
    {
        if (RegisterRedFlag(flagId, description))
        {
            if (sfxAudioSource != null && warningAlertClip != null)
                sfxAudioSource.PlayOneShot(warningAlertClip);

            if (redFlagNotificationText != null)
                redFlagNotificationText.text = $"Red Flag [{DiscoveredRedFlagsCount}/{TotalRedFlags}]: {description}";

            // TODO: Trigger haptic feedback
            // TODO: Show visual highlight on the inspected element
        }
    }

    // ═══════════════════════════════════════════════
    //  PHASE 3 → 4: Decision Actions
    // ═══════════════════════════════════════════════
    private void OnOpenAttachment()
    {
        hasOpenedAttachment = true;
        TransitionToConsequence();
    }

    private void OnReportPhishing()
    {
        hasReportedPhishing = true;
        TransitionToConsequence();
    }

    private void OnVerifyWithIT()
    {
        hasCalledIT = true;
        hasReportedPhishing = true;
        // TODO: Show IT verification dialogue
        TransitionToConsequence();
    }

    // ═══════════════════════════════════════════════
    //  PHASE 4: Consequence
    // ═══════════════════════════════════════════════
    private void TransitionToConsequence()
    {
        currentPhase = Phase.Phase4_Consequence;
        SetPhase(3);
        ShowOnly(consequencePanel);

        OutcomeType outcome = DetermineOutcome();

        if (consequenceTitleText != null && consequenceBodyText != null)
        {
            switch (outcome)
            {
                case OutcomeType.Failure:
                    consequenceTitleText.text = "SYSTEM COMPROMISED";
                    consequenceBodyText.text = "You opened a malicious attachment. Ransomware has encrypted company files and credentials have been exfiltrated to an attacker-controlled server.";
                    if (consequenceStatusIcon != null) consequenceStatusIcon.color = failureColor;
                    break;
                case OutcomeType.Partial:
                    consequenceBodyText.text = "You avoided the attachment but didn't report the phishing attempt. Other employees may still fall victim.";
                    consequenceTitleText.text = "ATTACK AVOIDED — PARTIALLY";
                    if (consequenceStatusIcon != null) consequenceStatusIcon.color = successColor;
                    break;
                case OutcomeType.Success:
                    consequenceTitleText.text = "PHISHING REPORTED";
                    consequenceBodyText.text = "You reported the phishing email to IT. The domain has been blocklisted and a company-wide alert has been issued.";
                    if (consequenceStatusIcon != null) consequenceStatusIcon.color = successColor;
                    break;
                case OutcomeType.Ideal:
                    consequenceTitleText.text = "PERFECT DEFENSE";
                    consequenceBodyText.text = "You identified all red flags, verified with IT, and reported the phishing attempt. The attacker's infrastructure has been taken down.";
                    if (consequenceStatusIcon != null) consequenceStatusIcon.color = idealColor;
                    break;
            }
        }

        StartCoroutine(ShowConsequenceThenDebrief(outcome));
    }

    private OutcomeType DetermineOutcome()
    {
        if (hasOpenedAttachment) return OutcomeType.Failure;
        if (hasCalledIT && DiscoveredRedFlagsCount >= 3) return OutcomeType.Ideal;
        if (hasReportedPhishing) return OutcomeType.Success;
        return OutcomeType.Partial;
    }

    private IEnumerator ShowConsequenceThenDebrief(OutcomeType outcome)
    {
        yield return new WaitForSeconds(consequenceDisplayDuration);
        CompleteScenario(outcome);
        TransitionToDebrief();
    }

    // ═══════════════════════════════════════════════
    //  PHASE 5: Debrief
    // ═══════════════════════════════════════════════
    private void TransitionToDebrief()
    {
        currentPhase = Phase.Phase5_Debrief;
        SetPhase(4);
        ShowOnly(debriefPanel);

        debriefCardIndex = 0;
        UpdateDebriefCard();

        if (earnedXPText != null)
        {
            int xp = DetermineOutcome() switch
            {
                OutcomeType.Failure => 0,
                OutcomeType.Partial => 50,
                OutcomeType.Success => 100,
                OutcomeType.Ideal => 200,
                _ => 0
            };
            earnedXPText.text = $"+{xp} XP";
        }

        if (redFlagsFoundText != null)
            redFlagsFoundText.text = $"Red Flags: {DiscoveredRedFlagsCount} / {TotalRedFlags}";
    }

    private void CycleDebriefCard()
    {
        debriefCardIndex = (debriefCardIndex + 1) % debriefCards.Length;
        UpdateDebriefCard();
    }

    private void UpdateDebriefCard()
    {
        for (int i = 0; i < debriefCards.Length; i++)
        {
            if (debriefCards[i] != null)
                debriefCards[i].SetActive(i == debriefCardIndex);
        }
    }

    // ═══════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════
    private void ShowOnly(GameObject target)
    {
        GameObject[] allPanels = {
            immersionNotificationPrompt,
            emailClientScreen,
            consequencePanel,
            debriefPanel
        };

        foreach (var panel in allPanels)
        {
            if (panel != null)
                panel.SetActive(panel == target);
        }
    }
}
