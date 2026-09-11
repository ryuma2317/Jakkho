using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Makes the phone's screen UI pop out of the physical phone as a large, vertical
/// floating panel near the player when the user interacts with the phone
/// (points at it or grabs it). It stays out while the phone has active content,
/// and slides back into the phone when the phone returns to its lock screen.
/// The real canvas is re-parented, so the floating screen stays fully interactive.
/// </summary>
public class PhoneScreenFloating : MonoBehaviour
{
    [SerializeField] private VRInteractiveSmartphone phone;
    [SerializeField] private RectTransform phoneCanvas;
    [SerializeField] private Transform floatingAnchor;
    [SerializeField] private GameObject floatingBacking;
    [SerializeField] private float floatingScale = 0.0009f;
    [SerializeField] private float popSpeed = 9f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Vector3 originalLocalScale;
    private bool floating;
    private bool summoned; // becomes true after the user points at / grabs the phone
    private float scaleFactor;

    public bool IsFloating => floating;

    private void Awake()
    {
        if (phone == null) phone = GetComponent<VRInteractiveSmartphone>();
        if (phoneCanvas == null) phoneCanvas = GetComponentInChildren<Canvas>(true)?.transform as RectTransform;

        if (phoneCanvas != null)
        {
            originalParent = phoneCanvas.parent;
            originalLocalPos = phoneCanvas.localPosition;
            originalLocalRot = phoneCanvas.localRotation;
            originalLocalScale = phoneCanvas.localScale;
        }

        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Popping the lock screen out is unnecessary — only summon once the phone
        // actually has something to show.
        if (phone != null && phone.CurrentState != VRInteractiveSmartphone.PhoneState.LockScreen)
            summoned = true;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (phone != null && phone.CurrentState != VRInteractiveSmartphone.PhoneState.LockScreen)
            summoned = true;
    }

    private void LateUpdate()
    {
        if (phone == null || phoneCanvas == null || floatingAnchor == null) return;

        var state = phone.CurrentState;

        // The authorization prompt and the outcome must always be readable — pop
        // those out even if the player never touched the phone (e.g. scanned by
        // looking at the QR code).
        if (state == VRInteractiveSmartphone.PhoneState.QRAuthorizationPrompt
            || state == VRInteractiveSmartphone.PhoneState.Defended
            || state == VRInteractiveSmartphone.PhoneState.Compromised)
        {
            summoned = true;
        }

        // Going back to the lock screen retracts everything.
        if (state == VRInteractiveSmartphone.PhoneState.LockScreen)
            summoned = false;

        // Stay on the phone itself while aiming the QR camera, so the reticle
        // feedback is visible in-hand during scanning.
        bool shouldFloat = summoned
            && state != VRInteractiveSmartphone.PhoneState.LockScreen
            && state != VRInteractiveSmartphone.PhoneState.QRScanner;

        if (shouldFloat)
        {
            if (!floating)
            {
                floating = true;
                scaleFactor = 0f;
                phoneCanvas.SetParent(floatingAnchor, false);
                phoneCanvas.localPosition = Vector3.zero;
                phoneCanvas.localRotation = Quaternion.identity;
                phoneCanvas.localScale = Vector3.zero;
                if (floatingBacking != null) floatingBacking.SetActive(true);
            }

            scaleFactor = Mathf.Lerp(scaleFactor, 1f, Time.deltaTime * popSpeed);
            phoneCanvas.localScale = Vector3.one * (floatingScale * scaleFactor);
        }
        else if (floating)
        {
            floating = false;
            phoneCanvas.SetParent(originalParent, false);
            phoneCanvas.localPosition = originalLocalPos;
            phoneCanvas.localRotation = originalLocalRot;
            phoneCanvas.localScale = originalLocalScale;
            if (floatingBacking != null) floatingBacking.SetActive(false);
        }
    }
}