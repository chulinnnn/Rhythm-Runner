using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum OceanRhythmPhase
{
    GuidedLessons,
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

    [Header("0-5 Flow")]
    public bool startInFreePond = true;

    [Header("Sprites")]
    public Sprite waterBackgroundSprite;
    public Sprite fishSprite;
    public Sprite octopusSprite;
    public Sprite turtleSprite;
    public Sprite jellyfishSprite;
    public Sprite netSprite;
    public Sprite bucketSprite;
    public Sprite bucketSlotSprite;
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
    public Sprite[] littleFishAnimationSprites;
    public Sprite[] bucketDecorationSprites;

    [Header("Optional music")]
    public AudioClip fourFourClip;
    public AudioClip threeFourClip;
    public AudioClip twoFourClip;
    public AudioClip sixEightClip;

    [Header("Timing")]
    public float perfectWindow = 0.08f;
    public float goodWindow = 0.18f;
    public float nearWindow = 0.3f;
    public float startDelay = 0.75f;

    private OceanRhythmUIController uiController;
    private SimpleMetronomeAudio metronomeAudio;
    private AudioSource musicSource;
    private OceanLesson[] lessons;
    private OceanRhythmPhase phase;
    private int lessonIndex;
    private int progress;
    private int beatIndex;
    private float beatInterval;
    private float nextBeatTime;
    private bool acceptingInput;
    private bool changingLesson;
    private bool pondCompleted;
    private bool paused;
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
    private Sprite[] cachedLittleFishAnimationFrames;

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

        EnsureCamera();
        BuildLessons();
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
        BuildLessons();
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
        uiController.Build(this);
        uiController.ShowFreePond(lessons, pondAnimals, 2);
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

        uiController.Build(this);
        if (startInFreePond)
        {
            EnterFreePond();
        }
        else
        {
            StartLesson(0);
        }
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

        StartLesson(lessonIndex);
    }

    public void RestartOceanRhythm()
    {
        SetPaused(false);
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        if (startInFreePond)
        {
            EnterFreePond();
        }
        else
        {
            StartLesson(0);
        }
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
        if (lesson == null)
        {
            return null;
        }

        if (lesson.animalKey == "Fish")
        {
            Sprite firstAnimationFrame = GetFirstSprite(GetAnimationFramesForLesson(lesson));
            if (firstAnimationFrame != null)
            {
                return firstAnimationFrame;
            }

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

    public Sprite[] GetAnimationFramesForLesson(OceanLesson lesson)
    {
        if (lesson != null && lesson.fishType == OceanFishType.Fish)
        {
            if (cachedLittleFishAnimationFrames == null)
            {
                cachedLittleFishAnimationFrames = BuildSpriteSheetFrames(littleFishAnimationSprites);
            }

            return cachedLittleFishAnimationFrames;
        }

        return null;
    }

    private Sprite[] BuildSpriteSheetFrames(Sprite[] sheets)
    {
        if (sheets == null)
        {
            return null;
        }

        List<Sprite> frames = new List<Sprite>();
        for (int i = 0; i < sheets.Length; i++)
        {
            Sprite source = sheets[i];
            if (source == null || source.texture == null)
            {
                continue;
            }

            Rect sourceRect = source.rect;
            int columns;
            int rows;
            GetSpriteSheetGrid(sourceRect, out columns, out rows);
            float frameWidth = sourceRect.width / columns;
            float frameHeight = sourceRect.height / rows;
            for (int row = rows - 1; row >= 0; row--)
            {
                for (int column = 0; column < columns; column++)
                {
                    Rect frameRect = new Rect(sourceRect.x + column * frameWidth, sourceRect.y + row * frameHeight, frameWidth, frameHeight);
                    frames.Add(Sprite.Create(source.texture, frameRect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit));
                }
            }
        }

        return frames.Count > 0 ? frames.ToArray() : null;
    }

    private void GetSpriteSheetGrid(Rect sourceRect, out int columns, out int rows)
    {
        if (Mathf.Approximately(sourceRect.width, sourceRect.height) && sourceRect.width >= 128f)
        {
            columns = 2;
            rows = 2;
            return;
        }

        float frameSize = Mathf.Min(sourceRect.width, sourceRect.height);
        columns = Mathf.Max(1, Mathf.RoundToInt(sourceRect.width / frameSize));
        rows = Mathf.Max(1, Mathf.RoundToInt(sourceRect.height / frameSize));
    }

    private Sprite GetFirstSprite(Sprite[] sprites)
    {
        if (sprites == null)
        {
            return null;
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                return sprites[i];
            }
        }

        return null;
    }

    public Sprite GetWaterBackgroundSprite()
    {
        return waterBackgroundSprite;
    }

    public Sprite GetNetSprite()
    {
        return netSprite;
    }

    public Sprite GetBucketSprite()
    {
        return bucketSprite;
    }

    public Sprite GetBucketSlotSprite()
    {
        return bucketSlotSprite;
    }

    public Sprite GetLockSprite()
    {
        return lockSprite;
    }

    public Sprite GetShellSprite()
    {
        return shellSprite;
    }

    public Sprite GetSingingShellSprite()
    {
        return singingShellSprite != null ? singingShellSprite : shellSprite;
    }

    public Sprite GetDecorationSprite(OceanDecorationReward reward)
    {
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

    public OceanBucketInventory GetBucketInventory()
    {
        return bucketInventory;
    }

    private void StartLesson(int index)
    {
        if (lessons == null || lessons.Length == 0)
        {
            return;
        }

        phase = OceanRhythmPhase.GuidedLessons;
        selectedPondAnimal = null;
        lessonIndex = Mathf.Clamp(index, 0, lessons.Length - 1);
        OceanLesson lesson = lessons[lessonIndex];
        progress = 0;
        acceptingInput = false;
        changingLesson = false;

        if (uiController != null)
        {
            uiController.ShowBeatCard(lesson, lessonIndex + 1, lessons.Length, delegate { BeginGuidedLesson(lessonIndex); });
        }
        else
        {
            BeginGuidedLesson(lessonIndex);
        }
    }

    private void BeginGuidedLesson(int index)
    {
        lessonIndex = Mathf.Clamp(index, 0, lessons.Length - 1);
        OceanLesson lesson = lessons[lessonIndex];
        progress = 0;
        ResetBeatClock(lesson, startDelay);
        acceptingInput = true;
        changingLesson = false;

        ConfigureMusic(lesson);

        if (uiController != null)
        {
            uiController.ShowLesson(lesson, lessonIndex + 1, lessons.Length, progress);
        }
    }

    private void ConfigureMusic(OceanLesson lesson)
    {
        AudioClip clip = GetClipForLesson(lesson);
        if (clip != null)
        {
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

    private AudioClip GetClipForLesson(OceanLesson lesson)
    {
        if (lesson == null)
        {
            return null;
        }
        if (lesson.beatsPerBar == 4 && lesson.animalKey == "Fish")
        {
            return fourFourClip;
        }
        if (lesson.beatsPerBar == 3)
        {
            return threeFourClip;
        }
        if (lesson.beatsPerBar == 2)
        {
            return twoFourClip;
        }
        return sixEightClip;
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

        if (phase == OceanRhythmPhase.FreePond)
        {
            JudgeFreePondInput(result, diff, lesson);
            return;
        }

        if (result == OceanRhythmHitResult.Perfect || result == OceanRhythmHitResult.Good)
        {
            progress++;
        }
        else if (result == OceanRhythmHitResult.Miss)
        {
            progress = Mathf.Max(0, progress - 1);
        }

        if (uiController != null)
        {
            uiController.ShowInputResult(result, diff, progress, lesson.requiredHits);
        }

        if (!changingLesson && progress >= lesson.requiredHits)
        {
            changingLesson = true;
            StartCoroutine(CompleteLessonRoutine());
        }
    }

    private IEnumerator CompleteLessonRoutine()
    {
        acceptingInput = false;
        OceanLesson lesson = lessons[lessonIndex];
        if (uiController != null)
        {
            uiController.MarkGuideFishCollected(lessonIndex);
        }

        if (uiController != null)
        {
            uiController.ShowLessonComplete(lesson, lessonIndex >= lessons.Length - 1);
        }

        yield return new WaitForSeconds(1.35f);

        if (lessonIndex >= lessons.Length - 1)
        {
            EnterFreePond();
            yield break;
        }

        StartLesson(lessonIndex + 1);
    }

    private void EnterFreePond()
    {
        phase = OceanRhythmPhase.FreePond;
        progress = 0;
        selectedPondAnimal = null;
        pondAnimals.Clear();
        acceptingInput = true;
        changingLesson = false;
        pondCompleted = false;

        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }

        if (uiController != null)
        {
            uiController.ShowFreePond(lessons, pondAnimals, 2);
            uiController.UpdateBucket(bucketInventory);
            uiController.SetSingingShellAvailable(false);
        }

        singingShellAvailable = false;
        soundMatchState = OceanSoundMatchState.Idle;
        catchesSinceLastSingingShell = 0;
        firstSingingShellShown = false;
        ScheduleNextMysteryFish(8f, 18f);
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
            ResetBeatClock(selectedPondAnimal.Lesson, 0.2f);
            ConfigureMusic(selectedPondAnimal.Lesson);
            uiController.ShowFreePondSelection(selectedPondAnimal);
        }
        else
        {
            uiController.ShowNoFishSelected();
        }
    }

    private void JudgeFreePondInput(OceanRhythmHitResult result, float timingError, OceanLesson lesson)
    {
        if (selectedPondAnimal == null)
        {
            if (uiController != null)
            {
                uiController.ShowNoFishSelected();
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

    public void StartSingingShellGame()
    {
        if (phase != OceanRhythmPhase.FreePond || lessons == null || lessons.Length == 0)
        {
            return;
        }

        soundMatchLesson = lessons[Random.Range(0, lessons.Length)];
        soundMatchState = OceanSoundMatchState.Listening;
        if (uiController != null)
        {
            uiController.ShowSoundMatch(soundMatchLesson, lessons, false);
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
        if (uiController == null || lessons == null || lessons.Length == 0)
        {
            ScheduleNextMysteryFish(45f, 75f);
            return;
        }

        OceanLesson baseLesson = lessons[Random.Range(0, lessons.Length)];
        OceanLesson mysteryLesson = new OceanLesson(
            OceanFishType.Mystery,
            "Mystery Fish",
            baseLesson.meterLabel,
            "Listen first. This fish has a surprise beat.",
            baseLesson.beatsPerBar,
            Random.Range(60f, 90f),
            Mathf.Max(5, baseLesson.requiredHits / 2),
            baseLesson.accentBeats);

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

        if (lessons == null || lessons.Length == 0)
        {
            return null;
        }

        return lessons[Mathf.Clamp(lessonIndex, 0, lessons.Length - 1)];
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
        nextBeatTime = Time.time + Mathf.Max(0.01f, delay);
    }

    private void BuildLessons()
    {
        lessons = new OceanLesson[]
        {
            new OceanLesson(OceanFishType.Fish, "Little Fish", "4/4", "Count 1 2 3 4. Tap Space with the bright bubbles.", 4, 76f, 12, new int[] { 0 }),
            new OceanLesson(OceanFishType.Octopus, "Octopus", "3/4", "Feel the swing: 1 2 3, 1 2 3.", 3, 72f, 9, new int[] { 0 }),
            new OceanLesson(OceanFishType.Turtle, "Sea Turtle", "2/4", "March gently: 1 2, 1 2.", 2, 82f, 8, new int[] { 0 }),
            new OceanLesson(OceanFishType.Jellyfish, "Jellyfish", "6/8", "Sway in two groups: 1 2 3, 4 5 6.", 6, 68f, 12, new int[] { 0, 3 })
        };
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

    public string animalKey
    {
        get { return fishType.ToString(); }
    }

    public OceanLesson(OceanFishType fishType, string animalName, string meterLabel, string instruction, int beatsPerBar, float bpm, int requiredHits, int[] accentBeats)
    {
        this.fishType = fishType;
        this.animalName = animalName;
        this.meterLabel = meterLabel;
        this.instruction = instruction;
        this.beatsPerBar = beatsPerBar;
        this.bpm = bpm;
        this.requiredHits = requiredHits;
        this.accentBeats = accentBeats;
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
