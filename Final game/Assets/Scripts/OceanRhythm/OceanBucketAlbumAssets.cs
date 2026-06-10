using UnityEngine;

[System.Serializable]
public class OceanDecorationSpriteBinding
{
    public OceanDecorationReward reward;
    public Sprite icon;
}

public class OceanBucketAlbumAssets : MonoBehaviour
{
    public OceanDecorationSpriteBinding[] decorationIcons;
    public Sprite lockSprite;
    public Sprite selectedSprite;
    public Sprite progressFillSprite;
    public Sprite slotHighlightSprite;

    public Sprite GetDecorationSprite(OceanDecorationReward reward)
    {
        if (decorationIcons == null)
        {
            return null;
        }

        for (int i = 0; i < decorationIcons.Length; i++)
        {
            OceanDecorationSpriteBinding binding = decorationIcons[i];
            if (binding != null && binding.reward == reward && binding.icon != null)
            {
                return binding.icon;
            }
        }

        return null;
    }
}
