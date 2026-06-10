using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OceanBucketSlot : MonoBehaviour, IPointerClickHandler
{
    private static readonly Color EmptyColor = new Color(0.78f, 0.95f, 1f, 0.16f);
    private static readonly Color FilledColor = new Color(1f, 1f, 1f, 0.30f);
    private static readonly Color HighlightColor = new Color(1f, 0.86f, 0.18f, 0.62f);

    public OceanBucketSlotId slotId;

    private OceanRhythmUIController owner;
    private Image backgroundImage;
    private Image highlightImage;
    private Image decorationImage;
    private Text labelText;
    private bool hasDecoration;
    private bool ownsBackgroundTint;
    private OceanDecorationReward decoration;
    private float pulseScale = 1f;

    public void Build(OceanRhythmUIController owner, OceanBucketSlotId slotId, Sprite slotSprite, Sprite highlightSprite, Font font)
    {
        this.owner = owner;
        this.slotId = slotId;

        backgroundImage = gameObject.GetComponent<Image>();
        bool createdBackground = false;
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
            createdBackground = true;
        }
        if (backgroundImage.sprite == null)
        {
            backgroundImage.sprite = slotSprite;
        }
        if (createdBackground)
        {
            backgroundImage.color = EmptyColor;
        }
        ownsBackgroundTint = createdBackground;
        backgroundImage.preserveAspect = true;
        backgroundImage.raycastTarget = true;

        Transform existingHighlight = transform.Find("Highlight");
        GameObject highlightObj = existingHighlight != null ? existingHighlight.gameObject : new GameObject("Highlight", typeof(RectTransform));
        if (existingHighlight == null)
        {
            highlightObj.transform.SetParent(transform, false);
        }
        RectTransform highlightRect = highlightObj.GetComponent<RectTransform>();
        if (existingHighlight == null)
        {
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.anchoredPosition = Vector2.zero;
            highlightRect.sizeDelta = Vector2.zero;
        }
        highlightImage = highlightObj.GetComponent<Image>();
        bool createdHighlight = false;
        if (highlightImage == null)
        {
            highlightImage = highlightObj.AddComponent<Image>();
            createdHighlight = true;
        }
        if (createdHighlight || highlightImage.sprite == null)
        {
            highlightImage.sprite = highlightSprite != null ? highlightSprite : slotSprite;
        }
        if (createdHighlight)
        {
            highlightImage.color = HighlightColor;
        }
        highlightImage.raycastTarget = false;
        highlightImage.preserveAspect = true;
        highlightImage.gameObject.SetActive(false);

        Transform existingDecoration = transform.Find("Decoration");
        GameObject decorationObj = existingDecoration != null ? existingDecoration.gameObject : new GameObject("Decoration", typeof(RectTransform));
        if (existingDecoration == null)
        {
            decorationObj.transform.SetParent(transform, false);
        }
        RectTransform decorationRect = decorationObj.GetComponent<RectTransform>();
        if (existingDecoration == null)
        {
            decorationRect.anchorMin = new Vector2(0.5f, 0.5f);
            decorationRect.anchorMax = new Vector2(0.5f, 0.5f);
            decorationRect.anchoredPosition = Vector2.zero;
            decorationRect.sizeDelta = new Vector2(50f, 50f);
        }
        decorationImage = decorationObj.GetComponent<Image>();
        if (decorationImage == null)
        {
            decorationImage = decorationObj.AddComponent<Image>();
        }
        decorationImage.raycastTarget = false;
        decorationImage.preserveAspect = true;

        Transform existingLabel = transform.Find("Label");
        GameObject labelObj = existingLabel != null ? existingLabel.gameObject : new GameObject("Label", typeof(RectTransform));
        if (existingLabel == null)
        {
            labelObj.transform.SetParent(transform, false);
        }
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        if (existingLabel == null)
        {
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, -20f);
            labelRect.sizeDelta = new Vector2(110f, 24f);
        }
        labelText = labelObj.GetComponent<Text>();
        bool createdLabelText = false;
        if (labelText == null)
        {
            labelText = labelObj.AddComponent<Text>();
            createdLabelText = true;
        }
        if (createdLabelText)
        {
            labelText.font = font;
            labelText.fontSize = 13;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = new Color(0.78f, 0.95f, 1f, 0.76f);
            labelText.alignment = TextAnchor.MiddleCenter;
        }
        else if (labelText.font == null)
        {
            labelText.font = font;
        }
        labelText.raycastTarget = false;
        if (string.IsNullOrEmpty(labelText.text))
        {
            labelText.text = SlotName(slotId);
        }
    }

    public void SetDecoration(OceanDecorationReward reward, Sprite sprite, Color color)
    {
        EnsureVisuals();
        hasDecoration = true;
        decoration = reward;
        decorationImage.sprite = sprite;
        decorationImage.color = color;
        decorationImage.gameObject.SetActive(true);
        if (backgroundImage != null)
        {
            if (ownsBackgroundTint)
            {
                backgroundImage.color = FilledColor;
            }
        }
        pulseScale = 1.28f;
    }

    public void ClearDecoration()
    {
        EnsureVisuals();
        hasDecoration = false;
        decorationImage.gameObject.SetActive(false);
        if (backgroundImage != null)
        {
            if (ownsBackgroundTint)
            {
                backgroundImage.color = EmptyColor;
            }
        }
    }

    private void EnsureVisuals()
    {
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
                ownsBackgroundTint = true;
            }
            if (ownsBackgroundTint)
            {
                backgroundImage.color = hasDecoration ? FilledColor : EmptyColor;
            }
            backgroundImage.preserveAspect = true;
            backgroundImage.raycastTarget = true;
        }

        if (decorationImage != null)
        {
            return;
        }

        Transform existingDecoration = transform.Find("Decoration");
        GameObject decorationObj = existingDecoration != null ? existingDecoration.gameObject : new GameObject("Decoration", typeof(RectTransform));
        if (existingDecoration == null)
        {
            decorationObj.transform.SetParent(transform, false);
        }

        RectTransform decorationRect = decorationObj.GetComponent<RectTransform>();
        decorationRect.anchorMin = new Vector2(0.5f, 0.5f);
        decorationRect.anchorMax = new Vector2(0.5f, 0.5f);
        decorationRect.anchoredPosition = Vector2.zero;
        decorationRect.sizeDelta = new Vector2(50f, 50f);

        decorationImage = decorationObj.GetComponent<Image>();
        if (decorationImage == null)
        {
            decorationImage = decorationObj.AddComponent<Image>();
        }
        decorationImage.raycastTarget = false;
        decorationImage.preserveAspect = true;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(highlighted);
            return;
        }

        if (backgroundImage != null && ownsBackgroundTint)
        {
            backgroundImage.color = highlighted ? HighlightColor : (hasDecoration ? FilledColor : EmptyColor);
        }
    }

    public bool ContainsScreenPoint(Vector2 screenPoint)
    {
        RectTransform rect = transform as RectTransform;
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner == null)
        {
            return;
        }

        if (owner.TryEquipSelectedDecorationToSlot(this))
        {
            pulseScale = 1.18f;
            return;
        }

        if (hasDecoration)
        {
            owner.ShowBucketHint(DecorationName(decoration) + " is on " + SlotName(slotId));
        }
        else
        {
            owner.ShowBucketHint("Drag a decoration here");
        }
        pulseScale = 1.12f;
    }

    private string SlotName(OceanBucketSlotId id)
    {
        if (id == OceanBucketSlotId.TopSlot)
        {
            return "Top";
        }
        if (id == OceanBucketSlotId.LeftSlot)
        {
            return "Left";
        }
        if (id == OceanBucketSlotId.RightSlot)
        {
            return "Right";
        }
        if (id == OceanBucketSlotId.FrontSlot)
        {
            return "Front";
        }
        return "Charm";
    }

    private string DecorationName(OceanDecorationReward reward)
    {
        return reward.ToString();
    }

    private void Update()
    {
        pulseScale = Mathf.Lerp(pulseScale, 1f, Time.unscaledDeltaTime * 8f);
        transform.localScale = Vector3.one * pulseScale;
    }
}
