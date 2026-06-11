using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// OceanRhythmManager is the scene-level gameplay owner for OceanRhythm.
// OceanRhythmManager 是 OceanRhythm 场景的玩法主控。
// 最核心的是update()函数，JudgeSpaceInput()负责节奏判定
// Reading map / 阅读入口:
// - Awake/Start: bind required helpers and enter Free Pond.
// - Update: main runtime loop for selection, beat ticks, and Space input.
// - JudgeSpaceInput: converts Space timing into hit quality.
// - EnterFreePond/UpdateFreePondSelection: fish selection, intro cards, and preview flow.
// - AwardCatch/SoundMatch methods: bucket rewards and Singing Shell mini-game.
//
// Visual ownership / 视觉归属:
// Inspector fields provide fallback sprites and music data, but the scene hierarchy
// owns card layouts, bucket album layout, button styling, and editable UI art.
// Inspector 字段提供兜底图和音乐数据；卡片、桶相册、按钮样式和 UI 美术由场景层级控制。

/// <summary>
/// Describes the high-level OceanRhythm gameplay phase.
/// 中文：表示当前 OceanRhythm 处于哪个玩法阶段。
/// </summary>
public enum OceanRhythmPhase
{
    FreePond
}

/// <summary>
/// Tracks the Singing Shell sound-matching mini-game state.
/// 中文：记录 Singing Shell 听音辨鱼小游戏当前进行到哪一步。
/// </summary>
public enum OceanSoundMatchState
{
    Idle,
    ShellAvailable,
    Listening,
    Choosing,
    Correct,
    Retry
}

/// <summary>
/// Scene-level gameplay controller for OceanRhythm. It owns music timing, fish selection,
/// beat judgment, catch rewards, Free Pond flow, and Singing Shell progression.
/// 中文：OceanRhythm 的核心玩法脚本，控制音乐节拍、选鱼、按键判定、抓鱼奖励、Free Pond 流程和 Singing Shell。
/// </summary>
[DefaultExecutionOrder(-140)]
public class OceanRhythmManager : MonoBehaviour
{
    private static bool registered;

    [Header("Sprites - single source for Ocean Rhythm visuals")]
    [Tooltip("All Ocean Rhythm texture overrides should be assigned here. Leave a field empty to use the project default asset path.")]
    public Sprite waterBackgroundSprite;
    public Sprite fishSprite;
    public Sprite octopusSprite;
    public Sprite turtleSprite;
    public Sprite jellyfishSprite;
    public Sprite netSprite;
    public Sprite bucketSprite;
    public Sprite bucketSlotSprite;
    public Sprite tapButtonNormalSprite;
    public Sprite tapButtonBeatSprite;
    public Sprite lockSprite;
    public Sprite shellSprite;
    public Sprite coralSprite;
    public Sprite seaweedSprite;
    public Sprite starSprite;
    public Sprite flagSprite;
    public Sprite pearlSprite;
    public Sprite mysteryFishSprite;
    public Sprite singingShellSprite;
    public Sprite bellCharmSprite;
    public Sprite glowStarSprite;
    public Sprite waveRibbonSprite;
    public Sprite[] fishVariantSprites;
    public Sprite[] bucketDecorationSprites;

    [Header("Gameplay track library")]
    [Tooltip("Real gameplay tracks for the Free Pond. Assign clip + BPM + meter here.")]
    public OceanTrackDefinition[] gameplayTracks;

    [Header("Mystery track library (optional)")]
    [Tooltip("Optional separate pool for Mystery Fish. If empty, mystery picks from gameplay tracks marked for mystery.")]
    public OceanTrackDefinition[] mysteryTracks;

    [Header("Timing")]
    public float perfectWindow = 0.08f;
    public float goodWindow = 0.18f;
    public float nearWindow = 0.3f;

    [Header("Free Pond gameplay")]
    public int freePondRequiredHits = 6;
    public int freePondPreviewBars = 2;
    public float freePondPreviewMinSeconds = 2f;
    public float freePondPreviewMaxSeconds = 5f;

    [Header("Ocean intro music")]
    public AudioClip introCardBgm;
    public AudioClip freePondIdleBgm;

    [Header("Runtime scene policy")]
    public RuntimeScenePolicy scenePolicy = RuntimeScenePolicy.Defaults();

    private OceanRhythmUIController uiController;
    private SimpleMetronomeAudio metronomeAudio;
    private AudioSource musicSource;
    private OceanLesson[] freePondLessons;
    private OceanRhythmPhase phase;
    private int beatIndex;
    private float beatInterval;
    private float nextBeatTime;
    private bool acceptingInput;
    private bool pondCompleted;
    private bool paused;
    private bool freePondInputEnabled;
    private OceanPondAnimal selectedPondAnimal;
    private OceanPondAnimal currentMysteryAnimal;
    private readonly List<OceanPondAnimal> pondAnimals = new List<OceanPondAnimal>();
    private OceanBucketInventory bucketInventory;
    private float nextMysterySpawnTime;
    private int mysteryCounter;
    private OceanSoundMatchState soundMatchState;
    private OceanLesson soundMatchLesson;
    private int catchesSinceLastSingingShell;
    private bool singingShellAvailable;
    private bool firstSingingShellShown;
    private Coroutine freePondPreviewRoutine;
    private readonly HashSet<OceanFishType> introducedFishTypes = new HashSet<OceanFishType>();
    private OceanBucketAlbumAssets bucketAlbumAssets;

    /// <summary>
    /// Registers a scene-loaded hook so OceanRhythm can create its manager if the scene is missing one.
    /// 中文：注册场景加载回调；如果 OceanRhythm 场景缺少 manager，就自动补一个。
    /// </summary>
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
    /// Runs after Unity loads a scene and checks whether OceanRhythm needs a manager.
    /// 中文：场景加载后检查当前是不是 OceanRhythm，并判断是否需要补 manager。
    /// </summary>
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    /// <summary>
    /// Ensures the active OceanRhythm scene has exactly one gameplay manager available.
    /// 中文：保证 OceanRhythm 场景里有玩法主控，没有就创建。
    /// </summary>
    private static void EnsureForScene(Scene scene)
    {
        if (scene.name != "OceanRhythm")
        {
            return;
        }

        if (FindObjectOfType<OceanRhythmManager>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("OceanRhythmManager");
        obj.AddComponent<OceanRhythmManager>();
    }

    /// <summary>
    /// Initializes required runtime helpers before gameplay starts.
    /// 中文：启动前准备 UI 控制器、节拍音效、音乐 AudioSource、桶存档等基础对象。
    /// </summary>
    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != "OceanRhythm")
        {
            return;
        }

        ApplyDefaultVisuals();
        EnsureCamera();
        uiController = GetComponent<OceanRhythmUIController>();
        if (uiController == null)
        {
            uiController = gameObject.AddComponent<OceanRhythmUIController>();
        }

        metronomeAudio = GetComponent<SimpleMetronomeAudio>();
        if (metronomeAudio == null)
        {
            metronomeAudio = gameObject.AddComponent<SimpleMetronomeAudio>();
        }

        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        bucketInventory = new OceanBucketInventory();
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Edit Mode Hierarchy")]
    /// <summary>
    /// Rebuilds the editable OceanRhythm hierarchy from the Editor context menu.
    /// 中文：在编辑器里手动补齐 OceanRhythm 的场景层级和基础 UI，不是正式 gameplay 流程。
    /// </summary>
    public void RebuildEditModeHierarchy()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EnsureCamera();
        ApplyDefaultVisuals();
        uiController = GetComponent<OceanRhythmUIController>();
        if (uiController == null)
        {
            uiController = gameObject.AddComponent<OceanRhythmUIController>();
        }

