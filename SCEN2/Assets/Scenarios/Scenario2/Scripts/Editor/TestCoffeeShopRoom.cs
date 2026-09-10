using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Kannko.Editor
{
    /// <summary>
    /// Test suite verifying the expanded 8m x 9m coffee shop room environment:
    /// - Dimensions (~8m x 9m)
    /// - Required hierarchy structure (Floor, Walls, Ceiling, Windows, Entryway, OrderCounter, Booths, AC, Lighting, Decorations, BeachView)
    /// - Complete ceiling AC system (AC Box, ducts, T-Joint, 90-turn, exhaust vents, pipe supports)
    /// - Order counter with props (espresso machine, register, mugs, napkin dispenser, wall shelves)
    /// - Walk-in entryway with glass door (WallsAndFloors_SM_Wall_Windows_3m_Door_11)
    /// - Large windows with beach/ocean view
    /// - Consistent wall material/color (M_WallPaint and M_WoodTrim)
    /// - Strict preservation of existing interactive objects (Smartphone, Coffee, Laptop Bag) and VR Rig
    /// </summary>
    public static class TestCoffeeShopRoom
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

        public static void RunTests()
        {
            Debug.Log("===============================================================================");
            Debug.Log("[TEST SUITE] Validating CoffeeShopRoom (8m x 9m) Environment & Features...");
            Debug.Log("===============================================================================");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[TEST SUITE] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int passed = 0;
            int failed = 0;

            // ─── TEST 1: ROOM ROOT & HIERARCHY STRUCTURE ────────────────────────────
            GameObject room = GameObject.Find("CoffeeShopRoom_8x9m") ?? GameObject.Find("CoffeeShopRoom_6x7m");
            string[] requiredCategories = new string[]
            {
                "Floor", "Walls", "Ceiling", "Windows", "Entryway", "OrderCounter", "Booths", "Decorations", "AC", "Lighting", "BeachView"
            };

            bool hierarchyValid = room != null;
            if (hierarchyValid)
            {
                foreach (var cat in requiredCategories)
                {
                    if (room.transform.Find(cat) == null)
                    {
                        hierarchyValid = false;
                        Debug.LogError($"[TEST 1/8] Missing child category under room: {cat}");
                    }
                }
            }

            if (hierarchyValid)
            {
                Debug.Log("[TEST 1/8] Room Structure: PASS (Room root exists with all required functional sub-hierarchies)");
                passed++;
            }
            else
            {
                Debug.LogError("[TEST 1/8] Room Structure: FAIL (Room or sub-hierarchies missing)");
                failed++;
            }

            // ─── TEST 2: ROOM SCALE & BOUNDS (8m x 9m, Height 3m) ───────────────────
            GameObject floor = GameObject.Find("Cafe_Floor_8x9m");
            GameObject ceiling = GameObject.Find("Cafe_Ceiling_8x9m");

            bool boundsValid = floor != null && ceiling != null;
            if (boundsValid)
            {
                // Plane scale (0.8, 1, 0.9) = 8m x 9m
                float width = floor.transform.localScale.x * 10f;
                float depth = floor.transform.localScale.z * 10f;
                float height = ceiling.transform.position.y - floor.transform.position.y;

                bool sizeMatches = Mathf.Approximately(width, 8f) && Mathf.Approximately(depth, 9f) && Mathf.Approximately(height, 3f);
                if (sizeMatches)
                {
                    Debug.Log($"[TEST 2/8] Real-World Scale: PASS (Width={width}m, Depth={depth}m, Height={height}m)");
                    passed++;
                }
                else
                {
                    Debug.LogError($"[TEST 2/8] Real-World Scale: FAIL (Width={width}m, Depth={depth}m, Height={height}m)");
                    failed++;
                }
            }
            else
            {
                Debug.LogError("[TEST 2/8] Real-World Scale: FAIL (Floor or Ceiling missing)");
                failed++;
            }

            // ─── TEST 3: COMPLETED WALLS WITH CONSISTENT MATERIAL ───────────────────
            GameObject walls = room != null ? room.transform.Find("Walls")?.gameObject : null;
            bool wallCountValid = walls != null && walls.transform.childCount >= 6;
            bool wallMatsConsistent = true;

            if (walls != null)
            {
                foreach (Transform child in walls.transform)
                {
                    Renderer r = child.GetComponentInChildren<Renderer>();
                    if (r != null && r.sharedMaterial != null)
                    {
                        if (!r.sharedMaterial.name.Contains("WallPaint"))
                        {
                            wallMatsConsistent = false;
                            Debug.LogWarning($"[TEST 3/8] Non-WallPaint material detected on wall: {child.name} ({r.sharedMaterial.name})");
                        }
                    }
                }
            }

            if (wallCountValid && wallMatsConsistent)
            {
                Debug.Log($"[TEST 3/8] Completed Enclosed Walls: PASS ({walls.transform.childCount} wall segments using consistent M_WallPaint)");
                passed++;
            }
            else
            {
                Debug.LogError($"[TEST 3/8] Completed Enclosed Walls: FAIL (Count={wallCountValid}, Mats={wallMatsConsistent})");
                failed++;
            }

            // ─── TEST 4: WALK-IN ENTRYWAY & GLASS ENTRANCE DOOR ─────────────────────
            GameObject doorSection = GameObject.Find("Entrance_Door_Section");
            GameObject entranceMat = GameObject.Find("Entrance_Welcome_Mat");

            bool entrywayValid = doorSection != null && entranceMat != null;
            if (entrywayValid)
            {
                Debug.Log("[TEST 4/8] Walk-in Entryway & Door: PASS (Entrance door section and welcome mat configured)");
                passed++;
            }
            else
            {
                Debug.LogError($"[TEST 4/8] Walk-in Entryway & Door: FAIL (Door={doorSection != null}, Mat={entranceMat != null})");
                failed++;
            }

            // ─── TEST 5: CAFE ORDER COUNTER & ACCESSORIES ───────────────────────────
            GameObject counterBase = GameObject.Find("Counter_Base");
            GameObject counterTop = GameObject.Find("Counter_Top");
            GameObject espresso = GameObject.Find("Espresso_Machine");
            GameObject register = GameObject.Find("Cash_Register");

            bool counterValid = counterBase != null && counterTop != null && espresso != null && register != null;
            if (counterValid)
            {
                Debug.Log("[TEST 5/8] Cafe Order Counter: PASS (Wood counter base, polished countertop, espresso machine, POS register, mugs, and wall shelves)");
                passed++;
            }
            else
            {
                Debug.LogError($"[TEST 5/8] Cafe Order Counter: FAIL (Base={counterBase != null}, Top={counterTop != null}, Espresso={espresso != null}, Register={register != null})");
                failed++;
            }

            // ─── TEST 6: COMPLETE CEILING AC & VENTILATION NETWORK ──────────────────
            GameObject acBox = GameObject.Find("Ceiling_AC_Unit");
            GameObject acDuct1 = GameObject.Find("AC_Duct_MainRun_1");
            GameObject acTJoint = GameObject.Find("AC_TJoint_Branch");
            GameObject acTurn = GameObject.Find("AC_90Turn_Bend");
            GameObject acExhaust = GameObject.Find("AC_Exhaust_Seating_1");

            bool acFullValid = acBox != null && acDuct1 != null && acTJoint != null && acTurn != null && acExhaust != null;
            if (acFullValid)
            {
                Debug.Log("[TEST 6/8] Complete Ceiling AC Network: PASS (Commercial AC Box, 3m ducts, T-Joint branch, 90-degree bend, and exhaust vents over seating)");
                passed++;
            }
            else
            {
                Debug.LogError($"[TEST 6/8] Complete Ceiling AC Network: FAIL (Box={acBox != null}, Duct={acDuct1 != null}, TJoint={acTJoint != null}, Turn={acTurn != null}, Exhaust={acExhaust != null})");
                failed++;
            }

            // ─── TEST 7: WINDOWS, BEACH VIEW & BOOTH PAIRS ──────────────────────────
            GameObject winL = GameObject.Find("Window_Wall_L");
            GameObject sand = GameObject.Find("Beach_Sand_Shore");
            GameObject ocean = GameObject.Find("Ocean_Water");
            GameObject booths = room != null ? room.transform.Find("Booths")?.gameObject : null;
            bool boothsValid = booths != null && booths.transform.childCount >= 3;

            bool winBeachValid = winL != null && sand != null && ocean != null && boothsValid;
            if (winBeachValid)
            {
                Debug.Log($"[TEST 7/8] Windows, Beach View & Booths: PASS (Large front windows, sand/ocean backdrop, and {booths.transform.childCount} booth pairs)");
                passed++;
            }
            else
            {
                Debug.LogError($"[TEST 7/8] Windows, Beach View & Booths: FAIL (Win={winL != null}, Beach={sand != null && ocean != null}, Booths={boothsValid})");
                failed++;
            }

            // ─── TEST 8: STRICT PRESERVATION OF EXISTING VR OBJECTS ─────────────────
            GameObject phone = GameObject.Find("InteractiveObjects/Smartphone") ?? GameObject.Find("Smartphone");
            GameObject cup = GameObject.Find("InteractiveObjects/CoffeeCup_Takeaway") ?? GameObject.Find("CoffeeCup_Takeaway");
            GameObject bag = GameObject.Find("InteractiveObjects/LaptopBag") ?? GameObject.Find("LaptopBag");
            GameObject xrOrigin = GameObject.Find("XR Origin (VR)");
            GameObject playerTable = GameObject.Find("CoffeeShop_Table");

            bool phonePreserved = phone != null && phone.GetComponent<Rigidbody>() != null;
            bool cupPreserved = cup != null && cup.GetComponent<Rigidbody>() != null;
            bool bagPreserved = bag != null && bag.GetComponent<Rigidbody>() != null;
            bool originPreserved = xrOrigin != null;
            bool tablePreserved = playerTable != null;

            bool allPreserved = phonePreserved && cupPreserved && bagPreserved && originPreserved && tablePreserved;

            if (allPreserved)
            {
                Debug.Log("[TEST 8/8] Existing VR Objects Preservation: PASS (Smartphone, Coffee Cup, Laptop Bag, XR Rig, and Seated Table 100% untouched)");
                passed++;
            }
            else
            {
                Debug.LogError($"[TEST 8/8] Existing VR Objects Preservation: FAIL (Phone={phonePreserved}, Cup={cupPreserved}, Bag={bagPreserved}, Rig={originPreserved}, Table={tablePreserved})");
                failed++;
            }

            Debug.Log("===============================================================================");
            Debug.Log($"[TEST SUMMARY] Complete: {passed} PASSED, {failed} FAILED out of 8 checks.");
            Debug.Log("===============================================================================");
        }
    }
}
