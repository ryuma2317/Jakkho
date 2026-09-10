using System;
using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CADT.VR.Editor
{
    /// <summary>
    /// Configures VR laser pointer beam & cursor reticle interaction for VR Controllers,
    /// VR Hand Tracking, and the XR Interaction Simulator.
    /// </summary>
    public static class VRUIInteractionSetup
    {
        private const string ReticlePrefabPath = "Assets/Prefabs/VR_Cursor_Reticle.prefab";
        private const string ReticleMaterialPath = "Assets/Materials/M_VR_Cursor_Reticle.mat";
        private const string UIInputModulePresetPath = "Assets/Samples/XR Interaction Toolkit/3.3.2/Starter Assets/Presets/XRI Default XR UI Input Module.preset";

        [MenuItem("CADT/Setup VR Cursor & UI Interaction", false, 5)]
        public static void ConfigureVRUIInteraction()
        {
            Debug.Log("<color=#00F0FF><b>[CADT VR]</b> Configuring VR Laser Pointer Beam & Cursor Reticle for Controllers & Hands...</color>");

            // 1. Ensure Reticle Material & Prefab exist
            GameObject reticlePrefab = EnsureReticlePrefab();

            // 2. Configure EventSystem with XRUIInputModule
            ConfigureEventSystem();

            // 3. Clean up conflicting legacy components on Right Controller
            CleanUpRightControllerRoot();

            // 4. Configure Near-Far Interactors on Controllers & Hands
            ConfigureInteractor("XR Origin (XR Rig)/Camera Offset/Right Controller/Near-Far Interactor", reticlePrefab, isRightHand: true);
            ConfigureInteractor("XR Origin (XR Rig)/Camera Offset/Left Controller/Near-Far Interactor", reticlePrefab, isRightHand: false);
            ConfigureInteractor("XR Origin (XR Rig)/Camera Offset/Right Hand/Near-Far Interactor", reticlePrefab, isRightHand: true);
            ConfigureInteractor("XR Origin (XR Rig)/Camera Offset/Left Hand/Near-Far Interactor", reticlePrefab, isRightHand: false);

            // 5. Verify World Space Canvases
            ConfigureCanvases();

            // 6. Save Scene
            Scene activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=#10B981><b>[CADT VR]</b> VR Cursor & UI Interaction successfully configured and saved!</color>");
        }

        private static GameObject EnsureReticlePrefab()
        {
            if (!Directory.Exists("Assets/Prefabs"))
            {
                Directory.CreateDirectory("Assets/Prefabs");
                AssetDatabase.Refresh();
            }

            if (!Directory.Exists("Assets/Materials"))
            {
                Directory.CreateDirectory("Assets/Materials");
                AssetDatabase.Refresh();
            }

            // Material
            Material reticleMat = AssetDatabase.LoadAssetAtPath<Material>(ReticleMaterialPath);
            if (reticleMat == null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") 
                                  ?? Shader.Find("Sprites/Default") 
                                  ?? Shader.Find("Unlit/Color");

                reticleMat = new Material(unlitShader);
                reticleMat.name = "M_VR_Cursor_Reticle";
                // Warm Amber Gold matching the reference image hit indicator
                reticleMat.color = new Color(1.0f, 0.85f, 0.25f, 0.95f);
                AssetDatabase.CreateAsset(reticleMat, ReticleMaterialPath);
                AssetDatabase.SaveAssets();
            }

            GameObject reticlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReticlePrefabPath);
            if (reticlePrefab == null)
            {
                // Create clean cursor reticle object: center dot + flat disc
                GameObject root = new GameObject("VR_Cursor_Reticle");

                // Inner glowing dot
                GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.name = "Cursor_Dot";
                dot.transform.SetParent(root.transform, false);
                dot.transform.localScale = new Vector3(0.018f, 0.018f, 0.005f); // Flat disc-like dot
                Collider dotCol = dot.GetComponent<Collider>();
                if (dotCol != null) UnityEngine.Object.DestroyImmediate(dotCol);

                Renderer dotRend = dot.GetComponent<Renderer>();
                if (dotRend != null) dotRend.sharedMaterial = reticleMat;

                // Outer subtle ring
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Cursor_Outer_Ring";
                ring.transform.SetParent(root.transform, false);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.transform.localScale = new Vector3(0.032f, 0.0005f, 0.032f);
                Collider ringCol = ring.GetComponent<Collider>();
                if (ringCol != null) UnityEngine.Object.DestroyImmediate(ringCol);

                Renderer ringRend = ring.GetComponent<Renderer>();
                if (ringRend != null) ringRend.sharedMaterial = reticleMat;

                reticlePrefab = PrefabUtility.SaveAsPrefabAsset(root, ReticlePrefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=#10B981>[CADT VR] Created VR_Cursor_Reticle prefab.</color>");
            }

            return reticlePrefab;
        }

        private static void ConfigureEventSystem()
        {
            EventSystem es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
                es = esObj.GetComponent<EventSystem>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // Remove legacy StandaloneInputModule if present
            StandaloneInputModule sim = es.GetComponent<StandaloneInputModule>();
            if (sim != null)
            {
                Undo.DestroyObjectImmediate(sim);
                Debug.Log("<color=#F59E0B>[CADT VR] Removed legacy StandaloneInputModule from EventSystem.</color>");
            }

            // Add or retrieve XRUIInputModule
            XRUIInputModule xrModule = es.GetComponent<XRUIInputModule>();
            if (xrModule == null)
            {
                xrModule = Undo.AddComponent<XRUIInputModule>(es.gameObject);
                Debug.Log("<color=#10B981>[CADT VR] Added XRUIInputModule to EventSystem.</color>");
            }

            // Apply Starter Asset preset to wire all Input Actions (point, click, scroll, navigate)
            Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(UIInputModulePresetPath);
            if (preset != null && preset.CanBeAppliedTo(xrModule))
            {
                preset.ApplyTo(xrModule);
                Debug.Log("<color=#10B981>[CADT VR] Applied XRI Default XR UI Input Module preset to EventSystem.</color>");
            }
            else
            {
                Debug.LogWarning("[CADT VR] Could not find or apply preset: " + UIInputModulePresetPath);
            }

            xrModule.enableXRInput = true;
            xrModule.enableMouseInput = true;
            EditorUtility.SetDirty(es);
        }

        private static void CleanUpRightControllerRoot()
        {
            GameObject rightCtrl = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Right Controller");
            if (rightCtrl == null) return;

            // Remove unassigned VRPointerBeam if present
            Component pointerBeam = rightCtrl.GetComponent("VRPointerBeam");
            if (pointerBeam != null)
            {
                Undo.DestroyObjectImmediate(pointerBeam);
                Debug.Log("<color=#F59E0B>[CADT VR] Removed outdated VRPointerBeam from Right Controller root.</color>");
            }

            // Remove orphaned 1.0-width LineRenderer if present on root
            LineRenderer lr = rightCtrl.GetComponent<LineRenderer>();
            if (lr != null)
            {
                Undo.DestroyObjectImmediate(lr);
                Debug.Log("<color=#F59E0B>[CADT VR] Removed orphaned LineRenderer from Right Controller root.</color>");
            }

            // Remove orphan Pointer_Reticle child if present on root
            Transform oldReticle = rightCtrl.transform.Find("Pointer_Reticle");
            if (oldReticle != null)
            {
                Undo.DestroyObjectImmediate(oldReticle.gameObject);
            }

            EditorUtility.SetDirty(rightCtrl);
        }

        private static void ConfigureInteractor(string hierarchyPath, GameObject reticlePrefab, bool isRightHand)
        {
            GameObject obj = GameObject.Find(hierarchyPath);
            if (obj == null)
            {
                Debug.LogWarning($"[CADT VR] Interactor at path '{hierarchyPath}' not found.");
                return;
            }

            // 1. Configure NearFarInteractor
            NearFarInteractor interactor = obj.GetComponent<NearFarInteractor>();
            if (interactor != null)
            {
                interactor.enableUIInteraction = true;
                interactor.blockUIOnInteractableSelection = false; // Allow continuing UI interaction smoothly
                EditorUtility.SetDirty(interactor);
            }

            // 2. Configure / Add XRInteractorReticleVisual
            XRInteractorReticleVisual reticleVisual = obj.GetComponent<XRInteractorReticleVisual>();
            if (reticleVisual == null)
            {
                reticleVisual = Undo.AddComponent<XRInteractorReticleVisual>(obj);
            }

            if (reticlePrefab != null)
            {
                reticleVisual.reticlePrefab = reticlePrefab;
            }
            reticleVisual.alignPrefabWithSurfaceNormal = true;
            reticleVisual.drawWhileSelecting = true;
            reticleVisual.drawOnNoHit = false;
            reticleVisual.prefabScalingFactor = 1.0f;
            reticleVisual.endpointSmoothingTime = 0.02f;
            EditorUtility.SetDirty(reticleVisual);

            // 3. Configure LineVisual curve & colors
            Transform lineVisTrans = obj.transform.Find("LineVisual");
            if (lineVisTrans != null)
            {
                CurveVisualController cvc = lineVisTrans.GetComponent<CurveVisualController>();
                if (cvc != null)
                {
                    cvc.renderLineInWorldSpace = true;
                    cvc.customizeLinePropertiesForState = true;
                    EditorUtility.SetDirty(cvc);
                }

                LineRenderer lr = lineVisTrans.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    // Ensure the material is unlit for crisp glow in VR
                    if (lr.sharedMaterial == null || lr.sharedMaterial.name == "Default-Line")
                    {
                        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") 
                                          ?? Shader.Find("Sprites/Default") 
                                          ?? Shader.Find("Unlit/Color");
                        Material beamMat = new Material(unlitShader);
                        beamMat.name = "M_VR_Pointer_Beam";
                        beamMat.color = new Color(1.0f, 0.85f, 0.25f, 0.85f);
                        lr.sharedMaterial = beamMat;
                    }
                    EditorUtility.SetDirty(lr);
                }
            }

            EditorUtility.SetDirty(obj);
            Debug.Log($"<color=#10B981>[CADT VR] Interactor '{obj.name}' configured with Reticle Visual & UI interaction.</color>");
        }

        private static void ConfigureCanvases()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = UnityEngine.Object.FindFirstObjectByType<Camera>();

            // 1. Diegetic Spatial Dashboard
            GameObject dashboard = GameObject.Find("CADT_Office_Environment/Diegetic_Spatial_Dashboard");
            if (dashboard != null)
            {
                Canvas canvas = dashboard.GetComponent<Canvas>();
                if (canvas != null && mainCam != null)
                {
                    canvas.worldCamera = mainCam;
                }

                TrackedDeviceGraphicRaycaster trRaycaster = dashboard.GetComponent<TrackedDeviceGraphicRaycaster>();
                if (trRaycaster == null)
                {
                    trRaycaster = Undo.AddComponent<TrackedDeviceGraphicRaycaster>(dashboard);
                }
                // canvas.worldCamera automatically governs eventCamera

                EditorUtility.SetDirty(dashboard);
                Debug.Log("<color=#10B981>[CADT VR] Diegetic Spatial Dashboard Canvas verified with TrackedDeviceGraphicRaycaster.</color>");
            }

            // 2. Primary Monitor Canvas
            GameObject monitorCanvas = GameObject.Find("CADT_Office_Environment/Workstation_Setup/Primary_Monitor_Setup/Monitor_Screen_Canvas");
            if (monitorCanvas != null)
            {
                Canvas canvas = monitorCanvas.GetComponent<Canvas>();
                if (canvas != null && mainCam != null)
                {
                    canvas.worldCamera = mainCam;
                }

                TrackedDeviceGraphicRaycaster trRaycaster = monitorCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();
                if (trRaycaster == null)
                {
                    trRaycaster = Undo.AddComponent<TrackedDeviceGraphicRaycaster>(monitorCanvas);
                }
                // canvas.worldCamera automatically governs eventCamera

                EditorUtility.SetDirty(monitorCanvas);
                Debug.Log("<color=#10B981>[CADT VR] Monitor Screen Canvas verified with TrackedDeviceGraphicRaycaster.</color>");
            }
        }
    }
}
