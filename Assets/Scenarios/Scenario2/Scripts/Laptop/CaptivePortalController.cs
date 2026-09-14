using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kannko.Laptop
{
    /// <summary>
    /// Manages the educational captive-portal security-awareness simulation in Scenario 2.
    /// Simulates a Rogue AP / Evil Twin phishing attack when connecting to 'Jakko-cooffee_free'.
    /// Educational only: Never transmits, stores, or captures real credentials.
    /// </summary>
    public class CaptivePortalController : MonoBehaviour
    {
        [Header("Panels")]
        [Tooltip("The initial portal card showing Google/Facebook buttons.")]
        [SerializeField] private GameObject m_InitialPortalCard;

        [Tooltip("The captive portal phishing simulation panel.")]
        [SerializeField] private GameObject m_CaptivePortalPanel;

        [Tooltip("The dramatic attack reveal warning panel.")]
        [SerializeField] private GameObject m_AttackRevealPanel;

        [Tooltip("The attacker dashboard terminal simulation panel.")]
        [SerializeField] private GameObject m_AttackerDashboardPanel;

        [Tooltip("The comprehensive attack summary panel.")]
        [SerializeField] private GameObject m_AttackSummaryPanel;

        [Tooltip("The educational rewind panel explaining the 6 warning signs.")]
        [SerializeField] private GameObject m_EducationalRewindPanel;

        [Tooltip("Controller for the Educational Rewind 6-scene interactive progression.")]
        [SerializeField] private EducationalRewindController m_EducationalRewindController;

        [Tooltip("Detailed educational explanation card inside AttackRevealPanel.")]
        [SerializeField] private GameObject m_AttackerLogCard;

        [Header("External World Space Presentation")]
        [Tooltip("The external World Space Canvas GameObject hosting the Reveal and Educational experience outside the laptop.")]
        [SerializeField] private GameObject m_RevealWorldSpaceCanvas;

        [Tooltip("Optional CanvasGroup on the external World Space Canvas for smooth fade transitions.")]
        [SerializeField] private CanvasGroup m_RevealCanvasGroup;

        [Tooltip("Reference to the FloatingLaptopPanel under WorldUI to hide during reveal presentation.")]
        [SerializeField] private GameObject m_FloatingLaptopPanel;

        [Header("Browser Chrome Context")]
        [SerializeField] private TextMeshProUGUI m_UrlText;
        [SerializeField] private TextMeshProUGUI m_TabTitleText;

        [Header("Captive Portal Controls")]
        [SerializeField] private TMP_InputField m_EmailInputField;
        [SerializeField] private TMP_InputField m_PasswordInputField;
        [SerializeField] private Button m_ConnectButton;
        [SerializeField] private Button m_FillDemoButton;
        [SerializeField] private TextMeshProUGUI m_PortalStatusText;
        [SerializeField] private TextMeshProUGUI m_DomainBadgeText;
        [SerializeField] private TextMeshProUGUI m_ProviderNoticeText;

        [Header("Attack Reveal Controls")]
        [SerializeField] private Button m_SeeWhatHappenedButton;
        [SerializeField] private Button m_ReturnToLaptopButton;
        [SerializeField] private Button m_CloseRevealButton;
        [SerializeField] private TextMeshProUGUI m_WarningTitleText;
        [SerializeField] private TextMeshProUGUI m_TrappedHeadlineText;
        [SerializeField] private TextMeshProUGUI m_ExplanationBodyText;
        [SerializeField] private TextMeshProUGUI m_AttackerSummaryText;

        [Header("Attacker Dashboard Controls")]
        [SerializeField] private GameObject[] m_EventFeedItems;
        [SerializeField] private Button m_ViewAttackSummaryButton;
        [SerializeField] private TextMeshProUGUI m_VictimTimeText;

        [Header("Attack Summary Controls")]
        [SerializeField] private Button m_LearnHowToDetectButton;

        [Header("Educational Rewind Controls")]
        [SerializeField] private Button m_FinishReturnButton;
        [SerializeField] private Button m_ReplaySimulationButton;

        [Header("Settings & Timings")]
        [SerializeField] private float m_AuthDuration = 1.5f;
        [SerializeField] private float m_SuccessDisplayDuration = 2.0f;
        [SerializeField] private float m_EventFeedInterval = 0.8f;
        [SerializeField] private float m_ClickDebounceTime = 0.25f;

        // Constants
        private const string FICTIONAL_PORTAL_URL = "https://coffeeshop-free-login.example";
        private const string ATTACK_ALERT_URL = "alert://security-warning.simulation/evil-twin-intercept";
        private const string ATTACKER_DASHBOARD_URL = "terminal://attacker-c2.simulation/dashboard";
        private const string ATTACK_SUMMARY_URL = "terminal://attacker-c2.simulation/summary";
        private const string EDU_REWIND_URL = "edu://security-awareness.training/attack-rewind";
        private const string DEFAULT_PORTAL_URL = "https://jakko-coffee.wifi/login";
        private const string DEMO_EMAIL = "demo@example.test";
        private const string DEMO_PASSWORD = "demo-password";

        private Coroutine m_AuthCoroutine;
        private Coroutine m_EventFeedCoroutine;
        private float m_LastClickTime;
        private JakkoFreeWifiBrowserController m_BrowserController;

        public bool IsSimulatingAuth { get; private set; }
        public bool IsRevealPresentationActive { get; private set; }
        public GameObject FloatingLaptopPanel => m_FloatingLaptopPanel;

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
            if (m_AuthCoroutine != null)
            {
                StopCoroutine(m_AuthCoroutine);
                m_AuthCoroutine = null;
            }
            if (m_EventFeedCoroutine != null)
            {
                StopCoroutine(m_EventFeedCoroutine);
                m_EventFeedCoroutine = null;
            }
            IsSimulatingAuth = false;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            // When the external World Space Canvas is active, ensure desktop mouse clicks can raycast to its buttons
            if (m_RevealWorldSpaceCanvas != null && m_RevealWorldSpaceCanvas.activeInHierarchy)
            {
                if (UnityEngine.InputSystem.Mouse.current != null &&
                    UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                        Ray mouseRay = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
                        if (Physics.Raycast(mouseRay, out RaycastHit hit, 10f))
                        {
                            DispatchHit(hit.collider.gameObject);
                        }
                    }
                }
            }
        }

        public void FindReferencesIfNull()
        {
            if (m_BrowserController == null)
            {
                m_BrowserController = GetComponent<JakkoFreeWifiBrowserController>();
                if (m_BrowserController == null)
                {
                    m_BrowserController = GetComponentInParent<JakkoFreeWifiBrowserController>();
                }
            }

            // Locate external World Space Canvas under WorldUI or scene root
            if (m_RevealWorldSpaceCanvas == null)
            {
                var rw = GameObject.Find("Scenario2RevealWorldSpace");
                if (rw == null)
                {
                    var wui = GameObject.Find("WorldUI");
                    if (wui != null)
                    {
                        var t = wui.transform.Find("Scenario2RevealWorldSpace");
                        if (t != null) rw = t.gameObject;
                    }
                }
                if (rw != null) m_RevealWorldSpaceCanvas = rw;
            }

            if (m_RevealWorldSpaceCanvas != null && m_RevealCanvasGroup == null)
            {
                m_RevealCanvasGroup = m_RevealWorldSpaceCanvas.GetComponent<CanvasGroup>();
            }

            if (m_FloatingLaptopPanel == null)
            {
                var wui = GameObject.Find("WorldUI");
                if (wui != null)
                {
                    var t = wui.transform.Find("FloatingLaptopPanel");
                    if (t != null) m_FloatingLaptopPanel = t.gameObject;
                }
                if (m_FloatingLaptopPanel == null)
                {
                    m_FloatingLaptopPanel = GameObject.Find("FloatingLaptopPanel");
                }
            }

            // Check external World Space Canvas first for the reveal sequence panels
            Transform externalRoot = m_RevealWorldSpaceCanvas != null ? m_RevealWorldSpaceCanvas.transform : null;
            if (externalRoot != null)
            {
                Transform tReveal = externalRoot.Find("AttackRevealPanel");
                if (tReveal != null) m_AttackRevealPanel = tReveal.gameObject;

                Transform tDash = externalRoot.Find("AttackerDashboardPanel");
                if (tDash != null) m_AttackerDashboardPanel = tDash.gameObject;

                Transform tSumm = externalRoot.Find("AttackSummaryPanel");
                if (tSumm != null) m_AttackSummaryPanel = tSumm.gameObject;

                Transform tEdu = externalRoot.Find("EducationalRewindPanel");
                if (tEdu != null) m_EducationalRewindPanel = tEdu.gameObject;
            }

            Transform pageContent = transform.Find("PageContent");
            if (pageContent == null && transform.parent != null)
            {
                pageContent = transform.parent.Find("PageContent");
            }

            if (pageContent != null)
            {
                if (m_InitialPortalCard == null)
                {
                    Transform t = pageContent.Find("PortalCard");
                    if (t != null) m_InitialPortalCard = t.gameObject;
                }

                if (m_CaptivePortalPanel == null)
                {
                    Transform t = pageContent.Find("CaptivePortalPanel");
                    if (t != null) m_CaptivePortalPanel = t.gameObject;
                }

                if (m_AttackRevealPanel == null)
                {
                    Transform t = pageContent.Find("AttackRevealPanel");
                    if (t != null) m_AttackRevealPanel = t.gameObject;
                }

                if (m_AttackerDashboardPanel == null)
                {
                    Transform t = pageContent.Find("AttackerDashboardPanel");
                    if (t != null) m_AttackerDashboardPanel = t.gameObject;
                }

                if (m_AttackSummaryPanel == null)
                {
                    Transform t = pageContent.Find("AttackSummaryPanel");
                    if (t != null) m_AttackSummaryPanel = t.gameObject;
                }

                if (m_EducationalRewindPanel == null)
                {
                    Transform t = pageContent.Find("EducationalRewindPanel");
                    if (t != null) m_EducationalRewindPanel = t.gameObject;
                }
            }

            // Browser Chrome References
            if (m_UrlText == null)
            {
                Transform t = transform.Find("BrowserHeader/NavBar/AddressBar/UrlText");
                if (t != null) m_UrlText = t.GetComponent<TextMeshProUGUI>();
            }

            if (m_TabTitleText == null)
            {
                Transform t = transform.Find("BrowserHeader/TabBar/ActiveTab/TabTitle");
                if (t != null) m_TabTitleText = t.GetComponent<TextMeshProUGUI>();
            }

            // Captive Portal panel child controls
            if (m_CaptivePortalPanel != null)
            {
                if (m_EmailInputField == null)
                {
                    var inp = m_CaptivePortalPanel.transform.Find("EmailField/InputField");
                    if (inp != null) m_EmailInputField = inp.GetComponent<TMP_InputField>();
                }

                if (m_PasswordInputField == null)
                {
                    var inp = m_CaptivePortalPanel.transform.Find("PasswordField/InputField");
                    if (inp != null) m_PasswordInputField = inp.GetComponent<TMP_InputField>();
                }

                if (m_ConnectButton == null)
                {
                    var btn = m_CaptivePortalPanel.transform.Find("ConnectButton");
                    if (btn != null) m_ConnectButton = btn.GetComponent<Button>();
                }

                if (m_FillDemoButton == null)
                {
                    var btn = m_CaptivePortalPanel.transform.Find("FillDemoButton");
                    if (btn != null) m_FillDemoButton = btn.GetComponent<Button>();
                }

                if (m_PortalStatusText == null)
                {
                    var txt = m_CaptivePortalPanel.transform.Find("PortalStatusText");
                    if (txt != null) m_PortalStatusText = txt.GetComponent<TextMeshProUGUI>();
                }

                if (m_DomainBadgeText == null)
                {
                    var txt = m_CaptivePortalPanel.transform.Find("DomainBadge/DomainText");
                    if (txt != null) m_DomainBadgeText = txt.GetComponent<TextMeshProUGUI>();
                }

                if (m_ProviderNoticeText == null)
                {
                    var txt = m_CaptivePortalPanel.transform.Find("ProviderNoticeText");
                    if (txt != null) m_ProviderNoticeText = txt.GetComponent<TextMeshProUGUI>();
                }
            }

            // Attack Reveal panel child controls
            if (m_AttackRevealPanel != null)
            {
                if (m_SeeWhatHappenedButton == null || !m_SeeWhatHappenedButton.transform.IsChildOf(m_AttackRevealPanel.transform))
                {
                    var btn = m_AttackRevealPanel.transform.Find("ActionButtons/SeeWhatHappenedButton");
                    if (btn == null) btn = m_AttackRevealPanel.transform.Find("SeeWhatHappenedButton");
                    if (btn != null) m_SeeWhatHappenedButton = btn.GetComponent<Button>();
                }

                if (m_ReturnToLaptopButton == null || !m_ReturnToLaptopButton.transform.IsChildOf(m_AttackRevealPanel.transform))
                {
                    var btn = m_AttackRevealPanel.transform.Find("ActionButtons/ReturnToLaptopButton");
                    if (btn == null) btn = m_AttackRevealPanel.transform.Find("ReturnToLaptopButton");
                    if (btn != null) m_ReturnToLaptopButton = btn.GetComponent<Button>();
                }

                if (m_CloseRevealButton == null || !m_CloseRevealButton.transform.IsChildOf(m_AttackRevealPanel.transform))
                {
                    var btn = m_AttackRevealPanel.transform.Find("CloseRevealButton");
                    if (btn != null) m_CloseRevealButton = btn.GetComponent<Button>();
                }

                if (m_AttackerLogCard == null || !m_AttackerLogCard.transform.IsChildOf(m_AttackRevealPanel.transform))
                {
                    var card = m_AttackRevealPanel.transform.Find("AttackerLogCard");
                    if (card != null) m_AttackerLogCard = card.gameObject;
                }

                if (m_WarningTitleText == null || !m_WarningTitleText.transform.IsChildOf(m_AttackRevealPanel.transform))
                {
                    var txt = m_AttackRevealPanel.transform.Find("WarningHeader/WarningTitle");
                    if (txt != null) m_WarningTitleText = txt.GetComponent<TextMeshProUGUI>();
                }

                if (m_TrappedHeadlineText == null || !m_TrappedHeadlineText.transform.IsChildOf(m_AttackRevealPanel.transform))
                {
                    var txt = m_AttackRevealPanel.transform.Find("TrappedHeadline");
                    if (txt != null) m_TrappedHeadlineText = txt.GetComponent<TextMeshProUGUI>();
                }

                if (m_ExplanationBodyText == null || !m_ExplanationBodyText.transform.IsChildOf(m_AttackRevealPanel.transform))
                {
                    var txt = m_AttackRevealPanel.transform.Find("ExplanationCard/ExplanationBody");
                    if (txt == null) txt = m_AttackRevealPanel.transform.Find("ExplanationBody");
                    if (txt != null) m_ExplanationBodyText = txt.GetComponent<TextMeshProUGUI>();
                }

                if ((m_AttackerSummaryText == null || (m_AttackerLogCard != null && !m_AttackerSummaryText.transform.IsChildOf(m_AttackerLogCard.transform))) && m_AttackerLogCard != null)
                {
                    var txt = m_AttackerLogCard.transform.Find("AttackerSummaryText");
                    if (txt != null) m_AttackerSummaryText = txt.GetComponent<TextMeshProUGUI>();
                }
            }

            // Attacker Dashboard panel child controls
            if (m_AttackerDashboardPanel != null)
            {
                if (m_ViewAttackSummaryButton == null)
                {
                    var btn = m_AttackerDashboardPanel.transform.Find("ViewAttackSummaryButton");
                    if (btn != null) m_ViewAttackSummaryButton = btn.GetComponent<Button>();
                }

                if (m_VictimTimeText == null)
                {
                    var txt = m_AttackerDashboardPanel.transform.Find("SimulatedVictimCard/TimeRow/Value");
                    if (txt != null) m_VictimTimeText = txt.GetComponent<TextMeshProUGUI>();
                }

                Transform eventContainer = m_AttackerDashboardPanel.transform.Find("EventFeedCard/EventList");
                if (eventContainer != null && (m_EventFeedItems == null || m_EventFeedItems.Length == 0))
                {
                    var list = new List<GameObject>();
                    for (int i = 0; i < eventContainer.childCount; i++)
                    {
                        list.Add(eventContainer.GetChild(i).gameObject);
                    }
                    m_EventFeedItems = list.ToArray();
                }
            }

            // Attack Summary panel child controls
            if (m_AttackSummaryPanel != null)
            {
                if (m_LearnHowToDetectButton == null)
                {
                    var btn = m_AttackSummaryPanel.transform.Find("LearnHowToDetectButton");
                    if (btn != null) m_LearnHowToDetectButton = btn.GetComponent<Button>();
                }
            }

            // Educational Rewind panel child controls
            if (m_EducationalRewindPanel != null)
            {
                if (m_EducationalRewindController == null)
                {
                    m_EducationalRewindController = m_EducationalRewindPanel.GetComponent<EducationalRewindController>();
                }

                if (m_FinishReturnButton == null)
                {
                    var btn = m_EducationalRewindPanel.transform.Find("Scene12_FinalComparison/FinalActionButtons/FinishReturnButton");
                    if (btn == null) btn = m_EducationalRewindPanel.transform.Find("ActionButtons/FinishReturnButton");
                    if (btn == null) btn = m_EducationalRewindPanel.transform.Find("FinishReturnButton");
                    if (btn != null) m_FinishReturnButton = btn.GetComponent<Button>();
                }

                if (m_ReplaySimulationButton == null)
                {
                    var btn = m_EducationalRewindPanel.transform.Find("Scene12_FinalComparison/FinalActionButtons/ReplaySimulationButton");
                    if (btn == null) btn = m_EducationalRewindPanel.transform.Find("ActionButtons/ReplaySimulationButton");
                    if (btn == null) btn = m_EducationalRewindPanel.transform.Find("ReplaySimulationButton");
                    if (btn != null) m_ReplaySimulationButton = btn.GetComponent<Button>();
                }
            }
        }

        public void SetupButtonListeners()
        {
            if (m_ConnectButton != null)
            {
                m_ConnectButton.onClick.RemoveListener(OnConnectClicked);
                m_ConnectButton.onClick.AddListener(OnConnectClicked);
            }

            if (m_FillDemoButton != null)
            {
                m_FillDemoButton.onClick.RemoveListener(OnFillDemoClicked);
                m_FillDemoButton.onClick.AddListener(OnFillDemoClicked);
            }

            if (m_SeeWhatHappenedButton != null)
            {
                m_SeeWhatHappenedButton.onClick.RemoveListener(OnSeeWhatHappenedClicked);
                m_SeeWhatHappenedButton.onClick.AddListener(OnSeeWhatHappenedClicked);
            }

            if (m_ReturnToLaptopButton != null)
            {
                m_ReturnToLaptopButton.onClick.RemoveListener(OnReturnToLaptopClicked);
                m_ReturnToLaptopButton.onClick.AddListener(OnReturnToLaptopClicked);
            }

            if (m_CloseRevealButton != null)
            {
                m_CloseRevealButton.onClick.RemoveListener(OnReturnToLaptopClicked);
                m_CloseRevealButton.onClick.AddListener(OnReturnToLaptopClicked);
            }

            if (m_ViewAttackSummaryButton != null)
            {
                m_ViewAttackSummaryButton.onClick.RemoveListener(ShowAttackSummary);
                m_ViewAttackSummaryButton.onClick.AddListener(ShowAttackSummary);
            }

            if (m_LearnHowToDetectButton != null)
            {
                m_LearnHowToDetectButton.onClick.RemoveListener(ShowEducationalRewind);
                m_LearnHowToDetectButton.onClick.AddListener(ShowEducationalRewind);
            }

            if (m_FinishReturnButton != null)
            {
                m_FinishReturnButton.onClick.RemoveListener(OnReturnToLaptopClicked);
                m_FinishReturnButton.onClick.AddListener(OnReturnToLaptopClicked);
            }

            if (m_ReplaySimulationButton != null)
            {
                m_ReplaySimulationButton.onClick.RemoveListener(ResetToInitialPortal);
                m_ReplaySimulationButton.onClick.AddListener(ResetToInitialPortal);
            }
        }

        public void RemoveButtonListeners()
        {
            if (m_ConnectButton != null) m_ConnectButton.onClick.RemoveListener(OnConnectClicked);
            if (m_FillDemoButton != null) m_FillDemoButton.onClick.RemoveListener(OnFillDemoClicked);
            if (m_SeeWhatHappenedButton != null) m_SeeWhatHappenedButton.onClick.RemoveListener(OnSeeWhatHappenedClicked);
            if (m_ReturnToLaptopButton != null) m_ReturnToLaptopButton.onClick.RemoveListener(OnReturnToLaptopClicked);
            if (m_CloseRevealButton != null) m_CloseRevealButton.onClick.RemoveListener(OnReturnToLaptopClicked);
            if (m_ViewAttackSummaryButton != null) m_ViewAttackSummaryButton.onClick.RemoveListener(ShowAttackSummary);
            if (m_LearnHowToDetectButton != null) m_LearnHowToDetectButton.onClick.RemoveListener(ShowEducationalRewind);
            if (m_FinishReturnButton != null) m_FinishReturnButton.onClick.RemoveListener(OnReturnToLaptopClicked);
            if (m_ReplaySimulationButton != null) m_ReplaySimulationButton.onClick.RemoveListener(ResetToInitialPortal);
        }

        /// <summary>
        /// Transitions from initial portal card to fictional captive portal login simulation.
        /// Invoked when clicking either Google or Facebook login button.
        /// </summary>
        public void ShowCaptivePortal(string providerName = "Google")
        {
            FindReferencesIfNull();

            if (m_InitialPortalCard != null) m_InitialPortalCard.SetActive(false);
            if (m_RevealWorldSpaceCanvas != null) m_RevealWorldSpaceCanvas.SetActive(false);
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(false);
            if (m_AttackerDashboardPanel != null) m_AttackerDashboardPanel.SetActive(false);
            if (m_AttackSummaryPanel != null) m_AttackSummaryPanel.SetActive(false);
            if (m_EducationalRewindPanel != null) m_EducationalRewindPanel.SetActive(false);
            if (m_CaptivePortalPanel != null) m_CaptivePortalPanel.SetActive(true);

            // Update browser chrome
            if (m_UrlText != null)
            {
                m_UrlText.text = FICTIONAL_PORTAL_URL;
            }

            if (m_TabTitleText != null)
            {
                m_TabTitleText.text = "Jakko-Cooffee Wi-Fi - Portal";
            }

            if (m_DomainBadgeText != null)
            {
                m_DomainBadgeText.text = "coffeeshop-free-login.example";
            }

            if (m_ProviderNoticeText != null)
            {
                m_ProviderNoticeText.text = $"<color=#8E8E93>Authenticating with {providerName} via Captive Portal</color>";
            }

            // Fill demo credentials
            if (m_EmailInputField != null)
            {
                m_EmailInputField.text = DEMO_EMAIL;
            }

            if (m_PasswordInputField != null)
            {
                m_PasswordInputField.text = DEMO_PASSWORD;
            }

            // Reset status
            if (m_PortalStatusText != null)
            {
                m_PortalStatusText.text = "<color=#999999>Enter demo credentials and connect.</color>";
            }

            if (m_ConnectButton != null)
            {
                m_ConnectButton.interactable = true;
            }

            EnsureCollidersAndInteractables();
            Debug.Log($"[CaptivePortalController] Captive portal displayed for {providerName}. Target domain: {FICTIONAL_PORTAL_URL}");
        }

        /// <summary>
        /// Handles 'Connect to Internet' button press.
        /// </summary>
        public void OnConnectClicked()
        {
            if (Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            if (IsSimulatingAuth) return;

            if (m_AuthCoroutine != null) StopCoroutine(m_AuthCoroutine);
            m_AuthCoroutine = StartCoroutine(SimulateAuthenticationFlow());
        }

        /// <summary>
        /// Populates inputs with simulated demo values.
        /// </summary>
        public void OnFillDemoClicked()
        {
            if (Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            if (m_EmailInputField != null) m_EmailInputField.text = DEMO_EMAIL;
            if (m_PasswordInputField != null) m_PasswordInputField.text = DEMO_PASSWORD;
            if (m_PortalStatusText != null)
            {
                m_PortalStatusText.text = "<color=#007AFF>Demo credentials loaded.</color>";
            }
        }

        private IEnumerator SimulateAuthenticationFlow()
        {
            IsSimulatingAuth = true;

            if (m_ConnectButton != null)
            {
                m_ConnectButton.interactable = false;
            }

            if (m_PortalStatusText != null)
            {
                m_PortalStatusText.text = "<color=#FFA500><b>Authenticating...</b></color>";
            }

            Debug.Log("[CaptivePortalController] Starting authentication simulation...");
            yield return new WaitForSeconds(m_AuthDuration);

            if (m_PortalStatusText != null)
            {
                m_PortalStatusText.text =
                    "<color=#4CD964><b>✓ Authentication successful</b></color>\n" +
                    "<color=#FFFFFF>Welcome to Jakko-Cooffee Wi-Fi.</color>\n" +
                    "<size=85%><color=#8E8E93>Internet access is now available.</color></size>";
            }

            Debug.Log("[CaptivePortalController] Authentication succeeded. Showing brief confirmation before attack reveal...");
            yield return new WaitForSeconds(m_SuccessDisplayDuration);

            IsSimulatingAuth = false;
            ShowAttackReveal();
        }

        /// <summary>
        /// Dramatically transitions to the security-awareness warning panel.
        /// </summary>
        public void ShowAttackReveal()
        {
            FindReferencesIfNull();

            IsRevealPresentationActive = true;

            // Reveal moves outside to the external World Space Canvas!
            // Hide the laptop visual presentation so the player sees ONLY the Reveal World Space presentation.
            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(false);
            }

            if (m_RevealWorldSpaceCanvas != null)
            {
                m_RevealWorldSpaceCanvas.SetActive(true);
                if (m_RevealCanvasGroup != null)
                {
                    m_RevealCanvasGroup.alpha = 1f;
                }
            }

            // Laptop browser keeps m_CaptivePortalPanel visible so physical context is preserved
            if (m_InitialPortalCard != null) m_InitialPortalCard.SetActive(false);
            if (m_AttackerDashboardPanel != null) m_AttackerDashboardPanel.SetActive(false);
            if (m_AttackSummaryPanel != null) m_AttackSummaryPanel.SetActive(false);
            if (m_EducationalRewindPanel != null) m_EducationalRewindPanel.SetActive(false);
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(true);

            if (m_AttackerLogCard != null)
            {
                m_AttackerLogCard.SetActive(false);
            }

            if (m_UrlText != null)
            {
                m_UrlText.text = ATTACK_ALERT_URL;
            }

            if (m_TabTitleText != null)
            {
                m_TabTitleText.text = "⚠ Security Warning - Fake AP";
            }

            if (m_WarningTitleText != null)
            {
                m_WarningTitleText.text = "⚠ WARNING";
            }

            if (m_TrappedHeadlineText != null)
            {
                m_TrappedHeadlineText.text = "<color=#FF3B30>YOU WERE TRAPPED</color>";
            }

            if (m_ExplanationBodyText != null)
            {
                m_ExplanationBodyText.text =
                    "<b>This was NOT the legitimate Jakko-Cooffee Wi-Fi.</b>\n\n" +
                    "You connected to a <b>simulated fake access point</b>.\n" +
                    "The information you entered was submitted to the <b>simulated attacker dashboard</b>.\n\n" +
                    "<size=80%><color=#8E8E93><i>Because this is an educational simulation, the attacker dashboard is only an in-game representation. No real credentials were transmitted or stored.</i></color></size>";
            }

            if (m_SeeWhatHappenedButton != null)
            {
                m_SeeWhatHappenedButton.gameObject.SetActive(true);
                m_SeeWhatHappenedButton.interactable = true;
            }

            EnsureCollidersAndInteractables();
            Debug.Log("[CaptivePortalController] Attack Reveal Panel displayed: YOU WERE TRAPPED!");
        }

        /// <summary>
        /// Transitions from AttackRevealPanel into the Attacker Dashboard view.
        /// Invoked when clicking [ See What Happened ].
        /// </summary>
        public void OnSeeWhatHappenedClicked()
        {
            if (Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            ShowAttackerDashboard();
        }

        /// <summary>
        /// Displays the Attacker Dashboard with animated sequential live event feed.
        /// </summary>
        public void ShowAttackerDashboard()
        {
            FindReferencesIfNull();

            if (m_RevealWorldSpaceCanvas != null) m_RevealWorldSpaceCanvas.SetActive(true);
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(false);
            if (m_AttackSummaryPanel != null) m_AttackSummaryPanel.SetActive(false);
            if (m_EducationalRewindPanel != null) m_EducationalRewindPanel.SetActive(false);
            if (m_InitialPortalCard != null) m_InitialPortalCard.SetActive(false);

            if (m_AttackerDashboardPanel != null)
            {
                m_AttackerDashboardPanel.SetActive(true);
            }

            if (m_UrlText != null) m_UrlText.text = ATTACKER_DASHBOARD_URL;
            if (m_TabTitleText != null) m_TabTitleText.text = "Attacker Dashboard (Simulation)";

            if (m_VictimTimeText != null)
            {
                m_VictimTimeText.text = DateTime.Now.ToString("HH:mm:ss") + " (Just now)";
            }

            if (m_EventFeedCoroutine != null) StopCoroutine(m_EventFeedCoroutine);
            m_EventFeedCoroutine = StartCoroutine(PlayEventFeedSequence());

            EnsureCollidersAndInteractables();
            Debug.Log("[CaptivePortalController] Transitioned to ATTACKER DASHBOARD.");
        }

        private IEnumerator PlayEventFeedSequence()
        {
            if (m_ViewAttackSummaryButton != null)
            {
                m_ViewAttackSummaryButton.interactable = false;
            }

            if (m_EventFeedItems != null && m_EventFeedItems.Length > 0)
            {
                // Hide all events initially
                for (int i = 0; i < m_EventFeedItems.Length; i++)
                {
                    if (m_EventFeedItems[i] != null)
                    {
                        m_EventFeedItems[i].SetActive(false);
                    }
                }

                // Reveal events sequentially with the planned 0.8s interval
                for (int i = 0; i < m_EventFeedItems.Length; i++)
                {
                    yield return new WaitForSeconds(m_EventFeedInterval);

                    if (m_EventFeedItems[i] != null)
                    {
                        m_EventFeedItems[i].SetActive(true);
                    }
                }
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            if (m_ViewAttackSummaryButton != null)
            {
                m_ViewAttackSummaryButton.interactable = true;
            }

            EnsureCollidersAndInteractables();
            Debug.Log("[CaptivePortalController] Live Event Feed animation completed. [ View Attack Summary ] enabled.");
        }

        /// <summary>
        /// Displays the Attack Summary panel with network details, masked demo data, and educational explanation.
        /// Invoked when clicking [ View Attack Summary ].
        /// </summary>
        public void ShowAttackSummary()
        {
            FindReferencesIfNull();

            if (m_RevealWorldSpaceCanvas != null) m_RevealWorldSpaceCanvas.SetActive(true);
            if (m_AttackerDashboardPanel != null) m_AttackerDashboardPanel.SetActive(false);
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(false);
            if (m_EducationalRewindPanel != null) m_EducationalRewindPanel.SetActive(false);

            if (m_AttackSummaryPanel != null)
            {
                m_AttackSummaryPanel.SetActive(true);
            }

            if (m_UrlText != null) m_UrlText.text = ATTACK_SUMMARY_URL;
            if (m_TabTitleText != null) m_TabTitleText.text = "Attack Summary & Analysis";

            EnsureCollidersAndInteractables();
            Debug.Log("[CaptivePortalController] Displayed ATTACK SUMMARY panel.");
        }

        /// <summary>
        /// Displays the Educational Rewind panel walking through the 6 key warning signs.
        /// Invoked when clicking [ Learn How You Could Have Detected This ].
        /// </summary>
        public void ShowEducationalRewind()
        {
            FindReferencesIfNull();

            if (m_RevealWorldSpaceCanvas != null) m_RevealWorldSpaceCanvas.SetActive(true);
            if (m_AttackSummaryPanel != null) m_AttackSummaryPanel.SetActive(false);
            if (m_AttackerDashboardPanel != null) m_AttackerDashboardPanel.SetActive(false);
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(false);

            if (m_EducationalRewindPanel != null)
            {
                m_EducationalRewindPanel.SetActive(true);
                if (m_EducationalRewindController != null)
                {
                    m_EducationalRewindController.StartRewindSequence();
                }
            }

            if (m_UrlText != null) m_UrlText.text = EDU_REWIND_URL;
            if (m_TabTitleText != null) m_TabTitleText.text = "Security Awareness - Attack Rewind";

            EnsureCollidersAndInteractables();
            Debug.Log("[CaptivePortalController] Displayed EDUCATIONAL REWIND panel.");
        }

        /// <summary>
        /// Closes the simulation and returns cleanly to laptop desktop / network list.
        /// </summary>
        public void OnReturnToLaptopClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            IsRevealPresentationActive = false;

            if (m_RevealWorldSpaceCanvas != null) m_RevealWorldSpaceCanvas.SetActive(false);
            ResetToInitialPortal();

            // Restore the laptop visual presentation
            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(true);
            }

            if (m_BrowserController != null)
            {
                m_BrowserController.CloseBrowser();
            }
            else
            {
                gameObject.SetActive(false);
            }

            Debug.Log("[CaptivePortalController] Returned to laptop interface.");
        }

        /// <summary>
        /// Resets the browser content back to the initial portal card state.
        /// </summary>
        public void ResetToInitialPortal()
        {
            IsRevealPresentationActive = false;

            if (m_AuthCoroutine != null)
            {
                StopCoroutine(m_AuthCoroutine);
                m_AuthCoroutine = null;
            }
            if (m_EventFeedCoroutine != null)
            {
                StopCoroutine(m_EventFeedCoroutine);
                m_EventFeedCoroutine = null;
            }
            IsSimulatingAuth = false;

            if (m_RevealWorldSpaceCanvas != null) m_RevealWorldSpaceCanvas.SetActive(false);
            if (m_CaptivePortalPanel != null) m_CaptivePortalPanel.SetActive(false);
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(false);
            if (m_AttackerLogCard != null) m_AttackerLogCard.SetActive(false);
            if (m_AttackerDashboardPanel != null) m_AttackerDashboardPanel.SetActive(false);
            if (m_AttackSummaryPanel != null) m_AttackSummaryPanel.SetActive(false);
            if (m_EducationalRewindPanel != null) m_EducationalRewindPanel.SetActive(false);
            if (m_InitialPortalCard != null) m_InitialPortalCard.SetActive(true);

            if (m_UrlText != null) m_UrlText.text = DEFAULT_PORTAL_URL;
            if (m_TabTitleText != null) m_TabTitleText.text = "Jakko Coffee - Free Wi-Fi Portal";

            if (m_ConnectButton != null) m_ConnectButton.interactable = true;
            if (m_SeeWhatHappenedButton != null) m_SeeWhatHappenedButton.interactable = true;
            if (m_ViewAttackSummaryButton != null) m_ViewAttackSummaryButton.interactable = true;
            if (m_LearnHowToDetectButton != null) m_LearnHowToDetectButton.interactable = true;
        }

        /// <summary>
        /// Equips 3D BoxColliders and XRSimpleInteractables on all interactive buttons in the panels.
        /// </summary>
        public void EnsureCollidersAndInteractables()
        {
            FindReferencesIfNull();

            if (m_ConnectButton != null)
            {
                SetupElementColliderAndInteractable(m_ConnectButton.gameObject, OnConnectClicked);
            }

            if (m_FillDemoButton != null)
            {
                SetupElementColliderAndInteractable(m_FillDemoButton.gameObject, OnFillDemoClicked);
            }

            if (m_SeeWhatHappenedButton != null)
            {
                SetupElementColliderAndInteractable(m_SeeWhatHappenedButton.gameObject, OnSeeWhatHappenedClicked);
            }

            if (m_ReturnToLaptopButton != null)
            {
                SetupElementColliderAndInteractable(m_ReturnToLaptopButton.gameObject, OnReturnToLaptopClicked);
            }

            if (m_CloseRevealButton != null)
            {
                SetupElementColliderAndInteractable(m_CloseRevealButton.gameObject, OnReturnToLaptopClicked);
            }

            if (m_ViewAttackSummaryButton != null)
            {
                SetupElementColliderAndInteractable(m_ViewAttackSummaryButton.gameObject, ShowAttackSummary);
            }

            if (m_LearnHowToDetectButton != null)
            {
                SetupElementColliderAndInteractable(m_LearnHowToDetectButton.gameObject, ShowEducationalRewind);
            }

            if (m_FinishReturnButton != null)
            {
                SetupElementColliderAndInteractable(m_FinishReturnButton.gameObject, OnReturnToLaptopClicked);
            }

            if (m_ReplaySimulationButton != null)
            {
                SetupElementColliderAndInteractable(m_ReplaySimulationButton.gameObject, ResetToInitialPortal);
            }
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

        /// <summary>
        /// Dispatches a hit detected by raycast to the appropriate button handler.
        /// </summary>
        public bool DispatchHit(GameObject hitObj)
        {
            if (hitObj == null) return false;

            if (Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return false;

            if (IsHitOnTarget(hitObj, m_ConnectButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnConnectClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_FillDemoButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnFillDemoClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_SeeWhatHappenedButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnSeeWhatHappenedClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_ReturnToLaptopButton?.gameObject) ||
                IsHitOnTarget(hitObj, m_CloseRevealButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnReturnToLaptopClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_ViewAttackSummaryButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                ShowAttackSummary();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_LearnHowToDetectButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                ShowEducationalRewind();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_FinishReturnButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnReturnToLaptopClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_ReplaySimulationButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                ResetToInitialPortal();
                return true;
            }

            if (m_EducationalRewindPanel != null && (m_EducationalRewindPanel.activeSelf || m_EducationalRewindPanel.activeInHierarchy) && m_EducationalRewindController != null)
            {
                if (m_EducationalRewindController.DispatchHit(hitObj))
                {
                    m_LastClickTime = Time.unscaledTime;
                    return true;
                }
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
