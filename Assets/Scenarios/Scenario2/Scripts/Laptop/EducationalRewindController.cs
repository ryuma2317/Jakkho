using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kannko.Laptop
{
    /// <summary>
    /// Manages the continuous 6-scene Educational Rewind progression (Scenes 7-12) in Scenario 2.
    /// Scene 7: Rewind to Wi-Fi
    /// Scene 8: Inspect Wi-Fi Details
    /// Scene 9: Rewind to Login Page
    /// Scene 10: Interactive Red-Flag Challenge (4 targets)
    /// Scene 11: Safe Decision Branching (Continue vs Stop & Verify)
    /// Scene 12: Final Comparison (Before vs After)
    /// </summary>
    public class EducationalRewindController : MonoBehaviour
    {
        [Header("Scene Panels (7 - 12)")]
        [SerializeField] private GameObject m_Scene7_RewindToWifi;
        [SerializeField] private GameObject m_Scene8_InspectWifiDetails;
        [SerializeField] private GameObject m_Scene9_RewindToLoginPage;
        [SerializeField] private GameObject m_Scene10_InteractiveChallenge;
        [SerializeField] private GameObject m_Scene11_SafeDecision;
        [SerializeField] private GameObject m_Scene12_FinalComparison;

        [Header("Navigation Buttons")]
        [SerializeField] private Button m_Scene7_InspectNetworkBtn;
        [SerializeField] private Button m_Scene8_InspectLoginBtn;
        [SerializeField] private Button m_Scene9_TestYourselfBtn;
        [SerializeField] private Button m_Scene10_MakeSafeDecisionBtn;
        [SerializeField] private Button m_Scene11_ContinueBtn;
        [SerializeField] private Button m_Scene11_StopVerifyBtn;
        [SerializeField] private Button m_Scene11_TryAgainBtn;
        [SerializeField] private Button m_Scene11_SeeBeforeAfterBtn;
        [SerializeField] private Button m_Scene12_FinishReturnBtn;
        [SerializeField] private Button m_Scene12_ReplaySimulationBtn;

        [Header("Scene 10 Challenge Targets & Texts")]
        [SerializeField] private Button m_TargetPasswordBtn;
        [SerializeField] private Button m_TargetDomainBtn;
        [SerializeField] private Button m_TargetWarningBtn;
        [SerializeField] private Button m_TargetOpenWifiBtn;
        [SerializeField] private TextMeshProUGUI m_ChallengeCounterText;
        [SerializeField] private TextMeshProUGUI m_ChallengeFeedbackText;

        [Header("Scene 11 Branching Cards")]
        [SerializeField] private GameObject m_Scene11_ChoicesCard;
        [SerializeField] private GameObject m_Scene11_AttackerWarningCard;
        [SerializeField] private GameObject m_Scene11_AttackAvoidedCard;

        [Header("Browser Chrome Context")]
        [SerializeField] private TextMeshProUGUI m_UrlText;
        [SerializeField] private TextMeshProUGUI m_TabTitleText;

        [Header("Settings")]
        [SerializeField] private float m_ClickDebounceTime = 0.25f;

        public int CurrentScene { get; private set; } = 7;

        // Challenge state tracking
        private readonly HashSet<int> m_FoundTargets = new HashSet<int>();
        private float m_LastClickTime;
        private CaptivePortalController m_CaptivePortalController;

        private void Awake()
        {
            FindReferencesIfNull();
        }

        private void Start()
        {
            FindReferencesIfNull();
            SetupButtonListeners();
            EnsureCollidersAndInteractables();
        }

        private void OnEnable()
        {
            SetupButtonListeners();
        }

        private void OnDisable()
        {
            RemoveButtonListeners();
        }

        public void FindReferencesIfNull()
        {
            if (m_CaptivePortalController == null)
            {
                m_CaptivePortalController = GetComponent<CaptivePortalController>();
                if (m_CaptivePortalController == null)
                {
                    m_CaptivePortalController = GetComponentInParent<CaptivePortalController>();
                }
                if (m_CaptivePortalController == null)
                {
                    m_CaptivePortalController = UnityEngine.Object.FindFirstObjectByType<CaptivePortalController>(FindObjectsInactive.Include);
                }
            }

            Transform panelRoot = transform.Find("PageContent/EducationalRewindPanel");
            if (panelRoot == null) panelRoot = transform;

            if (panelRoot != null)
            {
                if (m_Scene7_RewindToWifi == null)
                {
                    var t = panelRoot.Find("Scene7_RewindToWifi");
                    if (t != null) m_Scene7_RewindToWifi = t.gameObject;
                }
                if (m_Scene8_InspectWifiDetails == null)
                {
                    var t = panelRoot.Find("Scene8_InspectWifiDetails");
                    if (t != null) m_Scene8_InspectWifiDetails = t.gameObject;
                }
                if (m_Scene9_RewindToLoginPage == null)
                {
                    var t = panelRoot.Find("Scene9_RewindToLoginPage");
                    if (t != null) m_Scene9_RewindToLoginPage = t.gameObject;
                }
                if (m_Scene10_InteractiveChallenge == null)
                {
                    var t = panelRoot.Find("Scene10_InteractiveChallenge");
                    if (t != null) m_Scene10_InteractiveChallenge = t.gameObject;
                }
                if (m_Scene11_SafeDecision == null)
                {
                    var t = panelRoot.Find("Scene11_SafeDecision");
                    if (t != null) m_Scene11_SafeDecision = t.gameObject;
                }
                if (m_Scene12_FinalComparison == null)
                {
                    var t = panelRoot.Find("Scene12_FinalComparison");
                    if (t != null) m_Scene12_FinalComparison = t.gameObject;
                }
            }

            // Chrome
            Transform browserHeader = transform.Find("BrowserHeader");
            if (browserHeader == null && transform.parent != null)
            {
                browserHeader = transform.parent.parent?.Find("BrowserHeader");
                if (browserHeader == null) browserHeader = transform.parent?.Find("BrowserHeader");
            }
            if (browserHeader == null)
            {
                var lbp = GameObject.Find("LaptopBrowserPanel");
                if (lbp != null) browserHeader = lbp.transform.Find("BrowserHeader");
            }

            if (m_UrlText == null && browserHeader != null)
            {
                var t = browserHeader.Find("NavBar/AddressBar/UrlText");
                if (t != null) m_UrlText = t.GetComponent<TextMeshProUGUI>();
            }
            if (m_TabTitleText == null && browserHeader != null)
            {
                var t = browserHeader.Find("TabBar/ActiveTab/TabTitle");
                if (t != null) m_TabTitleText = t.GetComponent<TextMeshProUGUI>();
            }

            // Scene 7 buttons
            if (m_Scene7_RewindToWifi != null && m_Scene7_InspectNetworkBtn == null)
            {
                var b = m_Scene7_RewindToWifi.transform.Find("InspectNetworkButton");
                if (b == null) b = m_Scene7_RewindToWifi.transform.Find("Scene7_InspectWifiBtn");
                if (b != null) m_Scene7_InspectNetworkBtn = b.GetComponent<Button>();
            }

            // Scene 8 buttons
            if (m_Scene8_InspectWifiDetails != null && m_Scene8_InspectLoginBtn == null)
            {
                var b = m_Scene8_InspectWifiDetails.transform.Find("InspectLoginButton");
                if (b == null) b = m_Scene8_InspectWifiDetails.transform.Find("Scene8_InspectLoginBtn");
                if (b != null) m_Scene8_InspectLoginBtn = b.GetComponent<Button>();
            }

            // Scene 9 buttons
            if (m_Scene9_RewindToLoginPage != null && m_Scene9_TestYourselfBtn == null)
            {
                var b = m_Scene9_RewindToLoginPage.transform.Find("TestYourselfButton");
                if (b == null) b = m_Scene9_RewindToLoginPage.transform.Find("Scene9_TestYourselfBtn");
                if (b != null) m_Scene9_TestYourselfBtn = b.GetComponent<Button>();
            }

            // Scene 10 controls
            if (m_Scene10_InteractiveChallenge != null)
            {
                if (m_Scene10_MakeSafeDecisionBtn == null)
                {
                    var b = m_Scene10_InteractiveChallenge.transform.Find("MakeSafeDecisionButton");
                    if (b == null) b = m_Scene10_InteractiveChallenge.transform.Find("Scene10_ProceedBtn");
                    if (b != null) m_Scene10_MakeSafeDecisionBtn = b.GetComponent<Button>();
                }
                if (m_ChallengeCounterText == null)
                {
                    var t = m_Scene10_InteractiveChallenge.transform.Find("CounterBadge/CounterText");
                    if (t == null) t = m_Scene10_InteractiveChallenge.transform.Find("CounterText");
                    if (t != null) m_ChallengeCounterText = t.GetComponent<TextMeshProUGUI>();
                }
                if (m_ChallengeFeedbackText == null)
                {
                    var t = m_Scene10_InteractiveChallenge.transform.Find("FeedbackCard/FeedbackText");
                    if (t == null) t = m_Scene10_InteractiveChallenge.transform.Find("FeedbackBox/FeedbackText");
                    if (t != null) m_ChallengeFeedbackText = t.GetComponent<TextMeshProUGUI>();
                }

                var targets = m_Scene10_InteractiveChallenge.transform.Find("SimulatedPortalView");
                if (targets == null) targets = m_Scene10_InteractiveChallenge.transform.Find("ChallengePortalView");
                if (targets != null)
                {
                    if (m_TargetPasswordBtn == null)
                    {
                        var b = targets.Find("Target_Password");
                        if (b != null) m_TargetPasswordBtn = b.GetComponent<Button>();
                    }
                    if (m_TargetDomainBtn == null)
                    {
                        var b = targets.Find("Target_Domain");
                        if (b != null) m_TargetDomainBtn = b.GetComponent<Button>();
                    }
                    if (m_TargetWarningBtn == null)
                    {
                        var b = targets.Find("Target_Warning");
                        if (b != null) m_TargetWarningBtn = b.GetComponent<Button>();
                    }
                    if (m_TargetOpenWifiBtn == null)
                    {
                        var b = targets.Find("Target_OpenWifi");
                        if (b != null) m_TargetOpenWifiBtn = b.GetComponent<Button>();
                    }
                }
            }

            // Scene 11 controls
            if (m_Scene11_SafeDecision != null)
            {
                if (m_Scene11_ChoicesCard == null)
                {
                    var c = m_Scene11_SafeDecision.transform.Find("ChoicesCard");
                    if (c != null) m_Scene11_ChoicesCard = c.gameObject;
                }
                if (m_Scene11_AttackerWarningCard == null)
                {
                    var c = m_Scene11_SafeDecision.transform.Find("AttackerWarningCard");
                    if (c != null) m_Scene11_AttackerWarningCard = c.gameObject;
                }
                if (m_Scene11_AttackAvoidedCard == null)
                {
                    var c = m_Scene11_SafeDecision.transform.Find("AttackAvoidedCard");
                    if (c != null) m_Scene11_AttackAvoidedCard = c.gameObject;
                }

                if (m_Scene11_ChoicesCard != null)
                {
                    if (m_Scene11_ContinueBtn == null)
                    {
                        var b = m_Scene11_ChoicesCard.transform.Find("ContinueButton");
                        if (b == null) b = m_Scene11_ChoicesCard.transform.Find("Scene11_ContinueBtn");
                        if (b != null) m_Scene11_ContinueBtn = b.GetComponent<Button>();
                    }
                    if (m_Scene11_StopVerifyBtn == null)
                    {
                        var b = m_Scene11_ChoicesCard.transform.Find("StopVerifyButton");
                        if (b == null) b = m_Scene11_ChoicesCard.transform.Find("Scene11_StopVerifyBtn");
                        if (b != null) m_Scene11_StopVerifyBtn = b.GetComponent<Button>();
                    }
                }

                if (m_Scene11_AttackerWarningCard != null && m_Scene11_TryAgainBtn == null)
                {
                    var b = m_Scene11_AttackerWarningCard.transform.Find("TryAgainButton");
                    if (b == null) b = m_Scene11_AttackerWarningCard.transform.Find("Scene11_TryAgainBtn");
                    if (b != null) m_Scene11_TryAgainBtn = b.GetComponent<Button>();
                }

                if (m_Scene11_AttackAvoidedCard != null && m_Scene11_SeeBeforeAfterBtn == null)
                {
                    var b = m_Scene11_AttackAvoidedCard.transform.Find("SeeBeforeAfterButton");
                    if (b == null) b = m_Scene11_AttackAvoidedCard.transform.Find("Scene11_SeeComparisonBtn");
                    if (b != null) m_Scene11_SeeBeforeAfterBtn = b.GetComponent<Button>();
                }
            }

            // Scene 12 controls
            if (m_Scene12_FinalComparison != null)
            {
                if (m_Scene12_FinishReturnBtn == null)
                {
                    var b = m_Scene12_FinalComparison.transform.Find("FinalActionButtons/FinishReturnButton");
                    if (b == null) b = m_Scene12_FinalComparison.transform.Find("ActionButtons/FinishReturnButton");
                    if (b == null) b = m_Scene12_FinalComparison.transform.Find("FinishReturnButton");
                    if (b != null) m_Scene12_FinishReturnBtn = b.GetComponent<Button>();
                }
                if (m_Scene12_ReplaySimulationBtn == null)
                {
                    var b = m_Scene12_FinalComparison.transform.Find("FinalActionButtons/ReplaySimulationButton");
                    if (b == null) b = m_Scene12_FinalComparison.transform.Find("ActionButtons/ReplaySimulationButton");
                    if (b == null) b = m_Scene12_FinalComparison.transform.Find("ReplaySimulationButton");
                    if (b != null) m_Scene12_ReplaySimulationBtn = b.GetComponent<Button>();
                }
            }
        }

        public void SetupButtonListeners()
        {
            if (m_Scene7_InspectNetworkBtn != null)
            {
                m_Scene7_InspectNetworkBtn.onClick.RemoveListener(ShowScene8_InspectWifiDetails);
                m_Scene7_InspectNetworkBtn.onClick.AddListener(ShowScene8_InspectWifiDetails);
            }

            if (m_Scene8_InspectLoginBtn != null)
            {
                m_Scene8_InspectLoginBtn.onClick.RemoveListener(ShowScene9_RewindToLoginPage);
                m_Scene8_InspectLoginBtn.onClick.AddListener(ShowScene9_RewindToLoginPage);
            }

            if (m_Scene9_TestYourselfBtn != null)
            {
                m_Scene9_TestYourselfBtn.onClick.RemoveListener(ShowScene10_InteractiveChallenge);
                m_Scene9_TestYourselfBtn.onClick.AddListener(ShowScene10_InteractiveChallenge);
            }

            if (m_Scene10_MakeSafeDecisionBtn != null)
            {
                m_Scene10_MakeSafeDecisionBtn.onClick.RemoveListener(ShowScene11_SafeDecision);
                m_Scene10_MakeSafeDecisionBtn.onClick.AddListener(ShowScene11_SafeDecision);
            }

            // Challenge targets (1=Password, 2=Domain, 3=Warning, 4=OpenWifi)
            if (m_TargetPasswordBtn != null)
            {
                m_TargetPasswordBtn.onClick.RemoveAllListeners();
                m_TargetPasswordBtn.onClick.AddListener(() => OnChallengeTargetClicked(1));
            }
            if (m_TargetDomainBtn != null)
            {
                m_TargetDomainBtn.onClick.RemoveAllListeners();
                m_TargetDomainBtn.onClick.AddListener(() => OnChallengeTargetClicked(2));
            }
            if (m_TargetWarningBtn != null)
            {
                m_TargetWarningBtn.onClick.RemoveAllListeners();
                m_TargetWarningBtn.onClick.AddListener(() => OnChallengeTargetClicked(3));
            }
            if (m_TargetOpenWifiBtn != null)
            {
                m_TargetOpenWifiBtn.onClick.RemoveAllListeners();
                m_TargetOpenWifiBtn.onClick.AddListener(() => OnChallengeTargetClicked(4));
            }

            // Scene 11 buttons
            if (m_Scene11_ContinueBtn != null)
            {
                m_Scene11_ContinueBtn.onClick.RemoveListener(OnScene11ContinueClicked);
                m_Scene11_ContinueBtn.onClick.AddListener(OnScene11ContinueClicked);
            }
            if (m_Scene11_StopVerifyBtn != null)
            {
                m_Scene11_StopVerifyBtn.onClick.RemoveListener(OnScene11StopVerifyClicked);
                m_Scene11_StopVerifyBtn.onClick.AddListener(OnScene11StopVerifyClicked);
            }
            if (m_Scene11_TryAgainBtn != null)
            {
                m_Scene11_TryAgainBtn.onClick.RemoveListener(OnScene11TryAgainClicked);
                m_Scene11_TryAgainBtn.onClick.AddListener(OnScene11TryAgainClicked);
            }
            if (m_Scene11_SeeBeforeAfterBtn != null)
            {
                m_Scene11_SeeBeforeAfterBtn.onClick.RemoveListener(ShowScene12_FinalComparison);
                m_Scene11_SeeBeforeAfterBtn.onClick.AddListener(ShowScene12_FinalComparison);
            }

            // Scene 12 buttons
            if (m_Scene12_FinishReturnBtn != null)
            {
                m_Scene12_FinishReturnBtn.onClick.RemoveListener(OnFinishReturnClicked);
                m_Scene12_FinishReturnBtn.onClick.AddListener(OnFinishReturnClicked);
            }
            if (m_Scene12_ReplaySimulationBtn != null)
            {
                m_Scene12_ReplaySimulationBtn.onClick.RemoveListener(OnReplaySimulationClicked);
                m_Scene12_ReplaySimulationBtn.onClick.AddListener(OnReplaySimulationClicked);
            }
        }

        public void RemoveButtonListeners()
        {
            if (m_Scene7_InspectNetworkBtn != null) m_Scene7_InspectNetworkBtn.onClick.RemoveListener(ShowScene8_InspectWifiDetails);
            if (m_Scene8_InspectLoginBtn != null) m_Scene8_InspectLoginBtn.onClick.RemoveListener(ShowScene9_RewindToLoginPage);
            if (m_Scene9_TestYourselfBtn != null) m_Scene9_TestYourselfBtn.onClick.RemoveListener(ShowScene10_InteractiveChallenge);
            if (m_Scene10_MakeSafeDecisionBtn != null) m_Scene10_MakeSafeDecisionBtn.onClick.RemoveListener(ShowScene11_SafeDecision);

            if (m_Scene11_ContinueBtn != null) m_Scene11_ContinueBtn.onClick.RemoveListener(OnScene11ContinueClicked);
            if (m_Scene11_StopVerifyBtn != null) m_Scene11_StopVerifyBtn.onClick.RemoveListener(OnScene11StopVerifyClicked);
            if (m_Scene11_TryAgainBtn != null) m_Scene11_TryAgainBtn.onClick.RemoveListener(OnScene11TryAgainClicked);
            if (m_Scene11_SeeBeforeAfterBtn != null) m_Scene11_SeeBeforeAfterBtn.onClick.RemoveListener(ShowScene12_FinalComparison);

            if (m_Scene12_FinishReturnBtn != null) m_Scene12_FinishReturnBtn.onClick.RemoveListener(OnFinishReturnClicked);
            if (m_Scene12_ReplaySimulationBtn != null) m_Scene12_ReplaySimulationBtn.onClick.RemoveListener(OnReplaySimulationClicked);
        }

        /// <summary>
        /// Begins the continuous educational rewind starting at Scene 7.
        /// </summary>
        public void StartRewindSequence()
        {
            FindReferencesIfNull();
            ShowScene7_RewindToWifi();
        }

        public void ShowScene7_RewindToWifi()
        {
            CurrentScene = 7;
            HideAllRewindScenes();
            if (m_Scene7_RewindToWifi != null) m_Scene7_RewindToWifi.SetActive(true);

            if (m_UrlText != null) m_UrlText.text = "edu://rewind.simulation/step-7-wifi-redflag";
            if (m_TabTitleText != null) m_TabTitleText.text = "Rewind #1: Suspicious Wi-Fi";

            EnsureCollidersAndInteractables();
            Debug.Log("[EducationalRewindController] Entered Scene 7: Rewind to Wi-Fi.");
        }

        public void ShowScene8_InspectWifiDetails()
        {
            CurrentScene = 8;
            HideAllRewindScenes();
            if (m_Scene8_InspectWifiDetails != null) m_Scene8_InspectWifiDetails.SetActive(true);

            if (m_UrlText != null) m_UrlText.text = "edu://rewind.simulation/step-8-network-details";
            if (m_TabTitleText != null) m_TabTitleText.text = "Rewind #2: Network Details";

            EnsureCollidersAndInteractables();
            Debug.Log("[EducationalRewindController] Entered Scene 8: Inspect Wi-Fi Details.");
        }

        public void ShowScene9_RewindToLoginPage()
        {
            CurrentScene = 9;
            HideAllRewindScenes();
            if (m_Scene9_RewindToLoginPage != null) m_Scene9_RewindToLoginPage.SetActive(true);

            if (m_UrlText != null) m_UrlText.text = "edu://rewind.simulation/step-9-login-inspection";
            if (m_TabTitleText != null) m_TabTitleText.text = "Rewind #3: Login Page Red Flags";

            EnsureCollidersAndInteractables();
            Debug.Log("[EducationalRewindController] Entered Scene 9: Rewind to Login Page.");
        }

        public void ShowScene10_InteractiveChallenge()
        {
            CurrentScene = 10;
            HideAllRewindScenes();
            if (m_Scene10_InteractiveChallenge != null) m_Scene10_InteractiveChallenge.SetActive(true);

            if (m_UrlText != null) m_UrlText.text = "edu://rewind.simulation/step-10-challenge";
            if (m_TabTitleText != null) m_TabTitleText.text = "Rewind #4: Find Warning Signs";

            // Reset challenge state
            m_FoundTargets.Clear();
            UpdateChallengeUI();

            EnsureCollidersAndInteractables();
            Debug.Log("[EducationalRewindController] Entered Scene 10: Interactive Challenge.");
        }

        public void OnChallengeTargetClicked(int targetIndex)
        {
            m_FoundTargets.Add(targetIndex);
            UpdateChallengeUI();

            string feedback = "";
            switch (targetIndex)
            {
                case 1:
                    feedback = "<color=#FF3B30><b>[RED FLAG FOUND: Password Request]</b></color>\nAn unexpected portal is requesting sensitive credentials you normally use for email or banking.";
                    break;
                case 2:
                    feedback = "<color=#34C759><b>[RED FLAG FOUND: Website Address]</b></color>\nThe domain ('coffeeshop-free-login.example') does not match the legitimate coffee shop organization.";
                    break;
                case 3:
                    feedback = "<color=#FF9500><b>[RED FLAG FOUND: Security Warning]</b></color>\nDo not casually bypass browser security warnings. HTTPS alone does not prove a site is trustworthy.";
                    break;
                case 4:
                    feedback = "<color=#0A84FF><b>[RED FLAG FOUND: Open Network Indicator]</b></color>\nOpen networks are not automatically malicious, but require additional caution when sensitive credentials are asked.";
                    break;
            }

            if (m_ChallengeFeedbackText != null)
            {
                m_ChallengeFeedbackText.text = feedback;
            }

            EnsureCollidersAndInteractables();
            Debug.Log($"[EducationalRewindController] Challenge target {targetIndex} clicked. Total found: {m_FoundTargets.Count}/4");
        }

        private void UpdateChallengeUI()
        {
            int count = m_FoundTargets.Count;

            if (m_ChallengeCounterText != null)
            {
                if (count < 4)
                {
                    m_ChallengeCounterText.text = $"Warning signs found: <color=#FF9500><b>{count}/4</b></color>";
                }
                else
                {
                    m_ChallengeCounterText.text = "Warning signs found: <color=#34C759><b>4/4 — All Signs Identified!</b></color>";
                }
            }

            if (m_Scene10_MakeSafeDecisionBtn != null)
            {
                m_Scene10_MakeSafeDecisionBtn.interactable = (count >= 4);
            }
        }

        public void ShowScene11_SafeDecision()
        {
            CurrentScene = 11;
            HideAllRewindScenes();
            if (m_Scene11_SafeDecision != null) m_Scene11_SafeDecision.SetActive(true);

            // Reset branch cards
            if (m_Scene11_ChoicesCard != null) m_Scene11_ChoicesCard.SetActive(true);
            if (m_Scene11_AttackerWarningCard != null) m_Scene11_AttackerWarningCard.SetActive(false);
            if (m_Scene11_AttackAvoidedCard != null) m_Scene11_AttackAvoidedCard.SetActive(false);

            if (m_UrlText != null) m_UrlText.text = "edu://rewind.simulation/step-11-decision";
            if (m_TabTitleText != null) m_TabTitleText.text = "Rewind #5: Make the Safe Decision";

            EnsureCollidersAndInteractables();
            Debug.Log("[EducationalRewindController] Entered Scene 11: Safe Decision.");
        }

        public void OnScene11ContinueClicked()
        {
            if (m_Scene11_ChoicesCard != null) m_Scene11_ChoicesCard.SetActive(false);
            if (m_Scene11_AttackerWarningCard != null) m_Scene11_AttackerWarningCard.SetActive(true);
            if (m_Scene11_AttackAvoidedCard != null) m_Scene11_AttackAvoidedCard.SetActive(false);

            EnsureCollidersAndInteractables();
            Debug.Log("[EducationalRewindController] Player chose CONTINUE. Showed educational warning.");
        }

        public void OnScene11TryAgainClicked()
        {
            if (m_Scene11_ChoicesCard != null) m_Scene11_ChoicesCard.SetActive(true);
            if (m_Scene11_AttackerWarningCard != null) m_Scene11_AttackerWarningCard.SetActive(false);
            if (m_Scene11_AttackAvoidedCard != null) m_Scene11_AttackAvoidedCard.SetActive(false);

            EnsureCollidersAndInteractables();
            Debug.Log("[EducationalRewindController] Player selected Try Again. Reset choices.");
        }

        public void OnScene11StopVerifyClicked()
        {
            if (m_Scene11_ChoicesCard != null) m_Scene11_ChoicesCard.SetActive(false);
            if (m_Scene11_AttackerWarningCard != null) m_Scene11_AttackerWarningCard.SetActive(false);
            if (m_Scene11_AttackAvoidedCard != null) m_Scene11_AttackAvoidedCard.SetActive(true);

            EnsureCollidersAndInteractables();
            Debug.Log("[EducationalRewindController] Player chose STOP & VERIFY. ATTACK AVOIDED!");
        }

        public void ShowScene12_FinalComparison()
        {
            CurrentScene = 12;
            HideAllRewindScenes();
            if (m_Scene12_FinalComparison != null) m_Scene12_FinalComparison.SetActive(true);

            if (m_UrlText != null) m_UrlText.text = "edu://rewind.simulation/step-12-comparison";
            if (m_TabTitleText != null) m_TabTitleText.text = "Rewind #6: Final Comparison";

            EnsureCollidersAndInteractables();
            Debug.Log("[EducationalRewindController] Entered Scene 12: Final Comparison.");
        }

        public void OnFinishReturnClicked()
        {
            ResetRewind();

            if (m_CaptivePortalController != null)
            {
                m_CaptivePortalController.OnReturnToLaptopClicked();
            }
            else
            {
                gameObject.SetActive(false);
            }

            Debug.Log("[EducationalRewindController] Simulation finished. Returned cleanly to laptop desktop.");
        }

        public void OnReplaySimulationClicked()
        {
            ResetRewind();

            if (m_CaptivePortalController != null)
            {
                m_CaptivePortalController.ResetToInitialPortal();
            }

            Debug.Log("[EducationalRewindController] Replaying simulation from start.");
        }

        public void ResetRewind()
        {
            HideAllRewindScenes();
            m_FoundTargets.Clear();
            UpdateChallengeUI();

            if (m_Scene11_ChoicesCard != null) m_Scene11_ChoicesCard.SetActive(true);
            if (m_Scene11_AttackerWarningCard != null) m_Scene11_AttackerWarningCard.SetActive(false);
            if (m_Scene11_AttackAvoidedCard != null) m_Scene11_AttackAvoidedCard.SetActive(false);
        }

        private void HideAllRewindScenes()
        {
            if (m_Scene7_RewindToWifi != null) m_Scene7_RewindToWifi.SetActive(false);
            if (m_Scene8_InspectWifiDetails != null) m_Scene8_InspectWifiDetails.SetActive(false);
            if (m_Scene9_RewindToLoginPage != null) m_Scene9_RewindToLoginPage.SetActive(false);
            if (m_Scene10_InteractiveChallenge != null) m_Scene10_InteractiveChallenge.SetActive(false);
            if (m_Scene11_SafeDecision != null) m_Scene11_SafeDecision.SetActive(false);
            if (m_Scene12_FinalComparison != null) m_Scene12_FinalComparison.SetActive(false);
        }

        public void EnsureCollidersAndInteractables()
        {
            FindReferencesIfNull();

            // Scene 7
            if (m_Scene7_InspectNetworkBtn != null)
                SetupElementColliderAndInteractable(m_Scene7_InspectNetworkBtn.gameObject, ShowScene8_InspectWifiDetails);

            // Scene 8
            if (m_Scene8_InspectLoginBtn != null)
                SetupElementColliderAndInteractable(m_Scene8_InspectLoginBtn.gameObject, ShowScene9_RewindToLoginPage);

            // Scene 9
            if (m_Scene9_TestYourselfBtn != null)
                SetupElementColliderAndInteractable(m_Scene9_TestYourselfBtn.gameObject, ShowScene10_InteractiveChallenge);

            // Scene 10
            if (m_Scene10_MakeSafeDecisionBtn != null)
                SetupElementColliderAndInteractable(m_Scene10_MakeSafeDecisionBtn.gameObject, ShowScene11_SafeDecision);

            if (m_TargetPasswordBtn != null)
                SetupElementColliderAndInteractable(m_TargetPasswordBtn.gameObject, () => OnChallengeTargetClicked(1));
            if (m_TargetDomainBtn != null)
                SetupElementColliderAndInteractable(m_TargetDomainBtn.gameObject, () => OnChallengeTargetClicked(2));
            if (m_TargetWarningBtn != null)
                SetupElementColliderAndInteractable(m_TargetWarningBtn.gameObject, () => OnChallengeTargetClicked(3));
            if (m_TargetOpenWifiBtn != null)
                SetupElementColliderAndInteractable(m_TargetOpenWifiBtn.gameObject, () => OnChallengeTargetClicked(4));

            // Scene 11
            if (m_Scene11_ContinueBtn != null)
                SetupElementColliderAndInteractable(m_Scene11_ContinueBtn.gameObject, OnScene11ContinueClicked);
            if (m_Scene11_StopVerifyBtn != null)
                SetupElementColliderAndInteractable(m_Scene11_StopVerifyBtn.gameObject, OnScene11StopVerifyClicked);
            if (m_Scene11_TryAgainBtn != null)
                SetupElementColliderAndInteractable(m_Scene11_TryAgainBtn.gameObject, OnScene11TryAgainClicked);
            if (m_Scene11_SeeBeforeAfterBtn != null)
                SetupElementColliderAndInteractable(m_Scene11_SeeBeforeAfterBtn.gameObject, ShowScene12_FinalComparison);

            // Scene 12
            if (m_Scene12_FinishReturnBtn != null)
                SetupElementColliderAndInteractable(m_Scene12_FinishReturnBtn.gameObject, OnFinishReturnClicked);
            if (m_Scene12_ReplaySimulationBtn != null)
                SetupElementColliderAndInteractable(m_Scene12_ReplaySimulationBtn.gameObject, OnReplaySimulationClicked);
        }

        private void SetupElementColliderAndInteractable(GameObject target, Action onClickAction)
        {
            if (target == null) return;

            BoxCollider col = target.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = target.AddComponent<BoxCollider>();
            }

            RectTransform rt = target.GetComponent<RectTransform>();
            if (rt != null)
            {
                col.center = new Vector3(rt.rect.center.x, rt.rect.center.y, 0f);
                col.size = new Vector3(rt.rect.width, rt.rect.height, 50f);
            }
            col.enabled = true;

            XRSimpleInteractable interactable = target.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
            {
                interactable = target.AddComponent<XRSimpleInteractable>();
            }

            if (onClickAction != null)
            {
                interactable.selectEntered.RemoveAllListeners();
                interactable.selectEntered.AddListener((args) => onClickAction.Invoke());

                interactable.activated.RemoveAllListeners();
                interactable.activated.AddListener((args) => onClickAction.Invoke());
            }
        }

        public bool DispatchHit(GameObject hitObj)
        {
            if (hitObj == null) return false;

            if (Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return false;

            // Scene 7
            if (IsHitOnTarget(hitObj, m_Scene7_InspectNetworkBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                ShowScene8_InspectWifiDetails();
                return true;
            }

            // Scene 8
            if (IsHitOnTarget(hitObj, m_Scene8_InspectLoginBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                ShowScene9_RewindToLoginPage();
                return true;
            }

            // Scene 9
            if (IsHitOnTarget(hitObj, m_Scene9_TestYourselfBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                ShowScene10_InteractiveChallenge();
                return true;
            }

            // Scene 10
            if (IsHitOnTarget(hitObj, m_TargetPasswordBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnChallengeTargetClicked(1);
                return true;
            }
            if (IsHitOnTarget(hitObj, m_TargetDomainBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnChallengeTargetClicked(2);
                return true;
            }
            if (IsHitOnTarget(hitObj, m_TargetWarningBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnChallengeTargetClicked(3);
                return true;
            }
            if (IsHitOnTarget(hitObj, m_TargetOpenWifiBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnChallengeTargetClicked(4);
                return true;
            }
            if (IsHitOnTarget(hitObj, m_Scene10_MakeSafeDecisionBtn?.gameObject))
            {
                if (m_FoundTargets.Count >= 4)
                {
                    m_LastClickTime = Time.unscaledTime;
                    ShowScene11_SafeDecision();
                    return true;
                }
            }

            // Scene 11
            if (IsHitOnTarget(hitObj, m_Scene11_ContinueBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnScene11ContinueClicked();
                return true;
            }
            if (IsHitOnTarget(hitObj, m_Scene11_StopVerifyBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnScene11StopVerifyClicked();
                return true;
            }
            if (IsHitOnTarget(hitObj, m_Scene11_TryAgainBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnScene11TryAgainClicked();
                return true;
            }
            if (IsHitOnTarget(hitObj, m_Scene11_SeeBeforeAfterBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                ShowScene12_FinalComparison();
                return true;
            }

            // Scene 12
            if (IsHitOnTarget(hitObj, m_Scene12_FinishReturnBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnFinishReturnClicked();
                return true;
            }
            if (IsHitOnTarget(hitObj, m_Scene12_ReplaySimulationBtn?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnReplaySimulationClicked();
                return true;
            }

            return false;
        }

        private bool IsHitOnTarget(GameObject hitObj, GameObject target)
        {
            if (hitObj == null || target == null) return false;
            return hitObj == target || hitObj.transform.IsChildOf(target.transform);
        }
    }
}
