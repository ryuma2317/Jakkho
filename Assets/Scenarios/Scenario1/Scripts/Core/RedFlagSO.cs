using System;
using UnityEngine;

namespace CADT.VR.Core
{
    public enum RedFlagCategory
    {
        EmailHeader,
        FileExtension,
        SocialEngineering,
        NetworkAnomaly,
        CredentialHarvesting,
        SuspiciousAttachment,
        UrgencyPressure
    }

    [CreateAssetMenu(fileName = "New_RedFlag", menuName = "CADT VR/Core/Red Flag Indicator")]
    public class RedFlagSO : ScriptableObject
    {
        [Header("--- Identifier ---")]
        [SerializeField] private string flagId = "FLAG_001";
        [SerializeField] private string flagTitle = "Double File Extension Spoofing";

        [Header("--- Category & Score ---")]
        [SerializeField] private RedFlagCategory category = RedFlagCategory.FileExtension;
        [SerializeField] private int xpValue = 25;
        [SerializeField] private bool isCritical = true;

        [Header("--- Descriptions ---")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "The file uses a double extension (.pdf.exe) to disguise an executable payload as an Adobe PDF document.";

        [TextArea(2, 3)]
        [SerializeField] private string hint = "Inspect the actual file properties and file extension before executing.";

        public string FlagId => flagId;
        public string FlagTitle => flagTitle;
        public RedFlagCategory Category => category;
        public int XPValue => xpValue;
        public bool IsCritical => isCritical;
        public string Description => description;
        public string Hint => hint;

        public void Initialize(string id, string title, RedFlagCategory cat, int xp, bool critical, string desc, string h)
        {
            flagId = id;
            flagTitle = title;
            category = cat;
            xpValue = xp;
            isCritical = critical;
            description = desc;
            hint = h;
        }
    }
}
