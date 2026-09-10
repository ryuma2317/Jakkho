using UnityEngine;

namespace Kannko.Scenario2
{
    [CreateAssetMenu(fileName = "RedFlag_Data", menuName = "Kannko/Red Flag Data")]
    public class RedFlagData : ScriptableObject
    {
        public string flagId;
        public string title;
        [TextArea(3, 6)]
        public string description;
        public bool isEvilTwinClue;
    }
}
