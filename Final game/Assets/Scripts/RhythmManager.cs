using UnityEngine;
using UnityEngine.UI;

public enum RhythmTimingResult
{
    None,
    Perfect,
    Good,
    Miss
}

public class RhythmManager : MonoBehaviour
{
    public static RhythmManager Instance { get; private set; }

    [Header("Music timing")]
    public AudioSource musicSource;
    public string fallbackMusicObjectName = "146bpm";
    public float bpm = 107f;
    public float firstBeatOffset = 0f;

    [Header("Timing windows")]
    public float perfectWindow = 0.08f;
    public float goodWindow = 0.15f;

    [Header("Feedback")]
    public Text feedbackText;
    public float feedbackDuration = 0.45f;
    public bool createFeedbackTextIfMissing = true;

    [Header("Score reward")]
    public bool awardRhythmScore = true;
    public int perfectBonus = 2;
    public int goodBonus = 1;

    [Header("Beat visualization")]
    public string visualizationObjectName = "visualization";
    public string secondVisualizationObjectName = "visual2";
    public string correctObjectName = "correct";
    public string missObjectName = "miss";
    public float visualizationBpm = 107f;
    public float visualizationPulseScale = 1.35f;
    public float visualizationPulseDuration = 0.12f;
    public float resultVisualDuration = 0.35f;
    public Color visualizationBaseColor = Color.white;
    public Color visualizationBeatColor = new Color(1f, 0.85f, 0.15f);

    private float feedbackTimer;
    private GameObject visualizationObject;
    private GameObject secondVisualizationObject;
    private Transform visualizationTransform;
    private Transform secondVisualizationTransform;
    private GameObject correctObject;
    private GameObject missObject;
    private Vector3 visualizationBaseScale;
    private Vector3 secondVisualizationBaseScale;
    private Image visualizationImage;
    private Image secondVisualizationImage;
    private SpriteRenderer visualizationSpriteRenderer;
    private SpriteRenderer secondVisualizationSpriteRenderer;
    private Renderer visualizationRenderer;
    private Renderer secondVisualizationRenderer;
    private float visualizationPulseTimer;
    private int lastVisualizationBeat = -1;
    private bool showSecondVisualization;
    private float resultVisualTimer;

    private float BeatInterval
    {
        get { return 60f / Mathf.Max(1f, bpm); }
    }

    void Awake()
    {
        Instance = this;
        ResolveMusicSource();
        EnsureFeedbackText();
        ResolveVisualization();
    }

    void Update()
    {
        UpdateFeedbackText();
        UpdateResultVisuals();
        UpdateVisualization();
    }

    private void UpdateFeedbackText()
    {
        if (feedbackText == null || feedbackTimer <= 0f)
        {
            return;
        }

        feedbackTimer -= Time.deltaTime;
        if (feedbackTimer <= 0f)
        {
            feedbackText.text = "";
        }
    }

    public RhythmTimingResult JudgeInput()
    {
        if (musicSource == null || !musicSource.isPlaying)
        {
            return RhythmTimingResult.None;
        }

        float songTime = musicSource.time - firstBeatOffset;
        if (songTime < 0f)
        {
            return RhythmTimingResult.Miss;
        }

        float beatPosition = songTime / BeatInterval;
        float nearestBeat = Mathf.Round(beatPosition);
        float timeToNearestBeat = Mathf.Abs(songTime - nearestBeat * BeatInterval);

        if (timeToNearestBeat <= perfectWindow)
        {
            return RhythmTimingResult.Perfect;
        }

        if (timeToNearestBeat <= goodWindow)
        {
            return RhythmTimingResult.Good;
        }

        return RhythmTimingResult.Miss;
    }

    public RhythmTimingResult ReportInput(string actionName)
    {
        RhythmTimingResult result = JudgeInput();
        ShowFeedback(result, actionName);
        ShowResultVisual(result);
        AwardScore(result);
        return result;
    }

    private void AwardScore(RhythmTimingResult result)
    {
        if (!awardRhythmScore || GameManager.Instance == null)
        {
            return;
        }

        if (result == RhythmTimingResult.Perfect)
        {
            GameManager.Instance.UpdateBonus(perfectBonus);
        }
        else if (result == RhythmTimingResult.Good)
        {
            GameManager.Instance.UpdateBonus(goodBonus);
        }
    }

