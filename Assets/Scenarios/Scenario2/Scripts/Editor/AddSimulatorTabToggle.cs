using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kannko.Editor
{
    public static class AddSimulatorTabToggle
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

        [MenuItem("VR Prototype/Attach XR Simulator Tab Toggle", priority = 30)]
        public static void Attach()
        {
            if (!System.IO.File.Exists(ScenePath)) return;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject simGo = GameObject.Find("XR Device Simulator");
            if (simGo != null)
            {
                if (simGo.GetComponent<XRSimulatorTabToggle>() == null)
                {
                    simGo.AddComponent<XRSimulatorTabToggle>();
                    Debug.Log("[XR Simulator Tab Toggle] Component added to XR Device Simulator.");
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }
    }
}
