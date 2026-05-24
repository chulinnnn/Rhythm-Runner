using System.Collections;
using UnityEngine;

public enum TutorialState
{
    Intro,
    Countdown,
    Practice,
    Success,
    LoadGame,
    Failed
}

[DefaultExecutionOrder(-200)]
public class TutorialFlowManager : MonoBehaviour
{
    [Header("Flow")]
    public int requiredStreak = 8;
    public string targetSceneName = "Game";
    public float introDuration = 1.4f;
    public float countdownStepDuration = 0.7f;
    public float successDelay = 0.6f;

    [Header("Tutorial rhythm")]
    public float tutorialBpm = 126f;
    public int beatsBetweenObstacles = 2;
    public int beatObstacleCount = 16;
    public int firstObstacleBeat = 6;
    public GameObject backgroundPrefab;

    [Header("References")]
    public TutorialUIController uiController;
    public RhythmManager rhythmManager;
    public GameManager gameManager;

    private TutorialState state;
    private int currentStreak;
    private BackgroundTranform[] backgroundControllers;
    private PlayerController playerController;
    private Rigidbody2D playerRigidbody;
    private AudioSource musicSource;
    private GameObject gameOverObject;
    private bool successStarted;

    void Awake()
    {
        state = TutorialState.Intro;
        CacheRuntimeObjects();
        SetGameplayEnabled(false);
    }

    void Start()
    {
        CacheRuntimeObjects();
        ConfigureTutorialScene();
        EnsureUi();
        SubscribeEvents();
        StartCoroutine(TutorialRoutine());
    }

    void OnDestroy()
    {
        if (rhythmManager != null)
        {
            rhythmManager.InputReported -= OnRhythmInputReported;
        }

        if (gameManager != null)
        {
            gameManager.GameOverStarted -= OnGameOverStarted;
        }
    }

    private IEnumerator TutorialRoutine()
    {
        state = TutorialState.Intro;
        uiController.ShowIntro();
        yield return new WaitForSecondsRealtime(introDuration);

        state = TutorialState.Countdown;
        string[] countdown = { "3", "2", "1", "Go" };
        for (int i = 0; i < countdown.Length; i++)
        {
            uiController.ShowCountdown(countdown[i]);
            yield return new WaitForSecondsRealtime(countdownStepDuration);
        }

        StartPractice();
    }

