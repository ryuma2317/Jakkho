using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor builder for Scenario 1: Office Spear Phishing.
/// Constructs the complete Scenario1 scene hierarchy including:
///   - XR Origin (XR Rig) with camera and controllers
///   - Corporate office environment
///   - Curved holographic workstation (email client UI)
///   - All 5 phase UI panels
///   - Audio sources and clips
///   - Wires all serialized fields on Scenario1_SpearPhishingManager
///
/// Usage: Menu → Jakkho → Build Scenario 1 Scene & Environment
/// </summary>
public class Scenario1Builder : MonoBehaviour
{
    // ═══════════════════════════════════════════════
    //  Constants — Match the Scenario 3 standard
    // ═══════════════════════════════════════════════
    private const float WorkstationHeight = 1.10f;           // Eye-level for seated VR
    private const float WorkstationDistance = 1.45f;          // Distance from player to screen
    private const float WorkstationCanvasScale = 0.00105f;   // World-space canvas scale
    private const float WorkstationYaw = 270f;

    // Workstation anchor position (office room)
    private static readonly Vector3 WorkstationAnchor = new Vector3(-10.0f, 0f, 4.0f);

    // Curved section geometry (3-panel tangent approach)
    private const float CenterWidth = 704f;
    private const float SideWidth = 448f;
    private const float CanvasHeight = 900f;
    private const float CurveAngle = 18f;  // degrees per side

    [MenuItem("Jakkho/Build Scenario 1 Scene & Environment", priority = 4)]
    public static void BuildScenario1()
    {
        Debug.Log("[Scenario1Builder] ═══ Starting Scenario 1 Build ═══");

        // TODO: Implement full scene construction following Scenario3Builder pattern
        //
        // Required steps:
        // 1. Create new scene → save as Assets/Scenarios/Scenario1/Scenes/Scenario1.unity
        // 2. Build XR Origin (XR Rig)
        // 3. Build corporate office environment (BarkingDog modular kit or primitives)
        // 4. Build curved holographic workstation with email client UI
        // 5. Create Phase 1 UI — Immersion panel
        // 6. Create Phase 2 UI — Email inbox with phishing email
        // 7. Create Phase 3 UI — Email detail view with red flag inspect buttons
        // 8. Create Phase 4 UI — Consequence panel
        // 9. Create Phase 5 UI — Debrief cards (Spoofed Sender, Social Engineering, Malware)
        // 10. Setup audio sources and clips
        // 11. Add Scenario1_SpearPhishingManager component
        // 12. Wire all serialized fields via SerializedObject
        // 13. Save scene and prefab

        Debug.LogWarning("[Scenario1Builder] Builder is a stub — implement following Scenario3Builder.cs pattern");
        EditorUtility.DisplayDialog(
            "Scenario 1 Builder",
            "This builder is a template stub.\n\n" +
            "Implement the full build pipeline following the pattern in Scenario3Builder.cs.\n\n" +
            "See PROJECT_STRUCTURE.md for architecture guidelines.",
            "OK"
        );
    }
}
