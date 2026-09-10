// ===================================================================================
// TEMPORARY DESKTOP TESTING ONLY — REMOVE WHEN VR HARDWARE IS AVAILABLE
// ===================================================================================
// This script provides temporary desktop mouse grabbing for testing existing
// XRGrabInteractable objects (CoffeeCup, LaptopBag, Smartphone) without VR controllers.
//
// Controls:
// - Left Mouse Button (Hold): Grab and hold object in front of camera
// - Move Mouse: Move and orient held object naturally with camera aim
// - Left Mouse Button (Release): Drop object with physics velocity
// - 'E' Key (while holding Smartphone): Test phone opening/closing state transition
//
// To disable: deactivate or remove the DesktopTesting_TEMP GameObject.
// The existing VR Origin, Camera, Controllers, and Grabbing interactors remain 100% untouched.
// ===================================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Kannko.Phone;

namespace Kannko.Testing
{
    [AddComponentMenu("Desktop Testing (TEMP)/Desktop Grab Controller (TEMP)")]
    [DisallowMultipleComponent]
    public class DesktopGrabController_TEMP : MonoBehaviour
    {
        [Header("Target Camera (Auto-detected if unassigned)")]
        [SerializeField] private Camera m_Camera;

        [Header("Grabbing Settings")]
        [Tooltip("Maximum raycast distance to grab an object from the camera.")]
        [SerializeField] private float m_MaxGrabDistance = 3.0f;

        [Tooltip("Follow speed for the held object to track camera aim.")]
        [SerializeField] private float m_GrabFollowSpeed = 22.0f;

        [Header("Visual Reticle")]
        [Tooltip("Show a center crosshair/dot reticle when looking around.")]
        [SerializeField] private bool m_ShowReticle = true;

        private Rigidbody m_HeldRigidbody = null;
        private XRGrabInteractable m_HeldInteractable = null;
        private bool m_OriginalUseGravity = true;
        private float m_HoldDistance = 0.85f;
        private Vector3 m_LastTargetPosition = Vector3.zero;
        private Vector3 m_ThrowVelocity = Vector3.zero;
        private bool m_IsHoveringGrabbable = false;

        public bool IsHoldingObject => m_HeldRigidbody != null;
        public Rigidbody HeldRigidbody => m_HeldRigidbody;

        private void Start()
        {
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
            }
        }

        private void Update()
        {
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
                if (m_Camera == null) return;
            }

            HandleGrabbing();
            HandleSecondaryAction();
        }

        private void HandleGrabbing()
        {
            if (Mouse.current == null) return;

            Ray ray = new Ray(m_Camera.transform.position, m_Camera.transform.forward);

            // Update hovering check for reticle feedback
            if (m_HeldRigidbody == null)
            {
                if (Physics.Raycast(ray, out RaycastHit hoverHit, m_MaxGrabDistance))
                {
                    XRGrabInteractable grabInteractable = hoverHit.collider.GetComponentInParent<XRGrabInteractable>();
                    m_IsHoveringGrabbable = grabInteractable != null;
                }
                else
                {
                    m_IsHoveringGrabbable = false;
                }
            }

            // Left Mouse Button Pressed: Attempt Grab
            if (Mouse.current.leftButton.wasPressedThisFrame && m_HeldRigidbody == null)
            {
                if (Physics.Raycast(ray, out RaycastHit hit, m_MaxGrabDistance))
                {
                    XRGrabInteractable grabInteractable = hit.collider.GetComponentInParent<XRGrabInteractable>();
                    if (grabInteractable != null)
                    {
                        Rigidbody rb = hit.collider.attachedRigidbody;
                        if (rb == null) rb = grabInteractable.GetComponent<Rigidbody>();

                        if (rb != null && !rb.isKinematic)
                        {
                            m_HeldRigidbody = rb;
                            m_HeldInteractable = grabInteractable;
                            m_OriginalUseGravity = rb.useGravity;
                            rb.useGravity = false;
                            m_HoldDistance = Mathf.Clamp(hit.distance, 0.6f, 1.8f);
                            m_LastTargetPosition = rb.position;
                            m_ThrowVelocity = Vector3.zero;
                        }
                    }
                }
            }

            // Object currently held: follow camera target smoothly
            if (m_HeldRigidbody != null)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    Vector3 targetPos = m_Camera.transform.position + m_Camera.transform.forward * m_HoldDistance;

                    if (Time.deltaTime > 0f)
                    {
                        m_ThrowVelocity = (targetPos - m_LastTargetPosition) / Time.deltaTime;
                    }
                    m_LastTargetPosition = targetPos;

                    Vector3 desiredVelocity = (targetPos - m_HeldRigidbody.position) * m_GrabFollowSpeed;

#if UNITY_6000_0_OR_NEWER
                    m_HeldRigidbody.linearVelocity = desiredVelocity;
#else
                    m_HeldRigidbody.velocity = desiredVelocity;
#endif
                    m_HeldRigidbody.angularVelocity = Vector3.zero;
                }
                else
                {
                    // Left Mouse Button Released: Drop/Release
                    ReleaseHeldObject();
                }
            }
        }

        /// <summary>
        /// Secondary action test shortcut: pressing 'E' while holding an interactive object
        /// (e.g. Smartphone) triggers its opening/closing state transition.
        /// </summary>
        private void HandleSecondaryAction()
        {
            if (m_HeldRigidbody == null || Keyboard.current == null) return;

            if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)
            {
                SmartphoneController phone = m_HeldRigidbody.GetComponent<SmartphoneController>();
                if (phone == null && m_HeldInteractable != null)
                {
                    phone = m_HeldInteractable.GetComponent<SmartphoneController>();
                }

                if (phone != null)
                {
                    phone.TogglePhoneState();
                }
            }
        }

        public void ReleaseHeldObject()
        {
            if (m_HeldRigidbody != null)
            {
                m_HeldRigidbody.useGravity = m_OriginalUseGravity;

                Vector3 releaseVelocity = Vector3.ClampMagnitude(m_ThrowVelocity, 8.0f);
#if UNITY_6000_0_OR_NEWER
                m_HeldRigidbody.linearVelocity = releaseVelocity;
#else
                m_HeldRigidbody.velocity = releaseVelocity;
#endif
                // If the held object is a smartphone, releasing automatically closes it
                SmartphoneController phone = m_HeldRigidbody.GetComponent<SmartphoneController>();
                if (phone != null && (phone.CurrentState == SmartphoneState.ActiveOpen || phone.CurrentState == SmartphoneState.Closing))
                {
                    phone.TriggerClose();
                }

                m_HeldRigidbody = null;
                m_HeldInteractable = null;
                m_IsHoveringGrabbable = false;
            }
        }

        private void OnGUI()
        {
            if (!m_ShowReticle || !Application.isPlaying) return;

            float size = 6f;
            float x = (Screen.width - size) * 0.5f;
            float y = (Screen.height - size) * 0.5f;

            Color dotColor = m_HeldRigidbody != null ? new Color(0.2f, 0.9f, 0.6f, 0.9f) :
                            (m_IsHoveringGrabbable ? new Color(0.2f, 0.85f, 1.0f, 0.95f) : new Color(1f, 1f, 1f, 0.45f));

            Texture2D texture = Texture2D.whiteTexture;
            GUI.color = dotColor;
            GUI.DrawTexture(new Rect(x, y, size, size), texture);
            GUI.color = Color.white;
        }

        private void OnDisable()
        {
            ReleaseHeldObject();
        }

        // Configuration helper
        public void Configure(Camera cam)
        {
            m_Camera = cam;
        }
    }
}