    private void ShowFeedback(RhythmTimingResult result, string actionName)
    {
        if (feedbackText == null)
        {
            return;
        }

        if (result == RhythmTimingResult.None)
        {
            feedbackText.text = "No Music";
            feedbackText.color = Color.gray;
        }
        else if (result == RhythmTimingResult.Perfect)
        {
            feedbackText.text = "Perfect!";
            feedbackText.color = new Color(1f, 0.85f, 0.15f);
        }
        else if (result == RhythmTimingResult.Good)
        {
            feedbackText.text = "Good";
            feedbackText.color = new Color(0.35f, 0.9f, 1f);
        }
        else
        {
            feedbackText.text = "Miss";
            feedbackText.color = new Color(1f, 0.35f, 0.35f);
        }

        if (!string.IsNullOrEmpty(actionName))
        {
            feedbackText.text += " " + actionName;
        }

        feedbackTimer = feedbackDuration;
    }

    private void ResolveVisualization()
    {
        if (visualizationTransform != null)
        {
            return;
        }

        visualizationObject = FindSceneObjectByName(visualizationObjectName);
        if (visualizationObject == null)
        {
            return;
        }

        visualizationObject.SetActive(true);
        visualizationTransform = visualizationObject.transform;
        visualizationBaseScale = visualizationTransform.localScale;
        visualizationImage = visualizationObject.GetComponent<Image>();
        visualizationSpriteRenderer = visualizationObject.GetComponent<SpriteRenderer>();
        visualizationRenderer = visualizationObject.GetComponent<Renderer>();

        secondVisualizationObject = FindChildObjectByName(visualizationTransform, secondVisualizationObjectName);
        if (secondVisualizationObject == null)
        {
            secondVisualizationObject = FindSceneObjectByName(secondVisualizationObjectName);
        }

        if (secondVisualizationObject != null)
        {
            secondVisualizationObject.SetActive(true);
            secondVisualizationTransform = secondVisualizationObject.transform;
            secondVisualizationBaseScale = secondVisualizationTransform.localScale;
            secondVisualizationImage = secondVisualizationObject.GetComponent<Image>();
            secondVisualizationSpriteRenderer = secondVisualizationObject.GetComponent<SpriteRenderer>();
            secondVisualizationRenderer = secondVisualizationObject.GetComponent<Renderer>();
        }

        correctObject = FindSceneObjectByName(correctObjectName);
        missObject = FindSceneObjectByName(missObjectName);

        SetVisualizationColor(visualizationBaseColor);
        SetSecondVisualizationColor(visualizationBaseColor);
        HideResultObjects();
    }

    private void UpdateVisualization()
    {
        if (visualizationTransform == null)
        {
            ResolveVisualization();
            if (visualizationTransform == null)
            {
                return;
            }
        }

        if (resultVisualTimer > 0f)
        {
            SetBeatVisualsVisible(false, false);
            return;
        }

        float interval = 60f / Mathf.Max(1f, visualizationBpm);
        int currentBeat = Mathf.FloorToInt(Time.time / interval);
        if (currentBeat != lastVisualizationBeat)
        {
            lastVisualizationBeat = currentBeat;
            visualizationPulseTimer = visualizationPulseDuration;
            showSecondVisualization = !showSecondVisualization;
        }

        if (visualizationPulseTimer > 0f)
        {
            visualizationPulseTimer -= Time.deltaTime;
        }

        float pulse = Mathf.Clamp01(visualizationPulseTimer / Mathf.Max(0.01f, visualizationPulseDuration));
        float scale = Mathf.Lerp(1f, visualizationPulseScale, pulse);
        Color color = Color.Lerp(visualizationBaseColor, visualizationBeatColor, pulse);

        SetBeatVisualsVisible(!showSecondVisualization, showSecondVisualization);
        if (showSecondVisualization && secondVisualizationTransform != null)
        {
            secondVisualizationTransform.localScale = secondVisualizationBaseScale * scale;
            SetSecondVisualizationColor(color);
        }
        else
        {
            visualizationTransform.localScale = visualizationBaseScale * scale;
            SetVisualizationColor(color);
        }
    }

