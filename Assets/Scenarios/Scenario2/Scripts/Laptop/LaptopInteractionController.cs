using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Kannko.Scenario;

namespace Kannko.Laptop
{
    /// <summary>
    /// Controls interaction with the physical Laptop object in Scenario 2.
    /// Toggles the World Space FloatingLaptopPanel open and closed when clicked/interacted with.
    /// 
    /// Supports:
    /// 1. Real VR:
    ///    - Pointing VR controller ray at the laptop and pulling trigger / activating.
    ///    - XRSimpleInteractable events (selectEntered, activated).
    /// 2. Laptop / PC Test Version:
    ///    - Mouse cursor click and center reticle click on the laptop.
    ///    - XR Device Simulator controller ray clicking.
    ///    - Keyboard shortcuts ('L', 'E' while looking at laptop, and Escape key to close).
    /// </summary>
    [DisallowMultipleComponent]
    public class LaptopInteractionController : MonoBehaviour
    {
        [Header("Target References")]
        [Tooltip("The World Space FloatingLaptopPanel GameObject (under WorldUI).")]
        [SerializeField] private GameObject m_FloatingLaptopPanel;

        [Header("Scenario Intro Gate")]
        [Tooltip("Reference to Scenario2IntroController. FloatingLaptopPanel cannot show until scenario starts from IntroPanel.")]
        [SerializeField] private Scenario2IntroController m_IntroController;
        [SerializeField] private bool m_RequireScenarioStarted = true;

        [Header("Click Settings")]
        [Tooltip("Minimum time (seconds) between consecutive clicks to prevent rapid double-toggling.")]
        [SerializeField] private float m_ClickDebounceTime = 0.25f;

        [Header("VR Controller Input Actions")]
        [SerializeField] private InputActionProperty m_LeftTriggerAction;
        [SerializeField] private InputActionProperty m_RightTriggerAction;

        [Header("Desktop / Laptop Test Controls")]
        [SerializeField] private bool m_EnableMouseClick = true;
        [SerializeField] private bool m_EnableKeyboardShortcut = true;
        [SerializeField] private KeyCode m_ShortcutKey = KeyCode.L;
        [SerializeField] private KeyCode m_AlternativeShortcutKey = KeyCode.E;

        [Header("Events")]
        public UnityEvent OnPanelOpened = new UnityEvent();
        public UnityEvent OnPanelClosed = new UnityEvent();

        // Runtime state
        private float m_LastToggleTime = -1f;
        private XRSimpleInteractable m_SimpleInteractable;
        private BoxCollider m_Collider;
        private readonly List<XRRayInteractor> m_ActiveRayInteractors = new List<XRRayInteractor>();

        public bool IsPanelOpen => m_FloatingLaptopPanel != null && m_FloatingLaptopPanel.activeSelf;
        public GameObject FloatingLaptopPanel => m_FloatingLaptopPanel;
        public Scenario2IntroController IntroController
        {
            get => m_IntroController;
            set => m_IntroController = value;
        }

        public bool CanInteract
        {
            get
            {
                if (!m_RequireScenarioStarted) return true;
                if (m_IntroController == null)
                {
                    m_IntroController = Object.FindFirstObjectByType<Scenario2IntroController>(FindObjectsInactive.Include);
                }
                if (m_IntroController != null)
                {
                    return m_IntroController.IsScenarioStarted;
                }
                return true;
            }
        }

        private void Awake()
        {
            FindReferencesIfNull();
            SetupColliderAndInteractable();
            InitializeInputActions();

            // The FloatingLaptopPanel must be hidden/not visible when the game starts.
            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            FindReferencesIfNull();
            EnableInputActions();
            SetupInteractableHooks();
        }

        private void OnDisable()
        {
            DisableInputActions();
            TeardownInteractableHooks();
        }

        private void Start()
        {
            FindReferencesIfNull();
            ConfigureRayInteractors();

            // Ensure hidden at Start as well
            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            HandleVRControllerRayClicks();
            HandleDesktopMouseClicks();
            HandleKeyboardShortcuts();
        }

