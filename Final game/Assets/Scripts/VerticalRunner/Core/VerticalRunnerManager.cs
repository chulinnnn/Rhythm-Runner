using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// VerticalRunnerManager is the scene-level gameplay owner for VerticalRunner.

//Update() 每帧总入口：视觉节拍 → player.Tick() → 相机 → HUD
//GetBeatPosition() 当前第几拍；跳跃拍/动作拍、漏按检测都靠它
//ReportJumpInput：Space 跳跃是否踩准节拍。
//ReportCoinInput：Down/S 抓香蕉是否踩准动作拍。
//ReportDirectionalDodge：鹦鹉分支是否在正确拍子按对方向，并且按住 Space。


[DefaultExecutionOrder(-1000)]
public class VerticalRunnerManager : MonoBehaviour
{
    /// <summary>
    /// Holds the visual-only pressed/up state for the four independent prompt columns.
    /// </summary>
    /// <remarks>
    /// 中文：记录 Space、Down、Left、Right 四列图标此刻是否应该显示 Down。
    /// 这个状态只影响视觉提示，不读取玩家真实按键，也不影响玩法判定。
    /// </remarks>
    private struct ControlPromptState
    {
        public bool show;
        public bool spaceDown;
        public bool downDown;
        public bool leftDown;
        public bool rightDown;
    }

    private static bool registered;
    private const string VerticalRunnerSceneName = "VerticalRunner";

    [Header("Mode")]
    public VerticalRunnerMode mode = VerticalRunnerMode.Game;

    [Header("Settings")]
    public VerticalRunnerSettings settings = new VerticalRunnerSettings();

    [Header("Runtime scene policy")]
    public RuntimeScenePolicy scenePolicy = CreateDefaultScenePolicy();

    [Header("Visual Timing")]
    public float visualBeatDelaySeconds = 0.15f;

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

    /// <summary>
    /// Creates the default runtime policy used when the scene is missing required helper objects.
    /// </summary>
    /// <remarks>
    /// 中文：设置默认运行策略，例如是否允许自动补对象、是否覆盖相机位置、
    /// 以及生成物体放到哪个 runtime root 下。
    /// </remarks>
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

    /// <summary>
    /// Registers a scene-load hook so the manager can exist automatically when VerticalRunner opens.
    /// </summary>
    /// <remarks>
    /// 中文：进入 VerticalRunner 场景后，如果没有主控对象，就自动创建一个。
    /// </remarks>
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

    /// <summary>
    /// Reacts to Unity scene load events and ensures the VerticalRunner scene has a manager.
    /// </summary>
    /// <remarks>
    /// 中文：每次加载新场景时检查，如果是 VerticalRunner 就确认主控存在。
    /// </remarks>
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    /// <summary>
    /// Creates a VerticalRunnerManager only for the VerticalRunner scene when none is present.
    /// </summary>
    /// <remarks>
    /// 中文：只在 VerticalRunner 场景补主控，避免影响 Start、Ocean、Advanced 等其他场景。
    /// </remarks>
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

    /// <summary>
    /// Binds the scene, builds generated runtime objects, creates helper sprites, and prepares UI/world/player references.
    /// </summary>
    /// <remarks>
    /// 中文：场景启动时的初始化入口。这里会找模板、建路线、建玩家、绑定 UI 和相机。
    /// 如果关键对象缺失且不能自动创建，就停用脚本避免后续报错。
    /// </remarks>
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

    /// <summary>
    /// Starts the first tutorial or game flow after Awake has finished building the scene runtime.
    /// </summary>
    /// <remarks>
    /// 中文：初始化完成后决定先进入教程还是正式游戏，并显示对应说明/倒计时。
    /// </remarks>
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

