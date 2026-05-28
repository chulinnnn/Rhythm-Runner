using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum AdvancedRunnerMode
{
    Tutorial,
    Game
}

public enum AdvancedActionType
{
    Jump,
    Slide,
    LaneLeft,
    LaneRight,
    Coin,
    Rest
}

[System.Serializable]
public class AdvancedRunnerSettings
{
    public float bpm = 126f;
    public float firstBeatOffset = 0f;
    public float songDurationSeconds = 78f;
    public float judgementLineX = -4.5f;
    public float judgementLineY = -2.85f;
    public float targetBeatSpacingWorld = 2.15f;
    public float laneSpacing = 1.65f;
    public int tutorialHearts = 3;
    public int gameHearts = 4;
    public int scorePerHit = 100;
    public int comboBonusStep = 10;
    public float playerInputLockAfterHit = 0.08f;
    public Color backgroundColor = new Color(0.035f, 0.045f, 0.075f, 1f);
    public Color playerColor = new Color(0.18f, 0.92f, 1f, 1f);
    public Color jumpColor = new Color(1f, 0.82f, 0.22f, 1f);
    public Color slideColor = new Color(0.28f, 0.72f, 1f, 1f);
    public Color laneColor = new Color(0.7f, 0.46f, 1f, 1f);
    public Color coinColor = new Color(1f, 0.95f, 0.22f, 1f);
    public Color missColor = new Color(1f, 0.25f, 0.28f, 1f);
}

public class AdvancedBeatTarget
{
    public int beatIndex;
    public int laneIndex;
    public AdvancedActionType actionType;
    public bool isRequired;
    public int scoreValue;
    public string label;
    public bool judged;
    public GameObject visual;
}

[DefaultExecutionOrder(-1100)]
public class AdvancedRunnerManager : MonoBehaviour
{
    private static bool registered;

    public AdvancedRunnerMode mode = AdvancedRunnerMode.Game;
    public string targetGameSceneName = "Game2";
    public AdvancedRunnerSettings settings = new AdvancedRunnerSettings();

    private readonly List<AdvancedBeatTarget> targets = new List<AdvancedBeatTarget>();
    private AdvancedRunnerPlayer player;
    private AdvancedRunnerUI ui;
    private Sprite squareSprite;
    private Sprite circleSprite;
    private Transform targetRoot;
    private int hearts;
    private int score;
    private int combo;
    private int maxCombo;
    private int perfectCount;
    private int goodCount;
    private int missCount;
    private int nextTargetIndex;
    private int tutorialStepIndex;
    private int tutorialStepProgress;
    private bool runEnded;
    private bool waitingForStart;
    private bool loadingGame;
    private bool useLevelTimeClock;
    private float runStartTime;
    private float inputLockedUntil;
    private AdvancedTutorialStep[] tutorialSteps;

    private float BeatInterval { get { return 60f / Mathf.Max(1f, settings.bpm); } }

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
        if (scene.name != "Game2" && scene.name != "AdvancedTutorial")
        {
            return;
        }

        if (FindObjectOfType<AdvancedRunnerManager>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("AdvancedRunnerManager");
        AdvancedRunnerManager manager = obj.AddComponent<AdvancedRunnerManager>();
        manager.mode = scene.name == "AdvancedTutorial" ? AdvancedRunnerMode.Tutorial : AdvancedRunnerMode.Game;
    }

    private void Awake()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Game2" && sceneName != "AdvancedTutorial")
        {
            enabled = false;
            return;
        }

