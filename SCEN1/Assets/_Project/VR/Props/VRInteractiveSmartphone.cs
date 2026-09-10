using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CADT.VR.Props
{
    /// <summary>
    /// Interactive physical VR smartphone supporting incoming phone calls, ringtones,
    /// out-of-band coworker verification, and 2FA verification codes.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRInteractiveSmartphone : MonoBehaviour
    {
        [Header("--- Screen Elements ---")]
        [SerializeField] private GameObject screenDisplayCanvas;
        [SerializeField] private TextMeshProUGUI callerNameTMP;
        [SerializeField] private TextMeshProUGUI callerStatusTMP;
        [SerializeField] private Button answerButton;
        [SerializeField] private Button declineButton;

        [Header("--- Audio & Vibration ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip ringtoneSFX;
        [SerializeField] private AudioClip callConnectedSFX;
        [SerializeField] private AudioClip callEndSFX;

        [Header("--- Events ---")]
        public UnityEvent onCallReceived;
        public UnityEvent onCallAnswered;
        public UnityEvent onCallDeclined;

        private bool isRinging = false;
        private bool isInCall = false;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            if (answerButton != null) answerButton.onClick.AddListener(AnswerCall);
            if (declineButton != null) declineButton.onClick.AddListener(DeclineCall);
        }

        public void TriggerIncomingCall(string callerName, string statusMessage = "Incoming Call...")
        {
            isRinging = true;
            isInCall = false;

            if (callerNameTMP != null) callerNameTMP.text = callerName;
            if (callerStatusTMP != null) callerStatusTMP.text = statusMessage;
            if (screenDisplayCanvas != null) screenDisplayCanvas.SetActive(true);

            if (audioSource != null && ringtoneSFX != null)
            {
                audioSource.clip = ringtoneSFX;
                audioSource.loop = true;
                audioSource.Play();
            }

            onCallReceived?.Invoke();
        }

        public void AnswerCall()
        {
            if (!isRinging) return;
            isRinging = false;
            isInCall = true;

            if (audioSource != null)
            {
                audioSource.Stop();
                if (callConnectedSFX != null) audioSource.PlayOneShot(callConnectedSFX);
            }

            if (callerStatusTMP != null) callerStatusTMP.text = "Connected (00:01)";
            onCallAnswered?.Invoke();
        }

        public void DeclineCall()
        {
            isRinging = false;
            isInCall = false;

            if (audioSource != null)
            {
                audioSource.Stop();
                if (callEndSFX != null) audioSource.PlayOneShot(callEndSFX);
            }

            if (callerStatusTMP != null) callerStatusTMP.text = "Call Ended";
            onCallDeclined?.Invoke();
        }
    }
}
