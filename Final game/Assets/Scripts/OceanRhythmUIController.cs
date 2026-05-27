using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OceanRhythmUIController : MonoBehaviour
{
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
    private Image progressFill;
    private Transform beatBubbleRoot;
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
    private Image bucketDecorationImage;
    private Text rewardText;
    private GameObject beatInfoButton;
    private Text bucketHintText;
    private Transform decorationLibraryRoot;
    private readonly List<OceanBucketSlot> bucketSlots = new List<OceanBucketSlot>();
    private readonly List<Image> beatBubbles = new List<Image>();
    private readonly List<Image> guideCollectionIcons = new List<Image>();
    private readonly List<OceanPondAnimal> pondAnimals = new List<OceanPondAnimal>();
    private Sprite circleSprite;
    private int keyboardSelectedIndex;
    private float keyboardSelectionHoldUntil;
    private OceanPondAnimal currentFreePondSelection;

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
            Destroy(existing);
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
        CreateCompleteOverlay(root.transform);
        CreatePondCompleteOverlay(root.transform);
        CreateBeatCardOverlay(root.transform);
        CreateBucketUi(root.transform);
        CreateBucketAlbum(root.transform);
        CreateRewardToast(root.transform);
        CreateNavigation(root.transform);
        completeOverlay.transform.SetAsLastSibling();
        pondCompleteOverlay.transform.SetAsLastSibling();
        beatCardOverlay.transform.SetAsLastSibling();
        bucketAlbumOverlay.transform.SetAsLastSibling();

        completeOverlay.SetActive(false);
        pondCompleteOverlay.SetActive(false);
        beatCardOverlay.SetActive(false);
        bucketAlbumOverlay.SetActive(false);
        beatInfoButton.SetActive(false);
        pondLayer.gameObject.SetActive(false);
    }

    public void ShowLesson(OceanLesson lesson, int lessonNumber, int lessonCount, int currentProgress)
    {
        if (lesson == null)
        {
            return;
        }

        titleText.text = lesson.animalName;
        subtitleText.text = lesson.meterLabel + "  " + Mathf.RoundToInt(lesson.bpm) + " BPM";
        instructionText.text = lesson.instruction + "\nTap Space on the bright bubble.";
        feedbackText.text = "Listen first";
        feedbackText.color = new Color(1f, 0.95f, 0.68f);
        lessonCounterText.text = lessonNumber + " / " + lessonCount;
        beatInfoButton.SetActive(false);

        BuildBeatBubbles(lesson.beatsPerBar);
        SetProgress(currentProgress, lesson.requiredHits);

        if (animalController != null)
        {
            guidedAnimalRoot.gameObject.SetActive(true);
            animalController.SetAnimal(lesson.animalName, manager != null ? manager.GetSpriteForLesson(lesson) : null, ColorForAnimal(lesson.animalKey));
        }

        if (pondLayer != null)
        {
            pondLayer.gameObject.SetActive(false);
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
                bubble.transform.localScale = accented ? Vector3.one * 1.22f : Vector3.one * 1.12f;
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
    }

    public void ShowInputResult(OceanRhythmHitResult result, float timingError, int currentProgress, int requiredHits)
    {
        string text;
        Color color;
        if (result == OceanRhythmHitResult.Perfect)
        {
            text = "Perfect";
            color = new Color(1f, 0.86f, 0.18f);
        }
        else if (result == OceanRhythmHitResult.Good)
        {
            text = "Good";
            color = new Color(0.27f, 0.95f, 0.54f);
        }
        else if (result == OceanRhythmHitResult.Near)
        {
            text = "Almost";
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

        title.text = lesson.meterLabel + "  " + BeatName(lesson);
        pattern.text = BeatPatternText(lesson);
        body.text = lessonNumber > 0 ? BeatBodyText(lesson) + "\nFriend " + lessonNumber + " / " + lessonCount : BeatBodyText(lesson);
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

        titleText.text = "Choose a rhythm friend";
        subtitleText.text = "Move the net";
        instructionText.text = "Move the mouse near a fish. Tap Space with its beat.";
        feedbackText.text = "Choose a fish";
        feedbackText.color = new Color(1f, 0.95f, 0.68f);
        SetProgress(0, 1);
        BuildBeatBubbles(0);
        beatInfoButton.SetActive(false);

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
                GameObject obj = CreateRect(pondLayer, "Pond_" + lesson.animalKey + "_" + j, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(176f, 136f)).gameObject;
                OceanPondAnimal animal = obj.AddComponent<OceanPondAnimal>();
                animal.Build(lesson, manager != null ? manager.GetSpriteForLesson(lesson) : null, circleSprite, uiFont, ColorForAnimal(lesson.animalKey), position, lesson.animalKey + "_" + j);
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
        subtitleText.text = lesson.meterLabel + "  " + Mathf.RoundToInt(lesson.bpm) + " BPM";
        instructionText.text = "Keep the net near this fish. Tap Space on its bright bubbles.";
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

        titleText.text = "Choose a rhythm friend";
        subtitleText.text = "Mouse or Arrow Keys";
        instructionText.text = "Move the net near a fish. Then tap Space with the beat.";
        feedbackText.text = "Choose a fish";
        feedbackText.color = new Color(1f, 0.95f, 0.68f);
        SetProgress(0, 1);
        beatInfoButton.SetActive(false);
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
            text = "Almost";
            color = new Color(0.46f, 0.82f, 1f);
        }
        else
        {
            text = "Wait for the bright bubble";
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
        GameObject obj = CreateRect(pondLayer, instanceId, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(182f, 142f)).gameObject;
        OceanPondAnimal animal = obj.AddComponent<OceanPondAnimal>();
        animal.Build(lesson, mysterySprite, circleSprite, uiFont, new Color(0.9f, 0.78f, 1f), position, instanceId);
        pondAnimals.Add(animal);
        obj.transform.SetSiblingIndex(Mathf.Max(0, pondLayer.childCount - 2));
        ShowRewardText("A mystery fish appeared!");
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
            bucketShellText.text = inventory.Shells.ToString();
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
        string reward = animal != null && animal.IsMystery ? "+5 shells  Rare decoration!" : "+1 shell";
        ShowRewardText(reward);
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
        titleText.text = "All rhythm friends are ready!";
        subtitleText.text = "Great listening";
        instructionText.text = "You matched every fish with its rhythm.";
        feedbackText.text = "Choose what to do next";
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
        message.text = finalLesson ? "All rhythm friends learned!" : lesson.animalName + " rescued!";

        Text detail = completeOverlay.transform.Find("Card/Detail").GetComponent<Text>();
        detail.text = finalLesson ? "Opening the free pond." : "Next rhythm friend is coming.";
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
            progressText.text = currentProgress + " / " + requiredHits;
        }
    }

    private void BuildBeatBubbles(int count)
    {
        if (beatBubbleRoot == null)
        {
            return;
        }

        for (int i = beatBubbleRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(beatBubbleRoot.GetChild(i).gameObject);
        }
        beatBubbles.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject bubble = CreateRect(beatBubbleRoot, "BeatBubble_" + (i + 1), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(58f, 58f)).gameObject;
            Image image = bubble.AddComponent<Image>();
            image.sprite = circleSprite;
            image.color = new Color(0.78f, 0.95f, 1f, 0.46f);
            image.preserveAspect = true;

            Text number = CreateText(bubble.transform, "Number", (i + 1).ToString(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 50f), 23, FontStyle.Bold, new Color(0.02f, 0.16f, 0.24f), TextAnchor.MiddleCenter);
            number.raycastTarget = false;

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
        RectTransform topRect = top.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -58f);
        topRect.sizeDelta = new Vector2(1080f, 84f);

        titleText = CreateText(top.transform, "AnimalTitle", "Little Fish", new Vector2(0.5f, 0.62f), new Vector2(0f, 0f), new Vector2(620f, 44f), 34, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        subtitleText = CreateText(top.transform, "Meter", "4/4  76 BPM", new Vector2(0.5f, 0.2f), new Vector2(0f, 0f), new Vector2(480f, 30f), 22, FontStyle.Bold, new Color(1f, 0.86f, 0.24f), TextAnchor.MiddleCenter);
        lessonCounterText = CreateText(top.transform, "LessonCounter", "1 / 4", new Vector2(1f, 0.5f), new Vector2(-66f, 0f), new Vector2(120f, 40f), 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    private void CreateCenterStage(Transform parent)
    {
        GameObject animal = CreateRect(parent, "OceanAnimal", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(0f, 4f), new Vector2(270f, 230f)).gameObject;
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
        cardRect.sizeDelta = new Vector2(940f, 180f);

        instructionText = CreateText(card.transform, "Instruction", "", new Vector2(0.5f, 0.76f), Vector2.zero, new Vector2(850f, 58f), 25, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);

        GameObject beatRow = CreateRect(card.transform, "BeatBubbleRow", new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(560f, 62f)).gameObject;
        HorizontalLayoutGroup beatLayout = beatRow.AddComponent<HorizontalLayoutGroup>();
        beatLayout.spacing = 14;
        beatLayout.childAlignment = TextAnchor.MiddleCenter;
        beatLayout.childControlWidth = true;
        beatLayout.childControlHeight = true;
        beatLayout.childForceExpandWidth = false;
        beatBubbleRoot = beatRow.transform;

        feedbackText = CreateText(card.transform, "Feedback", "Listen first", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(430f, 42f), 30, FontStyle.Bold, new Color(1f, 0.95f, 0.68f), TextAnchor.MiddleCenter);

        GameObject progress = CreatePanel(card.transform, "ProgressBar", new Color(1f, 1f, 1f, 0.2f));
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0.05f);
        progressRect.anchorMax = new Vector2(0.5f, 0.05f);
        progressRect.anchoredPosition = Vector2.zero;
        progressRect.sizeDelta = new Vector2(640f, 22f);

        GameObject fill = CreateRect(progress.transform, "Fill", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero).gameObject;
        progressFill = fill.AddComponent<Image>();
        progressFill.color = new Color(0.27f, 0.95f, 0.54f, 1f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;

        progressText = CreateText(card.transform, "ProgressText", "0 / 12", new Vector2(0.5f, 0.05f), new Vector2(0f, 28f), new Vector2(180f, 28f), 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    private void CreateBeatInfoButton(Transform parent)
    {
        Button button = CreateButton(parent, "BeatInfoButton", "?", new Vector2(1f, 0f), new Vector2(-260f, 86f), new Vector2(54f, 54f), delegate
        {
            if (currentFreePondSelection != null)
            {
                ShowBeatCard(currentFreePondSelection.Lesson, 0, 0, delegate { });
            }
        });
        beatInfoButton = button.gameObject;
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
        GameObject bucket = CreatePanel(parent, "CatchBucketButton", new Color(1f, 1f, 1f, 0.9f));
        RectTransform rect = bucket.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-122f, 82f);
        rect.sizeDelta = new Vector2(190f, 96f);

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

        Image bucketImage = CreateRect(bucket.transform, "Icon", new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), Vector2.zero, new Vector2(62f, 62f)).gameObject.AddComponent<Image>();
        bucketImage.sprite = manager != null && manager.GetBucketSprite() != null ? manager.GetBucketSprite() : circleSprite;
        bucketImage.color = new Color(0.95f, 0.62f, 0.22f, 1f);
        bucketImage.preserveAspect = true;

        bucketDecorationImage = CreateRect(bucket.transform, "Decoration", new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0f, 8f), new Vector2(32f, 32f)).gameObject.AddComponent<Image>();
        bucketDecorationImage.sprite = circleSprite;
        bucketDecorationImage.color = new Color(0.36f, 0.88f, 0.48f, 1f);
        bucketDecorationImage.raycastTarget = false;

        bucketCountText = CreateText(bucket.transform, "Count", "0", new Vector2(0.62f, 0.62f), Vector2.zero, new Vector2(70f, 36f), 30, FontStyle.Bold, new Color(0.02f, 0.15f, 0.22f), TextAnchor.MiddleCenter);
        bucketShellText = CreateText(bucket.transform, "Shells", "0", new Vector2(0.62f, 0.28f), Vector2.zero, new Vector2(90f, 28f), 20, FontStyle.Bold, new Color(0.02f, 0.15f, 0.22f), TextAnchor.MiddleCenter);
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
        cardRect.sizeDelta = new Vector2(900f, 540f);

        CreateText(card.transform, "Title", "My Rhythm Bucket", new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(760f, 54f), 40, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateBucketPreview(card.transform);
        CreateDecorationLibrary(card.transform);
        bucketHintText = CreateText(card.transform, "Hint", "Tap a locked decoration to see how to unlock it.", new Vector2(0.73f, 0.52f), Vector2.zero, new Vector2(250f, 260f), 24, FontStyle.Bold, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);
        CreateButton(card.transform, "CloseButton", "Close", new Vector2(0.5f, 0.09f), Vector2.zero, new Vector2(200f, 56f), delegate { bucketAlbumOverlay.SetActive(false); });
    }

    private void CreateRewardToast(Transform parent)
    {
        rewardText = CreateText(parent, "RewardToast", "", new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(620f, 54f), 30, FontStyle.Bold, new Color(1f, 0.86f, 0.18f), TextAnchor.MiddleCenter);
        rewardText.gameObject.SetActive(false);
    }

    private void CreateBucketPreview(Transform parent)
    {
        GameObject bucket = CreatePanel(parent, "BucketPreview", new Color(1f, 1f, 1f, 0.12f));
        RectTransform bucketRect = bucket.GetComponent<RectTransform>();
        bucketRect.anchorMin = new Vector2(0.47f, 0.5f);
        bucketRect.anchorMax = new Vector2(0.47f, 0.5f);
        bucketRect.anchoredPosition = Vector2.zero;
        bucketRect.sizeDelta = new Vector2(330f, 290f);

        Image bucketImage = CreateRect(bucket.transform, "BucketImage", new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(210f, 190f)).gameObject.AddComponent<Image>();
        bucketImage.sprite = manager != null && manager.GetBucketSprite() != null ? manager.GetBucketSprite() : circleSprite;
        bucketImage.color = new Color(0.95f, 0.62f, 0.22f, 1f);
        bucketImage.preserveAspect = true;
        bucketImage.raycastTarget = false;

        bucketSlots.Clear();
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.TopSlot, new Vector2(0.5f, 0.82f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.LeftSlot, new Vector2(0.23f, 0.48f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.RightSlot, new Vector2(0.77f, 0.48f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.FrontSlot, new Vector2(0.5f, 0.42f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.CharmSlot, new Vector2(0.5f, 0.18f));
    }

    private void CreateBucketSlot(Transform parent, OceanBucketSlotId slotId, Vector2 anchor)
    {
        GameObject obj = CreateRect(parent, slotId.ToString(), anchor, anchor, Vector2.zero, new Vector2(76f, 76f)).gameObject;
        OceanBucketSlot slot = obj.AddComponent<OceanBucketSlot>();
        slot.Build(this, slotId, circleSprite, uiFont);
        bucketSlots.Add(slot);
    }

    private void CreateDecorationLibrary(Transform parent)
    {
        GameObject library = CreatePanel(parent, "DecorationLibrary", new Color(1f, 1f, 1f, 0.1f));
        RectTransform rect = library.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.18f, 0.5f);
        rect.anchorMax = new Vector2(0.18f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(230f, 300f);

        CreateText(library.transform, "LibraryTitle", "Decorations", new Vector2(0.5f, 0.9f), Vector2.zero, new Vector2(200f, 34f), 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        decorationLibraryRoot = CreateRect(library.transform, "Grid", new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(200f, 220f));
        GridLayoutGroup grid = decorationLibraryRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(82f, 82f);
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
            Destroy(decorationLibraryRoot.GetChild(i).gameObject);
        }

        CreateDecorationItem(decorationLibraryRoot, OceanDecorationReward.Seaweed, inventory);
        CreateDecorationItem(decorationLibraryRoot, OceanDecorationReward.Shell, inventory);
        CreateDecorationItem(decorationLibraryRoot, OceanDecorationReward.Star, inventory);
        CreateDecorationItem(decorationLibraryRoot, OceanDecorationReward.Flag, inventory);
        CreateDecorationItem(decorationLibraryRoot, OceanDecorationReward.Pearl, inventory);
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

        CreateText(item.transform, "Label", unlocked ? reward.ToString() : "LOCK", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(76f, 22f), 15, FontStyle.Bold, unlocked ? new Color(0.02f, 0.15f, 0.22f) : Color.white, TextAnchor.MiddleCenter);

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
        ShowBucketHint("Catch " + requirement.Remaining + " more " + FishName(requirement.fishType) + " to unlock " + reward);
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
                ShowBucketHint(reward + " placed on " + slot.slotId);
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
            Destroy(content.GetChild(i).gameObject);
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
        return new Color(0.36f, 0.88f, 0.48f);
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
                Destroy(child.gameObject);
            }
        }
        pondAnimals.Clear();
    }

    private void CreateNavigation(Transform parent)
    {
        CreateButton(parent, "BackButton", "Back", new Vector2(0f, 1f), new Vector2(82f, -56f), new Vector2(126f, 48f), delegate
        {
            if (manager != null)
            {
                manager.ReturnToStart();
            }
        });

        CreateButton(parent, "RetryButton", "Retry", new Vector2(1f, 1f), new Vector2(-86f, -56f), new Vector2(126f, 48f), delegate
        {
            if (manager != null)
            {
                manager.RestartCurrentLesson();
            }
        });
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