        mode = sceneName == "AdvancedTutorial" ? AdvancedRunnerMode.Tutorial : AdvancedRunnerMode.Game;
        BuildTutorialSteps();
        squareSprite = CreateSolidSprite("AdvancedSquare", 8, Color.white);
        circleSprite = CreateCircleSprite("AdvancedCircle", 96, Color.white);
        DisableLegacyRunnerObjects();
        EnsureCamera();
        ConfigureRhythmManager();
        BuildRuntimeObjects();
    }

    private void Start()
    {
        hearts = mode == AdvancedRunnerMode.Tutorial ? settings.tutorialHearts : settings.gameHearts;
        runStartTime = Time.time;
        waitingForStart = true;
        if (mode == AdvancedRunnerMode.Tutorial)
        {
            ui.ShowBriefing(
                "Advanced Tutorial",
                "Four-key rhythm runner\n\nSpace/Up: Jump\nDown: Slide\nLeft/Right: Change lane\n\nTargets fall into the yellow line. Clear five lessons to enter Advanced Game.",
                "Start Tutorial",
                BeginTutorial);
        }
        else
        {
            ui.ShowBriefing(
                "Advanced Runner",
                "Hit the correct action as it reaches the bottom yellow line.\nWrong key, wrong lane, or late timing costs a heart.\nBuild combo to push your score.",
                "Start Run",
                BeginGame);
        }

        UpdateUi();
    }

    private void Update()
    {
        if (runEnded || waitingForStart)
        {
            return;
        }

        float beatPosition = GetBeatPosition();
        player.Tick();
        UpdateTargets(beatPosition);
        CheckLateTargets(beatPosition);

        if (InputIsAllowed())
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                HandleInput(AdvancedActionType.Jump);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                HandleInput(AdvancedActionType.Slide);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                HandleInput(AdvancedActionType.LaneLeft);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                HandleInput(AdvancedActionType.LaneRight);
            }
        }

        if (mode == AdvancedRunnerMode.Game && Time.time - runStartTime >= settings.songDurationSeconds)
        {
            EndRun(true);
        }

        UpdateUi();
    }

    private void BeginTutorial()
    {
        waitingForStart = false;
        StartRhythmClock();
        tutorialStepIndex = 0;
        StartTutorialStep(0);
    }

    private void BeginGame()
    {
        waitingForStart = false;
        StartRhythmClock();
        runStartTime = Time.time;
        BuildGameChart();
        ui.ShowGameIntro();
    }

    private void StartTutorialStep(int index)
    {
        tutorialStepIndex = Mathf.Clamp(index, 0, tutorialSteps.Length - 1);
        tutorialStepProgress = 0;
        ResetRunObjects();
        BuildTutorialChart(tutorialSteps[tutorialStepIndex]);
        player.ResetToLane(1);
        ui.ShowTutorialStep(tutorialSteps[tutorialStepIndex].title, tutorialSteps[tutorialStepIndex].instruction, tutorialStepProgress, tutorialSteps[tutorialStepIndex].requiredHits, tutorialStepIndex + 1, tutorialSteps.Length);
    }

    private void BuildRuntimeObjects()
    {
        targetRoot = new GameObject("AdvancedTargets").transform;

        GameObject playerObject = new GameObject("AdvancedRunnerPlayer");
        player = playerObject.AddComponent<AdvancedRunnerPlayer>();
        player.Build(settings, circleSprite);

        ui = gameObject.AddComponent<AdvancedRunnerUI>();
        ui.Build(this, settings, circleSprite);
        DrawWorldGuide();
    }

    private void ResetRunObjects()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].visual != null)
            {
                Destroy(targets[i].visual);
            }
        }

        targets.Clear();
        nextTargetIndex = 0;
        if (targetRoot != null)
        {
            for (int i = targetRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(targetRoot.GetChild(i).gameObject);
            }
        }
    }

    private void BuildTutorialChart(AdvancedTutorialStep step)
    {
        int beat = Mathf.Max(4, Mathf.CeilToInt(GetBeatPosition()) + 4);
        int lane = 1;
        for (int i = 0; i < step.requiredHits; i++)
        {
            AdvancedActionType action = step.GetAction(i);
            if (action == AdvancedActionType.LaneLeft)
            {
                lane = Mathf.Max(0, lane - 1);
            }
            else if (action == AdvancedActionType.LaneRight)
            {
                lane = Mathf.Min(2, lane + 1);
            }

            AddTarget(beat + i * 3, lane, action, true, step.key.ToUpperInvariant());
        }
    }

    private void BuildGameChart()
    {
        ResetRunObjects();
        player.ResetToLane(1);

        int totalBeats = Mathf.CeilToInt(settings.songDurationSeconds / BeatInterval);
        int lane = 1;
        for (int beat = 4; beat < totalBeats; beat += 2)
        {
            int pattern = (beat / 2) % 8;
            AdvancedActionType action;
            if (pattern == 1)
            {
                action = AdvancedActionType.Slide;
            }
            else if (pattern == 3)
            {
                action = lane > 0 ? AdvancedActionType.LaneLeft : AdvancedActionType.LaneRight;
            }
            else if (pattern == 5)
            {
                action = lane < 2 ? AdvancedActionType.LaneRight : AdvancedActionType.LaneLeft;
            }
            else if (pattern == 6)
            {
                action = AdvancedActionType.Coin;
            }
            else
            {
                action = AdvancedActionType.Jump;
            }

            if (action == AdvancedActionType.LaneLeft)
            {
                lane = Mathf.Max(0, lane - 1);
            }
            else if (action == AdvancedActionType.LaneRight)
            {
                lane = Mathf.Min(2, lane + 1);
            }

            AddTarget(beat, lane, action, true, GetActionLabel(action));
        }
    }

    private void AddTarget(int beatIndex, int laneIndex, AdvancedActionType actionType, bool required, string label)
    {
        AdvancedBeatTarget target = new AdvancedBeatTarget();
        target.beatIndex = beatIndex;
        target.laneIndex = Mathf.Clamp(laneIndex, 0, 2);
        target.actionType = actionType;
        target.isRequired = required;
        target.scoreValue = actionType == AdvancedActionType.Coin ? settings.scorePerHit * 2 : settings.scorePerHit;
        target.label = label;
        target.visual = CreateTargetVisual(target);
        targets.Add(target);
    }

    private GameObject CreateTargetVisual(AdvancedBeatTarget target)
    {
        GameObject obj = new GameObject("AdvancedTarget_" + target.actionType + "_Beat_" + target.beatIndex);
        obj.transform.SetParent(targetRoot, false);

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = target.actionType == AdvancedActionType.Coin ? circleSprite : squareSprite;
        renderer.color = GetActionColor(target.actionType);
        renderer.sortingOrder = 3;
        obj.transform.localScale = GetTargetBaseScale(target.actionType);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(obj.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        labelObject.transform.localScale = Vector3.one;

        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = GetActionLabel(target.actionType);
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 42;
        text.characterSize = 0.055f;
        text.color = Color.white;
        MeshRenderer textRenderer = labelObject.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingOrder = 5;
        }

        return obj;
    }

    private void UpdateTargets(float beatPosition)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            AdvancedBeatTarget target = targets[i];
            if (target.visual == null || target.judged)
            {
                continue;
            }

            float x = GetLaneX(target.laneIndex);
            float y = settings.judgementLineY + (target.beatIndex - beatPosition) * settings.targetBeatSpacingWorld;
            target.visual.transform.position = new Vector3(x, y, 0f);
            float distance = Mathf.Abs(target.beatIndex - beatPosition);
            float pulse = Mathf.Clamp01(1f - distance / 0.35f);
            target.visual.transform.localScale = GetTargetBaseScale(target.actionType) * Mathf.Lerp(1f, 1.18f, pulse);
        }
    }

    private void CheckLateTargets(float beatPosition)
    {
        while (nextTargetIndex < targets.Count && targets[nextTargetIndex].judged)
        {
            nextTargetIndex++;
        }

        if (nextTargetIndex >= targets.Count)
        {
            if (mode == AdvancedRunnerMode.Tutorial && !loadingGame)
            {
                CompleteTutorialStep();
            }
            return;
        }

        AdvancedBeatTarget target = targets[nextTargetIndex];
        if (beatPosition - target.beatIndex > 0.45f)
        {
            RegisterMiss(target, "LATE", "Hit on the bottom yellow line.");
        }
    }

    private void HandleInput(AdvancedActionType inputAction)
    {
        AdvancedBeatTarget target = GetCurrentTarget();
        if (target == null)
        {
            ui.ShowFeedback("Wait", "Hit when target reaches the bottom yellow line", new Color(0.72f, 0.92f, 1f));
            return;
        }

        RhythmTimingResult result = RhythmManager.Instance != null ? RhythmManager.Instance.ReportInput(GetActionLabel(inputAction)) : RhythmTimingResult.None;
        bool timingSuccess = result == RhythmTimingResult.Perfect || result == RhythmTimingResult.Good;
        bool actionSuccess = inputAction == target.actionType || (target.actionType == AdvancedActionType.Coin && (inputAction == AdvancedActionType.Jump || inputAction == AdvancedActionType.Slide));

        if (inputAction == AdvancedActionType.LaneLeft)
        {
            player.MoveLane(-1);
        }
        else if (inputAction == AdvancedActionType.LaneRight)
        {
            player.MoveLane(1);
        }
        else if (inputAction == AdvancedActionType.Jump)
        {
            player.PlayJump();
        }
        else if (inputAction == AdvancedActionType.Slide)
        {
            player.PlaySlide();
        }

        bool laneSuccess = player.CurrentLane == target.laneIndex;
        if (!timingSuccess || !actionSuccess || !laneSuccess)
        {
            RegisterMiss(target, actionSuccess ? "MISS" : "WRONG", GetMissHint(target, timingSuccess, actionSuccess, laneSuccess));
            return;
        }

        RegisterHit(target, result);
    }

    private AdvancedBeatTarget GetCurrentTarget()
    {
        while (nextTargetIndex < targets.Count && targets[nextTargetIndex].judged)
        {
            nextTargetIndex++;
        }

        if (nextTargetIndex >= targets.Count)
        {
            return null;
        }

        AdvancedBeatTarget target = targets[nextTargetIndex];
        float distance = Mathf.Abs(GetBeatPosition() - target.beatIndex);
        return distance <= 0.45f ? target : null;
    }

    private void RegisterHit(AdvancedBeatTarget target, RhythmTimingResult result)
    {
        target.judged = true;
        HideTarget(target);
        combo++;
        maxCombo = Mathf.Max(maxCombo, combo);
        if (result == RhythmTimingResult.Perfect)
        {
            perfectCount++;
        }
        else
        {
            goodCount++;
        }

        score += target.scoreValue + combo * settings.comboBonusStep;
        inputLockedUntil = Time.time + settings.playerInputLockAfterHit;
        ui.ShowFeedback(result == RhythmTimingResult.Perfect ? "Perfect" : "Good", target.label, result == RhythmTimingResult.Perfect ? settings.jumpColor : settings.slideColor);

        if (mode == AdvancedRunnerMode.Tutorial)
        {
            tutorialStepProgress++;
            AdvancedTutorialStep step = tutorialSteps[tutorialStepIndex];
            ui.ShowTutorialStep(step.title, step.instruction, tutorialStepProgress, step.requiredHits, tutorialStepIndex + 1, tutorialSteps.Length);
            if (tutorialStepProgress >= step.requiredHits)
            {
                CompleteTutorialStep();
            }
        }
    }

    private void RegisterMiss(AdvancedBeatTarget target, string label, string hint)
    {
        target.judged = true;
        HideTarget(target);
        missCount++;
        combo = 0;
        ui.ShowFeedback(label, hint, settings.missColor);

        if (mode == AdvancedRunnerMode.Tutorial)
        {
            StartCoroutine(RestartTutorialStepRoutine(label, hint));
            return;
        }

        hearts--;
        if (hearts <= 0)
        {
            EndRun(false);
        }
    }

    private IEnumerator RestartTutorialStepRoutine(string label, string hint)
    {
        waitingForStart = true;
        yield return new WaitForSecondsRealtime(0.75f);
        waitingForStart = false;
        StartTutorialStep(tutorialStepIndex);
    }

    private void CompleteTutorialStep()
    {
        if (loadingGame)
        {
            return;
        }

        if (tutorialStepIndex >= tutorialSteps.Length - 1)
        {
            loadingGame = true;
            StartCoroutine(LoadGameAfterTutorial());
            return;
        }

        StartTutorialStep(tutorialStepIndex + 1);
    }

    private IEnumerator LoadGameAfterTutorial()
    {
        ui.ShowFeedback("Tutorial Clear", "Entering Advanced Game", settings.coinColor);
        yield return new WaitForSecondsRealtime(0.9f);
        SceneTransitionManager.LoadScene(targetGameSceneName);
    }

    private void EndRun(bool completed)
    {
        if (runEnded)
        {
            return;
        }

        runEnded = true;
        if (mode == AdvancedRunnerMode.Game)
        {
            LeaderboardManager.SaveScore(LeaderboardMode.Hard, Mathf.Max(1, score));
        }
        ui.ShowResult(completed, score, perfectCount, goodCount, missCount, maxCombo);
    }

    private bool InputIsAllowed()
    {
        return Time.time >= inputLockedUntil;
    }

    private void HideTarget(AdvancedBeatTarget target)
    {
        if (target.visual != null)
        {
            target.visual.SetActive(false);
        }
    }

    private void UpdateUi()
    {
        float progress = mode == AdvancedRunnerMode.Game
            ? Mathf.Clamp01((Time.time - runStartTime) / Mathf.Max(1f, settings.songDurationSeconds))
            : tutorialSteps == null || tutorialSteps.Length == 0 ? 0f : Mathf.Clamp01((tutorialStepIndex + tutorialStepProgress / Mathf.Max(1f, tutorialSteps[tutorialStepIndex].requiredHits)) / tutorialSteps.Length);
        ui.UpdateStats(hearts, score, combo, maxCombo, progress, GetBeatPosition());
    }

    private float GetBeatPosition()
    {
        if (useLevelTimeClock)
        {
            return (Time.timeSinceLevelLoad - settings.firstBeatOffset) / BeatInterval;
        }

        RhythmManager rhythm = RhythmManager.Instance;
        if (rhythm != null)
        {
            return rhythm.GetAdjustedSongTime() / BeatInterval;
        }

        return (Time.timeSinceLevelLoad - settings.firstBeatOffset) / BeatInterval;
    }

    public float GetLaneX(int lane)
    {
        return (Mathf.Clamp(lane, 0, 2) - 1) * settings.laneSpacing;
    }

    private Color GetActionColor(AdvancedActionType action)
    {
        if (action == AdvancedActionType.Jump)
        {
            return settings.jumpColor;
        }
        if (action == AdvancedActionType.Slide)
        {
            return settings.slideColor;
        }
        if (action == AdvancedActionType.Coin)
        {
            return settings.coinColor;
        }
        return settings.laneColor;
    }

    private Vector3 GetTargetBaseScale(AdvancedActionType action)
    {
        if (action == AdvancedActionType.Coin)
        {
            return new Vector3(0.44f, 0.44f, 1f);
        }

        if (action == AdvancedActionType.LaneLeft || action == AdvancedActionType.LaneRight)
        {
            return new Vector3(0.86f, 0.46f, 1f);
        }

        return new Vector3(0.78f, 0.52f, 1f);
    }

    private string GetActionLabel(AdvancedActionType action)
    {
        if (action == AdvancedActionType.Jump)
        {
            return "JUMP";
        }
        if (action == AdvancedActionType.Slide)
        {
            return "DOWN";
        }
        if (action == AdvancedActionType.LaneLeft)
        {
            return "LEFT";
        }
        if (action == AdvancedActionType.LaneRight)
        {
            return "RIGHT";
        }
        if (action == AdvancedActionType.Coin)
        {
            return "COIN";
        }
        return "WAIT";
    }

    private string GetMissHint(AdvancedBeatTarget target, bool timingSuccess, bool actionSuccess, bool laneSuccess)
    {
        if (!timingSuccess)
        {
            return "Use the yellow beat.";
        }
        if (!actionSuccess)
        {
            return "Next action: " + GetActionLabel(target.actionType);
        }
        if (!laneSuccess)
        {
            return "Move to lane " + (target.laneIndex + 1);
        }
        return "Try again.";
    }

    private void ConfigureRhythmManager()
    {
        RhythmManager rhythm = RhythmManager.Instance != null ? RhythmManager.Instance : FindObjectOfType<RhythmManager>();
        AudioSource source = rhythm != null ? rhythm.musicSource : null;
        if (source == null)
        {
            source = FindAdvancedAudioSource();
        }

        if (rhythm == null)
        {
            GameObject obj = new GameObject("RhythmManager");
            if (source == null)
            {
                source = obj.AddComponent<AudioSource>();
            }
            rhythm = obj.AddComponent<RhythmManager>();
        }

        if (source == null)
        {
            GameObject audioObject = new GameObject("AdvancedBeatMusic");
            source = audioObject.AddComponent<AudioSource>();
        }

        settings.bpm = rhythm.bpm > 0f ? rhythm.bpm : settings.bpm;
        settings.firstBeatOffset = rhythm.firstBeatOffset;
        useLevelTimeClock = EnsureAdvancedMusicSource(source);
        rhythm.musicSource = source;
        rhythm.bpm = settings.bpm;
        rhythm.visualizationBpm = settings.bpm;
        rhythm.useLevelTimeWhenMusicMissing = true;
        rhythm.levelTimeFallbackStart = Time.timeSinceLevelLoad;
        rhythm.awardRhythmScore = false;
        rhythm.SetVisualizationEnabled(false);

        if (source.isPlaying)
        {
            source.Stop();
        }
        source.time = 0f;
    }

    private void StartRhythmClock()
    {
        RhythmManager rhythm = RhythmManager.Instance != null ? RhythmManager.Instance : FindObjectOfType<RhythmManager>();
        settings.firstBeatOffset = Time.timeSinceLevelLoad;
        if (rhythm == null)
        {
            return;
        }

        rhythm.levelTimeFallbackStart = Time.timeSinceLevelLoad;
        if (rhythm.musicSource == null)
        {
            return;
        }

        rhythm.musicSource.Stop();
        rhythm.musicSource.time = 0f;
        rhythm.musicSource.Play();
    }

    private AudioSource FindAdvancedAudioSource()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] == null || !sources[i].gameObject.activeInHierarchy)
            {
                continue;
            }

            string lower = sources[i].gameObject.name.ToLowerInvariant();
            if (lower.Contains("advanced") || lower.Contains("126") || lower.Contains("dm_"))
            {
                return sources[i];
            }
        }

        return null;
    }

    private bool EnsureAdvancedMusicSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = source.volume <= 0.01f ? 0.38f : source.volume;
        if (source.clip == null)
        {
            source.clip = CreateAdvancedBeatLoopClip();
            return true;
        }

        return source.clip.name == "Advanced126BpmBeatLoop";
    }

    private AudioClip CreateAdvancedBeatLoopClip()
    {
        int sampleRate = 44100;
        float beatInterval = 60f / Mathf.Max(1f, settings.bpm);
        int beatsPerLoop = 4;
        int sampleCount = Mathf.CeilToInt(sampleRate * beatInterval * beatsPerLoop);
        float[] samples = new float[sampleCount];

        for (int beat = 0; beat < beatsPerLoop; beat++)
        {
            float frequency = beat == 0 ? 880f : 560f;
            float gain = beat == 0 ? 0.48f : 0.32f;
            int start = Mathf.RoundToInt(beat * beatInterval * sampleRate);
            int length = Mathf.Min(Mathf.RoundToInt(0.07f * sampleRate), sampleCount - start);
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (float)i / Mathf.Max(1, length);
                samples[start + i] += Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * gain;
            }
        }

        AudioClip clip = AudioClip.Create("Advanced126BpmBeatLoop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void DisableLegacyRunnerObjects()
    {
        PlayerController[] oldPlayers = FindObjectsOfType<PlayerController>(true);
        for (int i = 0; i < oldPlayers.Length; i++)
        {
            oldPlayers[i].gameObject.SetActive(false);
        }

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

        GameManager gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.enabled = false;
            gameManager.saveScoreOnGameOver = false;
        }

        GameObject gameOver = GameObject.Find("GameOver");
        if (gameOver != null)
        {
            gameOver.SetActive(false);
        }

        HideLegacyUiByKeywords();
    }

    private void HideLegacyUiByKeywords()
    {
        string[] keywords = { "gold", "distance", "score", "bonus", "gameover", "coin", "leaderboard" };
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            GameObject obj = transforms[i].gameObject;
            if (!obj.scene.isLoaded || obj.name == "AdvancedRunnerCanvas")
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

            if (match && obj.GetComponentInParent<Canvas>() != null)
            {
                obj.SetActive(false);
            }
        }
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
        camera.orthographicSize = 4.8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = settings.backgroundColor;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private void DrawWorldGuide()
    {
        for (int lane = 0; lane < 3; lane++)
        {
            GameObject line = new GameObject("AdvancedLane_" + lane);
            SpriteRenderer renderer = line.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = new Color(1f, 1f, 1f, lane == 1 ? 0.16f : 0.09f);
            renderer.sortingOrder = -2;
            line.transform.position = new Vector3(GetLaneX(lane), 0.55f, 0f);
            line.transform.localScale = new Vector3(0.05f, 7.6f, 1f);
        }

        GameObject judge = new GameObject("AdvancedJudgementLine");
        SpriteRenderer judgeRenderer = judge.AddComponent<SpriteRenderer>();
        judgeRenderer.sprite = squareSprite;
        judgeRenderer.color = new Color(1f, 0.86f, 0.18f, 0.78f);
        judgeRenderer.sortingOrder = -1;
        judge.transform.position = new Vector3(0f, settings.judgementLineY, 0f);
        judge.transform.localScale = new Vector3(6.4f, 0.08f, 1f);
    }

    private void BuildTutorialSteps()
    {
        tutorialSteps = new[]
        {
            new AdvancedTutorialStep("jump", "Beat Jump", "Press Space or Up on yellow.", 3, new[] { AdvancedActionType.Jump, AdvancedActionType.Jump, AdvancedActionType.Jump }),
            new AdvancedTutorialStep("slide", "Slide Gate", "Press Down on yellow.", 3, new[] { AdvancedActionType.Slide, AdvancedActionType.Slide, AdvancedActionType.Slide }),
            new AdvancedTutorialStep("lane", "Lane Switch", "Press Left or Right as the lane target reaches the bottom line.", 4, new[] { AdvancedActionType.LaneLeft, AdvancedActionType.LaneRight, AdvancedActionType.LaneRight, AdvancedActionType.LaneLeft }),
            new AdvancedTutorialStep("coin", "Coin Line", "Move left or right into the coin lane, then hit coin beats with Space or Down.", 4, new[] { AdvancedActionType.LaneRight, AdvancedActionType.Coin, AdvancedActionType.LaneLeft, AdvancedActionType.Coin }),
            new AdvancedTutorialStep("mix", "Final Mix", "Read the lane and action together.", 6, new[] { AdvancedActionType.Jump, AdvancedActionType.Slide, AdvancedActionType.LaneRight, AdvancedActionType.Coin, AdvancedActionType.LaneLeft, AdvancedActionType.Jump })
        };
    }

    private Sprite CreateSolidSprite(string name, int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Sprite CreateCircleSprite(string name, int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= radius ? color : clear);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private struct AdvancedTutorialStep
    {
        public readonly string key;
        public readonly string title;
        public readonly string instruction;
        public readonly int requiredHits;
        private readonly AdvancedActionType[] pattern;

        public AdvancedTutorialStep(string key, string title, string instruction, int requiredHits, AdvancedActionType[] pattern)
        {
            this.key = key;
            this.title = title;
            this.instruction = instruction;
            this.requiredHits = requiredHits;
            this.pattern = pattern;
        }

        public AdvancedActionType GetAction(int index)
        {
            return pattern[index % pattern.Length];
        }
    }
}

