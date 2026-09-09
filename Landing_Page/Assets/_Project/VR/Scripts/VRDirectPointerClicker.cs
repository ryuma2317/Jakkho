using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRSetup
{
    /// <summary>
    /// Ensures seamless button clickability across Mouse Cursor, Touch, XR Device Simulator,
    /// and VR Ray Interactors using the new Unity Input System package.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRDirectPointerClicker : MonoBehaviour
    {
        private GraphicRaycaster graphicRaycaster;
        private EventSystem eventSystem;
        private VRCardNavigationManager navManager;

        private void Awake()
        {
            graphicRaycaster = GetComponent<GraphicRaycaster>();
            navManager = GetComponent<VRCardNavigationManager>();
            eventSystem = EventSystem.current;
        }

        private void Update()
        {
            if (eventSystem == null) eventSystem = EventSystem.current;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                ProcessPointerClick(Mouse.current.position.ReadValue());
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                ProcessPointerClick(Touchscreen.current.primaryTouch.position.ReadValue());
            }
            else if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                ProcessPointerClick(Pointer.current.position.ReadValue());
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                ProcessPointerClick(Input.mousePosition);
            }
#endif
        }

        /// <summary>
        /// Raycasts at screen position and triggers underlying button click.
        /// </summary>
        public void ProcessPointerClick(Vector2 screenPosition)
        {
            if (graphicRaycaster == null) return;

            var pointerEventData = new PointerEventData(eventSystem)
            {
                position = screenPosition
            };

            var results = new List<RaycastResult>();
            graphicRaycaster.Raycast(pointerEventData, results);

            foreach (var result in results)
            {
                var btn = result.gameObject.GetComponentInParent<Button>();
                if (btn != null && btn.interactable)
                {
                    Debug.Log($"[VRDirectPointerClicker] Direct click registered on Button: {btn.gameObject.name}");
                    btn.onClick.Invoke();
                    break;
                }
            }
        }
    }
}
