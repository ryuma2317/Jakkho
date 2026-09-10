using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SCENARIO TEMPLATE — Copy this file to create a new scenario.
///
/// Instructions:
///   1. Copy this file to Assets/Scenarios/ScenarioN/Scripts/ScenarioN_YourNameManager.cs
///   2. Rename the class to match your filename
///   3. Update scenarioNumber, scenarioTitle, scenarioDescription in ScenarioManager base
///   4. Define your 4 red flags specific to your threat type
///   5. Implement each phase's UI logic
///   6. Create a matching ScenarioNBuilder.cs in Assets/Scenarios/ScenarioN/Scripts/Editor/
///   7. Register your scenario in CarouselController.cs (Assets/_Project/UI/Scripts/)
///   8. Add your scene name to GameManager.cs (Assets/_Project/Core/Scripts/)
///
/// Architecture Rules:
///   - All scenarios follow the 5-phase flow: Immersion → Hook → Investigation → Consequence → Debrief
///   - Always inherit from ScenarioManager
///   - Use RegisterRedFlag() for tracking red flag discovery
///   - Use CompleteScenario(OutcomeType) for scoring
///   - 4 outcomes: Failure (0 XP), Partial (50 XP), Success (100 XP), Ideal (200 XP)
///   - Use Input System Package (New) — never legacy UnityEngine.Input
///   - Audio: playOnAwake=false, loop=false, no ambient drones
///   - Curved workstation: 3-panel tangent at 18° per side, height Y=1.10m
///
/// Setting: [DESCRIBE YOUR ENVIRONMENT]
/// Threat:  [DESCRIBE THE CYBERSECURITY THREAT]
///
/// Red Flags (4):
///   1. [flag_id_1] — [Description]
///   2. [flag_id_2] — [Description]
///   3. [flag_id_3] — [Description]
///   4. [flag_id_4] — [Description]
/// </summary>
public class ScenarioTemplate_Manager : ScenarioManager
{
    public enum Phase
    {
        Phase1_Immersion,     // 0: Environment settling, ambient intro
        Phase2_Hook,          // 1: Threat trigger, initial interaction
        Phase3_Investigation, // 2: Red flag inspection, key decisions
        Phase4_Consequence,   // 3: Immediate result based on player actions
        Phase5_Debrief        // 4: Educational cards, XP breakdown, return/replay
    }

    [Header("Phase State")]
    [SerializeField] private Phase currentPhase = Phase.Phase1_Immersion;

    // ─────────────────────────────────────────────
    //  Phase 1: Immersion
    // ─────────────────────────────────────────────
    [Header("Phase 1: Immersion")]
    [SerializeField] private float immersionDuration = 4f;
    [SerializeField] private GameObject immersionNotificationPrompt;
    [SerializeField] private Button skipImmersionButton;

    // ─────────────────────────────────────────────
    //  Phase 2: Hook
    //  TODO: Add your hook-specific serialized fields
    // ─────────────────────────────────────────────
    [Header("Phase 2: Hook")]
    [SerializeField] private GameObject hookScreen;

    // ─────────────────────────────────────────────
    //  Phase 3: Investigation
    //  TODO: Add your investigation-specific fields
    // ─────────────────────────────────────────────
    [Header("Phase 3: Investigation")]
    [SerializeField] private GameObject investigationScreen;
    [SerializeField] private Button inspectRedFlag1Button;
    [SerializeField] private Button inspectRedFlag2Button;
    [SerializeField] private Button inspectRedFlag3Button;
    [SerializeField] private Button inspectRedFlag4Button;
    [SerializeField] private Button badChoiceButton;       // → Failure
    [SerializeField] private Button goodChoiceButton;      // → Success
    [SerializeField] private Button idealChoiceButton;     // → Ideal
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
    [SerializeField] private GameObject card1;  // Rename to your topic
    [SerializeField] private GameObject card2;  // Rename to your topic
    [SerializeField] private GameObject card3;  // Rename to your topic
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
    [SerializeField] private AudioClip notificationClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip warningAlertClip;
    [SerializeField] private AudioClip victoryFanfareClip;

    // ─────────────────────────────────────────────
    //  Runtime State
    // ─────────────────────────────────────────────
    private bool madeGoodChoice = false;
    private bool madeIdealChoice = false;
    private bool madeBadChoice = false;
    private int debriefCardIndex = 0;
    private readonly GameObject[] debriefCards = new GameObject[3];