    private void ShowResultVisual(RhythmTimingResult result)
    {
        if (correctObject == null || missObject == null)
        {
            ResolveVisualization();
        }

        bool isCorrect = result == RhythmTimingResult.Perfect || result == RhythmTimingResult.Good;
        bool isMiss = result == RhythmTimingResult.Miss;

        if (!isCorrect && !isMiss)
        {
            return;
        }

        resultVisualTimer = resultVisualDuration;
        SetBeatVisualsVisible(false, false);

        if (correctObject != null)
        {
            correctObject.SetActive(isCorrect);
        }

        if (missObject != null)
        {
            missObject.SetActive(isMiss);
        }
    }

    private void UpdateResultVisuals()
    {
        if (resultVisualTimer <= 0f)
        {
            return;
        }

        resultVisualTimer -= Time.deltaTime;
        if (resultVisualTimer <= 0f)
        {
            HideResultObjects();
        }
    }

    private void HideResultObjects()
    {
        if (correctObject != null)
        {
            correctObject.SetActive(false);
        }

        if (missObject != null)
        {
            missObject.SetActive(false);
        }
    }

    private void SetBeatVisualsVisible(bool showFirst, bool showSecond)
    {
        if (visualizationObject != null && !visualizationObject.activeSelf)
        {
            visualizationObject.SetActive(true);
        }

        if (secondVisualizationObject != null && !secondVisualizationObject.activeSelf)
        {
            secondVisualizationObject.SetActive(true);
        }

        SetSelfVisualVisible(visualizationImage, visualizationSpriteRenderer, visualizationRenderer, showFirst);
        SetSelfVisualVisible(secondVisualizationImage, secondVisualizationSpriteRenderer, secondVisualizationRenderer, showSecond);
    }

    private void SetSelfVisualVisible(
        Image image,
        SpriteRenderer spriteRenderer,
        Renderer renderer,
        bool visible)
    {
        if (image != null)
        {
            image.enabled = visible;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }

        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private void SetVisualizationColor(Color color)
    {
        if (visualizationImage != null)
        {
            visualizationImage.color = color;
        }

        if (visualizationSpriteRenderer != null)
        {
            visualizationSpriteRenderer.color = color;
        }

        if (visualizationRenderer != null && visualizationRenderer.material != null)
        {
            visualizationRenderer.material.color = color;
        }
    }

    private void SetSecondVisualizationColor(Color color)
    {
        if (secondVisualizationImage != null)
        {
            secondVisualizationImage.color = color;
        }

        if (secondVisualizationSpriteRenderer != null)
        {
            secondVisualizationSpriteRenderer.color = color;
        }

        if (secondVisualizationRenderer != null && secondVisualizationRenderer.material != null)
        {
            secondVisualizationRenderer.material.color = color;
        }
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            GameObject obj = transforms[i].gameObject;
            if (obj.name == objectName && obj.scene.isLoaded)
            {
                return obj;
            }
        }

        return null;
    }

    private GameObject FindChildObjectByName(Transform parent, string objectName)
    {
        if (parent == null || string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == objectName)
            {
                return children[i].gameObject;
            }
        }

        return null;
    }

    private void ResolveMusicSource()
    {
        if (musicSource != null)
        {
            return;
        }

        GameObject musicObject = GameObject.Find(fallbackMusicObjectName);
        if (musicObject != null)
        {
            musicSource = musicObject.GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            musicSource = FindObjectOfType<AudioSource>();
        }

        if (musicSource == null)
        {
            Debug.LogWarning("RhythmManager: No AudioSource found. Assign the 146bpm music AudioSource in the Inspector.");
        }
    }

    private void EnsureFeedbackText()
    {
        if (feedbackText != null || !createFeedbackTextIfMissing)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RhythmFeedbackCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject textObject = new GameObject("RhythmFeedbackText", typeof(RectTransform));
        textObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.75f);
        rect.anchorMax = new Vector2(0.5f, 0.75f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(420f, 80f);

        feedbackText = textObject.AddComponent<Text>();
        feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        feedbackText.fontSize = 42;
        feedbackText.fontStyle = FontStyle.Bold;
        feedbackText.alignment = TextAnchor.MiddleCenter;
        feedbackText.raycastTarget = false;
        feedbackText.text = "";
    }
}
