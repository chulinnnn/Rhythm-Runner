using UnityEngine;

public class Barrier : MonoBehaviour {

    public Transform[] barrierPoints;
    public GameObject[] barrierPrefabs;
    public bool spawnOnBeat = true;
    public float bpm = 107f;
    public int beatObstacleCount = 4;
    public int beatsBetweenObstacles = 2;
    public int firstObstacleBeat = 4;
    public float playerMeetX = -4.5f;
    public float fallbackMoveSpeed = 2f;
    public bool beatObstaclesMoveIndependently = true;
    public bool parentBeatObstaclesToNearestBackground = true;
    public float maxBackgroundAttachDistance = 14f;
    public bool snapBeatObstaclesToGround = true;
    public float groundYOffset = 0f;
    public bool createFallbackObstacleIfMissing = true;
    public Vector2 fallbackObstacleSize = new Vector2(0.75f, 0.75f);
    public Color fallbackObstacleColor = new Color(1f, 0.35f, 0.15f);

    public void CreateBarriers()
    {
        if (barrierPoints == null || barrierPoints.Length == 0)
        {
            Debug.LogWarning("Barrier: Barrier Points is empty. Assign at least one BarriersPoint transform.");
            return;
        }

        if (barrierPrefabs == null || barrierPrefabs.Length == 0)
        {
            if (!createFallbackObstacleIfMissing)
            {
                Debug.LogWarning("Barrier: Barrier Prefabs is empty. Assign a prefab from Assets/Prefabs/Barriers.");
                return;
            }
        }

        if (ShouldUseBeatSpawn())
        {
            CreateBeatBarriers();
            return;
        }

        CreateRandomBarriers();
    }

    private void CreateRandomBarriers()
    {
        GetSpawnSettings(
            out int minPointIndex,
            out int maxPointIndex,
            out int minEnemyCount,
            out int maxEnemyCount);

        maxPointIndex = Mathf.Min(maxPointIndex, barrierPoints.Length - 1);
        minPointIndex = Mathf.Clamp(minPointIndex, 0, maxPointIndex);

        int lastPointIndex = Random.Range(minPointIndex, maxPointIndex + 1);

        for (int i = 0; i <= lastPointIndex; i++)
        {
            Transform spawnPoint = barrierPoints[i];
            if (spawnPoint == null)
            {
                continue;
            }

            int prefabIndex = Random.Range(0, barrierPrefabs.Length);
            GameObject prefab = GetBarrierPrefab(prefabIndex);
            if (prefab == null)
            {
                Debug.LogWarning("Barrier: Barrier Prefabs has a missing element.");
                continue;
            }

            int enemyCount = Random.Range(minEnemyCount, maxEnemyCount + 1);
            float y = prefab.transform.position.y;

            for (int j = 0; j < enemyCount; j++)
            {
                GameObject enemy = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                enemy.SetActive(true);
                enemy.transform.SetParent(spawnPoint, false);
                enemy.transform.localPosition = new Vector3(j * 0.42f, y, 0f);
            }
        }
    }

    private void CreateBeatBarriers()
    {
        GetBeatSpawnSettings(
            out float beatBpm,
            out int obstacleCount,
            out int beatSpacing,
            out int startBeat,
            out float meetX);

        GetSpawnSettings(
            out int minPointIndex,
            out int maxPointIndex,
            out int minEnemyCount,
            out int maxEnemyCount);

        maxPointIndex = Mathf.Min(maxPointIndex, barrierPoints.Length - 1);
        minPointIndex = Mathf.Clamp(minPointIndex, 0, maxPointIndex);

        float moveSpeed = GetSegmentMoveSpeed();
        float beatDistance = moveSpeed * (60f / Mathf.Max(1f, beatBpm));
        int validPointCount = maxPointIndex - minPointIndex + 1;

        for (int i = 0; i < obstacleCount; i++)
        {
            int pointIndex = minPointIndex + i % validPointCount;
            Transform spawnPoint = barrierPoints[pointIndex];
            if (spawnPoint == null)
            {
                continue;
            }

            int prefabIndex = barrierPrefabs == null || barrierPrefabs.Length == 0 ? -1 : i % barrierPrefabs.Length;
            GameObject prefab = GetBarrierPrefab(prefabIndex);
            if (prefab == null)
            {
                continue;
            }

            int beat = startBeat + i * beatSpacing;
            float worldX = meetX + beat * beatDistance;
            float worldY = spawnPoint.position.y + prefab.transform.position.y;
            Vector3 worldPosition = new Vector3(worldX, worldY, spawnPoint.position.z);

            if (beatObstaclesMoveIndependently)
            {
                GameObject enemy = Instantiate(prefab, worldPosition, Quaternion.identity);
                enemy.SetActive(true);
                MarkAsRhythmGenerated(enemy);
                SnapObstacleToGround(enemy, spawnPoint.position.y + groundYOffset);

                Transform backgroundParent = FindNearestBackground(worldX);
                if (parentBeatObstaclesToNearestBackground && backgroundParent != null)
                {
                    enemy.transform.SetParent(backgroundParent, true);
                }
                else
                {
                    RhythmObstacleMover mover = enemy.GetComponent<RhythmObstacleMover>();
                    if (mover == null)
                    {
                        mover = enemy.AddComponent<RhythmObstacleMover>();
                    }
                    mover.moveSpeed = moveSpeed;
                }
            }
            else
            {
                GameObject enemy = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                enemy.SetActive(true);
                MarkAsRhythmGenerated(enemy);
                enemy.transform.SetParent(spawnPoint, false);
                enemy.transform.position = worldPosition;
                SnapObstacleToGround(enemy, spawnPoint.position.y + groundYOffset);
            }
        }
    }

