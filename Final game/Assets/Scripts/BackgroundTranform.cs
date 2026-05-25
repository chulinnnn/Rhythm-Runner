using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BackgroundTranform : MonoBehaviour {

    public float moveSpeed = 2f;
    public bool createInitialNextSegment = true;
    public bool spawnBarriersOnStart = true;
    public float nextSegmentSpawnX = 20f;
    public bool disableStaticBarrierChildren = true;
    public string staticBarrierTag = "EnemyBarrier";

    //private Barrier barrier;
    public GameObject[] mapPrefabs;

	void Awake()
    {
        ApplySceneSettings();
    }

	void Start () {
        ApplySceneSettings();
        DisableStaticBarrierChildren(gameObject);
        if (SceneManager.GetActiveScene().name != "Tutorial")
        {
            RhythmGroundGapGenerator.ApplyToSegment(gameObject);
        }
        CreateInitialNextSegmentIfNeeded();
        if (spawnBarriersOnStart)
        {
            TrySpawnEnemies();
        }
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
        GameObject segment = GameObject.Instantiate(prefabs[i], new Vector3(nextSegmentSpawnX, 0, 0), Quaternion.identity);

        BackgroundTranform nextBg = segment.GetComponent<BackgroundTranform>();
        if (nextBg != null)
        {
            nextBg.createInitialNextSegment = false;
            nextBg.spawnBarriersOnStart = false;

            if (SceneDifficultySettings.Instance != null)
            {
                nextBg.moveSpeed = SceneDifficultySettings.Instance.GetBackgroundMoveSpeed();
                GameObject[] scenePrefabs = SceneDifficultySettings.Instance.GetMapPrefabs();
                if (scenePrefabs != null && scenePrefabs.Length > 0)
                {
                    nextBg.mapPrefabs = scenePrefabs;
                }
            }
            else
            {
                nextBg.moveSpeed = moveSpeed;
                nextBg.mapPrefabs = mapPrefabs;
            }
        }

        DisableStaticBarrierChildren(segment);
        if (SceneManager.GetActiveScene().name != "Tutorial")
        {
            RhythmGroundGapGenerator.ApplyToSegment(segment);
        }

        if (ShouldSpawnEnemiesOnNewSegment())
        {
            TrySpawnEnemiesOnSegment(segment);
        }
    }

    public static void EnsureForwardSegmentExists()
    {
        BackgroundTranform[] backgrounds = FindObjectsOfType<BackgroundTranform>(true);
        if (backgrounds == null || backgrounds.Length == 0)
        {
            return;
        }

        BackgroundTranform source = null;
        float rightMostX = float.MinValue;
        float spawnX = 20f;

        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == null)
            {
                continue;
            }

            float x = backgrounds[i].transform.position.x;
            if (x > rightMostX)
            {
                rightMostX = x;
                source = backgrounds[i];
                spawnX = backgrounds[i].nextSegmentSpawnX;
            }
        }

        if (source == null)
        {
            return;
        }

        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == null)
            {
                continue;
            }

            if (Mathf.Abs(backgrounds[i].transform.position.x - spawnX) <= 2f)
            {
                return;
            }
        }

        source.CreateBackground();
    }

    private void CreateInitialNextSegmentIfNeeded()
    {
        if (!createInitialNextSegment)
        {
            return;
        }

        if (Mathf.Abs(transform.position.x) > 0.1f)
        {
            return;
        }

        CreateBackground();
    }

    private void TrySpawnEnemies()
    {
        TrySpawnEnemiesOnSegment(gameObject);
    }

    private bool ShouldSpawnEnemiesOnNewSegment()
    {
        if (SceneDifficultySettings.Instance != null)
        {
            return true;
        }

        return SceneManager.GetActiveScene().name.Contains("Game2");
    }

    private void TrySpawnEnemiesOnSegment(GameObject segment)
    {
        Barrier barrier = segment.GetComponent<Barrier>();
        if (barrier == null)
        {
            return;
        }

        if (SceneDifficultySettings.Instance != null)
        {
            if (!SceneDifficultySettings.Instance.ShouldAutoSpawnEnemies())
            {
                return;
            }
        }
        else
        {
            bool isHardScene = SceneManager.GetActiveScene().name.Contains("Game2");
            if (!isHardScene && !barrier.spawnOnBeat)
            {
                return;
            }
        }

        barrier.RespawnBarriers();
    }

    private void DisableStaticBarrierChildren(GameObject segment)
    {
        if (!disableStaticBarrierChildren || segment == null || string.IsNullOrEmpty(staticBarrierTag))
        {
            return;
        }

        Transform[] children = segment.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child == segment.transform)
            {
                continue;
            }

            if (child.GetComponentInParent<RhythmGeneratedObstacle>() != null)
            {
                continue;
            }

            if (IsStaticBarrierObject(child.gameObject))
            {
                child.gameObject.SetActive(false);
                continue;
            }

            if (SceneManager.GetActiveScene().name == "Tutorial" && IsStaticTutorialGameplayObject(child.gameObject))
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private bool IsStaticBarrierObject(GameObject obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (obj.CompareTag(staticBarrierTag))
        {
            return true;
        }

        string objectName = obj.name;
        if (objectName.StartsWith("BarriersPoint"))
        {
            return false;
        }

        return objectName == "Barrier"
            || objectName.StartsWith("Barrier1")
            || objectName.StartsWith("Barrier4")
            || objectName.StartsWith("CubeBarrier");
    }

    private bool IsStaticTutorialGameplayObject(GameObject obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (obj.GetComponentInParent<TutorialSpawnedObject>() != null)
        {
            return false;
        }

        if (obj.CompareTag("Bonus1") || obj.CompareTag("Bonus2") || obj.CompareTag("jiasu") || obj.CompareTag("xt") || obj.CompareTag("UpCollider"))
        {
            return true;
        }

        string objectName = obj.name;
        return objectName.StartsWith("Bonus")
            || objectName.StartsWith("Gold")
            || objectName.StartsWith("jetpack")
            || objectName.StartsWith("carrot_gold");
    }
}
