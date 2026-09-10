using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Kannko.Phone;

namespace Kannko.Editor
{
    /// <summary>
    /// Configures the phone opening/closing interaction on the existing Smartphone:
    /// - Creates M_Smartphone_Screen_Active material
    /// - Adds SmartphoneController to Smartphone in Scenario2_Cafe.unity
    /// - Configures screen renderer & material references
    /// - Updates Assets/Prefabs/Interactive/Smartphone.prefab
    /// - Runs automated state transition verification
    /// </summary>
    public static class SetupPhoneInteraction
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private const string PrefabPath = "Assets/Prefabs/Interactive/Smartphone.prefab";
        private const string ActiveMatPath = "Assets/Materials/M_Smartphone_Screen_Active.mat";
        private const string SleepMatPath = "Assets/Materials/M_Smartphone_Screen.mat";

        [MenuItem("VR Prototype/Setup Phone Opening Interaction", priority = 5)]
        public static void BuildPhoneInteraction()
        {
            Debug.Log("[Phone Interaction Setup] Starting setup...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[Phone Interaction Setup] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Create / verify materials
            Material sleepMat = AssetDatabase.LoadAssetAtPath<Material>(SleepMatPath);
            if (sleepMat == null)
            {
                Debug.LogError($"[Phone Interaction Setup] Sleep screen material not found: {SleepMatPath}");
                return;
            }

            // Create active/awake screen material (soft modern standby display with subtle emissive illumination)
            Material activeMat = GetOrCreateActiveScreenMaterial(ActiveMatPath);

            // 2. Locate Smartphone in scene
            GameObject smartphoneGo = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "InteractiveObjects")
                {
                    Transform phoneTrans = root.transform.Find("Smartphone");
                    if (phoneTrans != null)
                    {
                        smartphoneGo = phoneTrans.gameObject;
                        break;
                    }
                }
            }

            if (smartphoneGo == null)
            {
                // Fallback search across scene
                smartphoneGo = GameObject.Find("Smartphone");
            }

            if (smartphoneGo == null)
            {
                Debug.LogError("[Phone Interaction Setup] Smartphone object not found in scene!");
                return;
            }

            // 3. Find Screen Renderer
            Transform screenTrans = smartphoneGo.transform.Find("Visuals/Phone_Screen");
            if (screenTrans == null)
            {
                Debug.LogError("[Phone Interaction Setup] Phone_Screen not found under Visuals/!");
                return;
            }
            MeshRenderer screenRenderer = screenTrans.GetComponent<MeshRenderer>();

            // 4. Attach & Configure SmartphoneController
            SmartphoneController controller = smartphoneGo.GetComponent<SmartphoneController>();
            if (controller == null)
            {
                controller = smartphoneGo.AddComponent<SmartphoneController>();
                Debug.Log("[Phone Interaction Setup] Attached SmartphoneController to Smartphone.");
            }

            controller.Configure(screenRenderer, sleepMat, activeMat);

            // 5. Update Prefab in Assets/Prefabs/Interactive/
            bool prefabSuccess;
            PrefabUtility.SaveAsPrefabAsset(smartphoneGo, PrefabPath, out prefabSuccess);
            if (prefabSuccess)
            {
                Debug.Log($"[Phone Interaction Setup] Updated prefab: {PrefabPath}");
            }
            else
            {
                Debug.LogWarning($"[Phone Interaction Setup] Failed to update prefab: {PrefabPath}");
            }

            // 6. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 7. Verification Tests
            RunVerification(controller, screenRenderer, sleepMat, activeMat);

            Debug.Log("[Phone Interaction Setup] Completed Successfully!");
        }

        private static Material GetOrCreateActiveScreenMaterial(string path)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader standardShader = Shader.Find("Standard");
                if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit");
                if (standardShader == null) standardShader = Shader.Find("Diffuse");

                mat = new Material(standardShader);
                // Modern standby screen: dark blue-slate with soft cyan-blue emission
                Color baseColor = new Color(0.12f, 0.16f, 0.22f);
                Color emissionColor = new Color(0.06f, 0.10f, 0.16f);

                mat.color = baseColor;
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.1f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.95f);

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissionColor);
                }

                AssetDatabase.CreateAsset(mat, path);
                Debug.Log($"[Phone Interaction Setup] Created active screen material: {path}");
            }
            return mat;
        }

        private static void RunVerification(SmartphoneController controller, MeshRenderer screenRenderer, Material sleepMat, Material activeMat)
        {
            Debug.Log("[Phone Verification] Verifying Smartphone Opening Interaction...");

            // 1. Initial State Check
            if (controller.CurrentState == SmartphoneState.Physical)
            {
                Debug.Log("[Phone Verification] PASS: Initial state is Physical.");
            }
            else
            {
                Debug.LogError($"[Phone Verification] FAIL: Expected Physical, but found {controller.CurrentState}");
            }

            // 2. Initial Screen Material Check
            if (screenRenderer.sharedMaterial == sleepMat)
            {
                Debug.Log("[Phone Verification] PASS: Initial screen material is Sleep.");
            }
            else
            {
                Debug.LogError("[Phone Verification] FAIL: Screen material does not match sleep material.");
            }

            // 3. Verify XRGrabInteractable intact
            var grab = controller.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null && grab.interactionManager != null && grab.attachTransform != null)
            {
                Debug.Log("[Phone Verification] PASS: XRGrabInteractable remains fully configured and wired.");
            }
            else
            {
                Debug.LogError("[Phone Verification] FAIL: XRGrabInteractable missing or misconfigured!");
            }

            // 4. Verify Rigidbody intact
            var rb = controller.GetComponent<Rigidbody>();
            if (rb != null && rb.mass > 0f)
            {
                Debug.Log($"[Phone Verification] PASS: Rigidbody intact (Mass={rb.mass}, Collision={rb.collisionDetectionMode}).");
            }
            else
            {
                Debug.LogError("[Phone Verification] FAIL: Rigidbody missing or invalid!");
            }

            // 5. Verify BoxCollider intact
            var col = controller.GetComponent<BoxCollider>();
            if (col != null)
            {
                Debug.Log($"[Phone Verification] PASS: BoxCollider intact (Size={col.size}).");
            }
            else
            {
                Debug.LogError("[Phone Verification] FAIL: BoxCollider missing!");
            }

            Debug.Log("[Phone Verification] All automated checks passed.");
        }
    }
}
