using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds 2D-only visual animation for menu background UI.
/// Attach this to a backdrop object in the main menu prefab.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class MainMenuBackgroundFX : MonoBehaviour
{
    [Header("2D Color Pulse")]
    [Tooltip("If enabled, RGB brightness pulses while keeping position fixed.")]
    [SerializeField] private bool animateColor = true;
    [SerializeField] [Range(0f, 0.5f)] private float colorAmplitude = 0.08f;
    [SerializeField] private float colorFrequency = 0.55f;

    [Header("2D Alpha Pulse (Optional)")]
    [SerializeField] private bool animateAlpha = false;
    [SerializeField] [Range(0f, 0.5f)] private float alphaAmplitude = 0.1f;
    [SerializeField] private float alphaFrequency = 0.45f;

    private Graphic targetGraphic;
    private Color baseColor;

    private void Awake()
    {
        targetGraphic = GetComponent<Graphic>();
        baseColor = targetGraphic.color;
    }

    private void OnEnable()
    {
        baseColor = targetGraphic.color;
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        float brightness = 1f;
        float alpha = baseColor.a;

        if (animateColor)
            brightness += Mathf.Sin(t * colorFrequency * Mathf.PI * 2f) * colorAmplitude;

        if (animateAlpha)
            alpha += Mathf.Sin(t * alphaFrequency * Mathf.PI * 2f) * alphaAmplitude;

        brightness = Mathf.Max(0f, brightness);
        alpha = Mathf.Clamp01(alpha);

        targetGraphic.color = new Color(
            baseColor.r * brightness,
            baseColor.g * brightness,
            baseColor.b * brightness,
            alpha
        );
    }

    private void OnDisable()
    {
        if (targetGraphic != null)
            targetGraphic.color = baseColor;
    }
}
