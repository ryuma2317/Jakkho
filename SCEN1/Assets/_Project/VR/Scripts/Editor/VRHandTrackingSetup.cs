using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.OpenXR;

namespace CADT.VR.Editor
{
    /// <summary>
    /// Automates configuration of OpenXR Hand Tracking, Hand Visual Meshes,
    /// Gestures (Pinch to Click UI, Fist Grab), XR Device Simulator hand support,
    /// and Meta Quest Android manifest permissions.
    /// </summary>
    public static class VRHandTrackingSetup
    {
        private const string HandMaterialPath = "Assets/Samples/XR Interaction Toolkit/3.3.2/Hands Interaction Demo/Materials/Unity_Hand_Light.mat";
        private const string InputActionsPath = "Assets/Samples/XR Interaction Toolkit/3.3.2/Starter Assets/XRI Default Input Actions.inputactions";
        private const string ReticlePrefabPath = "Assets/Prefabs/VR_Cursor_Reticle.prefab";
        private const string AndroidManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";

        [MenuItem("CADT/Setup Full Hand Tracking & Gestures", false, 6)]
        public static void SetupHandTracking()
        {
            Debug.Log("<color=#00F0FF><b>[CADT VR]</b> Setting up Full Hand Tracking, Visuals & Gestures...</color>");

            // 1. Configure OpenXR Features for Hand Tracking
            ConfigureOpenXRFeatures();

            // 2. Ensure Android Manifest permissions
            EnsureAndroidManifest();

            // 3. Ensure Input Action Bindings for XRHandDevice
            ConfigureInputActions();

            // 4. Configure Hand GameObjects in active scene
            ConfigureSceneHands();

            // 5. Configure Simulator Hand Expressions (Pinch, Poke, Grab, Fist, Thumb, Open)
            ConfigureSimulatorExpressions();

            // 6. Save Scene
            Scene activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=#10B981><b>[CADT VR]</b> Full Hand Tracking setup completed and saved successfully!</color>");
        }

        private static void ConfigureOpenXRFeatures()
        {
            try
            {
                var settingsType = typeof(OpenXRSettings);
                var getSettingsMethod = settingsType.GetMethod("GetSettingsForBuildTargetGroup", new Type[] { typeof(BuildTargetGroup) });
                if (getSettingsMethod == null) return;

                BuildTargetGroup[] targetGroups = new BuildTargetGroup[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android };

                foreach (var group in targetGroups)
                {
                    var settings = getSettingsMethod.Invoke(null, new object[] { group }) as OpenXRSettings;
                    if (settings == null) continue;

                    var features = settings.GetFeatures();
                    foreach (var f in features)
                    {
                        string typeName = f.GetType().FullName;
                        if (typeName == "UnityEngine.XR.Hands.OpenXR.HandTracking" ||
                            typeName == "UnityEngine.XR.Hands.OpenXR.MetaHandTrackingAim" ||
                            typeName == "UnityEngine.XR.OpenXR.Features.Interactions.HandInteractionProfile" ||
                            typeName == "UnityEngine.XR.OpenXR.Features.Interactions.HandCommonPosesInteraction")
                        {
                            f.enabled = true;
                            // Set automaticallyInitializeSubsystem if available
                            var autoInitProp = f.GetType().GetProperty("automaticallyInitializeSubsystem");
                            if (autoInitProp != null) autoInitProp.SetValue(f, true, null);
                            EditorUtility.SetDirty(f);
                        }
                    }
                    EditorUtility.SetDirty(settings);
                }
                Debug.Log("<color=#10B981><b>[CADT VR]</b> OpenXR Hand Tracking features enabled for Standalone and Android.</color>");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CADT VR] Could not fully configure OpenXR features via script: " + ex.Message);
            }
        }

