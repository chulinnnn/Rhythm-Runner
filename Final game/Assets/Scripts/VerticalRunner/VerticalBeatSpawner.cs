using System.Collections.Generic;
using UnityEngine;

public class VerticalBeatSpawner : MonoBehaviour
{
    private readonly List<VerticalRunnerPlatform> platforms = new List<VerticalRunnerPlatform>();
    private readonly List<VerticalRunnerPickup> pickups = new List<VerticalRunnerPickup>();
    private readonly List<VerticalRunnerObstacle> obstacles = new List<VerticalRunnerObstacle>();
    private readonly Dictionary<int, VerticalRunnerPlatform> platformByBeat = new Dictionary<int, VerticalRunnerPlatform>();

    private VerticalRunnerSettings settings;
    private VerticalRunnerTemplates templates;
    private Transform root;
    private Sprite circleSprite;
    private int totalBeats;

    public void Build(VerticalRunnerSettings settings, VerticalRunnerMode mode, Sprite fallbackSprite)
    {
        Build(settings, mode, fallbackSprite, RuntimeScenePolicy.Defaults());
    }

    public void Build(VerticalRunnerSettings settings, VerticalRunnerMode mode, Sprite fallbackSprite, RuntimeScenePolicy scenePolicy)
    {
        Build(settings, mode, fallbackSprite, scenePolicy, null);
    }

    public void Build(VerticalRunnerSettings settings, VerticalRunnerMode mode, Sprite fallbackSprite, RuntimeScenePolicy scenePolicy, VerticalRunnerTemplates templates)
    {
        this.settings = settings;
        this.templates = templates;
        this.circleSprite = fallbackSprite;
        if (scenePolicy == null)
        {
            scenePolicy = RuntimeScenePolicy.Defaults();
        }

        Clear();

        Transform runtimeRoot = templates != null ? templates.RuntimeRoot : scenePolicy.GetOrCreateRuntimeRoot("VerticalBeatSpawner");
        if (runtimeRoot == null)
        {
            return;
        }

        Transform existingRoot = runtimeRoot.Find("VerticalRunnerGenerated");
        if (existingRoot != null)
        {
            root = existingRoot;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }
        else
        {
            GameObject rootObject = new GameObject("VerticalRunnerGenerated");
            rootObject.transform.SetParent(runtimeRoot, false);
            root = rootObject.transform;
        }

        settings.beatsPerPlatform = 2;

        totalBeats = mode == VerticalRunnerMode.Tutorial
            ? 64
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
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
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
            if (pickup == null || pickup.collected || pickup.missed || !pickup.gameObject.activeInHierarchy)
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

    public VerticalRunnerPickup GetPromptCollectibleCoin(float beatPosition, int leadBeats)
    {
        int currentBeat = Mathf.FloorToInt(beatPosition);
        int earliestBeat = currentBeat + 1;
        int latestBeat = currentBeat + Mathf.Max(1, leadBeats);
        VerticalRunnerPickup best = null;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < pickups.Count; i++)
        {
            VerticalRunnerPickup pickup = pickups[i];
            if (pickup == null || pickup.collected || pickup.missed || !pickup.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (pickup.beatIndex < earliestBeat || pickup.beatIndex > latestBeat)
            {
                continue;
            }

            int distance = Mathf.Abs(pickup.beatIndex - currentBeat);
            if (distance < bestDistance)
            {
                best = pickup;
                bestDistance = distance;
            }
        }

        return best;
    }

    public VerticalRunnerPickup GetCollectibleCoinAtBeat(int beatIndex)
    {
        for (int i = 0; i < pickups.Count; i++)
        {
            VerticalRunnerPickup pickup = pickups[i];
            if (pickup == null)
            {
                continue;
            }

            if (pickup.beatIndex == beatIndex)
            {
                return pickup;
            }
        }

        return null;
    }

    public VerticalRunnerPickup GetCollectibleCoinForPlatform(VerticalRunnerPlatform platform)
    {
        if (platform == null)
        {
            return null;
        }

        int expectedBeat = platform.beatIndex + 1;
        Vector3 platformPosition = platform.transform.position;
        VerticalRunnerPickup best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < pickups.Count; i++)
        {
            VerticalRunnerPickup pickup = pickups[i];
            if (pickup == null || pickup.beatIndex != expectedBeat)
            {
                continue;
            }

            float distance = Vector2.Distance(pickup.transform.position, platformPosition);
            if (distance < bestDistance)
            {
                best = pickup;
                bestDistance = distance;
            }
        }

        return best;
    }

