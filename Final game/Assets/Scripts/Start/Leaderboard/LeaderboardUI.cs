using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    private GameObject panelRoot;
    private Canvas leaderboardCanvas;
    private Font uiFont;

    void Start()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        BuildPanel();
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        BindPhbtn();
    }

    private void BindPhbtn()
    {
        GameObject phbtnObj = LeaderboardBootstrap.FindPhbtn();
        if (phbtnObj == null)
        {
            Debug.LogWarning("LeaderboardUI: phbtn not found in scene.");
            return;
        }

        Button button = phbtnObj.GetComponent<Button>();
        if (button == null)
        {
            button = phbtnObj.AddComponent<Button>();
        }

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(ShowLeaderboard);
    }

    public void ShowLeaderboard()
    {
        if (panelRoot == null)
        {
            BuildPanel();
        }

        if (panelRoot == null)
        {
            return;
        }

        RefreshContent();
        panelRoot.SetActive(true);

        if (leaderboardCanvas != null)
        {
            leaderboardCanvas.sortingOrder = 200;
        }
    }

    public void HideLeaderboard()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void RefreshContent()
    {
        Transform easyList = panelRoot.transform.Find("Content/EasyPanel/List");
        Transform hardList = panelRoot.transform.Find("Content/HardPanel/List");
        FillScoreList(easyList, LeaderboardManager.GetScores(LeaderboardMode.Easy));
        FillScoreList(hardList, LeaderboardManager.GetScores(LeaderboardMode.Hard));
    }

    private void FillScoreList(Transform listParent, List<int> scores)
    {
        if (listParent == null)
        {
            return;
        }

        for (int i = listParent.childCount - 1; i >= 0; i--)
        {
            Destroy(listParent.GetChild(i).gameObject);
        }

        if (scores.Count == 0)
        {
            CreateRow(listParent, 0, "\u6682\u65e0\u8bb0\u5f55", 0);
            return;
        }

        for (int i = 0; i < scores.Count; i++)
        {
            CreateRow(listParent, i + 1, scores[i] + "\u7c73", i);
        }
    }

    private void CreateRow(Transform parent, int rank, string distanceText, int index)
    {
        GameObject row = new GameObject("Row_" + index, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0, 36);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.spacing = 8;
        layout.padding = new RectOffset(8, 8, 4, 4);

        string rankText = rank > 0 ? ("\u7b2c" + rank + "\u540d") : "-";
        CreateLabel(row.transform, "Rank", rankText, 100);
        CreateLabel(row.transform, "Distance", distanceText, 160);
    }

    private void CreateLabel(Transform parent, string name, string text, float width)
    {
        GameObject labelObj = new GameObject(name, typeof(RectTransform));
        labelObj.transform.SetParent(parent, false);
        Text label = labelObj.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 22;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;
        label.text = text;

        LayoutElement layoutElement = labelObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = width;
        layoutElement.minHeight = 32;
    }

    private void BuildPanel()
    {
        if (panelRoot != null)
        {
            Destroy(panelRoot);
            panelRoot = null;
        }

        if (leaderboardCanvas == null)
        {
            GameObject canvasObj = new GameObject("LeaderboardCanvas");
            leaderboardCanvas = canvasObj.AddComponent<Canvas>();
            leaderboardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            leaderboardCanvas.sortingOrder = 200;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        EnsureEventSystem();

        panelRoot = new GameObject("LeaderboardPanel", typeof(RectTransform));
        panelRoot.transform.SetParent(leaderboardCanvas.transform, false);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject content = CreateChild(panelRoot.transform, "Content");
        RectTransform contentRect = content.GetComponent<RectTransform>();
        StretchFull(contentRect);
        contentRect.offsetMin = new Vector2(80, 80);
        contentRect.offsetMax = new Vector2(-80, -80);

        Image contentBg = content.AddComponent<Image>();
        contentBg.color = new Color(0.15f, 0.2f, 0.35f, 0.95f);

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(24, 24, 24, 24);
        contentLayout.spacing = 16;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        CreateTitle(content.transform, "LeaderboardTitle", "\u6392\u884c\u699c");
        CreateModePanel(content.transform, "EasyPanel", "\u7b80\u5355\u573a\u666f");
        CreateModePanel(content.transform, "HardPanel", "\u56f0\u96be\u573a\u666f");
        CreateBackButton(content.transform);
    }

    private void CreateTitle(Transform parent, string name, string title)
    {
        GameObject titleObj = CreateChild(parent, name);
        LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.minHeight = 48;
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = uiFont;
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.text = title;
    }

    private void CreateModePanel(Transform parent, string panelName, string header)
    {
        GameObject panel = CreateChild(parent, panelName);
        LayoutElement panelLayout = panel.AddComponent<LayoutElement>();
        panelLayout.minHeight = 220;
        panelLayout.flexibleHeight = 1;

        VerticalLayoutGroup vLayout = panel.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 8;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.padding = new RectOffset(12, 12, 12, 12);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.12f, 0.2f, 0.9f);

        GameObject headerObj = CreateChild(panel.transform, "Header");
        LayoutElement headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.minHeight = 36;
        Text headerText = headerObj.AddComponent<Text>();
        headerText.font = uiFont;
        headerText.fontSize = 26;
        headerText.fontStyle = FontStyle.Bold;
        headerText.color = new Color(1f, 0.9f, 0.5f);
        headerText.alignment = TextAnchor.MiddleLeft;
        headerText.text = header;

        GameObject listObj = CreateChild(panel.transform, "List");
        LayoutElement listLayout = listObj.AddComponent<LayoutElement>();
        listLayout.flexibleHeight = 1;
        listLayout.minHeight = 160;

        VerticalLayoutGroup listVLayout = listObj.AddComponent<VerticalLayoutGroup>();
        listVLayout.spacing = 4;
        listVLayout.childControlWidth = true;
        listVLayout.childControlHeight = true;
        listVLayout.childForceExpandWidth = true;
        listVLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = listObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void CreateBackButton(Transform parent)
    {
        GameObject btnObj = CreateChild(parent, "BackButton");
        LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
        btnLayout.minHeight = 52;

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.25f, 0.55f, 0.9f, 1f);

        Button button = btnObj.AddComponent<Button>();
        button.targetGraphic = btnImage;
        button.onClick.AddListener(HideLeaderboard);

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.35f, 0.65f, 1f);
        colors.pressedColor = new Color(0.2f, 0.45f, 0.8f);
        button.colors = colors;

        GameObject textObj = CreateChild(btnObj.transform, "Text");
        StretchFull(textObj.GetComponent<RectTransform>());
        Text btnText = textObj.AddComponent<Text>();
        btnText.font = uiFont;
        btnText.fontSize = 28;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.text = "\u8fd4\u56de";
    }

    private GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

    void OnDestroy()
    {
        if (panelRoot != null)
        {
            Destroy(panelRoot);
        }

        if (leaderboardCanvas != null)
        {
            Destroy(leaderboardCanvas.gameObject);
        }
    }
}
