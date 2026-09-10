using System;
using System.Collections.Generic;
using UnityEngine;

namespace CADT.VR.Core
{
    [CreateAssetMenu(fileName = "New_ScenarioConfig", menuName = "CADT VR/Core/Scenario Configuration")]
    public class ScenarioDataSO : ScriptableObject
    {
        [Header("--- General Information ---")]
        [SerializeField] private string scenarioId = "SCENARIO_01";
        [SerializeField] private string scenarioTitle = "Spear Phishing & Disguised Dropper";
        [SerializeField] private string scenarioSubtitle = "Workstation Security Breach Simulation";
        [SerializeField] private string targetSceneName = "Scenario1";

        [Header("--- Categorization ---")]
        [SerializeField] private string threatType = "Social Engineering / Malicious Attachment";
        [SerializeField] private int estimatedMinutes = 5;
        [SerializeField] private int totalPossibleXP = 100;

        [Header("--- Narrative & Briefing ---")]
        [TextArea(3, 5)]
        [SerializeField] private string scenarioBriefing = "You receive an urgent message from a colleague on Telegram containing an attached salary slip archive. Analyze the threat indicators and prevent the breach.";

        [TextArea(3, 5)]
        [SerializeField] private string debriefTakeaways = "Always inspect file extensions, verify unexpected attachments out-of-band via phone, and isolate host machines upon identifying active malware.";

        [Header("--- Red Flag Indicators ---")]
        [SerializeField] private List<RedFlagSO> redFlags = new List<RedFlagSO>();

        public string ScenarioId => scenarioId;
        public string ScenarioTitle => scenarioTitle;
        public string ScenarioSubtitle => scenarioSubtitle;
        public string TargetSceneName => targetSceneName;
        public string ThreatType => threatType;
        public int EstimatedMinutes => estimatedMinutes;
        public int TotalPossibleXP => totalPossibleXP;
        public string ScenarioBriefing => scenarioBriefing;
        public string DebriefTakeaways => debriefTakeaways;
        public IReadOnlyList<RedFlagSO> RedFlags => redFlags;

        public void Initialize(string id, string title, string subtitle, string sceneName, string threat, int minutes, int xp, string briefing, string debrief, IEnumerable<RedFlagSO> flags)
        {
            scenarioId = id;
            scenarioTitle = title;
            scenarioSubtitle = subtitle;
            targetSceneName = sceneName;
            threatType = threat;
            estimatedMinutes = minutes;
            totalPossibleXP = xp;
            scenarioBriefing = briefing;
            debriefTakeaways = debrief;
            redFlags = new List<RedFlagSO>(flags ?? Array.Empty<RedFlagSO>());
        }
    }
}
