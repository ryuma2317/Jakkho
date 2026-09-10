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
    /// Manages the Wi-Fi flyout view navigation between NetworkList and PasswordEntryUI.
    /// Supports selecting network items (Item_Jakkho, Item_Other, Item_Jakko_Free),
    /// entering a password, and returning to NetworkList via the Cancel button.
    /// 
    /// Ensures 100% reliable interaction in BOTH:
    /// 1. Real VR: VR controller rays, XRI simple interactables, and 3D hit detection.
    /// 2. Laptop / PC Test: Direct mouse clicks, reticle raycasts, and UI pointer events.
    /// 
    /// There is strictly NO Back button anywhere in this flow.
    /// </summary>
    [DisallowMultipleComponent]
    public class LaptopWifiPasswordUIController : MonoBehaviour
    {
        [Header("View Containers")]
        [Tooltip("Container holding the list of Wi-Fi network buttons.")]
        [SerializeField] private GameObject m_NetworkList;

        [Tooltip("Container holding the Wi-Fi password entry UI.")]
        [SerializeField] private GameObject m_PasswordEntryUI;

        [Header("Network Selection Buttons")]
        [SerializeField] private Button m_ItemJakkhoButton;
        [SerializeField] private Button m_ItemOtherButton;
        [SerializeField] private Button m_ItemJakkoFreeButton;

        [Header("Password UI Controls")]
        [SerializeField] private TextMeshProUGUI m_SelectedNetworkTitle;
        [SerializeField] private TextMeshProUGUI m_InstructionText;
        [SerializeField] private TMP_InputField m_PasswordInputField;
        [SerializeField] private Button m_ConnectButton;
        [SerializeField] private Button m_CancelButton;
        [SerializeField] private TextMeshProUGUI m_FeedbackText;

        [Header("Expected Credentials")]
        [SerializeField] private string m_TargetNetworkName = "Jakkho-Cooffee";
        [SerializeField] private string m_CorrectPassword = "LATTE";

        [Header("VR Controller Input Actions")]
        [SerializeField] private InputActionProperty m_LeftTriggerAction;
        [SerializeField] private InputActionProperty m_RightTriggerAction;

        [Header("Click Settings")]
        [SerializeField] private float m_ClickDebounceTime = 0.2f;

        [Header("Events")]
        public UnityEvent<string> OnNetworkSelected = new UnityEvent<string>();
        public UnityEvent OnPasswordUICanceled = new UnityEvent();
        public UnityEvent<bool> OnConnectionAttempted = new UnityEvent<bool>();

        private string m_CurrentSelectedNetwork = "";
        private bool m_IsConnected = false;
        private float m_LastClickTime = -1f;
        private readonly List<XRRayInteractor> m_ActiveRayInteractors = new List<XRRayInteractor>();

        public bool IsPasswordUIVisible => m_PasswordEntryUI != null && m_PasswordEntryUI.activeSelf;
        public bool IsNetworkListVisible => m_NetworkList != null && m_NetworkList.activeSelf;
        public string CurrentSelectedNetwork => m_CurrentSelectedNetwork;
        public bool IsConnected => m_IsConnected;
        public TMP_InputField PasswordInputField => m_PasswordInputField;
        public Button CancelButton => m_CancelButton;
        public Button ConnectButton => m_ConnectButton;

        private void Awake()
        {
            FindReferencesIfNull();
            EnsureCollidersAndInteractables();
            InitializeInputActions();
            SetupButtonListeners();
            ShowNetworkList();
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
            ShowNetworkList();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            HandleVRControllerRayClicks();
            HandleDesktopMouseClicks();
            HandleKeyboardShortcuts();
        }

        public void FindReferencesIfNull()
        {
            // Auto-discover NetworkList and PasswordEntryUI under this or parent transform
            if (m_NetworkList == null)
            {
                Transform listTrans = transform.Find("NetworkList");
                if (listTrans != null) m_NetworkList = listTrans.gameObject;
            }

            if (m_PasswordEntryUI == null)
            {
                Transform passTrans = transform.Find("PasswordEntryUI");
                if (passTrans != null) m_PasswordEntryUI = passTrans.gameObject;
            }

            // Find buttons under NetworkList
            if (m_NetworkList != null)
            {
                if (m_ItemJakkhoButton == null)
                {
                    Transform t = m_NetworkList.transform.Find("Item_Jakkho");
                    if (t == null) t = m_NetworkList.transform.Find("Item_Jakko");
                    if (t != null) m_ItemJakkhoButton = t.GetComponent<Button>();
                }

                if (m_ItemOtherButton == null)
                {
                    Transform t = m_NetworkList.transform.Find("Item_Other");
                    if (t == null) t = m_NetworkList.transform.Find("Item_other");
                    if (t != null) m_ItemOtherButton = t.GetComponent<Button>();
                }

                if (m_ItemJakkoFreeButton == null)
                {
                    Transform t = m_NetworkList.transform.Find("Item_Jakko_Free");
                    if (t != null) m_ItemJakkoFreeButton = t.GetComponent<Button>();
                }
            }

            // Find controls under PasswordEntryUI
            if (m_PasswordEntryUI != null)
            {
                if (m_SelectedNetworkTitle == null)
                {
                    Transform t = m_PasswordEntryUI.transform.Find("SelectedNetworkTitle");
                    if (t != null) m_SelectedNetworkTitle = t.GetComponent<TextMeshProUGUI>();
                }

                if (m_InstructionText == null)
                {
                    Transform t = m_PasswordEntryUI.transform.Find("InstructionText");
                    if (t != null) m_InstructionText = t.GetComponent<TextMeshProUGUI>();
                }

                if (m_PasswordInputField == null)
                {
                    m_PasswordInputField = m_PasswordEntryUI.GetComponentInChildren<TMP_InputField>(true);
                }

                if (m_ConnectButton == null)
                {
                    Transform t = m_PasswordEntryUI.transform.Find("ButtonRow/ConnectButton");
                    if (t == null) t = m_PasswordEntryUI.transform.Find("ConnectButton");
                    if (t != null) m_ConnectButton = t.GetComponent<Button>();
                }

                if (m_CancelButton == null)
                {
                    Transform t = m_PasswordEntryUI.transform.Find("ButtonRow/CancelButton");
                    if (t == null) t = m_PasswordEntryUI.transform.Find("CancelButton");
                    if (t != null) m_CancelButton = t.GetComponent<Button>();
                }

                if (m_FeedbackText == null)
                {
                    Transform t = m_PasswordEntryUI.transform.Find("FeedbackText");
                    if (t != null) m_FeedbackText = t.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        public void SetupButtonListeners()
        {
            if (m_ItemJakkhoButton != null)
            {
                m_ItemJakkhoButton.onClick.RemoveListener(OnItemJakkhoClicked);
                m_ItemJakkhoButton.onClick.AddListener(OnItemJakkhoClicked);
            }

            if (m_ItemOtherButton != null)
            {
                m_ItemOtherButton.onClick.RemoveListener(OnItemOtherClicked);
                m_ItemOtherButton.onClick.AddListener(OnItemOtherClicked);
            }

            if (m_ItemJakkoFreeButton != null)
            {
                m_ItemJakkoFreeButton.onClick.RemoveListener(OnItemJakkoFreeClicked);
                m_ItemJakkoFreeButton.onClick.AddListener(OnItemJakkoFreeClicked);
            }

            if (m_ConnectButton != null)
            {
                m_ConnectButton.onClick.RemoveListener(OnConnectClicked);
                m_ConnectButton.onClick.AddListener(OnConnectClicked);
            }

            if (m_CancelButton != null)
            {
                m_CancelButton.onClick.RemoveListener(OnCancelClicked);
                m_CancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        public void RemoveButtonListeners()
        {
            if (m_ItemJakkhoButton != null) m_ItemJakkhoButton.onClick.RemoveListener(OnItemJakkhoClicked);
            if (m_ItemOtherButton != null) m_ItemOtherButton.onClick.RemoveListener(OnItemOtherClicked);
            if (m_ItemJakkoFreeButton != null) m_ItemJakkoFreeButton.onClick.RemoveListener(OnItemJakkoFreeClicked);
            if (m_ConnectButton != null) m_ConnectButton.onClick.RemoveListener(OnConnectClicked);
            if (m_CancelButton != null) m_CancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        private void OnItemJakkhoClicked()
        {
            SelectNetwork("Jakkho-Cooffee");
        }

        private void OnItemOtherClicked()
        {
            SelectNetwork("Slanh Wifi Ke Mes");
        }

        private void OnItemJakkoFreeClicked()
        {
            var freeItemCtrl = m_ItemJakkoFreeButton != null ? m_ItemJakkoFreeButton.GetComponent<JakkoFreeWifiItemController>() : null;
            if (freeItemCtrl != null)
            {
                freeItemCtrl.TriggerInteraction();
                return;
            }

            SelectNetwork("Jakkho-Cooffee_free");
        }

        /// <summary>
        /// Displays the Wi-Fi password entry screen for the chosen network.
        /// Hides the NetworkList view.
        /// </summary>
        public void SelectNetwork(string networkName)
        {
            FindReferencesIfNull();
            m_CurrentSelectedNetwork = networkName;

            if (m_NetworkList != null)
            {
                m_NetworkList.SetActive(false);
            }

            if (m_PasswordEntryUI != null)
            {
                m_PasswordEntryUI.SetActive(true);
            }

            if (m_SelectedNetworkTitle != null)
            {
                m_SelectedNetworkTitle.text = $"<b>{networkName}</b>";
            }

            if (m_InstructionText != null)
            {
                m_InstructionText.text = "Enter the network security key";
            }

            if (m_PasswordInputField != null)
            {
                m_PasswordInputField.text = "";
                m_PasswordInputField.ActivateInputField();
            }

            if (m_FeedbackText != null)
            {
                m_FeedbackText.text = "";
            }

            EnsureCollidersAndInteractables();

            OnNetworkSelected?.Invoke(networkName);
            Debug.Log($"[LaptopWifiPasswordUIController] Network selected: '{networkName}'. Password UI displayed.");
        }

        /// <summary>
        /// Cancel button behavior:
        /// - Closes/hides the password-entry UI.
        /// - Returns to the previous NetworkList screen.
        /// - Does NOT close FloatingLaptopPanel.
        /// - Does NOT exit Wi-Fi screen completely.
        /// - Clears/resets the password input.
        /// </summary>
        public void OnCancelClicked()
        {
            FindReferencesIfNull();

            if (m_PasswordEntryUI != null)
            {
                m_PasswordEntryUI.SetActive(false);
            }

            if (m_NetworkList != null)
            {
                m_NetworkList.SetActive(true);
            }

            if (m_PasswordInputField != null)
            {
                m_PasswordInputField.text = "";
            }

            if (m_FeedbackText != null)
            {
                m_FeedbackText.text = "";
            }

            EnsureCollidersAndInteractables();

            OnPasswordUICanceled?.Invoke();
            Debug.Log("[LaptopWifiPasswordUIController] Cancel clicked. Returned to NetworkList. Password cleared.");
        }

        /// <summary>
        /// Connect button behavior:
        /// Validates entered password and provides user feedback / updates state.
        /// </summary>
        public void OnConnectClicked()
        {
            FindReferencesIfNull();

            string enteredPassword = m_PasswordInputField != null ? m_PasswordInputField.text.Trim() : "";

            bool isSuccess = false;
            // Case-insensitive match for LATTE
            if (string.Equals(enteredPassword, m_CorrectPassword, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(enteredPassword, "LATTE", StringComparison.OrdinalIgnoreCase))
            {
                isSuccess = true;
            }

            if (isSuccess)
            {
                m_IsConnected = true;
                if (m_FeedbackText != null)
                {
                    m_FeedbackText.text = "<color=#4CD964><b>Connected, secured</b></color>";
                }
                Debug.Log($"[LaptopWifiPasswordUIController] Successfully connected to '{m_CurrentSelectedNetwork}'!");
            }
            else
            {
                if (m_FeedbackText != null)
                {
                    m_FeedbackText.text = "<color=#FF453A><b>The network security key isn't correct. Please try again.</b></color>";
                }
                Debug.LogWarning($"[LaptopWifiPasswordUIController] Connection failed for '{m_CurrentSelectedNetwork}'. Incorrect key: '{enteredPassword}'");
            }

            OnConnectionAttempted?.Invoke(isSuccess);
        }

        /// <summary>
        /// Resets the Wi-Fi panel back to the default NetworkList view.
        /// </summary>
        public void ShowNetworkList()
        {
            FindReferencesIfNull();

            if (m_PasswordEntryUI != null)
            {
                m_PasswordEntryUI.SetActive(false);
            }

            if (m_NetworkList != null)
            {
                m_NetworkList.SetActive(true);
            }

            if (m_PasswordInputField != null)
            {
                m_PasswordInputField.text = "";
            }

            if (m_FeedbackText != null)
            {
                m_FeedbackText.text = "";
            }

            EnsureCollidersAndInteractables();
        }

        /// <summary>
        /// Ensures 3D BoxColliders and XRSimpleInteractable components are properly attached and sized
        /// on all interactable elements (Item_Jakkho, Item_Other, Item_Jakko_Free, ConnectButton, CancelButton).
        /// </summary>
        public void EnsureCollidersAndInteractables()
        {
            FindReferencesIfNull();

            // 1. NetworkList items
            if (m_ItemJakkhoButton != null)
            {
                SetupElementColliderAndInteractable(m_ItemJakkhoButton.gameObject, OnItemJakkhoClicked);
            }
            if (m_ItemOtherButton != null)
            {
                SetupElementColliderAndInteractable(m_ItemOtherButton.gameObject, OnItemOtherClicked);
            }
            if (m_ItemJakkoFreeButton != null)
            {
                SetupElementColliderAndInteractable(m_ItemJakkoFreeButton.gameObject, OnItemJakkoFreeClicked);
            }

            // 2. PasswordEntryUI items
            if (m_ConnectButton != null)
            {
                SetupElementColliderAndInteractable(m_ConnectButton.gameObject, OnConnectClicked);
            }
            if (m_CancelButton != null)
            {
                SetupElementColliderAndInteractable(m_CancelButton.gameObject, OnCancelClicked);
            }
            if (m_PasswordInputField != null)
            {
                SetupElementColliderAndInteractable(m_PasswordInputField.gameObject, () =>
                {
                    m_PasswordInputField.ActivateInputField();
                });
            }
        }

        private void SetupElementColliderAndInteractable(GameObject target, Action onClickAction)
        {
            if (target == null) return;

            // Setup BoxCollider
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

            // Setup XRSimpleInteractable
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

                // Check XRRayInteractor hit
                if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    if (EvaluateHitElement(hit.collider.gameObject))
                    {
                        return;
                    }
                }

                // Direct physics raycast from ray origin
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

            // When NetworkList is visible:
            if (IsNetworkListVisible)
            {
                if (IsHitOnTarget(hitObj, m_ItemJakkhoButton?.gameObject))
                {
                    m_LastClickTime = Time.unscaledTime;
                    Debug.Log("[LaptopWifiPasswordUIController] Hit detected on Item_Jakkho. Opening password UI.");
                    OnItemJakkhoClicked();
                    return true;
                }

                if (IsHitOnTarget(hitObj, m_ItemOtherButton?.gameObject))
                {
                    m_LastClickTime = Time.unscaledTime;
                    Debug.Log("[LaptopWifiPasswordUIController] Hit detected on Item_Other. Opening password UI.");
                    OnItemOtherClicked();
                    return true;
                }

                if (IsHitOnTarget(hitObj, m_ItemJakkoFreeButton?.gameObject))
                {
                    m_LastClickTime = Time.unscaledTime;
                    Debug.Log("[LaptopWifiPasswordUIController] Hit detected on Item_Jakko_Free. Opening password UI.");
                    OnItemJakkoFreeClicked();
                    return true;
                }
            }

            // When PasswordEntryUI is visible:
            if (IsPasswordUIVisible)
            {
                if (IsHitOnTarget(hitObj, m_ConnectButton?.gameObject))
                {
                    m_LastClickTime = Time.unscaledTime;
                    Debug.Log("[LaptopWifiPasswordUIController] Hit detected on ConnectButton.");
                    OnConnectClicked();
                    return true;
                }

                if (IsHitOnTarget(hitObj, m_CancelButton?.gameObject))
                {
                    m_LastClickTime = Time.unscaledTime;
                    Debug.Log("[LaptopWifiPasswordUIController] Hit detected on CancelButton.");
                    OnCancelClicked();
                    return true;
                }

                if (IsHitOnTarget(hitObj, m_PasswordInputField?.gameObject))
                {
                    m_LastClickTime = Time.unscaledTime;
                    Debug.Log("[LaptopWifiPasswordUIController] Hit detected on PasswordInputField. Activating input.");
                    m_PasswordInputField.ActivateInputField();
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

        private void HandleKeyboardShortcuts()
        {
            // Escape key cancels password screen if visible
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (IsPasswordUIVisible)
                {
                    OnCancelClicked();
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsPasswordUIVisible)
                {
                    OnCancelClicked();
                    return;
                }
            }

            // Quick shortcuts for desktop testing
            if (IsNetworkListVisible)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                {
                    SelectNetwork("Jakkho-Cooffee");
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                {
                    SelectNetwork("Jakkho-Cooffee_free");
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                {
                    SelectNetwork("Slanh Wifi Ke Mes");
                }
            }
            else if (IsPasswordUIVisible)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    OnConnectClicked();
                }
            }
        }
    }
}

