using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CADT.VR.Interaction
{
    /// <summary>
    /// Adds smooth spatial scale transitions, audio cues, and outline highlights
    /// when VR laser pointers or physical hand tracking fingers hover over UI elements.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRUIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("--- Hover Motion ---")]
        [SerializeField] private float hoverScaleMultiplier = 1.05f;
        [SerializeField] private float transitionSpeed = 12f;

        [Header("--- Audio ---")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioSource audioSource;

        private Vector3 originalScale;
        private Vector3 targetScale;
        private Coroutine scaleCoroutine;

        private void Awake()
        {
            originalScale = transform.localScale;
            targetScale = originalScale;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            transform.localScale = originalScale;
            targetScale = originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = originalScale * hoverScaleMultiplier;
            PlaySound(hoverSound);
            StartScaleTransition();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = originalScale;
            StartScaleTransition();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = originalScale * 0.96f;
            PlaySound(clickSound);
            StartScaleTransition();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = originalScale * hoverScaleMultiplier;
            StartScaleTransition();
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void StartScaleTransition()
        {
            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(ScaleRoutine());
        }

        private IEnumerator ScaleRoutine()
        {
            while (Vector3.Distance(transform.localScale, targetScale) > 0.001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * transitionSpeed);
                yield return null;
            }
            transform.localScale = targetScale;
            scaleCoroutine = null;
        }
    }
}
