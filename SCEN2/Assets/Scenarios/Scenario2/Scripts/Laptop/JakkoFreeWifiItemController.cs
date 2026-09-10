using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kannko.Laptop
{
    /// <summary>
    /// Dedicated interaction controller for the Item_Jakko-Free object in NetworkList.
    /// When the player clicks/selects Item_Jakko-Free via VR controller ray or mouse,
    /// this script intercepts the interaction and opens the full-screen Jakko Coffee Free Wi-Fi
    /// browser panel on the laptop display.
    /// 
    /// Keeps the Item_Jakko-Free behavior completely isolated from other NetworkList items.
    /// </summary>
    [DisallowMultipleComponent]
    public class JakkoFreeWifiItemController : MonoBehaviour
    {
        [Header("Browser Controller Reference")]
        [Tooltip("The browser controller managing the full-screen captive portal on the laptop screen.")]
        [SerializeField] private JakkoFreeWifiBrowserController m_BrowserController;

        [Header("Events")]
        public UnityEvent OnJakkoFreeSelected = new UnityEvent();

        private Button m_Button;
        private XRSimpleInteractable m_Interactable;
        private BoxCollider m_Collider;

        public JakkoFreeWifiBrowserController BrowserController
        {
            get => m_BrowserController;
            set => m_BrowserController = value;
        }

        private void Awake()
        {
            FindReferencesIfNull();
            SetupInteractionHooks();
        }

        private void OnEnable()
        {
            FindReferencesIfNull();
            SetupInteractionHooks();
        }

        private void Start()
        {
            FindReferencesIfNull();
            SetupInteractionHooks();
        }

        public void FindReferencesIfNull()
        {
            if (m_BrowserController == null)
            {
                m_BrowserController = Object.FindFirstObjectByType<JakkoFreeWifiBrowserController>(FindObjectsInactive.Include);
            }

            if (m_Button == null)
            {
                m_Button = GetComponent<Button>();
            }

            if (m_Interactable == null)
            {
                m_Interactable = GetComponent<XRSimpleInteractable>();
            }

            if (m_Collider == null)
            {
                m_Collider = GetComponent<BoxCollider>();
            }
        }

        public void SetupInteractionHooks()
        {
            // 1. Hook into UI Button onClick
            if (m_Button == null) m_Button = GetComponent<Button>();
            if (m_Button != null)
            {
                m_Button.onClick.RemoveListener(TriggerInteraction);
                m_Button.onClick.AddListener(TriggerInteraction);
            }

            // 2. Ensure BoxCollider exists with accurate RectTransform dimensions
            if (m_Collider == null) m_Collider = GetComponent<BoxCollider>();
            if (m_Collider == null) m_Collider = gameObject.AddComponent<BoxCollider>();
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                m_Collider.center = new Vector3(rt.rect.center.x, rt.rect.center.y, 0f);
                m_Collider.size = new Vector3(rt.rect.width, rt.rect.height, 50f);
            }
            m_Collider.enabled = true;

            // 3. Hook into XRSimpleInteractable
            if (m_Interactable == null) m_Interactable = GetComponent<XRSimpleInteractable>();
            if (m_Interactable == null) m_Interactable = gameObject.AddComponent<XRSimpleInteractable>();

            m_Interactable.selectEntered.RemoveListener(OnXRSelectEntered);
            m_Interactable.selectEntered.AddListener(OnXRSelectEntered);

            m_Interactable.activated.RemoveListener(OnXRActivated);
            m_Interactable.activated.AddListener(OnXRActivated);
        }

        private void OnXRSelectEntered(UnityEngine.XR.Interaction.Toolkit.BaseInteractionEventArgs args)
        {
            TriggerInteraction();
        }

        private void OnXRActivated(UnityEngine.XR.Interaction.Toolkit.BaseInteractionEventArgs args)
        {
            TriggerInteraction();
        }

        /// <summary>
        /// Main interaction entry point when Item_Jakko-Free is clicked or selected.
        /// Opens the full-screen Jakko Coffee Free Wi-Fi browser panel.
        /// </summary>
        public void TriggerInteraction()
        {
            if (m_BrowserController == null)
            {
                m_BrowserController = Object.FindFirstObjectByType<JakkoFreeWifiBrowserController>(FindObjectsInactive.Include);
            }

            if (m_BrowserController != null)
            {
                Debug.Log("[JakkoFreeWifiItemController] Item_Jakko-Free selected! Opening Jakko Coffee Free Wi-Fi browser.");
                m_BrowserController.OpenBrowser();
                OnJakkoFreeSelected?.Invoke();
            }
            else
            {
                Debug.LogWarning("[JakkoFreeWifiItemController] Cannot open browser: JakkoFreeWifiBrowserController not found!");
            }
        }
    }
}
