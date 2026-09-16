using System;
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
    /// Controls the Scenario 2 introduction dialogue overlay and gameplay start transition.
    /// Displays a game dialogue box at the bottom-left of the Main Camera view, matching
    /// Scenario 1's dialogue design and camera-anchored positioning.
    /// Cycles through the 3 narrative dialogue phrases with [NEXT >] buttons, then [START >].
    /// On [START >], enables interactive objects, activates the phone notification popup, and starts scenario gameplay.
    /// Supports VR Raycasts, XR Device Simulator, Mouse clicks, and Keyboard (Enter/Space) shortcuts.
    /// </summary>
    [DisallowMultipleComponent]
    public class Scenario2IntroController : MonoBehaviour
    {
        [Header("UI Components")]
        [Tooltip("The main container panel or root Canvas for the introduction UI.")]
        [SerializeField] private GameObject m_IntroPanel;

        [Tooltip("Header / Badge TextMeshPro component.")]
        [SerializeField] private TextMeshProUGUI m_TitleText;

        [Tooltip("Step Counter TextMeshPro component (e.g., [1/3]).")]
        [SerializeField] private TextMeshProUGUI m_StepCounterText;

        [Tooltip("Dialogue body TextMeshPro component.")]
        [SerializeField] private TextMeshProUGUI m_StoryText;

        [Tooltip("Action button that progresses through [NEXT >] and finally [START >].")]
        [SerializeField] private Button m_ActionButton;

        [Tooltip("Text label inside the action button.")]
        [SerializeField] private TextMeshProUGUI m_ActionButtonText;

        [Tooltip("Target graphic/image of the action button for color changes.")]
        [SerializeField] private Image m_ActionButtonImage;

        [Tooltip("Button to return to the Main Menu.")]
        [SerializeField] private Button m_BackToMenuButton;

        [Header("Interactive Objects Gating")]
        [Tooltip("Root container holding interactive gameplay objects (Laptop, CoffeeCup, Smartphone, LaptopBag) that must remain hidden until START is clicked.")]
        [SerializeField] private GameObject m_InteractiveObjects;

        [Header("Gameplay Transition / Popup")]
        [Tooltip("The floating Telegram message popup associated with the smartphone.")]
        [SerializeField] private GameObject m_TelegramMessagePopup;

        [Header("Camera Attachment & Manual Transform Settings")]
        [Tooltip("Whether to anchor this canvas to Camera.main. If false, the panel remains in world space wherever you place it in the scene.")]
        [SerializeField] private bool m_AttachToMainCamera = true;
        [Tooltip("If true, preserves your custom position, rotation, and scale set in the Unity Inspector/Scene instead of resetting to defaults.")]
        [SerializeField] private bool m_PreserveLocalTransformOffsets = true;
        [SerializeField] private float m_CameraDistance = 1.25f;
        [SerializeField] private float m_CanvasScale = 0.0011f;
        [Tooltip("Whether to automatically size BoxColliders to RectTransforms on Awake. Uncheck if you want to configure BoxCollider sizes manually.")]
        [SerializeField] private bool m_AutoAdjustColliders = true;

        [Header("Dialogue Sequence Settings")]
        [SerializeField] private string m_BadgeTitle = "◆ SCENARIO 2 BRIEFING";

        [TextArea(3, 6)]
        [SerializeField] private string[] m_DialogueLines = new string[]
        {
            "You have just arrived at the cafe and ordered the drink .\n\nEverything seems normal until you notice a message from your boss.",
            "He needs an important financial report sent immediately because he has a meeting.",
            "The message was sent 17 minutes ago.\n\nYou only have 3 minutes left."
        };

        [Header("Button Colors")]
        [SerializeField] private Color m_NextButtonColor = new Color(0.01f, 0.52f, 0.78f, 1f);      // #0284C7 Tech Blue
        [SerializeField] private Color m_StartButtonColor = new Color(0.06f, 0.72f, 0.50f, 1f);     // #10B981 Emerald Green

        [Header("Typewriter Effect Settings")]
        [Tooltip("Whether to display dialogue text with a typewriter effect.")]
        [SerializeField] private bool m_EnableTypewriterEffect = true;

        [Tooltip("Typing speed in seconds per character.")]
        [Range(0.005f, 0.1f)]
        [SerializeField] private float m_TypingSpeed = 0.025f;

        [Tooltip("Optional typing sound effect played as characters are displayed.")]
        [SerializeField] private AudioClip m_TypingSFX;

        [Tooltip("Extra pause duration after punctuation (. ! ?) in seconds.")]
        [SerializeField] private float m_PunctuationPause = 0.12f;

        [Header("Smooth Transition Settings")]
        [Tooltip("Duration in seconds for the smooth fade-out animation when START is pressed.")]
        [SerializeField] private float m_FadeOutDuration = 0.25f;

        [Header("Scene Navigation")]
        [Tooltip("Name of the Main Menu scene to load when Back To Menu is clicked.")]
        [SerializeField] private string m_MainMenuSceneName = "MainMenu";

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField] private AudioClip m_NextStepSFX;
        [SerializeField] private AudioClip m_StartSFX;

        [Header("Events")]
        [Tooltip("Event triggered when player advances to the next dialogue line.")]
        public UnityEvent<int> OnDialogueStepChanged = new UnityEvent<int>();

        [Tooltip("Event triggered when the player clicks START and gameplay begins.")]
        public UnityEvent OnScenarioStarted = new UnityEvent();

        [SerializeField] private int m_CurrentStepIndex = 0;
        private bool m_IsScenarioStarted = false;
        private bool m_IsTyping = false;
        private Coroutine m_TypewriterCoroutine;
        private CanvasGroup m_CanvasGroup;
        private Canvas m_Canvas;
        private XRSimpleInteractable m_ActionSimpleInteractable;
        private XRSimpleInteractable m_MenuSimpleInteractable;

        public bool IsScenarioStarted => m_IsScenarioStarted;
        public bool IsTyping => m_IsTyping;
        public int CurrentStepIndex { get => m_CurrentStepIndex; set => m_CurrentStepIndex = value; }
        public string[] DialogueLines { get => m_DialogueLines; set => m_DialogueLines = value; }
        public float TypingSpeed { get => m_TypingSpeed; set => m_TypingSpeed = value; }
        public bool EnableTypewriterEffect { get => m_EnableTypewriterEffect; set => m_EnableTypewriterEffect = value; }
        public Button StartButton => m_ActionButton;

        public void ResetToFirstStep()
        {
            m_CurrentStepIndex = 0;
            m_IsScenarioStarted = false;
            m_IsTyping = false;
            UpdateDialogueDisplay();
        }

        private void Awake()
        {
            if (m_IntroPanel == null)
            {
                m_IntroPanel = gameObject;
            }

            if (m_AudioSource == null)
            {
                m_AudioSource = GetComponent<AudioSource>();
            }

            EnsureAttachedToMainCamera();

            if (m_InteractiveObjects == null)
            {
                FindInteractiveObjects();
            }

            // Interactive gameplay objects must remain hidden until START is clicked
            if (m_InteractiveObjects != null && !m_IsScenarioStarted)
            {
                m_InteractiveObjects.SetActive(false);
            }

            if (m_TelegramMessagePopup == null)
            {
                FindTelegramMessagePopup();
            }

            // Telegram popup must not be visible until START is clicked
            if (m_TelegramMessagePopup != null && !m_IsScenarioStarted)
            {
                m_TelegramMessagePopup.SetActive(false);
            }

            SetupCanvasGroup();
            SetupButton3DCollidersAndXRInteractables();
        }

        public void EnsureAttachedToMainCamera()
        {
            if (!m_AttachToMainCamera) return;

            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam != null && transform.parent != cam.transform)
            {
                Vector3 currentLocalPos = transform.localPosition;
                Quaternion currentLocalRot = transform.localRotation;
                Vector3 currentLocalScale = transform.localScale;

                transform.SetParent(cam.transform, false);

                if (!m_PreserveLocalTransformOffsets)
                {
                    transform.localPosition = new Vector3(0f, 0f, m_CameraDistance);
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one * m_CanvasScale;
                }
                else
                {
                    transform.localPosition = currentLocalPos;
                    transform.localRotation = currentLocalRot;
                    transform.localScale = currentLocalScale;
                }
            }
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
            if (m_ActionButton != null)
            {
                BoxCollider btnCol = m_ActionButton.GetComponent<BoxCollider>();
                if (btnCol == null)
                {
                    btnCol = m_ActionButton.gameObject.AddComponent<BoxCollider>();
                }
                if (m_AutoAdjustColliders)
                {
                    RectTransform rectTransform = m_ActionButton.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        btnCol.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 10f);
                        btnCol.center = Vector3.zero;
                    }
                    else
                    {
                        btnCol.size = new Vector3(260f, 52f, 10f);
                        btnCol.center = Vector3.zero;
                    }
                }

                m_ActionSimpleInteractable = m_ActionButton.GetComponent<XRSimpleInteractable>();
                if (m_ActionSimpleInteractable == null)
                {
                    m_ActionSimpleInteractable = m_ActionButton.gameObject.AddComponent<XRSimpleInteractable>();
                }

                m_ActionSimpleInteractable.selectEntered.RemoveAllListeners();
                m_ActionSimpleInteractable.selectEntered.AddListener(_ => OnActionButtonClicked());
                m_ActionSimpleInteractable.activated.RemoveAllListeners();
                m_ActionSimpleInteractable.activated.AddListener(_ => OnActionButtonClicked());
            }

            if (m_BackToMenuButton != null)
            {
                BoxCollider btnCol = m_BackToMenuButton.GetComponent<BoxCollider>();
                if (btnCol == null)
                {
                    btnCol = m_BackToMenuButton.gameObject.AddComponent<BoxCollider>();
                }
                if (m_AutoAdjustColliders)
                {
                    RectTransform rectTransform = m_BackToMenuButton.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        btnCol.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 10f);
                        btnCol.center = Vector3.zero;
                    }
                    else
                    {
                        btnCol.size = new Vector3(210f, 52f, 10f);
                        btnCol.center = Vector3.zero;
                    }
                }

                m_MenuSimpleInteractable = m_BackToMenuButton.GetComponent<XRSimpleInteractable>();
                if (m_MenuSimpleInteractable == null)
                {
                    m_MenuSimpleInteractable = m_BackToMenuButton.gameObject.AddComponent<XRSimpleInteractable>();
                }

                m_MenuSimpleInteractable.selectEntered.RemoveAllListeners();
                m_MenuSimpleInteractable.selectEntered.AddListener(_ => OnBackToMenuButtonClicked());
                m_MenuSimpleInteractable.activated.RemoveAllListeners();
                m_MenuSimpleInteractable.activated.AddListener(_ => OnBackToMenuButtonClicked());
            }

            // BoxCollider on panel root so mouse/XR raycasts hit panel bounds
            GameObject panelRoot = m_IntroPanel != null ? m_IntroPanel : gameObject;
            BoxCollider panelCol = panelRoot.GetComponent<BoxCollider>();
            if (panelCol == null)
            {
                panelCol = panelRoot.AddComponent<BoxCollider>();
            }
            if (m_AutoAdjustColliders)
            {
                RectTransform panelRt = panelRoot.GetComponent<RectTransform>();
                if (panelRt != null)
                {
                    panelCol.size = new Vector3(panelRt.rect.width, panelRt.rect.height, 5f);
                }
                else
                {
                    panelCol.size = new Vector3(1000f, 640f, 5f);
                }
            }
        }

        private void OnEnable()
        {
            if (m_ActionButton != null)
            {
                m_ActionButton.onClick.RemoveListener(OnActionButtonClicked);
                m_ActionButton.onClick.AddListener(OnActionButtonClicked);
            }

            if (m_BackToMenuButton != null)
            {
                m_BackToMenuButton.onClick.RemoveListener(OnBackToMenuButtonClicked);
                m_BackToMenuButton.onClick.AddListener(OnBackToMenuButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (m_TypewriterCoroutine != null)
            {
                StopCoroutine(m_TypewriterCoroutine);
                m_TypewriterCoroutine = null;
                m_IsTyping = false;
            }

            if (m_ActionButton != null)
            {
                m_ActionButton.onClick.RemoveListener(OnActionButtonClicked);
            }

            if (m_BackToMenuButton != null)
            {
                m_BackToMenuButton.onClick.RemoveListener(OnBackToMenuButtonClicked);
            }
        }

        private void Start()
        {
            EnsureAttachedToMainCamera();

            if (m_ActionButton != null)
            {
                m_ActionButton.onClick.RemoveListener(OnActionButtonClicked);
                m_ActionButton.onClick.AddListener(OnActionButtonClicked);
            }

            if (m_BackToMenuButton != null)
            {
                m_BackToMenuButton.onClick.RemoveListener(OnBackToMenuButtonClicked);
                m_BackToMenuButton.onClick.AddListener(OnBackToMenuButtonClicked);
            }

            // Ensure intro panel is active and visible at start
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

            // Ensure interactive gameplay objects remain hidden until START
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

            // Ensure Telegram popup remains inactive until START
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

            if (m_Canvas != null && m_Canvas.renderMode == RenderMode.WorldSpace && m_Canvas.worldCamera == null)
            {
                m_Canvas.worldCamera = Camera.main;
            }

            UpdateDialogueDisplay();
            OptimizeRaycastTargets();
        }

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

            if (m_ActionButton != null && m_ActionButton.targetGraphic != null)
            {
                m_ActionButton.targetGraphic.raycastTarget = true;
            }

            if (m_BackToMenuButton != null && m_BackToMenuButton.targetGraphic != null)
            {
                m_BackToMenuButton.targetGraphic.raycastTarget = true;
            }
        }

        public void UpdateDialogueDisplay()
        {
            if (m_DialogueLines == null || m_DialogueLines.Length == 0) return;

            m_CurrentStepIndex = Mathf.Clamp(m_CurrentStepIndex, 0, m_DialogueLines.Length - 1);

            if (m_TitleText != null)
            {
                m_TitleText.text = m_BadgeTitle;
            }

            if (m_StepCounterText != null)
            {
                m_StepCounterText.text = $"[{m_CurrentStepIndex + 1}/{m_DialogueLines.Length}]";
            }

            string currentLine = m_DialogueLines[m_CurrentStepIndex];
            if (m_StoryText != null)
            {
                if (!Application.isPlaying || !m_EnableTypewriterEffect)
                {
                    m_StoryText.text = currentLine;
                    m_StoryText.maxVisibleCharacters = 99999;
                    m_IsTyping = false;
                }
                else
                {
                    StartTypingStoryText(currentLine);
                }
            }

            bool isLastStep = (m_CurrentStepIndex == m_DialogueLines.Length - 1);

            if (m_ActionButtonText != null)
            {
                m_ActionButtonText.text = isLastStep ? "<b>START ></b>" : "<b>NEXT ></b>";
            }

            if (m_ActionButtonImage != null)
            {
                m_ActionButtonImage.color = isLastStep ? m_StartButtonColor : m_NextButtonColor;
            }

            if (m_ActionButton != null)
            {
                ColorBlock cb = m_ActionButton.colors;
                Color baseColor = isLastStep ? m_StartButtonColor : m_NextButtonColor;
                cb.normalColor = baseColor;
                cb.highlightedColor = baseColor * 1.15f;
                cb.pressedColor = baseColor * 0.85f;
                cb.selectedColor = baseColor;
                m_ActionButton.colors = cb;
            }
        }

        public void StartTypingStoryText(string targetText)
        {
            if (m_StoryText == null) return;

            if (m_TypewriterCoroutine != null)
            {
                StopCoroutine(m_TypewriterCoroutine);
                m_TypewriterCoroutine = null;
            }

            if (gameObject.activeInHierarchy)
            {
                m_TypewriterCoroutine = StartCoroutine(TypewriterRoutine(targetText));
            }
            else
            {
                m_StoryText.text = targetText;
                m_StoryText.maxVisibleCharacters = 99999;
                m_IsTyping = false;
            }
        }

        private IEnumerator TypewriterRoutine(string targetText)
        {
            m_IsTyping = true;
            m_StoryText.text = targetText;
            m_StoryText.maxVisibleCharacters = 0;
            m_StoryText.ForceMeshUpdate();

            int totalCharacters = m_StoryText.textInfo.characterCount;
            float charDelay = Mathf.Max(0.005f, m_TypingSpeed);

            for (int i = 1; i <= totalCharacters; i++)
            {
                m_StoryText.maxVisibleCharacters = i;

                if (m_TypingSFX != null && m_AudioSource != null && (i % 2 == 0))
                {
                    m_AudioSource.PlayOneShot(m_TypingSFX, 0.35f);
                }

                char c = (i - 1 < targetText.Length) ? targetText[i - 1] : ' ';
                if (c == '.' || c == '!' || c == '?')
                {
                    yield return new WaitForSeconds(m_PunctuationPause);
                }
                else if (c == ',' || c == ';')
                {
                    yield return new WaitForSeconds(m_PunctuationPause * 0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(charDelay);
                }
            }

            m_StoryText.maxVisibleCharacters = 99999;
            m_IsTyping = false;
            m_TypewriterCoroutine = null;
        }

        public void CompleteCurrentTypingImmediately()
        {
            if (m_TypewriterCoroutine != null)
            {
                StopCoroutine(m_TypewriterCoroutine);
                m_TypewriterCoroutine = null;
            }

            if (m_StoryText != null)
            {
                m_StoryText.maxVisibleCharacters = 99999;
            }

            m_IsTyping = false;
        }

        public void OnActionButtonClicked()
        {
            if (m_IsScenarioStarted) return;

            if (m_IsTyping)
            {
                CompleteCurrentTypingImmediately();
                return;
            }

            if (m_CurrentStepIndex < m_DialogueLines.Length - 1)
            {
                m_CurrentStepIndex++;
                PlaySFX(m_NextStepSFX);
                UpdateDialogueDisplay();
                OnDialogueStepChanged?.Invoke(m_CurrentStepIndex);
                Debug.Log($"[Scenario2IntroController] Advanced to step {m_CurrentStepIndex + 1}/{m_DialogueLines.Length}");
            }
            else
            {
                PlaySFX(m_StartSFX);
                OnStartButtonClicked();
            }
        }

        public void OnStartButtonClicked()
        {
            if (m_IsScenarioStarted) return;
            CompleteCurrentTypingImmediately();
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
            var phoneController = UnityEngine.Object.FindFirstObjectByType<SmartphoneController>();
            if (phoneController != null)
            {
                phoneController.ResetMessageReadState();
            }

            if (m_TelegramMessagePopup == null)
            {
                FindTelegramMessagePopup();
            }

            if (m_TelegramMessagePopup != null)
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

            Debug.Log("[Scenario 2] START clicked! Intro dialogue fading out. Gameplay starting.");

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

        public void FindTelegramMessagePopup()
        {
            if (m_TelegramMessagePopup != null) return;

            // 1. Look under InteractiveObjects root if assigned
            if (m_InteractiveObjects != null)
            {
                Transform popupTrans = m_InteractiveObjects.transform.Find("Smartphone/TelegramMessagePopup");
                if (popupTrans != null)
                {
                    m_TelegramMessagePopup = popupTrans.gameObject;
                    return;
                }
            }

            // 2. Look under Smartphone GameObject in scene
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

            // 3. Look under InteractiveObjects root by name
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

            // 4. Fallback: Search loaded scene transforms (including inactive GameObjects)
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
            CompleteCurrentTypingImmediately();
            if (m_IntroPanel != null)
            {
                m_IntroPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void OnBackToMenuButtonClicked()
        {
            Debug.Log($"[Scenario 2] Returning to Main Menu: '{m_MainMenuSceneName}'");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadMainMenu();
            }
            else if (Application.CanStreamedLevelBeLoaded(m_MainMenuSceneName))
            {
                SceneManager.LoadScene(m_MainMenuSceneName);
            }
            else if (SceneManager.sceneCountInBuildSettings > 0)
            {
                SceneManager.LoadScene(0);
            }
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(clip);
            }
        }

        private void OnValidate()
        {
            // Intentionally left empty so manual text and layout adjustments made in Unity Inspector/Scene view are not overwritten.
        }

        [ContextMenu("Apply Dialogue Lines To TextMeshPro")]
        public void SyncDialogueLinesToTextMeshPro()
        {
            UpdateDialogueDisplay();
        }

        [ContextMenu("Extract Current TextMeshPro To Dialogue Line")]
        public void SyncTextMeshProToDialogueLine()
        {
            if (m_StoryText != null && m_DialogueLines != null && m_DialogueLines.Length > m_CurrentStepIndex)
            {
                m_DialogueLines[m_CurrentStepIndex] = m_StoryText.text;
            }
            if (m_TitleText != null)
            {
                m_BadgeTitle = m_TitleText.text;
            }
        }
    }
}
