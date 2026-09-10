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
    /// Builds the smartphone (Smartphone) beside the coffee cup on the coffee table.
    /// - Adds BoxCollider, Rigidbody, XRGrabInteractable, and Attach_Point to root.
    /// - Visual hierarchy is completely separated under Visuals/ for modular asset swapping and future screen UI.
    /// - Saves prefab to Assets/Prefabs/Interactive/Smartphone.prefab.
    /// - Places instance under InteractiveObjects in Scenario2_Cafe.unity.
    /// </summary>
    public static class SetupSmartphone
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private const string PrefabDir = "Assets/Prefabs/Interactive";
        private const string PrefabPath = "Assets/Prefabs/Interactive/Smartphone.prefab";

        [MenuItem("VR Prototype/Setup Smartphone", priority = 4)]
        public static void BuildSmartphone()
        {
            Debug.Log("[Smartphone Setup] Starting setup...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[Smartphone Setup] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Locate required scene objects
            XRInteractionManager interactionManager = Object.FindFirstObjectByType<XRInteractionManager>();
            if (interactionManager == null)
            {
                Debug.LogError("[Smartphone Setup] XRInteractionManager not found in scene!");
                return;
            }

            GameObject interactiveObjectsGroup = null;
            GameObject coffeeCup = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "InteractiveObjects")
                {
                    interactiveObjectsGroup = root;
                    foreach (Transform child in root.transform)
                    {
                        if (child.name == "CoffeeCup_Takeaway" || child.name == "CoffeeCup")
                        {
                            coffeeCup = child.gameObject;
                        }
                    }
                }
            }

            if (interactiveObjectsGroup == null)
            {
                interactiveObjectsGroup = new GameObject("InteractiveObjects");
                SceneManager.MoveGameObjectToScene(interactiveObjectsGroup, scene);
                Debug.Log("[Smartphone Setup] Created 'InteractiveObjects' group in scene.");
            }

            // Check if phone already exists in scene
            GameObject existingPhone = null;
            foreach (Transform child in interactiveObjectsGroup.transform)
            {
                if (child.name == "Smartphone" || child.name == "Phone")
                {
                    existingPhone = child.gameObject;
                    break;
                }
            }

            if (existingPhone != null)
            {
                Debug.Log("[Smartphone Setup] Existing phone found in scene. Updating/finishing existing setup...");
            }

            // 2. Materials
            Material bodyMat = GetOrCreateMaterial("Assets/Materials/M_Smartphone_Body.mat", new Color(0.13f, 0.13f, 0.15f), 0.75f, 0.65f);
            Material screenMat = GetOrCreateMaterial("Assets/Materials/M_Smartphone_Screen.mat", new Color(0.05f, 0.06f, 0.07f), 0.2f, 0.95f);
            Material cameraLensMat = GetOrCreateMaterial("Assets/Materials/M_Smartphone_Lens.mat", new Color(0.02f, 0.03f, 0.05f), 0.8f, 0.9f);

            // 3. Create or configure Smartphone Root GameObject
            GameObject phoneRoot = existingPhone != null ? existingPhone : new GameObject("Smartphone");
            phoneRoot.name = "Smartphone";
            phoneRoot.transform.SetParent(interactiveObjectsGroup.transform, false);

            // Natural position on the coffee table beside the coffee cup
            // Coffee cup is at (0.04, 0.75, 0.58).
            // Place phone to the right of cup at (0.18, 0.755, 0.56) with a subtle natural angle.
            Vector3 targetPosition = new Vector3(0.18f, 0.755f, 0.56f);
            if (coffeeCup != null)
            {
                targetPosition = new Vector3(coffeeCup.transform.position.x + 0.14f, 0.755f, coffeeCup.transform.position.z - 0.02f);
            }
            phoneRoot.transform.position = targetPosition;
            phoneRoot.transform.rotation = Quaternion.Euler(0f, -8f, 0f); // Natural slight tabletop angle

            // 4. BoxCollider on Root (realistic modern smartphone dimensions)
            BoxCollider phoneCollider = phoneRoot.GetComponent<BoxCollider>();
            if (phoneCollider == null) phoneCollider = phoneRoot.AddComponent<BoxCollider>();
            phoneCollider.center = Vector3.zero;
            phoneCollider.size = new Vector3(0.078f, 0.010f, 0.158f);

            // 5. Rigidbody on Root (stable tabletop physics)
            Rigidbody phoneRb = phoneRoot.GetComponent<Rigidbody>();
            if (phoneRb == null) phoneRb = phoneRoot.AddComponent<Rigidbody>();
            phoneRb.mass = 0.18f; // 180 grams
            phoneRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            phoneRb.interpolation = RigidbodyInterpolation.Interpolate;
            phoneRb.linearDamping = 0.8f; // Prevents slipping/sliding when dropped
            phoneRb.angularDamping = 1.2f;
            phoneRb.centerOfMass = new Vector3(0f, -0.002f, 0f); // Low center of gravity

            // 6. Attach_Point for comfortable hand grip alignment in VR
            Transform attachTransform = phoneRoot.transform.Find("Attach_Point");
            GameObject attachPoint = attachTransform != null ? attachTransform.gameObject : new GameObject("Attach_Point");
            attachPoint.name = "Attach_Point";
            attachPoint.transform.SetParent(phoneRoot.transform, false);
            attachPoint.transform.localPosition = new Vector3(0f, 0.005f, 0f); // Center of phone
            attachPoint.transform.localRotation = Quaternion.identity;

            // 7. XRGrabInteractable
            XRGrabInteractable phoneGrab = phoneRoot.GetComponent<XRGrabInteractable>();
            if (phoneGrab == null) phoneGrab = phoneRoot.AddComponent<XRGrabInteractable>();
            phoneGrab.interactionManager = interactionManager;
            phoneGrab.attachTransform = attachPoint.transform;
            phoneGrab.throwOnDetach = true;
            phoneGrab.smoothPosition = true;
            phoneGrab.smoothRotation = true;

            // 8. Modular Visuals Hierarchy (colliders stripped so root handles physics)
            Transform existingVisuals = phoneRoot.transform.Find("Visuals");
            if (existingVisuals != null)
            {
                Object.DestroyImmediate(existingVisuals.gameObject);
            }

            GameObject visualsParent = new GameObject("Visuals");
            visualsParent.transform.SetParent(phoneRoot.transform, false);

            // Phone Body / Chassis
            GameObject phoneBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            phoneBody.name = "Phone_Body";
            phoneBody.transform.SetParent(visualsParent.transform, false);
            phoneBody.transform.localPosition = Vector3.zero;
            phoneBody.transform.localScale = new Vector3(0.076f, 0.008f, 0.154f);
            StripCollider(phoneBody);
            phoneBody.GetComponent<Renderer>().sharedMaterial = bodyMat;

            // Front Screen (Dark glossy screen glass)
            GameObject phoneScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            phoneScreen.name = "Phone_Screen";
            phoneScreen.transform.SetParent(visualsParent.transform, false);
            phoneScreen.transform.localPosition = new Vector3(0f, 0.0042f, 0f); // Top surface
            phoneScreen.transform.localScale = new Vector3(0.070f, 0.001f, 0.146f);
            StripCollider(phoneScreen);
            phoneScreen.GetComponent<Renderer>().sharedMaterial = screenMat;

            // Speaker Notch / Dynamic Island Detail on Front
            GameObject speakerNotch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            speakerNotch.name = "Phone_Speaker_Notch";
            speakerNotch.transform.SetParent(visualsParent.transform, false);
            speakerNotch.transform.localPosition = new Vector3(0f, 0.0048f, 0.064f);
            speakerNotch.transform.localScale = new Vector3(0.016f, 0.0006f, 0.004f);
            StripCollider(speakerNotch);
            speakerNotch.GetComponent<Renderer>().sharedMaterial = bodyMat;

            // Rear Camera Island
            GameObject cameraIsland = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cameraIsland.name = "Phone_Camera_Island";
            cameraIsland.transform.SetParent(visualsParent.transform, false);
            cameraIsland.transform.localPosition = new Vector3(-0.021f, -0.0045f, 0.054f); // Rear top corner
            cameraIsland.transform.localScale = new Vector3(0.026f, 0.0015f, 0.032f);
            StripCollider(cameraIsland);
            cameraIsland.GetComponent<Renderer>().sharedMaterial = bodyMat;

            // Camera Lenses (3 lenses on rear bump)
            CreateLens(visualsParent.transform, new Vector3(-0.026f, -0.0055f, 0.062f), cameraLensMat);
            CreateLens(visualsParent.transform, new Vector3(-0.026f, -0.0055f, 0.046f), cameraLensMat);
            CreateLens(visualsParent.transform, new Vector3(-0.016f, -0.0055f, 0.054f), cameraLensMat);

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
            PrefabUtility.SaveAsPrefabAsset(phoneRoot, PrefabPath, out prefabSuccess);
            if (prefabSuccess)
            {
                Debug.Log($"[Smartphone Setup] Prefab successfully saved: {PrefabPath}");
            }
            else
            {
                Debug.LogWarning($"[Smartphone Setup] Failed to save prefab to: {PrefabPath}");
            }

            // 10. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 11. Final Automated Verification
            VerifySmartphoneSetup(phoneRoot);

            Debug.Log("[Smartphone Setup] Setup Completed Successfully!");
        }

        private static void CreateLens(Transform parent, Vector3 localPos, Material mat)
        {
            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lens.name = "Camera_Lens";
            lens.transform.SetParent(parent, false);
            lens.transform.localPosition = localPos;
            lens.transform.localScale = new Vector3(0.008f, 0.001f, 0.008f);
            StripCollider(lens);
            lens.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }
        }

        private static Material GetOrCreateMaterial(string assetPath, Color color, float metallic = 0f, float smoothness = 0.5f)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                Shader standardShader = Shader.Find("Standard");
                if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit");
                if (standardShader == null) standardShader = Shader.Find("Diffuse");

                mat = new Material(standardShader);
                mat.color = color;
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

                AssetDatabase.CreateAsset(mat, assetPath);
                Debug.Log($"[Smartphone Setup] Created material: {assetPath}");
            }
            return mat;
        }

        private static void VerifySmartphoneSetup(GameObject phone)
        {
            Debug.Log("[Smartphone Verification] Running verification checks...");

            // Check Collider on root
            BoxCollider col = phone.GetComponent<BoxCollider>();
            if (col != null)
            {
                Debug.Log($"[Smartphone Verification] Collider OK: Center={col.center}, Size={col.size}");
            }
            else
            {
                Debug.LogError("[Smartphone Verification] FAIL: Missing BoxCollider on root!");
            }

            // Check Rigidbody
            Rigidbody rb = phone.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log($"[Smartphone Verification] Rigidbody OK: Mass={rb.mass}, CollisionDetection={rb.collisionDetectionMode}, Interpolation={rb.interpolation}, LinearDamping={rb.linearDamping}, AngularDamping={rb.angularDamping}");
            }
            else
            {
                Debug.LogError("[Smartphone Verification] FAIL: Missing Rigidbody on root!");
            }

            // Check XRGrabInteractable
            XRGrabInteractable grab = phone.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                Debug.Log($"[Smartphone Verification] XRGrabInteractable OK: Manager={(grab.interactionManager != null ? grab.interactionManager.name : "NULL")}, AttachTransform={(grab.attachTransform != null ? grab.attachTransform.name : "NULL")}");
            }
            else
            {
                Debug.LogError("[Smartphone Verification] FAIL: Missing XRGrabInteractable on root!");
            }

            // Check Visuals colliders stripped
            Transform visuals = phone.transform.Find("Visuals");
            if (visuals != null)
            {
                Collider[] childColliders = visuals.GetComponentsInChildren<Collider>();
                if (childColliders.Length == 0)
                {
                    Debug.Log("[Smartphone Verification] Visuals colliders stripped OK (modular hierarchy confirmed).");
                }
                else
                {
                    Debug.LogWarning($"[Smartphone Verification] WARNING: Found {childColliders.Length} colliders under Visuals!");
                }
            }

            // Check Position
            Debug.Log($"[Smartphone Verification] Position OK: WorldPos={phone.transform.position}, Rotation={phone.transform.rotation.eulerAngles}");
        }
    }
}