    public void ClearSpawnedBarriers()
    {
        if (barrierPoints == null)
        {
            return;
        }

        for (int i = 0; i < barrierPoints.Length; i++)
        {
            if (barrierPoints[i] == null)
            {
                continue;
            }

            for (int j = barrierPoints[i].childCount - 1; j >= 0; j--)
            {
                Destroy(barrierPoints[i].GetChild(j).gameObject);
            }
        }
    }

    public void RespawnBarriers()
    {
        ClearSpawnedBarriers();
        CreateBarriers();
    }

    private void GetSpawnSettings(
        out int minPointIndex,
        out int maxPointIndex,
        out int minEnemyCount,
        out int maxEnemyCount)
    {
        minPointIndex = 0;
        maxPointIndex = Mathf.Max(0, barrierPoints.Length - 1);
        minEnemyCount = 1;
        maxEnemyCount = 2;

        if (SceneDifficultySettings.Instance != null)
        {
            SceneDifficultySettings.Instance.GetEnemySpawnSettings(
                out minPointIndex,
                out maxPointIndex,
                out minEnemyCount,
                out maxEnemyCount);
        }
    }

    private bool ShouldUseBeatSpawn()
    {
        if (SceneDifficultySettings.Instance != null)
        {
            return SceneDifficultySettings.Instance.ShouldSpawnEnemiesOnBeat();
        }

        return spawnOnBeat;
    }

    private void GetBeatSpawnSettings(
        out float beatBpm,
        out int obstacleCount,
        out int beatSpacing,
        out int startBeat,
        out float meetX)
    {
        beatBpm = bpm;
        obstacleCount = Mathf.Max(1, beatObstacleCount);
        beatSpacing = Mathf.Max(1, beatsBetweenObstacles);
        startBeat = Mathf.Max(0, firstObstacleBeat);
        meetX = playerMeetX;

        if (SceneDifficultySettings.Instance != null)
        {
            SceneDifficultySettings.Instance.GetRhythmSpawnSettings(
                out beatBpm,
                out obstacleCount,
                out beatSpacing,
                out startBeat,
                out meetX);
        }
    }

    private float GetSegmentMoveSpeed()
    {
        BackgroundTranform background = GetComponent<BackgroundTranform>();
        if (background != null)
        {
            return Mathf.Max(0.1f, background.moveSpeed);
        }

        return Mathf.Max(0.1f, fallbackMoveSpeed);
    }

    private GameObject GetBarrierPrefab(int prefabIndex)
    {
        if (barrierPrefabs != null && prefabIndex >= 0 && prefabIndex < barrierPrefabs.Length)
        {
            GameObject prefab = barrierPrefabs[prefabIndex];
            if (prefab != null)
            {
                return prefab;
            }
        }

        if (!createFallbackObstacleIfMissing)
        {
            return null;
        }

        return CreateFallbackObstaclePrefab();
    }

    private GameObject CreateFallbackObstaclePrefab()
    {
        GameObject obstacle = new GameObject("GeneratedRhythmBarrier");
        obstacle.tag = "EnemyBarrier";

        SpriteRenderer renderer = obstacle.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackSprite();
        renderer.color = fallbackObstacleColor;
        renderer.sortingOrder = 2;

        BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>();
        collider.size = fallbackObstacleSize;

        obstacle.transform.localScale = new Vector3(fallbackObstacleSize.x, fallbackObstacleSize.y, 1f);
        obstacle.SetActive(false);
        return obstacle;
    }

    private Sprite CreateFallbackSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private Transform FindNearestBackground(float worldX)
    {
        BackgroundTranform[] backgrounds = FindObjectsOfType<BackgroundTranform>();
        Transform nearest = null;
        float nearestDistance = maxBackgroundAttachDistance;

        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == null)
            {
                continue;
            }

            float distance = Mathf.Abs(backgrounds[i].transform.position.x - worldX);
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearest = backgrounds[i].transform;
            }
        }

        return nearest;
    }

    private void SnapObstacleToGround(GameObject obstacle, float groundY)
    {
        if (!snapBeatObstaclesToGround || obstacle == null)
        {
            return;
        }

        Bounds bounds;
        if (!TryGetObstacleBounds(obstacle, out bounds))
        {
            return;
        }

        float offsetY = groundY - bounds.min.y;
        obstacle.transform.position += new Vector3(0f, offsetY, 0f);
    }

    private bool TryGetObstacleBounds(GameObject obstacle, out Bounds bounds)
    {
        Collider2D[] colliders = obstacle.GetComponentsInChildren<Collider2D>();
        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
            return true;
        }

        Renderer[] renderers = obstacle.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return true;
        }

        bounds = new Bounds(obstacle.transform.position, Vector3.zero);
        return false;
    }

    private void MarkAsRhythmGenerated(GameObject obstacle)
    {
        if (obstacle != null && obstacle.GetComponent<RhythmGeneratedObstacle>() == null)
        {
            obstacle.AddComponent<RhythmGeneratedObstacle>();
        }
    }
}
