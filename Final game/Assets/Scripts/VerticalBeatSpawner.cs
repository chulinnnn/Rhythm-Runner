using System.Collections.Generic;
using UnityEngine;

public class VerticalBeatSpawner : MonoBehaviour
{
    private readonly List<VerticalRunnerPlatform> platforms = new List<VerticalRunnerPlatform>();
    private readonly List<VerticalRunnerPickup> pickups = new List<VerticalRunnerPickup>();
    private readonly List<VerticalRunnerObstacle> obstacles = new List<VerticalRunnerObstacle>();
    private readonly Dictionary<int, VerticalRunnerPlatform> platformByBeat = new Dictionary<int, VerticalRunnerPlatform>();

    private VerticalRunnerSettings settings;
    private Transform root;
    private Sprite circleSprite;
    private int totalBeats;

    public void Build(VerticalRunnerSettings settings, VerticalRunnerMode mode, Sprite fallbackSprite)
    {
        this.settings = settings;
        this.circleSprite = fallbackSprite;
        Clear();

        root = new GameObject("VerticalRunnerGenerated").transform;
        totalBeats = mode == VerticalRunnerMode.Tutorial
            ? 42
            : Mathf.Max(48, Mathf.CeilToInt(settings.songDurationSeconds / BeatInterval()));

        BuildRoute(mode);
    }

    public void Clear()
    {
        platforms.Clear();
        pickups.Clear();
        obstacles.Clear();
        platformByBeat.Clear();

        if (root != null)
        {
            Destroy(root.gameObject);
            root = null;
        }
    }

    public VerticalRunnerPlatform GetPlatformForBeat(int beatIndex)
    {
        VerticalRunnerPlatform platform;
        platformByBeat.TryGetValue(beatIndex, out platform);
        return platform;
    }

    public VerticalRunnerPlatform GetNextPlatformAfterBeat(int beatIndex)
    {
        for (int beat = beatIndex + 1; beat <= totalBeats; beat++)
        {
            VerticalRunnerPlatform platform = GetPlatformForBeat(beat);
            if (platform != null)
            {
                return platform;
            }
        }

        return null;
    }

    public VerticalRunnerPlatform GetNearestPlatform(Vector3 position)
    {
        VerticalRunnerPlatform nearest = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < platforms.Count; i++)
        {
            float distance = Vector2.Distance(position, platforms[i].transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = platforms[i];
            }
        }

