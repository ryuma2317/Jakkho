using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace Kannko.Editor
{
    public static class InspectSimulatorUIPrefab
    {
        private const string PrefabPath = "Assets/Samples/XR Interaction Toolkit/3.0.7/XR Device Simulator/UI/XR Device Simulator UI.prefab";

        [MenuItem("VR Prototype/Inspect XR Simulator UI Prefab", priority = 45)]
        public static void Inspect()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[UI Prefab Check] Prefab not found at {PrefabPath}");
                return;
            }

            Debug.Log($"[UI Prefab Check] Loaded {prefab.name}");
            Transform mainPanel = prefab.transform.Find("XR Device Simulator Main Panel");
            Transform collapsedPanel = prefab.transform.Find("XR Device Simulator Collapsed Panel");

            Debug.Log($"[UI Prefab Check] Main Panel present: {mainPanel != null}, Collapsed Panel present: {collapsedPanel != null}");

            Button[] buttons = prefab.GetComponentsInChildren<Button>(true);
            Debug.Log($"[UI Prefab Check] Found {buttons.Length} buttons on prefab.");
            foreach (var b in buttons)
            {
                Debug.Log($"[UI Prefab Check] Button: '{b.gameObject.name}', parent: '{b.transform.parent?.name}'");
            }
        }
    }
}
