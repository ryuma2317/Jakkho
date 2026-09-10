using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CADT.VR.Cybersecurity
{
    /// <summary>
    /// Lead VR Spatial UI: Provides volumetric glow pulses, dynamic color transitions,
    /// 3D depth pop displacement (Z-axis offset), scale punch, and audio/haptic cues
    /// when targeted by an XR Ray Interactor in World Space.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SpatialUIHoverEffect : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("--- Target Graphics ---")]
        [Tooltip("Background panel image")]
        [SerializeField] private Image backgroundImage;

        [Tooltip("Border / outline glow image or highlight graphic")]
        [SerializeField] private Graphic borderGlowGraphic;

        [Tooltip("Optional text label to tint or brighten on hover")]
        [SerializeField] private TextMeshProUGUI labelText;

        [Header("--- Color Configuration ---")]
        [Tooltip("Use custom predefined theme profile")]
        [SerializeField] private UIThemeType theme = UIThemeType.Custom;

        public enum UIThemeType
        {
            Custom,
            BranchA_FailThreat,   // Muted Amber -> Vivid Red on Hover/Click
            BranchB_SuccessTriage // Holographic Cyan -> Emerald Green on Hover/Click
        }

        [SerializeField] private Color normalColor = new Color(0.06f, 0.09f, 0.16f, 0.65f); // #0F172A translucent
        [SerializeField] private Color hoverColor = new Color(0.0f, 0.94f, 1.0f, 0.9f);       // #00F0FF glowing cyan
        [SerializeField] private Color pressedColor = new Color(0.06f, 0.72f, 0.51f, 1.0f);   // #10B981 emerald
        [SerializeField] private Color disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

        [Header("--- 3D Depth & Transform Response ---")]
        [Tooltip("Local Z-axis displacement when hovered (VR floating pop toward user) in meters/units")]
        [SerializeField] private float hoverZOffset = -15f; // Local Z offset for world canvas

        [Tooltip("Scale multiplier on hover (e.g., 1.05 = 5% expansion)")]
        [SerializeField] private float hoverScaleMultiplier = 1.05f;

        [Tooltip("Scale multiplier on press/click")]
        [SerializeField] private float pressedScaleMultiplier = 0.96f;

        [Tooltip("Animation speed for smooth spring interpolation")]
        [SerializeField] private float animationSpeed = 12f;

        [Header("--- Spatial Audio Feedback ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverEnterSound;
        [SerializeField] private AudioClip clickSound;

        private RectTransform rectTransform;
        private Vector3 initialLocalPosition;
        private Vector3 initialLocalScale;

        private Vector3 targetLocalPosition;
        private Vector3 targetLocalScale;
        private Color targetColor;

        private bool isHovered = false;
        private bool isPressed = false;
        private bool isInteractable = true;

        public bool IsPressed => isPressed;
        public bool IsHovered => isHovered;

        private Coroutine activeAnimationRoutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            initialLocalPosition = rectTransform.localPosition;
            initialLocalScale = rectTransform.localScale;

            targetLocalPosition = initialLocalPosition;
            targetLocalScale = initialLocalScale;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            ApplyThemeColors();
            targetColor = normalColor;
            ApplyGraphicColor(normalColor);
        }

        private void OnEnable()
        {
            ResetVisuals();
        }

        private void ApplyThemeColors()
        {
            switch (theme)
            {
                case UIThemeType.BranchA_FailThreat:
                    // Muted Amber base -> Bright Warning Red on Hover
                    normalColor = new Color(0.85f, 0.47f, 0.02f, 0.75f);  // #D97706 Muted Amber
                    hoverColor = new Color(0.94f, 0.27f, 0.27f, 0.95f);   // #EF4444 Bright Red
                    pressedColor = new Color(0.72f, 0.11f, 0.11f, 1.0f);  // #B91C1C Deep Red
                    break;

                case UIThemeType.BranchB_SuccessTriage:
                    // Cyan base -> Emerald Green on Hover
                    normalColor = new Color(0.0f, 0.75f, 0.85f, 0.75f);   // #00C0D8 Holographic Cyan
                    hoverColor = new Color(0.06f, 0.72f, 0.51f, 0.95f);   // #10B981 Emerald Green
                    pressedColor = new Color(0.02f, 0.59f, 0.41f, 1.0f);  // #059669 Deep Emerald
                    break;

                case UIThemeType.Custom:
                default:
                    // User defined values in inspector
                    break;
            }
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            if (!isInteractable)
            {
                isHovered = false;
                isPressed = false;
                targetColor = disabledColor;
                targetLocalPosition = initialLocalPosition;
                targetLocalScale = initialLocalScale;
                TriggerAnimationUpdate();
            }
            else
            {
                targetColor = normalColor;
                TriggerAnimationUpdate();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;

            isHovered = true;
            targetColor = hoverColor;
            targetLocalPosition = initialLocalPosition + new Vector3(0f, 0f, hoverZOffset);
            targetLocalScale = initialLocalScale * hoverScaleMultiplier;

            if (hoverEnterSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hoverEnterSound);
            }

            TriggerAnimationUpdate();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable) return;

            isHovered = false;
            isPressed = false;
            targetColor = normalColor;
            targetLocalPosition = initialLocalPosition;
            targetLocalScale = initialLocalScale;

            TriggerAnimationUpdate();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable) return;

            isPressed = true;
            targetColor = pressedColor;
            targetLocalScale = initialLocalScale * pressedScaleMultiplier;

            if (clickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }

            TriggerAnimationUpdate();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isInteractable) return;

            isPressed = false;
            if (isHovered)
            {
                targetColor = hoverColor;
                targetLocalScale = initialLocalScale * hoverScaleMultiplier;
                targetLocalPosition = initialLocalPosition + new Vector3(0f, 0f, hoverZOffset);
            }
            else
            {
                targetColor = normalColor;
                targetLocalScale = initialLocalScale;
                targetLocalPosition = initialLocalPosition;
            }

            TriggerAnimationUpdate();
        }

        private void TriggerAnimationUpdate()
        {
            if (!gameObject.activeInHierarchy) return;

            if (activeAnimationRoutine != null)
            {
                StopCoroutine(activeAnimationRoutine);
            }
            activeAnimationRoutine = StartCoroutine(AnimateSpatialHoverRoutine());
        }

        private IEnumerator AnimateSpatialHoverRoutine()
        {
            while (Vector3.Distance(rectTransform.localPosition, targetLocalPosition) > 0.01f ||
                   Vector3.Distance(rectTransform.localScale, targetLocalScale) > 0.005f ||
                   GetColorDifference() > 0.01f)
            {
                float delta = Time.deltaTime * animationSpeed;

                rectTransform.localPosition = Vector3.Lerp(rectTransform.localPosition, targetLocalPosition, delta);
                rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetLocalScale, delta);

                if (backgroundImage != null)
                {
                    backgroundImage.color = Color.Lerp(backgroundImage.color, targetColor, delta);
                }

                if (borderGlowGraphic != null)
                {
                    borderGlowGraphic.color = Color.Lerp(borderGlowGraphic.color, targetColor, delta);
                }

                yield return null;
            }

            rectTransform.localPosition = targetLocalPosition;
            rectTransform.localScale = targetLocalScale;
            ApplyGraphicColor(targetColor);
        }

        private void ApplyGraphicColor(Color color)
        {
            if (backgroundImage != null) backgroundImage.color = color;
            if (borderGlowGraphic != null) borderGlowGraphic.color = color;
        }

        private float GetColorDifference()
        {
            if (backgroundImage != null)
            {
                return Mathf.Abs(backgroundImage.color.r - targetColor.r) +
                       Mathf.Abs(backgroundImage.color.g - targetColor.g) +
                       Mathf.Abs(backgroundImage.color.b - targetColor.b) +
                       Mathf.Abs(backgroundImage.color.a - targetColor.a);
            }
            return 0f;
        }

        public void ResetVisuals()
        {
            isHovered = false;
            isPressed = false;
            rectTransform.localPosition = initialLocalPosition;
            rectTransform.localScale = initialLocalScale;
            targetLocalPosition = initialLocalPosition;
            targetLocalScale = initialLocalScale;
            ApplyThemeColors();
            targetColor = normalColor;
            ApplyGraphicColor(normalColor);
        }
    }
}
