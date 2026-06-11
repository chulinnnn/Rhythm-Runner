using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StartMenuCoverStageAnimator : MonoBehaviour
{
    [Header("Beat")]
    public AudioSource beatSource;
    [Min(30f)] public float fallbackBpm = 120f;
    public float pulseScale = 0.08f;
    public float pulseSharpness = 6f;

    [Header("Motion")]
    public float waveRingScale = 0.16f;
    public float cardGlowAlphaPulse = 0.18f;
    public float spotlightSwingDegrees = 4f;
    public float floatingDrift = 10f;
    public float floatingRotation = 6f;

    private readonly List<DecorTarget> waveRings = new List<DecorTarget>();
    private readonly List<DecorTarget> cardGlows = new List<DecorTarget>();
    private readonly List<DecorTarget> floatingDecorations = new List<DecorTarget>();

    private DecorTarget titleHalo;
    private DecorTarget leftSpotlight;
    private DecorTarget rightSpotlight;
    private DecorTarget leftSpeaker;
    private DecorTarget rightSpeaker;
    private float fallbackBeatStartTime;

    private void Awake()
    {
        ResolveBeatSource();
        CacheTargets();
        DisableRaycasts();
        fallbackBeatStartTime = Time.unscaledTime;
    }

    private void OnEnable()
    {
        fallbackBeatStartTime = Time.unscaledTime;
        CacheTargets();
        DisableRaycasts();
    }

    private void Update()
    {
        float time = GetMusicTime();
        float beatPosition = GetBeatPosition(time);
        float pulse = Mathf.Exp(-Mathf.Repeat(beatPosition, 1f) * pulseSharpness);
        float scaledPulse = pulse * pulseScale;

        AnimatePulse(titleHalo, scaledPulse, 1f, 0f);
        AnimatePulse(leftSpeaker, scaledPulse, 0.7f, 0f);
        AnimatePulse(rightSpeaker, scaledPulse, 0.7f, 0.18f);
        AnimateSpotlight(leftSpotlight, time, -1f);
        AnimateSpotlight(rightSpotlight, time, 1f);
        AnimateListPulse(waveRings, pulse, waveRingScale, 0.25f, true);
        AnimateListPulse(cardGlows, pulse, 0.05f, cardGlowAlphaPulse, false);
        AnimateFloating(time);
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

    private float GetBeatPosition(float time)
    {
        return time / (60f / Mathf.Max(30f, fallbackBpm));
    }

    private void CacheTargets()
    {
        titleHalo = CreateTarget(transform.Find("TitleHalo"));
        leftSpotlight = CreateTarget(transform.Find("Spotlights/Left"));
        rightSpotlight = CreateTarget(transform.Find("Spotlights/Right"));
        leftSpeaker = CreateTarget(transform.Find("Speakers/Left"));
        rightSpeaker = CreateTarget(transform.Find("Speakers/Right"));

        waveRings.Clear();
        AddChildren(transform.Find("WaveRings"), waveRings);

        cardGlows.Clear();
        AddChildren(transform.Find("CardGlows"), cardGlows);

        floatingDecorations.Clear();
        AddChildren(transform.Find("FloatingDecorations"), floatingDecorations);
    }

    private void AddChildren(Transform parent, List<DecorTarget> targets)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            DecorTarget target = CreateTarget(parent.GetChild(i));
            if (target.rect != null)
            {
                target.phase = i * 0.37f;
                targets.Add(target);
            }
        }
    }

    private DecorTarget CreateTarget(Transform targetTransform)
    {
        RectTransform rect = targetTransform as RectTransform;
        if (rect == null)
        {
            return new DecorTarget();
        }

        CanvasGroup group = rect.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = rect.gameObject.AddComponent<CanvasGroup>();
        }
        group.blocksRaycasts = false;
        group.interactable = false;

        return new DecorTarget
        {
            rect = rect,
            group = group,
            basePosition = rect.anchoredPosition,
            baseScale = rect.localScale,
            baseRotation = rect.localEulerAngles.z,
            baseAlpha = group.alpha,
            phase = 0f
        };
    }

    private void DisableRaycasts()
    {
        CanvasGroup rootGroup = GetComponent<CanvasGroup>();
        if (rootGroup == null)
        {
            rootGroup = gameObject.AddComponent<CanvasGroup>();
        }
        rootGroup.blocksRaycasts = false;
        rootGroup.interactable = false;

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            images[i].raycastTarget = false;
        }
    }

    private void AnimatePulse(DecorTarget target, float pulse, float amount, float phase)
    {
        if (target.rect == null)
        {
            return;
        }

        float wobble = Mathf.Sin((Time.unscaledTime + phase) * 1.7f) * 0.015f;
        target.rect.localScale = target.baseScale * (1f + (pulse * amount) + wobble);
    }

    private void AnimateSpotlight(DecorTarget target, float time, float direction)
    {
        if (target.rect == null)
        {
            return;
        }

        float swing = Mathf.Sin(time * 0.55f + target.phase) * spotlightSwingDegrees * direction;
        target.rect.localEulerAngles = new Vector3(0f, 0f, target.baseRotation + swing);
    }

    private void AnimateListPulse(List<DecorTarget> targets, float pulse, float scaleAmount, float alphaAmount, bool stagger)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            DecorTarget target = targets[i];
            if (target.rect == null)
            {
                continue;
            }

            float phasePulse = stagger ? Mathf.Clamp01(pulse - (i * 0.08f)) : pulse;
            target.rect.localScale = target.baseScale * (1f + phasePulse * scaleAmount);
            if (target.group != null)
            {
                target.group.alpha = Mathf.Clamp01(target.baseAlpha + phasePulse * alphaAmount);
            }
        }
    }

    private void AnimateFloating(float time)
    {
        for (int i = 0; i < floatingDecorations.Count; i++)
        {
            DecorTarget target = floatingDecorations[i];
            if (target.rect == null)
            {
                continue;
            }

            float phase = target.phase + i * 0.21f;
            float x = Mathf.Sin(time * 0.38f + phase) * floatingDrift;
            float y = Mathf.Cos(time * 0.46f + phase) * floatingDrift * 0.65f;
            target.rect.anchoredPosition = target.basePosition + new Vector2(x, y);
            target.rect.localEulerAngles = new Vector3(0f, 0f, target.baseRotation + Mathf.Sin(time * 0.5f + phase) * floatingRotation);
        }
    }

    private struct DecorTarget
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector2 basePosition;
        public Vector3 baseScale;
        public float baseRotation;
        public float baseAlpha;
        public float phase;
    }
}
