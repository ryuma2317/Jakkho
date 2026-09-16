using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Kannko.Scenario2
{
    /// <summary>
    /// Controls the camera-fixed World Space 'STOP PLAYING?' decision dialog for Scenario 2.
    /// Positioned under WorldUI (outside FloatingLaptopPanel) at the exact eye-level position
    /// in front of the camera, matching Scenario2ExitConfirmationPanel.
    /// 
    /// Manages:
    /// - Transitioning between the laptop interaction and the decision dialog.
    /// - Resuming scenario interaction ('Continue Scenario') by restoring the laptop and Wi-Fi interface.
    /// - Proceeding to scenario exit ('Get Out of This Cafe') by showing Scenario2ExitConfirmationPanel.
    /// - Dual VR controller ray clicks & desktop mouse interaction.
    /// </summary>
    [DisallowMultipleComponent]
    public class StopPlayingDecisionController : MonoBehaviour
    {
        [Header("UI Root Panels")]
        [Tooltip("The camera-facing World Space decision panel GameObject.")]
        [SerializeField] private GameObject m_DecisionPanel;

        [Tooltip("Reference to the FloatingLaptopPanel under WorldUI.")]
        [SerializeField] private GameObject m_FloatingLaptopPanel;

        [Tooltip("Reference to the EnlargedWifiPanel to restore on continue.")]
        [SerializeField] private GameObject m_EnlargedWifiPanel;

        [Tooltip("Reference to the Scenario2ExitController managing the final exit confirmation panel.")]
        [SerializeField] private Scenario2ExitController m_ExitController;

        [Header("Buttons")]
        [SerializeField] private Button m_ContinueScenarioButton;
        [SerializeField] private Button m_GetOutOfCafeButton;

        [Header("VR Controller Input Actions")]
        [SerializeField] private InputActionProperty m_LeftTriggerAction;
        [SerializeField] private InputActionProperty m_RightTriggerAction;

        [Header("Settings")]
        [SerializeField] private float m_ClickDebounceTime = 0.25f;

        private float m_LastClickTime = -1f;
        private readonly List<XRRayInteractor> m_ActiveRayInteractors = new List<XRRayInteractor>();

        public bool IsDecisionOpen => (m_DecisionPanel != null ? m_DecisionPanel.activeSelf : gameObject.activeSelf);
        public GameObject DecisionPanel => m_DecisionPanel != null ? m_DecisionPanel : gameObject;
        public Button ContinueScenarioButton => m_ContinueScenarioButton;
        public Button GetOutOfCafeButton => m_GetOutOfCafeButton;
        public GameObject FloatingLaptopPanel => m_FloatingLaptopPanel;
        public GameObject EnlargedWifiPanel => m_EnlargedWifiPanel;
        public Scenario2ExitController ExitController => m_ExitController;

        private void Awake()
        {
            FindReferencesIfNull();
            InitializeInputActions();
            SetupButtonListeners();
            EnsureCollidersAndInteractables();
        }

        private void Start()
        {
            FindReferencesIfNull();
            SetupButtonListeners();
            EnsureCollidersAndInteractables();
            ConfigureRayInteractors();
        }

        private void OnEnable()
        {
            FindReferencesIfNull();
            EnableInputActions();
            SetupButtonListeners();
            EnsureCollidersAndInteractables();
        }

        private void OnDisable()
        {
            DisableInputActions();
            RemoveButtonListeners();
        }

        private void Update()
        {
            if (!Application.isPlaying || !IsDecisionOpen) return;

            HandleVRControllerRayClicks();
        }

        public void FindReferencesIfNull()
        {
            if (m_DecisionPanel == null)
            {
                m_DecisionPanel = gameObject;
            }

            var wui = GameObject.Find("WorldUI");

            if (m_FloatingLaptopPanel == null && wui != null)
            {
                var t = wui.transform.Find("FloatingLaptopPanel");
                if (t != null) m_FloatingLaptopPanel = t.gameObject;
            }

            if (m_EnlargedWifiPanel == null && m_FloatingLaptopPanel != null)
            {
                var t = m_FloatingLaptopPanel.transform.Find("ScreenContainer/EnlargedWifiPanel");
                if (t != null) m_EnlargedWifiPanel = t.gameObject;
            }

            if (m_ExitController == null)
            {
                m_ExitController = UnityEngine.Object.FindFirstObjectByType<Scenario2ExitController>(FindObjectsInactive.Include);
            }

            Transform root = m_DecisionPanel != null ? m_DecisionPanel.transform : transform;

            if (m_ContinueScenarioButton == null && root != null)
            {
                var t = root.Find("ActionButtons/ContinueScenarioButton");
                if (t == null) t = root.Find("ContinueScenarioButton");
                if (t != null) m_ContinueScenarioButton = t.GetComponent<Button>();
            }

            if (m_GetOutOfCafeButton == null && root != null)
            {
                var t = root.Find("ActionButtons/GetOutOfCafeButton");
                if (t == null) t = root.Find("GetOutOfCafeButton");
                if (t != null) m_GetOutOfCafeButton = t.GetComponent<Button>();
            }

            var canvas = (m_DecisionPanel != null ? m_DecisionPanel : gameObject).GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            }
        }

        public void SetupButtonListeners()
        {
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
        }

        public void RemoveButtonListeners()
        {
            if (m_ContinueScenarioButton != null)
            {
                m_ContinueScenarioButton.onClick.RemoveListener(OnContinueScenarioClicked);
            }

            if (m_GetOutOfCafeButton != null)
            {
                m_GetOutOfCafeButton.onClick.RemoveListener(OnGetOutOfCafeClicked);
            }
        }

        /// <summary>
        /// Displays the camera-fixed 'STOP PLAYING?' decision panel.
        /// Hides the laptop screen so the player focuses entirely on the choice.
        /// </summary>
        public void ShowDecision()
        {
            FindReferencesIfNull();

            // Hide the laptop screen so player focuses on the world-space decision dialog
            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(false);
            }

            if (m_DecisionPanel != null)
            {
                m_DecisionPanel.SetActive(true);
            }

            EnsureCollidersAndInteractables();
            Debug.Log("[StopPlayingDecisionController] Displayed camera-fixed 'STOP PLAYING?' decision panel.");
        }

        /// <summary>
        /// Hides the decision panel.
        /// </summary>
        public void HideDecision()
        {
            if (m_DecisionPanel != null)
            {
                m_DecisionPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Player chose 'Continue Scenario': hides the decision panel and restores the laptop.
        /// </summary>
        public void OnContinueScenarioClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            Debug.Log("[StopPlayingDecisionController] 'Continue Scenario' clicked: Restoring laptop and Wi-Fi interface.");
            HideDecision();

            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(true);
            }

            if (m_EnlargedWifiPanel != null)
            {
                m_EnlargedWifiPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Player chose 'Get Out of This Cafe': hides the decision panel and transitions
        /// to the existing camera-fixed Scenario2ExitConfirmationPanel.
        /// </summary>
        public void OnGetOutOfCafeClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            Debug.Log("[StopPlayingDecisionController] 'Get Out of This Cafe' clicked: Transitioning to exit confirmation dialog.");
            HideDecision();

            if (m_ExitController != null)
            {
                m_ExitController.ShowExitConfirmation();
            }
            else
            {
                var exitCtrl = UnityEngine.Object.FindFirstObjectByType<Scenario2ExitController>(FindObjectsInactive.Include);
                if (exitCtrl != null)
                {
                    exitCtrl.ShowExitConfirmation();
                }
                else
                {
                    Debug.LogWarning("[StopPlayingDecisionController] Scenario2ExitController not found in scene!");
                }
            }
        }

        public void EnsureCollidersAndInteractables()
        {
            FindReferencesIfNull();

            if (m_ContinueScenarioButton != null)
            {
                SetupElementColliderAndInteractable(m_ContinueScenarioButton.gameObject, OnContinueScenarioClicked);
            }

            if (m_GetOutOfCafeButton != null)
            {
                SetupElementColliderAndInteractable(m_GetOutOfCafeButton.gameObject, OnGetOutOfCafeClicked);
            }
        }

        private void SetupElementColliderAndInteractable(GameObject target, Action onClickAction)
        {
            if (target == null) return;

            BoxCollider col = target.GetComponent<BoxCollider>();
            if (col == null) col = target.AddComponent<BoxCollider>();

            RectTransform rt = target.GetComponent<RectTransform>();
            if (rt != null)
            {
                float colW = Mathf.Max(rt.rect.width, 100f);
                float colH = Mathf.Max(rt.rect.height, 50f);
                col.center = new Vector3(rt.rect.center.x, rt.rect.center.y, -10f);
                col.size = new Vector3(colW, colH, 60f);
            }
            col.enabled = true;

            XRSimpleInteractable interactable = target.GetComponent<XRSimpleInteractable>();
            if (interactable == null) interactable = target.AddComponent<XRSimpleInteractable>();

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
                var leftAction = new InputAction("LeftTriggerDecision", InputActionType.Button, "<XRController>{LeftHand}/triggerPressed");
                leftAction.AddBinding("<XRController>{LeftHand}/primaryButton");
                m_LeftTriggerAction = new InputActionProperty(leftAction);
            }

            if (m_RightTriggerAction.action == null)
            {
                var rightAction = new InputAction("RightTriggerDecision", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
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

            m_ActiveRayInteractors.RemoveAll(r => r == null);
            if (m_ActiveRayInteractors.Count == 0) ConfigureRayInteractors();

            for (int i = 0; i < m_ActiveRayInteractors.Count; i++)
            {
                var ray = m_ActiveRayInteractors[i];
                if (ray == null || !ray.gameObject.activeInHierarchy) continue;

                if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    if (DispatchHit(hit.collider.gameObject)) return;
                }

                Transform rayOrigin = ray.rayOriginTransform != null ? ray.rayOriginTransform : ray.transform;
                if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit physHit, 10f))
                {
                    if (DispatchHit(physHit.collider.gameObject)) return;
                }
            }
        }

        public bool DispatchHit(GameObject hitObj)
        {
            if (hitObj == null) return false;

            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return false;

            if (IsHitOnTarget(hitObj, m_ContinueScenarioButton?.gameObject))
            {
                OnContinueScenarioClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_GetOutOfCafeButton?.gameObject))
            {
                OnGetOutOfCafeClicked();
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
