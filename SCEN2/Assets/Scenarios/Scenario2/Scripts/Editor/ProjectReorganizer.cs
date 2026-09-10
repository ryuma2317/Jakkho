using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

namespace Kannko.Editor
{
    public static class ProjectReorganizer
    {
        [MenuItem("Kannko/Reorganize & Clean Project Assets")]
        public static void ReorganizeAndClean()
        {
            string scenarioScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
            
            // 1. Open Scenario2_Cafe scene explicitly
            var loadedScene = EditorSceneManager.OpenScene(scenarioScenePath, OpenSceneMode.Single);
            Debug.Log($"[Project Reorganizer] Opened scene '{loadedScene.name}' at '{loadedScene.path}'.");

            HashSet<string> requiredAssets = new HashSet<string>();

            // Collect all dependencies of Scenario2_Cafe.unity
            string[] sceneDeps = AssetDatabase.GetDependencies(scenarioScenePath, true);
            foreach (string dep in sceneDeps)
            {
                if (!string.IsNullOrEmpty(dep) && File.Exists(dep))
                {
                    requiredAssets.Add(dep);
                }
            }

            // Collect dependencies of all prefabs under Assets/Prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/Prefabs" });
            foreach (string guid in prefabGuids)
            {
                string pPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(pPath))
                {
                    requiredAssets.Add(pPath);
                    string[] pDeps = AssetDatabase.GetDependencies(pPath, true);
                    foreach (string d in pDeps)
                    {
                        if (!string.IsNullOrEmpty(d) && File.Exists(d)) requiredAssets.Add(d);
                    }
                }
            }

