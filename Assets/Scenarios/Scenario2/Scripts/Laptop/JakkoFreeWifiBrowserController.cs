using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TMPro;

namespace Kannko.Laptop
{
    /// <summary>
    /// Controls the full-screen web browser panel on the laptop screen (FloatingLaptopPanel -> ScreenContainer).
    /// Displays the "Jakko Coffee Free Wi-Fi" captive portal webpage when Item_Jakko-Free is selected.
    /// 
    /// Features:
    /// - Full-screen browser window covering the 1920x1080 laptop display.
    /// - Realistic browser chrome: Tab bar ("Jakko Coffee - Free Wi-Fi"), URL bar ("https://wifi.jakkocoffee.com/login"),
    ///   secure captive portal badge, and window controls (Close 'X', Back arrow).
    /// - Captive portal page content:
    ///   - Jakko Coffee branding and Wi-Fi logo.
    ///   - Welcome message and access requirements.
    ///   - "Continue with Google" modern login button.
    ///   - "Continue with Facebook" modern login button.
    ///   - Detailed guest Wi-Fi connection and access information.
    ///   - Interactive status feedback area.
    /// - Dual VR controller & desktop mouse raycast interaction.
    /// - Clean navigation back to the laptop network interface.
    /// </summary>
    [DisallowMultipleComponent]
    public class JakkoFreeWifiBrowserController : MonoBehaviour
    {
        [Header("Root Panels")]
        [Tooltip("The root browser panel GameObject on ScreenContainer.")]
        [SerializeField] private GameObject m_BrowserPanel;

        [Tooltip("Reference to the EnlargedWifiPanel to restore when browser closes.")]
        [SerializeField] private GameObject m_EnlargedWifiPanel;

        [Tooltip("The World Space FloatingLaptopPanel GameObject to hide upon victory debrief.")]
        [SerializeField] private GameObject m_FloatingLaptopPanel;

        [Tooltip("Reference to the Scenario 2 Lessons Learned Debrief panel.")]
        [SerializeField] private GameObject m_LessonLearnedPanel;

        [Header("Browser Chrome Navigation Controls")]
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private Button m_BackButton;
        [SerializeField] private Button m_RefreshButton;
        [SerializeField] private TextMeshProUGUI m_UrlText;
        [SerializeField] private TextMeshProUGUI m_TabTitleText;

        [Header("Webpage Portal Login Controls")]
        [SerializeField] private Button m_GoogleLoginButton;
        [SerializeField] private Button m_FacebookLoginButton;
        [SerializeField] private TextMeshProUGUI m_StatusFeedbackText;

        [Header("Captive Portal Information")]
        [SerializeField] private TextMeshProUGUI m_PortalTitleText;
        [SerializeField] private TextMeshProUGUI m_PortalSubtitleText;
        [SerializeField] private TextMeshProUGUI m_WelcomeText;
        [SerializeField] private TextMeshProUGUI m_AccessInfoText;

        [Header("Stop Playing Decision Panel")]
        [Tooltip("The 'STOP PLAYING?' overlay dialog displayed when returning via the Back button.")]
        [SerializeField] private GameObject m_StopPlayingDecisionPanel;
        [SerializeField] private Button m_ContinueScenarioButton;
        [SerializeField] private Button m_GetOutOfCafeButton;
        [SerializeField] private Scenario2.Scenario2ExitController m_ExitController;

        [Header("VR Controller Input Actions")]
        [SerializeField] private InputActionProperty m_LeftTriggerAction;
        [SerializeField] private InputActionProperty m_RightTriggerAction;

        [Header("Click Settings")]
        [SerializeField] private float m_ClickDebounceTime = 0.2f;

        [Header("Events (Modular Authentication Hooks)")]
        public UnityEvent OnBrowserOpened = new UnityEvent();
        public UnityEvent OnBrowserClosed = new UnityEvent();
        public UnityEvent OnGoogleLogin = new UnityEvent();
        public UnityEvent OnFacebookLogin = new UnityEvent();

