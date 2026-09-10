using UnityEditor;
using UnityEngine;
using CADT.VR.Core;

namespace CADT.VR.Templates.Editor
{
    /// <summary>
    /// Editor blueprint utility for teammates to generate new scenario scaffoldings with 1-click.
    /// </summary>
    public static class _ScenarioTemplateBuilder
    {
        [MenuItem("CADT VR/Templates/Create New Scenario Boilerplate", priority = 100)]
        public static void CreateNewScenarioBoilerplate()
        {
            GameObject root = new GameObject("Scenario_Manager_Root", typeof(ScoreTracker), typeof(_ScenarioTemplate_Manager));
            Undo.RegisterCreatedObjectUndo(root, "Create Scenario Boilerplate");
            Selection.activeGameObject = root;
            Debug.Log("<color=#22C55E><b>[Scenario Template Builder]</b></color> Generated new Scenario Root GameObject.");
        }
    }
}
