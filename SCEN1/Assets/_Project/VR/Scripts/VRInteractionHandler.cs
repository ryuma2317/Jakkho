using System;
using UnityEngine;
using UnityEngine.Events;

namespace CADT.VR.Interaction
{
    /// <summary>
    /// Shared VR interaction handler managing raycast laser beams, direct poke interactions,
    /// and input mode switching between hand tracking and VR controllers.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRInteractionHandler : MonoBehaviour
    {
        public enum InputMode
        {
            Controllers,
            Hands
        }

        [Header("--- Input Configuration ---")]
        [SerializeField] private InputMode activeInputMode = InputMode.Hands;
        [SerializeField] private float raycastDistance = 6.0f;
        [SerializeField] private LayerMask interactableLayer = ~0;

        [Header("--- Raycast Pointers ---")]
        [SerializeField] private LineRenderer leftPointerBeam;
        [SerializeField] private LineRenderer rightPointerBeam;

        [Header("--- Events ---")]
        public UnityEvent<InputMode> onInputModeChanged;
        public UnityEvent<GameObject> onHoverEnter;
        public UnityEvent<GameObject> onHoverExit;
        public UnityEvent<GameObject> onObjectSelected;

        public InputMode ActiveMode => activeInputMode;

        public void SetInputMode(InputMode newMode)
        {
            if (activeInputMode != newMode)
            {
                activeInputMode = newMode;
                onInputModeChanged?.Invoke(activeInputMode);
            }
        }

        public void TriggerSelect(GameObject target)
        {
            if (target != null)
            {
                onObjectSelected?.Invoke(target);
            }
        }
    }
}