        [Header("Captive Portal Simulation")]
        [SerializeField] private CaptivePortalController m_CaptivePortalController;

        // Runtime state
        private float m_LastClickTime = -1f;
        private readonly List<XRRayInteractor> m_ActiveRayInteractors = new List<XRRayInteractor>();

        public bool IsBrowserOpen => m_BrowserPanel != null && m_BrowserPanel.activeSelf;
        public GameObject BrowserPanel => m_BrowserPanel;
        public Button GoogleLoginButton => m_GoogleLoginButton;
        public Button FacebookLoginButton => m_FacebookLoginButton;
        public Button CloseButton => m_CloseButton;
        public Button BackButton => m_BackButton;
        public TextMeshProUGUI StatusFeedbackText => m_StatusFeedbackText;
        public CaptivePortalController CaptivePortal => m_CaptivePortalController;
        public GameObject StopPlayingDecisionPanel => m_StopPlayingDecisionPanel;
        public Button ContinueScenarioButton => m_ContinueScenarioButton;
        public Button GetOutOfCafeButton => m_GetOutOfCafeButton;
        public Scenario2.Scenario2ExitController ExitController => m_ExitController;

        private void Awake()
        {
            if (m_BrowserPanel == null)
            {
                m_BrowserPanel = gameObject;
            }

            FindReferencesIfNull();
            EnsureCollidersAndInteractables();
            InitializeInputActions();
            SetupButtonListeners();
        }

        private void OnEnable()
        {
            FindReferencesIfNull();
            EnsureCollidersAndInteractables();
            EnableInputActions();
            SetupButtonListeners();
        }

        private void OnDisable()
        {
            DisableInputActions();
            RemoveButtonListeners();
        }

        private void Start()
        {
            FindReferencesIfNull();
            EnsureCollidersAndInteractables();
            ConfigureRayInteractors();
        }

        private void Update()
        {
            if (!Application.isPlaying || !IsBrowserOpen) return;

            HandleVRControllerRayClicks();
            HandleDesktopMouseClicks();
            HandleKeyboardShortcuts();
        }

