using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StartMenuMusicVisualizer : MonoBehaviour
{
    [Header("Hierarchy")]
    public string templatesChildName = "Templates";
    public string runtimeChildName = "Runtime";
    public string staffLinesChildName = "StaffLines";
    public bool hideSourceTemplatesOnPlay = true;

    [Header("Beat")]
    public AudioSource beatSource;
    [Min(30f)] public float fallbackBpm = 120f;
    public float beatPulseScale = 0.24f;
    public float beatPulseSharpness = 7.5f;

    [Header("Music Flow")]
    [Min(1)] public int noteCount = 16;
    [Range(1, 5)] public int laneCount = 5;
    public float travelSeconds = 8.5f;
    public Vector2 noteSizeRange = new Vector2(42f, 86f);
    public Vector2 laneSpacingRange = new Vector2(46f, 62f);
    public Vector2 waveAmplitudeRange = new Vector2(12f, 34f);
    public Vector2 waveFrequencyRange = new Vector2(1.2f, 2.4f);
    public Vector2 rotationRange = new Vector2(-12f, 12f);
    public float edgeFadePortion = 0.14f;
    public float spriteRendererTemplateSize = 72f;

    [Header("Staff Lines")]
    public bool createMissingStaffLines = true;
    public Color staffLineColor = new Color(1f, 1f, 1f, 0.18f);
    public Vector2 staffLineSize = new Vector2(1180f, 3f);

    private readonly List<Template> templates = new List<Template>();
    private readonly List<NoteVisual> notes = new List<NoteVisual>();
    private readonly List<Image> hiddenImages = new List<Image>();
    private readonly List<SpriteRenderer> hiddenRenderers = new List<SpriteRenderer>();

    private RectTransform rectTransform;
    private RectTransform boundsRect;
    private RectTransform runtimeRoot;
    private RectTransform staffRoot;
    private float fallbackBeatStartTime;
    private bool warnedMissingTemplates;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        boundsRect = ResolveBoundsRect();
        staffRoot = EnsureRectChild(staffLinesChildName);
        runtimeRoot = EnsureRectChild(runtimeChildName);
        EnsureStaffLines();
        ResolveBeatSource();
        fallbackBeatStartTime = Time.unscaledTime;
    }

    private void OnEnable()
    {
        fallbackBeatStartTime = Time.unscaledTime;
        if (runtimeRoot != null)
        {
            RebuildTemplates();
            BuildNotes();
        }
    }

    private void OnDisable()
    {
        RestoreSourceTemplates();
        ClearNotes();
    }

    private void Update()
    {
        if (templates.Count == 0)
        {
            if (!warnedMissingTemplates)
            {
                warnedMissingTemplates = true;
                Debug.LogWarning("StartMenuMusicVisualizer: No Image or SpriteRenderer templates found under '" + name + "'.");
            }
            return;
        }

        if (notes.Count != noteCount)
        {
            BuildNotes();
        }

        Rect bounds = boundsRect != null ? boundsRect.rect : new Rect(-640f, -360f, 1280f, 720f);
        float beatPosition = GetBeatPosition();
        float pulse = Mathf.Exp(-Mathf.Repeat(beatPosition, 1f) * beatPulseSharpness) * beatPulseScale;
        float time = GetMusicTime();
        float width = Mathf.Max(320f, bounds.width + 180f);
        float left = -width * 0.5f;
        float laneSpacing = Mathf.Lerp(laneSpacingRange.x, laneSpacingRange.y, 0.5f);
        float laneCenterOffset = (Mathf.Max(1, laneCount) - 1) * laneSpacing * 0.5f;

        for (int i = 0; i < notes.Count; i++)
        {
            NoteVisual note = notes[i];
            float travel = Mathf.Repeat((time / Mathf.Max(1f, travelSeconds)) + note.phase, 1f);
            float x = left + (travel * width);
            float laneY = (note.lane * laneSpacing) - laneCenterOffset;
            float wave = Mathf.Sin((travel * Mathf.PI * 2f * note.waveFrequency) + note.wavePhase) * note.waveAmplitude;
            float y = laneY + wave;
            note.rect.anchoredPosition = new Vector2(x, y);
            note.rect.localRotation = Quaternion.Euler(0f, 0f, note.rotation + (wave * 0.18f));

            float beatAccent = note.beatSlot == Mathf.FloorToInt(beatPosition) % 4 ? pulse : pulse * 0.45f;
            note.rect.localScale = note.baseScale * (1f + beatAccent);
            note.canvasGroup.alpha = EdgeFade(travel) * note.alpha;
        }
    }

    private RectTransform ResolveBoundsRect()
    {
        RectTransform current = rectTransform;
        while (current != null)
        {
            Rect rect = current.rect;
            if (rect.width > 32f && rect.height > 32f)
            {
                return current;
            }
            current = current.parent as RectTransform;
        }

        return rectTransform;
    }

    private RectTransform EnsureRectChild(string childName)
    {
        Transform existing = transform.Find(childName);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(childName, typeof(RectTransform));
        if (existing == null)
        {
            obj.transform.SetParent(transform, false);
        }

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = obj.AddComponent<RectTransform>();
        }
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        obj.SetActive(true);
        return rect;
    }

    private void EnsureStaffLines()
    {
        if (!createMissingStaffLines || staffRoot == null || staffRoot.childCount > 0)
        {
            DisableStaffRaycasts();
            return;
        }

        int count = Mathf.Clamp(laneCount, 3, 5);
        float spacing = Mathf.Lerp(laneSpacingRange.x, laneSpacingRange.y, 0.5f);
        float centerOffset = (count - 1) * spacing * 0.5f;
        for (int i = 0; i < count; i++)
        {
            GameObject line = new GameObject("Line_" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            line.transform.SetParent(staffRoot, false);
            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = new Vector2(0f, (i * spacing) - centerOffset);
            lineRect.sizeDelta = staffLineSize;
            Image image = line.GetComponent<Image>();
            image.color = staffLineColor;
            image.raycastTarget = false;
        }
    }

    private void DisableStaffRaycasts()
    {
        if (staffRoot == null)
        {
            return;
        }

        Image[] images = staffRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            images[i].raycastTarget = false;
        }
    }

    private void ResolveBeatSource()
    {
        if (beatSource != null)
        {
            return;
        }

        GameObject musicObject = GameObject.Find("menuMusic");
        if (musicObject != null)
        {
            beatSource = musicObject.GetComponent<AudioSource>();
        }
    }

    private float GetMusicTime()
    {
        if (beatSource != null && beatSource.clip != null && beatSource.isPlaying)
        {
            return beatSource.time;
        }

        return Time.unscaledTime - fallbackBeatStartTime;
    }

    private float GetBeatPosition()
    {
        return GetMusicTime() / (60f / Mathf.Max(30f, fallbackBpm));
    }

    private void RebuildTemplates()
    {
        RestoreSourceTemplates();
        templates.Clear();

        Transform templateRoot = transform.Find(templatesChildName);
        if (templateRoot != null)
        {
            CollectImages(templateRoot, true);
            CollectSpriteRenderers(templateRoot, true);
            if (templates.Count > 0)
            {
                return;
            }
        }

        CollectDirectChildImages();
        CollectDirectChildSpriteRenderers();
        CollectRootSpriteRenderer();
    }

    private void CollectImages(Transform root, bool includeInactive)
    {
        Image[] images = root.GetComponentsInChildren<Image>(includeInactive);
        for (int i = 0; i < images.Length; i++)
        {
            AddImageTemplate(images[i]);
        }
    }

    private void CollectSpriteRenderers(Transform root, bool includeInactive)
    {
        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(includeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            AddSpriteRendererTemplate(renderers[i]);
        }
    }

    private void CollectDirectChildImages()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == runtimeChildName || child.name == staffLinesChildName)
            {
                continue;
            }

            Image image = child.GetComponent<Image>();
            if (image != null)
            {
                AddImageTemplate(image);
            }
        }
    }

    private void CollectDirectChildSpriteRenderers()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == runtimeChildName || child.name == staffLinesChildName)
            {
                continue;
            }

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                AddSpriteRendererTemplate(renderer);
            }
        }
    }

    private void CollectRootSpriteRenderer()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            AddSpriteRendererTemplate(renderer);
        }
    }

    private void AddImageTemplate(Image image)
    {
        if (image == null || image.sprite == null || image.transform.IsChildOf(runtimeRoot) || image.transform.IsChildOf(staffRoot))
        {
            return;
        }

        RectTransform sourceRect = image.transform as RectTransform;
        Vector2 size = sourceRect != null ? sourceRect.rect.size : new Vector2(72f, 72f);
        if (size.x < 4f || size.y < 4f)
        {
            size = new Vector2(72f, 72f);
        }

        templates.Add(new Template(image.sprite, image.color, image.preserveAspect, size, image.transform.localScale));
        if (hideSourceTemplatesOnPlay && image.enabled)
        {
            image.enabled = false;
            hiddenImages.Add(image);
        }
    }

    private void AddSpriteRendererTemplate(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null || renderer.transform.IsChildOf(runtimeRoot) || renderer.transform.IsChildOf(staffRoot))
        {
            return;
        }

        float size = Mathf.Max(8f, spriteRendererTemplateSize);
        Vector3 localScale = renderer.transform.localScale;
        float averageScale = Mathf.Max(0.2f, (Mathf.Abs(localScale.x) + Mathf.Abs(localScale.y)) * 0.5f);
        templates.Add(new Template(renderer.sprite, renderer.color, true, new Vector2(size, size), Vector3.one * averageScale));
        if (hideSourceTemplatesOnPlay && renderer.enabled)
        {
            renderer.enabled = false;
            hiddenRenderers.Add(renderer);
        }
    }

    private void BuildNotes()
    {
        ClearNotes();
        if (templates.Count == 0 || runtimeRoot == null)
        {
            return;
        }

        int count = Mathf.Max(1, noteCount);
        for (int i = 0; i < count; i++)
        {
            Template template = templates[i % templates.Count];
            GameObject obj = new GameObject("MusicNote_" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            obj.transform.SetParent(runtimeRoot, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float size = Mathf.Lerp(noteSizeRange.x, noteSizeRange.y, Mathf.Repeat(i * 0.37f, 1f));
            rect.sizeDelta = template.size.sqrMagnitude > 1f ? Vector2.one * size : template.size;
            rect.localScale = template.localScale;

            Image image = obj.GetComponent<Image>();
            image.sprite = template.sprite;
            image.color = template.color;
            image.preserveAspect = template.preserveAspect;
            image.raycastTarget = false;

            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            notes.Add(new NoteVisual
            {
                rect = rect,
                canvasGroup = canvasGroup,
                baseScale = template.localScale,
                phase = (float)i / count,
                lane = i % Mathf.Max(1, laneCount),
                beatSlot = i % 4,
                waveAmplitude = Mathf.Lerp(waveAmplitudeRange.x, waveAmplitudeRange.y, Mathf.Repeat(i * 0.23f, 1f)),
                waveFrequency = Mathf.Lerp(waveFrequencyRange.x, waveFrequencyRange.y, Mathf.Repeat(i * 0.31f, 1f)),
                wavePhase = i * 0.71f,
                rotation = Mathf.Lerp(rotationRange.x, rotationRange.y, Mathf.Repeat(i * 0.41f, 1f)),
                alpha = Mathf.Lerp(0.5f, 0.95f, Mathf.Repeat(i * 0.19f, 1f))
            });
        }
    }

    private float EdgeFade(float progress)
    {
        float fadeIn = Mathf.Clamp01(progress / Mathf.Max(0.01f, edgeFadePortion));
        float fadeOut = Mathf.Clamp01((1f - progress) / Mathf.Max(0.01f, edgeFadePortion));
        return Mathf.Min(fadeIn, fadeOut);
    }

    private void ClearNotes()
    {
        for (int i = notes.Count - 1; i >= 0; i--)
        {
            NoteVisual note = notes[i];
            if (note.rect != null)
            {
                Destroy(note.rect.gameObject);
            }
        }
        notes.Clear();
    }

    private void RestoreSourceTemplates()
    {
        for (int i = 0; i < hiddenImages.Count; i++)
        {
            if (hiddenImages[i] != null)
            {
                hiddenImages[i].enabled = true;
            }
        }
        hiddenImages.Clear();

        for (int i = 0; i < hiddenRenderers.Count; i++)
        {
            if (hiddenRenderers[i] != null)
            {
                hiddenRenderers[i].enabled = true;
            }
        }
        hiddenRenderers.Clear();
    }

    private struct Template
    {
        public readonly Sprite sprite;
        public readonly Color color;
        public readonly bool preserveAspect;
        public readonly Vector2 size;
        public readonly Vector3 localScale;

        public Template(Sprite sprite, Color color, bool preserveAspect, Vector2 size, Vector3 localScale)
        {
            this.sprite = sprite;
            this.color = color;
            this.preserveAspect = preserveAspect;
            this.size = size;
            this.localScale = localScale;
        }
    }

    private class NoteVisual
    {
        public RectTransform rect;
        public CanvasGroup canvasGroup;
        public Vector3 baseScale;
        public float phase;
        public int lane;
        public int beatSlot;
        public float waveAmplitude;
        public float waveFrequency;
        public float wavePhase;
        public float rotation;
        public float alpha;
    }
}
