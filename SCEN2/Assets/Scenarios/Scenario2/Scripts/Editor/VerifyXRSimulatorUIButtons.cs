using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace Kannko.Editor
{
    public static class VerifyXRSimulatorUIButtons
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

        [MenuItem("VR Prototype/Verify XR Simulator UI Buttons", priority = 40)]
        public static void Verify()
        {
            if (!File.Exists(ScenePath)) return;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject simGo = GameObject.Find("XR Device Simulator");
            if (simGo == null)
            {
                Debug.LogError("[Simulator UI Check] XR Device Simulator not found in scene!");
                return;
            }

            Canvas canvas = simGo.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("[Simulator UI Fix] Added GraphicRaycaster to XR Device Simulator Canvas.");
                }
            }

            Button[] buttons = simGo.GetComponentsInChildren<Button>(true);
            Debug.Log($"[Simulator UI Check] Found {buttons.Length} UI buttons on XR Device Simulator.");
            foreach (var btn in buttons)
            {
                Debug.Log($"[Simulator UI Check] Button '{btn.gameObject.name}', parent '{btn.transform.parent?.name}', OnClick persistent calls: {btn.onClick.GetPersistentEventCount()}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
