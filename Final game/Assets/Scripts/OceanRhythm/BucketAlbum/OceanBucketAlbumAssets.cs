using UnityEngine;

// Scene config object for Bucket Album decoration icons and optional reusable sprites.
// Bucket Album 的场景配置组件，用来挂装饰图标和可选复用图片。
//
// Decoration icons may be supplied here; album layout, badges, panels, and button art remain hierarchy-owned.
// 具体装饰图标可以在这里配置；相册布局、徽章、面板和按钮美术仍由 Hierarchy 控制。

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
