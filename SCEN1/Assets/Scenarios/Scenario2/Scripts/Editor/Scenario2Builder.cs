using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CADT.VR.Core;

namespace CADT.VR.Scenarios.Scenario2.Editor
{
    public static class Scenario2Builder
    {
        [MenuItem("CADT VR/Scenarios/Build Scenario 2 Setup", priority = 32)]
        public static void BuildScenario2()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenarios/Scenario2/Scenes/Scenario2.unity", OpenSceneMode.Single);

            var mgrGo = GameObject.Find("Scenario2_Manager");
            if (mgrGo == null)
            {
                mgrGo = new GameObject("Scenario2_Manager");
            }

            var mgr = mgrGo.GetComponent<Scenario2_EvilTwinManager>();
            if (mgr == null)
            {
                mgr = mgrGo.AddComponent<Scenario2_EvilTwinManager>();
            }

            var config = AssetDatabase.LoadAssetAtPath<ScenarioDataSO>("Assets/Scenarios/Scenario2/Data/Scenario2_Config.asset");
            if (config != null)
            {
                var so = new SerializedObject(mgr);
                so.FindProperty("scenarioData").objectReferenceValue = config;
                so.ApplyModifiedProperties();
            }

            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenarios/Scenario2/Prefabs/Cafe_Table_Setup.prefab");
            if (tablePrefab != null && GameObject.Find("Cafe_Table_Setup") == null)
            {
                var inst = PrefabUtility.InstantiatePrefab(tablePrefab) as GameObject;
                inst.name = "Cafe_Table_Setup";
            }

            var portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenarios/Scenario2/Prefabs/Captive_Portal_Canvas.prefab");
            if (portalPrefab != null && GameObject.Find("Captive_Portal_Canvas") == null)
            {
                var inst = PrefabUtility.InstantiatePrefab(portalPrefab) as GameObject;
                inst.name = "Captive_Portal_Canvas";
            }

            if (Object.FindFirstObjectByType<GameManager>() == null)
            {
                var gmGo = new GameObject("GameManager");
                gmGo.AddComponent<GameManager>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#22C55E><b>[Scenario 2]</b></color> Successfully built Scenario 2 scene setup!");
        }
    }
}
