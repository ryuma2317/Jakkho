using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [System.Serializable]
    public class ChatMessage
    {
        public string senderName;
        public string messageText;
        public string timeString = "Just now";
        public bool isIncoming = true;
        public string embeddedUrl = "";
    }

    [Header("Chat UI Display")]
    [SerializeField] private GameObject chatBubblePrefab;
    [SerializeField] private Transform chatContentParent;
    [SerializeField] private ScrollRect chatScrollRect;

    [Header("Active Message Box (Standalone / Floating)")]
    [SerializeField] private GameObject dialogueContainer;
    [SerializeField] private TextMeshProUGUI senderNameText;
    [SerializeField] private TextMeshProUGUI messageBodyText;
    [SerializeField] private TextMeshProUGUI timeStampText;

    [Header("Audio Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip notificationSound;
    [SerializeField] private AudioClip typingSound;

    [Header("Typewriter Settings")]
    [SerializeField] private float typingSpeed = 0.03f;
    private Coroutine currentTypewriterRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowDialogue(string sender, string message, string timestamp = "Just now", Action onComplete = null)
    {
        if (dialogueContainer != null) dialogueContainer.SetActive(true);

        if (senderNameText != null) senderNameText.text = sender;
        if (timeStampText != null) timeStampText.text = timestamp;

        PlayNotificationSound();

        if (currentTypewriterRoutine != null)
        {
            StopCoroutine(currentTypewriterRoutine);
        }

        currentTypewriterRoutine = StartCoroutine(TypewriterRoutine(message, onComplete));
    }

    public void HideDialogue()
    {
        if (dialogueContainer != null) dialogueContainer.SetActive(false);
        if (currentTypewriterRoutine != null)
        {
            StopCoroutine(currentTypewriterRoutine);
            currentTypewriterRoutine = null;
        }
    }

    private IEnumerator TypewriterRoutine(string fullText, Action onComplete)
    {
        if (messageBodyText == null) yield break;

        messageBodyText.text = "";
        for (int i = 0; i < fullText.Length; i++)
        {
            messageBodyText.text += fullText[i];
            yield return new WaitForSeconds(typingSpeed);
        }

        currentTypewriterRoutine = null;
        onComplete?.Invoke();
    }

    public void PlayNotificationSound()
    {
        if (audioSource != null && notificationSound != null)
        {
            audioSource.PlayOneShot(notificationSound);
        }
    }
}
