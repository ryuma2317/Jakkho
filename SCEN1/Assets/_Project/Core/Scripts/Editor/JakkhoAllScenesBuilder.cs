using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CADT.VR.Scenarios.Scenario1.Editor;
using CADT.VR.Scenarios.Scenario2.Editor;
using CADT.VR.Scenarios.Scenario3.Editor;

namespace CADT.VR.Core.Editor
{
    public static class JakkhoAllScenesBuilder
    {
        private static readonly string[] ScenePaths = new string[]
        {
            "Assets/_Project/Core/Scenes/MainMenu.unity",
            "Assets/_Project/Core/Scenes/ScenarioSelection.unity",
            "Assets/Scenarios/Scenario1/Scenes/Scenario1.unity",
            "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity",
            "Assets/Scenarios/Scenario3/Scenes/Scenario3.unity"
        };

        [MenuItem("CADT VR/Scenes/Build All Project Scenes Setup", priority = 5)]
        public static void BuildAllProjectScenes()
        {
            // 1. Build MainMenu.unity
            var mmScene = EditorSceneManager.OpenScene(ScenePaths[0], OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<GameManager>() == null)
            {
                var gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
            }
            var mmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Core/Prefabs/MainMenu_Canvas.prefab");
            if (mmPrefab != null && GameObject.Find("MainMenu_Canvas") == null)
            {
                var inst = PrefabUtility.InstantiatePrefab(mmPrefab) as GameObject;
                inst.name = "MainMenu_Canvas";
            }
            EditorSceneManager.MarkSceneDirty(mmScene);
            EditorSceneManager.SaveScene(mmScene);

            // 2. Build ScenarioSelection.unity
            var ssScene = EditorSceneManager.OpenScene(ScenePaths[1], OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<GameManager>() == null)
            {
                var gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
            }
            var ssPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Core/Prefabs/ScenarioSelection_Canvas.prefab");
            if (ssPrefab != null && GameObject.Find("ScenarioSelection_Canvas") == null)
            {
                var inst = PrefabUtility.InstantiatePrefab(ssPrefab) as GameObject;
                inst.name = "ScenarioSelection_Canvas";
            }
            EditorSceneManager.MarkSceneDirty(ssScene);
            EditorSceneManager.SaveScene(ssScene);

            // 3. Build Scenario 1
            Scenario1Builder.BuildScenario1();

            // 4. Build Scenario 2
            Scenario2Builder.BuildScenario2();

            // 5. Build Scenario 3
            Scenario3Builder.BuildScenario3();

            // 6. Register all scenes in Build Settings
            RegisterAllScenesInBuildSettings();

            // Re-open Scenario1 as active working scene
            EditorSceneManager.OpenScene(ScenePaths[2], OpenSceneMode.Single);

            Debug.Log("<color=#22C55E><b>[Jakkho All Scenes]</b></color> All 5 Project Scenes successfully built, configured, and registered in Build Settings!");
        }

        [MenuItem("CADT VR/Scenes/Register All Scenes in Build Settings", priority = 10)]
        public static void RegisterAllScenesInBuildSettings()
        {
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

            for (int i = 0; i < ScenePaths.Length; i++)
            {
                string path = ScenePaths[i];
                if (System.IO.File.Exists(path))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"<color=#22C55E><b>[Jakkho Scenes]</b></color> Successfully updated Build Settings with {buildScenes.Count} scenes.");
        }
    }
}
