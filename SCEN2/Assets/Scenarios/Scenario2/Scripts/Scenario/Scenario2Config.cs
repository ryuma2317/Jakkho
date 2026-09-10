using UnityEngine;

namespace Kannko.Scenario2
{
    [CreateAssetMenu(fileName = "Scenario2_Config", menuName = "Kannko/Scenario 2 Config")]
    public class Scenario2Config : ScriptableObject
    {
        public string scenarioName = "Scenario 2: Evil Twin Wi-Fi Attack";
        [TextArea(3, 6)]
        public string description = "Educational scenario simulating a malicious rogue access point attack in a coffee shop setting.";
        public float timeLimitSeconds = 300f;
        public RedFlagData[] redFlags;
    }
}
