using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Kannko.Phone;

namespace Kannko.Scenario
{
    /// <summary>
    /// Manages the Scenario 2 Introduction UI overlay and gameplay transition.
    /// Ensures smooth visual fading, zero raycast overhead, and immediate control restoration on START.
    /// Shows the phone message popup when START is clicked and scenario gameplay begins.
    /// Supports VR Raycast clicks, XR Device Simulator trigger clicks, direct Mouse clicks, and Laptop Keyboard (Enter/Space) shortcuts.
    /// </summary>
    [DisallowMultipleComponent]
    public class Scenario2IntroController : MonoBehaviour
    {
        [Header("UI Components")]
        [Tooltip("The main container panel or root Canvas for the introduction UI.")]
        [SerializeField] private GameObject m_IntroPanel;

        [Tooltip("Title TextMeshPro component.")]
        [SerializeField] private TextMeshProUGUI m_TitleText;

        [Tooltip("Story body TextMeshPro component.")]
        [SerializeField] private TextMeshProUGUI m_StoryText;

        [Tooltip("Button to start Scenario 2 gameplay.")]
        [SerializeField] private Button m_StartButton;

        [Tooltip("Button to return to the Main Menu.")]
        [SerializeField] private Button m_BackToMenuButton;

        [Header("Interactive Objects Gating")]
        [Tooltip("Root container holding interactive gameplay objects (Laptop, CoffeeCup, Smartphone, LaptopBag) that must remain hidden until START is clicked.")]
        [SerializeField] private GameObject m_InteractiveObjects;

        [Header("Gameplay Transition / Popup")]
        [Tooltip("The floating Telegram message popup associated with the smartphone.")]
        [SerializeField] private GameObject m_TelegramMessagePopup;

        [Header("Content Settings")]
        [SerializeField] private string m_StoryTitle = "SCENARIO 2";

        [TextArea(6, 12)]
        [SerializeField] private string m_StoryBody = 
            "You have just arrived at the cafe and begin\ntaking customer orders.\n\n" +
            "Everything seems normal until you notice\na message from your boss.\n\n" +
            "He needs an important financial report\nsent immediately because he has a meeting.\n\n" +
            "The message was sent 17 minutes ago.\n\n" +
            "You only have 3 minutes left.";

        [Header("Smooth Transition Settings")]
        [Tooltip("Duration in seconds for the smooth fade-out animation when START is pressed.")]
        [SerializeField] private float m_FadeOutDuration = 0.25f;

        [Header("Scene Navigation")]
        [Tooltip("Name of the Main Menu scene to load when Back To Menu is clicked.")]
        [SerializeField] private string m_MainMenuSceneName = "MainMenu";

        [Header("Events")]
        [Tooltip("Event triggered when the player clicks START and gameplay begins.")]
        public UnityEvent OnScenarioStarted = new UnityEvent();

        private bool m_IsScenarioStarted = false;
        private CanvasGroup m_CanvasGroup;
        private Canvas m_Canvas;
        private XRSimpleInteractable m_StartSimpleInteractable;

        public bool IsScenarioStarted => m_IsScenarioStarted;

        private void Awake()
        {
            if (m_IntroPanel == null)
            {
                m_IntroPanel = gameObject;
            }

            if (m_StartButton == null)
            {
                m_StartButton = GetComponentInChildren<Button>(true);
            }

            if (m_InteractiveObjects == null)
            {
                FindInteractiveObjects();
            }

            // Interactive gameplay objects must remain hidden/disabled until START is clicked
            if (m_InteractiveObjects != null && !m_IsScenarioStarted)
            {
                m_InteractiveObjects.SetActive(false);
            }

            if (m_TelegramMessagePopup == null)
            {
                FindTelegramMessagePopup();
            }

            // At the beginning, the TelegramMessagePopup object must not be visible/active.
            if (m_TelegramMessagePopup != null && !m_IsScenarioStarted)
            {
                m_TelegramMessagePopup.SetActive(false);
            }

            ApplyTextContent();
            SetupCanvasGroup();
            SetupButton3DCollidersAndXRInteractables();
        }

        private void SetupCanvasGroup()
        {
            GameObject target = m_IntroPanel != null ? m_IntroPanel : gameObject;
            m_CanvasGroup = target.GetComponent<CanvasGroup>();
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = target.AddComponent<CanvasGroup>();
            }

