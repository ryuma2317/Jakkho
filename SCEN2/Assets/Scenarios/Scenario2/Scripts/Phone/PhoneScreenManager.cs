using System.Collections;
using UnityEngine;

namespace Kannko.Phone
{
    /// <summary>
    /// Connects the visual phone screen to the SmartphoneController state machine:
    /// - Attached directly to the physical smartphone display surface.
    /// - Controlled strictly by Physical -> Active/Open -> Closing -> Physical.
    /// - Enables/shows the screen Canvas in Active/Open.
    /// - Smoothly dims/hides the screen Canvas in Closing.
    /// - Completely disables the Canvas in Physical state (zero draw-call overhead when resting).
    /// - Coffee shop environment remains completely visible around the handheld screen.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasGroup))]
    public class PhoneScreenManager : MonoBehaviour
    {
        [Header("State Controller")]
        [SerializeField] private SmartphoneController m_PhoneController;

        [Header("Screen Canvas & View Containers")]
        [SerializeField] private Canvas m_ScreenCanvas;
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private GameObject m_HomeScreenContainer;

        [Header("Render Texture Architecture")]
        [SerializeField] private GameObject m_ScreenDisplayQuad;
        [SerializeField] private Camera m_PhoneUICamera;

        [Header("Fade Settings")]
        [SerializeField] private float m_FadeInDuration = 0.15f;
        [SerializeField] private float m_FadeOutDuration = 0.20f;

        private Coroutine m_FadeCoroutine;

        public bool IsScreenVisible => (m_ScreenDisplayQuad != null ? m_ScreenDisplayQuad.activeInHierarchy : (m_ScreenCanvas != null && m_ScreenCanvas.enabled)) && (m_CanvasGroup == null || m_CanvasGroup.alpha > 0.05f);

        private void Awake()
        {
            InitializeReferences();

            // Initially dark/hidden in physical state
            if (Application.isPlaying)
            {
                SetScreenInstant(false);
            }
        }

        private void OnEnable()
        {
            InitializeReferences();

            if (m_PhoneController != null)
            {
                m_PhoneController.OnStateChanged -= HandleStateChanged;
                m_PhoneController.OnStateChanged += HandleStateChanged;
            }
        }

        private void InitializeReferences()
        {
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = GetComponent<CanvasGroup>();
            }

            if (m_ScreenCanvas == null)
            {
                m_ScreenCanvas = GetComponent<Canvas>();
            }

            if (m_PhoneController == null)
            {
                m_PhoneController = GetComponentInParent<SmartphoneController>();
            }
        }

        private void OnDisable()
        {
            if (m_PhoneController != null)
            {
                m_PhoneController.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(SmartphoneState state)
        {
            switch (state)
            {
                case SmartphoneState.ActiveOpen:
                    ShowScreen();
                    break;

                case SmartphoneState.Closing:
                    HideScreen(smooth: true);
                    break;

                case SmartphoneState.Physical:
                    HideScreen(smooth: false);
                    break;
            }
        }

        public void ShowScreen()
        {
            if (m_ScreenCanvas != null)
            {
                m_ScreenCanvas.enabled = true;
            }

            if (m_ScreenDisplayQuad != null)
            {
                m_ScreenDisplayQuad.SetActive(true);
            }

            if (m_PhoneUICamera != null)
            {
                m_PhoneUICamera.enabled = true;
            }

            if (m_HomeScreenContainer != null)
            {
                m_HomeScreenContainer.SetActive(true);
            }

            if (m_FadeCoroutine != null)
            {
                StopCoroutine(m_FadeCoroutine);
            }

            if (Application.isPlaying && m_FadeInDuration > 0f)
            {
                m_FadeCoroutine = StartCoroutine(FadeRoutine(1f, m_FadeInDuration));
            }
            else
            {
                if (m_CanvasGroup != null) m_CanvasGroup.alpha = 1f;
            }
        }

        public void HideScreen(bool smooth = true)
        {
            if (m_FadeCoroutine != null)
            {
                StopCoroutine(m_FadeCoroutine);
                m_FadeCoroutine = null;
            }

            if (smooth && Application.isPlaying && m_FadeOutDuration > 0f)
            {
                m_FadeCoroutine = StartCoroutine(FadeRoutine(0f, m_FadeOutDuration, disableCanvasOnEnd: true));
            }
            else
            {
                SetScreenInstant(false);
            }
        }

        private void SetScreenInstant(bool visible)
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = visible ? 1f : 0f;
            }

            if (m_ScreenCanvas != null)
            {
                m_ScreenCanvas.enabled = visible;
            }

            if (m_ScreenDisplayQuad != null)
            {
                m_ScreenDisplayQuad.SetActive(visible);
            }

            if (m_PhoneUICamera != null)
            {
                m_PhoneUICamera.enabled = visible;
            }
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration, bool disableCanvasOnEnd = false)
        {
            if (m_CanvasGroup == null) yield break;

            float startAlpha = m_CanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                m_CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            m_CanvasGroup.alpha = targetAlpha;

            if (disableCanvasOnEnd && targetAlpha <= 0.001f)
            {
                if (m_ScreenCanvas != null)
                {
                    m_ScreenCanvas.enabled = false;
                }

                if (m_ScreenDisplayQuad != null)
                {
                    m_ScreenDisplayQuad.SetActive(false);
                }

                if (m_PhoneUICamera != null)
                {
                    m_PhoneUICamera.enabled = false;
                }
            }

            m_FadeCoroutine = null;
        }

        // Configuration helper
        public void Configure(SmartphoneController phoneController, Canvas canvas, CanvasGroup canvasGroup, GameObject homeScreen, GameObject displayQuad = null, Camera phoneCam = null)
        {
            m_PhoneController = phoneController;
            m_ScreenCanvas = canvas;
            m_CanvasGroup = canvasGroup;
            m_HomeScreenContainer = homeScreen;
            m_ScreenDisplayQuad = displayQuad;
            m_PhoneUICamera = phoneCam;
            SetScreenInstant(false);
        }
    }
}