        return nearest;
    }

    public VerticalRunnerPickup GetNearestCollectibleCoin(Vector3 position, float radius)
    {
        VerticalRunnerPickup nearest = null;
        float bestDistance = Mathf.Max(0f, radius);
        for (int i = 0; i < pickups.Count; i++)
        {
            VerticalRunnerPickup pickup = pickups[i];
            if (pickup == null || pickup.collected || !pickup.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector2.Distance(position, pickup.transform.position);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                nearest = pickup;
            }
        }

        return nearest;
    }

    private void BuildRoute(VerticalRunnerMode mode)
    {
        int beat = settings.startBeat;
        Vector2 position = new Vector2(0f, 0f);
        VerticalRunnerPlatform start = CreatePlatform(beat, position, true, false, true, true);

        if (mode == VerticalRunnerMode.Tutorial)
        {
            BuildTutorialRoute(beat, position, start);
        }
        else
        {
            BuildGameRoute(beat, position, start);
        }
    }

    private void BuildTutorialRoute(int startBeat, Vector2 startPosition, VerticalRunnerPlatform previous)
    {
        int beat = startBeat;
        Vector2 position = startPosition;
        for (int i = 1; i <= 34; i++)
        {
            beat += i == 22 ? 2 : settings.beatsPerPlatform;
            bool longJump = i == 22;
            bool strong = beat % 4 == 0;
            bool dangerBranch = i >= 16 && i <= 19;
            if (dangerBranch)
            {
                VerticalBranchChoice safeChoice = i % 2 == 0 ? VerticalBranchChoice.Right : VerticalBranchChoice.Left;
                previous = CreateDangerBranch(previous, beat, i, safeChoice, out position);
                continue;
            }

            position = NextPosition(position, i, longJump);
            VerticalRunnerPlatform platform = CreatePlatform(beat, position, strong, longJump, true, true);
            LinkDefault(previous, platform);
            previous = platform;

            if (i >= 9 && i <= 14)
            {
                CreateCoin(beat, position + new Vector2(0f, 0.58f));
            }

            if (i >= 26 && i <= 34 && i % 3 == 0)
            {
                CreateCoin(beat, position + new Vector2(0f, 0.58f));
            }
        }

        CreateFinishMarker(position + new Vector2(0f, settings.platformSpacingY * 2.2f));
    }

    private void BuildGameRoute(int startBeat, Vector2 startPosition, VerticalRunnerPlatform previous)
    {
        int beat = startBeat;
        Vector2 position = startPosition;
        int routeIndex = 0;

        while (beat < totalBeats)
        {
            bool longJump = beat > 12 && beat % 16 == 0;
            beat += longJump ? 2 : settings.beatsPerPlatform;
            routeIndex++;
            bool strong = beat % 4 == 0;
            bool dangerBranch = routeIndex > 10 && routeIndex % 7 == 0 && !longJump;
            if (dangerBranch)
            {
                VerticalBranchChoice safeChoice = routeIndex % 2 == 0 ? VerticalBranchChoice.Left : VerticalBranchChoice.Right;
                previous = CreateDangerBranch(previous, beat, routeIndex, safeChoice, out position);
                continue;
            }

            position = NextPosition(position, routeIndex, longJump);
            VerticalRunnerPlatform platform = CreatePlatform(beat, position, strong, longJump, true, true);
            LinkDefault(previous, platform);
            previous = platform;

            if (routeIndex > 4 && routeIndex % 2 == 0)
            {
                CreateCoin(beat, position + new Vector2(0f, 0.58f));
            }
        }

        CreateFinishMarker(position + new Vector2(0f, settings.platformSpacingY * 2.2f));
    }

    private Vector2 NextPosition(Vector2 previous, int routeIndex, bool longJump)
    {
        float y = previous.y + (longJump ? settings.longJumpSpacingY : settings.platformSpacingY);
        float lane = Mathf.Sin(routeIndex * 0.92f) * settings.laneWidth;
        if (routeIndex % 5 == 0)
        {
            lane *= 0.45f;
        }

        return new Vector2(lane, y);
    }

    private void LinkDefault(VerticalRunnerPlatform from, VerticalRunnerPlatform to)
    {
        if (from != null)
        {
            from.defaultNext = to;
        }
    }

    private VerticalRunnerPlatform CreateDangerBranch(VerticalRunnerPlatform origin, int beatIndex, int routeIndex, VerticalBranchChoice safeChoice, out Vector2 safePosition)
    {
        float y = origin != null ? origin.transform.position.y + settings.platformSpacingY : routeIndex * settings.platformSpacingY;
        float center = Mathf.Sin(routeIndex * 0.55f) * settings.laneWidth * 0.25f;
        Vector2 leftPosition = new Vector2(center - settings.dangerBranchLaneOffset, y);
        Vector2 rightPosition = new Vector2(center + settings.dangerBranchLaneOffset, y);
        bool leftSafe = safeChoice == VerticalBranchChoice.Left;

        VerticalRunnerPlatform left = CreatePlatform(beatIndex, leftPosition, false, false, leftSafe, leftSafe);
        VerticalRunnerPlatform right = CreatePlatform(beatIndex, rightPosition, false, false, !leftSafe, !leftSafe);

        if (origin != null)
        {
            origin.requiresDirectionalChoice = true;
            origin.safeChoice = safeChoice;
            origin.leftNext = left;
            origin.rightNext = right;
        }

        MarkBranchSide(left, VerticalBranchChoice.Left, leftSafe);
        MarkBranchSide(right, VerticalBranchChoice.Right, !leftSafe);
        safePosition = leftSafe ? leftPosition : rightPosition;
        return leftSafe ? left : right;
    }

    private void MarkBranchSide(VerticalRunnerPlatform platform, VerticalBranchChoice side, bool safe)
    {
        platform.isDangerBranchPlatform = true;
        string arrow = side == VerticalBranchChoice.Left ? "<" : ">";
        Color color = safe ? new Color(0.27f, 0.95f, 0.54f) : settings.obstacleColor;
        string label = safe ? arrow + " SAFE" : arrow + " DANGER";
        CreateWorldLabel(label, platform.transform.position + new Vector3(0f, 0.72f, 0f), color, platform.beatIndex);
        AddBeatPulse(platform.gameObject, platform.beatIndex);
        if (!safe)
        {
            CreateObstacle(platform.beatIndex, platform.transform.position + new Vector3(0f, 0.42f, 0f), false);
        }
    }

    private VerticalRunnerPlatform CreatePlatform(int beatIndex, Vector2 position, bool strongBeat, bool longJump, bool isSafePlatform, bool registerByBeat)
    {
        GameObject obj = new GameObject("VerticalPlatform_Beat_" + beatIndex);
        obj.transform.SetParent(root, false);
        obj.transform.position = position;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = settings.mushroomSprite != null ? settings.mushroomSprite : circleSprite;
        renderer.color = isSafePlatform ? (strongBeat ? settings.strongPlatformColor : settings.platformColor) : settings.obstacleColor;
        renderer.sortingOrder = 0;
        obj.transform.localScale = longJump ? new Vector3(1.55f, 0.42f, 1f) : new Vector3(1.3f, 0.36f, 1f);

        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.3f, 0.34f);

        VerticalRunnerPlatform platform = obj.AddComponent<VerticalRunnerPlatform>();
        platform.beatIndex = beatIndex;
        platform.strongBeat = strongBeat;
        platform.longJump = longJump;
        platform.isSafePlatform = isSafePlatform;
        platforms.Add(platform);
        if (registerByBeat)
        {
            platformByBeat[beatIndex] = platform;
        }
        return platform;
    }

    private void CreateCoin(int beatIndex, Vector2 position)
    {
        GameObject obj = new GameObject("VerticalCoin_Beat_" + beatIndex);
        obj.transform.SetParent(root, false);
        obj.transform.position = position;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = settings.coinSprite != null ? settings.coinSprite : circleSprite;
        renderer.color = settings.coinColor;
        renderer.sortingOrder = 2;
        obj.transform.localScale = new Vector3(0.32f, 0.32f, 1f);

        CircleCollider2D collider = obj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.45f;

        VerticalRunnerPickup pickup = obj.AddComponent<VerticalRunnerPickup>();
        pickup.beatIndex = beatIndex;
        pickup.value = 1;
        pickups.Add(pickup);
    }

    private void CreateObstacle(int beatIndex, Vector2 position, bool createLabel)
    {
        GameObject obj = new GameObject("VerticalObstacle_Beat_" + beatIndex);
        obj.transform.SetParent(root, false);
        obj.transform.position = position;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = settings.obstacleSprite != null ? settings.obstacleSprite : circleSprite;
        renderer.color = settings.obstacleColor;
        renderer.sortingOrder = 2;
        obj.transform.localScale = new Vector3(0.52f, 0.52f, 1f);

        CircleCollider2D collider = obj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.48f;

        VerticalRunnerObstacle obstacle = obj.AddComponent<VerticalRunnerObstacle>();
        obstacle.beatIndex = beatIndex;
        obstacles.Add(obstacle);
        AddBeatPulse(obj, beatIndex);
        if (createLabel)
        {
            CreateWorldLabel("DANGER", position + new Vector2(0f, 0.56f), settings.obstacleColor, beatIndex);
        }
    }

    private void CreateWorldLabel(string label, Vector2 position, Color color, int beatIndex = -1)
    {
        GameObject obj = new GameObject(label + "_Label");
        obj.transform.SetParent(root, false);
        obj.transform.position = position;
        TextMesh text = obj.AddComponent<TextMesh>();
        text.text = label;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 42;
        text.characterSize = 0.045f;
        text.color = color;
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 6;
        }
        if (beatIndex >= 0)
        {
            AddBeatPulse(obj, beatIndex);
        }
    }

    private void AddBeatPulse(GameObject obj, int beatIndex)
    {
        VerticalRunnerBeatPulse pulse = obj.AddComponent<VerticalRunnerBeatPulse>();
        pulse.beatIndex = beatIndex;
    }

    private void CreateFinishMarker(Vector2 position)
    {
        GameObject obj = new GameObject("VerticalFinish");
        obj.transform.SetParent(root, false);
        obj.transform.position = position;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = new Color(1f, 0.86f, 0.18f, 1f);
        renderer.sortingOrder = 1;
        obj.transform.localScale = new Vector3(2.2f, 0.22f, 1f);
    }

    private float BeatInterval()
    {
        return 60f / Mathf.Max(1f, settings.bpm);
    }
}