        metronomeAudio = GetComponent<SimpleMetronomeAudio>();
        if (metronomeAudio == null)
        {
            metronomeAudio = gameObject.AddComponent<SimpleMetronomeAudio>();
        }

        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        bucketInventory = new OceanBucketInventory();
        pondAnimals.Clear();
        RuntimeScenePolicy editModePolicy = RuntimeScenePolicy.Defaults();
        editModePolicy.rebuildUiOnPlay = false;
        uiController.Build(this, editModePolicy);
        uiController.ShowFreePond(GetFreePondLessons(), pondAnimals, 2);
        uiController.UpdateBucket(bucketInventory);
        uiController.SetSingingShellAvailable(false);

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    /// <summary>
    /// Binds the scene UI and enters the Free Pond gameplay flow.
    /// 中文：绑定场景 UI，然后进入 Free Pond 主流程。
    /// </summary>
    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "OceanRhythm")
        {
            return;
        }

        uiController.Build(this, scenePolicy);
        if (!uiController.IsReady)
        {
            Debug.LogWarning("OceanRhythmManager: OceanRhythmUIController is not ready. Disabling manager.");
            enabled = false;
            return;
        }

        EnterFreePond();
    }

    /// <summary>
    /// Main runtime loop for OceanRhythm input, fish selection, and beat ticks.
    /// 中文：每帧检查返回键、暂停、弹窗阻挡、选鱼、节拍推进和 Space 输入。
    /// </summary>
    private void Update()
    {
        // Core loop: keep gameplay input and beat feedback aligned with the active lesson.
        // 核心循环：让当前 lesson 的输入、节拍反馈和鱼选择保持同步。
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToStart();
        }

        if (paused)
        {
            return;
        }

        if (uiController != null && uiController.IsBeatCardOpen())
        {
            return;
        }

        if (!acceptingInput)
        {
            return;
        }

        UpdateFreePondSelection();

        OceanLesson currentLesson = GetActiveLesson();
        if (currentLesson != null && beatInterval > 0f)
        {
            while (Time.time >= nextBeatTime)
            {
                OnBeat(currentLesson);
                nextBeatTime += beatInterval;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryTapInput();
        }

    }

    /// <summary>
    /// Entry point for tap/Space input from keyboard or UI.
    /// 中文：处理玩家点击 TAP 或按 Space；如果还在试听阶段就只提示预览，否则进入节奏判定。
    /// </summary>
    public void TryTapInput()
    {
        if (paused)
        {
            return;
        }

        if (uiController != null && uiController.IsBeatCardOpen())
        {
            return;
        }

        if (phase == OceanRhythmPhase.FreePond && !freePondInputEnabled)
        {
            if (uiController != null && selectedPondAnimal != null)
            {
                uiController.ShowFreePondPreview(selectedPondAnimal.Lesson);
            }
            return;
        }

        JudgeSpaceInput();
    }

    /// <summary>
    /// Leaves OceanRhythm and loads the Start scene.
    /// 中文：退出 OceanRhythm，回到 Start 主菜单。
    /// </summary>
    public void ReturnToStart()
    {
        SetPaused(false);
        SceneTransitionManager.LoadScene("Start");
    }

    /// <summary>
    /// Restarts the current OceanRhythm flow.
    /// 中文：重新开始当前流程；目前会回到 Free Pond。
    /// </summary>
    public void RestartCurrentLesson()
    {
        SetPaused(false);
        if (phase == OceanRhythmPhase.FreePond)
        {
            EnterFreePond();
            return;
        }

        EnterFreePond();
    }

    /// <summary>
    /// Fully resets OceanRhythm music and returns to Free Pond.
    /// 中文：停止当前音乐并重新进入 Free Pond，相当于重开 OceanRhythm。
    /// </summary>
    public void RestartOceanRhythm()
    {
        SetPaused(false);
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        EnterFreePond();
    }

    /// <summary>
    /// Shows the beat information card for the currently selected pond animal.
    /// 中文：打开当前选中鱼对应的节拍说明卡；没选鱼时提示先选鱼。
    /// </summary>
    public void ShowCurrentBeatInfo()
    {
        if (uiController == null || phase != OceanRhythmPhase.FreePond)
        {
            return;
        }

        OceanLesson lesson = selectedPondAnimal != null ? selectedPondAnimal.Lesson : null;
        if (lesson == null)
        {
            uiController.ShowNoFishSelected();
            return;
        }

        uiController.ShowBeatCardInfo(lesson);
    }

    /// <summary>
    /// Restarts the Free Pond flow directly.
    /// 中文：直接重置并重新进入 Free Pond。
    /// </summary>
    public void RestartFreePond()
    {
        SetPaused(false);
        EnterFreePond();
    }

    /// <summary>
    /// Toggles the paused state for gameplay and music.
    /// 中文：切换暂停/继续，同时控制音乐和 UI 暂停显示。
    /// </summary>
    public void TogglePause()
    {
        SetPaused(!paused);
    }

    /// <summary>
    /// Applies paused state to time scale, music playback, and UI.
    /// 中文：真正执行暂停逻辑：停时间、暂停音乐、刷新 UI 状态。
    /// </summary>
    private void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = paused ? 0f : 1f;
        if (musicSource != null)
        {
            if (paused && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
            else if (!paused && musicSource.clip != null)
            {
                musicSource.UnPause();
            }
        }
        if (uiController != null)
        {
            uiController.SetPauseState(paused);
        }
    }

    /// <summary>
    /// Returns the display sprite that matches a lesson's fish type.
    /// 中文：根据 lesson 的鱼类型返回对应显示图片。
    /// </summary>
    public Sprite GetSpriteForLesson(OceanLesson lesson)
    {
        ApplyDefaultVisuals();
        if (lesson == null)
        {
            return null;
        }

        if (lesson.animalKey == "Fish")
        {
            return fishSprite;
        }
        if (lesson.animalKey == "Octopus")
        {
            return octopusSprite;
        }
        if (lesson.animalKey == "Turtle")
        {
            return turtleSprite;
        }
        if (lesson.fishType == OceanFishType.Mystery)
        {
            return mysteryFishSprite;
        }
        return jellyfishSprite;
    }

    /// <summary>
    /// Returns the configured water background sprite.
    /// 中文：返回海洋背景图；如果 Inspector 没挂，会先尝试使用默认图。
    /// </summary>
    public Sprite GetWaterBackgroundSprite()
    {
        ApplyDefaultVisuals();
        return waterBackgroundSprite;
    }

    /// <summary>
    /// Returns the configured net sprite.
    /// 中文：返回捕捉网图片。
    /// </summary>
    public Sprite GetNetSprite()
    {
        ApplyDefaultVisuals();
        return netSprite;
    }

    /// <summary>
    /// Returns the configured bucket sprite.
    /// 中文：返回 bucket 主图片。
    /// </summary>
    public Sprite GetBucketSprite()
    {
        ApplyDefaultVisuals();
        return bucketSprite;
    }

    /// <summary>
    /// Returns the configured bucket slot sprite.
    /// 中文：返回 bucket 装饰槽位图片。
    /// </summary>
    public Sprite GetBucketSlotSprite()
    {
        ApplyDefaultVisuals();
        return bucketSlotSprite;
    }

    /// <summary>
    /// Returns the normal-state tap button sprite.
    /// 中文：返回 TAP 按钮普通状态图片。
    /// </summary>
    public Sprite GetTapButtonNormalSprite()
    {
        ApplyDefaultVisuals();
        return tapButtonNormalSprite;
    }

    /// <summary>
    /// Returns the beat-state tap button sprite, falling back to the normal sprite.
    /// 中文：返回 TAP 按钮节拍高亮图片；没配置时使用普通图片。
    /// </summary>
    public Sprite GetTapButtonBeatSprite()
    {
        ApplyDefaultVisuals();
        return tapButtonBeatSprite != null ? tapButtonBeatSprite : tapButtonNormalSprite;
    }

    /// <summary>
    /// Returns the lock sprite used by locked decoration UI.
    /// 中文：返回装饰未解锁时使用的锁图标。
    /// </summary>
    public Sprite GetLockSprite()
    {
        ApplyDefaultVisuals();
        return lockSprite;
    }

    /// <summary>
    /// Returns the shell currency/reward sprite.
    /// 中文：返回贝壳奖励图片。
    /// </summary>
    public Sprite GetShellSprite()
    {
        ApplyDefaultVisuals();
        return shellSprite;
    }

    /// <summary>
    /// Returns the Singing Shell sprite, falling back to the normal shell sprite.
    /// 中文：返回 Singing Shell 图片；没配置时用普通贝壳图。
    /// </summary>
    public Sprite GetSingingShellSprite()
    {
        ApplyDefaultVisuals();
        return singingShellSprite != null ? singingShellSprite : shellSprite;
    }

    /// <summary>
    /// Resolves the sprite for a bucket decoration reward.
    /// 中文：为 bucket 装饰查找图片；优先读 Hierarchy 配置，其次读 manager 字段，最后用默认图兜底。
    /// </summary>
    public Sprite GetDecorationSprite(OceanDecorationReward reward)
    {
        ApplyDefaultVisuals();
        OceanBucketAlbumAssets albumAssets = GetBucketAlbumAssets();
        if (albumAssets != null)
        {
            Sprite configuredSprite = albumAssets.GetDecorationSprite(reward);
            if (configuredSprite != null)
            {
                return configuredSprite;
            }
        }

        if (bucketDecorationSprites != null && (int)reward >= 0 && (int)reward < bucketDecorationSprites.Length && bucketDecorationSprites[(int)reward] != null)
        {
            return bucketDecorationSprites[(int)reward];
        }

        if (reward == OceanDecorationReward.Shell)
        {
            return shellSprite;
        }
        if (reward == OceanDecorationReward.Star)
        {
            return starSprite;
        }
        if (reward == OceanDecorationReward.Seaweed)
        {
            return seaweedSprite;
        }
        if (reward == OceanDecorationReward.Flag)
        {
            return flagSprite != null ? flagSprite : coralSprite;
        }
        if (reward == OceanDecorationReward.Pearl)
        {
            return pearlSprite != null ? pearlSprite : shellSprite;
        }
        if (reward == OceanDecorationReward.BellCharm)
        {
            return bellCharmSprite != null ? bellCharmSprite : shellSprite;
        }
        if (reward == OceanDecorationReward.GlowStar)
        {
            return glowStarSprite != null ? glowStarSprite : starSprite;
        }
        if (reward == OceanDecorationReward.WaveRibbon)
        {
            return waveRibbonSprite != null ? waveRibbonSprite : coralSprite;
        }

        return shellSprite;
    }

    /// <summary>
    /// Finds and caches the hierarchy-owned Bucket Album asset configuration component.
    /// 中文：查找并缓存 BucketAlbumAssets 配置对象，用来读取装饰图片。
    /// </summary>
    public OceanBucketAlbumAssets GetBucketAlbumAssets()
    {
        if (bucketAlbumAssets != null)
        {
            return bucketAlbumAssets;
        }

        Transform configTransform = null;
        GameObject configRoot = GameObject.Find("OceanRhythmConfig");
        if (configRoot != null)
        {
            configTransform = configRoot.transform.Find("BucketAlbumAssets");
        }

        if (configTransform != null)
        {
            bucketAlbumAssets = configTransform.GetComponent<OceanBucketAlbumAssets>();
        }

        if (bucketAlbumAssets == null)
        {
            bucketAlbumAssets = FindObjectOfType<OceanBucketAlbumAssets>(true);
        }

        return bucketAlbumAssets;
    }

    /// <summary>
    /// Loads editor-only fallback sprites for any unassigned Inspector fields.
    /// 中文：如果 Inspector 没挂图，就在编辑器里按默认路径补备用图片。
    /// </summary>
    private void ApplyDefaultVisuals()
    {
        fishSprite = ResolveSprite(fishSprite, "Assets/fishes/little_fish.png");
        octopusSprite = ResolveSprite(octopusSprite, "Assets/fishes/octopus.png");
        turtleSprite = ResolveSprite(turtleSprite, "Assets/fishes/turtle.png");
        jellyfishSprite = ResolveSprite(jellyfishSprite, "Assets/fishes/jellyfish.png");
        mysteryFishSprite = ResolveSprite(mysteryFishSprite, "Assets/fishes/mystery_shell_transparent.png");
        netSprite = ResolveSprite(netSprite, "Assets/fishes/fishing_net_brown_childlike.png");
        bucketSprite = ResolveSprite(bucketSprite, "Assets/fishes/bucket_transparent.png");
        bucketSlotSprite = ResolveSprite(bucketSlotSprite, "Assets/fishes/bucket_slot_sprite_bright.png");
        tapButtonNormalSprite = ResolveSprite(tapButtonNormalSprite, "Assets/Editor/button/PNG/Blue/Default/button_rectangle_depth_flat.png");
        tapButtonBeatSprite = ResolveSprite(tapButtonBeatSprite, "Assets/Editor/button/PNG/Yellow/Default/button_rectangle_depth_flat.png");
        shellSprite = ResolveSprite(shellSprite, "Assets/fishes/mystery_shell_transparent.png");
        singingShellSprite = ResolveSprite(singingShellSprite, "Assets/fishes/mystery_shell_transparent.png");

        seaweedSprite = ResolveSprite(seaweedSprite, "Assets/fishes/Vector/seaweed_green_a.svg");
        starSprite = ResolveSprite(starSprite, "Assets/fishes/PNG/Default/hud_plus.png");
        coralSprite = ResolveSprite(coralSprite, "Assets/fishes/PNG/Default/seaweed_pink_a.png");
        pearlSprite = ResolveSprite(pearlSprite, "Assets/fishes/mystery_shell_transparent.png");
        bellCharmSprite = ResolveSprite(bellCharmSprite, "Assets/fishes/Vector/hud_dot.svg");
        glowStarSprite = ResolveSprite(glowStarSprite, "Assets/fishes/PNG/Default/hud_plus.png");
        waveRibbonSprite = ResolveSprite(waveRibbonSprite, "Assets/fishes/Vector/bubble_c.svg");

    }

    /// <summary>
    /// Returns an already assigned sprite or loads a fallback sprite in the Unity Editor.
    /// 中文：已有图片就直接用；没有时仅在编辑器里按路径加载备用图。
    /// </summary>
    private Sprite ResolveSprite(Sprite current, string assetPath)
    {
        if (current != null)
        {
            return current;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
        return null;
#endif
    }

    /// <summary>
    /// Exposes the current bucket inventory data to UI components.
    /// 中文：把当前 bucket 收集/装饰数据提供给 UI 使用。
    /// </summary>
    public OceanBucketInventory GetBucketInventory()
    {
        return bucketInventory;
    }

    /// <summary>
    /// Applies a lesson's music clip to the shared OceanRhythm music source.
    /// 中文：根据当前 lesson 设置并播放音乐；没有音乐时停止播放。
    /// </summary>
    private void ConfigureMusic(OceanLesson lesson)
    {
        AudioClip clip = lesson != null ? lesson.musicClip : null;
        if (clip != null)
        {
            musicSource.loop = true;
            musicSource.clip = clip;
            musicSource.Play();
        }
        else if (musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }
        else
        {
            musicSource.clip = null;
        }
    }

    /// <summary>
    /// Plays a looping background or lesson clip through the shared music source.
    /// 中文：循环播放指定背景音乐或 lesson 音乐。
    /// </summary>
    private void PlayLoopingMusic(AudioClip clip)
    {
        if (musicSource == null)
        {
            return;
        }

        if (clip == null)
        {
            StopMusic();
            return;
        }

        musicSource.loop = true;
        if (musicSource.clip != clip)
        {
            musicSource.clip = clip;
        }
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    /// <summary>
    /// Stops and clears the shared music source.
    /// 中文：停止当前音乐并清空 AudioSource 的 clip。
    /// </summary>
    private void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        musicSource.clip = null;
    }

    /// <summary>
    /// Advances the beat counter and sends beat pulse feedback to audio/UI.
    /// 中文：推进当前节拍，并通知节拍音效和 UI 做高亮反馈。
    /// </summary>
    private void OnBeat(OceanLesson lesson)
    {
        // Beat tick: advances the visual/audio pulse only; scoring happens in JudgeSpaceInput.
        // 节拍推进：这里只触发视觉/音频脉冲，真正判定在 JudgeSpaceInput。
        beatIndex++;
        int beatInBar = PositiveModulo(beatIndex, lesson.beatsPerBar);
        bool accented = lesson.IsAccentBeat(beatInBar);

        if (metronomeAudio != null && musicSource.clip == null)
        {
            metronomeAudio.PlayBeat(accented);
        }

        if (uiController != null)
        {
            uiController.PulseBeat(beatInBar, accented);
        }
    }

    /// <summary>
    /// Judges the current Space/tap timing against the nearest beat window.
    /// 中文：核心节奏判定函数；比较玩家输入和最近节拍的时间差，得到 Perfect/Good/Near/Miss。
    /// </summary>
    private void JudgeSpaceInput()
    {
        // Main timing judgment for OceanRhythm: compare Space against the nearest beat window.
        // OceanRhythm 的核心输入判定：把 Space 与最近节拍窗口比较。
        OceanLesson lesson = GetActiveLesson();
        if (lesson == null)
        {
            if (uiController != null)
            {
                uiController.ShowNoFishSelected();
            }
            return;
        }

        float previousBeatTime = nextBeatTime - beatInterval;
        float previousDiff = Mathf.Abs(Time.time - previousBeatTime);
        float nextDiff = Mathf.Abs(Time.time - nextBeatTime);
        float diff = Mathf.Min(previousDiff, nextDiff);

        OceanRhythmHitResult result;
        if (diff <= perfectWindow)
        {
            result = OceanRhythmHitResult.Perfect;
        }
        else if (diff <= goodWindow)
        {
            result = OceanRhythmHitResult.Good;
        }
        else if (diff <= nearWindow)
        {
            result = OceanRhythmHitResult.Near;
        }
        else
        {
            result = OceanRhythmHitResult.Miss;
        }

        JudgeFreePondInput(result, diff, lesson);
    }

    /// <summary>
    /// Enters and initializes the Free Pond gameplay flow.
    /// 中文：进入 Free Pond，生成鱼的 lesson、刷新 UI、播放介绍音乐并显示 intro 卡片。
    /// </summary>
    private void EnterFreePond()
    {
        // Free Pond is the current OceanRhythm play mode: build fish lessons, show intro, then wait for selection.
        // Free Pond 是当前海洋玩法主流程：生成鱼节奏、展示介绍卡，然后等待玩家选鱼。
        phase = OceanRhythmPhase.FreePond;
        freePondInputEnabled = false;
        selectedPondAnimal = null;
        beatIndex = -1;
        beatInterval = 0f;
        pondAnimals.Clear();
        introducedFishTypes.Clear();
        freePondLessons = BuildFreePondLessons();
        acceptingInput = true;
        pondCompleted = false;

        StopMusic();

        if (uiController != null)
        {
            uiController.ShowFreePond(GetFreePondLessons(), pondAnimals, 2);
            uiController.UpdateBucket(bucketInventory);
            uiController.SetSingingShellAvailable(false);
            PlayLoopingMusic(introCardBgm);
            uiController.ShowOceanIntroCard(OnOceanIntroClosed);
        }

        singingShellAvailable = false;
        soundMatchState = OceanSoundMatchState.Idle;
        catchesSinceLastSingingShell = 0;
        firstSingingShellShown = false;
        ScheduleNextMysteryFish(8f, 18f);
    }

    /// <summary>
    /// Handles the intro card close event before any fish is selected.
    /// 中文：intro 卡片关闭后，如果还没选鱼，就切到 Free Pond 待机音乐。
    /// </summary>
    private void OnOceanIntroClosed()
    {
        if (phase != OceanRhythmPhase.FreePond || selectedPondAnimal != null || pondCompleted)
        {
            return;
        }

        PlayFreePondIdleBgm();
    }

    /// <summary>
    /// Plays the Free Pond idle music when no fish is selected.
    /// 中文：没有选鱼、池塘未完成时播放 Free Pond 待机音乐。
    /// </summary>
    private void PlayFreePondIdleBgm()
    {
        if (phase == OceanRhythmPhase.FreePond && selectedPondAnimal == null && !pondCompleted)
        {
            PlayLoopingMusic(freePondIdleBgm);
        }
    }

    /// <summary>
    /// Polls UI selection state and transitions fish selection, preview, and intro-card flow.
    /// 中文：检查玩家当前选中的鱼；选中新鱼时先显示节拍卡或试听，再允许输入。
    /// </summary>
    private void UpdateFreePondSelection()
    {
        if (phase != OceanRhythmPhase.FreePond || uiController == null)
        {
            return;
        }

        if (pondCompleted)
        {
            return;
        }

        if (currentMysteryAnimal == null && Time.time >= nextMysterySpawnTime)
        {
            SpawnMysteryFish();
        }

        OceanPondAnimal newSelection = uiController.UpdateFreePondSelection();
        if (newSelection == selectedPondAnimal)
        {
            return;
        }

        selectedPondAnimal = newSelection;
        if (selectedPondAnimal != null)
        {
            freePondInputEnabled = false;
            StopFreePondPreviewRoutine();
            if (ShouldShowFirstSelectionIntro(selectedPondAnimal))
            {
                StopMusic();
                OceanPondAnimal introducedAnimal = selectedPondAnimal;
                uiController.ShowBeatCardInfo(introducedAnimal.Lesson, delegate
                {
                    if (phase == OceanRhythmPhase.FreePond && selectedPondAnimal == introducedAnimal && !pondCompleted)
                    {
                        StartFreePondPreview(introducedAnimal);
                    }
                });
            }
            else
            {
                StartFreePondPreview(selectedPondAnimal);
            }
        }
        else
        {
            freePondInputEnabled = false;
            StopFreePondPreviewRoutine();
            PlayFreePondIdleBgm();
            uiController.ShowNoFishSelected();
        }
    }

    /// <summary>
    /// Decides whether this fish type should show its first-selection beat card.
    /// 中文：判断这种鱼是不是第一次被选中；第一次会弹出对应节拍说明卡。
    /// </summary>
    private bool ShouldShowFirstSelectionIntro(OceanPondAnimal animal)
    {
        if (animal == null || animal.IsMystery)
        {
            return false;
        }

        return introducedFishTypes.Add(animal.FishType);
    }

    /// <summary>
    /// Starts the listen-first preview for the selected pond animal.
    /// 中文：开始选中鱼的试听阶段，重置节拍钟、播放音乐，并暂时锁住输入。
    /// </summary>
    private void StartFreePondPreview(OceanPondAnimal animal)
    {
        if (animal == null || animal.Lesson == null)
        {
            return;
        }

        ResetBeatClock(animal.Lesson, 0f);
        ConfigureMusic(animal.Lesson);
        freePondInputEnabled = false;
        StopFreePondPreviewRoutine();
        freePondPreviewRoutine = StartCoroutine(FreePondPreviewRoutine(animal));
    }

    /// <summary>
    /// Stops the active Free Pond preview coroutine if one is running.
    /// 中文：停止当前试听流程，避免切换鱼时旧试听继续运行。
    /// </summary>
    private void StopFreePondPreviewRoutine()
    {
        if (freePondPreviewRoutine != null)
        {
            StopCoroutine(freePondPreviewRoutine);
            freePondPreviewRoutine = null;
        }
    }

    /// <summary>
    /// Applies a timing result to the selected fish's catch progress.
    /// 中文：把节奏判定结果应用到当前鱼；Perfect/Good 增加捕捉进度，Miss/Near 给提示。
    /// </summary>
    private void JudgeFreePondInput(OceanRhythmHitResult result, float timingError, OceanLesson lesson)
    {
        if (selectedPondAnimal == null || !freePondInputEnabled)
        {
            if (uiController != null)
            {
                if (selectedPondAnimal != null)
                {
                    uiController.ShowFreePondPreview(selectedPondAnimal.Lesson);
                }
                else
                {
                    uiController.ShowNoFishSelected();
                }
            }
            return;
        }

        bool progressed = result == OceanRhythmHitResult.Perfect || result == OceanRhythmHitResult.Good;
        if (progressed)
        {
            selectedPondAnimal.AddCaptureProgress(result);
        }
        else
        {
            selectedPondAnimal.ShowRhythmHint(result);
        }

        if (uiController != null)
        {
            uiController.ShowFreePondInputResult(selectedPondAnimal, result, timingError);
        }

        if (selectedPondAnimal.IsCaptured)
        {
            OceanPondAnimal capturedAnimal = selectedPondAnimal;
            AwardCatch(capturedAnimal);
            Vector2 bucketTarget = uiController != null ? uiController.GetBucketDropPositionInPond() : capturedAnimal.AnchoredPosition + new Vector2(0f, 86f);
            selectedPondAnimal.PlayRescue(bucketTarget);
            if (uiController != null)
            {
                uiController.MarkFreePondFishCollected(capturedAnimal.Lesson);
                uiController.ShowCatchReward(capturedAnimal, bucketInventory);
            }

            selectedPondAnimal = null;
            freePondInputEnabled = false;
            PlayFreePondIdleBgm();
            if (capturedAnimal.IsMystery)
            {
                currentMysteryAnimal = null;
                StartCoroutine(RemoveMysteryFishRoutine(capturedAnimal));
                ScheduleNextMysteryFish(45f, 75f);
            }
            else
            {
                StartCoroutine(RespawnPondAnimalRoutine(capturedAnimal));
            }

            OnFreePondCatchCompleted(capturedAnimal);
        }
    }

    /// <summary>
    /// Updates Singing Shell availability after a normal fish catch.
    /// 中文：普通鱼被抓到后累计次数，并决定是否显示 Singing Shell。
    /// </summary>
    private void OnFreePondCatchCompleted(OceanPondAnimal capturedAnimal)
    {
        if (capturedAnimal == null || capturedAnimal.IsMystery)
        {
            return;
        }

        catchesSinceLastSingingShell++;
        int threshold = firstSingingShellShown ? 3 : 1;
        if (!singingShellAvailable && catchesSinceLastSingingShell >= threshold)
        {
            ShowSingingShell();
        }
    }

    /// <summary>
    /// Makes the Singing Shell mini-game available in the UI.
    /// 中文：让 Singing Shell 按钮变为可用，提示玩家可以进入听音小游戏。
    /// </summary>
    private void ShowSingingShell()
    {
        if (uiController == null)
        {
            return;
        }

        singingShellAvailable = true;
        firstSingingShellShown = true;
        soundMatchState = OceanSoundMatchState.ShellAvailable;
        uiController.SetSingingShellAvailable(true);
    }

    /// <summary>
    /// Waits through the selected animal's listen-first preview before enabling input.
    /// 中文：控制试听时长；试听结束且鱼仍被选中时，才允许玩家开始按节拍捕捉。
    /// </summary>
    private IEnumerator FreePondPreviewRoutine(OceanPondAnimal animal)
    {
        if (animal == null || animal.Lesson == null)
        {
            freePondPreviewRoutine = null;
            yield break;
        }

        OceanLesson lesson = animal.Lesson;
        uiController.ShowFreePondPreview(lesson);

        float previewDuration = Mathf.Max(
            freePondPreviewMinSeconds,
            (60f / Mathf.Max(30f, lesson.bpm)) * Mathf.Max(1, lesson.beatsPerBar) * Mathf.Max(1, freePondPreviewBars));
        previewDuration = Mathf.Min(previewDuration, Mathf.Max(freePondPreviewMinSeconds, freePondPreviewMaxSeconds));

        float elapsed = 0f;
        while (elapsed < previewDuration)
        {
            if (animal != selectedPondAnimal || phase != OceanRhythmPhase.FreePond)
            {
                freePondPreviewRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (animal == selectedPondAnimal && uiController != null)
        {
            freePondInputEnabled = true;
            uiController.ShowFreePondSelection(animal);
        }

        freePondPreviewRoutine = null;
    }

    /// <summary>
    /// Starts the Singing Shell sound-matching mini-game from the current pond lesson pool.
    /// 中文：启动 Singing Shell，从当前池塘鱼里抽一个目标节奏让玩家听并选择。
    /// </summary>
    public void StartSingingShellGame()
    {
        OceanLesson[] pool = GetFreePondLessons();
        if (phase != OceanRhythmPhase.FreePond || pool == null || pool.Length == 0)
        {
            return;
        }

        soundMatchLesson = CreateGameplayLessonFromBase(pool[Random.Range(0, pool.Length)]);
        soundMatchState = OceanSoundMatchState.Listening;
        StopMusic();
        if (uiController != null)
        {
            uiController.ShowSoundMatch(soundMatchLesson, pool, false);
        }
        StartCoroutine(PlaySoundMatchPatternRoutine(soundMatchLesson));
    }

    /// <summary>
    /// Replays the current Singing Shell beat pattern.
    /// 中文：重新播放当前 Singing Shell 的节奏提示。
    /// </summary>
    public void ReplaySoundMatchPattern()
    {
        if (soundMatchLesson == null)
        {
            return;
        }

        StartCoroutine(PlaySoundMatchPatternRoutine(soundMatchLesson));
    }

    /// <summary>
    /// Resolves the player's Singing Shell fish choice and awards music pearls on success.
    /// 中文：处理 Singing Shell 选择结果；选对给 music pearl 并刷新奖励，选错则重播。
    /// </summary>
    public void ChooseSoundMatch(OceanFishType fishType)
    {
        if (soundMatchLesson == null || uiController == null)
        {
            return;
        }

        bool correct = fishType == soundMatchLesson.fishType;
        if (correct)
        {
            soundMatchState = OceanSoundMatchState.Correct;
            singingShellAvailable = false;
            catchesSinceLastSingingShell = 0;
            bucketInventory.AddMusicPearls(1);
            UnlockMusicPearlDecorations();
            uiController.UpdateBucket(bucketInventory);
            uiController.SetSingingShellAvailable(false);
            uiController.ShowSoundMatchResult(true, soundMatchLesson, bucketInventory);
            HighlightSoundMatchFish(soundMatchLesson.fishType);
        }
        else
        {
            soundMatchState = OceanSoundMatchState.Retry;
            uiController.ShowSoundMatchResult(false, soundMatchLesson, bucketInventory);
            StartCoroutine(PlaySoundMatchPatternRoutine(soundMatchLesson));
        }
    }

    /// <summary>
    /// Plays one bar of the target lesson rhythm for the Singing Shell mini-game.
    /// 中文：按目标 lesson 的拍号播放一小节节奏，然后切到选择阶段。
    /// </summary>
    private IEnumerator PlaySoundMatchPatternRoutine(OceanLesson lesson)
    {
        if (lesson == null)
        {
            yield break;
        }

        soundMatchState = OceanSoundMatchState.Listening;
        float interval = 60f / Mathf.Max(30f, lesson.bpm);
        for (int i = 0; i < lesson.beatsPerBar; i++)
        {
            bool accented = lesson.IsAccentBeat(i);
            if (metronomeAudio != null)
            {
                metronomeAudio.PlayBeat(accented);
            }
            if (uiController != null)
            {
                uiController.PulseSoundMatchBeat(i, accented);
            }
            yield return new WaitForSeconds(interval);
        }

        soundMatchState = OceanSoundMatchState.Choosing;
        if (uiController != null)
        {
            uiController.SetSoundMatchChoosing();
        }
    }

    /// <summary>
    /// Highlights one matching pond animal after a correct Singing Shell answer.
    /// 中文：Singing Shell 答对后，在池塘里高亮对应鱼。
    /// </summary>
    private void HighlightSoundMatchFish(OceanFishType fishType)
    {
        for (int i = 0; i < pondAnimals.Count; i++)
        {
            OceanPondAnimal animal = pondAnimals[i];
            if (animal != null && !animal.IsCaptured && animal.FishType == fishType)
            {
                animal.HighlightFromSoundMatch();
                break;
            }
        }
    }

    /// <summary>
    /// Unlocks bucket decorations based on accumulated music pearls.
    /// 中文：根据 music pearl 数量解锁高级 bucket 装饰。
    /// </summary>
    private void UnlockMusicPearlDecorations()
    {
        if (bucketInventory.MusicPearls >= 3)
        {
            bucketInventory.UnlockDecoration(OceanDecorationReward.BellCharm);
        }
        if (bucketInventory.MusicPearls >= 5)
        {
            bucketInventory.UnlockDecoration(OceanDecorationReward.GlowStar);
        }
        if (bucketInventory.MusicPearls >= 8)
        {
            bucketInventory.UnlockDecoration(OceanDecorationReward.WaveRibbon);
        }
    }

    /// <summary>
    /// Grants shell rewards and decoration unlocks for a captured pond animal.
    /// 中文：抓到鱼后发贝壳、记录捕捉次数，并在满足条件时解锁装饰。
    /// </summary>
    private void AwardCatch(OceanPondAnimal animal)
    {
        if (animal == null || bucketInventory == null)
        {
            return;
        }

        int shellReward = animal.IsMystery ? 5 : 1;
        bucketInventory.AddCatch(animal.FishType, shellReward);

        int count = bucketInventory.GetCatchCount(animal.FishType);
        if (animal.IsMystery)
        {
            bucketInventory.UnlockDecoration(OceanDecorationReward.Pearl);
        }
        else if (count >= 3)
        {
            bucketInventory.UnlockDecoration(DecorationForFish(animal.FishType));
        }
    }

    /// <summary>
    /// Maps a fish type to the decoration unlocked by catching that fish repeatedly.
    /// 中文：把鱼类型转换成对应可解锁的 bucket 装饰。
    /// </summary>
    private OceanDecorationReward DecorationForFish(OceanFishType fishType)
    {
        if (fishType == OceanFishType.Fish)
        {
            return OceanDecorationReward.Shell;
        }
        if (fishType == OceanFishType.Octopus)
        {
            return OceanDecorationReward.Star;
        }
        if (fishType == OceanFishType.Turtle)
        {
            return OceanDecorationReward.Flag;
        }
        if (fishType == OceanFishType.Jellyfish)
        {
            return OceanDecorationReward.Pearl;
        }

        return OceanDecorationReward.Seaweed;
    }

    /// <summary>
    /// Waits briefly, assigns a new lesson, and returns a normal pond animal to play.
    /// 中文：普通鱼被抓后等待一会儿，换一首同类型曲目并重新出现。
    /// </summary>
    private IEnumerator RespawnPondAnimalRoutine(OceanPondAnimal animal)
    {
        yield return new WaitForSeconds(1.5f);
        if (animal != null)
        {
            AssignRandomGameplayLesson(animal);
            animal.ResetCatch();
        }
    }

    /// <summary>
    /// Removes a captured mystery fish from the pond after its rescue animation.
    /// 中文：神秘鱼被抓后等待动画结束，然后从池塘列表和场景中移除。
    /// </summary>
    private IEnumerator RemoveMysteryFishRoutine(OceanPondAnimal animal)
    {
        yield return new WaitForSeconds(1.5f);
        if (animal != null)
        {
            pondAnimals.Remove(animal);
            Destroy(animal.gameObject);
        }
    }

    /// <summary>
    /// Schedules the next mystery fish spawn time within a delay range.
    /// 中文：设置下一条神秘鱼出现的随机时间。
    /// </summary>
    private void ScheduleNextMysteryFish(float minDelay, float maxDelay)
    {
        nextMysterySpawnTime = Time.time + Random.Range(minDelay, maxDelay);
    }

    /// <summary>
    /// Creates a mystery fish in the pond if a valid mystery lesson is available.
    /// 中文：如果有可用神秘曲目，就在池塘里生成神秘鱼。
    /// </summary>
    private void SpawnMysteryFish()
    {
        if (uiController == null)
        {
            ScheduleNextMysteryFish(45f, 75f);
            return;
        }

        OceanLesson mysteryLesson = CreateMysteryLesson();
        if (mysteryLesson == null)
        {
            ScheduleNextMysteryFish(45f, 75f);
            return;
        }

        currentMysteryAnimal = uiController.SpawnMysteryFish(mysteryLesson, mysteryFishSprite, "Mystery_" + mysteryCounter);
        mysteryCounter++;
        if (currentMysteryAnimal != null)
        {
            pondAnimals.Add(currentMysteryAnimal);
        }
        else
        {
            ScheduleNextMysteryFish(45f, 75f);
        }
    }

    /// <summary>
    /// Checks whether every pond animal currently in the list has been captured.
    /// 中文：检查池塘列表里的鱼是否都已经被抓到。
    /// </summary>
    private bool AreAllPondAnimalsCaptured()
    {
        if (pondAnimals.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < pondAnimals.Count; i++)
        {
            if (pondAnimals[i] != null && !pondAnimals[i].IsCaptured)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Ends the Free Pond flow and shows the completion UI.
    /// 中文：结束 Free Pond，停止输入和音乐，并显示完成界面。
    /// </summary>
    private void CompleteFreePond()
    {
        if (pondCompleted)
        {
            return;
        }

        pondCompleted = true;
        acceptingInput = false;
        freePondInputEnabled = false;
        selectedPondAnimal = null;
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }

        if (uiController != null)
        {
            uiController.ShowFreePondComplete();
        }
    }

    /// <summary>
    /// Returns the lesson currently being judged for gameplay input.
    /// 中文：返回当前用于节奏判定的 lesson；Free Pond 中就是选中鱼的 lesson。
    /// </summary>
    private OceanLesson GetActiveLesson()
    {
        if (phase == OceanRhythmPhase.FreePond)
        {
            return selectedPondAnimal != null ? selectedPondAnimal.Lesson : null;
        }

        return null;
    }

    /// <summary>
    /// Resets beat timing for a lesson and schedules the next beat.
    /// 中文：根据 lesson 的 BPM 重置节拍钟，并设置下一拍开始时间。
    /// </summary>
    private void ResetBeatClock(OceanLesson lesson, float delay)
    {
        if (lesson == null)
        {
            beatInterval = 0f;
            return;
        }

        beatIndex = -1;
        beatInterval = 60f / Mathf.Max(30f, lesson.bpm);
        nextBeatTime = Time.time + Mathf.Max(0f, delay);
    }

    /// <summary>
    /// Returns the cached Free Pond lessons, rebuilding them if needed.
    /// 中文：获取当前 Free Pond 的鱼类 lesson；没有缓存时重新生成。
    /// </summary>
    private OceanLesson[] GetFreePondLessons()
    {
        if (freePondLessons == null || freePondLessons.Length == 0)
        {
            freePondLessons = BuildFreePondLessons();
        }

        return freePondLessons;
    }

    /// <summary>
    /// Builds one Free Pond lesson for each supported meter.
    /// 中文：为 4/4、3/4、2/4、6/8 各生成一个池塘 lesson。
    /// </summary>
    private OceanLesson[] BuildFreePondLessons()
    {
        List<OceanLesson> result = new List<OceanLesson>();
        OceanTrackMeter[] meters = new OceanTrackMeter[]
        {
            OceanTrackMeter.FourFour,
            OceanTrackMeter.ThreeFour,
            OceanTrackMeter.TwoFour,
            OceanTrackMeter.SixEight
        };

        for (int i = 0; i < meters.Length; i++)
        {
            OceanLesson lesson = CreateGameplayLessonForMeter(meters[i]);
            if (lesson != null)
            {
                result.Add(lesson);
            }
        }

        if (result.Count == 0)
        {
            for (int i = 0; i < meters.Length; i++)
            {
                OceanLesson fallback = CreateDefaultLessonForMeter(meters[i]);
                if (fallback != null)
                {
                    result.Add(fallback);
                }
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Creates a gameplay lesson from a matching track, or falls back to defaults.
    /// 中文：按拍号找一首 gameplay 曲目生成 lesson；找不到就用默认节奏数据。
    /// </summary>
    private OceanLesson CreateGameplayLessonForMeter(OceanTrackMeter meter)
    {
        OceanTrackDefinition track = PickRandomTrack(gameplayTracks, meter, false);
        if (track != null)
        {
            return CreateLessonFromTrack(track, false);
        }

        return CreateDefaultLessonForMeter(meter);
    }

    /// <summary>
    /// Creates a fallback lesson for a meter when no configured track is available.
    /// 中文：没有配置音乐时，为指定拍号生成默认 lesson。
    /// </summary>
    private OceanLesson CreateDefaultLessonForMeter(OceanTrackMeter meter)
    {
        OceanFishType fishType = FishTypeForMeter(meter);
        return new OceanLesson(
            fishType,
            AnimalNameForFishType(fishType),
            MeterLabelForMeter(meter),
            "Stay near the fish. Tap Space when the bubble lights up.",
            BeatsPerBarForMeter(meter),
            DefaultBpmForMeter(meter),
            Mathf.Max(1, freePondRequiredHits),
            DefaultAccentBeatsForMeter(meter));
    }

    /// <summary>
    /// Creates a fresh gameplay lesson copy from an existing base lesson.
    /// 中文：复制已有 lesson，重新套用当前 Free Pond 的命中目标数。
    /// </summary>
    private OceanLesson CreateGameplayLessonFromBase(OceanLesson baseLesson)
    {
        if (baseLesson == null)
        {
            return null;
        }

        return new OceanLesson(
            baseLesson.fishType,
            baseLesson.animalName,
            baseLesson.meterLabel,
            "Stay near the fish. Tap Space when the bubble lights up.",
            baseLesson.beatsPerBar,
            baseLesson.bpm,
            Mathf.Max(1, freePondRequiredHits),
            CloneAccentBeats(baseLesson.accentBeats),
            baseLesson.musicClip);
    }

    /// <summary>
    /// Creates a mystery fish lesson from the mystery pool or a fallback pond lesson.
    /// 中文：为神秘鱼生成 lesson；优先用 mystery 曲库，没有就借用池塘曲目。
    /// </summary>
    private OceanLesson CreateMysteryLesson()
    {
        OceanTrackDefinition track = PickRandomMysteryTrack();
        if (track != null)
        {
            return CreateLessonFromTrack(track, true);
        }

        OceanLesson[] pondPool = GetFreePondLessons();
        if (pondPool == null || pondPool.Length == 0)
        {
            return null;
        }

        OceanLesson baseLesson = pondPool[Random.Range(0, pondPool.Length)];
        return new OceanLesson(
            OceanFishType.Mystery,
            "Mystery Fish",
            baseLesson.meterLabel,
            "Listen first. This fish has a surprise beat.",
            baseLesson.beatsPerBar,
            baseLesson.bpm,
            Mathf.Max(1, freePondRequiredHits),
            CloneAccentBeats(baseLesson.accentBeats),
            baseLesson.musicClip);
    }

    /// <summary>
    /// Converts an Inspector track definition into a runtime OceanLesson.
    /// 中文：把 Inspector 里配置的曲目数据转换成游戏运行用的 lesson。
    /// </summary>
    private OceanLesson CreateLessonFromTrack(OceanTrackDefinition track, bool mystery)
    {
        if (track == null)
        {
            return null;
        }

        OceanFishType fishType = mystery ? OceanFishType.Mystery : FishTypeForMeter(track.meter);
        return new OceanLesson(
            fishType,
            mystery ? "Mystery Fish" : AnimalNameForFishType(fishType),
            MeterLabelForMeter(track.meter),
            mystery ? "Listen first. This fish has a surprise beat." : "Stay near the fish. Tap Space when the bubble lights up.",
            BeatsPerBarForMeter(track.meter),
            Mathf.Max(30f, track.bpm),
            Mathf.Max(1, freePondRequiredHits),
            ResolveAccentBeats(track),
            track.clip);
    }

    /// <summary>
    /// Picks a random mystery-eligible track using mystery tracks first, then gameplay tracks.
    /// 中文：随机选择神秘鱼曲目；优先 mystery 曲库，其次允许 mystery 的 gameplay 曲库。
    /// </summary>
    private OceanTrackDefinition PickRandomMysteryTrack()
    {
        OceanTrackDefinition track = PickRandomTrack(mysteryTracks, null, true);
        if (track != null)
        {
            return track;
        }

        track = PickRandomTrack(gameplayTracks, null, true);
        if (track != null)
        {
            return track;
        }

        return PickRandomTrack(gameplayTracks, null, false);
    }

    /// <summary>
    /// Selects a random configured track that matches optional meter and mystery filters.
    /// 中文：按拍号和 mystery 条件筛选曲目，再随机选一首。
    /// </summary>
    private OceanTrackDefinition PickRandomTrack(OceanTrackDefinition[] source, OceanTrackMeter? meter, bool mysteryOnly)
    {
        if (source == null || source.Length == 0)
        {
            return null;
        }

        List<OceanTrackDefinition> matches = new List<OceanTrackDefinition>();
        for (int i = 0; i < source.Length; i++)
        {
            OceanTrackDefinition track = source[i];
            if (track == null || track.clip == null)
            {
                continue;
            }
            if (meter.HasValue && track.meter != meter.Value)
            {
                continue;
            }
            if (mysteryOnly && !track.allowForMystery)
            {
                continue;
            }

            matches.Add(track);
        }

        if (matches.Count == 0)
        {
            return null;
        }

        return matches[Random.Range(0, matches.Count)];
    }

    /// <summary>
    /// Assigns a new random gameplay lesson to a respawned normal pond animal.
    /// 中文：普通鱼重新出现时，为它换一首同拍号的 gameplay 曲目。
    /// </summary>
    private void AssignRandomGameplayLesson(OceanPondAnimal animal)
    {
        if (animal == null || animal.IsMystery)
        {
            return;
        }

        OceanTrackMeter meter = MeterForFishType(animal.FishType);
        OceanLesson lesson = CreateGameplayLessonForMeter(meter);
        if (lesson != null)
        {
            animal.AssignLesson(lesson);
        }
    }

    /// <summary>
    /// Resolves accent beats from a track, falling back to meter defaults.
    /// 中文：读取曲目的重拍配置；没配置时按拍号使用默认重拍。
    /// </summary>
    private int[] ResolveAccentBeats(OceanTrackDefinition track)
    {
        if (track != null && track.accentBeats != null && track.accentBeats.Length > 0)
        {
            return CloneAccentBeats(track.accentBeats);
        }

        if (track != null && track.meter == OceanTrackMeter.SixEight)
        {
            return new int[] { 0, 3 };
        }

        return new int[] { 0 };
    }

    /// <summary>
    /// Copies accent beat data so runtime lessons do not share mutable arrays.
    /// 中文：复制重拍数组，避免多个 lesson 共用同一个可变数组。
    /// </summary>
    private int[] CloneAccentBeats(int[] source)
    {
        if (source == null)
        {
            return null;
        }

        int[] clone = new int[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            clone[i] = source[i];
        }

        return clone;
    }

    /// <summary>
    /// Maps a meter to the fish type that represents it in Free Pond.
    /// 中文：把拍号映射到对应鱼类型。
    /// </summary>
    private OceanFishType FishTypeForMeter(OceanTrackMeter meter)
    {
        if (meter == OceanTrackMeter.ThreeFour)
        {
            return OceanFishType.Octopus;
        }
        if (meter == OceanTrackMeter.TwoFour)
        {
            return OceanFishType.Turtle;
        }
        if (meter == OceanTrackMeter.SixEight)
        {
            return OceanFishType.Jellyfish;
        }

        return OceanFishType.Fish;
    }

    /// <summary>
    /// Maps a fish type back to its represented meter.
    /// 中文：把鱼类型反查成对应拍号。
    /// </summary>
    private OceanTrackMeter MeterForFishType(OceanFishType fishType)
    {
        if (fishType == OceanFishType.Octopus)
        {
            return OceanTrackMeter.ThreeFour;
        }
        if (fishType == OceanFishType.Turtle)
        {
            return OceanTrackMeter.TwoFour;
        }
        if (fishType == OceanFishType.Jellyfish)
        {
            return OceanTrackMeter.SixEight;
        }

        return OceanTrackMeter.FourFour;
    }

    /// <summary>
    /// Returns the display animal name for a fish type.
    /// 中文：返回鱼类型在 UI 里显示的名字。
    /// </summary>
    private string AnimalNameForFishType(OceanFishType fishType)
    {
        if (fishType == OceanFishType.Octopus)
        {
            return "Octopus";
        }
        if (fishType == OceanFishType.Turtle)
        {
            return "Sea Turtle";
        }
        if (fishType == OceanFishType.Jellyfish)
        {
            return "Jellyfish";
        }
        if (fishType == OceanFishType.Mystery)
        {
            return "Mystery Fish";
        }

        return "Little Fish";
    }

    /// <summary>
    /// Returns the number of beats per bar for a meter.
    /// 中文：返回拍号每小节有几拍。
    /// </summary>
    private int BeatsPerBarForMeter(OceanTrackMeter meter)
    {
        if (meter == OceanTrackMeter.ThreeFour)
        {
            return 3;
        }
        if (meter == OceanTrackMeter.TwoFour)
        {
            return 2;
        }
        if (meter == OceanTrackMeter.SixEight)
        {
            return 6;
        }

        return 4;
    }

    /// <summary>
    /// Returns the fallback BPM used when no track is configured for a meter.
    /// 中文：返回某个拍号没有配置音乐时使用的默认 BPM。
    /// </summary>
    private float DefaultBpmForMeter(OceanTrackMeter meter)
    {
        if (meter == OceanTrackMeter.ThreeFour)
        {
            return 72f;
        }
        if (meter == OceanTrackMeter.TwoFour)
        {
            return 82f;
        }
        if (meter == OceanTrackMeter.SixEight)
        {
            return 68f;
        }

        return 76f;
    }

    /// <summary>
    /// Returns the fallback accent beats for a meter.
    /// 中文：返回某个拍号默认哪些拍是重拍。
    /// </summary>
    private int[] DefaultAccentBeatsForMeter(OceanTrackMeter meter)
    {
        if (meter == OceanTrackMeter.SixEight)
        {
            return new int[] { 0, 3 };
        }

        return new int[] { 0 };
    }

    /// <summary>
    /// Returns the display label for a meter.
    /// 中文：返回拍号在 UI 中显示的文字，比如 4/4 或 6/8。
    /// </summary>
    private string MeterLabelForMeter(OceanTrackMeter meter)
    {
        if (meter == OceanTrackMeter.ThreeFour)
        {
            return "3/4";
        }
        if (meter == OceanTrackMeter.TwoFour)
        {
            return "2/4";
        }
        if (meter == OceanTrackMeter.SixEight)
        {
            return "6/8";
        }

        return "4/4";
    }

    /// <summary>
    /// Ensures a usable camera exists for OceanRhythm and applies fallback camera settings when allowed.
    /// 中文：确保场景有可用相机；在允许时补相机并设置海洋场景默认视角。
    /// </summary>
    private void EnsureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            if (!scenePolicy.autoCreateMissingObjects)
            {
                Debug.LogWarning("OceanRhythmManager: Main Camera is missing and auto creation is disabled.");
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
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.backgroundColor = new Color(0.04f, 0.28f, 0.46f);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    /// <summary>
    /// Calculates a modulo value that is always non-negative.
    /// 中文：计算不会出现负数的取模结果，用来稳定计算第几拍。
    /// </summary>
    private int PositiveModulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}

/// <summary>
/// Represents the quality of one OceanRhythm timing input.
/// 中文：表示一次按键节奏判定的结果。
/// </summary>
public enum OceanRhythmHitResult
{
    Perfect,
    Good,
    Near,
    Miss
}

/// <summary>
/// Runtime lesson data for one fish or track: meter, BPM, hit target, accents, and music.
/// 中文：一条鱼/一首曲目的运行时 lesson 数据，包含拍号、BPM、目标次数、重拍和音乐。
/// </summary>
[System.Serializable]
public class OceanLesson
{
    public OceanFishType fishType;
    public string animalName;
    public string meterLabel;
    public string instruction;
    public int beatsPerBar;
    public float bpm;
    public int requiredHits;
    public int[] accentBeats;
    public AudioClip musicClip;

    /// <summary>
    /// Returns a simple string key derived from the fish type.
    /// 中文：返回鱼类型字符串，用于 UI 选择对应图片。
    /// </summary>
    public string animalKey
    {
        get { return fishType.ToString(); }
    }

    /// <summary>
    /// Creates a lesson instance used by Free Pond and Singing Shell.
    /// 中文：创建一个可用于 Free Pond 或 Singing Shell 的节奏 lesson。
    /// </summary>
    public OceanLesson(OceanFishType fishType, string animalName, string meterLabel, string instruction, int beatsPerBar, float bpm, int requiredHits, int[] accentBeats, AudioClip musicClip = null)
    {
        this.fishType = fishType;
        this.animalName = animalName;
        this.meterLabel = meterLabel;
        this.instruction = instruction;
        this.beatsPerBar = beatsPerBar;
        this.bpm = bpm;
        this.requiredHits = requiredHits;
        this.accentBeats = accentBeats;
        this.musicClip = musicClip;
    }

    /// <summary>
    /// Checks whether a beat index inside the bar is accented.
    /// 中文：判断小节内某一拍是不是重拍。
    /// </summary>
    public bool IsAccentBeat(int beatInBar)
    {
        if (accentBeats == null)
        {
            return beatInBar == 0;
        }

        for (int i = 0; i < accentBeats.Length; i++)
        {
            if (accentBeats[i] == beatInBar)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Meter choices supported by OceanRhythm track definitions.
/// 中文：OceanRhythm 曲目配置支持的拍号类型。
/// </summary>
[System.Serializable]
public enum OceanTrackMeter
{
    FourFour,
    ThreeFour,
    TwoFour,
    SixEight
}

/// <summary>
/// Inspector-defined track entry used to build runtime OceanLesson data.
/// 中文：Inspector 里配置的一首曲目，用来生成运行时 lesson。
/// </summary>
[System.Serializable]
public class OceanTrackDefinition
{
    public string displayName;
    public AudioClip clip;
    public OceanTrackMeter meter = OceanTrackMeter.FourFour;
    public float bpm = 76f;
    public bool allowForMystery = true;
    public int[] accentBeats;
}
