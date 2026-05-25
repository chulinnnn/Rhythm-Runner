using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-140)]
public class OceanRhythmManager : MonoBehaviour
{
    private static bool registered;

    [Header("Sprites")]
    public Sprite waterBackgroundSprite;
    public Sprite fishSprite;
    public Sprite octopusSprite;
    public Sprite turtleSprite;
    public Sprite jellyfishSprite;
    public Sprite netSprite;

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
    private int lessonIndex;
    private int progress;
    private int beatIndex;
    private float beatInterval;
    private float nextBeatTime;
    private bool acceptingInput;
    private bool changingLesson;

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
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "OceanRhythm")
        {
            return;
        }

        uiController.Build(this);
        StartLesson(0);
    }

    private void Update()
    {
        if (!acceptingInput)
        {
            return;
        }

        while (Time.time >= nextBeatTime)
        {
            OnBeat();
            nextBeatTime += beatInterval;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            JudgeSpaceInput();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneTransitionManager.LoadScene("Start");
        }
    }

    public void ReturnToStart()
    {
        SceneTransitionManager.LoadScene("Start");
    }

    public void RestartCurrentLesson()
    {
        StartLesson(lessonIndex);
    }

    public Sprite GetSpriteForLesson(OceanLesson lesson)
    {
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
        return jellyfishSprite;
    }

    public Sprite GetWaterBackgroundSprite()
    {
        return waterBackgroundSprite;
    }

    public Sprite GetNetSprite()
    {
        return netSprite;
    }

    private void StartLesson(int index)
    {
        if (lessons == null || lessons.Length == 0)
        {
            return;
        }

        lessonIndex = Mathf.Clamp(index, 0, lessons.Length - 1);
        OceanLesson lesson = lessons[lessonIndex];
        progress = 0;
        beatIndex = -1;
        beatInterval = 60f / Mathf.Max(30f, lesson.bpm);
        nextBeatTime = Time.time + startDelay;
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

    private void OnBeat()
    {
        OceanLesson lesson = lessons[lessonIndex];
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
        OceanLesson lesson = lessons[lessonIndex];
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
            uiController.ShowLessonComplete(lesson, lessonIndex >= lessons.Length - 1);
        }

        yield return new WaitForSeconds(1.35f);

        if (lessonIndex >= lessons.Length - 1)
        {
            SceneTransitionManager.LoadScene("Start");
            yield break;
        }

        StartLesson(lessonIndex + 1);
    }

    private void BuildLessons()
    {
        lessons = new OceanLesson[]
        {
            new OceanLesson("Fish", "Little Fish", "4/4", "Count 1 2 3 4. Tap Space with the bright bubbles.", 4, 76f, 12, new int[] { 0 }),
            new OceanLesson("Octopus", "Octopus", "3/4", "Feel the swing: 1 2 3, 1 2 3.", 3, 72f, 9, new int[] { 0 }),
            new OceanLesson("Turtle", "Sea Turtle", "2/4", "March gently: 1 2, 1 2.", 2, 82f, 8, new int[] { 0 }),
            new OceanLesson("Jellyfish", "Jellyfish", "6/8", "Sway in two groups: 1 2 3, 4 5 6.", 6, 68f, 12, new int[] { 0, 3 })
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
    public string animalKey;
    public string animalName;
    public string meterLabel;
    public string instruction;
    public int beatsPerBar;
    public float bpm;
    public int requiredHits;
    public int[] accentBeats;

    public OceanLesson(string animalKey, string animalName, string meterLabel, string instruction, int beatsPerBar, float bpm, int requiredHits, int[] accentBeats)
    {
        this.animalKey = animalKey;
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