public class AdvancedRunnerPlayer : MonoBehaviour
{
    private AdvancedRunnerSettings settings;
    private SpriteRenderer spriteRenderer;
    private int lane = 1;
    private float jumpTimer;
    private float slideTimer;

    public int CurrentLane { get { return lane; } }

    public void Build(AdvancedRunnerSettings settings, Sprite sprite)
    {
        this.settings = settings;
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = settings.playerColor;
        spriteRenderer.sortingOrder = 6;
        ResetToLane(1);
    }

    public void ResetToLane(int newLane)
    {
        lane = Mathf.Clamp(newLane, 0, 2);
        transform.position = new Vector3(GetLaneX(), settings.judgementLineY, 0f);
        transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        jumpTimer = 0f;
        slideTimer = 0f;
    }

    public void Tick()
    {
        Vector3 target = new Vector3(GetLaneX(), settings.judgementLineY, 0f);
        if (jumpTimer > 0f)
        {
            jumpTimer -= Time.deltaTime;
            target.y += Mathf.Sin(Mathf.Clamp01(jumpTimer / 0.34f) * Mathf.PI) * 0.62f;
        }
        if (slideTimer > 0f)
        {
            slideTimer -= Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(0.72f, 0.34f, 1f), Time.deltaTime * 14f);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(0.55f, 0.55f, 1f), Time.deltaTime * 12f);
        }

        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 13f);
    }

    public void MoveLane(int delta)
    {
        lane = Mathf.Clamp(lane + delta, 0, 2);
    }

    public void PlayJump()
    {
        jumpTimer = 0.34f;
        slideTimer = 0f;
    }

    public void PlaySlide()
    {
        slideTimer = 0.28f;
        jumpTimer = 0f;
    }

    private float GetLaneX()
    {
        return (lane - 1) * settings.laneSpacing;
    }
}

