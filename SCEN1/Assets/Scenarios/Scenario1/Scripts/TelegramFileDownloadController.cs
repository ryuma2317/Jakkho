using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using CADT.VR.Cybersecurity;

namespace CADT.VR.UI
{
    /// <summary>
    /// Manages the Telegram .rar file download simulation.
    /// Handles the 10-second spinning download interaction, audio feedback,
    /// waiting for user click on download completion, and subsequent transition
    /// to the Command Prompt security breach screen.
    /// </summary>
    [DisallowMultipleComponent]
    public class TelegramFileDownloadController : MonoBehaviour
    {
        [Header("--- Download Configuration ---")]
        [Tooltip("Download duration in seconds (10 seconds requested)")]
        [SerializeField] private float downloadDuration = 10.0f;

        [Tooltip("Spin speed in degrees per second")]
        [SerializeField] private float spinSpeed = 360.0f;

        [Header("--- UI References ---")]
        [Tooltip("Click target button over the .rar attachment card")]
        [SerializeField] private Button attachmentButton;

        [Tooltip("Spinner overlay GameObject holding the spinning Image")]
        [SerializeField] private GameObject spinnerOverlay;

        [Tooltip("Transform of the spinning icon to rotate")]
        [SerializeField] private RectTransform spinnerTransform;

        [Tooltip("Icon displayed when download is completed")]
        [SerializeField] private GameObject completedIconOverlay;

        [Tooltip("Optional Text showing simulated file download progress")]
        [SerializeField] private TextMeshProUGUI progressText;

        [Tooltip("File Properties modal window (optional secondary inspection)")]
        [SerializeField] private GameObject filePropertiesModal;

        [Header("--- Command Prompt Breach Screen ---")]
        [Tooltip("Command prompt security breach screen GameObject to activate after user clicks downloaded file")]
        [SerializeField] private GameObject commandPromptScreen;

        [Tooltip("Simulator script that handles the 20s batch command loop")]
        [SerializeField] private CommandPromptBreachSimulator commandPromptSimulator;

        [Tooltip("Telegram window display root to hide when command prompt breaches the screen")]
        [SerializeField] private GameObject telegramWindowDisplay;

