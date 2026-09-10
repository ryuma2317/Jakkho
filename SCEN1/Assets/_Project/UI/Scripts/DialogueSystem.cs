using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CADT.VR.UI
{
    /// <summary>
    /// Typewriter dialogue system for scenario briefings, phone conversations,
    /// and character debrief evaluations.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueSystem : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private TextMeshProUGUI speakerNameTMP;
        [SerializeField] private TextMeshProUGUI dialogueTMP;
        [SerializeField] private Image avatarImage;
        [SerializeField] private GameObject dialoguePanel;

        [Header("--- Typewriter Settings ---")]
        [SerializeField] private float charactersPerSecond = 35f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip blipSFX;

        [Header("--- Events ---")]
        public UnityEvent onDialogueStarted;
        public UnityEvent onDialogueCompleted;

        private Coroutine typeRoutine;
        private bool isTyping = false;

        public bool IsTyping => isTyping;

        public void DisplayDialogue(string speaker, string fullText, Sprite avatar = null)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (speakerNameTMP != null) speakerNameTMP.text = speaker;
            if (avatarImage != null && avatar != null)
            {
                avatarImage.sprite = avatar;
                avatarImage.gameObject.SetActive(true);
            }

            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypewriterRoutine(fullText));
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            isTyping = true;
            onDialogueStarted?.Invoke();

            if (dialogueTMP != null) dialogueTMP.text = "";

            float delay = 1f / charactersPerSecond;
            for (int i = 0; i < text.Length; i++)
            {
                if (dialogueTMP != null) dialogueTMP.text += text[i];
                if (audioSource != null && blipSFX != null && i % 3 == 0)
                {
                    audioSource.PlayOneShot(blipSFX, 0.25f);
                }
                yield return new WaitForSeconds(delay);
            }

            isTyping = false;
            onDialogueCompleted?.Invoke();
        }

        public void CloseDialogue()
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            isTyping = false;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }
    }
}