    /// <summary>
    /// Runs the main frame loop for visual beat updates, prompt updates, player ticking, camera following, completion checks, and HUD refresh.
    /// </summary>
    /// <remarks>
    /// 中文：这是本脚本最核心的运行函数。每帧先更新 BeatLane 和四列 icon 的视觉节拍，
    /// 再在没有弹窗/倒计时阻塞时推进玩家输入、相机、通关检查和 HUD 数值。
    /// </remarks>
    private void Update()
    {
        // Core loop: update visual timing first, then gameplay/player/camera while no modal state blocks input.
        // 核心循环：先更新视觉节拍，再在没有弹窗/倒计时阻塞时推进玩家和相机。
        float beatPosition = GetBeatPosition();
        float visualBeatPosition = beatPosition - visualBeatDelaySeconds / BeatInterval();
        if (ui != null)
        {
            ControlPromptState prompt = ResolveControlPromptState(visualBeatPosition);
            ui.UpdateControlRhythmPrompt(prompt.spaceDown, prompt.downDown, prompt.leftDown, prompt.rightDown, prompt.show);
            if (!runEnded && !waitingForBriefing && !waitingForGameRules)
            {
                ui.UpdateBeatLane(visualBeatPosition, settings.startBeat, settings.beatsPerPlatform);
            }
        }

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
        if (mode == VerticalRunnerMode.Game && Time.time - startTime >= settings.songDurationSeconds)
        {
            CompleteRun();
        }

        UpdateUi();
    }

    /// <summary>
    /// Returns whether the current beat slot matches the supplied route beat index.
    /// </summary>
    /// <remarks>
    /// 中文：判断当前节拍是否轮到某个平台/路线点对应的拍子。
    /// </remarks>
    public bool IsBeatInWindow(int beatIndex)
    {
        return IsCurrentBeatSlotForBeatIndex(beatIndex);
    }

    /// <summary>
    /// Returns whether the full beat slot for a route beat has already passed.
    /// </summary>
    /// <remarks>
    /// 中文：判断某个拍子的操作时机是否已经过去，用来处理漏跳/漏香蕉/漏鹦鹉。
    /// </remarks>
    public bool HasPassedBeatWindow(int beatIndex)
    {
        return beatIndex >= 0 && GetBeatPosition() >= beatIndex + 1f;
    }

