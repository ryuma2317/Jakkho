using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class SceneRestorer
{
    static SceneRestorer()
    {
        EditorApplication.delayCall += RestoreSceneIfNeeded;
    }

    [MenuItem("Tools/Restore 3D VR Scene File")]
    public static void RestoreScene()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SceneRestorer] Cannot restore scene during Play Mode.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        string scenePath = "Assets/Scenes/SampleScene.unity";
        Scene activeScene = EditorSceneManager.GetActiveScene();

        if (string.IsNullOrEmpty(activeScene.path) || activeScene.path != scenePath)
        {
            activeScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        // Build 3D Sci-Fi Command Room & VR Menu
        SciFiCommandRoomBuilder.BuildCommandRoom();

        // Save scene
        EditorSceneManager.SaveScene(activeScene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<b>[SceneRestorer]</b> Minimalist Sci-Fi VR Command Room successfully restored and saved to " + scenePath);
    }

    private static void RestoreSceneIfNeeded()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

        string scenePath = "Assets/Scenes/SampleScene.unity";
        if (!System.IO.File.Exists(scenePath))
        {
            RestoreScene();
        }
    }
}
