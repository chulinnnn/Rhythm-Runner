using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// OceanRhythmUIController binds the hierarchy-owned OceanRhythmCanvas.
// OceanRhythmUIController 负责绑定由 Hierarchy 拥有的 OceanRhythmCanvas。
//
// Reading map / 阅读入口:
// - Build/BindExistingCanvas: locate scene objects and wire listeners.
// - Beat card methods: show fixed hierarchy cards without overwriting card content.
// - Free Pond methods: create/runtime-update fish instances and selection feedback.
// - Bucket Album methods: clone templates, page decorations, and equip slots.
// - Sound Match methods: show Singing Shell options and result feedback.
//
// Ownership rule / 归属规则:
// Runtime may update dynamic text, active states, fill amounts, listener bindings, and generated fish/items.
// Runtime 只更新动态文本、显隐、fill、按钮监听以及运行时鱼/物品。
// Scene-authored sprites, colors, fonts, panels, button labels, and layout should stay editable in Hierarchy.
// 场景里已经编辑好的图片、颜色、字体、面板、按钮文字和布局应保持 Hierarchy 可控。

public class OceanRhythmUIController : MonoBehaviour
{
    private const int BucketAlbumItemsPerPage = 4;

    private static class OceanVisual
    {
        public static readonly Color DeepPanel = new Color(0.018f, 0.12f, 0.19f, 0.78f);
        public static readonly Color CardPanel = new Color(0.035f, 0.28f, 0.40f, 0.96f);
        public static readonly Color FoamLine = new Color(0.78f, 0.95f, 1f, 0.20f);
        public static readonly Color Gold = new Color(1f, 0.86f, 0.18f, 1f);
        public static readonly Color SoftWhite = new Color(1f, 1f, 1f, 0.92f);

        public static readonly Vector2 BucketButtonSize = new Vector2(132f, 122f);
        public static readonly Vector2 BucketIconSize = new Vector2(118f, 118f);
        public static readonly Vector2 BucketPreviewSize = new Vector2(360f, 320f);
        public static readonly Vector2 BucketSlotSize = new Vector2(82f, 82f);
    }

    private class ImageVisualOverride
    {
        public Sprite sprite;
        public Color color;
        public Material material;
        public Image.Type type;
        public bool preserveAspect;
        public bool raycastTarget;
    }

    private class RectTransformOverride
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 pivot;
        public Vector3 localScale;
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
    private Transform beatCardRoot;
    private GameObject bucketAlbumOverlay;
    private Text bucketCountText;
    private Text bucketShellText;
    private Text bucketPearlText;
    private GameObject bucketButtonObject;
    private Image bucketIconImage;
    private Image bucketPreviewImage;
    private Image bucketDecorationImage;
    private Text bucketAlbumShellText;
    private Text bucketAlbumPearlText;
    private Image decorationDetailIcon;
    private Image decorationDetailProgressFill;
    private Image decorationDetailRequirementIcon;
    private Text decorationDetailNameText;
    private Text decorationDetailStatusText;
    private Text decorationDetailProgressText;
    private Text decorationDetailRequirementText;
    private Text decorationDetailActionText;
    private Button decorationDetailActionButton;
    private GameObject decorationDetailActionObject;
    private Button bucketAlbumPrevPageButton;
    private Button bucketAlbumNextPageButton;
    private Text bucketAlbumPageText;
    private GameObject unlockedDecorationTemplate;
    private GameObject lockedDecorationTemplate;
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
    private bool preserveExistingImageOverrides;
    private float tapPulseScale = 1f;
    private bool tapButtonShowingBeatSprite;
    private bool beatCardOpen;
    private bool hasSelectedDecoration;
    private bool choosingBucketSlot;
    private int bucketAlbumPageIndex;
    private OceanDecorationReward selectedDecoration;
    private readonly Dictionary<OceanBeatCardId, GameObject> beatCardViews = new Dictionary<OceanBeatCardId, GameObject>();
    private readonly Dictionary<string, ImageVisualOverride> pendingImageOverrides = new Dictionary<string, ImageVisualOverride>();
    private readonly Dictionary<string, ImageVisualOverride> pendingNameImageOverrides = new Dictionary<string, ImageVisualOverride>();
    private readonly Dictionary<string, RectTransformOverride> pendingRectOverrides = new Dictionary<string, RectTransformOverride>();
    private readonly Dictionary<string, RectTransformOverride> pendingNameRectOverrides = new Dictionary<string, RectTransformOverride>();

    public bool IsReady { get; private set; }

    public void Build(OceanRhythmManager owner)
    {
        Build(owner, RuntimeScenePolicy.Defaults());
    }

    public void Build(OceanRhythmManager owner, RuntimeScenePolicy scenePolicy)
    {
        // Prefer the saved scene hierarchy. Generated UI is only a fallback for missing legacy scenes.
        // 优先绑定已保存的场景层级；生成 UI 只作为旧场景缺失时的兜底。
        IsReady = false;
        manager = owner;
        if (scenePolicy == null)
        {
            scenePolicy = RuntimeScenePolicy.Defaults();
        }
        preserveExistingImageOverrides = scenePolicy.preserveExistingImageOverrides;

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        circleSprite = CreateCircleSprite("OceanCircleSprite", 96, Color.white);
        EnsureEventSystem();

        GameObject existing = GameObject.Find("OceanRhythmCanvas");
        if (existing != null && scenePolicy.useExistingSceneObjects && !scenePolicy.rebuildUiOnPlay)
        {
            if (BindExistingCanvas(existing, scenePolicy.preserveExistingImageOverrides))
            {
                ApplyPendingRectTransformOverrides();
                if (scenePolicy.preserveExistingImageOverrides)
                {
                    ApplyPendingImageOverrides();
                }
                NormalizeOceanRaycasts(existing.transform);
                ApplyManagerSprites();
                IsReady = true;
                return;
            }

            Debug.LogWarning("OceanRhythmUIController: Existing OceanRhythmCanvas is missing required children. Keeping it untouched.");
            return;
        }

        if (existing != null && scenePolicy.rebuildUiOnPlay)
        {
            if (scenePolicy.preserveExistingImageOverrides)
            {
                CaptureImageOverrides(existing.transform);
            }
            else
            {
                ClearImageOverrides();
            }
            CaptureRectTransformOverrides(existing.transform);
            DestroyObject(existing);
        }
        else if (existing != null)
        {
            return;
        }
        else
        {
            pendingImageOverrides.Clear();
            pendingNameImageOverrides.Clear();
            pendingRectOverrides.Clear();
            pendingNameRectOverrides.Clear();
        }

        Debug.LogWarning("OceanRhythmUIController: OceanRhythmCanvas is missing. Add it to the scene hierarchy before play.");
        return;

#pragma warning disable 162
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
        background.raycastTarget = false;
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
        ApplyPendingRectTransformOverrides();
        if (scenePolicy.preserveExistingImageOverrides)
        {
            ApplyPendingImageOverrides();
        }
        NormalizeOceanRaycasts(canvasObject.transform);
        ApplyManagerSprites();
        IsReady = true;
#pragma warning restore 162
    }