        /// <summary>
        /// Locates FloatingLaptopPanel in scene if not assigned in Inspector.
        /// </summary>
        public void FindReferencesIfNull()
        {
            if (m_FloatingLaptopPanel == null)
            {
                GameObject worldUi = GameObject.Find("WorldUI");
                if (worldUi != null)
                {
                    Transform panelTrans = worldUi.transform.Find("FloatingLaptopPanel");
                    if (panelTrans != null)
                    {
                        m_FloatingLaptopPanel = panelTrans.gameObject;
                    }
                }

                if (m_FloatingLaptopPanel == null)
                {
                    var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                    for (int i = 0; i < allObjects.Length; i++)
                    {
                        var go = allObjects[i];
                        if (go.name == "FloatingLaptopPanel" && go.scene.isLoaded)
                        {
                            m_FloatingLaptopPanel = go;
                            break;
                        }
                    }
                }
            }

            if (m_IntroController == null)
            {
                m_IntroController = Object.FindFirstObjectByType<Scenario2IntroController>(FindObjectsInactive.Include);
            }
        }

        /// <summary>
        /// Ensures a BoxCollider and XRSimpleInteractable exist on the laptop.
        /// </summary>
        public void SetupColliderAndInteractable()
        {
            m_Collider = GetComponent<BoxCollider>();
            if (m_Collider == null)
            {
                m_Collider = gameObject.AddComponent<BoxCollider>();
                m_Collider.center = new Vector3(0.15f, 0.10f, 0.06f);
                m_Collider.size = new Vector3(0.35f, 0.25f, 0.35f);
            }

            m_SimpleInteractable = GetComponent<XRSimpleInteractable>();
            if (m_SimpleInteractable == null)
            {
                m_SimpleInteractable = gameObject.AddComponent<XRSimpleInteractable>();
            }

            SetupInteractableHooks();
        }

        private void SetupInteractableHooks()
        {
            if (m_SimpleInteractable == null)
            {
                m_SimpleInteractable = GetComponent<XRSimpleInteractable>();
            }

            if (m_SimpleInteractable != null)
            {
                m_SimpleInteractable.selectEntered.RemoveListener(OnInteractableTriggered);
                m_SimpleInteractable.selectEntered.AddListener(OnInteractableTriggered);

                m_SimpleInteractable.activated.RemoveListener(OnInteractableTriggered);
                m_SimpleInteractable.activated.AddListener(OnInteractableTriggered);
            }
        }

        private void TeardownInteractableHooks()
        {
            if (m_SimpleInteractable != null)
            {
                m_SimpleInteractable.selectEntered.RemoveListener(OnInteractableTriggered);
                m_SimpleInteractable.activated.RemoveListener(OnInteractableTriggered);
            }
        }

        private void OnInteractableTriggered(BaseInteractionEventArgs args)
        {
            if (!CanInteract)
            {
                Debug.Log($"[LaptopInteractionController] Laptop interactable triggered by {args.interactorObject?.transform.name}, but scenario has not started yet. Ignoring interaction.");
                return;
            }

            Debug.Log($"[LaptopInteractionController] Laptop interactable triggered by {args.interactorObject?.transform.name}. Toggling panel.");
            TogglePanel();
        }

