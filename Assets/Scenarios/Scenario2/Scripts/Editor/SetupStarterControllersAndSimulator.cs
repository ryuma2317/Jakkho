using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace Kannko.Editor
{
    /// <summary>
    /// Upgrades XR Origin (VR) in Scenario2_Cafe.unity with the Starter Assets controllers and features:
    /// - High-fidelity 3D UniversalController visuals with animations on both controllers
    /// - Poke Interactor, Near-Far Interactor, Teleport Interactor on both controllers
    /// - Full Locomotion suite: Move (DynamicMoveProvider), Turn (Snap & Continuous), Climb (ClimbProvider), Teleport
    /// - XR Device Simulator integration for full laptop testing without VR hardware
    /// </summary>
    public static class SetupStarterControllersAndSimulator
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";
        private const string StarterRigPath = "Assets/Samples/XR Interaction Toolkit/3.0.7/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        private const string SimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.0.7/XR Device Simulator/XR Device Simulator.prefab";
        private const string InputActionsPath = "Assets/Samples/XR Interaction Toolkit/3.0.7/Starter Assets/XRI Default Input Actions.inputactions";

        [MenuItem("VR Prototype/Upgrade to Starter Assets Controllers & Simulator", priority = 20)]
        public static void Upgrade()
        {
            Debug.Log("[Upgrade Starter Controllers] Starting setup...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[Upgrade Starter Controllers] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Locate XR Origin (VR)
            GameObject xrOriginGo = GameObject.Find("XR Origin (VR)");
            if (xrOriginGo == null)
            {
                Debug.LogError("[Upgrade Starter Controllers] XR Origin (VR) not found!");
                return;
            }

            // 2. Load Starter Assets Rig Prefab as template
            GameObject starterRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterRigPath);
            if (starterRigPrefab == null)
            {
                Debug.LogError($"[Upgrade Starter Controllers] Starter Rig prefab not found: {StarterRigPath}");
                return;
            }

            GameObject tempRig = Object.Instantiate(starterRigPrefab);
            tempRig.name = "TempStarterRig";

            // 3. Locate existing controllers in scene
            Transform cameraOffset = xrOriginGo.transform.Find("Camera Offset");
            Transform leftControllerGo = cameraOffset != null ? cameraOffset.Find("Left Controller") : null;
            Transform rightControllerGo = cameraOffset != null ? cameraOffset.Find("Right Controller") : null;

            Transform tempCameraOffset = tempRig.transform.Find("Camera Offset");
            Transform tempLeftCtrl = tempCameraOffset != null ? tempCameraOffset.Find("Left Controller") : null;
            Transform tempRightCtrl = tempCameraOffset != null ? tempCameraOffset.Find("Right Controller") : null;

            // 4. Upgrade Left Controller Visuals & Interactors
            if (leftControllerGo != null && tempLeftCtrl != null)
            {
                // Remove placeholder sphere ControllerVisual_L
                Transform oldSphere = leftControllerGo.Find("ControllerVisual_L");
                if (oldSphere != null) Object.DestroyImmediate(oldSphere.gameObject);

                Transform existingVisual = leftControllerGo.Find("Left Controller Visual");
                if (existingVisual != null) Object.DestroyImmediate(existingVisual.gameObject);

                // Copy Left Controller Visual from template
                Transform tempVisual = tempLeftCtrl.Find("Left Controller Visual");
                if (tempVisual != null)
                {
                    GameObject newVisual = Object.Instantiate(tempVisual.gameObject, leftControllerGo);
                    newVisual.name = "Left Controller Visual";
                    newVisual.transform.localPosition = tempVisual.localPosition;
                    newVisual.transform.localRotation = tempVisual.localRotation;
                    newVisual.transform.localScale = tempVisual.localScale;
                }

                // Copy Poke Interactor
                Transform existingPoke = leftControllerGo.Find("Poke Interactor");
                if (existingPoke != null) Object.DestroyImmediate(existingPoke.gameObject);
                Transform tempPoke = tempLeftCtrl.Find("Poke Interactor");
                if (tempPoke != null)
                {
                    GameObject newPoke = Object.Instantiate(tempPoke.gameObject, leftControllerGo);
                    newPoke.name = "Poke Interactor";
                    newPoke.transform.localPosition = tempPoke.localPosition;
                    newPoke.transform.localRotation = tempPoke.localRotation;
                }

                // Copy Near-Far Interactor
                Transform existingNearFar = leftControllerGo.Find("Near-Far Interactor");
                if (existingNearFar != null) Object.DestroyImmediate(existingNearFar.gameObject);
                Transform tempNearFar = tempLeftCtrl.Find("Near-Far Interactor");
                if (tempNearFar != null)
                {
                    GameObject newNearFar = Object.Instantiate(tempNearFar.gameObject, leftControllerGo);
                    newNearFar.name = "Near-Far Interactor";
                    newNearFar.transform.localPosition = tempNearFar.localPosition;
                    newNearFar.transform.localRotation = tempNearFar.localRotation;
                }

                // Copy Teleport Interactor
                Transform existingTele = leftControllerGo.Find("Teleport Interactor");
                if (existingTele != null) Object.DestroyImmediate(existingTele.gameObject);
                Transform tempTele = tempLeftCtrl.Find("Teleport Interactor");
                if (tempTele != null)
                {
                    GameObject newTele = Object.Instantiate(tempTele.gameObject, leftControllerGo);
                    newTele.name = "Teleport Interactor";
                    newTele.transform.localPosition = tempTele.localPosition;
                    newTele.transform.localRotation = tempTele.localRotation;
                }
            }

            // 5. Upgrade Right Controller Visuals & Interactors
            if (rightControllerGo != null && tempRightCtrl != null)
            {
                // Remove placeholder sphere ControllerVisual_R
                Transform oldSphere = rightControllerGo.Find("ControllerVisual_R");
                if (oldSphere != null) Object.DestroyImmediate(oldSphere.gameObject);

                Transform existingVisual = rightControllerGo.Find("Right Controller Visual");
                if (existingVisual != null) Object.DestroyImmediate(existingVisual.gameObject);

                // Copy Right Controller Visual from template
                Transform tempVisual = tempRightCtrl.Find("Right Controller Visual");
                if (tempVisual != null)
                {
                    GameObject newVisual = Object.Instantiate(tempVisual.gameObject, rightControllerGo);
                    newVisual.name = "Right Controller Visual";
                    newVisual.transform.localPosition = tempVisual.localPosition;
                    newVisual.transform.localRotation = tempVisual.localRotation;
                    newVisual.transform.localScale = tempVisual.localScale;
                }

                // Copy Poke Interactor
                Transform existingPoke = rightControllerGo.Find("Poke Interactor");
                if (existingPoke != null) Object.DestroyImmediate(existingPoke.gameObject);
                Transform tempPoke = tempRightCtrl.Find("Poke Interactor");
                if (tempPoke != null)
                {
                    GameObject newPoke = Object.Instantiate(tempPoke.gameObject, rightControllerGo);
                    newPoke.name = "Poke Interactor";
                    newPoke.transform.localPosition = tempPoke.localPosition;
                    newPoke.transform.localRotation = tempPoke.localRotation;
                }

                // Copy Near-Far Interactor
                Transform existingNearFar = rightControllerGo.Find("Near-Far Interactor");
                if (existingNearFar != null) Object.DestroyImmediate(existingNearFar.gameObject);
                Transform tempNearFar = tempRightCtrl.Find("Near-Far Interactor");
                if (tempNearFar != null)
                {
                    GameObject newNearFar = Object.Instantiate(tempNearFar.gameObject, rightControllerGo);
                    newNearFar.name = "Near-Far Interactor";
                    newNearFar.transform.localPosition = tempNearFar.localPosition;
                    newNearFar.transform.localRotation = tempNearFar.localRotation;
                }

                // Copy Teleport Interactor
                Transform existingTele = rightControllerGo.Find("Teleport Interactor");
                if (existingTele != null) Object.DestroyImmediate(existingTele.gameObject);
                Transform tempTele = tempRightCtrl.Find("Teleport Interactor");
                if (tempTele != null)
                {
                    GameObject newTele = Object.Instantiate(tempTele.gameObject, rightControllerGo);
                    newTele.name = "Teleport Interactor";
                    newTele.transform.localPosition = tempTele.localPosition;
                    newTele.transform.localRotation = tempTele.localRotation;
                }
            }

            // Copy Gaze & Stabilized origins from tempCameraOffset to match the screenshot 100%
            string[] extraCameraOffsetChildren = new string[]
            {
                "Gaze Interactor",
                "Gaze Stabilized",
                "Left Controller Teleport Stabilized Origin",
                "Right Controller Teleport Stabilized Origin"
            };

            foreach (var childName in extraCameraOffsetChildren)
            {
                Transform src = tempCameraOffset != null ? tempCameraOffset.Find(childName) : null;
                if (src != null)
                {
                    Transform existing = cameraOffset.Find(childName);
                    if (existing != null) Object.DestroyImmediate(existing.gameObject);

                    GameObject newChild = Object.Instantiate(src.gameObject, cameraOffset);
                    newChild.name = childName;
                    newChild.transform.localPosition = src.localPosition;
                    newChild.transform.localRotation = src.localRotation;
                }
            }

            // 6. Copy Locomotion suite (Turn, Move, Climb, Grab Move)
            Transform tempLocomotion = tempRig.transform.Find("Locomotion");
            if (tempLocomotion != null)
            {
                Transform existingLoco = xrOriginGo.transform.Find("Locomotion");
                if (existingLoco != null) Object.DestroyImmediate(existingLoco.gameObject);

                GameObject newLoco = Object.Instantiate(tempLocomotion.gameObject, xrOriginGo.transform);
                newLoco.name = "Locomotion";
                newLoco.transform.localPosition = Vector3.zero;
                newLoco.transform.localRotation = Quaternion.identity;
            }

            // 7. Add InputActionManager on XR Origin to enable Starter Assets actions
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            InputActionManager inputActionMgr = xrOriginGo.GetComponent<InputActionManager>();
            if (inputActionMgr == null) inputActionMgr = xrOriginGo.AddComponent<InputActionManager>();
            if (inputActions != null)
            {
                SerializedObject so = new SerializedObject(inputActionMgr);
                SerializedProperty prop = so.FindProperty("m_ActionAssets");
                if (prop != null)
                {
                    bool alreadyExists = false;
                    for (int i = 0; i < prop.arraySize; i++)
                    {
                        if (prop.GetArrayElementAtIndex(i).objectReferenceValue == inputActions)
                        {
                            alreadyExists = true;
                            break;
                        }
                    }
                    if (!alreadyExists)
                    {
                        prop.arraySize++;
                        prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = inputActions;
                    }
                    so.ApplyModifiedProperties();
                }
            }

            // Clean up temporary template
            Object.DestroyImmediate(tempRig);

            // 8. Place XR Device Simulator into the scene
            GameObject simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (simPrefab != null)
            {
                GameObject existingSim = GameObject.Find("XR Device Simulator");
                if (existingSim != null) Object.DestroyImmediate(existingSim);

                GameObject simInstance = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
                simInstance.name = "XR Device Simulator";
                simInstance.transform.SetParent(null); // Must be a root GameObject for DontDestroyOnLoad!
                Debug.Log("[Upgrade Starter Controllers] XR Device Simulator successfully added as root GameObject!");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Upgrade Starter Controllers] Successfully upgraded controllers and configured XR Device Simulator!");
        }
    }
}
