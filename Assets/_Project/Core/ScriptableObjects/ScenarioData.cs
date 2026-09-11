using UnityEngine;

[CreateAssetMenu(fileName = "NewScenarioData", menuName = "Scam Training/Scenario Data")]
public class ScenarioData : ScriptableObject
{
    [Header("Scenario Information")]
    public string title = "Scenario Title";
    [TextArea(3, 5)]
    public string description = "Scenario description text goes here...";
    public string[] tags = new string[] { "Phishing", "Security" };
    public Sprite thumbnail;
    public string sceneToLoad = "Scenario_Scene";
}
