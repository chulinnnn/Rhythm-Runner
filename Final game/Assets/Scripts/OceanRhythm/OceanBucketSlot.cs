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
    private Image decorationImage;
    private Text labelText;
    private bool hasDecoration;
    private OceanDecorationReward decoration;
    private float pulseScale = 1f;

    public void Build(OceanRhythmUIController owner, OceanBucketSlotId slotId, Sprite slotSprite, Font font)
    {
        this.owner = owner;
        this.slotId = slotId;

        backgroundImage = gameObject.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }
        backgroundImage.sprite = slotSprite;
        backgroundImage.color = EmptyColor;
        backgroundImage.preserveAspect = true;
        backgroundImage.raycastTarget = true;

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

        Transform existingLabel = transform.Find("Label");
        GameObject labelObj = existingLabel != null ? existingLabel.gameObject : new GameObject("Label", typeof(RectTransform));
        if (existingLabel == null)
        {
            labelObj.transform.SetParent(transform, false);
        }
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, -20f);
        labelRect.sizeDelta = new Vector2(110f, 24f);
        labelText = labelObj.GetComponent<Text>();
        if (labelText == null)
        {
            labelText = labelObj.AddComponent<Text>();
        }
        labelText.font = font;
        labelText.fontSize = 13;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = new Color(0.78f, 0.95f, 1f, 0.76f);
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.raycastTarget = false;
        labelText.text = SlotName(slotId);
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
            backgroundImage.color = FilledColor;
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
            backgroundImage.color = EmptyColor;
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
            }
            backgroundImage.color = hasDecoration ? FilledColor : EmptyColor;
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
        if (backgroundImage != null)
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
