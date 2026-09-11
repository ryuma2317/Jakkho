using System;
using System.Collections.Generic;
using UnityEngine;

namespace CADT.VR.Core
{
    /// <summary>
    /// Tracks scenario scoring, discovered red flags, mistakes, accuracy percentage,
    /// and final XP calculations for scenario debriefs.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScoreTracker : MonoBehaviour
    {
        [Header("--- Score Settings ---")]
        [SerializeField] private int baseCompletionXP = 50;
        [SerializeField] private int penaltyPerMistake = 15;

        private readonly HashSet<string> detectedRedFlagIds = new HashSet<string>();
        private int mistakeCount = 0;
        private float startTime = 0f;
        private float completionTime = 0f;
        private bool isTracking = false;

        public int MistakeCount => mistakeCount;
        public float ElapsedSeconds => isTracking ? Time.time - startTime : completionTime;
        public IReadOnlyCollection<string> DetectedRedFlagIds => detectedRedFlagIds;

        public event Action<string, int> OnRedFlagDiscovered;
        public event Action<int> OnMistakeRecorded;

        public void StartTracking()
        {
            detectedRedFlagIds.Clear();
            mistakeCount = 0;
            startTime = Time.time;
            isTracking = true;
        }

        public void RecordRedFlag(RedFlagSO flag)
        {
            if (flag == null) return;
            if (detectedRedFlagIds.Add(flag.FlagId))
            {
                OnRedFlagDiscovered?.Invoke(flag.FlagTitle, flag.XPValue);
            }
        }

        public void RecordMistake()
        {
            mistakeCount++;
            OnMistakeRecorded?.Invoke(mistakeCount);
        }

        public void StopTracking()
        {
            if (!isTracking) return;
            isTracking = false;
            completionTime = Time.time - startTime;
        }

        public int CalculateTotalXP(ScenarioDataSO scenarioConfig)
        {
            int score = baseCompletionXP;
            if (scenarioConfig != null)
            {
                foreach (var flag in scenarioConfig.RedFlags)
                {
                    if (detectedRedFlagIds.Contains(flag.FlagId))
                    {
                        score += flag.XPValue;
                    }
                }
            }
            score -= (mistakeCount * penaltyPerMistake);
            return Mathf.Max(0, score);
        }

        public float CalculateAccuracy(ScenarioDataSO scenarioConfig)
        {
            if (scenarioConfig == null || scenarioConfig.RedFlags.Count == 0) return 100f;
            float totalFlags = scenarioConfig.RedFlags.Count;
            float found = detectedRedFlagIds.Count;
            float baseAcc = (found / totalFlags) * 100f;
            float penalty = mistakeCount * 10f;
            return Mathf.Clamp(baseAcc - penalty, 0f, 100f);
        }
    }
}
