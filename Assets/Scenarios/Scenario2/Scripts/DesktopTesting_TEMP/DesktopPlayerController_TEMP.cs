// ===================================================================================
// TEMPORARY DESKTOP TESTING ONLY — REMOVE WHEN VR HARDWARE IS AVAILABLE
// ===================================================================================
// This script provides temporary desktop keyboard (WASD) and mouse look controls
// for testing the café scene in the Unity Editor without a VR headset.
//
// To disable desktop testing: deactivate or remove the DesktopTesting_TEMP GameObject.
// The existing VR Origin, Camera, Controllers, and Locomotion systems remain 100% untouched.
// ===================================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

namespace Kannko.Testing
{
    [AddComponentMenu("Desktop Testing (TEMP)/Desktop Player Controller (TEMP)")]
    [DisallowMultipleComponent]
    public class DesktopPlayerController_TEMP : MonoBehaviour
    {
        [Header("Target References (Auto-detected if unassigned)")]
        [SerializeField] private XROrigin m_XROrigin;
        [SerializeField] private Camera m_Camera;

        [Header("Movement Settings")]
        [Tooltip("Base walk speed in meters per second.")]
        [SerializeField] private float m_MoveSpeed = 2.5f;

        [Tooltip("Sprint multiplier when holding Shift.")]
        [SerializeField] private float m_SprintMultiplier = 1.8f;

        [Header("Mouse Look Settings")]
        [Tooltip("Mouse look sensitivity.")]
        [SerializeField] private float m_MouseSensitivity = 1.5f;

        [Tooltip("Minimum pitch angle (looking down).")]
        [SerializeField] private float m_MinPitch = -80f;

        [Tooltip("Maximum pitch angle (looking up).")]
        [SerializeField] private float m_MaxPitch = 80f;

        [Header("Cursor Lock Settings")]
        [Tooltip("Auto-lock mouse cursor on click in Game view.")]
        [SerializeField] private bool m_AutoLockCursor = true;

        private float m_Pitch = 0f;
        private float m_Yaw = 0f;
        private bool m_CursorLocked = false;

        public bool IsCursorLocked => m_CursorLocked;

        private void Start()
        {
            FindReferences();

            // Initialize rotation tracking
            if (m_XROrigin != null)
            {
                m_Yaw = m_XROrigin.transform.eulerAngles.y;
            }
            else
            {
                m_Yaw = transform.eulerAngles.y;
            }

            if (m_Camera != null)
            {
                m_Pitch = m_Camera.transform.localEulerAngles.x;
                if (m_Pitch > 180f) m_Pitch -= 360f;
            }

            if (m_AutoLockCursor && Application.isPlaying)
            {
                SetCursorLock(true);
            }
        }

        private void FindReferences()
        {
            if (m_XROrigin == null)
            {
                m_XROrigin = Object.FindAnyObjectByType<XROrigin>();
            }

            if (m_Camera == null)
            {
                if (m_XROrigin != null && m_XROrigin.Camera != null)
                {
                    m_Camera = m_XROrigin.Camera;
                }
                else
                {
                    m_Camera = Camera.main;
                }
            }
        }

        private void Update()
        {
            HandleCursorToggle();

            if (m_CursorLocked)
            {
                HandleMouseLook();
            }

            HandleKeyboardMovement();
        }

        private void HandleCursorToggle()
        {
            if (Mouse.current == null || Keyboard.current == null) return;

            // Click Game view to lock cursor
            if (Mouse.current.leftButton.wasPressedThisFrame && !m_CursorLocked)
            {
                SetCursorLock(true);
            }

            // Escape key to unlock cursor
            if (Keyboard.current.escapeKey.wasPressedThisFrame && m_CursorLocked)
            {
                SetCursorLock(false);
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
            if (Mouse.current == null) return;

            Vector2 delta = Mouse.current.delta.ReadValue() * 0.1f * m_MouseSensitivity;

            if (delta.sqrMagnitude > 0.0001f)
            {
                m_Yaw += delta.x;
                m_Pitch -= delta.y;
                m_Pitch = Mathf.Clamp(m_Pitch, m_MinPitch, m_MaxPitch);

                // Rotate XR Origin body horizontally (Yaw)
                if (m_XROrigin != null)
                {
                    m_XROrigin.transform.rotation = Quaternion.Euler(0f, m_Yaw, 0f);
                }

                // Rotate Camera vertically (Pitch)
                if (m_Camera != null)
                {
                    m_Camera.transform.localRotation = Quaternion.Euler(m_Pitch, 0f, 0f);
                }
            }
        }

        private void HandleKeyboardMovement()
        {
            if (Keyboard.current == null) return;

            float x = 0f;
            float z = 0f;

            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;

            bool isSprinting = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

            if (Mathf.Abs(x) > 0.01f || Mathf.Abs(z) > 0.01f)
            {
                Transform orientTarget = m_Camera != null ? m_Camera.transform : (m_XROrigin != null ? m_XROrigin.transform : transform);

                Vector3 forward = orientTarget.forward;
                forward.y = 0f;
                forward.Normalize();

                Vector3 right = orientTarget.right;
                right.y = 0f;
                right.Normalize();

                Vector3 moveDir = (forward * z + right * x).normalized;
                float currentSpeed = m_MoveSpeed * (isSprinting ? m_SprintMultiplier : 1f);

                Transform moveTarget = m_XROrigin != null ? m_XROrigin.transform : transform;
                moveTarget.position += moveDir * currentSpeed * Time.deltaTime;
            }
        }

        private void OnDisable()
        {
            if (m_CursorLocked)
            {
                SetCursorLock(false);
            }
        }

        // Configuration helper for editor setup
        public void Configure(XROrigin xrOrigin, Camera cam)
        {
            m_XROrigin = xrOrigin;
            m_Camera = cam;
        }
    }
}
