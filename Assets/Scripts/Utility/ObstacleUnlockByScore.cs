using UnityEngine;

/// <summary>
/// Enables obstacle objects when score reaches given thresholds.
/// </summary>
public class ObstacleUnlockByScore : MonoBehaviour
{
    [System.Serializable]
    public class ScoreUnlockEntry
    {
        [Tooltip("Obstacle to enable when score reaches unlockScore.")]
        public GameObject obstacle;

        [Tooltip("Enable when current score is greater than this value.")]
        public int unlockScore = 10;
    }

    [Tooltip("Assign hidden obstacles and their unlock scores.")]
    public ScoreUnlockEntry[] unlocks;

    private bool[] unlocked;

    private void Awake()
    {
        if (unlocks == null)
            return;

        unlocked = new bool[unlocks.Length];
    }

    private void Update()
    {
        if (unlocks == null || unlocks.Length == 0)
            return;

        int currentScore = GameManager.score;

        for (int i = 0; i < unlocks.Length; i++)
        {
            if (unlocked[i])
                continue;

            ScoreUnlockEntry entry = unlocks[i];
            if (entry == null || entry.obstacle == null)
                continue;

            if (currentScore > entry.unlockScore)
            {
                entry.obstacle.SetActive(true);
                unlocked[i] = true;
            }
        }
    }
}
