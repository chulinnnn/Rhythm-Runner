using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class VerticalRunnerManager : MonoBehaviour
{
    private static bool registered;

    [Header("Mode")]
    public VerticalRunnerMode mode = VerticalRunnerMode.Game;
    public string targetGameSceneName = "Game";

    [Header("Settings")]
    public VerticalRunnerSettings settings = new VerticalRunnerSettings();

    private VerticalBeatSpawner spawner;
    private VerticalRunnerPlayer player;
    private VerticalRunnerCamera cameraController;
    private VerticalRunnerUI ui;
    private Sprite circleSprite;
    private int hearts;
    private int coins;
    private int combo;
    private int maxCombo;
    private int perfectCount;
    private int goodCount;
    private int missCount;
    private bool runEnded;
    private float startTime;
    private int tutorialStepIndex;
    private int tutorialStepProgress;
    private int tutorialLastAdvancedBeat;
    private VerticalTutorialStep[] tutorialSteps;
    private bool loadingNextScene;
    private bool waitingForBriefing;
    private bool waitingForGameRules;
    private bool gameRulesBeforeLoad;
    private const string GameRulesSeenKey = "VerticalRunner_GameRulesSeen";
    private float CameraBottom
    {
        get
        {
            Camera camera = Camera.main;
            return camera == null ? -999f : camera.transform.position.y - camera.orthographicSize;
        }
    }

    public float CameraBottomY { get { return CameraBottom; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (registered)
        {
            return;
        }

        registered = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (scene.name != "Game" && scene.name != "Tutorial")
        {
            return;
        }

        if (FindObjectOfType<VerticalRunnerManager>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("VerticalRunnerManager");
        VerticalRunnerManager manager = obj.AddComponent<VerticalRunnerManager>();
        manager.mode = scene.name == "Tutorial" ? VerticalRunnerMode.Tutorial : VerticalRunnerMode.Game;
    }

    private void Awake()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Game" && sceneName != "Tutorial")
        {
            enabled = false;
            return;
        }

        mode = sceneName == "Tutorial" ? VerticalRunnerMode.Tutorial : mode;
        BuildTutorialSteps();
        circleSprite = CreateCircleSprite("VerticalRunnerCircle", 96, Color.white);
        DisableLegacyRunnerObjects();
        EnsureCamera();
        ConfigureRhythmManager();
        ConfigureLegacyGameManager();
        BuildWorld();
    }

    private void Start()
    {
        startTime = Time.time;
        hearts = settings.heartCount;
        UpdateUi();
        if (mode == VerticalRunnerMode.Tutorial)
        {
            waitingForBriefing = true;
            if (player != null)
            {
                player.SetInputLocked(true);
            }
            ui.ShowTutorialBriefing();
        }
        else
        {
            ui.ShowGameIntro();
            if (PlayerPrefs.GetInt(GameRulesSeenKey, 0) == 0)
            {
                waitingForGameRules = true;
                if (player != null)
                {
                    player.SetInputLocked(true);
                }
                ui.ShowGameRules(false);
            }
        }
    }

    private void Update()
    {
        if (runEnded || waitingForBriefing || waitingForGameRules)
        {
            return;
        }

        if (player != null)
        {
            player.Tick();
        }
        if (cameraController != null)
        {
            cameraController.Tick();
        }

        float beatPosition = GetBeatPosition();
        if (ui != null)
        {
            ui.UpdateBeatLane(beatPosition);
        }

        if (mode == VerticalRunnerMode.Game && Time.time - startTime >= settings.songDurationSeconds)
        {
            CompleteRun();
        }

        UpdateUi();
    }

    public void ReportJumpInput(RhythmTimingResult result)
    {
        RecordRhythmResult(result);
    }

    public bool ReportCoinInput(RhythmTimingResult result)
    {
        RecordRhythmResult(result);
        bool success = result == RhythmTimingResult.Perfect || result == RhythmTimingResult.Good;
        if (!success && ui != null)
        {
            ui.ShowFeedback("Miss Coin", "Press Down/S on yellow", new Color(1f, 0.32f, 0.28f));
        }

        return success;
    }

    public bool ReportDirectionalDodge(RhythmTimingResult result, bool correctDirection)
    {
        bool rhythmSuccess = result == RhythmTimingResult.Perfect || result == RhythmTimingResult.Good;
        if (rhythmSuccess && correctDirection)
        {
            RecordRhythmResult(result);
            if (ui != null)
            {
                ui.ShowFeedback("Safe Dodge", "Good choice", new Color(0.27f, 0.95f, 0.54f));
            }
            return true;
        }

        RecordRhythmResult(RhythmTimingResult.Miss);
        string label = rhythmSuccess ? "DANGER" : "OFF BEAT";
        string hint = rhythmSuccess ? "Choose the SAFE arrow." : "Press Left/Right on yellow.";
        if (mode == VerticalRunnerMode.Tutorial && settings.dangerTutorialFailureRestartsLesson)
        {
            StartCoroutine(RestartTutorialStepRoutine(label, hint));
        }
        else
        {
            TakeDamage(label, hint, false);
        }

        return false;
    }

    public void ShowDirectionalChoiceHint()
    {
        if (ui != null)
        {
            ui.ShowFeedback("Choose SAFE", "Left/A or Right/D on yellow", new Color(1f, 0.86f, 0.18f));
        }
    }

    private void RecordRhythmResult(RhythmTimingResult result)
    {
        if (result == RhythmTimingResult.Perfect)
        {
            perfectCount++;
            combo++;
        }
        else if (result == RhythmTimingResult.Good)
        {
            goodCount++;
            combo++;
        }
        else
        {
            missCount++;
            combo = 0;
        }

        maxCombo = Mathf.Max(maxCombo, combo);
    }

    public void ShowJumpFeedback(RhythmTimingResult result, bool longJump)
    {
        if (ui == null)
        {
            return;
        }

        if (result == RhythmTimingResult.Perfect)
        {
            ui.ShowFeedback("Perfect Jump", longJump ? "Big beat!" : "", new Color(1f, 0.86f, 0.18f));
        }
        else if (result == RhythmTimingResult.Good)
        {
            ui.ShowFeedback("Good Jump", longJump ? "Big beat!" : "", new Color(0.25f, 0.86f, 1f));
        }
    }

    public void ReportPlatformLanded(VerticalRunnerPlatform platform)
    {
        if (platform == null)
        {
            return;
        }

        if (mode == VerticalRunnerMode.Tutorial)
        {
            AdvanceTutorialForLanding(platform);
        }
        else if (platform.beatIndex >= Mathf.CeilToInt(settings.songDurationSeconds / BeatInterval()))
        {
            CompleteRun();
        }
    }

    public void CollectCoin(int amount)
    {
        coins += Mathf.Max(1, amount);
        if (ui != null)
        {
            ui.ShowFeedback("Coin", "+1", new Color(1f, 0.86f, 0.18f));
        }

        if (mode == VerticalRunnerMode.Tutorial)
        {
            AdvanceTutorialForCoin();
        }
    }

    public void ShowCoinCollectHint()
    {
        if (ui != null)
        {
            ui.ShowFeedback("Coin Ready", "Down/S on yellow", new Color(1f, 0.86f, 0.18f));
        }
    }

    public void TakeDamage(string label, string hint, bool countMiss = true)
    {
        if (runEnded)
        {
            return;
        }

        if (combo >= settings.shieldComboRequirement)
        {
            combo = 0;
            if (ui != null)
            {
                ui.ShowFeedback("Beat Shield", "Saved you", new Color(0.25f, 0.86f, 1f));
            }
            return;
        }

        hearts--;
        combo = 0;
        if (countMiss)
        {
            missCount++;
        }
        if (ui != null)
        {
            ui.ShowFeedback(label, hint, new Color(1f, 0.32f, 0.28f));
        }

        if (hearts <= 0)
        {
            if (mode == VerticalRunnerMode.Tutorial)
            {
                StartCoroutine(RestartTutorialStepRoutine(label, hint));
                return;
            }

            EndRun(false);
            return;
        }

        StartCoroutine(RecoverRoutine());
    }

    public void CompleteRun()
    {
        if (runEnded)
        {
            return;
        }

        if (mode == VerticalRunnerMode.Tutorial)
        {
            if (!loadingNextScene)
            {
                loadingNextScene = true;
                StartCoroutine(ShowGameRulesAfterTutorial());
            }
            return;
        }

        EndRun(true);
    }

    public void BeginTutorialAfterBriefing()
    {
        if (mode != VerticalRunnerMode.Tutorial)
        {
            return;
        }

        waitingForBriefing = false;
        if (ui != null)
        {
            ui.HideTutorialBriefing();
        }
        if (player != null)
        {
            player.SetInputLocked(false);
        }
        StartTutorialStep(0);
    }

    public void ContinueAfterGameRules()
    {
        if (ui != null)
        {
            ui.HideGameRules();
        }

        if (gameRulesBeforeLoad)
        {
            PlayerPrefs.SetInt(GameRulesSeenKey, 1);
            PlayerPrefs.Save();
            SceneTransitionManager.LoadScene(targetGameSceneName);
            return;
        }

        PlayerPrefs.SetInt(GameRulesSeenKey, 1);
        PlayerPrefs.Save();
        waitingForGameRules = false;
        if (player != null)
        {
            player.SetInputLocked(false);
        }
    }

    private void BuildWorld()
    {
        spawner = gameObject.AddComponent<VerticalBeatSpawner>();
        spawner.Build(settings, mode, circleSprite);

        GameObject playerObject = new GameObject("VerticalRunnerPlayer");
        player = playerObject.AddComponent<VerticalRunnerPlayer>();
        player.Build(this, settings, spawner, circleSprite);

        cameraController = gameObject.AddComponent<VerticalRunnerCamera>();
        cameraController.Follow(player.transform);

        ui = gameObject.AddComponent<VerticalRunnerUI>();
        ui.Build(this, circleSprite);
        DrawBackground();
    }

    private void ConfigureRhythmManager()
    {
        RhythmManager rhythm = RhythmManager.Instance != null ? RhythmManager.Instance : FindObjectOfType<RhythmManager>();
        if (rhythm == null)
        {
            GameObject obj = new GameObject("RhythmManager");
            rhythm = obj.AddComponent<RhythmManager>();
        }

        settings.bpm = rhythm.bpm > 0f ? rhythm.bpm : settings.bpm;
        settings.firstBeatOffset = rhythm.firstBeatOffset;
        rhythm.bpm = settings.bpm;
        rhythm.visualizationBpm = settings.bpm;
        rhythm.useLevelTimeWhenMusicMissing = true;
        rhythm.levelTimeFallbackStart = Time.timeSinceLevelLoad;
        rhythm.SetVisualizationEnabled(false);
    }

    private void ConfigureLegacyGameManager()
    {
        GameManager gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.saveScoreOnGameOver = false;
        }
    }

    private void DisableLegacyRunnerObjects()
    {
        DisableLegacyComponentsEarly();

        BackgroundTranform[] backgrounds = FindObjectsOfType<BackgroundTranform>(true);
        for (int i = 0; i < backgrounds.Length; i++)
        {
            backgrounds[i].enabled = false;
            backgrounds[i].gameObject.SetActive(false);
        }

        Barrier[] barriers = FindObjectsOfType<Barrier>(true);
        for (int i = 0; i < barriers.Length; i++)
        {
            barriers[i].enabled = false;
        }

        TutorialFlowManager[] tutorialManagers = FindObjectsOfType<TutorialFlowManager>(true);
        for (int i = 0; i < tutorialManagers.Length; i++)
        {
            tutorialManagers[i].enabled = false;
        }

        TutorialUIController[] tutorialUis = FindObjectsOfType<TutorialUIController>(true);
        for (int i = 0; i < tutorialUis.Length; i++)
        {
            tutorialUis[i].gameObject.SetActive(false);
        }

        PlayerController[] oldPlayers = FindObjectsOfType<PlayerController>(true);
        for (int i = 0; i < oldPlayers.Length; i++)
        {
            oldPlayers[i].gameObject.SetActive(false);
        }

        HideLegacyUiByKeywords();
        GameObject gameOver = GameObject.Find("GameOver");
        if (gameOver != null)
        {
            gameOver.SetActive(false);
        }
    }

    private void HideLegacyUiByKeywords()
    {
        string[] keywords = { "score", "bonus", "distance", "gameover", "coin", "visualization", "correct", "miss", "rhythmvisualtogglebutton" };
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            GameObject obj = transforms[i].gameObject;
            if (!obj.scene.isLoaded || obj.name == "VerticalRunnerCanvas")
            {
                continue;
            }

            string lower = obj.name.ToLowerInvariant();
            bool match = false;
            for (int k = 0; k < keywords.Length; k++)
            {
                if (lower.Contains(keywords[k]))
                {
                    match = true;
                    break;
                }
            }

            if (match && obj.GetComponentInParent<Canvas>() != null && obj.GetComponentInParent<Canvas>().name != "VerticalRunnerCanvas")
            {
                CanvasRenderer canvasRenderer = obj.GetComponent<CanvasRenderer>();
                Graphic graphic = obj.GetComponent<Graphic>();
                if (canvasRenderer != null || graphic != null || lower.Contains("gameover"))
                {
                    obj.SetActive(false);
                }
            }
        }
    }

    private void DisableLegacyComponentsEarly()
    {
        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            if (behaviour is TutorialFlowManager
                || behaviour is TutorialUIController
                || behaviour is TutorialBeatSpawner
                || behaviour is BackgroundTranform
                || behaviour is Barrier)
            {
                behaviour.enabled = false;
            }
        }
    }

    private void StartTutorialStep(int index)
    {
        tutorialStepIndex = Mathf.Clamp(index, 0, tutorialSteps.Length - 1);
        tutorialStepProgress = 0;
        tutorialLastAdvancedBeat = -1;
        VerticalTutorialStep step = tutorialSteps[tutorialStepIndex];
        if (ui != null)
        {
            ui.ShowTutorialStep(step.title, step.instruction, step.objective, step.successRule, step.failureRule, tutorialStepProgress, step.requiredSuccesses, tutorialStepIndex + 1, tutorialSteps.Length);
        }
    }

    private void AdvanceTutorialForLanding(VerticalRunnerPlatform platform)
    {
        VerticalTutorialStep step = tutorialSteps[tutorialStepIndex];
        if (platform != null && platform.beatIndex == tutorialLastAdvancedBeat)
        {
            return;
        }

        if (step.type == VerticalTutorialStepType.CollectCoin)
        {
            return;
        }

        if (step.type == VerticalTutorialStepType.AvoidObstacle && (platform == null || !platform.isDangerBranchPlatform || !platform.isSafePlatform))
        {
            return;
        }

        if (step.type == VerticalTutorialStepType.LongJump && platform != null && !platform.longJump)
        {
            return;
        }

        tutorialLastAdvancedBeat = platform != null ? platform.beatIndex : tutorialLastAdvancedBeat;
        tutorialStepProgress++;
        if (tutorialStepProgress >= step.requiredSuccesses)
        {
            CompleteTutorialStep();
        }
        else
        {
            UpdateTutorialObjective();
        }
    }

    private void AdvanceTutorialForCoin()
    {
        VerticalTutorialStep step = tutorialSteps[tutorialStepIndex];
        if (step.type != VerticalTutorialStepType.CollectCoin && step.type != VerticalTutorialStepType.FinalMiniRun)
        {
            return;
        }

        tutorialStepProgress++;
        if (tutorialStepProgress >= step.requiredSuccesses)
        {
            CompleteTutorialStep();
        }
        else
        {
            UpdateTutorialObjective();
        }
    }

    private void UpdateTutorialObjective()
    {
        if (ui == null || tutorialSteps == null || tutorialStepIndex < 0 || tutorialStepIndex >= tutorialSteps.Length)
        {
            return;
        }

        VerticalTutorialStep step = tutorialSteps[tutorialStepIndex];
        ui.UpdateTutorialObjective(step.objective, step.successRule, step.failureRule, tutorialStepProgress, step.requiredSuccesses);
    }

    private void CompleteTutorialStep()
    {
        if (tutorialStepIndex >= tutorialSteps.Length - 1)
        {
            CompleteRun();
            return;
        }

        StartTutorialStep(tutorialStepIndex + 1);
    }

    private IEnumerator ShowGameRulesAfterTutorial()
    {
        if (ui != null)
        {
            ui.ShowFeedback("Tutorial Complete", "You are ready", new Color(1f, 0.86f, 0.18f));
        }
        yield return new WaitForSecondsRealtime(0.85f);
        waitingForGameRules = true;
        gameRulesBeforeLoad = true;
        if (player != null)
        {
            player.SetInputLocked(true);
        }
        if (ui != null)
        {
            ui.ShowGameRules(true);
        }
    }

    private IEnumerator RecoverRoutine()
    {
        if (player != null)
        {
            player.SetInputLocked(true);
        }
        yield return new WaitForSeconds(settings.playerRecoverDelay);
        if (player != null)
        {
            player.RecoverToSafePlatform();
            player.SetInputLocked(false);
        }
    }

    private IEnumerator RestartTutorialStepRoutine(string label, string hint)
    {
        if (player != null)
        {
            player.SetInputLocked(true);
        }
        if (ui != null)
        {
            ui.ShowFeedback("Lesson Retry", label + ": " + hint, new Color(1f, 0.72f, 0.18f));
        }
        yield return new WaitForSecondsRealtime(0.8f);
        hearts = settings.heartCount;
        tutorialStepProgress = 0;
        tutorialLastAdvancedBeat = -1;
        if (player != null)
        {
            player.RecoverToSafePlatform();
            player.SetInputLocked(false);
        }
        UpdateTutorialObjective();
    }

    private void EndRun(bool completed)
    {
        runEnded = true;
        if (player != null)
        {
            player.SetInputLocked(true);
        }
        if (ui != null)
        {
            ui.ShowResult(completed, coins, perfectCount, goodCount, missCount, maxCombo);
        }
    }

    private void UpdateUi()
    {
        if (ui == null)
        {
            return;
        }

        float progress = 0f;
        if (mode == VerticalRunnerMode.Game)
        {
            progress = Mathf.Clamp01((Time.time - startTime) / Mathf.Max(1f, settings.songDurationSeconds));
        }
        else
        {
            progress = tutorialSteps == null || tutorialSteps.Length == 0 ? 0f : Mathf.Clamp01((tutorialStepIndex + tutorialStepProgress * 0.2f) / tutorialSteps.Length);
        }

        ui.UpdateStats(hearts, coins, combo, maxCombo, progress);
    }

    private float GetBeatPosition()
    {
        RhythmManager rhythm = RhythmManager.Instance;
        if (rhythm != null)
        {
            return rhythm.GetAdjustedSongTime() / BeatInterval();
        }

        return (Time.timeSinceLevelLoad - settings.firstBeatOffset) / BeatInterval();
    }

    private float BeatInterval()
    {
        return 60f / Mathf.Max(1f, settings.bpm);
    }

    private void BuildTutorialSteps()
    {
        tutorialSteps = new[]
        {
            new VerticalTutorialStep(VerticalTutorialStepType.BeatJump, "Beat Jump", "Yellow means jump. Blue means wait.", "Jump to 1 mushroom", "Press Space on yellow and land", "Off beat loses a heart", 1),
            new VerticalTutorialStep(VerticalTutorialStepType.LandOnMushroom, "Land on Mushroom", "You do not need every beat. Jump when the next mushroom is ready.", "Land on 4 mushrooms", "Good/Perfect jump plus landing", "Falling restarts this lesson", 4),
            new VerticalTutorialStep(VerticalTutorialStepType.CollectCoin, "Collect Coin", "Coins sit on the rhythm path. Jump near them, then press Down or S on yellow.", "Collect 3 coins", "Press Down/S on beat while close to a yellow coin", "Miss keeps the coin in place", 3),
            new VerticalTutorialStep(VerticalTutorialStepType.AvoidObstacle, "Avoid Obstacle", "Watch the SAFE arrow. On yellow, press Left/A or Right/D.", "Pass 4 safe mushrooms", "Choose the SAFE side on beat", "Wrong side or off beat retries this lesson", 4),
            new VerticalTutorialStep(VerticalTutorialStepType.LongJump, "Long Jump", "Big yellow beat, big jump.", "Land on 1 far mushroom", "Use Space on the strong yellow beat", "Short jumps do not finish this lesson", 1),
            new VerticalTutorialStep(VerticalTutorialStepType.FinalMiniRun, "Final Mini Run", "Use what you learned. Reach the finish.", "Complete the mini run", "Jump, dodge, and collect on beat", "Hearts reaching 0 restarts this lesson", 4)
        };
    }

    private void DrawBackground()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.backgroundColor = settings.backgroundColor;
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private void EnsureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 1.5f, -10f);
    }

    private Sprite CreateCircleSprite(string spriteName, int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.47f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? color : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private struct VerticalTutorialStep
    {
        public readonly VerticalTutorialStepType type;
        public readonly string title;
        public readonly string instruction;
        public readonly string objective;
        public readonly string successRule;
        public readonly string failureRule;
        public readonly int requiredSuccesses;

        public VerticalTutorialStep(VerticalTutorialStepType type, string title, string instruction, string objective, string successRule, string failureRule, int requiredSuccesses)
        {
            this.type = type;
            this.title = title;
            this.instruction = instruction;
            this.objective = objective;
            this.successRule = successRule;
            this.failureRule = failureRule;
            this.requiredSuccesses = requiredSuccesses;
        }
    }
}
