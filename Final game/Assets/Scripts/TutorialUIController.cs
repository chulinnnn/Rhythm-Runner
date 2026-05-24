using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialUIController : MonoBehaviour
{
    [Header("Optional art")]
    public Sprite backgroundArt;
    public Sprite coachImage;
    public Sprite jumpIcon;
    public Sprite beatIcon;
    public Sprite successImage;
    public Sprite failureImage;

    [Header("Style")]
    public Color backdropColor = new Color(0.02f, 0.03f, 0.04f, 0.28f);
    public Color panelColor = new Color(0.04f, 0.06f, 0.08f, 0.82f);
    public Color softPanelColor = new Color(0.09f, 0.12f, 0.15f, 0.78f);
    public Color accentColor = new Color(1f, 0.78f, 0.18f);
    public Color goodColor = new Color(0.18f, 0.86f, 1f);
    public Color successColor = new Color(0.36f, 0.92f, 0.52f);
    public Color missColor = new Color(1f, 0.22f, 0.18f);
    public Color textColor = Color.white;

    [Header("Copy")]
    public string titleIntro = "Warm Up";
    public string titlePractice = "Tutorial";
    public string introPrimary = "Jump on the yellow pulse";
    public string introSecondary = "Press SPACE or UP ARROW when the ring flashes.";
    public string practicePrimary = "Match the beat";
    public string practiceSecondary = "Keep jumping every 2 beats.";
    public string successTitle = "Ready!";
    public string successSubtitle = "Entering the run...";
    public string failureTitle = "Try Again";
    public string failureSubtitle = "Missed the rhythm. Restart the warm up.";

    private const float ResultScale = 1.18f;
    private const float BeatReadyWindow = 0.14f;

    private Canvas canvas;
    private CanvasGroup rootGroup;
    private GameObject mainRoot;
    private GameObject failOverlay;
    private GameObject successOverlay;

    private Text topTitleText;
    private Text bpmText;
    private Text objectiveText;
    private Text coachSpeechText;
    private Text coachNameText;
    private Text actionTitleText;
    private Text actionBodyText;
    private Text keyText;
    private Text beatPromptText;
    private Text countdownText;
    private Text resultText;
    private Text progressText;
    private Text successText;
    private Text successSubText;
    private Text failText;
    private Text failSubText;
    private Text coachPlaceholderText;
    private Text jumpPlaceholderText;
    private Text beatPlaceholderText;
    private Text successPlaceholderText;
    private Text failurePlaceholderText;

    private Image backdropImage;
    private Image backgroundArtView;
    private Image coachImageView;
    private Image jumpIconView;
    private Image beatIconView;
    private Image successImageView;
    private Image failureImageView;
    private Image beatRingBack;
    private Image beatRingFill;
    private Image beatCore;
    private Image keyCapImage;
    private Image progressFill;
    private Image[] progressPips;

    private int requiredHits = 8;
    private int currentStreak;
    private float tutorialBpm = 126f;
    private int cueEveryBeats = 2;
    private int firstCueBeat = 6;
    private float resultFlashTimer;
    private float missFlashTimer;
    private bool practiceVisible;
    private RhythmManager rhythmManager;

    private Font UiFont
    {
        get { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
    }

    public void Configure(int required, float bpm, int beatsBetweenCues, int firstBeat)
    {
        requiredHits = Mathf.Max(1, required);
        tutorialBpm = Mathf.Max(1f, bpm);
        cueEveryBeats = Mathf.Max(1, beatsBetweenCues);
        firstCueBeat = Mathf.Max(0, firstBeat);
        RefreshStaticText();
        SetProgress(currentStreak, requiredHits);
    }

    public void BindRhythmManager(RhythmManager manager)
    {
        rhythmManager = manager;
    }

    public void EnsureUi()
    {
        if (canvas != null)
        {
            ApplyOptionalSprites();
            RefreshStaticText();
            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("TutorialCanvas", typeof(RectTransform));
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1600;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        rootGroup = canvasObject.AddComponent<CanvasGroup>();
        rootGroup.alpha = 1f;

        mainRoot = CreateRect(canvasObject.transform, "TutorialHUD", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        backdropImage = mainRoot.AddComponent<Image>();
        backdropImage.color = backdropColor;
        backdropImage.raycastTarget = false;

        backgroundArtView = CreateImage(mainRoot.transform, "TutorialBackgroundArt", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1280f, 720f), new Color(1f, 1f, 1f, 0.18f));
        backgroundArtView.raycastTarget = false;

        CreateTopBar(mainRoot.transform);
        CreateCoachCard(mainRoot.transform);
        CreateBeatGuide(mainRoot.transform);
        CreateActionCard(mainRoot.transform);
        CreateProgressBar(mainRoot.transform);
        CreateOverlayPanels(canvasObject.transform);

        ApplyOptionalSprites();
        RefreshStaticText();
        ShowIntro();
    }

    public void ShowIntro()
    {
        EnsureUi();
        practiceVisible = false;
        currentStreak = 0;
        SetMainVisible(true);
        SetOverlayVisible(failOverlay, false);
        SetOverlayVisible(successOverlay, false);

        topTitleText.text = titleIntro;
        objectiveText.text = "Goal  " + requiredHits + " beat hits in a row";
        coachSpeechText.text = introPrimary + "\n" + introSecondary;
        actionTitleText.text = "Your move";
        actionBodyText.text = "Watch the ring. Jump when it turns bright.";
        beatPromptText.text = "Get ready";
        countdownText.text = "";
        resultText.text = "";
        keyText.text = "SPACE";
        SetProgress(0, requiredHits);
    }

    public void ShowCountdown(string value)
    {
        EnsureUi();
        practiceVisible = false;
        countdownText.text = value;
        beatPromptText.text = value == "Go" ? "Jump on beat" : "Starting";
        resultText.text = "";
        keyCapImage.color = value == "Go" ? accentColor : softPanelColor;
    }

    public void ShowPractice(int streak, int required)
    {
        EnsureUi();
        practiceVisible = true;
        currentStreak = streak;
        requiredHits = Mathf.Max(1, required);

        topTitleText.text = titlePractice;
        objectiveText.text = "Hit streak  " + currentStreak + " / " + requiredHits;
        coachSpeechText.text = practicePrimary + "\n" + practiceSecondary;
        actionTitleText.text = "Jump timing";
        actionBodyText.text = "SPACE or UP ARROW. Hit the bright pulse.";
        countdownText.text = "";
        resultText.text = "Listen first, then jump";
        resultText.color = new Color(1f, 1f, 1f, 0.76f);
        keyText.text = "SPACE";
        SetProgress(streak, required);
    }

    public void ShowInputResult(RhythmTimingResult result, int streak, int required)
    {
        EnsureUi();
        practiceVisible = true;
        currentStreak = Mathf.Max(0, streak);
        requiredHits = Mathf.Max(1, required);
        SetProgress(currentStreak, requiredHits);

        if (result == RhythmTimingResult.Perfect)
        {
            ShowResult("PERFECT", accentColor, "Clean hit. Keep the streak.");
        }
        else if (result == RhythmTimingResult.Good)
        {
            ShowResult("GOOD", goodColor, "Close enough. Stay with it.");
        }
        else if (result == RhythmTimingResult.Miss)
        {
            ShowResult("MISS", missColor, "Too early or too late. Reset.");
            missFlashTimer = 0.22f;
        }
        else
        {
            ShowResult("NO MUSIC", Color.gray, "Music source is not playing.");
        }
    }

    public void ShowSuccess()
    {
        EnsureUi();
        practiceVisible = false;
        SetMainVisible(false);
        SetOverlayVisible(failOverlay, false);
        SetOverlayVisible(successOverlay, true);
        successText.text = successTitle;
        successSubText.text = successSubtitle;
        successImageView.enabled = successImage != null;
        successPlaceholderText.enabled = successImage == null;
    }

    public void ShowFailure()
    {
        EnsureUi();
        practiceVisible = false;
        SetMainVisible(false);
        SetOverlayVisible(successOverlay, false);
        SetOverlayVisible(failOverlay, true);
        failText.text = failureTitle;
        failSubText.text = failureSubtitle;
    }

    void Update()
    {
        AnimateResult();
        AnimateBeatGuide();
        AnimateMissFlash();
    }

    private void ShowResult(string label, Color color, string coachLine)
    {
        resultText.text = label;
        resultText.color = color;
        resultText.transform.localScale = Vector3.one * ResultScale;
        resultFlashTimer = 0.36f;
        coachSpeechText.text = coachLine;
        objectiveText.text = "Hit streak  " + currentStreak + " / " + requiredHits;
        keyCapImage.color = color;
    }

    private void AnimateResult()
    {
        if (resultText == null)
        {
            return;
        }

        resultText.transform.localScale = Vector3.Lerp(resultText.transform.localScale, Vector3.one, 9f * Time.unscaledDeltaTime);
        if (resultFlashTimer > 0f)
        {
            resultFlashTimer -= Time.unscaledDeltaTime;
            if (resultFlashTimer <= 0f && practiceVisible)
            {
                resultText.text = "Next pulse";
                resultText.color = new Color(1f, 1f, 1f, 0.76f);
            }
        }
    }

    private void AnimateBeatGuide()
    {
        if (beatRingFill == null || beatRingBack == null || beatCore == null)
        {
            return;
        }

        float songTime = GetSongTime();
        float beatInterval = 60f / Mathf.Max(1f, tutorialBpm);
        float currentBeat = songTime / Mathf.Max(0.001f, beatInterval);
        float nextCueBeat = GetNextCueBeat(currentBeat);
        float timeToCue = nextCueBeat * beatInterval - songTime;
        float cueWindow = beatInterval * Mathf.Max(1, cueEveryBeats);
        float progress = 1f - Mathf.Clamp01(timeToCue / Mathf.Max(0.001f, cueWindow));
        bool ready = timeToCue <= BeatReadyWindow && timeToCue >= -BeatReadyWindow;

        beatRingFill.fillAmount = progress;
        beatRingFill.color = ready ? accentColor : goodColor;
        beatCore.color = ready ? accentColor : new Color(1f, 1f, 1f, 0.22f);
        beatPromptText.text = ready ? "JUMP" : "wait";
        keyCapImage.color = ready ? accentColor : softPanelColor;

        float pulse = ready ? 1.16f + Mathf.Sin(Time.unscaledTime * 26f) * 0.04f : 1f + progress * 0.08f;
        beatRingBack.transform.localScale = Vector3.one * pulse;
        beatRingFill.transform.localScale = Vector3.one * pulse;
        beatCore.transform.localScale = Vector3.one * (ready ? 1.08f : 1f);

        if (!practiceVisible && countdownText != null && !string.IsNullOrEmpty(countdownText.text))
        {
            beatPromptText.text = countdownText.text == "Go" ? "JUMP" : "ready";
        }
    }

    private void AnimateMissFlash()
    {
        if (backdropImage == null)
        {
            return;
        }

        if (missFlashTimer > 0f)
        {
            missFlashTimer -= Time.unscaledDeltaTime;
            backdropImage.color = Color.Lerp(backdropColor, new Color(1f, 0.05f, 0.03f, 0.2f), missFlashTimer / 0.22f);
        }
        else
        {
            backdropImage.color = backdropColor;
        }
    }

    private float GetSongTime()
    {
        if (rhythmManager != null)
        {
            return Mathf.Max(0f, rhythmManager.GetAdjustedSongTime());
        }

        return Mathf.Max(0f, Time.timeSinceLevelLoad);
    }

    private float GetNextCueBeat(float currentBeat)
    {
        if (currentBeat <= firstCueBeat)
        {
            return firstCueBeat;
        }

        float beatsSinceStart = currentBeat - firstCueBeat;
        int cueIndex = Mathf.CeilToInt(beatsSinceStart / Mathf.Max(1, cueEveryBeats));
        return firstCueBeat + cueIndex * Mathf.Max(1, cueEveryBeats);
    }

    private void CreateTopBar(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "TutorialTopBar", new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(1040f, 72f), panelColor);
        topTitleText = CreateText(panel.transform, "Title", new Vector2(0.06f, 0.58f), Vector2.zero, new Vector2(260f, 34f), 28, FontStyle.Bold, TextAnchor.MiddleLeft);
        objectiveText = CreateText(panel.transform, "Objective", new Vector2(0.52f, 0.58f), Vector2.zero, new Vector2(430f, 30f), 20, FontStyle.Bold, TextAnchor.MiddleCenter);
        bpmText = CreateText(panel.transform, "Bpm", new Vector2(0.92f, 0.58f), Vector2.zero, new Vector2(150f, 30f), 20, FontStyle.Bold, TextAnchor.MiddleRight);

        Text hint = CreateText(panel.transform, "Hint", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(860f, 22f), 16, FontStyle.Normal, TextAnchor.MiddleCenter);
        hint.text = "Watch the ring, press once, then land back into the rhythm.";
        hint.color = new Color(1f, 1f, 1f, 0.66f);
    }

    private void CreateCoachCard(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "TutorialCoachCard", new Vector2(0f, 0.5f), new Vector2(154f, 42f), new Vector2(260f, 300f), softPanelColor);
        coachNameText = CreateText(panel.transform, "CoachName", new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(220f, 28f), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        coachNameText.text = "Coach";

        coachImageView = CreateImage(panel.transform, "CoachImage", new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(130f, 130f), new Color(1f, 1f, 1f, 0.08f));
        coachPlaceholderText = CreateText(coachImageView.transform, "CoachPlaceholder", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 50f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        coachPlaceholderText.text = "COACH";
        coachPlaceholderText.color = new Color(1f, 1f, 1f, 0.42f);

        coachSpeechText = CreateText(panel.transform, "CoachSpeech", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(220f, 96f), 19, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    private void CreateBeatGuide(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "TutorialBeatGuide", new Vector2(0.5f, 0.54f), new Vector2(0f, 14f), new Vector2(330f, 330f), new Color(0.02f, 0.03f, 0.04f, 0.52f));

        beatRingBack = CreateImage(panel.transform, "BeatRingBack", new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(206f, 206f), new Color(1f, 1f, 1f, 0.16f));
        beatRingBack.sprite = CreateRingSprite();
        beatRingFill = CreateImage(panel.transform, "BeatRingFill", new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(206f, 206f), goodColor);
        beatRingFill.sprite = CreateRingSprite();
        beatRingFill.type = Image.Type.Filled;
        beatRingFill.fillMethod = Image.FillMethod.Radial360;
        beatRingFill.fillOrigin = 2;
        beatRingFill.fillClockwise = true;
        beatRingFill.fillAmount = 0f;

        beatCore = CreateImage(panel.transform, "BeatCore", new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(92f, 92f), new Color(1f, 1f, 1f, 0.2f));
        beatCore.sprite = CreateCircleSprite();

        beatIconView = CreateImage(panel.transform, "BeatIcon", new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(54f, 54f), accentColor);
        beatPlaceholderText = CreateText(beatIconView.transform, "BeatPlaceholder", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 32f), 20, FontStyle.Bold, TextAnchor.MiddleCenter);
        beatPlaceholderText.text = "BEAT";
        beatPlaceholderText.color = new Color(0f, 0f, 0f, 0.58f);

        beatPromptText = CreateText(panel.transform, "BeatPrompt", new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(260f, 46f), 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        beatPromptText.color = accentColor;
        countdownText = CreateText(panel.transform, "Countdown", new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(210f, 92f), 70, FontStyle.Bold, TextAnchor.MiddleCenter);
        countdownText.color = Color.white;
        resultText = CreateText(panel.transform, "Result", new Vector2(0.5f, 0.03f), Vector2.zero, new Vector2(260f, 34f), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    private void CreateActionCard(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "TutorialActionCard", new Vector2(1f, 0.5f), new Vector2(-166f, 42f), new Vector2(282f, 300f), softPanelColor);

        actionTitleText = CreateText(panel.transform, "ActionTitle", new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(220f, 28f), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        jumpIconView = CreateImage(panel.transform, "JumpIcon", new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(92f, 92f), goodColor);
        jumpPlaceholderText = CreateText(jumpIconView.transform, "JumpPlaceholder", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 40f), 19, FontStyle.Bold, TextAnchor.MiddleCenter);
        jumpPlaceholderText.text = "JUMP";
        jumpPlaceholderText.color = new Color(0f, 0f, 0f, 0.58f);

        keyCapImage = CreateImage(panel.transform, "KeyCap", new Vector2(0.5f, 0.40f), Vector2.zero, new Vector2(150f, 54f), softPanelColor);
        keyText = CreateText(keyCapImage.transform, "KeyText", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(146f, 52f), 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        keyText.text = "SPACE";

        actionBodyText = CreateText(panel.transform, "ActionBody", new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(230f, 80f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    private void CreateProgressBar(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "TutorialProgressPanel", new Vector2(0.5f, 0f), new Vector2(0f, 74f), new Vector2(800f, 86f), panelColor);
        progressText = CreateText(panel.transform, "ProgressText", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(640f, 24f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);

        GameObject track = CreatePanel(panel.transform, "ProgressTrack", new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(650f, 16f), new Color(1f, 1f, 1f, 0.14f));
        GameObject fill = CreateRect(track.transform, "ProgressFill", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero).gameObject;
        progressFill = fill.AddComponent<Image>();
        progressFill.color = accentColor;

        progressPips = new Image[8];
        for (int i = 0; i < progressPips.Length; i++)
        {
            float x = -280f + i * 80f;
            Image pip = CreateImage(panel.transform, "ProgressPip" + (i + 1), new Vector2(0.5f, 0.30f), new Vector2(x, 0f), new Vector2(18f, 18f), new Color(1f, 1f, 1f, 0.22f));
            pip.sprite = CreateCircleSprite();
            progressPips[i] = pip;
        }
    }

    private void CreateOverlayPanels(Transform parent)
    {
        failOverlay = CreateFullOverlay(parent, "TutorialFailOverlay", new Color(0.05f, 0.02f, 0.02f, 0.9f));
        failureImageView = CreateImage(failOverlay.transform, "FailureImage", new Vector2(0.5f, 0.71f), Vector2.zero, new Vector2(130f, 130f), new Color(1f, 1f, 1f, 0.08f));
        failurePlaceholderText = CreateText(failureImageView.transform, "FailurePlaceholder", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 48f), 20, FontStyle.Bold, TextAnchor.MiddleCenter);
        failurePlaceholderText.text = "MISS";
        failurePlaceholderText.color = new Color(1f, 1f, 1f, 0.5f);
        failText = CreateText(failOverlay.transform, "FailText", new Vector2(0.5f, 0.60f), Vector2.zero, new Vector2(620f, 82f), 56, FontStyle.Bold, TextAnchor.MiddleCenter);
        failText.color = missColor;
        failSubText = CreateText(failOverlay.transform, "FailSubText", new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(640f, 40f), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        failSubText.color = new Color(1f, 1f, 1f, 0.76f);
        CreateButton(failOverlay.transform, "RetryButton", "Retry", new Vector2(0.5f, 0.38f), new Vector2(-112f, 0f), new Vector2(190f, 58f), delegate { SceneTransitionManager.LoadScene("Tutorial"); });
        CreateButton(failOverlay.transform, "BackButton", "Back", new Vector2(0.5f, 0.38f), new Vector2(112f, 0f), new Vector2(190f, 58f), delegate { SceneTransitionManager.LoadScene("Start"); });

        successOverlay = CreateFullOverlay(parent, "TutorialSuccessOverlay", new Color(0.02f, 0.05f, 0.06f, 0.92f));
        successImageView = CreateImage(successOverlay.transform, "SuccessImage", new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(150f, 150f), new Color(1f, 1f, 1f, 0.1f));
        successPlaceholderText = CreateText(successImageView.transform, "SuccessPlaceholder", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 56f), 20, FontStyle.Bold, TextAnchor.MiddleCenter);
        successPlaceholderText.text = "PASS";
        successPlaceholderText.color = new Color(1f, 1f, 1f, 0.5f);
        successText = CreateText(successOverlay.transform, "SuccessText", new Vector2(0.5f, 0.46f), Vector2.zero, new Vector2(620f, 90f), 64, FontStyle.Bold, TextAnchor.MiddleCenter);
        successText.color = accentColor;
        successSubText = CreateText(successOverlay.transform, "SuccessSubText", new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(620f, 36f), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        successSubText.color = new Color(1f, 1f, 1f, 0.72f);

        SetOverlayVisible(failOverlay, false);
        SetOverlayVisible(successOverlay, false);
    }

    private void SetProgress(int streak, int required)
    {
        if (progressText == null || progressFill == null)
        {
            return;
        }

        int safeRequired = Mathf.Max(1, required);
        int safeStreak = Mathf.Clamp(streak, 0, safeRequired);
        progressText.text = "Rhythm streak  " + safeStreak + " / " + safeRequired;

        RectTransform rect = progressFill.GetComponent<RectTransform>();
        rect.anchorMax = new Vector2((float)safeStreak / safeRequired, 1f);

        if (progressPips == null)
        {
            return;
        }

        for (int i = 0; i < progressPips.Length; i++)
        {
            bool lit = i < safeStreak;
            progressPips[i].color = lit ? accentColor : new Color(1f, 1f, 1f, 0.22f);
            progressPips[i].transform.localScale = lit ? Vector3.one * 1.12f : Vector3.one;
        }
    }

    private void ApplyOptionalSprites()
    {
        ApplySprite(backgroundArtView, null, backgroundArt);
        ApplySprite(coachImageView, coachPlaceholderText, coachImage);
        ApplySprite(jumpIconView, jumpPlaceholderText, jumpIcon);
        ApplySprite(beatIconView, beatPlaceholderText, beatIcon);
        ApplySprite(successImageView, successPlaceholderText, successImage);
        ApplySprite(failureImageView, failurePlaceholderText, failureImage);
    }

    private void ApplySprite(Image image, Text placeholder, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.enabled = sprite != null || placeholder != null;

        if (placeholder != null)
        {
            placeholder.enabled = sprite == null;
        }
    }

    private void RefreshStaticText()
    {
        if (bpmText != null)
        {
            bpmText.text = Mathf.RoundToInt(tutorialBpm) + " BPM";
        }

        if (practiceSecondary == "Keep jumping every 2 beats.")
        {
            practiceSecondary = "Keep jumping every " + cueEveryBeats + " beats.";
        }
    }

    private GameObject CreatePanel(Transform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = CreateRect(parent, objectName, anchor, anchor, position, size).gameObject;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return obj;
    }

    private GameObject CreateFullOverlay(Transform parent, string objectName, Color color)
    {
        GameObject obj = CreateRect(parent, objectName, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return obj;
    }

    private Text CreateText(Transform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject obj = CreateRect(parent, objectName, anchor, anchor, position, size).gameObject;
        Text text = obj.AddComponent<Text>();
        text.font = UiFont;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = textColor;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private Image CreateImage(Transform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = CreateRect(parent, objectName, anchor, anchor, position, size).gameObject;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void CreateButton(Transform parent, string objectName, string label, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = CreatePanel(parent, objectName, anchor, position, size, new Color(1f, 1f, 1f, 0.16f));
        Button button = obj.AddComponent<Button>();
        button.onClick.AddListener(action);
        Text text = CreateText(obj.transform, "Text", new Vector2(0.5f, 0.5f), Vector2.zero, size, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.text = label;
    }

    private RectTransform CreateRect(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.pivot = new Vector2(0.5f, 0.5f);

        if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        return rect;
    }

    private void SetMainVisible(bool visible)
    {
        if (mainRoot != null)
        {
            mainRoot.SetActive(visible);
        }
    }

    private void SetOverlayVisible(GameObject overlay, bool visible)
    {
        if (overlay != null)
        {
            overlay.SetActive(visible);
        }
    }

    private Sprite CreateRingSprite()
    {
        const int size = 128;
        const float outerRadius = 58f;
        const float innerRadius = 43f;
        Texture2D texture = new Texture2D(size, size);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= outerRadius && distance >= innerRadius ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Sprite CreateCircleSprite()
    {
        const int size = 64;
        const float radius = 29f;
        Texture2D texture = new Texture2D(size, size);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
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
