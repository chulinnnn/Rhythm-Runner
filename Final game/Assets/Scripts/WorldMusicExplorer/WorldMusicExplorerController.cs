using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//ShowItem(int index)切换条目
[DefaultExecutionOrder(-120)]
public class WorldMusicExplorerController : MonoBehaviour
{
    private const string DefaultSceneName = "WorldMusicExplorer";

    private static bool registered;

    [Header("Scene")]
    public string explorerSceneName = DefaultSceneName;
    public string startSceneName = "Start";

    [Header("Hierarchy")]
    public string canvasName = "WorldMusicExplorerCanvas";
    public string contentRootName = "WorldMusicExplorerContent";
    public string itemsPath = "WorldMusicExplorerContent/Items";
    public string rootPath = "Root";
    public string nowPlayingPath = "Root/NowPlaying";
    public string hintTextPath = "Root/HintText";
    public string backButtonPath = "Root/BackButton";

    [Header("Animation")]
    public float fadeSeconds = 0.35f;
    public float driftAmplitude = 14f;
    public float driftSpeed = 0.22f;
    public float scalePulse = 0.025f;
    public float scaleSpeed = 0.18f;
    public float parallaxStep = 0.18f;

    private readonly List<ExplorerItem> items = new List<ExplorerItem>();
    private readonly List<LayerState> currentLayers = new List<LayerState>();

    private Text nowPlayingText;
    private Text hintText;
    private Button backButton;
    private int currentIndex = -1;
    private float itemStartedAt;

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
        if (scene.name != DefaultSceneName)
        {
            return;
        }

        if (FindObjectOfType<WorldMusicExplorerController>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("WorldMusicExplorerController");
        obj.AddComponent<WorldMusicExplorerController>();
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != explorerSceneName)
        {
            return;
        }

