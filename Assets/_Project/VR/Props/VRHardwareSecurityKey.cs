using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Physical USB-C 2FA Hardware Security Key (e.g. YubiKey) for VR.
/// Player can physically pick it up from the desk and insert it into a glowing
/// workstation port to enforce hardware two-factor authentication.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class VRHardwareSecurityKey : MonoBehaviour
{
    [Header("Terminal Port Socket")]
    [SerializeField] private Transform terminalSlotTransform;
    [SerializeField] private float snapDistance = 0.15f;
    [SerializeField] private MeshRenderer ledRenderer;
    [SerializeField] private Material activeLedMaterial;

    [Header("Feedback Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip insertClip;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isInserted = false;
    private bool isHeld = false;

    public bool IsInserted => isInserted;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void Update()
    {
        if (isInserted) return;

        if (terminalSlotTransform != null)
        {
            float dist = Vector3.Distance(transform.position, terminalSlotTransform.position);
            if (dist <= snapDistance)
            {
                SnapIntoTerminalPort();
            }
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
        if (terminalSlotTransform != null)
        {
            float dist = Vector3.Distance(transform.position, terminalSlotTransform.position);
            if (dist <= snapDistance)
            {
                SnapIntoTerminalPort();
            }
        }
    }

    public void SnapIntoTerminalPort()
    {
        if (isInserted) return;
        isInserted = true;

        // Release from hand if currently grabbed
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            if (grabInteractable.interactionManager != null)
            {
                grabInteractable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grabInteractable);
            }
            grabInteractable.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Align directly inside the port
        if (terminalSlotTransform != null)
        {
            transform.SetParent(terminalSlotTransform, true);
            transform.position = terminalSlotTransform.position;
            transform.rotation = terminalSlotTransform.rotation;
        }

        // Activate LED
        if (ledRenderer != null && activeLedMaterial != null)
        {
            ledRenderer.material = activeLedMaterial;
        }

        // Audio chime
        if (audioSource != null && insertClip != null)
        {
            audioSource.PlayOneShot(insertClip);
        }

        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.PlaySuccessSound();
            VRInteractionHandler.Instance.TriggerBothHandsHaptic(0.6f, 0.2f);
        }

        Debug.Log("[VRHardwareSecurityKey] 2FA Security Key physically plugged into terminal!");

        // Notify scenario manager
        var mgr = FindObjectOfType<Scenario3_FakeLinkManager>();
        if (mgr != null)
        {
            mgr.OnEnable2FAClicked();
        }
    }

    public void SetTerminalSlot(Transform slot)
    {
        terminalSlotTransform = slot;
    }
}
