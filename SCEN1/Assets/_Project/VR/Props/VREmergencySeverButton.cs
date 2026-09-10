using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CADT.VR.Props
{
    /// <summary>
    /// Physical 3D Emergency Sever / Kill Switch button.
    /// Pushed down with VR hand tracking or controllers to instantly isolate
    /// workstation network cables or cut power during active security breaches.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class VREmergencySeverButton : MonoBehaviour
    {
        [Header("--- Push Movement ---")]
        [SerializeField] private Transform capTransform;
        [SerializeField] private float pushDepth = 0.025f;
        [SerializeField] private float springSpeed = 10f;

        [Header("--- Audio ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip alarmSlamSFX;

        [Header("--- Events ---")]
        public UnityEvent onSeverActivated;

        private Vector3 initialCapLocalPos;
        private bool isTriggered = false;

        private void Awake()
        {
            if (capTransform == null) capTransform = transform;
            initialCapLocalPos = capTransform.localPosition;
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered) return;
            if (other.name.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                other.name.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0 ||
                other.name.IndexOf("Poke", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                TriggerEmergencySever();
            }
        }

        [ContextMenu("Trigger Sever")]
        public void TriggerEmergencySever()
        {
            if (isTriggered) return;
            isTriggered = true;

            if (audioSource != null && alarmSlamSFX != null) audioSource.PlayOneShot(alarmSlamSFX);
            StartCoroutine(PushAnimationRoutine());

            onSeverActivated?.Invoke();
        }

        private IEnumerator PushAnimationRoutine()
        {
            Vector3 pushedPos = initialCapLocalPos - new Vector3(0f, pushDepth, 0f);
            capTransform.localPosition = pushedPos;

            yield return new WaitForSeconds(0.4f);

            while (Vector3.Distance(capTransform.localPosition, initialCapLocalPos) > 0.001f)
            {
                capTransform.localPosition = Vector3.Lerp(capTransform.localPosition, initialCapLocalPos, Time.deltaTime * springSpeed);
                yield return null;
            }
            capTransform.localPosition = initialCapLocalPos;
            isTriggered = false;
        }
    }
}