        public void FindReferencesIfNull()
        {
            if (m_BrowserPanel == null)
            {
                m_BrowserPanel = gameObject;
            }

            if (m_EnlargedWifiPanel == null && transform.parent != null)
            {
                Transform wifiTrans = transform.parent.Find("EnlargedWifiPanel");
                if (wifiTrans != null) m_EnlargedWifiPanel = wifiTrans.gameObject;
            }

            // Browser Header
            if (m_CloseButton == null)
            {
                Transform t = transform.Find("BrowserHeader/TabBar/WindowControls/CloseButton");
                if (t == null) t = transform.Find("BrowserHeader/WindowControls/CloseButton");
                if (t == null) t = transform.Find("BrowserHeader/CloseButton");
                if (t != null) m_CloseButton = t.GetComponent<Button>();
            }

            if (m_BackButton == null)
            {
                Transform t = transform.Find("BrowserHeader/NavBar/BackButton");
                if (t == null) t = transform.Find("BrowserHeader/BackButton");
                if (t != null) m_BackButton = t.GetComponent<Button>();
            }

            if (m_RefreshButton == null)
            {
                Transform t = transform.Find("BrowserHeader/NavBar/RefreshButton");
                if (t != null) m_RefreshButton = t.GetComponent<Button>();
            }

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

            // Portal Card Controls
            Transform card = transform.Find("PageContent/PortalCard");
            if (card != null)
            {
                if (m_GoogleLoginButton == null)
                {
                    Transform t = card.Find("ButtonContainer/GoogleLoginButton");
                    if (t != null) m_GoogleLoginButton = t.GetComponent<Button>();
                }

                if (m_FacebookLoginButton == null)
                {
                    Transform t = card.Find("ButtonContainer/FacebookLoginButton");
                    if (t != null) m_FacebookLoginButton = t.GetComponent<Button>();
                }

                if (m_StatusFeedbackText == null)
                {
                    Transform t = card.Find("StatusFeedbackText");
                    if (t != null) m_StatusFeedbackText = t.GetComponent<TextMeshProUGUI>();
                }

                if (m_PortalTitleText == null)
                {
                    Transform t = card.Find("HeaderSection/PortalTitle");
                    if (t != null) m_PortalTitleText = t.GetComponent<TextMeshProUGUI>();
                }

                if (m_PortalSubtitleText == null)
                {
                    Transform t = card.Find("HeaderSection/PortalSubtitle");
                    if (t != null) m_PortalSubtitleText = t.GetComponent<TextMeshProUGUI>();
                }

                if (m_WelcomeText == null)
                {
                    Transform t = card.Find("WelcomeText");
                    if (t != null) m_WelcomeText = t.GetComponent<TextMeshProUGUI>();
                }

                if (m_AccessInfoText == null)
                {
                    Transform t = card.Find("AccessInfoCard/AccessInfoText");
                    if (t != null) m_AccessInfoText = t.GetComponent<TextMeshProUGUI>();
                }
            }

            if (m_CaptivePortalController == null)
            {
                m_CaptivePortalController = GetComponent<CaptivePortalController>();
                if (m_CaptivePortalController == null)
                {
                    m_CaptivePortalController = GetComponentInChildren<CaptivePortalController>(true);
                }
            }

            if (m_StopPlayingDecisionPanel == null && transform.parent != null)
            {
                Transform t = transform.parent.Find("StopPlayingDecisionPanel");
                if (t == null) t = transform.parent.Find("EnlargedWifiPanel/StopPlayingDecisionPanel");
                if (t != null) m_StopPlayingDecisionPanel = t.gameObject;
            }

            if (m_StopPlayingDecisionPanel != null)
            {
                if (m_ContinueScenarioButton == null)
                {
                    Transform t = m_StopPlayingDecisionPanel.transform.Find("ActionButtons/ContinueScenarioButton");
                    if (t == null) t = m_StopPlayingDecisionPanel.transform.Find("ContinueScenarioButton");
                    if (t != null) m_ContinueScenarioButton = t.GetComponent<Button>();
                }

                if (m_GetOutOfCafeButton == null)
                {
                    Transform t = m_StopPlayingDecisionPanel.transform.Find("ActionButtons/GetOutOfCafeButton");
                    if (t == null) t = m_StopPlayingDecisionPanel.transform.Find("GetOutOfCafeButton");
                    if (t != null) m_GetOutOfCafeButton = t.GetComponent<Button>();
                }
            }

            if (m_ExitController == null)
            {
                m_ExitController = UnityEngine.Object.FindFirstObjectByType<Scenario2.Scenario2ExitController>(FindObjectsInactive.Include);
            }

            if (m_FloatingLaptopPanel == null)
            {
                Transform p = transform;
                while (p != null && p.name != "FloatingLaptopPanel") p = p.parent;
                if (p != null) m_FloatingLaptopPanel = p.gameObject;
                if (m_FloatingLaptopPanel == null)
                {
                    var wui = GameObject.Find("WorldUI");
                    if (wui != null)
                    {
                        var t = wui.transform.Find("FloatingLaptopPanel");
                        if (t != null) m_FloatingLaptopPanel = t.gameObject;
                    }
                }
            }

            if (m_LessonLearnedPanel == null)
            {
                var wui = GameObject.Find("WorldUI");
                if (wui != null)
                {
                    var t = wui.transform.Find("Scenario2_Lesson_Learned_Panel");
                    if (t != null) m_LessonLearnedPanel = t.gameObject;
                }
                if (m_LessonLearnedPanel == null)
                {
                    m_LessonLearnedPanel = GameObject.Find("Scenario2_Lesson_Learned_Panel");
                }
            }
        }

