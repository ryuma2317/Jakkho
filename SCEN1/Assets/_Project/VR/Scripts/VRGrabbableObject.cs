using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CADT.VR.Cybersecurity
{
    /// <summary>
    /// Lead VR Interaction: Enables physical 3D objects (such as USB drives, security badges,
    /// or desk artifacts) to be grabbed, inspected, and thrown with realistic physics
    /// using VR Hands or XR Controllers.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class VRGrabbableObject : MonoBehaviour
    {
        [Header("--- Grab Behavior ---")]
        [Tooltip("Snap smoothly to hand grip position or keep relative offset")]
        [SerializeField] private bool snapToHandOrigin = false;

        [Tooltip("Throw velocity multiplier when released")]
        [SerializeField] private float throwForceMultiplier = 1.2f;

        [Header("--- Hover Outline & Material ---")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color hoverGlowColor = new Color(0.0f, 0.94f, 1.0f, 1.0f);

        [Header("--- Audio ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip grabSound;
        [SerializeField] private AudioClip dropSound;
        [SerializeField] private AudioClip impactSound;

        [Header("--- Events ---")]
        public UnityEvent onHoverEntered;
        public UnityEvent onHoverExited;
        public UnityEvent onGrabbed;
        public UnityEvent onReleased;

        private Rigidbody rb;
        private Transform currentHoldingHand;
        private bool isHeld = false;
        private Color originalColor;
        private List<Vector3> velocitySamples = new List<Vector3>();
        private const int MAX_SAMPLES = 8;

        public bool IsHeld => isHeld;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer != null && targetRenderer.material != null)
            {
                originalColor = targetRenderer.material.color;
            }
        }

        private void FixedUpdate()
        {
            if (isHeld && currentHoldingHand != null)
            {
                // Track hand velocity for realistic throwing
                velocitySamples.Add((currentHoldingHand.position - transform.position) / Time.fixedDeltaTime);
                if (velocitySamples.Count > MAX_SAMPLES)
                {
                    velocitySamples.RemoveAt(0);
                }

                if (snapToHandOrigin)
                {
                    rb.MovePosition(currentHoldingHand.position);
                    rb.MoveRotation(currentHoldingHand.rotation);
                }
            }
        }

        /// <summary>
        /// Called when VR Hand or Controller initiates grab (pinch or grip trigger).
        /// </summary>
        public void Grab(Transform handTransform)
        {
            if (isHeld) Release();

            currentHoldingHand = handTransform;
            isHeld = true;

            rb.isKinematic = true;
            rb.useGravity = false;

            transform.SetParent(handTransform, true);

            if (grabSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(grabSound);
            }

            onGrabbed?.Invoke();
            Debug.Log($"[VRGrabbableObject] '{gameObject.name}' grabbed by {handTransform.name}");
        }

        /// <summary>
        /// Called when VR Hand or Controller releases grip.
        /// </summary>
        public void Release()
        {
            if (!isHeld) return;

            transform.SetParent(null, true);
            rb.isKinematic = false;
            rb.useGravity = true;

            // Calculate average release velocity
            if (velocitySamples.Count > 0)
            {
                Vector3 avgVelocity = Vector3.zero;
                foreach (var v in velocitySamples) avgVelocity += v;
                avgVelocity /= velocitySamples.Count;

                rb.linearVelocity = avgVelocity * throwForceMultiplier;
            }

            velocitySamples.Clear();
            currentHoldingHand = null;
            isHeld = false;

            if (dropSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(dropSound);
            }

            onReleased?.Invoke();
            Debug.Log($"[VRGrabbableObject] '{gameObject.name}' released.");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isHeld && collision.relativeVelocity.magnitude > 0.6f)
            {
                if (impactSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(impactSound);
                }
            }
        }

        public void HighlightHover(bool highlight)
        {
            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.color = highlight ? hoverGlowColor : originalColor;
            }

            if (highlight) onHoverEntered?.Invoke();
            else onHoverExited?.Invoke();
        }
    }
}
