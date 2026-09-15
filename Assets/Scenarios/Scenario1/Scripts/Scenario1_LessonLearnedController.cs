using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CADT.VR.Cybersecurity
{
    /// <summary>
    /// Coordinates the Incident Debrief & Lessons Learned interactive panel
    /// displayed on the workstation monitor after the 10-second terminal breach simulation.
    /// Provides interactive lesson cards, XP scorecard, replay, and menu navigation.
    /// </summary>
    [DisallowMultipleComponent]
    public class Scenario1_LessonLearnedController : MonoBehaviour
    {
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
        [Tooltip("Whether to hide the panel in Start() during Play mode so it starts hidden until breach completes, while remaining visible in Edit Mode")]
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
        }

        private void Start()
        {
            // In Play Mode, start hidden until CommandPromptBreachSimulator triggers it.
            // In Edit Mode, remains active and visible in the Scene View and Hierarchy.
            if (hideOnStart && Application.isPlaying)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            CacheBaseTransform();
            ConfigureOutcome(isVictoryMode);
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
            new Color(0.85f, 0.15f, 0.15f, 0.95f), // 0: Red
            new Color(0.95f, 0.35f, 0.15f, 0.95f), // 1: Orange/Amber
            new Color(0.15f, 0.60f, 0.95f, 0.95f), // 2: Cyan
            new Color(0.12f, 0.75f, 0.45f, 0.95f)  // 3: Emerald
        };

        private static readonly Color[] WinTabActiveColors = new Color[]
        {
            new Color(0.12f, 0.75f, 0.45f, 0.95f), // 0: Emerald (Assets Protected)
            new Color(0.15f, 0.60f, 0.95f, 0.95f), // 1: Cyan (Threat Contained)
            new Color(0.35f, 0.55f, 0.95f, 0.95f), // 2: Blue (Out-of-band)
            new Color(0.12f, 0.75f, 0.45f, 0.95f)  // 3: Emerald (Victory 100 XP)
        };

        private static readonly Color TabInactiveColor = new Color(0.10f, 0.14f, 0.20f, 0.85f);

        [SerializeField] private bool isVictoryMode = true;
        public bool IsVictoryMode => isVictoryMode;

        /// <summary>
        /// Configures the Lesson Learned Panel for either:
        /// - Victory (emergency shutdown clicked in time -> threat contained, assets protected, 100 XP)
        /// - Loss (10s breach ran out -> assets compromised, security alert, 0 XP)
        /// </summary>
        public void ConfigureOutcome(bool isVictory)
        {
            isVictoryMode = isVictory;

            // Root panel outline glow
            Outline rootOutline = GetComponent<Outline>();
            if (rootOutline != null)
            {
                rootOutline.effectColor = isVictory
                    ? new Color(0.12f, 0.85f, 0.50f, 0.95f)  // Emerald victory glow
                    : new Color(0.35f, 0.60f, 0.95f, 0.85f); // Cool cyber blue glow
            }

            // Update Tab Labels
            string[] winTabLabels = new string[]
            {
                "1. ASSETS PROTECTED",
                "2. THREAT CONTAINED",
                "3. OUT-OF-BAND",
                "4. VICTORY (100 XP)"
            };

            string[] lossTabLabels = new string[]
            {
                "1. YOUR LOSSES",
                "2. SECURITY ALERT",
                "3. OUT-OF-BAND",
                "4. SCENARIO COMPLETE"
            };

            string[] currentTabLabels = isVictory ? winTabLabels : lossTabLabels;
            if (tabPillButtons != null)
            {
                for (int i = 0; i < tabPillButtons.Length && i < currentTabLabels.Length; i++)
                {
                    if (tabPillButtons[i] != null)
                    {
                        var tmp = tabPillButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = currentTabLabels[i];
                    }
                }
            }

            // Update Card 0 (Losses vs Assets Protected)
            if (lessonCards != null && lessonCards.Length > 0 && lessonCards[0] != null)
            {
                var card0 = lessonCards[0];
                var headerTMP = card0.transform.Find("Card_Header")?.GetComponent<TextMeshProUGUI>();
                var bodyTMP = card0.transform.Find("Losses_Body")?.GetComponent<TextMeshProUGUI>();
                var cardImg = card0.GetComponent<Image>();
                var cardOutline = card0.GetComponent<Outline>();
                var dividerImg = card0.transform.Find("Divider")?.GetComponent<Image>();

                if (isVictory)
                {
                    if (headerTMP != null) headerTMP.text = "<color=#34D399><b>✓ ASSETS PROTECTED — THREAT CONTAINED:</b></color>";
                    if (bodyTMP != null)
                    {
                        bodyTMP.text =
                            "<line-height=145%>" +
                            "<color=#10B981><b>✓</b></color>  <b>Bank login credentials (ABA Bank)</b> — <color=#34D399><b>SECURED</b></color>\n" +
                            "<color=#10B981><b>✓</b></color>  <b>Work email password</b> — <color=#34D399><b>PROTECTED</b></color>\n" +
                            "<color=#10B981><b>✓</b></color>  <b>Telegram account session</b> — <color=#34D399><b>INTACT</b></color>\n" +
                            "<color=#10B981><b>✓</b></color>  <b>PayPal account access</b> — <color=#34D399><b>SAFEGUARDED</b></color>\n" +
                            "<color=#10B981><b>✓</b></color>  <b>Webcam privacy</b> — <color=#34D399><b>NO UNAUTHORIZED ACCESS</b></color>\n" +
                            "<color=#10B981><b>✓</b></color>  <b>847 personal contacts</b> — <color=#34D399><b>PRESERVED</b></color>\n" +
                            "<color=#10B981><b>✓</b></color>  <b>234 corporate contacts</b> — <color=#34D399><b>NOT EXPOSED</b></color>\n" +
                            "<color=#10B981><b>✓</b></color>  <b>Identity abuse</b> — <color=#34D399><b>BLOCKED IN TIME</b></color>\n" +
                            "<color=#10B981><b>✓</b></color>  <b>Financial theft</b> — <color=#34D399><b>PREVENTED</b></color>\n" +
                            "<color=#10B981><b>✓</b></color>  <b>Malware C2 exfiltration</b> — <color=#34D399><b>BEACON SEVERED</b></color>" +
                            "</line-height>";
                    }
                    if (cardImg != null) cardImg.color = new Color(0.02f, 0.08f, 0.05f, 0.98f);
                    if (cardOutline != null) cardOutline.effectColor = new Color(0.12f, 0.85f, 0.50f, 0.95f);
                    if (dividerImg != null) dividerImg.color = new Color(0.12f, 0.85f, 0.50f, 0.6f);
                }
                else
                {
                    if (headerTMP != null) headerTMP.text = "<color=#EF4444><b>YOUR LOSSES:</b></color>";
                    if (bodyTMP != null)
                    {
                        bodyTMP.text =
                            "<line-height=145%>" +
                            "<color=#EF4444><b>✗</b></color>  <b>Bank login credentials (ABA Bank)</b>\n" +
                            "<color=#EF4444><b>✗</b></color>  <b>Work email password</b>\n" +
                            "<color=#EF4444><b>✗</b></color>  <b>Telegram account session</b>\n" +
                            "<color=#EF4444><b>✗</b></color>  <b>PayPal account access</b>\n" +
                            "<color=#EF4444><b>✗</b></color>  <b>Webcam photo of you</b>\n" +
                            "<color=#EF4444><b>✗</b></color>  <b>847 personal contacts exposed</b>\n" +
                            "<color=#EF4444><b>✗</b></color>  <b>234 corporate contacts exposed</b>\n" +
                            "<color=#EF4444><b>✗</b></color>  <b>Your identity used to spam 10+ people</b>\n" +
                            "<color=#EF4444><b>✗</b></color>  <b>Potential financial theft</b>\n" +
                            "<color=#EF4444><b>✗</b></color>  <b>Potential identity theft</b>" +
                            "</line-height>";
                    }
                    if (cardImg != null) cardImg.color = new Color(0.08f, 0.03f, 0.04f, 0.98f);
                    if (cardOutline != null) cardOutline.effectColor = new Color(0.95f, 0.20f, 0.20f, 0.95f);
                    if (dividerImg != null) dividerImg.color = new Color(0.85f, 0.20f, 0.20f, 0.5f);
                }
            }

            // Update Card 1 (Security Alert vs Threat Contained)
            if (lessonCards != null && lessonCards.Length > 1 && lessonCards[1] != null)
            {
                var card1 = lessonCards[1];
                var headerTMP = card1.transform.Find("Card_Header")?.GetComponent<TextMeshProUGUI>();
                var bodyTMP = card1.transform.Find("Alert_Body")?.GetComponent<TextMeshProUGUI>();
                var cardImg = card1.GetComponent<Image>();
                var cardOutline = card1.GetComponent<Outline>();
                var dividerImg = card1.transform.Find("Divider")?.GetComponent<Image>();

                if (isVictory)
                {
                    if (headerTMP != null) headerTMP.text = "<color=#38BDF8><b>✓ EMERGENCY INCIDENT CONTAINED</b></color>";
                    if (bodyTMP != null)
                    {
                        bodyTMP.text =
                            "<line-height=170%><size=27>" +
                            "<color=#10B981>●</color>  <b>Emergency Shutdown Executed in Time!</b>\n" +
                            "<color=#94A3B8>You halted the workstation before the 10-second breach window completed.</color>\n\n" +
                            "<color=#38BDF8>●</color>  <b>Network Connection Cut Off:</b>\n" +
                            "<color=#94A3B8>The InfoStealer failed to transmit stolen credentials to the attacker.</color>\n\n" +
                            "<color=#10B981>●</color>  <b>Dropper Subprocesses Terminated:</b>\n" +
                            "<color=#94A3B8>Malicious batch payload killed in volatile memory before persistence.</color>\n\n" +
                            "<color=#FBBF24>●</color>  <b>IT Incident Ticket Generated:</b>\n" +
                            "<color=#94A3B8>Incident #INC-8924 logged for workstation forensic wipe.</color>" +
                            "</size></line-height>";
                    }
                    if (cardImg != null) cardImg.color = new Color(0.02f, 0.05f, 0.10f, 0.98f);
                    if (cardOutline != null) cardOutline.effectColor = new Color(0.20f, 0.70f, 0.98f, 0.95f);
                    if (dividerImg != null) dividerImg.color = new Color(0.20f, 0.70f, 0.98f, 0.6f);
                }
                else
                {
                    if (headerTMP != null) headerTMP.text = "<color=#F97316><b>[!] SECURITY ALERT</b></color>";
                    if (bodyTMP != null)
                    {
                        bodyTMP.text =
                            "<line-height=180%><size=30>" +
                            "<color=#EF4444>●</color>  <b>Workstation Network Isolated</b>\n\n" +
                            "<color=#F59E0B>●</color>  <b>Active InfoStealer Detected</b>\n\n" +
                            "<color=#EF4444>●</color>  <b>All files and credentials may be compromised</b>\n\n" +
                            "<color=#38BDF8>●</color>  <b>Contact IT Security immediately</b>" +
                            "</size></line-height>";
                    }
                    if (cardImg != null) cardImg.color = new Color(0.10f, 0.03f, 0.03f, 0.98f);
                    if (cardOutline != null) cardOutline.effectColor = new Color(0.95f, 0.35f, 0.15f, 0.95f);
                    if (dividerImg != null) dividerImg.color = new Color(0.95f, 0.35f, 0.15f, 0.5f);
                }
            }

            // Update Card 3 (Scenario Complete Scorecard)
            if (lessonCards != null && lessonCards.Length > 3 && lessonCards[3] != null)
            {
                var card3 = lessonCards[3];
                var headerTMP = card3.transform.Find("Card_Header")?.GetComponent<TextMeshProUGUI>();
                var bodyTMP = card3.transform.Find("Summary_Body")?.GetComponent<TextMeshProUGUI>();
                var cardImg = card3.GetComponent<Image>();
                var cardOutline = card3.GetComponent<Outline>();
                var dividerImg = card3.transform.Find("Divider")?.GetComponent<Image>();

                if (isVictory)
                {
                    if (headerTMP != null) headerTMP.text = "<color=#34D399><b>★ SCENARIO 1 COMPLETE — VICTORY! ★</b></color>";
                    if (bodyTMP != null)
                    {
                        bodyTMP.text =
                            "<line-height=150%><size=27>" +
                            "<b>Red Flags Addressed:</b>  <color=#34D399><b>3 / 3 (Full Awareness)</b></color>\n" +
                            "<b>Choice Made:</b>  <color=#34D399><b>Safe (Emergency Power Cutoff in Time)</b></color>\n" +
                            "<b>XP Earned:</b>  <color=#FBBF24><b>100 / 100 XP (PERFECT SCORE!)</b></color>\n\n" +
                            "<color=#38BDF8><b>SECURITY BADGE UNLOCKED:</b></color>  <color=#FBBF24><b>★ Rapid Incident Responder ★</b></color>\n\n" +
                            "<color=#FBBF24><b>KEY RULES TO REMEMBER:</b></color>\n" +
                            "<b>1. Documents are never executables</b>\n" +
                            "<b>2. Password-protected archives = suspicious</b>\n" +
                            "<b>3. Verify out-of-band through a friend</b>" +
                            "</size></line-height>";
                    }
                    if (cardImg != null) cardImg.color = new Color(0.02f, 0.08f, 0.05f, 0.98f);
                    if (cardOutline != null) cardOutline.effectColor = new Color(0.15f, 0.85f, 0.50f, 0.95f);
                    if (dividerImg != null) dividerImg.color = new Color(0.15f, 0.85f, 0.50f, 0.6f);
                }
                else
                {
                    if (headerTMP != null) headerTMP.text = "<color=#EF4444><b>SCENARIO 1 COMPLETE</b></color>";
                    if (bodyTMP != null)
                    {
                        bodyTMP.text =
                            "<line-height=150%><size=27>" +
                            "<b>Red Flags Detected:</b>  <color=#F87171><b>0 / 3</b></color>\n" +
                            "<b>Choice Made:</b>  <color=#EF4444><b>Unsafe (Executed Payload)</b></color>\n" +
                            "<b>XP Earned:</b>  <color=#F87171><b>0 / 100 XP</b></color>\n\n" +
                            "<color=#FBBF24><b>KEY RULES TO REMEMBER:</b></color>\n" +
                            "<b>1. Documents are never executables</b>\n" +
                            "<b>2. Password-protected archives = suspicious</b>\n" +
                            "<b>3. Verify out-of-band through a friend</b>" +
                            "</size></line-height>";
                    }
                    if (cardImg != null) cardImg.color = new Color(0.03f, 0.08f, 0.06f, 0.98f);
                    if (cardOutline != null) cardOutline.effectColor = new Color(0.15f, 0.85f, 0.50f, 0.95f);
                    if (dividerImg != null) dividerImg.color = new Color(0.15f, 0.85f, 0.50f, 0.5f);
                }
            }

            UpdateTabPills();
        }

        private void UpdateTabPills()
        {
            if (tabPillButtons == null) return;

            Color[] activePalette = isVictoryMode ? WinTabActiveColors : TabActiveColors;

            for (int i = 0; i < tabPillButtons.Length; i++)
            {
                if (tabPillButtons[i] == null) continue;

                Image img = tabPillButtons[i].GetComponent<Image>();
                Outline outline = tabPillButtons[i].GetComponent<Outline>();
                bool isActive = (i == currentLessonIndex);

                if (img != null)
                {
                    img.color = isActive ? activePalette[Mathf.Min(i, activePalette.Length - 1)] : TabInactiveColor;
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
                    nImg.color = hasNext ? new Color(0.12f, 0.35f, 0.60f, 0.95f) : new Color(0.08f, 0.10f, 0.14f, 0.45f);
                }
            }

            if (pageIndicatorTMP != null && lessonCards != null && lessonCards.Length > 0)
            {
                pageIndicatorTMP.text = $"Card {currentLessonIndex + 1} of {lessonCards.Length}";
            }
        }

        public void ReplayScenario()
        {
            PlayClickSound();
            Debug.Log("<color=#38BDF8><b>[CADT VR]</b> Replaying Scenario 1...</color>");
            onReplayTriggered?.Invoke();
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

        public void ReturnToMenu()
        {
            PlayClickSound();
            Debug.Log("<color=#38BDF8><b>[CADT VR]</b> Returning to Menu...</color>");
            onMenuTriggered?.Invoke();
            VRSetup.VRCardNavigationManager.navigateToScenarioSelectionDirectly = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScenarioSelection();
            }
            else
            {
                // Fallback to MainMenu if GameManager not initialized
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void PlayClickSound()
        {
            if (audioSource != null && buttonClickSFX != null)
            {
                audioSource.PlayOneShot(buttonClickSFX);
            }
        }
    }
}
