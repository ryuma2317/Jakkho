using System;
using UnityEngine;
using UnityEngine.Events;

namespace CADT.VR.Props
{
    /// <summary>
    /// Interactive physical VR Hardware Security Key (e.g. YubiKey / FIDO2 Token).
    /// Detects insertion into workstation USB port colliders, tap authentication,
    /// and controls pulsing status LED.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class VRHardwareSecurityKey : MonoBehaviour
    {
        [Header("--- Status LED ---")]
        [SerializeField] private Renderer ledRenderer;
        [SerializeField] private Color standbyColor = new Color(0.1f, 0.6f, 1.0f);
        [SerializeField] private Color authenticatedColor = new Color(0.1f, 1.0f, 0.3f);

        [Header("--- Audio ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip insertSFX;
        [SerializeField] private AudioClip tapAuthenticateSFX;

        [Header("--- Events ---")]
        public UnityEvent onInserted;
        public UnityEvent onRemoved;
        public UnityEvent onAuthenticated;

        private bool isInserted = false;
        private bool isAuthenticated = false;

        public bool IsInserted => isInserted;
        public bool IsAuthenticated => isAuthenticated;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Respawn") || other.name.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                InsertKey();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.name.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RemoveKey();
            }
        }

        public void InsertKey()
        {
            if (isInserted) return;
            isInserted = true;

            if (audioSource != null && insertSFX != null) audioSource.PlayOneShot(insertSFX);
            if (ledRenderer != null) ledRenderer.material.color = standbyColor;

            onInserted?.Invoke();
        }

        public void RemoveKey()
        {
            if (!isInserted) return;
            isInserted = false;
            isAuthenticated = false;

            onRemoved?.Invoke();
        }

        public void TapToAuthenticate()
        {
            if (!isInserted || isAuthenticated) return;
            isAuthenticated = true;

            if (audioSource != null && tapAuthenticateSFX != null) audioSource.PlayOneShot(tapAuthenticateSFX);
            if (ledRenderer != null) ledRenderer.material.color = authenticatedColor;

            onAuthenticated?.Invoke();
        }
    }
}
