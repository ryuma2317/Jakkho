using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.Events;

public class VRSplashScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup splashCanvasGroup;
    [SerializeField] private CanvasGroup logoCanvasGroup;
    [SerializeField] private RectTransform logoRectTransform;
    [SerializeField] private Image logoImage;
    [SerializeField] private Image backgroundImage;

    [Header("Timing Settings")]
    [Tooltip("Time to fade & animate in the logo (in seconds)")]
    [SerializeField] private float fadeInDuration = 1.0f;

    [Tooltip("Duration the logo stays on screen (in seconds)")]
    [SerializeField] private float displayDuration = 3.0f;

    [Tooltip("Time to fade out from the splash screen to the scene (in seconds)")]
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Tooltip("Require headset proximity sensor / presence before starting the sequence")]
    [SerializeField] private bool waitForHeadsetPresence = true;

    [Header("Animation Settings")]
    [Tooltip("Start scale when fading in (e.g. 0.8 for smooth zoom-in)")]
    [SerializeField] private float startScale = 0.8f;

    [Tooltip("Target resting scale")]
    [SerializeField] private float targetScale = 1.0f;

    [Tooltip("Exit scale during fade out (e.g. 1.08 for subtle expansion)")]
    [SerializeField] private float exitScale = 1.08f;

    [Tooltip("Enable subtle floating / breathing animation during the 3s hold")]
    [SerializeField] private bool enableBreathingMotion = true;
    [SerializeField] private float breathSpeed = 2.0f;
    [SerializeField] private float breathAmount = 0.025f;

    [Header("Events")]
    public UnityEvent onSplashStarted;
    public UnityEvent onSplashFinished;

    private void Awake()
    {
        // Ensure everything starts solid black and logo scaled down
        if (splashCanvasGroup != null)
        {
            splashCanvasGroup.alpha = 1f;
        }

        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.alpha = 0f;
        }

        if (logoRectTransform != null)
        {
            logoRectTransform.localScale = Vector3.one * startScale;
        }
    }

    private void Start()
    {
        StartCoroutine(SplashScreenRoutine());
    }

    private IEnumerator SplashScreenRoutine()
    {
        onSplashStarted?.Invoke();

        // 1. Wait for user to put on Meta Quest headset
        if (waitForHeadsetPresence && !Application.isEditor)
        {
            while (!CheckUserPresence())
            {
                yield return null;
            }
        }

        // Small stabilization delay
        yield return new WaitForSeconds(0.2f);

        // 2. Animate and Fade IN the logo (Smooth Ease-Out with scale-up from startScale to targetScale)
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            
            // Cubic Ease-Out curve for sleek, punchy entrance
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);
            
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = t;
            }

            if (logoRectTransform != null)
            {
                float currentScale = Mathf.Lerp(startScale, targetScale, easeOut);
                logoRectTransform.localScale = Vector3.one * currentScale;
            }

            yield return null;
        }

        if (logoCanvasGroup != null) logoCanvasGroup.alpha = 1f;
        if (logoRectTransform != null) logoRectTransform.localScale = Vector3.one * targetScale;

        // 3. Display logo with gentle breathing/floating motion during the hold duration
        float holdElapsed = 0f;
        while (holdElapsed < displayDuration)
        {
            holdElapsed += Time.deltaTime;

            if (enableBreathingMotion && logoRectTransform != null)
            {
                float breath = Mathf.Sin(holdElapsed * breathSpeed) * breathAmount;
                logoRectTransform.localScale = Vector3.one * (targetScale + breath);
            }

            yield return null;
        }

        // 4. Animate and Fade OUT entire splash screen (Expands smoothly as it fades out into scene)
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            
            // Smooth step for exit
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (splashCanvasGroup != null)
            {
                splashCanvasGroup.alpha = 1f - smoothT;
            }

            if (logoRectTransform != null)
            {
                float currentScale = Mathf.Lerp(targetScale, exitScale, smoothT);
                logoRectTransform.localScale = Vector3.one * currentScale;
            }

            yield return null;
        }

        if (splashCanvasGroup != null) splashCanvasGroup.alpha = 0f;

        // 5. Completion
        onSplashFinished?.Invoke();
        gameObject.SetActive(false);
    }

    private bool CheckUserPresence()
    {
        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (headDevice.isValid && headDevice.TryGetFeatureValue(CommonUsages.userPresence, out bool isPresent))
        {
            return isPresent;
        }

        // Fallback if userPresence characteristic is not supported by current runtime
        return headDevice.isValid;
    }
}
