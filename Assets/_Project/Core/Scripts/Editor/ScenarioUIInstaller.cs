#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class ScenarioUIInstaller
{
    private static readonly string[] Scenes = new string[]
    {
        "Assets/Scenarios/Scenario1/Scenes/Scenario1.unity",
        "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity",
        "Assets/Scenarios/Scenario3/Scenes/Scenario3.unity"
    };

    private const string FontPath = "Assets/Fonts/Inter/Inter-Bold SDF.asset";

    [MenuItem("Jakkho/Install Scenario UI (Exit + End Panels)", false, 50)]
    public static void Install()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) Debug.LogWarning("[ScenarioUIInstaller] Inter-Bold SDF not found at " + FontPath);

        int totalFonts = 0;
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            var go = GameObject.Find("[ScenarioUI]");
            if (go == null) go = new GameObject("[ScenarioUI]");
            var overlay = go.GetComponent<ScenarioUIOverlay>();
            if (overlay == null) overlay = go.AddComponent<ScenarioUIOverlay>();
            overlay.font = font;
            EditorUtility.SetDirty(overlay);

            int changed = 0;

            totalFonts += changed;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ScenarioUIInstaller] " + scene.name + ": overlay installed, fonts switched=" + changed);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[ScenarioUIInstaller] Done. Total fonts switched: " + totalFonts);
    }
}
#endif