        /// <summary>
        /// Ensures all XRRayInteractors allow distance trigger activation.
        /// </summary>
        public void ConfigureRayInteractors()
        {
            m_ActiveRayInteractors.Clear();
            var rays = Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

        private void InitializeInputActions()
        {
            if (m_LeftTriggerAction.action == null)
            {
                var leftAction = new InputAction("LeftTriggerClick", InputActionType.Button, "<XRController>{LeftHand}/triggerPressed");
                leftAction.AddBinding("<XRController>{LeftHand}/primaryButton");
                m_LeftTriggerAction = new InputActionProperty(leftAction);
            }

            if (m_RightTriggerAction.action == null)
            {
                var rightAction = new InputAction("RightTriggerClick", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
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

        /// <summary>
        /// Detects VR controller raycasts hitting the laptop and trigger presses.
        /// </summary>
        private void HandleVRControllerRayClicks()
        {
            bool leftTrigger = m_LeftTriggerAction.action != null && m_LeftTriggerAction.action.WasPressedThisFrame();
            bool rightTrigger = m_RightTriggerAction.action != null && m_RightTriggerAction.action.WasPressedThisFrame();

            if (!leftTrigger && !rightTrigger) return;
            if (!CanInteract) return;

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
                    if (IsHitOnLaptop(hit.collider))
                    {
                        Debug.Log($"[LaptopInteractionController] VR Controller ray '{ray.name}' clicked laptop. Toggling panel.");
                        TogglePanel();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Detects direct mouse clicks and reticle raycasts in Desktop / PC testing mode.
        /// </summary>
        private void HandleDesktopMouseClicks()
        {
            if (!m_EnableMouseClick || !Input.GetMouseButtonDown(0)) return;
            if (!CanInteract) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // 1. Center-screen reticle raycast
            Ray centerRay = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            if (Physics.Raycast(centerRay, out RaycastHit centerHit, 5.0f))
            {
                if (IsHitOnLaptop(centerHit.collider))
                {
                    Debug.Log("[LaptopInteractionController] Laptop clicked via desktop reticle raycast. Toggling panel.");
                    TogglePanel();
                    return;
                }
            }

            // 2. Free cursor mouse raycast
            Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, 5.0f))
            {
                if (IsHitOnLaptop(mouseHit.collider))
                {
                    Debug.Log("[LaptopInteractionController] Laptop clicked via desktop mouse cursor raycast. Toggling panel.");
                    TogglePanel();
                    return;
                }
            }
        }

        /// <summary>
        /// Handles keyboard shortcuts for quick testing on laptops.
        /// </summary>
        private void HandleKeyboardShortcuts()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (IsPanelOpen)
                {
                    Debug.Log("[LaptopInteractionController] Escape key pressed. Closing laptop panel.");
                    ClosePanel();
                    return;
                }
            }

            if (!m_EnableKeyboardShortcut) return;

            if (Input.GetKeyDown(m_ShortcutKey) || Input.GetKeyDown(m_AlternativeShortcutKey))
            {
                if (!CanInteract) return;

                Camera cam = Camera.main;
                if (cam != null)
                {
                    float dist = Vector3.Distance(cam.transform.position, transform.position);
                    if (dist <= 6.0f)
                    {
                        Debug.Log($"[LaptopInteractionController] Keyboard shortcut triggered near laptop ({dist:F1}m). Toggling panel.");
                        TogglePanel();
                    }
                }
            }
        }

        private bool IsHitOnLaptop(Collider col)
        {
            if (col == null) return false;
            return col.gameObject == gameObject || col.transform.IsChildOf(transform);
        }

        /// <summary>
        /// Toggles the FloatingLaptopPanel between open and closed.
        /// </summary>
        public void TogglePanel()
        {
            if (Time.unscaledTime - m_LastToggleTime < m_ClickDebounceTime)
            {
                return;
            }
            m_LastToggleTime = Time.unscaledTime;

            FindReferencesIfNull();

            if (IsPanelOpen)
            {
                ClosePanel();
            }
            else
            {
                if (!CanInteract)
                {
                    Debug.Log("[LaptopInteractionController] Cannot toggle open FloatingLaptopPanel before scenario has started from IntroPanel.");
                    return;
                }
                OpenPanel();
            }
        }

        /// <summary>
        /// Opens the FloatingLaptopPanel.
        /// </summary>
        public void OpenPanel()
        {
            if (!CanInteract)
            {
                Debug.Log("[LaptopInteractionController] Cannot open FloatingLaptopPanel before scenario has started from IntroPanel.");
                return;
            }

            m_LastToggleTime = Time.unscaledTime;
            FindReferencesIfNull();

            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(true);
            }

            OnPanelOpened?.Invoke();
            Debug.Log("[LaptopInteractionController] FloatingLaptopPanel OPENED.");
        }

        /// <summary>
        /// Closes the FloatingLaptopPanel.
        /// </summary>
        public void ClosePanel()
        {
            m_LastToggleTime = Time.unscaledTime;
            FindReferencesIfNull();

            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(false);
            }

            OnPanelClosed?.Invoke();
            Debug.Log("[LaptopInteractionController] FloatingLaptopPanel CLOSED.");
        }
    }
}
