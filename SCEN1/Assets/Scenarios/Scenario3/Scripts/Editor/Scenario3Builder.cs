using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CADT.VR.Core;

namespace CADT.VR.Scenarios.Scenario3.Editor
{
    public static class Scenario3Builder
    {
        [MenuItem("CADT VR/Scenarios/Build Scenario 3 Setup", priority = 33)]
        public static void BuildScenario3()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenarios/Scenario3/Scenes/Scenario3.unity", OpenSceneMode.Single);

            var mgrGo = GameObject.Find("Scenario3_Manager");
            if (mgrGo == null)
            {
                mgrGo = new GameObject("Scenario3_Manager");
            }

            var mgr = mgrGo.GetComponent<Scenario3_FakeLinkManager>();
            if (mgr == null)
            {
                mgr = mgrGo.AddComponent<Scenario3_FakeLinkManager>();
            }

            var config = AssetDatabase.LoadAssetAtPath<ScenarioDataSO>("Assets/Scenarios/Scenario3/Data/Scenario3_Config.asset");
            if (config != null)
            {
                var so = new SerializedObject(mgr);
                so.FindProperty("scenarioData").objectReferenceValue = config;
                so.ApplyModifiedProperties();
            }

            var canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenarios/Scenario3/Prefabs/Scenario3_Canvas.prefab");
            if (canvasPrefab != null && GameObject.Find("Scenario3_Canvas") == null)
            {
                var inst = PrefabUtility.InstantiatePrefab(canvasPrefab) as GameObject;
                inst.name = "Scenario3_Canvas";
            }

            var dogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenarios/Scenario3/Prefabs/BarkingDog_Level.prefab");
            if (dogPrefab != null && GameObject.Find("BarkingDog_Level") == null)
            {
                var inst = PrefabUtility.InstantiatePrefab(dogPrefab) as GameObject;
                inst.name = "BarkingDog_Level";
            }

            if (Object.FindFirstObjectByType<GameManager>() == null)
            {
                var gmGo = new GameObject("GameManager");
                gmGo.AddComponent<GameManager>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#22C55E><b>[Scenario 3]</b></color> Successfully built Scenario 3 Reference scene setup!");
        }
    }
}
