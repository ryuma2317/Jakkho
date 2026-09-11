using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CADT.VR.Scenarios.Scenario1.Editor
{
    public static class Scenario1Builder
    {
        [MenuItem("CADT VR/Scenarios/Build Scenario 1 Setup", priority = 31)]
        public static void BuildScenario1()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenarios/Scenario1/Scenes/Scenario1.unity", OpenSceneMode.Single);

            var mgrGo = GameObject.Find("Scenario1_Manager");
            if (mgrGo == null)
            {
                mgrGo = new GameObject("Scenario1_Manager");
            }

            var mgr = mgrGo.GetComponent<Scenario1_SpearPhishingManager>();
            if (mgr == null)
            {
                mgr = mgrGo.AddComponent<Scenario1_SpearPhishingManager>();
            }

            var config = AssetDatabase.LoadAssetAtPath<ScenarioDataSO>("Assets/Scenarios/Scenario1/Data/Scenario1_Config.asset");
            if (config != null)
            {
                var so = new SerializedObject(mgr);
                so.FindProperty("scenarioData").objectReferenceValue = config;

                var monitor = GameObject.Find("Monitor_Screen_Canvas");
                if (monitor != null)
                {
                    so.FindProperty("workstationMonitor").objectReferenceValue = monitor;
                }
                so.ApplyModifiedProperties();
            }

            if (Object.FindFirstObjectByType<GameManager>() == null)
            {
                var gmGo = new GameObject("GameManager");
                gmGo.AddComponent<GameManager>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#22C55E><b>[Scenario 1]</b></color> Successfully built Scenario 1 scene setup!");
        }
    }
}