    private void StartPractice()
    {
        state = TutorialState.Practice;
        currentStreak = 0;
        uiController.ShowPractice(currentStreak, requiredStreak);
        SetGameplayEnabled(true);

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.time = 0f;
            musicSource.Play();
        }
    }

    private void OnRhythmInputReported(RhythmTimingResult result, string actionName)
    {
        if (state != TutorialState.Practice)
        {
            return;
        }

        if (result == RhythmTimingResult.Perfect || result == RhythmTimingResult.Good)
        {
            currentStreak++;
        }
        else
        {
            currentStreak = 0;
        }

        uiController.ShowInputResult(result, currentStreak, requiredStreak);

        if (currentStreak >= requiredStreak && !successStarted)
        {
            successStarted = true;
            StartCoroutine(SuccessRoutine());
        }
    }

    private IEnumerator SuccessRoutine()
    {
        state = TutorialState.Success;
        SetGameplayEnabled(false);
        uiController.ShowSuccess();
        yield return new WaitForSecondsRealtime(successDelay);

        state = TutorialState.LoadGame;
        SceneTransitionManager.LoadScene(targetSceneName);
    }

    private void OnGameOverStarted()
    {
        if (state == TutorialState.Success || state == TutorialState.LoadGame || state == TutorialState.Failed)
        {
            return;
        }

        state = TutorialState.Failed;
        SetGameplayEnabled(false);
        if (gameOverObject != null)
        {
            gameOverObject.SetActive(false);
        }
        uiController.ShowFailure();
    }

    private void CacheRuntimeObjects()
    {
        if (rhythmManager == null)
        {
            rhythmManager = RhythmManager.Instance != null ? RhythmManager.Instance : FindObjectOfType<RhythmManager>();
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        }

        backgroundControllers = FindObjectsOfType<BackgroundTranform>();
        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerRigidbody = playerController.GetComponent<Rigidbody2D>();
        }

        if (rhythmManager != null && rhythmManager.musicSource != null)
        {
            musicSource = rhythmManager.musicSource;
        }
        else
        {
            musicSource = FindObjectOfType<AudioSource>();
        }

        if (gameOverObject == null)
        {
            gameOverObject = GameObject.Find("GameOver");
        }
    }

    private void EnsureUi()
    {
        if (uiController == null)
        {
            uiController = FindObjectOfType<TutorialUIController>();
        }

        if (uiController == null)
        {
            GameObject obj = new GameObject("TutorialUIController");
            uiController = obj.AddComponent<TutorialUIController>();
        }

        uiController.BindRhythmManager(rhythmManager);
        uiController.Configure(requiredStreak, tutorialBpm, beatsBetweenObstacles, firstObstacleBeat);
        uiController.EnsureUi();
    }

    private void SubscribeEvents()
    {
        if (rhythmManager != null)
        {
            rhythmManager.InputReported -= OnRhythmInputReported;
            rhythmManager.InputReported += OnRhythmInputReported;
        }

        if (gameManager != null)
        {
            gameManager.GameOverStarted -= OnGameOverStarted;
            gameManager.GameOverStarted += OnGameOverStarted;
        }
    }

    private void ConfigureTutorialScene()
    {
        if (gameManager != null)
        {
            gameManager.saveScoreOnGameOver = false;
        }

        SceneDifficultySettings settings = SceneDifficultySettings.Instance;
        if (settings != null)
        {
            settings.backgroundMoveSpeed = 8f;
            settings.extraSpeedMultiplier = 1f;
            settings.autoSpawnEnemies = true;
            settings.minBarrierPointIndex = 0;
            settings.maxBarrierPointIndex = 0;
            settings.minEnemiesPerPoint = 1;
            settings.maxEnemiesPerPoint = 1;
            settings.spawnEnemiesOnBeat = true;
            settings.obstacleBpm = tutorialBpm;
            settings.beatObstacleCount = beatObstacleCount;
            settings.beatsBetweenObstacles = beatsBetweenObstacles;
            settings.firstObstacleBeat = firstObstacleBeat;
            settings.playerMeetX = -4.5f;

            if (backgroundPrefab != null)
            {
                settings.mapPrefabs = new[] { backgroundPrefab };
            }
        }

        BackgroundTranform[] backgrounds = FindObjectsOfType<BackgroundTranform>(true);
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == null)
            {
                continue;
            }

            backgrounds[i].spawnBarriersOnStart = true;
            backgrounds[i].disableStaticBarrierChildren = true;
            if (backgroundPrefab != null)
            {
                backgrounds[i].mapPrefabs = new[] { backgroundPrefab };
            }

            Barrier barrier = backgrounds[i].GetComponent<Barrier>();
            if (barrier != null)
            {
                barrier.spawnOnBeat = true;
                barrier.bpm = tutorialBpm;
                barrier.beatObstacleCount = beatObstacleCount;
                barrier.beatsBetweenObstacles = beatsBetweenObstacles;
                barrier.firstObstacleBeat = firstObstacleBeat;
            }
        }

        if (rhythmManager != null)
        {
            rhythmManager.bpm = tutorialBpm;
            rhythmManager.visualizationBpm = tutorialBpm;
        }
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (backgroundControllers == null || backgroundControllers.Length == 0)
        {
            backgroundControllers = FindObjectsOfType<BackgroundTranform>();
        }

        for (int i = 0; i < backgroundControllers.Length; i++)
        {
            if (backgroundControllers[i] != null)
            {
                backgroundControllers[i].enabled = enabled;
            }
        }

        if (playerController != null)
        {
            playerController.enabled = enabled;
        }

        if (!enabled && playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
        }

        if (!enabled && musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }
}