    public VerticalRunnerPlatform GetPromptDirectionalPlatform(float beatPosition, int leadBeats)
    {
        int currentBeat = Mathf.FloorToInt(beatPosition);
        int earliestBeat = currentBeat + 1;
        int latestBeat = currentBeat + Mathf.Max(1, leadBeats);
        VerticalRunnerPlatform best = null;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < platforms.Count; i++)
        {
            VerticalRunnerPlatform platform = platforms[i];
            if (platform == null || !platform.requiresDirectionalChoice || platform.actionBeatIndex < 0)
            {
                continue;
            }

            if (platform.actionBeatIndex < earliestBeat || platform.actionBeatIndex > latestBeat)
            {
                continue;
            }

            int distance = Mathf.Abs(platform.actionBeatIndex - currentBeat);
            if (distance < bestDistance)
            {
                best = platform;
                bestDistance = distance;
            }
        }

        return best;
    }

    public VerticalRunnerPlatform GetDirectionalPlatformForActionBeat(int beatIndex)
    {
        for (int i = 0; i < platforms.Count; i++)
        {
            VerticalRunnerPlatform platform = platforms[i];
            if (platform == null || !platform.requiresDirectionalChoice || platform.actionBeatIndex < 0)
            {
                continue;
            }

            if (platform.actionBeatIndex == beatIndex)
            {
                return platform;
            }
        }

        return null;
    }

    public VerticalRunnerPickup GetMissedCollectibleCoin(Vector3 position, float radius, float beatPosition, float actionWindowBeats)
    {
        VerticalRunnerPickup nearest = null;
        float bestDistance = Mathf.Max(0f, radius);
        for (int i = 0; i < pickups.Count; i++)
        {
            VerticalRunnerPickup pickup = pickups[i];
            if (pickup == null || pickup.collected || pickup.missed || !pickup.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (beatPosition < pickup.beatIndex + 1f)
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
        for (int i = 1; i <= 22; i++)
        {
            beat += settings.beatsPerPlatform;
            bool longJump = i == 14;
            bool strong = beat % 4 == 0;
            bool dangerBranch = i >= 9 && i <= 10;
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

            if (i >= 5 && i <= 7)
            {
                CreateCoin(beat + 1, position + new Vector2(0f, 0.58f));
            }

            if (i >= 17 && i <= 22 && i % 2 == 0)
            {
                CreateCoin(beat + 1, position + new Vector2(0f, 0.58f));
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
            beat += settings.beatsPerPlatform;
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

            bool nextRouteIsDanger = routeIndex + 1 > 10 && (routeIndex + 1) % 7 == 0;
            if (routeIndex > 4 && routeIndex % 2 == 0 && !nextRouteIsDanger)
            {
                CreateCoin(beat + 1, position + new Vector2(0f, 0.58f));
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
            origin.actionBeatIndex = beatIndex - 1;
            origin.safeChoice = safeChoice;
            origin.leftNext = left;
            origin.rightNext = right;
            origin.defaultNext = leftSafe ? left : right;
        }

        MarkBranchSide(left, leftSafe);
        MarkBranchSide(right, !leftSafe);
        safePosition = leftSafe ? leftPosition : rightPosition;
        return leftSafe ? left : right;
    }

    private void MarkBranchSide(VerticalRunnerPlatform platform, bool safe)
    {
        platform.isDangerBranchPlatform = true;
        AddBeatPulse(platform.gameObject, platform.beatIndex);
        if (!safe)
        {
            CreateObstacle(platform.beatIndex, platform.transform.position + new Vector3(0f, 0.42f, 0f));
        }
    }

    private VerticalRunnerPlatform CreatePlatform(int beatIndex, Vector2 position, bool strongBeat, bool longJump, bool isSafePlatform, bool registerByBeat)
    {
        GameObject template = templates != null ? templates.PlatformTemplateFor(longJump) : null;
        GameObject obj = CreateGeneratedObject(template, "VerticalPlatform_Beat_" + beatIndex, position);
        bool usingTemplate = template != null;

        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = obj.AddComponent<SpriteRenderer>();
        }
        if (!usingTemplate)
        {
            renderer.sprite = settings.mushroomSprite != null ? settings.mushroomSprite : circleSprite;
            renderer.color = isSafePlatform ? (strongBeat ? settings.strongPlatformColor : settings.platformColor) : settings.obstacleColor;
            renderer.sortingOrder = 0;
            obj.transform.localScale = longJump ? new Vector3(1.55f, 0.42f, 1f) : new Vector3(1.3f, 0.36f, 1f);
        }
        else if (renderer.sprite == null)
        {
            renderer.sprite = circleSprite;
        }

        BoxCollider2D collider = obj.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.3f, 0.34f);
        }
        else if (!usingTemplate)
        {
            collider.size = new Vector2(1.3f, 0.34f);
        }

        VerticalRunnerPlatform platform = obj.GetComponent<VerticalRunnerPlatform>();
        if (platform == null)
        {
            platform = obj.AddComponent<VerticalRunnerPlatform>();
        }
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
        GameObject template = templates != null ? templates.coinTemplate : null;
        GameObject obj = CreateGeneratedObject(template, "VerticalCoin_Beat_" + beatIndex, position);
        bool usingTemplate = template != null;

        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = obj.AddComponent<SpriteRenderer>();
        }
        if (!usingTemplate)
        {
            renderer.sprite = settings.coinSprite != null ? settings.coinSprite : circleSprite;
            renderer.color = settings.coinColor;
            renderer.sortingOrder = 2;
            obj.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
        }
        else if (renderer.sprite == null)
        {
            renderer.sprite = circleSprite;
        }

        CircleCollider2D collider = obj.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = obj.AddComponent<CircleCollider2D>();
            collider.radius = 0.45f;
        }
        collider.isTrigger = true;

        VerticalRunnerPickup pickup = obj.GetComponent<VerticalRunnerPickup>();
        if (pickup == null)
        {
            pickup = obj.AddComponent<VerticalRunnerPickup>();
        }
        pickup.beatIndex = beatIndex;
        pickup.value = 1;
        pickups.Add(pickup);
    }

    private void CreateObstacle(int beatIndex, Vector2 position)
    {
        GameObject template = templates != null ? templates.obstacleTemplate : null;
        GameObject obj = CreateGeneratedObject(template, "VerticalObstacle_Beat_" + beatIndex, position);
        bool usingTemplate = template != null;

        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = obj.AddComponent<SpriteRenderer>();
        }
        if (!usingTemplate)
        {
            renderer.sprite = settings.obstacleSprite != null ? settings.obstacleSprite : circleSprite;
            renderer.color = settings.obstacleColor;
            renderer.sortingOrder = 2;
            obj.transform.localScale = new Vector3(0.52f, 0.52f, 1f);
        }
        else if (renderer.sprite == null)
        {
            renderer.sprite = circleSprite;
        }

        CircleCollider2D collider = obj.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = obj.AddComponent<CircleCollider2D>();
            collider.radius = 0.48f;
        }
        collider.isTrigger = true;

        VerticalRunnerObstacle obstacle = obj.GetComponent<VerticalRunnerObstacle>();
        if (obstacle == null)
        {
            obstacle = obj.AddComponent<VerticalRunnerObstacle>();
        }
        obstacle.beatIndex = beatIndex;
        obstacles.Add(obstacle);
        AddBeatPulse(obj, beatIndex);
    }

    private void AddBeatPulse(GameObject obj, int beatIndex)
    {
        VerticalRunnerBeatPulse pulse = obj.GetComponent<VerticalRunnerBeatPulse>();
        if (pulse == null)
        {
            pulse = obj.AddComponent<VerticalRunnerBeatPulse>();
        }
        pulse.beatIndex = beatIndex;
    }

    private void CreateFinishMarker(Vector2 position)
    {
        GameObject template = templates != null ? templates.finishTemplate : null;
        GameObject obj = CreateGeneratedObject(template, "VerticalFinish", position);
        if (template == null)
        {
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = circleSprite;
            renderer.color = new Color(1f, 0.86f, 0.18f, 1f);
            renderer.sortingOrder = 1;
            obj.transform.localScale = new Vector3(2.2f, 0.22f, 1f);
        }
        else
        {
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite == null)
            {
                renderer.sprite = circleSprite;
            }
        }
    }

    private GameObject CreateGeneratedObject(GameObject template, string objectName, Vector2 position)
    {
        GameObject obj = template != null ? Instantiate(template) : new GameObject(objectName);
        obj.name = objectName;
        obj.transform.SetParent(root, false);
        obj.transform.position = position;
        obj.SetActive(true);
        return obj;
    }

    private float BeatInterval()
    {
        return 60f / Mathf.Max(1f, settings.bpm);
    }
}
