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

        [Tooltip("The intermediate Wi-Fi Connected successfully browser state panel.")]
        [SerializeField] private GameObject m_WifiConnectedSuccessPanel;

        [Tooltip("The dramatic attack reveal warning panel.")]
        [SerializeField] private GameObject m_AttackRevealPanel;

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
        [SerializeField] private Button m_ReturnToLaptopButton;
        [SerializeField] private Button m_CloseRevealButton;

        [Header("Attacker Telemetry Panel")]
        [SerializeField] private GameObject m_AttackerTelemetryPanel;
        [SerializeField] private Button m_ViewAttackerInfoButton;
        [SerializeField] private Button m_BackFromAttackerInfoButton;
        [SerializeField] private Button m_AttackerInfoReturnToLaptopButton;

        [Header("Lesson Learned Panel")]
        [SerializeField] private GameObject m_LessonLearnedPanel;
        [SerializeField] private Button m_ViewLessonLearnedButton;
        [SerializeField] private Button m_BackFromLessonLearnedButton;
        [SerializeField] private Button m_LessonLearnedReturnToLaptopButton;
        [SerializeField] private TextMeshProUGUI m_WarningTitleText;
        [SerializeField] private TextMeshProUGUI m_TrappedHeadlineText;
        [SerializeField] private TextMeshProUGUI m_ExplanationBodyText;
        [SerializeField] private TextMeshProUGUI m_AttackerSummaryText;

        [Header("Settings & Timings")]
        [SerializeField] private float m_AuthDuration = 1.5f;
        [SerializeField] private float m_SuccessDisplayDuration = 2.0f;
        [SerializeField] private float m_ClickDebounceTime = 0.25f;

        // Constants
        private const string FICTIONAL_PORTAL_URL = "https://coffeeshop-free-login.example";
        private const string WIFI_CONNECTED_URL = "https://wifi.jakkocoffee.com/connected";
        private const string ATTACK_ALERT_URL = "alert://security-warning.simulation/evil-twin-intercept";
        private const string DEFAULT_PORTAL_URL = "https://jakko-coffee.wifi/login";
        private const string DEMO_EMAIL = "demo@example.test";
        private const string DEMO_PASSWORD = "demo-password";

        private Coroutine m_AuthCoroutine;
        private float m_LastClickTime;
        private JakkoFreeWifiBrowserController m_BrowserController;

        public bool IsSimulatingAuth { get; private set; }
        public bool IsRevealPresentationActive { get; private set; }
        public GameObject FloatingLaptopPanel => m_FloatingLaptopPanel;
        public GameObject WifiConnectedSuccessPanel => m_WifiConnectedSuccessPanel;

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
            IsSimulatingAuth = false;
        }

        private void Update()
        {
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

            // Check external World Space Canvas first for the reveal sequence panel
            Transform externalRoot = m_RevealWorldSpaceCanvas != null ? m_RevealWorldSpaceCanvas.transform : null;
            if (externalRoot != null)
            {
                Transform tReveal = externalRoot.Find("AttackRevealPanel");
                if (tReveal != null) m_AttackRevealPanel = tReveal.gameObject;
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

                if (m_WifiConnectedSuccessPanel == null)
                {
                    Transform t = pageContent.Find("WifiConnectedSuccessPanel");
                    if (t != null) m_WifiConnectedSuccessPanel = t.gameObject;
                }

                if (m_WifiConnectedSuccessPanel == null)
                {
                    EnsureWifiConnectedSuccessPanel(pageContent);
                }

                if (m_AttackRevealPanel == null)
                {
                    Transform t = pageContent.Find("AttackRevealPanel");
                    if (t != null) m_AttackRevealPanel = t.gameObject;
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
                if (m_ReturnToLaptopButton == null || !m_ReturnToLaptopButton.transform.IsChildOf(m_AttackRevealPanel.transform))
                {
                    var btn = m_AttackRevealPanel.transform.Find("ActionButtons/ReturnToLaptopButton");
                    if (btn == null) btn = m_AttackRevealPanel.transform.Find("ReturnToLaptopButton");
                    if (btn != null) m_ReturnToLaptopButton = btn.GetComponent<Button>();
                }

                if (m_ViewAttackerInfoButton == null || !m_ViewAttackerInfoButton.transform.IsChildOf(m_AttackRevealPanel.transform))
                {
                    var btn = m_AttackRevealPanel.transform.Find("ActionButtons/ViewAttackerInfoButton");
                    if (btn == null) btn = m_AttackRevealPanel.transform.Find("ViewAttackerInfoButton");
                    if (btn != null) m_ViewAttackerInfoButton = btn.GetComponent<Button>();
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

            // Attacker Telemetry Panel child controls
            if (m_AttackerTelemetryPanel == null && m_RevealWorldSpaceCanvas != null)
            {
                var panel = m_RevealWorldSpaceCanvas.transform.Find("AttackerTelemetryPanel");
                if (panel != null) m_AttackerTelemetryPanel = panel.gameObject;
            }

            if (m_AttackerTelemetryPanel != null)
            {
                if (m_ViewLessonLearnedButton == null || !m_ViewLessonLearnedButton.transform.IsChildOf(m_AttackerTelemetryPanel.transform))
                {
                    var btn = m_AttackerTelemetryPanel.transform.Find("ActionButtons/ViewLessonLearnedButton");
                    if (btn == null) btn = m_AttackerTelemetryPanel.transform.Find("ViewLessonLearnedButton");
                    if (btn != null) m_ViewLessonLearnedButton = btn.GetComponent<Button>();
                }

                if (m_BackFromAttackerInfoButton == null || !m_BackFromAttackerInfoButton.transform.IsChildOf(m_AttackerTelemetryPanel.transform))
                {
                    var btn = m_AttackerTelemetryPanel.transform.Find("ActionButtons/BackFromAttackerInfoButton");
                    if (btn == null) btn = m_AttackerTelemetryPanel.transform.Find("BackFromAttackerInfoButton");
                    if (btn != null) m_BackFromAttackerInfoButton = btn.GetComponent<Button>();
                }

                if (m_AttackerInfoReturnToLaptopButton == null || !m_AttackerInfoReturnToLaptopButton.transform.IsChildOf(m_AttackerTelemetryPanel.transform))
                {
                    var btn = m_AttackerTelemetryPanel.transform.Find("ActionButtons/ReturnToLaptopButton");
                    if (btn == null) btn = m_AttackerTelemetryPanel.transform.Find("ReturnToLaptopButton");
                    if (btn != null) m_AttackerInfoReturnToLaptopButton = btn.GetComponent<Button>();
                }
            }

            // Lesson Learned Panel child controls
            if (m_LessonLearnedPanel == null && m_RevealWorldSpaceCanvas != null)
            {
                var panel = m_RevealWorldSpaceCanvas.transform.Find("LessonLearnedPanel");
                if (panel != null) m_LessonLearnedPanel = panel.gameObject;
            }

            if (m_LessonLearnedPanel != null)
            {
                if (m_BackFromLessonLearnedButton == null || !m_BackFromLessonLearnedButton.transform.IsChildOf(m_LessonLearnedPanel.transform))
                {
                    var btn = m_LessonLearnedPanel.transform.Find("ActionButtons/BackFromLessonLearnedButton");
                    if (btn == null) btn = m_LessonLearnedPanel.transform.Find("BackFromLessonLearnedButton");
                    if (btn != null) m_BackFromLessonLearnedButton = btn.GetComponent<Button>();
                }

                if (m_LessonLearnedReturnToLaptopButton == null || !m_LessonLearnedReturnToLaptopButton.transform.IsChildOf(m_LessonLearnedPanel.transform))
                {
                    var btn = m_LessonLearnedPanel.transform.Find("ActionButtons/ReturnToLaptopButton");
                    if (btn == null) btn = m_LessonLearnedPanel.transform.Find("ReturnToLaptopButton");
                    if (btn != null) m_LessonLearnedReturnToLaptopButton = btn.GetComponent<Button>();
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

            if (m_ReturnToLaptopButton != null)
            {
                m_ReturnToLaptopButton.onClick.RemoveListener(OnReturnToLaptopClicked);
                m_ReturnToLaptopButton.onClick.AddListener(OnReturnToLaptopClicked);
            }

            if (m_ViewAttackerInfoButton != null)
            {
                m_ViewAttackerInfoButton.onClick.RemoveListener(OnViewAttackerInfoClicked);
                m_ViewAttackerInfoButton.onClick.AddListener(OnViewAttackerInfoClicked);
            }

            if (m_ViewLessonLearnedButton != null)
            {
                m_ViewLessonLearnedButton.onClick.RemoveListener(OnViewLessonLearnedClicked);
                m_ViewLessonLearnedButton.onClick.AddListener(OnViewLessonLearnedClicked);
            }

            if (m_BackFromAttackerInfoButton != null)
            {
                m_BackFromAttackerInfoButton.onClick.RemoveListener(OnBackFromAttackerInfoClicked);
                m_BackFromAttackerInfoButton.onClick.AddListener(OnBackFromAttackerInfoClicked);
            }

            if (m_AttackerInfoReturnToLaptopButton != null)
            {
                m_AttackerInfoReturnToLaptopButton.onClick.RemoveListener(OnReturnToLaptopClicked);
                m_AttackerInfoReturnToLaptopButton.onClick.AddListener(OnReturnToLaptopClicked);
            }

            if (m_BackFromLessonLearnedButton != null)
            {
                m_BackFromLessonLearnedButton.onClick.RemoveListener(OnBackFromLessonLearnedClicked);
                m_BackFromLessonLearnedButton.onClick.AddListener(OnBackFromLessonLearnedClicked);
            }

            if (m_LessonLearnedReturnToLaptopButton != null)
            {
                m_LessonLearnedReturnToLaptopButton.onClick.RemoveListener(OnReturnToLaptopClicked);
                m_LessonLearnedReturnToLaptopButton.onClick.AddListener(OnReturnToLaptopClicked);
            }

            if (m_CloseRevealButton != null)
            {
                m_CloseRevealButton.onClick.RemoveListener(OnReturnToLaptopClicked);
                m_CloseRevealButton.onClick.AddListener(OnReturnToLaptopClicked);
            }
        }

        public void RemoveButtonListeners()
        {
            if (m_ConnectButton != null) m_ConnectButton.onClick.RemoveListener(OnConnectClicked);
            if (m_FillDemoButton != null) m_FillDemoButton.onClick.RemoveListener(OnFillDemoClicked);
            if (m_ReturnToLaptopButton != null) m_ReturnToLaptopButton.onClick.RemoveListener(OnReturnToLaptopClicked);
            if (m_ViewAttackerInfoButton != null) m_ViewAttackerInfoButton.onClick.RemoveListener(OnViewAttackerInfoClicked);
            if (m_ViewLessonLearnedButton != null) m_ViewLessonLearnedButton.onClick.RemoveListener(OnViewLessonLearnedClicked);
            if (m_BackFromAttackerInfoButton != null) m_BackFromAttackerInfoButton.onClick.RemoveListener(OnBackFromAttackerInfoClicked);
            if (m_AttackerInfoReturnToLaptopButton != null) m_AttackerInfoReturnToLaptopButton.onClick.RemoveListener(OnReturnToLaptopClicked);
            if (m_BackFromLessonLearnedButton != null) m_BackFromLessonLearnedButton.onClick.RemoveListener(OnBackFromLessonLearnedClicked);
            if (m_LessonLearnedReturnToLaptopButton != null) m_LessonLearnedReturnToLaptopButton.onClick.RemoveListener(OnReturnToLaptopClicked);
            if (m_CloseRevealButton != null) m_CloseRevealButton.onClick.RemoveListener(OnReturnToLaptopClicked);
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
            if (m_AttackerTelemetryPanel != null) m_AttackerTelemetryPanel.SetActive(false);
            if (m_LessonLearnedPanel != null) m_LessonLearnedPanel.SetActive(false);
            if (m_WifiConnectedSuccessPanel != null) m_WifiConnectedSuccessPanel.SetActive(false);
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

            // Transition from CaptivePortalPanel to the dedicated Wi-Fi Connected Success page
            ShowWifiConnectedSuccess();

            Debug.Log("[CaptivePortalController] Wi-Fi connection succeeded. Showing confirmation page before attack reveal...");
            yield return new WaitForSeconds(m_SuccessDisplayDuration);

            IsSimulatingAuth = false;
            ShowAttackReveal();
        }

        /// <summary>
        /// Displays the dedicated "Wi-Fi Connected Successfully" browser page/state.
        /// Shown immediately after CONNECT authentication succeeds, before the Attack Reveal Warning.
        /// </summary>
        public void ShowWifiConnectedSuccess()
        {
            FindReferencesIfNull();

            if (m_CaptivePortalPanel != null) m_CaptivePortalPanel.SetActive(false);
            if (m_InitialPortalCard != null) m_InitialPortalCard.SetActive(false);
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(false);

            if (m_WifiConnectedSuccessPanel != null)
            {
                m_WifiConnectedSuccessPanel.SetActive(true);
            }

            // Update browser chrome
            if (m_UrlText != null)
            {
                m_UrlText.text = WIFI_CONNECTED_URL;
            }

            if (m_TabTitleText != null)
            {
                m_TabTitleText.text = "Wi-Fi Connected";
            }

            Debug.Log($"[CaptivePortalController] Displayed 'Wi-Fi Connected Successfully' browser page ({WIFI_CONNECTED_URL}).");
        }

        /// <summary>
        /// Dramatically transitions to the security-awareness warning panel.
        /// </summary>
        public void ShowAttackReveal()
        {
            FindReferencesIfNull();

            IsRevealPresentationActive = true;

            if (m_WifiConnectedSuccessPanel != null)
            {
                m_WifiConnectedSuccessPanel.SetActive(false);
            }

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
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(true);
            if (m_AttackerTelemetryPanel != null) m_AttackerTelemetryPanel.SetActive(false);
            if (m_LessonLearnedPanel != null) m_LessonLearnedPanel.SetActive(false);

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
                m_TabTitleText.text = "Security Warning - Fake AP";
            }

            // Keep scene / Edit Mode text intact so objects look identical in Play Mode as in Edit Mode
            if (m_WarningTitleText != null)
            {
                if (string.IsNullOrEmpty(m_WarningTitleText.text))
                {
                    m_WarningTitleText.text = "WARNING";
                }
                else if (m_WarningTitleText.text.Contains("⚠"))
                {
                    m_WarningTitleText.text = m_WarningTitleText.text.Replace("⚠", "").TrimStart();
                }
            }

            if (m_TrappedHeadlineText != null && string.IsNullOrEmpty(m_TrappedHeadlineText.text))
            {
                m_TrappedHeadlineText.text = "<color=#FF3B30>YOU WERE TRAPPED</color>";
            }

            if (m_ExplanationBodyText != null && string.IsNullOrEmpty(m_ExplanationBodyText.text))
            {
                m_ExplanationBodyText.text =
                    "<b>This was NOT the legitimate Jakko-Cooffee Wi-Fi.</b>\n\n" +
                    "You connected to a <b>simulated fake access point</b>.\n" +
                    "The information you entered was intercepted.\n\n" +
                    "<size=80%><color=#8E8E93><i>Because this is an educational simulation, no real credentials were transmitted or stored.</i></color></size>";
            }

            EnsureCollidersAndInteractables();
            Debug.Log("[CaptivePortalController] Attack Reveal Panel displayed: YOU WERE TRAPPED!");
        }

        /// <summary>
        /// Navigates to the Attacker Telemetry Panel detailing intercepted victim data (Rogue Wi-Fi).
        /// </summary>
        public void OnViewAttackerInfoClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(false);
            if (m_LessonLearnedPanel != null) m_LessonLearnedPanel.SetActive(false);
            if (m_AttackerTelemetryPanel != null) m_AttackerTelemetryPanel.SetActive(true);

            EnsureCollidersAndInteractables();
            Debug.Log("[CaptivePortalController] Navigated to Attacker Telemetry Panel.");
        }

        /// <summary>
        /// Returns from Attacker Telemetry Panel back to the main Attack Reveal Warning panel.
        /// </summary>
        public void OnBackFromAttackerInfoClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            if (m_AttackerTelemetryPanel != null) m_AttackerTelemetryPanel.SetActive(false);
            if (m_LessonLearnedPanel != null) m_LessonLearnedPanel.SetActive(false);
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(true);

            EnsureCollidersAndInteractables();
            Debug.Log("[CaptivePortalController] Returned to Attack Reveal Warning Panel.");
        }

        /// <summary>
        /// Navigates to the Lesson Learned Panel detailing prevention steps.
        /// </summary>
        public void OnViewLessonLearnedClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(false);
            if (m_AttackerTelemetryPanel != null) m_AttackerTelemetryPanel.SetActive(false);
            if (m_LessonLearnedPanel != null) m_LessonLearnedPanel.SetActive(true);

            EnsureCollidersAndInteractables();
            Debug.Log("[CaptivePortalController] Navigated to Lesson Learned Panel.");
        }

        /// <summary>
        /// Returns from Lesson Learned Panel back to Attacker Telemetry Panel.
        /// </summary>
        public void OnBackFromLessonLearnedClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            if (m_LessonLearnedPanel != null) m_LessonLearnedPanel.SetActive(false);
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(false);
            if (m_AttackerTelemetryPanel != null) m_AttackerTelemetryPanel.SetActive(true);

            EnsureCollidersAndInteractables();
            Debug.Log("[CaptivePortalController] Returned to Attacker Telemetry Panel.");
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
            IsSimulatingAuth = false;

            if (m_RevealWorldSpaceCanvas != null) m_RevealWorldSpaceCanvas.SetActive(false);
            if (m_WifiConnectedSuccessPanel != null) m_WifiConnectedSuccessPanel.SetActive(false);
            if (m_CaptivePortalPanel != null) m_CaptivePortalPanel.SetActive(false);
            if (m_AttackRevealPanel != null) m_AttackRevealPanel.SetActive(false);
            if (m_AttackerTelemetryPanel != null) m_AttackerTelemetryPanel.SetActive(false);
            if (m_LessonLearnedPanel != null) m_LessonLearnedPanel.SetActive(false);
            if (m_AttackerLogCard != null) m_AttackerLogCard.SetActive(false);
            if (m_InitialPortalCard != null) m_InitialPortalCard.SetActive(true);

            if (m_UrlText != null) m_UrlText.text = DEFAULT_PORTAL_URL;
            if (m_TabTitleText != null) m_TabTitleText.text = "Jakko Coffee - Free Wi-Fi Portal";

            if (m_ConnectButton != null) m_ConnectButton.interactable = true;
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

            if (m_ReturnToLaptopButton != null)
            {
                SetupElementColliderAndInteractable(m_ReturnToLaptopButton.gameObject, OnReturnToLaptopClicked);
            }

            if (m_ViewAttackerInfoButton != null)
            {
                SetupElementColliderAndInteractable(m_ViewAttackerInfoButton.gameObject, OnViewAttackerInfoClicked);
            }

            if (m_BackFromAttackerInfoButton != null)
            {
                SetupElementColliderAndInteractable(m_BackFromAttackerInfoButton.gameObject, OnBackFromAttackerInfoClicked);
            }

            if (m_AttackerInfoReturnToLaptopButton != null)
            {
                SetupElementColliderAndInteractable(m_AttackerInfoReturnToLaptopButton.gameObject, OnReturnToLaptopClicked);
            }

            if (m_ViewLessonLearnedButton != null)
            {
                SetupElementColliderAndInteractable(m_ViewLessonLearnedButton.gameObject, OnViewLessonLearnedClicked);
            }

            if (m_BackFromLessonLearnedButton != null)
            {
                SetupElementColliderAndInteractable(m_BackFromLessonLearnedButton.gameObject, OnBackFromLessonLearnedClicked);
            }

            if (m_LessonLearnedReturnToLaptopButton != null)
            {
                SetupElementColliderAndInteractable(m_LessonLearnedReturnToLaptopButton.gameObject, OnReturnToLaptopClicked);
            }

            if (m_CloseRevealButton != null)
            {
                SetupElementColliderAndInteractable(m_CloseRevealButton.gameObject, OnReturnToLaptopClicked);
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

            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return false;

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

            if (IsHitOnTarget(hitObj, m_ViewAttackerInfoButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnViewAttackerInfoClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_BackFromAttackerInfoButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnBackFromAttackerInfoClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_AttackerInfoReturnToLaptopButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnReturnToLaptopClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_ViewLessonLearnedButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnViewLessonLearnedClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_BackFromLessonLearnedButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnBackFromLessonLearnedClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_LessonLearnedReturnToLaptopButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnReturnToLaptopClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_ReturnToLaptopButton?.gameObject) ||
                IsHitOnTarget(hitObj, m_CloseRevealButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                OnReturnToLaptopClicked();
                return true;
            }

            return false;
        }

        private bool IsHitOnTarget(GameObject hitObj, GameObject target)
        {
            if (hitObj == null || target == null) return false;
            return hitObj == target || hitObj.transform.IsChildOf(target.transform);
        }

        private void EnsureWifiConnectedSuccessPanel(Transform pageContent)
        {
            if (m_WifiConnectedSuccessPanel != null || pageContent == null) return;

            Transform existing = pageContent.Find("WifiConnectedSuccessPanel");
            if (existing != null)
            {
                m_WifiConnectedSuccessPanel = existing.gameObject;
                return;
            }

            var panelGo = new GameObject("WifiConnectedSuccessPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelGo.transform.SetParent(pageContent, false);
            var rt = panelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 49f);
            rt.sizeDelta = new Vector2(800f, 780f);
            rt.localScale = Vector3.one;

            var img = panelGo.GetComponent<Image>();
            if (m_CaptivePortalPanel != null)
            {
                var cpImg = m_CaptivePortalPanel.GetComponent<Image>();
                if (cpImg != null)
                {
                    img.sprite = cpImg.sprite;
                    img.type = cpImg.type;
                    img.color = cpImg.color;
                }
            }
            else
            {
                img.color = new Color(0.14f, 0.13f, 0.13f, 0.96f);
            }

            TMP_FontAsset font = null;
            if (m_PortalStatusText != null) font = m_PortalStatusText.font;

            // 1. Header Section Pill
            CreateLabelElement("HeaderSection", panelGo.transform, new Vector2(0f, 315f), new Vector2(700f, 42f),
                "<color=#8E8E93><b>WI-FI CONNECTION STATUS</b></color>", 16, TextAlignmentOptions.Center, font);

            // 2. Big Green Checkmark
            CreateLabelElement("SuccessIcon", panelGo.transform, new Vector2(0f, 225f), new Vector2(160f, 80f),
                "<color=#34C759><b>✓</b></color>", 68, TextAlignmentOptions.Center, font);

            // 3. Main Title
            CreateLabelElement("TitleText", panelGo.transform, new Vector2(0f, 150f), new Vector2(700f, 48f),
                "<b>Wi-Fi Connected</b>", 30, TextAlignmentOptions.Center, font);

            // 4. Success Badge Pill
            CreateLabelElement("SuccessBadge", panelGo.transform, new Vector2(0f, 92f), new Vector2(400f, 44f),
                "<color=#34C759><b>✓ Connected successfully</b></color>", 20, TextAlignmentOptions.Center, font);

            // 5. Subtitle
            CreateLabelElement("SubtitleText", panelGo.transform, new Vector2(0f, 38f), new Vector2(700f, 36f),
                "<color=#D1D1D6>Your device is now connected to Wi-Fi.</color>", 18, TextAlignmentOptions.Center, font);

            // 6. Network Details Card
            var detailsGo = new GameObject("DetailsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            detailsGo.transform.SetParent(panelGo.transform, false);
            var detRt = detailsGo.GetComponent<RectTransform>();
            detRt.anchorMin = new Vector2(0.5f, 0.5f);
            detRt.anchorMax = new Vector2(0.5f, 0.5f);
            detRt.anchoredPosition = new Vector2(0f, -100f);
            detRt.sizeDelta = new Vector2(700f, 200f);
            var detImg = detailsGo.GetComponent<Image>();
            detImg.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);

            CreateLabelElement("NetworkInfo", detailsGo.transform, new Vector2(0f, 0f), new Vector2(640f, 170f),
                "<b>Network:</b>  Jakko-cooffee_free\n" +
                "<b>Security:</b>  Captive Portal Authentication\n" +
                "<b>IP Address:</b>  192.168.1.142\n" +
                "<b>Status:</b>  <color=#34C759>Active (Internet Access)</color>",
                16, TextAlignmentOptions.Left, font);

            // 7. Notice Text
            CreateLabelElement("NoticeText", panelGo.transform, new Vector2(0f, -250f), new Vector2(700f, 32f),
                "<size=15><color=#8E8E93>You have full access to the internet through this hotspot.</color></size>",
                15, TextAlignmentOptions.Center, font);

            panelGo.SetActive(false);
            m_WifiConnectedSuccessPanel = panelGo;
        }

        private static GameObject CreateLabelElement(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, TextAlignmentOptions align, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.text = text;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return go;
        }
    }
}