        private static void EnsureAndroidManifest()
        {
            string dir = Path.GetDirectoryName(AndroidManifestPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(AndroidManifestPath))
            {
                string manifestContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
    package=""com.unity3d.player""
    xmlns:tools=""http://schemas.android.com/tools"">

    <uses-feature android:name=""android.hardware.vr.headtracking"" android:version=""1"" android:required=""true"" />
    <uses-feature android:name=""oculus.software.handtracking"" android:required=""false"" />
    <uses-permission android:name=""com.oculus.permission.HAND_TRACKING"" />

    <application
        android:label=""@string/app_name""
        android:icon=""@mipmap/app_icon""
        android:allowBackup=""false"">
        <activity
            android:name=""com.unity3d.player.UnityPlayerActivity""
            android:theme=""@style/UnityThemeSelector""
            android:screenOrientation=""landscape""
            android:launchMode=""singleTask""
            android:configChanges=""mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density""
            android:resizeableActivity=""false""
            android:exported=""true"">
            <intent-filter>
                <action android:name=""android.intent.action.MAIN"" />
                <category android:name=""android.intent.category.LAUNCHER"" />
                <category android:name=""com.oculus.intent.category.VR"" />
            </intent-filter>
            <meta-data android:name=""com.oculus.vr.focusaware"" android:value=""true"" />
        </activity>
        <meta-data android:name=""com.oculus.handtracking.frequency"" android:value=""LOW"" />
        <meta-data android:name=""com.oculus.handtracking.version"" android:value=""V2.0"" />
    </application>
</manifest>
";
                File.WriteAllText(AndroidManifestPath, manifestContent);
                AssetDatabase.ImportAsset(AndroidManifestPath);
                Debug.Log("<color=#10B981><b>[CADT VR]</b> Created AndroidManifest.xml with Quest hand tracking tags.</color>");
            }
        }

        private static void ConfigureInputActions()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (asset == null)
            {
                Debug.LogWarning("[CADT VR] InputActionAsset not found at " + InputActionsPath);
                return;
            }

            int addedCount = 0;

            Action<string, string, string> ensureBinding = (mapName, actionName, bindingPath) =>
            {
                var map = asset.FindActionMap(mapName);
                if (map == null) return;
                var action = map.FindAction(actionName);
                if (action == null) return;

                foreach (var b in action.bindings)
                {
                    if (b.path == bindingPath) return;
                }

                InputActionSetupExtensions.AddBinding(action, bindingPath);
                addedCount++;
            };

            // Right Interaction
            ensureBinding("XRI Right Interaction", "UI Press", "<XRHandDevice>{RightHand}/pinchTouched");
            ensureBinding("XRI Right Interaction", "UI Press Value", "<XRHandDevice>{RightHand}/pinchValue");
            ensureBinding("XRI Right Interaction", "Select", "<XRHandDevice>{RightHand}/pinchTouched");
            ensureBinding("XRI Right Interaction", "Select", "<XRHandDevice>{RightHand}/graspFirm");
            ensureBinding("XRI Right Interaction", "Select Value", "<XRHandDevice>{RightHand}/pinchValue");
            ensureBinding("XRI Right Interaction", "Select Value", "<XRHandDevice>{RightHand}/graspValue");

            // Left Interaction
            ensureBinding("XRI Left Interaction", "UI Press", "<XRHandDevice>{LeftHand}/pinchTouched");
            ensureBinding("XRI Left Interaction", "UI Press Value", "<XRHandDevice>{LeftHand}/pinchValue");
            ensureBinding("XRI Left Interaction", "Select", "<XRHandDevice>{LeftHand}/pinchTouched");
            ensureBinding("XRI Left Interaction", "Select", "<XRHandDevice>{LeftHand}/graspFirm");
            ensureBinding("XRI Left Interaction", "Select Value", "<XRHandDevice>{LeftHand}/pinchValue");
            ensureBinding("XRI Left Interaction", "Select Value", "<XRHandDevice>{LeftHand}/graspValue");

            // Aim Position & Rotation
            ensureBinding("XRI Right", "Aim Position", "<XRHandDevice>{RightHand}/aimPosition");
            ensureBinding("XRI Right", "Aim Rotation", "<XRHandDevice>{RightHand}/aimRotation");
            ensureBinding("XRI Left", "Aim Position", "<XRHandDevice>{LeftHand}/aimPosition");
            ensureBinding("XRI Left", "Aim Rotation", "<XRHandDevice>{LeftHand}/aimRotation");

