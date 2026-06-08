using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class VerticalRunnerManager : MonoBehaviour
{
    private static bool registered;
    private const string VerticalRunnerSceneName = "VerticalRunner";

    [Header("Mode")]
    public VerticalRunnerMode mode = VerticalRunnerMode.Game;

    [Header("Settings")]
    public VerticalRunnerSettings settings = new VerticalRunnerSettings();

    [Header("Runtime scene policy")]
    public RuntimeScenePolicy scenePolicy = CreateDefaultScenePolicy();

    private VerticalBeatSpawner spawner;
    private VerticalRunnerPlayer player;
    private VerticalRunnerCamera cameraController;
    private VerticalRunnerUI ui;
    private VerticalRunnerTemplates templates;
    private Sprite circleSprite;
    private int coins;
    private int score;
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
    private bool showingTutorialCompleteRules;
    private bool waitingForBriefing;
    private bool waitingForGameRules;
    private bool waitingForCountdown;
    private float rhythmBaseFirstBeatOffset;
    private bool hasRhythmBaseFirstBeatOffset;
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

    public float CurrentBeatPosition { get { return GetBeatPosition(); } }

    public bool CanContinueRun { get { return !runEnded; } }

    private static RuntimeScenePolicy CreateDefaultScenePolicy()
    {
        return new RuntimeScenePolicy
        {
            useExistingSceneObjects = true,
            autoCreateMissingObjects = true,
            overrideCameraTransform = true,
            rebuildUiOnPlay = false,
            preserveExistingImageOverrides = false,
            runtimeGeneratedRootName = "VerticalRunnerRuntime"
        };
    }

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
        if (scene.name != VerticalRunnerSceneName)
        {
            return;
        }

        if (FindObjectOfType<VerticalRunnerManager>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("VerticalRunnerManager");
        VerticalRunnerManager manager = obj.AddComponent<VerticalRunnerManager>();
        manager.mode = VerticalRunnerMode.Tutorial;
    }

    private void Awake()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != VerticalRunnerSceneName)
        {
            enabled = false;
            return;
        }

        mode = VerticalRunnerMode.Tutorial;
        BuildTutorialSteps();
        circleSprite = CreateCircleSprite("VerticalRunnerCircle", 96, Color.white);
        DisableLegacyRunnerObjects();
        EnsureCamera();
        ConfigureRhythmManager();
        templates = FindSceneTemplates();
        BuildWorld();
        if (spawner == null || player == null || ui == null || !ui.IsReady)
        {
            Debug.LogWarning("VerticalRunnerManager: Required runtime objects are missing. Disabling manager.");
            enabled = false;
        }
    }

    private void Start()
    {
        if (!enabled || ui == null)
        {
            return;
        }

        startTime = Time.time;
        if (mode == VerticalRunnerMode.Tutorial)
        {
            StartTutorialRun(true, false);
        }
        else
        {
            StartGameRun(PlayerPrefs.GetInt(GameRulesSeenKey, 0) == 0, false);
        }
    }

    private void Update()
    {
        if (runEnded || waitingForBriefing || waitingForGameRules || waitingForCountdown)
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
            ui.UpdateBeatLane(beatPosition, settings.startBeat, settings.beatsPerPlatform);
        }

        if (mode == VerticalRunnerMode.Game && Time.time - startTime >= settings.songDurationSeconds)
        {
            CompleteRun();
        }

        UpdateUi();
    }

    public bool IsBeatInWindow(int beatIndex)
    {
        return IsCurrentBeatSlotForBeatIndex(beatIndex);
    }

    public bool HasPassedBeatWindow(int beatIndex)
    {
        return beatIndex >= 0 && GetBeatPosition() >= beatIndex + 1f;
    }

    public bool ReportJumpInput(VerticalRunnerPlatform next, out RhythmTimingResult result)
    {
        result = next != null ? JudgeJumpBeat() : RhythmTimingResult.Miss;
        bool success = IsTimingHit(result);
        RecordRhythmResult(success ? result : RhythmTimingResult.Miss);
        if (!success)
        {
            TakeDamage("Miss", "Space", false, false);
            return false;
        }

        AddScore(settings.jumpScore);
        return true;
    }

    public bool ReportCoinInput(VerticalRunnerPickup pickup, out RhythmTimingResult result)
    {
        result = pickup != null ? JudgeBetweenJumpBeat() : RhythmTimingResult.Miss;
        bool success = IsTimingHit(result);
        RecordRhythmResult(success ? result : RhythmTimingResult.Miss);
        if (!success)
        {
            MissPickup(pickup, "Banana", "Down/S", false);
            return false;
        }

        return true;
    }

    public bool ReportDirectionalDodge(VerticalRunnerPlatform origin, VerticalBranchChoice choice, bool spaceHeld, out RhythmTimingResult result)
    {
        result = origin != null ? JudgeBetweenJumpBeat() : RhythmTimingResult.Miss;
        bool timingHit = IsTimingHit(result);
        bool correctDirection = origin != null && choice == origin.safeChoice;
        bool accepted = timingHit && correctDirection && spaceHeld;
        RecordRhythmResult(accepted ? result : RhythmTimingResult.Miss);
        if (!accepted)
        {
            string hint = !spaceHeld ? "Space + Left/Right" : correctDirection ? "Beat" : "Avoid parrot";
            TakeDamage("Parrot", hint, false, false);
            return false;
        }

        AddScore(settings.parrotScore);
        if (ui != null)
        {
            ui.ShowParrotFeedback();
        }
        return true;
    }

    public void ReportMissedJump(VerticalRunnerPlatform next)
    {
        RecordRhythmResult(RhythmTimingResult.Miss);
        TakeDamage("Miss", "Space", false, false);
    }

    public void ReportMissedPickup(VerticalRunnerPickup pickup)
    {
        RecordRhythmResult(RhythmTimingResult.Miss);
        MissPickup(pickup, "Banana", "Down/S", false);
    }

    public void ReportMissedParrot(VerticalRunnerPlatform origin)
    {
        RecordRhythmResult(RhythmTimingResult.Miss);
        TakeDamage("Parrot", "Space + Left/Right", false, false);
    }

    public void ReportJumpInput(RhythmTimingResult result)
    {
        RecordRhythmResult(result);
    }

    public bool ReportCoinInput(RhythmTimingResult result)
    {
        RecordRhythmResult(result);
        bool success = IsTimingHit(result);
        if (!success)
        {
            TakeDamage("Banana", "Down/S", false, false);
        }

        return success;
    }

    public bool ReportDirectionalDodge(RhythmTimingResult result, bool correctDirection)
    {
        bool success = IsTimingHit(result) && correctDirection;
        RecordRhythmResult(success ? result : RhythmTimingResult.Miss);
        if (!success)
        {
            TakeDamage("Parrot", correctDirection ? "Beat" : "Avoid parrot", false, false);
            return false;
        }

        AddScore(settings.parrotScore);
        if (ui != null)
        {
            ui.ShowParrotFeedback();
        }
        return true;
    }

    public void ShowDirectionalChoiceHint()
    {
        if (ui != null)
        {
            ui.ShowFeedback("Parrot", "Space + Left/Right", new Color(1f, 0.86f, 0.18f));
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

    private RhythmTimingResult JudgeJumpBeat()
    {
        if (!IsCurrentJumpBeatSlot())
        {
            return RhythmTimingResult.Miss;
        }

        return JudgeCurrentBeatFraction();
    }

    private RhythmTimingResult JudgeBetweenJumpBeat()
    {
        if (IsCurrentJumpBeatSlot())
        {
            return RhythmTimingResult.Miss;
        }

        return JudgeCurrentBeatFraction();
    }

    private RhythmTimingResult JudgeCurrentBeatFraction()
    {
        float beatFraction = GetBeatPosition() - Mathf.Floor(GetBeatPosition());
        return beatFraction <= 0.45f ? RhythmTimingResult.Perfect : RhythmTimingResult.Good;
    }

    private bool IsCurrentJumpBeatSlot()
    {
        int currentBeat = Mathf.FloorToInt(GetBeatPosition());
        return PositiveModulo(currentBeat, Mathf.Max(1, settings.beatsPerPlatform)) == 0;
    }

    private bool IsCurrentBeatSlotForBeatIndex(int beatIndex)
    {
        if (beatIndex < 0)
        {
            return false;
        }

        int currentBeat = Mathf.FloorToInt(GetBeatPosition());
        return PositiveModulo(currentBeat - beatIndex, Mathf.Max(1, settings.beatsPerPlatform)) == 0;
    }

    private int PositiveModulo(int value, int modulo)
    {
        if (modulo <= 0)
        {
            return 0;
        }

        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private bool IsTimingHit(RhythmTimingResult result)
    {
        return result == RhythmTimingResult.Perfect || result == RhythmTimingResult.Good;
    }

    private void AddScore(int baseAmount)
    {
        score += Mathf.Max(0, baseAmount) + Mathf.Max(0, combo - 1) * Mathf.Max(0, settings.comboBonusStep);
        if (ui != null)
        {
            ui.ShowScoreFeedback();
        }
    }

    private void MissPickup(VerticalRunnerPickup pickup, string label, string hint, bool countMiss)
    {
        if (pickup != null)
        {
            pickup.missed = true;
        }
        TakeDamage(label, hint, countMiss, false);
    }

    public void ShowJumpFeedback(RhythmTimingResult result, bool longJump)
    {
        if (ui == null)
        {
            return;
        }

        if (result == RhythmTimingResult.Perfect)
        {
            ui.ShowJumpFeedback(longJump);
        }
        else if (result == RhythmTimingResult.Good)
        {
            ui.ShowJumpFeedback(longJump);
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
        AddScore(settings.bananaScore);
        if (ui != null)
        {
            ui.ShowBananaFeedback();
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
            ui.ShowFeedback("Banana", "Down/S", new Color(1f, 0.86f, 0.18f));
        }
    }

    public void TakeDamage(string label, string hint, bool countMiss = true, bool recover = true)
    {
        if (runEnded)
        {
            return;
        }

        combo = 0;
        if (countMiss)
        {
            missCount++;
        }
        if (ui != null)
        {
            ui.ShowMiss(label, hint);
        }

        if (recover)
        {
            StartCoroutine(RecoverRoutine());
        }
    }

    public void CompleteRun()
    {
        if (runEnded)
        {
            return;
        }

        if (mode == VerticalRunnerMode.Tutorial)
        {
            if (!showingTutorialCompleteRules)
            {
                showingTutorialCompleteRules = true;
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
            player.SetInputLocked(true);
        }
        StartTutorialStep(0);
        StartCoroutine(StartCountdownRoutine(null));
    }

    public void ContinueAfterGameRules()
    {
        if (ui != null)
        {
            ui.HideGameRules();
            ui.SetGameControlsVisible(false);
        }

        PlayerPrefs.SetInt(GameRulesSeenKey, 1);
        PlayerPrefs.Save();
        if (mode == VerticalRunnerMode.Tutorial)
        {
            StartGameRun(false, true);
            return;
        }

        waitingForGameRules = false;
        if (player != null)
        {
            player.SetInputLocked(true);
        }
        StartCoroutine(StartCountdownRoutine(null));
    }

    public void RestartCurrentRun()
    {
        StopAllCoroutines();
        if (mode == VerticalRunnerMode.Game)
        {
            StartGameRun(false, true);
        }
        else
        {
            StartTutorialRun(true, true);
        }
    }

    private void StartTutorialRun(bool showBriefing, bool rebuildWorld)
    {
        mode = VerticalRunnerMode.Tutorial;
        ResetRunState();
        if (rebuildWorld)
        {
            RebuildWorldForCurrentMode();
        }

        if (ui != null)
        {
            ui.HideResult();
            ui.HideGameRules();
            ui.SetGameControlsVisible(false);
        }
        UpdateUi();

        if (showBriefing)
        {
            waitingForBriefing = true;
            if (player != null)
            {
                player.SetInputLocked(true);
            }
            if (ui != null)
            {
                ui.ShowTutorialBriefing();
            }
        }
        else
        {
            if (ui != null)
            {
                ui.HideTutorialBriefing();
            }
            if (player != null)
            {
                player.SetInputLocked(true);
            }
            StartTutorialStep(0);
            StartCoroutine(StartCountdownRoutine(null));
        }
    }

    private void StartGameRun(bool showRules, bool rebuildWorld)
    {
        mode = VerticalRunnerMode.Game;
        ResetRunState();
        if (rebuildWorld)
        {
            RebuildWorldForCurrentMode();
        }

        if (ui != null)
        {
            ui.HideResult();
            ui.HideTutorialBriefing();
            ui.HideGameRules();
            ui.SetGameControlsVisible(false);
            ui.ShowGameIntro();
        }
        UpdateUi();

        if (showRules)
        {
            waitingForGameRules = true;
            if (player != null)
            {
                player.SetInputLocked(true);
            }
            if (ui != null)
            {
                ui.ShowGameRules(false);
            }
        }
        else if (player != null)
        {
            player.SetInputLocked(true);
            StartCoroutine(StartCountdownRoutine(null));
        }
    }

    private void ResetRunState()
    {
        coins = 0;
        score = 0;
        combo = 0;
        maxCombo = 0;
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
        runEnded = false;
        showingTutorialCompleteRules = false;
        waitingForBriefing = false;
        waitingForGameRules = false;
        waitingForCountdown = false;
        tutorialStepIndex = 0;
        tutorialStepProgress = 0;
        tutorialLastAdvancedBeat = -1;
        startTime = Time.time;
    }

    private void RebuildWorldForCurrentMode()
    {
        EnsureCamera();
        ConfigureRhythmManager();
        BuildWorld();
    }

    private void BuildWorld()
    {
        spawner = scenePolicy.useExistingSceneObjects ? FindObjectOfType<VerticalBeatSpawner>() : null;
        if (spawner == null)
        {
            if (!scenePolicy.autoCreateMissingObjects)
            {
                Debug.LogWarning("VerticalRunnerManager: VerticalBeatSpawner is missing and auto creation is disabled.");
                return;
            }

            spawner = gameObject.AddComponent<VerticalBeatSpawner>();
        }
        DestroyCurrentPlayer();
        spawner.Build(settings, mode, circleSprite, scenePolicy, templates);

        player = null;
        if (player == null)
        {
            if (!scenePolicy.autoCreateMissingObjects)
            {
                Debug.LogWarning("VerticalRunnerManager: VerticalRunnerPlayer is missing and auto creation is disabled.");
                return;
            }

            Transform runtimeRoot = templates != null ? templates.RuntimeRoot : scenePolicy.GetOrCreateRuntimeRoot("VerticalRunnerManager");
            GameObject playerObject = CreatePlayerObject(runtimeRoot);
            player = playerObject.GetComponent<VerticalRunnerPlayer>();
            if (player == null)
            {
                player = playerObject.AddComponent<VerticalRunnerPlayer>();
            }
        }
        player.Build(this, settings, spawner, circleSprite, templates);

        cameraController = scenePolicy.useExistingSceneObjects ? FindObjectOfType<VerticalRunnerCamera>() : null;
        if (cameraController == null)
        {
            cameraController = gameObject.AddComponent<VerticalRunnerCamera>();
        }
        cameraController.Follow(player.transform);

        ui = GetComponent<VerticalRunnerUI>();
        if (ui == null)
        {
            ui = gameObject.AddComponent<VerticalRunnerUI>();
        }
        ui.Build(this, circleSprite, scenePolicy);
        DrawBackground();
    }

    private void DestroyCurrentPlayer()
    {
        if (player == null)
        {
            return;
        }

        GameObject playerObject = player.gameObject;
        player = null;
        if (playerObject == null)
        {
            return;
        }

        playerObject.SetActive(false);
        if (Application.isPlaying)
        {
            Destroy(playerObject);
        }
        else
        {
            DestroyImmediate(playerObject);
        }
    }

    private VerticalRunnerTemplates FindSceneTemplates()
    {
        VerticalRunnerTemplates[] found = FindObjectsOfType<VerticalRunnerTemplates>(true);
        return found.Length > 0 ? found[0] : null;
    }

    private GameObject CreatePlayerObject(Transform runtimeRoot)
    {
        GameObject template = templates != null ? templates.playerTemplate : null;
        GameObject playerObject = template != null ? Instantiate(template) : new GameObject("VerticalRunnerPlayer");
        playerObject.name = "VerticalRunnerPlayer";
        if (runtimeRoot != null)
        {
            playerObject.transform.SetParent(runtimeRoot, false);
        }
        playerObject.SetActive(true);
        return playerObject;
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
        if (!hasRhythmBaseFirstBeatOffset)
        {
            rhythmBaseFirstBeatOffset = rhythm.firstBeatOffset;
            hasRhythmBaseFirstBeatOffset = true;
        }
        rhythm.firstBeatOffset = rhythmBaseFirstBeatOffset;
        settings.firstBeatOffset = rhythmBaseFirstBeatOffset;
        rhythm.bpm = settings.bpm;
        rhythm.visualizationBpm = settings.bpm;
        rhythm.useLevelTimeWhenMusicMissing = true;
        rhythm.levelTimeFallbackStart = Time.timeSinceLevelLoad;
        rhythm.SetVisualizationEnabled(false);
        rhythm.SetVisualizationToggleVisible(false);
        if (rhythm.musicSource != null)
        {
            rhythm.musicSource.playOnAwake = false;
            rhythm.musicSource.Stop();
            rhythm.musicSource.time = 0f;
        }
    }

    private IEnumerator StartCountdownRoutine(System.Action onCountdownComplete)
    {
        waitingForCountdown = true;
        if (player != null)
        {
            player.SetInputLocked(true);
        }

        int beats = Mathf.Max(0, settings.countdownBeats);
        float interval = BeatInterval();
        StartRhythmClock(beats * interval);
        startTime = Time.time;

        for (int remaining = beats; remaining > 0; remaining--)
        {
            if (ui != null)
            {
                ui.ShowFeedback(remaining.ToString(), "Listen", new Color(1f, 0.86f, 0.18f));
            }
            yield return new WaitForSeconds(interval);
        }

        if (ui != null)
        {
            ui.ShowFeedback("Go", "Climb", new Color(0.27f, 0.95f, 0.54f));
        }

        startTime = Time.time;
        waitingForCountdown = false;
        if (onCountdownComplete != null)
        {
            onCountdownComplete();
        }
        if (ui != null)
        {
            ui.SetGameControlsVisible(mode == VerticalRunnerMode.Game && !runEnded && !waitingForBriefing && !waitingForGameRules);
        }
        if (player != null && !runEnded && !waitingForBriefing && !waitingForGameRules)
        {
            player.SetInputLocked(false);
        }
    }

    private void StartRhythmClock(float gameplayDelaySeconds)
    {
        RhythmManager rhythm = RhythmManager.Instance != null ? RhythmManager.Instance : FindObjectOfType<RhythmManager>();
        if (rhythm == null)
        {
            return;
        }

        float startBeatSeconds = Mathf.Max(0, settings.startBeat) * BeatInterval();
        float offset = rhythmBaseFirstBeatOffset + Mathf.Max(0f, gameplayDelaySeconds - startBeatSeconds);
        rhythm.firstBeatOffset = offset;
        settings.firstBeatOffset = offset;
        rhythm.levelTimeFallbackStart = Time.timeSinceLevelLoad;
        if (rhythm.musicSource == null)
        {
            return;
        }

        rhythm.musicSource.Stop();
        rhythm.musicSource.time = 0f;
        rhythm.musicSource.Play();
    }

    private void DisableLegacyRunnerObjects()
    {
        DisableLegacyComponentsEarly();

        HideLegacyObjectsByName();
        HideLegacyUiByKeywords();
        GameObject gameOver = GameObject.Find("GameOver");
        if (gameOver != null)
        {
            gameOver.SetActive(false);
        }
    }

    private void HideLegacyObjectsByName()
    {
        string[] names = { "player", "gamemanager", "tutorialflowmanager", "tutorialuicontroller", "tutorialbeatspawner", "background1", "floor", "barrierspoint", "barrier" };
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            GameObject obj = transforms[i].gameObject;
            if (!obj.scene.isLoaded || obj.name == "VerticalRunnerManager" || obj.name == "VerticalRunnerCanvas" || obj.name == "VerticalRunnerTemplates" || obj.name == "VerticalRunnerRuntime")
            {
                continue;
            }

            string lower = obj.name.ToLowerInvariant();
            for (int k = 0; k < names.Length; k++)
            {
                if (lower == names[k] || lower.StartsWith(names[k]))
                {
                    obj.SetActive(false);
                    break;
                }
            }
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

            string typeName = behaviour.GetType().Name;
            if (typeName == "TutorialFlowManager"
                || typeName == "TutorialUIController"
                || typeName == "TutorialBeatSpawner"
                || typeName == "BackgroundTranform"
                || typeName == "Barrier"
                || typeName == "GameManager"
                || typeName == "PlayerController")
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
            ui.ShowTutorialStep(step.title, step.instruction, step.objective, step.hint, tutorialStepProgress, step.requiredCount, tutorialStepIndex + 1, tutorialSteps.Length);
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
        if (tutorialStepProgress >= step.requiredCount)
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
        if (tutorialStepProgress >= step.requiredCount)
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
        ui.UpdateTutorialObjective(step.objective, step.hint, tutorialStepProgress, step.requiredCount);
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
            ui.ShowFeedback("Ready", "Climb up", new Color(1f, 0.86f, 0.18f));
        }
        yield return new WaitForSecondsRealtime(0.85f);
        waitingForGameRules = true;
        if (player != null)
        {
            player.SetInputLocked(true);
        }
        if (ui != null)
        {
            ui.SetGameControlsVisible(false);
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
            player.SetInputLocked(runEnded || waitingForBriefing || waitingForGameRules || waitingForCountdown);
        }
    }

    private void EndRun(bool completed)
    {
        runEnded = true;
        if (player != null)
        {
            player.SetInputLocked(true);
        }
        if (completed)
        {
            score += Mathf.Max(0, settings.finishScore);
        }
        if (ui != null)
        {
            ui.SetGameControlsVisible(false);
            ui.ShowResult(completed, score, missCount, coins, maxCombo);
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
            if (tutorialSteps == null || tutorialSteps.Length == 0)
            {
                progress = 0f;
            }
            else
            {
                VerticalTutorialStep step = tutorialSteps[Mathf.Clamp(tutorialStepIndex, 0, tutorialSteps.Length - 1)];
                float stepProgress = Mathf.Clamp01(tutorialStepProgress / (float)Mathf.Max(1, step.requiredCount));
                progress = Mathf.Clamp01((tutorialStepIndex + stepProgress) / tutorialSteps.Length);
            }
        }

        ui.UpdateStats(missCount, score, coins, combo, maxCombo, progress);
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
            new VerticalTutorialStep(VerticalTutorialStepType.BeatJump, "Jump", "Monkey jumps up.", "Jump up", "Space", 1),
            new VerticalTutorialStep(VerticalTutorialStepType.LandOnMushroom, "Climb", "Jump, wait, jump.", "Climb up", "Space every 2 beats", 2),
            new VerticalTutorialStep(VerticalTutorialStepType.CollectCoin, "Banana", "Grab between jumps.", "Grab banana", "Down/S", 1),
            new VerticalTutorialStep(VerticalTutorialStepType.AvoidObstacle, "Parrot", "Move away between jumps.", "Avoid parrot", "Hold Space + Left/Right", 2),
            new VerticalTutorialStep(VerticalTutorialStepType.LongJump, "Big Jump", "Jump higher.", "Big jump", "Space", 1),
            new VerticalTutorialStep(VerticalTutorialStepType.FinalMiniRun, "Run", "Jump up. Grab banana. Avoid parrot.", "Banana climb", "Space  Down/S  Space+Left/Right", 2)
        };
    }

    private void DrawBackground()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        if (scenePolicy.useExistingSceneObjects && !scenePolicy.overrideCameraTransform)
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
            if (!scenePolicy.autoCreateMissingObjects)
            {
                Debug.LogWarning("VerticalRunnerManager: Main Camera is missing and auto creation is disabled.");
                return;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        if (scenePolicy.useExistingSceneObjects && !scenePolicy.overrideCameraTransform)
        {
            return;
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
        public readonly string hint;
        public readonly int requiredCount;

        public VerticalTutorialStep(VerticalTutorialStepType type, string title, string instruction, string objective, string hint, int requiredCount)
        {
            this.type = type;
            this.title = title;
            this.instruction = instruction;
            this.objective = objective;
            this.hint = hint;
            this.requiredCount = requiredCount;
        }
    }
}