            // Collect all scripts under Assets/Scripts
            string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new string[] { "Assets/Scripts" });
            foreach (string guid in scriptGuids)
            {
                string sPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(sPath)) requiredAssets.Add(sPath);
            }

            // Explicitly scan scene components to guarantee no asset is missed
            GameObject[] rootObjects = loadedScene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                Component[] components = root.GetComponentsInChildren<Component>(true);
                foreach (Component c in components)
                {
                    if (c == null) continue;

                    if (c is MeshFilter mf && mf.sharedMesh != null)
                    {
                        AddAssetPath(mf.sharedMesh, requiredAssets);
                    }
                    if (c is Renderer r)
                    {
                        foreach (Material mat in r.sharedMaterials)
                        {
                            if (mat != null) AddAssetPath(mat, requiredAssets);
                        }
                    }
                    if (c is UnityEngine.UI.Image img && img.sprite != null)
                    {
                        AddAssetPath(img.sprite, requiredAssets);
                    }
                    if (c is AudioSource audio && audio.clip != null)
                    {
                        AddAssetPath(audio.clip, requiredAssets);
                    }

                    // Prefab source check
                    GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject);
                    if (prefabSource != null)
                    {
                        AddAssetPath(prefabSource, requiredAssets);
                    }
                }
            }

            Debug.Log($"[Project Reorganizer] Total required asset dependencies identified: {requiredAssets.Count}");

            // Ensure destination directories exist
            EnsureDirectoryExists("Assets/Materials");
            EnsureDirectoryExists("Assets/Models");
            EnsureDirectoryExists("Assets/Textures");
            EnsureDirectoryExists("Assets/Prefabs/Environment");
            EnsureDirectoryExists("Assets/Prefabs/Interactive");
            EnsureDirectoryExists("Assets/Prefabs/Scenario2");
            EnsureDirectoryExists("Assets/Prefabs/UI");
            EnsureDirectoryExists("Assets/Prefabs/XR");
            EnsureDirectoryExists("Assets/Audio");
            EnsureDirectoryExists("Assets/Scripts");

            // 2. Relocate active imported assets into main clean project folders
            List<string> assetList = new List<string>(requiredAssets);
            int movedCount = 0;

            foreach (string path in assetList)
            {
                if (IsThirdPartyAsset(path) && File.Exists(path))
                {
                    string fileName = Path.GetFileName(path);
                    string ext = Path.GetExtension(path).ToLower();

                    string targetFolder = null;

                    if (ext == ".mat") targetFolder = "Assets/Materials";
                    else if (ext == ".fbx" || ext == ".obj" || ext == ".blend") targetFolder = "Assets/Models";
                    else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".hdr" || ext == ".exr" || ext == ".tif") targetFolder = "Assets/Textures";
                    else if (ext == ".prefab") targetFolder = "Assets/Prefabs/Environment";
                    else if (ext == ".wav" || ext == ".mp3" || ext == ".ogg") targetFolder = "Assets/Audio";
                    else if (ext == ".cs") targetFolder = "Assets/Scripts";

                    if (!string.IsNullOrEmpty(targetFolder))
                    {
                        EnsureDirectoryExists(targetFolder);
                        string targetPath = targetFolder + "/" + fileName;

                        if (path != targetPath)
                        {
                            targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);
                            string error = AssetDatabase.MoveAsset(path, targetPath);
                            if (string.IsNullOrEmpty(error))
                            {
                                movedCount++;
                            }
                            else
                            {
                                Debug.LogWarning($"[Project Reorganizer] Move failed '{path}' -> '{targetPath}': {error}");
                            }
                        }
                    }
                }
            }

            Debug.Log($"[Project Reorganizer] Moved {movedCount} active assets into standardized project folders.");

            // Re-gather dependencies after moves
            requiredAssets.Clear();
            sceneDeps = AssetDatabase.GetDependencies(scenarioScenePath, true);
            foreach (string dep in sceneDeps)
            {
                if (!string.IsNullOrEmpty(dep) && File.Exists(dep)) requiredAssets.Add(dep);
            }

            // 3. Delete unused third-party assets and package archives
            int deletedCount = 0;
            string[] scanFolders = new string[] { "Assets/LeartesStudios", "Assets/Laptop", "Assets/AK STUDIO ART" };

            foreach (string folder in scanFolders)
            {
                if (Directory.Exists(folder))
                {
                    string[] allGuids = AssetDatabase.FindAssets("", new string[] { folder });
                    foreach (string guid in allGuids)
                    {
                        string aPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(aPath) && File.Exists(aPath))
                        {
                            if (!requiredAssets.Contains(aPath) && !Directory.Exists(aPath))
                            {
                                if (AssetDatabase.DeleteAsset(aPath))
                                {
                                    deletedCount++;
                                }
                            }
                        }
                    }
                }
            }

            // Delete heavy .unitypackage files anywhere under Assets
            string[] allAssetFiles = Directory.GetFiles("Assets", "*.unitypackage", SearchOption.AllDirectories);
            foreach (string pkgPath in allAssetFiles)
            {
                string normPath = pkgPath.Replace('\\', '/');
                if (AssetDatabase.DeleteAsset(normPath))
                {
                    deletedCount++;
                }
            }

            // Delete unused demo scenes
            string[] unusedScenes = new string[] {
                "Assets/Laptop/Scenes/DemoScene.unity",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Scenes/CoffeeShopInteriorDAY.unity",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Scenes/CoffeeShopInteriorNIGHT.unity",
                "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Scenes/Overview.unity"
            };
            foreach (string sPath in unusedScenes)
            {
                if (File.Exists(sPath))
                {
                    AssetDatabase.DeleteAsset(sPath);
                    deletedCount++;
                }
            }

            // 4. Remove empty directories
            foreach (string folder in scanFolders)
            {
                RemoveEmptyDirectories(folder);
            }

            AssetDatabase.Refresh();
            EditorSceneManager.SaveOpenScenes();

            Debug.Log($"[Project Reorganizer] FINISHED SUCCESS: Moved {movedCount} used assets into root folders, deleted {deletedCount} unused assets/packages. Scenario2_Cafe scene saved!");
        }

        private static void AddAssetPath(Object obj, HashSet<string> set)
        {
            if (obj == null) return;
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                set.Add(path);
                string[] deps = AssetDatabase.GetDependencies(path, true);
                foreach (string d in deps)
                {
                    if (!string.IsNullOrEmpty(d)) set.Add(d);
                }
            }
        }

        private static bool IsThirdPartyAsset(string path)
        {
            return path.StartsWith("Assets/LeartesStudios/") ||
                   path.StartsWith("Assets/Laptop/") ||
                   path.StartsWith("Assets/AK STUDIO ART/");
        }

        private static void EnsureDirectoryExists(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                AssetDatabase.Refresh();
            }
        }

        private static void RemoveEmptyDirectories(string startDir)
        {
            if (!Directory.Exists(startDir)) return;

            foreach (var dir in Directory.GetDirectories(startDir))
            {
                RemoveEmptyDirectories(dir);
            }

            if (Directory.GetFiles(startDir).Length == 0 && Directory.GetDirectories(startDir).Length == 0)
            {
                AssetDatabase.DeleteAsset(startDir);
            }
        }
    }
}
