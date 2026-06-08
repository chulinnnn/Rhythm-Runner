using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum OceanRhythmPhase
{
    FreePond
}

public enum OceanSoundMatchState
{
    Idle,
    ShellAvailable,
    Listening,
    Choosing,
    Correct,
    Retry
}

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

    private void Update()
    {
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

    public void ReturnToStart()
    {
        SetPaused(false);
        SceneTransitionManager.LoadScene("Start");
    }

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

    public void RestartOceanRhythm()
    {
        SetPaused(false);
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        EnterFreePond();
    }

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

    public void RestartFreePond()
    {
        SetPaused(false);
        EnterFreePond();
    }

    public void TogglePause()
    {
        SetPaused(!paused);
    }

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

    public Sprite GetWaterBackgroundSprite()
    {
        ApplyDefaultVisuals();
        return waterBackgroundSprite;
    }

    public Sprite GetNetSprite()
    {
        ApplyDefaultVisuals();
        return netSprite;
    }

    public Sprite GetBucketSprite()
    {
        ApplyDefaultVisuals();
        return bucketSprite;
    }

    public Sprite GetBucketSlotSprite()
    {
        ApplyDefaultVisuals();
        return bucketSlotSprite;
    }

    public Sprite GetTapButtonNormalSprite()
    {
        ApplyDefaultVisuals();
        return tapButtonNormalSprite;
    }

    public Sprite GetTapButtonBeatSprite()
    {
        ApplyDefaultVisuals();
        return tapButtonBeatSprite != null ? tapButtonBeatSprite : tapButtonNormalSprite;
    }

    public Sprite GetLockSprite()
    {
        ApplyDefaultVisuals();
        return lockSprite;
    }

    public Sprite GetShellSprite()
    {
        ApplyDefaultVisuals();
        return shellSprite;
    }

    public Sprite GetSingingShellSprite()
    {
        ApplyDefaultVisuals();
        return singingShellSprite != null ? singingShellSprite : shellSprite;
    }

    public Sprite GetDecorationSprite(OceanDecorationReward reward)
    {
        ApplyDefaultVisuals();
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

    public OceanBucketInventory GetBucketInventory()
    {
        return bucketInventory;
    }

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

    private void OnBeat(OceanLesson lesson)
    {
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

    private void JudgeSpaceInput()
    {
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

    private void EnterFreePond()
    {
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

    private void OnOceanIntroClosed()
    {
        if (phase != OceanRhythmPhase.FreePond || selectedPondAnimal != null || pondCompleted)
        {
            return;
        }

        PlayFreePondIdleBgm();
    }

    private void PlayFreePondIdleBgm()
    {
        if (phase == OceanRhythmPhase.FreePond && selectedPondAnimal == null && !pondCompleted)
        {
            PlayLoopingMusic(freePondIdleBgm);
        }
    }

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

    private bool ShouldShowFirstSelectionIntro(OceanPondAnimal animal)
    {
        if (animal == null || animal.IsMystery)
        {
            return false;
        }

        return introducedFishTypes.Add(animal.FishType);
    }

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

    private void StopFreePondPreviewRoutine()
    {
        if (freePondPreviewRoutine != null)
        {
            StopCoroutine(freePondPreviewRoutine);
            freePondPreviewRoutine = null;
        }
    }

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

    public void ReplaySoundMatchPattern()
    {
        if (soundMatchLesson == null)
        {
            return;
        }

        StartCoroutine(PlaySoundMatchPatternRoutine(soundMatchLesson));
    }

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

    private IEnumerator RespawnPondAnimalRoutine(OceanPondAnimal animal)
    {
        yield return new WaitForSeconds(1.5f);
        if (animal != null)
        {
            AssignRandomGameplayLesson(animal);
            animal.ResetCatch();
        }
    }

    private IEnumerator RemoveMysteryFishRoutine(OceanPondAnimal animal)
    {
        yield return new WaitForSeconds(1.5f);
        if (animal != null)
        {
            pondAnimals.Remove(animal);
            Destroy(animal.gameObject);
        }
    }

    private void ScheduleNextMysteryFish(float minDelay, float maxDelay)
    {
        nextMysterySpawnTime = Time.time + Random.Range(minDelay, maxDelay);
    }

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

    private OceanLesson GetActiveLesson()
    {
        if (phase == OceanRhythmPhase.FreePond)
        {
            return selectedPondAnimal != null ? selectedPondAnimal.Lesson : null;
        }

        return null;
    }

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

    private OceanLesson[] GetFreePondLessons()
    {
        if (freePondLessons == null || freePondLessons.Length == 0)
        {
            freePondLessons = BuildFreePondLessons();
        }

        return freePondLessons;
    }

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

    private OceanLesson CreateGameplayLessonForMeter(OceanTrackMeter meter)
    {
        OceanTrackDefinition track = PickRandomTrack(gameplayTracks, meter, false);
        if (track != null)
        {
            return CreateLessonFromTrack(track, false);
        }

        return CreateDefaultLessonForMeter(meter);
    }

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

    private int[] DefaultAccentBeatsForMeter(OceanTrackMeter meter)
    {
        if (meter == OceanTrackMeter.SixEight)
        {
            return new int[] { 0, 3 };
        }

        return new int[] { 0 };
    }

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

    private int PositiveModulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}

public enum OceanRhythmHitResult
{
    Perfect,
    Good,
    Near,
    Miss
}

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

    public string animalKey
    {
        get { return fishType.ToString(); }
    }

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

[System.Serializable]
public enum OceanTrackMeter
{
    FourFour,
    ThreeFour,
    TwoFour,
    SixEight
}

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