            m_Canvas = target.GetComponent<Canvas>();
            if (m_Canvas != null)
            {
                if (m_Canvas.renderMode == RenderMode.WorldSpace && m_Canvas.worldCamera == null)
                {
                    m_Canvas.worldCamera = Camera.main;
                }

                // Ensure GraphicRaycaster for Mouse Clicks
                var graphicRaycaster = target.GetComponent<GraphicRaycaster>();
                if (graphicRaycaster == null)
                {
                    target.AddComponent<GraphicRaycaster>();
                }

                // Ensure TrackedDeviceGraphicRaycaster for VR Controller Ray & XR Device Simulator
                var trackedRaycaster = target.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                if (trackedRaycaster == null)
                {
                    target.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
                }
            }

            // Ensure EventSystem has XRUIInputModule & InputSystemUIInputModule
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem");
                eventSystem = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            else
            {
                var xrInputModule = eventSystem.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                if (xrInputModule == null)
                {
                    eventSystem.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
                }

                var inputSystemModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (inputSystemModule == null)
                {
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }
        }

        private void SetupButton3DCollidersAndXRInteractables()
        {
            if (m_StartButton != null)
            {
                // Attach BoxCollider to Start Button if missing so 3D raycasts hit it
                BoxCollider btnCol = m_StartButton.GetComponent<BoxCollider>();
                if (btnCol == null)
                {
                    btnCol = m_StartButton.gameObject.AddComponent<BoxCollider>();
                    RectTransform rectTransform = m_StartButton.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        btnCol.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 10f);
                    }
                    else
                    {
                        btnCol.size = new Vector3(200f, 60f, 10f);
                    }
                }

                // Attach XRSimpleInteractable so XR Device Simulator / VR Controller Rays can trigger select/activate
                m_StartSimpleInteractable = m_StartButton.GetComponent<XRSimpleInteractable>();
                if (m_StartSimpleInteractable == null)
                {
                    m_StartSimpleInteractable = m_StartButton.gameObject.AddComponent<XRSimpleInteractable>();
                }

                m_StartSimpleInteractable.selectEntered.AddListener(_ => OnStartButtonClicked());
                m_StartSimpleInteractable.activated.AddListener(_ => OnStartButtonClicked());
            }

