using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRSetup
{
    /// <summary>
    /// Smooth hover scale and press bounce animation for VR UI buttons and cards.
    /// Supports both Mouse/Touch and VR Ray Interactors / Tracked Devices via EventSystem interfaces.
    /// </summary>
    [DisallowMultipleComponent]
    public class VRCardButtonAnimator : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, 
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Animation Settings")]
        [Tooltip("Scale factor when hovered (e.g. 1.08 = 8% scale up).")]
        [SerializeField] private float hoverScaleMultiplier = 1.08f;

        [Tooltip("Scale factor when pressed (e.g. 0.92 = 8% scale down).")]
        [SerializeField] private float pressScaleMultiplier = 0.92f;

        [Tooltip("Speed of scale interpolation.")]
        [SerializeField] private float animationSpeed = 16f;

        private Vector3 initialScale;
        private Vector3 targetScale;
        private bool isHovered = false;
        private bool isPressed = false;
        private Selectable selectable;
        private Button button;

        private void Awake()
        {
            initialScale = transform.localScale;
            targetScale = initialScale;
            selectable = GetComponent<Selectable>();
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            transform.localScale = initialScale;
            targetScale = initialScale;
            isHovered = false;
            isPressed = false;
        }

        private void Update()
        {
            if (transform.localScale != targetScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable != null && !selectable.interactable) return;
            isHovered = true;
            UpdateTargetScale();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
            UpdateTargetScale();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable != null && !selectable.interactable) return;
            isPressed = true;
            UpdateTargetScale();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            UpdateTargetScale();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (selectable != null && !selectable.interactable) return;

            // Bounce feedback on click
            targetScale = initialScale * pressScaleMultiplier;

            // Ensure button onClick is directly triggered
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }

        private void UpdateTargetScale()
        {
            if (isPressed)
            {
                targetScale = initialScale * pressScaleMultiplier;
            }
            else if (isHovered)
            {
                targetScale = initialScale * hoverScaleMultiplier;
            }
            else
            {
                targetScale = initialScale;
            }
        }
    }
}
