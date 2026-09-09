using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRInteractionHandler : MonoBehaviour
{
    public static VRInteractionHandler Instance { get; private set; }

    [Header("Audio Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip alertSound;
    [SerializeField] private AudioClip successSound;

    [Header("Haptics Settings")]
    [SerializeField] private float defaultHapticAmplitude = 0.5f;
    [SerializeField] private float defaultHapticDuration = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void TriggerHaptic(XRNode node, float amplitude = -1f, float duration = -1f)
    {
        if (amplitude < 0) amplitude = defaultHapticAmplitude;
        if (duration < 0) duration = defaultHapticDuration;

        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid)
        {
            HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }

    public void TriggerBothHandsHaptic(float amplitude = 0.6f, float duration = 0.2f)
    {
        TriggerHaptic(XRNode.LeftHand, amplitude, duration);
        TriggerHaptic(XRNode.RightHand, amplitude, duration);
    }

    public void PlayHoverSound()
    {
        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound, 0.4f);
        }
    }

    public void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound, 0.8f);
        }
        TriggerHaptic(XRNode.RightHand, 0.4f, 0.08f);
    }

    public void PlayAlertSound()
    {
        if (audioSource != null && alertSound != null)
        {
            audioSource.PlayOneShot(alertSound, 1.0f);
        }
        TriggerBothHandsHaptic(0.8f, 0.25f);
    }

    public void PlaySuccessSound()
    {
        if (audioSource != null && successSound != null)
        {
            audioSource.PlayOneShot(successSound, 1.0f);
        }
        TriggerBothHandsHaptic(0.5f, 0.2f);
    }
}
