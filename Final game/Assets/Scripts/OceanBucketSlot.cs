using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OceanBucketSlot : MonoBehaviour, IPointerClickHandler
{
    public OceanBucketSlotId slotId;

    private OceanRhythmUIController owner;
    private Image backgroundImage;
    private Image decorationImage;
    private Text labelText;
    private bool hasDecoration;
    private OceanDecorationReward decoration;

    public void Build(OceanRhythmUIController owner, OceanBucketSlotId slotId, Sprite slotSprite, Font font)
    {
        this.owner = owner;
        this.slotId = slotId;

        backgroundImage = gameObject.AddComponent<Image>();
        backgroundImage.sprite = slotSprite;
        backgroundImage.color = new Color(1f, 1f, 1f, 0.18f);
        backgroundImage.preserveAspect = true;

        GameObject decorationObj = new GameObject("Decoration", typeof(RectTransform));
        decorationObj.transform.SetParent(transform, false);
        RectTransform decorationRect = decorationObj.GetComponent<RectTransform>();
        decorationRect.anchorMin = new Vector2(0.5f, 0.5f);
        decorationRect.anchorMax = new Vector2(0.5f, 0.5f);
        decorationRect.anchoredPosition = Vector2.zero;
        decorationRect.sizeDelta = new Vector2(54f, 54f);
        decorationImage = decorationObj.AddComponent<Image>();
        decorationImage.raycastTarget = false;
        decorationImage.preserveAspect = true;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, -18f);
        labelRect.sizeDelta = new Vector2(120f, 26f);
        labelText = labelObj.AddComponent<Text>();
        labelText.font = font;
        labelText.fontSize = 15;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.raycastTarget = false;
        labelText.text = SlotName(slotId);
    }

    public void SetDecoration(OceanDecorationReward reward, Sprite sprite, Color color)
    {
        hasDecoration = true;
        decoration = reward;
        decorationImage.sprite = sprite;
        decorationImage.color = color;
        decorationImage.gameObject.SetActive(true);
    }

    public void ClearDecoration()
    {
        hasDecoration = false;
        decorationImage.gameObject.SetActive(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = highlighted ? new Color(1f, 0.86f, 0.18f, 0.68f) : new Color(1f, 1f, 1f, 0.18f);
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
}
