using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OceanDecorationDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public OceanDecorationReward reward;
    public bool unlocked;

    private OceanRhythmUIController owner;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 startPosition;

    public void Build(OceanRhythmUIController owner, OceanDecorationReward reward, bool unlocked)
    {
        this.owner = owner;
        this.reward = reward;
        this.unlocked = unlocked;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null)
        {
            owner.ShowDecorationInfo(reward);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!unlocked || rectTransform == null)
        {
            return;
        }

        startPosition = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!unlocked || rectTransform == null)
        {
            return;
        }

        rectTransform.position = eventData.position;
        if (owner != null)
        {
            owner.HighlightBucketSlotAt(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (!unlocked || rectTransform == null)
        {
            return;
        }

        bool placed = owner != null && owner.TryPlaceDecorationAt(reward, eventData.position);
        rectTransform.anchoredPosition = startPosition;
        if (owner != null)
        {
            owner.ClearBucketSlotHighlights();
        }
        if (!placed && owner != null)
        {
            owner.ShowBucketHint("Drop it on a bucket spot");
        }
    }
}
