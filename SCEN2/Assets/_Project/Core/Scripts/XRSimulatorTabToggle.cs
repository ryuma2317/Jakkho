using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kannko
{
    /// <summary>
    /// Adds Tab key shortcut to easily open and close the XR Simulator UI tab in the top-left corner during Play Mode.
    /// </summary>
    public class XRSimulatorTabToggle : MonoBehaviour
    {
        [Header("Key Shortcut to Open/Close XR Simulator Tab")]
        [Tooltip("Pressing this key toggles the XR Device Simulator UI panel open and closed.")]
        public Key toggleKey = Key.Tab;

        private MonoBehaviour simulatorUIComponent;
        private MethodInfo openMethod;
        private MethodInfo closeMethod;
        private bool isOpen = true;

        private void Start()
        {
            FindSimulatorUI();
        }

        private void FindSimulatorUI()
        {
            MonoBehaviour[] allScripts = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (MonoBehaviour script in allScripts)
            {
                if (script != null && script.GetType().Name == "XRDeviceSimulatorUI")
                {
                    simulatorUIComponent = script;
                    Type type = script.GetType();
                    openMethod = type.GetMethod("OnClickOpenSimulatorUIPanel", BindingFlags.Public | BindingFlags.Instance);
                    closeMethod = type.GetMethod("OnClickCloseSimulatorUIPanel", BindingFlags.Public | BindingFlags.Instance);
                    break;
                }
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                ToggleTab();
            }
        }

        public void ToggleTab()
        {
            if (simulatorUIComponent == null)
            {
                FindSimulatorUI();
            }

            if (simulatorUIComponent != null)
            {
                isOpen = !isOpen;
                if (isOpen)
                {
                    openMethod?.Invoke(simulatorUIComponent, null);
                }
                else
                {
                    closeMethod?.Invoke(simulatorUIComponent, null);
                }
            }
        }
    }
}