    /// <summary>
    /// Judges a Space jump input against the jump beat slot and records score or miss state.
    /// </summary>
    /// <remarks>
    /// 中文：Space 跳跃的核心判定入口。Player 负责读键和移动，Manager 负责判断是否踩在跳跃拍上。
    /// 成功则加跳跃分，失败则记 miss 并显示 Space 提示。
    /// </remarks>
    public bool ReportJumpInput(VerticalRunnerPlatform next, out RhythmTimingResult result)
    {
        // Space/jump judgment entry. Player reads the key; Manager owns rhythm scoring.
        // Space/跳跃判定入口。Player 读取按键，Manager 负责节奏评分。
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

    /// <summary>
    /// Judges a banana pickup input against the between-jump action beat slot.
    /// </summary>
    /// <remarks>
    /// 中文：Down/S 抓香蕉的核心判定入口。它使用跳跃之后的动作拍，而不是 Space 跳跃拍。
    /// 成功返回 true，失败会把香蕉标记为 missed 并显示提示。
    /// </remarks>
    public bool ReportCoinInput(VerticalRunnerPickup pickup, out RhythmTimingResult result)
    {
        // Banana/Down judgment entry. This uses the between-jump action beat window.
        // 香蕉/Down 判定入口。这里使用跳跃之间的动作拍窗口。
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

    /// <summary>
    /// Judges a parrot branch dodge using action-beat timing, safe direction, and held Space.
    /// </summary>
    /// <remarks>
    /// 中文：鹦鹉左右分支的核心判定入口。玩家必须在动作拍按对安全方向，
    /// 同时按住 Space，才算成功躲避鹦鹉。
    /// </remarks>
    public bool ReportDirectionalDodge(VerticalRunnerPlatform origin, VerticalBranchChoice choice, bool spaceHeld, out RhythmTimingResult result)
    {
        // Parrot branch judgment entry: timing, safe direction, and held Space are all required.
        // 鹦鹉分支判定入口：节拍、安全方向和按住 Space 都必须满足。
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

    /// <summary>
    /// Records an automatic miss when the player passes a required jump beat without jumping.
    /// </summary>
    /// <remarks>
    /// 中文：玩家错过该跳的拍子时调用，记录 miss 并显示 Space 提示。
    /// </remarks>
    public void ReportMissedJump(VerticalRunnerPlatform next)
    {
        RecordRhythmResult(RhythmTimingResult.Miss);
        TakeDamage("Miss", "Space", false, false);
    }

    /// <summary>
    /// Records an automatic miss when the player passes a banana action beat without collecting it.
    /// </summary>
    /// <remarks>
    /// 中文：玩家错过香蕉动作拍时调用，记录 miss 并提示 Down/S。
    /// </remarks>
    public void ReportMissedPickup(VerticalRunnerPickup pickup)
    {
        RecordRhythmResult(RhythmTimingResult.Miss);
        MissPickup(pickup, "Banana", "Down/S", false);
    }

    /// <summary>
    /// Records an automatic miss when the player passes a parrot branch action beat without dodging correctly.
    /// </summary>
    /// <remarks>
    /// 中文：玩家错过鹦鹉分支动作拍时调用，记录 miss 并提示 Space + Left/Right。
    /// </remarks>
    public void ReportMissedParrot(VerticalRunnerPlatform origin)
    {
        RecordRhythmResult(RhythmTimingResult.Miss);
        TakeDamage("Parrot", "Space + Left/Right", false, false);
    }


    /// 中文：兼容旧调用路径，外部已经算好结果时只负责记录 Perfect/Good/Miss。
    /// </remarks>
    public void ReportJumpInput(RhythmTimingResult result)
    {
        RecordRhythmResult(result);
    }

 
    /// 中文：兼容旧调用路径，外部传入抓香蕉结果，Manager 负责记录和失败反馈。
    /// </remarks>
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


    /// 中文：兼容旧调用路径，外部传入节拍结果和方向是否正确，Manager 负责加分或失败反馈。
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

    /// <summary>
    /// Shows a short UI hint for the parrot branch input.
    /// </summary>
    /// <remarks>
    /// 中文：显示鹦鹉分支提示，告诉玩家需要 Space + 左/右。
    /// </remarks>
    public void ShowDirectionalChoiceHint()
    {
        if (ui != null)
        {
            ui.ShowFeedback("Parrot", "Space + Left/Right", new Color(1f, 0.86f, 0.18f));
        }
    }

    /// <summary>
    /// Updates Perfect/Good/Miss counters, combo, and max combo from a judgment result.
    /// </summary>
    /// <remarks>
    /// 中文：统一记录节奏判定结果。Perfect/Good 增加 combo，Miss 清空 combo 并增加 miss。
    /// </remarks>
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

    /// <summary>
    /// Judges whether the current time is on a jump beat and grades the beat fraction.
    /// </summary>
    /// <remarks>
    /// 中文：判断当前是不是 Space 跳跃拍；不是跳跃拍就直接 Miss，是的话再判断 Perfect/Good。
    /// </remarks>
    private RhythmTimingResult JudgeJumpBeat()
    {
        if (!IsCurrentJumpBeatSlot())
        {
            return RhythmTimingResult.Miss;
        }

        return JudgeCurrentBeatFraction();
    }

    /// <summary>
    /// Judges whether the current time is on an action beat between jumps and grades the beat fraction.
    /// </summary>
    /// <remarks>
    /// 中文：判断当前是不是跳跃之间的动作拍。香蕉和鹦鹉使用这个拍子。
    /// </remarks>
    private RhythmTimingResult JudgeBetweenJumpBeat()
    {
        if (IsCurrentJumpBeatSlot())
        {
            return RhythmTimingResult.Miss;
        }

        return JudgeCurrentBeatFraction();
    }

    /// <summary>
    /// Converts the current position within a beat into a Perfect or Good timing result.
    /// </summary>
    /// <remarks>
    /// 中文：把当前拍内的小数位置转换成 Perfect 或 Good。越靠近拍头越好。
    /// </remarks>
    private RhythmTimingResult JudgeCurrentBeatFraction()
    {
        float beatFraction = GetBeatPosition() - Mathf.Floor(GetBeatPosition());
        float perfectFraction = Mathf.Clamp(settings.perfectBeatFraction, 0.05f, 0.95f);
        float goodFraction = Mathf.Clamp(settings.goodBeatFraction, perfectFraction, 1f);
        if (beatFraction <= perfectFraction)
        {
            return RhythmTimingResult.Perfect;
        }

        if (beatFraction <= goodFraction)
        {
            return RhythmTimingResult.Good;
        }

        return RhythmTimingResult.Miss;
    }

    /// <summary>
    /// Returns whether the current beat slot is the configured jump slot.
    /// </summary>
    /// <remarks>
    /// 中文：按 beatsPerPlatform 判断当前拍是不是跳跃拍，通常是每两拍跳一次。
    /// </remarks>
    private bool IsCurrentJumpBeatSlot()
    {
        int currentBeat = Mathf.FloorToInt(GetBeatPosition());
        return PositiveModulo(currentBeat, Mathf.Max(1, settings.beatsPerPlatform)) == 0;
    }

    /// <summary>
    /// Returns whether the current beat slot is aligned with a specific route beat index.
    /// </summary>
    /// <remarks>
    /// 中文：判断当前拍是否和某个路线 beatIndex 对齐，用于平台/目标的时机检查。
    /// </remarks>
    private bool IsCurrentBeatSlotForBeatIndex(int beatIndex)
    {
        if (beatIndex < 0)
        {
            return false;
        }

        int currentBeat = Mathf.FloorToInt(GetBeatPosition());
        return PositiveModulo(currentBeat - beatIndex, Mathf.Max(1, settings.beatsPerPlatform)) == 0;
    }

    /// <summary>
    /// Calculates a non-negative modulo result for beat-slot comparisons.
    /// </summary>
    /// <remarks>
    /// 中文：安全取模，保证结果不会是负数，方便节拍循环判断。
    /// </remarks>
    private int PositiveModulo(int value, int modulo)
    {
        if (modulo <= 0)
        {
            return 0;
        }

        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    /// <summary>
    /// Returns whether a timing result counts as a successful hit.
    /// </summary>
    /// <remarks>
    /// 中文：Perfect 和 Good 都算成功，Miss 不算。
    /// </remarks>
    private bool IsTimingHit(RhythmTimingResult result)
    {
        if (settings.requirePerfectForSuccess)
        {
            return result == RhythmTimingResult.Perfect;
        }

        return result == RhythmTimingResult.Perfect || result == RhythmTimingResult.Good;
    }

    /// <summary>
    /// Adds score using the base action amount plus a combo bonus.
    /// </summary>
    /// <remarks>
    /// 中文：加分入口。基础分加上 combo 奖励，并触发 UI 分数反馈。
    /// </remarks>
    private void AddScore(int baseAmount)
    {
        score += Mathf.Max(0, baseAmount) + Mathf.Max(0, combo - 1) * Mathf.Max(0, settings.comboBonusStep);
        if (ui != null)
        {
            ui.ShowScoreFeedback();
        }
    }

    /// <summary>
    /// Marks a pickup as missed and routes the miss through the shared damage/feedback path.
    /// </summary>
    /// <remarks>
    /// 中文：香蕉没抓到时调用。先标记 pickup 已 missed，再显示失败反馈。
    /// </remarks>
    private void MissPickup(VerticalRunnerPickup pickup, string label, string hint, bool countMiss)
    {
        if (pickup != null)
        {
            pickup.missed = true;
        }
        TakeDamage(label, hint, countMiss, false);
    }

    /// <summary>
    /// Shows jump feedback when a jump input was accepted as Perfect or Good.
    /// </summary>
    /// <remarks>
    /// 中文：跳跃成功时显示 UI 反馈；Miss 不显示成功反馈。
    /// </remarks>
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

    /// <summary>
    /// Notifies the manager that the player landed on a platform, advancing tutorial or ending game when appropriate.
    /// </summary>
    /// <remarks>
    /// 中文：玩家落到平台后调用。教程模式推进教程目标；正式模式到达终点则完成游戏。
    /// </remarks>
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

    /// <summary>
    /// Collects a banana, adds score, updates UI feedback, and advances tutorial collection steps.
    /// </summary>
    /// <remarks>
    /// 中文：抓到香蕉时调用。增加香蕉数量和分数，显示反馈，并推进相关教程步骤。
    /// </remarks>
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

    /// <summary>
    /// Shows a short UI hint for banana collection input.
    /// </summary>
    /// <remarks>
    /// 中文：显示抓香蕉提示，告诉玩家使用 Down/S。
    /// </remarks>
    public void ShowCoinCollectHint()
    {
        if (ui != null)
        {
            ui.ShowFeedback("Banana", "Down/S", new Color(1f, 0.86f, 0.18f));
        }
    }

    /// <summary>
    /// Handles a failed action by resetting combo, optionally counting a miss, showing UI feedback, and optionally recovering the player.
    /// </summary>
    /// <remarks>
    /// 中文：统一失败处理。它不会直接结束游戏，而是清 combo、按需记 miss、显示提示，
    /// 并按情况把玩家恢复到安全平台。
    /// </remarks>
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

    /// <summary>
    /// Completes the current run, either by showing post-tutorial game rules or ending the game run.
    /// </summary>
    /// <remarks>
    /// 中文：当前路线完成时调用。教程完成后会进入正式游戏说明；正式模式完成后显示结果。
    /// </remarks>
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

    /// <summary>
    /// Closes the tutorial briefing and starts the tutorial countdown.
    /// </summary>
    /// <remarks>
    /// 中文：玩家看完教程说明后调用，隐藏说明面板、锁输入、开始第一步教程倒计时。
    /// </remarks>
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

    /// <summary>
    /// Closes the game rules panel and starts either the game run or its countdown.
    /// </summary>
    /// <remarks>
    /// 中文：玩家看完正式游戏规则后调用。教程结束后会切到正式模式；
    /// 已经在正式模式时会直接进入倒计时。
    /// </remarks>
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

    /// <summary>
    /// Restarts the current tutorial or game mode from a clean run state.
    /// </summary>
    /// <remarks>
    /// 中文：重开当前模式。正式模式重开正式路线，教程模式重开教程路线。
    /// </remarks>
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

    /// <summary>
    /// Initializes tutorial mode, optionally rebuilding the route and showing the tutorial briefing.
    /// </summary>
    /// <remarks>
    /// 中文：进入教程模式。它会重置分数/miss/combo，按需重建世界，
    /// 然后显示教程说明或直接开始倒计时。
    /// </remarks>
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

    /// <summary>
    /// Initializes game mode, optionally rebuilding the route and showing the rules panel.
    /// </summary>
    /// <remarks>
    /// 中文：进入正式模式。它会重置运行状态，按需重建路线，
    /// 然后显示规则或直接开始倒计时。
    /// </remarks>
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

    /// <summary>
    /// Clears all per-run counters and modal/waiting flags.
    /// </summary>
    /// <remarks>
    /// 中文：清空本局状态，包括分数、香蕉、combo、miss、教程进度和等待弹窗/倒计时状态。
    /// </remarks>
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

    /// <summary>
    /// Rebuilds camera, rhythm configuration, generated route, player, and UI for the current mode.
    /// </summary>
    /// <remarks>
    /// 中文：根据当前是教程还是正式模式，重新配置相机/节拍/路线/玩家/UI。
    /// </remarks>
    private void RebuildWorldForCurrentMode()
    {
        EnsureCamera();
        ConfigureRhythmManager();
        BuildWorld();
    }

    /// <summary>
    /// Builds the generated VerticalRunner world by coordinating spawner, player, camera, UI, and background setup.
    /// </summary>
    /// <remarks>
    /// 中文：生成游戏世界的主函数。平台、香蕉、鹦鹉等由 VerticalBeatSpawner 生成；
    /// Manager 负责组织 spawner、player、camera、UI 这些模块一起工作。
    /// </remarks>
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

    /// <summary>
    /// Removes the current generated player object before rebuilding the run.
    /// </summary>
    /// <remarks>
    /// 中文：重建路线前清掉旧玩家，避免场景里留下重复玩家对象。
    /// </remarks>
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

    /// <summary>
    /// Finds the scene-owned template container used for player and obstacle visuals.
    /// </summary>
    /// <remarks>
    /// 中文：查找 Hierarchy 里的模板容器。美术图片和模板外观由场景控制。
    /// </remarks>
    private VerticalRunnerTemplates FindSceneTemplates()
    {
        VerticalRunnerTemplates[] found = FindObjectsOfType<VerticalRunnerTemplates>(true);
        return found.Length > 0 ? found[0] : null;
    }

    /// <summary>
    /// Instantiates the player from a scene template when available.
    /// </summary>
    /// <remarks>
    /// 中文：创建玩家对象。复制你在 Hierarchy 里放好的 playerTemplate；
    /// </remarks>
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

    /// <summary>
    /// Configures the shared RhythmManager so VerticalRunner owns BPM, offsets, fallback timing, and music startup.
    /// </summary>
    /// <remarks>
    /// 中文：配置全局节拍管理器。这里统一 BPM、第一拍偏移、音乐源和 fallback 计时，
    /// 保证判定、BeatLane 和 icon 提示使用同一套节拍基础。
    /// </remarks>
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

    /// <summary>
    /// Plays the listening/countdown beats, starts the rhythm clock early, then unlocks player input.
    /// </summary>
    /// <remarks>
    /// 中文：开始前倒计时。音乐先开始，玩家先听几拍；倒计时结束后解锁输入。
    /// </remarks>
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
            ui.SetGameControlsVisible(ShouldShowGameControls());
        }
        if (player != null && !runEnded && !waitingForBriefing && !waitingForGameRules)
        {
            player.SetInputLocked(false);
        }
    }

    /// <summary>
    /// Starts the shared rhythm clock and optional music source with an offset that aligns gameplay after countdown.
    /// </summary>
    /// <remarks>
    /// 中文：启动节拍时钟和音乐，并根据倒计时长度调整 firstBeatOffset，
    /// 让真正可操作的第一拍和路线起点对齐。
    /// </remarks>
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

  
    /// 中文：隐藏/停用旧版本跑酷残留对象，避免旧 UI、旧玩家或旧管理器和当前系统冲突。

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

    
    /// 中文：按旧对象名字查找并隐藏，例如旧 player、旧 background、旧 barrier。

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

    /// 中文：按关键词隐藏旧 UI，但保留当前 VerticalRunnerCanvas，避免误关正在使用的新 UI。
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

    /// 中文：提前停用旧脚本，防止旧玩法逻辑在当前 VerticalRunner 里继续运行。
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

    /// <summary>
    /// Selects a tutorial step, resets its progress, and tells the UI to display the step text/images.
    /// </summary>
    /// <remarks>
    /// 中文：进入某个教程小步骤，并刷新左侧目标/提示/进度。
    /// </remarks>
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

    /// <summary>
    /// Advances tutorial progress when a landing satisfies the current step requirements.
    /// </summary>
    /// <remarks>
    /// 中文：玩家落地后检查是否完成当前教程目标，例如普通跳、长跳或安全躲鹦鹉。
    /// </remarks>
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

    /// <summary>
    /// Advances tutorial progress when a banana collection satisfies the current step requirements.
    /// </summary>
    /// <remarks>
    /// 中文：抓到香蕉后推进香蕉相关教程目标。
    /// </remarks>
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

    /// <summary>
    /// Refreshes the current tutorial objective progress shown in the UI.
    /// </summary>
    /// <remarks>
    /// 中文：刷新教程目标文字和进度数字，不改变教程步骤本身。
    /// </remarks>
    private void UpdateTutorialObjective()
    {
        if (ui == null || tutorialSteps == null || tutorialStepIndex < 0 || tutorialStepIndex >= tutorialSteps.Length)
        {
            return;
        }

        VerticalTutorialStep step = tutorialSteps[tutorialStepIndex];
        ui.UpdateTutorialObjective(step.objective, step.hint, tutorialStepProgress, step.requiredCount);
    }

    /// <summary>
    /// Completes the current tutorial step and either advances to the next step or completes the tutorial run.
    /// </summary>
    /// <remarks>
    /// 中文：当前教程目标完成后调用。还有下一步就进入下一步；最后一步完成则结束教程。
    /// </remarks>
    private void CompleteTutorialStep()
    {
        if (tutorialStepIndex >= tutorialSteps.Length - 1)
        {
            CompleteRun();
            return;
        }

        StartTutorialStep(tutorialStepIndex + 1);
    }

    /// <summary>
    /// Shows the transition from tutorial completion into the game rules panel.
    /// </summary>
    /// <remarks>
    /// 中文：教程结束后短暂停顿，然后显示正式游戏规则，并锁住玩家输入。
    /// </remarks>
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

    /// <summary>
    /// Temporarily locks input after a failure, then returns the player to a safe platform.
    /// </summary>
    /// <remarks>
    /// 中文：失败后的恢复流程。短暂锁输入，然后把玩家拉回安全平台。
    /// </remarks>
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

    /// <summary>
    /// Ends the current game run, locks input, applies finish score when completed, and shows the result panel.
    /// </summary>
    /// <remarks>
    /// 中文：结束本局，锁住输入，按需加通关分，并显示最终结果面板。
    /// </remarks>
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

        if (mode == VerticalRunnerMode.Game && score > 0)
        {
            LeaderboardManager.SaveScore(LeaderboardMode.Easy, score);
        }
    }

    /// <summary>
    /// Returns whether Back/Retry should be visible during active tutorial or game play.
    /// </summary>
    /// <remarks>
    /// 中文：教程和正式模式在倒计时结束、可操作后都显示 Back/Retry；说明页、规则页、结算页隐藏。
    /// </remarks>
    private bool ShouldShowGameControls()
    {
        return !runEnded
            && !waitingForBriefing
            && !waitingForGameRules
            && !waitingForCountdown;
    }

    /// <summary>
    /// Computes tutorial/game progress and pushes current counters into the hierarchy-bound UI controller.
    /// </summary>
    /// <remarks>
    /// 中文：刷新 HUD 数值，包括 miss、分数、香蕉、combo、最高 combo 和进度条。
    /// 教程进度按步骤完成比例算，正式游戏进度按歌曲时间算。
    /// </remarks>
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

    /// <summary>
    /// Resolves the four-column prompt state from visual beat time and current route context.
    /// </summary>
    /// <remarks>
    /// 中文：计算 Space/Down/Left/Right 哪一列应该显示 Down。
    /// 这个函数只根据节拍和当前路线目标决定提示，不读取玩家真实按键。
    /// </remarks>
    private ControlPromptState ResolveControlPromptState(float beatPosition)
    {
        ControlPromptState state = new ControlPromptState
        {
            show = false
        };

        if (runEnded || waitingForBriefing || waitingForGameRules)
        {
            return state;
        }

        if (!StartMenuAudioSettings.BeatPromptsEnabled)
        {
            return state;
        }

        state.show = true;
        state.spaceDown = IsPromptJumpBeat(beatPosition);
        state.downDown = HasPickupActionOnCurrentBeat(beatPosition);
        ApplyDirectionalPrompt(beatPosition, ref state);
        return state;
    }

    /// <summary>
    /// Lights the safe Left or Right prompt column when the current route platform requires a parrot branch action.
    /// </summary>
    /// <remarks>
    /// 中文：遇到鹦鹉分支时，根据安全方向点亮 Left 或 Right 那一列；另一列保持 Up。
    /// </remarks>
    private void ApplyDirectionalPrompt(float beatPosition, ref ControlPromptState state)
    {
        VerticalRunnerPlatform platform = player != null ? player.CurrentPlatform : null;
        if (platform == null || !platform.requiresDirectionalChoice || platform.actionBeatIndex < 0)
        {
            return;
        }
        if (!IsPromptActionBeat(beatPosition))
        {
            return;
        }

        if (platform.safeChoice == VerticalBranchChoice.Left)
        {
            state.leftDown = true;
        }
        else if (platform.safeChoice == VerticalBranchChoice.Right)
        {
            state.rightDown = true;
        }
    }

    /// <summary>
    /// Returns whether the current visual action beat should light the Down prompt for an active banana.
    /// </summary>
    /// <remarks>
    /// 中文：判断当前动作拍是否有香蕉需要抓，有就让 Down 那一列显示 Down。
    /// </remarks>
    private bool HasPickupActionOnCurrentBeat(float beatPosition)
    {
        if (!IsPromptActionBeat(beatPosition))
        {
            return false;
        }

        VerticalRunnerPickup pickup = GetActiveRoutePickupForPlayer();
        return pickup != null;
    }

    /// <summary>
    /// Finds the banana pickup attached to the player's current or next route platform.
    /// </summary>
    /// <remarks>
    /// 中文：查找玩家当前/目标平台上的香蕉，用来驱动 Down 图标提示。
    /// </remarks>
    private VerticalRunnerPickup GetActiveRoutePickupForPlayer()
    {
        if (spawner == null || player == null)
        {
            return null;
        }

        VerticalRunnerPlatform platform = player.TargetPlatform != null ? player.TargetPlatform : player.CurrentPlatform;
        return spawner.GetCollectibleCoinForPlatform(platform);
    }

    /// <summary>
    /// Returns whether a visual beat position belongs to the Space prompt beat slots.
    /// </summary>
    /// <remarks>
    /// 中文：四拍循环中第 1、3 拍点亮 Space 列。
    /// </remarks>
    private bool IsPromptJumpBeat(float beatPosition)
    {
        int currentBeat = Mathf.FloorToInt(beatPosition);
        int beatInBar = PositiveModulo(currentBeat, 4);
        return beatInBar == 0 || beatInBar == 2;
    }

    /// <summary>
    /// Returns whether a visual beat position belongs to action prompt slots for Down/Left/Right.
    /// </summary>
    /// <remarks>
    /// 中文：四拍循环中第 2、4 拍用于 Down、Left、Right 这些跳跃后的动作。
    /// </remarks>
    private bool IsPromptActionBeat(float beatPosition)
    {
        int currentBeat = Mathf.FloorToInt(beatPosition);
        int beatInBar = PositiveModulo(currentBeat, 4);
        return beatInBar == 1 || beatInBar == 3;
    }

    /// <summary>
    /// Converts shared rhythm time into beat position, falling back to level time when no RhythmManager exists.
    /// </summary>
    /// <remarks>
    /// 中文：获取当前是第几拍。优先使用 RhythmManager 的音乐时间；
    /// 如果没有 RhythmManager，就用关卡运行时间兜底。
    /// </remarks>
    private float GetBeatPosition()
    {
        RhythmManager rhythm = RhythmManager.Instance;
        if (rhythm != null)
        {
            return rhythm.GetAdjustedSongTime() / BeatInterval();
        }

        return (Time.timeSinceLevelLoad - settings.firstBeatOffset) / BeatInterval();
    }

    /// <summary>
    /// Returns the duration of one beat in seconds from the configured BPM.
    /// </summary>
    /// <remarks>
    /// 中文：根据 BPM 计算一拍有多少秒。
    /// </remarks>
    private float BeatInterval()
    {
        return 60f / Mathf.Max(1f, settings.bpm);
    }

    /// <summary>
    /// Defines the ordered tutorial lesson steps used by tutorial mode.
    /// </summary>
    /// <remarks>
    /// 中文：配置教程步骤列表，包括跳跃、香蕉、鹦鹉、长跳和最终综合练习。
    /// </remarks>
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

    /// <summary>
    /// Applies the fallback camera background color when the scene policy allows camera overrides.
    /// </summary>
    /// <remarks>
    /// 中文：设置相机背景色。真正的背景图片仍由场景里的 vertical 背景对象控制。
    /// </remarks>
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

    /// <summary>
    /// Ensures a main orthographic camera exists and applies the default VerticalRunner framing when allowed.
    /// </summary>
    /// <remarks>
    /// 中文：保证场景有 Main Camera，并在策略允许时设置正交相机位置和大小。
    /// </remarks>
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

    /// <summary>
    /// Creates a simple runtime circle sprite used as a fallback visual for generated objects.
    /// </summary>
    /// <remarks>
    /// 中文：生成一个圆形临时 sprite。只有缺少 Hierarchy 模板图片时才用于兜底显示。
    /// </remarks>
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

    /// <summary>
    /// Immutable data for one tutorial step: type, text, hint, and required completion count.
    /// </summary>
    /// <remarks>
    /// 中文：单个教程步骤的数据结构，记录教程类型、显示文字、提示和需要完成的次数。
    /// </remarks>
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
