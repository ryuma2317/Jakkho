using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace Kannko.Editor
{
    public static class ReorganizeAssetsNow
    {

        [MenuItem("Kannko/Move Third-Party Assets to Root Folders & Clean Unused")]
        public static void RunReorganization()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            var scene = EditorSceneManager.OpenScene("Assets/Scenarios/Scenario2/Scenes/Scenario2.unity", OpenSceneMode.Single);

            EnsureDir("Assets/Materials");
            EnsureDir("Assets/Models");
            EnsureDir("Assets/Textures");
            EnsureDir("Assets/Prefabs/Environment");

            // 1. Move all Materials from third-party folders
            int matMoved = MoveFilesOfType("*.mat", "Assets/Materials");
            
            // 2. Move all FBX / OBJ meshes from third-party folders
            int meshMoved = MoveFilesOfType("*.fbx", "Assets/Models") + MoveFilesOfType("*.obj", "Assets/Models");

            // 3. Move all Textures from third-party folders
            int texMoved = MoveFilesOfType("*.png", "Assets/Textures") + 
                           MoveFilesOfType("*.jpg", "Assets/Textures") + 
                           MoveFilesOfType("*.tga", "Assets/Textures") + 
                           MoveFilesOfType("*.exr", "Assets/Textures");

            // 4. Move all Prefabs from third-party folders
            int prefabMoved = MoveFilesOfType("*.prefab", "Assets/Prefabs/Environment");

            Debug.Log($"[ReorganizeAssetsNow] MIGRATION COMPLETED:\n- Materials moved: {matMoved}\n- Meshes moved: {meshMoved}\n- Textures moved: {texMoved}\n- Prefabs moved: {prefabMoved}");

            // 5. Delete heavy unused archive files & scenes
            DeleteIfExists("Assets/LeartesStudios/CoffeeShopInterior/Built-In/CoffeeShopBuiltIn.unitypackage");
            DeleteIfExists("Assets/LeartesStudios/CoffeeShopInterior/URP/CoffeeShopURP.unitypackage");
            DeleteIfExists("Assets/Laptop/Scenes/DemoScene.unity");
            DeleteIfExists("Assets/LeartesStudios/CoffeeShopInterior/HDRP/Scenes/CoffeeShopInteriorDAY.unity");
            DeleteIfExists("Assets/LeartesStudios/CoffeeShopInterior/HDRP/Scenes/CoffeeShopInteriorNIGHT.unity");
            DeleteIfExists("Assets/LeartesStudios/CoffeeShopInterior/HDRP/Scenes/Overview.unity");

            // Clean up empty directories
            RemoveDirectoryRecursive("Assets/LeartesStudios");
            RemoveDirectoryRecursive("Assets/Laptop");
            RemoveDirectoryRecursive("Assets/AK STUDIO ART");

            AssetDatabase.Refresh();
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ReorganizeAssetsNow] Scene saved cleanly after reorganization!");
        }

        private static int MoveFilesOfType(string extensionPattern, string targetFolder)
        {
            string[] thirdPartyRoots = new string[] { "Assets/LeartesStudios", "Assets/Laptop", "Assets/AK STUDIO ART" };
            int count = 0;

            foreach (string root in thirdPartyRoots)
            {
                if (!Directory.Exists(root)) continue;

                string[] files = Directory.GetFiles(root, extensionPattern, SearchOption.AllDirectories);
                foreach (string fullFilePath in files)
                {
                    string oldAssetPath = fullFilePath.Replace('\\', '/');
                    string fileName = Path.GetFileName(oldAssetPath);
                    string newAssetPath = targetFolder + "/" + fileName;

                    if (File.Exists(newAssetPath))
                    {
                        newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);
                    }

                    string err = AssetDatabase.MoveAsset(oldAssetPath, newAssetPath);
                    if (string.IsNullOrEmpty(err))
                    {
                        count++;
                    }
                    else
                    {
                        Debug.LogWarning($"Move failed: '{oldAssetPath}' -> '{newAssetPath}': {err}");
                    }
                }
            }
            return count;
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static void DeleteIfExists(string assetPath)
        {
            if (File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void RemoveDirectoryRecursive(string path)
        {
            if (!Directory.Exists(path)) return;

            foreach (var dir in Directory.GetDirectories(path))
            {
                RemoveDirectoryRecursive(dir);
            }

            if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
