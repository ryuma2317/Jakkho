using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Kannko.Editor
{
    /// <summary>
    /// Builds the personal bag (LaptopBag) beside the player's chair.
    /// - Adds BoxCollider, Rigidbody, XRGrabInteractable, and Attach_Point to root.
    /// - Visual hierarchy is completely separated under Visuals/ for modular asset swapping.
    /// - Saves prefab to Assets/Prefabs/Interactive/LaptopBag.prefab.
    /// - Places instance under InteractiveObjects in Scenario2_Cafe.unity.
    /// </summary>
    public static class SetupPersonalBag
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private const string PrefabDir = "Assets/Prefabs/Interactive";
        private const string PrefabPath = "Assets/Prefabs/Interactive/LaptopBag.prefab";

        [MenuItem("VR Prototype/Setup Personal Bag", priority = 3)]
        public static void BuildLaptopBag()
        {
            Debug.Log("[Personal Bag] Starting setup...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[Personal Bag] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Locate required scene objects
            XRInteractionManager interactionManager = Object.FindFirstObjectByType<XRInteractionManager>();
            if (interactionManager == null)
            {
                Debug.LogError("[Personal Bag] XRInteractionManager not found in scene!");
                return;
            }

            GameObject interactiveObjectsGroup = null;
            GameObject playerChair = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "InteractiveObjects")
                {
                    interactiveObjectsGroup = root;
                }
                if (root.name == "PlayerArea")
                {
                    foreach (Transform child in root.transform)
                    {
                        if (child.name == "CoffeeShop_Chair")
                        {
                            playerChair = child.gameObject;
                        }
                    }
                }
            }

            if (interactiveObjectsGroup == null)
            {
                interactiveObjectsGroup = new GameObject("InteractiveObjects");
                SceneManager.MoveGameObjectToScene(interactiveObjectsGroup, scene);
                Debug.Log("[Personal Bag] Created 'InteractiveObjects' group in scene.");
            }

            // Check if bag already exists in scene
            GameObject existingBag = null;
            foreach (Transform child in interactiveObjectsGroup.transform)
            {
                if (child.name == "LaptopBag" || child.name == "PersonalBag")
                {
                    existingBag = child.gameObject;
                    break;
                }
            }

            if (existingBag != null)
            {
                Debug.Log("[Personal Bag] Existing bag found in scene. Updating/finishing existing setup...");
            }

            // 2. Materials
            Material bagLeatherMat = GetOrCreateMaterial("Assets/Materials/M_LaptopBag_Leather.mat", new Color(0.16f, 0.16f, 0.18f));
            Material bagStrapMat = GetOrCreateMaterial("Assets/Materials/M_LaptopBag_Strap.mat", new Color(0.11f, 0.11f, 0.13f));
            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_MetalDark.mat");
            if (metalMat == null)
            {
                metalMat = GetOrCreateMaterial("Assets/Materials/M_MetalDark.mat", new Color(0.25f, 0.27f, 0.30f));
            }

            // 3. Create or configure Bag Root GameObject
            GameObject bagRoot = existingBag != null ? existingBag : new GameObject("LaptopBag");
            bagRoot.name = "LaptopBag";
            bagRoot.transform.SetParent(interactiveObjectsGroup.transform, false);

            // Natural position beside the chair (chair is at 0,0,0, player sits facing +Z)
            // Placed at player's right-hand side on the floor (x = 0.42, y = 0.0, z = 0.06)
            Vector3 targetPosition = new Vector3(0.42f, 0.0f, 0.06f);
            if (playerChair != null)
            {
                // Align relative to player chair transform
                targetPosition = playerChair.transform.position + new Vector3(0.42f, 0.0f, 0.06f);
            }
            bagRoot.transform.position = targetPosition;
            bagRoot.transform.rotation = Quaternion.Euler(0f, -12f, 0f); // slight natural tilt

            // 4. BoxCollider on Root (encapsulates the bag body & handle)
            BoxCollider bagCollider = bagRoot.GetComponent<BoxCollider>();
            if (bagCollider == null) bagCollider = bagRoot.AddComponent<BoxCollider>();
            bagCollider.center = new Vector3(0f, 0.15f, 0f);
            bagCollider.size = new Vector3(0.36f, 0.30f, 0.14f);

            // 5. Rigidbody on Root (stable personal belonging physics)
            Rigidbody bagRb = bagRoot.GetComponent<Rigidbody>();
            if (bagRb == null) bagRb = bagRoot.AddComponent<Rigidbody>();
            bagRb.mass = 1.5f;
            bagRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            bagRb.interpolation = RigidbodyInterpolation.Interpolate;
            bagRb.linearDamping = 0.6f;
            bagRb.angularDamping = 1.5f;
            bagRb.centerOfMass = new Vector3(0f, -0.05f, 0f); // Low center of gravity to stay upright on floor

            // 6. Attach_Point for sensible grip alignment in VR
            Transform attachTransform = bagRoot.transform.Find("Attach_Point");
            GameObject attachPoint = attachTransform != null ? attachTransform.gameObject : new GameObject("Attach_Point");
            attachPoint.name = "Attach_Point";
            attachPoint.transform.SetParent(bagRoot.transform, false);
            attachPoint.transform.localPosition = new Vector3(0f, 0.30f, 0f); // Top handle grip position
            attachPoint.transform.localRotation = Quaternion.identity;

            // 7. XRGrabInteractable
            XRGrabInteractable bagGrab = bagRoot.GetComponent<XRGrabInteractable>();
            if (bagGrab == null) bagGrab = bagRoot.AddComponent<XRGrabInteractable>();
            bagGrab.interactionManager = interactionManager;
            bagGrab.attachTransform = attachPoint.transform;
            bagGrab.throwOnDetach = true;
            bagGrab.smoothPosition = true;
            bagGrab.smoothRotation = true;

            // 8. Modular Visuals Hierarchy (colliders stripped so root handles physics)
            Transform existingVisuals = bagRoot.transform.Find("Visuals");
            if (existingVisuals != null)
            {
                Object.DestroyImmediate(existingVisuals.gameObject);
            }

            GameObject visualsParent = new GameObject("Visuals");
            visualsParent.transform.SetParent(bagRoot.transform, false);

            // Main Bag Body (Messenger/briefcase style)
            GameObject bagBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bagBody.name = "Bag_Body";
            bagBody.transform.SetParent(visualsParent.transform, false);
            bagBody.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            bagBody.transform.localScale = new Vector3(0.35f, 0.24f, 0.11f);
            StripCollider(bagBody);
            bagBody.GetComponent<Renderer>().sharedMaterial = bagLeatherMat;

            // Front Flap / Cover
            GameObject bagFlap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bagFlap.name = "Bag_Flap";
            bagFlap.transform.SetParent(visualsParent.transform, false);
            bagFlap.transform.localPosition = new Vector3(0f, 0.15f, 0.058f);
            bagFlap.transform.localScale = new Vector3(0.352f, 0.15f, 0.015f);
            StripCollider(bagFlap);
            bagFlap.GetComponent<Renderer>().sharedMaterial = bagLeatherMat;

            // Front Accent Pocket
            GameObject bagPocket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bagPocket.name = "Bag_Pocket";
            bagPocket.transform.SetParent(visualsParent.transform, false);
            bagPocket.transform.localPosition = new Vector3(0f, 0.055f, 0.06f);
            bagPocket.transform.localScale = new Vector3(0.28f, 0.07f, 0.015f);
            StripCollider(bagPocket);
            bagPocket.GetComponent<Renderer>().sharedMaterial = bagStrapMat;

            // Handle Post Left
            GameObject handleL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleL.name = "Bag_Handle_Post_L";
            handleL.transform.SetParent(visualsParent.transform, false);
            handleL.transform.localPosition = new Vector3(-0.07f, 0.27f, 0f);
            handleL.transform.localScale = new Vector3(0.015f, 0.03f, 0.015f);
            StripCollider(handleL);
            handleL.GetComponent<Renderer>().sharedMaterial = bagStrapMat;

            // Handle Post Right
            GameObject handleR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleR.name = "Bag_Handle_Post_R";
            handleR.transform.SetParent(visualsParent.transform, false);
            handleR.transform.localPosition = new Vector3(0.07f, 0.27f, 0f);
            handleR.transform.localScale = new Vector3(0.015f, 0.03f, 0.015f);
            StripCollider(handleR);
            handleR.GetComponent<Renderer>().sharedMaterial = bagStrapMat;

            // Top Handle Grip
            GameObject handleGrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handleGrip.name = "Bag_Handle_Grip";
            handleGrip.transform.SetParent(visualsParent.transform, false);
            handleGrip.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            handleGrip.transform.localScale = new Vector3(0.16f, 0.018f, 0.024f);
            StripCollider(handleGrip);
            handleGrip.GetComponent<Renderer>().sharedMaterial = bagLeatherMat;

            // Metallic Buckle Details
            GameObject buckleL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buckleL.name = "Bag_Buckle_L";
            buckleL.transform.SetParent(visualsParent.transform, false);
            buckleL.transform.localPosition = new Vector3(-0.085f, 0.08f, 0.068f);
            buckleL.transform.localScale = new Vector3(0.022f, 0.035f, 0.008f);
            StripCollider(buckleL);
            buckleL.GetComponent<Renderer>().sharedMaterial = metalMat;

            GameObject buckleR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buckleR.name = "Bag_Buckle_R";
            buckleR.transform.SetParent(visualsParent.transform, false);
            buckleR.transform.localPosition = new Vector3(0.085f, 0.08f, 0.068f);
            buckleR.transform.localScale = new Vector3(0.022f, 0.035f, 0.008f);
            StripCollider(buckleR);
            buckleR.GetComponent<Renderer>().sharedMaterial = metalMat;

            // 9. Save as Prefab in Assets/Prefabs/Interactive/
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Interactive"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/Prefabs", "Interactive");
            }

            bool prefabSuccess;
            PrefabUtility.SaveAsPrefabAsset(bagRoot, PrefabPath, out prefabSuccess);
            if (prefabSuccess)
            {
                Debug.Log($"[Personal Bag] Prefab successfully saved: {PrefabPath}");
            }
            else
            {
                Debug.LogWarning($"[Personal Bag] Failed to save prefab to: {PrefabPath}");
            }

            // 10. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 11. Final Automated Verification
            VerifyBagSetup(bagRoot);

            Debug.Log("[Personal Bag] Setup Completed Successfully!");
        }

        private static void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }
        }

        private static Material GetOrCreateMaterial(string assetPath, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                Shader standardShader = Shader.Find("Standard");
                if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit");
                if (standardShader == null) standardShader = Shader.Find("Diffuse");

                mat = new Material(standardShader);
                mat.color = color;
                AssetDatabase.CreateAsset(mat, assetPath);
                Debug.Log($"[Personal Bag] Created material: {assetPath}");
            }
            return mat;
        }

        private static void VerifyBagSetup(GameObject bag)
        {
            Debug.Log("[Personal Bag] Running verification checks...");

            // Check Collider on root
            BoxCollider col = bag.GetComponent<BoxCollider>();
            if (col != null)
            {
                Debug.Log($"[Personal Bag Verification] Collider OK: Center={col.center}, Size={col.size}");
            }
            else
            {
                Debug.LogError("[Personal Bag Verification] FAIL: Missing BoxCollider on root!");
            }

            // Check Rigidbody
            Rigidbody rb = bag.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log($"[Personal Bag Verification] Rigidbody OK: Mass={rb.mass}, CollisionDetection={rb.collisionDetectionMode}, Interpolation={rb.interpolation}, LinearDamping={rb.linearDamping}, AngularDamping={rb.angularDamping}");
            }
            else
            {
                Debug.LogError("[Personal Bag Verification] FAIL: Missing Rigidbody on root!");
            }

            // Check XRGrabInteractable
            XRGrabInteractable grab = bag.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                Debug.Log($"[Personal Bag Verification] XRGrabInteractable OK: Manager={(grab.interactionManager != null ? grab.interactionManager.name : "NULL")}, AttachTransform={(grab.attachTransform != null ? grab.attachTransform.name : "NULL")}");
            }
            else
            {
                Debug.LogError("[Personal Bag Verification] FAIL: Missing XRGrabInteractable on root!");
            }

            // Check Visuals colliders stripped
            Transform visuals = bag.transform.Find("Visuals");
            if (visuals != null)
            {
                Collider[] childColliders = visuals.GetComponentsInChildren<Collider>();
                if (childColliders.Length == 0)
                {
                    Debug.Log("[Personal Bag Verification] Visuals colliders stripped OK (modular hierarchy confirmed).");
                }
                else
                {
                    Debug.LogWarning($"[Personal Bag Verification] WARNING: Found {childColliders.Length} colliders under Visuals!");
                }
            }

            // Check Position
            Debug.Log($"[Personal Bag Verification] Position OK: WorldPos={bag.transform.position}, Rotation={bag.transform.rotation.eulerAngles}");
        }
    }
}
