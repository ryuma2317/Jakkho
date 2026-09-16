using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Kannko.Phone
{
    /// <summary>
    /// Represents the operational states of the handheld smartphone.
    /// </summary>
    public enum SmartphoneState
    {
        Physical,   // Normal physical prop, dark screen, resting or held
        ActiveOpen, // Awoken state, screen illuminated, held in hand
        Closing     // Brief transition state dimming screen before returning to Physical
    }

    /// <summary>
    /// Controls the physical smartphone state transitions:
    /// Physical -> Active/Open -> Closing -> Physical.
    /// 
    /// - Grabbing with VR controllers or desktop mouse only holds the phone physically.
    /// - Deliberate secondary interaction (VR index trigger / Desktop 'E' key while held)
    ///   activates/deactivates the phone.
    /// - Dropping or releasing the phone automatically returns it through Closing -> Physical.
    /// - In-world handheld object: cafe environment remains 100% visible at all times.
    /// - Provides clean events for the subsequent phone screen/UI feature.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class SmartphoneController : MonoBehaviour
    {
        [Header("Current State")]
        [SerializeField] private SmartphoneState m_CurrentState = SmartphoneState.Physical;

        [Header("Floating UI & Notification References")]
        [Tooltip("The standalone floating smartphone panel in WorldUI.")]
        [SerializeField] private GameObject m_FloatingSmartphonePanel;

        [Tooltip("The floating Telegram message popup above the phone.")]
        [SerializeField] private GameObject m_TelegramMessagePopup;

        [Header("Message State")]
        [Tooltip("Permanent read state for the Telegram message during the current session.")]
        [SerializeField] private bool m_IsMessageRead = false;

        [Header("Screen Visual Feedback")]
        [SerializeField] private MeshRenderer m_ScreenRenderer;
        [SerializeField] private Material m_ScreenSleepMaterial;
        [SerializeField] private Material m_ScreenActiveMaterial;

        [Header("Transition Timings")]
        [SerializeField] private float m_ClosingDuration = 0.25f;

        [Header("VR Controller Input Actions")]
        [SerializeField] private InputActionProperty m_LeftActivateAction;
        [SerializeField] private InputActionProperty m_RightActivateAction;

        // References
        private XRGrabInteractable m_GrabInteractable;
        private Rigidbody m_Rigidbody;
        private Coroutine m_ClosingCoroutine;
        private IXRSelectInteractor m_CurrentHoldingInteractor;

        public enum Handedness
        {
            Unknown,
            Left,
            Right
        }

        // Public Events (Hooks for UI / external systems)
        public event System.Action<SmartphoneState> OnStateChanged;
        public event System.Action OnPhoneOpened;
        public event System.Action OnPhoneClosed;
        public event System.Action OnMessageRead;
        public event System.Action OnFloatingPanelOpened;
        public event System.Action OnFloatingPanelClosed;

        public SmartphoneState CurrentState => m_CurrentState;
        public bool IsActiveOpen => m_CurrentState == SmartphoneState.ActiveOpen;
        public bool IsMessageRead => m_IsMessageRead;
        public GameObject FloatingSmartphonePanel => m_FloatingSmartphonePanel;
        public GameObject TelegramMessagePopup => m_TelegramMessagePopup;

        /// <summary>
        /// Resets the message read state back to false (unread).
        /// </summary>
        public void ResetMessageReadState()
        {
            m_IsMessageRead = false;
        }

        private void Awake()
        {
            m_GrabInteractable = GetComponent<XRGrabInteractable>();
            m_Rigidbody = GetComponent<Rigidbody>();

            // Fresh session: ensure message starts UNREAD so TelegramMessagePopup shows on START
            m_IsMessageRead = false;

            FindReferencesIfNull();

            // Auto-discover screen renderer if not manually assigned
            if (m_ScreenRenderer == null)
            {
                Transform screenTrans = transform.Find("Visuals/Phone_Screen");
                if (screenTrans != null)
                {
                    m_ScreenRenderer = screenTrans.GetComponent<MeshRenderer>();
                }
            }

            // Ensure initial physical state
            SetScreenMaterial(m_ScreenSleepMaterial);
            SetupFloatingPanelCloseButtons();
        }

        private void OnEnable()
        {
            if (m_GrabInteractable != null)
            {
                m_GrabInteractable.selectEntered.AddListener(OnSelectEntered);
                m_GrabInteractable.selectExited.AddListener(OnSelectExited);
                m_GrabInteractable.activated.AddListener(OnActivated);
            }

            InitializeInputActions();
        }

        private void OnDisable()
        {
            if (m_GrabInteractable != null)
            {
                m_GrabInteractable.selectEntered.RemoveListener(OnSelectEntered);
                m_GrabInteractable.selectExited.RemoveListener(OnSelectExited);
                m_GrabInteractable.activated.RemoveListener(OnActivated);
            }

            DisableInputActions();
        }

        private void InitializeInputActions()
        {
            if (m_LeftActivateAction.action == null)
            {
                var leftAction = new InputAction("LeftActivate", InputActionType.Button, "<XRController>{LeftHand}/triggerPressed");
                leftAction.AddBinding("<XRController>{LeftHand}/primaryButton");
                m_LeftActivateAction = new InputActionProperty(leftAction);
            }

            if (m_RightActivateAction.action == null)
            {
                var rightAction = new InputAction("RightActivate", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
                rightAction.AddBinding("<XRController>{RightHand}/primaryButton");
                m_RightActivateAction = new InputActionProperty(rightAction);
            }

            try
            {
                m_LeftActivateAction.action?.Enable();
                m_RightActivateAction.action?.Enable();
            }
            catch { /* Handled for edit mode */ }
        }

        private void DisableInputActions()
        {
            try
            {
                m_LeftActivateAction.action?.Disable();
                m_RightActivateAction.action?.Disable();
            }
            catch { /* Handled for edit mode */ }
        }

        private void Update()
        {
            HandleVRControllerInput();
        }

        /// <summary>
        /// Reads Left and Right VR controller trigger/activate inputs while the phone is held.
        /// Either controller can operate the phone based on which hand is holding it.
        /// </summary>
        private void HandleVRControllerInput()
        {
            if (!IsCurrentlyHeld()) return;

            bool leftPressed = m_LeftActivateAction.action != null && m_LeftActivateAction.action.WasPressedThisFrame();
            bool rightPressed = m_RightActivateAction.action != null && m_RightActivateAction.action.WasPressedThisFrame();

            Handedness hand = GetHoldingHand();

            // When held in Left Hand -> Left Controller Activate toggles
            if (hand == Handedness.Left && leftPressed)
            {
                TogglePhoneState();
            }
            // When held in Right Hand -> Right Controller Activate toggles
            else if (hand == Handedness.Right && rightPressed)
            {
                TogglePhoneState();
            }
            // If held in ray or unspecified hand -> either controller trigger toggles
            else if (hand == Handedness.Unknown && (leftPressed || rightPressed))
            {
                TogglePhoneState();
            }
        }

        /// <summary>
        /// Determines whether the Left or Right controller is currently holding the phone.
        /// </summary>
        public Handedness GetHoldingHand()
        {
            var interactor = m_CurrentHoldingInteractor;
            if (interactor == null)
            {
                if (m_GrabInteractable == null) m_GrabInteractable = GetComponent<XRGrabInteractable>();
                if (m_GrabInteractable != null && m_GrabInteractable.isSelected)
                {
                    interactor = m_GrabInteractable.firstInteractorSelecting;
                }
            }

            if (interactor == null) return Handedness.Unknown;

            Transform t = interactor.transform;
            while (t != null)
            {
                string lower = t.name.ToLowerInvariant();
                if (lower.Contains("left")) return Handedness.Left;
                if (lower.Contains("right")) return Handedness.Right;
                t = t.parent;
            }

            return Handedness.Unknown;
        }

        /// <summary>
        /// Returns true if the phone is currently held by a VR interactor.
        /// </summary>
        public bool IsCurrentlyHeld()
        {
            if (m_CurrentHoldingInteractor != null)
            {
                return true;
            }

            if (m_GrabInteractable == null) m_GrabInteractable = GetComponent<XRGrabInteractable>();
            if (m_Rigidbody == null) m_Rigidbody = GetComponent<Rigidbody>();

            if (m_GrabInteractable != null && m_GrabInteractable.isSelected)
            {
                return true;
            }

            return false;
        }

        // ─── XRI EVENT HANDLERS & SIMULATION ──────────────────────────────────────

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            m_CurrentHoldingInteractor = args.interactorObject;
            // Grabbing the phone opens the floating panel and marks message read
            OnSmartphoneInteracted();
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            m_CurrentHoldingInteractor = null;
            // Releasing / dropping the phone automatically returns it to Physical state
            if (m_CurrentState == SmartphoneState.ActiveOpen || m_CurrentState == SmartphoneState.Closing)
            {
                TriggerClose();
            }

            // Ensure Telegram popup remains suppressed if read
            if (m_IsMessageRead && m_TelegramMessagePopup != null)
            {
                m_TelegramMessagePopup.SetActive(false);
            }
        }

        public void SimulateSelectEnter(IXRSelectInteractor interactor)
        {
            m_CurrentHoldingInteractor = interactor;
            OnSmartphoneInteracted();
        }

        public void SimulateSelectExit()
        {
            m_CurrentHoldingInteractor = null;
            if (m_CurrentState == SmartphoneState.ActiveOpen || m_CurrentState == SmartphoneState.Closing)
            {
                TriggerClose();
            }

            if (m_IsMessageRead && m_TelegramMessagePopup != null)
            {
                m_TelegramMessagePopup.SetActive(false);
            }
        }

        private void OnActivated(ActivateEventArgs args)
        {
            // Secondary trigger press while holding in VR toggles panel
            ToggleFloatingPanel();
        }

        // ─── SMARTPHONE INTERACTION & FLOATING PANEL LOGIC ───────────────────────

        /// <summary>
        /// Auto-discovers scene references for TelegramMessagePopup and FloatingSmartphonePanel if unassigned.
        /// </summary>
        public void FindReferencesIfNull()
        {
            if (m_TelegramMessagePopup == null)
            {
                Transform popupTrans = transform.Find("TelegramMessagePopup");
                if (popupTrans != null)
                {
                    m_TelegramMessagePopup = popupTrans.gameObject;
                }
                else
                {
                    var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                    for (int i = 0; i < allTransforms.Length; i++)
                    {
                        var t = allTransforms[i];
                        if (t != null && t.gameObject.name == "TelegramMessagePopup" && t.gameObject.scene.isLoaded)
                        {
                            m_TelegramMessagePopup = t.gameObject;
                            break;
                        }
                    }
                }
            }

            if (m_FloatingSmartphonePanel == null)
            {
                GameObject worldUi = GameObject.Find("WorldUI");
                if (worldUi != null)
                {
                    Transform panelTrans = worldUi.transform.Find("FloatingSmartphonePanel");
                    if (panelTrans != null)
                    {
                        m_FloatingSmartphonePanel = panelTrans.gameObject;
                    }
                }

                if (m_FloatingSmartphonePanel == null)
                {
                    var allPanels = Resources.FindObjectsOfTypeAll<GameObject>();
                    for (int i = 0; i < allPanels.Length; i++)
                    {
                        if (allPanels[i].name == "FloatingSmartphonePanel" && allPanels[i].scene.isLoaded)
                        {
                            m_FloatingSmartphonePanel = allPanels[i];
                            break;
                        }
                    }
                }
            }

            // Ensure Telegram popup has BoxCollider for 3D raycasts
            if (m_TelegramMessagePopup != null)
            {
                BoxCollider popupCol = m_TelegramMessagePopup.GetComponent<BoxCollider>();
                if (popupCol == null)
                {
                    popupCol = m_TelegramMessagePopup.AddComponent<BoxCollider>();
                    RectTransform rt = m_TelegramMessagePopup.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        popupCol.size = new Vector3(rt.rect.width, rt.rect.height, 10f);
                    }
                    else
                    {
                        popupCol.size = new Vector3(420f, 110f, 10f);
                    }
                }
            }
        }

        /// <summary>
        /// Configures the close buttons and home indicator on FloatingSmartphonePanel.
        /// </summary>
        public void SetupFloatingPanelCloseButtons()
        {
            if (m_FloatingSmartphonePanel == null) FindReferencesIfNull();
            if (m_FloatingSmartphonePanel == null) return;

            // Ensure root floating panel has a BoxCollider so 3D raycasts can click it
            BoxCollider panelCol = m_FloatingSmartphonePanel.GetComponent<BoxCollider>();
            if (panelCol == null)
            {
                panelCol = m_FloatingSmartphonePanel.AddComponent<BoxCollider>();
            }
            RectTransform panelRt = m_FloatingSmartphonePanel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelCol.size = new Vector3(panelRt.rect.width, panelRt.rect.height, 5f);
                panelCol.center = new Vector3(panelRt.rect.width * (0.5f - panelRt.pivot.x), panelRt.rect.height * (0.5f - panelRt.pivot.y), 0f);
            }

            Transform screen = m_FloatingSmartphonePanel.transform.Find("ScreenContainer");
            if (screen != null)
            {
                // Wire ClearButton
                Transform clearBtnTrans = screen.Find("ClearButton");
                if (clearBtnTrans != null)
                {
                    Button clearBtn = clearBtnTrans.GetComponent<Button>();
                    if (clearBtn != null)
                    {
                        clearBtn.onClick.RemoveListener(CloseFloatingPanel);
                        clearBtn.onClick.AddListener(CloseFloatingPanel);
                    }

                    BoxCollider col = clearBtnTrans.GetComponent<BoxCollider>();
                    if (col == null)
                    {
                        col = clearBtnTrans.gameObject.AddComponent<BoxCollider>();
                    }
                    RectTransform rt = clearBtnTrans.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        col.size = new Vector3(rt.rect.width, rt.rect.height, 10f);
                        col.center = new Vector3(rt.rect.width * (0.5f - rt.pivot.x), rt.rect.height * (0.5f - rt.pivot.y), 0f);
                    }

                    var clearInteractable = clearBtnTrans.GetComponent<XRSimpleInteractable>();
                    if (clearInteractable == null)
                    {
                        clearInteractable = clearBtnTrans.gameObject.AddComponent<XRSimpleInteractable>();
                    }
                    clearInteractable.selectEntered.RemoveAllListeners();
                    clearInteractable.selectEntered.AddListener(_ => CloseFloatingPanel());
                    clearInteractable.activated.RemoveAllListeners();
                    clearInteractable.activated.AddListener(_ => CloseFloatingPanel());
                }

                // Wire HomeIndicator
                Transform homeTrans = screen.Find("HomeIndicator");
                if (homeTrans != null)
                {
                    Button homeBtn = homeTrans.GetComponent<Button>();
                    if (homeBtn == null) homeBtn = homeTrans.gameObject.AddComponent<Button>();
                    homeBtn.onClick.RemoveListener(CloseFloatingPanel);
                    homeBtn.onClick.AddListener(CloseFloatingPanel);

                    BoxCollider homeCol = homeTrans.GetComponent<BoxCollider>();
                    if (homeCol == null)
                    {
                        homeCol = homeTrans.gameObject.AddComponent<BoxCollider>();
                    }
                    RectTransform rt = homeTrans.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        homeCol.size = new Vector3(rt.rect.width, 30f, 10f);
                        homeCol.center = new Vector3(rt.rect.width * (0.5f - rt.pivot.x), 30f * (0.5f - rt.pivot.y), 0f);
                    }

                    var homeInteractable = homeTrans.GetComponent<XRSimpleInteractable>();
                    if (homeInteractable == null)
                    {
                        homeInteractable = homeTrans.gameObject.AddComponent<XRSimpleInteractable>();
                    }
                    homeInteractable.selectEntered.RemoveAllListeners();
                    homeInteractable.selectEntered.AddListener(_ => CloseFloatingPanel());
                    homeInteractable.activated.RemoveAllListeners();
                    homeInteractable.activated.AddListener(_ => CloseFloatingPanel());
                }
            }
        }

        /// <summary>
        /// Called when the player clicks or interacts with the existing smartphone object.
        /// Sets TelegramMessagePopup to inactive, marks the message permanently READ,
        /// and displays FloatingSmartphonePanel.
        /// </summary>
        public void OnSmartphoneInteracted()
        {
            FindReferencesIfNull();

            // Permanently mark message as READ
            m_IsMessageRead = true;

            // 1. Set existing TelegramMessagePopup Toggle Active state to false
            if (m_TelegramMessagePopup != null)
            {
                m_TelegramMessagePopup.SetActive(false);
            }

            // 2. Show existing FloatingSmartphonePanel
            if (m_FloatingSmartphonePanel != null)
            {
                m_FloatingSmartphonePanel.SetActive(true);
                SetupFloatingPanelCloseButtons();
            }

            // Illuminate physical phone display
            TriggerOpen(force: true);

            OnMessageRead?.Invoke();
            OnFloatingPanelOpened?.Invoke();

            Debug.Log("[SmartphoneController] Smartphone interacted with! Telegram popup hidden, FloatingSmartphonePanel shown, message permanently READ.");
        }

        /// <summary>
        /// Hides the existing FloatingSmartphonePanel while keeping the TelegramMessagePopup hidden
        /// and the message permanently marked as READ.
        /// </summary>
        public void CloseFloatingPanel()
        {
            FindReferencesIfNull();

            if (m_FloatingSmartphonePanel != null)
            {
                m_FloatingSmartphonePanel.SetActive(false);
            }

            // Telegram popup must stay hidden!
            if (m_TelegramMessagePopup != null)
            {
                m_TelegramMessagePopup.SetActive(false);
            }

            // Return physical phone screen to sleep/physical state
            if (m_CurrentState == SmartphoneState.ActiveOpen)
            {
                TriggerClose();
            }

            var toggleTrigger = GetComponent<SmartphonePanelToggleTrigger>();
            if (toggleTrigger != null)
            {
                toggleTrigger.NotifyToggled();
            }

            // Closing the panel must not reset the message state (m_IsMessageRead remains true)
            OnFloatingPanelClosed?.Invoke();

            Debug.Log("[SmartphoneController] FloatingSmartphonePanel closed. Message remains READ.");
        }

        /// <summary>
        /// Toggles between opening and closing the FloatingSmartphonePanel.
        /// </summary>
        public void ToggleFloatingPanel()
        {
            var toggleTrigger = GetComponent<SmartphonePanelToggleTrigger>();
            if (toggleTrigger != null)
            {
                toggleTrigger.TogglePanel();
                return;
            }

            FindReferencesIfNull();

            if (m_FloatingSmartphonePanel != null && m_FloatingSmartphonePanel.activeSelf)
            {
                CloseFloatingPanel();
            }
            else
            {
                OnSmartphoneInteracted();
            }
        }

        // ─── STATE TRANSITIONS ──────────────────────────────────────────────────

        /// <summary>
        /// Toggles between Physical -> Active/Open and Active/Open -> Closing.
        /// </summary>
        public void TogglePhoneState()
        {
            ToggleFloatingPanel();
        }

        /// <summary>
        /// Transitions from Physical -> ActiveOpen.
        /// </summary>
        public void TriggerOpen(bool force = false)
        {
            if (!force && !IsCurrentlyHeld())
            {
                // Must be held to open unless forced
                return;
            }

            if (m_ClosingCoroutine != null)
            {
                StopCoroutine(m_ClosingCoroutine);
                m_ClosingCoroutine = null;
            }

            m_CurrentState = SmartphoneState.ActiveOpen;
            SetScreenMaterial(m_ScreenActiveMaterial);

            OnStateChanged?.Invoke(m_CurrentState);
            OnPhoneOpened?.Invoke();

            Debug.Log("[SmartphoneController] State: Active/Open (Phone illuminated in hand)");
        }

        /// <summary>
        /// Transitions from ActiveOpen -> Closing -> Physical.
        /// </summary>
        public void TriggerClose()
        {
            if (m_CurrentState == SmartphoneState.Physical)
            {
                return;
            }

            if (m_ClosingCoroutine != null)
            {
                StopCoroutine(m_ClosingCoroutine);
            }

            m_CurrentState = SmartphoneState.Closing;
            OnStateChanged?.Invoke(m_CurrentState);

            Debug.Log("[SmartphoneController] State: Closing (Dimming down...)");
            if (Application.isPlaying)
            {
                m_ClosingCoroutine = StartCoroutine(ClosingRoutine());
            }
        }

        private IEnumerator ClosingRoutine()
        {
            float elapsed = 0f;
            while (elapsed < m_ClosingDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            CompleteClosing();
        }

        public void CompleteClosing()
        {
            m_CurrentState = SmartphoneState.Physical;
            SetScreenMaterial(m_ScreenSleepMaterial);
            m_ClosingCoroutine = null;

            // Ensure Telegram popup remains suppressed if read
            if (m_IsMessageRead && m_TelegramMessagePopup != null)
            {
                m_TelegramMessagePopup.SetActive(false);
            }

            OnStateChanged?.Invoke(m_CurrentState);
            OnPhoneClosed?.Invoke();

            Debug.Log("[SmartphoneController] State: Physical (Sleep state restored)");
        }

        private void SetScreenMaterial(Material mat)
        {
            if (m_ScreenRenderer == null)
            {
                Transform screenTrans = transform.Find("Visuals/Phone_Screen");
                if (screenTrans != null)
                {
                    m_ScreenRenderer = screenTrans.GetComponent<MeshRenderer>();
                }
            }

            if (m_ScreenRenderer != null && mat != null)
            {
                m_ScreenRenderer.sharedMaterial = mat;
            }
        }

        // Configuration helpers for editor / external setup
        public void Configure(MeshRenderer screenRenderer, Material sleepMat, Material activeMat)
        {
            m_ScreenRenderer = screenRenderer;
            m_ScreenSleepMaterial = sleepMat;
            m_ScreenActiveMaterial = activeMat;
            SetScreenMaterial(m_ScreenSleepMaterial);
        }
    }
}