            // Also attach BoxCollider to Panel Root so mouse raycasts hit panel bounds
            GameObject panelRoot = m_IntroPanel != null ? m_IntroPanel : gameObject;
            BoxCollider panelCol = panelRoot.GetComponent<BoxCollider>();
            if (panelCol == null)
            {
                panelCol = panelRoot.AddComponent<BoxCollider>();
                RectTransform rectTransform = panelRoot.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    panelCol.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 5f);
                }
                else
                {
                    panelCol.size = new Vector3(600f, 400f, 5f);
                }
            }
        }

        private void OnEnable()
        {
            if (m_StartButton == null)
            {
                m_StartButton = GetComponentInChildren<Button>(true);
            }

            if (m_StartButton != null)
            {
                m_StartButton.onClick.RemoveListener(OnStartButtonClicked);
                m_StartButton.onClick.AddListener(OnStartButtonClicked);
            }

            if (m_BackToMenuButton != null)
            {
                m_BackToMenuButton.onClick.RemoveListener(OnBackToMenuButtonClicked);
                m_BackToMenuButton.onClick.AddListener(OnBackToMenuButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (m_StartButton != null)
            {
                m_StartButton.onClick.RemoveListener(OnStartButtonClicked);
            }

            if (m_BackToMenuButton != null)
            {
                m_BackToMenuButton.onClick.RemoveListener(OnBackToMenuButtonClicked);
            }
        }

        private void Start()
        {
            if (m_StartButton == null)
            {
                m_StartButton = GetComponentInChildren<Button>(true);
            }

            if (m_StartButton != null)
            {
                m_StartButton.onClick.RemoveListener(OnStartButtonClicked);
                m_StartButton.onClick.AddListener(OnStartButtonClicked);
            }

            // Ensure intro panel is active and visible at start if scenario hasn't started yet
            if (m_IntroPanel != null && !m_IsScenarioStarted)
            {
                m_IntroPanel.SetActive(true);
                if (m_CanvasGroup != null)
                {
                    m_CanvasGroup.alpha = 1f;
                    m_CanvasGroup.interactable = true;
                    m_CanvasGroup.blocksRaycasts = true;
                }
            }

            // Ensure interactive gameplay objects remain hidden until START is clicked
            if (!m_IsScenarioStarted)
            {
                if (m_InteractiveObjects == null)
                {
                    FindInteractiveObjects();
                }

                if (m_InteractiveObjects != null)
                {
                    m_InteractiveObjects.SetActive(false);
                }
            }

            // At the beginning, the TelegramMessagePopup object must not be visible/active (Toggle Active state false)
            if (!m_IsScenarioStarted)
            {
                if (m_TelegramMessagePopup == null)
                {
                    FindTelegramMessagePopup();
                }

                if (m_TelegramMessagePopup != null)
                {
                    m_TelegramMessagePopup.SetActive(false);
                }
            }

            // Ensure main camera is bound if needed for WorldSpace Raycaster
            if (m_Canvas != null && m_Canvas.renderMode == RenderMode.WorldSpace && m_Canvas.worldCamera == null)
            {
                m_Canvas.worldCamera = Camera.main;
            }

            OptimizeRaycastTargets();
        }

        private void Update()
        {
            if (m_IsScenarioStarted || !Application.isPlaying) return;

            // Method A & B Fallback (Direct Mouse Click / Reticle Raycast Click):
            // When user clicks Left Mouse Button while looking at panel/button, trigger START immediately!
            bool mousePressed = UnityEngine.InputSystem.Mouse.current != null &&
                                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            if (mousePressed)
            {
                Camera cam = Camera.main != null ? Camera.main : (m_Canvas != null ? m_Canvas.worldCamera : null);
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
                    if (Physics.Raycast(ray, out RaycastHit hit, 10f))
                    {
                        if (hit.collider.GetComponentInParent<Scenario2IntroController>() == this ||
                            hit.collider.gameObject == m_StartButton?.gameObject)
                        {
                            Debug.Log("[Scenario2IntroController] START button triggered via Desktop Mouse / Reticle Raycast!");
                            OnStartButtonClicked();
                            return;
                        }
                    }

                    // Also check mouse position if cursor is not locked
                    Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    Ray mouseRay = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
                    if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, 10f))
                    {
                        if (mouseHit.collider.GetComponentInParent<Scenario2IntroController>() == this ||
                            mouseHit.collider.gameObject == m_StartButton?.gameObject)
                        {
                            Debug.Log("[Scenario2IntroController] START button triggered via Direct Mouse Cursor Raycast!");
                            OnStartButtonClicked();
                            return;
                        }
                    }
                }
            }

            // Method C Shortcut (Keyboard Enter / Space Key):
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            {
                Debug.Log("[Scenario2IntroController] START triggered via Keyboard shortcut (Enter/Space key).");
                OnStartButtonClicked();
            }
        }

        /// <summary>
        /// Disables raycast targets on non-interactive decorative elements for optimal UI performance.
        /// </summary>
        public void OptimizeRaycastTargets()
        {
            GameObject root = m_IntroPanel != null ? m_IntroPanel : gameObject;
            var images = root.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                Button btn = img.GetComponentInParent<Button>();
                if (btn == null || (btn.targetGraphic != img))
                {
                    img.raycastTarget = false;
                }
            }

            var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                txt.raycastTarget = false;
            }

            if (m_StartButton != null)
            {
                if (m_StartButton.targetGraphic != null)
                {
                    m_StartButton.targetGraphic.raycastTarget = true;
                }
                var btnImages = m_StartButton.GetComponentsInChildren<Image>(true);
                foreach (var bImg in btnImages)
                {
                    bImg.raycastTarget = true;
                }
            }

            if (m_BackToMenuButton != null && m_BackToMenuButton.targetGraphic != null)
            {
                m_BackToMenuButton.targetGraphic.raycastTarget = true;
            }
        }

        /// <summary>
        /// Updates text components with Inspector-configured strings.
        /// </summary>
        public void ApplyTextContent()
        {
            if (m_TitleText != null)
            {
                m_TitleText.text = m_StoryTitle;
            }

            if (m_StoryText != null)
            {
                m_StoryText.text = m_StoryBody;
            }
        }

        /// <summary>
        /// Called when the player clicks the START button or triggers any interaction method.
        /// Immediately unblocks controls, displays the Phone Message Popup, smoothly fades out intro UI, and starts gameplay.
        /// </summary>
        public void OnStartButtonClicked()
        {
            if (m_IsScenarioStarted) return;
            m_IsScenarioStarted = true;

            // Immediately unblock player raycasts & interaction
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.interactable = false;
                m_CanvasGroup.blocksRaycasts = false;
            }

            // Enable existing interactive objects (Laptop, CoffeeCup, Smartphone, LaptopBag)
            if (m_InteractiveObjects == null)
            {
                FindInteractiveObjects();
            }

            if (m_InteractiveObjects != null)
            {
                m_InteractiveObjects.SetActive(true);
                Debug.Log("[Scenario2IntroController] InteractiveObjects enabled on START.");
            }

            // Activate Telegram Message Popup (Toggle Active state set to true) and start blinking
            if (m_TelegramMessagePopup == null)
            {
                FindTelegramMessagePopup();
            }

            var phoneController = Object.FindFirstObjectByType<SmartphoneController>();
            if (phoneController != null && phoneController.IsMessageRead)
            {
                Debug.Log("[Scenario2IntroController] Message is already marked READ. TelegramMessagePopup will remain hidden.");
            }
            else if (m_TelegramMessagePopup != null)
            {
                m_TelegramMessagePopup.SetActive(true);
                Debug.Log("[Scenario2IntroController] TelegramMessagePopup set to active/visible.");

                var blinker = m_TelegramMessagePopup.GetComponent<TelegramPopupBlinker>();
                if (blinker == null)
                {
                    blinker = m_TelegramMessagePopup.AddComponent<TelegramPopupBlinker>();
                }
                blinker.StartBlinking();
            }
            else
            {
                Debug.LogWarning("[Scenario2IntroController] TelegramMessagePopup not found to activate on START.");
            }

            Debug.Log("[Scenario 2] START clicked! Intro UI fading out. Gameplay starting.");

            // Trigger gameplay event
            OnScenarioStarted?.Invoke();

            // Smooth fade out
            if (gameObject.activeInHierarchy && m_FadeOutDuration > 0.01f && m_CanvasGroup != null)
            {
                StartCoroutine(FadeOutRoutine());
            }
            else
            {
                DeactivatePanel();
            }
        }

        /// <summary>
        /// Locates the existing TelegramMessagePopup GameObject in the scene hierarchy.
        /// Works even when the popup starts in an inactive state.
        /// </summary>
        public void FindTelegramMessagePopup()
        {
            if (m_TelegramMessagePopup != null) return;

            // 1. Look under Smartphone GameObject in scene
            GameObject smartphone = GameObject.Find("Smartphone");
            if (smartphone != null)
            {
                Transform popupTrans = smartphone.transform.Find("TelegramMessagePopup");
                if (popupTrans != null)
                {
                    m_TelegramMessagePopup = popupTrans.gameObject;
                    return;
                }
            }

            // 2. Look under InteractiveObjects root
            GameObject interactiveObjects = GameObject.Find("InteractiveObjects");
            if (interactiveObjects != null)
            {
                Transform popupTrans = interactiveObjects.transform.Find("Smartphone/TelegramMessagePopup");
                if (popupTrans != null)
                {
                    m_TelegramMessagePopup = popupTrans.gameObject;
                    return;
                }
            }

            // 3. Fallback: Search loaded scene transforms (including inactive GameObjects)
            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < allTransforms.Length; i++)
            {
                var t = allTransforms[i];
                if (t != null && t.gameObject.name == "TelegramMessagePopup" && t.gameObject.scene.isLoaded)
                {
                    m_TelegramMessagePopup = t.gameObject;
                    return;
                }
            }
        }

        /// <summary>
        /// Locates the existing InteractiveObjects root GameObject in the scene hierarchy.
        /// Works even when the container starts in an inactive state.
        /// </summary>
        public void FindInteractiveObjects()
        {
            if (m_InteractiveObjects != null) return;

            GameObject go = GameObject.Find("InteractiveObjects");
            if (go != null)
            {
                m_InteractiveObjects = go;
                return;
            }

            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < allTransforms.Length; i++)
            {
                var t = allTransforms[i];
                if (t != null && t.gameObject.name == "InteractiveObjects" && t.gameObject.scene.isLoaded)
                {
                    m_InteractiveObjects = t.gameObject;
                    return;
                }
            }
        }

        private IEnumerator FadeOutRoutine()
        {
            float elapsed = 0f;
            float startAlpha = m_CanvasGroup != null ? m_CanvasGroup.alpha : 1f;

            while (elapsed < m_FadeOutDuration)
            {
                elapsed += Time.deltaTime;
                if (m_CanvasGroup != null)
                {
                    m_CanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / m_FadeOutDuration);
                }
                yield return null;
            }

            DeactivatePanel();
        }

        private void DeactivatePanel()
        {
            if (m_IntroPanel != null)
            {
                m_IntroPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Called when the player clicks the BACK TO MENU button.
        /// </summary>
        public void OnBackToMenuButtonClicked()
        {
            Debug.Log($"[Scenario 2] Returning to Main Menu: '{m_MainMenuSceneName}'");
            if (Application.CanStreamedLevelBeLoaded(m_MainMenuSceneName))
            {
                SceneManager.LoadScene(m_MainMenuSceneName);
            }
            else
            {
                Debug.LogWarning($"[Scenario 2] Main Menu scene '{m_MainMenuSceneName}' is not in Build Settings or does not exist. Loading scene index 0 as fallback if available.");
                if (SceneManager.sceneCountInBuildSettings > 0)
                {
                    SceneManager.LoadScene(0);
                }
            }
        }

        private void OnValidate()
        {
            ApplyTextContent();
        }
    }
}
