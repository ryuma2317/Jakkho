using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Kannko.Editor
{
    /// <summary>
    /// Safe project reorganization utility.
    /// - Creates target folder structure
    /// - Renamed CoffeeShop_VR.unity → Scenario2_Cafe.unity (complete — rename function is now a no-op)
    /// - Groups existing scene GameObjects under target parent groups (NO transform changes)
    /// - Saves CoffeeCup_Takeaway as Assets/Prefabs/Interactive/CoffeeCup.prefab
    ///
    /// SAFE: Does NOT modify any components, transforms, references, or behaviors.
    /// SAFE: Uses AssetDatabase.MoveAsset / RenameAsset so GUIDs are preserved.
    /// </summary>
    public static class ReorganizeProject
    {
        [MenuItem("VR Prototype/Reorganize Project Structure", priority = 2)]
        public static void Run()
        {
            Debug.Log("[Reorganize] Starting safe project reorganization...");

            CreateFolderStructure();
            RenameCoffeeShopScene();
            CreatePrefabsFromScene();
            ReorganizeSceneHierarchy();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Reorganize] Project reorganization completed successfully.");
        }

        // ─── 1. CREATE FOLDER STRUCTURE ─────────────────────────────────────────

        static void CreateFolderStructure()
        {
            Debug.Log("[Reorganize] Creating folder structure...");

            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "XR");
            EnsureFolder("Assets/Prefabs", "Environment");
            EnsureFolder("Assets/Prefabs", "Interactive");
            EnsureFolder("Assets/Prefabs", "UI");
            EnsureFolder("Assets/Prefabs", "NPC");

            EnsureFolder("Assets/Scripts", "Core");
            EnsureFolder("Assets/Scripts", "Interaction");
            EnsureFolder("Assets/Scripts", "Phone");
            EnsureFolder("Assets/Scripts", "Laptop");
            EnsureFolder("Assets/Scripts", "Wifi");
            EnsureFolder("Assets/Scripts", "Security");
            EnsureFolder("Assets/Scripts", "Scenario");
            EnsureFolder("Assets/Scripts", "UI");

            EnsureFolder("Assets", "Audio");
            EnsureFolder("Assets", "Models");
            EnsureFolder("Assets", "Settings");

            Debug.Log("[Reorganize] Folder structure ready.");
        }

        static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
                Debug.Log($"[Reorganize] Created: {path}");
            }
        }

        // ─── 2. RENAME SCENE ────────────────────────────────────────────────────

        static void RenameCoffeeShopScene()
        {
            const string oldPath = "Assets/Scenes/CoffeeShop_VR.unity";
            const string newName = "Scenario2_Cafe";
            const string newPath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                EnsureFolder("Assets", "Scenes");
            }

            if (File.Exists(newPath))
            {
                Debug.Log($"[Reorganize] Scene already named correctly: {newPath}");
                return;
            }

            if (!File.Exists(oldPath))
            {
                Debug.LogWarning($"[Reorganize] Source scene not found: {oldPath}");
                return;
            }

            string result = AssetDatabase.RenameAsset(oldPath, newName);
            if (string.IsNullOrEmpty(result))
            {
                Debug.Log($"[Reorganize] Scene renamed: {oldPath} → {newPath}");

                // Update build settings to use new path
                UpdateBuildSettingsScene(oldPath, newPath);
            }
            else
            {
                Debug.LogWarning($"[Reorganize] Scene rename failed: {result}");
            }
        }

        static void UpdateBuildSettingsScene(string oldPath, string newPath)
        {
            var scenes = EditorBuildSettings.scenes;
            bool changed = false;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == oldPath)
                {
                    scenes[i] = new EditorBuildSettingsScene(newPath, scenes[i].enabled);
                    changed = true;
                }
            }
            if (changed)
            {
                EditorBuildSettings.scenes = scenes;
                Debug.Log("[Reorganize] Build settings updated to new scene path.");
            }
        }

        // ─── 3. SAVE COFFEECUP AS PREFAB ────────────────────────────────────────

        static void CreatePrefabsFromScene()
        {
            // Determine scene path (may have already been renamed)
            string scenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[Reorganize] Scene not found for prefab extraction: {scenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find CoffeeCup_Takeaway in scene
            GameObject cupGo = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                cupGo = FindInChildren(root, "CoffeeCup_Takeaway");
                if (cupGo != null) break;
            }

            if (cupGo == null)
            {
                Debug.LogWarning("[Reorganize] CoffeeCup_Takeaway not found in scene — skipping prefab creation.");
                return;
            }

            const string prefabPath = "Assets/Prefabs/Interactive/CoffeeCup.prefab";
            if (File.Exists(prefabPath))
            {
                Debug.Log($"[Reorganize] Prefab already exists: {prefabPath}");
            }
            else
            {
                bool success;
                UnityEngine.GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cupGo, prefabPath, out success);
                if (success)
                {
                    Debug.Log($"[Reorganize] CoffeeCup prefab created: {prefabPath}");
                }
                else
                {
                    Debug.LogWarning($"[Reorganize] Prefab save failed for CoffeeCup.");
                }
            }

            EditorSceneManager.SaveScene(scene);
        }

        // ─── 4. REORGANIZE SCENE HIERARCHY ──────────────────────────────────────
        // Groups existing root GameObjects under named parent groups.
        // Uses SetParent(parent, worldPositionStays: true) so transforms are UNCHANGED.

        static void ReorganizeSceneHierarchy()
        {
            string scenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

            if (!File.Exists(scenePath))
            {
                Debug.LogWarning("[Reorganize] Scene not found for hierarchy reorganization.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // ── Locate existing objects ──────────────────────────────────────────

            GameObject floor = FindInScene(scene, "Floor");
            GameObject environment = FindInScene(scene, "Environment");
            GameObject coffeeCup = FindInScene(scene, "CoffeeCup_Takeaway");
            GameObject dirLight = FindInScene(scene, "Directional Light");
            GameObject xrOrigin = FindInScene(scene, "XR Origin (VR)");
            GameObject xrManager = FindInScene(scene, "XR Interaction Manager");
            GameObject teleportDest = FindInScene(scene, "Teleport_Destinations");
            GameObject coffeeTable = environment != null ? FindInChildren(environment, "CoffeeShop_Table") : null;
            GameObject chair = environment != null ? FindInChildren(environment, "CoffeeShop_Chair") : null;
            GameObject counter = environment != null ? FindInChildren(environment, "Cafe_OrderCounter") : null;
            GameObject lounge = environment != null ? FindInChildren(environment, "Beachside_Lounge") : null;

            // ── Create target group parents ──────────────────────────────────────

            // CafeEnvironment group
            GameObject cafeEnv = GetOrCreateGroup(scene, "CafeEnvironment");
            MoveToGroup(floor, cafeEnv, "Floor");
            MoveChildToGroup(counter, cafeEnv, environment, "Cafe_OrderCounter");
            MoveChildToGroup(lounge, cafeEnv, environment, "Beachside_Lounge");

            // PlayerArea group
            GameObject playerArea = GetOrCreateGroup(scene, "PlayerArea");
            MoveChildToGroup(coffeeTable, playerArea, environment, "CoffeeShop_Table");
            MoveChildToGroup(chair, playerArea, environment, "CoffeeShop_Chair");

            // InteractiveObjects group
            GameObject interactives = GetOrCreateGroup(scene, "InteractiveObjects");
            MoveToGroup(coffeeCup, interactives, "CoffeeCup_Takeaway");

            // WorldUI — create placeholder, nothing to move yet
            GetOrCreateGroup(scene, "WorldUI");

            // NPCs — create placeholder, nothing to move yet
            GetOrCreateGroup(scene, "NPCs");

            // Scenario — create placeholder
            GameObject scenarioGroup = GetOrCreateGroup(scene, "Scenario");

            // Lighting — keep directional light at root (Unity convention), don't force into a group
            // XR Origin stays at root (required by XR tracking system)

            // Remove Environment empty if now childless
            if (environment != null && environment.transform.childCount == 0)
            {
                Debug.Log("[Reorganize] Removing now-empty 'Environment' parent.");
                UnityEngine.Object.DestroyImmediate(environment);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Reorganize] Scene hierarchy reorganized and saved.");
        }

        // ─── HELPERS ────────────────────────────────────────────────────────────

        static GameObject GetOrCreateGroup(Scene scene, string name)
        {
            GameObject existing = FindInScene(scene, name);
            if (existing != null) return existing;

            GameObject go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            Debug.Log($"[Reorganize] Created group: {name}");
            return go;
        }

        static void MoveToGroup(GameObject obj, GameObject group, string label)
        {
            if (obj == null || group == null) return;
            if (obj.transform.parent == group.transform) return;
            obj.transform.SetParent(group.transform, worldPositionStays: true);
            Debug.Log($"[Reorganize] Moved '{label}' → '{group.name}'");
        }

        static void MoveChildToGroup(GameObject obj, GameObject newParent, GameObject oldParent, string label)
        {
            if (obj == null || newParent == null) return;
            if (obj.transform.parent == newParent.transform) return;
            obj.transform.SetParent(newParent.transform, worldPositionStays: true);
            Debug.Log($"[Reorganize] Moved '{label}' → '{newParent.name}'");
        }

        static GameObject FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
                var found = FindInChildren(root, name);
                if (found != null) return found;
            }
            return null;
        }

        static GameObject FindInChildren(GameObject parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent.transform)
            {
                var found = FindInChildren(child.gameObject, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