        public void SetupButtonListeners()
        {
            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.RemoveListener(CloseBrowser);
                m_CloseButton.onClick.AddListener(CloseBrowser);
            }

            if (m_BackButton != null)
            {
                m_BackButton.onClick.RemoveListener(OnBackButtonClicked);
                m_BackButton.onClick.AddListener(OnBackButtonClicked);
            }

            if (m_ContinueScenarioButton != null)
            {
                m_ContinueScenarioButton.onClick.RemoveListener(OnContinueScenarioClicked);
                m_ContinueScenarioButton.onClick.AddListener(OnContinueScenarioClicked);
            }

            if (m_GetOutOfCafeButton != null)
            {
                m_GetOutOfCafeButton.onClick.RemoveListener(OnGetOutOfCafeClicked);
                m_GetOutOfCafeButton.onClick.AddListener(OnGetOutOfCafeClicked);
            }

            if (m_RefreshButton != null)
            {
                m_RefreshButton.onClick.RemoveListener(OnRefreshClicked);
                m_RefreshButton.onClick.AddListener(OnRefreshClicked);
            }

            if (m_GoogleLoginButton != null)
            {
                m_GoogleLoginButton.onClick.RemoveListener(OnGoogleLoginClicked);
                m_GoogleLoginButton.onClick.AddListener(OnGoogleLoginClicked);
            }

            if (m_FacebookLoginButton != null)
            {
                m_FacebookLoginButton.onClick.RemoveListener(OnFacebookLoginClicked);
                m_FacebookLoginButton.onClick.AddListener(OnFacebookLoginClicked);
            }
        }

        public void RemoveButtonListeners()
        {
            if (m_CloseButton != null) m_CloseButton.onClick.RemoveListener(CloseBrowser);
            if (m_BackButton != null) m_BackButton.onClick.RemoveListener(OnBackButtonClicked);
            if (m_ContinueScenarioButton != null) m_ContinueScenarioButton.onClick.RemoveListener(OnContinueScenarioClicked);
            if (m_GetOutOfCafeButton != null) m_GetOutOfCafeButton.onClick.RemoveListener(OnGetOutOfCafeClicked);
            if (m_RefreshButton != null) m_RefreshButton.onClick.RemoveListener(OnRefreshClicked);
            if (m_GoogleLoginButton != null) m_GoogleLoginButton.onClick.RemoveListener(OnGoogleLoginClicked);
            if (m_FacebookLoginButton != null) m_FacebookLoginButton.onClick.RemoveListener(OnFacebookLoginClicked);
        }

        /// <summary>
        /// Opens the full-screen browser on the laptop display.
        /// Hides the network list flyout to let the browser occupy the entire screen.
        /// </summary>
        public void OpenBrowser()
        {
            FindReferencesIfNull();

            if (m_BrowserPanel != null)
            {
                m_BrowserPanel.SetActive(true);
            }

            // Hide the EnlargedWifiPanel flyout so browser fills display cleanly
            if (m_EnlargedWifiPanel != null)
            {
                m_EnlargedWifiPanel.SetActive(false);
            }

            // Reset status text
            if (m_StatusFeedbackText != null)
            {
                m_StatusFeedbackText.text = "";
            }

            EnsureCollidersAndInteractables();
            OnBrowserOpened?.Invoke();
            Debug.Log("[JakkoFreeWifiBrowserController] Full-screen Jakko Coffee Free Wi-Fi browser OPENED on laptop screen.");
        }

