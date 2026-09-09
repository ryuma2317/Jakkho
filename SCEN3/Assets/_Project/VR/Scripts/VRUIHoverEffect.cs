using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

public class VRUIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Depth & Scale Settings")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressedScale = 0.96f;
    [SerializeField] private float hoverZOffset = -12.0f; // Shifts forward in local Z towards player
    [SerializeField] private float lerpSpeed = 16.0f;

    [Header("Audio & Haptics")]
    [SerializeField] private bool triggerHaptics = true;
    [SerializeField] private float hoverHapticAmplitude = 0.12f;
    [SerializeField] private float hoverHapticDuration = 0.04f;
    [SerializeField] private float clickHapticAmplitude = 0.40f;
    [SerializeField] private float clickHapticDuration = 0.08f;

    private RectTransform rectTransform;
    private float defaultZ;
    private float targetZ;
    private float targetScale = 1.0f;
    private bool isHovered = false;
    private bool isPressed = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            defaultZ = rectTransform.anchoredPosition3D.z;
            targetZ = defaultZ;
        }
    }

    private void OnEnable()
    {
        isHovered = false;
        isPressed = false;
        targetScale = 1.0f;
        if (rectTransform != null)
        {
            targetZ = defaultZ;
            rectTransform.localScale = Vector3.one;
            Vector3 pos = rectTransform.anchoredPosition3D;
            pos.z = defaultZ;
            rectTransform.anchoredPosition3D = pos;
        }
    }

    private void Update()
    {
        if (rectTransform == null) return;

        // Smooth scale interpolation
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * lerpSpeed);

        // Smooth Z-depth interpolation
        Vector3 pos = rectTransform.anchoredPosition3D;
        pos.z = Mathf.Lerp(pos.z, targetZ, Time.unscaledDeltaTime * lerpSpeed);
        rectTransform.anchoredPosition3D = pos;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed)
        {
            targetScale = hoverScale;
            targetZ = defaultZ + hoverZOffset;
        }

        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.PlayHoverSound();
            if (triggerHaptics)
            {
                VRInteractionHandler.Instance.TriggerHaptic(XRNode.RightHand, hoverHapticAmplitude, hoverHapticDuration);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed)
        {
            targetScale = 1.0f;
            targetZ = defaultZ;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        targetScale = pressedScale;
        targetZ = defaultZ + (hoverZOffset * 0.4f);

        if (triggerHaptics && VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.TriggerHaptic(XRNode.RightHand, clickHapticAmplitude, clickHapticDuration);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        if (isHovered)
        {
            targetScale = hoverScale;
            targetZ = defaultZ + hoverZOffset;
        }
        else
        {
            targetScale = 1.0f;
            targetZ = defaultZ;
        }
    }
}
