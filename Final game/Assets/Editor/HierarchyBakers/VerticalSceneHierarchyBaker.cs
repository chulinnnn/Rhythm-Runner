using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerticalSceneHierarchyBaker
{
    private const string VerticalRunnerScenePath = "Assets/Scenes/VerticalRunner.unity";

    [MenuItem("Tools/Rhythm Runner/Rebuild Vertical Runner Hierarchy")]
    public static void RebuildVerticalScenes()
    {
        string originalScenePath = SceneManager.GetActiveScene().path;

        RebuildScene(VerticalRunnerScenePath, VerticalRunnerMode.Tutorial);

        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        AssetDatabase.SaveAssets();
    }

    private static void RebuildScene(string scenePath, VerticalRunnerMode mode)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        VerticalRunnerManager manager = Object.FindObjectOfType<VerticalRunnerManager>(true);
        if (manager == null)
        {
            GameObject managerObject = new GameObject("VerticalRunnerManager");
            manager = managerObject.AddComponent<VerticalRunnerManager>();
        }
        manager.mode = mode;
        manager.scenePolicy.useExistingSceneObjects = true;
        manager.scenePolicy.autoCreateMissingObjects = true;
        manager.scenePolicy.overrideCameraTransform = true;
        manager.scenePolicy.rebuildUiOnPlay = false;
        manager.scenePolicy.preserveExistingImageOverrides = true;
        manager.scenePolicy.runtimeGeneratedRootName = "VerticalRunnerRuntime";

        VerticalBeatSpawner spawner = manager.GetComponent<VerticalBeatSpawner>();
        if (spawner == null)
        {
            spawner = manager.gameObject.AddComponent<VerticalBeatSpawner>();
        }

        VerticalRunnerUI ui = manager.GetComponent<VerticalRunnerUI>();
        if (ui == null)
        {
            ui = manager.gameObject.AddComponent<VerticalRunnerUI>();
        }

        VerticalRunnerCamera cameraController = manager.GetComponent<VerticalRunnerCamera>();
        if (cameraController == null)
        {
            manager.gameObject.AddComponent<VerticalRunnerCamera>();
        }

        GameObject runtime = FindOrCreateRoot("VerticalRunnerRuntime");
        runtime.transform.position = Vector3.zero;

        VerticalRunnerTemplates templates = EnsureTemplates(runtime.transform);
        EnsureScrollingBackground();
        EnsureCanvas(ui);
        EnsureCamera();

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(spawner);
        EditorUtility.SetDirty(ui);
        EditorUtility.SetDirty(templates);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static VerticalRunnerTemplates EnsureTemplates(Transform runtimeRoot)
    {
        GameObject templatesObject = FindOrCreateRoot("VerticalRunnerTemplates");
        VerticalRunnerTemplates templates = templatesObject.GetComponent<VerticalRunnerTemplates>();
        if (templates == null)
        {
            templates = templatesObject.AddComponent<VerticalRunnerTemplates>();
        }
        templates.runtimeRoot = runtimeRoot;

        templates.playerTemplate = EnsureSpriteTemplate(templatesObject.transform, "PlayerTemplate", new Vector3(0.48f, 0.48f, 1f), new Color(0.25f, 0.82f, 1f, 1f), 5, true);
        EnsureCircleCollider(templates.playerTemplate, 0.48f, true);
        if (templates.playerTemplate.GetComponent<VerticalRunnerPlayer>() == null)
        {
            templates.playerTemplate.AddComponent<VerticalRunnerPlayer>();
        }
        templates.playerTemplate.SetActive(false);

        templates.platformTemplate = EnsureSpriteTemplate(templatesObject.transform, "PlatformTemplate", new Vector3(1.3f, 0.36f, 1f), new Color(0.35f, 0.82f, 0.45f, 1f), 0, false);
        EnsureBoxCollider(templates.platformTemplate, new Vector2(1.3f, 0.34f), false);
        templates.platformTemplate.SetActive(false);

        templates.longPlatformTemplate = EnsureSpriteTemplate(templatesObject.transform, "LongPlatformTemplate", new Vector3(1.55f, 0.42f, 1f), new Color(0.58f, 0.42f, 0.95f, 1f), 0, false);
        EnsureBoxCollider(templates.longPlatformTemplate, new Vector2(1.55f, 0.38f), false);
        templates.longPlatformTemplate.SetActive(false);

        templates.coinTemplate = EnsureSpriteTemplate(templatesObject.transform, "CoinTemplate", new Vector3(0.32f, 0.32f, 1f), new Color(1f, 0.84f, 0.16f, 1f), 2, true);
        EnsureCircleCollider(templates.coinTemplate, 0.45f, true);
        templates.coinTemplate.SetActive(false);

        templates.obstacleTemplate = EnsureSpriteTemplate(templatesObject.transform, "ObstacleTemplate", new Vector3(0.52f, 0.52f, 1f), new Color(1f, 0.24f, 0.22f, 1f), 2, true);
        EnsureCircleCollider(templates.obstacleTemplate, 0.48f, true);
        templates.obstacleTemplate.SetActive(false);

        templates.finishTemplate = EnsureSpriteTemplate(templatesObject.transform, "FinishTemplate", new Vector3(2.2f, 0.22f, 1f), new Color(1f, 0.86f, 0.18f, 1f), 1, false);
        templates.finishTemplate.SetActive(false);

        templates.worldLabelTemplate = EnsureTextTemplate(templatesObject.transform, "WorldLabelTemplate");
        templates.worldLabelTemplate.SetActive(false);

        return templates;
    }

    private static void EnsureScrollingBackground()
    {
        GameObject backgroundObject = GameObject.Find("vertical");
        bool createdObject = false;
        if (backgroundObject == null)
        {
            backgroundObject = new GameObject("vertical");
            createdObject = true;
        }

        Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Background/vertical.png");
        SpriteRenderer renderer = backgroundObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = backgroundObject.AddComponent<SpriteRenderer>();
        }
        if (renderer.sprite == null)
        {
            renderer.sprite = defaultSprite;
        }
        if (createdObject)
        {
            renderer.sortingOrder = -50;
        }

        VerticalScrollingBackground background = backgroundObject.GetComponent<VerticalScrollingBackground>();
        bool createdComponent = false;
        if (background == null)
        {
            background = backgroundObject.AddComponent<VerticalScrollingBackground>();
            createdComponent = true;
        }
        if (background.backgroundSprite == null)
        {
            background.backgroundSprite = renderer.sprite != null ? renderer.sprite : defaultSprite;
        }
        if (createdComponent)
        {
            background.sortingOrder = -50;
            background.tileCount = 4;
            background.fitWidthToCamera = true;
            background.widthPadding = 0.35f;
            background.verticalOffset = 0f;
            background.cameraParallax = 0.2f;
            background.autoScrollSpeed = 0f;
        }
        background.UpdateTiles();
        EditorUtility.SetDirty(backgroundObject);
        EditorUtility.SetDirty(background);
    }

    private static void EnsureCanvas(VerticalRunnerUI ui)
    {
        GameObject canvasObject = FindOrCreateUiRoot("VerticalRunnerCanvas");
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 160;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        CreateHud(canvasObject.transform);
        CreateResultOverlay(canvasObject.transform);
        CreateTutorialBriefingOverlay(canvasObject.transform);
        CreateGameRulesOverlay(canvasObject.transform, "GameRulesOverlay", "Climb Up", "Start");
        CreateGameRulesOverlay(canvasObject.transform, "TutorialCompleteRulesOverlay", "Ready", "Start");
        EnsureGameControls(canvasObject.transform);
        EditorUtility.SetDirty(canvasObject);
    }

    private static void CreateHud(Transform parent)
    {
        GameObject top = EnsureUiPanel(parent, "TopHud", new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(1080f, 92f), new Color(0.02f, 0.08f, 0.13f, 0.78f));
        EnsureText(top.transform, "Title", "Monkey Climb", new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(620f, 42f), 32, FontStyle.Bold, Color.white);
        EnsureText(top.transform, "Subtitle", "Jump up", new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(720f, 30f), 20, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        EnsureHudCounter(top.transform, "Misses", "0", new Vector2(0.1f, 0.5f), new Vector2(180f, 38f));
        EnsureHudCounter(top.transform, "Coins", "0", new Vector2(0.88f, 0.68f), new Vector2(170f, 34f));
        EnsureHudCounter(top.transform, "Combo", "0/0", new Vector2(0.88f, 0.28f), new Vector2(190f, 30f));

        GameObject objective = EnsureUiPanel(parent, "ObjectivePanel", new Vector2(0f, 0.5f), new Vector2(170f, 18f), new Vector2(330f, 230f), new Color(0.02f, 0.08f, 0.13f, 0.68f));
        EnsureText(objective.transform, "Objective", "Climb up", new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(286f, 52f), 22, FontStyle.Bold, Color.white);
        EnsureText(objective.transform, "Progress", "o", new Vector2(0.5f, 0.51f), Vector2.zero, new Vector2(220f, 46f), 30, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        EnsureText(objective.transform, "Rules", "Space", new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(288f, 92f), 18, FontStyle.Normal, new Color(0.83f, 0.98f, 1f));
        EnsureTutorialImages(objective.transform);

        GameObject bottom = EnsureUiPanel(parent, "BottomHud", new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(820f, 130f), new Color(0.02f, 0.08f, 0.13f, 0.72f));
        EnsureText(bottom.transform, "Feedback", "Ready", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(620f, 42f), 30, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));

        GameObject beatLane = EnsureUiRect(bottom.transform, "BeatLane", new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(300f, 44f));
        EnsureBeatDots(beatLane.transform);
        EnsureOptionalButton(bottom.transform, "BeatVisualToggleButton", "Beat: ON", new Vector2(0.18f, 0.38f), Vector2.zero, new Vector2(126f, 40f));
        GameObject progress = EnsureUiPanel(bottom.transform, "Progress", new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(540f, 18f), new Color(1f, 1f, 1f, 0.18f));
        Image fill = EnsureUiRect(progress.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 18f)).GetComponent<Image>();
        if (fill == null)
        {
            fill = progress.transform.Find("Fill").gameObject.AddComponent<Image>();
        }
        fill.color = new Color(0.27f, 0.95f, 0.54f, 1f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;
        EnsureText(bottom.transform, "ProgressText", "0%", new Vector2(0.84f, 0.12f), Vector2.zero, new Vector2(90f, 26f), 18, FontStyle.Bold, Color.white);
    }

    private static void EnsureGameControls(Transform parent)
    {
        GameObject controls = EnsureUiRect(parent, "GameControls", new Vector2(1f, 1f), new Vector2(-142f, -128f), new Vector2(260f, 58f));
        EnsureButton(controls.transform, "RetryButton", "Retry", new Vector2(0.28f, 0.5f), Vector2.zero, new Vector2(116f, 48f));
        EnsureButton(controls.transform, "BackButton", "Back", new Vector2(0.74f, 0.5f), Vector2.zero, new Vector2(116f, 48f));
        controls.SetActive(false);
    }

    private static void EnsureHudCounter(Transform parent, string name, string value, Vector2 anchor, Vector2 size)
    {
        GameObject counter = EnsureMissingUiRect(parent, name, anchor, Vector2.zero, size);
        Text legacyText = counter.GetComponent<Text>();
        if (legacyText != null)
        {
            legacyText.text = "";
            legacyText.raycastTarget = false;
        }

        GameObject icon = EnsureMissingUiRect(counter.transform, "Icon", new Vector2(0.18f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
        Image iconImage = icon.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = icon.AddComponent<Image>();
        }
        Color iconColor = iconImage.color;
        iconColor.a = 1f;
        iconImage.color = iconColor;
        iconImage.raycastTarget = false;

        Text valueText = EnsureMissingText(counter.transform, "Value", value, new Vector2(0.66f, 0.5f), Vector2.zero, new Vector2(size.x * 0.58f, size.y), 21, FontStyle.Bold, Color.white);
        valueText.raycastTarget = false;
    }

    private static void EnsureTutorialImages(Transform objective)
    {
        GameObject root = EnsureMissingUiRect(objective, "TutorialImages", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 110f));
        string[] names =
        {
            "BeatJumpImage",
            "HaystackClimbImage",
            "BananaPickupImage",
            "ParrotDodgeImage",
            "BigHaystackJumpImage",
            "MiniBananaClimbImage"
        };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject slot = EnsureMissingUiRect(root.transform, names[i], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 110f));
            Image image = slot.GetComponent<Image>();
            if (image == null)
            {
                image = slot.AddComponent<Image>();
                image.color = Color.white;
            }
            image.raycastTarget = false;
            slot.SetActive(false);
        }
    }

    private static void CreateResultOverlay(Transform parent)
    {
        GameObject overlay = EnsureFullscreenPanel(parent, "VerticalRunnerResult", new Color(0.01f, 0.04f, 0.07f, 0.82f));
        GameObject card = EnsureUiPanel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 470f), new Color(0.04f, 0.28f, 0.38f, 0.98f));
        EnsureText(card.transform, "Title", "Climbed Up", new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(540f, 58f), 40, FontStyle.Bold, Color.white);
        EnsureText(card.transform, "Stats", "", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(500f, 220f), 24, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        EnsureButton(card.transform, "Retry", "Retry", new Vector2(0.35f, 0.15f), Vector2.zero, new Vector2(160f, 54f));
        EnsureButton(card.transform, "Back", "Back", new Vector2(0.65f, 0.15f), Vector2.zero, new Vector2(160f, 54f));
        overlay.SetActive(false);
    }

    private static void CreateTutorialBriefingOverlay(Transform parent)
    {
        GameObject overlay = EnsureFullscreenPanel(parent, "TutorialBriefingOverlay", new Color(0.01f, 0.04f, 0.07f, 0.86f));
        GameObject card = EnsureUiPanel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 500f), new Color(0.04f, 0.28f, 0.38f, 0.98f));
        EnsureText(card.transform, "Title", "Monkey Climb", new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(660f, 58f), 42, FontStyle.Bold, Color.white);
        EnsureText(card.transform, "Body", "Jump up.\nGrab banana.\nAvoid parrot.\n\nSpace every 2 beats.\nDown/S between jumps.\nSpace + Left/Right between jumps.", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(650f, 260f), 27, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        EnsureButton(card.transform, "StartTutorialButton", "Start", new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(240f, 62f));
        overlay.SetActive(false);
    }

    private static void CreateGameRulesOverlay(Transform parent, string name, string title, string buttonLabel)
    {
        GameObject overlay = EnsureFullscreenPanel(parent, name, new Color(0.01f, 0.04f, 0.07f, 0.84f));
        GameObject card = EnsureUiPanel(overlay.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 500f), new Color(0.05f, 0.31f, 0.42f, 0.98f));
        EnsureText(card.transform, "Title", title, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(660f, 58f), 42, FontStyle.Bold, Color.white);
        EnsureText(card.transform, "Body", "Space: jump\nDown/S: banana\nSpace + Left/Right: parrot\nMisses are counted.", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(650f, 260f), 27, FontStyle.Bold, new Color(1f, 0.94f, 0.68f));
        EnsureButton(card.transform, "StartGameButton", buttonLabel, new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(220f, 62f));
        overlay.SetActive(false);
    }

    private static GameObject FindOrCreateRoot(string name)
    {
        GameObject obj = GameObject.Find(name);
        return obj != null ? obj : new GameObject(name);
    }

    private static GameObject FindOrCreateUiRoot(string name)
    {
        GameObject obj = GameObject.Find(name);
        return obj != null ? obj : new GameObject(name, typeof(RectTransform));
    }

    private static GameObject EnsureSpriteTemplate(Transform parent, string name, Vector3 scale, Color color, int sortingOrder, bool trigger)
    {
        Transform existing = parent.Find(name);
        bool created = existing == null;
        GameObject obj = EnsureChild(parent, name);
        if (created)
        {
            obj.transform.localScale = scale;
        }
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = obj.AddComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = trigger;
        }
        return obj;
    }

    private static GameObject EnsureTextTemplate(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        bool created = existing == null;
        GameObject obj = EnsureChild(parent, name);
        TextMesh text = obj.GetComponent<TextMesh>();
        if (text == null)
        {
            text = obj.AddComponent<TextMesh>();
            created = true;
        }
        if (created)
        {
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.characterSize = 0.045f;
            text.color = Color.white;
        }
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null && created)
        {
            renderer.sortingOrder = 6;
        }
        return obj;
    }

    private static void EnsureBoxCollider(GameObject obj, Vector2 size, bool trigger)
    {
        BoxCollider2D collider = obj.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = obj.AddComponent<BoxCollider2D>();
            collider.size = size;
        }
        collider.isTrigger = trigger;
    }

    private static void EnsureCircleCollider(GameObject obj, float radius, bool trigger)
    {
        CircleCollider2D collider = obj.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = obj.AddComponent<CircleCollider2D>();
            collider.radius = radius;
        }
        collider.isTrigger = trigger;
    }

    private static void EnsureCamera()
    {
        Camera camera = Object.FindObjectOfType<Camera>();
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 1.5f, -10f);
    }

    private static GameObject EnsureFullscreenPanel(Transform parent, string name, Color color)
    {
        GameObject obj = EnsureUiRect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }
        image.color = color;
        return obj;
    }

    private static GameObject EnsureUiPanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = EnsureUiRect(parent, name, anchor, position, size);
        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            image = obj.AddComponent<Image>();
        }
        image.color = color;
        return obj;
    }

    private static GameObject EnsureUiRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = EnsureUiChild(parent, name);
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogWarning("VerticalSceneHierarchyBaker: '" + name + "' exists without RectTransform. Recreate it manually if UI editing behaves incorrectly.");
            return obj;
        }
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj;
    }

    private static GameObject EnsureUiChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static Text EnsureText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = EnsureUiRect(parent, name, anchor, position, size);
        Text text = obj.GetComponent<Text>();
        if (text == null)
        {
            text = obj.AddComponent<Text>();
        }
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static GameObject EnsureMissingUiRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj;
    }

    private static Text EnsureMissingText(Transform parent, string name, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = EnsureMissingUiRect(parent, name, anchor, position, size);
        Text text = obj.GetComponent<Text>();
        if (text != null)
        {
            return text;
        }

        text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = EnsureUiPanel(parent, name, anchor, position, size, new Color(1f, 1f, 1f, 0.92f));
        Button button = obj.GetComponent<Button>();
        if (button == null)
        {
            button = obj.AddComponent<Button>();
        }
        button.targetGraphic = obj.GetComponent<Image>();
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        Text text = EnsureText(obj.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 22, FontStyle.Bold, new Color(0.02f, 0.12f, 0.18f));
        text.raycastTarget = false;
        return button;
    }

    private static Button EnsureOptionalButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Button existingButton = existing.GetComponent<Button>();
            if (existingButton == null)
            {
                existingButton = existing.gameObject.AddComponent<Button>();
                Graphic graphic = existing.GetComponent<Graphic>();
                if (graphic != null)
                {
                    existingButton.targetGraphic = graphic;
                }
            }
            Navigation navigation = existingButton.navigation;
            navigation.mode = Navigation.Mode.None;
            existingButton.navigation = navigation;
            return existingButton;
        }

        return EnsureButton(parent, name, label, anchor, position, size);
    }

    private static void EnsureBeatDots(Transform beatLane)
    {
        for (int i = 0; i < 4; i++)
        {
            string name = "BeatDot_" + i;
            Transform existing = beatLane.Find(name);
            GameObject dot;
            if (existing != null)
            {
                dot = existing.gameObject;
            }
            else
            {
                dot = new GameObject(name, typeof(RectTransform));
                dot.transform.SetParent(beatLane, false);
                RectTransform rect = dot.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-63f + i * 42f, 0f);
                rect.sizeDelta = new Vector2(32f, 32f);
            }

            if (dot.GetComponent<CanvasRenderer>() == null)
            {
                dot.AddComponent<CanvasRenderer>();
            }
            if (dot.GetComponent<Image>() == null)
            {
                Image image = dot.AddComponent<Image>();
                image.color = new Color(0.25f, 0.72f, 1f, 0.55f);
            }
        }
    }

    private static GameObject EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }
}
