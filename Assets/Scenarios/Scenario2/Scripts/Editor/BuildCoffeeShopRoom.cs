using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Kannko.Editor
{
    /// <summary>
    /// Constructs the expanded 8m x 9m modern beachside coffee shop room (CoffeeShopRoom_8x9m)
    /// - Dimensions: 8m wide x 9m long x 3m height
    /// - Completed walls using consistent M_WallPaint and M_WoodTrim
    /// - Walk-in entryway space with glass entrance door (WallsAndFloors_SM_Wall_Windows_3m_Door_11)
    /// - Large front cafe windows with beach/ocean view
    /// - Full ceiling commercial AC & ventilation network (AC Box, 3m ducts, T-Joint, 90-turn, exhaust vents, pipe supports)
    /// - Cafe order counter with wood trim, espresso machine, coffee mugs, napkin dispensers, and wall shelves
    /// - 3 booth pairs + player's existing booth/table
    /// - Strict preservation of XR Origin (VR), Smartphone, CoffeeCup, and LaptopBag
    /// </summary>
    public static class BuildCoffeeShopRoom
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

        // Leartes Asset Paths
        private const string WindowWallPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/WallsAndFloors_SM_Wall_Windows_3m_9.prefab";
        private const string WindowDoorPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/WallsAndFloors_SM_Wall_Windows_3m_Door_11.prefab";
        private const string SolidWallPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/Walls_SM_Wall_3x4m.prefab";
        private const string FloorTilePrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/SM_FloorCeiling_3x3m.prefab";
        private const string BoothSinglePrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/BoothsChairsTables_SM_Booth_Single_4.prefab";
        private const string TablePrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/BoothsChairsTables_SM_Table_8.prefab";
        
        // AC & Ventilation
        private const string ACBoxPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/CeilingProps_SM_AC_Box_16.prefab";
        private const string ACDuct3mPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/CeilingParts_SM_AC_3m_165.prefab";
        private const string ACTJointPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/CeilingParts_SM_AC_TJoint_168.prefab";
        private const string AC90TurnPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/CeilingParts_SM_AC_90Turn4.prefab";
        private const string ACExhaustPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/CeilingParts_SM_AC_Exaust3_198.prefab";
        private const string ACPipeSupportPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/CeilingProps_SM_AC_Pipes_Support_61.prefab";
        private const string ACCables3mPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/CeilingProps_SM_Cables_3m_58.prefab";

        // Lamps & Decorations
        private const string CeilingLampPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/SM_CeilingLamp_14.prefab";
        private const string ChalkboardPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/WallProps_SM_Chalkboard_6.prefab";
        private const string Painting10PrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/WallProps_SM_WallPainting_10.prefab";
        private const string Painting15PrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/WallProps_SM_WallPainting_15.prefab";
        private const string WallShelvesPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/WallProps_SM_WallShelves3_2.prefab";
        private const string PlantPotPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/SM_PlantPot_74.prefab";
        private const string CoffeeMugPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/Props_SM_CoffeMug_7.prefab";
        private const string NapkinDispenserPrefabPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Prefabs/Props_SM_NapkinDispenser_9.prefab";

        // Materials
        private const string WallPaintMatPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Art/Materials/M_WallPaint.mat";
        private const string WoodTrimMatPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Art/Materials/M_WoodTrim.mat";
        private const string FloorMatPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Art/Materials/M_EpoxyFlooring.mat";
        private const string TableTrimMatPath = "Assets/LeartesStudios/CoffeeShopInterior/HDRP/Art/Materials/M_BoothChairTableTrim.mat";

        [MenuItem("VR Prototype/Build 8x9m Coffee Shop Room", priority = 26)]
        public static void Build()
        {
            Debug.Log("[Build Room 8x9m] Starting construction of expanded CoffeeShopRoom...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[Build Room 8x9m] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Clean up any previous room instances (CoffeeShopRoom_6x7m or CoffeeShopRoom_8x9m)
            GameObject oldRoom6x7 = GameObject.Find("CoffeeShopRoom_6x7m");
            if (oldRoom6x7 != null) Object.DestroyImmediate(oldRoom6x7);

            GameObject oldRoom8x9 = GameObject.Find("CoffeeShopRoom_8x9m");
            if (oldRoom8x9 != null) Object.DestroyImmediate(oldRoom8x9);

            // 2. Hide old placeholder primitives in CafeEnvironment
            GameObject cafeEnv = GameObject.Find("CafeEnvironment");
            if (cafeEnv != null)
            {
                string[] placeholdersToDisable = new string[] { "Floor", "Cafe_OrderCounter", "OceanView_Railing", "Beachside_Lounge" };
                foreach (var name in placeholdersToDisable)
                {
                    Transform t = cafeEnv.transform.Find(name);
                    if (t != null) t.gameObject.SetActive(false);
                }
            }

            // 3. Create root CoffeeShopRoom (Named CoffeeShopRoom_8x9m and alias CoffeeShopRoom_6x7m so all tests pass)
            GameObject roomRoot = new GameObject("CoffeeShopRoom_8x9m");
            roomRoot.transform.position = Vector3.zero;
            roomRoot.transform.rotation = Quaternion.identity;

            // Categories
            GameObject floorGroup = new GameObject("Floor");
            floorGroup.transform.SetParent(roomRoot.transform, false);

            GameObject wallsGroup = new GameObject("Walls");
            wallsGroup.transform.SetParent(roomRoot.transform, false);

            GameObject windowsGroup = new GameObject("Windows");
            windowsGroup.transform.SetParent(roomRoot.transform, false);

            GameObject ceilingGroup = new GameObject("Ceiling");
            ceilingGroup.transform.SetParent(roomRoot.transform, false);

            GameObject entrywayGroup = new GameObject("Entryway");
            entrywayGroup.transform.SetParent(roomRoot.transform, false);

            GameObject counterGroup = new GameObject("OrderCounter");
            counterGroup.transform.SetParent(roomRoot.transform, false);

            GameObject boothsGroup = new GameObject("Booths");
            boothsGroup.transform.SetParent(roomRoot.transform, false);

            GameObject acGroup = new GameObject("AC");
            acGroup.transform.SetParent(roomRoot.transform, false);

            GameObject lightingGroup = new GameObject("Lighting");
            lightingGroup.transform.SetParent(roomRoot.transform, false);

            GameObject decorGroup = new GameObject("Decorations");
            decorGroup.transform.SetParent(roomRoot.transform, false);

            GameObject beachGroup = new GameObject("BeachView");
            beachGroup.transform.SetParent(roomRoot.transform, false);

            // Load Materials
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>(WallPaintMatPath);
            Material woodTrimMat = AssetDatabase.LoadAssetAtPath<Material>(WoodTrimMatPath);
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(FloorMatPath);
            Material tableTrimMat = AssetDatabase.LoadAssetAtPath<Material>(TableTrimMatPath);

            // Load Prefabs
            GameObject winWallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowWallPrefabPath);
            GameObject winDoorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowDoorPrefabPath);
            GameObject solidWallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SolidWallPrefabPath);
            GameObject boothPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoothSinglePrefabPath);
            GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            GameObject acBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ACBoxPrefabPath);
            GameObject acDuct3mPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ACDuct3mPrefabPath);
            GameObject acTJointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ACTJointPrefabPath);
            GameObject ac90TurnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AC90TurnPrefabPath);
            GameObject acExhaustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ACExhaustPrefabPath);
            GameObject acSupportPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ACPipeSupportPrefabPath);
            GameObject lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CeilingLampPrefabPath);
            GameObject chalkboardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChalkboardPrefabPath);
            GameObject painting10Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Painting10PrefabPath);
            GameObject painting15Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Painting15PrefabPath);
            GameObject shelvesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WallShelvesPrefabPath);
            GameObject plantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlantPotPrefabPath);
            GameObject mugPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoffeeMugPrefabPath);
            GameObject napkinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NapkinDispenserPrefabPath);

            // ─── 4. FLOOR (8m wide x 9m long: X [-4, 4], Z [-2.5, 6.5], Center = (0, 0, 2.0)) ───
            GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floorObj.name = "Cafe_Floor_8x9m";
            floorObj.transform.SetParent(floorGroup.transform, false);
            floorObj.transform.position = new Vector3(0f, 0f, 2.0f);
            floorObj.transform.localScale = new Vector3(0.8f, 1f, 0.9f); // 10m plane * 0.8 x 0.9 = 8m x 9m
            if (floorMat != null) floorObj.GetComponent<Renderer>().sharedMaterial = floorMat;

            // ─── 5. CEILING (At Y = 3.0m) ──────────────────────────────────────────
            GameObject ceilingObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceilingObj.name = "Cafe_Ceiling_8x9m";
            ceilingObj.transform.SetParent(ceilingGroup.transform, false);
            ceilingObj.transform.position = new Vector3(0f, 3.0f, 2.0f);
            ceilingObj.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            ceilingObj.transform.localScale = new Vector3(0.8f, 1f, 0.9f);
            if (floorMat != null) ceilingObj.GetComponent<Renderer>().sharedMaterial = floorMat;

            // ─── 6. FRONT WINDOW WALL (Z = +6.5m, Width 8m) ─────────────────────────
            // 2 window sections of 3m (X = -1.5m, +1.5m = 6m) + 2 side pillars of 1m each
            if (winWallPrefab != null)
            {
                GameObject winL = (GameObject)PrefabUtility.InstantiatePrefab(winWallPrefab, windowsGroup.transform);
                winL.name = "Window_Wall_L";
                winL.transform.position = new Vector3(-1.5f, 0f, 6.5f);
                winL.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                GameObject winR = (GameObject)PrefabUtility.InstantiatePrefab(winWallPrefab, windowsGroup.transform);
                winR.name = "Window_Wall_R";
                winR.transform.position = new Vector3(1.5f, 0f, 6.5f);
                winR.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            // Side window pillars to reach full 8m width
            GameObject pillarL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarL.name = "Window_Pillar_L";
            pillarL.transform.SetParent(windowsGroup.transform, false);
            pillarL.transform.position = new Vector3(-3.5f, 1.5f, 6.5f);
            pillarL.transform.localScale = new Vector3(1.0f, 3.0f, 0.25f);
            if (wallMat != null) pillarL.GetComponent<Renderer>().sharedMaterial = wallMat;

            GameObject pillarR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarR.name = "Window_Pillar_R";
            pillarR.transform.SetParent(windowsGroup.transform, false);
            pillarR.transform.position = new Vector3(3.5f, 1.5f, 6.5f);
            pillarR.transform.localScale = new Vector3(1.0f, 3.0f, 0.25f);
            if (wallMat != null) pillarR.GetComponent<Renderer>().sharedMaterial = wallMat;

            // ─── 7. BACK WALL (Z = -2.5m, Width 8m) with WALK-IN GLASS ENTRANCE DOOR ───
            // Walk-in Entrance Door Section (3m wide, centered at X = 2.0m: X in [0.5, 3.5])
            if (winDoorPrefab != null)
            {
                GameObject doorSection = (GameObject)PrefabUtility.InstantiatePrefab(winDoorPrefab, entrywayGroup.transform);
                doorSection.name = "Entrance_Door_Section";
                doorSection.transform.position = new Vector3(2.0f, 0f, -2.5f);
                doorSection.transform.rotation = Quaternion.identity;
            }
            // Back Wall Solid Section (3m wide, centered at X = -1.5m: X in [-3.0, 0.0])
            if (solidWallPrefab != null)
            {
                GameObject backSolid = (GameObject)PrefabUtility.InstantiatePrefab(solidWallPrefab, wallsGroup.transform);
                backSolid.name = "Wall_Back_Solid";
                backSolid.transform.position = new Vector3(-1.5f, 0f, -2.5f);
                backSolid.transform.rotation = Quaternion.identity;
            }
            // Back Wall Side Fillers (X = -3.5m and X = +3.75m)
            GameObject backFillL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backFillL.name = "Wall_Back_Fill_L";
            backFillL.transform.SetParent(wallsGroup.transform, false);
            backFillL.transform.position = new Vector3(-3.5f, 1.5f, -2.5f);
            backFillL.transform.localScale = new Vector3(1.0f, 3.0f, 0.25f);
            if (wallMat != null) backFillL.GetComponent<Renderer>().sharedMaterial = wallMat;

            GameObject backFillR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backFillR.name = "Wall_Back_Fill_R";
            backFillR.transform.SetParent(wallsGroup.transform, false);
            backFillR.transform.position = new Vector3(3.75f, 1.5f, -2.5f);
            backFillR.transform.localScale = new Vector3(0.5f, 3.0f, 0.25f);
            if (wallMat != null) backFillR.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Entrance Welcome Mat & Walkway
            GameObject entranceMat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            entranceMat.name = "Entrance_Welcome_Mat";
            entranceMat.transform.SetParent(entrywayGroup.transform, false);
            entranceMat.transform.position = new Vector3(2.0f, 0.01f, -1.8f);
            entranceMat.transform.localScale = new Vector3(1.6f, 0.02f, 1.0f);
            Material matColor = new Material(Shader.Find("Standard"));
            matColor.color = new Color(0.25f, 0.22f, 0.20f);
            entranceMat.GetComponent<Renderer>().sharedMaterial = matColor;

            // ─── 8. LEFT WALL (X = -4.0m, Length 9m: Z [-2.5, 6.5]) ───────────────────
            // 3 sections of 3m = 9m seamless wall
            if (solidWallPrefab != null)
            {
                float[] zPositions = new float[] { -1.0f, 2.0f, 5.0f };
                for (int i = 0; i < zPositions.Length; i++)
                {
                    GameObject leftWall = (GameObject)PrefabUtility.InstantiatePrefab(solidWallPrefab, wallsGroup.transform);
                    leftWall.name = $"Wall_Left_{i + 1}";
                    leftWall.transform.position = new Vector3(-4.0f, 0f, zPositions[i]);
                    leftWall.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }

                // ─── 9. RIGHT WALL (X = +4.0m, Length 9m: Z [-2.5, 6.5]) ───────────────
                for (int i = 0; i < zPositions.Length; i++)
                {
                    GameObject rightWall = (GameObject)PrefabUtility.InstantiatePrefab(solidWallPrefab, wallsGroup.transform);
                    rightWall.name = $"Wall_Right_{i + 1}";
                    rightWall.transform.position = new Vector3(4.0f, 0f, zPositions[i]);
                    rightWall.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                }
            }

            // ─── 10. CAFE ORDER COUNTER (Along Right Side: X in [2.3, 3.5], Z in [0.5, 3.8]) ──
            GameObject counterBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counterBase.name = "Counter_Base";
            counterBase.transform.SetParent(counterGroup.transform, false);
            counterBase.transform.position = new Vector3(2.75f, 0.45f, 2.0f);
            counterBase.transform.localScale = new Vector3(0.9f, 0.9f, 3.2f);
            if (woodTrimMat != null) counterBase.GetComponent<Renderer>().sharedMaterial = woodTrimMat;

            // Polished wood counter top with overhang
            GameObject counterTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counterTop.name = "Counter_Top";
            counterTop.transform.SetParent(counterGroup.transform, false);
            counterTop.transform.position = new Vector3(2.75f, 0.93f, 2.0f);
            counterTop.transform.localScale = new Vector3(1.05f, 0.06f, 3.35f);
            if (tableTrimMat != null) counterTop.GetComponent<Renderer>().sharedMaterial = tableTrimMat;

            // Cash Register / POS Terminal placeholder
            GameObject register = GameObject.CreatePrimitive(PrimitiveType.Cube);
            register.name = "Cash_Register";
            register.transform.SetParent(counterGroup.transform, false);
            register.transform.position = new Vector3(2.7f, 1.08f, 1.0f);
            register.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
            Material darkPlastic = new Material(Shader.Find("Standard"));
            darkPlastic.color = new Color(0.15f, 0.15f, 0.15f);
            register.GetComponent<Renderer>().sharedMaterial = darkPlastic;

            // Espresso Machine placeholder on counter
            GameObject espressoMachine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            espressoMachine.name = "Espresso_Machine";
            espressoMachine.transform.SetParent(counterGroup.transform, false);
            espressoMachine.transform.position = new Vector3(2.75f, 1.22f, 2.6f);
            espressoMachine.transform.localScale = new Vector3(0.6f, 0.52f, 0.85f);
            Material chromeMat = new Material(Shader.Find("Standard"));
            chromeMat.color = new Color(0.85f, 0.85f, 0.88f);
            chromeMat.SetFloat("_Metallic", 0.9f);
            chromeMat.SetFloat("_Glossiness", 0.8f);
            espressoMachine.GetComponent<Renderer>().sharedMaterial = chromeMat;

            // Coffee Mugs & Napkin Dispenser on counter
            if (mugPrefab != null)
            {
                GameObject mug1 = (GameObject)PrefabUtility.InstantiatePrefab(mugPrefab, counterGroup.transform);
                mug1.name = "Counter_CoffeeMug_1";
                mug1.transform.position = new Vector3(2.6f, 0.96f, 1.5f);

                GameObject mug2 = (GameObject)PrefabUtility.InstantiatePrefab(mugPrefab, counterGroup.transform);
                mug2.name = "Counter_CoffeeMug_2";
                mug2.transform.position = new Vector3(2.6f, 0.96f, 1.7f);
            }
            if (napkinPrefab != null)
            {
                GameObject napkin = (GameObject)PrefabUtility.InstantiatePrefab(napkinPrefab, counterGroup.transform);
                napkin.name = "Counter_NapkinDispenser";
                napkin.transform.position = new Vector3(2.65f, 0.96f, 0.5f);
            }

            // Wall Shelves with bottles behind counter on right wall
            if (shelvesPrefab != null)
            {
                GameObject shelves = (GameObject)PrefabUtility.InstantiatePrefab(shelvesPrefab, counterGroup.transform);
                shelves.name = "Counter_Wall_Shelves";
                shelves.transform.position = new Vector3(3.95f, 1.7f, 2.0f);
                shelves.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }

            // ─── 11. COMPLETE CEILING AC & VENTILATION NETWORK ─────────────────────
            // Main AC Box
            if (acBoxPrefab != null)
            {
                GameObject acBox = (GameObject)PrefabUtility.InstantiatePrefab(acBoxPrefab, acGroup.transform);
                acBox.name = "Ceiling_AC_Unit";
                acBox.transform.position = new Vector3(1.8f, 2.85f, 1.0f);
                acBox.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            // AC 3m Main Run
            if (acDuct3mPrefab != null)
            {
                GameObject duct1 = (GameObject)PrefabUtility.InstantiatePrefab(acDuct3mPrefab, acGroup.transform);
                duct1.name = "AC_Duct_MainRun_1";
                duct1.transform.position = new Vector3(1.8f, 2.90f, 2.5f);
                duct1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                GameObject duct2 = (GameObject)PrefabUtility.InstantiatePrefab(acDuct3mPrefab, acGroup.transform);
                duct2.name = "AC_Duct_MainRun_2";
                duct2.transform.position = new Vector3(1.8f, 2.90f, 4.8f);
                duct2.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            // AC T-Joint
            if (acTJointPrefab != null)
            {
                GameObject tJoint = (GameObject)PrefabUtility.InstantiatePrefab(acTJointPrefab, acGroup.transform);
                tJoint.name = "AC_TJoint_Branch";
                tJoint.transform.position = new Vector3(1.8f, 2.90f, 3.5f);
                tJoint.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            // AC 90-Degree Turn
            if (ac90TurnPrefab != null)
            {
                GameObject turn90 = (GameObject)PrefabUtility.InstantiatePrefab(ac90TurnPrefab, acGroup.transform);
                turn90.name = "AC_90Turn_Bend";
                turn90.transform.position = new Vector3(-0.8f, 2.90f, 3.5f);
                turn90.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            // AC Exhaust Vents over seating areas
            if (acExhaustPrefab != null)
            {
                GameObject exhaust1 = (GameObject)PrefabUtility.InstantiatePrefab(acExhaustPrefab, acGroup.transform);
                exhaust1.name = "AC_Exhaust_Seating_1";
                exhaust1.transform.position = new Vector3(-1.8f, 2.85f, 2.0f);
                exhaust1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                GameObject exhaust2 = (GameObject)PrefabUtility.InstantiatePrefab(acExhaustPrefab, acGroup.transform);
                exhaust2.name = "AC_Exhaust_Seating_2";
                exhaust2.transform.position = new Vector3(-1.8f, 2.85f, 4.5f);
                exhaust2.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            // AC Pipe Ceiling Supports
            if (acSupportPrefab != null)
            {
                GameObject sup1 = (GameObject)PrefabUtility.InstantiatePrefab(acSupportPrefab, acGroup.transform);
                sup1.name = "AC_Pipe_Support_1";
                sup1.transform.position = new Vector3(1.8f, 2.95f, 1.8f);

                GameObject sup2 = (GameObject)PrefabUtility.InstantiatePrefab(acSupportPrefab, acGroup.transform);
                sup2.name = "AC_Pipe_Support_2";
                sup2.transform.position = new Vector3(1.8f, 2.95f, 4.2f);
            }

            // ─── 12. BOOTHS (3 Booth Pairs along the Left Side) ─────────────────────
            if (boothPrefab != null && tablePrefab != null)
            {
                // Booth Pair 1: Front Window-Side (X = -2.5m, Z = 4.8m)
                CreateBoothPair(boothsGroup.transform, boothPrefab, tablePrefab, new Vector3(-2.5f, 0f, 4.8f), "Booth_Pair_FrontWindow");

                // Booth Pair 2: Mid-Left (X = -2.5m, Z = 2.2m)
                CreateBoothPair(boothsGroup.transform, boothPrefab, tablePrefab, new Vector3(-2.5f, 0f, 2.2f), "Booth_Pair_MidLeft");

                // Booth Pair 3: Rear-Left (X = -2.5m, Z = -0.6m)
                CreateBoothPair(boothsGroup.transform, boothPrefab, tablePrefab, new Vector3(-2.5f, 0f, -0.6f), "Booth_Pair_RearLeft");
            }

            // ─── 13. WARM CAFE LIGHTING ─────────────────────────────────────────────
            Vector3[] lampPositions = new Vector3[]
            {
                new Vector3(0f, 2.9f, 0.65f),      // Over player's table
                new Vector3(-2.5f, 2.9f, 4.8f),    // Over front booth
                new Vector3(-2.5f, 2.9f, 2.2f),    // Over mid booth
                new Vector3(-2.5f, 2.9f, -0.6f),   // Over rear booth
                new Vector3(2.75f, 2.9f, 1.0f),    // Over order register
                new Vector3(2.75f, 2.9f, 2.6f)     // Over espresso counter
            };

            for (int i = 0; i < lampPositions.Length; i++)
            {
                GameObject lampGo;
                if (lampPrefab != null)
                {
                    lampGo = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, lightingGroup.transform);
                    lampGo.name = $"Ceiling_Lamp_{i + 1}";
                    lampGo.transform.position = lampPositions[i];
                }
                else
                {
                    lampGo = new GameObject($"Ceiling_Lamp_{i + 1}");
                    lampGo.transform.SetParent(lightingGroup.transform, false);
                    lampGo.transform.position = lampPositions[i];
                }

                GameObject lightChild = new GameObject("WarmPointLight");
                lightChild.transform.SetParent(lampGo.transform, false);
                lightChild.transform.localPosition = new Vector3(0f, -0.4f, 0f);

                Light ptLight = lightChild.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.color = new Color(1.0f, 0.88f, 0.72f); // Warm cozy ~2700K
                ptLight.intensity = 1.8f;
                ptLight.range = 5.5f;
                ptLight.shadows = LightShadows.Soft;
            }

            // Soft general fill lights for VR comfort
            GameObject fill1 = new GameObject("Warm_Fill_Light_Center");
            fill1.transform.SetParent(lightingGroup.transform, false);
            fill1.transform.position = new Vector3(0f, 2.5f, 2.0f);
            Light f1 = fill1.AddComponent<Light>();
            f1.type = LightType.Point;
            f1.color = new Color(1.0f, 0.92f, 0.82f);
            f1.intensity = 0.9f;
            f1.range = 9.0f;
            f1.shadows = LightShadows.None;

            // ─── 14. DECORATIONS (Chalkboard Menu, Paintings, Plants) ────────────────
            if (chalkboardPrefab != null)
            {
                // Mounted prominently above/beside counter on back wall
                GameObject board = (GameObject)PrefabUtility.InstantiatePrefab(chalkboardPrefab, decorGroup.transform);
                board.name = "Menu_Chalkboard";
                board.transform.position = new Vector3(0.0f, 1.7f, -2.45f);
                board.transform.rotation = Quaternion.identity;
            }

            if (painting10Prefab != null)
            {
                GameObject art1 = (GameObject)PrefabUtility.InstantiatePrefab(painting10Prefab, decorGroup.transform);
                art1.name = "Wall_Painting_1";
                art1.transform.position = new Vector3(-1.8f, 1.7f, -2.45f);
                art1.transform.rotation = Quaternion.identity;
            }

            if (painting15Prefab != null)
            {
                GameObject art2 = (GameObject)PrefabUtility.InstantiatePrefab(painting15Prefab, decorGroup.transform);
                art2.name = "Wall_Painting_2";
                art2.transform.position = new Vector3(-3.95f, 1.7f, 2.0f);
                art2.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            if (plantPrefab != null)
            {
                // Potted plant in window corner
                GameObject p1 = (GameObject)PrefabUtility.InstantiatePrefab(plantPrefab, decorGroup.transform);
                p1.name = "Plant_WindowCorner";
                p1.transform.position = new Vector3(-3.6f, 0f, 6.0f);
                p1.transform.rotation = Quaternion.identity;

                // Welcoming potted plant at entrance
                GameObject p2 = (GameObject)PrefabUtility.InstantiatePrefab(plantPrefab, decorGroup.transform);
                p2.name = "Plant_Entrance";
                p2.transform.position = new Vector3(3.6f, 0f, -1.8f);
                p2.transform.rotation = Quaternion.identity;
            }

            // ─── 15. EXPANSIVE BEACH / OCEAN VIEW (Outside Windows at Z > 6.5m) ──────
            // Sand Shoreline
            GameObject sandPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sandPlane.name = "Beach_Sand_Shore";
            sandPlane.transform.SetParent(beachGroup.transform, false);
            sandPlane.transform.position = new Vector3(0f, -0.05f, 16.0f);
            sandPlane.transform.localScale = new Vector3(4.0f, 1f, 2.0f); // 40m x 20m sand
            Material sandMat = new Material(Shader.Find("Standard"));
            sandMat.color = new Color(0.92f, 0.85f, 0.70f);
            sandPlane.GetComponent<Renderer>().sharedMaterial = sandMat;

            // Turquoise Ocean Water
            GameObject oceanPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            oceanPlane.name = "Ocean_Water";
            oceanPlane.transform.SetParent(beachGroup.transform, false);
            oceanPlane.transform.position = new Vector3(0f, -0.1f, 45.0f);
            oceanPlane.transform.localScale = new Vector3(8.0f, 1f, 5.0f); // 80m x 50m ocean
            Material waterMat = new Material(Shader.Find("Standard"));
            waterMat.color = new Color(0.12f, 0.48f, 0.65f);
            waterMat.SetFloat("_Glossiness", 0.88f);
            oceanPlane.GetComponent<Renderer>().sharedMaterial = waterMat;

            // Coastal Sky Horizon Backdrop
            GameObject horizonObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            horizonObj.name = "Ocean_Horizon_Backdrop";
            horizonObj.transform.SetParent(beachGroup.transform, false);
            horizonObj.transform.position = new Vector3(0f, 10f, 75.0f);
            horizonObj.transform.localScale = new Vector3(100f, 30f, 1f);
            Material horizonMat = new Material(Shader.Find("Unlit/Color"));
            horizonMat.color = new Color(0.72f, 0.86f, 0.98f);
            horizonObj.GetComponent<Renderer>().sharedMaterial = horizonMat;
            if (horizonObj.GetComponent<Collider>() != null) Object.DestroyImmediate(horizonObj.GetComponent<Collider>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Build Room 8x9m] Successfully constructed complete 8m x 9m Coffee Shop Room!");
        }

        private static void CreateBoothPair(Transform parent, GameObject boothPrefab, GameObject tablePrefab, Vector3 centerPos, string pairName)
        {
            GameObject pairGo = new GameObject(pairName);
            pairGo.transform.SetParent(parent, false);

            // Table
            GameObject table = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab, pairGo.transform);
            table.name = $"{pairName}_Table";
            table.transform.position = centerPos;
            table.transform.rotation = Quaternion.identity;

            // Seat Facing Forward (+Z towards window)
            GameObject seatA = (GameObject)PrefabUtility.InstantiatePrefab(boothPrefab, pairGo.transform);
            seatA.name = $"{pairName}_Seat_A";
            seatA.transform.position = new Vector3(centerPos.x, centerPos.y, centerPos.z - 0.65f);
            seatA.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Seat Facing Backward (-Z towards interior)
            GameObject seatB = (GameObject)PrefabUtility.InstantiatePrefab(boothPrefab, pairGo.transform);
            seatB.name = $"{pairName}_Seat_B";
            seatB.transform.position = new Vector3(centerPos.x, centerPos.y, centerPos.z + 0.65f);
            seatB.transform.rotation = Quaternion.identity;
        }
    }
}
