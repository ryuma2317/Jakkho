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
                m_BackButton.onClick.RemoveListener(CloseBrowser);
                m_BackButton.onClick.AddListener(CloseBrowser);
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
            if (m_BackButton != null) m_BackButton.onClick.RemoveListener(CloseBrowser);
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
        /// Closes the browser and returns to the previous laptop / network interface.
        /// </summary>
        public void CloseBrowser()
        {
            FindReferencesIfNull();

            if (m_BrowserPanel != null)
            {
                m_BrowserPanel.SetActive(false);
            }

            // Restore the EnlargedWifiPanel flyout
            if (m_EnlargedWifiPanel != null)
            {
                m_EnlargedWifiPanel.SetActive(true);
            }

            OnBrowserClosed?.Invoke();
            Debug.Log("[JakkoFreeWifiBrowserController] Browser CLOSED. Returned to laptop network interface.");
        }

        private void OnRefreshClicked()
        {
            if (m_StatusFeedbackText != null)
            {
                m_StatusFeedbackText.text = "<color=#999999>Page refreshed. Please choose a login option.</color>";
            }
            Debug.Log("[JakkoFreeWifiBrowserController] Browser refreshed.");
        }

        /// <summary>
        /// Handles Google Sign-In button interaction.
        /// Provides clean visual feedback and invokes modular event for future authentication backend.
        /// </summary>
        public void OnGoogleLoginClicked()
        {
            if (Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            if (m_StatusFeedbackText != null)
            {
                m_StatusFeedbackText.text = "<color=#4CD964><b>✓ Google Authentication Successful!</b></color>\n<size=80%>Wi-Fi access granted for 2 hours. Enjoy your stay at Jakko Coffee!</size>";
            }

            OnGoogleLogin?.Invoke();
            Debug.Log("[JakkoFreeWifiBrowserController] 'Continue with Google' clicked. Modular login event invoked.");
        }

        /// <summary>
        /// Handles Facebook Sign-In button interaction.
        /// Provides clean visual feedback and invokes modular event for future authentication backend.
        /// </summary>
        public void OnFacebookLoginClicked()
        {
            if (Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            if (m_StatusFeedbackText != null)
            {
                m_StatusFeedbackText.text = "<color=#1877F2><b>✓ Facebook Authentication Successful!</b></color>\n<size=80%>Wi-Fi access granted for 2 hours. Enjoy your stay at Jakko Coffee!</size>";
            }

            OnFacebookLogin?.Invoke();
            Debug.Log("[JakkoFreeWifiBrowserController] 'Continue with Facebook' clicked. Modular login event invoked.");
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
                SetupElementColliderAndInteractable(m_BackButton.gameObject, CloseBrowser);
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
            if (!Input.GetMouseButtonDown(0)) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // 1. Mouse cursor raycast
            Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
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
                CloseBrowser();
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

            return false;
        }

        private bool IsHitOnTarget(GameObject hitObj, GameObject target)
        {
            if (hitObj == null || target == null) return false;
            return hitObj == target || hitObj.transform.IsChildOf(target.transform);
        }

        private void HandleKeyboardShortcuts()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseBrowser();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseBrowser();
                return;
            }

            // Quick shortcuts for desktop testing
            if (Input.GetKeyDown(KeyCode.G))
            {
                OnGoogleLoginClicked();
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
                OnFacebookLoginClicked();
            }
        }
    }
}
