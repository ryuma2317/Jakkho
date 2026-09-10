using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CADT.VR.UI
{
    /// <summary>
    /// Controls the Telegram incoming message popup dialog.
    /// Handles popup animations, audio chimes, OPEN action, dismiss interactions,
    /// and transition from the Windows Homescreen to the Monitor Screen Canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public class TelegramPopupController : MonoBehaviour
    {
        [Header("--- Interactive Buttons ---")]
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dismissButton;

        [Header("--- Screen Transition Controls ---")]
        [Tooltip("Root or panel displaying the Windows 10 Homescreen / Desktop view")]
        [SerializeField] private GameObject homescreenDisplay;

        [Tooltip("Root or panel displaying the Telegram Window / Monitor Screen Canvas view")]
        [SerializeField] private GameObject monitorScreenDisplay;

        [Tooltip("Duration of the smooth window reveal transition in seconds")]
        [Range(0.1f, 1.5f)]
#pragma warning disable CS0414
        [SerializeField] private float transitionDuration = 0.35f;
#pragma warning restore CS0414

        public float TransitionDuration
        {
            get => transitionDuration;
            set => transitionDuration = value;
        }

        [Tooltip("Transition to Monitor Screen Canvas when Open is clicked")]
        [SerializeField] private bool transitionOnOpen = true;

        [Tooltip("Transition to Monitor Screen Canvas when Close is clicked")]
        [SerializeField] private bool transitionOnClose = false;

        [Tooltip("If true, the monitor screen directly starts showing the Telegram Window interface")]
        [SerializeField] private bool startOnTelegramWindow = false;

        [Header("--- Audio Feedback ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip notificationChimeSFX;
        [SerializeField] private AudioClip buttonClickSFX;

        [Header("--- Events ---")]
        public UnityEvent onOpened;
        public UnityEvent onClosed;
        public UnityEvent onScreenTransitionCompleted;

        private Coroutine animCoroutine;
        private Vector3 targetScale = Vector3.one;
        private bool hasTransitioned = false;

        public GameObject HomescreenDisplay
        {
            get => homescreenDisplay;
            set => homescreenDisplay = value;
        }

        public GameObject MonitorScreenDisplay
        {
            get => monitorScreenDisplay;
            set => monitorScreenDisplay = value;
        }

        public bool TransitionOnClose
        {
            get => transitionOnClose;
            set => transitionOnClose = value;
        }

        public bool StartOnTelegramWindow
        {
            get => startOnTelegramWindow;
            set => startOnTelegramWindow = value;
        }

        private void Awake()
        {
            if (openButton == null)
            {
                Transform btnTrans = transform.Find("Open_Button");
                if (btnTrans != null) openButton = btnTrans.GetComponent<Button>();
            }

            if (closeButton == null)
            {
                Transform closeTrans = transform.Find("Header_Bar/Close_Button");
                if (closeTrans != null) closeButton = closeTrans.GetComponent<Button>();
            }

            if (dismissButton == null)
            {
                Transform disTrans = transform.Find("Dismiss_Button");
                if (disTrans != null) dismissButton = disTrans.GetComponent<Button>();
            }

            // Auto-resolve screen displays from parent if not explicitly assigned
            if (transform.parent != null)
            {
                if (homescreenDisplay == null)
                {
                    Transform hs = transform.parent.Find("Homescreen_Display");
                    if (hs != null) homescreenDisplay = hs.gameObject;
                }

                if (monitorScreenDisplay == null)
                {
                    Transform ms = transform.parent.Find("Telegram_Window_Display");
                    if (ms != null) monitorScreenDisplay = ms.gameObject;
                }
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1f; // 3D spatial audio
                    audioSource.minDistance = 0.5f;
                    audioSource.maxDistance = 10f;
                }
            }

            HookButtons();
        }

        private void Start()
        {
            HookButtons();
            InitializeScreenState();
        }

        private void OnEnable()
        {
            HookButtons();
            PlayPopInAnimation();
        }

        /// <summary>
        /// Resets the workstation monitor to show the Homescreen first with popup,
        /// and hides the full Telegram application window.
        /// </summary>
        public void InitializeScreenState()
        {
            if (startOnTelegramWindow)
            {
                hasTransitioned = true;
                if (homescreenDisplay != null)
                {
                    homescreenDisplay.SetActive(false);
                }
                if (monitorScreenDisplay != null)
                {
                    monitorScreenDisplay.SetActive(true);
                    CanvasGroup targetCg = monitorScreenDisplay.GetComponent<CanvasGroup>();
                    if (targetCg != null) targetCg.alpha = 1f;
                    monitorScreenDisplay.transform.localScale = Vector3.one;
                }
                gameObject.SetActive(false);
                return;
            }

            hasTransitioned = false;

            if (homescreenDisplay != null)
            {
                homescreenDisplay.SetActive(true);
                CanvasGroup cg = homescreenDisplay.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }

            if (monitorScreenDisplay != null)
            {
                monitorScreenDisplay.SetActive(false);
            }

            gameObject.SetActive(true);
            PlayPopInAnimation();
        }

        public void HookButtons()
        {
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(HandleOpenClicked);
                openButton.onClick.AddListener(HandleOpenClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (dismissButton != null)
            {
                dismissButton.onClick.RemoveListener(HandleCloseClicked);
                dismissButton.onClick.AddListener(HandleCloseClicked);
            }
        }

        [ContextMenu("Show Popup")]
        public void ShowPopup()
        {
            gameObject.SetActive(true);
            PlayPopInAnimation();
        }

        [ContextMenu("Hide Popup")]
        public void HidePopup()
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            if (gameObject.activeInHierarchy && Application.isPlaying)
            {
                animCoroutine = StartCoroutine(PopOutRoutine());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void HandleOpenClicked()
        {
            PlaySFX(buttonClickSFX);
            Debug.Log("<color=#229ED9><b>[Telegram]</b> User clicked OPEN on Sovann's message popup.</color>");
            onOpened?.Invoke();

            if (transitionOnOpen)
            {
                TransitionToMonitorScreen();
            }
            else
            {
                HidePopup();
            }
        }

        public void HandleCloseClicked()
        {
            PlaySFX(buttonClickSFX);
            Debug.Log("<color=#94A3B8><b>[Telegram]</b> User clicked CLOSE on incoming message popup.</color>");
            onClosed?.Invoke();

            if (transitionOnClose)
            {
                TransitionToMonitorScreen();
            }
            else
            {
                HidePopup();
            }
        }

        /// <summary>
        /// Transitions the monitor view from the Windows Homescreen to the Monitor Screen Canvas (Telegram Window).
        /// </summary>
        [ContextMenu("Transition to Monitor Screen")]
        public void TransitionToMonitorScreen()
        {
            if (hasTransitioned) return;
            hasTransitioned = true;

            // 1. Hide the notification popup
            HidePopup();

            // 2. Immediately clear and deactivate the Windows Homescreen
            if (homescreenDisplay != null)
            {
                homescreenDisplay.SetActive(false);
                CanvasGroup homeCg = homescreenDisplay.GetComponent<CanvasGroup>();
                if (homeCg != null) homeCg.alpha = 1f;
            }

            // 3. Immediately activate the Telegram Monitor Screen with 100% full opacity
            if (monitorScreenDisplay != null)
            {
                monitorScreenDisplay.SetActive(true);
                CanvasGroup targetCg = monitorScreenDisplay.GetComponent<CanvasGroup>();
                if (targetCg != null) targetCg.alpha = 1f;
                monitorScreenDisplay.transform.localScale = Vector3.one;
            }

            onScreenTransitionCompleted?.Invoke();
            Debug.Log("<color=#10B981><b>[CADT VR]</b> Homescreen cleared and Monitor_Screen_Canvas fully revealed (100% opaque).</color>");
        }

        private void PlayPopInAnimation()
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(PopInRoutine());

            PlaySFX(notificationChimeSFX);
        }

        private IEnumerator PopInRoutine()
        {
            transform.localScale = targetScale * 0.7f;
            float elapsed = 0f;
            float duration = 0.28f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Overshoot bounce curve
                float scaleT = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
                transform.localScale = targetScale * scaleT;
                yield return null;
            }

            transform.localScale = targetScale;
            animCoroutine = null;
        }

        private IEnumerator PopOutRoutine()
        {
            float elapsed = 0f;
            float duration = 0.16f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                yield return null;
            }

            gameObject.SetActive(false);
            transform.localScale = targetScale;
            animCoroutine = null;
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
