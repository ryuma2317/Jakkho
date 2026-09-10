using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kannko
{
    /// <summary>
    /// Temporary Desktop Testing Controller for editor/non-VR development.
    /// Allows exploring the scene in Play Mode using:
    /// - WASD for Movement (Shift to sprint)
    /// - Mouse for Looking around
    /// - Left Mouse Button to Grab, hold, move, and release XRGrabInteractables
    /// Does NOT modify or replace the VR Interaction systems and can be easily disabled or removed.
    /// </summary>
    [AddComponentMenu("VR Prototype/XR Desktop Testing Controller")]
    [DisallowMultipleComponent]
    public class XRDesktopTestingController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Base walk speed in meters per second.")]
        [SerializeField] private float m_MoveSpeed = 2.5f;

        [Tooltip("Sprint speed multiplier when holding Shift.")]
        [SerializeField] private float m_SprintMultiplier = 1.8f;

        [Header("Mouse Look Settings")]
        [Tooltip("Mouse look sensitivity.")]
        [SerializeField] private float m_MouseSensitivity = 1.5f;

        [Tooltip("Minimum look pitch angle (looking down).")]
        [SerializeField] private float m_MinPitch = -80f;

        [Tooltip("Maximum look pitch angle (looking up).")]
        [SerializeField] private float m_MaxPitch = 80f;

        [Header("Desktop Grabbing Settings")]
        [Tooltip("Maximum distance to grab an object from the camera.")]
        [SerializeField] private float m_MaxGrabDistance = 3.0f;

        [Tooltip("Speed at which the held object follows the camera target position.")]
        [SerializeField] private float m_GrabFollowSpeed = 22.0f;

        [Header("Editor Testing Options")]
        [Tooltip("Auto-lock mouse cursor when clicking inside the Game view.")]
        [SerializeField] private bool m_AutoLockCursor = true;

        [Tooltip("Show a small center dot reticle in Game view when cursor is locked.")]
        [SerializeField] private bool m_ShowCenterReticle = true;

        private XROrigin m_XROrigin;
        private Camera m_Camera;
        private float m_Pitch = 0f;
        private float m_Yaw = 0f;
        private bool m_CursorLocked = false;

        // Grabbing state
        private Rigidbody m_HeldRigidbody = null;
        private bool m_OriginalUseGravity = true;
        private float m_HoldDistance = 0.85f;
        private Vector3 m_LastTargetPosition = Vector3.zero;
        private Vector3 m_ThrowVelocity = Vector3.zero;
        private bool m_IsHoveringGrabbable = false;

        public float moveSpeed
        {
            get => m_MoveSpeed;
            set => m_MoveSpeed = value;
        }

        public float mouseSensitivity
        {
            get => m_MouseSensitivity;
            set => m_MouseSensitivity = value;
        }

        private void Start()
        {
            m_XROrigin = GetComponent<XROrigin>();
            if (m_XROrigin == null)
            {
                m_XROrigin = GetComponentInParent<XROrigin>();
            }

            if (m_XROrigin != null && m_XROrigin.Camera != null)
            {
                m_Camera = m_XROrigin.Camera;
            }
            else
            {
                m_Camera = Camera.main;
            }

            // Initialize rotation tracking
            m_Yaw = transform.eulerAngles.y;
            if (m_Camera != null)
            {
                m_Pitch = m_Camera.transform.localEulerAngles.x;
                if (m_Pitch > 180f) m_Pitch -= 360f;
            }
        }

        private void Update()
        {
            HandleCursorLock();
            HandleMouseLook();
            HandleKeyboardMovement();
            HandleGrabbing();
        }

        private void HandleCursorLock()
        {
            // Lock cursor on click inside game window
            if (m_AutoLockCursor)
            {
                if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
                {
                    if (!m_CursorLocked)
                    {
                        SetCursorLock(true);
                    }
                }

                // Unlock cursor on Escape
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    if (m_HeldRigidbody != null)
                    {
                        ReleaseHeldObject();
                    }
                    SetCursorLock(false);
                }
            }
        }

        private void SetCursorLock(bool locked)
        {
            m_CursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleMouseLook()
        {
            bool isLooking = m_CursorLocked || (Mouse.current != null && Mouse.current.rightButton.isPressed);
            if (!isLooking) return;

            Vector2 delta = Vector2.zero;
            if (Mouse.current != null)
            {
                delta = Mouse.current.delta.ReadValue() * 0.1f * m_MouseSensitivity;
            }

            if (delta.sqrMagnitude > 0.0001f)
            {
                m_Yaw += delta.x;
                m_Pitch -= delta.y;
                m_Pitch = Mathf.Clamp(m_Pitch, m_MinPitch, m_MaxPitch);

                // Rotate XR Origin body horizontally (Yaw)
                transform.rotation = Quaternion.Euler(0f, m_Yaw, 0f);

                // Rotate Camera vertically (Pitch)
                if (m_Camera != null)
                {
                    m_Camera.transform.localRotation = Quaternion.Euler(m_Pitch, 0f, 0f);
                }
            }
        }

        private void HandleKeyboardMovement()
        {
            float x = 0f;
            float z = 0f;
            bool isSprinting = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) z += 1f;
                if (Keyboard.current.sKey.isPressed) z -= 1f;
                if (Keyboard.current.aKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed) x += 1f;

                isSprinting = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            }

            if (Mathf.Abs(x) > 0.01f || Mathf.Abs(z) > 0.01f)
            {
                Vector3 forward = m_Camera != null ? m_Camera.transform.forward : transform.forward;
                forward.y = 0f;
                forward.Normalize();

                Vector3 right = m_Camera != null ? m_Camera.transform.right : transform.right;
                right.y = 0f;
                right.Normalize();

                Vector3 moveDir = (forward * z + right * x).normalized;
                float currentSpeed = m_MoveSpeed * (isSprinting ? m_SprintMultiplier : 1f);

                transform.position += moveDir * currentSpeed * Time.deltaTime;
            }
        }

        private void HandleGrabbing()
        {
            if (m_Camera == null || Mouse.current == null) return;

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

            // Left Mouse Button Down: Try Grab
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
                            m_OriginalUseGravity = rb.useGravity;
                            rb.useGravity = false;
                            m_HoldDistance = Mathf.Clamp(hit.distance, 0.6f, 1.8f);
                            m_LastTargetPosition = rb.position;
                            m_ThrowVelocity = Vector3.zero;
                        }
                    }
                }
            }

            // Holding Object: Move with camera
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
                    // Left Mouse Button Released: Release/Drop
                    ReleaseHeldObject();
                }
            }
        }

        private void ReleaseHeldObject()
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
                m_HeldRigidbody = null;
                m_IsHoveringGrabbable = false;
            }
        }

        private void OnGUI()
        {
            if (!m_ShowCenterReticle || !m_CursorLocked || !Application.isPlaying) return;

            float size = 6f;
            float x = (Screen.width - size) * 0.5f;
            float y = (Screen.height - size) * 0.5f;

            Color dotColor = m_HeldRigidbody != null ? new Color(0.2f, 0.9f, 0.6f, 0.9f) : 
                            (m_IsHoveringGrabbable ? new Color(0.2f, 0.85f, 1.0f, 0.95f) : new Color(1f, 1f, 1f, 0.5f));

            Texture2D texture = Texture2D.whiteTexture;
            GUI.color = dotColor;
            GUI.DrawTexture(new Rect(x, y, size, size), texture);
            GUI.color = Color.white;
        }

        private void OnDisable()
        {
            if (m_HeldRigidbody != null)
            {
                ReleaseHeldObject();
            }

            if (m_CursorLocked)
            {
                SetCursorLock(false);
            }
        }
    }
}
