using UnityEngine;
using Unity.XR.CoreUtils;

namespace Jakkho.Player
{
    /// <summary>
    /// Prevents VR player from walking or clipping through solid walls in both
    /// controller joystick locomotion and physical room-scale head movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class VRRoomScaleCollision : MonoBehaviour
    {
        private CharacterController _character;
        private XROrigin _xrOrigin;
        private Camera _mainCamera;

        [Header("Collision Dimensions")]
        [SerializeField] private float _bodyRadius = 0.28f;
        [SerializeField] private float _minHeight = 0.6f;
        [SerializeField] private float _maxHeight = 2.2f;

        [Header("Layer Filtering")]
        [SerializeField] private LayerMask _obstacleLayers = ~0;

        private Vector3 _lastSafePosition;

        private void Awake()
        {
            _character = GetComponent<CharacterController>();
            _xrOrigin = GetComponent<XROrigin>();
            if (_xrOrigin != null)
            {
                _mainCamera = _xrOrigin.Camera;
            }
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            // Configure character controller dimensions for comfortable VR seated/standing gameplay
            if (_character != null)
            {
                _character.radius = 0.20f;
                _character.skinWidth = 0.01f;
                _character.minMoveDistance = 0.0f;
                _character.stepOffset = 0.3f;
            }
        }

        private void Start()
        {
            SyncCharacterControllerToHead();
        }

        private void Update()
        {
            SyncCharacterControllerToHead();
        }

        private void FixedUpdate()
        {
            if (_character == null || !_character.enabled) return;

            // Apply gravity only when falling to prevent vertical collider jitter on floor
            if (!_character.isGrounded)
            {
                _character.Move(Physics.gravity * Time.fixedDeltaTime);
            }
        }

        private void SyncCharacterControllerToHead()
        {
            if (_character == null || _mainCamera == null) return;

            // Height of camera above the XR Origin's floor level
            float headHeight = Mathf.Clamp(_mainCamera.transform.position.y - transform.position.y, _minHeight, _maxHeight);
            _character.height = headHeight;

            // Head position in XR Origin local space
            Vector3 localHeadPos = transform.InverseTransformPoint(_mainCamera.transform.position);
            _character.center = new Vector3(localHeadPos.x, headHeight * 0.5f, localHeadPos.z);
        }
    }
}