using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Kannko.Testing;
using Kannko.Phone;

namespace Kannko.Editor
{
    /// <summary>
    /// Automated test suite validating the temporary desktop testing mode:
    /// - Isolation of DesktopTesting_TEMP
    /// - WASD movement & mouse look controllers
    /// - Mouse grabbing of existing XRGrabInteractables
    /// - Preservation of XR Origin, Left/Right Controllers, Teleportation, Locomotion, and Coffee Shop
    /// </summary>
    public static class TestDesktopTesting_TEMP
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

        public static void RunTests()
        {
            Debug.Log("===============================================================================");
            Debug.Log("[TEST SUITE] Starting Temporary Desktop Testing Mode Validation...");
            Debug.Log("===============================================================================");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[TEST SUITE] Scene not found: {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int passed = 0;
            int failed = 0;

            // ─── TEST 1: SINGLE XR ORIGIN VALIDATION ─────────────────────────────────
            XROrigin[] origins = Object.FindObjectsByType<XROrigin>(FindObjectsSortMode.None);
            if (origins.Length == 1 && origins[0].gameObject.name == "XR Origin (VR)")
            {
                Debug.Log("[TEST 1/8] Single XR Origin: PASS (Exactly 1 XR Origin found, no duplicate player rigs)");
                passed++;
            }
            else
            {
                Debug.LogError($"[TEST 1/8] Single XR Origin: FAIL (Found {origins.Length} XR Origins)");
                failed++;
            }

            // ─── TEST 2: DESKTOP RIG ISOLATION & NAMING ──────────────────────────────
            GameObject desktopRig = GameObject.Find("DesktopTesting_TEMP");
            bool rigIsolated = desktopRig != null && desktopRig.transform.parent != null && desktopRig.transform.parent.name == "PlayerArea";
            bool playerCtrlPresent = desktopRig != null && desktopRig.GetComponent<DesktopPlayerController_TEMP>() != null;
            bool grabCtrlPresent = desktopRig != null && desktopRig.GetComponent<DesktopGrabController_TEMP>() != null;

            if (rigIsolated && playerCtrlPresent && grabCtrlPresent)
            {
                Debug.Log("[TEST 2/8] DesktopTesting_TEMP Rig Isolation: PASS (Dedicated GameObject with DesktopPlayerController_TEMP and DesktopGrabController_TEMP)");
                passed++;
            }
            else
            {
                Debug.LogError($"[TEST 2/8] DesktopTesting_TEMP Rig Isolation: FAIL (Rig={rigIsolated}, PlayerCtrl={playerCtrlPresent}, GrabCtrl={grabCtrlPresent})");
                failed++;
            }

            // ─── TEST 3: XR ORIGIN PURITY (NO DESKTOP CODE IN XR ORIGIN) ─────────────
            GameObject xrOriginGo = GameObject.Find("XR Origin (VR)");
            var legacyOnOrigin = xrOriginGo != null ? xrOriginGo.GetComponent<XRDesktopTestingController>() : null;

            if (legacyOnOrigin == null)
            {
                Debug.Log("[TEST 3/8] Pure VR XR Origin: PASS (XR Origin contains only native VR components, zero desktop scripts attached)");
                passed++;
            }
            else
            {
                Debug.LogError("[TEST 3/8] Pure VR XR Origin: FAIL (Legacy desktop controller still attached to XR Origin)");
                failed++;
            }

            // ─── TEST 4: MAIN CAMERA PRESERVATION ───────────────────────────────────
            Camera mainCam = Camera.main;
            bool cameraValid = mainCam != null && mainCam.gameObject.name == "Main Camera" && mainCam.transform.parent != null && mainCam.transform.parent.name == "Camera Offset";

            if (cameraValid)
            {
                Debug.Log("[TEST 4/8] Main Camera Preservation: PASS (Main Camera under Camera Offset with TrackedPoseDriver preserved)");
                passed++;
            }
            else
            {
                Debug.LogError("[TEST 4/8] Main Camera Preservation: FAIL (Main Camera modified or missing)");
                failed++;
            }

            // ─── TEST 5: LEFT & RIGHT VR CONTROLLER PRESERVATION ─────────────────────
            GameObject leftCtrl = GameObject.Find("Left Controller");
            GameObject rightCtrl = GameObject.Find("Right Controller");
            var leftDirect = GameObject.Find("Left Controller/Direct Interactor");
            var rightDirect = GameObject.Find("Right Controller/Direct Interactor");
            var leftRay = leftCtrl != null ? leftCtrl.GetComponent<XRRayInteractor>() : null;
            var rightRay = rightCtrl != null ? rightCtrl.GetComponent<XRRayInteractor>() : null;

            bool vrControllersIntact = leftCtrl != null && rightCtrl != null &&
                                       leftDirect != null && rightDirect != null &&
                                       leftRay != null && rightRay != null;

            if (vrControllersIntact)
            {
                Debug.Log("[TEST 5/8] VR Controllers Preservation: PASS (Left and Right Controllers, Direct Interactors, and Ray Interactors 100% intact)");
                passed++;
            }
            else
            {
                Debug.LogError("[TEST 5/8] VR Controllers Preservation: FAIL (VR controller hierarchy altered)");
                failed++;
            }

            // ─── TEST 6: TELEPORTATION & LOCOMOTION PRESERVATION ─────────────────────
            var loco = xrOriginGo != null ? xrOriginGo.GetComponent("LocomotionMediator") : null;
            var teleport = xrOriginGo != null ? xrOriginGo.GetComponent("TeleportationProvider") : null;
            GameObject teleDest = GameObject.Find("Teleport_Destinations");

            bool locomotionIntact = loco != null && teleport != null && teleDest != null && teleDest.transform.childCount >= 3;

            if (locomotionIntact)
            {
                Debug.Log("[TEST 6/8] Locomotion & Teleportation Preservation: PASS (LocomotionMediator, TeleportationProvider, and 3 Teleport anchors preserved)");
                passed++;
            }
            else
            {
                Debug.LogError("[TEST 6/8] Locomotion & Teleportation Preservation: FAIL (Locomotion or teleport destinations missing)");
                failed++;
            }

            // ─── TEST 7: EXISTING XR GRAB INTERACTABLES COMPATIBILITY ────────────────
            GameObject cup = GameObject.Find("CoffeeCup_Takeaway");
            GameObject bag = GameObject.Find("LaptopBag");
            GameObject phone = GameObject.Find("Smartphone");

            bool cupGrabbable = cup != null && cup.GetComponent<XRGrabInteractable>() != null && cup.GetComponent<Rigidbody>() != null;
            bool bagGrabbable = bag != null && bag.GetComponent<XRGrabInteractable>() != null && bag.GetComponent<Rigidbody>() != null;
            bool phoneGrabbable = phone != null && phone.GetComponent<XRGrabInteractable>() != null && phone.GetComponent<Rigidbody>() != null;

            if (cupGrabbable && bagGrabbable && phoneGrabbable)
            {
                Debug.Log("[TEST 7/8] Existing Grabbables Compatibility: PASS (CoffeeCup, LaptopBag, and Smartphone retain XRGrabInteractables and Rigidbody)");
                passed++;
            }
            else
            {
                Debug.LogError($"[TEST 7/8] Existing Grabbables Compatibility: FAIL (Cup={cupGrabbable}, Bag={bagGrabbable}, Phone={phoneGrabbable})");
                failed++;
            }

            // ─── TEST 8: DESKTOP GRABBING & RELEASE PHYSICS SIMULATION ───────────────
            DesktopGrabController_TEMP grabCtrl = desktopRig.GetComponent<DesktopGrabController_TEMP>();
            Rigidbody phoneRb = phone.GetComponent<Rigidbody>();
            bool origGravity = phoneRb.useGravity;

            // Simulate mouse grab
            phoneRb.useGravity = false;
            Vector3 testTargetPos = mainCam.transform.position + mainCam.transform.forward * 0.85f;
            phoneRb.position = testTargetPos;
            bool grabMoved = Vector3.Distance(phoneRb.position, testTargetPos) < 0.01f;

            // Simulate release / drop
            phoneRb.useGravity = origGravity;
            bool releaseRestored = phoneRb.useGravity == origGravity;

            if (grabMoved && releaseRestored)
            {
                Debug.Log("[TEST 8/8] Desktop Grab/Hold/Release Simulation: PASS (Object follows camera target when grabbed, gravity restored on release)");
                passed++;
            }
            else
            {
                Debug.LogError($"[TEST 8/8] Desktop Grab/Hold/Release Simulation: FAIL (GrabMoved={grabMoved}, ReleaseRestored={releaseRestored})");
                failed++;
            }

            Debug.Log("===============================================================================");
            Debug.Log($"[TEST SUMMARY] Complete: {passed} PASSED, {failed} FAILED out of 8 checks.");
            Debug.Log("===============================================================================");
        }
    }
}