    private bool BindExistingCanvas(GameObject existing, bool preserveExistingImageOverrides)
    {
        canvas = existing.GetComponent<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        rootRect = FindRect(existing.transform, "OceanRoot");
        if (rootRect == null)
        {
            rootRect = existing.GetComponent<RectTransform>();
        }

        if (preserveExistingImageOverrides)
        {
            CaptureImageOverrides(existing.transform);
        }
        else
        {
            ClearImageOverrides();
        }
        CaptureRectTransformOverrides(existing.transform);

        topBarObject = FindObject(existing.transform, "OceanRoot/TopBar");
        learningModeText = FindText(existing.transform, "OceanRoot/TopBar/LearningMode");
        titleText = FindText(existing.transform, "OceanRoot/TopBar/AnimalTitle");
        subtitleText = FindText(existing.transform, "OceanRoot/TopBar/Meter");
        lessonCounterText = FindText(existing.transform, "OceanRoot/TopBar/LessonCounter");
        instructionText = FindText(existing.transform, "OceanRoot/BottomHud/Instruction");
        feedbackText = FindText(existing.transform, "OceanRoot/BottomHud/Feedback");
        lessonGoalText = FindText(existing.transform, "OceanRoot/BottomHud/LessonGoal");
        progressHelpText = FindText(existing.transform, "OceanRoot/BottomHud/ProgressHelp");
        progressText = FindText(existing.transform, "OceanRoot/BottomHud/ProgressText");
        progressFill = FindImage(existing.transform, "OceanRoot/BottomHud/ProgressBar/Fill");
        beatBubbleRoot = FindTransform(existing.transform, "OceanRoot/BottomHud/BeatBubbleRow");
        lessonTargetBubbleRoot = FindTransform(existing.transform, "OceanRoot/BottomHud/LessonTargetBubbleRow");

        guidedAnimalRoot = FindRect(existing.transform, "OceanRoot/OceanAnimal");
        animalController = guidedAnimalRoot != null ? guidedAnimalRoot.GetComponent<OceanAnimalController>() : null;
        if (guidedAnimalRoot != null && animalController == null)
        {
            animalController = guidedAnimalRoot.gameObject.AddComponent<OceanAnimalController>();
            animalController.Build(circleSprite, uiFont);
        }

        pondLayer = FindRect(existing.transform, "OceanRoot/FreePondLayer");
        if (pondLayer != null)
        {
            Transform net = pondLayer.Find("NetCursor");
            netCursor = net != null ? net.GetComponent<OceanNetCursor>() : null;
            if (net != null && netCursor == null)
            {
                netCursor = net.gameObject.AddComponent<OceanNetCursor>();
                netCursor.Build(circleSprite, manager != null ? manager.GetNetSprite() : null);
            }
        }

        completeOverlay = FindObject(existing.transform, "OceanRoot/CompleteOverlay");
        pondCompleteOverlay = FindObject(existing.transform, "OceanRoot/PondCompleteOverlay");
        beatCardOverlay = FindObject(existing.transform, "OceanRoot/BeatCardOverlay");
        CacheBeatCards();
        parentHelpOverlay = FindObject(existing.transform, "OceanRoot/ParentHelpOverlay");
        bucketAlbumOverlay = FindObject(existing.transform, "OceanRoot/BucketAlbumOverlay");
        soundMatchOverlay = FindObject(existing.transform, "OceanRoot/SoundMatchOverlay");
        rewardText = FindText(existing.transform, "OceanRoot/RewardToast");
        beatInfoButton = FindObject(existing.transform, "OceanRoot/ParentHelpButton");
        tapButtonObject = FindObject(existing.transform, "OceanRoot/TapButton");
        tapButtonImage = tapButtonObject != null ? tapButtonObject.GetComponent<Image>() : null;
        tapButtonText = tapButtonObject != null ? tapButtonObject.GetComponentInChildren<Text>(true) : null;
        Button tapButton = tapButtonObject != null ? tapButtonObject.GetComponent<Button>() : null;
        if (tapButton != null)
        {
            tapButton.transition = Selectable.Transition.None;
        }
        backButtonObject = FindObject(existing.transform, "OceanRoot/BackButton");
        retryButtonObject = FindObject(existing.transform, "OceanRoot/RetryButton");
        pauseButtonObject = FindObject(existing.transform, "OceanRoot/PauseButton");
        pauseButtonText = pauseButtonObject != null ? pauseButtonObject.GetComponentInChildren<Text>(true) : null;
        bucketButtonObject = FindObject(existing.transform, "OceanRoot/CatchBucketButton");
        bucketIconImage = FindImage(existing.transform, "OceanRoot/CatchBucketButton/Icon");
        bucketDecorationImage = FindImage(existing.transform, "OceanRoot/CatchBucketButton/Decoration");
        bucketCountText = FindText(existing.transform, "OceanRoot/CatchBucketButton/Count");
        bucketShellText = FindText(existing.transform, "OceanRoot/CatchBucketButton/Shells");
        bucketPearlText = FindText(existing.transform, "OceanRoot/CatchBucketButton/MusicPearls");
        singingShellButton = FindObject(existing.transform, "OceanRoot/SingingShellButton");
        singingShellButtonImage = FindImage(existing.transform, "OceanRoot/SingingShellButton/ShellIcon");
        singingShellButtonText = FindText(existing.transform, "OceanRoot/SingingShellButton/Text");
        bucketPreviewImage = FindImage(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/BucketPreview/BucketImage");
        bucketDecorationImage = bucketDecorationImage != null ? bucketDecorationImage : FindImage(existing.transform, "OceanRoot/CatchBucketButton/Decoration");
        bucketAlbumShellText = FindText(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/Header/Shells/Value");
        bucketAlbumPearlText = FindText(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/Header/Pearls/Value");
        bucketHintText = FindText(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationDetail/Hint");
        if (bucketHintText == null)
        {
            bucketHintText = FindText(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/Hint");
        }
        decorationLibraryRoot = FindTransform(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/Grid");
        if (decorationLibraryRoot == null)
        {
            decorationLibraryRoot = FindTransform(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationLibrary/Grid");
        }
        unlockedDecorationTemplate = FindObject(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/Grid/UnlockedItemTemplate");
        lockedDecorationTemplate = FindObject(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/Grid/LockedItemTemplate");
        bucketAlbumPrevPageButton = FindButton(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/PrevPageButton");
        bucketAlbumNextPageButton = FindButton(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/NextPageButton");
        bucketAlbumPageText = FindText(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/PageText");
        decorationDetailIcon = FindImage(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationDetail/Icon");
        decorationDetailNameText = FindText(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationDetail/Name");
        decorationDetailStatusText = FindText(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationDetail/Status");
        decorationDetailProgressFill = FindImage(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationDetail/Progress/Fill");
        decorationDetailProgressText = FindText(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationDetail/Progress/Value");
        decorationDetailRequirementIcon = FindImage(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationDetail/Requirement/Icon");
        decorationDetailRequirementText = FindText(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationDetail/Requirement/Text");
        decorationDetailActionObject = FindObject(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationDetail/ActionButton");
        decorationDetailActionButton = decorationDetailActionObject != null ? decorationDetailActionObject.GetComponent<Button>() : null;
        if (decorationDetailActionObject != null && decorationDetailActionButton == null)
        {
            decorationDetailActionButton = decorationDetailActionObject.AddComponent<Button>();
            Graphic graphic = decorationDetailActionObject.GetComponent<Graphic>();
            if (graphic != null)
            {
                decorationDetailActionButton.targetGraphic = graphic;
            }
        }
        decorationDetailActionText = decorationDetailActionObject != null ? decorationDetailActionObject.GetComponentInChildren<Text>(true) : null;
        soundMatchBubbleRoot = FindTransform(existing.transform, "OceanRoot/SoundMatchOverlay/Card/SoundBubbles");
        soundMatchTitleText = FindText(existing.transform, "OceanRoot/SoundMatchOverlay/Card/Title");
        soundMatchBodyText = FindText(existing.transform, "OceanRoot/SoundMatchOverlay/Card/Body");
        soundMatchResultText = FindText(existing.transform, "OceanRoot/SoundMatchOverlay/Card/Result");
        soundMatchPearlText = FindText(existing.transform, "OceanRoot/SoundMatchOverlay/Card/Pearls");

        if (canvas == null || titleText == null || feedbackText == null || progressFill == null || pondLayer == null)
        {
            return false;
        }

        BindButton(existing.transform, "OceanRoot/ParentHelpButton", delegate
        {
            if (manager != null && showingGuidedLesson)
            {
                if (parentHelpOverlay != null)
                {
                    parentHelpOverlay.SetActive(true);
                }
                return;
            }

            if (manager != null)
            {
                manager.ShowCurrentBeatInfo();
            }
        });
        BindButton(existing.transform, "OceanRoot/TapButton", delegate
        {
            if (manager != null)
            {
                manager.TryTapInput();
            }
        });
        BindButton(existing.transform, "OceanRoot/BackButton", delegate
        {
            if (manager != null)
            {
                manager.ReturnToStart();
            }
        });
        BindButton(existing.transform, "OceanRoot/RetryButton", delegate
        {
            if (manager != null)
            {
                manager.RestartCurrentLesson();
            }
        });
        BindButton(existing.transform, "OceanRoot/PauseButton", delegate
        {
            if (manager != null)
            {
                manager.TogglePause();
            }
        });
        BindButton(existing.transform, "OceanRoot/CatchBucketButton", delegate
        {
            if (bucketAlbumOverlay != null)
            {
                RefreshBucketWorkshop(manager != null ? manager.GetBucketInventory() : null);
                bucketAlbumOverlay.SetActive(true);
            }
        });
        BindButton(existing.transform, "OceanRoot/ParentHelpOverlay/Card/CloseButton", delegate { parentHelpOverlay.SetActive(false); });
        BindButton(existing.transform, "OceanRoot/ParentHelpOverlay/Card/BackButton", delegate
        {
            if (manager != null)
            {
                manager.ReturnToStart();
            }
        });
        BindButton(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/CloseButton", CloseBucketAlbum);
        BindButton(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/Header/CloseButton", CloseBucketAlbum);
        BindButton(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/PrevPageButton", delegate { ChangeBucketAlbumPage(-1); });
        BindButton(existing.transform, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/NextPageButton", delegate { ChangeBucketAlbumPage(1); });
        BindButton(existing.transform, "OceanRoot/PondCompleteOverlay/Card/PlayAgainButton", delegate
        {
            pondCompleteOverlay.SetActive(false);
            if (manager != null)
            {
                manager.RestartFreePond();
            }
        });
        BindButton(existing.transform, "OceanRoot/PondCompleteOverlay/Card/BackToStartButton", delegate
        {
            if (manager != null)
            {
                manager.ReturnToStart();
            }
        });
        BindButton(existing.transform, "OceanRoot/SingingShellButton", delegate
        {
            if (manager != null)
            {
                manager.StartSingingShellGame();
            }
        });
        BindButton(existing.transform, "OceanRoot/SoundMatchOverlay/Card/ReplayButton", delegate
        {
            if (manager != null)
            {
                manager.ReplaySoundMatchPattern();
            }
        });
        BindButton(existing.transform, "OceanRoot/SoundMatchOverlay/Card/CloseButton", delegate { soundMatchOverlay.SetActive(false); });

        CacheCollectionIcons(existing.transform);
        CacheBucketSlots(existing.transform);
        if (unlockedDecorationTemplate != null) unlockedDecorationTemplate.SetActive(false);
        if (lockedDecorationTemplate != null) lockedDecorationTemplate.SetActive(false);
        if (completeOverlay != null) completeOverlay.SetActive(false);
        if (pondCompleteOverlay != null) pondCompleteOverlay.SetActive(false);
        if (beatCardOverlay != null)
        {
            HideBeatCardViews();
            beatCardOverlay.SetActive(false);
            beatCardOpen = false;
        }
        if (parentHelpOverlay != null) parentHelpOverlay.SetActive(false);
        if (bucketAlbumOverlay != null) bucketAlbumOverlay.SetActive(false);
        if (soundMatchOverlay != null) soundMatchOverlay.SetActive(false);
        if (singingShellButton != null) singingShellButton.SetActive(false);
        if (beatInfoButton != null) beatInfoButton.SetActive(false);
        SetBucketButtonVisible(false);
        NormalizeOceanRaycasts(existing.transform);
        if (pondLayer != null) pondLayer.gameObject.SetActive(false);
        return true;
    }

    private Transform FindTransform(Transform root, string path)
    {
        return root != null ? root.Find(path) : null;
    }

    private RectTransform FindRect(Transform root, string path)
    {
        Transform child = FindTransform(root, path);
        return child != null ? child.GetComponent<RectTransform>() : null;
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

    private Button FindButton(Transform root, string path)
    {
        Transform child = FindTransform(root, path);
        return child != null ? child.GetComponent<Button>() : null;
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

        Image image = child.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }
    }

    private void CacheCollectionIcons(Transform root)
    {
        guideCollectionIcons.Clear();
        Transform collection = FindTransform(root, "OceanRoot/FishCollection");
        if (collection == null)
        {
            return;
        }

        Image[] images = collection.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].transform != collection)
            {
                guideCollectionIcons.Add(images[i]);
            }
        }
    }

    private void CacheBucketSlots(Transform root)
    {
        bucketSlots.Clear();
        Transform preview = FindTransform(root, "OceanRoot/BucketAlbumOverlay/Card/BucketPreview");
        if (preview == null)
        {
            return;
        }

        OceanBucketSlotId[] slotIds =
        {
            OceanBucketSlotId.TopSlot,
            OceanBucketSlotId.LeftSlot,
            OceanBucketSlotId.RightSlot,
            OceanBucketSlotId.FrontSlot,
            OceanBucketSlotId.CharmSlot
        };

        for (int i = 0; i < slotIds.Length; i++)
        {
            Transform slotTransform = preview.Find(slotIds[i].ToString());
            if (slotTransform == null)
            {
                continue;
            }

            OceanBucketSlot slot = slotTransform.GetComponent<OceanBucketSlot>();
            if (slot == null)
            {
                slot = slotTransform.gameObject.AddComponent<OceanBucketSlot>();
            }

            Sprite slotSprite = manager != null && manager.GetBucketSlotSprite() != null ? manager.GetBucketSlotSprite() : circleSprite;
            slot.Build(this, slotIds[i], slotSprite, GetBucketSlotHighlightSprite(), uiFont);
            bucketSlots.Add(slot);
        }
    }

    private void Update()
    {
        if (tapButtonObject != null)
        {
            tapPulseScale = Mathf.Lerp(tapPulseScale, 1f, Time.unscaledDeltaTime * 7f);
            tapButtonObject.transform.localScale = Vector3.one * tapPulseScale;
            if (tapButtonShowingBeatSprite && tapPulseScale < 1.02f)
            {
                ApplyTapButtonSprite(false);
            }
        }
    }

    private void HideBeatCard()
    {
        beatCardOpen = false;
        if (beatCardOverlay != null)
        {
            HideBeatCardViews();
            beatCardOverlay.SetActive(false);
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

    public void ShowOceanIntroCard(UnityEngine.Events.UnityAction onClose)
    {
        ShowBeatCard(OceanBeatCardId.Intro, onClose);
    }

    public void ShowBeatCardInfo(OceanLesson lesson)
    {
        ShowBeatCardInfo(lesson, null);
    }

    public void ShowBeatCardInfo(OceanLesson lesson, UnityEngine.Events.UnityAction onClose)
    {
        if (lesson == null || beatCardOverlay == null)
        {
            if (onClose != null)
            {
                onClose.Invoke();
            }
            return;
        }

        ShowBeatCard(CardIdForLesson(lesson), onClose);
    }

    private void ShowBeatCard(OceanBeatCardId cardId, UnityEngine.Events.UnityAction onClose)
    {
        if (beatCardOverlay == null)
        {
            if (onClose != null)
            {
                onClose.Invoke();
            }
            return;
        }

        CacheBeatCards();
        GameObject card = FindBeatCardView(cardId);
        if (card == null)
        {
            Debug.LogWarning("OceanRhythmUIController: Missing editable beat card '" + BeatCardName(cardId) + "'. Add it under OceanRoot/BeatCardOverlay/Cards.");
            if (onClose != null)
            {
                onClose.Invoke();
            }
            return;
        }

        HideBeatCardViews();
        beatCardOverlay.SetActive(true);
        beatCardOpen = true;
        card.SetActive(true);
        BindBeatCardCloseButtons(card.transform, onClose);
    }

    private OceanBeatCardId CardIdForLesson(OceanLesson lesson)
    {
        if (lesson == null)
        {
            return OceanBeatCardId.FourFour;
        }

        if (lesson.beatsPerBar == 3)
        {
            return OceanBeatCardId.ThreeFour;
        }
        if (lesson.beatsPerBar == 2)
        {
            return OceanBeatCardId.TwoFour;
        }
        if (lesson.beatsPerBar == 6)
        {
            return OceanBeatCardId.SixEight;
        }

        return OceanBeatCardId.FourFour;
    }

    private void CacheBeatCards()
    {
        beatCardViews.Clear();
        beatCardRoot = null;
        if (beatCardOverlay == null)
        {
            return;
        }

        beatCardRoot = beatCardOverlay.transform.Find("Cards");
        CacheBeatCardView(OceanBeatCardId.Intro);
        CacheBeatCardView(OceanBeatCardId.FourFour);
        CacheBeatCardView(OceanBeatCardId.ThreeFour);
        CacheBeatCardView(OceanBeatCardId.TwoFour);
        CacheBeatCardView(OceanBeatCardId.SixEight);
    }

    private void CacheBeatCardView(OceanBeatCardId cardId)
    {
        Transform view = beatCardRoot != null ? beatCardRoot.Find(BeatCardName(cardId)) : null;
        if (view == null && cardId == OceanBeatCardId.FourFour)
        {
            view = beatCardOverlay.transform.Find("Card");
        }

        if (view != null)
        {
            beatCardViews[cardId] = view.gameObject;
        }
    }

    private GameObject FindBeatCardView(OceanBeatCardId cardId)
    {
        GameObject view;
        if (beatCardViews.TryGetValue(cardId, out view) && view != null)
        {
            return view;
        }

        return null;
    }

    private void HideBeatCardViews()
    {
        for (int i = 0; i < beatCardOverlay.transform.childCount; i++)
        {
            Transform child = beatCardOverlay.transform.GetChild(i);
            if (child != null && child.name == "Card")
            {
                child.gameObject.SetActive(false);
            }
        }

        if (beatCardRoot == null && beatCardOverlay != null)
        {
            beatCardRoot = beatCardOverlay.transform.Find("Cards");
        }
        if (beatCardRoot == null)
        {
            return;
        }

        for (int i = 0; i < beatCardRoot.childCount; i++)
        {
            beatCardRoot.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void BindBeatCardCloseButtons(Transform card, UnityEngine.Events.UnityAction onClose)
    {
        bool bound = false;
        bound |= BindBeatCardCloseButton(card, "CloseButton", onClose);
        bound |= BindBeatCardCloseButton(card, "TryButton", onClose);
        bound |= BindBeatCardCloseButton(card, "SkipButton", onClose);

        if (!bound)
        {
            Debug.LogWarning("OceanRhythmUIController: Beat card '" + card.name + "' has no CloseButton, TryButton, or SkipButton.");
        }
    }

    private bool BindBeatCardCloseButton(Transform card, string childName, UnityEngine.Events.UnityAction onClose)
    {
        Transform child = card != null ? card.Find(childName) : null;
        if (child == null)
        {
            return false;
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
        button.onClick.AddListener(delegate
        {
            HideBeatCard();
            if (onClose != null)
            {
                onClose.Invoke();
            }
        });

        Image image = child.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }
        return true;
    }

    private string BeatCardName(OceanBeatCardId cardId)
    {
        if (cardId == OceanBeatCardId.Intro)
        {
            return "IntroCard";
        }
        if (cardId == OceanBeatCardId.ThreeFour)
        {
            return "ThreeFourCard";
        }
        if (cardId == OceanBeatCardId.TwoFour)
        {
            return "TwoFourCard";
        }
        if (cardId == OceanBeatCardId.SixEight)
        {
            return "SixEightCard";
        }

        return "FourFourCard";
    }

    public bool IsBeatCardOpen()
    {
        return beatCardOpen && beatCardOverlay != null && beatCardOverlay.activeSelf;
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

        if (preserveExistingImageOverrides)
        {
            ApplyPendingImageOverrides();
        }
        ApplyManagerSprites();
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

        if (IsFreePondPointerBlocked())
        {
            ClearPondHover();
            return currentFreePondSelection;
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
            if (IsPointerOverBlockingOceanUi())
            {
                return currentFreePondSelection;
            }

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

    private bool IsFreePondPointerBlocked()
    {
        return IsOverlayActive(bucketAlbumOverlay)
            || IsOverlayActive(parentHelpOverlay)
            || IsOverlayActive(pondCompleteOverlay)
            || IsOverlayActive(completeOverlay)
            || IsOverlayActive(soundMatchOverlay)
            || IsBeatCardOpen();
    }

    private bool IsOverlayActive(GameObject overlay)
    {
        return overlay != null && overlay.activeInHierarchy;
    }

    private bool IsPointerOverBlockingOceanUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (IsPointerOverBlockingOceanUi(Input.mousePosition, -1))
        {
            return true;
        }

        if (Input.touchCount <= 0)
        {
            return false;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (IsPointerOverBlockingOceanUi(touch.position, touch.fingerId))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPointerOverBlockingOceanUi(Vector2 screenPosition, int pointerId)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        eventData.pointerId = pointerId;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        for (int i = 0; i < results.Count; i++)
        {
            GameObject hit = results[i].gameObject;
            if (hit == null || !hit.activeInHierarchy)
            {
                continue;
            }

            Transform hitTransform = hit.transform;
            if (IsTransformWithin(hitTransform, pondLayer))
            {
                continue;
            }

            Selectable selectable = hit.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.gameObject.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTransformWithin(Transform child, Transform possibleParent)
    {
        if (child == null || possibleParent == null)
        {
            return false;
        }

        Transform current = child;
        while (current != null)
        {
            if (current == possibleParent)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void ClearPondHover()
    {
        for (int i = 0; i < pondAnimals.Count; i++)
        {
            if (pondAnimals[i] != null)
            {
                pondAnimals[i].SetHovered(false);
            }
        }
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

    public void ShowFreePondPreview(OceanLesson lesson)
    {
        if (lesson == null)
        {
            ShowNoFishSelected();
            return;
        }

        titleText.text = lesson.fishType == OceanFishType.Mystery ? "Mystery Fish" : lesson.animalName;
        subtitleText.text = lesson.meterLabel + "  " + Mathf.RoundToInt(lesson.bpm) + " BPM";
        instructionText.text = FreePondPreviewDescription(lesson);
        feedbackText.text = "Listen first. Gameplay starts after the short preview.";
        feedbackText.color = new Color(1f, 0.95f, 0.68f);
        BuildBeatBubbles(lesson.beatsPerBar);
        SetProgress(0, Mathf.Max(1, lesson.requiredHits));
        if (netCursor != null)
        {
            netCursor.SetCaptureRatio(0f);
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
        ShowRewardText("*");
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
        if (bucketDecorationImage != null)
        {
            Sprite decoration = GetDecorationSprite(inventory.SelectedDecoration);
            bucketDecorationImage.sprite = decoration != null ? decoration : circleSprite;
            bucketDecorationImage.color = ColorForDecoration(inventory.SelectedDecoration);
        }
    }

    public void ShowCatchReward(OceanPondAnimal animal, OceanBucketInventory inventory)
    {
        UpdateBucket(inventory);
        string reward = animal != null && animal.IsMystery ? "* * *" : "*";
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
        ApplyTapButtonSprite(true);
    }

    private void ApplyTapButtonSprite(bool beatState)
    {
        if (tapButtonImage != null)
        {
            Sprite sprite = null;
            if (manager != null)
            {
                sprite = beatState ? manager.GetTapButtonBeatSprite() : manager.GetTapButtonNormalSprite();
            }

            tapButtonImage.sprite = sprite != null ? sprite : circleSprite;
            tapButtonImage.color = Color.white;
            tapButtonImage.preserveAspect = false;
        }
        tapButtonShowingBeatSprite = beatState;
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
            image.raycastTarget = false;

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
            image.color = i % 2 == 0 ? new Color(0.24f, 0.86f, 0.95f, 0.11f) : new Color(1f, 1f, 1f, 0.055f);
            image.raycastTarget = false;
        }
    }

    private void CreateTopBar(Transform parent)
    {
        GameObject top = CreatePanel(parent, "TopBar", OceanVisual.DeepPanel);
        SetRaycastTarget(top, false);
        topBarObject = top;
        RectTransform topRect = top.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -58f);
        topRect.sizeDelta = new Vector2(1080f, 84f);

        learningModeText = CreateText(top.transform, "LearningMode", "FREE POND", new Vector2(0f, 0.5f), new Vector2(96f, 0f), new Vector2(190f, 42f), 22, FontStyle.Bold, new Color(1f, 0.86f, 0.18f), TextAnchor.MiddleCenter);
        titleText = CreateText(top.transform, "AnimalTitle", "Pick a fish", new Vector2(0.5f, 0.62f), new Vector2(0f, 0f), new Vector2(620f, 44f), 34, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        subtitleText = CreateText(top.transform, "Meter", "Move the net", new Vector2(0.5f, 0.2f), new Vector2(0f, 0f), new Vector2(480f, 30f), 22, FontStyle.Bold, new Color(1f, 0.86f, 0.24f), TextAnchor.MiddleCenter);
        lessonCounterText = CreateText(top.transform, "LessonCounter", "", new Vector2(1f, 0.5f), new Vector2(-66f, 0f), new Vector2(120f, 40f), 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
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
            image.raycastTarget = false;
            LayoutElement element = icon.AddComponent<LayoutElement>();
            element.minWidth = 42f;
            element.minHeight = 42f;
            guideCollectionIcons.Add(image);
        }
    }

    private void CreatePondLayer(Transform parent)
    {
        pondLayer = CreateRect(parent, "FreePondLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject cursorObject = CreateRect(pondLayer, "NetCursor", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f)).gameObject;
        netCursor = cursorObject.AddComponent<OceanNetCursor>();
        netCursor.Build(circleSprite, manager != null ? manager.GetNetSprite() : null);
    }

    private void CreateBottomHud(Transform parent)
    {
        GameObject card = CreatePanel(parent, "BottomHud", OceanVisual.DeepPanel);
        SetRaycastTarget(card, false);
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
        lessonGoalText = CreateText(card.transform, "LessonGoal", "", new Vector2(0.18f, 0.34f), Vector2.zero, new Vector2(240f, 38f), 20, FontStyle.Bold, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);
        progressHelpText = CreateText(card.transform, "ProgressHelp", "", new Vector2(0.82f, 0.34f), Vector2.zero, new Vector2(240f, 38f), 20, FontStyle.Bold, new Color(0.78f, 0.95f, 1f), TextAnchor.MiddleCenter);

        GameObject progress = CreatePanel(card.transform, "ProgressBar", OceanVisual.FoamLine);
        SetRaycastTarget(progress, false);
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0.11f);
        progressRect.anchorMax = new Vector2(0.5f, 0.11f);
        progressRect.anchoredPosition = Vector2.zero;
        progressRect.sizeDelta = new Vector2(640f, 22f);

        GameObject fill = CreateRect(progress.transform, "Fill", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero).gameObject;
        progressFill = fill.AddComponent<Image>();
        progressFill.color = new Color(0.27f, 0.95f, 0.54f, 1f);
        progressFill.raycastTarget = false;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;

        progressText = CreateText(card.transform, "ProgressText", "0 / 1", new Vector2(0.5f, 0.11f), new Vector2(0f, 28f), new Vector2(240f, 28f), 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

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
        button.transition = Selectable.Transition.None;
        ApplyTapButtonSprite(false);
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

        GameObject cards = CreateRect(beatCardOverlay.transform, "Cards", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        CreateBeatCardView(cards.transform, "IntroCard", "Little Rhythm Ocean", "Feel 2/4, 3/4, 4/4, and 6/8", "Pick a sea friend and listen first. Then tap with the bright bubbles to feel the beat and bring the fish home.");
        CreateBeatCardView(cards.transform, "FourFourCard", "4/4 Walking Beat", "STRONG  soft  soft  soft", "A walking beat feels steady: one strong step, then three soft steps.");
        CreateBeatCardView(cards.transform, "ThreeFourCard", "3/4 Sway Beat", "STRONG  soft  soft", "A sway beat rocks like a boat: strong, soft, soft.");
        CreateBeatCardView(cards.transform, "TwoFourCard", "2/4 March Beat", "STRONG  soft", "A march beat steps left and right: strong, soft.");
        CreateBeatCardView(cards.transform, "SixEightCard", "6/8 Wave Beat", "STRONG  soft  soft   STRONG  soft  soft", "A wave beat rolls in two groups: strong soft soft, strong soft soft.");
        CacheBeatCards();
        HideBeatCardViews();
    }

    private GameObject CreateBeatCardView(Transform parent, string name, string titleTextValue, string patternTextValue, string bodyTextValue)
    {
        GameObject card = CreatePanel(parent, name, new Color(0.05f, 0.36f, 0.46f, 0.98f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(780f, 420f);

        CreateText(card.transform, "Title", titleTextValue, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(680f, 62f), 42, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateText(card.transform, "Pattern", patternTextValue, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(690f, 58f), 30, FontStyle.Bold, new Color(1f, 0.86f, 0.18f), TextAnchor.MiddleCenter);
        CreateText(card.transform, "Body", bodyTextValue, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(680f, 88f), 26, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
        CreateButton(card.transform, "CloseButton", "Close", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(220f, 64f), delegate { });
        card.SetActive(false);
        return card;
    }

    private void CreateBucketUi(Transform parent)
    {
        GameObject bucket = CreatePanel(parent, "CatchBucketButton", new Color(1f, 1f, 1f, 0f));
        bucketButtonObject = bucket;
        RectTransform rect = bucket.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(96f, 84f);
        rect.sizeDelta = OceanVisual.BucketButtonSize;

        Image buttonHitArea = bucket.GetComponent<Image>();
        buttonHitArea.raycastTarget = true;

        bucketIconImage = CreateRect(bucket.transform, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), OceanVisual.BucketIconSize).gameObject.AddComponent<Image>();
        bucketIconImage.sprite = ResolveBucketSprite();
        bucketIconImage.color = Color.white;
        bucketIconImage.preserveAspect = true;
        bucketIconImage.raycastTarget = false;

        Button button = bucket.AddComponent<Button>();
        button.targetGraphic = buttonHitArea;
        button.onClick.AddListener(delegate
        {
            if (bucketAlbumOverlay != null)
            {
                RefreshBucketWorkshop(manager != null ? manager.GetBucketInventory() : null);
                bucketAlbumOverlay.SetActive(true);
            }
        });

        bucketDecorationImage = CreateRect(bucket.transform, "Decoration", new Vector2(0.72f, 0.72f), new Vector2(0.72f, 0.72f), Vector2.zero, new Vector2(34f, 34f)).gameObject.AddComponent<Image>();
        bucketDecorationImage.sprite = GetDecorationSprite(OceanDecorationReward.Seaweed);
        bucketDecorationImage.color = ColorForDecoration(OceanDecorationReward.Seaweed);
        bucketDecorationImage.raycastTarget = false;
        bucketDecorationImage.preserveAspect = true;

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
        shade.raycastTarget = true;

        GameObject card = CreatePanel(bucketAlbumOverlay.transform, "Card", OceanVisual.CardPanel);
        SetRaycastTarget(card, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1080f, 620f);

        GameObject header = CreateRect(card.transform, "Header", new Vector2(0.5f, 0.91f), new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(980f, 70f)).gameObject;
        CreateText(header.transform, "Title", "My Rhythm Bucket", new Vector2(0.34f, 0.5f), Vector2.zero, new Vector2(470f, 54f), 42, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        bucketAlbumShellText = CreateCounter(header.transform, "Shells", "Shells", "0", new Vector2(0.68f, 0.5f));
        bucketAlbumPearlText = CreateCounter(header.transform, "Pearls", "Pearls", "0", new Vector2(0.83f, 0.5f));
        CreateButton(header.transform, "CloseButton", "Close", new Vector2(0.96f, 0.5f), Vector2.zero, new Vector2(120f, 48f), CloseBucketAlbum);

        CreateBucketPreview(card.transform);
        CreateDecorationLibrary(card.transform);
        CreateDecorationDetail(card.transform);
        CreateButton(card.transform, "CloseButton", "Close", new Vector2(0.5f, 0.075f), Vector2.zero, new Vector2(200f, 56f), CloseBucketAlbum);
    }

    private Text CreateCounter(Transform parent, string name, string label, string value, Vector2 anchor)
    {
        GameObject counter = CreateRect(parent, name, anchor, anchor, Vector2.zero, new Vector2(142f, 48f)).gameObject;
        CreateText(counter.transform, "Label", label, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(132f, 20f), 15, FontStyle.Bold, new Color(0.78f, 0.95f, 1f), TextAnchor.MiddleCenter);
        return CreateText(counter.transform, "Value", value, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(132f, 24f), 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    private void CreateRewardToast(Transform parent)
    {
        rewardText = CreateText(parent, "RewardToast", "", new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(620f, 54f), 30, FontStyle.Bold, new Color(1f, 0.86f, 0.18f), TextAnchor.MiddleCenter);
        rewardText.gameObject.SetActive(false);
    }

    private void CreateSingingShellButton(Transform parent)
    {
        GameObject shell = CreatePanel(parent, "SingingShellButton", OceanVisual.SoftWhite);
        RectTransform rect = shell.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(140f, 82f);
        rect.sizeDelta = new Vector2(210f, 92f);

        Image shellBackground = shell.GetComponent<Image>();
        if (shellBackground != null)
        {
            shellBackground.raycastTarget = true;
        }

        Button button = shell.AddComponent<Button>();
        button.targetGraphic = shellBackground;
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
        singingShellButtonImage.raycastTarget = false;

        singingShellButtonText = CreateText(shell.transform, "Text", "Listen Game", new Vector2(0.66f, 0.5f), Vector2.zero, new Vector2(118f, 52f), 20, FontStyle.Bold, new Color(0.02f, 0.15f, 0.22f), TextAnchor.MiddleCenter);
        singingShellButton = shell;
    }

    private void CreateSoundMatchOverlay(Transform parent)
    {
        soundMatchOverlay = CreateRect(parent, "SoundMatchOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image shade = soundMatchOverlay.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.07f, 0.1f, 0.7f);
        shade.raycastTarget = true;

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
        shellIcon.raycastTarget = false;

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
        GameObject bucket = CreatePanel(parent, "BucketPreview", new Color(1f, 1f, 1f, 0f));
        SetRaycastTarget(bucket, false);
        RectTransform bucketRect = bucket.GetComponent<RectTransform>();
        bucketRect.anchorMin = new Vector2(0.47f, 0.5f);
        bucketRect.anchorMax = new Vector2(0.47f, 0.5f);
        bucketRect.anchoredPosition = Vector2.zero;
        bucketRect.sizeDelta = new Vector2(420f, 380f);

        bucketPreviewImage = CreateRect(bucket.transform, "BucketImage", new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), Vector2.zero, OceanVisual.BucketPreviewSize).gameObject.AddComponent<Image>();
        bucketPreviewImage.sprite = ResolveBucketSprite();
        bucketPreviewImage.color = Color.white;
        bucketPreviewImage.preserveAspect = true;
        bucketPreviewImage.raycastTarget = false;

        bucketSlots.Clear();
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.TopSlot, new Vector2(0.5f, 0.84f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.LeftSlot, new Vector2(0.24f, 0.5f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.RightSlot, new Vector2(0.76f, 0.5f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.FrontSlot, new Vector2(0.5f, 0.42f));
        CreateBucketSlot(bucket.transform, OceanBucketSlotId.CharmSlot, new Vector2(0.5f, 0.14f));
    }

    private void CreateBucketSlot(Transform parent, OceanBucketSlotId slotId, Vector2 anchor)
    {
        GameObject obj = CreateRect(parent, slotId.ToString(), anchor, anchor, Vector2.zero, OceanVisual.BucketSlotSize).gameObject;
        OceanBucketSlot slot = obj.AddComponent<OceanBucketSlot>();
        Sprite slotSprite = manager != null && manager.GetBucketSlotSprite() != null ? manager.GetBucketSlotSprite() : circleSprite;
        slot.Build(this, slotId, slotSprite, GetBucketSlotHighlightSprite(), uiFont);
        bucketSlots.Add(slot);
    }

    private void CreateDecorationLibrary(Transform parent)
    {
        GameObject library = CreatePanel(parent, "DecorationCollection", new Color(1f, 1f, 1f, 0.08f));
        SetRaycastTarget(library, false);
        RectTransform rect = library.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.48f, 0.49f);
        rect.anchorMax = new Vector2(0.48f, 0.49f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(330f, 410f);

        CreateText(library.transform, "Title", "My Decorations", new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(290f, 34f), 25, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        decorationLibraryRoot = CreateRect(library.transform, "Grid", new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.44f), Vector2.zero, new Vector2(286f, 320f));
        GridLayoutGroup grid = decorationLibraryRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(132f, 94f);
        grid.spacing = new Vector2(12f, 12f);
        grid.childAlignment = TextAnchor.MiddleCenter;

        unlockedDecorationTemplate = CreateDecorationTemplate(decorationLibraryRoot, "UnlockedItemTemplate", new Color(1f, 1f, 1f, 0.92f));
        lockedDecorationTemplate = CreateDecorationTemplate(decorationLibraryRoot, "LockedItemTemplate", new Color(0.18f, 0.28f, 0.34f, 0.92f));
        unlockedDecorationTemplate.SetActive(false);
        lockedDecorationTemplate.SetActive(false);

        bucketAlbumPrevPageButton = CreateButton(library.transform, "PrevPageButton", "<", new Vector2(0.08f, 0.5f), Vector2.zero, new Vector2(56f, 72f), delegate { ChangeBucketAlbumPage(-1); });
        bucketAlbumNextPageButton = CreateButton(library.transform, "NextPageButton", ">", new Vector2(0.92f, 0.5f), Vector2.zero, new Vector2(56f, 72f), delegate { ChangeBucketAlbumPage(1); });
        bucketAlbumPageText = CreateText(library.transform, "PageText", "1 / 1", new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(120f, 30f), 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    private GameObject CreateDecorationTemplate(Transform parent, string name, Color background)
    {
        GameObject item = CreatePanel(parent, name, background);
        SetRaycastTarget(item, true);
        CreateRect(item.transform, "SelectedBadge", new Vector2(0.14f, 0.82f), new Vector2(0.14f, 0.82f), Vector2.zero, new Vector2(24f, 24f)).gameObject.AddComponent<Image>().color = new Color(1f, 0.86f, 0.18f, 0.95f);
        CreateRect(item.transform, "EquippedBadge", new Vector2(0.86f, 0.82f), new Vector2(0.86f, 0.82f), Vector2.zero, new Vector2(24f, 24f)).gameObject.AddComponent<Image>().color = new Color(0.27f, 0.95f, 0.54f, 0.95f);
        CreateRect(item.transform, "LockBadge", new Vector2(0.86f, 0.82f), new Vector2(0.86f, 0.82f), Vector2.zero, new Vector2(24f, 24f)).gameObject.AddComponent<Image>().color = new Color(1f, 0.94f, 0.68f, 0.95f);
        Image icon = CreateRect(item.transform, "Icon", new Vector2(0.26f, 0.54f), new Vector2(0.26f, 0.54f), Vector2.zero, new Vector2(48f, 48f)).gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        CreateText(item.transform, "Name", "Decoration", new Vector2(0.66f, 0.68f), Vector2.zero, new Vector2(78f, 24f), 14, FontStyle.Bold, new Color(0.02f, 0.15f, 0.22f), TextAnchor.MiddleCenter);
        CreateText(item.transform, "Status", "Use", new Vector2(0.66f, 0.42f), Vector2.zero, new Vector2(78f, 22f), 12, FontStyle.Bold, new Color(0.12f, 0.28f, 0.48f), TextAnchor.MiddleCenter);
        GameObject progress = CreatePanel(item.transform, "Progress", new Color(1f, 1f, 1f, 0.16f));
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0.14f);
        progressRect.anchorMax = new Vector2(0.5f, 0.14f);
        progressRect.anchoredPosition = Vector2.zero;
        progressRect.sizeDelta = new Vector2(104f, 12f);
        Image fill = CreateRect(progress.transform, "Fill", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 12f)).gameObject.AddComponent<Image>();
        fill.color = new Color(0.27f, 0.95f, 0.54f, 1f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        CreateText(progress.transform, "Value", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 18f), 10, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        CreateText(item.transform, "Requirement", "", new Vector2(0.5f, 0.02f), Vector2.zero, new Vector2(116f, 18f), 10, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        return item;
    }

    private void CreateDecorationDetail(Transform parent)
    {
        GameObject detail = CreatePanel(parent, "DecorationDetail", new Color(1f, 1f, 1f, 0.08f));
        SetRaycastTarget(detail, false);
        RectTransform rect = detail.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.81f, 0.49f);
        rect.anchorMax = new Vector2(0.81f, 0.49f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(280f, 410f);

        decorationDetailIcon = CreateRect(detail.transform, "Icon", new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), Vector2.zero, new Vector2(82f, 82f)).gameObject.AddComponent<Image>();
        decorationDetailIcon.preserveAspect = true;
        decorationDetailIcon.raycastTarget = false;
        decorationDetailNameText = CreateText(detail.transform, "Name", "Decoration", new Vector2(0.5f, 0.63f), Vector2.zero, new Vector2(240f, 34f), 25, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        decorationDetailStatusText = CreateText(detail.transform, "Status", "Tap a decoration", new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(240f, 28f), 18, FontStyle.Bold, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);
        GameObject progress = CreatePanel(detail.transform, "Progress", new Color(1f, 1f, 1f, 0.16f));
        RectTransform progressRect = progress.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0.43f);
        progressRect.anchorMax = new Vector2(0.5f, 0.43f);
        progressRect.anchoredPosition = Vector2.zero;
        progressRect.sizeDelta = new Vector2(210f, 20f);
        decorationDetailProgressFill = CreateRect(progress.transform, "Fill", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 20f)).gameObject.AddComponent<Image>();
        decorationDetailProgressFill.color = new Color(0.27f, 0.95f, 0.54f, 1f);
        decorationDetailProgressFill.type = Image.Type.Filled;
        decorationDetailProgressFill.fillMethod = Image.FillMethod.Horizontal;
        decorationDetailProgressText = CreateText(progress.transform, "Value", "", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 22f), 14, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        GameObject requirement = CreateRect(detail.transform, "Requirement", new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(230f, 58f)).gameObject;
        decorationDetailRequirementIcon = CreateRect(requirement.transform, "Icon", new Vector2(0.16f, 0.5f), new Vector2(0.16f, 0.5f), Vector2.zero, new Vector2(34f, 34f)).gameObject.AddComponent<Image>();
        decorationDetailRequirementIcon.preserveAspect = true;
        decorationDetailRequirementIcon.raycastTarget = false;
        decorationDetailRequirementText = CreateText(requirement.transform, "Text", "Choose a decoration", new Vector2(0.62f, 0.5f), Vector2.zero, new Vector2(170f, 50f), 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        decorationDetailActionButton = CreateButton(detail.transform, "ActionButton", "Use", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(180f, 54f), delegate { SetChoosingBucketSlot(true); });
        decorationDetailActionObject = decorationDetailActionButton.gameObject;
        decorationDetailActionText = decorationDetailActionObject.GetComponentInChildren<Text>(true);
        bucketHintText = CreateText(detail.transform, "Hint", "Tap a decoration to see how to unlock it.", new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(240f, 42f), 15, FontStyle.Bold, new Color(1f, 0.94f, 0.68f), TextAnchor.MiddleCenter);
    }

    private void RefreshBucketWorkshop(OceanBucketInventory inventory)
    {
        if (bucketAlbumOverlay == null || inventory == null)
        {
            return;
        }

        choosingBucketSlot = false;
        if (!hasSelectedDecoration)
        {
            selectedDecoration = inventory.SelectedDecoration;
            hasSelectedDecoration = true;
        }
        KeepSelectedDecorationPageVisible();

        RefreshBucketHeader(inventory);
        RefreshBucketSlots(inventory);
        RefreshDecorationLibrary(inventory);
        RefreshDecorationDetail(inventory);
    }

    private void RefreshBucketHeader(OceanBucketInventory inventory)
    {
        if (inventory == null)
        {
            return;
        }

        if (bucketAlbumShellText != null)
        {
            bucketAlbumShellText.text = inventory.Shells.ToString();
        }
        if (bucketAlbumPearlText != null)
        {
            bucketAlbumPearlText.text = inventory.MusicPearls.ToString();
        }
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
        if (decorationLibraryRoot == null || inventory == null)
        {
            return;
        }

        for (int i = decorationLibraryRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = decorationLibraryRoot.GetChild(i);
            if (child != null && (child.name == "UnlockedItemTemplate" || child.name == "LockedItemTemplate"))
            {
                child.gameObject.SetActive(false);
                continue;
            }

            DestroyObject(child.gameObject);
        }

        OceanDecorationReward[] decorations = OceanBucketInventory.GetAllDecorations();
        int pageCount = BucketAlbumPageCount(decorations);
        bucketAlbumPageIndex = Mathf.Clamp(bucketAlbumPageIndex, 0, pageCount - 1);
        int firstItem = bucketAlbumPageIndex * BucketAlbumItemsPerPage;
        int lastItem = Mathf.Min(firstItem + BucketAlbumItemsPerPage, decorations.Length);
        for (int i = firstItem; i < lastItem; i++)
        {
            CreateDecorationItem(decorationLibraryRoot, decorations[i], inventory);
        }
        RefreshBucketAlbumPaging(decorations);
    }

    private void ChangeBucketAlbumPage(int delta)
    {
        OceanBucketInventory inventory = manager != null ? manager.GetBucketInventory() : null;
        if (inventory == null)
        {
            return;
        }

        OceanDecorationReward[] decorations = OceanBucketInventory.GetAllDecorations();
        int pageCount = BucketAlbumPageCount(decorations);
        int newPage = Mathf.Clamp(bucketAlbumPageIndex + delta, 0, pageCount - 1);
        if (newPage == bucketAlbumPageIndex)
        {
            RefreshBucketAlbumPaging(decorations);
            return;
        }

        bucketAlbumPageIndex = newPage;
        choosingBucketSlot = false;
        ClearBucketSlotHighlights();
        RefreshDecorationLibrary(inventory);
        RefreshDecorationDetail(inventory);
    }

    private int BucketAlbumPageCount(OceanDecorationReward[] decorations)
    {
        int count = decorations != null ? decorations.Length : 0;
        return Mathf.Max(1, Mathf.CeilToInt((float)count / BucketAlbumItemsPerPage));
    }

    private void KeepSelectedDecorationPageVisible()
    {
        if (!hasSelectedDecoration)
        {
            bucketAlbumPageIndex = 0;
            return;
        }

        OceanDecorationReward[] decorations = OceanBucketInventory.GetAllDecorations();
        int selectedIndex = -1;
        for (int i = 0; i < decorations.Length; i++)
        {
            if (decorations[i] == selectedDecoration)
            {
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex < 0)
        {
            bucketAlbumPageIndex = 0;
            return;
        }

        bucketAlbumPageIndex = Mathf.Clamp(selectedIndex / BucketAlbumItemsPerPage, 0, BucketAlbumPageCount(decorations) - 1);
    }

    private void RefreshBucketAlbumPaging(OceanDecorationReward[] decorations)
    {
        int pageCount = BucketAlbumPageCount(decorations);
        bucketAlbumPageIndex = Mathf.Clamp(bucketAlbumPageIndex, 0, pageCount - 1);

        if (bucketAlbumPrevPageButton != null)
        {
            bucketAlbumPrevPageButton.interactable = bucketAlbumPageIndex > 0;
        }
        if (bucketAlbumNextPageButton != null)
        {
            bucketAlbumNextPageButton.interactable = bucketAlbumPageIndex < pageCount - 1;
        }
        if (bucketAlbumPageText != null)
        {
            bucketAlbumPageText.text = (bucketAlbumPageIndex + 1) + " / " + pageCount;
        }
    }

    private void CreateDecorationItem(Transform parent, OceanDecorationReward reward, OceanBucketInventory inventory)
    {
        bool unlocked = inventory.IsDecorationUnlocked(reward);
        GameObject template = unlocked ? unlockedDecorationTemplate : lockedDecorationTemplate;
        GameObject item = template != null ? Instantiate(template, parent, false) : CreatePanel(parent, reward.ToString() + "Item", unlocked ? new Color(1f, 1f, 1f, 0.9f) : new Color(0.25f, 0.31f, 0.36f, 0.88f));
        item.name = reward + "Item";
        item.SetActive(true);
        SetRaycastTarget(item, true);

        bool equipped = IsDecorationEquipped(inventory, reward);
        bool selected = hasSelectedDecoration && selectedDecoration == reward;
        OceanDecorationUnlockRequirement requirement = inventory.GetUnlockProgress(reward);

        Image icon = FindImage(item.transform, "Icon");
        if (icon == null)
        {
            icon = CreateRect(item.transform, "Icon", new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(44f, 44f)).gameObject.AddComponent<Image>();
        }
        icon.sprite = GetDecorationSprite(reward);
        icon.color = unlocked ? ColorForDecoration(reward) : new Color(0.55f, 0.6f, 0.65f, 0.55f);
        icon.raycastTarget = false;
        icon.preserveAspect = true;

        SetOptionalText(item.transform, "Name", DecorationLabel(reward));
        SetOptionalText(item.transform, "Label", unlocked ? DecorationLabel(reward) : "Locked");
        SetOptionalText(item.transform, "Status", DecorationItemStatus(unlocked, equipped, selected));
        SetOptionalText(item.transform, "Progress/Value", ProgressText(requirement, unlocked));

        Image progressFill = FindImage(item.transform, "Progress/Fill");
        if (progressFill != null)
        {
            progressFill.fillAmount = UnlockProgress(requirement, unlocked);
        }

        SetOptionalActive(item.transform, "LockBadge", !unlocked);
        SetOptionalActive(item.transform, "SelectedBadge", selected && !equipped);
        SetOptionalActive(item.transform, "EquippedBadge", equipped);
        SetOptionalText(item.transform, "Requirement", RequirementText(requirement, unlocked, reward));

        OceanDecorationDragItem dragItem = item.GetComponent<OceanDecorationDragItem>();
        if (dragItem == null)
        {
            dragItem = item.AddComponent<OceanDecorationDragItem>();
        }
        dragItem.Build(this, reward, unlocked);

        Button button = item.GetComponent<Button>();
        if (button == null)
        {
            button = item.AddComponent<Button>();
        }
        Graphic graphic = item.GetComponent<Graphic>();
        if (button.targetGraphic == null && graphic != null)
        {
            button.targetGraphic = graphic;
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate
        {
            SelectDecoration(reward);
        });
    }

    private void SelectDecoration(OceanDecorationReward reward)
    {
        OceanBucketInventory inventory = manager != null ? manager.GetBucketInventory() : null;
        if (inventory == null)
        {
            return;
        }

        selectedDecoration = reward;
        hasSelectedDecoration = true;
        choosingBucketSlot = false;
        KeepSelectedDecorationPageVisible();
        ClearBucketSlotHighlights();
        RefreshDecorationLibrary(inventory);
        RefreshDecorationDetail(inventory);
    }

    private void RefreshDecorationDetail(OceanBucketInventory inventory)
    {
        if (inventory == null || !hasSelectedDecoration)
        {
            return;
        }

        bool unlocked = inventory.IsDecorationUnlocked(selectedDecoration);
        bool equipped = IsDecorationEquipped(inventory, selectedDecoration);
        OceanDecorationUnlockRequirement requirement = inventory.GetUnlockProgress(selectedDecoration);

        if (decorationDetailIcon != null)
        {
            decorationDetailIcon.sprite = GetDecorationSprite(selectedDecoration);
            decorationDetailIcon.color = unlocked ? ColorForDecoration(selectedDecoration) : new Color(0.55f, 0.6f, 0.65f, 0.72f);
            decorationDetailIcon.preserveAspect = true;
        }
        if (decorationDetailNameText != null)
        {
            decorationDetailNameText.text = DecorationLabel(selectedDecoration);
        }
        if (decorationDetailStatusText != null)
        {
            decorationDetailStatusText.text = DetailStatusText(unlocked, equipped);
        }
        if (decorationDetailProgressFill != null)
        {
            decorationDetailProgressFill.fillAmount = UnlockProgress(requirement, unlocked);
        }
        if (decorationDetailProgressText != null)
        {
            decorationDetailProgressText.text = ProgressText(requirement, unlocked);
        }
        if (decorationDetailRequirementText != null)
        {
            decorationDetailRequirementText.text = RequirementText(requirement, unlocked, selectedDecoration);
        }
        if (decorationDetailActionButton != null)
        {
            decorationDetailActionButton.interactable = unlocked;
            decorationDetailActionButton.onClick.RemoveAllListeners();
            decorationDetailActionButton.onClick.AddListener(delegate
            {
                SetChoosingBucketSlot(!choosingBucketSlot);
            });
        }
        if (decorationDetailActionObject != null)
        {
            decorationDetailActionObject.SetActive(true);
        }
        if (decorationDetailActionText != null)
        {
            decorationDetailActionText.text = unlocked ? choosingBucketSlot ? "Cancel" : equipped ? "Move" : "Use" : "Locked";
        }

        ShowBucketHint(unlocked ? choosingBucketSlot ? "Tap a bucket spot" : "Tap Use, then tap a bucket spot" : RequirementText(requirement, false, selectedDecoration));
    }

    private void SetChoosingBucketSlot(bool value)
    {
        OceanBucketInventory inventory = manager != null ? manager.GetBucketInventory() : null;
        if (inventory == null || !hasSelectedDecoration || !inventory.IsDecorationUnlocked(selectedDecoration))
        {
            choosingBucketSlot = false;
            ClearBucketSlotHighlights();
            RefreshDecorationDetail(inventory);
            return;
        }

        choosingBucketSlot = value;
        for (int i = 0; i < bucketSlots.Count; i++)
        {
            if (bucketSlots[i] != null)
            {
                bucketSlots[i].SetHighlighted(choosingBucketSlot);
            }
        }
        RefreshDecorationDetail(inventory);
    }

    public void ShowDecorationInfo(OceanDecorationReward reward)
    {
        OceanBucketInventory inventory = manager != null ? manager.GetBucketInventory() : null;
        if (inventory == null)
        {
            return;
        }

        selectedDecoration = reward;
        hasSelectedDecoration = true;
        choosingBucketSlot = false;
        ClearBucketSlotHighlights();
        RefreshDecorationLibrary(inventory);
        RefreshDecorationDetail(inventory);

        if (inventory.IsDecorationUnlocked(reward))
        {
            ShowBucketHint("Tap Use, then tap a bucket spot");
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

    private void CloseBucketAlbum()
    {
        choosingBucketSlot = false;
        ClearBucketSlotHighlights();
        if (bucketAlbumOverlay != null)
        {
            bucketAlbumOverlay.SetActive(false);
        }
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
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
                return EquipDecorationToSlot(reward, slot, inventory);
            }
        }

        return false;
    }

    public bool TryEquipSelectedDecorationToSlot(OceanBucketSlot slot)
    {
        OceanBucketInventory inventory = manager != null ? manager.GetBucketInventory() : null;
        if (!choosingBucketSlot || !hasSelectedDecoration || slot == null || inventory == null)
        {
            return false;
        }

        if (!inventory.IsDecorationUnlocked(selectedDecoration))
        {
            ShowDecorationInfo(selectedDecoration);
            return false;
        }

        return EquipDecorationToSlot(selectedDecoration, slot, inventory);
    }

    private bool EquipDecorationToSlot(OceanDecorationReward reward, OceanBucketSlot slot, OceanBucketInventory inventory)
    {
        if (slot == null || inventory == null || !inventory.IsDecorationUnlocked(reward))
        {
            return false;
        }

        selectedDecoration = reward;
        hasSelectedDecoration = true;
        choosingBucketSlot = false;
        inventory.SetSlotDecoration(slot.slotId, reward);
        slot.SetDecoration(reward, GetDecorationSprite(reward), ColorForDecoration(reward));
        UpdateBucket(inventory);
        RefreshBucketSlots(inventory);
        RefreshDecorationLibrary(inventory);
        RefreshDecorationDetail(inventory);
        ClearBucketSlotHighlights();
        ShowBucketHint(DecorationLabel(reward) + " placed on " + SlotLabel(slot.slotId));
        return true;
    }

    private bool IsDecorationEquipped(OceanBucketInventory inventory, OceanDecorationReward reward)
    {
        if (inventory == null)
        {
            return false;
        }

        OceanBucketSlotId[] slots = BucketSlotIds();
        for (int i = 0; i < slots.Length; i++)
        {
            if (inventory.HasSlotDecoration(slots[i]) && inventory.GetSlotDecoration(slots[i]) == reward)
            {
                return true;
            }
        }

        return false;
    }

    private OceanBucketSlotId EquippedSlot(OceanBucketInventory inventory, OceanDecorationReward reward)
    {
        OceanBucketSlotId[] slots = BucketSlotIds();
        for (int i = 0; i < slots.Length; i++)
        {
            if (inventory != null && inventory.HasSlotDecoration(slots[i]) && inventory.GetSlotDecoration(slots[i]) == reward)
            {
                return slots[i];
            }
        }

        return OceanBucketSlotId.FrontSlot;
    }

    private OceanBucketSlotId[] BucketSlotIds()
    {
        return new OceanBucketSlotId[]
        {
            OceanBucketSlotId.TopSlot,
            OceanBucketSlotId.LeftSlot,
            OceanBucketSlotId.RightSlot,
            OceanBucketSlotId.FrontSlot,
            OceanBucketSlotId.CharmSlot
        };
    }

    private void SetOptionalText(Transform root, string path, string value)
    {
        Text text = FindText(root, path);
        if (text != null)
        {
            text.text = value;
        }
    }

    private void SetOptionalActive(Transform root, string path, bool active)
    {
        Transform child = FindTransform(root, path);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private string DecorationItemStatus(bool unlocked, bool equipped, bool selected)
    {
        if (!unlocked)
        {
            return "Locked";
        }
        if (equipped)
        {
            return "Equipped";
        }
        if (selected)
        {
            return "Selected";
        }

        return "Use";
    }

    private string DetailStatusText(bool unlocked, bool equipped)
    {
        if (!unlocked)
        {
            return "Locked";
        }
        if (equipped)
        {
            OceanBucketInventory inventory = manager != null ? manager.GetBucketInventory() : null;
            return "Equipped on " + SlotLabel(EquippedSlot(inventory, selectedDecoration));
        }

        return "Ready to decorate";
    }

    private float UnlockProgress(OceanDecorationUnlockRequirement requirement, bool unlocked)
    {
        if (unlocked || requirement.requiredCount <= 0)
        {
            return 1f;
        }

        return Mathf.Clamp01((float)requirement.currentCount / requirement.requiredCount);
    }

    private string ProgressText(OceanDecorationUnlockRequirement requirement, bool unlocked)
    {
        if (unlocked || requirement.requiredCount <= 0)
        {
            return "Unlocked";
        }

        return Mathf.Clamp(requirement.currentCount, 0, requirement.requiredCount) + " / " + requirement.requiredCount;
    }

    private string RequirementText(OceanDecorationUnlockRequirement requirement, bool unlocked, OceanDecorationReward reward)
    {
        if (unlocked || requirement.requiredCount <= 0)
        {
            return "Unlocked. Choose a bucket spot.";
        }
        if (requirement.usesMusicPearls)
        {
            return "Earn " + requirement.Remaining + " more Music Pearls";
        }

        return "Catch " + requirement.Remaining + " more " + FishName(requirement.fishType);
    }

    private Sprite GetBucketSlotHighlightSprite()
    {
        OceanBucketAlbumAssets assets = manager != null ? manager.GetBucketAlbumAssets() : null;
        if (assets != null && assets.slotHighlightSprite != null)
        {
            return assets.slotHighlightSprite;
        }

        return circleSprite;
    }

    private string SlotLabel(OceanBucketSlotId slotId)
    {
        if (slotId == OceanBucketSlotId.TopSlot)
        {
            return "Top";
        }
        if (slotId == OceanBucketSlotId.LeftSlot)
        {
            return "Left";
        }
        if (slotId == OceanBucketSlotId.RightSlot)
        {
            return "Right";
        }
        if (slotId == OceanBucketSlotId.CharmSlot)
        {
            return "Charm";
        }

        return "Front";
    }

    private Sprite GetDecorationSprite(OceanDecorationReward reward)
    {
        Sprite sprite = manager != null ? manager.GetDecorationSprite(reward) : null;
        if (sprite != null)
        {
            return sprite;
        }

        return circleSprite;
    }

    private Sprite ResolveBucketSprite()
    {
        Sprite sprite = manager != null ? manager.GetBucketSprite() : null;
        if (sprite != null)
        {
            return sprite;
        }

        return circleSprite;
    }

    private void ApplyManagerSprites()
    {
        ApplyTapButtonSprite(false);

        Sprite bucketSprite = ResolveBucketSprite();
        if (bucketIconImage != null)
        {
            bucketIconImage.sprite = bucketSprite;
            bucketIconImage.color = Color.white;
        }
        if (bucketPreviewImage != null)
        {
            bucketPreviewImage.sprite = bucketSprite;
            bucketPreviewImage.color = Color.white;
        }

        if (bucketDecorationImage != null)
        {
            bucketDecorationImage.sprite = GetDecorationSprite(OceanDecorationReward.Seaweed);
            bucketDecorationImage.color = ColorForDecoration(OceanDecorationReward.Seaweed);
            bucketDecorationImage.preserveAspect = true;
        }
        if (singingShellButtonImage != null)
        {
            singingShellButtonImage.sprite = manager != null && manager.GetSingingShellSprite() != null ? manager.GetSingingShellSprite() : circleSprite;
            singingShellButtonImage.preserveAspect = true;
        }
        if (netCursor != null)
        {
            netCursor.Build(circleSprite, manager != null ? manager.GetNetSprite() : null);
        }
    }

    private string FreePondPreviewDescription(OceanLesson lesson)
    {
        if (lesson == null)
        {
            return "";
        }

        if (lesson.beatsPerBar == 3)
        {
            return "You are hearing 3/4 time. This meter often feels flowing, dancing, and gently expressive. Listen to the three-beat cycle before you play.";
        }
        if (lesson.beatsPerBar == 2)
        {
            return "You are hearing 2/4 time. This meter often feels steady, marching, and direct. Listen to the strong two-beat pulse before you play.";
        }
        if (lesson.beatsPerBar == 6)
        {
            return "You are hearing 6/8 time. This meter often feels swaying, rolling, and wave-like. Listen to the two larger pulses inside the six beats before you play.";
        }

        return "You are hearing 4/4 time. This meter often feels grounded, balanced, and familiar. Listen to the regular four-beat pulse before you play.";
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

    private void ClearImageOverrides()
    {
        pendingImageOverrides.Clear();
        pendingNameImageOverrides.Clear();
    }

    private void CaptureRectTransformOverrides(Transform root)
    {
        pendingRectOverrides.Clear();
        pendingNameRectOverrides.Clear();

        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null)
            {
                continue;
            }

            RectTransformOverride visual = new RectTransformOverride();
            visual.anchorMin = rect.anchorMin;
            visual.anchorMax = rect.anchorMax;
            visual.anchoredPosition = rect.anchoredPosition;
            visual.sizeDelta = rect.sizeDelta;
            visual.pivot = rect.pivot;
            visual.localScale = rect.localScale;

            pendingRectOverrides[IndexedPath(root, rect)] = visual;
            pendingNameRectOverrides[NamePath(root, rect)] = visual;
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

    private void ApplyPendingRectTransformOverrides()
    {
        if (pendingRectOverrides.Count == 0 && pendingNameRectOverrides.Count == 0)
        {
            return;
        }

        if (canvas == null)
        {
            return;
        }

        Transform root = canvas.transform;
        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null)
            {
                continue;
            }

            RectTransformOverride visual;
            if (!pendingRectOverrides.TryGetValue(IndexedPath(root, rect), out visual)
                && !pendingNameRectOverrides.TryGetValue(NamePath(root, rect), out visual))
            {
                continue;
            }

            rect.anchorMin = visual.anchorMin;
            rect.anchorMax = visual.anchorMax;
            rect.anchoredPosition = visual.anchoredPosition;
            rect.sizeDelta = visual.sizeDelta;
            rect.pivot = visual.pivot;
            rect.localScale = visual.localScale;
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

        Button retryButton = CreateButton(parent, "RetryButton", "R", new Vector2(0f, 1f), new Vector2(112f, -46f), new Vector2(58f, 58f), delegate
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
        Image image = obj.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }
        button.targetGraphic = image;
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
        image.raycastTarget = false;
        return obj;
    }

    private void SetRaycastTarget(GameObject obj, bool enabled)
    {
        if (obj == null)
        {
            return;
        }

        Image image = obj.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = enabled;
        }
    }

    private void NormalizeOceanRaycasts(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
            {
                continue;
            }

            image.raycastTarget = false;
        }

        EnableRaycast(root, "OceanRoot/CatchBucketButton");
        EnableRaycast(root, "OceanRoot/TapButton");
        EnableRaycast(root, "OceanRoot/ParentHelpButton");
        EnableRaycast(root, "OceanRoot/BackButton");
        EnableRaycast(root, "OceanRoot/RetryButton");
        EnableRaycast(root, "OceanRoot/PauseButton");
        EnableRaycast(root, "OceanRoot/SingingShellButton");
        EnableRaycast(root, "OceanRoot/BucketAlbumOverlay");
        EnableRaycast(root, "OceanRoot/BucketAlbumOverlay/Card/CloseButton");
        EnableRaycast(root, "OceanRoot/BucketAlbumOverlay/Card/Header/CloseButton");
        EnableRaycast(root, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/PrevPageButton");
        EnableRaycast(root, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/NextPageButton");
        EnableRaycast(root, "OceanRoot/BucketAlbumOverlay/Card/DecorationDetail/ActionButton");
        EnableRaycast(root, "OceanRoot/ParentHelpOverlay");
        EnableRaycast(root, "OceanRoot/ParentHelpOverlay/Card/CloseButton");
        EnableRaycast(root, "OceanRoot/ParentHelpOverlay/Card/BackButton");
        EnableRaycast(root, "OceanRoot/PondCompleteOverlay");
        EnableRaycast(root, "OceanRoot/PondCompleteOverlay/Card/PlayAgainButton");
        EnableRaycast(root, "OceanRoot/PondCompleteOverlay/Card/BackToStartButton");
        EnableRaycast(root, "OceanRoot/CompleteOverlay");
        EnableRaycast(root, "OceanRoot/BeatCardOverlay");
        EnableRaycast(root, "OceanRoot/BeatCardOverlay/Card/TryButton");
        EnableRaycast(root, "OceanRoot/BeatCardOverlay/Card/SkipButton");
        EnableRaycast(root, "OceanRoot/BeatCardOverlay/Card/CloseButton");
        EnableRaycast(root, "OceanRoot/BeatCardOverlay/Cards/IntroCard/CloseButton");
        EnableRaycast(root, "OceanRoot/BeatCardOverlay/Cards/FourFourCard/CloseButton");
        EnableRaycast(root, "OceanRoot/BeatCardOverlay/Cards/ThreeFourCard/CloseButton");
        EnableRaycast(root, "OceanRoot/BeatCardOverlay/Cards/TwoFourCard/CloseButton");
        EnableRaycast(root, "OceanRoot/BeatCardOverlay/Cards/SixEightCard/CloseButton");
        EnableRaycast(root, "OceanRoot/SoundMatchOverlay");
        EnableRaycast(root, "OceanRoot/SoundMatchOverlay/Card/ReplayButton");
        EnableRaycast(root, "OceanRoot/SoundMatchOverlay/Card/CloseButton");

        Transform bucketPreview = FindTransform(root, "OceanRoot/BucketAlbumOverlay/Card/BucketPreview");
        if (bucketPreview != null)
        {
            OceanBucketSlot[] slots = bucketPreview.GetComponentsInChildren<OceanBucketSlot>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                Image slotImage = slots[i].GetComponent<Image>();
                if (slotImage != null)
                {
                    slotImage.raycastTarget = true;
                }
            }
        }

        Transform library = FindTransform(root, "OceanRoot/BucketAlbumOverlay/Card/DecorationCollection/Grid");
        if (library == null)
        {
            library = FindTransform(root, "OceanRoot/BucketAlbumOverlay/Card/DecorationLibrary/Grid");
        }
        if (library != null)
        {
            OceanDecorationDragItem[] items = library.GetComponentsInChildren<OceanDecorationDragItem>(true);
            for (int i = 0; i < items.Length; i++)
            {
                Image itemImage = items[i].GetComponent<Image>();
                if (itemImage != null)
                {
                    itemImage.raycastTarget = true;
                }
            }
        }
    }

    private void EnableRaycast(Transform root, string path)
    {
        Transform target = FindTransform(root, path);
        if (target == null)
        {
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }
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
