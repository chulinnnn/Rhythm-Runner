using UnityEngine;

[ExecuteAlways]
public class VerticalScrollingBackground : MonoBehaviour
{
    [Header("Sprite")]
    public Sprite backgroundSprite;
    public Color color = Color.white;
    public int sortingOrder = -50;

    [Header("Layout")]
    public Camera targetCamera;
    public int tileCount = 4;
    public bool fitWidthToCamera = true;
    public float widthPadding = 0.25f;
    public float horizontalOffset;
    public float verticalOffset;

    [Header("Motion")]
    [Tooltip("0 keeps the background in world space. 1 locks it to the camera. Values around 0.15-0.35 give a vertical parallax scroll.")]
    public float cameraParallax = 0.25f;
    public float autoScrollSpeed;

    private const string TilePrefix = "BackgroundTile_";

    private void LateUpdate()
    {
        UpdateTiles();
    }

    private void OnValidate()
    {
        tileCount = Mathf.Clamp(tileCount, 3, 8);
        cameraParallax = Mathf.Clamp01(cameraParallax);
    }

    public void UpdateTiles()
    {
        Camera camera = ResolveCamera();
        Sprite sprite = ResolveSprite();
        if (camera == null || sprite == null)
        {
            SetTilesActive(false);
            return;
        }

        EnsureTileCount();

        float spriteWidth = Mathf.Max(0.01f, sprite.bounds.size.x);
        float spriteHeight = Mathf.Max(0.01f, sprite.bounds.size.y);
        float cameraHeight = camera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * camera.aspect;
        float widthScale = fitWidthToCamera ? (cameraWidth + widthPadding) / spriteWidth : 1f;
        float heightScale = (cameraHeight * 1.08f) / spriteHeight;
        float tileScale = Mathf.Max(widthScale, heightScale);
        float tileHeight = spriteHeight * tileScale;
        float cameraY = camera.transform.position.y;
        float movingOffset = cameraY * cameraParallax + ApplicationSafeTime() * autoScrollSpeed;
        float anchorCenterY = cameraY + verticalOffset + movingOffset;
        float bottomY = cameraY - cameraHeight * 0.5f;
        float firstCenterY = anchorCenterY - Mathf.Ceil((anchorCenterY - bottomY) / tileHeight) * tileHeight;
        if (firstCenterY + tileHeight * 0.5f < bottomY)
        {
            firstCenterY += tileHeight;
        }

        for (int i = 0; i < tileCount; i++)
        {
            Transform tile = GetTile(i);
            SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            tile.gameObject.SetActive(true);
            Vector3 parentScale = transform.lossyScale;
            float localScaleX = tileScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x));
            float localScaleY = tileScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y));
            tile.localScale = new Vector3(localScaleX, localScaleY, 1f);
            tile.position = new Vector3(camera.transform.position.x + horizontalOffset, firstCenterY + tileHeight * i, 0f);
        }
    }

    private Camera ResolveCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        return Camera.main;
    }

    private Sprite ResolveSprite()
    {
        SpriteRenderer ownRenderer = GetComponent<SpriteRenderer>();
        if (ownRenderer != null)
        {
            if (backgroundSprite == null)
            {
                backgroundSprite = ownRenderer.sprite;
            }

            ownRenderer.enabled = false;
        }

        return backgroundSprite;
    }

    private void EnsureTileCount()
    {
        for (int i = 0; i < tileCount; i++)
        {
            GameObject tile = GetOrCreateTile(i).gameObject;
            tile.SetActive(i < tileCount);
            SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = tile.AddComponent<SpriteRenderer>();
            }
            renderer.sprite = backgroundSprite;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(TilePrefix))
            {
                continue;
            }

            int tileIndex;
            if (!TryReadTileIndex(child.name, out tileIndex) || tileIndex >= tileCount)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private Transform GetTile(int index)
    {
        return GetOrCreateTile(index);
    }

    private Transform GetOrCreateTile(int index)
    {
        string tileName = TilePrefix + index;
        Transform tile = transform.Find(tileName);
        if (tile != null)
        {
            return tile;
        }

        GameObject tileObject = new GameObject(tileName);
        tileObject.transform.SetParent(transform, false);
        tileObject.AddComponent<SpriteRenderer>();
        return tileObject.transform;
    }

    private bool TryReadTileIndex(string tileName, out int index)
    {
        index = -1;
        if (!tileName.StartsWith(TilePrefix))
        {
            return false;
        }

        return int.TryParse(tileName.Substring(TilePrefix.Length), out index);
    }

    private void SetTilesActive(bool active)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name.StartsWith(TilePrefix))
            {
                transform.GetChild(i).gameObject.SetActive(active);
            }
        }
    }

    private float ApplicationSafeTime()
    {
        return Application.isPlaying ? Time.time : 0f;
    }
}