public class AdvancedRunnerUI : MonoBehaviour
{
    private AdvancedRunnerManager manager;
    private AdvancedRunnerSettings settings;
    private Font font;
    private Canvas canvas;
    private Text titleText;
    private Text hintText;
    private Text feedbackText;
    private Text statsText;
    private Text beatText;
    private Image progressFill;
    private GameObject briefingOverlay;
    private GameObject resultOverlay;
    private Text resultTitle;
    private Text resultStats;
    private Sprite circleSprite;

    public void Build(AdvancedRunnerManager manager, AdvancedRunnerSettings settings, Sprite circleSprite)
    {
        this.manager = manager;
        this.settings = settings;
        this.circleSprite = circleSprite;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        EnsureEventSystem();
        GameObject existing = GameObject.Find("AdvancedRunnerCanvas");
        if (existing != null)
        {
            Destroy(existing);
        }

        GameObject canvasObject = new GameObject("AdvancedRunnerCanvas", typeof(RectTransform));
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        CreateHud(canvasObject.transform);
        CreateBriefingOverlay(canvasObject.transform);
        CreateResultOverlay(canvasObject.transform);
        briefingOverlay.SetActive(false);
        resultOverlay.SetActive(false);
    }

    public void ShowBriefing(string title, string body, string buttonLabel, UnityEngine.Events.UnityAction onStart)
    {
        briefingOverlay.SetActive(true);
        Transform card = briefingOverlay.transform.Find("Card");
        card.Find("Title").GetComponent<Text>().text = title;
        card.Find("Body").GetComponent<Text>().text = body;
        Button button = card.Find("StartButton").GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate
        {
            briefingOverlay.SetActive(false);
            onStart();
        });
        card.Find("StartButton/Text").GetComponent<Text>().text = buttonLabel;
    }

    public void ShowGameIntro()
    {
        titleText.text = "Advanced Runner";
        hintText.text = "Read lane + action. Hit on yellow.";
    }

    public void ShowTutorialStep(string title, string instruction, int current, int required, int index, int count)
    {
        titleText.text = "Lesson " + index + " / " + count + "  " + title;
        hintText.text = instruction + "  Progress " + current + " / " + required;
    }

    public void UpdateStats(int hearts, int score, int combo, int maxCombo, float progress01, float beatPosition)
    {
        statsText.text = "Hearts " + hearts + "   Score " + score + "   Combo " + combo + "   Best " + maxCombo;
        progressFill.fillAmount = Mathf.Clamp01(progress01);
        beatText.text = "Beat " + beatPosition.ToString("0.00");
    }

    public void ShowFeedback(string label, string detail, Color color)
    {
        feedbackText.text = string.IsNullOrEmpty(detail) ? label : label + "  " + detail;
        feedbackText.color = color;
        feedbackText.transform.localScale = Vector3.one * 1.16f;
    }

    public void ShowResult(bool completed, int score, int perfect, int good, int miss, int maxCombo)
    {
        resultOverlay.SetActive(true);
        resultTitle.text = completed ? "Advanced Complete" : "Advanced Failed";
        resultStats.text =
            "Score: " + score +
            "\nPerfect: " + perfect +
            "\nGood: " + good +
            "\nMiss: " + miss +
            "\nMax Combo: " + maxCombo;
    }

    private void Update()
    {
        if (feedbackText != null)
        {
            feedbackText.transform.localScale = Vector3.Lerp(feedbackText.transform.localScale, Vector3.one, Time.deltaTime * 8f);
        }
    }

    private void CreateHud(Transform parent)
    {
        GameObject top = CreatePanel(parent, "TopHud", new Color(0.02f, 0.025f, 0.04f, 0.82f));
        RectTransform topRect = top.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -54f);
        topRect.sizeDelta = new Vector2(1080f, 86f);

        titleText = CreateText(top.transform, "Title", "Advanced Runner", new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(740f, 38f), 30, FontStyle.Bold, Color.white);
        hintText = CreateText(top.transform, "Hint", "Four-key rhythm runner", new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(880f, 30f), 19, FontStyle.Bold, new Color(0.72f, 0.92f, 1f));

        statsText = CreateText(parent, "Stats", "Hearts 0 Score 0", new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(880f, 34f), 22, FontStyle.Bold, Color.white);
        feedbackText = CreateText(parent, "Feedback", "Ready", new Vector2(0.5f, 0f), new Vector2(0f, 145f), new Vector2(760f, 46f), 32, FontStyle.Bold, new Color(1f, 0.86f, 0.18f));
        beatText = CreateText(parent, "Beat", "Beat 0.00", new Vector2(0.92f, 0.5f), Vector2.zero, new Vector2(120f, 34f), 18, FontStyle.Bold, new Color(1f, 0.86f, 0.18f));

        GameObject progress = CreatePanel(parent, "Progress", new Color(1f, 1f, 1f, 0.16f));
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0f);
        progressRect.anchorMax = new Vector2(0.5f, 0f);
        progressRect.anchoredPosition = new Vector2(0f, 55f);
        progressRect.sizeDelta = new Vector2(680f, 18f);
        progressFill = CreateRect(progress.transform, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        progressFill.color = new Color(0.2f, 0.9f, 1f, 1f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
    }

    private void CreateBriefingOverlay(Transform parent)
    {
        briefingOverlay = CreateRect(parent, "Briefing", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        briefingOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);
        GameObject card = CreatePanel(briefingOverlay.transform, "Card", new Color(0.04f, 0.055f, 0.09f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(760f, 500f);
        CreateText(card.transform, "Title", "", new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(660f, 58f), 42, FontStyle.Bold, Color.white);
        CreateText(card.transform, "Body", "", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(650f, 250f), 25, FontStyle.Bold, new Color(0.8f, 0.95f, 1f));
        CreateButton(card.transform, "StartButton", "Start", new Vector2(0.5f, 0.13f), Vector2.zero, new Vector2(240f, 62f), null);
    }

    private void CreateResultOverlay(Transform parent)
    {
        resultOverlay = CreateRect(parent, "Result", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        resultOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);
        GameObject card = CreatePanel(resultOverlay.transform, "Card", new Color(0.04f, 0.055f, 0.09f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(650f, 470f);
        resultTitle = CreateText(card.transform, "Title", "Result", new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(540f, 58f), 38, FontStyle.Bold, Color.white);
        resultStats = CreateText(card.transform, "Stats", "", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(520f, 210f), 25, FontStyle.Bold, new Color(0.8f, 0.95f, 1f));
        CreateButton(card.transform, "Retry", "Retry", new Vector2(0.35f, 0.15f), Vector2.zero, new Vector2(160f, 54f), delegate { SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
        CreateButton(card.transform, "Back", "Back", new Vector2(0.65f, 0.15f), Vector2.zero, new Vector2(160f, 54f), delegate { SceneTransitionManager.LoadScene("Start"); });
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = CreatePanel(parent, name, new Color(1f, 1f, 1f, 0.95f));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }
        Text text = CreateText(obj.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 22, FontStyle.Bold, new Color(0.04f, 0.055f, 0.09f));
        text.raycastTarget = false;
        return button;
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        obj.AddComponent<Image>().color = color;
        return obj;
    }

    private Text CreateText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = CreateRect(parent, name, anchor, anchor, position, size).gameObject;
        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("EventSystem");
        obj.AddComponent<EventSystem>();
        obj.AddComponent<StandaloneInputModule>();
    }
}