    protected override void Start()
    {
        base.Start();
        debriefCards[0] = card1;
        debriefCards[1] = card2;
        debriefCards[2] = card3;
        WireButtons();
        StartCoroutine(RunImmersionPhase());
    }

    private void WireButtons()
    {
        if (skipImmersionButton != null)
            skipImmersionButton.onClick.AddListener(() => { StopAllCoroutines(); TransitionToHook(); });

        // Red flag inspectors
        if (inspectRedFlag1Button != null)
            inspectRedFlag1Button.onClick.AddListener(() => OnInspectRedFlag("flag_id_1", "Description of red flag 1"));
        if (inspectRedFlag2Button != null)
            inspectRedFlag2Button.onClick.AddListener(() => OnInspectRedFlag("flag_id_2", "Description of red flag 2"));
        if (inspectRedFlag3Button != null)
            inspectRedFlag3Button.onClick.AddListener(() => OnInspectRedFlag("flag_id_3", "Description of red flag 3"));
        if (inspectRedFlag4Button != null)
            inspectRedFlag4Button.onClick.AddListener(() => OnInspectRedFlag("flag_id_4", "Description of red flag 4"));

        // Decision buttons
        if (badChoiceButton != null) badChoiceButton.onClick.AddListener(() => { madeBadChoice = true; TransitionToConsequence(); });
        if (goodChoiceButton != null) goodChoiceButton.onClick.AddListener(() => { madeGoodChoice = true; TransitionToConsequence(); });
        if (idealChoiceButton != null) idealChoiceButton.onClick.AddListener(() => { madeIdealChoice = true; TransitionToConsequence(); });

        // Debrief navigation
        if (debriefNextCardButton != null) debriefNextCardButton.onClick.AddListener(CycleDebriefCard);
        if (restartButton != null) restartButton.onClick.AddListener(RestartScenario);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    // ═══ PHASE 1 ═══
    private IEnumerator RunImmersionPhase()
    {
        currentPhase = Phase.Phase1_Immersion;
        SetPhase(0);
        ShowOnly(immersionNotificationPrompt);
        yield return new WaitForSeconds(immersionDuration);
        TransitionToHook();
    }

    // ═══ PHASE 2 ═══
    private void TransitionToHook()
    {
        currentPhase = Phase.Phase2_Hook;
        SetPhase(1);
        ShowOnly(hookScreen);
        // TODO: Implement hook logic
    }

    // ═══ PHASE 3 ═══
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

    // ═══ PHASE 4 ═══
    private void TransitionToConsequence()
    {
        currentPhase = Phase.Phase4_Consequence;
        SetPhase(3);
        ShowOnly(consequencePanel);

        OutcomeType outcome = DetermineOutcome();
        // TODO: Set consequence text based on outcome
        StartCoroutine(ShowConsequenceThenDebrief(outcome));
    }

    private OutcomeType DetermineOutcome()
    {
        if (madeBadChoice) return OutcomeType.Failure;
        if (madeIdealChoice && DiscoveredRedFlagsCount >= 3) return OutcomeType.Ideal;
        if (madeGoodChoice) return OutcomeType.Success;
        return OutcomeType.Partial;
    }

    private IEnumerator ShowConsequenceThenDebrief(OutcomeType outcome)
    {
        yield return new WaitForSeconds(consequenceDisplayDuration);
        CompleteScenario(outcome);
        TransitionToDebrief();
    }

    // ═══ PHASE 5 ═══
    private void TransitionToDebrief()
    {
        currentPhase = Phase.Phase5_Debrief;
        SetPhase(4);
        ShowOnly(debriefPanel);
        debriefCardIndex = 0;
        UpdateDebriefCard();

        int xp = DetermineOutcome() switch
        {
            OutcomeType.Failure => 0, OutcomeType.Partial => 50,
            OutcomeType.Success => 100, OutcomeType.Ideal => 200, _ => 0
        };
        if (earnedXPText != null) earnedXPText.text = $"+{xp} XP";
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
            if (debriefCards[i] != null) debriefCards[i].SetActive(i == debriefCardIndex);
    }

    private void ShowOnly(GameObject target)
    {
        GameObject[] allPanels = { immersionNotificationPrompt, hookScreen, investigationScreen, consequencePanel, debriefPanel };
        foreach (var p in allPanels)
            if (p != null) p.SetActive(p == target);
    }
}
