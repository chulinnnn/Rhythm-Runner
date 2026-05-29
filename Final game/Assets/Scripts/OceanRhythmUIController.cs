using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OceanRhythmUIController : MonoBehaviour
{
    private class ImageVisualOverride
    {
        public Sprite sprite;
        public Color color;
        public Material material;
        public Image.Type type;
        public bool preserveAspect;
        public bool raycastTarget;
    }

    private OceanRhythmManager manager;
    private Canvas canvas;
    private RectTransform rootRect;
    private Font uiFont;
    private Text titleText;
    private Text subtitleText;
    private Text instructionText;
    private Text feedbackText;
    private Text progressText;
    private Text lessonCounterText;
    private Text learningModeText;
    private Text lessonGoalText;
    private Text progressHelpText;
    private Image progressFill;
    private Transform beatBubbleRoot;
    private Transform lessonTargetBubbleRoot;
    private OceanAnimalController animalController;
    private RectTransform guidedAnimalRoot;
    private RectTransform pondLayer;
    private OceanNetCursor netCursor;
    private GameObject completeOverlay;
    private GameObject pondCompleteOverlay;
    private GameObject beatCardOverlay;
    private GameObject bucketAlbumOverlay;
    private Text bucketCountText;
    private Text bucketShellText;
    private Text bucketPearlText;
    private GameObject bucketButtonObject;
    private Image bucketDecorationImage;
    private Text rewardText;
    private GameObject beatInfoButton;
    private GameObject parentHelpOverlay;
    private GameObject topBarObject;
    private GameObject tapButtonObject;
    private Image tapButtonImage;
    private Text tapButtonText;
    private GameObject backButtonObject;
    private GameObject retryButtonObject;
    private GameObject pauseButtonObject;
    private Text pauseButtonText;
    private GameObject singingShellButton;
    private Text singingShellButtonText;
    private Image singingShellButtonImage;
    private GameObject soundMatchOverlay;
    private Transform soundMatchBubbleRoot;
    private Text soundMatchTitleText;
    private Text soundMatchBodyText;
    private Text soundMatchResultText;
    private Text soundMatchPearlText;
    private Text bucketHintText;
    private Transform decorationLibraryRoot;
    private readonly List<OceanBucketSlot> bucketSlots = new List<OceanBucketSlot>();
    private readonly List<Image> beatBubbles = new List<Image>();
    private readonly List<Image> lessonTargetBubbles = new List<Image>();
    private readonly List<Image> soundMatchBubbles = new List<Image>();
    private readonly List<Image> guideCollectionIcons = new List<Image>();
    private readonly List<OceanPondAnimal> pondAnimals = new List<OceanPondAnimal>();
    private Sprite circleSprite;
    private int keyboardSelectedIndex;
    private float keyboardSelectionHoldUntil;
    private OceanPondAnimal currentFreePondSelection;
    private bool showingGuidedLesson;
    private float tapPulseScale = 1f;
    private readonly Dictionary<string, ImageVisualOverride> pendingImageOverrides = new Dictionary<string, ImageVisualOverride>();
    private readonly Dictionary<string, ImageVisualOverride> pendingNameImageOverrides = new Dictionary<string, ImageVisualOverride>();

    public void Build(OceanRhythmManager owner)
    {
        manager = owner;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        circleSprite = CreateCircleSprite("OceanCircleSprite", 96, Color.white);
        EnsureEventSystem();

        GameObject existing = GameObject.Find("OceanRhythmCanvas");
        if (existing != null)
        {
            CaptureImageOverrides(existing.transform);
            DestroyObject(existing);
        }
        else
        {
            pendingImageOverrides.Clear();
            pendingNameImageOverrides.Clear();
        }

        GameObject canvasObject = new GameObject("OceanRhythmCanvas", typeof(RectTransform));
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject root = CreateRect(canvasObject.transform, "OceanRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        rootRect = root.GetComponent<RectTransform>();
        Image background = root.AddComponent<Image>();
        background.color = new Color(0.03f, 0.43f, 0.66f, 1f);
        Sprite bgSprite = manager != null ? manager.GetWaterBackgroundSprite() : null;
        if (bgSprite != null)
        {
            background.sprite = bgSprite;
            background.preserveAspect = false;
        }

        WaterRippleController rippleController = root.AddComponent<WaterRippleController>();
        rippleController.Initialize(circleSprite);

        CreateAnimatedWater(root.transform);
        CreateTopBar(root.transform);
        CreateGuideCollection(root.transform);
        CreateCenterStage(root.transform);
        CreatePondLayer(root.transform);
        CreateBottomHud(root.transform);
        CreateBeatInfoButton(root.transform);
        CreateTapButton(root.transform);
        CreateCompleteOverlay(root.transform);
        CreatePondCompleteOverlay(root.transform);
        CreateBeatCardOverlay(root.transform);
        CreateParentHelpOverlay(root.transform);
        CreateBucketUi(root.transform);
        CreateBucketAlbum(root.transform);
        CreateSingingShellButton(root.transform);
        CreateSoundMatchOverlay(root.transform);
        CreateRewardToast(root.transform);
        CreateNavigation(root.transform);
        completeOverlay.transform.SetAsLastSibling();
        pondCompleteOverlay.transform.SetAsLastSibling();
        beatCardOverlay.transform.SetAsLastSibling();
        parentHelpOverlay.transform.SetAsLastSibling();
        bucketAlbumOverlay.transform.SetAsLastSibling();
        soundMatchOverlay.transform.SetAsLastSibling();

        completeOverlay.SetActive(false);
        pondCompleteOverlay.SetActive(false);
        beatCardOverlay.SetActive(false);
        parentHelpOverlay.SetActive(false);
        bucketAlbumOverlay.SetActive(false);
        soundMatchOverlay.SetActive(false);
        singingShellButton.SetActive(false);
        beatInfoButton.SetActive(false);
        SetBucketButtonVisible(false);
        pondLayer.gameObject.SetActive(false);
        ApplyPendingImageOverrides();
    }

    private void Update()
    {
        if (tapButtonObject != null)
        {
            tapPulseScale = Mathf.Lerp(tapPulseScale, 1f, Time.unscaledDeltaTime * 7f);
            tapButtonObject.transform.localScale = Vector3.one * tapPulseScale;
        }
    }

    public void ShowLesson(OceanLesson lesson, int lessonNumber, int lessonCount, int currentProgress)
    {
        if (lesson == null)
        {
            return;
        }

        titleText.text = lesson.animalName;
        showingGuidedLesson = true;
        SetMainTextVisible(true);
        if (topBarObject != null)
        {
            topBarObject.SetActive(true);
        }
        if (tapButtonObject != null)
        {
            tapButtonObject.SetActive(false);
        }
        SetBucketButtonVisible(true);
        SetNavigationVisible(true);
        subtitleText.text = lesson.meterLabel + "  " + Mathf.RoundToInt(lesson.bpm) + " BPM";
        instructionText.text = "Learning Mode: " + lesson.instruction + "\nTap Space on bright bubbles to fill the lesson bubbles.";
        feedbackText.text = "Listen first";
        feedbackText.color = new Color(1f, 0.95f, 0.68f);
        lessonCounterText.text = "Lesson " + lessonNumber + " / " + lessonCount;
        lessonCounterText.gameObject.SetActive(true);
        if (learningModeText != null)
        {
            learningModeText.gameObject.SetActive(true);
            learningModeText.text = "RHYTHM LESSON";
        }
        if (lessonGoalText != null)
        {
            lessonGoalText.gameObject.SetActive(true);
            lessonGoalText.text = "Need " + lesson.requiredHits + " bright-bubble taps";
        }
        if (progressHelpText != null)
        {
            progressHelpText.gameObject.SetActive(true);
            progressHelpText.text = "Fill the lesson bubbles";
        }
        beatInfoButton.SetActive(false);

        BuildBeatBubbles(lesson.beatsPerBar);
        BuildLessonTargetBubbles(lesson.requiredHits);
        SetProgress(currentProgress, lesson.requiredHits);
        UpdateLessonTargetBubbles(currentProgress, lesson.requiredHits, OceanRhythmHitResult.Near);

        if (animalController != null)
        {
            guidedAnimalRoot.gameObject.SetActive(true);
            animalController.SetAnimal(lesson.animalName, manager != null ? manager.GetSpriteForLesson(lesson) : null, manager != null ? manager.GetAnimationFramesForLesson(lesson) : null, ColorForAnimal(lesson.animalKey));
        }

        if (pondLayer != null)
        {
            pondLayer.gameObject.SetActive(false);
        }
        if (singingShellButton != null)
        {
            singingShellButton.SetActive(false);
        }

        completeOverlay.SetActive(false);
        if (pondCompleteOverlay != null)
        {
            pondCompleteOverlay.SetActive(false);
        }
    }

    public void PulseBeat(int beatInBar, bool accented)
    {
        for (int i = 0; i < beatBubbles.Count; i++)
        {
            Image bubble = beatBubbles[i];
            if (bubble == null)
            {
                continue;
            }

            if (i == beatInBar)
            {
                bubble.color = accented ? new Color(1f, 0.86f, 0.18f, 1f) : new Color(0.25f, 0.74f, 1f, 1f);
                bubble.transform.localScale = accented ? Vector3.one * 1.38f : Vector3.one * 1.22f;
            }
            else
            {
                bubble.color = new Color(0.78f, 0.95f, 1f, 0.46f);
                bubble.transform.localScale = Vector3.one;
            }
        }

        if (animalController != null)
        {
            animalController.Bounce(accentedScale: accented ? 1.08f : 1.04f);
        }
        PulseTapButton(accented);
    }

    public void ShowInputResult(OceanRhythmHitResult result, float timingError, int currentProgress, int requiredHits)
    {
        string text;
        Color color;
        if (result == OceanRhythmHitResult.Perfect)
        {
            text = "+1 Perfect  " + RemainingLessonText(currentProgress, requiredHits);
            color = new Color(1f, 0.86f, 0.18f);
        }
        else if (result == OceanRhythmHitResult.Good)
        {
            text = "+1 Good  " + RemainingLessonText(currentProgress, requiredHits);
            color = new Color(0.27f, 0.95f, 0.54f);
        }
        else if (result == OceanRhythmHitResult.Near)
        {
            text = "Almost. No bubble yet";
            color = new Color(0.46f, 0.82f, 1f);
        }
        else
        {
            text = "Wait for the bright bubble";
            color = new Color(1f, 0.46f, 0.42f);
        }

        feedbackText.text = text;
        feedbackText.color = color;
        feedbackText.transform.localScale = Vector3.one * 1.1f;
        SetProgress(currentProgress, requiredHits);
        UpdateLessonTargetBubbles(currentProgress, requiredHits, result);
    }

    public void MarkGuideFishCollected(int lessonIndex)
    {
        if (lessonIndex < 0 || lessonIndex >= guideCollectionIcons.Count)
        {
            return;
        }

        Image icon = guideCollectionIcons[lessonIndex];
        icon.color = new Color(1f, 0.86f, 0.18f, 1f);
        icon.transform.localScale = Vector3.one * 1.18f;
    }

    public void ShowBeatCard(OceanLesson lesson, int lessonNumber, int lessonCount, UnityEngine.Events.UnityAction onTry)
    {
        if (lesson == null || beatCardOverlay == null)
        {
            if (onTry != null)
            {
                onTry.Invoke();
            }
            return;
        }

        beatCardOverlay.SetActive(true);
        Text title = beatCardOverlay.transform.Find("Card/Title").GetComponent<Text>();
        Text pattern = beatCardOverlay.transform.Find("Card/Pattern").GetComponent<Text>();
        Text body = beatCardOverlay.transform.Find("Card/Body").GetComponent<Text>();
        Button button = beatCardOverlay.transform.Find("Card/TryButton").GetComponent<Button>();
        Text buttonText = button.GetComponentInChildren<Text>(true);

        title.text = lessonNumber > 0 ? "Rhythm Lesson  " + lessonNumber + " / " + lessonCount : lesson.meterLabel + "  " + BeatName(lesson);
        pattern.text = BeatPatternText(lesson);
        body.text = lessonNumber > 0
            ? lesson.meterLabel + "  " + BeatName(lesson) + "\n" + BeatBodyText(lesson) + "\nFill " + lesson.requiredHits + " bubbles to help " + lesson.animalName + "."
            : BeatBodyText(lesson);
        if (buttonText != null)
        {
            buttonText.text = lessonNumber > 0 ? "Try it" : "Close";
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate
        {
            beatCardOverlay.SetActive(false);
            if (onTry != null)
            {
                onTry.Invoke();
            }
        });
    }

    public void ShowFreePond(OceanLesson[] lessons, List<OceanPondAnimal> outputAnimals, int instancesPerFish)
    {
        completeOverlay.SetActive(false);
        if (pondCompleteOverlay != null)
        {
            pondCompleteOverlay.SetActive(false);
        }
        if (guidedAnimalRoot != null)
        {
            guidedAnimalRoot.gameObject.SetActive(false);
        }

        pondLayer.gameObject.SetActive(true);
        ClearPond();
        keyboardSelectedIndex = 0;
        currentFreePondSelection = null;

        titleText.text = "Pick a fish";
        showingGuidedLesson = false;
        SetMainTextVisible(false);
        if (topBarObject != null)
        {
            topBarObject.SetActive(false);
        }
        if (tapButtonObject != null)
        {
            tapButtonObject.SetActive(true);
        }
        SetBucketButtonVisible(true);
        SetNavigationVisible(true);
        subtitleText.text = "Move the net";
        instructionText.text = "Move the net to a fish. Tap the bright bubble.";
        feedbackText.text = "Pick a fish";
        feedbackText.color = new Color(1f, 0.95f, 0.68f);
        if (learningModeText != null)
        {
            learningModeText.gameObject.SetActive(false);
        }
        if (lessonCounterText != null)
        {
            lessonCounterText.gameObject.SetActive(false);
        }
        if (lessonGoalText != null)
        {
            lessonGoalText.gameObject.SetActive(false);
        }
        if (progressHelpText != null)
        {
            progressHelpText.gameObject.SetActive(false);
        }
        SetProgress(0, 1);
        BuildBeatBubbles(0);
        BuildLessonTargetBubbles(0);
        beatInfoButton.SetActive(true);

        Vector2[] positions = new Vector2[]
        {
            new Vector2(-440f, 88f),
            new Vector2(-320f, -84f),
            new Vector2(-110f, 32f),
            new Vector2(80f, -92f),
            new Vector2(260f, 82f),
            new Vector2(410f, -42f),
            new Vector2(-18f, 128f),
            new Vector2(470f, 118f)
        };

        int positionIndex = 0;
        int countPerFish = Mathf.Max(2, instancesPerFish);
        for (int i = 0; i < lessons.Length; i++)
        {
            OceanLesson lesson = lessons[i];
            for (int j = 0; j < countPerFish; j++)
            {
                Vector2 position = positions[positionIndex % positions.Length];
                positionIndex++;
                GameObject obj = CreateRect(pondLayer, "Pond_" + lesson.animalKey + "_" + j, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(300f, 300f)).gameObject;
                OceanPondAnimal animal = obj.AddComponent<OceanPondAnimal>();
                animal.Build(lesson, manager != null ? manager.GetSpriteForLesson(lesson) : null, manager != null ? manager.GetAnimationFramesForLesson(lesson) : null, circleSprite, uiFont, ColorForAnimal(lesson.animalKey), position, lesson.animalKey + "_" + j);
                pondAnimals.Add(animal);
                if (outputAnimals != null)
                {
                    outputAnimals.Add(animal);
                }
            }
        }

        if (netCursor != null)
        {
            netCursor.SetCaptureRatio(0f);
            netCursor.transform.SetAsLastSibling();
        }

        ApplyPendingImageOverrides();
    }

    public OceanPondAnimal UpdateFreePondSelection()
    {
        if (pondLayer == null || !pondLayer.gameObject.activeSelf || pondAnimals.Count == 0)
        {
            return null;
        }

        if (currentFreePondSelection != null && currentFreePondSelection.IsCaptured)
        {
            return ApplyFreePondSelection(null);
        }

        OceanPondAnimal hovered = FindClosestFishToMouse(165f);
        for (int i = 0; i < pondAnimals.Count; i++)
        {
            if (pondAnimals[i] != null)
            {
                pondAnimals[i].SetHovered(pondAnimals[i] == hovered);
            }
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            keyboardSelectedIndex = FindNextAvailableIndex(1);
            keyboardSelectionHoldUntil = Time.unscaledTime + 3f;
            return ApplyFreePondSelection(pondAnimals[keyboardSelectedIndex]);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            keyboardSelectedIndex = FindNextAvailableIndex(-1);
            keyboardSelectionHoldUntil = Time.unscaledTime + 3f;
            return ApplyFreePondSelection(pondAnimals[keyboardSelectedIndex]);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (hovered != null)
            {
                keyboardSelectedIndex = pondAnimals.IndexOf(hovered);
                keyboardSelectionHoldUntil = 0f;
                return ApplyFreePondSelection(hovered);
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return currentFreePondSelection;
            }

            return ApplyFreePondSelection(null);
        }

        if (Time.unscaledTime < keyboardSelectionHoldUntil && keyboardSelectedIndex >= 0 && keyboardSelectedIndex < pondAnimals.Count)
        {
            OceanPondAnimal keyboardAnimal = pondAnimals[keyboardSelectedIndex];
            if (keyboardAnimal != null && !keyboardAnimal.IsCaptured)
            {
                return ApplyFreePondSelection(keyboardAnimal);
            }
        }

        return currentFreePondSelection;
    }

    private OceanPondAnimal FindClosestFishToMouse(float maxDistance)
    {
        Vector2 localMouse;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(pondLayer, Input.mousePosition, null, out localMouse);
        OceanPondAnimal closest = null;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < pondAnimals.Count; i++)
        {
            OceanPondAnimal animal = pondAnimals[i];
            if (animal == null)
            {
                continue;
            }

            if (animal.IsCaptured)
            {
                continue;
            }

            float distance = Vector2.Distance(localMouse, animal.AnchoredPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = animal;
                keyboardSelectedIndex = i;
            }
        }

        if (closestDistance > 165f)
        {
            return null;
        }

        return closestDistance <= maxDistance ? closest : null;
    }

    public void ShowFreePondSelection(OceanPondAnimal animal)
    {
        if (animal == null)
        {
            ShowNoFishSelected();
            return;
        }

        OceanLesson lesson = animal.Lesson;
        titleText.text = animal.IsMystery ? "Mystery Fish" : lesson.animalName;
        subtitleText.text = "Bright bubble";
        instructionText.text = "Stay near the fish. Tap Space when the bubble lights up.";
        feedbackText.text = animal.RemainingHitsText;
        feedbackText.color = new Color(1f, 0.95f, 0.68f);
        BuildBeatBubbles(lesson.beatsPerBar);
        SetProgress(animal.CaptureProgress, animal.RequiredHits);
        if (netCursor != null)
        {
            netCursor.SetCaptureRatio(animal.CaptureRatio);
        }
        beatInfoButton.SetActive(true);
    }

    public void ShowNoFishSelected()
    {
        if (pondLayer == null || !pondLayer.gameObject.activeSelf)
        {
            return;
        }

        titleText.text = "Pick a fish";
        subtitleText.text = "Move the net";
        instructionText.text = "Move the net to a fish. Tap the bright bubble.";
        feedbackText.text = "Pick a fish";
        feedbackText.color = new Color(1f, 0.95f, 0.68f);
        SetProgress(0, 1);
        beatInfoButton.SetActive(true);
        if (netCursor != null)
        {
            netCursor.SetCaptureRatio(0f);
        }
    }

    public void ShowFreePondInputResult(OceanPondAnimal animal, OceanRhythmHitResult result, float timingError)
    {
        if (animal == null)
        {
            ShowNoFishSelected();
            return;
        }

        string text;
        Color color;
        if (result == OceanRhythmHitResult.Perfect)
        {
            text = "Perfect  " + animal.RemainingHitsText;
            color = new Color(1f, 0.86f, 0.18f);
        }
        else if (result == OceanRhythmHitResult.Good)
        {
            text = "Good  " + animal.RemainingHitsText;
            color = new Color(0.27f, 0.95f, 0.54f);
        }
        else if (result == OceanRhythmHitResult.Near)
        {
            text = "Try again";
            color = new Color(0.46f, 0.82f, 1f);
        }
        else
        {
            text = "Tap the bright bubble";
            color = new Color(1f, 0.46f, 0.42f);
        }

        feedbackText.text = text;
        feedbackText.color = color;
        SetProgress(animal.CaptureProgress, animal.RequiredHits);
        if (netCursor != null)
        {
            netCursor.SetCaptureRatio(animal.CaptureRatio);
            netCursor.Pulse(result);
        }
    }

    public OceanPondAnimal SpawnMysteryFish(OceanLesson lesson, Sprite mysterySprite, string instanceId)
    {
        if (pondLayer == null || !pondLayer.gameObject.activeSelf || lesson == null)
        {
            return null;
        }

        Vector2 position = new Vector2(Random.Range(-430f, 430f), Random.Range(-90f, 135f));
        GameObject obj = CreateRect(pondLayer, instanceId, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(300f, 300f)).gameObject;
        OceanPondAnimal animal = obj.AddComponent<OceanPondAnimal>();
        animal.Build(lesson, mysterySprite, circleSprite, uiFont, new Color(0.9f, 0.78f, 1f), position, instanceId);
        pondAnimals.Add(animal);
        obj.transform.SetSiblingIndex(Mathf.Max(0, pondLayer.childCount - 2));
        ShowRewardText("★");
        return animal;
    }

    public void UpdateBucket(OceanBucketInventory inventory)
    {
        if (inventory == null)
        {
            return;
        }

        if (bucketCountText != null)
        {
            bucketCountText.text = inventory.GetTotalCatchCount().ToString();
        }
        if (bucketShellText != null)
        {
            bucketShellText.text = inventory.Shells + " shells";
        }
        if (bucketPearlText != null)
        {
            bucketPearlText.text = inventory.MusicPearls + " pearls";
        }
        if (bucketDecorationImage != null && manager != null)
        {
            Sprite decoration = manager.GetDecorationSprite(inventory.SelectedDecoration);
            bucketDecorationImage.sprite = decoration != null ? decoration : circleSprite;
            bucketDecorationImage.color = ColorForDecoration(inventory.SelectedDecoration);
        }
    }

    public void ShowCatchReward(OceanPondAnimal animal, OceanBucketInventory inventory)
    {
        UpdateBucket(inventory);
        string reward = animal != null && animal.IsMystery ? "★ ★ ★" : "★";
        ShowRewardText(reward);
        RefreshBucketWorkshop(inventory);
    }

    public void SetSingingShellAvailable(bool available)
    {
        if (singingShellButton == null)
        {
            return;
        }

        singingShellButton.SetActive(false);
        if (singingShellButtonImage != null)
        {
            singingShellButtonImage.color = available ? new Color(1f, 0.88f, 0.22f, 1f) : new Color(0.7f, 0.92f, 1f, 0.9f);
        }
        if (singingShellButtonText != null)
        {
            singingShellButtonText.text = available ? "?" : "";
        }
    }

    public void ShowSoundMatch(OceanLesson targetLesson, OceanLesson[] allLessons, bool retry)
    {
        if (soundMatchOverlay == null || targetLesson == null || allLessons == null)
        {
            return;
        }

        soundMatchOverlay.SetActive(true);
        soundMatchTitleText.text = "Listen to the shell";
        soundMatchBodyText.text = retry ? "Try again. Which friend sings this beat?" : "Which friend sings this beat?";
        soundMatchResultText.text = "Listen...";
        soundMatchResultText.color = new Color(1f, 0.94f, 0.68f);
        BuildSoundMatchBubbles(targetLesson.beatsPerBar);

        Transform optionsRoot = soundMatchOverlay.transform.Find("Card/Options");
        if (optionsRoot != null)
        {
            for (int i = optionsRoot.childCount - 1; i >= 0; i--)
            {
                DestroyObject(optionsRoot.GetChild(i).gameObject);
            }

            List<OceanLesson> options = BuildSoundMatchOptions(targetLesson, allLessons);
            for (int i = 0; i < options.Count; i++)
            {
                OceanLesson option = options[i];
                Button button = CreateButton(optionsRoot, "Option_" + option.animalKey, option.animalName + "\n" + option.meterLabel, Vector2.zero, Vector2.zero, new Vector2(170f, 116f), delegate
                {
                    if (manager != null)
                    {
                        manager.ChooseSoundMatch(option.fishType);
                    }
                });
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = ColorForAnimal(option.animalKey);
                }
                LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
                layout.minWidth = 170f;
                layout.minHeight = 116f;
                layout.preferredWidth = 170f;
                layout.preferredHeight = 116f;
            }
        }

        OceanBucketInventory inventory = manager != null ? manager.GetBucketInventory() : null;
        if (soundMatchPearlText != null && inventory != null)
        {
            soundMatchPearlText.text = inventory.MusicPearls + " music pearls";
        }
    }

    public void PulseSoundMatchBeat(int beatInBar, bool accented)
    {
        for (int i = 0; i < soundMatchBubbles.Count; i++)
        {
            Image bubble = soundMatchBubbles[i];
            if (bubble == null)
            {
                continue;
            }

            if (i == beatInBar)
            {
                bubble.color = accented ? new Color(1f, 0.86f, 0.18f, 1f) : new Color(0.25f, 0.74f, 1f, 1f);
                bubble.transform.localScale = accented ? Vector3.one * 1.26f : Vector3.one * 1.14f;
            }
            else
            {
                bubble.color = new Color(0.78f, 0.95f, 1f, 0.4f);
                bubble.transform.localScale = Vector3.one;
            }
        }
    }

    public void SetSoundMatchChoosing()
    {
        if (soundMatchResultText != null)
        {
            soundMatchResultText.text = "Choose the rhythm friend";
            soundMatchResultText.color = Color.white;
        }
    }

    public void ShowSoundMatchResult(bool correct, OceanLesson targetLesson, OceanBucketInventory inventory)
    {
        if (soundMatchResultText == null)
        {
            return;
        }

        if (correct)
        {
            soundMatchResultText.text = "+1 Music Pearl! Now catch the glowing friend.";
            soundMatchResultText.color = new Color(1f, 0.86f, 0.18f);
            ShowRewardText("+1 music pearl");
            Invoke("HideSoundMatchOverlay", 1.15f);
        }
        else
        {
            soundMatchResultText.text = "Listen again";
            soundMatchResultText.color = new Color(0.46f, 0.82f, 1f);
        }

        if (soundMatchPearlText != null && inventory != null)
        {
            soundMatchPearlText.text = inventory.MusicPearls + " music pearls";
        }
        RefreshBucketWorkshop(inventory);
    }

    public void MarkFreePondFishCollected(OceanLesson lesson)
    {
        if (lesson == null)
        {
            return;
        }

        for (int i = 0; i < guideCollectionIcons.Count; i++)
        {
            if (i < pondAnimals.Count && pondAnimals[i] != null && pondAnimals[i].Lesson == lesson)
            {
                guideCollectionIcons[i].color = new Color(0.27f, 0.95f, 0.54f, 1f);
                guideCollectionIcons[i].transform.localScale = Vector3.one * 1.28f;
            }
        }
    }

    public void ShowFreePondComplete()
    {
        titleText.text = "All fish are home!";
        subtitleText.text = "Great!";
        instructionText.text = "All fish came home.";
        feedbackText.text = "Play again?";
        feedbackText.color = new Color(1f, 0.86f, 0.18f);
        BuildBeatBubbles(0);
        SetProgress(1, 1);

        for (int i = 0; i < pondAnimals.Count; i++)
        {
            if (pondAnimals[i] != null)
            {
                pondAnimals[i].SetSelected(false);
            }
        }

        if (netCursor != null)
        {
            netCursor.SetCaptureRatio(1f);
        }

        if (pondCompleteOverlay != null)
        {
            pondCompleteOverlay.SetActive(true);
        }
    }

    private int FindNextAvailableIndex(int direction)
    {
        if (pondAnimals.Count == 0)
        {
            return 0;
        }

        int index = keyboardSelectedIndex;
        for (int i = 0; i < pondAnimals.Count; i++)
        {
            index = (index + direction + pondAnimals.Count) % pondAnimals.Count;
            if (pondAnimals[index] != null && !pondAnimals[index].IsCaptured)
            {
                return index;
            }
        }

        return Mathf.Clamp(keyboardSelectedIndex, 0, pondAnimals.Count - 1);
    }

    public void ShowLessonComplete(OceanLesson lesson, bool finalLesson)
    {
        if (animalController != null)
        {
            animalController.Capture();
        }

        completeOverlay.SetActive(true);
        Text message = completeOverlay.transform.Find("Card/Message").GetComponent<Text>();
        message.text = finalLesson ? "Free Pond unlocked!" : "Lesson Complete";

        Text detail = completeOverlay.transform.Find("Card/Detail").GetComponent<Text>();
        detail.text = finalLesson ? "All rhythm friends are ready. Opening the free pond." : lesson.animalName + " learned " + lesson.meterLabel + ".";
    }

    private void SetProgress(int currentProgress, int requiredHits)
    {
        float ratio = requiredHits <= 0 ? 0f : Mathf.Clamp01((float)currentProgress / requiredHits);
        if (progressFill != null)
        {
            progressFill.fillAmount = ratio;
        }
        if (progressText != null)
        {
            progressText.text = showingGuidedLesson ? "Good taps: " + currentProgress + " / " + requiredHits : currentProgress + " / " + requiredHits;
        }
    }

    private void SetMainTextVisible(bool visible)
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(visible);
        }
        if (subtitleText != null)
        {
            subtitleText.gameObject.SetActive(visible);
        }
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(visible);
        }
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(visible);
        }
        if (progressText != null)
        {
            progressText.gameObject.SetActive(visible);
        }
    }

    private void SetNavigationVisible(bool visible)
    {
        if (backButtonObject != null)
        {
            backButtonObject.SetActive(visible);
        }
        if (retryButtonObject != null)
        {
            retryButtonObject.SetActive(visible);
        }
        if (pauseButtonObject != null)
        {
            pauseButtonObject.SetActive(visible);
        }
    }

    private void SetBucketButtonVisible(bool visible)
    {
        if (bucketButtonObject != null)
        {
            bucketButtonObject.SetActive(visible);
        }
        if (bucketCountText != null)
        {
            bucketCountText.gameObject.SetActive(false);
        }
        if (bucketShellText != null)
        {
            bucketShellText.gameObject.SetActive(false);
        }
        if (bucketPearlText != null)
        {
            bucketPearlText.gameObject.SetActive(false);
        }
    }

    private void PulseTapButton(bool accented)
    {
        tapPulseScale = accented ? 1.18f : 1.08f;
        if (tapButtonImage != null)
        {
            tapButtonImage.color = accented ? new Color(1f, 0.88f, 0.18f, 1f) : new Color(0.28f, 0.78f, 1f, 0.96f);
        }
    }

    public void SetPauseState(bool paused)
    {
        if (pauseButtonText != null)
        {
            pauseButtonText.text = paused ? ">" : "||";
        }
    }

    public Vector2 GetBucketDropPositionInPond()
    {
        if (pondLayer == null || bucketButtonObject == null)
        {
            return Vector2.zero;
        }

        RectTransform bucketRect = bucketButtonObject.GetComponent<RectTransform>();
        if (bucketRect == null)
        {
            return Vector2.zero;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, bucketRect.position);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(pondLayer, screenPoint, null, out localPoint);
        return localPoint;
    }

    private void BuildLessonTargetBubbles(int requiredHits)
    {
        if (lessonTargetBubbleRoot == null)
        {
            return;
        }

        for (int i = lessonTargetBubbleRoot.childCount - 1; i >= 0; i--)
        {
            DestroyObject(lessonTargetBubbleRoot.GetChild(i).gameObject);
        }
        lessonTargetBubbles.Clear();

        if (requiredHits <= 1)
        {
            lessonTargetBubbleRoot.gameObject.SetActive(false);
            return;
        }

        lessonTargetBubbleRoot.gameObject.SetActive(true);
        for (int i = 0; i < requiredHits; i++)
        {
            GameObject bubble = CreateRect(lessonTargetBubbleRoot, "LessonTarget_" + (i + 1), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24f, 24f)).gameObject;
            Image image = bubble.AddComponent<Image>();
            image.sprite = circleSprite;
            image.color = new Color(0.78f, 0.95f, 1f, 0.26f);
            image.preserveAspect = true;
            LayoutElement layout = bubble.AddComponent<LayoutElement>();
            layout.minWidth = 24f;
            layout.minHeight = 24f;
            lessonTargetBubbles.Add(image);
        }
    }

    private void UpdateLessonTargetBubbles(int currentProgress, int requiredHits, OceanRhythmHitResult result)
    {
        if (lessonTargetBubbles.Count == 0)
        {
            return;
        }

        for (int i = 0; i < lessonTargetBubbles.Count; i++)
        {
            Image bubble = lessonTargetBubbles[i];
            if (bubble == null)
            {
                continue;
            }

            if (i < currentProgress)
            {
                bubble.color = result == OceanRhythmHitResult.Perfect ? new Color(1f, 0.86f, 0.18f, 1f) : new Color(0.27f, 0.95f, 0.54f, 1f);
                bubble.transform.localScale = i == currentProgress - 1 ? Vector3.one * 1.18f : Vector3.one;
            }
            else
            {
                bubble.color = result == OceanRhythmHitResult.Miss && i == currentProgress ? new Color(1f, 0.46f, 0.42f, 0.45f) : new Color(0.78f, 0.95f, 1f, 0.26f);
                bubble.transform.localScale = Vector3.one;
            }
        }

        if (progressHelpText != null)
        {
            progressHelpText.text = RemainingLessonText(currentProgress, requiredHits);
        }
    }

    private string RemainingLessonText(int currentProgress, int requiredHits)
    {
        int remaining = Mathf.Max(0, requiredHits - currentProgress);
        if (remaining == 0)
        {
            return "Lesson complete";
        }
        if (remaining == 1)
        {
            return "1 more";
        }
        return remaining + " more";
    }

    private void BuildBeatBubbles(int count)
    {
        if (beatBubbleRoot == null)
        {
            return;
        }

        for (int i = beatBubbleRoot.childCount - 1; i >= 0; i--)
        {
            DestroyObject(beatBubbleRoot.GetChild(i).gameObject);
        }
        beatBubbles.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject bubble = CreateRect(beatBubbleRoot, "BeatBubble_" + (i + 1), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(58f, 58f)).gameObject;
            Image image = bubble.AddComponent<Image>();
            image.sprite = circleSprite;
            image.color = new Color(0.78f, 0.95f, 1f, 0.46f);
            image.preserveAspect = true;

            if (showingGuidedLesson)
            {
                Text number = CreateText(bubble.transform, "Number", (i + 1).ToString(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 50f), 23, FontStyle.Bold, new Color(0.02f, 0.16f, 0.24f), TextAnchor.MiddleCenter);
                number.raycastTarget = false;
            }

            LayoutElement layout = bubble.AddComponent<LayoutElement>();
            layout.minWidth = 58f;
            layout.minHeight = 58f;
            beatBubbles.Add(image);
        }
    }

    private void CreateAnimatedWater(Transform parent)
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject band = CreateRect(parent, "SoftWave_" + i, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f + i * 92f), new Vector2(1480f, 58f)).gameObject;
            Image image = band.AddComponent<Image>();
            image.color = i % 2 == 0 ? new Color(0.24f, 0.86f, 0.95f, 0.14f) : new Color(1f, 1f, 1f, 0.08f);
        }
    }

    private void CreateTopBar(Transform parent)
    {
        GameObject top = CreatePanel(parent, "TopBar", new Color(0.02f, 0.12f, 0.2f, 0.74f));
        topBarObject = top;
        RectTransform topRect = top.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -58f);
        topRect.sizeDelta = new Vector2(1080f, 84f);

        learningModeText = CreateText(top.transform, "LearningMode", "RHYTHM LESSON", new Vector2(0f, 0.5f), new Vector2(96f, 0f), new Vector2(190f, 42f), 22, FontStyle.Bold, new Color(1f, 0.86f, 0.18f), TextAnchor.MiddleCenter);
        titleText = CreateText(top.transform, "AnimalTitle", "Little Fish", new Vector2(0.5f, 0.62f), new Vector2(0f, 0f), new Vector2(620f, 44f), 34, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        subtitleText = CreateText(top.transform, "Meter", "4/4  76 BPM", new Vector2(0.5f, 0.2f), new Vector2(0f, 0f), new Vector2(480f, 30f), 22, FontStyle.Bold, new Color(1f, 0.86f, 0.24f), TextAnchor.MiddleCenter);
        lessonCounterText = CreateText(top.transform, "LessonCounter", "1 / 4", new Vector2(1f, 0.5f), new Vector2(-66f, 0f), new Vector2(120f, 40f), 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    private void CreateCenterStage(Transform parent)
    {
        GameObject animal = CreateRect(parent, "OceanAnimal", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(0f, 4f), new Vector2(300f, 300f)).gameObject;
        guidedAnimalRoot = animal.GetComponent<RectTransform>();
        animalController = animal.AddComponent<OceanAnimalController>();
        animalController.Build(circleSprite, uiFont);
    }

    private void CreateGuideCollection(Transform parent)
    {
        GameObject row = CreateRect(parent, "FishCollection", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(360f, 54f)).gameObject;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;

        guideCollectionIcons.Clear();
        for (int i = 0; i < 4; i++)
        {
            GameObject icon = CreateRect(row.transform, "CollectionIcon_" + i, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(42f, 42f)).gameObject;
            Image image = icon.AddComponent<Image>();
            image.sprite = circleSprite;
            image.preserveAspect = true;
            image.color = new Color(0.78f, 0.95f, 1f, 0.28f);
            LayoutElement element = icon.AddComponent<LayoutElement>();
            element.minWidth = 42f;
            element.minHeight = 42f;
            guideCollectionIcons.Add(image);
        }
    }

    private void CreatePondLayer(Transform parent)
    {
        pondLayer = CreateRect(parent, "FreePondLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject cursorObject = CreateRect(pondLayer, "NetCursor", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 190f)).gameObject;
        netCursor = cursorObject.AddComponent<OceanNetCursor>();
        netCursor.Build(circleSprite, manager != null ? manager.GetNetSprite() : null);
    }

    private void CreateBottomHud(Transform parent)
    {
        GameObject card = CreatePanel(parent, "BottomHud", new Color(0.02f, 0.12f, 0.2f, 0.76f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0f);
        cardRect.anchorMax = new Vector2(0.5f, 0f);
        cardRect.anchoredPosition = new Vector2(0f, 118f);
        cardRect.sizeDelta = new Vector2(980f, 214f);

        instructionText = CreateText(card.transform, "Instruction", "", new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(890f, 54f), 24, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);

        GameObject beatRow = CreateRect(card.transform, "BeatBubbleRow", new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(560f, 58f)).gameObject;
        HorizontalLayoutGroup beatLayout = beatRow.AddComponent<HorizontalLayoutGroup>();
        beatLayout.spacing = 14;
        beatLayout.childAlignment = TextAnchor.MiddleCenter;
        beatLayout.childControlWidth = true;
        beatLayout.childControlHeight = true;
        beatLayout.childForceExpandWidth = false;
        beatBubbleRoot = beatRow.transform;

        feedbackText = CreateText(card.transform, "Feedback", "Listen first", new Vector2(0.5f, 0.34f), Vector2.zero, new Vector2(520f, 38f), 28, FontStyle.Bold, new Color(1f, 0.95f, 0.68f), TextAnchor.MiddleCenter);
        lessonGoalText = CreateText(card.transform, "LessonGoal", "Need 12 bright-bubble taps", new Vector2(0.18f, 0.34f), Vector2.zero, new Vector2(240f, 38f), 20, FontStyle.Bold, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);
        progressHelpText = CreateText(card.transform, "ProgressHelp", "Fill the lesson bubbles", new Vector2(0.82f, 0.34f), Vector2.zero, new Vector2(240f, 38f), 20, FontStyle.Bold, new Color(0.78f, 0.95f, 1f), TextAnchor.MiddleCenter);

        GameObject progress = CreatePanel(card.transform, "ProgressBar", new Color(1f, 1f, 1f, 0.2f));
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0.11f);
        progressRect.anchorMax = new Vector2(0.5f, 0.11f);
        progressRect.anchoredPosition = Vector2.zero;
        progressRect.sizeDelta = new Vector2(640f, 22f);

        GameObject fill = CreateRect(progress.transform, "Fill", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero).gameObject;
        progressFill = fill.AddComponent<Image>();
        progressFill.color = new Color(0.27f, 0.95f, 0.54f, 1f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;

        progressText = CreateText(card.transform, "ProgressText", "Good taps: 0 / 12", new Vector2(0.5f, 0.11f), new Vector2(0f, 28f), new Vector2(240f, 28f), 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        GameObject targetRow = CreateRect(card.transform, "LessonTargetBubbleRow", new Vector2(0.5f, 0.02f), new Vector2(0.5f, 0.02f), new Vector2(0f, 8f), new Vector2(760f, 34f)).gameObject;
        HorizontalLayoutGroup targetLayout = targetRow.AddComponent<HorizontalLayoutGroup>();
        targetLayout.spacing = 6;
        targetLayout.childAlignment = TextAnchor.MiddleCenter;
        targetLayout.childControlWidth = true;
        targetLayout.childControlHeight = true;
        targetLayout.childForceExpandWidth = false;
        lessonTargetBubbleRoot = targetRow.transform;
    }

    private void CreateBeatInfoButton(Transform parent)
    {
        Button button = CreateButton(parent, "ParentHelpButton", "?", new Vector2(1f, 1f), new Vector2(-62f, -56f), new Vector2(58f, 58f), delegate
        {
            if (parentHelpOverlay != null)
            {
                parentHelpOverlay.SetActive(true);
            }
        });
        beatInfoButton = button.gameObject;
    }

    private void CreateTapButton(Transform parent)
    {
        Button button = CreateButton(parent, "TapButton", "TAP", new Vector2(1f, 0f), new Vector2(-180f, 78f), new Vector2(176f, 96f), delegate
        {
            if (manager != null)
            {
                manager.TryTapInput();
            }
        });

        tapButtonObject = button.gameObject;
        tapButtonImage = tapButtonObject.GetComponent<Image>();
        tapButtonText = tapButtonObject.GetComponentInChildren<Text>(true);
        if (tapButtonImage != null)
        {
            tapButtonImage.color = new Color(1f, 0.86f, 0.18f, 0.96f);
        }
        if (tapButtonText != null)
        {
            tapButtonText.fontSize = 34;
            tapButtonText.resizeTextForBestFit = true;
            tapButtonText.resizeTextMinSize = 24;
            tapButtonText.resizeTextMaxSize = 34;
        }
    }

    private void CreateParentHelpOverlay(Transform parent)
    {
        parentHelpOverlay = CreateRect(parent, "ParentHelpOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = parentHelpOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.07f, 0.1f, 0.72f);

        GameObject card = CreatePanel(parentHelpOverlay.transform, "Card", new Color(0.05f, 0.36f, 0.46f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(760f, 430f);

        CreateText(card.transform, "Title", "For parents", new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(660f, 58f), 40, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateText(card.transform, "Body", "Children do not need to read rules.\n\n1. Click a fish.\n2. Watch the bright beat bubble.\n3. Click TAP or press Space on the beat.\n\nThree good taps catch the fish. Misses only give visual feedback; there is no losing.", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 210f), 25, FontStyle.Normal, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);
        CreateButton(card.transform, "CloseButton", "Close", new Vector2(0.35f, 0.15f), Vector2.zero, new Vector2(180f, 56f), delegate { parentHelpOverlay.SetActive(false); });
        CreateButton(card.transform, "BackButton", "Back", new Vector2(0.65f, 0.15f), Vector2.zero, new Vector2(180f, 56f), delegate
        {
            if (manager != null)
            {
                manager.ReturnToStart();
            }
        });
    }

    private void CreateCompleteOverlay(Transform parent)
    {
        completeOverlay = CreateRect(parent, "CompleteOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = completeOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.07f, 0.1f, 0.72f);

        GameObject card = CreatePanel(completeOverlay.transform, "Card", new Color(0.06f, 0.42f, 0.5f, 0.96f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(660f, 260f);

        CreateText(card.transform, "Message", "Rescued!", new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(580f, 70f), 42, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateText(card.transform, "Detail", "Next rhythm friend is coming.", new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(580f, 54f), 26, FontStyle.Normal, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);
    }

    private void CreatePondCompleteOverlay(Transform parent)
    {
        pondCompleteOverlay = CreateRect(parent, "PondCompleteOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = pondCompleteOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.07f, 0.1f, 0.66f);

        GameObject card = CreatePanel(pondCompleteOverlay.transform, "Card", new Color(0.05f, 0.36f, 0.46f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(760f, 330f);

        CreateText(card.transform, "Message", "All fish are ready!", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(650f, 64f), 44, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateText(card.transform, "Detail", "Play again in the pond, or go back to choose another mode.", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(650f, 76f), 27, FontStyle.Normal, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);

        CreateButton(card.transform, "PlayAgainButton", "Play Again", new Vector2(0.5f, 0.25f), new Vector2(-126f, 0f), new Vector2(210f, 58f), delegate
        {
            pondCompleteOverlay.SetActive(false);
            if (manager != null)
            {
                manager.RestartFreePond();
            }
        });

        CreateButton(card.transform, "BackToStartButton", "Back", new Vector2(0.5f, 0.25f), new Vector2(126f, 0f), new Vector2(210f, 58f), delegate
        {
            if (manager != null)
            {
                manager.ReturnToStart();
            }
        });
    }

    private void CreateBeatCardOverlay(Transform parent)
    {
        beatCardOverlay = CreateRect(parent, "BeatCardOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = beatCardOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.07f, 0.1f, 0.68f);

        GameObject card = CreatePanel(beatCardOverlay.transform, "Card", new Color(0.05f, 0.36f, 0.46f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(780f, 420f);

        CreateText(card.transform, "Title", "4/4 Walking Beat", new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(680f, 62f), 42, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateText(card.transform, "Pattern", "STRONG soft soft soft", new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(690f, 58f), 30, FontStyle.Bold, new Color(1f, 0.86f, 0.18f), TextAnchor.MiddleCenter);
        CreateText(card.transform, "Body", "Tap with the bright bubbles.", new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(680f, 88f), 26, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
        CreateButton(card.transform, "TryButton", "Try it", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(220f, 64f), delegate { });
    }

    private void CreateBucketUi(Transform parent)
    {
        GameObject bucket = CreatePanel(parent, "CatchBucketButton", new Color(1f, 1f, 1f, 0.82f));
        bucketButtonObject = bucket;
        RectTransform rect = bucket.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(92f, 78f);
        rect.sizeDelta = new Vector2(112f, 92f);

        Button button = bucket.AddComponent<Button>();
        button.targetGraphic = bucket.GetComponent<Image>();
        button.onClick.AddListener(delegate
        {
            if (bucketAlbumOverlay != null)
            {
                RefreshBucketWorkshop(manager != null ? manager.GetBucketInventory() : null);
                bucketAlbumOverlay.SetActive(true);
            }
        });

        Image bucketImage = CreateRect(bucket.transform, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(74f, 74f)).gameObject.AddComponent<Image>();
        bucketImage.sprite = manager != null && manager.GetBucketSprite() != null ? manager.GetBucketSprite() : circleSprite;
        bucketImage.color = new Color(0.95f, 0.62f, 0.22f, 1f);
        bucketImage.preserveAspect = true;

        bucketDecorationImage = CreateRect(bucket.transform, "Decoration", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(32f, 32f)).gameObject.AddComponent<Image>();
        bucketDecorationImage.sprite = circleSprite;
        bucketDecorationImage.color = new Color(0.36f, 0.88f, 0.48f, 1f);
        bucketDecorationImage.raycastTarget = false;

        bucketCountText = CreateText(bucket.transform, "Count", "0", new Vector2(0.62f, 0.62f), Vector2.zero, new Vector2(70f, 36f), 30, FontStyle.Bold, new Color(0.02f, 0.15f, 0.22f), TextAnchor.MiddleCenter);
        bucketShellText = CreateText(bucket.transform, "Shells", "0", new Vector2(0.62f, 0.28f), Vector2.zero, new Vector2(90f, 28f), 20, FontStyle.Bold, new Color(0.02f, 0.15f, 0.22f), TextAnchor.MiddleCenter);
        bucketPearlText = CreateText(bucket.transform, "MusicPearls", "0 pearls", new Vector2(0.76f, 0.1f), Vector2.zero, new Vector2(110f, 22f), 15, FontStyle.Bold, new Color(0.12f, 0.28f, 0.48f), TextAnchor.MiddleCenter);
        SetBucketButtonVisible(false);
    }

    private void CreateBucketAlbum(Transform parent)
    {
        bucketAlbumOverlay = CreateRect(parent, "BucketAlbumOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = bucketAlbumOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.07f, 0.1f, 0.72f);

        GameObject card = CreatePanel(bucketAlbumOverlay.transform, "Card", new Color(0.05f, 0.34f, 0.48f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1080f, 620f);

        CreateText(card.transform, "Title", "My Rhythm Bucket", new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(850f, 54f), 42, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateBucketPreview(card.transform);
        CreateDecorationLibrary(card.transform);
        bucketHintText = CreateText(card.transform, "Hint", "Tap a locked decoration to see how to unlock it.", new Vector2(0.78f, 0.52f), Vector2.zero, new Vector2(290f, 300f), 25, FontStyle.Bold, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);
        CreateButton(card.transform, "CloseButton", "Close", new Vector2(0.5f, 0.075f), Vector2.zero, new Vector2(200f, 56f), delegate { bucketAlbumOverlay.SetActive(false); });
    }

    private void CreateRewardToast(Transform parent)
    {
        rewardText = CreateText(parent, "RewardToast", "", new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(620f, 54f), 30, FontStyle.Bold, new Color(1f, 0.86f, 0.18f), TextAnchor.MiddleCenter);
        rewardText.gameObject.SetActive(false);
    }

    private void CreateSingingShellButton(Transform parent)
    {
        GameObject shell = CreatePanel(parent, "SingingShellButton", new Color(1f, 1f, 1f, 0.92f));
        RectTransform rect = shell.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(140f, 82f);
        rect.sizeDelta = new Vector2(210f, 92f);

        Button button = shell.AddComponent<Button>();
        button.targetGraphic = shell.GetComponent<Image>();
        button.onClick.AddListener(delegate
        {
            if (manager != null)
            {
                manager.StartSingingShellGame();
            }
        });

        singingShellButtonImage = CreateRect(shell.transform, "ShellIcon", new Vector2(0.24f, 0.5f), new Vector2(0.24f, 0.5f), Vector2.zero, new Vector2(62f, 62f)).gameObject.AddComponent<Image>();
        singingShellButtonImage.sprite = manager != null && manager.GetSingingShellSprite() != null ? manager.GetSingingShellSprite() : circleSprite;
        singingShellButtonImage.color = new Color(0.7f, 0.92f, 1f, 0.9f);
        singingShellButtonImage.preserveAspect = true;

        singingShellButtonText = CreateText(shell.transform, "Text", "Listen Game", new Vector2(0.66f, 0.5f), Vector2.zero, new Vector2(118f, 52f), 20, FontStyle.Bold, new Color(0.02f, 0.15f, 0.22f), TextAnchor.MiddleCenter);
        singingShellButton = shell;
    }

    private void CreateSoundMatchOverlay(Transform parent)
    {
        soundMatchOverlay = CreateRect(parent, "SoundMatchOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = soundMatchOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.07f, 0.1f, 0.7f);

        GameObject card = CreatePanel(soundMatchOverlay.transform, "Card", new Color(0.06f, 0.38f, 0.55f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(840f, 520f);

        Image shellIcon = CreateRect(card.transform, "ShellIcon", new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.79f), Vector2.zero, new Vector2(86f, 86f)).gameObject.AddComponent<Image>();
        shellIcon.sprite = manager != null && manager.GetSingingShellSprite() != null ? manager.GetSingingShellSprite() : circleSprite;
        shellIcon.color = new Color(1f, 0.88f, 0.22f, 1f);
        shellIcon.preserveAspect = true;

        soundMatchTitleText = CreateText(card.transform, "Title", "Listen to the shell", new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(720f, 46f), 36, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        soundMatchBodyText = CreateText(card.transform, "Body", "Which friend sings this beat?", new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(720f, 44f), 26, FontStyle.Bold, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);

        GameObject beatRow = CreateRect(card.transform, "SoundBubbles", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(560f, 62f)).gameObject;
        HorizontalLayoutGroup beatLayout = beatRow.AddComponent<HorizontalLayoutGroup>();
        beatLayout.spacing = 14;
        beatLayout.childAlignment = TextAnchor.MiddleCenter;
        beatLayout.childControlWidth = true;
        beatLayout.childControlHeight = true;
        beatLayout.childForceExpandWidth = false;
        soundMatchBubbleRoot = beatRow.transform;

        GameObject options = CreateRect(card.transform, "Options", new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(620f, 130f)).gameObject;
        HorizontalLayoutGroup optionLayout = options.AddComponent<HorizontalLayoutGroup>();
        optionLayout.spacing = 20;
        optionLayout.childAlignment = TextAnchor.MiddleCenter;
        optionLayout.childControlWidth = true;
        optionLayout.childControlHeight = true;
        optionLayout.childForceExpandWidth = false;

        soundMatchResultText = CreateText(card.transform, "Result", "Listen...", new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(700f, 44f), 25, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        soundMatchPearlText = CreateText(card.transform, "Pearls", "0 music pearls", new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(260f, 30f), 19, FontStyle.Bold, new Color(0.78f, 0.95f, 1f), TextAnchor.MiddleCenter);

        CreateButton(card.transform, "ReplayButton", "Replay", new Vector2(0.16f, 0.08f), Vector2.zero, new Vector2(150f, 48f), delegate
        {
            if (manager != null)
            {
                manager.ReplaySoundMatchPattern();
            }
        });
        CreateButton(card.transform, "CloseButton", "Close", new Vector2(0.84f, 0.08f), Vector2.zero, new Vector2(150f, 48f), delegate { soundMatchOverlay.SetActive(false); });
    }

    private void CreateBucketPreview(Transform parent)
    {
        GameObject bucket = CreatePanel(parent, "BucketPreview", new Color(1f, 1f, 1f, 0.12f));
        RectTransform bucketRect = bucket.GetComponent<RectTransform>();
        bucketRect.anchorMin = new Vector2(0.47f, 0.5f);
        bucketRect.anchorMax = new Vector2(0.47f, 0.5f);
        bucketRect.anchoredPosition = Vector2.zero;
        bucketRect.sizeDelta = new Vector2(420f, 380f);

        Image bucketImage = CreateRect(bucket.transform, "BucketImage", new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(290f, 250f)).gameObject.AddComponent<Image>();
        bucketImage.sprite = manager != null && manager.GetBucketSprite() != null ? manager.GetBucketSprite() : circleSprite;
        bucketImage.color = new Color(0.95f, 0.62f, 0.22f, 1f);
        bucketImage.preserveAspect = true;
        bucketImage.raycastTarget = false;

        bucketSlots.Clear();
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.TopSlot, new Vector2(0.5f, 0.84f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.LeftSlot, new Vector2(0.2f, 0.48f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.RightSlot, new Vector2(0.8f, 0.48f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.FrontSlot, new Vector2(0.5f, 0.42f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.CharmSlot, new Vector2(0.5f, 0.14f));
    }

    private void CreateBucketSlot(Transform parent, OceanBucketSlotId slotId, Vector2 anchor)
    {
        GameObject obj = CreateRect(parent, slotId.ToString(), anchor, anchor, Vector2.zero, new Vector2(88f, 88f)).gameObject;
        OceanBucketSlot slot = obj.AddComponent<OceanBucketSlot>();
        Sprite slotSprite = manager != null && manager.GetBucketSlotSprite() != null ? manager.GetBucketSlotSprite() : circleSprite;
        slot.Build(this, slotId, slotSprite, uiFont);
        bucketSlots.Add(slot);
    }

    private void CreateDecorationLibrary(Transform parent)
    {
        GameObject library = CreatePanel(parent, "DecorationLibrary", new Color(1f, 1f, 1f, 0.1f));
        RectTransform rect = library.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.17f, 0.5f);
        rect.anchorMax = new Vector2(0.17f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(270f, 400f);

        CreateText(library.transform, "LibraryTitle", "Decorations", new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(230f, 34f), 25, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        decorationLibraryRoot = CreateRect(library.transform, "Grid", new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(226f, 300f));
        GridLayoutGroup grid = decorationLibraryRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(96f, 84f);
        grid.spacing = new Vector2(12f, 12f);
        grid.childAlignment = TextAnchor.MiddleCenter;
    }

    private void RefreshBucketWorkshop(OceanBucketInventory inventory)
    {
        if (bucketAlbumOverlay == null || inventory == null)
        {
            return;
        }

        RefreshBucketSlots(inventory);
        RefreshDecorationLibrary(inventory);
        ShowBucketHint("Drag unlocked decorations onto the bucket spots");
    }

    private void RefreshBucketSlots(OceanBucketInventory inventory)
    {
        for (int i = 0; i < bucketSlots.Count; i++)
        {
            OceanBucketSlot slot = bucketSlots[i];
            if (slot == null)
            {
                continue;
            }

            if (inventory.HasSlotDecoration(slot.slotId))
            {
                OceanDecorationReward reward = inventory.GetSlotDecoration(slot.slotId);
                slot.SetDecoration(reward, GetDecorationSprite(reward), ColorForDecoration(reward));
            }
            else
            {
                slot.ClearDecoration();
            }
        }
    }

    private void RefreshDecorationLibrary(OceanBucketInventory inventory)
    {
        if (decorationLibraryRoot == null)
        {
            return;
        }

        for (int i = decorationLibraryRoot.childCount - 1; i >= 0; i--)
        {
            DestroyObject(decorationLibraryRoot.GetChild(i).gameObject);
        }

        OceanDecorationReward[] decorations = OceanBucketInventory.GetAllDecorations();
        for (int i = 0; i < decorations.Length; i++)
        {
            CreateDecorationItem(decorationLibraryRoot, decorations[i], inventory);
        }
    }

    private void CreateDecorationItem(Transform parent, OceanDecorationReward reward, OceanBucketInventory inventory)
    {
        bool unlocked = inventory.IsDecorationUnlocked(reward);
        GameObject item = CreatePanel(parent, reward.ToString() + "Item", unlocked ? new Color(1f, 1f, 1f, 0.9f) : new Color(0.25f, 0.31f, 0.36f, 0.88f));
        Image icon = CreateRect(item.transform, "Icon", new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(44f, 44f)).gameObject.AddComponent<Image>();
        icon.sprite = GetDecorationSprite(reward);
        icon.color = unlocked ? ColorForDecoration(reward) : new Color(0.55f, 0.6f, 0.65f, 0.55f);
        icon.raycastTarget = false;
        icon.preserveAspect = true;

        CreateText(item.transform, "Label", unlocked ? DecorationLabel(reward) : "LOCK", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(90f, 22f), 14, FontStyle.Bold, unlocked ? new Color(0.02f, 0.15f, 0.22f) : Color.white, TextAnchor.MiddleCenter);
        if (!unlocked)
        {
            Image lockIcon = CreateRect(item.transform, "LockIcon", new Vector2(0.78f, 0.77f), new Vector2(0.78f, 0.77f), Vector2.zero, new Vector2(24f, 24f)).gameObject.AddComponent<Image>();
            Sprite lockSprite = manager != null ? manager.GetLockSprite() : null;
            lockIcon.sprite = lockSprite != null ? lockSprite : circleSprite;
            lockIcon.color = new Color(1f, 0.94f, 0.68f, 0.95f);
            lockIcon.preserveAspect = true;
            lockIcon.raycastTarget = false;
        }

        OceanDecorationDragItem dragItem = item.AddComponent<OceanDecorationDragItem>();
        dragItem.Build(this, reward, unlocked);
    }

    public void ShowDecorationInfo(OceanDecorationReward reward)
    {
        OceanBucketInventory inventory = manager != null ? manager.GetBucketInventory() : null;
        if (inventory == null)
        {
            return;
        }

        if (inventory.IsDecorationUnlocked(reward))
        {
            ShowBucketHint("Drag " + reward + " to your bucket");
            return;
        }

        OceanDecorationUnlockRequirement requirement = inventory.GetUnlockProgress(reward);
        if (requirement.usesMusicPearls)
        {
            ShowBucketHint("Earn " + requirement.Remaining + " more Music Pearls to unlock " + DecorationLabel(reward));
        }
        else
        {
            ShowBucketHint("Catch " + requirement.Remaining + " more " + FishName(requirement.fishType) + " to unlock " + DecorationLabel(reward));
        }
    }

    public void ShowBucketHint(string text)
    {
        if (bucketHintText != null)
        {
            bucketHintText.text = text;
        }
    }

    public void HighlightBucketSlotAt(Vector2 screenPoint)
    {
        for (int i = 0; i < bucketSlots.Count; i++)
        {
            OceanBucketSlot slot = bucketSlots[i];
            if (slot != null)
            {
                slot.SetHighlighted(slot.ContainsScreenPoint(screenPoint));
            }
        }
    }

    public void ClearBucketSlotHighlights()
    {
        for (int i = 0; i < bucketSlots.Count; i++)
        {
            if (bucketSlots[i] != null)
            {
                bucketSlots[i].SetHighlighted(false);
            }
        }
    }

    public bool TryPlaceDecorationAt(OceanDecorationReward reward, Vector2 screenPoint)
    {
        OceanBucketInventory inventory = manager != null ? manager.GetBucketInventory() : null;
        if (inventory == null || !inventory.IsDecorationUnlocked(reward))
        {
            ShowDecorationInfo(reward);
            return false;
        }

        for (int i = 0; i < bucketSlots.Count; i++)
        {
            OceanBucketSlot slot = bucketSlots[i];
            if (slot != null && slot.ContainsScreenPoint(screenPoint))
            {
                inventory.SetSlotDecoration(slot.slotId, reward);
                slot.SetDecoration(reward, GetDecorationSprite(reward), ColorForDecoration(reward));
                UpdateBucket(inventory);
                ShowBucketHint(DecorationLabel(reward) + " placed on " + slot.slotId);
                return true;
            }
        }

        return false;
    }

    private Sprite GetDecorationSprite(OceanDecorationReward reward)
    {
        Sprite sprite = manager != null ? manager.GetDecorationSprite(reward) : null;
        return sprite != null ? sprite : circleSprite;
    }

    private void RefreshBucketAlbum(OceanBucketInventory inventory)
    {
        if (bucketAlbumOverlay == null || inventory == null)
        {
            return;
        }

        Transform content = bucketAlbumOverlay.transform.Find("Card/Content");
        if (content == null)
        {
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            DestroyObject(content.GetChild(i).gameObject);
        }

        VerticalLayoutGroup layout = content.gameObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateAlbumRow(content, "Shells", inventory.Shells + " shells", false, OceanDecorationReward.Seaweed, inventory);
        CreateAlbumRow(content, "Fish 4/4", inventory.GetCatchCount(OceanFishType.Fish) + " caught", false, OceanDecorationReward.Shell, inventory);
        CreateAlbumRow(content, "Octopus 3/4", inventory.GetCatchCount(OceanFishType.Octopus) + " caught", false, OceanDecorationReward.Star, inventory);
        CreateAlbumRow(content, "Turtle 2/4", inventory.GetCatchCount(OceanFishType.Turtle) + " caught", false, OceanDecorationReward.Flag, inventory);
        CreateAlbumRow(content, "Jellyfish 6/8", inventory.GetCatchCount(OceanFishType.Jellyfish) + " caught", false, OceanDecorationReward.Pearl, inventory);
        CreateAlbumRow(content, "Seaweed", inventory.IsDecorationUnlocked(OceanDecorationReward.Seaweed) ? "Use" : "Locked", true, OceanDecorationReward.Seaweed, inventory);
        CreateAlbumRow(content, "Shell Decor", inventory.IsDecorationUnlocked(OceanDecorationReward.Shell) ? "Use" : "Catch 3 fish", true, OceanDecorationReward.Shell, inventory);
        CreateAlbumRow(content, "Star Decor", inventory.IsDecorationUnlocked(OceanDecorationReward.Star) ? "Use" : "Catch 3 octopus", true, OceanDecorationReward.Star, inventory);
        CreateAlbumRow(content, "Flag Decor", inventory.IsDecorationUnlocked(OceanDecorationReward.Flag) ? "Use" : "Catch 3 turtles", true, OceanDecorationReward.Flag, inventory);
        CreateAlbumRow(content, "Pearl Decor", inventory.IsDecorationUnlocked(OceanDecorationReward.Pearl) ? "Use" : "Catch mystery fish", true, OceanDecorationReward.Pearl, inventory);
    }

    private void CreateAlbumRow(Transform parent, string label, string value, bool selectable, OceanDecorationReward reward, OceanBucketInventory inventory)
    {
        GameObject row = CreatePanel(parent, label.Replace(" ", "") + "Row", new Color(1f, 1f, 1f, 0.12f));
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 34f;
        HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
        group.padding = new RectOffset(12, 12, 2, 2);
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;

        CreateFlowLabel(row.transform, label, 24, FontStyle.Bold, Color.white, 300f);
        CreateFlowLabel(row.transform, value, 22, FontStyle.Normal, new Color(1f, 0.94f, 0.68f), 260f);
        if (selectable)
        {
            Button button = row.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            button.onClick.AddListener(delegate
            {
                if (inventory.TrySelectDecoration(reward))
                {
                    UpdateBucket(inventory);
                    RefreshBucketWorkshop(inventory);
                }
            });
        }
    }

    private void CreateFlowLabel(Transform parent, string text, int size, FontStyle style, Color color, float width)
    {
        GameObject obj = new GameObject("Label", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Text label = obj.AddComponent<Text>();
        label.font = uiFont;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAnchor.MiddleLeft;
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.minHeight = 30f;
    }

    private string BeatName(OceanLesson lesson)
    {
        if (lesson.beatsPerBar == 4)
        {
            return "Walking Beat";
        }
        if (lesson.beatsPerBar == 3)
        {
            return "Sway Beat";
        }
        if (lesson.beatsPerBar == 2)
        {
            return "March Beat";
        }
        return "Wave Beat";
    }

    private string BeatPatternText(OceanLesson lesson)
    {
        if (lesson.beatsPerBar == 4)
        {
            return "STRONG  soft  soft  soft";
        }
        if (lesson.beatsPerBar == 3)
        {
            return "STRONG  soft  soft";
        }
        if (lesson.beatsPerBar == 2)
        {
            return "STRONG  soft";
        }
        return "STRONG  soft  soft   STRONG  soft  soft";
    }

    private string BeatBodyText(OceanLesson lesson)
    {
        if (lesson.beatsPerBar == 4)
        {
            return "A walking beat feels steady: one strong step, then three soft steps.";
        }
        if (lesson.beatsPerBar == 3)
        {
            return "A sway beat rocks like a boat: strong, soft, soft.";
        }
        if (lesson.beatsPerBar == 2)
        {
            return "A march beat steps left and right: strong, soft.";
        }
        return "A wave beat rolls in two groups: strong soft soft, strong soft soft.";
    }

    private Color ColorForDecoration(OceanDecorationReward reward)
    {
        if (reward == OceanDecorationReward.Shell)
        {
            return new Color(1f, 0.82f, 0.44f);
        }
        if (reward == OceanDecorationReward.Star)
        {
            return new Color(1f, 0.88f, 0.18f);
        }
        if (reward == OceanDecorationReward.Flag)
        {
            return new Color(1f, 0.38f, 0.32f);
        }
        if (reward == OceanDecorationReward.Pearl)
        {
            return new Color(0.9f, 0.78f, 1f);
        }
        if (reward == OceanDecorationReward.BellCharm)
        {
            return new Color(1f, 0.78f, 0.2f);
        }
        if (reward == OceanDecorationReward.GlowStar)
        {
            return new Color(0.45f, 0.95f, 1f);
        }
        if (reward == OceanDecorationReward.WaveRibbon)
        {
            return new Color(0.42f, 0.62f, 1f);
        }
        return new Color(0.36f, 0.88f, 0.48f);
    }

    private string DecorationLabel(OceanDecorationReward reward)
    {
        if (reward == OceanDecorationReward.BellCharm)
        {
            return "Bell";
        }
        if (reward == OceanDecorationReward.GlowStar)
        {
            return "Glow";
        }
        if (reward == OceanDecorationReward.WaveRibbon)
        {
            return "Ribbon";
        }
        return reward.ToString();
    }

    private string FishName(OceanFishType fishType)
    {
        if (fishType == OceanFishType.Fish)
        {
            return "Fish";
        }
        if (fishType == OceanFishType.Octopus)
        {
            return "Octopus";
        }
        if (fishType == OceanFishType.Turtle)
        {
            return "Turtle";
        }
        if (fishType == OceanFishType.Jellyfish)
        {
            return "Jellyfish";
        }
        return "Mystery Fish";
    }

    private void ShowRewardText(string text)
    {
        if (rewardText == null)
        {
            return;
        }

        rewardText.text = text;
        rewardText.gameObject.SetActive(true);
        CancelInvoke("HideRewardText");
        Invoke("HideRewardText", 1.8f);
    }

    private void HideRewardText()
    {
        if (rewardText != null)
        {
            rewardText.gameObject.SetActive(false);
        }
    }

    private void HideSoundMatchOverlay()
    {
        if (soundMatchOverlay != null)
        {
            soundMatchOverlay.SetActive(false);
        }
    }

    private void BuildSoundMatchBubbles(int count)
    {
        if (soundMatchBubbleRoot == null)
        {
            return;
        }

        for (int i = soundMatchBubbleRoot.childCount - 1; i >= 0; i--)
        {
            DestroyObject(soundMatchBubbleRoot.GetChild(i).gameObject);
        }
        soundMatchBubbles.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject bubble = CreateRect(soundMatchBubbleRoot, "SoundBeat_" + (i + 1), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(58f, 58f)).gameObject;
            Image image = bubble.AddComponent<Image>();
            image.sprite = circleSprite;
            image.color = new Color(0.78f, 0.95f, 1f, 0.4f);
            image.preserveAspect = true;

            Text number = CreateText(bubble.transform, "Number", (i + 1).ToString(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 50f), 23, FontStyle.Bold, new Color(0.02f, 0.16f, 0.24f), TextAnchor.MiddleCenter);
            number.raycastTarget = false;

            LayoutElement layout = bubble.AddComponent<LayoutElement>();
            layout.minWidth = 58f;
            layout.minHeight = 58f;
            soundMatchBubbles.Add(image);
        }
    }

    private List<OceanLesson> BuildSoundMatchOptions(OceanLesson targetLesson, OceanLesson[] allLessons)
    {
        List<OceanLesson> options = new List<OceanLesson>();
        options.Add(targetLesson);
        for (int i = 0; i < allLessons.Length && options.Count < 3; i++)
        {
            OceanLesson lesson = allLessons[i];
            if (lesson != null && lesson.fishType != targetLesson.fishType)
            {
                options.Add(lesson);
            }
        }

        for (int i = 0; i < options.Count; i++)
        {
            int swapIndex = Random.Range(i, options.Count);
            OceanLesson temp = options[i];
            options[i] = options[swapIndex];
            options[swapIndex] = temp;
        }

        return options;
    }

    private OceanPondAnimal ApplyFreePondSelection(OceanPondAnimal selected)
    {
        if (selected == currentFreePondSelection)
        {
            return selected;
        }

        currentFreePondSelection = selected;
        for (int i = 0; i < pondAnimals.Count; i++)
        {
            if (pondAnimals[i] != null)
            {
                pondAnimals[i].SetSelected(pondAnimals[i] == selected);
            }
        }

        if (selected == null)
        {
            ShowNoFishSelected();
        }

        return selected;
    }

    private void ClearPond()
    {
        for (int i = pondLayer.childCount - 1; i >= 0; i--)
        {
            Transform child = pondLayer.GetChild(i);
            if (child != null && child.name != "NetCursor")
            {
                DestroyObject(child.gameObject);
            }
        }
        pondAnimals.Clear();
    }

    private void DestroyObject(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }

    private void CaptureImageOverrides(Transform root)
    {
        pendingImageOverrides.Clear();
        pendingNameImageOverrides.Clear();

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
            {
                continue;
            }

            ImageVisualOverride visual = new ImageVisualOverride();
            visual.sprite = image.sprite;
            visual.color = image.color;
            visual.material = image.material;
            visual.type = image.type;
            visual.preserveAspect = image.preserveAspect;
            visual.raycastTarget = image.raycastTarget;

            pendingImageOverrides[IndexedPath(root, image.transform)] = visual;
            pendingNameImageOverrides[NamePath(root, image.transform)] = visual;
        }
    }

    private void ApplyPendingImageOverrides()
    {
        if (pendingImageOverrides.Count == 0 && pendingNameImageOverrides.Count == 0)
        {
            return;
        }

        if (canvas == null)
        {
            return;
        }

        Transform root = canvas.transform;
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
            {
                continue;
            }

            ImageVisualOverride visual;
            if (!pendingImageOverrides.TryGetValue(IndexedPath(root, image.transform), out visual)
                && !pendingNameImageOverrides.TryGetValue(NamePath(root, image.transform), out visual))
            {
                continue;
            }

            image.sprite = visual.sprite;
            image.color = visual.color;
            image.material = visual.material;
            image.type = visual.type;
            image.preserveAspect = visual.preserveAspect;
            image.raycastTarget = visual.raycastTarget;

            OceanSpriteAnimator animator = image.GetComponent<OceanSpriteAnimator>();
            if (animator != null && visual.sprite != null)
            {
                animator.StopOn(visual.sprite);
            }
        }
    }

    private string IndexedPath(Transform root, Transform target)
    {
        if (target == root)
        {
            return target.name;
        }

        string path = target.name + "#" + target.GetSiblingIndex();
        Transform current = target.parent;
        while (current != null && current != root)
        {
            path = current.name + "#" + current.GetSiblingIndex() + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private string NamePath(Transform root, Transform target)
    {
        if (target == root)
        {
            return target.name;
        }

        string path = target.name;
        Transform current = target.parent;
        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private void CreateNavigation(Transform parent)
    {
        Button backButton = CreateButton(parent, "BackButton", "<", new Vector2(0f, 1f), new Vector2(46f, -46f), new Vector2(58f, 58f), delegate
        {
            if (manager != null)
            {
                manager.ReturnToStart();
            }
        });
        backButtonObject = backButton.gameObject;

        Button retryButton = CreateButton(parent, "RetryButton", "↻", new Vector2(0f, 1f), new Vector2(112f, -46f), new Vector2(58f, 58f), delegate
        {
            if (manager != null)
            {
                manager.RestartCurrentLesson();
            }
        });
        retryButtonObject = retryButton.gameObject;

        Button pauseButton = CreateButton(parent, "PauseButton", "||", new Vector2(0f, 1f), new Vector2(178f, -46f), new Vector2(58f, 58f), delegate
        {
            if (manager != null)
            {
                manager.TogglePause();
            }
        });
        pauseButtonObject = pauseButton.gameObject;
        pauseButtonText = pauseButton.GetComponentInChildren<Text>(true);
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = CreatePanel(parent, name, new Color(1f, 1f, 1f, 0.9f));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(onClick);

        Text text = CreateText(obj.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 22, FontStyle.Bold, new Color(0.02f, 0.15f, 0.22f), TextAnchor.MiddleCenter);
        text.raycastTarget = false;
        return button;
    }

    private Color ColorForAnimal(string animalKey)
    {
        if (animalKey == "Fish")
        {
            return new Color(1f, 0.72f, 0.2f);
        }
        if (animalKey == "Octopus")
        {
            return new Color(0.95f, 0.38f, 0.76f);
        }
        if (animalKey == "Turtle")
        {
            return new Color(0.34f, 0.82f, 0.45f);
        }
        return new Color(0.48f, 0.78f, 1f);
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return obj;
    }

    private Text CreateText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color, TextAnchor alignment)
    {
        GameObject obj = CreateRect(parent, name, anchor, anchor, position, size).gameObject;
        Text text = obj.AddComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = fontSize;
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

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
