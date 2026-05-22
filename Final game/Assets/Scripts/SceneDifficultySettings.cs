using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-50)]
public class SceneDifficultySettings : MonoBehaviour
{
    public static SceneDifficultySettings Instance { get; private set; }

    [Header("Background scroll speed")]
    public float backgroundMoveSpeed = 2f;

    [Header("Background segments (use Game2-only prefabs in hard scene)")]
    public GameObject[] mapPrefabs;

    [Header("Extra multiplier applied on top of background speed")]
    public float extraSpeedMultiplier = 1f;

    [Header("Enemy spawn on each background segment")]
    public bool autoSpawnEnemies = true;
    public int minBarrierPointIndex = 0;
    public int maxBarrierPointIndex = 2;
    public int minEnemiesPerPoint = 1;
    public int maxEnemiesPerPoint = 2;

    [Header("Rhythm enemy spawn")]
    public bool spawnEnemiesOnBeat = true;
    public float obstacleBpm = 107f;
    public int beatObstacleCount = 4;
    public int beatsBetweenObstacles = 2;
    public int firstObstacleBeat = 4;
    public float playerMeetX = -4.5f;

    void Awake()
    {
        Instance = this;
        ApplyHardSceneDefaultsIfNeeded();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public GameObject[] GetMapPrefabs()
    {
        if (mapPrefabs != null && mapPrefabs.Length > 0)
        {
            return mapPrefabs;
        }
        return null;
    }

    public float GetBackgroundMoveSpeed()
    {
        return backgroundMoveSpeed;
    }

    public float GetExtraSpeedMultiplier()
    {
        return extraSpeedMultiplier;
    }

    public bool ShouldAutoSpawnEnemies()
    {
        return autoSpawnEnemies;
    }

    public void GetEnemySpawnSettings(
        out int minPointIndex,
        out int maxPointIndex,
        out int minEnemyCount,
        out int maxEnemyCount)
    {
        minPointIndex = minBarrierPointIndex;
        maxPointIndex = maxBarrierPointIndex;
        minEnemyCount = minEnemiesPerPoint;
        maxEnemyCount = maxEnemiesPerPoint;
    }

    public bool ShouldSpawnEnemiesOnBeat()
    {
        return spawnEnemiesOnBeat;
    }

    public void GetRhythmSpawnSettings(
        out float bpm,
        out int obstacleCount,
        out int beatSpacing,
        out int firstBeat,
        out float meetX)
    {
        bpm = obstacleBpm;
        obstacleCount = Mathf.Max(1, beatObstacleCount);
        beatSpacing = Mathf.Max(1, beatsBetweenObstacles);
        firstBeat = Mathf.Max(0, firstObstacleBeat);
        meetX = playerMeetX;
    }

    private void ApplyHardSceneDefaultsIfNeeded()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneName.Contains("Game2"))
        {
            return;
        }

        if (backgroundMoveSpeed <= 2f)
        {
            backgroundMoveSpeed = 3f;
        }

        if (extraSpeedMultiplier < 1.15f)
        {
            extraSpeedMultiplier = 1.25f;
        }

        autoSpawnEnemies = true;
        minBarrierPointIndex = 1;
        maxBarrierPointIndex = 5;
        minEnemiesPerPoint = 2;
        maxEnemiesPerPoint = 4;
    }
}
