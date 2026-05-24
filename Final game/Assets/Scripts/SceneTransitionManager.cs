using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;

    public float fadeDuration = 0.25f;
    public Color fadeColor = Color.black;

    private CanvasGroup canvasGroup;
    private Image fadeImage;
    private bool isLoading;

    private static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("SceneTransitionManager");
                instance = obj.AddComponent<SceneTransitionManager>();
            }

            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SceneTransitionManager: sceneName is empty.");
            return;
        }

        Instance.StartCoroutine(Instance.LoadSceneRoutine(sceneName));
    }

    public static void LoadScene(int sceneIndex)
    {
        Instance.StartCoroutine(Instance.LoadSceneRoutine(sceneIndex));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (isLoading)
        {
            yield break;
        }

        isLoading = true;
        yield return FadeTo(1f);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneRoutine(int sceneIndex)
    {
        if (isLoading)
        {
            yield break;
        }

        isLoading = true;
        yield return FadeTo(1f);
        SceneManager.LoadScene(sceneIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureOverlay();
        StartCoroutine(FadeInAfterLoad());
    }

    private IEnumerator FadeInAfterLoad()
    {
        yield return null;
        yield return FadeTo(0f);
        isLoading = false;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        EnsureOverlay();
        canvasGroup.blocksRaycasts = true;

        float startAlpha = canvasGroup.alpha;
        float timer = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0.01f;
    }

    private void EnsureOverlay()
    {
        if (canvasGroup != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("SceneTransitionCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        GameObject imageObject = new GameObject("Fade", typeof(RectTransform));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = true;
    }
}
