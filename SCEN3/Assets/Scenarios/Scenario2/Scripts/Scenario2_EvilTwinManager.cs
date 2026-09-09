using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Scenario 2: Evil Twin at the Cafe
/// Setting: Public Coffee Shop
/// Threat: Wi-Fi Man-in-the-Middle
///
/// The player sits down in a cafe and needs to connect to Wi-Fi for work.
/// Multiple networks appear — including an "Evil Twin" access point mimicking
/// the cafe's legitimate network. They must identify rogue networks, detect
/// SSL stripping, and avoid entering credentials on intercepted connections.
///
/// Red Flags (4):
///   1. duplicate_ssid     — Two networks with identical/similar cafe name
///   2. no_password        — Fake network has no WPA security
///   3. ssl_stripping      — Login page shows HTTP instead of HTTPS
///   4. certificate_error  — Browser certificate warning on banking site
/// </summary>
public class Scenario2_EvilTwinManager : ScenarioManager
{
    public enum Phase
    {
        Phase1_Immersion,     // 0: Arrive at cafe, settle in, ambient cafe sounds
        Phase2_Hook,          // 1: Need to connect to Wi-Fi, network list appears
        Phase3_Investigation, // 2: Inspect networks, check certificates, detect MITM
        Phase4_Consequence,   // 3: Immediate result based on player actions
        Phase5_Debrief        // 4: Educational cards, XP breakdown, return/replay
    }

    [Header("Phase State")]
    [SerializeField] private Phase currentPhase = Phase.Phase1_Immersion;

    // ─────────────────────────────────────────────
    //  Phase 1: Immersion — Cafe Environment
    // ─────────────────────────────────────────────
    [Header("Phase 1: Immersion")]
    [SerializeField] private float immersionDuration = 4f;
    [SerializeField] private GameObject immersionNotificationPrompt;
    [SerializeField] private Button skipImmersionButton;

    // ─────────────────────────────────────────────
    //  Phase 2: Hook — Wi-Fi Network Selection
    // ─────────────────────────────────────────────
    [Header("Phase 2: Hook / Wi-Fi Selection UI")]
    [SerializeField] private GameObject wifiSelectionScreen;
    [SerializeField] private GameObject networkListPanel;
    [SerializeField] private Button legitimateNetworkButton;    // Correct choice
    [SerializeField] private Button evilTwinNetworkButton;      // Trap — no password
    [SerializeField] private Button otherNetwork1Button;        // Decoy
    [SerializeField] private Button otherNetwork2Button;        // Decoy
    [SerializeField] private TextMeshProUGUI legitimateSSIDText;
    [SerializeField] private TextMeshProUGUI evilTwinSSIDText;
    [SerializeField] private GameObject wifiSignalStrengthIcons;
    [SerializeField] private GameObject padlockIconLegitimate;  // Shows WPA2
    [SerializeField] private GameObject noPadlockIconEvilTwin;  // No security

    // ─────────────────────────────────────────────
    //  Phase 3: Investigation — Browser & MITM
    // ─────────────────────────────────────────────
    [Header("Phase 3: Investigation / Browser")]
    [SerializeField] private GameObject browserScreen;
    [SerializeField] private TextMeshProUGUI urlAddressBarText;
    [SerializeField] private GameObject httpWarningBadge;        // SSL stripped
    [SerializeField] private GameObject certificateErrorPanel;   // Cert mismatch
    [SerializeField] private GameObject captivePortalPanel;      // Fake login page
    [SerializeField] private Button inspectSSIDButton;          // Red flag 1: duplicate_ssid
    [SerializeField] private Button inspectSecurityButton;      // Red flag 2: no_password
    [SerializeField] private Button inspectSSLButton;           // Red flag 3: ssl_stripping
    [SerializeField] private Button inspectCertButton;          // Red flag 4: certificate_error
    [SerializeField] private Button enterCredentialsButton;     // Bad choice → Failure
    [SerializeField] private Button disconnectButton;           // Good choice → Success
    [SerializeField] private Button enableVPNButton;            // Ideal action
    [SerializeField] private TextMeshProUGUI redFlagNotificationText;

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
    [SerializeField] private GameObject cardEvilTwinAttack;
    [SerializeField] private GameObject cardSSLImportance;
    [SerializeField] private GameObject cardVPNProtection;
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
    [SerializeField] private AudioClip wifiConnectClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip warningAlertClip;
    [SerializeField] private AudioClip victoryFanfareClip;

