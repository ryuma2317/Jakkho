using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace Kannko.Editor
{
    public static class AttachPanelManager
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

        [MenuItem("VR Prototype/Attach XR Simulator Panel Manager", priority = 55)]
        public static void Attach()
        {
            // Reset EditorPrefs to force collapsed by default
            EditorPrefs.SetBool("XRI.XRDeviceSimulatorUI.MenuOpenState", false);
            Debug.Log("[XR Panel Manager] Set EditorPrefs menu state to collapsed (false).");

            if (!File.Exists(ScenePath)) return;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject simGo = GameObject.Find("XR Device Simulator");
            if (simGo != null)
            {
                // Remove old toggle script if present
                var oldScript = simGo.GetComponent<XRSimulatorTabToggle>();
                if (oldScript != null) Object.DestroyImmediate(oldScript);

                if (simGo.GetComponent<XRSimulatorPanelManager>() == null)
                {
                    simGo.AddComponent<XRSimulatorPanelManager>();
                    Debug.Log("[XR Panel Manager] Added XRSimulatorPanelManager component to XR Device Simulator.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
