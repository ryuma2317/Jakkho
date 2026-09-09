using UnityEngine;
using Unity.XR.CoreUtils;

namespace VRSetup
{
    /// <summary>
    /// Utility script to validate and maintain XR Rig, Camera tracking, Character Controller,
    /// and locomotion configurations for room-scale/standing VR.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XROrigin))]
    public class XRRigLocomotionValidator : MonoBehaviour
    {
        [Header("Locomotion Configuration")]
        [Tooltip("Move speed in meters per second.")]
        [SerializeField] private float moveSpeed = 2.5f;

        [Tooltip("Snap turn angle in degrees.")]
        [SerializeField] private float snapTurnAngle = 45f;

        private XROrigin xrOrigin;
        private CharacterController characterController;

        private void Awake()
        {
            xrOrigin = GetComponent<XROrigin>();
            ValidateRig();
        }

        /// <summary>
        /// Validates and ensures all components are correctly configured and grounded.
        /// </summary>
        public void ValidateRig()
        {
            // 1. Ensure transform scale is normal (1, 1, 1) and layer is Default (0)
            transform.localScale = Vector3.one;
            gameObject.layer = 0; // Default layer for physics collision

            // 2. Ensure Floor tracking origin mode for standing VR
            if (xrOrigin != null)
            {
                xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
                xrOrigin.CameraYOffset = 0f;

                if (xrOrigin.Camera != null)
                {
                    xrOrigin.Camera.tag = "MainCamera";
                    xrOrigin.Camera.nearClipPlane = 0.01f;
                    xrOrigin.Camera.farClipPlane = 1000f;
                    xrOrigin.Camera.cullingMask = -1; // Render all layers
                }
            }

            // 3. Ensure CharacterController is properly sized and grounded
            if (!TryGetComponent<CharacterController>(out characterController))
            {
                characterController = gameObject.AddComponent<CharacterController>();
            }

            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.slopeLimit = 45f;
            characterController.stepOffset = 0.3f;
            characterController.skinWidth = 0.05f;

            Debug.Log("[XRRigLocomotionValidator] XR Rig grounded and camera settings verified.");
        }
    }
}