    // ─────────────────────────────────────────────
    //  Runtime State
    // ─────────────────────────────────────────────
    private bool connectedToEvilTwin = false;
    private bool enteredCredentials = false;
    private bool enabledVPN = false;
    private bool disconnectedSafely = false;
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
        debriefCards[0] = cardEvilTwinAttack;
        debriefCards[1] = cardSSLImportance;
        debriefCards[2] = cardVPNProtection;
    }

    private void WireButtons()
    {
        // Phase 1
        if (skipImmersionButton != null)
            skipImmersionButton.onClick.AddListener(() => { StopAllCoroutines(); TransitionToHook(); });

        // Phase 2 — Network selection
        if (legitimateNetworkButton != null)
            legitimateNetworkButton.onClick.AddListener(OnConnectLegitimate);
        if (evilTwinNetworkButton != null)
            evilTwinNetworkButton.onClick.AddListener(OnConnectEvilTwin);

        // Phase 3 — Red flag inspectors
        if (inspectSSIDButton != null)
            inspectSSIDButton.onClick.AddListener(() => OnInspectRedFlag("duplicate_ssid", "Two networks share the same cafe name"));
        if (inspectSecurityButton != null)
            inspectSecurityButton.onClick.AddListener(() => OnInspectRedFlag("no_password", "Network has no WPA/WPA2 security"));
        if (inspectSSLButton != null)
            inspectSSLButton.onClick.AddListener(() => OnInspectRedFlag("ssl_stripping", "Banking page loaded over HTTP, not HTTPS"));
        if (inspectCertButton != null)
            inspectCertButton.onClick.AddListener(() => OnInspectRedFlag("certificate_error", "SSL certificate doesn't match the domain"));

        // Phase 3 — Decision buttons
        if (enterCredentialsButton != null)
            enterCredentialsButton.onClick.AddListener(OnEnterCredentials);
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(OnDisconnect);
        if (enableVPNButton != null)
            enableVPNButton.onClick.AddListener(OnEnableVPN);

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
    //  PHASE 2: Hook — Wi-Fi Network List
    // ═══════════════════════════════════════════════
    private void TransitionToHook()
    {
        currentPhase = Phase.Phase2_Hook;
        SetPhase(1);
        ShowOnly(wifiSelectionScreen);

        // TODO: Animate network list appearing one-by-one
        // TODO: Show signal strength indicators
        // TODO: Highlight padlock icons for secured vs. unsecured networks
    }

    private void OnConnectLegitimate()
    {
        connectedToEvilTwin = false;
        // TODO: Show password prompt for WPA2 network
        // TODO: Connect and proceed to safe browsing
        TransitionToInvestigation();
    }

    private void OnConnectEvilTwin()
    {
        connectedToEvilTwin = true;
        if (sfxAudioSource != null && wifiConnectClip != null)
            sfxAudioSource.PlayOneShot(wifiConnectClip);

        // TODO: Instant connection (no password required — red flag)
        TransitionToInvestigation();
    }

    // ═══════════════════════════════════════════════
    //  PHASE 3: Investigation
    // ═══════════════════════════════════════════════
    private void TransitionToInvestigation()
    {
        currentPhase = Phase.Phase3_Investigation;
        SetPhase(2);
        ShowOnly(browserScreen);

        if (connectedToEvilTwin)
        {
            // TODO: Show HTTP (stripped SSL) banking page
            // TODO: Display certificate error
            // TODO: Show captive portal login form
            if (httpWarningBadge != null) httpWarningBadge.SetActive(true);
        }
        else
        {
            // TODO: Show normal HTTPS banking page
            if (httpWarningBadge != null) httpWarningBadge.SetActive(false);
        }
    }

    private void OnInspectRedFlag(string flagId, string description)
    {
        if (RegisterRedFlag(flagId, description))
        {
            if (sfxAudioSource != null && warningAlertClip != null)
                sfxAudioSource.PlayOneShot(warningAlertClip);

            if (redFlagNotificationText != null)
                redFlagNotificationText.text = $"Red Flag [{DiscoveredRedFlagsCount}/{TotalRedFlags}]: {description}";
        }
    }

    // ═══════════════════════════════════════════════
    //  PHASE 3 → 4: Decision Actions
    // ═══════════════════════════════════════════════
    private void OnEnterCredentials()
    {
        enteredCredentials = true;
        TransitionToConsequence();
    }

    private void OnDisconnect()
    {
        disconnectedSafely = true;
        TransitionToConsequence();
    }

    private void OnEnableVPN()
    {
        enabledVPN = true;
        disconnectedSafely = true;
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
                    consequenceTitleText.text = "CREDENTIALS INTERCEPTED";
                    consequenceBodyText.text = "You entered your banking credentials over an evil twin network. The attacker has captured your username, password, and session cookies via SSL stripping.";
                    if (consequenceStatusIcon != null) consequenceStatusIcon.color = failureColor;
                    break;
                case OutcomeType.Partial:
                    consequenceTitleText.text = "DISCONNECTED — PARTIALLY SAFE";
                    consequenceBodyText.text = "You disconnected from the suspicious network but didn't investigate or use a VPN. You avoided the attack this time, but other users remain at risk.";
                    if (consequenceStatusIcon != null) consequenceStatusIcon.color = successColor;
                    break;
                case OutcomeType.Success:
                    consequenceTitleText.text = "ATTACK THWARTED";
                    consequenceBodyText.text = "You identified the rogue network and disconnected. Your data remains secure, and you reported the evil twin to cafe staff.";
                    if (consequenceStatusIcon != null) consequenceStatusIcon.color = successColor;
                    break;
                case OutcomeType.Ideal:
                    consequenceTitleText.text = "PERFECT DEFENSE";
                    consequenceBodyText.text = "You identified all red flags, enabled VPN encryption, and reported the evil twin. Other customers were warned and the rogue AP was shut down.";
                    if (consequenceStatusIcon != null) consequenceStatusIcon.color = idealColor;
                    break;
            }
        }

        StartCoroutine(ShowConsequenceThenDebrief(outcome));
    }

    private OutcomeType DetermineOutcome()
    {
        if (connectedToEvilTwin && enteredCredentials) return OutcomeType.Failure;
        if (enabledVPN && DiscoveredRedFlagsCount >= 3) return OutcomeType.Ideal;
        if (disconnectedSafely && DiscoveredRedFlagsCount >= 2) return OutcomeType.Success;
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
            wifiSelectionScreen,
            browserScreen,
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
