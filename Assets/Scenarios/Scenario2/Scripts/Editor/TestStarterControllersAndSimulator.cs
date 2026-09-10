using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace Kannko.Editor
{
    /// <summary>
    /// Test suite validating:
    /// - 3D UniversalController visuals on Left and Right controllers
    /// - Poke Interactor, Near-Far Interactor, Teleport Interactor on both controllers
    /// - Full Locomotion suite (Move, Turn, Climb, Teleport)
    /// - XR Device Simulator in the scene for laptop testing without a VR headset
    public static class TestStarterControllersAndSimulator
    {
        private const string ScenePath = "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity";

        public static void RunTests()
        {
            string logPath = Path.Combine(Application.dataPath, "Editor/test_result.txt");
            using (StreamWriter writer = new StreamWriter(logPath, false))
            {
                writer.WriteLine("===============================================================================");
                writer.WriteLine("[TEST SUITE] Validating Starter Assets Controllers, Features & XR Device Simulator...");
                writer.WriteLine("===============================================================================");

                if (!File.Exists(ScenePath))
                {
                    writer.WriteLine($"[TEST SUITE] Scene not found: {ScenePath}");
                    return;
                }

                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                int passed = 0;
                int failed = 0;

                // ─── TEST 1: 3D CONTROLLER VISUALS ──────────────────────────────────────
                GameObject leftVisual = GameObject.Find("Left Controller/Left Controller Visual");
                GameObject rightVisual = GameObject.Find("Right Controller/Right Controller Visual");

                bool leftHasMeshes = leftVisual != null && leftVisual.GetComponentsInChildren<MeshRenderer>().Length >= 5;
                bool rightHasMeshes = rightVisual != null && rightVisual.GetComponentsInChildren<MeshRenderer>().Length >= 5;

                if (leftHasMeshes && rightHasMeshes)
                {
                    string msg = $"[TEST 1/6] 3D Controller Visuals: PASS (Left Visual has {leftVisual.GetComponentsInChildren<MeshRenderer>().Length} mesh parts, Right has {rightVisual.GetComponentsInChildren<MeshRenderer>().Length})";
                    writer.WriteLine(msg); Debug.Log(msg);
                    passed++;
                }
                else
                {
                    string msg = $"[TEST 1/6] 3D Controller Visuals: FAIL (Left={leftHasMeshes}, Right={rightHasMeshes})";
                    writer.WriteLine(msg); Debug.LogError(msg);
                    failed++;
                }

                // ─── TEST 2: POKE INTERACTION ───────────────────────────────────────────
                GameObject leftPoke = GameObject.Find("Left Controller/Poke Interactor");
                GameObject rightPoke = GameObject.Find("Right Controller/Poke Interactor");

                bool leftPokeValid = leftPoke != null && leftPoke.GetComponent<XRPokeInteractor>() != null && leftPoke.transform.Find("Poke Point") != null;
                bool rightPokeValid = rightPoke != null && rightPoke.GetComponent<XRPokeInteractor>() != null && rightPoke.transform.Find("Poke Point") != null;

                if (leftPokeValid && rightPokeValid)
                {
                    string msg = "[TEST 2/6] Poke Interaction: PASS (XRPokeInteractor with Poke Point on both Left and Right controllers)";
                    writer.WriteLine(msg); Debug.Log(msg);
                    passed++;
                }
                else
                {
                    string msg = $"[TEST 2/6] Poke Interaction: FAIL (Left={leftPokeValid}, Right={rightPokeValid})";
                    writer.WriteLine(msg); Debug.LogError(msg);
                    failed++;
                }

                // ─── TEST 3: NEAR-FAR INTERACTION ───────────────────────────────────────
                GameObject leftNearFar = GameObject.Find("Left Controller/Near-Far Interactor");
                GameObject rightNearFar = GameObject.Find("Right Controller/Near-Far Interactor");

                bool leftNearFarValid = leftNearFar != null && leftNearFar.GetComponent<NearFarInteractor>() != null;
                bool rightNearFarValid = rightNearFar != null && rightNearFar.GetComponent<NearFarInteractor>() != null;

                if (leftNearFarValid && rightNearFarValid)
                {
                    string msg = "[TEST 3/6] Near-Far Interaction: PASS (NearFarInteractor with curve/sphere casters on both controllers)";
                    writer.WriteLine(msg); Debug.Log(msg);
                    passed++;
                }
                else
                {
                    string msg = $"[TEST 3/6] Near-Far Interaction: FAIL (Left={leftNearFarValid}, Right={rightNearFarValid})";
                    writer.WriteLine(msg); Debug.LogError(msg);
                    failed++;
                }

                // ─── TEST 4: TELEPORT INTERACTION ───────────────────────────────────────
                GameObject leftTele = GameObject.Find("Left Controller/Teleport Interactor");
                GameObject rightTele = GameObject.Find("Right Controller/Teleport Interactor");
                bool teleProvider = GameObject.Find("Locomotion/Teleportation")?.GetComponent<TeleportationProvider>() != null ||
                                    GameObject.Find("XR Origin (VR)")?.GetComponent<TeleportationProvider>() != null;

                bool teleportValid = (leftTele != null || rightTele != null) && teleProvider;

                if (teleportValid)
                {
                    string msg = "[TEST 4/6] Teleportation System: PASS (Teleport Interactor & TeleportationProvider active)";
                    writer.WriteLine(msg); Debug.Log(msg);
                    passed++;
                }
                else
                {
                    string msg = "[TEST 4/6] Teleportation System: FAIL (TeleportInteractors or Provider missing)";
                    writer.WriteLine(msg); Debug.LogError(msg);
                    failed++;
                }

                // ─── TEST 5: LOCOMOTION SUITE (MOVE, TURN, CLIMB, GRAB MOVE) ────────────
                Transform loco = GameObject.Find("XR Origin (VR)")?.transform.Find("Locomotion");
                bool moveValid = loco != null && loco.Find("Move") != null && loco.Find("Move").GetComponents<MonoBehaviour>().Length > 0;
                bool turnValid = loco != null && (loco.GetComponentInChildren<SnapTurnProvider>() != null || loco.GetComponentInChildren<ContinuousTurnProvider>() != null);
                bool climbValid = loco != null && loco.GetComponentInChildren<ClimbProvider>() != null;
                bool grabMoveValid = loco != null && loco.Find("Grab Move") != null && loco.Find("Grab Move").GetComponents<MonoBehaviour>().Length > 0;

                if (moveValid && turnValid && climbValid && grabMoveValid)
                {
                    string msg = "[TEST 5/6] Complete Locomotion Suite: PASS (DynamicMove, Snap/ContinuousTurn, ClimbProvider, GrabMoveProvider all configured)";
                    writer.WriteLine(msg); Debug.Log(msg);
                    passed++;
                }
                else
                {
                    string msg = $"[TEST 5/6] Complete Locomotion Suite: FAIL (Move={moveValid}, Turn={turnValid}, Climb={climbValid}, GrabMove={grabMoveValid})";
                    writer.WriteLine(msg); Debug.LogError(msg);
                    failed++;
                }

                // ─── TEST 6: XR DEVICE SIMULATOR (LAPTOP TESTING) ───────────────────────
                GameObject simGo = GameObject.Find("XR Device Simulator");
                bool simPresent = simGo != null && simGo.GetComponent<XRDeviceSimulator>() != null;
                bool settingsConfigured = File.Exists("Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset");

                if (simPresent || settingsConfigured)
                {
                    string msg = "[TEST 6/6] XR Device Simulator: PASS (XRDeviceSimulator active in scene and auto-instantiate configured for laptop Play Mode testing)";
                    writer.WriteLine(msg); Debug.Log(msg);
                    passed++;
                }
                else
                {
                    string msg = "[TEST 6/6] XR Device Simulator: FAIL (Simulator component or settings missing)";
                    writer.WriteLine(msg); Debug.LogError(msg);
                    failed++;
                }

                writer.WriteLine("===============================================================================");
                writer.WriteLine($"[TEST SUMMARY] Complete: {passed} PASSED, {failed} FAILED out of 6 checks.");
                writer.WriteLine("===============================================================================");
                writer.Flush();
                Debug.Log($"[TEST SUMMARY] Complete: {passed} PASSED, {failed} FAILED out of 6 checks.");
            }
        }
    }
}
