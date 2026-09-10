using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kannko.Phone
{
    /// <summary>
    /// Controls the blinking light/color effect on the TelegramMessagePopup.
    /// When active, smoothly alternates the popup's background graphics between their normal color
    /// and a brighter highlight color to simulate an alert notification indicator.
    /// On deactivation, cleanly restores the normal color state.
    /// </summary>
    [DisallowMultipleComponent]
    public class TelegramPopupBlinker : MonoBehaviour
    {
        [System.Serializable]
        public struct ImageColorEntry
        {
            public Image image;
            public Color normalColor;
            public Color highlightColor;

            public ImageColorEntry(Image img, Color normal, Color highlight)
            {
                image = img;
                normalColor = normal;
                highlightColor = highlight;
            }
        }

        [Header("Blink Effect Settings")]
        [Tooltip("Frequency in Hz (cycles per second). 1.2 Hz gives a calm, clean breathing pulse.")]
        [SerializeField] private float m_BlinkFrequency = 1.25f;

        [Tooltip("Brightness increase applied to normal color to produce the highlight color (0 = none, 1 = max bright).")]
        [Range(0f, 1f)]
        [SerializeField] private float m_BrightnessBoost = 0.55f;

        [Tooltip("Subtle alert tint blended into the highlight color (light cool cyan alert).")]
        [SerializeField] private Color m_AlertTint = new Color(0.85f, 0.94f, 1.0f, 1.0f);

        [Tooltip("How strongly the alert tint affects the highlight color.")]
        [Range(0f, 1f)]
        [SerializeField] private float m_TintWeight = 0.40f;

        [Header("Optional Light Component")]
        [Tooltip("Optional Unity Light component attached to the popup for 3D illumination.")]
        [SerializeField] private Light m_PopupLight;
        [SerializeField] private float m_LightMinIntensity = 0.0f;
        [SerializeField] private float m_LightMaxIntensity = 0.8f;

        [Header("Tracked Visuals (Auto-populated if empty)")]
        [SerializeField] private List<ImageColorEntry> m_TrackedImages = new List<ImageColorEntry>();

        private bool m_IsBlinking = true;
        private bool m_IsInitialized = false;

        public bool IsBlinking => m_IsBlinking;

        private void Awake()
        {
            InitializeTrackedGraphics();
        }

        private void OnEnable()
        {
            // Safeguard: Once the message is marked READ, the popup must never reappear.
            var phone = GetComponentInParent<SmartphoneController>();
            if (phone == null)
            {
                phone = Object.FindFirstObjectByType<SmartphoneController>();
            }

            if (phone != null && phone.IsMessageRead)
            {
                gameObject.SetActive(false);
                return;
            }

            if (!m_IsInitialized)
            {
                InitializeTrackedGraphics();
            }

            m_IsBlinking = true;
        }

        private void OnDisable()
        {
            ResetToNormal();
        }

        /// <summary>
        /// Collects existing UI Image components and computes their normal and brighter highlight colors.
        /// Ignores AppIcon to preserve icon fidelity.
        /// </summary>
        public void InitializeTrackedGraphics()
        {
            m_TrackedImages.Clear();

            // Auto-detect optional Light component
            if (m_PopupLight == null)
            {
                m_PopupLight = GetComponentInChildren<Light>(true);
            }

            var allImages = GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                if (img == null) continue;

                // Do not blink app icon or buttons directly to keep icon sharp
                if (img.gameObject.name == "AppIcon" || img.gameObject.name == "Icon")
                {
                    continue;
                }

                Color normal = img.color;

                // Compute brighter highlight color (lerp towards pure white for clean brightness boost)
                Color highlight = Color.Lerp(normal, Color.white, m_BrightnessBoost);

                // Blend with subtle cool alert tint while guaranteeing it never darkens any channel
                Color alertTone = new Color(
                    Mathf.Max(normal.r, m_AlertTint.r),
                    Mathf.Max(normal.g, m_AlertTint.g),
                    Mathf.Max(normal.b, m_AlertTint.b),
                    normal.a
                );
                highlight = Color.Lerp(highlight, alertTone, m_TintWeight);

                // Final safety: ensure every channel is >= normal color
                highlight.r = Mathf.Max(normal.r, highlight.r);
                highlight.g = Mathf.Max(normal.g, highlight.g);
                highlight.b = Mathf.Max(normal.b, highlight.b);
                highlight.a = normal.a; // preserve original alpha

                m_TrackedImages.Add(new ImageColorEntry(img, normal, highlight));
            }

            m_IsInitialized = true;
        }

        private void Update()
        {
            if (!m_IsBlinking || m_TrackedImages.Count == 0) return;

            // Calculate smooth breathing wave (starts at 0 when time = 0)
            float rawSine = Mathf.Sin(Time.time * m_BlinkFrequency * Mathf.PI * 2f - Mathf.PI * 0.5f);
            float t = (rawSine + 1f) * 0.5f;

            // Smooth easing for subtle, clean transition
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Modulate tracked UI image colors
            for (int i = 0; i < m_TrackedImages.Count; i++)
            {
                var entry = m_TrackedImages[i];
                if (entry.image != null)
                {
                    entry.image.color = Color.Lerp(entry.normalColor, entry.highlightColor, smoothT);
                }
            }

            // Modulate optional 3D Light component if present
            if (m_PopupLight != null)
            {
                m_PopupLight.intensity = Mathf.Lerp(m_LightMinIntensity, m_LightMaxIntensity, smoothT);
            }
        }

        /// <summary>
        /// Resets all tracked images immediately to their normal baseline colors.
        /// </summary>
        public void ResetToNormal()
        {
            for (int i = 0; i < m_TrackedImages.Count; i++)
            {
                var entry = m_TrackedImages[i];
                if (entry.image != null)
                {
                    entry.image.color = entry.normalColor;
                }
            }

            if (m_PopupLight != null)
            {
                m_PopupLight.intensity = m_LightMinIntensity;
            }
        }

        /// <summary>
        /// Explicitly starts or resumes blinking.
        /// </summary>
        public void StartBlinking()
        {
            m_IsBlinking = true;
        }

        /// <summary>
        /// Stops blinking and restores the popup to its normal color.
        /// </summary>
        public void StopBlinking()
        {
            m_IsBlinking = false;
            ResetToNormal();
        }
    }
}