        [Header("--- Audio Feedback ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip startDownloadSFX;
        [SerializeField] private AudioClip downloadCompleteSFX;

        [Header("--- Events ---")]
        public UnityEvent onDownloadStarted;
        public UnityEvent<float> onDownloadProgress; // 0.0 to 1.0
        public UnityEvent onDownloadCompleted;
        public UnityEvent onBreachTriggered;

        private bool isDownloading = false;
        private bool isDownloaded = false;
        private Coroutine downloadCoroutine;

        public bool IsDownloading => isDownloading;
        public bool IsDownloaded => isDownloaded;

        public GameObject SpinnerOverlay
        {
            get => spinnerOverlay;
            set => spinnerOverlay = value;
        }

        public RectTransform SpinnerTransform
        {
            get => spinnerTransform;
            set => spinnerTransform = value;
        }

        public GameObject CompletedIconOverlay
        {
            get => completedIconOverlay;
            set => completedIconOverlay = value;
        }

        public GameObject FilePropertiesModal
        {
            get => filePropertiesModal;
            set => filePropertiesModal = value;
        }

        public GameObject CommandPromptScreen
        {
            get => commandPromptScreen;
            set => commandPromptScreen = value;
        }

        public CommandPromptBreachSimulator CommandPromptSimulator
        {
            get => commandPromptSimulator;
            set => commandPromptSimulator = value;
        }

        public GameObject TelegramWindowDisplay
        {
            get => telegramWindowDisplay;
            set => telegramWindowDisplay = value;
        }

        public TextMeshProUGUI ProgressText
        {
            get => progressText;
            set => progressText = value;
        }

        private void Awake()
        {
            if (attachmentButton == null)
            {
                attachmentButton = GetComponent<Button>();
            }

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

            if (spinnerTransform == null && spinnerOverlay != null)
            {
                spinnerTransform = spinnerOverlay.GetComponent<RectTransform>();
            }

            if (telegramWindowDisplay == null && transform.parent != null)
            {
                telegramWindowDisplay = transform.parent.gameObject;
            }

            HookButton();
        }

        private void Start()
        {
            ResetDownloadState();
        }

        public void HookButton()
        {
            if (attachmentButton != null)
            {
                attachmentButton.onClick.RemoveListener(HandleAttachmentClicked);
                attachmentButton.onClick.AddListener(HandleAttachmentClicked);
            }
        }

        public void ResetDownloadState()
        {
            if (downloadCoroutine != null)
            {
                StopCoroutine(downloadCoroutine);
                downloadCoroutine = null;
            }

            isDownloading = false;
            isDownloaded = false;

            if (spinnerOverlay != null)
            {
                spinnerOverlay.SetActive(false);
                if (spinnerTransform != null) spinnerTransform.localRotation = Quaternion.identity;
            }

            if (completedIconOverlay != null)
            {
                completedIconOverlay.SetActive(false);
            }

            if (progressText != null)
            {
                progressText.text = "324 KB";
                if (progressText.transform.parent != null && progressText.transform.parent.name == "Progress_Text_Overlay")
                {
                    progressText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (commandPromptScreen != null)
            {
                commandPromptScreen.SetActive(false);
            }

            if (telegramWindowDisplay != null)
            {
                telegramWindowDisplay.SetActive(true);
            }
        }

        public void HandleAttachmentClicked()
        {
            if (isDownloaded)
            {
                TriggerBreachSequence();
                return;
            }

            if (isDownloading) return;

            StartDownload();
        }

        [ContextMenu("Trigger Breach Sequence")]
        public void TriggerBreachSequence()
        {
            Debug.Log("<color=#EF4444><b>[CADT VR]</b> Player clicked completed file! Switching to Command Prompt security breach screen...</color>");

            // Close file properties modal if open
            if (filePropertiesModal != null)
            {
                filePropertiesModal.SetActive(false);
            }

            // Hide telegram window
            if (telegramWindowDisplay != null)
            {
                telegramWindowDisplay.SetActive(false);
            }

            // Show command prompt screen
            if (commandPromptScreen != null)
            {
                commandPromptScreen.SetActive(true);
            }

            // Start 20-second command loop
            if (commandPromptSimulator != null)
            {
                commandPromptSimulator.StartBreachSequence(0.02f);
            }

            onBreachTriggered?.Invoke();
        }

        [ContextMenu("Start Download")]
        public void StartDownload()
        {
            if (isDownloading || isDownloaded) return;

            isDownloading = true;

            if (spinnerOverlay != null)
            {
                spinnerOverlay.SetActive(true);
            }

            if (completedIconOverlay != null)
            {
                completedIconOverlay.SetActive(false);
            }

            if (progressText != null)
            {
                if (progressText.transform.parent != null && progressText.transform.parent.name == "Progress_Text_Overlay")
                {
                    progressText.transform.parent.gameObject.SetActive(true);
                }
            }

            if (startDownloadSFX != null && audioSource != null)
            {
                audioSource.PlayOneShot(startDownloadSFX);
            }

            onDownloadStarted?.Invoke();
            downloadCoroutine = StartCoroutine(DownloadRoutine());
        }

        private IEnumerator DownloadRoutine()
        {
            float elapsed = 0f;
            int totalBytes = 324;

            while (elapsed < downloadDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / downloadDuration);

                // Spin the icon
                if (spinnerTransform != null)
                {
                    spinnerTransform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
                }

                // Update simulated download size
                if (progressText != null)
                {
                    int currentBytes = Mathf.RoundToInt(progress * totalBytes);
                    int percent = Mathf.RoundToInt(progress * 100f);
                    progressText.text = $"{currentBytes} KB / {totalBytes} KB ({percent}%)";
                }

                onDownloadProgress?.Invoke(progress);
                yield return null;
            }

            // Download Finished!
            isDownloading = false;
            isDownloaded = true;
            downloadCoroutine = null;

            if (spinnerOverlay != null)
            {
                spinnerOverlay.SetActive(false);
            }

            if (completedIconOverlay != null)
            {
                completedIconOverlay.SetActive(true);
            }

            // Hide progress text overlay to reveal native Telegram "324 KB" text
            if (progressText != null && progressText.transform.parent != null && progressText.transform.parent.name == "Progress_Text_Overlay")
            {
                progressText.transform.parent.gameObject.SetActive(false);
            }

            if (downloadCompleteSFX != null && audioSource != null)
            {
                audioSource.PlayOneShot(downloadCompleteSFX);
            }

            Debug.Log("<color=#10B981><b>[CADT VR]</b> Download completed (10 seconds elapsed). Download Complete button displayed. Waiting for player to click...</color>");
            onDownloadCompleted?.Invoke();
        }
    }
}