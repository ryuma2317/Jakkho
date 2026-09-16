using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Kannko.Scenario2
{
    /// <summary>
    /// Coordinates the Incident Debrief & Lessons Learned interactive panel for Scenario 2 (Evil Twin Wi-Fi).
    /// Triggered when the player identifies the rogue captive portal and exits the browser without submitting credentials.
    /// Provides 4 rich interactive lesson cards, XP scorecard, replay, and menu navigation.
    /// </summary>
    [DisallowMultipleComponent]
    public class Scenario2_LessonLearnedController : MonoBehaviour
    {
        [Header("--- Header & Status Elements ---")]
        [SerializeField] private TextMeshProUGUI headerTitleTMP;
        [SerializeField] private TextMeshProUGUI headerSubtitleTMP;
        [SerializeField] private TextMeshProUGUI statusBadgeTMP;
        [SerializeField] private TextMeshProUGUI xpScoreTMP;

        [Header("--- Lesson Card Containers ---")]
        [Tooltip("The 4 lesson cards to switch between")]
        [SerializeField] private GameObject[] lessonCards;
        [SerializeField] private Button[] tabPillButtons;

        [Header("--- Navigation Buttons ---")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button returnMenuButton;
        [SerializeField] private Button cardCompleteReturnButton;
        [SerializeField] private TextMeshProUGUI pageIndicatorTMP;

        [Header("--- 3D Pop Out Configuration ---")]
        [SerializeField] private float popDuration = 0.45f;
        [SerializeField] private bool animatePopOnEnable = true;
        [Tooltip("Whether to hide the panel in Start() during Play mode so it starts hidden until triggered")]
        [SerializeField] private bool hideOnStart = true;

        [Header("--- Audio Feedback ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip panelOpenedSFX;
        [SerializeField] private AudioClip buttonClickSFX;

        [Header("--- Events ---")]
        public UnityEvent onPanelShown;
        public UnityEvent<int> onLessonChanged;
        public UnityEvent onReplayTriggered;
        public UnityEvent onMenuTriggered;

        private int currentLessonIndex = 0;
        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale = new Vector3(0.00085f, 0.00085f, 0.00085f);
        private bool hasCachedBaseTransform = false;

        public int CurrentLessonIndex => currentLessonIndex;

        private void CacheBaseTransform()
        {
            if (!hasCachedBaseTransform)
            {
                baseLocalPosition = transform.localPosition;
                if (transform.localScale.x > 0.00001f && transform.localScale.x < 0.1f)
                {
                    baseLocalScale = transform.localScale;
                }
                hasCachedBaseTransform = true;
            }
        }

        private readonly List<XRRayInteractor> activeRayInteractors = new List<XRRayInteractor>();
        private float lastClickTime = -1f;
        private const float CLICK_DEBOUNCE_TIME = 0.25f;

        private void Awake()
        {
            CacheBaseTransform();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1.0f;
                    audioSource.minDistance = 0.5f;
                    audioSource.maxDistance = 10f;
                }
            }

            HookButtons();
            EnsureCollidersAndInteractables();
        }

        private void Start()
        {
            EnsureCollidersAndInteractables();
            ConfigureRayInteractors();

            if (hideOnStart && Application.isPlaying)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            CacheBaseTransform();
            EnsureCollidersAndInteractables();
            ShowLesson(0);

            if (audioSource != null && panelOpenedSFX != null && Application.isPlaying)
            {
                audioSource.PlayOneShot(panelOpenedSFX);
            }

            if (animatePopOnEnable && Application.isPlaying)
            {
                StopAllCoroutines();
                StartCoroutine(PopOutAnimationRoutine());
            }
            else
            {
                transform.localPosition = baseLocalPosition;
                transform.localScale = baseLocalScale;
            }

            onPanelShown?.Invoke();
        }

        private void Update()
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy) return;

            HandleDesktopMouseClicks();
            HandleVRControllerRayClicks();
            HandleKeyboardNavigation();
        }

        private IEnumerator PopOutAnimationRoutine()
        {
            CacheBaseTransform();

            Vector3 startScale = baseLocalScale * 0.2f;
            Vector3 targetScale = baseLocalScale;

            Vector3 startPos = baseLocalPosition + transform.forward * 0.12f;
            Vector3 targetPos = baseLocalPosition;

            transform.localPosition = startPos;
            transform.localScale = startScale;

            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);

                // Overshoot bounce curve for a clean physical pop-out
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float ease = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

                transform.localPosition = Vector3.LerpUnclamped(startPos, targetPos, ease);
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, ease);
                yield return null;
            }

            transform.localPosition = targetPos;
            transform.localScale = targetScale;
        }

        public void HookButtons()
        {
            if (prevButton != null)
            {
                prevButton.onClick.RemoveListener(PreviousLesson);
                prevButton.onClick.AddListener(PreviousLesson);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(NextLesson);
                nextButton.onClick.AddListener(NextLesson);
            }

            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(ReplayScenario);
                replayButton.onClick.AddListener(ReplayScenario);
            }

            if (returnMenuButton != null)
            {
                returnMenuButton.onClick.RemoveListener(ReturnToMenu);
                returnMenuButton.onClick.AddListener(ReturnToMenu);
            }

            if (cardCompleteReturnButton != null)
            {
                cardCompleteReturnButton.onClick.RemoveListener(ReturnToMenu);
                cardCompleteReturnButton.onClick.AddListener(ReturnToMenu);
            }

            if (tabPillButtons != null)
            {
                for (int i = 0; i < tabPillButtons.Length; i++)
                {
                    int index = i;
                    if (tabPillButtons[i] != null)
                    {
                        tabPillButtons[i].onClick.RemoveAllListeners();
                        tabPillButtons[i].onClick.AddListener(() =>
                        {
                            PlayClickSound();
                            ShowLesson(index);
                        });
                    }
                }
            }
        }

        public void ShowLesson(int index)
        {
            if (lessonCards == null || lessonCards.Length == 0) return;

            currentLessonIndex = Mathf.Clamp(index, 0, lessonCards.Length - 1);

            for (int i = 0; i < lessonCards.Length; i++)
            {
                if (lessonCards[i] != null)
                {
                    lessonCards[i].SetActive(i == currentLessonIndex);
                }
            }

            UpdateTabPills();
            UpdateNavigationState();

            onLessonChanged?.Invoke(currentLessonIndex);
        }

        public void NextLesson()
        {
            PlayClickSound();
            if (currentLessonIndex < lessonCards.Length - 1)
            {
                ShowLesson(currentLessonIndex + 1);
            }
        }

        public void PreviousLesson()
        {
            PlayClickSound();
            if (currentLessonIndex > 0)
            {
                ShowLesson(currentLessonIndex - 1);
            }
        }

        private static readonly Color[] TabActiveColors = new Color[]
        {
            new Color(0.12f, 0.75f, 0.45f, 0.95f), // 0: Emerald (Assets Protected)
            new Color(0.15f, 0.60f, 0.95f, 0.95f), // 1: Cyan (Attack Debrief)
            new Color(0.95f, 0.65f, 0.15f, 0.95f), // 2: Amber (Red Flags)
            new Color(0.12f, 0.85f, 0.50f, 0.95f)  // 3: Emerald (Victory 200 XP)
        };

        private static readonly Color TabInactiveColor = new Color(0.10f, 0.14f, 0.20f, 0.85f);

        private void UpdateTabPills()
        {
            if (tabPillButtons == null) return;

            for (int i = 0; i < tabPillButtons.Length; i++)
            {
                if (tabPillButtons[i] == null) continue;

                Image img = tabPillButtons[i].GetComponent<Image>();
                Outline outline = tabPillButtons[i].GetComponent<Outline>();
                bool isActive = (i == currentLessonIndex);

                if (img != null)
                {
                    img.color = isActive ? TabActiveColors[Mathf.Min(i, TabActiveColors.Length - 1)] : TabInactiveColor;
                }
                if (outline != null)
                {
                    outline.effectColor = isActive ? Color.white : new Color(0.25f, 0.32f, 0.42f, 0.6f);
                }
            }
        }

        private void UpdateNavigationState()
        {
            if (prevButton != null)
            {
                bool hasPrev = (currentLessonIndex > 0);
                prevButton.interactable = hasPrev;
                Image pImg = prevButton.GetComponent<Image>();
                if (pImg != null)
                {
                    pImg.color = hasPrev ? new Color(0.15f, 0.22f, 0.32f, 0.95f) : new Color(0.08f, 0.10f, 0.14f, 0.45f);
                }
            }

            if (nextButton != null)
            {
                bool hasNext = (currentLessonIndex < lessonCards.Length - 1);
                nextButton.interactable = hasNext;
                Image nImg = nextButton.GetComponent<Image>();
                if (nImg != null)
                {
                    nImg.color = hasNext ? new Color(0.12f, 0.45f, 0.85f, 0.95f) : new Color(0.08f, 0.10f, 0.14f, 0.45f);
                }
            }

            if (pageIndicatorTMP != null)
            {
                pageIndicatorTMP.text = $"Card {currentLessonIndex + 1} of {lessonCards.Length}";
            }
        }

        private void PlayClickSound()
        {
            if (audioSource != null && buttonClickSFX != null && Application.isPlaying)
            {
                audioSource.PlayOneShot(buttonClickSFX);
            }
        }

        public void ReplayScenario()
        {
            PlayClickSound();
            onReplayTriggered?.Invoke();
            Debug.Log("[Scenario2_LessonLearnedController] Replay Scenario triggered.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMenu()
        {
            PlayClickSound();
            onMenuTriggered?.Invoke();
            Debug.Log("[Scenario2_LessonLearnedController] Return to Menu triggered.");
            VRSetup.VRCardNavigationManager.navigateToScenarioSelectionDirectly = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScenarioSelection();
            }
            else if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
            else if (SceneManager.sceneCountInBuildSettings > 0)
            {
                SceneManager.LoadScene(0);
            }
        }

        public void EnsureCollidersAndInteractables()
        {
            if (prevButton != null) SetupElementColliderAndInteractable(prevButton.gameObject, PreviousLesson);
            if (nextButton != null) SetupElementColliderAndInteractable(nextButton.gameObject, NextLesson);
            if (replayButton != null) SetupElementColliderAndInteractable(replayButton.gameObject, ReplayScenario);
            if (returnMenuButton != null) SetupElementColliderAndInteractable(returnMenuButton.gameObject, ReturnToMenu);
            if (cardCompleteReturnButton != null) SetupElementColliderAndInteractable(cardCompleteReturnButton.gameObject, ReturnToMenu);

            if (tabPillButtons != null)
            {
                for (int i = 0; i < tabPillButtons.Length; i++)
                {
                    int index = i;
                    if (tabPillButtons[i] != null)
                    {
                        SetupElementColliderAndInteractable(tabPillButtons[i].gameObject, () =>
                        {
                            PlayClickSound();
                            ShowLesson(index);
                        });
                    }
                }
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
            activeRayInteractors.Clear();
            var rays = UnityEngine.Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < rays.Length; i++)
            {
                var ray = rays[i];
                if (ray != null)
                {
                    ray.allowHoveredActivate = true;
                    activeRayInteractors.Add(ray);
                }
            }
        }

        private void HandleVRControllerRayClicks()
        {
            if (activeRayInteractors.Count == 0) ConfigureRayInteractors();

            for (int i = 0; i < activeRayInteractors.Count; i++)
            {
                var ray = activeRayInteractors[i];
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

        private void HandleDesktopMouseClicks()
        {
            if (UnityEngine.InputSystem.Mouse.current == null ||
                !UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            Ray mouseRay = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, 10f))
            {
                if (DispatchHit(mouseHit.collider.gameObject)) return;
            }

            Ray centerRay = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            if (Physics.Raycast(centerRay, out RaycastHit centerHit, 10f))
            {
                if (DispatchHit(centerHit.collider.gameObject)) return;
            }
        }

        private void HandleKeyboardNavigation()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            if (kb.leftArrowKey.wasPressedThisFrame) PreviousLesson();
            else if (kb.rightArrowKey.wasPressedThisFrame) NextLesson();
            else if (kb.digit1Key.wasPressedThisFrame) ShowLesson(0);
            else if (kb.digit2Key.wasPressedThisFrame) ShowLesson(1);
            else if (kb.digit3Key.wasPressedThisFrame) ShowLesson(2);
            else if (kb.digit4Key.wasPressedThisFrame) ShowLesson(3);
        }

        public bool DispatchHit(GameObject hitObj)
        {
            if (hitObj == null) return false;

            if (Application.isPlaying && Time.unscaledTime - lastClickTime < CLICK_DEBOUNCE_TIME) return false;

            if (IsHitOnTarget(hitObj, prevButton?.gameObject))
            {
                lastClickTime = Time.unscaledTime;
                PreviousLesson();
                return true;
            }

            if (IsHitOnTarget(hitObj, nextButton?.gameObject))
            {
                lastClickTime = Time.unscaledTime;
                NextLesson();
                return true;
            }

            if (IsHitOnTarget(hitObj, replayButton?.gameObject))
            {
                lastClickTime = Time.unscaledTime;
                ReplayScenario();
                return true;
            }

            if (IsHitOnTarget(hitObj, returnMenuButton?.gameObject))
            {
                lastClickTime = Time.unscaledTime;
                ReturnToMenu();
                return true;
            }

            if (IsHitOnTarget(hitObj, cardCompleteReturnButton?.gameObject))
            {
                lastClickTime = Time.unscaledTime;
                ReturnToMenu();
                return true;
            }

            if (tabPillButtons != null)
            {
                for (int i = 0; i < tabPillButtons.Length; i++)
                {
                    if (tabPillButtons[i] != null && IsHitOnTarget(hitObj, tabPillButtons[i].gameObject))
                    {
                        lastClickTime = Time.unscaledTime;
                        PlayClickSound();
                        ShowLesson(i);
                        return true;
                    }
                }
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
