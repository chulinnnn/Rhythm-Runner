//Update() 每帧总入口：算 beat、移目标、查漏按、读键盘、判结束
//HandleInput() 按键后唯一判定入口：timing + 动作 + lane，命中或 Miss
//GetBeatPosition()唯一游戏时钟；下落、漏按、判定都靠它
//Tick() 每帧：平滑换道、跳起弧线、下滑缩扁
//MoveLane() 改 lane；Manager 用 CurrentLane 判 lane 对不对
//
//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Which flow the manager is currently running.
// 当前运行的是教程流程还是正式游戏流程。
public enum AdvancedRunnerMode
{
    Tutorial,
    Game
}

// The playable actions that can appear as falling targets.
// 下落目标可能要求玩家执行的动作类型。
public enum AdvancedActionType
{
    Jump,
    Slide,
    LaneLeft,
    LaneRight,
    Coin,
    Rest
}

// Feedback message keys. Actual display text/color can be edited through
// AdvancedRunnerConfig/Feedback in the scene hierarchy.
// 反馈消息的类型。真正显示的文案和颜色可以在场景里的
// AdvancedRunnerConfig/Feedback 中编辑。
public enum AdvancedFeedbackKey
{
    Wait,
    Perfect,
    Good,
    Miss,
    TutorialClear,
    WrongAction,
    WrongLane,
    TryAgain
}

// Music stages used by the scene: ambient/menu, tutorial, and formal game.
// 音乐阶段：场景待机/菜单、教程、正式游戏。
public enum AdvancedMusicStage
{
    Scene,
    Tutorial,
    Game
}

[System.Serializable]
// Inspector-editable feedback style.
// 可在 Inspector 中编辑的反馈样式。
//
// This is copied from AdvancedRunnerFeedbackConfig into AdvancedRunnerSettings.
// Runtime uses it to decide feedback text, color, font, and pulse size.
// 这个对象会从 AdvancedRunnerFeedbackConfig 同步到 AdvancedRunnerSettings。
// 运行时用它决定反馈文字、颜色、字体和缩放反馈。
public class AdvancedFeedbackStyle
{
    public Font font;
    public int fontSize = 32;
    public FontStyle fontStyle = FontStyle.Bold;
    public float pulseScale = 1.16f;
    public bool useOneColorForAllFeedback = false;
    public Color unifiedColor = new Color(1f, 0.86f, 0.18f, 1f);
    public Color waitColor = new Color(0.72f, 0.92f, 1f, 1f);
    public Color perfectColor = new Color(1f, 0.82f, 0.22f, 1f);
    public Color goodColor = new Color(0.28f, 0.72f, 1f, 1f);
    public Color missColor = new Color(1f, 0.25f, 0.28f, 1f);
    public Color tutorialClearColor = new Color(1f, 0.95f, 0.22f, 1f);
    public string waitText = "Wait";
    public string perfectText = "Perfect";
    public string goodText = "Good";
    public string missText = "Miss";
    public string tutorialClearText = "Tutorial Clear";
    public string wrongActionText = "Wrong";
    public string wrongLaneText = "Wrong Lane";
    public string tryAgainText = "Try Again";
    public string waitDetail = "Hit when ink drops reach the bottom line";
    public string perfectDetail = "{action}";
    public string goodDetail = "{action}";
    public string wrongActionDetail = "Next action: {action}";
    public string wrongLaneDetail = "Move to lane {lane}";
    public string tryAgainDetail = "Try again";
    public string missDetail = "Follow the music";
    public string tutorialClearDetail = "Entering Advanced Game";
    public bool includeDetail = true;

    public Color GetColor(AdvancedFeedbackKey key)
    {
        if (useOneColorForAllFeedback)
        {
            return unifiedColor;
        }

        switch (key)
        {
            case AdvancedFeedbackKey.Wait:
                return waitColor;
            case AdvancedFeedbackKey.Perfect:
                return perfectColor;
            case AdvancedFeedbackKey.Good:
                return goodColor;
            case AdvancedFeedbackKey.TutorialClear:
                return tutorialClearColor;
            case AdvancedFeedbackKey.Miss:
            case AdvancedFeedbackKey.WrongAction:
            case AdvancedFeedbackKey.WrongLane:
            case AdvancedFeedbackKey.TryAgain:
                return missColor;
            default:
                return unifiedColor;
        }
    }

    public string GetLabel(AdvancedFeedbackKey key)
    {
        switch (key)
        {
            case AdvancedFeedbackKey.Wait:
                return waitText;
            case AdvancedFeedbackKey.Perfect:
                return perfectText;
            case AdvancedFeedbackKey.Good:
                return goodText;
            case AdvancedFeedbackKey.TutorialClear:
                return tutorialClearText;
            case AdvancedFeedbackKey.WrongAction:
                return wrongActionText;
            case AdvancedFeedbackKey.WrongLane:
                return wrongLaneText;
            case AdvancedFeedbackKey.TryAgain:
                return tryAgainText;
            case AdvancedFeedbackKey.Miss:
            default:
                return missText;
        }
    }

    public string GetDefaultDetail(AdvancedFeedbackKey key)
    {
        switch (key)
        {
            case AdvancedFeedbackKey.Wait:
                return waitDetail;
            case AdvancedFeedbackKey.Perfect:
                return perfectDetail;
            case AdvancedFeedbackKey.Good:
                return goodDetail;
            case AdvancedFeedbackKey.TutorialClear:
                return tutorialClearDetail;
            case AdvancedFeedbackKey.WrongAction:
                return wrongActionDetail;
            case AdvancedFeedbackKey.WrongLane:
                return wrongLaneDetail;
            case AdvancedFeedbackKey.TryAgain:
                return tryAgainDetail;
            case AdvancedFeedbackKey.Miss:
                return missDetail;
            default:
                return "";
        }
    }
}

[System.Serializable]
// Runtime/gameplay settings for AdvancedRunner.
// AdvancedRunner 的运行时/玩法参数。
//
// Some values can be edited directly on the manager, but the current project
// prefers hierarchy config objects under AdvancedRunnerConfig/*.
// 一部分值可以直接在 Manager 上改，但当前项目更推荐通过
// AdvancedRunnerConfig/* 这些 Hierarchy 配置对象来编辑。
public class AdvancedRunnerSettings
{
    [Header("Music / Chart")]
    public float bpm = 126f;
    public float firstBeatOffset = 0f;
    public int startBeat = 4;
    public int countdownBeats = 0;
    public int beatsPerAction = 1;
    public AudioClip sceneBgm;
    public float sceneBpm = 126f;
    public AudioClip tutorialBgm;
    public float tutorialBpm = 126f;
    public AudioClip gameBgm;
    public float gameBpm = 126f;
    public float songDurationSeconds = 78f;

    [Header("Timing / Judgment")]
    [Tooltip("Seconds from target beat center for Perfect. Smaller = harder.")]
    public float perfectWindowSeconds = 0.08f;
    [Tooltip("Seconds from target beat center for Good. Must be >= Perfect window.")]
    public float goodWindowSeconds = 0.15f;

    [Header("World Layout")]
    public float judgementLineX = -4.5f;
    public float judgementLineY = -2.85f;
    public float targetBeatSpacingWorld = 2.15f;
    public float laneSpacing = 1.65f;

    [Header("Rules / Score")]
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
    public AdvancedFeedbackStyle feedback = new AdvancedFeedbackStyle();
}

// One scheduled falling target in the current chart.
// 当前谱面中的一个下落目标。
//
// beatIndex is the musical beat where this target should hit the judgment line.
// visual is the cloned template object currently falling in the world.
// beatIndex 表示目标应该到达判定线的音乐拍点；
// visual 是从模板克隆出来、正在下落的世界物体。
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
    public Vector3 baseScale;
}

[DefaultExecutionOrder(-1100)]
// Main controller for the AdvancedRunner scene. It keeps the scene-authored
// hierarchy editable, then binds and drives the parts that need runtime behavior.
// AdvancedRunner 场景主控制器。Hierarchy 里的对象和 UI 保持可编辑；
// 这个类在运行时绑定它们，并接管音乐、谱面、判定、分数和结果流程。
public partial class AdvancedRunnerManager
{
    private struct ControlPromptState
    {
        public bool show;
        public bool spaceDown;
        public bool downDown;
        public bool leftDown;
        public bool rightDown;
    }

    private const string SceneName = "AdvancedRunner";
    private const int ActionBeatCycle = 4;
    private const int MinActionBeatSpacing = 1;
    private static bool registered;
    private static AdvancedRunnerManager activeInstance;

    public AdvancedRunnerMode mode = AdvancedRunnerMode.Game;
    public AdvancedRunnerSettings settings = new AdvancedRunnerSettings();

    [Header("Runtime scene policy")]
    public RuntimeScenePolicy scenePolicy = RuntimeScenePolicy.Defaults();

    [Header("Visual Timing")]
    public float visualBeatDelaySeconds = 0.15f;

    private readonly List<AdvancedBeatTarget> targets = new List<AdvancedBeatTarget>();
    private AdvancedRunnerPlayer player;
    private AdvancedRunnerUI ui;
    private Sprite squareSprite;
    private Sprite circleSprite;
    private Sprite backdropSprite;
    private Transform worldRoot;
    private Transform laneAnchorsRoot;
    private Transform targetTemplatesRoot;
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
    private AudioSource advancedMusicSource;
    private AdvancedRunnerFeedbackConfig feedbackConfig;
    private AdvancedRunnerMusicConfig sceneMusicConfig;
    private AdvancedRunnerMusicConfig tutorialMusicConfig;
    private AdvancedRunnerMusicConfig gameMusicConfig;
    private float lastMusicPlaybackTime;
    private float inputLockedUntil;
    private AdvancedRunnerTutorialStep[] tutorialSteps;

