using UnityEngine;

public class Barrier : MonoBehaviour {

    public Transform[] barrierPoints;
    public GameObject[] barrierPrefabs;

    public void CreateBarriers()
    {
        if (barrierPoints == null || barrierPoints.Length == 0)
        {
            return;
        }

        if (barrierPrefabs == null || barrierPrefabs.Length == 0)
        {
            return;
        }

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
            GameObject prefab = barrierPrefabs[prefabIndex];
            if (prefab == null)
            {
                continue;
            }

            int enemyCount = Random.Range(minEnemyCount, maxEnemyCount + 1);
            float y = prefab.transform.position.y;

            for (int j = 0; j < enemyCount; j++)
            {
                GameObject enemy = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                enemy.transform.SetParent(spawnPoint, false);
                enemy.transform.localPosition = new Vector3(j * 0.42f, y, 0f);
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
}
