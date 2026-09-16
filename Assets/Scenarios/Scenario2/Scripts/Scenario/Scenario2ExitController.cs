using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Kannko.Scenario2
{
    /// <summary>
    /// Controls the fixed camera-facing World Space exit confirmation dialog for Scenario 2.
    /// Manages transition between laptop interaction and the exit dialog,
    /// and performs safe scene exit to ScenarioSelection or MainMenu without calling Application.Quit().
    /// </summary>
    [DisallowMultipleComponent]
    public class Scenario2ExitController : MonoBehaviour
    {
        [Header("UI Panels")]
        [Tooltip("The camera-facing World Space confirmation panel.")]
        [SerializeField] private GameObject m_ExitConfirmationPanel;

        [Tooltip("Reference to the FloatingLaptopPanel under WorldUI.")]
        [SerializeField] private GameObject m_FloatingLaptopPanel;

        [Tooltip("Reference to the EnlargedWifiPanel to ensure it is visible when returning to laptop.")]
        [SerializeField] private GameObject m_EnlargedWifiPanel;

        [Tooltip("Reference to the StopPlayingDecisionPanel under WorldUI.")]
        [SerializeField] private GameObject m_StopPlayingDecisionPanel;

        [Header("Buttons")]
        [SerializeField] private Button m_BackToScenarioButton;
        [SerializeField] private Button m_ExitScenarioButton;

        [Header("Scene Navigation")]
        [SerializeField] private string m_ScenarioSelectionScene = "ScenarioSelection";
        [SerializeField] private string m_MainMenuScene = "MainMenu";

        [Header("Settings")]
        [SerializeField] private float m_ClickDebounceTime = 0.25f;

        private float m_LastClickTime = -1f;
        private readonly List<XRRayInteractor> m_ActiveRayInteractors = new List<XRRayInteractor>();

        public bool IsConfirmationOpen => m_ExitConfirmationPanel != null && m_ExitConfirmationPanel.activeSelf;
        public GameObject StopPlayingDecisionPanel => m_StopPlayingDecisionPanel;

        private void Awake()
        {
            FindReferencesIfNull();
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
            SetupButtonListeners();
            EnsureCollidersAndInteractables();
        }

        private void OnDisable()
        {
            RemoveButtonListeners();
        }

        private void Update()
        {
            if (!Application.isPlaying || !IsConfirmationOpen) return;

            HandleVRControllerRayClicks();
        }

        public void FindReferencesIfNull()
        {
            if (m_ExitConfirmationPanel == null)
            {
                m_ExitConfirmationPanel = gameObject;
            }

            if (m_FloatingLaptopPanel == null)
            {
                var wui = GameObject.Find("WorldUI");
                if (wui != null)
                {
                    var t = wui.transform.Find("FloatingLaptopPanel");
                    if (t != null) m_FloatingLaptopPanel = t.gameObject;
                }
            }

            if (m_EnlargedWifiPanel == null && m_FloatingLaptopPanel != null)
            {
                var t = m_FloatingLaptopPanel.transform.Find("ScreenContainer/EnlargedWifiPanel");
                if (t != null) m_EnlargedWifiPanel = t.gameObject;
            }

            if (m_StopPlayingDecisionPanel == null)
            {
                var wui = GameObject.Find("WorldUI");
                if (wui != null)
                {
                    var t = wui.transform.Find("StopPlayingDecisionPanel");
                    if (t != null) m_StopPlayingDecisionPanel = t.gameObject;
                }
                if (m_StopPlayingDecisionPanel == null)
                {
                    var go = GameObject.Find("StopPlayingDecisionPanel");
                    if (go != null) m_StopPlayingDecisionPanel = go;
                }
            }

            if (m_BackToScenarioButton == null && m_ExitConfirmationPanel != null)
            {
                var btn = m_ExitConfirmationPanel.transform.Find("ActionButtons/BackToScenarioButton");
                if (btn == null) btn = m_ExitConfirmationPanel.transform.Find("BackToScenarioButton");
                if (btn != null) m_BackToScenarioButton = btn.GetComponent<Button>();
            }

            if (m_ExitScenarioButton == null && m_ExitConfirmationPanel != null)
            {
                var btn = m_ExitConfirmationPanel.transform.Find("ActionButtons/ExitScenarioButton");
                if (btn == null) btn = m_ExitConfirmationPanel.transform.Find("ExitScenarioButton");
                if (btn != null) m_ExitScenarioButton = btn.GetComponent<Button>();
            }
        }

        public void SetupButtonListeners()
        {
            if (m_BackToScenarioButton != null)
            {
                m_BackToScenarioButton.onClick.RemoveListener(OnBackToScenarioClicked);
                m_BackToScenarioButton.onClick.AddListener(OnBackToScenarioClicked);
            }

            if (m_ExitScenarioButton != null)
            {
                m_ExitScenarioButton.onClick.RemoveListener(OnExitScenarioConfirmed);
                m_ExitScenarioButton.onClick.AddListener(OnExitScenarioConfirmed);
            }
        }

        public void RemoveButtonListeners()
        {
            if (m_BackToScenarioButton != null)
            {
                m_BackToScenarioButton.onClick.RemoveListener(OnBackToScenarioClicked);
            }

            if (m_ExitScenarioButton != null)
            {
                m_ExitScenarioButton.onClick.RemoveListener(OnExitScenarioConfirmed);
            }
        }

        /// <summary>
        /// Displays the camera-facing exit confirmation dialog.
        /// Hides the laptop screen so the player focuses entirely on the exit choice.
        /// </summary>
        public void ShowExitConfirmation()
        {
            FindReferencesIfNull();

            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(false);
            }

            if (m_StopPlayingDecisionPanel != null)
            {
                m_StopPlayingDecisionPanel.SetActive(false);
            }

            if (m_ExitConfirmationPanel != null)
            {
                m_ExitConfirmationPanel.SetActive(true);
            }

            EnsureCollidersAndInteractables();
            Debug.Log("[Scenario2ExitController] Showing Scenario 2 exit confirmation dialog.");
        }

        /// <summary>
        /// Hides the exit confirmation dialog.
        /// </summary>
        public void HideExitConfirmation()
        {
            if (m_ExitConfirmationPanel != null)
            {
                m_ExitConfirmationPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Player cancels exiting: hides the exit dialog and returns to the laptop.
        /// </summary>
        public void OnBackToScenarioClicked()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            HideExitConfirmation();

            if (m_StopPlayingDecisionPanel != null)
            {
                m_StopPlayingDecisionPanel.SetActive(false);
            }

            if (m_FloatingLaptopPanel != null)
            {
                m_FloatingLaptopPanel.SetActive(true);
            }

            if (m_EnlargedWifiPanel != null)
            {
                m_EnlargedWifiPanel.SetActive(true);
            }

            Debug.Log("[Scenario2ExitController] Player returned to Scenario 2 laptop.");
        }

        /// <summary>
        /// Player confirms scenario exit: routes to ScenarioSelection or MainMenu.
        /// Strictly avoids Application.Quit().
        /// </summary>
        public void OnExitScenarioConfirmed()
        {
            if (Application.isPlaying && Time.unscaledTime - m_LastClickTime < m_ClickDebounceTime) return;
            m_LastClickTime = Time.unscaledTime;

            Debug.Log("[Scenario2ExitController] Player confirmed exit. Loading scenario selection / main menu...");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScenarioSelection();
            }
            else if (Application.CanStreamedLevelBeLoaded(m_ScenarioSelectionScene))
            {
                SceneManager.LoadScene(m_ScenarioSelectionScene);
            }
            else if (Application.CanStreamedLevelBeLoaded(m_MainMenuScene))
            {
                SceneManager.LoadScene(m_MainMenuScene);
            }
            else if (SceneManager.sceneCountInBuildSettings > 0)
            {
                SceneManager.LoadScene(0);
            }
            else
            {
                Debug.LogWarning("[Scenario2ExitController] No valid scene found in Build Settings to exit to.");
            }
        }

        public void EnsureCollidersAndInteractables()
        {
            FindReferencesIfNull();

            if (m_BackToScenarioButton != null)
            {
                SetupElementColliderAndInteractable(m_BackToScenarioButton.gameObject, OnBackToScenarioClicked);
            }

            if (m_ExitScenarioButton != null)
            {
                SetupElementColliderAndInteractable(m_ExitScenarioButton.gameObject, OnExitScenarioConfirmed);
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
                col.center = new Vector3(rt.rect.center.x, rt.rect.center.y, 0f);
                col.size = new Vector3(rt.rect.width, rt.rect.height, 50f);
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

            if (IsHitOnTarget(hitObj, m_BackToScenarioButton?.gameObject))
            {
                OnBackToScenarioClicked();
                return true;
            }

            if (IsHitOnTarget(hitObj, m_ExitScenarioButton?.gameObject))
            {
                OnExitScenarioConfirmed();
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
