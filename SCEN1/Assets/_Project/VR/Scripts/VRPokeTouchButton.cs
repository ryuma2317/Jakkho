using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CADT.VR.Cybersecurity
{
    /// <summary>
    /// Lead VR Interaction: Enables realistic 3D physical finger poke and touch interactions
    /// on holographic buttons and physical workstation switches in VR.
    /// Provides spring resistance, depth depression, audio clicks, and haptic feedback.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class VRPokeTouchButton : MonoBehaviour
    {
        [Header("--- Push Movement Configuration ---")]
        [Tooltip("Local direction the button moves when pressed (usually local Z or -Z)")]
        [SerializeField] private Vector3 pushAxis = new Vector3(0f, 0f, 1f);

        [Tooltip("Maximum depth travel in meters (e.g., 0.015 = 15mm)")]
        [SerializeField] private float maxPushDistance = 0.018f;

        [Tooltip("Normalized threshold (0.0 to 1.0) of max travel required to trigger a click")]
        [Range(0.2f, 0.95f)]
        [SerializeField] private float clickThreshold = 0.70f;

        [Tooltip("Speed at which the button springs back to initial position")]
        [SerializeField] private float returnSpringSpeed = 16f;

        [Header("--- Visual & Material Response ---")]
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Color normalColor = new Color(0.0f, 0.94f, 1.0f, 0.8f);
        [SerializeField] private Color hoverColor = new Color(0.1f, 1.0f, 0.8f, 1.0f);
        [SerializeField] private Color pressedColor = new Color(0.06f, 0.72f, 0.51f, 1.0f);

        [Header("--- Audio & Haptics ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pressSound;
        [SerializeField] private AudioClip releaseSound;

        [Header("--- Events ---")]
        public UnityEvent onTouchEnter;
        public UnityEvent onTouchExit;
        public UnityEvent onButtonPressed;
        public UnityEvent onButtonReleased;

        private Vector3 initialLocalPos;
        private Transform pokingFingerTransform;
        private bool isPressed = false;
        private bool isHovered = false;
        private float currentCompression = 0f;

        private void Awake()
        {
            initialLocalPos = transform.localPosition;
            Collider col = GetComponent<Collider>();
            col.isTrigger = true; // Ensure trigger for smooth finger entry without physics jitter

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (buttonRenderer == null)
            {
                buttonRenderer = GetComponent<Renderer>();
            }
        }

        private void Update()
        {
            if (pokingFingerTransform != null)
            {
                // Calculate distance pushed by finger relative to button face
                Vector3 toFinger = transform.parent != null 
                    ? transform.parent.InverseTransformPoint(pokingFingerTransform.position) - initialLocalPos
                    : pokingFingerTransform.position - initialLocalPos;

                float projectedDistance = Vector3.Dot(toFinger, pushAxis.normalized);
                currentCompression = Mathf.Clamp01(projectedDistance / maxPushDistance);

                // Set pushed position
                transform.localPosition = initialLocalPos + (pushAxis.normalized * (currentCompression * maxPushDistance));

                // Check click trigger threshold
                if (currentCompression >= clickThreshold && !isPressed)
                {
                    TriggerButtonPress();
                }
                else if (currentCompression < (clickThreshold * 0.5f) && isPressed)
                {
                    TriggerButtonRelease();
                }
            }
            else
            {
                // Spring back smoothly
                transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPos, Time.deltaTime * returnSpringSpeed);
                currentCompression = 0f;

                if (isPressed)
                {
                    TriggerButtonRelease();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Detect VR Hand finger tip (e.g. tag "Hand", "Finger", or XR Controller)
            if (other.CompareTag("Player") || other.name.ToLower().Contains("finger") || 
                other.name.ToLower().Contains("hand") || other.name.ToLower().Contains("poke"))
            {
                pokingFingerTransform = other.transform;
                isHovered = true;
                onTouchEnter?.Invoke();
                SetRendererColor(hoverColor);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform == pokingFingerTransform)
            {
                pokingFingerTransform = null;
                isHovered = false;
                onTouchExit?.Invoke();
                SetRendererColor(normalColor);
            }
        }

        private void TriggerButtonPress()
        {
            isPressed = true;
            SetRendererColor(pressedColor);

            if (pressSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(pressSound);
            }

            onButtonPressed?.Invoke();
            Debug.Log($"[VRPokeTouchButton] '{gameObject.name}' physically pressed by VR hand!");
        }

        private void TriggerButtonRelease()
        {
            isPressed = false;
            SetRendererColor(isHovered ? hoverColor : normalColor);

            if (releaseSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(releaseSound);
            }

            onButtonReleased?.Invoke();
        }

        private void SetRendererColor(Color color)
        {
            if (buttonRenderer != null && buttonRenderer.material != null)
            {
                buttonRenderer.material.color = color;
            }
        }
    }
}
