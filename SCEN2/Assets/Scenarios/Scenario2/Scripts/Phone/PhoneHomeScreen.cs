using System;
using UnityEngine;
using UnityEngine.UI;

namespace Kannko.Phone
{
    /// <summary>
    /// Controls the modern Gen-Z smartphone home screen presentation:
    /// - Status bar clock & battery
    /// - Large aesthetic digital clock & weather widget
    /// - App grid with clean spacing and high contrast
    /// - Bottom dock with quick access apps
    /// - Bottom home swipe indicator bar
    /// </summary>
    public class PhoneHomeScreen : MonoBehaviour
    {
        [Header("Status Bar")]
        [SerializeField] private Text m_StatusClockText;
        [SerializeField] private Text m_StatusBatteryText;

        [Header("Widget Area")]
        [SerializeField] private Text m_WidgetClockText;
        [SerializeField] private Text m_WidgetDateText;

        [Header("App Slots")]
        [SerializeField] private PhoneAppSlot[] m_AppSlots;

        private void Update()
        {
            UpdateClock();
        }

        private void OnEnable()
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            string timeStr = DateTime.Now.ToString("HH:mm");

            if (m_StatusClockText != null)
            {
                m_StatusClockText.text = timeStr;
            }

            if (m_WidgetClockText != null)
            {
                m_WidgetClockText.text = timeStr;
            }

            if (m_WidgetDateText != null)
            {
                m_WidgetDateText.text = DateTime.Now.ToString("dddd, MMM d") + " • 24°C";
            }

            if (m_StatusBatteryText != null && string.IsNullOrEmpty(m_StatusBatteryText.text))
            {
                m_StatusBatteryText.text = "88%";
            }
        }
    }
}