            if (addedCount > 0)
            {
                File.WriteAllText(InputActionsPath, asset.ToJson());
                AssetDatabase.ImportAsset(InputActionsPath);
                Debug.Log("<color=#10B981><b>[CADT VR]</b> Added " + addedCount + " XRHandDevice input bindings.</color>");
            }
        }

        private static void ConfigureSceneHands()
        {
            Material handMat = AssetDatabase.LoadAssetAtPath<Material>(HandMaterialPath);
            GameObject reticlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReticlePrefabPath);

            // Left Hand
            ConfigureSingleHand("Left Hand", "LeftHandQuestVisual", "LeftHandAndroidXRVisual", handMat, reticlePrefab);

            // Right Hand
            ConfigureSingleHand("Right Hand", "RightHandQuestVisual", "RightHandAndroidXRVisual", handMat, reticlePrefab);

            // XR Origin Input Modality Manager
            GameObject rig = GameObject.Find("XR Origin (XR Rig)");
            if (rig != null)
            {
                var modality = rig.GetComponent<XRInputModalityManager>();
                if (modality != null)
                {
                    GameObject leftHand = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Left Hand");
                    GameObject rightHand = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Right Hand");
                    GameObject leftController = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Left Controller");
                    GameObject rightController = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Right Controller");

                    SerializedObject so = new SerializedObject(modality);
                    if (leftHand != null) so.FindProperty("m_LeftHand").objectReferenceValue = leftHand;
                    if (rightHand != null) so.FindProperty("m_RightHand").objectReferenceValue = rightHand;
                    if (leftController != null) so.FindProperty("m_LeftController").objectReferenceValue = leftController;
                    if (rightController != null) so.FindProperty("m_RightController").objectReferenceValue = rightController;
                    so.ApplyModifiedProperties();
                }
            }
        }

        private static void ConfigureSingleHand(string handName, string questVisualName, string androidVisualName, Material handMat, GameObject reticlePrefab)
        {
            string basePath = "XR Origin (XR Rig)/Camera Offset/" + handName;
            GameObject handObj = GameObject.Find(basePath);
            if (handObj == null)
            {
                Debug.LogWarning("[CADT VR] Hand object not found: " + basePath);
                return;
            }

            // 1. Activate Quest Visual and assign material
            Transform questVisual = handObj.transform.Find(questVisualName);
            if (questVisual != null)
            {
                questVisual.gameObject.SetActive(true);
                if (handMat != null)
                {
                    SkinnedMeshRenderer smr = questVisual.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    if (smr != null)
                    {
                        smr.sharedMaterial = handMat;
                        EditorUtility.SetDirty(smr);
                    }
                }
                EditorUtility.SetDirty(questVisual.gameObject);
            }

            // 2. Ensure Android XR Visual is inactive
            Transform androidVisual = handObj.transform.Find(androidVisualName);
            if (androidVisual != null)
            {
                androidVisual.gameObject.SetActive(false);
                EditorUtility.SetDirty(androidVisual.gameObject);
            }

            // 3. Ensure Near-Far Interactor reticle visual is set
            Transform nfiTransform = handObj.transform.Find("Near-Far Interactor");
            if (nfiTransform != null && reticlePrefab != null)
            {
                var reticle = nfiTransform.GetComponent<XRInteractorReticleVisual>();
                if (reticle == null)
                {
                    reticle = nfiTransform.gameObject.AddComponent<XRInteractorReticleVisual>();
                }
                SerializedObject reticleSo = new SerializedObject(reticle);
                var prefabProp = reticleSo.FindProperty("m_ReticlePrefab");
                if (prefabProp != null)
                {
                    prefabProp.objectReferenceValue = reticlePrefab;
                    reticleSo.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(nfiTransform.gameObject);
            }
        }

        private static void ConfigureSimulatorExpressions()
        {
            const string simHandControlsPath = "Assets/Samples/XR Interaction Toolkit/3.3.2/XR Interaction Simulator/XR Interaction Hand Controls.inputactions";
            var subassets = AssetDatabase.LoadAllAssetsAtPath(simHandControlsPath);

            GameObject simObj = GameObject.Find("XR Interaction Simulator");
            if (simObj == null) return;

            var exprMgr = simObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.SimulatedHandExpressionManager>();
            if (exprMgr == null) return;

            SerializedObject so = new SerializedObject(exprMgr);
            SerializedProperty listProp = so.FindProperty("m_SimulatedHandExpressions");
            if (listProp == null) return;

            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty elem = listProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = elem.FindPropertyRelative("m_Name");
                if (nameProp == null) continue;

                string exprName = nameProp.stringValue;
                SerializedProperty toggleProp = elem.FindPropertyRelative("m_ToggleInput");
                SerializedProperty refPerfProp = toggleProp != null ? toggleProp.FindPropertyRelative("m_InputActionReferencePerformed") : null;

                if (refPerfProp != null && refPerfProp.objectReferenceValue == null)
                {
                    foreach (var sub in subassets)
                    {
                        if (sub is InputActionReference && sub.name == "Hands/" + exprName)
                        {
                            refPerfProp.objectReferenceValue = sub;
                            break;
                        }
                    }
                }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(simObj);
            Debug.Log("<color=#10B981><b>[CADT VR]</b> Configured XR Interaction Simulator hand gesture expressions.</color>");
        }
    }
}
