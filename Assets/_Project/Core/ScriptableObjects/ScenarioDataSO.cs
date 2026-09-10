using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject defining a scenario's metadata.
/// Create instances via: Assets → Create → JakkhoVR → Scenario Data
/// </summary>
[CreateAssetMenu(fileName = "NewScenarioData", menuName = "JakkhoVR/Scenario Data")]
public class ScenarioDataSO : ScriptableObject
{
    [Header("Identity")]
    public int scenarioNumber;
    public string title;
    [TextArea(2, 4)]
    public string description;
    public string setting;
    public string threatType;
    public string sceneName;

    [Header("Scoring")]
    public int maxXP = 200;

    [Header("Red Flags")]
    public List<RedFlagSO> redFlags = new List<RedFlagSO>();

    [Header("Presentation")]
    public Sprite thumbnail;
    public bool isAvailable = false;
}
