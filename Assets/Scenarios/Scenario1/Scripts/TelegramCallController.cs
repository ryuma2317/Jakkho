using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CADT.VR.UI
{
    /// <summary>
    /// Controls the Telegram Call Screen interface.
    /// Handles user clicking the call button in the chat header,
    /// transitioning to the call screen, showing "Waiting...",
    /// waiting 2 seconds then switching to "Ringing...",
    /// keeping "Ringing..." for 5 seconds (or until user clicks End Call),
    /// automatically ending the call after 5 seconds if not ended manually,
    /// playing audio feedback, and ending the call back to the main Telegram window.
    /// </summary>
    [DisallowMultipleComponent]
    public class TelegramCallController : MonoBehaviour
    {
        [Header("--- Screen References ---")]
        [Tooltip("Telegram main chat window display to hide during call")]
        [SerializeField] private GameObject telegramWindowDisplay;

        [Tooltip("Call screen root GameObject to display during active call")]
        [SerializeField] private GameObject telegramCallScreen;

        [Header("--- UI Elements ---")]
        [Tooltip("Clickable call button in Telegram chat header")]
        [SerializeField] private Button callButton;

        [Tooltip("Clickable End Call button on call screen")]
        [SerializeField] private Button endCallButton;

        [Tooltip("Status text displaying 'Waiting...' then 'Ringing...'")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("--- Timing Configuration ---")]
        [Tooltip("Duration to display 'Waiting...' before switching to 'Ringing...' (2 seconds requested)")]
        [SerializeField] private float waitingDuration = 2.0f;

        [Tooltip("Duration to keep 'Ringing...' before automatically ending the call if user does not click End Call (5 seconds requested)")]
        [SerializeField] private float ringingDuration = 5.0f;

        [Header("--- Audio Feedback ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickSFX;
        [SerializeField] private AudioClip ringoutSFX;
        [SerializeField] private AudioClip endCallSFX;

        [Header("--- Call History / Missed Call Bubbles ---")]
        [Tooltip("First canceled/missed call message bubble shown in chat after 1st call ends")]
        [SerializeField] private GameObject canceledCallBubble1;

        [Tooltip("Second canceled/missed call message bubble shown in chat after 2nd call ends")]
        [SerializeField] private GameObject canceledCallBubble2;

        [Header("--- Events ---")]
        public UnityEvent onCallStarted;
        public UnityEvent onStatusChangedToRinging;
        public UnityEvent onCallEnded;

        private Coroutine callCoroutine;
        private bool isCallActive = false;
        private int completedCallCount = 0;

        public bool IsCallActive => isCallActive;
        public int CompletedCallCount => completedCallCount;

        public float WaitingDuration
        {
            get => waitingDuration;
            set => waitingDuration = value;
        }

        public float RingingDuration
        {
            get => ringingDuration;
            set => ringingDuration = value;
        }

        public GameObject CanceledCallBubble1
        {
            get => canceledCallBubble1;
            set => canceledCallBubble1 = value;
        }

        public GameObject CanceledCallBubble2
        {
            get => canceledCallBubble2;
            set => canceledCallBubble2 = value;
        }

        public GameObject TelegramWindowDisplay
        {
            get => telegramWindowDisplay;
            set => telegramWindowDisplay = value;
        }

        public GameObject TelegramCallScreen
        {
            get => telegramCallScreen;
            set => telegramCallScreen = value;
        }

        public Button CallButton
        {
            get => callButton;
            set => callButton = value;
        }

        public Button EndCallButton
        {
            get => endCallButton;
            set => endCallButton = value;
        }

        public TextMeshProUGUI StatusText
        {
            get => statusText;
            set => statusText = value;
        }

        public AudioSource AudioSourceComp
        {
            get => audioSource;
            set => audioSource = value;
        }

        public AudioClip ButtonClickSFX
        {
            get => buttonClickSFX;
            set => buttonClickSFX = value;
        }

        public AudioClip RingoutSFX
        {
            get => ringoutSFX;
            set => ringoutSFX = value;
        }

        public AudioClip EndCallSFX
        {
            get => endCallSFX;
            set => endCallSFX = value;
        }

        private void Awake()
        {
            ResolveReferences();

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
            ResolveReferences();
            HookButtons();
            if (telegramCallScreen != null && !isCallActive)
            {
                telegramCallScreen.SetActive(false);
            }
            if (canceledCallBubble1 != null) canceledCallBubble1.SetActive(false);
            if (canceledCallBubble2 != null) canceledCallBubble2.SetActive(false);
        }

        private void OnEnable()
        {
            HookButtons();
        }

        public void ResolveReferences()
        {
            // Auto-resolve references from canvas or hierarchy if unassigned
            Transform searchRoot = transform;
            if (telegramWindowDisplay == null)
            {
                Transform disp = searchRoot.Find("Telegram_Client_Root/Telegram_Window_Display");
                if (disp == null && searchRoot.parent != null) disp = searchRoot.parent.Find("Telegram_Client_Root/Telegram_Window_Display");
                if (disp != null) telegramWindowDisplay = disp.gameObject;
            }

            if (telegramCallScreen == null)
            {
                Transform cs = searchRoot.Find("Telegram_Call_Screen");
                if (cs == null && searchRoot.parent != null) cs = searchRoot.parent.Find("Telegram_Call_Screen");
                if (cs != null) telegramCallScreen = cs.gameObject;
            }

            if (callButton == null && telegramWindowDisplay != null)
            {
                Transform cb = telegramWindowDisplay.transform.Find("Header_Call_Button");
                if (cb != null) callButton = cb.GetComponent<Button>();
            }

            if (telegramWindowDisplay != null)
            {
                if (canceledCallBubble1 == null)
                {
                    Transform b1 = telegramWindowDisplay.transform.Find("Canceled_Call_Bubble_1");
                    if (b1 != null) canceledCallBubble1 = b1.gameObject;
                }

                if (canceledCallBubble2 == null)
                {
                    Transform b2 = telegramWindowDisplay.transform.Find("Canceled_Call_Bubble_2");
                    if (b2 != null) canceledCallBubble2 = b2.gameObject;
                }
            }

            if (telegramCallScreen != null)
            {
                if (endCallButton == null)
                {
                    Transform eb = telegramCallScreen.transform.Find("End_Call_Button");
                    if (eb != null) endCallButton = eb.GetComponent<Button>();
                }

                if (statusText == null)
                {
                    Transform st = telegramCallScreen.transform.Find("Status_Text");
                    if (st != null) statusText = st.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        public void HookButtons()
        {
            if (callButton != null)
            {
                callButton.onClick.RemoveListener(StartCall);
                callButton.onClick.AddListener(StartCall);
            }

            if (endCallButton != null)
            {
                endCallButton.onClick.RemoveListener(EndCall);
                endCallButton.onClick.AddListener(EndCall);
            }
        }

        [ContextMenu("Start Call")]
        public void StartCall()
        {
            if (isCallActive) return;
            isCallActive = true;

            Debug.Log("<color=#229ED9><b>[Telegram Call]</b> Call initiated with Sovann (Coworker). Status: Waiting...</color>");

            if (buttonClickSFX != null && audioSource != null)
            {
                audioSource.PlayOneShot(buttonClickSFX);
            }

            if (telegramWindowDisplay != null)
            {
                telegramWindowDisplay.SetActive(false);
            }

            if (telegramCallScreen != null)
            {
                telegramCallScreen.SetActive(true);
            }

            if (statusText != null)
            {
                statusText.text = "Waiting...";
            }

            onCallStarted?.Invoke();

            if (callCoroutine != null) StopCoroutine(callCoroutine);
            callCoroutine = StartCoroutine(CallProgressRoutine());
        }

        private IEnumerator CallProgressRoutine()
        {
            yield return new WaitForSeconds(waitingDuration);

            if (statusText != null)
            {
                statusText.text = "Ringing...";
            }

            Debug.Log("<color=#10B981><b>[Telegram Call]</b> 2 seconds elapsed. Status changed to: Ringing...</color>");
            onStatusChangedToRinging?.Invoke();

            if (ringoutSFX != null && audioSource != null)
            {
                audioSource.clip = ringoutSFX;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Keep Ringing status for 5 seconds if user does not click End Call
            yield return new WaitForSeconds(ringingDuration);

            // Automatically end call after 5 seconds of ringing
            if (isCallActive)
            {
                Debug.Log("<color=#F59E0B><b>[Telegram Call]</b> 5 seconds of ringing elapsed without user response. Automatically ending call.</color>");
                EndCall();
            }
        }

        [ContextMenu("End Call")]
        public void EndCall()
        {
            if (!isCallActive && (telegramCallScreen == null || !telegramCallScreen.activeSelf)) return;

            Debug.Log("<color=#EF4444><b>[Telegram Call]</b> Call ended. Returning to Telegram_Window_Display.</color>");

            isCallActive = false;

            if (callCoroutine != null)
            {
                StopCoroutine(callCoroutine);
                callCoroutine = null;
            }

            if (audioSource != null)
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                audioSource.loop = false;

                if (endCallSFX != null)
                {
                    audioSource.PlayOneShot(endCallSFX);
                }
            }

            if (telegramCallScreen != null)
            {
                telegramCallScreen.SetActive(false);
            }

            if (telegramWindowDisplay != null)
            {
                telegramWindowDisplay.SetActive(true);
            }

            completedCallCount++;
            Debug.Log($"<color=#10B981><b>[Telegram Call]</b> Call ended. Total completed calls: {completedCallCount}</color>");

            if (completedCallCount >= 1 && canceledCallBubble1 != null)
            {
                canceledCallBubble1.SetActive(true);
            }

            if (completedCallCount >= 2 && canceledCallBubble2 != null)
            {
                canceledCallBubble2.SetActive(true);
            }

            onCallEnded?.Invoke();
        }

        [ContextMenu("Reset Call History")]
        public void ResetCallHistory()
        {
            completedCallCount = 0;
            if (canceledCallBubble1 != null) canceledCallBubble1.SetActive(false);
            if (canceledCallBubble2 != null) canceledCallBubble2.SetActive(false);
        }
    }
}
