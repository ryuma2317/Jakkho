using UnityEngine;

public class ScoreTracker : MonoBehaviour
{
    public static ScoreTracker Instance { get; private set; }
    public int TotalXP { get; private set; }
    public int ScenariosPlayed { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RecordOutcome(ScenarioManager.OutcomeType outcome)
    {
        int xp = outcome switch
        {
            ScenarioManager.OutcomeType.Failure  => 0,
            ScenarioManager.OutcomeType.Partial  => 50,
            ScenarioManager.OutcomeType.Success  => 100,
            ScenarioManager.OutcomeType.Ideal    => 200,
            _                                    => 0
        };
        TotalXP += xp;
        ScenariosPlayed++;
        Debug.Log($"[ScoreTracker] +{xp} XP | Total: {TotalXP} XP (Scenarios Completed: {ScenariosPlayed})");
    }

    public void ResetSession() 
    { 
        TotalXP = 0; 
        ScenariosPlayed = 0; 
    }
}
