using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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

            m_ActiveRayInteractors.RemoveAll(r => r == null);
            if (m_ActiveRayInteractors.Count == 0)
            {
                ConfigureRayInteractors();
            }

            for (int i = 0; i < m_ActiveRayInteractors.Count; i++)
            {
                var ray = m_ActiveRayInteractors[i];
                if (ray == null || !ray.gameObject.activeInHierarchy) continue;

                // 1. Check 3D physics raycast hit on phone or colliders
                if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    if (IsHitOnPhone(hit.collider))
                    {
                        Debug.Log($"[SmartphonePanelToggleTrigger] VR Controller ray '{ray.name}' clicked smartphone (3D). Toggling panel.");
                        TogglePanel();
                        return;
                    }
                }

                // 2. Check UI raycast hit (crucial when FloatingSmartphonePanel is open and intercepts ray)
                if (ray.TryGetCurrentUIRaycastResult(out RaycastResult uiHit))
                {
                    if (IsHitOnPanelOrPhone(uiHit.gameObject))
                    {
                        Debug.Log($"[SmartphonePanelToggleTrigger] VR Controller ray '{ray.name}' clicked panel (UI). Toggling panel.");
                        TogglePanel();
                        return;
                    }
                }
            }
        }

        private bool IsHitOnPhone(Collider col)
        {
            if (col == null) return false;
            if (col.gameObject == gameObject || col.transform.IsChildOf(transform)) return true;
            if (m_PhoneController != null && (col.gameObject == m_PhoneController.gameObject || col.transform.IsChildOf(m_PhoneController.transform))) return true;
            if (m_TelegramMessagePopup != null && (col.gameObject == m_TelegramMessagePopup || col.transform.IsChildOf(m_TelegramMessagePopup.transform))) return true;
            if (m_FloatingSmartphonePanel != null && (col.gameObject == m_FloatingSmartphonePanel || col.transform.IsChildOf(m_FloatingSmartphonePanel.transform))) return true;
            return false;
        }

        private bool IsHitOnPanelOrPhone(GameObject go)
        {
            if (go == null) return false;
            if (go == gameObject || go.transform.IsChildOf(transform)) return true;
            if (m_PhoneController != null && (go == m_PhoneController.gameObject || go.transform.IsChildOf(m_PhoneController.transform))) return true;
            if (m_TelegramMessagePopup != null && (go == m_TelegramMessagePopup || go.transform.IsChildOf(m_TelegramMessagePopup.transform))) return true;
            if (m_FloatingSmartphonePanel != null && (go == m_FloatingSmartphonePanel || go.transform.IsChildOf(m_FloatingSmartphonePanel.transform))) return true;
            return false;
        }

        /// <summary>
        /// Call to register an external toggle event (e.g. from ClearButton click) to maintain debounce sync.
        /// </summary>
        public void NotifyToggled()
        {
            m_LastToggleTime = Time.unscaledTime;
        }

        /// <summary>
        /// Toggles the FloatingSmartphonePanel between open and closed.
        /// </summary>
        public void TogglePanel()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastToggleTime < m_ClickDebounceTime)
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
