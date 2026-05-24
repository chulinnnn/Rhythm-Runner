using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameManager : MonoBehaviour {

    private static GameManager _instance;
    public static GameManager Instance
    {
        get{return _instance;}
    }

    private float f_dis = 0;
    private int dis = 0;
    private int bonus = 0;

    private Text goldText;
    private Text distanceText;
    private GameObject go;
    private bool isGameOver = false;
    

    private BackgroundTranform bgT;

    public float speedMultiplier = 1f;
    public bool saveScoreOnGameOver = true;
    public bool IsGameOver { get { return isGameOver; } }
    public event System.Action GameOverStarted;

    void Awake()
    {
        _instance = this;
    }

	// Use this for initialization
	void Start () {
        GameObject goldTextObject = GameObject.Find("GoldText");
        if (goldTextObject != null)
        {
            goldText = goldTextObject.GetComponent<Text>();
        }

        GameObject distanceTextObject = GameObject.Find("DistanceText");
        if (distanceTextObject != null)
        {
            distanceText = distanceTextObject.GetComponent<Text>();
        }

        GameObject backgroundObject = GameObject.Find("Background1");
        if (backgroundObject != null)
        {
            bgT = backgroundObject.GetComponent<BackgroundTranform>();
        }

        go = GameObject.Find("GameOver");

        ApplySceneDifficulty();

        if (go != null)
        {
            go.SetActive(false);
        }
	}

    private void ApplySceneDifficulty()
    {
        if (SceneDifficultySettings.Instance == null || bgT == null)
        {
            return;
        }

        bgT.moveSpeed = SceneDifficultySettings.Instance.GetBackgroundMoveSpeed();
        GameObject[] scenePrefabs = SceneDifficultySettings.Instance.GetMapPrefabs();
        if (scenePrefabs != null && scenePrefabs.Length > 0)
        {
            bgT.mapPrefabs = scenePrefabs;
        }

        float extra = SceneDifficultySettings.Instance.GetExtraSpeedMultiplier();
        if (extra > 1f && speedMultiplier <= 1f)
        {
            speedMultiplier = extra;
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (isGameOver)
        {
            return;
        }

        UpdateDistance();
	}

    private void UpdateDistance()
    {
        if (bgT == null)
        {
            GameObject background = GameObject.Find("Background1");
            if (background != null)
            {
                bgT = background.GetComponent<BackgroundTranform>();
            }
        }

        if (bgT == null)
        {
            return;
        }

        f_dis += bgT.moveSpeed * speedMultiplier * Time.deltaTime;
        dis = (int)f_dis;

        if (distanceText != null)
        {
            distanceText.text = dis.ToString();
        }
    }

    public int GetCurrentDistance()
    {
        return dis;
    }

    public void GameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        SoundManager.PlaySFX("shibai");
        if (saveScoreOnGameOver)
        {
            LeaderboardManager.SaveScore(LeaderboardManager.GetModeFromActiveScene(), dis);
        }

        GameObject[] bg = GameObject.FindGameObjectsWithTag("Background");
        foreach (GameObject i in bg)
        {
            BackgroundTranform backgroundTranform = i.GetComponent<BackgroundTranform>();
            if (backgroundTranform != null)
            {
                backgroundTranform.enabled = false;
            }
        }

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Animator playerAnimator = player.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.enabled = false;
            }

            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }
        }

        this.enabled = false;
        if (go != null)
        {
            go.SetActive(true);
        }

        if (GameOverStarted != null)
        {
            GameOverStarted();
        }
    }

    public void UpdateBonus(int count)
    {
        bonus += count;
        if (goldText != null)
        {
            goldText.text = bonus.ToString();
        }

    }

    public void RestartClick()
    {
        SceneTransitionManager.LoadScene(SceneManager.GetActiveScene().name);
        if (go != null)
        {
            go.SetActive(false);
        }
    }

    public void ExitClick()
    {
        SceneTransitionManager.LoadScene("Start");
    }
}
