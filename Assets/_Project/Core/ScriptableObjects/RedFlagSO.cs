using UnityEngine;

/// <summary>
/// ScriptableObject defining a single red flag for a scenario.
/// Create instances via: Assets → Create → JakkhoVR → Red Flag
/// </summary>
[CreateAssetMenu(fileName = "NewRedFlag", menuName = "JakkhoVR/Red Flag")]
public class RedFlagSO : ScriptableObject
{
    [Header("Identity")]
    public string flagId;
    public string flagName;
    [TextArea(2, 4)]
    public string description;

    [Header("Presentation")]
    public Sprite icon;
    [TextArea(2, 6)]
    public string educationalText;
}