        BindScene();
        ShowItem(0);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != explorerSceneName)
        {
            return;
        }

        if (Input.anyKeyDown && IsOrdinaryKeyDown())
        {
            ShowNextItem();
        }

        AnimateCurrentVisual();
    }

    private void BindScene()
    {
        EnsureEventSystem();

        GameObject canvas = GameObject.Find(canvasName);
        Transform canvasTransform = canvas != null ? canvas.transform : null;
        Transform contentRoot = GameObject.Find(contentRootName) != null ? GameObject.Find(contentRootName).transform : null;
        Transform itemsRoot = GameObject.Find(itemsPath) != null ? GameObject.Find(itemsPath).transform : null;

        if (canvasTransform != null)
        {
            nowPlayingText = FindText(canvasTransform, nowPlayingPath);
            hintText = FindText(canvasTransform, hintTextPath);
            backButton = FindButton(canvasTransform, backButtonPath);
        }

        if (itemsRoot == null && contentRoot != null)
        {
            itemsRoot = contentRoot.Find("Items");
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ReturnToStart);
        }

        items.Clear();
        if (itemsRoot != null)
        {
            for (int i = 0; i < itemsRoot.childCount; i++)
            {
                Transform child = itemsRoot.GetChild(i);
                ExplorerItem item = ExplorerItem.FromTransform(child);
                if (item.IsUsable)
                {
                    items.Add(item);
                }

                if (item.VisualRoot != null)
                {
                    item.VisualRoot.gameObject.SetActive(false);
                }
            }
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(items.Count == 0);
        }
    }

    private Text FindText(Transform root, string path)
    {
        Transform child = root.Find(path);
        return child != null ? child.GetComponent<Text>() : null;
    }

    private Button FindButton(Transform root, string path)
    {
        Transform child = root.Find(path);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private void ShowNextItem()
    {
        if (items.Count == 0)
        {
            return;
        }

        int next = currentIndex + 1;
        if (next >= items.Count)
        {
            next = 0;
        }

        ShowItem(next);
    }

    private void ShowItem(int index)
    {
        StopCurrentItem();
        currentLayers.Clear();

        if (items.Count == 0)
        {
            currentIndex = -1;
            if (nowPlayingText != null)
            {
                nowPlayingText.text = "";
            }
            if (hintText != null)
            {
                hintText.gameObject.SetActive(true);
            }
            return;
        }

        currentIndex = Mathf.Clamp(index, 0, items.Count - 1);
        ExplorerItem item = items[currentIndex];
        itemStartedAt = Time.time;

        if (item.VisualRoot != null)
        {
            item.VisualRoot.gameObject.SetActive(true);
            CanvasGroup group = item.VisualRoot.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = item.VisualRoot.gameObject.AddComponent<CanvasGroup>();
            }
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            CaptureLayers(item.VisualRoot);
        }

        if (item.Audio != null)
        {
            item.Audio.Stop();
            item.Audio.Play();
        }

        if (nowPlayingText != null)
        {
            nowPlayingText.text = item.DisplayName;
        }
        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }
    }

    private void StopCurrentItem()
    {
        if (currentIndex < 0 || currentIndex >= items.Count)
        {
            return;
        }

        ExplorerItem current = items[currentIndex];
        if (current.Audio != null)
        {
            current.Audio.Stop();
        }
        if (current.VisualRoot != null)
        {
            current.VisualRoot.gameObject.SetActive(false);
        }
    }

    private void CaptureLayers(Transform visualRoot)
    {
        currentLayers.Clear();
        currentLayers.Add(new LayerState(visualRoot, visualRoot.localPosition, visualRoot.localScale, 0));

        int layerIndex = 1;
        for (int i = 0; i < visualRoot.childCount; i++)
        {
            Transform child = visualRoot.GetChild(i);
            currentLayers.Add(new LayerState(child, child.localPosition, child.localScale, layerIndex));
            layerIndex++;
        }
    }

    private void AnimateCurrentVisual()
    {
        if (currentIndex < 0 || currentIndex >= items.Count)
        {
            return;
        }

        ExplorerItem item = items[currentIndex];
        if (item.VisualRoot == null)
        {
            return;
        }

        float elapsed = Time.time - itemStartedAt;
        CanvasGroup group = item.VisualRoot.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = fadeSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeSeconds);
        }

        for (int i = 0; i < currentLayers.Count; i++)
        {
            LayerState layer = currentLayers[i];
            if (layer.Transform == null)
            {
                continue;
            }

            float phase = elapsed * driftSpeed + layer.LayerIndex * 0.71f;
            float drift = Mathf.Sin(phase) * driftAmplitude * (1f + layer.LayerIndex * parallaxStep);
            float scale = 1f + Mathf.Sin(elapsed * scaleSpeed + layer.LayerIndex * 0.33f) * scalePulse;
            layer.Transform.localPosition = layer.BasePosition + new Vector3(drift, 0f, 0f);
            layer.Transform.localScale = layer.BaseScale * scale;
        }
    }

    private bool IsOrdinaryKeyDown()
    {
        for (KeyCode code = KeyCode.Backspace; code <= KeyCode.Menu; code++)
        {
            if (!Input.GetKeyDown(code))
            {
                continue;
            }

            return IsOrdinaryKey(code);
        }

        return false;
    }

    private bool IsOrdinaryKey(KeyCode code)
    {
        if (code == KeyCode.Escape ||
            code == KeyCode.LeftAlt ||
            code == KeyCode.RightAlt ||
            code == KeyCode.LeftControl ||
            code == KeyCode.RightControl ||
            code == KeyCode.LeftCommand ||
            code == KeyCode.RightCommand ||
            code == KeyCode.LeftWindows ||
            code == KeyCode.RightWindows ||
            code == KeyCode.Menu)
        {
            return false;
        }

        if (code >= KeyCode.F1 && code <= KeyCode.F15)
        {
            return false;
        }

        if (code >= KeyCode.Mouse0 && code <= KeyCode.Mouse6)
        {
            return false;
        }

        if (code >= KeyCode.JoystickButton0 && code <= KeyCode.Joystick8Button19)
        {
            return false;
        }

        return true;
    }

    private void ReturnToStart()
    {
        if (string.IsNullOrEmpty(startSceneName))
        {
            return;
        }

        SceneTransitionManager.LoadScene(startSceneName);
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

    private struct LayerState
    {
        public readonly Transform Transform;
        public readonly Vector3 BasePosition;
        public readonly Vector3 BaseScale;
        public readonly int LayerIndex;

        public LayerState(Transform transform, Vector3 basePosition, Vector3 baseScale, int layerIndex)
        {
            Transform = transform;
            BasePosition = basePosition;
            BaseScale = baseScale;
            LayerIndex = layerIndex;
        }
    }

    private sealed class ExplorerItem
    {
        public Transform Root;
        public Transform VisualRoot;
        public AudioSource Audio;
        public string DisplayName;

        public bool IsUsable
        {
            get { return Root != null && (VisualRoot != null || Audio != null); }
        }

        public static ExplorerItem FromTransform(Transform root)
        {
            ExplorerItem item = new ExplorerItem();
            item.Root = root;
            item.VisualRoot = root != null ? root.Find("VisualRoot") : null;
            Transform music = root != null ? root.Find("Music") : null;
            item.Audio = music != null ? music.GetComponent<AudioSource>() : null;
            if (item.Audio == null && root != null)
            {
                item.Audio = root.GetComponentInChildren<AudioSource>(true);
            }
            item.DisplayName = ReadDisplayName(root);
            return item;
        }

        private static string ReadDisplayName(Transform root)
        {
            if (root == null)
            {
                return "";
            }

            string label = ReadText(root, "Label");
            string description = ReadText(root, "Description");
            if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(description))
            {
                return label + "\n" + description;
            }
            if (!string.IsNullOrEmpty(label))
            {
                return label;
            }
            if (!string.IsNullOrEmpty(description))
            {
                return description;
            }
            return root.name.Replace("Item_", "");
        }

        private static string ReadText(Transform root, string childName)
        {
            Transform child = root.Find(childName);
            Text text = child != null ? child.GetComponent<Text>() : null;
            return text != null ? text.text : "";
        }
    }
}
