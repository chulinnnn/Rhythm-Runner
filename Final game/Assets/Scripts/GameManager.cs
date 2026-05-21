using UnityEngine;
using System.Collections;
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
    

    private BackgroundTranform bgT;

    public float speedMultiplier = 1f;

    void Awake()
    {
        _instance = this;
    }

	// Use this for initialization
	void Start () {
        goldText = GameObject.Find("GoldText").GetComponent<Text>();
        distanceText = GameObject.Find("DistanceText").GetComponent<Text>();
        bgT = GameObject.Find("Background1").GetComponent<BackgroundTranform>();
        go = GameObject.Find("GameOver");

        ApplySceneDifficulty();

        go.SetActive(false);
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
        UpdateDistance();
	}

    private void UpdateDistance()
    {
        f_dis += bgT.moveSpeed * speedMultiplier * Time.deltaTime;
        dis = (int)f_dis;
        
        distanceText.text = dis.ToString();
    }

    public int GetCurrentDistance()
    {
        return dis;
    }

    public void GameOver()
    {
        SoundManager.PlaySFX("shibai");
        LeaderboardManager.SaveScore(LeaderboardManager.GetModeFromActiveScene(), dis);

        GameObject[] bg = GameObject.FindGameObjectsWithTag("Background");
        foreach (GameObject i in bg)
        {
            i.GetComponent<BackgroundTranform>().enabled = false;
        }
        GameObject.Find("Player").GetComponent<Animator>().enabled = false;
        GameObject.Find("Player").GetComponent<PlayerController>().enabled = false;
        this.enabled = false;
        go.SetActive(true);
    }

    public void UpdateBonus(int count)
    {
        bonus += count;
        goldText.text = bonus.ToString();

    }

    public void RestartClick()
    {
        Application.LoadLevel(1);
        go.SetActive(false);
    }

    public void ExitClick()
    {
        Application.LoadLevel(0);
    }
}