        /// <summary>
        /// Closes the browser when the player decides to leave the rogue captive portal.
        /// Concludes Scenario 2 with VICTORY: The player spotted the Evil Twin trap,
        /// refused to submit credentials, and left the browser.
        /// Displays the Incident Debrief & Lessons Learned panel with the success outcome.
        /// </summary>
        public void CloseBrowser()
        {
            FindReferencesIfNull();

            if (m_CaptivePortalController != null)
            {
                m_CaptivePortalController.ResetToInitialPortal();
            }

            if (m_BrowserPanel != null)
            {
                m_BrowserPanel.SetActive(false);
            }

            if (m_StopPlayingDecisionPanel != null)
            {
                m_StopPlayingDecisionPanel.SetActive(false);
            }

            // Hide the FloatingLaptopPanel so the player's focus is fully on the 3D Lessons Learned Panel
            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(false);
            }

            // Present the Scenario 2 Incident Debrief & Lessons Learned Panel
            if (m_LessonLearnedPanel != null)
            {
                m_LessonLearnedPanel.SetActive(true);
                var ctrl = m_LessonLearnedPanel.GetComponent<Scenario2.Scenario2_LessonLearnedController>();
                if (ctrl != null)
                {
                    ctrl.ShowLesson(0);
                }
                Debug.Log("<color=#34D399><b>[Scenario 2]</b> Player closed rogue captive portal browser — VICTORY! Lessons Learned panel presented.</color>");
            }
            else
            {
                Debug.LogWarning("[JakkoFreeWifiBrowserController] Scenario2_Lesson_Learned_Panel not found! Restoring network list.");
                if (m_EnlargedWifiPanel != null)
                {
                    m_EnlargedWifiPanel.SetActive(true);
                }
            }

            // Record score and victory
            if (ScoreTracker.Instance != null)
            {
                ScoreTracker.Instance.RecordOutcome(ScenarioManager.OutcomeType.Success);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteScenario(200);
            }

