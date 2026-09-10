using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kannko
{
    /// <summary>
    /// Ensures the XR Device Simulator panel starts COLLAPSED by default in Play Mode,
    /// giving the player a 100% clean view of the coffee shop scene.
    /// Provides guaranteed toggle on 'Tab' / 'F1' key press and fixes the (-) close button.
    /// </summary>
    public class XRSimulatorPanelManager : MonoBehaviour
    {
        [Header("Simulator UI Options")]
        [Tooltip("If true, the large simulator panel will start collapsed into a small tab in the top-left corner so you can view the scene fully.")]
        public bool startCollapsed = true;

        [Tooltip("Key shortcut to toggle the simulator panel open/closed.")]
        public Key toggleKey = Key.Tab;

        private MonoBehaviour simulatorUI;
        private MethodInfo openMethod;
        private MethodInfo closeMethod;
        private bool isPanelOpen = false;

        private void Start()
        {
            InitializeUI();

            if (startCollapsed)
            {
                CollapsePanel();
            }
        }

        private void InitializeUI()
        {
            MonoBehaviour[] allScripts = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (MonoBehaviour script in allScripts)
            {
                if (script != null && script.GetType().Name == "XRDeviceSimulatorUI")
                {
                    simulatorUI = script;
                    Type type = script.GetType();
                    openMethod = type.GetMethod("OnClickOpenSimulatorUIPanel", BindingFlags.Public | BindingFlags.Instance);
                    closeMethod = type.GetMethod("OnClickCloseSimulatorUIPanel", BindingFlags.Public | BindingFlags.Instance);

                    // Bind button click handlers explicitly if present on runtime UI canvas
                    Button[] buttons = script.GetComponentsInChildren<Button>(true);
                    foreach (Button b in buttons)
                    {
                        if (b.name == "CloseUIBtn" || b.name == "CloseButton")
                        {
                            b.onClick.RemoveAllListeners();
                            b.onClick.AddListener(CollapsePanel);
                        }
                        else if (b.name == "EnableDisableDeviceSimUIBtn")
                        {
                            b.onClick.RemoveAllListeners();
                            b.onClick.AddListener(ExpandPanel);
                        }
                    }
                    break;
                }
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                TogglePanel();
            }
        }

        public void CollapsePanel()
        {
            if (simulatorUI == null) InitializeUI();
            if (simulatorUI != null && closeMethod != null)
            {
                closeMethod.Invoke(simulatorUI, null);
                isPanelOpen = false;
            }
        }

        public void ExpandPanel()
        {
            if (simulatorUI == null) InitializeUI();
            if (simulatorUI != null && openMethod != null)
            {
                openMethod.Invoke(simulatorUI, null);
                isPanelOpen = true;
            }
        }

        public void TogglePanel()
        {
            if (isPanelOpen)
            {
                CollapsePanel();
            }
            else
            {
                ExpandPanel();
            }
        }
    }
}
