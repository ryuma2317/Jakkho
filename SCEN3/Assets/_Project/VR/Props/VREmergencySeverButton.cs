using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Physical Emergency Sever Push Button for VR.
/// Listens to XRPoke / XRSimpleInteractable selectEntered events and immediately
/// triggers the emergency session disconnect in Scenario3_FakeLinkManager.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class VREmergencySeverButton : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private MeshRenderer buttonRenderer;
    [SerializeField] private Material activatedMaterial;

    [Header("Audio Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clunkSound;

    private XRSimpleInteractable interactable;
    private bool hasBeenPressed = false;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnButtonPressed);
        }
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        if (hasBeenPressed) return;
        hasBeenPressed = true;

        Debug.Log("[VREmergencySeverButton] Physical emergency button slammed down!");

        if (audioSource != null && clunkSound != null)
        {
            audioSource.PlayOneShot(clunkSound);
        }

        if (buttonRenderer != null && activatedMaterial != null)
        {
            buttonRenderer.material = activatedMaterial;
        }

        if (VRInteractionHandler.Instance != null)
        {
            VRInteractionHandler.Instance.TriggerBothHandsHaptic(0.9f, 0.25f);
            VRInteractionHandler.Instance.PlayAlertSound();
        }

        var mgr = FindObjectOfType<Scenario3_FakeLinkManager>();
        if (mgr != null)
        {
            mgr.OnRefuseQRAction();
        }
    }

    public void ResetButton()
    {
        hasBeenPressed = false;
    }
}
