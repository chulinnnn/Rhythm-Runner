using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BackgroundTranform : MonoBehaviour {

    public float moveSpeed = 2f;

    //private Barrier barrier;
    public GameObject[] mapPrefabs;

	void Awake()
    {
        ApplySceneSettings();
    }

	void Start () {
        ApplySceneSettings();
        TrySpawnEnemies();
	}

    private void ApplySceneSettings()
    {
        if (SceneDifficultySettings.Instance == null)
        {
            return;
        }

        moveSpeed = SceneDifficultySettings.Instance.GetBackgroundMoveSpeed();

        GameObject[] scenePrefabs = SceneDifficultySettings.Instance.GetMapPrefabs();
        if (scenePrefabs != null && scenePrefabs.Length > 0)
        {
            mapPrefabs = scenePrefabs;
        }
    }
	
	// Update is called once per frame
	void Update () {
        //moveSpeed += Time.deltaTime;
        float speed = moveSpeed;
        if (GameManager.Instance != null)
        {
            speed *= GameManager.Instance.speedMultiplier;
        }
        this.transform.Translate(Vector3.left * speed * Time.deltaTime);
        Vector3 position = this.transform.position;
        if (position.x <= -20)
        {
            //barrier.DestroyBarriers();
            CreateBackground();
            Destroy(this.gameObject);
        }
	}

    private void CreateBackground()
    {
        GameObject[] prefabs = mapPrefabs;
        if (SceneDifficultySettings.Instance != null)
        {
            GameObject[] scenePrefabs = SceneDifficultySettings.Instance.GetMapPrefabs();
            if (scenePrefabs != null && scenePrefabs.Length > 0)
            {
                prefabs = scenePrefabs;
            }
        }

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("BackgroundTranform: mapPrefabs is empty.");
            return;
        }

        int i = Random.Range(0, prefabs.Length);
        GameObject segment = GameObject.Instantiate(prefabs[i], new Vector3(20, 0, 0), Quaternion.identity);

        BackgroundTranform nextBg = segment.GetComponent<BackgroundTranform>();
        if (nextBg != null && SceneDifficultySettings.Instance != null)
        {
            nextBg.moveSpeed = SceneDifficultySettings.Instance.GetBackgroundMoveSpeed();
            GameObject[] scenePrefabs = SceneDifficultySettings.Instance.GetMapPrefabs();
            if (scenePrefabs != null && scenePrefabs.Length > 0)
            {
                nextBg.mapPrefabs = scenePrefabs;
            }
        }

        TrySpawnEnemiesOnSegment(segment);
    }

    private void TrySpawnEnemies()
    {
        TrySpawnEnemiesOnSegment(gameObject);
    }

    private void TrySpawnEnemiesOnSegment(GameObject segment)
    {
        if (SceneDifficultySettings.Instance != null && !SceneDifficultySettings.Instance.ShouldAutoSpawnEnemies())
        {
            return;
        }

        bool isHardScene = SceneManager.GetActiveScene().name.Contains("Game2");
        if (SceneDifficultySettings.Instance == null && !isHardScene)
        {
            return;
        }

        Barrier barrier = segment.GetComponent<Barrier>();
        if (barrier == null)
        {
            return;
        }

        barrier.RespawnBarriers();
    }
}