            OnBrowserClosed?.Invoke();
        }

        /// <summary>
        /// Handles the browser navbar BackButton click.
        /// Player decides to leave the rogue captive portal — triggers VICTORY and presents Lessons Learned.
        /// </summary>
        public void OnBackButtonClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            Debug.Log("[JakkoFreeWifiBrowserController] Back button clicked: Player leaving rogue captive portal — VICTORY!");
            CloseBrowser();
        }

        public void ShowStopPlayingDecision()
        {
            FindReferencesIfNull();

            if (m_StopPlayingDecisionPanel != null)
            {
                m_StopPlayingDecisionPanel.SetActive(true);
            }

            EnsureCollidersAndInteractables();
            Debug.Log("[JakkoFreeWifiBrowserController] Displayed 'STOP PLAYING?' decision panel on laptop screen.");
        }

        public void HideStopPlayingDecision()
        {
            if (m_StopPlayingDecisionPanel != null)
            {
                m_StopPlayingDecisionPanel.SetActive(false);
            }
        }

        public void OnContinueScenarioClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            Debug.Log("[JakkoFreeWifiBrowserController] [ Continue Scenario ] clicked: Resuming laptop Wi-Fi interaction.");
            HideStopPlayingDecision();
        }

        public void OnGetOutOfCafeClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            Debug.Log("[JakkoFreeWifiBrowserController] [ Get Out of This Cafe ] clicked: Player leaving rogue portal — VICTORY!");
            CloseBrowser();
        }

        private void OnRefreshClicked()
        {
            if (m_CaptivePortalController != null)
            {
                m_CaptivePortalController.ResetToInitialPortal();
            }

            if (m_StatusFeedbackText != null)
            {
                m_StatusFeedbackText.text = "<color=#999999>Page refreshed. Please choose a login option.</color>";
            }
            Debug.Log("[JakkoFreeWifiBrowserController] Browser refreshed.");
        }

        /// <summary>
        /// Handles Google Sign-In button interaction.
        /// Launches the educational captive-portal security-awareness simulation.
        /// </summary>
        public void OnGoogleLoginClicked()
        {
            if (Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            if (m_StatusFeedbackText != null)
            {
                m_StatusFeedbackText.text = "<color=#4CD964><b>Redirecting to Captive Portal...</b></color>";
            }

            OnGoogleLogin?.Invoke();
            Debug.Log("[JakkoFreeWifiBrowserController] 'Continue with Google' clicked. Launching captive portal simulation.");

            if (m_CaptivePortalController != null)
            {
                m_CaptivePortalController.ShowCaptivePortal("Google");
            }
        }

        /// <summary>
        /// Handles Facebook Sign-In button interaction.
        /// Launches the educational captive-portal security-awareness simulation.
        /// </summary>
        public void OnFacebookLoginClicked()
        {
            if (Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            if (m_StatusFeedbackText != null)
            {
                m_StatusFeedbackText.text = "<color=#1877F2><b>Redirecting to Captive Portal...</b></color>";
            }

            OnFacebookLogin?.Invoke();
            Debug.Log("[JakkoFreeWifiBrowserController] 'Continue with Facebook' clicked. Launching captive portal simulation.");

            if (m_CaptivePortalController != null)
            {
                m_CaptivePortalController.ShowCaptivePortal("Facebook");
            }
        }

        /// <summary>
        /// Equips 3D BoxColliders and XRSimpleInteractables on all interactive buttons in the browser.
        /// </summary>
        public void EnsureCollidersAndInteractables()
        {
            FindReferencesIfNull();

            if (m_CloseButton != null)
            {
                SetupElementColliderAndInteractable(m_CloseButton.gameObject, CloseBrowser);
            }

            if (m_BackButton != null)
            {
                SetupElementColliderAndInteractable(m_BackButton.gameObject, OnBackButtonClicked);
            }

            if (m_ContinueScenarioButton != null)
            {
                SetupElementColliderAndInteractable(m_ContinueScenarioButton.gameObject, OnContinueScenarioClicked);
            }

            if (m_GetOutOfCafeButton != null)
            {
                SetupElementColliderAndInteractable(m_GetOutOfCafeButton.gameObject, OnGetOutOfCafeClicked);
            }

            if (m_RefreshButton != null)
            {
                SetupElementColliderAndInteractable(m_RefreshButton.gameObject, OnRefreshClicked);
            }

            if (m_GoogleLoginButton != null)
            {
                SetupElementColliderAndInteractable(m_GoogleLoginButton.gameObject, OnGoogleLoginClicked);
            }

            if (m_FacebookLoginButton != null)
            {
                SetupElementColliderAndInteractable(m_FacebookLoginButton.gameObject, OnFacebookLoginClicked);
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

        private void InitializeInputActions()
        {
            if (m_LeftTriggerAction.action == null)
            {
                var leftAction = new InputAction("LeftTriggerBrowser", InputActionType.Button, "<XRController>{LeftHand}/triggerPressed");
                leftAction.AddBinding("<XRController>{LeftHand}/primaryButton");
                m_LeftTriggerAction = new InputActionProperty(leftAction);
            }

            if (m_RightTriggerAction.action == null)
            {
                var rightAction = new InputAction("RightTriggerBrowser", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
                rightAction.AddBinding("<XRController>{RightHand}/primaryButton");
                m_RightTriggerAction = new InputActionProperty(rightAction);
            }
        }

        private void EnableInputActions()
        {
            try
            {
                m_LeftTriggerAction.action?.Enable();
                m_RightTriggerAction.action?.Enable();
            }
            catch { }
        }

        private void DisableInputActions()
        {
            try
            {
                m_LeftTriggerAction.action?.Disable();
                m_RightTriggerAction.action?.Disable();
            }
            catch { }
        }

        public void ConfigureRayInteractors()
        {
            m_ActiveRayInteractors.Clear();
            var rays = UnityEngine.Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < rays.Length; i++)
            {
                var ray = rays[i];
                if (ray != null)
                {
                    ray.allowHoveredActivate = true;
                    m_ActiveRayInteractors.Add(ray);
                }
            }
        }

        private void HandleVRControllerRayClicks()
        {
            bool leftTrigger = m_LeftTriggerAction.action != null && m_LeftTriggerAction.action.WasPressedThisFrame();
            bool rightTrigger = m_RightTriggerAction.action != null && m_RightTriggerAction.action.WasPressedThisFrame();

            if (!leftTrigger && !rightTrigger) return;

            if (m_ActiveRayInteractors.Count == 0)
            {
                ConfigureRayInteractors();
            }

            for (int i = 0; i < m_ActiveRayInteractors.Count; i++)
            {
                var ray = m_ActiveRayInteractors[i];
                if (ray == null || !ray.gameObject.activeInHierarchy) continue;

                if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    if (EvaluateHitElement(hit.collider.gameObject))
                    {
                        return;
                    }
                }

                Transform rayOrigin = ray.rayOriginTransform != null ? ray.rayOriginTransform : ray.transform;
                if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit physHit, 10f))
                {
                    if (EvaluateHitElement(physHit.collider.gameObject))
                    {
                        return;
                    }
                }
            }
        }

        private void HandleDesktopMouseClicks()
        {
            if (UnityEngine.InputSystem.Mouse.current == null ||
                !UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // 1. Mouse cursor raycast
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            Ray mouseRay = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, 10f))
            {
                if (EvaluateHitElement(mouseHit.collider.gameObject))
                {
                    return;
                }
            }

            // 2. Center-screen reticle raycast
            Ray centerRay = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            if (Physics.Raycast(centerRay, out RaycastHit centerHit, 10f))
            {
                if (EvaluateHitElement(centerHit.collider.gameObject))
                {
                    return;
                }
            }
        }

        private bool EvaluateHitElement(GameObject hitObj)
        {
            if (hitObj == null) return false;

            if (Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime)
            {
                return false;
            }

            if (IsHitOnTarget(hitObj, m_CloseButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                Debug.Log("[JakkoFreeWifiBrowserController] Hit detected on CloseButton.");
                CloseBrowser();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_BackButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                Debug.Log("[JakkoFreeWifiBrowserController] Hit detected on BackButton.");
                OnBackButtonClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_ContinueScenarioButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                Debug.Log("[JakkoFreeWifiBrowserController] Hit detected on ContinueScenarioButton.");
                OnContinueScenarioClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_GetOutOfCafeButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                Debug.Log("[JakkoFreeWifiBrowserController] Hit detected on GetOutOfCafeButton.");
                OnGetOutOfCafeClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_GoogleLoginButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                Debug.Log("[JakkoFreeWifiBrowserController] Hit detected on GoogleLoginButton.");
                OnGoogleLoginClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_FacebookLoginButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                Debug.Log("[JakkoFreeWifiBrowserController] Hit detected on FacebookLoginButton.");
                OnFacebookLoginClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_RefreshButton?.gameObject))
            {
                m_LastClickTime = Time.unscaledTime;
                Debug.Log("[JakkoFreeWifiBrowserController] Hit detected on RefreshButton.");
                OnRefreshClicked();
                return true;
            }

            if (m_CaptivePortalController != null && m_CaptivePortalController.DispatchHit(hitObj))
            {
                return true;
            }

            return false;
        }

        private bool IsHitOnTarget(GameObject hitObj, GameObject target)
        {
            if (hitObj == null || target == null) return false;
            return hitObj == target || hitObj.transform.IsChildOf(target.transform);
        }

        private void HandleKeyboardShortcuts()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                CloseBrowser();
                return;
            }

            // Quick shortcuts for desktop testing
            if (kb != null && kb.gKey.wasPressedThisFrame)
            {
                OnGoogleLoginClicked();
            }
            else if (kb != null && kb.fKey.wasPressedThisFrame)
            {
                OnFacebookLoginClicked();
            }
        }
    }
}
