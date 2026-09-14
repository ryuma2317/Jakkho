using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Kannko.Phone
{
    /// <summary>
    /// Handles controller clicks directed at the physical smartphone to toggle (open and close)
    /// the FloatingSmartphonePanel.
    /// 
    /// Supports:
    /// 1. Real VR:
    ///    - Pointing VR controller ray at the phone and pressing Trigger/Activate.
    ///    - Direct controller touch or poke interaction.
    ///    - Secondary trigger press while holding the phone.
    /// 2. Laptop / PC Test Version:
    ///    - Mouse cursor click and center-screen reticle click on the phone.
    ///    - XR Device Simulator controller ray clicking.
    ///    - Configurable keyboard shortcuts ('E', 'P').
    /// </summary>
    [DisallowMultipleComponent]
    public class SmartphonePanelToggleTrigger : MonoBehaviour
    {
        [Header("Target References")]
        [Tooltip("SmartphoneController on this or parent GameObject.")]
        [SerializeField] private SmartphoneController m_PhoneController;

        [Tooltip("The floating smartphone panel in WorldUI.")]
        [SerializeField] private GameObject m_FloatingSmartphonePanel;

        [Tooltip("The floating Telegram message popup above the phone.")]
        [SerializeField] private GameObject m_TelegramMessagePopup;

        [Header("Click Settings")]
        [Tooltip("Minimum time (seconds) between consecutive clicks to prevent double-toggling.")]
        [SerializeField] private float m_ClickDebounceTime = 0.25f;

        [Header("VR Controller Input Actions")]
        [SerializeField] private InputActionProperty m_LeftTriggerAction;
        [SerializeField] private InputActionProperty m_RightTriggerAction;

        [Header("Laptop / Desktop Test Controls")]
        [SerializeField] private bool m_EnableMouseClick = true;
        [SerializeField] private bool m_EnableKeyboardShortcut = true;
        [SerializeField] private KeyCode m_ShortcutKey = KeyCode.E;
        [SerializeField] private KeyCode m_AlternativeShortcutKey = KeyCode.P;

        [Header("Events")]
        public UnityEvent OnPanelOpened = new UnityEvent();
        public UnityEvent OnPanelClosed = new UnityEvent();

        // Runtime state
        private float m_LastToggleTime = -1f;
        private XRGrabInteractable m_GrabInteractable;
        private readonly List<XRRayInteractor> m_ActiveRayInteractors = new List<XRRayInteractor>();

        public bool IsPanelOpen => m_FloatingSmartphonePanel != null && m_FloatingSmartphonePanel.activeSelf;
        public SmartphoneController PhoneController => m_PhoneController;

        private void Awake()
        {
            FindReferencesIfNull();
            InitializeInputActions();
            SetupInteractableHooks();
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
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            HandleVRControllerRayClicks();
            HandleDesktopMouseClicks();
            HandleKeyboardShortcuts();
        }

        /// <summary>
        /// Auto-discovers SmartphoneController, FloatingSmartphonePanel, and TelegramMessagePopup.
        /// </summary>
        public void FindReferencesIfNull()
        {
            if (m_PhoneController == null)
            {
                m_PhoneController = GetComponent<SmartphoneController>();
                if (m_PhoneController == null)
                {
                    m_PhoneController = GetComponentInParent<SmartphoneController>();
                }
                if (m_PhoneController == null)
                {
                    m_PhoneController = Object.FindFirstObjectByType<SmartphoneController>();
                }
            }

            if (m_FloatingSmartphonePanel == null && m_PhoneController != null)
            {
                m_FloatingSmartphonePanel = m_PhoneController.FloatingSmartphonePanel;
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
            }

            if (m_TelegramMessagePopup == null && m_PhoneController != null)
            {
                m_TelegramMessagePopup = m_PhoneController.TelegramMessagePopup;
            }

            if (m_TelegramMessagePopup == null)
            {
                Transform popupTrans = transform.Find("TelegramMessagePopup");
                if (popupTrans != null)
                {
                    m_TelegramMessagePopup = popupTrans.gameObject;
                }
            }
        }

        /// <summary>
        /// Configures XRRayInteractors in the scene so that pointing at the phone and pulling trigger
        /// sends activate events even while hovering from a distance.
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
            catch { /* Handled for edit mode */ }
        }

        private void DisableInputActions()
        {
            try
            {
                m_LeftTriggerAction.action?.Disable();
                m_RightTriggerAction.action?.Disable();
            }
            catch { /* Handled for edit mode */ }
        }

        private void SetupInteractableHooks()
        {
            if (m_GrabInteractable == null)
            {
                m_GrabInteractable = GetComponent<XRGrabInteractable>();
            }
        }

        private void TeardownInteractableHooks()
        {
        }

        /// <summary>
        /// Detects VR controller raycasts hitting the phone and trigger click presses.
        /// Works with real VR controllers as well as the XR Device Simulator.
        /// </summary>
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
                    if (IsHitOnPhone(hit.collider))
                    {
                        Debug.Log($"[SmartphonePanelToggleTrigger] VR Controller ray '{ray.name}' clicked smartphone. Toggling panel.");
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
            if (!m_EnableMouseClick) return;
            if (UnityEngine.InputSystem.Mouse.current == null ||
                !UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // 1. Center-screen reticle raycast
            Ray centerRay = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            if (Physics.Raycast(centerRay, out RaycastHit centerHit, 5.0f))
            {
                if (IsHitOnPhone(centerHit.collider))
                {
                    Debug.Log("[SmartphonePanelToggleTrigger] Smartphone clicked via desktop reticle raycast. Toggling panel.");
                    TogglePanel();
                    return;
                }
            }

            // 2. Free cursor mouse raycast
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            Ray mouseRay = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, 5.0f))
            {
                if (IsHitOnPhone(mouseHit.collider))
                {
                    Debug.Log("[SmartphonePanelToggleTrigger] Smartphone clicked via desktop mouse cursor raycast. Toggling panel.");
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
            if (!m_EnableKeyboardShortcut) return;

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            // Map KeyCode to Input System Key
            bool shortcutPressed = IsKeyCodePressed(kb, m_ShortcutKey) || IsKeyCodePressed(kb, m_AlternativeShortcutKey);
            if (shortcutPressed)
            {
                // Verify camera is looking in the general direction of phone or player is within range (5m)
                Camera cam = Camera.main;
                if (cam != null)
                {
                    float dist = Vector3.Distance(cam.transform.position, transform.position);
                    if (dist <= 6.0f)
                    {
                        Debug.Log($"[SmartphonePanelToggleTrigger] Keyboard shortcut triggered near phone ({dist:F1}m). Toggling panel.");
                        TogglePanel();
                    }
                }
            }
        }

        private static bool IsKeyCodePressed(UnityEngine.InputSystem.Keyboard kb, KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.P => kb.pKey.wasPressedThisFrame,
                KeyCode.Q => kb.qKey.wasPressedThisFrame,
                KeyCode.E => kb.eKey.wasPressedThisFrame,
                KeyCode.F => kb.fKey.wasPressedThisFrame,
                KeyCode.T => kb.tKey.wasPressedThisFrame,
                KeyCode.M => kb.mKey.wasPressedThisFrame,
                KeyCode.N => kb.nKey.wasPressedThisFrame,
                _ => false,
            };
        }

        private bool IsHitOnPhone(Collider col)
        {
            if (col == null) return false;
            if (col.gameObject == gameObject || col.transform.IsChildOf(transform)) return true;
            if (m_PhoneController != null && (col.gameObject == m_PhoneController.gameObject || col.transform.IsChildOf(m_PhoneController.transform))) return true;
            if (m_TelegramMessagePopup != null && (col.gameObject == m_TelegramMessagePopup || col.transform.IsChildOf(m_TelegramMessagePopup.transform))) return true;
            return false;
        }

        /// <summary>
        /// Toggles the FloatingSmartphonePanel between open and closed.
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
                OpenPanel();
            }
        }

        /// <summary>
        /// Opens the FloatingSmartphonePanel and marks the message as permanently read.
        /// </summary>
        public void OpenPanel()
        {
            m_LastToggleTime = Time.unscaledTime;
            FindReferencesIfNull();

            if (m_PhoneController != null)
            {
                m_PhoneController.OnSmartphoneInteracted();
            }
            else if (m_FloatingSmartphonePanel != null)
            {
                m_FloatingSmartphonePanel.SetActive(true);
                if (m_TelegramMessagePopup != null)
                {
                    m_TelegramMessagePopup.SetActive(false);
                }
            }

            OnPanelOpened?.Invoke();
            Debug.Log("[SmartphonePanelToggleTrigger] FloatingSmartphonePanel OPENED.");
        }

        /// <summary>
        /// Closes the FloatingSmartphonePanel while keeping the Telegram message marked read
        /// and the TelegramMessagePopup hidden.
        /// </summary>
        public void ClosePanel()
        {
            m_LastToggleTime = Time.unscaledTime;
            FindReferencesIfNull();

            if (m_PhoneController != null)
            {
                m_PhoneController.CloseFloatingPanel();
            }
            else if (m_FloatingSmartphonePanel != null)
            {
                m_FloatingSmartphonePanel.SetActive(false);
            }

            // Ensure Telegram popup remains suppressed
            if (m_TelegramMessagePopup != null)
            {
                m_TelegramMessagePopup.SetActive(false);
            }

            OnPanelClosed?.Invoke();
            Debug.Log("[SmartphonePanelToggleTrigger] FloatingSmartphonePanel CLOSED.");
        }
    }
}
