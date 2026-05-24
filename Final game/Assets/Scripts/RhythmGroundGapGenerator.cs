using System.Collections.Generic;
using UnityEngine;

public static class RhythmGroundGapGenerator
{
    private const float MinChunkWidth = 0.35f;
    private static Sprite fallbackGroundSprite;

    public static void ApplyToSegment(GameObject segment)
    {
        if (segment == null || SceneDifficultySettings.Instance == null)
        {
            return;
        }

        if (!SceneDifficultySettings.Instance.ShouldSpawnGapsOnBeat())
        {
            return;
        }

        if (segment.GetComponent<RhythmGeneratedGroundSegment>() != null)
        {
            return;
        }

        BoxCollider2D[] floorColliders = FindFloorColliders(segment);
        if (floorColliders.Length == 0)
        {
            return;
        }

        Bounds floorBounds = GetCombinedBounds(floorColliders);
        SpriteRenderer templateRenderer = FindTemplateRenderer(floorColliders);
        DisableOriginalFloor(floorColliders);

        List<GapRange> gaps = CalculateGapRanges(segment, floorBounds);
        CreateGroundChunks(segment.transform, floorBounds, gaps, templateRenderer);
        segment.AddComponent<RhythmGeneratedGroundSegment>();
    }

    private static BoxCollider2D[] FindFloorColliders(GameObject segment)
    {
        BoxCollider2D[] colliders = segment.GetComponentsInChildren<BoxCollider2D>(true);
        List<BoxCollider2D> floorColliders = new List<BoxCollider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].CompareTag("Floor"))
            {
                floorColliders.Add(colliders[i]);
            }
        }

        return floorColliders.ToArray();
    }

    private static Bounds GetCombinedBounds(BoxCollider2D[] colliders)
    {
        Bounds bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        {
            bounds.Encapsulate(colliders[i].bounds);
        }

        return bounds;
    }

    private static SpriteRenderer FindTemplateRenderer(BoxCollider2D[] floorColliders)
    {
        for (int i = 0; i < floorColliders.Length; i++)
        {
            SpriteRenderer renderer = floorColliders[i].GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null && renderer.sprite != null)
            {
                return renderer;
            }
        }

        return null;
    }

    private static void DisableOriginalFloor(BoxCollider2D[] floorColliders)
    {
        for (int i = 0; i < floorColliders.Length; i++)
        {
            if (floorColliders[i] == null)
            {
                continue;
            }

            floorColliders[i].enabled = false;
            SpriteRenderer[] renderers = floorColliders[i].GetComponentsInChildren<SpriteRenderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                if (renderers[j] != null)
                {
                    renderers[j].enabled = false;
                }
            }
        }
    }

    private static List<GapRange> CalculateGapRanges(GameObject segment, Bounds floorBounds)
    {
        SceneDifficultySettings settings = SceneDifficultySettings.Instance;
        settings.GetRhythmGapSettings(
            out float bpm,
            out int firstBeat,
            out int beatSpacing,
            out float durationBeats,
            out float minGapWidth,
            out float maxGapWidth,
            out float meetX);

        float beatInterval = 60f / Mathf.Max(1f, bpm);
        float moveSpeed = GetSegmentMoveSpeed(segment);
        float songTime = GetCurrentSongTime();
        float gapWidth = Mathf.Clamp(moveSpeed * beatInterval * durationBeats, minGapWidth, maxGapWidth);

        GetSegmentBounds(segment, floorBounds, out float segmentMinX, out float segmentMaxX);
        int sequenceIndex = GetFirstFutureSequenceIndex(songTime, beatInterval, firstBeat, beatSpacing);
        int safetyLimit = 64;
        List<GapRange> gaps = new List<GapRange>();

        for (int attempts = 0; attempts < safetyLimit; attempts++)
        {
            int beat = firstBeat + (sequenceIndex + attempts) * beatSpacing;
            float targetSongTime = beat * beatInterval;
            if (targetSongTime <= songTime)
            {
                continue;
            }

            float gapStartX = meetX + moveSpeed * (targetSongTime - songTime);
            float gapEndX = gapStartX + gapWidth;
            if (gapStartX >= segmentMaxX)
            {
                break;
            }

            if (gapEndX <= segmentMinX)
            {
                continue;
            }

            gaps.Add(new GapRange(
                Mathf.Clamp(gapStartX, segmentMinX, segmentMaxX),
                Mathf.Clamp(gapEndX, segmentMinX, segmentMaxX)));
        }

        gaps.Sort((a, b) => a.startX.CompareTo(b.startX));
        return gaps;
    }

    private static void CreateGroundChunks(Transform parent, Bounds floorBounds, List<GapRange> gaps, SpriteRenderer templateRenderer)
    {
        float segmentMinX = floorBounds.min.x;
        float segmentMaxX = floorBounds.max.x;
        BackgroundTranform background = parent.GetComponent<BackgroundTranform>();
        if (background != null)
        {
            float halfWidth = Mathf.Max(0.1f, background.nextSegmentSpawnX) * 0.5f;
            segmentMinX = parent.position.x - halfWidth;
            segmentMaxX = parent.position.x + halfWidth;
        }

        float cursor = segmentMinX;
        for (int i = 0; i < gaps.Count; i++)
        {
            CreateChunkIfWideEnough(parent, cursor, gaps[i].startX, floorBounds, templateRenderer);
            cursor = Mathf.Max(cursor, gaps[i].endX);
        }

        CreateChunkIfWideEnough(parent, cursor, segmentMaxX, floorBounds, templateRenderer);
    }

    private static void CreateChunkIfWideEnough(Transform parent, float startX, float endX, Bounds floorBounds, SpriteRenderer templateRenderer)
    {
        float width = endX - startX;
        if (width < MinChunkWidth)
        {
            return;
        }

        float centerX = (startX + endX) * 0.5f;
        float height = Mathf.Max(0.12f, floorBounds.size.y);
        GameObject chunk = new GameObject("RhythmGroundChunk");
        chunk.tag = "Floor";
        chunk.transform.SetParent(parent, false);
        chunk.transform.position = new Vector3(centerX, floorBounds.center.y, floorBounds.center.z);

        Vector3 parentScale = parent.lossyScale;
        float scaleX = Mathf.Approximately(parentScale.x, 0f) ? width : width / Mathf.Abs(parentScale.x);
        float scaleY = Mathf.Approximately(parentScale.y, 0f) ? height : height / Mathf.Abs(parentScale.y);
        chunk.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        SpriteRenderer renderer = chunk.AddComponent<SpriteRenderer>();
        renderer.sprite = GetGroundSprite();
        renderer.color = SceneDifficultySettings.Instance.generatedGroundColor;
        renderer.sortingOrder = templateRenderer != null ? templateRenderer.sortingOrder : 0;
        if (templateRenderer != null)
        {
            renderer.sortingLayerID = templateRenderer.sortingLayerID;
        }

        BoxCollider2D collider = chunk.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.offset = Vector2.zero;
        chunk.AddComponent<RhythmGeneratedGroundChunk>();
    }

    private static float GetSegmentMoveSpeed(GameObject segment)
    {
        float speed = SceneDifficultySettings.Instance.GetBackgroundMoveSpeed();
        BackgroundTranform background = segment.GetComponent<BackgroundTranform>();
        if (background != null)
        {
            speed = background.moveSpeed;
        }

        if (GameManager.Instance != null)
        {
            speed *= GameManager.Instance.speedMultiplier;
        }

        return Mathf.Max(0.1f, speed);
    }

    private static float GetCurrentSongTime()
    {
        if (RhythmManager.Instance != null)
        {
            return RhythmManager.Instance.GetAdjustedSongTime();
        }

        return Time.time;
    }

    private static int GetFirstFutureSequenceIndex(float songTime, float beatInterval, int startBeat, int beatSpacing)
    {
        float currentBeat = songTime / Mathf.Max(0.001f, beatInterval);
        float beatsFromStart = currentBeat - startBeat;
        if (beatsFromStart <= 0f)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.CeilToInt(beatsFromStart / Mathf.Max(1, beatSpacing)));
    }

    private static void GetSegmentBounds(GameObject segment, Bounds floorBounds, out float minX, out float maxX)
    {
        BackgroundTranform background = segment.GetComponent<BackgroundTranform>();
        if (background != null)
        {
            float halfWidth = Mathf.Max(0.1f, background.nextSegmentSpawnX) * 0.5f;
            minX = segment.transform.position.x - halfWidth;
            maxX = segment.transform.position.x + halfWidth;
            return;
        }

        minX = floorBounds.min.x;
        maxX = floorBounds.max.x;
    }

    private static Sprite GetGroundSprite()
    {
        if (fallbackGroundSprite != null)
        {
            return fallbackGroundSprite;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        fallbackGroundSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return fallbackGroundSprite;
    }

    private struct GapRange
    {
        public readonly float startX;
        public readonly float endX;

        public GapRange(float startX, float endX)
        {
            this.startX = startX;
            this.endX = endX;
        }
    }
}

public class RhythmGeneratedGroundSegment : MonoBehaviour
{
}

public class RhythmGeneratedGroundChunk : MonoBehaviour
{
}
