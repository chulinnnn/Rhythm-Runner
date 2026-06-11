using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural route builder for VerticalRunner: spawns platforms, banana pickups, parrot branches,
/// obstacles, and the finish marker from scene templates or fallback sprites.
/// Build() 总入口：清空 → 建 VerticalRunnerGenerated → 算 totalBeats → BuildRoute
/// BuildRoute() 分发：起点平台 → 教程 or 正式路线
///CreatePlatform() 克隆模板、写 beatIndex、登记 platformByBeat —— 看见的平台和 beat 数据在这里对齐
/// </remarks>
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


    /// 中文：Build 的简化入口，使用默认场景策略。

    public void Build(VerticalRunnerSettings settings, VerticalRunnerMode mode, Sprite fallbackSprite)
    {
        Build(settings, mode, fallbackSprite, RuntimeScenePolicy.Defaults());
    }

   
    /// 中文：不传模板容器时的 Build 重载。
    public void Build(VerticalRunnerSettings settings, VerticalRunnerMode mode, Sprite fallbackSprite, RuntimeScenePolicy scenePolicy)
    {
        Build(settings, mode, fallbackSprite, scenePolicy, null);
    }

    /// <summary>
    /// Rebuilds the full generated route: clears old objects, creates the runtime root, computes total beats,
    /// and dispatches tutorial or game route construction.
    /// </summary>
    /// <remarks>
    /// 中文：路线总入口。清空旧物体 → 挂到 VerticalRunnerGenerated 根节点 → 按模式算总拍数 → 调用 BuildRoute。
    /// 不修改 Hierarchy 里的模板，只重建克隆出来的路线。
    /// </remarks>
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

        if (settings.beatsPerPlatform < 1)
        {
            settings.beatsPerPlatform = 2;
        }

        totalBeats = mode == VerticalRunnerMode.Tutorial
            ? 64
            : Mathf.Max(48, Mathf.CeilToInt(settings.songDurationSeconds / BeatInterval()));

        BuildRoute(mode);
    }

    /// <summary>
    /// Clears cached route lists and destroys all children under the generated root transform.
    /// </summary>
    /// <remarks>
    /// 中文：清空平台/香蕉/障碍列表和 beat 索引表，并销毁已生成子物体。
    /// </remarks>
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

    /// <summary>
    /// Looks up the platform registered at a specific beat index.
    /// </summary>
    /// <remarks>
    /// 中文：按 beat 查平台。开局落点、节拍判定都靠这个索引。
    /// </remarks>
    public VerticalRunnerPlatform GetPlatformForBeat(int beatIndex)
    {
        VerticalRunnerPlatform platform;
        platformByBeat.TryGetValue(beatIndex, out platform);
        return platform;
    }

    /// <summary>
    /// Returns the next platform whose beat index is greater than the given beat.
    /// </summary>
    /// <remarks>
    /// 中文：找「当前 beat 之后」的下一个平台，用于普通 Space 跳跃的目标。
    /// </remarks>
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

    /// <summary>
    /// Finds the platform closest to a world position.
    /// </summary>
    /// <remarks>
    /// 中文：按世界坐标找最近平台，失败恢复或没有 defaultNext 时的兜底。
    /// </remarks>
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

    /// <summary>
    /// Finds the nearest uncollected banana within a collection radius of the player.
    /// </summary>
    /// <remarks>
    /// 中文：在半径内找最近可捡香蕉，供 Player 按 Down/S 收集用。
    /// </remarks>
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

    /// <summary>
    /// Selects the banana whose beat index falls within a forward-looking beat window for UI prompting.
    /// </summary>
    /// <remarks>
    /// 中文：在「当前 beat 往后 leadBeats 拍」窗口内选最近的香蕉，给 Manager 做抓取提示用。
    /// </remarks>
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

    /// <summary>
    /// Returns the banana scheduled exactly at the given beat index, if any.
    /// </summary>
    /// <remarks>
    /// 中文：按 beat 精确查香蕉，用于节拍判定或谱面对齐。
    /// </remarks>
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

    /// <summary>
    /// Finds the banana tied to the beat immediately after a platform's beat, closest to that platform.
    /// </summary>
    /// <remarks>
    /// 中文：找「平台 beat+1」且离该平台最近的香蕉，关联平台与收集事件。
    /// </remarks>
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

    /// <summary>
    /// Selects the parrot branch platform whose action beat falls within a forward-looking beat window for UI prompting.
    /// </summary>
    /// <remarks>
    /// 中文：在向前 leadBeats 窗口内找需要左/右选择的鹦鹉分支平台，供 Manager 显示方向提示。
    /// </remarks>
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

    /// <summary>
    /// Returns the directional-choice platform scheduled at a specific action beat index.
    /// </summary>
    /// <remarks>
    /// 中文：按 action beat 查鹦鹉分支平台，用于该拍的躲鹦鹉判定。
    /// </remarks>
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

    /// <summary>
    /// Finds a nearby banana whose collection beat window has already passed, for missed-pickup reporting.
    /// </summary>
    /// <remarks>
    /// 中文：找「节拍窗口已过」且仍在半径内的香蕉，供 Player 上报漏捡。
    /// </remarks>
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

    /// <summary>
    /// Dispatches tutorial or game route generation after the start platform is created.
    /// </summary>
    /// <remarks>
    /// 中文：路线分发器。先建起点平台，再按模式走教程固定 22 步或游戏 procedural 循环。
    /// </remarks>
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

    /// <summary>
    /// Builds the fixed 22-step tutorial route with scripted bananas, parrot branches, and a long jump.
    /// </summary>
    /// <remarks>
    /// 中文：教程路线。固定 22 步：第 5–7 步香蕉、第 9–10 步鹦鹉、第 14 步长跳、第 17–22 步收尾香蕉。
    /// </remarks>
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

    /// <summary>
    /// Procedurally builds the game route until total beats are consumed, spacing branches, coins, and long jumps.
    /// </summary>
    /// <remarks>
    /// 中文：正式游戏路线。循环到 totalBeats：每 7 步可能鹦鹉分支，偶数步放香蕉（且避开下一格鹦鹉），每 16 beat 长跳。
    /// </remarks>
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

    /// <summary>
    /// Computes the next platform world position using vertical spacing, sine-based lane drift, and optional long-jump spacing.
    /// </summary>
    /// <remarks>
    /// 中文：算下一个平台坐标：Y 用普通或长跳间距，X 用 sin 摆 lane，每 5 步收窄摆动。
    /// </remarks>
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

    /// <summary>
    /// Sets the default forward link from one platform to the next in the main route chain.
    /// </summary>
    /// <remarks>
    /// 中文：把 from.defaultNext 指向下一个平台，构成主路线单向链。
    /// </remarks>
    private void LinkDefault(VerticalRunnerPlatform from, VerticalRunnerPlatform to)
    {
        if (from != null)
        {
            from.defaultNext = to;
        }
    }

    /// <summary>
    /// Creates a left/right parrot branch at a beat: marks the origin for directional choice, spawns safe and danger sides,
    /// and returns the safe landing platform.
    /// </summary>
    /// <remarks>
    /// 中文：鹦鹉分支核心。上一平台设 requiresDirectionalChoice 和 leftNext/rightNext；
    /// 左右各一平台，危险侧放鹦鹉障碍；返回安全侧供路线继续。
    /// </remarks>
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

    /// <summary>
    /// Tags a branch platform as safe or danger and spawns a parrot obstacle on the danger side.
    /// </summary>
    /// <remarks>
    /// 中文：标记分支平台安全/危险；危险侧创建鹦鹉障碍并加节拍脉冲组件。
    /// </remarks>
    private void MarkBranchSide(VerticalRunnerPlatform platform, bool safe)
    {
        platform.isDangerBranchPlatform = true;
        AddBeatPulse(platform.gameObject, platform.beatIndex);
        if (!safe)
        {
            CreateObstacle(platform.beatIndex, platform.transform.position + new Vector3(0f, 0.42f, 0f));
        }
    }

    /// <summary>
    /// Instantiates or creates a platform GameObject, applies template or fallback visuals, and registers route metadata.
    /// </summary>
    /// <remarks>
    /// 中文：创建单个平台：克隆模板或程序生成 Sprite/Collider，写入 beatIndex、longJump 等，并加入列表和 beat 字典。
    /// </remarks>
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

    /// <summary>
    /// Spawns a banana pickup at a beat index and world position.
    /// </summary>
    /// <remarks>
    /// 中文：在指定 beat 和位置生成香蕉，挂触发器 Collider 和 VerticalRunnerPickup。
    /// </remarks>
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

    /// <summary>
    /// Spawns a parrot obstacle trigger at a beat index and world position.
    /// </summary>
    /// <remarks>
    /// 中文：在危险分支侧生成鹦鹉障碍，玩家碰到会触发失败反馈。
    /// </remarks>
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

    /// <summary>
    /// Ensures a beat-pulse component exists on a spawned object for visual rhythm highlighting.
    /// </summary>
    /// <remarks>
    /// 中文：给物体加 VerticalRunnerBeatPulse，让障碍/分支在对应 beat 有视觉脉冲。
    /// </remarks>
    private void AddBeatPulse(GameObject obj, int beatIndex)
    {
        VerticalRunnerBeatPulse pulse = obj.GetComponent<VerticalRunnerBeatPulse>();
        if (pulse == null)
        {
            pulse = obj.AddComponent<VerticalRunnerBeatPulse>();
        }
        pulse.beatIndex = beatIndex;
    }

    /// <summary>
    /// Places the finish marker above the final route position.
    /// </summary>
    /// <remarks>
    /// 中文：在路线末端上方放终点标记，表示可完成关卡。
    /// </remarks>
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

    /// <summary>
    /// Clones a scene template or creates a bare GameObject under the generated root at a world position.
    /// </summary>
    /// <remarks>
    /// 中文：就是在克隆原型template，创建玩家，平台，香蕉等等。其实可以当做prefab
    /// </remarks>
    private GameObject CreateGeneratedObject(GameObject template, string objectName, Vector2 position)
    {
        GameObject obj = template != null ? Instantiate(template) : new GameObject(objectName);
        obj.name = objectName;
        obj.transform.SetParent(root, false);
        obj.transform.position = position;
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// Returns the duration of one beat in seconds from the current BPM setting.
    /// </summary>
    /// <remarks>
    /// 中文：根据 BPM 算一拍多少秒，用于按歌曲长度估算 totalBeats。
    /// </remarks>
    private float BeatInterval()
    {
        return 60f / Mathf.Max(1f, settings.bpm);
    }
}
