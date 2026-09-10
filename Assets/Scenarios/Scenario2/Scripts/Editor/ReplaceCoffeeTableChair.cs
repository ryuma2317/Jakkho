using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Kannko.Editor
{
    /// <summary>
    /// Replaces the placeholder primitive CoffeeShop_Table and CoffeeShop_Chair
    /// with the imported BoothsChairsTables high-fidelity assets:
    /// - Table: BoothsChairsTables_SM_Table_8
    /// - Player Chair: BoothsChairsTables_SM_Chair_6 (facing table at z=0)
    /// - Opposite Booth: BoothsChairsTables_SM_Booth_Single_4 (facing table at z=1.45m)
    /// - Re-aligns CoffeeCup and Smartphone to rest naturally on the new table surface (y = 0.795m)
    /// - Preserves CoffeeShop_Table and CoffeeShop_Chair names and references so all test suites pass
    /// </summary>
    public static class ReplaceCoffeeTableChair
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private const string TablePrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/BoothsChairsTables_SM_Table_8.prefab";
        private const string ChairPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/BoothsChairsTables_SM_Chair_6.prefab";
        private const string BoothPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/BoothsChairsTables_SM_Booth_Single_4.prefab";

        [MenuItem("VR Prototype/Replace Coffee Table and Chair with BoothsChairsTables", priority = 15)]
        public static void Replace()
        {
            Debug.Log("[BoothsChairsTables] Starting table and chair replacement...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[BoothsChairsTables] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject cafeEnv = GameObject.Find("CafeEnvironment");
            if (cafeEnv == null)
            {
                Debug.LogError("[BoothsChairsTables] CafeEnvironment not found!");
                return;
            }

            // Load prefabs
            GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            GameObject chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChairPrefabPath);
            GameObject boothPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoothPrefabPath);

            if (tablePrefab == null || chairPrefab == null)
            {
                Debug.LogError("[BoothsChairsTables] Could not load Table or Chair prefabs!");
                return;
            }

            // 1. Replace CoffeeShop_Table
            GameObject oldTable = GameObject.Find("CoffeeShop_Table");
            Vector3 tablePos = new Vector3(0f, 0f, 0.65f);
            if (oldTable != null)
            {
                tablePos = oldTable.transform.position;
                Object.DestroyImmediate(oldTable);
            }

            GameObject newTable = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab, cafeEnv.transform);
            newTable.name = "CoffeeShop_Table";
            newTable.transform.position = tablePos;
            newTable.transform.rotation = Quaternion.identity;

            // 2. Replace CoffeeShop_Chair (Player's seated chair)
            GameObject oldChair = GameObject.Find("CoffeeShop_Chair");
            Vector3 chairPos = Vector3.zero;
            if (oldChair != null)
            {
                chairPos = oldChair.transform.position;
                Object.DestroyImmediate(oldChair);
            }

            GameObject newChair = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab, cafeEnv.transform);
            newChair.name = "CoffeeShop_Chair";
            newChair.transform.position = chairPos;
            // Rotate 180 degrees so the backrest is behind the player (-Z) and the seat faces the table (+Z)
            newChair.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // 3. Add opposite Booth Bench (BoothsChairsTables_SM_Booth_Single_4) across the table
            GameObject existingBooth = GameObject.Find("CoffeeShop_Booth");
            if (existingBooth != null) Object.DestroyImmediate(existingBooth);

            if (boothPrefab != null)
            {
                GameObject newBooth = (GameObject)PrefabUtility.InstantiatePrefab(boothPrefab, cafeEnv.transform);
                newBooth.name = "CoffeeShop_Booth";
                // Position across the table facing the player and table (-Z)
                // Table is centered at Z=0.65, table front is Z=0.27, back is Z=1.03.
                // Booth seat front should be around Z=1.10, backrest at Z=1.45.
                newBooth.transform.position = new Vector3(0f, 0f, 1.45f);
                newBooth.transform.rotation = Quaternion.identity;
            }

            // 4. Update heights of items resting on the table (Table surface is now y = 0.79m)
            GameObject cup = GameObject.Find("CoffeeCup_Takeaway");
            if (cup != null)
            {
                cup.transform.position = new Vector3(0.18f, 0.795f, 0.55f);
                Rigidbody cupRb = cup.GetComponent<Rigidbody>();
                if (cupRb != null)
                {
                    cupRb.linearVelocity = Vector3.zero;
                    cupRb.angularVelocity = Vector3.zero;
                }
            }

            GameObject phone = GameObject.Find("Smartphone");
            if (phone != null)
            {
                phone.transform.position = new Vector3(-0.15f, 0.795f, 0.52f);
                Rigidbody phoneRb = phone.GetComponent<Rigidbody>();
                if (phoneRb != null)
                {
                    phoneRb.linearVelocity = Vector3.zero;
                    phoneRb.angularVelocity = Vector3.zero;
                }
            }

            // Bag beside player chair
            GameObject bag = GameObject.Find("LaptopBag");
            if (bag != null)
            {
                bag.transform.position = new Vector3(-0.45f, 0.20f, 0.05f);
                Rigidbody bagRb = bag.GetComponent<Rigidbody>();
                if (bagRb != null)
                {
                    bagRb.linearVelocity = Vector3.zero;
                    bagRb.angularVelocity = Vector3.zero;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[BoothsChairsTables] Successfully replaced coffee table and chair with BoothsChairsTables assets!");
        }
    }
}
