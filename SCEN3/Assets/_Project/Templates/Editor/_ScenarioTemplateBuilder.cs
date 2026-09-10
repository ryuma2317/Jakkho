using UnityEngine;
using UnityEditor;

/// <summary>
/// BUILDER TEMPLATE — Copy this file to create a new scenario builder.
///
/// Instructions:
///   1. Copy to Assets/Scenarios/ScenarioN/Scripts/Editor/ScenarioNBuilder.cs
///   2. Rename the class to match your filename
///   3. Update the MenuItem path and priority
///   4. Implement each build step following Scenario3Builder.cs as reference
///
/// Builder Architecture:
///   - Builders are Editor-only static classes with [MenuItem] entry points
///   - They programmatically construct entire scenes: XR rig, environment, UI, audio, wiring
///   - They save both the scene (.unity) and canvas prefab (.prefab) to Assets/
///   - All serialized fields on the scenario manager are wired via SerializedObject
///   - Curved workstation uses 3-panel tangent sections (Center + Left + Right)
///
/// Reference implementation: Assets/Scenarios/Scenario3/Scripts/Editor/Scenario3Builder.cs
/// </summary>
public class ScenarioTemplateBuilder : MonoBehaviour
{
    // ═══════════════════════════════════════════════
    //  Standard Constants (DO NOT CHANGE unless
    //  approved by team — these ensure consistency)
    // ═══════════════════════════════════════════════

    // Workstation display geometry
    private const float WorkstationHeight = 1.10f;           // Meters, eye-level for seated VR
    private const float WorkstationDistance = 1.45f;          // Meters, player-to-screen distance
    private const float WorkstationCanvasScale = 0.00105f;   // World-space canvas scale factor

    // Curved panel geometry (3-section tangent approach)
    private const float CenterWidth = 704f;     // pixels
    private const float SideWidth = 448f;        // pixels (each side)
    private const float CanvasHeight = 900f;     // pixels
    private const float CurveAngle = 18f;        // degrees per side section

    // Total screen width: 704 + 448 + 448 = 1600px at 900px height

    // [MenuItem("Jakkho/Build Scenario N Scene & Environment", priority = 10)]
    public static void BuildScenario()
    {
        // TODO: Implement — see Scenario3Builder.cs for the complete reference pattern
        Debug.LogWarning("[ScenarioTemplateBuilder] This is a template — copy and implement");
    }
}
