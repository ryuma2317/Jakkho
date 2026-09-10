using UnityEngine;
using UnityEngine.UI;

namespace Kannko.Phone
{
    /// <summary>
    /// Represents a modular app icon placeholder on the phone home screen.
    /// Ready to launch future app modules without modifying the physical phone interaction.
    /// </summary>
    public class PhoneAppSlot : MonoBehaviour
    {
        [Header("App Identity")]
        [SerializeField] private string m_AppId;
        [SerializeField] private string m_AppTitle;

        [Header("UI References")]
        [SerializeField] private Image m_IconImage;
        [SerializeField] private Text m_LabelText;

        public string AppId => m_AppId;
        public string AppTitle => m_AppTitle;

        public void Configure(string appId, string appTitle, Color iconColor)
        {
            m_AppId = appId;
            m_AppTitle = appTitle;

            if (m_LabelText != null)
            {
                m_LabelText.text = appTitle;
            }

            if (m_IconImage != null)
            {
                m_IconImage.color = iconColor;
            }
        }
    }
}
