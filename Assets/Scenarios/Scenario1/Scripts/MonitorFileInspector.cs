using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CADT.VR.Cybersecurity
{
    /// <summary>
    /// Lead VR Monitor Interaction: Controls file property toggle actions,
    /// extension spoofing highlights (.pdf.exe), and Telegram chat payload inspection
    /// using XR Ray Interactor pointer clicks on in-world desk canvases.
    /// </summary>
    [DisallowMultipleComponent]
    public class MonitorFileInspector : MonoBehaviour, IPointerClickHandler
    {
        [Header("--- Manager Reference ---")]
        [SerializeField] private VRBranchManager branchManager;

        [Header("--- Monitor 1 (Windows 11 File Inspector) ---")]
        [Tooltip("File properties modal window GameObject")]
        [SerializeField] private GameObject filePropertiesModal;

        [Tooltip("Text label showing full spoofed filename")]
        [SerializeField] private TextMeshProUGUI fileNameText;

        [Tooltip("Text label showing true file type (e.g. Executable Application)")]
        [SerializeField] private TextMeshProUGUI fileTypeText;

        [Tooltip("Visual highlight / warning box around the disguised extension")]
        [SerializeField] private GameObject extensionWarningHighlight;

        [Header("--- Monitor 2 (Telegram Payload Inspector) ---")]
        [Tooltip("Telegram payload inspect details panel")]
        [SerializeField] private GameObject telegramPayloadDetailsModal;

        [Tooltip("Text element displaying threat metadata (e.g., Unknown Sender, Macro Risk)")]
        [SerializeField] private TextMeshProUGUI threatMetadataText;

        [Header("--- Inspection Audio ---")]
        [SerializeField] private AudioSource spatialAudioSource;
        [SerializeField] private AudioClip openPropertiesSound;
        [SerializeField] private AudioClip closePropertiesSound;
        [SerializeField] private AudioClip threatDiscoveredSound;

        [Header("--- Visual Pulsing Settings ---")]
        [SerializeField] private bool autoPulseWarningOnOpen = true;
        [SerializeField] private float pulseFrequency = 4f;

        private bool isPropertiesOpen = false;
        private Coroutine pulseCoroutine;

        private void Awake()
        {
            if (branchManager == null)
            {
                branchManager = Object.FindFirstObjectByType<VRBranchManager>();
            }

            if (spatialAudioSource == null)
            {
                spatialAudioSource = GetComponent<AudioSource>();
            }
        }

        private void Start()
        {
            if (filePropertiesModal != null)
            {
                filePropertiesModal.SetActive(false);
            }

            if (telegramPayloadDetailsModal != null)
            {
                telegramPayloadDetailsModal.SetActive(false);
            }

            if (extensionWarningHighlight != null)
            {
                extensionWarningHighlight.SetActive(false);
            }
        }

        /// <summary>
        /// Handles click from XR Pointer / TrackedDeviceGraphicRaycaster.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            ToggleFileProperties();
        }

        /// <summary>
        /// Toggles the Windows 11 File Properties dialog open/close.
        /// </summary>
        public void ToggleFileProperties()
        {
            isPropertiesOpen = !isPropertiesOpen;

            if (filePropertiesModal != null)
            {
                filePropertiesModal.SetActive(isPropertiesOpen);
            }

            if (isPropertiesOpen)
            {
                PopulatePropertyData();

                if (extensionWarningHighlight != null)
                {
                    extensionWarningHighlight.SetActive(true);
                    if (autoPulseWarningOnOpen)
                    {
                        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                        pulseCoroutine = StartCoroutine(PulseWarningHighlightRoutine(extensionWarningHighlight));
                    }
                }

                if (openPropertiesSound != null && spatialAudioSource != null)
                {
                    spatialAudioSource.PlayOneShot(openPropertiesSound);
                }

                // Notify scenario manager that file has been inspected
                if (branchManager != null)
                {
                    branchManager.RegisterFileInspection();
                }

                if (threatDiscoveredSound != null && spatialAudioSource != null)
                {
                    spatialAudioSource.PlayOneShot(threatDiscoveredSound);
                }
            }
            else
            {
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                    pulseCoroutine = null;
                }

                if (closePropertiesSound != null && spatialAudioSource != null)
                {
                    spatialAudioSource.PlayOneShot(closePropertiesSound);
                }
            }
        }

        /// <summary>
        /// Populates text fields with realistic Windows 11 file property metadata.
        /// </summary>
        private void PopulatePropertyData()
        {
            if (fileNameText != null)
            {
                fileNameText.text = "document_invoice.pdf<color=#EF4444><b>.exe</b></color>";
            }

            if (fileTypeText != null)
            {
                fileTypeText.text = "Type of file: <color=#EF4444><b>Application (.exe)</b></color>\n" +
                                   "<color=#94A3B8>Description: Suspicious Binary Dropper</color>\n" +
                                   "<color=#94A3B8>Digital Signature: <color=#EF4444>Unsigned / Invalid</color></color>";
            }

            if (threatMetadataText != null)
            {
                threatMetadataText.text = "<b>Payload Analysis:</b>\n" +
                                          "• Archive: Staff_Bonus_List_2026.rar\n" +
                                          "• Origin: External Anonymous Telegram Account\n" +
                                          "• Risk Level: <color=#EF4444>CRITICAL (High Confidence Phishing)</color>";
            }
        }

        /// <summary>
        /// Opens or toggles the Telegram payload details modal on Monitor 2.
        /// </summary>
        public void ToggleTelegramPayloadModal()
        {
            if (telegramPayloadDetailsModal != null)
            {
                bool active = !telegramPayloadDetailsModal.activeSelf;
                telegramPayloadDetailsModal.SetActive(active);

                if (active)
                {
                    PopulatePropertyData();
                    if (openPropertiesSound != null && spatialAudioSource != null)
                    {
                        spatialAudioSource.PlayOneShot(openPropertiesSound);
                    }
                    if (branchManager != null)
                    {
                        branchManager.RegisterFileInspection();
                    }
                }
            }
        }

        /// <summary>
        /// Closes all active modal inspection dialogs.
        /// </summary>
        public void CloseAllModals()
        {
            isPropertiesOpen = false;
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            if (filePropertiesModal != null) filePropertiesModal.SetActive(false);
            if (telegramPayloadDetailsModal != null) telegramPayloadDetailsModal.SetActive(false);
            if (extensionWarningHighlight != null) extensionWarningHighlight.SetActive(false);
        }

        private IEnumerator PulseWarningHighlightRoutine(GameObject highlightObj)
        {
            CanvasRenderer renderer = highlightObj.GetComponent<CanvasRenderer>();
            Graphic graphic = highlightObj.GetComponent<Graphic>();
            if (graphic == null) yield break;

            Color baseColor = graphic.color;
            float timer = 0f;

            while (isPropertiesOpen && highlightObj.activeSelf)
            {
                timer += Time.deltaTime * pulseFrequency;
                float alphaMultiplier = 0.5f + Mathf.PingPong(timer, 0.5f);
                Color c = baseColor;
                c.a *= alphaMultiplier;
                graphic.color = c;
                yield return null;
            }

            graphic.color = baseColor;
        }
    }
}
