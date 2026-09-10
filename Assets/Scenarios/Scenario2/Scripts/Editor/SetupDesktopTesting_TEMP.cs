using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using Kannko.Testing;

namespace Kannko.Editor
{
    /// <summary>
    /// Sets up the isolated, temporary desktop testing rig in Scenario2_Cafe.unity:
    /// - Creates DesktopTesting_TEMP GameObject under PlayerArea
    /// - Adds DesktopPlayerController_TEMP (WASD movement + mouse look)
    /// - Adds DesktopGrabController_TEMP (Mouse grabbing + E shortcut for XRGrabInteractables)
    /// - Removes any desktop testing scripts from XR Origin (VR) to keep XR Origin 100% pure VR
    /// - Leaves existing XR Origin, Controllers, Locomotion, and Teleportation untouched
    /// </summary>
    public static class SetupDesktopTesting_TEMP
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

        [MenuItem("VR Prototype/Setup Desktop Testing (TEMP)", priority = 10)]
        public static void Setup()
        {
            Debug.Log("[DesktopTesting Setup] Starting temporary desktop testing setup...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[DesktopTesting Setup] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Locate PlayerArea and XR Origin
            GameObject playerArea = GameObject.Find("PlayerArea");
            GameObject xrOriginGo = GameObject.Find("XR Origin (VR)");

            if (xrOriginGo == null)
            {
                Debug.LogError("[DesktopTesting Setup] XR Origin (VR) not found!");
                return;
            }

            XROrigin xrOrigin = xrOriginGo.GetComponent<XROrigin>();
            Camera mainCam = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;

            // 2. Remove any legacy XRDesktopTestingController on XR Origin (VR) to keep it pure VR
            var legacyController = xrOriginGo.GetComponent<XRDesktopTestingController>();
            if (legacyController != null)
            {
                Object.DestroyImmediate(legacyController);
                Debug.Log("[DesktopTesting Setup] Removed legacy XRDesktopTestingController from XR Origin (VR).");
            }

            // 3. Locate or create DesktopTesting_TEMP GameObject
            Transform parentTransform = playerArea != null ? playerArea.transform : null;
            GameObject desktopRig = GameObject.Find("DesktopTesting_TEMP");

            if (desktopRig == null)
            {
                desktopRig = new GameObject("DesktopTesting_TEMP");
                if (parentTransform != null)
                {
                    desktopRig.transform.SetParent(parentTransform, false);
                }
            }

            // 4. Configure DesktopPlayerController_TEMP
            DesktopPlayerController_TEMP playerCtrl = desktopRig.GetComponent<DesktopPlayerController_TEMP>();
            if (playerCtrl == null)
            {
                playerCtrl = desktopRig.AddComponent<DesktopPlayerController_TEMP>();
            }
            playerCtrl.Configure(xrOrigin, mainCam);

            // 5. Configure DesktopGrabController_TEMP
            DesktopGrabController_TEMP grabCtrl = desktopRig.GetComponent<DesktopGrabController_TEMP>();
            if (grabCtrl == null)
            {
                grabCtrl = desktopRig.AddComponent<DesktopGrabController_TEMP>();
            }
            grabCtrl.Configure(mainCam);

            // 6. Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[DesktopTesting Setup] DesktopTesting_TEMP successfully configured and scene saved!");
        }
    }
}