    private float BeatInterval { get { return 60f / Mathf.Max(1f, settings.bpm); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Auto-entry point called by Unity after every scene load.
    // Unity 每次加载场景后自动调用的入口。
    //
    // This makes AdvancedRunner self-healing: if the scene has no manager
    // component attached, EnsureForScene() creates one at runtime.
    // 这让 AdvancedRunner 有自修复能力：如果场景里没有挂 manager，
    // EnsureForScene() 会在运行时创建一个。
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
        if (scene.name != SceneName)
        {
            return;
        }

        if (FindObjectOfType<AdvancedRunnerManager>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("AdvancedRunnerManager");
        AdvancedRunnerManager manager = obj.AddComponent<AdvancedRunnerManager>();
        manager.mode = AdvancedRunnerMode.Game;
    }

    // Scene bootstrap: bind config/UI/world objects by name, then disable the
    // manager if the required hierarchy contract is missing.
    // 场景启动入口：按名字绑定配置、UI、世界物体；如果必须的 Hierarchy
    // 合约缺失，就禁用 manager，避免进入半坏状态。
    //
    // Key calls / 关键调用:
    // - BindHierarchyConfig(): read AdvancedRunnerConfig/Feedback and Music.
    //   读取 AdvancedRunnerConfig/Feedback 和 Music。
    // - BuildRuntimeObjects(): bind AdvancedRunnerWorld, templates, player, UI.
    //   绑定 AdvancedRunnerWorld、目标模板、玩家和 UI。
    // - ConfigureRhythmManager(): prepare the shared RhythmManager visual/window provider.
    //   准备 RhythmManager，让它提供节奏窗口和可视化辅助。
    private void Awake()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != SceneName)
        {
            enabled = false;
            return;
        }

        if (activeInstance != null && activeInstance != this)
        {
            enabled = false;
            gameObject.SetActive(false);
            return;
        }

        activeInstance = this;
        mode = AdvancedRunnerMode.Game;
        BindHierarchyConfig();
        BuildTutorialSteps();
        squareSprite = CreateSolidSprite("AdvancedSquare", 8, Color.white);
        circleSprite = CreateCircleSprite("AdvancedCircle", 96, Color.white);
        backdropSprite = CreateSolidSprite("AdvancedBackdrop", 64, Color.white, 100f);
        DisableLegacyRunnerObjects();
        EnsureCamera();
        ConfigureRhythmManager();
        BuildRuntimeObjects();
        if (targetRoot == null || player == null || ui == null || !ui.IsReady)
        {
            Debug.LogWarning("AdvancedRunnerManager: Required runtime objects are missing. Disabling manager.");
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void Start()
    {
        if (!enabled || ui == null)
        {
            return;
        }

        ShowTutorialBriefing();
    }

    // One place resets score, combo, target state, music timing, overlays, and
    // the player lane before Tutorial/Game starts or retries.
    // 统一重置函数：教程、正式游戏、Retry 都会回到这里清空状态。
    //
    // What it resets / 重置内容:
    // score/combo/miss, target list, next target pointer, input lock, music time,
    // player lane, and generated target visuals.
    // 分数/combo/miss、目标列表、下一个目标指针、输入锁、音乐时间、
    // 玩家 lane，以及生成出来的下落目标。
    private void ResetRunState()
    {
        hearts = mode == AdvancedRunnerMode.Tutorial ? settings.tutorialHearts : settings.gameHearts;
        score = 0;
        combo = 0;
        maxCombo = 0;
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
        nextTargetIndex = 0;
        tutorialStepProgress = 0;
        runEnded = false;
        loadingGame = false;
        inputLockedUntil = 0f;
        lastMusicPlaybackTime = 0f;
        waitingForStart = true;
        StopRhythmClock();
        ResetRunObjects();
        if (player != null)
        {
            player.ResetToLane(1);
        }
        if (ui != null)
        {
            ui.UpdateControlRhythmPrompt(false, false, false, false, false);
        }

        UpdateUi();
    }

    // Current normal entry is Game mode shown through the existing TutorialOverlay.
    // 当前正常入口是正式 Game，但复用场景里的 TutorialOverlay 作为开始页。
    //
    // The name is historical: this used to show tutorial-first flow.
    // 函数名保留了历史原因：以前这里是教程优先流程。
    private void ShowTutorialBriefing()
    {
        mode = AdvancedRunnerMode.Game;
        tutorialStepIndex = 0;
        ResetRunState();
        ApplyMusicStage(AdvancedMusicStage.Game, false);
        ui.ShowTutorialEntryBriefing(BeginGame);
    }

    private void ShowGameBriefing()
    {
        mode = AdvancedRunnerMode.Game;
        ResetRunState();
        ApplyMusicStage(AdvancedMusicStage.Game, true);
        ui.ShowBriefing(
            "Advanced Runner",
            "Hit the correct action as it reaches the bottom yellow line.\nWrong key, wrong lane, or late timing costs a heart.\nBuild combo to push your score.",
            "Start Run",
            BeginGame);
    }

    public void RetryCurrentMode()
    {
        StopAllCoroutines();
        ShowTutorialBriefing();
    }

    // Per-frame game loop: advance the music-derived beat, move visuals, judge
    // late targets, accept input, and finish when the configured track ends.
    // 每帧主循环：用音乐播放时间算当前 beat，移动玩家和目标，处理漏按，
    // 接收键盘输入，并在音乐结束时结算。
    //
    // This is the best function to read when asking "what happens each frame?"
    // 如果想知道“每一帧发生什么”，优先看这个函数。
    private void Update()
    {
        if (runEnded || waitingForStart)
        {
            return;
        }

        float beatPosition = GetBeatPosition();
        float visualBeatPosition = GetVisualBeatPosition(beatPosition);
        if (ui != null)
        {
            ControlPromptState prompt = ResolveControlPromptState(visualBeatPosition);
            ui.UpdateControlRhythmPrompt(prompt.spaceDown, prompt.downDown, prompt.leftDown, prompt.rightDown, prompt.show);
        }

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

        if (mode == AdvancedRunnerMode.Game && IsGameMusicComplete())
        {
            EndRun(true);
        }

        UpdateUi();
    }

    private void BeginTutorial()
    {
        if (!StartRhythmClock(AdvancedMusicStage.Tutorial))
        {
            return;
        }

        tutorialStepIndex = 0;
        StartTutorialStep(0);
    }

    public void BeginTutorialFromUi()
    {
        BeginTutorial();
    }

    // Formal game entry. The game always rebuilds its chart and restarts the
    // Game music stage so Retry never inherits stale run state.
    // 正式游戏入口。每次开始或 Retry 都重新重置状态、重启 Game 音乐、
    // 重建谱面，避免继承上一局的 runEnded/target/music 状态。
    //
    // If you change how a run starts, this is the central function.
    // 如果要改“一局怎么开始”，这里是核心入口。
    private void BeginGame()
    {
        StopAllCoroutines();
        mode = AdvancedRunnerMode.Game;
        ResetRunState();
        ui.HideRunOverlays();

        if (!StartRhythmClock(AdvancedMusicStage.Game))
        {
            return;
        }

        BuildGameChart();
        if (!ValidateRunReady("Game"))
        {
            return;
        }

        waitingForStart = false;
        ui.ShowGameIntro();
    }

    public void BeginGameFromUi()
    {
        BeginGame();
    }

    private void StartTutorialStep(int index)
    {
        tutorialStepIndex = Mathf.Clamp(index, 0, tutorialSteps.Length - 1);
        tutorialStepProgress = 0;
        ResetRunObjects();
        BuildTutorialChart(tutorialSteps[tutorialStepIndex]);
        player.ResetToLane(1);
        ui.ShowTutorialStep(tutorialSteps[tutorialStepIndex].title, tutorialSteps[tutorialStepIndex].instruction, tutorialStepProgress, tutorialSteps[tutorialStepIndex].requiredHits, tutorialStepIndex + 1, tutorialSteps.Length);
        if (ValidateRunReady("Tutorial " + tutorialSteps[tutorialStepIndex].title))
        {
            waitingForStart = false;
        }
    }

    // Runtime binding pass for the scene's editable world. Existing objects are
    // preferred; missing objects are only created when RuntimeScenePolicy allows.
    // 运行时世界绑定：优先使用场景里已经存在、可手动编辑的物体；
    // 只有 RuntimeScenePolicy 允许时才创建缺失对象。
    //
    // Bound hierarchy / 绑定的主要 Hierarchy:
    // - AdvancedRunnerWorld
    // - LaneAnchors/Lane_0..2
    // - AdvancedTargetTemplates/*
    // - AdvancedRunnerPlayer
    // - AdvancedRunnerCanvas through AdvancedRunnerUI
    private void BuildRuntimeObjects()
    {
        worldRoot = FindOrCreateWorldRoot();
        if (worldRoot == null)
        {
            return;
        }

        EnsureLaneAnchors();
        EnsureWorldGuide();
        EnsureTargetTemplates();

        Transform existingTargets = worldRoot.Find("AdvancedTargets");
        if (existingTargets != null)
        {
            targetRoot = existingTargets;
        }
        else
        {
            GameObject targetsObject = new GameObject("AdvancedTargets");
            targetsObject.transform.SetParent(worldRoot, false);
            targetRoot = targetsObject.transform;
        }

        player = scenePolicy.useExistingSceneObjects ? FindAdvancedPlayer() : null;
        if (player == null)
        {
            if (!scenePolicy.autoCreateMissingObjects)
            {
                Debug.LogWarning("AdvancedRunnerManager: AdvancedRunnerPlayer is missing and auto creation is disabled.");
                return;
            }

            GameObject playerObject = new GameObject("AdvancedRunnerPlayer");
            playerObject.transform.SetParent(worldRoot, false);
            player = playerObject.AddComponent<AdvancedRunnerPlayer>();
        }
        player.Build(this, settings, circleSprite);
        EnsureBackdrop(player.transform, backdropSprite, new Vector3(8f, 8f, 1f), 5);

        ui = GetComponent<AdvancedRunnerUI>();
        if (ui == null)
        {
            ui = gameObject.AddComponent<AdvancedRunnerUI>();
        }
        ui.Build(this, settings, circleSprite, scenePolicy);
    }

    // Designer-facing config lives in AdvancedRunnerConfig/* in the Hierarchy.
    // This sync copies those components into runtime settings before playback.
    // 面向设计调整的配置放在 Hierarchy 的 AdvancedRunnerConfig/* 下。
    // 每次播放音乐前都会同步一次，让 Inspector 里的音乐/BPM/反馈配置生效。
    private void BindHierarchyConfig()
    {
        ResolveHierarchyConfigComponents();
        ApplyHierarchyConfigToSettings();
    }

    private void ResolveHierarchyConfigComponents()
    {
        Transform configRoot = FindOrCreateConfigRoot();
        if (configRoot == null)
        {
            return;
        }

        feedbackConfig = FindOrCreateConfigComponent<AdvancedRunnerFeedbackConfig>(configRoot, "Feedback");
        Transform musicRoot = FindOrCreateConfigChild(configRoot, "Music");
        sceneMusicConfig = FindOrCreateConfigComponent<AdvancedRunnerMusicConfig>(musicRoot, "Scene");
        tutorialMusicConfig = FindOrCreateConfigComponent<AdvancedRunnerMusicConfig>(musicRoot, "Tutorial");
        gameMusicConfig = FindOrCreateConfigComponent<AdvancedRunnerMusicConfig>(musicRoot, "Game");
    }

    private Transform FindOrCreateConfigRoot()
    {
        GameObject existing = GameObject.Find("AdvancedRunnerConfig");
        if (existing != null)
        {
            return existing.transform;
        }

        if (!scenePolicy.autoCreateMissingObjects)
        {
            return null;
        }

        return new GameObject("AdvancedRunnerConfig").transform;
    }

    private Transform FindOrCreateConfigChild(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing;
        }

        if (!scenePolicy.autoCreateMissingObjects)
        {
            return null;
        }

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }

    private T FindOrCreateConfigComponent<T>(Transform parent, string name) where T : Component
    {
        Transform child = FindOrCreateConfigChild(parent, name);
        if (child == null)
        {
            return null;
        }

        T component = child.GetComponent<T>();
        if (component == null && scenePolicy.autoCreateMissingObjects)
        {
            component = child.gameObject.AddComponent<T>();
        }

        return component;
    }

    private void ApplyHierarchyConfigToSettings()
    {
        ResolveHierarchyConfigComponents();
        if (feedbackConfig != null && feedbackConfig.feedback != null)
        {
            settings.feedback = feedbackConfig.feedback;
        }

        ApplyMusicConfigToSettings(sceneMusicConfig, ref settings.sceneBgm, ref settings.sceneBpm);
        ApplyMusicConfigToSettings(tutorialMusicConfig, ref settings.tutorialBgm, ref settings.tutorialBpm);
        ApplyMusicConfigToSettings(gameMusicConfig, ref settings.gameBgm, ref settings.gameBpm);
    }

    private void ApplyMusicConfigToSettings(AdvancedRunnerMusicConfig config, ref AudioClip clip, ref float bpm)
    {
        if (config == null)
        {
            return;
        }

        if (config.bgm != null)
        {
            clip = config.bgm;
        }
        bpm = config.bpm > 0f ? config.bpm : bpm;
    }

    private Transform FindOrCreateWorldRoot()
    {
        GameObject existing = GameObject.Find("AdvancedRunnerWorld");
        if (existing != null)
        {
            return existing.transform;
        }

        if (!scenePolicy.autoCreateMissingObjects)
        {
            Debug.LogWarning("AdvancedRunnerManager: AdvancedRunnerWorld is missing and auto creation is disabled.");
            return null;
        }

        GameObject worldObject = new GameObject("AdvancedRunnerWorld");
        return worldObject.transform;
    }

    private AdvancedRunnerPlayer FindAdvancedPlayer()
    {
        Transform playerTransform = worldRoot != null ? worldRoot.Find("AdvancedRunnerPlayer") : null;
        if (playerTransform != null)
        {
            AdvancedRunnerPlayer worldPlayer = playerTransform.GetComponent<AdvancedRunnerPlayer>();
            if (worldPlayer != null)
            {
                return worldPlayer;
            }

            if (scenePolicy.autoCreateMissingObjects)
            {
                return playerTransform.gameObject.AddComponent<AdvancedRunnerPlayer>();
            }
        }

        return FindObjectOfType<AdvancedRunnerPlayer>();
    }

    private void EnsureLaneAnchors()
    {
        Transform existing = worldRoot.Find("LaneAnchors");
        if (existing == null)
        {
            if (!scenePolicy.autoCreateMissingObjects)
            {
                return;
            }

            GameObject anchorsObject = new GameObject("LaneAnchors");
            anchorsObject.transform.SetParent(worldRoot, false);
            existing = anchorsObject.transform;
        }

        laneAnchorsRoot = existing;
        for (int lane = 0; lane < 3; lane++)
        {
            string name = "Lane_" + lane;
            Transform anchor = laneAnchorsRoot.Find(name);
            if (anchor == null && scenePolicy.autoCreateMissingObjects)
            {
                GameObject anchorObject = new GameObject(name);
                anchorObject.transform.SetParent(laneAnchorsRoot, false);
                anchor = anchorObject.transform;
                anchor.position = new Vector3((lane - 1) * settings.laneSpacing, settings.judgementLineY, 0f);
            }
        }
    }

    private void EnsureWorldGuide()
    {
        Transform guideRoot = worldRoot.Find("AdvancedWorldGuide");
        if (guideRoot == null)
        {
            if (!scenePolicy.autoCreateMissingObjects)
            {
                return;
            }

            GameObject guideObject = new GameObject("AdvancedWorldGuide");
            guideObject.transform.SetParent(worldRoot, false);
            guideRoot = guideObject.transform;
        }

        for (int lane = 0; lane < 3; lane++)
        {
            string name = "AdvancedLane_" + lane;
            Transform line = guideRoot.Find(name);
            if (line == null && scenePolicy.autoCreateMissingObjects)
            {
                GameObject lineObject = new GameObject(name);
                lineObject.transform.SetParent(guideRoot, false);
                line = lineObject.transform;
                line.position = new Vector3(GetLaneX(lane), 0.55f, 0f);
                line.localScale = new Vector3(0.05f, 7.6f, 1f);
            }

            if (line != null)
            {
                EnsureSpriteRenderer(line.gameObject, squareSprite, Color.white, -2, false);
            }
        }

        Transform judgement = guideRoot.Find("AdvancedJudgementLine");
        if (judgement == null && scenePolicy.autoCreateMissingObjects)
        {
            GameObject judgementObject = new GameObject("AdvancedJudgementLine");
            judgementObject.transform.SetParent(guideRoot, false);
            judgement = judgementObject.transform;
            judgement.position = new Vector3(0f, settings.judgementLineY, 0f);
            judgement.localScale = new Vector3(6.4f, 0.08f, 1f);
        }

        if (judgement != null)
        {
            EnsureSpriteRenderer(judgement.gameObject, squareSprite, new Color(1f, 0.86f, 0.18f, 0.78f), -1, false);
        }
    }

    private void EnsureTargetTemplates()
    {
        Transform existing = worldRoot.Find("AdvancedTargetTemplates");
        if (existing == null)
        {
            if (!scenePolicy.autoCreateMissingObjects)
            {
                return;
            }

            GameObject templatesObject = new GameObject("AdvancedTargetTemplates");
            templatesObject.transform.SetParent(worldRoot, false);
            existing = templatesObject.transform;
        }

        targetTemplatesRoot = existing;
        EnsureTargetTemplate(AdvancedActionType.Jump, "Jump");
        EnsureTargetTemplate(AdvancedActionType.Slide, "Slide");
        EnsureTargetTemplate(AdvancedActionType.LaneLeft, "LaneLeft");
        EnsureTargetTemplate(AdvancedActionType.LaneRight, "LaneRight");
        EnsureTargetTemplate(AdvancedActionType.Coin, "Coin");
    }

    private void EnsureTargetTemplate(AdvancedActionType action, string name)
    {
        Transform template = targetTemplatesRoot.Find(name);
        if (template == null)
        {
            if (!scenePolicy.autoCreateMissingObjects)
            {
                return;
            }

            GameObject templateObject = new GameObject(name);
            templateObject.transform.SetParent(targetTemplatesRoot, false);
            template = templateObject.transform;
            template.localScale = GetTargetBaseScale(action);
        }

        EnsureSpriteRenderer(template.gameObject, action == AdvancedActionType.Coin ? circleSprite : squareSprite, Color.white, 3, false);
        EnsureBackdrop(template, backdropSprite, GetBackdropScale(action), 2);
        EnsureTemplateLabel(template, GetActionLabel(action));
        template.gameObject.SetActive(false);
    }

    private void EnsureBackdrop(Transform parent, Sprite sprite, Vector3 localScale, int sortingOrder)
    {
        Transform existing = parent.Find("Backdrop");
        bool created = existing == null;
        if (created)
        {
            GameObject obj = new GameObject("Backdrop");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = localScale;
            existing = obj.transform;
        }

        SpriteRenderer renderer = existing.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = existing.gameObject.AddComponent<SpriteRenderer>();
            renderer.color = new Color(1f, 1f, 1f, 0.86f);
            renderer.sortingOrder = sortingOrder;
        }

        if (renderer.sprite == null)
        {
            renderer.sprite = sprite;
        }
        if (renderer.sortingOrder == 0)
        {
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void EnsureTemplateLabel(Transform template, string label)
    {
        Transform labelTransform = template.Find("Label");
        if (labelTransform == null)
        {
            if (!scenePolicy.autoCreateMissingObjects)
            {
                return;
            }

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(template, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            labelObject.transform.localScale = Vector3.one;
            labelTransform = labelObject.transform;
        }

        TextMesh text = labelTransform.GetComponent<TextMesh>();
        if (text == null)
        {
            text = labelTransform.gameObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.characterSize = 0.055f;
            text.color = Color.white;
        }
        else if (string.IsNullOrEmpty(text.text) || text.text == "UP" || text.text == "MIX")
        {
            text.text = label;
        }

        MeshRenderer textRenderer = labelTransform.GetComponent<MeshRenderer>();
        if (textRenderer != null && textRenderer.sortingOrder == 0)
        {
            textRenderer.sortingOrder = 5;
        }
    }

    private SpriteRenderer EnsureSpriteRenderer(GameObject obj, Sprite sprite, Color color, int sortingOrder, bool overrideExistingVisuals)
    {
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        if (overrideExistingVisuals)
        {
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
        else
        {
            if (renderer.sprite == null)
            {
                renderer.sprite = sprite;
            }
            if (renderer.sortingOrder == 0)
            {
                renderer.sortingOrder = sortingOrder;
            }
        }

        return renderer;
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

    // Tutorial targets are generated from the current lesson, then snapped onto
    // allowed beat slots so visual cues and judgment timing stay aligned.
    // 教程谱面生成：根据当前 lesson 的 pattern 生成目标，并通过
    // GetNextAllowedActionBeat 对齐到允许的拍点，保证 HUD beat、下落位置、
    // 判定窗口三者一致。
    private void BuildTutorialChart(AdvancedRunnerTutorialStep step)
    {
        int startBeat = Mathf.Max(0, settings.startBeat);
        int beat = GetNextAllowedActionBeat(Mathf.Max(startBeat, Mathf.CeilToInt(GetBeatPosition()) + startBeat));
        Debug.Log("AdvancedRunnerManager: Building Tutorial chart bpm=" + settings.bpm + " beatInterval=" + BeatInterval.ToString("0.000") + " startBeat=" + beat);
        int lane = 1;
        int beatSpacing = Mathf.Max(MinActionBeatSpacing, settings.beatsPerAction);
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

            AddTarget(beat, lane, action, true, GetActionLabel(action));
            beat = GetNextAllowedActionBeat(beat + beatSpacing);
        }
    }

    // Formal game chart: repeat a compact action pattern across the configured
    // song length, correcting lane actions at the lane boundaries.
    // 正式游戏谱面生成：按 songDurationSeconds 和当前 BPM 算总拍数，
    // 然后循环一个 8 拍动作模式。遇到 lane 边界时会自动改成合法方向。
    //
    // To change gameplay difficulty/action distribution, edit this function and
    // GetGamePatternAction() together.
    // 如果要改难度、动作分布或出目标频率，通常同时改这里和
    // GetGamePatternAction()。
    private void BuildGameChart()
    {
        ResetRunObjects();
        player.ResetToLane(1);

        int totalBeats = Mathf.CeilToInt(settings.songDurationSeconds / BeatInterval);
        Debug.Log("AdvancedRunnerManager: Building Game chart bpm=" + settings.bpm + " beatInterval=" + BeatInterval.ToString("0.000") + " totalBeats=" + totalBeats);
        int lane = 1;
        int beatSpacing = Mathf.Max(MinActionBeatSpacing, settings.beatsPerAction);
        for (int beat = GetNextAllowedActionBeat(Mathf.Max(0, settings.startBeat)); beat < totalBeats; beat = GetNextAllowedActionBeat(beat + beatSpacing))
        {
            int pattern = (beat - Mathf.Max(0, settings.startBeat)) % 8;
            AdvancedActionType action = GetGamePatternAction(pattern, lane);

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

    private AdvancedActionType GetGamePatternAction(int pattern, int currentLane)
    {
        switch (pattern)
        {
            case 1:
            case 5:
                return AdvancedActionType.Slide;
            case 2:
                return currentLane > 0 ? AdvancedActionType.LaneLeft : AdvancedActionType.LaneRight;
            case 3:
            case 7:
                return AdvancedActionType.Coin;
            case 6:
                return currentLane < 2 ? AdvancedActionType.LaneRight : AdvancedActionType.LaneLeft;
            default:
                return AdvancedActionType.Jump;
        }
    }

    private void AddTarget(int beatIndex, int laneIndex, AdvancedActionType actionType, bool required, string label)
    {
        ValidateActionBeat(beatIndex, actionType);
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

    // Beat ownership gate for future chart edits. Keep target beats going
    // through this helper so HUD dots, falling targets, and judgment agree.
    // 谱面拍点守门函数。以后如果要限制“哪些拍点可以出目标”，
    // 应该改 IsAllowedActionBeat()，并让所有 AddTarget 前都经过这里。
    private int GetNextAllowedActionBeat(int candidateBeat)
    {
        int beat = Mathf.Max(0, candidateBeat);
        while (!IsAllowedActionBeat(beat))
        {
            beat++;
        }

        return beat;
    }

    private bool IsAllowedActionBeat(int beatIndex)
    {
        return beatIndex >= 0;
    }

    private int GetFourBeatSlot(int beatIndex)
    {
        return ((beatIndex % ActionBeatCycle) + ActionBeatCycle) % ActionBeatCycle;
    }

    private void ValidateActionBeat(int beatIndex, AdvancedActionType actionType)
    {
        if (IsAllowedActionBeat(beatIndex))
        {
            return;
        }

        int slot = GetFourBeatSlot(beatIndex);
        Debug.LogWarning("AdvancedRunnerManager: Target scheduled on an invalid beat action="
            + actionType
            + " beatIndex="
            + beatIndex
            + " fourBeatSlot="
            + (slot + 1));
    }

    // Visual targets are clones of editable Hierarchy templates when available;
    // the fallback is only for missing-template recovery.
    // 目标视觉创建：优先克隆 Hierarchy 里的 AdvancedTargetTemplates。
    // 这样美术可以直接改模板 sprite/color/scale/backdrop。
    // fallback 只用于模板缺失时避免游戏直接崩掉。
    private GameObject CreateTargetVisual(AdvancedBeatTarget target)
    {
        Transform template = FindTargetTemplate(target.actionType);
        GameObject obj;
        if (template != null)
        {
            obj = Instantiate(template.gameObject, targetRoot);
            obj.name = "AdvancedTarget_" + target.actionType + "_Beat_" + target.beatIndex;
            obj.SetActive(true);
        }
        else
        {
            obj = new GameObject("AdvancedTarget_" + target.actionType + "_Beat_" + target.beatIndex);
            obj.transform.SetParent(targetRoot, false);
            SpriteRenderer fallbackRenderer = obj.AddComponent<SpriteRenderer>();
            fallbackRenderer.sprite = target.actionType == AdvancedActionType.Coin ? circleSprite : squareSprite;
            fallbackRenderer.color = Color.white;
            fallbackRenderer.sortingOrder = 3;
            obj.transform.localScale = GetTargetBaseScale(target.actionType);
            EnsureBackdrop(obj.transform, backdropSprite, GetBackdropScale(target.actionType), 2);
            EnsureTemplateLabel(obj.transform, GetActionLabel(target.actionType));
        }

        target.baseScale = obj.transform.localScale;
        SetTargetLabel(obj.transform, string.IsNullOrEmpty(target.label) ? GetActionLabel(target.actionType) : target.label);
        return obj;
    }

    private Transform FindTargetTemplate(AdvancedActionType action)
    {
        if (targetTemplatesRoot == null)
        {
            return null;
        }

        Transform template = targetTemplatesRoot.Find(GetTargetTemplateName(action));
        if (template != null)
        {
            return template;
        }

        return null;
    }

    private string GetTargetTemplateName(AdvancedActionType action)
    {
        switch (action)
        {
            case AdvancedActionType.Jump:
                return "Jump";
            case AdvancedActionType.Slide:
                return "Slide";
            case AdvancedActionType.LaneLeft:
                return "LaneLeft";
            case AdvancedActionType.LaneRight:
                return "LaneRight";
            case AdvancedActionType.Coin:
                return "Coin";
            default:
                return "Jump";
        }
    }

    private void SetTargetLabel(Transform targetTransform, string label)
    {
        Transform labelTransform = targetTransform.Find("Label");
        if (labelTransform == null)
        {
            return;
        }

        TextMesh text = labelTransform.GetComponent<TextMesh>();
        if (text != null)
        {
            text.text = label;
        }
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
            Vector3 baseScale = target.baseScale == Vector3.zero ? target.visual.transform.localScale : target.baseScale;
            target.visual.transform.localScale = baseScale * Mathf.Lerp(1f, 1.18f, pulse);
        }
    }

    // Late misses use the same good timing window as input judgment, converted
    // from RhythmManager seconds into current BPM beats.
    // 漏按检测：如果当前 beat 已经超过目标 beat 太多，就自动 RegisterMiss。
    // 这里使用和输入判定相同的 Good window，避免“能按中但已经被判漏”的冲突。
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
        if (beatPosition - target.beatIndex > GetGoodWindowBeats())
        {
            RegisterMiss(target, AdvancedFeedbackKey.Miss, "", GetActionLabel(target.actionType), target.laneIndex + 1);
        }
    }

    // Input has two phases: immediate player animation/lane movement first,
    // then rhythm/action/lane judgment against the current hittable target.
    // 输入判定核心，分两步：
    // 1. 先立即移动玩家 lane 或播放 jump/slide 表现，保证操作有响应。
    // 2. 再找当前判定窗口内的目标，检查 timing/action/lane 是否都正确。
    //
    // This is the core function for "why did my key count as hit or miss?"
    // 如果要查“为什么这次按键算命中/失败”，重点看这个函数。
    private void HandleInput(AdvancedActionType inputAction)
    {
        bool isLaneInput = inputAction == AdvancedActionType.LaneLeft || inputAction == AdvancedActionType.LaneRight;
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

        AdvancedBeatTarget target = GetCurrentTarget();
        if (target == null)
        {
            ui.ShowFeedback(AdvancedFeedbackKey.Wait);
            return;
        }

        RhythmTimingResult result = JudgeAdvancedTiming(target);
        bool timingSuccess = result == RhythmTimingResult.Perfect || result == RhythmTimingResult.Good;
        bool actionSuccess = inputAction == target.actionType || (target.actionType == AdvancedActionType.Coin && (inputAction == AdvancedActionType.Jump || inputAction == AdvancedActionType.Slide));

        bool laneSuccess = player.CurrentLane == target.laneIndex;
        if (isLaneInput)
        {
            laneSuccess = actionSuccess && laneSuccess;
        }
        if (!timingSuccess || !actionSuccess || !laneSuccess)
        {
            RegisterMiss(target, GetMissFeedbackKey(timingSuccess, actionSuccess, laneSuccess), "", GetActionLabel(target.actionType), target.laneIndex + 1);
            return;
        }

        RegisterHit(target, result);
    }

    // AdvancedRunner judges directly against its own music beat clock instead
    // of calling RhythmManager.ReportInput, so target motion and input match.
    // 节奏判定核心：直接比较当前音乐 beat 和目标 beat 的距离。
    // 不调用 RhythmManager.ReportInput，是为了让目标下落、漏按和按键判定
    // 全部使用同一个 AdvancedRunnerAudio 时钟。
    private RhythmTimingResult JudgeAdvancedTiming(AdvancedBeatTarget target)
    {
        if (target == null)
        {
            return RhythmTimingResult.None;
        }

        float beatDistance = Mathf.Abs(GetBeatPosition() - target.beatIndex);
        float perfectWindowBeats = GetPerfectWindowBeats();
        float goodWindowBeats = GetGoodWindowBeats();

        if (beatDistance <= perfectWindowBeats)
        {
            return RhythmTimingResult.Perfect;
        }
        if (beatDistance <= goodWindowBeats)
        {
            return RhythmTimingResult.Good;
        }

        return RhythmTimingResult.Miss;
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
        return distance <= GetGoodWindowBeats() ? target : null;
    }

    // Successful hits update scoring and tutorial progress, then briefly lock
    // input so one key press cannot double-judge nearby targets.
    // 命中处理：隐藏目标、加分、增加 combo、更新教程进度，并短暂锁输入，
    // 防止一次按键连续判定多个相邻目标。
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
        ui.ShowFeedback(result == RhythmTimingResult.Perfect ? AdvancedFeedbackKey.Perfect : AdvancedFeedbackKey.Good, "", target.laneIndex + 1, GetActionLabel(target.actionType));

        if (mode == AdvancedRunnerMode.Tutorial)
        {
            tutorialStepProgress++;
            AdvancedRunnerTutorialStep step = tutorialSteps[tutorialStepIndex];
            ui.ShowTutorialStep(step.title, step.instruction, tutorialStepProgress, step.requiredHits, tutorialStepIndex + 1, tutorialSteps.Length);
            if (tutorialStepProgress >= step.requiredHits)
            {
                CompleteTutorialStep();
            }
        }
    }

    // Current game rule is score-only game over: any formal Game miss ends the
    // run, while Tutorial misses restart the active lesson step.
    // 失败处理：教程中失败会重启当前 lesson；正式 Game 中任何 miss
    // 都结束本局，并显示只包含 Score 的 Game Over 结果。
    private void RegisterMiss(AdvancedBeatTarget target, AdvancedFeedbackKey key, string hint, string actionLabel, int laneNumber)
    {
        target.judged = true;
        HideTarget(target);
        missCount++;
        combo = 0;
        ui.ShowFeedback(key, hint, laneNumber, actionLabel);

        if (mode == AdvancedRunnerMode.Tutorial)
        {
            ui.UpdateControlRhythmPrompt(false, false, false, false, false);
            StartCoroutine(RestartTutorialStepRoutine());
            return;
        }

        EndRun(false);
    }

    private IEnumerator RestartTutorialStepRoutine()
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
        ui.ShowFeedback(AdvancedFeedbackKey.TutorialClear);
        yield return new WaitForSecondsRealtime(0.9f);
        ShowGameBriefing();
    }

    // Finalizes the current run and lets UI decide how to present the result.
    // 结束当前一局，并交给 UI 决定结果如何显示。
    //
    // completed=true means music finished successfully; completed=false means
    // current rules ended the run from a miss.
    // completed=true 表示音乐正常播完；completed=false 表示当前规则因 miss 结束本局。
    private void EndRun(bool completed)
    {
        if (runEnded)
        {
            return;
        }

        runEnded = true;
        StopRhythmClock();
        ui.UpdateControlRhythmPrompt(false, false, false, false, false);
        ui.ShowResult(completed, score, perfectCount, goodCount, missCount, maxCombo);
        if (score > 0)
        {
            LeaderboardManager.SaveScore(LeaderboardMode.Hard, score);
        }
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
            ? Mathf.Clamp01(GetMusicPlaybackTime() / Mathf.Max(1f, settings.songDurationSeconds))
            : tutorialSteps == null || tutorialSteps.Length == 0 ? 0f : Mathf.Clamp01((tutorialStepIndex + tutorialStepProgress / Mathf.Max(1f, tutorialSteps[tutorialStepIndex].requiredHits)) / tutorialSteps.Length);
        float beatPosition = GetBeatPosition();
        ui.UpdateStats(hearts, score, combo, maxCombo, progress, GetVisualBeatPosition(beatPosition));
    }

    // Single source of gameplay timing: configured music playback seconds
    // divided by the current stage BPM beat interval.
    // 游戏 beat 的唯一来源：当前配置音乐播放秒数 / 当前阶段 BPM 对应的每拍秒数。
    //
    // Target falling, input windows, late misses, progress, and beat HUD all
    // depend on this value.
    // 下落目标、输入窗口、漏按、进度条、BottomHud Beat 都依赖这个值。
    private float GetBeatPosition()
    {
        return GetMusicPlaybackTime() / BeatInterval;
    }

    private float GetVisualBeatPosition(float beatPosition)
    {
        return Mathf.Max(0f, beatPosition - Mathf.Max(0f, visualBeatDelaySeconds) / BeatInterval);
    }

    private ControlPromptState ResolveControlPromptState(float beatPosition)
    {
        ControlPromptState state = new ControlPromptState();
        if (runEnded || waitingForStart)
        {
            return state;
        }

        if (!StartMenuAudioSettings.BeatPromptsEnabled)
        {
            return state;
        }

        state.show = true;
        AdvancedBeatTarget target = GetVisualPromptTarget(beatPosition);
        if (target == null)
        {
            return state;
        }

        switch (target.actionType)
        {
            case AdvancedActionType.Jump:
            case AdvancedActionType.Coin:
                state.spaceDown = true;
                break;
            case AdvancedActionType.Slide:
                state.downDown = true;
                break;
            case AdvancedActionType.LaneLeft:
                state.leftDown = true;
                break;
            case AdvancedActionType.LaneRight:
                state.rightDown = true;
                break;
        }

        return state;
    }

    private AdvancedBeatTarget GetVisualPromptTarget(float beatPosition)
    {
        int visualBeat = Mathf.FloorToInt(Mathf.Max(0f, beatPosition));
        for (int i = 0; i < targets.Count; i++)
        {
            AdvancedBeatTarget target = targets[i];
            if (target == null)
            {
                continue;
            }

            if (target.beatIndex == visualBeat)
            {
                return target;
            }

            if (target.beatIndex > visualBeat)
            {
                return null;
            }
        }

        return null;
    }

    // Cache the furthest observed AudioSource.time so the beat clock remains
    // stable across frame reads and source stop checks.
    // 读取音乐播放时间，并缓存已看到的最大播放时间。
    // 这样在 AudioSource 短暂停止或同一帧多次读取时，beat 不会倒退。
    private float GetMusicPlaybackTime()
    {
        RhythmManager rhythm = RhythmManager.Instance != null ? RhythmManager.Instance : FindObjectOfType<RhythmManager>();
        AudioSource source = advancedMusicSource != null ? advancedMusicSource : rhythm != null ? rhythm.musicSource : FindAdvancedAudioSource();
        if (source == null || source.clip == null)
        {
            return 0f;
        }

        if (source.isPlaying)
        {
            lastMusicPlaybackTime = Mathf.Max(lastMusicPlaybackTime, source.time - settings.firstBeatOffset);
        }

        return Mathf.Max(0f, lastMusicPlaybackTime);
    }

    private bool IsGameMusicComplete()
    {
        RhythmManager rhythm = RhythmManager.Instance != null ? RhythmManager.Instance : FindObjectOfType<RhythmManager>();
        AudioSource source = advancedMusicSource != null ? advancedMusicSource : rhythm != null ? rhythm.musicSource : FindAdvancedAudioSource();
        if (source == null || source.clip == null)
        {
            return false;
        }

        float playbackTime = Mathf.Max(0f, GetMusicPlaybackTime());
        if (playbackTime >= settings.songDurationSeconds)
        {
            return true;
        }

        return playbackTime > 0f && !source.isPlaying && playbackTime >= source.clip.length - 0.05f;
    }

    private float GetPerfectWindowBeats()
    {
        float perfectWindowSeconds = Mathf.Max(0.01f, settings.perfectWindowSeconds);
        return Mathf.Max(0.01f, perfectWindowSeconds / BeatInterval);
    }

    private float GetGoodWindowBeats()
    {
        float goodWindowSeconds = Mathf.Max(settings.perfectWindowSeconds, settings.goodWindowSeconds);
        return Mathf.Max(GetPerfectWindowBeats(), goodWindowSeconds / BeatInterval);
    }

    public float GetLaneX(int lane)
    {
        Transform anchor = GetLaneAnchor(lane);
        if (anchor != null)
        {
            return anchor.position.x;
        }

        return (Mathf.Clamp(lane, 0, 2) - 1) * settings.laneSpacing;
    }

    private Transform GetLaneAnchor(int lane)
    {
        if (laneAnchorsRoot == null)
        {
            return null;
        }

        return laneAnchorsRoot.Find("Lane_" + Mathf.Clamp(lane, 0, 2));
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

    private Vector3 GetBackdropScale(AdvancedActionType action)
    {
        if (action == AdvancedActionType.Coin)
        {
            return new Vector3(8f, 12f, 1f);
        }

        if (action == AdvancedActionType.LaneLeft || action == AdvancedActionType.LaneRight)
        {
            return new Vector3(18f, 5f, 1f);
        }

        return new Vector3(16f, 16f, 1f);
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

    private AdvancedFeedbackKey GetMissFeedbackKey(bool timingSuccess, bool actionSuccess, bool laneSuccess)
    {
        if (!actionSuccess)
        {
            return AdvancedFeedbackKey.WrongAction;
        }
        if (!laneSuccess)
        {
            return AdvancedFeedbackKey.WrongLane;
        }
        if (!timingSuccess)
        {
            return AdvancedFeedbackKey.Miss;
        }

        return AdvancedFeedbackKey.TryAgain;
    }

    // RhythmManager is kept as a shared visual/window provider, but Advanced
    // gameplay still reads timing from the dedicated AdvancedRunnerAudio source.
    // RhythmManager 只作为共享节奏窗口/可视化辅助存在；
    // AdvancedRunner 真正的 gameplay 时钟仍然来自 AdvancedRunnerAudio。
    //
    // This avoids old scene music sources accidentally driving Advanced timing.
    // 这样可以避免旧场景里的其它 AudioSource 意外影响 Advanced 的判定。
    private void ConfigureRhythmManager()
    {
        AudioSource source = FindOrCreateAdvancedAudioSource();
        advancedMusicSource = source;
        EnsureAdvancedMusicSource(source);
        RhythmManager rhythm = GetOrCreateRhythmManager();
        if (rhythm == null)
        {
            return;
        }

        rhythm.musicSource = source;
        rhythm.bpm = settings.bpm;
        rhythm.visualizationBpm = settings.bpm;
        rhythm.useLevelTimeWhenMusicMissing = false;
        rhythm.awardRhythmScore = false;
        rhythm.SetVisualizationEnabled(false);
        rhythm.SetVisualizationToggleVisible(false);

        if (source.isPlaying)
        {
            source.Stop();
        }
        source.time = 0f;
        ApplyMusicStage(AdvancedMusicStage.Scene, false);
    }

    private void PlaySceneMusic()
    {
        ApplyMusicStage(AdvancedMusicStage.Scene, true);
    }

    private bool StartRhythmClock(AdvancedMusicStage stage)
    {
        return ApplyMusicStage(stage, true);
    }

    // Re-resolve Hierarchy music config each time a stage starts so Inspector
    // edits to AdvancedRunnerConfig/Music are honored on the next run.
    // 每次切换音乐阶段都重新同步 Hierarchy 音乐配置。
    // 这样在 Inspector 里改 AdvancedRunnerConfig/Music/Game 的 clip 或 BPM，
    // 下一次 BeginGame/Retry 就会生效。
    private bool ApplyMusicStage(AdvancedMusicStage stage, bool play)
    {
        ApplyHierarchyConfigToSettings();
        AudioClip clip;
        float bpm;
        GetMusicStageValues(stage, out clip, out bpm);

        AudioSource source = FindOrCreateAdvancedAudioSource();
        advancedMusicSource = source;
        EnsureAdvancedMusicSource(source);
        RhythmManager rhythm = GetOrCreateRhythmManager();
        if (rhythm == null)
        {
            return false;
        }

        settings.bpm = Mathf.Max(1f, bpm > 0f ? bpm : settings.bpm);
        settings.firstBeatOffset = 0f;
        rhythm.musicSource = source;
        rhythm.bpm = settings.bpm;
        rhythm.visualizationBpm = settings.bpm;
        rhythm.firstBeatOffset = settings.firstBeatOffset;
        rhythm.useLevelTimeWhenMusicMissing = false;
        rhythm.awardRhythmScore = false;
        rhythm.SetVisualizationToggleVisible(false);
        source.clip = clip;

        source.playOnAwake = false;
        if (play)
        {
            source.Stop();
            source.time = 0f;
            lastMusicPlaybackTime = 0f;
            if (clip == null)
            {
                WarnAndPause(stage + " music has no AudioClip in AdvancedRunnerConfig/Music");
                return false;
            }

            source.Play();
            Debug.Log("AdvancedRunnerManager: Playing " + stage + " config music clip=" + clip.name + " bpm=" + settings.bpm + " beatInterval=" + BeatInterval.ToString("0.000"));
        }

        return true;
    }

    private void GetMusicStageValues(AdvancedMusicStage stage, out AudioClip clip, out float bpm)
    {
        if (stage == AdvancedMusicStage.Tutorial)
        {
            clip = settings.tutorialBgm;
            bpm = settings.tutorialBpm;
            return;
        }
        if (stage == AdvancedMusicStage.Game)
        {
            clip = settings.gameBgm;
            bpm = settings.gameBpm;
            return;
        }

        clip = settings.sceneBgm;
        bpm = settings.sceneBpm;
    }

    private void StopRhythmClock()
    {
        lastMusicPlaybackTime = 0f;
        RhythmManager rhythm = RhythmManager.Instance != null ? RhythmManager.Instance : FindObjectOfType<RhythmManager>();
        AudioSource source = rhythm != null && rhythm.musicSource != null ? rhythm.musicSource : advancedMusicSource;
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.time = 0f;
    }

    private AudioSource FindAdvancedAudioSource()
    {
        GameObject existing = GameObject.Find("AdvancedRunnerAudio");
        if (existing != null)
        {
            return existing.GetComponent<AudioSource>();
        }

        return advancedMusicSource;
    }

    private AudioSource FindOrCreateAdvancedAudioSource()
    {
        GameObject existing = GameObject.Find("AdvancedRunnerAudio");
        if (existing != null)
        {
            AudioSource existingSource = existing.GetComponent<AudioSource>();
            if (existingSource != null)
            {
                return existingSource;
            }

            return existing.AddComponent<AudioSource>();
        }

        GameObject audioObject = new GameObject("AdvancedRunnerAudio");
        return audioObject.AddComponent<AudioSource>();
    }

    private RhythmManager GetOrCreateRhythmManager()
    {
        RhythmManager rhythm = RhythmManager.Instance != null ? RhythmManager.Instance : FindObjectOfType<RhythmManager>();
        if (rhythm != null)
        {
            return rhythm;
        }

        GameObject obj = new GameObject("RhythmManager");
        return obj.AddComponent<RhythmManager>();
    }

    // Prevent silent broken runs: no chart or no playing configured music means
    // pause the run and keep the player on an overlay instead.
    // 防止静默坏局：如果没有生成目标，或 Game 音乐没有正确播放，
    // 就暂停流程并给 warning，而不是让玩家进入一个无法判定的空场景。
    private bool ValidateRunReady(string context)
    {
        if (targets.Count == 0)
        {
            WarnAndPause(context + " created no targets");
            return false;
        }

        AudioSource source = advancedMusicSource != null ? advancedMusicSource : FindAdvancedAudioSource();
        if (source == null || source.clip == null || !source.isPlaying)
        {
            WarnAndPause(context + " needs playing AdvancedRunnerConfig music");
            return false;
        }

        nextTargetIndex = 0;
        return true;
    }

    private bool EnsureAdvancedMusicSource(AudioSource source)
    {
        if (source == null)
        {
            return false;
        }

        source.enabled = true;
        source.mute = false;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = source.volume <= 0.01f ? 0.38f : source.volume;
        return true;
    }

    private void WarnAndPause(string message)
    {
        waitingForStart = true;
        Debug.LogWarning("AdvancedRunnerManager: " + message);
        if (ui != null && ui.IsReady)
        {
            ui.ShowFeedback(AdvancedFeedbackKey.Wait, message, 0, "");
        }
    }

    private void DisableLegacyRunnerObjects()
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
            if (typeName == "PlayerController"
                || typeName == "BackgroundTranform"
                || typeName == "Barrier"
                || typeName == "GameManager")
            {
                behaviour.enabled = false;
            }
        }

        HideLegacyObjectsByName();
        GameObject gameOver = GameObject.Find("GameOver");
        if (gameOver != null)
        {
            gameOver.SetActive(false);
        }

        HideLegacyUiByKeywords();
    }

    private void HideLegacyObjectsByName()
    {
        string[] names = { "player", "gamemanager", "background1", "floor", "barrierspoint", "barrier" };
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            GameObject obj = transforms[i].gameObject;
            if (!obj.scene.isLoaded || obj.name == "AdvancedRunnerManager" || obj.name == "AdvancedRunnerCanvas")
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
            if (!scenePolicy.autoCreateMissingObjects)
            {
                Debug.LogWarning("AdvancedRunnerManager: Main Camera is missing and auto creation is disabled.");
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
        camera.orthographicSize = 4.8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = settings.backgroundColor;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private void DrawWorldGuide(Transform runtimeRoot)
    {
        worldRoot = runtimeRoot;
        EnsureWorldGuide();
    }

    private void BuildTutorialSteps()
    {
        tutorialSteps = new[]
        {
            new AdvancedRunnerTutorialStep("jump", "Beat Jump", "Press Space or Up on yellow.", 3, new[] { AdvancedActionType.Jump, AdvancedActionType.Jump, AdvancedActionType.Jump }),
            new AdvancedRunnerTutorialStep("slide", "Slide Gate", "Press Down on yellow.", 3, new[] { AdvancedActionType.Slide, AdvancedActionType.Slide, AdvancedActionType.Slide }),
            new AdvancedRunnerTutorialStep("lane", "Lane Switch", "Press Left or Right as the lane target reaches the bottom line.", 4, new[] { AdvancedActionType.LaneLeft, AdvancedActionType.LaneRight, AdvancedActionType.LaneRight, AdvancedActionType.LaneLeft }),
            new AdvancedRunnerTutorialStep("coin", "Coin Line", "Move left or right into the coin lane, then hit coin beats with Space or Down.", 4, new[] { AdvancedActionType.LaneRight, AdvancedActionType.Coin, AdvancedActionType.LaneLeft, AdvancedActionType.Coin }),
            new AdvancedRunnerTutorialStep("mix", "Final Mix", "Read the lane and action together.", 6, new[] { AdvancedActionType.Jump, AdvancedActionType.Slide, AdvancedActionType.LaneRight, AdvancedActionType.Coin, AdvancedActionType.LaneLeft, AdvancedActionType.Jump })
        };
    }

    private Sprite CreateSolidSprite(string name, int size, Color color)
    {
        return CreateSolidSprite(name, size, color, size);
    }

    private Sprite CreateSolidSprite(string name, int size, Color color, float pixelsPerUnit)
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
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
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

    private struct AdvancedRunnerTutorialStep
    {
        public readonly string key;
        public readonly string title;
        public readonly string instruction;
        public readonly int requiredHits;
        private readonly AdvancedActionType[] pattern;

        public AdvancedRunnerTutorialStep(string key, string title, string instruction, int requiredHits, AdvancedActionType[] pattern)
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

// Runtime behavior for the scene-attached AdvancedRunnerPlayer component. The
// same-name bridge script keeps Unity attachments stable while logic stays here.
// 场景中 AdvancedRunnerPlayer 组件的运行时逻辑。
// 同名桥接脚本负责稳定 Unity 挂载引用，真正实现集中在这里。
public partial class AdvancedRunnerPlayer
{
    private AdvancedRunnerSettings settings;
    private SpriteRenderer spriteRenderer;
    private int lane = 1;
    private float jumpTimer;
    private float slideTimer;

    public int CurrentLane { get { return lane; } }
    private AdvancedRunnerManager manager;

    // Bind an existing scene SpriteRenderer when present; only fill missing
    // renderer defaults so artist-authored sprites/colors survive play mode.
    // 绑定玩家视觉：如果场景里已经有 SpriteRenderer，就保留美术设置；
    // 只有缺 sprite 或 sorting order 时才补默认值。
    public void Build(AdvancedRunnerManager manager, AdvancedRunnerSettings settings, Sprite sprite)
    {
        this.manager = manager;
        this.settings = settings;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = settings.playerColor;
            spriteRenderer.sortingOrder = 6;
        }
        else
        {
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = sprite;
            }
            if (spriteRenderer.sortingOrder == 0)
            {
                spriteRenderer.sortingOrder = 6;
            }
        }
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

    // Smoothly eases lane movement and short action poses every frame.
    // 每帧平滑移动到目标 lane，并处理短暂的 jump/slide 姿态表现。
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
        if (manager != null)
        {
            return manager.GetLaneX(lane);
        }

        return (lane - 1) * settings.laneSpacing;
    }
}

// UI binder for AdvancedRunnerCanvas. It wires buttons and dynamic text while
// leaving the scene-authored layout, images, fonts, and sizing editable.
// AdvancedRunnerCanvas 的 UI 绑定器：只绑定按钮和动态文本/进度/beat；
// 布局、图片、字体、尺寸都留给场景 Hierarchy 编辑。
public partial class AdvancedRunnerUI
{
    private sealed class PromptColumn
    {
        public GameObject up;
        public GameObject down;
        public GameObject handUp;
        public GameObject handDown;
        public Transform scaleRoot;
        public Vector3 defaultScale = Vector3.one;
    }

    private AdvancedRunnerManager manager;
    private AdvancedRunnerSettings settings;
    private Font font;
    private Canvas canvas;
    private Text titleText;
    private Text hintText;
    private Text feedbackText;
    private Text statsText;
    private Text beatText;
    private Text heartsValueText;
    private Text scoreValueText;
    private Text comboValueText;
    private Text bestValueText;
    private Text beatValueText;
    private Text lessonProgressValueText;
    private Text resultStatusValueText;
    private Text resultScoreValueText;
    private Text resultPerfectValueText;
    private Text resultGoodValueText;
    private Text resultMissValueText;
    private Text resultComboValueText;
    private Image progressFill;
    private readonly Image[] beatDotImages = new Image[4];
    private readonly Color[] beatDotBaseColors = new Color[4];
    private readonly Vector3[] beatDotBaseScales = new Vector3[4];
    private Image beatPulseImage;
    private Color beatPulseBaseColor = Color.white;
    private Vector3 beatPulseBaseScale = Vector3.one;
    private GameObject controlRhythmPrompt;
    private CanvasGroup controlRhythmPromptGroup;
    private readonly PromptColumn spacePrompt = new PromptColumn();
    private readonly PromptColumn downPrompt = new PromptColumn();
    private readonly PromptColumn leftPrompt = new PromptColumn();
    private readonly PromptColumn rightPrompt = new PromptColumn();
    private GameObject tutorialOverlay;
    private GameObject gameRulesOverlay;
    private GameObject resultOverlay;
    private Text resultTitle;
    private Text resultStats;
    private Sprite circleSprite;
    private string tutorialEntryTitleText;
    private string tutorialEntryBodyText;
    private string tutorialEntryButtonText;
    private bool hasTutorialEntryTextSnapshot;

    public bool IsReady { get; private set; }

    public void Build(AdvancedRunnerManager manager, AdvancedRunnerSettings settings, Sprite circleSprite)
    {
        Build(manager, settings, circleSprite, RuntimeScenePolicy.Defaults());
    }

    // Advanced UI must come from the Hierarchy; missing required entry UI stops
    // the manager instead of creating a replacement layout at runtime.
    // Advanced UI 必须来自场景 Hierarchy。缺少必要入口 UI 时，
    // 这里不会运行时创建替代 UI，而是让 manager 停下来并输出 warning。
    public void Build(AdvancedRunnerManager manager, AdvancedRunnerSettings settings, Sprite circleSprite, RuntimeScenePolicy scenePolicy)
    {
        IsReady = false;
        this.manager = manager;
        this.settings = settings;
        this.circleSprite = circleSprite;
        if (scenePolicy == null)
        {
            scenePolicy = RuntimeScenePolicy.Defaults();
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        EnsureEventSystem();
        GameObject existing = GameObject.Find("AdvancedRunnerCanvas");
        if (existing == null)
        {
            Debug.LogWarning("AdvancedRunnerUI: AdvancedRunnerCanvas is missing. Use the scene Hierarchy canvas for AdvancedRunner UI.");
            return;
        }

        if (!scenePolicy.useExistingSceneObjects)
        {
            Debug.LogWarning("AdvancedRunnerUI: scenePolicy.useExistingSceneObjects is disabled, but AdvancedRunner UI must use the scene Hierarchy canvas.");
            return;
        }

        if (scenePolicy.rebuildUiOnPlay)
        {
            Debug.LogWarning("AdvancedRunnerUI: rebuildUiOnPlay is ignored. AdvancedRunner UI is bound from the scene Hierarchy.");
        }

        if (BindExistingCanvas(existing))
        {
            IsReady = true;
            return;
        }

        Debug.LogWarning("AdvancedRunnerUI: Existing AdvancedRunnerCanvas is missing TutorialOverlay/Card/StartButton. Keeping the scene UI untouched.");
    }

    // Name/path binding contract for AdvancedRunnerCanvas. If these paths are
    // renamed in the Hierarchy, update this method and the baker together.
    // AdvancedRunnerCanvas 的名字/路径绑定合约。
    // 如果你在 Hierarchy 里改了这些节点名字，必须同步改这里和 baker，
    // 否则运行时会找不到 UI。
    private bool BindExistingCanvas(GameObject existing)
    {
        canvas = existing.GetComponent<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        titleText = FindText(existing.transform, "TopHud/Title");
        hintText = FindText(existing.transform, "TopHud/Hint");
        statsText = FindText(existing.transform, "TopHud/Stats");
        heartsValueText = FindText(existing.transform, "TopHud/Hearts/Value");
        scoreValueText = FindText(existing.transform, "TopHud/Score/Value");
        comboValueText = FindText(existing.transform, "TopHud/Combo/Value");
        bestValueText = FindText(existing.transform, "TopHud/Best/Value");
        lessonProgressValueText = FindText(existing.transform, "ObjectivePanel/LessonProgress/Value");
        feedbackText = FindText(existing.transform, "BottomHud/Feedback");
        beatText = FindText(existing.transform, "BottomHud/Beat");
        beatValueText = FindText(existing.transform, "BottomHud/Beat/Value");
        BindBeatVisuals(existing.transform);
        EnsureControlRhythmPrompt(existing.transform);
        progressFill = FindImage(existing.transform, "BottomHud/Progress/Fill");
        tutorialOverlay = FindObject(existing.transform, "TutorialOverlay");
        gameRulesOverlay = FindObject(existing.transform, "GameRulesOverlay");
        resultOverlay = FindObject(existing.transform, "AdvancedRunnerResult");
        resultTitle = FindText(existing.transform, "AdvancedRunnerResult/Card/Title");
        resultStats = FindText(existing.transform, "AdvancedRunnerResult/Card/Stats");
        resultStatusValueText = FindText(existing.transform, "AdvancedRunnerResult/Card/Status/Value");
        resultScoreValueText = FindText(existing.transform, "AdvancedRunnerResult/Card/Score/Value");
        resultPerfectValueText = FindText(existing.transform, "AdvancedRunnerResult/Card/Perfect/Value");
        resultGoodValueText = FindText(existing.transform, "AdvancedRunnerResult/Card/Good/Value");
        resultMissValueText = FindText(existing.transform, "AdvancedRunnerResult/Card/Miss/Value");
        resultComboValueText = FindText(existing.transform, "AdvancedRunnerResult/Card/MaxCombo/Value");

        Transform tutorialStartButton = FindTransform(existing.transform, "TutorialOverlay/Card/StartButton");
        if (tutorialOverlay == null || tutorialStartButton == null)
        {
            return false;
        }

        CaptureTutorialEntryText();
        BindButton(existing.transform, "TutorialOverlay/Card/StartButton", delegate
        {
            if (manager != null)
            {
                tutorialOverlay.SetActive(false);
                manager.BeginGameFromUi();
            }
        });
        if (gameRulesOverlay != null)
        {
            BindButton(existing.transform, "GameRulesOverlay/Card/StartButton", delegate
            {
                if (manager != null)
                {
                    gameRulesOverlay.SetActive(false);
                    manager.BeginGameFromUi();
                }
            });
        }
        if (resultOverlay != null)
        {
            BindButton(existing.transform, "AdvancedRunnerResult/Card/Retry", delegate { if (manager != null) manager.BeginGameFromUi(); });
            BindButton(existing.transform, "AdvancedRunnerResult/Card/Back", delegate { SceneTransitionManager.LoadScene("Start"); });
        }
        ApplyFeedbackStyle();
        tutorialOverlay.SetActive(false);
        if (gameRulesOverlay != null)
        {
            gameRulesOverlay.SetActive(false);
        }
        if (resultOverlay != null)
        {
            resultOverlay.SetActive(false);
        }
        UpdateControlRhythmPrompt(false, false, false, false, false);
        return true;
    }

    private Transform FindTransform(Transform root, string path)
    {
        return root != null ? root.Find(path) : null;
    }

    private GameObject FindObject(Transform root, string path)
    {
        Transform child = FindTransform(root, path);
        return child != null ? child.gameObject : null;
    }

    private Text FindText(Transform root, string path)
    {
        Transform child = FindTransform(root, path);
        return child != null ? child.GetComponent<Text>() : null;
    }

    private Image FindImage(Transform root, string path)
    {
        Transform child = FindTransform(root, path);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private void EnsureControlRhythmPrompt(Transform root)
    {
        if (controlRhythmPrompt == null)
        {
            controlRhythmPrompt = FindObject(root, "BottomHud/ControlRhythmPrompt");
        }
        if (controlRhythmPrompt == null)
        {
            Transform bottom = FindTransform(root, "BottomHud");
            if (bottom == null)
            {
                return;
            }

            controlRhythmPrompt = CreateUiRect(bottom, "ControlRhythmPrompt", new Vector2(0.5f, 0.5f), new Vector2(260f, 0f), new Vector2(420f, 130f)).gameObject;
        }

        controlRhythmPromptGroup = controlRhythmPrompt.GetComponent<CanvasGroup>();
        if (controlRhythmPromptGroup == null)
        {
            controlRhythmPromptGroup = controlRhythmPrompt.AddComponent<CanvasGroup>();
        }
        controlRhythmPromptGroup.interactable = false;
        controlRhythmPromptGroup.blocksRaycasts = false;

        EnsurePromptColumn(controlRhythmPrompt.transform, "SpaceColumn", "SpaceUp", "SpaceDown", new Vector2(0.125f, 0.5f));
        EnsurePromptColumn(controlRhythmPrompt.transform, "DownColumn", "DownUp", "DownDown", new Vector2(0.375f, 0.5f));
        EnsurePromptColumn(controlRhythmPrompt.transform, "LeftColumn", "LeftUp", "LeftDown", new Vector2(0.625f, 0.5f));
        EnsurePromptColumn(controlRhythmPrompt.transform, "RightColumn", "RightUp", "RightDown", new Vector2(0.875f, 0.5f));
        CacheControlRhythmPrompt();
    }

    private void EnsurePromptColumn(Transform parent, string columnName, string upName, string downName, Vector2 anchor)
    {
        RectTransform column = EnsurePromptSlot(parent, columnName, anchor, Vector2.zero, new Vector2(86f, 96f));
        RectTransform key = EnsurePromptSlot(column, "Key", new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(78f, 52f));
        RectTransform hand = EnsurePromptSlot(column, "Hand", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(54f, 42f));
        EnsurePromptImageSlot(key, upName);
        EnsurePromptImageSlot(key, downName);
        EnsurePromptImageSlot(hand, "HandUp");
        EnsurePromptImageSlot(hand, "HandDown");
    }

    private RectTransform EnsurePromptSlot(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        if (existing != null)
        {
            RectTransform existingRect = existing.GetComponent<RectTransform>();
            if (existingRect != null)
            {
                return existingRect;
            }
        }

        return CreateUiRect(parent, name, anchor, position, size);
    }

    private void EnsurePromptImageSlot(Transform parent, string name)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        GameObject slot = existing != null ? existing.gameObject : CreateUiRect(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
        if (existing == null)
        {
            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            slot.SetActive(false);
        }

        Image image = slot.GetComponent<Image>();
        if (image == null)
        {
            image = slot.AddComponent<Image>();
            image.preserveAspect = true;
        }
        image.raycastTarget = false;
    }

    private RectTransform CreateUiRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            obj.transform.SetParent(parent, false);
        }

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private void CacheControlRhythmPrompt()
    {
        if (controlRhythmPrompt == null)
        {
            return;
        }

        CachePromptColumn(spacePrompt, "SpaceColumn", "SpaceUp", "SpaceDown");
        CachePromptColumn(downPrompt, "DownColumn", "DownUp", "DownDown");
        CachePromptColumn(leftPrompt, "LeftColumn", "LeftUp", "LeftDown");
        CachePromptColumn(rightPrompt, "RightColumn", "RightUp", "RightDown");
    }

    private void CachePromptColumn(PromptColumn column, string columnName, string upName, string downName)
    {
        if (column == null || controlRhythmPrompt == null)
        {
            return;
        }

        Transform root = controlRhythmPrompt.transform.Find(columnName);
        column.scaleRoot = root;
        column.defaultScale = root != null ? root.localScale : Vector3.one;
        column.up = root != null ? FindObject(root, "Key/" + upName) : null;
        column.down = root != null ? FindObject(root, "Key/" + downName) : null;
        column.handUp = root != null ? FindObject(root, "Hand/HandUp") : null;
        column.handDown = root != null ? FindObject(root, "Hand/HandDown") : null;
        SetPromptSlotNonRaycast(column.up);
        SetPromptSlotNonRaycast(column.down);
        SetPromptSlotNonRaycast(column.handUp);
        SetPromptSlotNonRaycast(column.handDown);
    }

    private void SetPromptSlotNonRaycast(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        Image image = obj.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
        }
    }

    private void BindBeatVisuals(Transform root)
    {
        for (int i = 0; i < beatDotImages.Length; i++)
        {
            Image dot = FindImage(root, "BottomHud/Beat/BeatDot_" + i);
            beatDotImages[i] = dot;
            if (dot != null)
            {
                beatDotBaseColors[i] = dot.color;
                beatDotBaseScales[i] = dot.transform.localScale;
            }
        }

        beatPulseImage = FindImage(root, "BottomHud/Beat/Pulse");
        if (beatPulseImage != null)
        {
            beatPulseBaseColor = beatPulseImage.color;
            beatPulseBaseScale = beatPulseImage.transform.localScale;
        }
    }

    private void BindButton(Transform root, string path, UnityEngine.Events.UnityAction onClick)
    {
        Transform child = FindTransform(root, path);
        if (child == null)
        {
            return;
        }

        Button button = child.GetComponent<Button>();
        if (button == null)
        {
            button = child.gameObject.AddComponent<Button>();
            Graphic graphic = child.GetComponent<Graphic>();
            if (graphic != null)
            {
                button.targetGraphic = graphic;
            }
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }

    public void ShowBriefing(string title, string body, string buttonLabel, UnityEngine.Events.UnityAction onStart)
    {
        ShowBriefingOnOverlay(manager != null && manager.mode == AdvancedRunnerMode.Tutorial ? tutorialOverlay : gameRulesOverlay, onStart, false);
    }

    public void ShowTutorialEntryBriefing(UnityEngine.Events.UnityAction onStart)
    {
        ShowBriefingOnOverlay(tutorialOverlay, onStart, true);
    }

    // Shows an existing overlay and rewires only the Start button callback.
    // Tutorial entry text can be restored from the scene snapshot.
    // 显示已有 overlay，并且只重绑 StartButton 的点击事件。
    // 如果是 TutorialOverlay 入口页，会恢复场景里原本写好的标题/正文/按钮文字。
    private void ShowBriefingOnOverlay(GameObject overlay, UnityEngine.Events.UnityAction onStart, bool restoreTutorialEntryText)
    {
        HideRunOverlays();

        if (overlay == null)
        {
            return;
        }

        overlay.SetActive(true);
        if (restoreTutorialEntryText && overlay == tutorialOverlay)
        {
            RestoreTutorialEntryText();
        }

        Transform card = overlay.transform.Find("Card");
        Transform startButton = card != null ? card.Find("StartButton") : null;
        Button button = startButton != null ? startButton.GetComponent<Button>() : null;
        if (button == null)
        {
            Debug.LogWarning("AdvancedRunnerUI: TutorialOverlay/Card/StartButton is missing a Button component.");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate
        {
            overlay.SetActive(false);
            onStart();
        });
    }

    // Preserve designer-authored entry copy before runtime feedback/result
    // fallbacks are allowed to write into the same overlay.
    // 保存设计师在 Hierarchy 里写好的入口页文案。
    // 后面 Game Over fallback 可能临时复用同一个 overlay，所以需要能恢复。
    private void CaptureTutorialEntryText()
    {
        if (tutorialOverlay == null)
        {
            return;
        }

        Transform card = tutorialOverlay.transform.Find("Card");
        Text title = card != null ? FindText(card, "Title") : null;
        Text body = card != null ? FindText(card, "Body") : null;
        Text button = card != null ? FindText(card, "StartButton/Text") : null;
        tutorialEntryTitleText = title != null ? title.text : "";
        tutorialEntryBodyText = body != null ? body.text : "";
        tutorialEntryButtonText = button != null ? button.text : "";
        hasTutorialEntryTextSnapshot = true;
    }

    private void RestoreTutorialEntryText()
    {
        if (!hasTutorialEntryTextSnapshot || tutorialOverlay == null)
        {
            return;
        }

        Transform card = tutorialOverlay.transform.Find("Card");
        Text title = card != null ? FindText(card, "Title") : null;
        Text body = card != null ? FindText(card, "Body") : null;
        Text button = card != null ? FindText(card, "StartButton/Text") : null;
        if (title != null)
        {
            title.text = tutorialEntryTitleText;
        }
        if (body != null)
        {
            body.text = tutorialEntryBodyText;
        }
        if (button != null)
        {
            button.text = tutorialEntryButtonText;
        }
    }

    public void HideRunOverlays()
    {
        UpdateControlRhythmPrompt(false, false, false, false, false);
        if (tutorialOverlay != null)
        {
            tutorialOverlay.SetActive(false);
        }
        if (gameRulesOverlay != null)
        {
            gameRulesOverlay.SetActive(false);
        }
        if (resultOverlay != null)
        {
            resultOverlay.SetActive(false);
        }
    }

    public void ShowGameIntro()
    {
        if (lessonProgressValueText != null)
        {
            lessonProgressValueText.text = "";
        }
        else
        {
            if (titleText != null)
            {
                titleText.text = "Advanced Runner";
            }
            if (hintText != null)
            {
                hintText.text = "Read lane + action. Hit on yellow.";
            }
        }
    }

    public void ShowTutorialStep(string title, string instruction, int current, int required, int index, int count)
    {
        if (lessonProgressValueText != null)
        {
            lessonProgressValueText.text = index + "/" + count + "  " + current + "/" + required;
        }
        else
        {
            if (titleText != null)
            {
                titleText.text = "Lesson " + index + " / " + count + "  " + title;
            }
            if (hintText != null)
            {
                hintText.text = instruction + "  Progress " + current + " / " + required;
            }
        }
    }

    // HUD update is intentionally narrow: dynamic values, progress fill, and
    // beat visualization only. Static labels and art remain scene-owned.
    // HUD 更新范围刻意保持很窄：只改动态数值、进度条和 beat 可视化。
    // 静态 label、图标、美术样式仍然由场景负责。
    public void UpdateStats(int hearts, int score, int combo, int maxCombo, float progress01, float beatPosition)
    {
        if (scoreValueText != null && comboValueText != null)
        {
            if (heartsValueText != null)
            {
                heartsValueText.text = "";
            }
            scoreValueText.text = score.ToString();
            comboValueText.text = combo.ToString();
            if (bestValueText != null)
            {
                bestValueText.text = "";
            }
        }
        else if (statsText != null)
        {
            statsText.text = "Score " + score + "   Combo " + combo;
        }

        if (progressFill != null)
        {
            progressFill.fillAmount = Mathf.Clamp01(progress01);
        }
        if (beatValueText != null)
        {
            beatValueText.text = beatPosition.ToString("0.00");
        }
        else if (beatText != null)
        {
            beatText.text = "Beat " + beatPosition.ToString("0.00");
        }

        UpdateBeatVisuals(beatPosition);
    }

    public void UpdateControlRhythmPrompt(bool spaceDown, bool downDown, bool leftDown, bool rightDown, bool visible)
    {
        if (controlRhythmPrompt == null)
        {
            return;
        }

        if (controlRhythmPrompt.activeSelf != visible)
        {
            controlRhythmPrompt.SetActive(visible);
        }
        if (controlRhythmPromptGroup != null)
        {
            controlRhythmPromptGroup.alpha = visible ? 1f : 0f;
            controlRhythmPromptGroup.interactable = false;
            controlRhythmPromptGroup.blocksRaycasts = false;
        }

        SetPromptColumnActive(spacePrompt, visible, spaceDown);
        SetPromptColumnActive(downPrompt, visible, downDown);
        SetPromptColumnActive(leftPrompt, visible, leftDown);
        SetPromptColumnActive(rightPrompt, visible, rightDown);
        ApplyPromptColumnScale(spacePrompt, visible && spaceDown);
        ApplyPromptColumnScale(downPrompt, visible && downDown);
        ApplyPromptColumnScale(leftPrompt, visible && leftDown);
        ApplyPromptColumnScale(rightPrompt, visible && rightDown);
    }

    private void SetPromptColumnActive(PromptColumn column, bool visible, bool pressed)
    {
        SetActiveIfPresent(column.up, visible && !pressed);
        SetActiveIfPresent(column.down, visible && pressed);
        SetActiveIfPresent(column.handUp, visible && !pressed);
        SetActiveIfPresent(column.handDown, visible && pressed);
    }

    private void ApplyPromptColumnScale(PromptColumn column, bool pressed)
    {
        if (column.scaleRoot == null)
        {
            return;
        }

        float targetScale = pressed ? 1.08f : 1f;
        Vector3 baseScale = column.defaultScale == Vector3.zero ? Vector3.one : column.defaultScale;
        column.scaleRoot.localScale = Vector3.Lerp(column.scaleRoot.localScale, baseScale * targetScale, Time.unscaledDeltaTime * 16f);
    }

    private void SetActiveIfPresent(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active)
        {
            obj.SetActive(active);
        }
    }

    // Four-dot beat display follows the same beat clock that drives targets and
    // input windows.
    // 四点 beat 显示使用和目标下落、输入判定完全相同的 beat 时钟。
    // 如果 HUD 和目标不同步，优先检查 GetBeatPosition()/音乐 BPM。
    private void UpdateBeatVisuals(float beatPosition)
    {
        int currentBeat = Mathf.FloorToInt(Mathf.Max(0f, beatPosition));
        int currentDot = currentBeat % beatDotImages.Length;
        float beatPhase = Mathf.Repeat(Mathf.Max(0f, beatPosition), 1f);
        float pulse = 1f - Mathf.Clamp01(beatPhase);
        float pulseEase = pulse * pulse;

        for (int i = 0; i < beatDotImages.Length; i++)
        {
            Image dot = beatDotImages[i];
            if (dot == null)
            {
                continue;
            }

            bool active = i == currentDot;
            Color baseColor = beatDotBaseColors[i];
            Color color = baseColor;
            color.a = active ? Mathf.Max(baseColor.a, 0.65f) : baseColor.a * 0.38f;
            dot.color = color;
            Vector3 baseScale = beatDotBaseScales[i] == Vector3.zero ? Vector3.one : beatDotBaseScales[i];
            dot.transform.localScale = baseScale * (active ? Mathf.Lerp(1.12f, 1.42f, pulseEase) : 0.88f);
        }

        if (beatPulseImage != null)
        {
            Color color = beatPulseBaseColor;
            color.a = Mathf.Max(beatPulseBaseColor.a, 0.18f) * pulseEase;
            beatPulseImage.color = color;
            beatPulseImage.transform.localScale = beatPulseBaseScale * Mathf.Lerp(1.85f, 1f, pulseEase);
        }
    }

    private void ApplyFeedbackStyle()
    {
        if (feedbackText == null || settings == null || settings.feedback == null)
        {
            return;
        }

        AdvancedFeedbackStyle style = settings.feedback;
        Font selectedFont = style.font != null ? style.font : font;
        if (selectedFont != null)
        {
            feedbackText.font = selectedFont;
        }
        feedbackText.fontSize = Mathf.Max(1, style.fontSize);
        feedbackText.fontStyle = style.fontStyle;
    }

    public void ShowFeedback(AdvancedFeedbackKey key)
    {
        ShowFeedback(key, "", 0, "");
    }

    // Feedback text comes from AdvancedRunnerConfig/Feedback when available so
    // copy, color, font, and pulse can be adjusted in the Hierarchy.
    // 反馈文字优先来自 AdvancedRunnerConfig/Feedback。
    // 这样文案、颜色、字体、缩放反馈都可以在 Hierarchy/Inspector 中调整。
    public void ShowFeedback(AdvancedFeedbackKey key, string detailOverride, int laneNumber, string actionLabel)
    {
        if (feedbackText == null)
        {
            return;
        }

        if (settings == null || settings.feedback == null)
        {
            ShowFeedback(key.ToString(), detailOverride, Color.white);
            return;
        }

        AdvancedFeedbackStyle style = settings.feedback;
        ApplyFeedbackStyle();
        string label = style.GetLabel(key);
        string detail = !string.IsNullOrEmpty(detailOverride) ? detailOverride : style.GetDefaultDetail(key);
        detail = FormatFeedbackDetail(detail, laneNumber, actionLabel);
        feedbackText.text = style.includeDetail && !string.IsNullOrEmpty(detail) ? label + "  " + detail : label;
        feedbackText.color = style.GetColor(key);
        feedbackText.transform.localScale = Vector3.one * Mathf.Max(1f, style.pulseScale);
    }

    public void ShowFeedback(string label, string detail, Color color)
    {
        feedbackText.text = string.IsNullOrEmpty(detail) ? label : label + "  " + detail;
        feedbackText.color = color;
        feedbackText.transform.localScale = Vector3.one * 1.16f;
    }

    private string FormatFeedbackDetail(string detail, int laneNumber, string actionLabel)
    {
        if (string.IsNullOrEmpty(detail))
        {
            return "";
        }

        string action = string.IsNullOrEmpty(actionLabel) ? "" : actionLabel;
        string lane = laneNumber > 0 ? laneNumber.ToString() : "";
        return detail.Replace("{action}", action).Replace("{lane}", lane);
    }

    // Current result screen is score-only Game Over. Hidden rows stay in the
    // Hierarchy for future reuse but are not shown by the active rule set.
    // 当前结果页规则是“只显示 Score 的 Game Over”。
    // Perfect/Good/Miss/MaxCombo 等行保留在 Hierarchy，方便以后恢复使用，
    // 但当前玩法不会显示它们。
    public void ShowResult(bool completed, int score, int perfect, int good, int miss, int maxCombo)
    {
        if (tutorialOverlay != null)
        {
            tutorialOverlay.SetActive(false);
        }
        if (gameRulesOverlay != null)
        {
            gameRulesOverlay.SetActive(false);
        }

        if (resultOverlay == null)
        {
            ShowResultOnTutorialOverlay(score);
            return;
        }

        resultOverlay.SetActive(true);
        SetResultRowVisible("Status", false);
        SetResultRowVisible("Perfect", false);
        SetResultRowVisible("Good", false);
        SetResultRowVisible("Miss", false);
        SetResultRowVisible("MaxCombo", false);
        SetResultRowVisible("Score", true);
        if (resultStatusValueText != null)
        {
            resultStatusValueText.text = "";
        }
        if (resultScoreValueText != null)
        {
            resultScoreValueText.text = score.ToString();
        }
        if (resultPerfectValueText != null)
        {
            resultPerfectValueText.text = "";
        }
        if (resultGoodValueText != null)
        {
            resultGoodValueText.text = "";
        }
        if (resultMissValueText != null)
        {
            resultMissValueText.text = "";
        }
        if (resultComboValueText != null)
        {
            resultComboValueText.text = "";
        }
        if (resultTitle != null)
        {
            resultTitle.text = "Game Over";
        }
        if (resultStats != null)
        {
            resultStats.text = "Score: " + score;
        }
    }

    private void SetResultRowVisible(string rowName, bool visible)
    {
        if (resultOverlay == null)
        {
            return;
        }

        Transform row = resultOverlay.transform.Find("Card/" + rowName);
        if (row != null)
        {
            row.gameObject.SetActive(visible);
        }
    }

    private void ShowResultOnTutorialOverlay(int score)
    {
        if (tutorialOverlay == null)
        {
            ShowFeedback("Game Over", "Score: " + score, Color.white);
            return;
        }

        tutorialOverlay.SetActive(true);
        Transform card = tutorialOverlay.transform.Find("Card");
        Text title = card != null ? FindText(card, "Title") : null;
        Text body = card != null ? FindText(card, "Body") : null;
        if (title != null)
        {
            title.text = "Game Over";
        }
        if (body != null)
        {
            body.text = "Score: " + score;
        }
        Text buttonText = card != null ? FindText(card, "StartButton/Text") : null;
        if (buttonText != null)
        {
            buttonText.text = "Retry";
        }

        BindButton(tutorialOverlay.transform, "Card/StartButton", delegate
        {
            tutorialOverlay.SetActive(false);
            if (manager != null)
            {
                manager.BeginGameFromUi();
            }
        });

        Transform back = card != null ? card.Find("Back") : null;
        if (back == null && card != null)
        {
            back = card.Find("BackButton");
        }
        if (back != null)
        {
            back.gameObject.SetActive(true);
            Button button = back.GetComponent<Button>();
            if (button == null)
            {
                button = back.gameObject.AddComponent<Button>();
            }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { SceneTransitionManager.LoadScene("Start"); });
        }
        else
        {
            BindButton(tutorialOverlay.transform, "Card/StartButton", delegate { SceneTransitionManager.LoadScene("Start"); });
        }
    }

    private void Update()
    {
        if (feedbackText != null)
        {
            feedbackText.transform.localScale = Vector3.Lerp(feedbackText.transform.localScale, Vector3.one, Time.deltaTime * 8f);
        }
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
